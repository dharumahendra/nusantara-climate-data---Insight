namespace Junpro_Project;

public sealed record KartuAksi(string Kode, string Kategori, string Judul,
                               string Deskripsi, string Dampak, string Alasan);

// Rule engine deterministik (PRD §10 F-06): rekomendasi dipilih dari aturan
// tetap berdasarkan angka profil, bukan dikarang AI.
public static class MesinRekomendasi
{
    private const int MaksKartu = 5;
    private const int MinKartu = 3;

    public static List<KartuAksi> Buat(ProfilIklim p)
    {
        string kota = p.Wilayah.Kota;
        HasilTren suhu = p.Tren(Indikator.SuhuRataRata);
        HasilTren panas = p.Tren(Indikator.HariPanas);
        HasilTren hujan = p.Tren(Indikator.CurahHujan);
        HasilTren lebat = p.Tren(Indikator.HariHujanLebat);
        double hujanRata = p.RataRata(Indikator.CurahHujan);

        // Urutan penambahan = urutan spesifisitas; OrderBy di bawah bersifat stabil.
        List<KartuAksi> spesifik = new List<KartuAksi>();

        if (suhu.Arah == "Naik" && suhu.PerubahanTotal >= 1.0)
            spesifik.Add(new KartuAksi("TEMP_RISING_STRONG", "Vegetasi",
                "Dukung ruang terbuka hijau",
                "Tanam pohon peneduh di sekitar rumah dan ikut mendorong taman atau jalur hijau di lingkungan sekitar.",
                "Tinggi",
                $"Karena kenaikan suhu rata-rata {kota} sudah mencapai {Format.Nilai(Indikator.SuhuRataRata, suhu.PerubahanTotal)} sejak {p.TahunAwal}."));

        if (suhu.Arah == "Naik" && suhu.PerubahanTotal >= 0.5)
            spesifik.Add(new KartuAksi("TEMP_RISING", "Energi",
                "Kurangi beban pendingin ruangan",
                "Atur AC di 25–26 °C, manfaatkan ventilasi silang, dan pasang peneduh atau cat reflektif di sisi rumah yang terkena matahari.",
                "Sedang",
                $"Karena suhu rata-rata {kota} naik {Format.Nilai(Indikator.SuhuRataRata, suhu.PerubahanTotal)} sejak {p.TahunAwal}."));

        if (panas.Arah == "Naik" && panas.PerubahanTotal >= 5)
            spesifik.Add(new KartuAksi("HOTDAYS_RISING", "Kesiapsiagaan",
                "Waspadai hari-hari terik",
                "Kurangi aktivitas berat di luar ruangan pukul 11–15, cukupi minum, dan kenali gejala kelelahan akibat panas.",
                "Tinggi",
                $"Karena hari panas di {kota} bertambah {Format.Nilai(Indikator.HariPanas, panas.PerubahanTotal)} sejak {p.TahunAwal}."));

        if (hujan.Arah == "Turun" && -hujan.PerubahanTotal / hujanRata >= 0.10)
            spesifik.Add(new KartuAksi("RAINFALL_DECLINING", "Air",
                "Konservasi air jangka panjang",
                "Gunakan ulang air bekas cucian sayur untuk menyiram tanaman dan dukung program sumur resapan di lingkungan.",
                "Tinggi",
                $"Karena curah hujan {kota} cenderung turun {Format.Nilai(Indikator.CurahHujan, -hujan.PerubahanTotal)} sejak {p.TahunAwal}."));

        if (hujanRata >= 2500)
            spesifik.Add(new KartuAksi("RAINFALL_HIGH", "Air",
                "Panen air hujan dan rawat drainase",
                "Tampung air hujan untuk menyiram tanaman, buat lubang biopori, dan pastikan saluran air rumah tidak tersumbat.",
                "Sedang",
                $"Karena curah hujan tahunan {kota} rata-rata {Format.Nilai(Indikator.CurahHujan, hujanRata)}."));

        if (hujanRata <= 1500)
            spesifik.Add(new KartuAksi("RAINFALL_LOW", "Air",
                "Hemat dan tampung air",
                "Pasang mulsa pada tanaman, perbaiki keran yang bocor, dan tampung air selama musim hujan.",
                "Tinggi",
                $"Karena curah hujan tahunan {kota} hanya sekitar {Format.Nilai(Indikator.CurahHujan, hujanRata)}."));

        if (lebat.Arah == "Naik")
            spesifik.Add(new KartuAksi("HEAVYRAIN_RISING", "Kesiapsiagaan",
                "Siaga genangan saat hujan lebat",
                "Jangan buang sampah ke saluran air, simpan dokumen penting di tempat tinggi, dan pantau informasi resmi BMKG.",
                "Tinggi",
                $"Karena hari hujan lebat di {kota} cenderung bertambah {Format.Nilai(Indikator.HariHujanLebat, lebat.PerubahanTotal)} sejak {p.TahunAwal}."));

        List<KartuAksi> hasil = spesifik
            .OrderBy(k => PeringkatDampak(k.Dampak))
            .Take(MaksKartu)
            .ToList();

        // Kartu BASELINE hanya dipakai bila kartu spesifik kurang dari 3.
        foreach (KartuAksi umum in AksiUmum(kota))
        {
            if (hasil.Count >= MinKartu) break;
            hasil.Add(umum);
        }

        return hasil;
    }

    private static int PeringkatDampak(string dampak) => dampak switch
    {
        "Tinggi" => 0,
        "Sedang" => 1,
        _ => 2,
    };

    private static IEnumerable<KartuAksi> AksiUmum(string kota)
    {
        string alasan = $"Aksi dasar yang bermanfaat di wilayah mana pun, termasuk {kota}.";

        yield return new KartuAksi("BASELINE", "Energi", "Hemat listrik di rumah",
            "Matikan perangkat yang tidak dipakai, cabut pengisi daya, dan pilih lampu LED.",
            "Rendah", alasan);
        yield return new KartuAksi("BASELINE", "Transportasi", "Pilih transportasi rendah emisi",
            "Gunakan transportasi publik, bersepeda, atau berjalan kaki untuk perjalanan dekat.",
            "Rendah", alasan);
        yield return new KartuAksi("BASELINE", "Vegetasi", "Olah sisa makanan jadi kompos",
            "Kurangi sampah makanan dan jadikan sisa organik kompos untuk tanaman di rumah.",
            "Rendah", alasan);
    }
}
