using System.Globalization;

namespace Junpro_Project;

// Palet warna desain NADI (export.zip): latar abu kehijauan, sidebar putih,
// aksen teal muda dengan teks gelap. Nama lama dipertahankan agar semua form tetap jalan.
public static class Tema
{
    public static readonly Color Navy = Color.FromArgb(24, 39, 42);          // tinta judul
    public static readonly Color NavAktif = Color.FromArgb(153, 216, 219);   // menu aktif
    public static readonly Color Aksen = Color.FromArgb(153, 216, 219);      // tombol utama
    public static readonly Color AksenHover = Color.FromArgb(128, 203, 207);
    public static readonly Color AksenTekan = Color.FromArgb(110, 192, 197);

    // Teal untuk garis grafik (>= 3:1 terhadap putih) dan teks teal (>= 4,5:1).
    public static readonly Color Teal = Color.FromArgb(24, 150, 156);
    public static readonly Color TealTeks = Color.FromArgb(13, 110, 115);
    public static readonly Color TealTeksHover = Color.FromArgb(10, 92, 96);
    public static readonly Color TealMuda = Color.FromArgb(230, 246, 247);
    public static readonly Color Latar = Color.FromArgb(242, 245, 245);
    public static readonly Color Putih = Color.White;
    public static readonly Color Teks = Color.FromArgb(62, 82, 86);
    public static readonly Color TeksRedup = Color.FromArgb(86, 106, 110);
    public static readonly Color Garis = Color.FromArgb(220, 229, 230);
    public static readonly Color Oranye = Color.FromArgb(232, 93, 4);
    public static readonly Color OranyeTerang = Color.FromArgb(252, 107, 5);
    public static readonly Color Kuning = Color.FromArgb(255, 182, 43);
    public static readonly Color KuningMuda = Color.FromArgb(255, 246, 224);
    public static readonly Color KuningGaris = Color.FromArgb(255, 223, 158);
    public static readonly Color GelembungAi = Color.FromArgb(236, 244, 245);
    public static readonly Color Hijau = Color.FromArgb(92, 168, 40);
    public static readonly Color HijauGrafik = Color.FromArgb(70, 140, 30);   // >= 3:1 di putih
    public static readonly Color HijauMuda = Color.FromArgb(238, 247, 227);
    public static readonly Color Merah = Color.FromArgb(180, 35, 24);
    public static readonly Color MerahMuda = Color.FromArgb(255, 240, 238);
    public static readonly Color MerahGaris = Color.FromArgb(247, 190, 182);
    public static readonly Color Nonaktif = Color.FromArgb(206, 230, 232);

    // Versi gelap untuk teks status, agar kontrasnya cukup di latar terang.
    public static readonly Color OranyeTua = Color.FromArgb(166, 72, 0);
    public static readonly Color HijauTua = Color.FromArgb(63, 110, 20);

    // Ikon memakai font simbol bawaan Windows 10/11, tanpa berkas gambar tambahan.
    public static Font Ikon(float ukuran) => new Font("Segoe MDL2 Assets", ukuran);

    public static System.Drawing.Drawing2D.GraphicsPath Bulat(RectangleF r, float jari)
    {
        float d = Math.Min(jari * 2, Math.Min(r.Width, r.Height));
        var p = new System.Drawing.Drawing2D.GraphicsPath();
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }

    // Warna latar induk, untuk mengisi sudut di luar bentuk bulat.
    public static Color LatarInduk(Control c)
    {
        for (Control? p = c.Parent; p != null; p = p.Parent)
            if (p.BackColor.A == 255) return p.BackColor;
        return Latar;
    }

    // Plus Jakarta Sans ditanam di NADI.exe (desain export.zip, PRD §13). Didaftarkan ke GDI+
    // (PrivateFontCollection) dan ke GDI (AddFontMemResourceEx), karena label WinForms digambar
    // lewat GDI. Bila gagal dimuat, aplikasi kembali ke Segoe UI.
    private static readonly System.Drawing.Text.PrivateFontCollection koleksiFont = new();
    private static readonly FontFamily? keluargaNadi = MuatFont();

    public static bool FontNadiAktif => keluargaNadi != null;

    [System.Runtime.InteropServices.DllImport("gdi32.dll")]
    private static extern IntPtr AddFontMemResourceEx(IntPtr data, uint panjang, IntPtr cadangan, ref uint jumlah);

    private static FontFamily? MuatFont()
    {
        try
        {
            foreach (string nama in new[] { "font-regular", "font-bold" })
            {
                using Stream? aliran = typeof(Tema).Assembly.GetManifestResourceStream(nama);
                if (aliran == null) return null;
                byte[] data = new byte[aliran.Length];
                aliran.ReadExactly(data);
                // Memori sengaja tidak dibebaskan: font dipakai selama aplikasi berjalan.
                IntPtr ptr = System.Runtime.InteropServices.Marshal.AllocCoTaskMem(data.Length);
                System.Runtime.InteropServices.Marshal.Copy(data, 0, ptr, data.Length);
                koleksiFont.AddMemoryFont(ptr, data.Length);
                uint jumlah = 0;
                AddFontMemResourceEx(ptr, (uint)data.Length, IntPtr.Zero, ref jumlah);
            }
            return koleksiFont.Families.FirstOrDefault();
        }
        catch (Exception)
        {
            return null;
        }
    }

    public static Font Huruf(float ukuran, FontStyle gaya = FontStyle.Regular) =>
        keluargaNadi != null ? new Font(keluargaNadi, ukuran, gaya) : new Font("Segoe UI", ukuran, gaya);

    // Ikon NADI ikut tertanam di NADI.exe (ApplicationIcon). Dibaca dari berkas exe itu
    // agar jendela memakai ikon yang sama dengan taskbar; null bila gagal dibaca
    // sehingga jendela memakai ikon bawaan dan aplikasi tetap berjalan.
    public static readonly Icon? IkonAplikasi = MuatIkonAplikasi();

    private static Icon? MuatIkonAplikasi()
    {
        try
        {
            string? exe = Environment.ProcessPath;
            return exe == null ? null : Icon.ExtractAssociatedIcon(exe);
        }
        catch (Exception)
        {
            return null;
        }
    }
}

// Aturan penyajian angka (PRD §6.5): pemisah desimal koma, ribuan titik.
public static class Format
{
    private static readonly CultureInfo Id = CultureInfo.GetCultureInfo("id-ID");

    public static string NamaIndikator(Indikator i) => i switch
    {
        Indikator.SuhuRataRata => "Suhu rata-rata",
        Indikator.SuhuMaksimum => "Suhu maksimum",
        Indikator.SuhuMinimum => "Suhu minimum",
        Indikator.CurahHujan => "Curah hujan",
        Indikator.HariPanas => "Hari panas",
        Indikator.HariHujan => "Hari hujan",
        _ => "Hari hujan lebat",
    };

    public static bool AdalahSuhu(Indikator i) =>
        i is Indikator.SuhuRataRata or Indikator.SuhuMaksimum or Indikator.SuhuMinimum;

    public static string Nilai(Indikator i, double v)
    {
        if (AdalahSuhu(i)) return v.ToString("0.0", Id) + " °C";
        if (i == Indikator.CurahHujan) return v.ToString("N0", Id) + " mm";
        return v.ToString("0", Id) + " hari";
    }

    public static string Perubahan(Indikator i, double v) =>
        (v < 0 ? "−" : "+") + Nilai(i, Math.Abs(v));

    public static string PolaSumbu(Indikator i) =>
        AdalahSuhu(i) ? "0.0" : i == Indikator.CurahHujan ? "N0" : "0";

    public static string Angka(double v, string pola) => v.ToString(pola, Id);
}
