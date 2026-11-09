namespace Junpro_Project;

public enum TingkatDampak
{
    Rendah,
    Sedang,
    Tinggi
}

public sealed record KartuAksi(string Kode, string Kategori, string Judul,
                               string Deskripsi, TingkatDampak Dampak, string Alasan);

// Kelas dasar setiap aturan rekomendasi aksi iklim (PRD §10 F-06).
//
// - Inheritance  : setiap aturan konkret mewarisi kelas ini, sehingga data kartu
//                  dan cara membuat kartunya cukup ditulis sekali di sini.
// - Encapsulation: isi kartu hanya bisa diisi lewat konstruktor dan dibaca lewat
//                  properti get-only; cara menilai profil tersembunyi di subclass.
// - Polymorphism : MesinRekomendasi hanya mengenal tipe AturanAksi. Saat
//                  BerlakuUntuk() dan SusunAlasan() dipanggil, yang dijalankan
//                  adalah versi milik subclass yang sebenarnya.
public abstract class AturanAksi
{
    protected AturanAksi(string kode, string kategori, string judul,
                         string deskripsi, TingkatDampak dampak)
    {
        Kode = kode;
        Kategori = kategori;
        Judul = judul;
        Deskripsi = deskripsi;
        Dampak = dampak;
    }

    public string Kode { get; }
    public string Kategori { get; }
    public string Judul { get; }
    public string Deskripsi { get; }
    public TingkatDampak Dampak { get; }

    public abstract bool BerlakuUntuk(ProfilIklim profil);

    protected abstract string SusunAlasan(ProfilIklim profil);

    // Template method: langkah membuat kartu sama untuk semua aturan,
    // hanya kalimat alasannya yang ditentukan oleh subclass.
    public KartuAksi BuatKartu(ProfilIklim profil) =>
        new(Kode, Kategori, Judul, Deskripsi, Dampak, SusunAlasan(profil));
}

// Aturan yang terpicu bila tren suatu indikator naik minimal sebesar ambang
// tertentu. Subclass cukup menentukan indikator, ambang, isi kartu, dan
// kalimat alasannya.
public abstract class AturanKenaikanTren : AturanAksi
{
    private readonly Indikator indikator;
    private readonly double ambangKenaikan;

    protected AturanKenaikanTren(Indikator indikator, double ambangKenaikan,
                                 string kode, string kategori, string judul,
                                 string deskripsi, TingkatDampak dampak)
        : base(kode, kategori, judul, deskripsi, dampak)
    {
        this.indikator = indikator;
        this.ambangKenaikan = ambangKenaikan;
    }

    public sealed override bool BerlakuUntuk(ProfilIklim profil)
    {
        HasilTren tren = profil.Tren(indikator);
        return tren.Arah == "Naik" && tren.PerubahanTotal >= ambangKenaikan;
    }

    protected sealed override string SusunAlasan(ProfilIklim profil)
    {
        string kenaikan = Format.Nilai(indikator, profil.Tren(indikator).PerubahanTotal);
        return KalimatAlasan(profil.Wilayah.Kota, kenaikan, profil.TahunAwal);
    }

    protected abstract string KalimatAlasan(string kota, string kenaikan, int tahunAwal);
}
