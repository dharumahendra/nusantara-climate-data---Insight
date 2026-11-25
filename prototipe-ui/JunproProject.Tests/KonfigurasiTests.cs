using Xunit;

namespace Junpro_Project.Tests;

// Konfigurasi lokal, pilihan wilayah terakhir, dan pencatatan error (PRD §11.5, F-01, §15).
public class KonfigurasiTests : IDisposable
{
    private readonly string folder = Path.Combine(Path.GetTempPath(), "nadi-uji-" + Guid.NewGuid().ToString("N"));

    public KonfigurasiTests()
    {
        Directory.CreateDirectory(folder);
    }

    public void Dispose() => Directory.Delete(folder, recursive: true);

    private string Tulis(string isi)
    {
        string path = Path.Combine(folder, "appsettings.local.json");
        File.WriteAllText(path, isi);
        return path;
    }

    // ---------- KonfigurasiLokal ----------

    [Fact]
    public void BacaDari_MembacaConnectionStringDanApiKey()
    {
        string path = Tulis("""
            { "ConnectionStrings": { "Nadi": "Host=localhost;Database=nadi" },
              "Gemini": { "ApiKey": "kunci-rahasia" } }
            """);

        Assert.Equal("Host=localhost;Database=nadi", KonfigurasiLokal.BacaDari(path, "ConnectionStrings", "Nadi"));
        Assert.Equal("kunci-rahasia", KonfigurasiLokal.BacaDari(path, "Gemini", "ApiKey"));
    }

    [Fact]
    public void BacaDari_FileTidakAda_Null()
    {
        Assert.Null(KonfigurasiLokal.BacaDari(Path.Combine(folder, "tidak-ada.json"), "Gemini", "ApiKey"));
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{ "Gemini": {} }""")]
    [InlineData("""{ "Gemini": "bukan objek" }""")]
    [InlineData("""{ "Gemini": { "ApiKey": 123 } }""")]
    public void BacaDari_KunciTidakAdaAtauBentukSalah_Null(string isi)
    {
        Assert.Null(KonfigurasiLokal.BacaDari(Tulis(isi), "Gemini", "ApiKey"));
    }

    [Fact]
    public void BacaDari_BukanJson_MelemparJsonException()
    {
        Assert.ThrowsAny<System.Text.Json.JsonException>(
            () => KonfigurasiLokal.BacaDari(Tulis("ini bukan json"), "Gemini", "ApiKey"));
    }

    // ---------- PilihanTerakhir ----------

    [Fact]
    public void PilihanTerakhir_SimpanLaluBaca_MengembalikanWilayahYangSama()
    {
        string lama = PilihanTerakhir.Berkas;
        PilihanTerakhir.Berkas = Path.Combine(folder, "sub", "pilihan.json");   // folder induk belum ada
        try
        {
            Wilayah sleman = DataIklim.Cari("DI Yogyakarta", "Sleman")!;

            PilihanTerakhir.Simpan(sleman);

            Assert.Equal(sleman, PilihanTerakhir.Baca());
        }
        finally { PilihanTerakhir.Berkas = lama; }
    }

    [Fact]
    public void PilihanTerakhir_BelumPernahDisimpan_Null()
    {
        string lama = PilihanTerakhir.Berkas;
        PilihanTerakhir.Berkas = Path.Combine(folder, "belum-ada.json");
        try { Assert.Null(PilihanTerakhir.Baca()); }
        finally { PilihanTerakhir.Berkas = lama; }
    }

    [Theory]
    [InlineData("rusak {{{")]
    [InlineData("""{"Provinsi":"Negeri Antah Berantah","Kota":"Tidak Ada"}""")]   // wilayah sudah tidak ada di daftar
    [InlineData("null")]
    public void PilihanTerakhir_BerkasRusakAtauWilayahHilang_NullTanpaError(string isi)
    {
        string lama = PilihanTerakhir.Berkas;
        PilihanTerakhir.Berkas = Path.Combine(folder, "pilihan.json");
        File.WriteAllText(PilihanTerakhir.Berkas, isi);
        try { Assert.Null(PilihanTerakhir.Baca()); }
        finally { PilihanTerakhir.Berkas = lama; }
    }

    // ---------- CatatanGalat ----------

    [Fact]
    public void CatatanGalat_MenulisDetailKeFileLogHarian()
    {
        string lama = CatatanGalat.Folder;
        CatatanGalat.Folder = Path.Combine(folder, "logs");
        try
        {
            string? path = CatatanGalat.Catat(new InvalidOperationException("uji pencatatan"));

            Assert.NotNull(path);
            Assert.StartsWith(CatatanGalat.Folder, path);
            Assert.Contains("uji pencatatan", File.ReadAllText(path));
        }
        finally { CatatanGalat.Folder = lama; }
    }

    [Fact]
    public void CatatanGalat_FolderTidakBisaDitulis_MengembalikanNullTanpaMelempar()
    {
        // Folder log ditunjuk ke sebuah FILE sehingga CreateDirectory pasti gagal.
        string berkas = Path.Combine(folder, "bukan-folder");
        File.WriteAllText(berkas, "x");
        string lama = CatatanGalat.Folder;
        CatatanGalat.Folder = berkas;
        try { Assert.Null(CatatanGalat.Catat(new Exception("x"))); }
        finally { CatatanGalat.Folder = lama; }
    }
}
