using System.Drawing;
using Xunit;

namespace Junpro_Project.Tests;

// Aksesibilitas (PRD §12 dan §13): teks wajib punya kontras >= 4,5:1 terhadap latarnya,
// dan elemen grafis (garis grafik) >= 3:1 menurut WCAG 2.1 AA.
public class KontrasWarnaTests
{
    private static double Luminans(Color c)
    {
        static double Kanal(int nilai)
        {
            double v = nilai / 255.0;
            return v <= 0.03928 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);
        }
        return 0.2126 * Kanal(c.R) + 0.7152 * Kanal(c.G) + 0.0722 * Kanal(c.B);
    }

    private static double Rasio(Color a, Color b)
    {
        double la = Luminans(a), lb = Luminans(b);
        return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
    }

    [Fact]
    public void Rasio_PutihTerhadapHitam_Adalah21()
    {
        Assert.Equal(21.0, Rasio(Color.White, Color.Black), 6);
    }

    public static IEnumerable<object[]> PasanganTeks() => new[]
    {
        new object[] { "teks utama di latar halaman", Tema.Teks, Tema.Latar },
        new object[] { "judul di latar halaman", Tema.Navy, Tema.Latar },
        new object[] { "teks redup di latar halaman", Tema.TeksRedup, Tema.Latar },
        new object[] { "teks redup di kartu putih", Tema.TeksRedup, Tema.Putih },
        new object[] { "teks tombol utama", Tema.Putih, Tema.TealTeks },
        new object[] { "teks tombol utama saat hover", Tema.Putih, Tema.TealTeksHover },
        new object[] { "teks tombol sekunder", Tema.TealTeks, Tema.Putih },
        new object[] { "teks tombol sekunder saat hover", Tema.TealTeks, Tema.TealMuda },
        new object[] { "teks sidebar", Tema.Putih, Tema.Navy },
        new object[] { "teks menu aktif", Tema.Putih, Tema.NavAktif },
        new object[] { "teks banner peringatan", Tema.Teks, Tema.KuningMuda },
        new object[] { "status peringatan", Tema.OranyeTua, Tema.Latar },
        new object[] { "status sukses", Tema.HijauTua, Tema.Latar },
        new object[] { "gelembung chat pengguna", Tema.Putih, Tema.TealTeks },
        new object[] { "gelembung chat AI", Tema.Teks, Tema.GelembungAi },
    };

    [Theory]
    [MemberData(nameof(PasanganTeks))]
    public void Teks_MemenuhiKontras45(string nama, Color teks, Color latar)
    {
        double rasio = Rasio(teks, latar);

        Assert.True(rasio >= 4.5, $"{nama}: kontras {rasio:0.00}:1 di bawah 4,5:1");
    }

    [Theory]
    [InlineData("garis grafik utama")]
    [InlineData("garis tren")]
    public void GarisGrafik_MemenuhiKontrasElemenGrafis3(string nama)
    {
        Color warna = nama == "garis tren" ? Tema.Oranye : Tema.Teal;

        Assert.True(Rasio(warna, Tema.Putih) >= 3.0, $"{nama}: kontras terhadap latar grafik kurang dari 3:1");
    }
}
