using System.Drawing.Drawing2D;

namespace Junpro_Project;

// Grafik kolom titik (desain NADI), berasal dari grafik batang desain praktik. Format label nilai
// diterima dari luar supaya tidak terikat satu indikator saja, dan nilai
// negatif digambar ke bawah garis nol (tren nyata bisa turun).
public sealed class GrafikBatangPanel : Panel
{
    private readonly string[] label;
    private readonly double[] nilai;
    private readonly Color warnaTeks;
    private readonly Func<double, string> format;

    public GrafikBatangPanel(string[] label, double[] nilai, Color warnaBatang,
                             Color warnaTeks, Func<double, string> format)
    {
        this.label = label;
        this.nilai = nilai;
        this.warnaTeks = warnaTeks;
        this.format = format;
        BackColor = Color.White;
        DoubleBuffered = true;
        ResizeRedraw = true;
    }

    // Desain NADI: kolom titik-titik. Titik bawah teal, tengah kuning, atas oranye,
    // sehingga kenaikan terlihat "memanas". Nilai negatif digambar ke bawah garis nol.
    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.Clear(Tema.LatarInduk(this));
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using (GraphicsPath p = Tema.Bulat(new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f), 14))
        {
            g.FillPath(Brushes.White, p);
            using Pen border = new Pen(Tema.Garis);
            g.DrawPath(border, p);
        }

        const int jarak = 11, diameter = 8, titikPerBaris = 4;
        int padKiriKanan = 24, padAtas = 30, padBawah = 34;
        int areaH = Height - padAtas - padBawah;
        int n = nilai.Length;
        int slot = (Width - padKiriKanan * 2) / n;
        int barisMaks = Math.Max(1, (areaH - 20) / jarak);

        double atas = Math.Max(0, nilai.Max());
        double bawah = Math.Min(0, nilai.Min());
        double rentang = atas - bawah;
        if (rentang <= 0) rentang = 1;
        int yNol = padAtas + 20 + (int)Math.Round((areaH - 20) * (atas / rentang));

        Color teal = Color.FromArgb(130, 208, 212);
        using Font fTahun = Tema.Huruf(9);
        using Font fNilai = Tema.Huruf(8.5f, FontStyle.Bold);
        for (int i = 0; i < n; i++)
        {
            int baris = Math.Max(1, (int)Math.Round(barisMaks * Math.Abs(nilai[i]) / rentang));
            bool negatif = nilai[i] < 0;
            int x0 = padKiriKanan + i * slot + (slot - titikPerBaris * jarak) / 2;

            for (int r = 0; r < baris; r++)
            {
                double posisi = (double)r / barisMaks;
                Color warna = negatif || posisi < 0.4 ? teal : posisi < 0.7 ? Tema.Kuning : Tema.OranyeTerang;
                int y = negatif ? yNol + 4 + r * jarak : yNol - (r + 1) * jarak;
                using SolidBrush br = new SolidBrush(warna);
                for (int k = 0; k < titikPerBaris; k++)
                    g.FillEllipse(br, x0 + k * jarak, y, diameter, diameter);
            }

            int tinggiKolom = baris * jarak;
            int yLabel = negatif ? yNol + 6 + tinggiKolom : yNol - tinggiKolom - 20;
            bool terakhir = i == n - 1;
            Rectangle kotakLabel = new Rectangle(x0 - 20, yLabel, titikPerBaris * jarak + 40, 18);
            TextRenderer.DrawText(g, format(nilai[i]), fNilai, kotakLabel, terakhir ? Tema.OranyeTua : warnaTeks,
                TextFormatFlags.HorizontalCenter);
            TextRenderer.DrawText(g, label[i], fTahun,
                new Rectangle(x0 - 20, Height - padBawah + 8, titikPerBaris * jarak + 40, 18), warnaTeks,
                TextFormatFlags.HorizontalCenter);
        }
    }
}
