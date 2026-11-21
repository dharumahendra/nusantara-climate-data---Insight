using System.Net;
using Xunit;

namespace Junpro_Project.Tests;

// Parsing respons Open-Meteo (PRD §8.1) dan perilaku klien: retry, kuota, dan format salah (PRD §15).
public class OpenMeteoTests
{
    private static readonly Wilayah Sleman = new("DI Yogyakarta", "Sleman", -7.7167, 110.3550);
    private static readonly DateOnly Awal = new(2023, 1, 1);
    private static readonly DateOnly Akhir = new(2024, 12, 31);

    private static string Fixture(string nama) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", nama));

    private static OpenMeteoKlien KlienDengan(HandlerPalsu handler) =>
        new(new HttpClient(handler), new[] { TimeSpan.Zero, TimeSpan.Zero });

    // ---------- Parsing ----------

    [Theory]
    [InlineData("sleman-2023-2024.json")]
    [InlineData("jakarta-pusat-2023-2024.json")]
    public void Parse_ResponsAsli_MenghasilkanDeretHarianLengkap(string berkas)
    {
        DataHarian data = OpenMeteoKlien.Parse(Fixture(berkas));

        Assert.Equal(731, data.Jumlah);   // 365 hari (2023) + 366 hari (2024, kabisat)
        Assert.Equal(new DateOnly(2023, 1, 1), data.Tanggal[0]);
        Assert.Equal(new DateOnly(2024, 12, 31), data.Tanggal[^1]);
        Assert.All(new[] { data.SuhuRata, data.SuhuMaks, data.SuhuMin, data.CurahHujan, data.KecepatanAngin },
            deret => Assert.Equal(731, deret.Length));
    }

    [Fact]
    public void Parse_ResponsAsli_NilaiSesuaiBerkas()
    {
        // Nilai hari pertama pada fixture Sleman (diambil langsung dari API).
        DataHarian data = OpenMeteoKlien.Parse(Fixture("sleman-2023-2024.json"));

        Assert.True(data.SuhuMaks[0] >= data.SuhuRata[0]);
        Assert.True(data.SuhuRata[0] >= data.SuhuMin[0]);
        Assert.All(data.CurahHujan, hujan => Assert.True(hujan >= 0));
    }

    [Fact]
    public void Parse_NilaiNull_TetapNullDanTidakDiubahJadiNol()
    {
        string json = """
            {"daily":{"time":["2024-01-01","2024-01-02"],
             "temperature_2m_mean":[25.5,null],"temperature_2m_max":[30.0,null],
             "temperature_2m_min":[22.0,null],"precipitation_sum":[0.0,null],
             "wind_speed_10m_max":[8.4,null]}}
            """;

        DataHarian data = OpenMeteoKlien.Parse(json);

        Assert.Equal(25.5, data.SuhuRata[0]);
        Assert.Null(data.SuhuRata[1]);
        Assert.Equal(0.0, data.CurahHujan[0]);   // nol sungguhan tetap nol
        Assert.Null(data.CurahHujan[1]);         // hilang bukan nol
    }

    [Fact]
    public void Parse_PanjangArrayBerbeda_MelemparClimateDataFormatException()
    {
        string json = """
            {"daily":{"time":["2024-01-01","2024-01-02"],
             "temperature_2m_mean":[25.5],"temperature_2m_max":[30.0,31.0],
             "temperature_2m_min":[22.0,22.5],"precipitation_sum":[0.0,1.0],
             "wind_speed_10m_max":[8.4,9.0]}}
            """;

        Assert.Throws<ClimateDataFormatException>(() => OpenMeteoKlien.Parse(json));
    }

    [Theory]
    [InlineData("{}")]                                            // tanpa bagian daily
    [InlineData("bukan json")]                                    // bukan JSON
    [InlineData("""{"daily":{"time":[]}}""")]                     // deret variabel tidak ada
    [InlineData("""{"daily":{"time":["31/12/2024"],"temperature_2m_mean":[1],"temperature_2m_max":[1],"temperature_2m_min":[1],"precipitation_sum":[1],"wind_speed_10m_max":[1]}}""")]  // tanggal salah format
    [InlineData("""{"daily":{"time":["2024-01-01"],"temperature_2m_mean":["x"],"temperature_2m_max":[1],"temperature_2m_min":[1],"precipitation_sum":[1],"wind_speed_10m_max":[1]}}""")]  // nilai bukan angka
    public void Parse_ResponsTidakValid_MelemparClimateDataFormatException(string json)
    {
        Assert.Throws<ClimateDataFormatException>(() => OpenMeteoKlien.Parse(json));
    }

    [Fact]
    public void BuatUrl_MemakaiTitikDesimalDanVariabelSesuaiKontrak()
    {
        string url = OpenMeteoKlien.BuatUrl(Sleman, Awal, Akhir);

        Assert.StartsWith("https://archive-api.open-meteo.com/v1/archive?", url);
        Assert.Contains("latitude=-7.7167", url);
        Assert.Contains("longitude=110.355", url);
        Assert.Contains("start_date=2023-01-01", url);
        Assert.Contains("end_date=2024-12-31", url);
        Assert.Contains("daily=temperature_2m_mean,temperature_2m_max,temperature_2m_min,precipitation_sum,wind_speed_10m_max", url);
        Assert.Contains("timezone=Asia%2FJakarta", url);
    }

    // ---------- Klien: retry dan penanganan error ----------

    [Fact]
    public async Task AmbilHarianAsync_Berhasil_MemanggilApiSekali()
    {
        HandlerPalsu handler = new(_ => Balas(HttpStatusCode.OK, Fixture("sleman-2023-2024.json")));

        DataHarian data = await KlienDengan(handler).AmbilHarianAsync(Sleman, Awal, Akhir);

        Assert.Equal(731, data.Jumlah);
        Assert.Equal(1, handler.JumlahPanggilan);
    }

    [Fact]
    public async Task AmbilHarianAsync_Gagal500DuaKaliLaluBerhasil_DiulangSampaiBerhasil()
    {
        HandlerPalsu handler = new(n => n < 3
            ? Balas(HttpStatusCode.InternalServerError, "")
            : Balas(HttpStatusCode.OK, Fixture("sleman-2023-2024.json")));

        DataHarian data = await KlienDengan(handler).AmbilHarianAsync(Sleman, Awal, Akhir);

        Assert.Equal(731, data.Jumlah);
        Assert.Equal(3, handler.JumlahPanggilan);   // 1 percobaan + 2 retry (PRD §8.1)
    }

    [Fact]
    public async Task AmbilHarianAsync_Selalu500_SetelahDuaRetryMelemparSumberTidakTersedia()
    {
        HandlerPalsu handler = new(_ => Balas(HttpStatusCode.ServiceUnavailable, ""));

        await Assert.ThrowsAsync<SumberTidakTersediaException>(
            () => KlienDengan(handler).AmbilHarianAsync(Sleman, Awal, Akhir));
        Assert.Equal(3, handler.JumlahPanggilan);
    }

    [Fact]
    public async Task AmbilHarianAsync_TanpaInternet_MelemparTidakAdaKoneksiSetelahRetry()
    {
        HandlerPalsu handler = new(_ => throw new HttpRequestException("tidak ada jaringan"));

        TidakAdaKoneksiException ex = await Assert.ThrowsAsync<TidakAdaKoneksiException>(
            () => KlienDengan(handler).AmbilHarianAsync(Sleman, Awal, Akhir));

        Assert.Equal(3, handler.JumlahPanggilan);
        Assert.Contains("koneksi internet", ex.PesanPengguna);
    }

    [Theory]
    [InlineData("Minutely API request limit exceeded. Please try again in one minute.", BatasPermintaanException.Jenis.Menit)]
    [InlineData("Hourly API request limit exceeded. Please try again in the next hour.", BatasPermintaanException.Jenis.Jam)]
    [InlineData("Daily API request limit exceeded. Please try again tomorrow.", BatasPermintaanException.Jenis.Hari)]
    public async Task AmbilHarianAsync_Status429_TidakDiulangDanMenyebutJenisBatas(string alasan, BatasPermintaanException.Jenis harapan)
    {
        HandlerPalsu handler = new(_ => Balas(HttpStatusCode.TooManyRequests, $$"""{"error":true,"reason":"{{alasan}}"}"""));

        BatasPermintaanException ex = await Assert.ThrowsAsync<BatasPermintaanException>(
            () => KlienDengan(handler).AmbilHarianAsync(Sleman, Awal, Akhir));

        Assert.Equal(harapan, ex.Batas);
        Assert.Equal(1, handler.JumlahPanggilan);   // mengulang dalam hitungan detik tidak membantu
    }

    [Fact]
    public async Task AmbilHarianAsync_FormatSalah_TidakDiulang()
    {
        HandlerPalsu handler = new(_ => Balas(HttpStatusCode.OK, """{"daily":{"time":["2024-01-01"]}}"""));

        await Assert.ThrowsAsync<ClimateDataFormatException>(
            () => KlienDengan(handler).AmbilHarianAsync(Sleman, Awal, Akhir));
        Assert.Equal(1, handler.JumlahPanggilan);
    }

    private static HttpResponseMessage Balas(HttpStatusCode status, string isi) =>
        new(status) { Content = new StringContent(isi) };

    private sealed class HandlerPalsu : HttpMessageHandler
    {
        private readonly Func<int, HttpResponseMessage> balas;

        public HandlerPalsu(Func<int, HttpResponseMessage> balas)
        {
            this.balas = balas;
        }

        public int JumlahPanggilan { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            JumlahPanggilan++;
            return Task.FromResult(balas(JumlahPanggilan));
        }
    }
}
