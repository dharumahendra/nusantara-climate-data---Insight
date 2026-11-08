using System.Globalization;

namespace Junpro_Project;

// Palet warna dari desain praktik (Form1), dipusatkan di sini supaya semua
// form memakai warna yang sama.
public static class Tema
{
    public static readonly Color Navy = Color.FromArgb(20, 45, 78);
    public static readonly Color NavAktif = Color.FromArgb(35, 70, 115);
    public static readonly Color Teal = Color.FromArgb(15, 157, 138);
    public static readonly Color TealHover = Color.FromArgb(12, 135, 120);
    public static readonly Color TealMuda = Color.FromArgb(226, 244, 241);
    public static readonly Color Latar = Color.FromArgb(244, 247, 250);
    public static readonly Color Putih = Color.White;
    public static readonly Color Teks = Color.FromArgb(45, 55, 72);
    public static readonly Color TeksRedup = Color.FromArgb(100, 112, 130);
    public static readonly Color Garis = Color.FromArgb(220, 226, 234);
    public static readonly Color Oranye = Color.FromArgb(214, 122, 32);
    public static readonly Color KuningMuda = Color.FromArgb(255, 247, 230);
    public static readonly Color GelembungAi = Color.FromArgb(232, 239, 247);

    // Versi gelap untuk teks status, agar kontrasnya cukup di latar terang.
    public static readonly Color OranyeTua = Color.FromArgb(166, 84, 12);
    public static readonly Color HijauTua = Color.FromArgb(25, 118, 80);

    public static Font Huruf(float ukuran, FontStyle gaya = FontStyle.Regular) =>
        new Font("Segoe UI", ukuran, gaya);
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
