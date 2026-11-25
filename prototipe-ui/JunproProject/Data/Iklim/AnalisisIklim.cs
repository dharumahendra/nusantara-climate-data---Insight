namespace Junpro_Project;

// Mengubah data harian mentah menjadi ProfilIklim dengan rumus PRD §6.
// Semua angka di aplikasi dihitung di sini (D-08), bukan oleh AI.
public static class AnalisisIklim
{
    public const int TahunBaselineAwal = 1991;    // PRD D-05
    public const int TahunBaselineAkhir = 2020;
    public const int PanjangPeriode = 30;         // PRD D-04
    public const int MinHariValid = 330;          // PRD §6.1: tahun valid
    public const double AmbangHariHujan = 1.0;    // mm, PRD §6.2
    public const double AmbangHujanLebat = 50.0;  // mm
    private const double PersentilHariPanas = 0.90;
    private const int HariTundaData = 10;         // data reanalisis tertinggal beberapa hari (PRD §8.1)

    // Tahun berjalan dikecualikan karena belum lengkap (PRD §6.1). Beberapa hari
    // pertama Januari masih memakai tahun sebelumnya karena data ERA5 belum terbit.
    public static int TahunAkhirPeriode(DateOnly hariIni) => hariIni.AddDays(-HariTundaData).Year - 1;

    public static (int Awal, int Akhir) Periode(DateOnly hariIni)
    {
        int akhir = TahunAkhirPeriode(hariIni);
        return (akhir - PanjangPeriode + 1, akhir);
    }

    // Rentang unduhan mencakup baseline 1991-2020 agar P90 hari panas dapat dihitung.
    public static (DateOnly Awal, DateOnly Akhir) RentangUnduh(DateOnly hariIni)
    {
        var (awal, akhir) = Periode(hariIni);
        return (new DateOnly(Math.Min(TahunBaselineAwal, awal), 1, 1), new DateOnly(akhir, 12, 31));
    }

    // Persentil dengan interpolasi linear antar-peringkat (metode yang sama dengan numpy).
    public static double Persentil(IEnumerable<double> nilai, double p)
    {
        double[] urut = nilai.OrderBy(v => v).ToArray();
        if (urut.Length == 0) throw new ArgumentException("Tidak ada nilai untuk dihitung persentilnya.", nameof(nilai));

        double peringkat = p * (urut.Length - 1);
        int bawah = (int)Math.Floor(peringkat);
        int atas = (int)Math.Ceiling(peringkat);
        return urut[bawah] + (urut[atas] - urut[bawah]) * (peringkat - bawah);
    }

    public static ProfilIklim Bangun(Wilayah wilayah, DataHarian data, int tahunAkhir)
    {
        int tahunAwal = tahunAkhir - PanjangPeriode + 1;
        int tahunPertama = Math.Min(TahunBaselineAwal, tahunAwal);

        double p90 = HitungAmbangHariPanas(data);

        Dictionary<int, TahunBerjalan> perTahun = new();
        for (int i = 0; i < data.Jumlah; i++)
        {
            int tahun = data.Tanggal[i].Year;
            if (tahun < tahunPertama || tahun > tahunAkhir) continue;

            if (!perTahun.TryGetValue(tahun, out TahunBerjalan? t))
                perTahun[tahun] = t = new TahunBerjalan();
            t.TambahHari(data.SuhuRata[i], data.SuhuMaks[i], data.SuhuMin[i], data.CurahHujan[i], p90);
        }

        // Tahun tidak valid dikecualikan dari tren dan dicatat agar UI bisa memberi tanda (PRD §6.1).
        List<int> tahunValid = new();
        List<int> tahunTidakValid = new();
        for (int tahun = tahunAwal; tahun <= tahunAkhir; tahun++)
        {
            if (perTahun.TryGetValue(tahun, out TahunBerjalan? t) && t.Valid) tahunValid.Add(tahun);
            else tahunTidakValid.Add(tahun);
        }

        if (tahunValid.Count == 0)
            throw new ClimateDataFormatException(
                $"Tidak ada tahun yang memenuhi syarat ≥ {MinHariValid} hari data pada {tahunAwal}–{tahunAkhir}");

        Dictionary<Indikator, double[]> deret = new();
        foreach (Indikator indikator in Enum.GetValues<Indikator>())
            deret[indikator] = tahunValid.Select(tahun => perTahun[tahun].Nilai(indikator)).ToArray();

        // Baseline normal = rata-rata nilai tahunan pada 1991-2020 (PRD D-05).
        Dictionary<Indikator, double> baseline = new();
        List<TahunBerjalan> tahunBaseline = perTahun
            .Where(p => p.Key is >= TahunBaselineAwal and <= TahunBaselineAkhir && p.Value.Valid)
            .Select(p => p.Value)
            .ToList();
        if (tahunBaseline.Count > 0)
            foreach (Indikator indikator in Enum.GetValues<Indikator>())
                baseline[indikator] = tahunBaseline.Average(t => t.Nilai(indikator));

        return new ProfilIklim(wilayah, tahunValid.ToArray(), deret, baseline, p90,
                               tahunTidakValid.ToArray(), PanjangPeriode);
    }

    // P90 dari suhu maksimum harian pada baseline 1991-2020 untuk wilayah ini (PRD §6.2).
    public static double HitungAmbangHariPanas(DataHarian data)
    {
        List<double> maksBaseline = new();
        for (int i = 0; i < data.Jumlah; i++)
        {
            int tahun = data.Tanggal[i].Year;
            if (tahun is >= TahunBaselineAwal and <= TahunBaselineAkhir && data.SuhuMaks[i] is double v)
                maksBaseline.Add(v);
        }

        if (maksBaseline.Count == 0)
            throw new ClimateDataFormatException("Data baseline 1991–2020 tidak tersedia untuk menghitung hari panas");

        return Persentil(maksBaseline, PersentilHariPanas);
    }

    // Penampung hitungan satu tahun kalender (PRD §6.2).
    private sealed class TahunBerjalan
    {
        private int hariLengkap;
        private int jumlahSuhu;
        private double totalSuhu;
        private double? maks;
        private double? min;
        private double totalHujan;
        private int hariHujan;
        private int hujanLebat;
        private int hariPanas;

        public bool Valid => hariLengkap >= MinHariValid && jumlahSuhu > 0 && maks != null && min != null;

        public void TambahHari(double? rata, double? suhuMaks, double? suhuMin, double? hujan, double ambangPanas)
        {
            if (rata is double r) { totalSuhu += r; jumlahSuhu++; }

            if (suhuMaks is double mk)
            {
                maks = maks is double lama ? Math.Max(lama, mk) : mk;
                if (mk >= ambangPanas) hariPanas++;
            }

            if (suhuMin is double mn)
                min = min is double lama ? Math.Min(lama, mn) : mn;

            if (hujan is double h)
            {
                totalHujan += h;
                if (h >= AmbangHariHujan) hariHujan++;
                if (h >= AmbangHujanLebat) hujanLebat++;
            }

            if (rata != null && suhuMaks != null && suhuMin != null && hujan != null) hariLengkap++;
        }

        public double Nilai(Indikator indikator) => indikator switch
        {
            Indikator.SuhuRataRata => totalSuhu / jumlahSuhu,
            Indikator.SuhuMaksimum => maks ?? double.NaN,
            Indikator.SuhuMinimum => min ?? double.NaN,
            Indikator.CurahHujan => totalHujan,
            Indikator.HariPanas => hariPanas,
            Indikator.HariHujan => hariHujan,
            _ => hujanLebat,
        };
    }
}
