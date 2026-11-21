using Xunit;

namespace Junpro_Project.Tests;

// Rule engine rekomendasi (PRD §10 F-06): deterministik, 3-5 kartu, dan beralasan berbasis angka.
public class MesinRekomendasiTests
{
    private static readonly Wilayah Uji = new("DI Yogyakarta", "Sleman", -7.7167, 110.3550);
    private static readonly int[] Tahun = Enumerable.Range(1996, 30).ToArray();

    // Deret linear dari 'awal' naik sebesar 'perubahanTotal' selama 30 tahun.
    private static double[] Garis(double awal, double perubahanTotal) =>
        Tahun.Select(t => awal + perubahanTotal * (t - 1996) / 29.0).ToArray();

    private static ProfilIklim Profil(double suhu = 0, double hariPanas = 0, double hujanAwal = 2000,
                                      double hujanPerubahan = 0, double lebat = 0)
    {
        Dictionary<Indikator, double[]> deret = new()
        {
            [Indikator.SuhuRataRata] = Garis(25, suhu),
            [Indikator.SuhuMaksimum] = Garis(32, 0),
            [Indikator.SuhuMinimum] = Garis(20, 0),
            [Indikator.CurahHujan] = Garis(hujanAwal, hujanPerubahan),
            [Indikator.HariPanas] = Garis(35, hariPanas),
            [Indikator.HariHujan] = Garis(150, 0),
            [Indikator.HariHujanLebat] = Garis(3, lebat),
        };
        return new ProfilIklim(Uji, Tahun, deret, new Dictionary<Indikator, double>(), 32.0,
                               Array.Empty<int>(), 30);
    }

    [Fact]
    public void Buat_ProfilDatar_HanyaAksiDasarDanMinimalTigaKartu()
    {
        List<KartuAksi> kartu = MesinRekomendasi.Buat(Profil());

        Assert.Equal(3, kartu.Count);
        Assert.All(kartu, k => Assert.Equal("BASELINE", k.Kode));
    }

    [Fact]
    public void Buat_SuhuNaik08_MengaktifkanTempRisingDenganKategoriEnergi()
    {
        List<KartuAksi> kartu = MesinRekomendasi.Buat(Profil(suhu: 0.8));

        KartuAksi suhu = Assert.Single(kartu, k => k.Kode == "TEMP_RISING");
        Assert.Equal("Energi", suhu.Kategori);
        Assert.DoesNotContain(kartu, k => k.Kode == "TEMP_RISING_STRONG");   // 0,8 < 1,0
        Assert.True(kartu.Count >= 3);
    }

    [Fact]
    public void Buat_SuhuNaik12_MengaktifkanTempRisingStrongDanTempRising()
    {
        List<KartuAksi> kartu = MesinRekomendasi.Buat(Profil(suhu: 1.2));

        Assert.Contains(kartu, k => k.Kode == "TEMP_RISING_STRONG" && k.Kategori == "Vegetasi");
        Assert.Contains(kartu, k => k.Kode == "TEMP_RISING");
    }

    [Fact]
    public void Buat_SuhuNaikDiBawahAmbang05_TidakMengaktifkanTempRising()
    {
        List<KartuAksi> kartu = MesinRekomendasi.Buat(Profil(suhu: 0.4));

        Assert.DoesNotContain(kartu, k => k.Kode.StartsWith("TEMP_RISING"));
    }

    [Theory]
    [InlineData(2600, "RAINFALL_HIGH")]
    [InlineData(2500, "RAINFALL_HIGH")]
    [InlineData(1400, "RAINFALL_LOW")]
    [InlineData(1500, "RAINFALL_LOW")]
    public void Buat_AmbangCurahHujanTinggiDanRendah(double hujanTahunan, string kodeDiharapkan)
    {
        Assert.Contains(MesinRekomendasi.Buat(Profil(hujanAwal: hujanTahunan)), k => k.Kode == kodeDiharapkan);
    }

    [Fact]
    public void Buat_CurahHujanDiTengahRentang_TidakAdaAturanHujan()
    {
        List<KartuAksi> kartu = MesinRekomendasi.Buat(Profil(hujanAwal: 2000));

        Assert.DoesNotContain(kartu, k => k.Kode is "RAINFALL_HIGH" or "RAINFALL_LOW");
    }

    [Fact]
    public void Buat_CurahHujanMenurunLebihDari10Persen_MengaktifkanRainfallDeclining()
    {
        // 2000 mm turun 300 mm (rata-rata 1850) = 16% -> aktif.
        Assert.Contains(MesinRekomendasi.Buat(Profil(hujanAwal: 2000, hujanPerubahan: -300)),
            k => k.Kode == "RAINFALL_DECLINING");
        // Turun 100 mm dari ±1950 = 5% -> tidak aktif.
        Assert.DoesNotContain(MesinRekomendasi.Buat(Profil(hujanAwal: 2000, hujanPerubahan: -100)),
            k => k.Kode == "RAINFALL_DECLINING");
    }

    [Fact]
    public void Buat_HariPanasBertambahDanHujanLebatNaik_MengaktifkanAturannya()
    {
        List<KartuAksi> kartu = MesinRekomendasi.Buat(Profil(hariPanas: 9, lebat: 4));

        Assert.Contains(kartu, k => k.Kode == "HOTDAYS_RISING");
        Assert.Contains(kartu, k => k.Kode == "HEAVYRAIN_RISING");
    }

    [Fact]
    public void Buat_SemuaAturanTerpicu_DibatasiMaksimalLimaKartu()
    {
        // Suhu +1,5, hari panas +10, hujan 3500 turun 700 mm, hujan lebat naik.
        List<KartuAksi> kartu = MesinRekomendasi.Buat(
            Profil(suhu: 1.5, hariPanas: 10, hujanAwal: 3500, hujanPerubahan: -700, lebat: 5));

        Assert.Equal(5, kartu.Count);
        Assert.DoesNotContain(kartu, k => k.Kode == "BASELINE");
    }

    [Fact]
    public void Buat_UrutanDariDampakTertinggi()
    {
        List<KartuAksi> kartu = MesinRekomendasi.Buat(
            Profil(suhu: 1.5, hariPanas: 10, hujanAwal: 3500, hujanPerubahan: -700, lebat: 5));

        Assert.Equal(kartu.OrderByDescending(k => k.Dampak).Select(k => k.Dampak), kartu.Select(k => k.Dampak));
    }

    [Fact]
    public void Buat_InputSama_HasilIdentikSetiapKaliDijalankan()
    {
        ProfilIklim profil = Profil(suhu: 0.8, hariPanas: 7, hujanAwal: 2600);

        List<KartuAksi> pertama = MesinRekomendasi.Buat(profil);
        List<KartuAksi> kedua = MesinRekomendasi.Buat(profil);

        Assert.Equal(pertama, kedua);   // record: kesamaan nilai, termasuk urutan
    }

    [Fact]
    public void Buat_SetiapKartuPunyaAlasanDenganAngkaKecualiAksiDasar()
    {
        List<KartuAksi> kartu = MesinRekomendasi.Buat(Profil(suhu: 0.8, hariPanas: 7, hujanAwal: 2600));

        Assert.All(kartu, k => Assert.False(string.IsNullOrWhiteSpace(k.Alasan)));
        Assert.All(kartu.Where(k => k.Kode != "BASELINE"), k => Assert.Contains(k.Alasan, ch => char.IsDigit(ch)));
        Assert.All(kartu.Where(k => k.Kode != "BASELINE"), k => Assert.Contains("Sleman", k.Alasan));
    }

    [Theory]
    [InlineData(0, 0, 2000)]
    [InlineData(0.8, 0, 2000)]
    [InlineData(1.5, 10, 3500)]
    [InlineData(0, 0, 1200)]
    public void Buat_SelaluMenghasilkanTigaSampaiLimaKartu(double suhu, double panas, double hujan)
    {
        int jumlah = MesinRekomendasi.Buat(Profil(suhu: suhu, hariPanas: panas, hujanAwal: hujan)).Count;

        Assert.InRange(jumlah, 3, 5);
    }
}
