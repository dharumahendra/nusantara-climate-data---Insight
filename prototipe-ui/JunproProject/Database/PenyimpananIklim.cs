using Npgsql;
using NpgsqlTypes;

namespace Junpro_Project;

// Menyimpan data iklim harian per wilayah di tabel `data_iklim_harian`
// (PRD §9 ClimateDailyRecords) agar wilayah yang pernah diunduh bisa dibuka
// lagi tanpa internet (PRD D-06, AC-01.2 dan AC-01.4).
public sealed class PenyimpananIklim
{
    private readonly DatabaseNadi database;

    public PenyimpananIklim(DatabaseNadi database)
    {
        this.database = database;
    }

    // Mengembalikan null bila data wilayah ini belum lengkap untuk rentang yang
    // diminta, sehingga pemanggil mengunduh ulang dari Open-Meteo.
    public DataHarian? Ambil(Wilayah wilayah, DateOnly awal, DateOnly akhir)
    {
        using NpgsqlCommand perintah = database.BuatPerintah("""
            SELECT d.tanggal, d.suhu_rata, d.suhu_maks, d.suhu_min, d.curah_hujan, d.kecepatan_angin
            FROM data_iklim_harian d
            JOIN wilayah w ON w.id = d.wilayah_id
            WHERE w.provinsi = @provinsi AND w.kota = @kota AND d.tanggal BETWEEN @awal AND @akhir
            ORDER BY d.tanggal
            """);
        perintah.Parameters.AddWithValue("provinsi", wilayah.Provinsi);
        perintah.Parameters.AddWithValue("kota", wilayah.Kota);
        perintah.Parameters.AddWithValue("awal", awal);
        perintah.Parameters.AddWithValue("akhir", akhir);
        using NpgsqlDataReader baris = perintah.ExecuteReader();

        List<DateOnly> tanggal = new();
        List<double?> rata = new(), maks = new(), min = new(), hujan = new(), angin = new();
        while (baris.Read())
        {
            tanggal.Add(baris.GetFieldValue<DateOnly>(0));
            rata.Add(Baca(baris, 1));
            maks.Add(Baca(baris, 2));
            min.Add(Baca(baris, 3));
            hujan.Add(Baca(baris, 4));
            angin.Add(Baca(baris, 5));
        }

        int hariDiminta = akhir.DayNumber - awal.DayNumber + 1;
        if (tanggal.Count != hariDiminta) return null;

        return new DataHarian(tanggal.ToArray(), rata.ToArray(), maks.ToArray(),
                              min.ToArray(), hujan.ToArray(), angin.ToArray());
    }

    // Mengganti seluruh data harian wilayah ini dalam satu transaksi, sehingga
    // tidak pernah tersimpan data parsial bila terjadi kegagalan di tengah jalan.
    public void Simpan(Wilayah wilayah, DataHarian data)
    {
        using NpgsqlConnection koneksi = database.BukaKoneksi();
        using NpgsqlTransaction transaksi = koneksi.BeginTransaction();

        int idWilayah;
        using (NpgsqlCommand cari = new("SELECT id FROM wilayah WHERE provinsi = @provinsi AND kota = @kota",
                                        koneksi, transaksi))
        {
            cari.Parameters.AddWithValue("provinsi", wilayah.Provinsi);
            cari.Parameters.AddWithValue("kota", wilayah.Kota);
            idWilayah = cari.ExecuteScalar() as int? ??
                        throw new InvalidOperationException($"Wilayah {wilayah.NamaLengkap} tidak ditemukan di database.");
        }

        using (NpgsqlCommand hapus = new("DELETE FROM data_iklim_harian WHERE wilayah_id = @id", koneksi, transaksi))
        {
            hapus.Parameters.AddWithValue("id", idWilayah);
            hapus.ExecuteNonQuery();
        }

        using (NpgsqlBinaryImporter penulis = koneksi.BeginBinaryImport(
                   "COPY data_iklim_harian (wilayah_id, tanggal, suhu_rata, suhu_maks, suhu_min, curah_hujan, " +
                   "kecepatan_angin) FROM STDIN (FORMAT BINARY)"))
        {
            for (int i = 0; i < data.Jumlah; i++)
            {
                penulis.StartRow();
                penulis.Write(idWilayah, NpgsqlDbType.Integer);
                penulis.Write(data.Tanggal[i], NpgsqlDbType.Date);
                Tulis(penulis, data.SuhuRata[i]);
                Tulis(penulis, data.SuhuMaks[i]);
                Tulis(penulis, data.SuhuMin[i]);
                Tulis(penulis, data.CurahHujan[i]);
                Tulis(penulis, data.KecepatanAngin[i]);
            }
            penulis.Complete();
        }

        transaksi.Commit();
    }

    // Nilai hilang disimpan sebagai NULL, bukan 0 (PRD §8.1).
    private static double? Baca(NpgsqlDataReader baris, int kolom) =>
        baris.IsDBNull(kolom) ? null : baris.GetDouble(kolom);

    private static void Tulis(NpgsqlBinaryImporter penulis, double? nilai)
    {
        if (nilai is double v) penulis.Write(v, NpgsqlDbType.Double);
        else penulis.WriteNull();
    }
}
