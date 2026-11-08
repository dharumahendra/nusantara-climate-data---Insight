namespace Junpro_Project;

public sealed record PesanChat(bool DariPengguna, string Isi);

// Data yang dipakai bersama oleh semua form (wilayah aktif, API key, riwayat chat).
public static class StatusAplikasi
{
    private static readonly Dictionary<Wilayah, List<PesanChat>> riwayatChat = new();

    public static Wilayah? WilayahAktif { get; private set; }
    public static ProfilIklim? Profil => WilayahAktif == null ? null : DataIklim.Profil(WilayahAktif);

    public static string ApiKey { get; private set; } = "";
    public static bool AiAktif => ApiKey.Length > 0;

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

    public static List<PesanChat> RiwayatChat(Wilayah wilayah)
    {
        if (!riwayatChat.TryGetValue(wilayah, out List<PesanChat>? riwayat))
        {
            riwayat = new List<PesanChat>();
            riwayatChat[wilayah] = riwayat;
        }
        return riwayat;
    }

    public static void HapusSemuaRiwayatChat()
    {
        riwayatChat.Clear();
        Versi++;
    }
}
