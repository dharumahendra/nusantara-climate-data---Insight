using Npgsql;

namespace Junpro_Project;

public sealed record PesanChat(bool DariPengguna, string Isi);

public interface IPenyimpananChat
{
    IReadOnlyList<PesanChat> Ambil(Wilayah wilayah);
    void Tambah(Wilayah wilayah, PesanChat pesan);
    void Hapus(Wilayah wilayah);
    void HapusSemua();
}

public sealed class PenyimpananChatMemori : IPenyimpananChat
{
    private readonly Dictionary<Wilayah, List<PesanChat>> riwayat = new();

    public IReadOnlyList<PesanChat> Ambil(Wilayah wilayah) =>
        riwayat.TryGetValue(wilayah, out List<PesanChat>? daftar) ? daftar : Array.Empty<PesanChat>();

    public void Tambah(Wilayah wilayah, PesanChat pesan)
    {
        if (!riwayat.TryGetValue(wilayah, out List<PesanChat>? daftar))
        {
            daftar = new List<PesanChat>();
            riwayat[wilayah] = daftar;
        }
        daftar.Add(pesan);
    }

    public void Hapus(Wilayah wilayah) => riwayat.Remove(wilayah);

    public void HapusSemua() => riwayat.Clear();
}

public sealed class PenyimpananChatPostgres : IPenyimpananChat
{
    private const string PeranPengguna = "pengguna";
    private const string PeranAsisten = "asisten";

    private readonly DatabaseNadi database;

    public PenyimpananChatPostgres(DatabaseNadi database)
    {
        this.database = database;
    }

    public IReadOnlyList<PesanChat> Ambil(Wilayah wilayah)
    {
        using NpgsqlCommand perintah = database.BuatPerintah("""
            SELECT p.peran, p.isi
            FROM pesan_chat p
            JOIN wilayah w ON w.id = p.wilayah_id
            WHERE w.provinsi = @provinsi AND w.kota = @kota
            ORDER BY p.id
            """);
        TambahParameterWilayah(perintah, wilayah);
        using NpgsqlDataReader baris = perintah.ExecuteReader();

        List<PesanChat> riwayat = new List<PesanChat>();
        while (baris.Read())
            riwayat.Add(new PesanChat(baris.GetString(0) == PeranPengguna, baris.GetString(1)));
        return riwayat;
    }

    public void Tambah(Wilayah wilayah, PesanChat pesan)
    {
        using NpgsqlCommand perintah = database.BuatPerintah("""
            INSERT INTO pesan_chat (wilayah_id, peran, isi)
            SELECT id, @peran, @isi FROM wilayah
            WHERE provinsi = @provinsi AND kota = @kota
            """);
        TambahParameterWilayah(perintah, wilayah);
        perintah.Parameters.AddWithValue("peran", pesan.DariPengguna ? PeranPengguna : PeranAsisten);
        perintah.Parameters.AddWithValue("isi", pesan.Isi);

        if (perintah.ExecuteNonQuery() == 0)
            throw new InvalidOperationException($"Wilayah {wilayah.NamaLengkap} tidak ditemukan di database.");
    }

    public void Hapus(Wilayah wilayah)
    {
        using NpgsqlCommand perintah = database.BuatPerintah("""
            DELETE FROM pesan_chat
            WHERE wilayah_id = (SELECT id FROM wilayah WHERE provinsi = @provinsi AND kota = @kota)
            """);
        TambahParameterWilayah(perintah, wilayah);
        perintah.ExecuteNonQuery();
    }

    public void HapusSemua()
    {
        using NpgsqlCommand perintah = database.BuatPerintah("DELETE FROM pesan_chat");
        perintah.ExecuteNonQuery();
    }

    private static void TambahParameterWilayah(NpgsqlCommand perintah, Wilayah wilayah)
    {
        perintah.Parameters.AddWithValue("provinsi", wilayah.Provinsi);
        perintah.Parameters.AddWithValue("kota", wilayah.Kota);
    }
}
