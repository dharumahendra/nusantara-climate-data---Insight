using Xunit;

namespace Junpro_Project.Tests;

// Rumus PRD §6: tahun valid, persentil hari panas, indikator tahunan, tren (OLS), dan ambang stabil.
public class AnalisisIklimTests
{
    private static readonly Wilayah Uji = new("DI Yogyakarta", "Sleman", -7.7167, 110.3550);
    private const int TahunAkhir = 2025;   // periode analisis 1996-2025

    private record Hari(double? Rata, double? Maks, double? Min, double? Hujan);

    // Membangun deret harian sintetik 1991-2025 dengan satu fungsi per hari.
    private static DataHarian Bangun(Func<DateOnly, Hari> isi)
    {
        List<DateOnly> tanggal = new();
        for (DateOnly d = new(1991, 1, 1); d <= new DateOnly(TahunAkhir, 12, 31); d = d.AddDays(1))
            tanggal.Add(d);

        Hari[] hari = tanggal.Select(isi).ToArray();
        return new DataHarian(tanggal.ToArray(),
            hari.Select(h => h.Rata).ToArray(), hari.Select(h => h.Maks).ToArray(),
            hari.Select(h => h.Min).ToArray(), hari.Select(h => h.Hujan).ToArray(),
            hari.Select(_ => (double?)5.0).ToArray());
    }

    private static Hari Normal(double rata = 25, double hujan = 5) => new(rata, rata + 5, rata - 4, hujan);

    private static ProfilIklim Profil(Func<DateOnly, Hari> isi) =>
        AnalisisIklim.Bangun(Uji, Bangun(isi), TahunAkhir);

    // ---------- Periode ----------

    [Theory]
    [InlineData(2026, 11, 21, 2025)]   // akhir tahun: tahun berjalan dikecualikan
    [InlineData(2026, 1, 11, 2025)]
    [InlineData(2026, 1, 5, 2024)]     // data ERA5 baru terbit beberapa hari kemudian
    [InlineData(2025, 6, 1, 2024)]
    public void TahunAkhirPeriode_TahunBerjalanDikecualikan(int tahun, int bulan, int hari, int harapan)
    {
        Assert.Equal(harapan, AnalisisIklim.TahunAkhirPeriode(new DateOnly(tahun, bulan, hari)));
    }

    [Fact]
    public void Periode_TigaPuluhTahunKalenderPenuhTerakhir()
    {
        var (awal, akhir) = AnalisisIklim.Periode(new DateOnly(2026, 11, 21));

        Assert.Equal((1996, 2025), (awal, akhir));
    }

    [Fact]
    public void RentangUnduh_MencakupBaseline1991SampaiAkhirTahunPenuhTerakhir()
    {
        var (awal, akhir) = AnalisisIklim.RentangUnduh(new DateOnly(2026, 11, 21));

        Assert.Equal(new DateOnly(1991, 1, 1), awal);
        Assert.Equal(new DateOnly(2025, 12, 31), akhir);   // bukan hari ini (PRD §8.1)
    }

    // ---------- Persentil ----------

    [Fact]
    public void Persentil_InterpolasiLinear()
    {
        double[] nilai = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

        Assert.Equal(9.1, AnalisisIklim.Persentil(nilai, 0.90), 10);
        Assert.Equal(5.5, AnalisisIklim.Persentil(nilai, 0.50), 10);
        Assert.Equal(1.0, AnalisisIklim.Persentil(nilai, 0.0), 10);
        Assert.Equal(10.0, AnalisisIklim.Persentil(nilai, 1.0), 10);
    }

    [Fact]
    public void Persentil_UrutanInputTidakMemengaruhiHasil()
    {
        Assert.Equal(
            AnalisisIklim.Persentil(new double[] { 5, 1, 4, 2, 3 }, 0.9),
            AnalisisIklim.Persentil(new double[] { 1, 2, 3, 4, 5 }, 0.9));
    }

    [Fact]
    public void Persentil_TanpaNilai_MelemparArgumentException()
    {
        Assert.Throws<ArgumentException>(() => AnalisisIklim.Persentil(Array.Empty<double>(), 0.9));
    }

    // ---------- Tren (regresi linear) ----------

    [Fact]
    public void HitungTren_ContohBukuTeks_SlopeDanRSquaredTepat()
    {
        // x = 1..5, y = 2,4,5,4,5  =>  slope 0,6 ; R² 0,6 (hitung tangan: Sxy=6, Sxx=10, Syy=6)
        HasilTren tren = DataIklim.HitungTren(new[] { 1, 2, 3, 4, 5 }, new[] { 2.0, 4, 5, 4, 5 }, 0);

        Assert.Equal(0.6, tren.Slope, 10);
        Assert.Equal(0.6, tren.RSquared, 10);
        Assert.Equal(2.4, tren.PerubahanTotal, 10);   // slope x (5 - 1)
        Assert.Equal(6.0, tren.SlopePerDekade, 10);
    }

    [Fact]
    public void HitungTren_GarisLurus_RSquaredSatuDanKeyakinanKuat()
    {
        int[] tahun = Enumerable.Range(1996, 30).ToArray();
        double[] nilai = tahun.Select(t => 25 + 0.03 * (t - 1996)).ToArray();

        HasilTren tren = DataIklim.HitungTren(tahun, nilai, 0.2);

        Assert.Equal(0.03, tren.Slope, 10);
        Assert.Equal(1.0, tren.RSquared, 10);
        Assert.Equal(0.87, tren.PerubahanTotal, 10);
        Assert.Equal("Naik", tren.Arah);
        Assert.Equal("Kuat", tren.Keyakinan);
        Assert.Equal(25 + 0.03 * 29, tren.NilaiPada(2025), 10);
    }

    [Fact]
    public void HitungTren_DataKonstan_StabilDanKeyakinanLemah()
    {
        HasilTren tren = DataIklim.HitungTren(new[] { 2000, 2001, 2002, 2003 }, new[] { 7.0, 7, 7, 7 }, 0.2);

        Assert.Equal(0, tren.Slope);
        Assert.Equal(0, tren.RSquared);
        Assert.Equal("Stabil", tren.Arah);
        Assert.Equal("Lemah", tren.Keyakinan);
    }

    [Theory]
    [InlineData(0.50, "Kuat")]
    [InlineData(0.49, "Sedang")]
    [InlineData(0.25, "Sedang")]
    [InlineData(0.24, "Lemah")]
    public void HitungTren_AmbangKeyakinanSesuaiPrd(double rSquaredDiinginkan, string harapan)
    {
        // Menyusun data berpola y = x + derau agar R² tepat di batas: R² = 1 / (1 + derau²/varians)
        // sehingga cukup memakai deret yang R²-nya dihitung langsung, lalu memeriksa kelasnya.
        int[] x = Enumerable.Range(0, 11).ToArray();
        double derau = Math.Sqrt(110.0 * (1 - rSquaredDiinginkan) / rSquaredDiinginkan) / Math.Sqrt(11);
        double[] y = x.Select(v => v + (v % 2 == 0 ? derau : -derau)).ToArray();

        HasilTren tren = DataIklim.HitungTren(x, y, 0);

        Assert.Equal(harapan, tren.Keyakinan);
    }

    // ---------- Ambang "Stabil" (PRD §6.4) ----------

    [Theory]
    [InlineData(0.005, "Stabil")]   // total 0,145 °C  < 0,2
    [InlineData(0.010, "Naik")]     // total 0,29 °C   >= 0,2
    [InlineData(-0.010, "Turun")]
    public void Tren_Suhu_AmbangStabil02Derajat(double kenaikanPerTahun, string harapan)
    {
        ProfilIklim profil = Profil(d => Normal(25 + kenaikanPerTahun * (d.Year - 1996)));

        Assert.Equal(harapan, profil.Tren(Indikator.SuhuRataRata).Arah);
    }

    [Theory]
    [InlineData(0.001, "Stabil")]   // total ±10 mm  << 5% dari baseline (±97 mm)
    [InlineData(0.020, "Naik")]     // total ±211 mm >> 5%
    public void Tren_CurahHujan_AmbangStabilLimaPersenDariBaseline(double kenaikanHariPerTahun, string harapan)
    {
        ProfilIklim profil = Profil(d => Normal(hujan: 5 + kenaikanHariPerTahun * (d.Year - 1996)));

        Assert.Equal(harapan, profil.Tren(Indikator.CurahHujan).Arah);
    }

    // ---------- Tahun valid (PRD §6.1) ----------

    [Fact]
    public void Bangun_TahunDenganKurangDari330HariDikecualikan()
    {
        // 2000: hanya 329 hari pertama berisi data (tidak valid); 2001: tepat 330 hari (valid).
        ProfilIklim profil = Profil(d => d.Year switch
        {
            2000 when d.DayOfYear > 329 => new Hari(null, null, null, null),
            2001 when d.DayOfYear > 330 => new Hari(null, null, null, null),
            _ => Normal()
        });

        Assert.Contains(2000, profil.TahunTidakValid);
        Assert.DoesNotContain(2000, profil.Tahun);
        Assert.Contains(2001, profil.Tahun);
        Assert.DoesNotContain(2001, profil.TahunTidakValid);
        Assert.Equal(29, profil.Tahun.Length);
        Assert.Equal(30, profil.JumlahTahunPeriode);
    }

    [Fact]
    public void Bangun_HariDenganSebagianNilaiHilang_TidakDihitungLengkap()
    {
        // Suhu ada tetapi hujan null sepanjang 2010 -> hari tidak lengkap -> 2010 tidak valid.
        ProfilIklim profil = Profil(d => d.Year == 2010 ? Normal() with { Hujan = null } : Normal());

        Assert.Contains(2010, profil.TahunTidakValid);
    }

    [Fact]
    public void Bangun_TahunDiLuarPeriode30TahunTidakMasukDeret()
    {
        ProfilIklim profil = Profil(_ => Normal());

        Assert.Equal(1996, profil.TahunAwal);
        Assert.Equal(2025, profil.TahunAkhir);
        Assert.Equal(30, profil.Tahun.Length);
        Assert.Empty(profil.TahunTidakValid);
    }

    [Fact]
    public void Bangun_KurangDariSepuluhTahunValid_TrenTidakTersedia()
    {
        // Hanya 2018-2025 (8 tahun) yang punya data.
        ProfilIklim profil = Profil(d => d.Year >= 2018 ? Normal() : new Hari(null, null, null, null));

        Assert.Equal(8, profil.Tahun.Length);
        Assert.False(profil.TrenTersedia);
    }

    [Fact]
    public void Bangun_TanpaSatuPunTahunValid_MelemparClimateDataFormatException()
    {
        Assert.Throws<ClimateDataFormatException>(() => Profil(_ => new Hari(null, null, null, null)));
    }

    // ---------- Indikator tahunan (PRD §6.2) ----------

    [Fact]
    public void Bangun_HariHujanDanHujanLebat_MemakaiAmbang1MmDan50Mm()
    {
        // Pada 2025: 0,99 mm (bukan hari hujan), 1,0 mm (hari hujan), 49,99 mm (bukan lebat), 50 mm (lebat).
        double[] hujan2025 = { 0.99, 1.0, 49.99, 50.0 };
        ProfilIklim profil = Profil(d => d.Year == 2025 && d.DayOfYear <= 4
            ? Normal(hujan: hujan2025[d.DayOfYear - 1])
            : Normal(hujan: 0));

        Assert.Equal(3, profil.Terakhir(Indikator.HariHujan));       // 1,0 ; 49,99 ; 50
        Assert.Equal(1, profil.Terakhir(Indikator.HariHujanLebat));  // hanya 50
        Assert.Equal(0.99 + 1.0 + 49.99 + 50.0, profil.Terakhir(Indikator.CurahHujan), 6);
    }

    [Fact]
    public void Bangun_SuhuMaksDanMin_AdalahNilaiTertinggiDanTerendahDalamTahun()
    {
        ProfilIklim profil = Profil(d => d is { Year: 2025, Month: 3, Day: 10 }
            ? new Hari(26, 38.5, 12.5, 5)
            : Normal());

        Assert.Equal(38.5, profil.Terakhir(Indikator.SuhuMaksimum));
        Assert.Equal(12.5, profil.Terakhir(Indikator.SuhuMinimum));
    }

    [Fact]
    public void Bangun_SuhuRataRata_AdalahRataAritmetikHarian()
    {
        // Setengah tahun 24 °C dan setengah lagi 26 °C pada 2025 (365 hari: 182 + 183).
        ProfilIklim profil = Profil(d => d.Year == 2025 ? Normal(d.DayOfYear <= 182 ? 24 : 26) : Normal());

        double harapan = (182 * 24 + 183 * 26) / 365.0;
        Assert.Equal(harapan, profil.Terakhir(Indikator.SuhuRataRata), 10);
    }

    [Fact]
    public void Bangun_HariPanas_AdalahHariDenganSuhuMaksSamaAtauDiAtasP90Baseline()
    {
        // Pola 10 level suhu maks yang berulang tiap hari (25,0 ... 29,5).
        static Hari Pola(DateOnly d) => new(25, 25 + d.DayNumber % 10 * 0.5, 21, 5);
        DataHarian data = Bangun(Pola);

        ProfilIklim profil = AnalisisIklim.Bangun(Uji, data, TahunAkhir);

        double[] baseline = Enumerable.Range(0, data.Jumlah)
            .Where(i => data.Tanggal[i].Year is >= 1991 and <= 2020)
            .Select(i => data.SuhuMaks[i]!.Value).ToArray();
        double p90 = AnalisisIklim.Persentil(baseline, 0.90);
        int diharapkan = Enumerable.Range(0, data.Jumlah)
            .Count(i => data.Tanggal[i].Year == 2025 && data.SuhuMaks[i] >= p90);

        Assert.Equal(p90, profil.AmbangHariPanas);
        Assert.Equal(diharapkan, profil.Terakhir(Indikator.HariPanas));
        Assert.InRange(diharapkan, 20, 60);   // sekitar 10% dari 365 hari
    }

    [Fact]
    public void Bangun_P90DihitungHanyaDariBaseline1991Sampai2020()
    {
        // Suhu maks 30 pada baseline, 40 setelah 2020: ambang harus tetap 30, bukan terseret data baru.
        ProfilIklim profil = Profil(d => new Hari(25, d.Year <= 2020 ? 30 : 40, 21, 5));

        Assert.Equal(30, profil.AmbangHariPanas);
        Assert.Equal(365, profil.Terakhir(Indikator.HariPanas));   // semua hari 2025 (40) >= 30
    }

    // ---------- Baseline dan anomali (PRD §6.1) ----------

    [Fact]
    public void Baseline_RataRata1991Sampai2020_TidakTerpengaruhDataSesudahnya()
    {
        ProfilIklim profil = Profil(d => Normal(d.Year <= 2020 ? 20 : 30));

        Assert.Equal(20, profil.Baseline(Indikator.SuhuRataRata)!.Value, 10);
    }

    [Fact]
    public void Anomali_AdalahNilaiTahunDikurangiBaseline()
    {
        ProfilIklim profil = Profil(_ => Normal(25));

        Assert.Equal(0.5, profil.Anomali(Indikator.SuhuRataRata, 25.5)!.Value, 10);
        Assert.Equal(-1.0, profil.Anomali(Indikator.SuhuRataRata, 24.0)!.Value, 10);
    }

    // ---------- Pembatasan tahun untuk perbandingan (AC-03.4) ----------

    [Fact]
    public void Batasi_HanyaMenyisakanTahunYangDiminta()
    {
        ProfilIklim profil = Profil(d => Normal(25 + (d.Year - 1996) * 0.01));

        ProfilIklim terbatas = profil.Batasi(new[] { 2010, 2011, 2012 });

        Assert.Equal(new[] { 2010, 2011, 2012 }, terbatas.Tahun);
        Assert.Equal(3, terbatas.Nilai(Indikator.SuhuRataRata).Length);
        Assert.Equal(profil.Baseline(Indikator.SuhuRataRata), terbatas.Baseline(Indikator.SuhuRataRata));
    }

    [Fact]
    public void Batasi_TanpaTahunYangCocok_MelemparArgumentException()
    {
        ProfilIklim profil = Profil(_ => Normal());

        Assert.Throws<ArgumentException>(() => profil.Batasi(new[] { 1800 }));
    }
}
