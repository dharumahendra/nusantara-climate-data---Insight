using System.Text.Json;

namespace Junpro_Project;

// Menyimpan wilayah yang terakhir dipilih agar dipulihkan saat aplikasi dibuka
// lagi (PRD F-01). Hanya mengingat pilihan; data iklimnya tetap dimuat saat
// pengguna menekan Tampilkan Data.
public static class PilihanTerakhir
{
    public static string Berkas { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NADI", "pilihan-terakhir.json");

    private sealed record Isi(string Provinsi, string Kota);

    public static void Simpan(Wilayah wilayah)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Berkas)!);
            File.WriteAllText(Berkas, JsonSerializer.Serialize(new Isi(wilayah.Provinsi, wilayah.Kota)));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Fitur kenyamanan saja: gagal menyimpan tidak boleh mengganggu pengguna.
        }
    }

    // Null bila belum pernah memilih, berkasnya rusak, atau wilayahnya tidak ada lagi di daftar.
    public static Wilayah? Baca()
    {
        try
        {
            if (!File.Exists(Berkas)) return null;

            Isi? isi = JsonSerializer.Deserialize<Isi>(File.ReadAllText(Berkas));
            return isi == null ? null : DataIklim.Cari(isi.Provinsi, isi.Kota);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return null;
        }
    }
}
