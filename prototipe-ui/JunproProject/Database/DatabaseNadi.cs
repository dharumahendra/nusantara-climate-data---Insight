using Npgsql;

namespace Junpro_Project;

public sealed class DatabaseNadi : IDisposable
{
    private readonly NpgsqlDataSource sumberData;

    private DatabaseNadi(NpgsqlDataSource sumberData)
    {
        this.sumberData = sumberData;
    }

    // Mengembalikan null bila connection string belum diatur.
    public static DatabaseNadi? DariKonfigurasi()
    {
        string? connectionString = KonfigurasiLokal.ConnectionStringNadi();
        return string.IsNullOrWhiteSpace(connectionString)
            ? null
            : new DatabaseNadi(NpgsqlDataSource.Create(connectionString));
    }

    public NpgsqlCommand BuatPerintah(string sql) => sumberData.CreateCommand(sql);

    // Untuk operasi yang butuh transaksi atau COPY (mis. menyimpan data iklim harian).
    public NpgsqlConnection BukaKoneksi() => sumberData.OpenConnection();

    public string Periksa()
    {
        using NpgsqlConnection koneksi = sumberData.OpenConnection();
        using NpgsqlCommand perintah = new NpgsqlCommand(
            "SELECT (SELECT count(*) FROM wilayah), (SELECT count(*) FROM pesan_chat), " +
            "(SELECT count(DISTINCT wilayah_id) FROM data_iklim_harian)", koneksi);
        using NpgsqlDataReader baris = perintah.ExecuteReader();
        baris.Read();

        return $"Tersambung ke PostgreSQL {koneksi.PostgreSqlVersion}, database \"{koneksi.Database}\": " +
               $"{baris.GetInt64(0)} wilayah, {baris.GetInt64(1)} pesan chat, dan data iklim " +
               $"{baris.GetInt64(2)} wilayah tersimpan.";
    }

    public void Dispose() => sumberData.Dispose();
}
