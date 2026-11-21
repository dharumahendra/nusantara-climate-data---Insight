using System.Text.Json;

namespace Junpro_Project;

// Membaca appsettings.local.json di folder aplikasi (PRD §11.5). File ini tidak
// ikut di-commit karena memuat rahasia (password database, API key).
public static class KonfigurasiLokal
{
    public const string NamaFile = "appsettings.local.json";

    public static string? ConnectionStringNadi() => Baca("ConnectionStrings", "Nadi");

    public static string? ApiKeyGemini() => Baca("Gemini", "ApiKey");

    public static string? Baca(string bagian, string kunci) =>
        BacaDari(Path.Combine(AppContext.BaseDirectory, NamaFile), bagian, kunci);

    // Mengembalikan null bila file atau kuncinya tidak ada. File yang bukan JSON
    // valid melempar JsonException agar pemanggil bisa memberi pesan yang jelas.
    public static string? BacaDari(string path, string bagian, string kunci)
    {
        if (!File.Exists(path)) return null;

        using JsonDocument json = JsonDocument.Parse(File.ReadAllText(path));
        return json.RootElement.TryGetProperty(bagian, out JsonElement isiBagian) &&
               isiBagian.ValueKind == JsonValueKind.Object &&
               isiBagian.TryGetProperty(kunci, out JsonElement nilai) &&
               nilai.ValueKind == JsonValueKind.String
            ? nilai.GetString()
            : null;
    }
}
