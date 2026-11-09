using System.Text.Json;
using Npgsql;

namespace Junpro_Project;

public sealed class DatabaseNadi : IDisposable
{
    private const string FileKonfigurasi = "appsettings.local.json";

    private readonly NpgsqlDataSource sumberData;

    private DatabaseNadi(NpgsqlDataSource sumberData)
    {
        this.sumberData = sumberData;
    }

    // Mengembalikan null bila connection string belum diatur.
    public static DatabaseNadi? DariKonfigurasi()
    {
        string? connectionString = BacaConnectionString();
        return string.IsNullOrWhiteSpace(connectionString)
            ? null
            : new DatabaseNadi(NpgsqlDataSource.Create(connectionString));
    }

    public NpgsqlCommand BuatPerintah(string sql) => sumberData.CreateCommand(sql);

    public string Periksa()
    {
        using NpgsqlConnection koneksi = sumberData.OpenConnection();
        using NpgsqlCommand perintah = new NpgsqlCommand(
            "SELECT (SELECT count(*) FROM wilayah), (SELECT count(*) FROM pesan_chat)", koneksi);
        using NpgsqlDataReader baris = perintah.ExecuteReader();
        baris.Read();

        return $"Tersambung ke PostgreSQL {koneksi.PostgreSqlVersion}, database \"{koneksi.Database}\": " +
               $"{baris.GetInt64(0)} wilayah dan {baris.GetInt64(1)} pesan chat tersimpan.";
    }

    public void Dispose() => sumberData.Dispose();

    private static string? BacaConnectionString()
    {
        string path = Path.Combine(AppContext.BaseDirectory, FileKonfigurasi);
        if (!File.Exists(path)) return null;

        using JsonDocument json = JsonDocument.Parse(File.ReadAllText(path));
        return json.RootElement.TryGetProperty("ConnectionStrings", out JsonElement daftar) &&
               daftar.TryGetProperty("Nadi", out JsonElement nadi)
            ? nadi.GetString()
            : null;
    }
}
