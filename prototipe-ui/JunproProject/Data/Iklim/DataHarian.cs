namespace Junpro_Project;

// Deret harian mentah dari Open-Meteo (PRD §8.1). Array bersifat paralel:
// indeks ke-i di semua array merujuk tanggal yang sama. Nilai null berarti
// data hilang dan tidak boleh diganti 0.
public sealed record DataHarian(
    DateOnly[] Tanggal,
    double?[] SuhuRata,
    double?[] SuhuMaks,
    double?[] SuhuMin,
    double?[] CurahHujan,
    double?[] KecepatanAngin)
{
    public int Jumlah => Tanggal.Length;
}

// Kegagalan mengambil data iklim. Tiga turunannya cocok dengan katalog error
// PRD §15 supaya UI dapat menampilkan pesan dan tindakan pemulihan yang tepat.
public abstract class SumberDataException : Exception
{
    protected SumberDataException(string pesan, Exception? penyebab = null) : base(pesan, penyebab)
    {
    }

    // Pesan untuk pengguna (bukan stack trace), mengikuti PRD §15.
    public abstract string PesanPengguna { get; }
}

// E-01: tidak ada internet / server tidak terjangkau.
public sealed class TidakAdaKoneksiException : SumberDataException
{
    public TidakAdaKoneksiException(string pesan, Exception? penyebab = null) : base(pesan, penyebab)
    {
    }

    public override string PesanPengguna =>
        "Butuh koneksi internet untuk mengunduh data wilayah ini pertama kali. " +
        "Periksa koneksi Anda lalu coba lagi. Wilayah yang sudah pernah diunduh tetap bisa dibuka.";
}

// E-02: API membalas 4xx/5xx setelah retry.
public sealed class SumberTidakTersediaException : SumberDataException
{
    public SumberTidakTersediaException(string pesan, Exception? penyebab = null) : base(pesan, penyebab)
    {
    }

    public override string PesanPengguna =>
        "Sumber data iklim (Open-Meteo) sedang tidak tersedia. Coba lagi beberapa saat lagi.";
}

// E-02 (varian): kuota permintaan Open-Meteo habis (HTTP 429). Mengulang dalam hitungan
// detik tidak membantu, jadi tidak di-retry otomatis dan pengguna diberi tahu harus menunggu.
public sealed class BatasPermintaanException : SumberDataException
{
    public enum Jenis { Menit, Jam, Hari }

    public BatasPermintaanException(Jenis batas) : base($"Batas permintaan Open-Meteo per {batas} tercapai")
    {
        Batas = batas;
    }

    public Jenis Batas { get; }

    public override string PesanPengguna => Batas switch
    {
        Jenis.Hari => "Batas permintaan data harian ke Open-Meteo sudah tercapai. Coba lagi besok. " +
                      "Wilayah yang sudah pernah diunduh tetap dapat dibuka.",
        Jenis.Jam => "Batas permintaan data per jam ke Open-Meteo sudah tercapai. Coba lagi sekitar satu jam lagi. " +
                     "Wilayah yang sudah pernah diunduh tetap dapat dibuka.",
        _ => "Batas permintaan data per menit ke Open-Meteo sudah tercapai (satu unduhan 30 tahun cukup berat). " +
             "Tunggu sekitar satu menit lalu tekan Coba Lagi."
    };
}

// E-03: respons API tidak sesuai format yang diharapkan (PRD §8.1).
public sealed class ClimateDataFormatException : SumberDataException
{
    public ClimateDataFormatException(string pesan, Exception? penyebab = null) : base(pesan, penyebab)
    {
    }

    public override string PesanPengguna =>
        "Data dari sumber iklim tidak sesuai format yang diharapkan, sehingga tidak ditampilkan " +
        "dan tidak disimpan. Coba lagi nanti.";
}
