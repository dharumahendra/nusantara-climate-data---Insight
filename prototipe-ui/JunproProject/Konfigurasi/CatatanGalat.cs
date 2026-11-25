namespace Junpro_Project;

// Mencatat exception ke file log (PRD §15: pengguna tidak boleh melihat stack
// trace, tetapi semua exception harus tercatat di %LOCALAPPDATA%/NADI/logs/).
public static class CatatanGalat
{
    public static string Folder { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NADI", "logs");

    // Mengembalikan path file log, atau null bila log sendiri gagal ditulis
    // (mencatat error tidak boleh menjadi penyebab error baru).
    public static string? Catat(Exception galat)
    {
        try
        {
            Directory.CreateDirectory(Folder);
            string path = Path.Combine(Folder, $"nadi-{DateTime.Now:yyyyMMdd}.log");
            File.AppendAllText(path, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {galat}{Environment.NewLine}{Environment.NewLine}");
            return path;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }
}
