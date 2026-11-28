using System.Drawing.Drawing2D;

namespace Junpro_Project;

// Baseline (nilai normal 1991-2020) opsional; bila ada, tooltip menampilkan anomalinya.
public sealed record SeriGrafik(string Nama, double[] Nilai, Color Warna, bool PutusPutus = false,
                                double? Baseline = null);

// Grafik garis deret tahunan (PRD F-02/F-03). Garis tren bisa ditumpangkan,
// dan nilai tiap tahun muncul saat kursor diarahkan ke grafik.
public sealed class GrafikGarisPanel : Panel
{
    private const int Kiri = 60, Kanan = 24, Atas = 44, Bawah = 34;
    private const int JumlahGarisBantu = 4;

    private readonly int[] tahun;
    private readonly SeriGrafik[] seri;
    private readonly Func<double, string> formatNilai;
    private readonly Func<double, string> formatAnomali;
    private readonly string polaSumbu;
    private readonly bool tampilkanTren;
    private int indeksHover = -1;

    public GrafikGarisPanel(int[] tahun, SeriGrafik[] seri, Func<double, string> formatNilai,
                            Func<double, string> formatAnomali, string polaSumbu, bool tampilkanTren)
    {
        this.tahun = tahun;
        this.seri = seri;
        this.formatNilai = formatNilai;
        this.formatAnomali = formatAnomali;
        this.polaSumbu = polaSumbu;
        this.tampilkanTren = tampilkanTren;
        BackColor = Color.White;
        DoubleBuffered = true;
        ResizeRedraw = true;
    }

    private Rectangle Area => new Rectangle(Kiri, Atas, Width - Kiri - Kanan, Height - Atas - Bawah);

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        Rectangle area = Area;
        int n = tahun.Length;

        HasilTren[] tren = seri.Select(s => DataIklim.HitungTren(tahun, s.Nilai, 0)).ToArray();
        (double min, double maks) = RentangNilai(tren);

        float X(int i) => area.Left + (n == 1 ? 0 : i * (float)area.Width / (n - 1));
        float Y(double v) => (float)(area.Bottom - (v - min) / (maks - min) * area.Height);

        using Pen garisBantu = new Pen(Color.FromArgb(236, 240, 245));
        using Font fKecil = Tema.Huruf(8.5f);
        using Font fTebal = Tema.Huruf(8.5f, FontStyle.Bold);
        using SolidBrush brRedup = new SolidBrush(Tema.TeksRedup);
        using SolidBrush brTeks = new SolidBrush(Tema.Teks);
        using StringFormat rataKanan = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };
        using StringFormat tengah = new StringFormat { Alignment = StringAlignment.Center };


        // Garis bantu & label sumbu Y
        for (int k = 0; k <= JumlahGarisBantu; k++)
        {
            double v = min + (maks - min) * k / JumlahGarisBantu;
            float y = Y(v);
            g.DrawLine(garisBantu, area.Left, y, area.Right, y);
            g.DrawString(Format.Angka(v, polaSumbu), fKecil, brRedup,
                new RectangleF(0, y - 8, Kiri - 8, 16), rataKanan);
        }

        // Label sumbu X: setiap 5 tahun + tahun terakhir
        for (int i = 0; i < n; i++)
            if (tahun[i] % 5 == 0 || i == n - 1)
                g.DrawString(tahun[i].ToString(), fKecil, brRedup,
                    new RectangleF(X(i) - 20, area.Bottom + 8, 40, 16), tengah);

        if (tampilkanTren)
        {
            using Pen penTren = new Pen(Tema.Navy, 1.8f) { DashStyle = DashStyle.Dash };
            foreach (HasilTren t in tren)
                g.DrawLine(penTren, X(0), Y(t.NilaiPada(tahun[0])), X(n - 1), Y(t.NilaiPada(tahun[^1])));
        }

        if (seri.Length == 1 && seri[0].Nilai.Length > 1)
        {
            PointF[] garis = seri[0].Nilai.Select((v, i) => new PointF(X(i), Y(v))).ToArray();
            PointF[] luas = garis.Append(new PointF(garis[^1].X, area.Bottom))
                                 .Append(new PointF(garis[0].X, area.Bottom)).ToArray();
            using SolidBrush brLuas = new SolidBrush(Color.FromArgb(26, seri[0].Warna));
            g.FillPolygon(brLuas, luas);
        }

        foreach (SeriGrafik s in seri)
        {
            PointF[] titik = s.Nilai.Select((v, i) => new PointF(X(i), Y(v))).ToArray();
            using Pen pen = new Pen(s.Warna, 2.2f) { DashStyle = s.PutusPutus ? DashStyle.Dash : DashStyle.Solid };
            using SolidBrush br = new SolidBrush(s.Warna);
            if (titik.Length > 1) g.DrawLines(pen, titik);
            foreach (PointF t in titik) g.FillEllipse(br, t.X - 3, t.Y - 3, 6, 6);
        }

        // Penanda titik tertinggi & terendah (hanya untuk satu deret agar tidak ramai)
        if (seri.Length == 1)
        {
            double[] d = seri[0].Nilai;
            int iMaks = Array.IndexOf(d, d.Max());
            int iMin = Array.IndexOf(d, d.Min());
            using Pen cincin = new Pen(Tema.Navy, 1.5f);
            g.DrawEllipse(cincin, X(iMaks) - 6, Y(d[iMaks]) - 6, 12, 12);
            g.DrawEllipse(cincin, X(iMin) - 6, Y(d[iMin]) - 6, 12, 12);
            // Label dijaga tetap di dalam area grafik agar tidak menimpa angka sumbu.
            float yMaks = Y(d[iMaks]) - 24;
            if (yMaks < area.Top - 4) yMaks = Y(d[iMaks]) + 8;
            float yMin = Y(d[iMin]) + 8;
            if (yMin + 16 > area.Bottom) yMin = Y(d[iMin]) - 24;
            void Pil(string teks, int i, float y, Color latar, Color garisPil, Color warnaTeks)
            {
                Size ukuran = TextRenderer.MeasureText(teks, fKecil);
                float lebarPil = ukuran.Width + 10;
                float xPil = Math.Clamp(X(i) - lebarPil / 2, area.Left, area.Right - lebarPil);
                RectangleF kotakPil = new RectangleF(xPil, y - 1, lebarPil, 18);
                using GraphicsPath jalur = Tema.Bulat(kotakPil, 6);
                using (SolidBrush b = new SolidBrush(latar)) g.FillPath(b, jalur);
                using (Pen pn = new Pen(garisPil)) g.DrawPath(pn, jalur);
                TextRenderer.DrawText(g, teks, fKecil, Rectangle.Round(kotakPil), warnaTeks,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
            Pil($"Tertinggi · {tahun[iMaks]} · {formatNilai(d[iMaks])}", iMaks, yMaks,
                Tema.KuningMuda, Tema.KuningGaris, Tema.OranyeTua);
            Pil($"Terendah · {tahun[iMin]} · {formatNilai(d[iMin])}", iMin, yMin,
                Tema.Putih, Tema.Garis, Tema.Navy);
        }

        GambarLegenda(g, fKecil, brTeks);

        if (indeksHover >= 0)
            GambarTooltip(g, area, X(indeksHover), fKecil, fTebal, brTeks);
    }

    private (double Min, double Maks) RentangNilai(HasilTren[] tren)
    {
        List<double> semua = seri.SelectMany(s => s.Nilai).ToList();
        if (tampilkanTren)
            foreach (HasilTren t in tren)
            {
                semua.Add(t.NilaiPada(tahun[0]));
                semua.Add(t.NilaiPada(tahun[^1]));
            }

        double min = semua.Min(), maks = semua.Max();
        double jarak = maks - min;
        double pad = jarak == 0 ? 1 : jarak * 0.15;

        // Jumlah hari, curah hujan, dan suhu di wilayah tropis tidak pernah negatif:
        // padding tidak boleh membuat sumbu turun di bawah nol.
        double bawah = min >= 0 ? Math.Max(0, min - pad) : min - pad;
        return (bawah, maks + pad);
    }

    private void GambarLegenda(Graphics g, Font font, Brush brTeks)
    {
        float x = Kiri;
        const float y = 16;

        void Item(string nama, Color warna, bool putusPutus, float tebal)
        {
            using Pen pen = new Pen(warna, tebal) { DashStyle = putusPutus ? DashStyle.Dash : DashStyle.Solid };
            g.DrawLine(pen, x, y + 8, x + 24, y + 8);
            x += 30;
            g.DrawString(nama, font, brTeks, x, y);
            x += g.MeasureString(nama, font).Width + 18;
        }

        foreach (SeriGrafik s in seri) Item(s.Nama, s.Warna, s.PutusPutus, 2.2f);
        if (tampilkanTren) Item("Garis tren", Tema.Navy, true, 1.8f);
    }

    private void GambarTooltip(Graphics g, Rectangle area, float x, Font font, Font fTebal, Brush brTeks)
    {
        using (Pen penPemandu = new Pen(Tema.Garis))
            g.DrawLine(penPemandu, x, area.Top, x, area.Bottom);

        string judul = tahun[indeksHover].ToString();
        List<string> baris = new();
        foreach (SeriGrafik s in seri)
        {
            baris.Add((seri.Length > 1 ? s.Nama + ": " : "") + formatNilai(s.Nilai[indeksHover]));
            if (s.Baseline is double normal)
                baris.Add((seri.Length > 1 ? "   " : "") + "Anomali: " + formatAnomali(s.Nilai[indeksHover] - normal));
        }

        float lebar = Math.Max(g.MeasureString(judul, fTebal).Width,
                               baris.Max(b => g.MeasureString(b, font).Width)) + 16;
        float tinggi = 22 + baris.Count * 16;
        float kotakX = x + 10 + lebar > area.Right ? x - 10 - lebar : x + 10;
        RectangleF kotak = new RectangleF(kotakX, area.Top + 6, lebar, tinggi);

        using (GraphicsPath jalur = Tema.Bulat(kotak, 8))
        using (SolidBrush brLatar = new SolidBrush(Tema.Navy))
            g.FillPath(brLatar, jalur);

        using SolidBrush brPutih = new SolidBrush(Color.White);
        using SolidBrush brAnomali = new SolidBrush(Tema.Kuning);
        g.DrawString(judul, fTebal, brPutih, kotak.X + 8, kotak.Y + 4);
        for (int i = 0; i < baris.Count; i++)
            g.DrawString(baris[i], font, baris[i].TrimStart().StartsWith("Anomali") ? brAnomali : brPutih,
                kotak.X + 8, kotak.Y + 20 + i * 16);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        Rectangle area = Area;
        int baru = -1;

        if (tahun.Length > 1 && e.X >= area.Left - 10 && e.X <= area.Right + 10 &&
            e.Y >= area.Top && e.Y <= area.Bottom)
        {
            float langkah = (float)area.Width / (tahun.Length - 1);
            baru = Math.Clamp((int)Math.Round((e.X - area.Left) / langkah), 0, tahun.Length - 1);
        }

        if (baru != indeksHover)
        {
            indeksHover = baru;
            Invalidate();
        }
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        if (indeksHover != -1)
        {
            indeksHover = -1;
            Invalidate();
        }
    }
}
