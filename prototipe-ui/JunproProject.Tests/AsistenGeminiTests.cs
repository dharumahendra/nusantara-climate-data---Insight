using System.Net;
using System.Text;
using Xunit;

namespace Junpro_Project.Tests;

// Ketahanan AsistenGemini terhadap model yang sibuk, hilang, atau tidak membalas.
// Memakai klien HTTP palsu, jadi tidak membutuhkan internet maupun API key asli.
public class AsistenGeminiTests
{
    private const string Sukses =
        "{\"candidates\":[{\"content\":{\"parts\":[{\"text\":\"Siap\"}]}}]}";

    private sealed class HandlerPalsu : HttpMessageHandler
    {
        private readonly Func<string, CancellationToken, Task<HttpResponseMessage>> balas;
        public List<string> ModelDipanggil { get; } = new();

        public HandlerPalsu(Func<string, HttpResponseMessage> balas)
            : this((m, _) => Task.FromResult(balas(m)))
        {
        }

        public HandlerPalsu(Func<string, CancellationToken, Task<HttpResponseMessage>> balas) => this.balas = balas;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            string model = request.RequestUri!.AbsolutePath.Split('/').Last().Split(':')[0];
            ModelDipanggil.Add(model);
            return balas(model, ct);
        }
    }

    // Meniru server yang tidak pernah membalas: menunggu sampai permintaan dibatalkan.
    private static async Task<HttpResponseMessage> TidakMembalas(CancellationToken ct)
    {
        await Task.Delay(Timeout.Infinite, ct);
        throw new InvalidOperationException("tidak terjangkau");
    }

    private static readonly string M1 = AsistenGemini.DaftarModel[0];
    private static readonly string M2 = AsistenGemini.DaftarModel[1];

    private static HttpResponseMessage Respons(HttpStatusCode kode, string isi = "{}") =>
        new(kode) { Content = new StringContent(isi, Encoding.UTF8, "application/json") };

    private static ProfilIklim ProfilUji()
    {
        int[] tahun = Enumerable.Range(1996, 30).ToArray();
        double[] Garis(double awal, double ubah) => tahun.Select(t => awal + ubah * (t - 1996) / 29.0).ToArray();
        Dictionary<Indikator, double[]> deret = new()
        {
            [Indikator.SuhuRataRata] = Garis(25, 1),
            [Indikator.SuhuMaksimum] = Garis(32, 0),
            [Indikator.SuhuMinimum] = Garis(20, 0),
            [Indikator.CurahHujan] = Garis(2000, 0),
            [Indikator.HariPanas] = Garis(35, 40),
            [Indikator.HariHujan] = Garis(150, 0),
            [Indikator.HariHujanLebat] = Garis(3, 0),
        };
        return new ProfilIklim(new Wilayah("DI Yogyakarta", "Sleman", -7.7167, 110.3550), tahun, deret,
                               new Dictionary<Indikator, double>(), 32.0, Array.Empty<int>(), 30);
    }

    private static AsistenGemini Buat(HandlerPalsu handler) => new("kunci-palsu", new HttpClient(handler));

    [Fact]
    public void DaftarModel_TidakMemakaiModelYangSudahDitutup()
    {
        // 23 Nov 2026: gemini-2.0-flash dan gemini-2.5-flash membalas 404 untuk key baru.
        Assert.DoesNotContain("gemini-2.0-flash", AsistenGemini.DaftarModel);
        Assert.DoesNotContain("gemini-2.5-flash", AsistenGemini.DaftarModel);
        Assert.True(AsistenGemini.DaftarModel.Count >= 3);
        Assert.Contains(AsistenGemini.DaftarModel, m => m.EndsWith("-latest"));
    }

    [Fact]
    public async Task ModelUtamaBerhasil_ModelCadanganTidakDipanggil()
    {
        var handler = new HandlerPalsu(_ => Respons(HttpStatusCode.OK, Sukses));
        var (berhasil, _) = await Buat(handler).UjiKoneksiAsync();

        Assert.True(berhasil);
        Assert.Equal(new[] { M1 }, handler.ModelDipanggil);
    }

    [Theory]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    public async Task ModelUtamaGagal_BeralihKeModelCadangan(HttpStatusCode gagal)
    {
        var handler = new HandlerPalsu(m => m == M1 ? Respons(gagal) : Respons(HttpStatusCode.OK, Sukses));
        AsistenGemini asisten = Buat(handler);

        var (berhasil, pesan) = await asisten.UjiKoneksiAsync();

        Assert.True(berhasil);
        Assert.Equal(new[] { M1, M2 }, handler.ModelDipanggil);
        Assert.Contains(M2, asisten.Nama);
        Assert.Contains(M2, pesan);
    }

    [Fact]
    public async Task DuaModelGagal_ModelKetigaDicoba()
    {
        string m3 = AsistenGemini.DaftarModel[2];
        var handler = new HandlerPalsu(m => m == m3 ? Respons(HttpStatusCode.OK, Sukses) : Respons(HttpStatusCode.ServiceUnavailable));
        AsistenGemini asisten = Buat(handler);

        var (berhasil, _) = await asisten.UjiKoneksiAsync();

        Assert.True(berhasil);
        Assert.Equal(AsistenGemini.DaftarModel, handler.ModelDipanggil);
        Assert.Contains(m3, asisten.Nama);
    }

    // Kasus Dharu (23 Nov 2026): model utama 404, cadangan 503. Pesan harus memuat keduanya,
    // supaya penyebab sebenarnya (model utama tidak tersedia) tidak tertutup.
    [Fact]
    public async Task SemuaModelGagal_PesanMenyebutAlasanTiapModel()
    {
        var handler = new HandlerPalsu(m => m == M1 ? Respons(HttpStatusCode.NotFound) : Respons(HttpStatusCode.ServiceUnavailable));
        var (berhasil, pesan) = await Buat(handler).UjiKoneksiAsync();

        Assert.False(berhasil);
        Assert.Contains(M1 + " tidak tersedia untuk key ini (404)", pesan);
        Assert.Contains(M2 + " sedang sibuk (503)", pesan);
        Assert.DoesNotContain("terlalu lama", pesan);
    }

    private const string DaftarModelGoogle = """
        {"models":[
          {"name":"models/gemini-2.5-flash","supportedGenerationMethods":["generateContent"]},
          {"name":"models/gemini-9.0-flash","supportedGenerationMethods":["generateContent","countTokens"]},
          {"name":"models/gemini-9.0-flash-image","supportedGenerationMethods":["generateContent"]},
          {"name":"models/gemini-9.1-flash-preview","supportedGenerationMethods":["generateContent"]},
          {"name":"models/gemini-9.0-pro","supportedGenerationMethods":["generateContent"]},
          {"name":"models/gemini-embedding-001","supportedGenerationMethods":["embedContent"]},
          {"name":"models/gemini-9.0-flash-tts","supportedGenerationMethods":["generateContent"]},
          {"name":"models/gemini-8.0-flash-batch","supportedGenerationMethods":["batchGenerateContent"]}
        ]}
        """;

    [Fact]
    public void PilihModelFlash_HanyaModelTeksFlash_StabilDanTerbaruDulu()
    {
        using var json = System.Text.Json.JsonDocument.Parse(DaftarModelGoogle);
        List<string> hasil = AsistenGemini.PilihModelFlash(json.RootElement);

        Assert.Equal(new[] { "gemini-9.0-flash", "gemini-2.5-flash", "gemini-9.1-flash-preview" }, hasil);
    }

    [Fact]
    public void PilihModelFlash_ResponsAneh_TidakError()
    {
        using var json = System.Text.Json.JsonDocument.Parse("""{"error":{"code":403}}""");
        Assert.Empty(AsistenGemini.PilihModelFlash(json.RootElement));
    }

    // Semua model di daftar tetap ditutup (404) seperti gemini-2.5-flash di uji Dharu:
    // aplikasi menanyakan daftar model ke Google lalu memakai model flash yang tersedia.
    [Fact]
    public async Task SemuaModelTetapDitutup_MemakaiModelDariDaftarGoogle()
    {
        var handler = new HandlerPalsu(m => m switch
        {
            "models" => Respons(HttpStatusCode.OK, DaftarModelGoogle),
            "gemini-9.0-flash" => Respons(HttpStatusCode.OK, Sukses),
            _ => Respons(HttpStatusCode.NotFound),
        });
        AsistenGemini asisten = Buat(handler);

        var (berhasil, pesan) = await asisten.UjiKoneksiAsync();

        Assert.True(berhasil);
        Assert.Contains("gemini-9.0-flash", asisten.Nama);
        Assert.Contains("gemini-9.0-flash", pesan);
        Assert.Equal(AsistenGemini.DaftarModel.Concat(new[] { "models", "gemini-9.0-flash" }), handler.ModelDipanggil);
    }

    [Fact]
    public async Task DaftarModelGoogleGagalDibaca_TetapMelaporkanAlasanTiapModel()
    {
        var handler = new HandlerPalsu(m => m == "models" ? Respons(HttpStatusCode.Forbidden) : Respons(HttpStatusCode.NotFound));
        var (berhasil, pesan) = await Buat(handler).UjiKoneksiAsync();

        Assert.False(berhasil);
        foreach (string m in AsistenGemini.DaftarModel)
            Assert.Contains(m + " tidak tersedia untuk key ini (404)", pesan);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task KeyDitolak_TidakMencobaModelLain(HttpStatusCode gagal)
    {
        var handler = new HandlerPalsu(_ => Respons(gagal));
        var (berhasil, pesan) = await Buat(handler).UjiKoneksiAsync();

        Assert.False(berhasil);
        Assert.Single(handler.ModelDipanggil);
        Assert.Contains("API key ditolak", pesan);
    }

    [Fact]
    public async Task ModelUtamaTidakMembalas_DibatalkanLaluBeralihKeModelCadangan()
    {
        var handler = new HandlerPalsu((m, ct) => m == M1 ? TidakMembalas(ct) : Task.FromResult(Respons(HttpStatusCode.OK, Sukses)));
        AsistenGemini asisten = new("kunci-palsu", new HttpClient(handler),
                                    batasPerModel: TimeSpan.FromMilliseconds(200), batasTotal: TimeSpan.FromSeconds(10));

        var (berhasil, _) = await asisten.UjiKoneksiAsync();

        Assert.True(berhasil);
        Assert.Equal(new[] { M1, M2 }, handler.ModelDipanggil);
    }

    // Batas total menjaga pengguna tidak menunggu per model x jumlah model.
    [Fact]
    public async Task SemuaModelTidakMembalas_BerhentiSesuaiBatasTotal()
    {
        var handler = new HandlerPalsu((_, ct) => TidakMembalas(ct));
        AsistenGemini asisten = new("kunci-palsu", new HttpClient(handler),
                                    batasPerModel: TimeSpan.FromSeconds(2.5), batasTotal: TimeSpan.FromSeconds(4));

        var jam = System.Diagnostics.Stopwatch.StartNew();
        var (berhasil, pesan) = await asisten.UjiKoneksiAsync();
        jam.Stop();

        Assert.False(berhasil);
        Assert.Single(handler.ModelDipanggil);   // sisa 1,5 detik < 3 detik minimum: model lain dilewati
        Assert.Contains("tidak membalas", pesan);
        Assert.Contains("tidak sempat dicoba", pesan);
        Assert.InRange(jam.Elapsed.TotalSeconds, 2, 4);
    }

    [Fact]
    public async Task TanpaInternet_TidakMencobaModelLain()
    {
        var handler = new HandlerPalsu(_ => throw new HttpRequestException("tidak ada jaringan"));
        var (berhasil, pesan) = await Buat(handler).UjiKoneksiAsync();

        Assert.False(berhasil);
        Assert.Single(handler.ModelDipanggil);
        Assert.Contains("koneksi internet", pesan);
    }

    [Fact]
    public async Task ChatSaatGeminiGagal_JatuhKeModeDemoDenganCatatan()
    {
        var handler = new HandlerPalsu(_ => Respons(HttpStatusCode.ServiceUnavailable));

        string jawaban = await Buat(handler).JawabAsync(ProfilUji(), "Bagaimana suhu di sini?");

        Assert.Contains("mode demo", jawaban);
        Assert.Contains("sibuk", jawaban);
    }

    // Temuan Dharu: setelah jatuh ke mode demo, label "Disusun oleh" masih menyebut Gemini.
    [Fact]
    public async Task PenjelasanJatuhKeModeDemo_SumberMenyebutModeDemo()
    {
        bool gagal = true;
        var handler = new HandlerPalsu(_ => gagal ? Respons(HttpStatusCode.ServiceUnavailable) : Respons(HttpStatusCode.OK, Sukses));
        AsistenGemini asisten = Buat(handler);

        await asisten.JelaskanAsync(ProfilUji(), Indikator.SuhuRataRata);
        Assert.False(asisten.PenjelasanTerakhirDariAi);
        Assert.Contains("mode demo", asisten.SumberPenjelasanTerakhir);
        Assert.DoesNotContain("Google Gemini", asisten.SumberPenjelasanTerakhir);

        gagal = false;
        await asisten.JelaskanAsync(ProfilUji(), Indikator.SuhuRataRata);
        Assert.True(asisten.PenjelasanTerakhirDariAi);
        Assert.Contains("Google Gemini", asisten.SumberPenjelasanTerakhir);
    }

    [Fact]
    public void AsistenDemo_SumberPenjelasanSamaDenganNama()
    {
        AsistenDemo demo = new();
        Assert.Equal(demo.Nama, demo.SumberPenjelasanTerakhir);
        Assert.False(demo.PenjelasanTerakhirDariAi);
    }

    // Temuan Dharu: "naik 156 hari" (garis tren) membingungkan karena kartu menunjukkan 152 hari.
    [Fact]
    public async Task JawabanDemoHariPanas_MenyebutGarisTrenDanNilaiTahunTerakhir()
    {
        string jawaban = await new AsistenDemo().JawabAsync(ProfilUji(), "apakah makin panas?");
        Assert.Contains("Menurut garis tren", jawaban);
        Assert.Contains("pada 2025 tercatat", jawaban);
    }
}
