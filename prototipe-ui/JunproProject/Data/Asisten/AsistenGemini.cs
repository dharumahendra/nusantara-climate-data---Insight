using System.Diagnostics;
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
    // Nama model dapat berubah atau ditutup untuk key baru; verifikasi ke dokumentasi
    // Google AI (PRD §11.6). Model dicoba berurutan. Bila satu model tidak tersedia (404),
    // kuotanya habis (429), sibuk (5xx), atau tidak membalas, model berikutnya dicoba.
    // Dua alias "-latest" di belakang selalu diarahkan Google ke model terbaru, sehingga
    // aplikasi tetap punya cadangan bila model bernomor dihentikan.
    // (Diuji 23 Nov 2026: gemini-2.5-flash sudah ditutup untuk key baru.)
    public static readonly IReadOnlyList<string> DaftarModel =
        new[] { "gemini-3.5-flash", "gemini-flash-latest", "gemini-flash-lite-latest" };

    private const string AwalEndpoint = "https://generativelanguage.googleapis.com/v1beta/models/";
    private const string AkhiranEndpoint = ":generateContent";

    // Batas tunggu per model, dan batas total untuk semua model (PRD §8.1: timeout 30 detik).
    // Model yang menolak cepat (404/429/503) tidak menghabiskan waktu, jadi cadangan tetap dicoba.
    private static readonly TimeSpan BatasPerModelBawaan = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan BatasTotalBawaan = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan SisaMinimum = TimeSpan.FromSeconds(3);
    private static readonly HttpClient httpBersama = new() { Timeout = Timeout.InfiniteTimeSpan };

    private readonly HttpClient http;
    private readonly TimeSpan batasPerModel;
    private readonly TimeSpan batasTotal;
    private string modelTerpakai = DaftarModel[0];
    private bool penjelasanTerakhirDariCadangan;

    // Encapsulation: API key hanya disimpan di field private ini. Tidak ada
    // properti yang mengembalikannya, sehingga tidak bisa dibaca atau
    // tercetak ke log dari luar kelas (PRD §11.5).
    private readonly string apiKey;

    public AsistenGemini(string apiKey) : this(apiKey, null)
    {
    }

    // Klien HTTP dan batas waktu dapat diganti (dipakai pengujian dengan respons palsu).
    public AsistenGemini(string apiKey, HttpClient? klien, TimeSpan? batasPerModel = null, TimeSpan? batasTotal = null)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("API key Gemini wajib diisi.", nameof(apiKey));
        this.apiKey = apiKey.Trim();
        http = klien ?? httpBersama;
        this.batasPerModel = batasPerModel ?? BatasPerModelBawaan;
        this.batasTotal = batasTotal ?? BatasTotalBawaan;
    }

    public override string Nama => "Google Gemini (" + modelTerpakai + ")";

    public override bool MemakaiAi => true;

    // Bila Gemini gagal, penjelasan disusun template mode demo; label sumbernya harus ikut jujur.
    public override string SumberPenjelasanTerakhir =>
        penjelasanTerakhirDariCadangan ? "mode demo NADI (Gemini tidak dapat dipakai)" : Nama;

    public override bool PenjelasanTerakhirDariAi => !penjelasanTerakhirDariCadangan;

    // Override (polymorphism): versi Gemini mengirim prompt Explainer (PRD §11.3).
    protected override async Task<string> SusunPenjelasanAsync(ProfilIklim profil, Indikator indikator)
    {
        string prompt = TemplatePrompt.Explainer.Replace("{climate_json}", KonteksJson(profil, indikator));
        try
        {
            string teks = await KirimAsync(prompt, null);
            penjelasanTerakhirDariCadangan = false;
            return teks;
        }
        catch (GagalMenghubungiGemini ex)
        {
            penjelasanTerakhirDariCadangan = true;
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

    // Mencoba tiap model berurutan. Kegagalan yang berasal dari model (tidak tersedia, sibuk,
    // tidak membalas, kuota habis) berlanjut ke model berikutnya, karena kuota gratis Gemini
    // dihitung per model. Key ditolak dan tanpa internet langsung dilaporkan: model lain tidak
    // akan berbeda. Bila semua model gagal, alasan tiap model dilaporkan supaya penyebab
    // sebenarnya (misalnya 404 di model utama) tidak tertutup oleh kegagalan model cadangan.
    private async Task<string> KirimAsync(string pesan, string? instruksiSistem)
    {
        object isi = instruksiSistem == null
            ? new { contents = new[] { new { role = "user", parts = new[] { new { text = pesan } } } } }
            : new
            {
                system_instruction = new { parts = new[] { new { text = instruksiSistem } } },
                contents = new[] { new { role = "user", parts = new[] { new { text = pesan } } } }
            };

        Stopwatch jam = Stopwatch.StartNew();
        List<string> alasan = new();
        List<string> sudahDicoba = new();

        // Langkah 1: daftar model tetap.
        string? teks = await CobaModelAsync(DaftarModel, isi, jam, alasan, sudahDicoba);
        if (teks != null) return teks;

        // Langkah 2 (bila semuanya gagal dan waktu masih ada): tanya Google model flash apa yang
        // tersedia untuk key ini, lalu coba maksimal tiga. Ini menjaga aplikasi tetap jalan saat
        // Google menutup model bernomor untuk key baru (kasus gemini-2.5-flash, 23 Nov 2026).
        if (batasTotal - jam.Elapsed >= SisaMinimum)
        {
            List<string> ditemukan = (await CariModelTersediaAsync(batasTotal - jam.Elapsed))
                .Where(m => !sudahDicoba.Contains(m)).Take(3).ToList();
            teks = await CobaModelAsync(ditemukan, isi, jam, alasan, sudahDicoba);
            if (teks != null) return teks;
        }

        throw new GagalMenghubungiGemini(alasan.Count == 1
            ? alasan[0]
            : "semua model gagal — " + string.Join("; ", alasan));
    }

    // Mengembalikan teks dari model pertama yang berhasil, atau null bila semuanya gagal
    // dengan alasan yang membolehkan mencoba model lain. Alasan tiap model dikumpulkan.
    private async Task<string?> CobaModelAsync(IEnumerable<string> daftar, object isi, Stopwatch jam,
                                               List<string> alasan, List<string> sudahDicoba)
    {
        foreach (string model in daftar)
        {
            TimeSpan sisa = batasTotal - jam.Elapsed;
            if (sisa < SisaMinimum)
            {
                alasan.Add(model + " tidak sempat dicoba (batas waktu " + (int)batasTotal.TotalSeconds + " detik habis)");
                return null;
            }

            sudahDicoba.Add(model);
            try
            {
                string teks = await KirimKeModelAsync(model, isi, sisa < batasPerModel ? sisa : batasPerModel);
                modelTerpakai = model;
                return teks;
            }
            catch (GagalMenghubungiGemini ex) when (ex.BisaCobaModelLain)
            {
                alasan.Add(ex.Message);
            }
        }
        return null;
    }

    // Membaca daftar model dari Google (ListModels) dan memilih model "flash" untuk teks yang
    // mendukung generateContent. Model khusus (gambar, suara, live, embedding) dilewati.
    // Gagal membaca daftar bukan error: cukup kembalikan daftar kosong.
    private async Task<List<string>> CariModelTersediaAsync(TimeSpan sisa)
    {
        if (sisa < SisaMinimum) return new List<string>();
        TimeSpan batas = sisa - SisaMinimum < batasPerModel ? sisa - SisaMinimum : batasPerModel;
        if (batas <= TimeSpan.Zero) return new List<string>();

        try
        {
            using HttpRequestMessage permintaan = new(HttpMethod.Get, AwalEndpoint.TrimEnd('/') + "?pageSize=200");
            permintaan.Headers.Add("x-goog-api-key", apiKey);
            using CancellationTokenSource waktu = new(batas);
            using HttpResponseMessage respons = await http.SendAsync(permintaan, waktu.Token);
            if (!respons.IsSuccessStatusCode) return new List<string>();

            using JsonDocument json = JsonDocument.Parse(await respons.Content.ReadAsStringAsync(waktu.Token));
            return PilihModelFlash(json.RootElement);
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or JsonException
                                       or InvalidOperationException or KeyNotFoundException)
        {
            return new List<string>();
        }
    }

    private static readonly string[] KataModelKhusus =
        { "image", "tts", "audio", "live", "embedding", "vision", "thinking", "computer", "robotics" };

    public static List<string> PilihModelFlash(JsonElement akar)
    {
        List<string> hasil = new();
        if (!akar.TryGetProperty("models", out JsonElement daftar) || daftar.ValueKind != JsonValueKind.Array)
            return hasil;

        foreach (JsonElement m in daftar.EnumerateArray())
        {
            if (!m.TryGetProperty("name", out JsonElement n) || n.GetString() is not string nama) continue;
            nama = nama.StartsWith("models/") ? nama["models/".Length..] : nama;
            if (!nama.StartsWith("gemini-") || !nama.Contains("flash")) continue;
            if (KataModelKhusus.Any(k => nama.Contains(k))) continue;
            if (!m.TryGetProperty("supportedGenerationMethods", out JsonElement metode) ||
                metode.ValueKind != JsonValueKind.Array ||
                !metode.EnumerateArray().Any(x => x.GetString() == "generateContent")) continue;
            hasil.Add(nama);
        }

        // Model stabil (tanpa "preview"/"exp") dulu, lalu versi tertinggi dulu.
        return hasil
            .OrderBy(n => n.Contains("preview") || n.Contains("exp") ? 1 : 0)
            .ThenByDescending(n => n, StringComparer.Ordinal)
            .ToList();
    }

    private async Task<string> KirimKeModelAsync(string model, object isi, TimeSpan batas)
    {
        using HttpRequestMessage permintaan = new(HttpMethod.Post, AwalEndpoint + model + AkhiranEndpoint)
        {
            Content = JsonContent.Create(isi)
        };
        permintaan.Headers.Add("x-goog-api-key", apiKey);

        using CancellationTokenSource waktu = new(batas);
        try
        {
            using HttpResponseMessage respons = await http.SendAsync(permintaan, waktu.Token);
            if (!respons.IsSuccessStatusCode)
                throw new GagalMenghubungiGemini(PesanStatus(respons.StatusCode, model),
                    BisaCobaModelLain(respons.StatusCode));

            using JsonDocument json = JsonDocument.Parse(await respons.Content.ReadAsStringAsync(waktu.Token));
            return AmbilTeks(json.RootElement);
        }
        catch (HttpRequestException)
        {
            throw new GagalMenghubungiGemini("tidak ada koneksi internet atau server Gemini tidak terjangkau");
        }
        catch (OperationCanceledException)
        {
            throw new GagalMenghubungiGemini(model + " tidak membalas dalam " + Math.Round(batas.TotalSeconds) +
                                             " detik (kemungkinan sedang sibuk)", bisaCobaModelLain: true);
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
        HttpStatusCode.TooManyRequests => "kuota " + model + " sedang penuh (429)",
        HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden
            => "API key ditolak oleh Gemini — periksa kembali key dari Google AI Studio",
        HttpStatusCode.NotFound => model + " tidak tersedia untuk key ini (404)",
        HttpStatusCode.ServiceUnavailable => model + " sedang sibuk (503)",
        _ => model + " membalas dengan status " + (int)status,
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
