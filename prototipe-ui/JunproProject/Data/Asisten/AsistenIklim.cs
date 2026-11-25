using System.Text.Json;

namespace Junpro_Project;

// Kelas dasar asisten AI NADI (PRD F-04 AI Climate Explainer & F-05 AI Climate
// Chat; di rancangan solusi berperan sebagai IAiExplainerService).
//
// - Inheritance  : AsistenDemo dan AsistenGemini mewarisi kelas ini, sehingga
//                  validasi pertanyaan, pencatatan jumlah permintaan, dan
//                  penyusunan konteks JSON (PRD §11.2) cukup ditulis sekali.
// - Encapsulation: penghitung permintaan disimpan di field private dan hanya
//                  bisa dibaca lewat properti get-only; langkah internal
//                  (membersihkan pertanyaan, mencatat permintaan) bersifat private.
// - Polymorphism : form hanya mengenal tipe AsistenIklim (StatusAplikasi.Asisten).
//                  Saat JelaskanAsync()/JawabAsync() dipanggil, yang dijalankan
//                  adalah SusunPenjelasanAsync()/SusunJawabanAsync() milik
//                  subclass yang sedang aktif — demo atau Gemini.
public abstract class AsistenIklim
{
    // Batas panjang pesan chat (PRD §11.6). Public agar ChatForm memakai angka yang sama.
    public const int BatasKarakterPertanyaan = 500;

    // protected: hanya kelas turunan yang boleh memakai opsi serialisasi ini.
    protected static readonly JsonSerializerOptions OpsiJson = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    // private: tidak bisa diubah dari luar kelas, termasuk oleh subclass.
    private int jumlahPermintaan;

    // Nama mesin yang menyusun jawaban, ditampilkan di banner halaman AI.
    public abstract string Nama { get; }

    // true bila jawaban benar-benar berasal dari model AI (bukan template).
    public abstract bool MemakaiAi { get; }

    // Siapa yang benar-benar menyusun penjelasan terakhir. Bawaannya sama dengan Nama;
    // subclass yang bisa jatuh ke template (AsistenGemini) meng-override agar label jujur.
    public virtual string SumberPenjelasanTerakhir => Nama;

    public virtual bool PenjelasanTerakhirDariAi => MemakaiAi;

    public int JumlahPermintaan => jumlahPermintaan;

    // Template method: urutan langkahnya sama untuk semua asisten, isi
    // penjelasannya ditentukan oleh subclass (polymorphism).
    public async Task<string> JelaskanAsync(ProfilIklim profil, Indikator indikator)
    {
        CatatPermintaan();
        string hasil = await SusunPenjelasanAsync(profil, indikator);
        return hasil.Trim();
    }

    public async Task<string> JawabAsync(ProfilIklim profil, string pertanyaan)
    {
        string bersih = BersihkanPertanyaan(pertanyaan);
        if (bersih.Length == 0)
            return "Tulis dulu pertanyaanmu tentang iklim " + profil.Wilayah.Kota + " ya.";

        CatatPermintaan();
        string hasil = await SusunJawabanAsync(profil, bersih);
        return hasil.Trim();
    }

    protected abstract Task<string> SusunPenjelasanAsync(ProfilIklim profil, Indikator indikator);

    protected abstract Task<string> SusunJawabanAsync(ProfilIklim profil, string pertanyaan);

    // Konteks yang dikirim ke AI: hanya angka hasil hitung aplikasi, bukan data
    // mentah (PRD §11.1 & §11.2). Dipakai subclass yang memanggil model AI.
    protected static string KonteksJson(ProfilIklim profil, params Indikator[] indikator)
    {
        Dictionary<string, object> data = new()
        {
            ["wilayah"] = profil.Wilayah.NamaLengkap,
            ["periode"] = new { mulai = profil.TahunAwal, akhir = profil.TahunAkhir, tahun_valid = profil.Tahun.Length },
            ["sumber_data"] = "ERA5 via Open-Meteo",
        };

        foreach (Indikator i in indikator)
        {
            HasilTren t = profil.Tren(i);
            data[Format.NamaIndikator(i).ToLowerInvariant().Replace(' ', '_')] = new
            {
                satuan = Satuan(i),
                rata_rata = Math.Round(profil.RataRata(i), 2),
                rata_rata_baseline_1991_2020 = profil.Baseline(i) is double b ? Math.Round(b, 2) : (double?)null,
                nilai_tahun_terakhir = Math.Round(profil.Terakhir(i), 2),
                tren_per_dekade = Math.Round(t.SlopePerDekade, 2),
                perubahan_total = Math.Round(t.PerubahanTotal, 2),
                arah = t.Arah,
                keyakinan = t.Keyakinan,
            };
        }

        return JsonSerializer.Serialize(data, OpsiJson);
    }

    private void CatatPermintaan() => jumlahPermintaan++;

    private static string Satuan(Indikator i) =>
        Format.AdalahSuhu(i) ? "°C" : i == Indikator.CurahHujan ? "mm/tahun" : "hari/tahun";

    private static string BersihkanPertanyaan(string pertanyaan)
    {
        string bersih = pertanyaan.Trim();
        return bersih.Length > BatasKarakterPertanyaan ? bersih[..BatasKarakterPertanyaan] : bersih;
    }
}
