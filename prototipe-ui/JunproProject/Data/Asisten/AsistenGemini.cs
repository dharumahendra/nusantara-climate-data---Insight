using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Junpro_Project;

// Asisten yang memanggil Google Gemini API (PRD §5 & §11). Mewarisi
// AsistenIklim dan meng-override cara menyusun penjelasan/jawaban: angka
// konteks dikirim sebagai JSON, lalu Gemini menerjemahkannya menjadi paragraf.
// Bila Gemini gagal dihubungi, jawaban diambil dari template mode demo supaya
// aplikasi tidak pernah crash (PRD §12 Ketahanan).
public sealed class AsistenGemini : AsistenIklim
{
    // Nama model & endpoint dapat berubah; verifikasi ke dokumentasi Google AI (PRD §11.6).
    // Dicoba berurutan: model bernomor lebih stabil, alias "-latest" menjadi cadangan.
    // Bila satu model sibuk (5xx), hilang (404), kuotanya habis (429), atau tidak membalas,
    // model berikutnya dicoba.
    private static readonly string[] DaftarModel = { "gemini-2.5-flash", "gemini-flash-latest" };
    private const string AwalEndpoint = "https://generativelanguage.googleapis.com/v1beta/models/";
    private const string AkhiranEndpoint = ":generateContent";

    // Batas tunggu per model; dua model berarti paling lama sekitar 40 detik sebelum mode demo.
    private static readonly TimeSpan BatasTunggu = TimeSpan.FromSeconds(20);
    private static readonly HttpClient httpBersama = new() { Timeout = BatasTunggu };

    private readonly HttpClient http;
    private string modelTerpakai = DaftarModel[0];

    // Encapsulation: API key hanya disimpan di field private ini. Tidak ada
    // properti yang mengembalikannya, sehingga tidak bisa dibaca atau
    // tercetak ke log dari luar kelas (PRD §11.5).
    private readonly string apiKey;

    public AsistenGemini(string apiKey) : this(apiKey, null)
    {
    }

    // Klien HTTP dapat diganti (dipakai pengujian dengan respons palsu).
    public AsistenGemini(string apiKey, HttpClient? klien)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("API key Gemini wajib diisi.", nameof(apiKey));
        this.apiKey = apiKey.Trim();
        http = klien ?? httpBersama;
    }

    public override string Nama => "Google Gemini (" + modelTerpakai + ")";

    public override bool MemakaiAi => true;

    // Override (polymorphism): versi Gemini mengirim prompt Explainer (PRD §11.3).
    protected override async Task<string> SusunPenjelasanAsync(ProfilIklim profil, Indikator indikator)
    {
        string prompt = TemplatePrompt.Explainer.Replace("{climate_json}", KonteksJson(profil, indikator));
        try
        {
            return await KirimAsync(prompt, null);
        }
        catch (GagalMenghubungiGemini ex)
        {
            return AsistenDemo.TeksPenjelasan(profil, indikator) + CatatanCadangan(ex);
        }
    }

    // Override (polymorphism): versi Gemini mengirim prompt Chat (PRD §11.4).
    protected override async Task<string> SusunJawabanAsync(ProfilIklim profil, string pertanyaan)
    {
        string konteks = KonteksJson(profil,
            Indikator.SuhuRataRata, Indikator.CurahHujan, Indikator.HariPanas, Indikator.HariHujanLebat);
        string instruksi = TemplatePrompt.Chat.Replace("{climate_json}", konteks);
        try
        {
            return await KirimAsync(pertanyaan, instruksi);
        }
        catch (GagalMenghubungiGemini ex)
        {
            return AsistenDemo.TeksJawaban(profil, pertanyaan) + CatatanCadangan(ex);
        }
    }

    // Dipakai tombol "Uji Koneksi" di Pengaturan: mengirim satu permintaan
    // kecil untuk memastikan API key diterima Gemini.
    public async Task<(bool Berhasil, string Pesan)> UjiKoneksiAsync()
    {
        try
        {
            await KirimAsync("Balas dengan satu kata: siap", null);
            return (true, "Koneksi ke Gemini berhasil. Simpan API key agar Penjelasan AI dan Chat AI memakai " +
                          modelTerpakai + ".");
        }
        catch (GagalMenghubungiGemini ex)
        {
            return (false, "Koneksi ke Gemini gagal: " + ex.Message + ".");
        }
    }

    // Mencoba tiap model berurutan. Kegagalan yang berasal dari model (sibuk, hilang, tidak
    // membalas, kuota habis) berlanjut ke model berikutnya, karena kuota gratis Gemini dihitung
    // per model. Key ditolak dan tanpa internet langsung dilaporkan: model lain tidak akan berbeda.
    private async Task<string> KirimAsync(string pesan, string? instruksiSistem)
    {
        object isi = instruksiSistem == null
            ? new { contents = new[] { new { role = "user", parts = new[] { new { text = pesan } } } } }
            : new
            {
                system_instruction = new { parts = new[] { new { text = instruksiSistem } } },
                contents = new[] { new { role = "user", parts = new[] { new { text = pesan } } } }
            };

        GagalMenghubungiGemini? terakhir = null;
        foreach (string model in DaftarModel)
        {
            try
            {
                string teks = await KirimKeModelAsync(model, isi);
                modelTerpakai = model;
                return teks;
            }
            catch (GagalMenghubungiGemini ex) when (ex.BisaCobaModelLain)
            {
                terakhir = ex;
            }
        }
        throw terakhir!;
    }

    private async Task<string> KirimKeModelAsync(string model, object isi)
    {
        using HttpRequestMessage permintaan = new(HttpMethod.Post, AwalEndpoint + model + AkhiranEndpoint)
        {
            Content = JsonContent.Create(isi)
        };
        permintaan.Headers.Add("x-goog-api-key", apiKey);

        try
        {
            using HttpResponseMessage respons = await http.SendAsync(permintaan);
            if (!respons.IsSuccessStatusCode)
                throw new GagalMenghubungiGemini(PesanStatus(respons.StatusCode, model),
                    BisaCobaModelLain(respons.StatusCode));

            using JsonDocument json = JsonDocument.Parse(await respons.Content.ReadAsStringAsync());
            return AmbilTeks(json.RootElement);
        }
        catch (HttpRequestException)
        {
            throw new GagalMenghubungiGemini("tidak ada koneksi internet atau server Gemini tidak terjangkau");
        }
        catch (TaskCanceledException)
        {
            throw new GagalMenghubungiGemini("Gemini tidak membalas dalam " + (int)BatasTunggu.TotalSeconds +
                                             " detik, kemungkinan sedang sibuk", bisaCobaModelLain: true);
        }
        catch (JsonException)
        {
            throw new GagalMenghubungiGemini("format respons Gemini tidak dikenali");
        }
    }

    private static string AmbilTeks(JsonElement akar)
    {
        if (akar.TryGetProperty("candidates", out JsonElement kandidat) && kandidat.GetArrayLength() > 0 &&
            kandidat[0].TryGetProperty("content", out JsonElement konten) &&
            konten.TryGetProperty("parts", out JsonElement bagian))
        {
            string teks = string.Concat(bagian.EnumerateArray()
                .Select(b => b.TryGetProperty("text", out JsonElement t) ? t.GetString() : ""));
            if (teks.Trim().Length > 0) return teks;
        }
        throw new GagalMenghubungiGemini("Gemini tidak mengembalikan jawaban (mungkin diblokir filter keamanan)");
    }

    private static bool BisaCobaModelLain(HttpStatusCode status) =>
        status is HttpStatusCode.NotFound or HttpStatusCode.TooManyRequests || (int)status >= 500;

    private static string PesanStatus(HttpStatusCode status, string model) => status switch
    {
        HttpStatusCode.TooManyRequests => "Kuota AI sedang penuh, coba lagi beberapa saat lagi",
        HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden
            => "API key ditolak oleh Gemini — periksa kembali key dari Google AI Studio",
        HttpStatusCode.NotFound => "model " + model + " tidak ditemukan",
        HttpStatusCode.ServiceUnavailable => "server Gemini sedang sibuk (503), coba lagi beberapa saat lagi",
        _ => "server Gemini membalas dengan status " + (int)status,
    };

    private static string CatatanCadangan(GagalMenghubungiGemini ex) =>
        $"\n\n(Gemini tidak dapat dipakai: {ex.Message}. Jawaban di atas disusun oleh mode demo.)";

    // private nested class: jenis error ini hanya dikenal di dalam AsistenGemini.
    private sealed class GagalMenghubungiGemini : Exception
    {
        public bool BisaCobaModelLain { get; }

        public GagalMenghubungiGemini(string pesan, bool bisaCobaModelLain = false) : base(pesan)
        {
            BisaCobaModelLain = bisaCobaModelLain;
        }
    }
}

// internal: template prompt hanya dipakai di dalam assembly NADI, tidak
// menjadi bagian API publik. Isi mengikuti PRD §11.3 dan §11.4.
internal static class TemplatePrompt
{
    internal const string Explainer = """
        Kamu adalah asisten edukasi iklim untuk aplikasi NADI.

        TUGAS
        Jelaskan data iklim berikut dalam 3-5 kalimat Bahasa Indonesia yang mudah
        dipahami pelajar SMA.

        ATURAN KETAT
        - Gunakan HANYA angka yang ada di data JSON di bawah. Dilarang menghitung,
          memperkirakan, atau menambah angka lain.
        - Sebutkan nama wilayah dan rentang tahun.
        - Jika "keyakinan" bernilai "Lemah", gunakan kata seperti "belum jelas" atau
          "belum menunjukkan pola tegas". Jangan menyimpulkan dengan tegas.
        - Jangan memprediksi kondisi masa depan.
        - Jangan memberi nasihat kesehatan atau peringatan bencana.
        - Tutup dengan satu kalimat tentang apa arti data ini bagi kehidupan
          sehari-hari warga wilayah tersebut.
        - Jangan gunakan bullet point. Tulis sebagai paragraf mengalir.

        DATA
        {climate_json}
        """;

    internal const string Chat = """
        Kamu adalah asisten iklim NADI. Kamu hanya membahas iklim, cuaca, lingkungan,
        dan aksi iklim.

        KONTEKS WILAYAH AKTIF
        {climate_json}

        ATURAN KETAT
        - Jawab hanya berdasarkan data konteks di atas. Jika pengguna menanyakan
          wilayah, indikator, atau periode yang tidak ada di konteks, katakan terus
          terang bahwa datanya belum dimuat dan sarankan memilih wilayah itu di menu
          Dashboard Explore. JANGAN mengarang angka.
        - Jika pertanyaan di luar topik iklim/lingkungan, tolak dengan sopan dalam
          satu kalimat dan tawarkan kembali topik iklim.
        - Jangan memprediksi cuaca atau iklim masa depan.
        - Jangan memberi peringatan dini bencana; arahkan ke BMKG untuk informasi resmi.
        - Jawaban maksimal 4 kalimat kecuali pengguna meminta detail.
        - Bahasa Indonesia yang santai tapi sopan.
        - Jangan gunakan format Markdown (tanpa tanda bintang atau bullet).
        """;
}
