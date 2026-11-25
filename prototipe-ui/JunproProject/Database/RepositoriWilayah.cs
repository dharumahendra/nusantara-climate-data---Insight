using Npgsql;

namespace Junpro_Project;

// Membaca daftar wilayah dari tabel `wilayah`.
public sealed class RepositoriWilayah
{
    private readonly DatabaseNadi database;

    public RepositoriWilayah(DatabaseNadi database)
    {
        this.database = database;
    }

    public Wilayah[] AmbilSemua()
    {
        using NpgsqlCommand perintah = database.BuatPerintah(
            "SELECT provinsi, kota, latitude, longitude FROM wilayah " +
            "WHERE latitude IS NOT NULL AND longitude IS NOT NULL ORDER BY id");
        using NpgsqlDataReader baris = perintah.ExecuteReader();

        List<Wilayah> daftar = new List<Wilayah>();
        while (baris.Read())
            daftar.Add(new Wilayah(
                Provinsi: baris.GetString(0),
                Kota: baris.GetString(1),
                Latitude: baris.GetDouble(2),
                Longitude: baris.GetDouble(3)));

        return daftar.ToArray();
    }
}
