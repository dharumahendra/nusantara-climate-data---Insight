namespace Junpro_Project;

public enum Indikator
{
    SuhuRataRata,
    SuhuMaksimum,
    SuhuMinimum,
    CurahHujan,
    HariPanas,
    HariHujan,
    HariHujanLebat
}

// Satu titik koordinat yang mewakili pusat kota/kabupaten (PRD §6.1).
public sealed record Wilayah(string Provinsi, string Kota, double Latitude, double Longitude)
{
    public string NamaLengkap => Kota + ", " + Provinsi;

    public override string ToString() => Kota + " (" + Provinsi + ")";
}

public sealed record HasilTren(double Slope, double Intersep, double RSquared,
                               double PerubahanTotal, string Arah, string Keyakinan)
{
    public double SlopePerDekade => Slope * 10;
    public bool BelumJelas => Keyakinan == "Lemah";
    public double NilaiPada(int tahun) => Intersep + Slope * tahun;
}

public sealed class ProfilIklim
{
    // PRD §15 E-04: di bawah ini garis tren disembunyikan karena data belum cukup.
    public const int MinTahunUntukTren = 10;

    private readonly Dictionary<Indikator, double[]> deret;
    private readonly Dictionary<Indikator, double> baseline;

    public ProfilIklim(Wilayah wilayah, int[] tahun, Dictionary<Indikator, double[]> deret,
                       Dictionary<Indikator, double> baseline, double ambangHariPanas,
                       int[] tahunTidakValid, int jumlahTahunPeriode)
    {
        Wilayah = wilayah;
        Tahun = tahun;
        this.deret = deret;
        this.baseline = baseline;
        AmbangHariPanas = ambangHariPanas;
        TahunTidakValid = tahunTidakValid;
        JumlahTahunPeriode = jumlahTahunPeriode;
    }

    public Wilayah Wilayah { get; }

    // Hanya tahun valid (PRD §6.1); semua deret nilai sejajar dengan array ini.
    public int[] Tahun { get; }
    public int[] TahunTidakValid { get; }
    public int JumlahTahunPeriode { get; }
    public double AmbangHariPanas { get; }

    public int TahunAwal => Tahun[0];
    public int TahunAkhir => Tahun[^1];
    public string Periode => TahunAwal + "–" + TahunAkhir;
    public bool TrenTersedia => Tahun.Length >= MinTahunUntukTren;

    public double[] Nilai(Indikator i) => deret[i];
    public double RataRata(Indikator i) => deret[i].Average();
    public double Terakhir(Indikator i) => deret[i][^1];

    // Rata-rata normal 1991-2020 untuk indikator ini; null bila baseline tidak tersedia.
    public double? Baseline(Indikator i) => baseline.TryGetValue(i, out double nilai) ? nilai : null;

    // Anomali = nilai tahun - nilai baseline (PRD §6.1). Positif = di atas normal.
    public double? Anomali(Indikator i, double nilai) => Baseline(i) is double b ? nilai - b : null;

    public HasilTren Tren(Indikator i) =>
        DataIklim.HitungTren(Tahun, deret[i], DataIklim.AmbangStabil(i, Baseline(i) ?? RataRata(i)));

    public (int Tahun, double Nilai) Tertinggi(Indikator i)
    {
        int idx = Array.IndexOf(deret[i], deret[i].Max());
        return (Tahun[idx], deret[i][idx]);
    }

    public (int Tahun, double Nilai) Terendah(Indikator i)
    {
        int idx = Array.IndexOf(deret[i], deret[i].Min());
        return (Tahun[idx], deret[i][idx]);
    }

    // Profil yang dibatasi ke tahun tertentu saja; dipakai perbandingan agar hanya
    // tahun yang valid di kedua wilayah yang dibandingkan (PRD AC-03.4).
    public ProfilIklim Batasi(IReadOnlyCollection<int> tahunDipakai)
    {
        int[] indeks = Enumerable.Range(0, Tahun.Length).Where(i => tahunDipakai.Contains(Tahun[i])).ToArray();
        if (indeks.Length == 0)
            throw new ArgumentException("Tidak ada tahun yang cocok.", nameof(tahunDipakai));

        Dictionary<Indikator, double[]> baru = deret.ToDictionary(
            p => p.Key, p => indeks.Select(i => p.Value[i]).ToArray());

        return new ProfilIklim(Wilayah, indeks.Select(i => Tahun[i]).ToArray(), baru, baseline,
                               AmbangHariPanas, TahunTidakValid, JumlahTahunPeriode);
    }
}

// Daftar wilayah dan cache profil iklim yang sudah dimuat. Data diambil dari
// Open-Meteo (PRD §8) dan disimpan ke PostgreSQL bila tersedia, sehingga
// wilayah yang pernah diunduh bisa dibuka lagi tanpa internet (PRD D-06).
public static class DataIklim
{
    // Daftar bawaan, dipakai bila database PostgreSQL tidak tersedia. Isinya
    // sama dengan data awal di Database/schema.sql. Minimal satu wilayah dari
    // tiap pulau besar agar perbandingan bermakna (PRD §8.4).
    private static readonly Wilayah[] wilayahBawaan =
    {
        new("DI Yogyakarta", "Kota Yogyakarta", -7.7956, 110.3695),
        new("DI Yogyakarta", "Sleman", -7.7167, 110.3550),
        new("DI Yogyakarta", "Bantul", -7.8881, 110.3289),
        new("DKI Jakarta", "Jakarta Pusat", -6.1862, 106.8342),
        new("DKI Jakarta", "Jakarta Selatan", -6.2615, 106.8106),
        new("DKI Jakarta", "Jakarta Utara", -6.1384, 106.8636),
        new("Jawa Barat", "Bandung", -6.9175, 107.6191),
        new("Jawa Barat", "Bogor", -6.5971, 106.8060),
        new("Jawa Barat", "Bekasi", -6.2383, 106.9756),
        new("Jawa Tengah", "Semarang", -6.9667, 110.4167),
        new("Jawa Tengah", "Surakarta", -7.5755, 110.8243),
        new("Jawa Tengah", "Magelang", -7.4797, 110.2177),
        new("Bali", "Denpasar", -8.6705, 115.2126),
        new("Bali", "Badung", -8.5819, 115.1770),
        new("Bali", "Gianyar", -8.5389, 115.3258),
        new("Sumatera Utara", "Medan", 3.5952, 98.6722),
        new("Kalimantan Timur", "Samarinda", -0.5022, 117.1536),
        new("Kalimantan Timur", "Balikpapan", -1.2379, 116.8529),
        new("Sulawesi Selatan", "Makassar", -5.1477, 119.4327),
        new("Nusa Tenggara Timur", "Kupang", -10.1772, 123.6070),
        new("Maluku", "Ambon", -3.6954, 128.1814),
        new("Papua", "Jayapura", -2.5337, 140.7181),
    };

    private static readonly Dictionary<Wilayah, ProfilIklim> tersimpan = new();

    public static Wilayah[] SemuaWilayah { get; private set; } = wilayahBawaan;

    // Klien yang dipakai MuatAsync; dapat diganti (mis. saat pengujian).
    public static OpenMeteoKlien Klien { get; set; } = OpenMeteoKlien.Bawaan;

    // Dipanggil saat start-up bila daftar wilayah berhasil dibaca dari database.
    public static void GunakanDaftarWilayah(Wilayah[] daftar)
    {
        SemuaWilayah = daftar;
        tersimpan.Clear();
    }

    public static IEnumerable<string> DaftarProvinsi() =>
        SemuaWilayah.Select(w => w.Provinsi).Distinct();

    public static IEnumerable<Wilayah> WilayahDi(string provinsi) =>
        SemuaWilayah.Where(w => w.Provinsi == provinsi);

    public static Wilayah? Cari(string provinsi, string kota) =>
        SemuaWilayah.FirstOrDefault(w => w.Provinsi == provinsi && w.Kota == kota);

    public static Wilayah? CariKota(string kota) =>
        SemuaWilayah.FirstOrDefault(w => string.Equals(w.Kota, kota.Trim(), StringComparison.OrdinalIgnoreCase));

    public static bool SudahDimuat(Wilayah w) => tersimpan.ContainsKey(w);

    public static ProfilIklim? CobaProfil(Wilayah w) => tersimpan.GetValueOrDefault(w);

    // Hanya untuk wilayah yang sudah dimuat lewat MuatAsync.
    public static ProfilIklim Profil(Wilayah w) =>
        CobaProfil(w) ?? throw new InvalidOperationException(
            $"Data {w.NamaLengkap} belum dimuat. Panggil DataIklim.MuatAsync() lebih dulu.");

    // Alur PRD §7: memori -> database lokal -> Open-Meteo. Data dari internet
    // langsung disimpan agar tidak perlu diunduh ulang (PRD D-06).
    public static async Task<ProfilIklim> MuatAsync(Wilayah wilayah, IProgress<string>? progres = null,
                                                    CancellationToken ct = default)
    {
        if (tersimpan.TryGetValue(wilayah, out ProfilIklim? sudahAda)) return sudahAda;

        DateOnly hariIni = DateOnly.FromDateTime(DateTime.Today);
        var (tahunAwal, tahunAkhir) = AnalisisIklim.Periode(hariIni);
        var (awal, akhir) = AnalisisIklim.RentangUnduh(hariIni);
        PenyimpananIklim? penyimpanan = StatusAplikasi.PenyimpananIklim;

        DataHarian? data = null;
        if (penyimpanan != null)
        {
            progres?.Report($"Memeriksa data {wilayah.Kota} yang tersimpan…");
            data = await CobaDatabaseAsync(() => penyimpanan.Ambil(wilayah, awal, akhir));
        }

        if (data == null)
        {
            progres?.Report($"Mengunduh data iklim {wilayah.Kota} {tahunAwal}–{tahunAkhir} dari Open-Meteo…");
            data = await Klien.AmbilHarianAsync(wilayah, awal, akhir, ct);

            if (penyimpanan != null)
            {
                progres?.Report("Menyimpan data ke database lokal…");
                DataHarian untukDisimpan = data;
                await CobaDatabaseAsync<object?>(() =>
                {
                    penyimpanan.Simpan(wilayah, untukDisimpan);
                    return null;
                });
            }
        }

        progres?.Report("Menghitung statistik dan tren…");
        DataHarian dataFinal = data;
        ProfilIklim profil = await Task.Run(() => AnalisisIklim.Bangun(wilayah, dataFinal, tahunAkhir), ct);

        tersimpan[wilayah] = profil;
        return profil;
    }

    // Database hanya cache: kegagalannya dicatat tetapi tidak boleh menggagalkan
    // pemuatan data (PRD §12 Ketahanan).
    private static async Task<T?> CobaDatabaseAsync<T>(Func<T?> kerja)
    {
        try
        {
            return await Task.Run(kerja);
        }
        catch (Exception ex) when (ex is Npgsql.NpgsqlException or InvalidOperationException)
        {
            StatusAplikasi.CatatMasalahDatabase("Penyimpanan data iklim bermasalah: " + ex.Message);
            return default;
        }
    }

    // Ambang perubahan total yang masih dianggap "Stabil" (PRD §6.4).
    public static double AmbangStabil(Indikator i, double rataRata) => i switch
    {
        Indikator.CurahHujan => rataRata * 0.05,
        Indikator.HariPanas or Indikator.HariHujan => 3,
        Indikator.HariHujanLebat => 1,
        _ => 0.2,
    };

    // Regresi linear kuadrat terkecil + R² (PRD §6.3).
    public static HasilTren HitungTren(int[] tahun, double[] nilai, double ambangStabil)
    {
        double xBar = tahun.Average();
        double yBar = nilai.Average();
        double sxy = 0, sxx = 0, syy = 0;

        for (int i = 0; i < nilai.Length; i++)
        {
            double dx = tahun[i] - xBar;
            double dy = nilai[i] - yBar;
            sxy += dx * dy;
            sxx += dx * dx;
            syy += dy * dy;
        }

        double slope = sxx == 0 ? 0 : sxy / sxx;
        double r2 = (sxx == 0 || syy == 0) ? 0 : sxy * sxy / (sxx * syy);
        double total = slope * (tahun[^1] - tahun[0]);

        string arah = Math.Abs(total) < ambangStabil ? "Stabil" : slope > 0 ? "Naik" : "Turun";
        string keyakinan = r2 >= 0.5 ? "Kuat" : r2 >= 0.25 ? "Sedang" : "Lemah";

        return new HasilTren(slope, yBar - slope * xBar, r2, total, arah, keyakinan);
    }
}
