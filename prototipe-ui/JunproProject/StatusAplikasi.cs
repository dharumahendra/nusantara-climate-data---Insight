namespace Junpro_Project;

// Data yang dipakai bersama oleh semua form (wilayah aktif, API key,
// koneksi database, riwayat chat).
public static class StatusAplikasi
{
    private static IPenyimpananChat penyimpananChat = new PenyimpananChatMemori();

    public static Wilayah? WilayahAktif { get; private set; }
    public static ProfilIklim? Profil => WilayahAktif == null ? null : DataIklim.CobaProfil(WilayahAktif);

    public static string ApiKey { get; private set; } = "";
    public static bool AiAktif => ApiKey.Length > 0;

    // Asisten AI aktif. Bertipe kelas dasar AsistenIklim: form tidak perlu tahu
    // apakah isinya AsistenDemo atau AsistenGemini (polymorphism).
    public static AsistenIklim Asisten { get; private set; } = new AsistenDemo();

    public static DatabaseNadi? Database { get; private set; }

    // Cache data iklim harian di PostgreSQL; null bila database tidak aktif.
    public static PenyimpananIklim? PenyimpananIklim { get; private set; }
    public static string KeteranganDatabase { get; private set; } = "Database belum disambungkan.";

    // Naik setiap ada perubahan yang memengaruhi isi halaman. Form yang sedang
    // tersembunyi membangun ulang tampilannya saat dibuka jika versinya tertinggal.
    public static int Versi { get; private set; }

    public static void PilihWilayah(Wilayah wilayah)
    {
        WilayahAktif = wilayah;
        Versi++;
    }

    public static void SimpanApiKey(string apiKey)
    {
        ApiKey = apiKey.Trim();
        Asisten = AiAktif ? new AsistenGemini(ApiKey) : new AsistenDemo();
        Versi++;
    }

    public static void GunakanDatabase(DatabaseNadi database, string keterangan)
    {
        Database = database;
        KeteranganDatabase = keterangan;
        penyimpananChat = new PenyimpananChatPostgres(database);
        PenyimpananIklim = new PenyimpananIklim(database);
    }

    public static void CatatDatabaseTidakAktif(string alasan) => KeteranganDatabase = alasan;

    // Dipanggil dari thread mana pun; hanya mengganti teks status yang dibaca halaman Pengaturan.
    public static void CatatMasalahDatabase(string masalah) => KeteranganDatabase = masalah;

    public static IReadOnlyList<PesanChat> RiwayatChat(Wilayah wilayah) => penyimpananChat.Ambil(wilayah);

    public static void TambahPesanChat(Wilayah wilayah, PesanChat pesan) => penyimpananChat.Tambah(wilayah, pesan);

    public static void HapusRiwayatChat(Wilayah wilayah) => penyimpananChat.Hapus(wilayah);

    public static void HapusSemuaRiwayatChat()
    {
        penyimpananChat.HapusSemua();
        Versi++;
    }
}
