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
            "SELECT provinsi, kota, suhu_normal, curah_hujan_normal FROM wilayah ORDER BY id");
        using NpgsqlDataReader baris = perintah.ExecuteReader();

        List<Wilayah> daftar = new List<Wilayah>();
        while (baris.Read())
            daftar.Add(new Wilayah(
                Provinsi: baris.GetString(0),
                Kota: baris.GetString(1),
                SuhuNormal: (double)baris.GetDecimal(2),
                HujanNormal: (double)baris.GetDecimal(3)));

        return daftar.ToArray();
    }
}
