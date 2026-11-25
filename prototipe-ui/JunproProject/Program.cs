using System.Text.Json;
using Npgsql;

namespace Junpro_Project;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        PasangPenangkapGalat();

        using DatabaseNadi? database = SambungkanDatabase();
        TerapkanApiKeyDariKonfigurasi();
        Application.Run(Navigasi.Ambil<DashboardForm>());
    }

    // PRD §15: kegagalan tak terduga tidak boleh menutup aplikasi atau menampilkan
    // stack trace; pengguna hanya diberi pesan singkat dan detailnya masuk ke log.
    private static void PasangPenangkapGalat()
    {
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => TampilkanGalat(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception galat) CatatanGalat.Catat(galat);
        };
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            CatatanGalat.Catat(e.Exception);
            e.SetObserved();
        };
    }

    private static void TampilkanGalat(Exception galat)
    {
        string? log = CatatanGalat.Catat(galat);
        string rujukan = log == null ? "" : "\n\nCatatan teknis disimpan di:\n" + log;
        MessageBox.Show(
            "Terjadi kesalahan tak terduga, tetapi NADI tetap berjalan. Coba ulangi tindakan terakhir Anda." +
            "\n\n" + galat.Message + rujukan,
            "NADI", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    // API key Gemini dibaca dari appsettings.local.json supaya tidak perlu diketik
    // ulang setiap aplikasi dibuka (PRD §11.5). Kosong berarti fitur AI memakai mode demo.
    private static void TerapkanApiKeyDariKonfigurasi()
    {
        try
        {
            string? apiKey = KonfigurasiLokal.ApiKeyGemini();
            if (!string.IsNullOrWhiteSpace(apiKey)) StatusAplikasi.SimpanApiKey(apiKey);
        }
        catch (JsonException)
        {
            // File konfigurasi rusak: pesan jelasnya sudah ditampilkan lewat status database.
        }
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
