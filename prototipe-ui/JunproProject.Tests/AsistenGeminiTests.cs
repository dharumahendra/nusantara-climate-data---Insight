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
        private readonly Func<string, HttpResponseMessage> balas;
        public List<string> ModelDipanggil { get; } = new();

        public HandlerPalsu(Func<string, HttpResponseMessage> balas) => this.balas = balas;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            string model = request.RequestUri!.AbsolutePath.Split('/').Last().Split(':')[0];
            ModelDipanggil.Add(model);
            return Task.FromResult(balas(model));
        }
    }

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
            [Indikator.HariPanas] = Garis(35, 0),
            [Indikator.HariHujan] = Garis(150, 0),
            [Indikator.HariHujanLebat] = Garis(3, 0),
        };
        return new ProfilIklim(new Wilayah("DI Yogyakarta", "Sleman", -7.7167, 110.3550), tahun, deret,
                               new Dictionary<Indikator, double>(), 32.0, Array.Empty<int>(), 30);
    }

    private static AsistenGemini Buat(HandlerPalsu handler) => new("kunci-palsu", new HttpClient(handler));

    [Fact]
    public async Task ModelUtamaBerhasil_ModelCadanganTidakDipanggil()
    {
        var handler = new HandlerPalsu(_ => Respons(HttpStatusCode.OK, Sukses));
        var (berhasil, _) = await Buat(handler).UjiKoneksiAsync();

        Assert.True(berhasil);
        Assert.Equal(new[] { "gemini-2.5-flash" }, handler.ModelDipanggil);
    }

    [Theory]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    public async Task ModelUtamaGagal_BeralihKeModelCadangan(HttpStatusCode gagal)
    {
        var handler = new HandlerPalsu(m => m == "gemini-2.5-flash"
            ? Respons(gagal) : Respons(HttpStatusCode.OK, Sukses));
        AsistenGemini asisten = Buat(handler);

        var (berhasil, _) = await asisten.UjiKoneksiAsync();

        Assert.True(berhasil);
        Assert.Equal(new[] { "gemini-2.5-flash", "gemini-flash-latest" }, handler.ModelDipanggil);
        Assert.Contains("gemini-flash-latest", asisten.Nama);
    }

    [Fact]
    public async Task SemuaModelSibuk_PesanMenyebutServerSibuk_BukanTerlaluLama()
    {
        var handler = new HandlerPalsu(_ => Respons(HttpStatusCode.ServiceUnavailable));
        var (berhasil, pesan) = await Buat(handler).UjiKoneksiAsync();

        Assert.False(berhasil);
        Assert.Contains("sibuk", pesan);
        Assert.DoesNotContain("terlalu lama", pesan);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task KeyDitolak_TidakMencobaModelLain(HttpStatusCode gagal)
    {
        var handler = new HandlerPalsu(_ => Respons(gagal));
        var (berhasil, _) = await Buat(handler).UjiKoneksiAsync();

        Assert.False(berhasil);
        Assert.Single(handler.ModelDipanggil);
    }

    [Fact]
    public async Task ModelUtamaTidakMembalas_BeralihKeModelCadangan()
    {
        var handler = new HandlerPalsu(m => m == "gemini-2.5-flash"
            ? throw new TaskCanceledException("batas waktu habis") : Respons(HttpStatusCode.OK, Sukses));
        var (berhasil, _) = await Buat(handler).UjiKoneksiAsync();

        Assert.True(berhasil);
        Assert.Equal(2, handler.ModelDipanggil.Count);
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
}
