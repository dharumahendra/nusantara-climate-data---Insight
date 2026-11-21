using System.Globalization;
using System.Net;
using System.Text.Json;

namespace Junpro_Project;

// Klien Open-Meteo Historical Weather API (ERA5) sesuai kontrak PRD §8.1.
// Pola yang sama dengan kelas Ongkir di Lab 10.1: satu kelas yang membungkus
// panggilan REST sehingga form tidak perlu tahu detail HTTP/JSON-nya.
public sealed class OpenMeteoKlien
{
    private const string UrlArsip = "https://archive-api.open-meteo.com/v1/archive";
    private const string VariabelHarian =
        "temperature_2m_mean,temperature_2m_max,temperature_2m_min,precipitation_sum,wind_speed_10m_max";

    // PRD §8.1: timeout 30 detik, retry maksimal 2x dengan jeda naik (1 dtk, 3 dtk).
    public static readonly OpenMeteoKlien Bawaan = new(
        BuatHttpClient(),
        new[] { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3) });

    private readonly HttpClient http;
    private readonly TimeSpan[] jedaUlang;

    // Kompresi memangkas respons 30 tahun dari ±485 KB menjadi ±100 KB, sehingga unduhan
    // pertama jauh lebih cepat (target PRD AC-01.1: <= 15 detik).
    private static HttpClient BuatHttpClient() => new(
        new HttpClientHandler { AutomaticDecompression = DecompressionMethods.All })
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    public OpenMeteoKlien(HttpClient http, TimeSpan[] jedaUlang)
    {
        this.http = http;
        this.jedaUlang = jedaUlang;
    }

    public async Task<DataHarian> AmbilHarianAsync(Wilayah wilayah, DateOnly awal, DateOnly akhir,
                                                   CancellationToken ct = default)
    {
        string url = BuatUrl(wilayah, awal, akhir);

        for (int percobaan = 0; ; percobaan++)
        {
            try
            {
                return await KirimAsync(url, ct);
            }
            catch (SumberDataException ex) when (ex is TidakAdaKoneksiException or SumberTidakTersediaException &&
                                                 percobaan < jedaUlang.Length)
            {
                // Hanya gangguan jaringan/server yang diulang. Format salah atau kuota habis
                // tidak akan membaik dalam hitungan detik.
                await Task.Delay(jedaUlang[percobaan], ct);
            }
        }
    }

    public static string BuatUrl(Wilayah wilayah, DateOnly awal, DateOnly akhir)
    {
        CultureInfo inv = CultureInfo.InvariantCulture;
        return $"{UrlArsip}?latitude={wilayah.Latitude.ToString("0.0###", inv)}" +
               $"&longitude={wilayah.Longitude.ToString("0.0###", inv)}" +
               $"&start_date={awal.ToString("yyyy-MM-dd", inv)}" +
               $"&end_date={akhir.ToString("yyyy-MM-dd", inv)}" +
               $"&daily={VariabelHarian}&timezone=Asia%2FJakarta";
    }

    private async Task<DataHarian> KirimAsync(string url, CancellationToken ct)
    {
        string isi;
        try
        {
            using HttpResponseMessage respons = await http.GetAsync(url, ct);
            if (respons.StatusCode == HttpStatusCode.TooManyRequests)
                throw new BatasPermintaanException(BacaJenisBatas(await respons.Content.ReadAsStringAsync(ct)));

            if (!respons.IsSuccessStatusCode)
                throw new SumberTidakTersediaException($"Open-Meteo membalas dengan status {(int)respons.StatusCode}");

            isi = await respons.Content.ReadAsStringAsync(ct);
        }
        catch (HttpRequestException ex)
        {
            throw new TidakAdaKoneksiException("Open-Meteo tidak terjangkau", ex);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new TidakAdaKoneksiException("Open-Meteo terlalu lama merespons", ex);
        }

        return Parse(isi);
    }

    // Isi 429 Open-Meteo: {"error":true,"reason":"Minutely API request limit exceeded..."}.
    public static BatasPermintaanException.Jenis BacaJenisBatas(string isi)
    {
        string alasan = isi.ToLowerInvariant();
        if (alasan.Contains("daily")) return BatasPermintaanException.Jenis.Hari;
        if (alasan.Contains("hourly")) return BatasPermintaanException.Jenis.Jam;
        return BatasPermintaanException.Jenis.Menit;
    }

    // Memvalidasi bentuk respons lalu mengubahnya menjadi DataHarian. Tidak
    // menyimpan apa pun bila ada yang janggal (PRD §15 E-03: jangan simpan data parsial).
    public static DataHarian Parse(string json)
    {
        try
        {
            using JsonDocument doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("daily", out JsonElement daily))
                throw new ClimateDataFormatException("Respons tidak memuat bagian 'daily'");

            JsonElement waktu = Ambil(daily, "time");
            DateOnly[] tanggal = waktu.EnumerateArray()
                .Select(t => DateOnly.ParseExact(t.GetString()!, "yyyy-MM-dd", CultureInfo.InvariantCulture))
                .ToArray();

            double?[] suhuRata = BacaDeret(daily, "temperature_2m_mean", tanggal.Length);
            double?[] suhuMaks = BacaDeret(daily, "temperature_2m_max", tanggal.Length);
            double?[] suhuMin = BacaDeret(daily, "temperature_2m_min", tanggal.Length);
            double?[] hujan = BacaDeret(daily, "precipitation_sum", tanggal.Length);
            double?[] angin = BacaDeret(daily, "wind_speed_10m_max", tanggal.Length);

            if (tanggal.Length == 0)
                throw new ClimateDataFormatException("Respons tidak berisi data harian");

            return new DataHarian(tanggal, suhuRata, suhuMaks, suhuMin, hujan, angin);
        }
        catch (Exception ex) when (ex is JsonException or FormatException or InvalidOperationException)
        {
            throw new ClimateDataFormatException("Respons tidak dapat dibaca: " + ex.Message, ex);
        }
    }

    private static JsonElement Ambil(JsonElement daily, string nama)
    {
        if (!daily.TryGetProperty(nama, out JsonElement elemen) || elemen.ValueKind != JsonValueKind.Array)
            throw new ClimateDataFormatException($"Respons tidak memuat deret '{nama}'");
        return elemen;
    }

    // Array paralel: panjangnya harus sama dengan jumlah tanggal. Null tetap null.
    private static double?[] BacaDeret(JsonElement daily, string nama, int panjangDiharapkan)
    {
        JsonElement deret = Ambil(daily, nama);
        if (deret.GetArrayLength() != panjangDiharapkan)
            throw new ClimateDataFormatException(
                $"Panjang deret '{nama}' ({deret.GetArrayLength()}) berbeda dari jumlah tanggal ({panjangDiharapkan})");

        return deret.EnumerateArray().Select(e => e.ValueKind switch
        {
            JsonValueKind.Null => (double?)null,
            JsonValueKind.Number => e.GetDouble(),
            _ => throw new ClimateDataFormatException($"Nilai tidak valid pada deret '{nama}'")
        }).ToArray();
    }
}
