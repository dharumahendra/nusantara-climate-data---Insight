using System.Text.Json;
using Npgsql;

namespace Junpro_Project;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        using DatabaseNadi? database = SambungkanDatabase();
        Application.Run(Navigasi.Ambil<DashboardForm>());
    }

    private static DatabaseNadi? SambungkanDatabase()
    {
        DatabaseNadi? database = null;
        try
        {
            database = DatabaseNadi.DariKonfigurasi();
            if (database == null)
            {
                StatusAplikasi.CatatDatabaseTidakAktif(
                    "Connection string belum diatur di appsettings.local.json. " +
                    "Aplikasi memakai daftar wilayah bawaan dan riwayat chat hanya disimpan di memori.");
                return null;
            }

            Wilayah[] wilayah = new RepositoriWilayah(database).AmbilSemua();
            if (wilayah.Length == 0)
                throw new InvalidOperationException("Tabel wilayah masih kosong. Jalankan Database/schema.sql.");

            DataIklim.GunakanDaftarWilayah(wilayah);
            StatusAplikasi.GunakanDatabase(database,
                $"Tersambung ke PostgreSQL. {wilayah.Length} wilayah dibaca dari database dan riwayat chat disimpan permanen.");
            return database;
        }
        catch (Exception ex) when (ex is NpgsqlException or ArgumentException
                                       or JsonException or InvalidOperationException)
        {
            database?.Dispose();
            StatusAplikasi.CatatDatabaseTidakAktif(
                $"Gagal tersambung ke PostgreSQL ({ex.Message}). " +
                "Aplikasi memakai daftar wilayah bawaan dan riwayat chat hanya disimpan di memori.");
            return null;
        }
    }
}
