namespace Junpro_Project;

// Aturan konkret rekomendasi aksi iklim (PRD §10 F-06). Menambah aturan baru
// cukup dengan membuat subclass baru dan mendaftarkannya di MesinRekomendasi.

public sealed class AturanSuhuNaikTajam : AturanKenaikanTren
{
    public AturanSuhuNaikTajam() : base(Indikator.SuhuRataRata, 1.0,
        "TEMP_RISING_STRONG", "Vegetasi", "Dukung ruang terbuka hijau",
        "Tanam pohon peneduh di sekitar rumah dan ikut mendorong taman atau jalur hijau di lingkungan sekitar.",
        TingkatDampak.Tinggi)
    {
    }

    protected override string KalimatAlasan(string kota, string kenaikan, int tahunAwal) =>
        $"Karena kenaikan suhu rata-rata {kota} sudah mencapai {kenaikan} sejak {tahunAwal}.";
}

public sealed class AturanSuhuNaik : AturanKenaikanTren
{
    public AturanSuhuNaik() : base(Indikator.SuhuRataRata, 0.5,
        "TEMP_RISING", "Energi", "Kurangi beban pendingin ruangan",
        "Atur AC di 25–26 °C, manfaatkan ventilasi silang, dan pasang peneduh atau cat reflektif di sisi rumah yang terkena matahari.",
        TingkatDampak.Sedang)
    {
    }

    protected override string KalimatAlasan(string kota, string kenaikan, int tahunAwal) =>
        $"Karena suhu rata-rata {kota} naik {kenaikan} sejak {tahunAwal}.";
}

public sealed class AturanHariPanasBertambah : AturanKenaikanTren
{
    public AturanHariPanasBertambah() : base(Indikator.HariPanas, 5,
        "HOTDAYS_RISING", "Kesiapsiagaan", "Waspadai hari-hari terik",
        "Kurangi aktivitas berat di luar ruangan pukul 11–15, cukupi minum, dan kenali gejala kelelahan akibat panas.",
        TingkatDampak.Tinggi)
    {
    }

    protected override string KalimatAlasan(string kota, string kenaikan, int tahunAwal) =>
        $"Karena hari panas di {kota} bertambah {kenaikan} sejak {tahunAwal}.";
}

public sealed class AturanHujanLebatBertambah : AturanKenaikanTren
{
    // Ambang 0: setiap tren "Naik" sudah melewati ambang stabil (PRD §6.4).
    public AturanHujanLebatBertambah() : base(Indikator.HariHujanLebat, 0,
        "HEAVYRAIN_RISING", "Kesiapsiagaan", "Siaga genangan saat hujan lebat",
        "Jangan buang sampah ke saluran air, simpan dokumen penting di tempat tinggi, dan pantau informasi resmi BMKG.",
        TingkatDampak.Tinggi)
    {
    }

    protected override string KalimatAlasan(string kota, string kenaikan, int tahunAwal) =>
        $"Karena hari hujan lebat di {kota} cenderung bertambah {kenaikan} sejak {tahunAwal}.";
}

public sealed class AturanCurahHujanMenurun : AturanAksi
{
    private const double AmbangPenurunanRelatif = 0.10;

    public AturanCurahHujanMenurun() : base(
        "RAINFALL_DECLINING", "Air", "Konservasi air jangka panjang",
        "Gunakan ulang air bekas cucian sayur untuk menyiram tanaman dan dukung program sumur resapan di lingkungan.",
        TingkatDampak.Tinggi)
    {
    }

    public override bool BerlakuUntuk(ProfilIklim profil)
    {
        HasilTren tren = profil.Tren(Indikator.CurahHujan);
        double penurunanRelatif = -tren.PerubahanTotal / profil.RataRata(Indikator.CurahHujan);
        return tren.Arah == "Turun" && penurunanRelatif >= AmbangPenurunanRelatif;
    }

    protected override string SusunAlasan(ProfilIklim profil)
    {
        double penurunan = -profil.Tren(Indikator.CurahHujan).PerubahanTotal;
        return $"Karena curah hujan {profil.Wilayah.Kota} cenderung turun " +
               $"{Format.Nilai(Indikator.CurahHujan, penurunan)} sejak {profil.TahunAwal}.";
    }
}

public sealed class AturanCurahHujanTinggi : AturanAksi
{
    private const double AmbangMinimum = 2500;   // mm per tahun

    public AturanCurahHujanTinggi() : base(
        "RAINFALL_HIGH", "Air", "Panen air hujan dan rawat drainase",
        "Tampung air hujan untuk menyiram tanaman, buat lubang biopori, dan pastikan saluran air rumah tidak tersumbat.",
        TingkatDampak.Sedang)
    {
    }

    public override bool BerlakuUntuk(ProfilIklim profil) =>
        profil.RataRata(Indikator.CurahHujan) >= AmbangMinimum;

    protected override string SusunAlasan(ProfilIklim profil) =>
        $"Karena curah hujan tahunan {profil.Wilayah.Kota} rata-rata " +
        $"{Format.Nilai(Indikator.CurahHujan, profil.RataRata(Indikator.CurahHujan))}.";
}

public sealed class AturanCurahHujanRendah : AturanAksi
{
    private const double AmbangMaksimum = 1500;   // mm per tahun

    public AturanCurahHujanRendah() : base(
        "RAINFALL_LOW", "Air", "Hemat dan tampung air",
        "Pasang mulsa pada tanaman, perbaiki keran yang bocor, dan tampung air selama musim hujan.",
        TingkatDampak.Tinggi)
    {
    }

    public override bool BerlakuUntuk(ProfilIklim profil) =>
        profil.RataRata(Indikator.CurahHujan) <= AmbangMaksimum;

    protected override string SusunAlasan(ProfilIklim profil) =>
        $"Karena curah hujan tahunan {profil.Wilayah.Kota} hanya sekitar " +
        $"{Format.Nilai(Indikator.CurahHujan, profil.RataRata(Indikator.CurahHujan))}.";
}

// Aksi dasar (BASELINE) yang relevan di wilayah mana pun; dipakai untuk
// melengkapi daftar bila aturan spesifik yang terpicu terlalu sedikit.
public sealed class AturanAksiDasar : AturanAksi
{
    public AturanAksiDasar(string kategori, string judul, string deskripsi)
        : base("BASELINE", kategori, judul, deskripsi, TingkatDampak.Rendah)
    {
    }

    public override bool BerlakuUntuk(ProfilIklim profil) => true;

    protected override string SusunAlasan(ProfilIklim profil) =>
        $"Aksi dasar yang bermanfaat di wilayah mana pun, termasuk {profil.Wilayah.Kota}.";
}
