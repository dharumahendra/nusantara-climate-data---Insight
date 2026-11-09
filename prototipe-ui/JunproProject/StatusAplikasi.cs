namespace Junpro_Project;

// Data yang dipakai bersama oleh semua form (wilayah aktif, API key,
// koneksi database, riwayat chat).
public static class StatusAplikasi
{
    private static IPenyimpananChat penyimpananChat = new PenyimpananChatMemori();

    public static Wilayah? WilayahAktif { get; private set; }
    public static ProfilIklim? Profil => WilayahAktif == null ? null : DataIklim.Profil(WilayahAktif);

    public static string ApiKey { get; private set; } = "";
    public static bool AiAktif => ApiKey.Length > 0;

    public static DatabaseNadi? Database { get; private set; }
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
        Versi++;
    }

    public static void GunakanDatabase(DatabaseNadi database, string keterangan)
    {
        Database = database;
        KeteranganDatabase = keterangan;
        penyimpananChat = new PenyimpananChatPostgres(database);
    }

    public static void CatatDatabaseTidakAktif(string alasan) => KeteranganDatabase = alasan;

    public static IReadOnlyList<PesanChat> RiwayatChat(Wilayah wilayah) => penyimpananChat.Ambil(wilayah);

    public static void TambahPesanChat(Wilayah wilayah, PesanChat pesan) => penyimpananChat.Tambah(wilayah, pesan);

    public static void HapusRiwayatChat(Wilayah wilayah) => penyimpananChat.Hapus(wilayah);

    public static void HapusSemuaRiwayatChat()
    {
        penyimpananChat.HapusSemua();
        Versi++;
    }
}
