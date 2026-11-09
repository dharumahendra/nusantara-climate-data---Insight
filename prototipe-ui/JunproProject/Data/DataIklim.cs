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

public sealed record Wilayah(string Provinsi, string Kota, double SuhuNormal, double HujanNormal)
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
    private readonly Dictionary<Indikator, double[]> deret;

    public ProfilIklim(Wilayah wilayah, int[] tahun, Dictionary<Indikator, double[]> deret)
    {
        Wilayah = wilayah;
        Tahun = tahun;
        this.deret = deret;
    }

    public Wilayah Wilayah { get; }
    public int[] Tahun { get; }
    public int TahunAwal => Tahun[0];
    public int TahunAkhir => Tahun[^1];
    public string Periode => TahunAwal + "–" + TahunAkhir;

    public double[] Nilai(Indikator i) => deret[i];
    public double RataRata(Indikator i) => deret[i].Average();
    public double Terakhir(Indikator i) => deret[i][^1];

    public HasilTren Tren(Indikator i) =>
        DataIklim.HitungTren(Tahun, deret[i], DataIklim.AmbangStabil(i, RataRata(i)));

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
}

// Sumber data ilustrasi untuk prototipe UI. Deret tahunan dibangkitkan dari
// nilai normal tiap wilayah + tren + variasi acak dengan seed tetap, sehingga
// wilayah yang sama selalu menghasilkan angka yang sama. Nantinya diganti
// data asli Open-Meteo (PRD §8).
public static class DataIklim
{
    public const int TahunAwal = 1995;
    public const int TahunAkhir = 2024;

    // Daftar bawaan, dipakai bila database PostgreSQL tidak tersedia. Isinya
    // sama dengan data awal di Database/schema.sql. Minimal satu wilayah dari
    // tiap pulau besar agar perbandingan bermakna (PRD §8.4).
    private static readonly Wilayah[] wilayahBawaan =
    {
        new("DI Yogyakarta", "Kota Yogyakarta", 26.8, 2100),
        new("DI Yogyakarta", "Sleman", 26.3, 2400),
        new("DI Yogyakarta", "Bantul", 26.9, 1900),
        new("DKI Jakarta", "Jakarta Pusat", 28.0, 1850),
        new("DKI Jakarta", "Jakarta Selatan", 27.6, 2100),
        new("DKI Jakarta", "Jakarta Utara", 28.3, 1700),
        new("Jawa Barat", "Bandung", 23.6, 2300),
        new("Jawa Barat", "Bogor", 25.4, 3900),
        new("Jawa Barat", "Bekasi", 27.8, 1900),
        new("Jawa Tengah", "Semarang", 27.9, 2200),
        new("Jawa Tengah", "Surakarta", 27.1, 2100),
        new("Jawa Tengah", "Magelang", 25.2, 2700),
        new("Bali", "Denpasar", 27.6, 1700),
        new("Bali", "Badung", 27.4, 1800),
        new("Bali", "Gianyar", 26.5, 2200),
        new("Sumatera Utara", "Medan", 27.2, 2200),
        new("Kalimantan Timur", "Samarinda", 27.3, 2400),
        new("Kalimantan Timur", "Balikpapan", 27.1, 2700),
        new("Sulawesi Selatan", "Makassar", 27.5, 2900),
        new("Nusa Tenggara Timur", "Kupang", 27.8, 1400),
        new("Maluku", "Ambon", 26.6, 3400),
        new("Papua", "Jayapura", 27.4, 2500),
    };

    private static readonly Dictionary<Wilayah, ProfilIklim> tersimpan = new();

    public static Wilayah[] SemuaWilayah { get; private set; } = wilayahBawaan;

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

    public static bool SudahTersimpan(Wilayah w) => tersimpan.ContainsKey(w);

    public static ProfilIklim Profil(Wilayah w)
    {
        if (!tersimpan.TryGetValue(w, out ProfilIklim? profil))
        {
            profil = Bangkitkan(w);
            tersimpan[w] = profil;
        }
        return profil;
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

    private static ProfilIklim Bangkitkan(Wilayah w)
    {
        Random acak = new Random(HashStabil(w.Kota));
        int n = TahunAkhir - TahunAwal + 1;
        double tengah = (n - 1) / 2.0;

        double trenSuhu = 0.010 + acak.NextDouble() * 0.030;    // °C per tahun
        double trenHujan = -0.007 + acak.NextDouble() * 0.012;  // fraksi per tahun
        double trenLebat = -0.04 + acak.NextDouble() * 0.12;    // hari per tahun

        int[] tahun = new int[n];
        double[] suhu = new double[n], maks = new double[n], min = new double[n];
        double[] hujan = new double[n], panas = new double[n];
        double[] hariHujan = new double[n], lebat = new double[n];

        for (int i = 0; i < n; i++)
        {
            double t = i - tengah;
            tahun[i] = TahunAwal + i;
            suhu[i] = Math.Round(w.SuhuNormal + trenSuhu * t + Gangguan(acak) * 0.22, 2);
            maks[i] = Math.Round(suhu[i] + 7.4 + Gangguan(acak) * 0.45, 2);
            min[i] = Math.Round(suhu[i] - 6.3 + Gangguan(acak) * 0.45, 2);
            hujan[i] = Math.Round(w.HujanNormal * (1 + trenHujan * t + Gangguan(acak) * 0.10));
            panas[i] = Math.Max(0, Math.Round(36 + (suhu[i] - w.SuhuNormal) * 28 + Gangguan(acak) * 4));
            hariHujan[i] = Math.Round(Math.Clamp(hujan[i] / 14.5 + Gangguan(acak) * 6, 60, 230));
            lebat[i] = Math.Max(0, Math.Round(hujan[i] / 260 + trenLebat * t + Gangguan(acak) * 1.5));
        }

        return new ProfilIklim(w, tahun, new Dictionary<Indikator, double[]>
        {
            [Indikator.SuhuRataRata] = suhu,
            [Indikator.SuhuMaksimum] = maks,
            [Indikator.SuhuMinimum] = min,
            [Indikator.CurahHujan] = hujan,
            [Indikator.HariPanas] = panas,
            [Indikator.HariHujan] = hariHujan,
            [Indikator.HariHujanLebat] = lebat,
        });
    }

    // Variasi acak kira-kira berdistribusi normal (rata-rata 0, simpangan 1).
    private static double Gangguan(Random acak) =>
        (acak.NextDouble() + acak.NextDouble() + acak.NextDouble() - 1.5) * 2;

    // string.GetHashCode() berubah setiap aplikasi dijalankan, jadi pakai FNV-1a
    // supaya angka ilustrasi tetap sama dari sesi ke sesi.
    private static int HashStabil(string teks)
    {
        unchecked
        {
            uint hash = 2166136261;
            foreach (char c in teks)
            {
                hash ^= c;
                hash *= 16777619;
            }
            return (int)hash;
        }
    }
}
