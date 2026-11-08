using System.Drawing.Drawing2D;

namespace Junpro_Project;

// Grafik batang dari desain praktik (Form1.GrafikPanel). Sekarang format label
// nilainya diterima dari luar supaya tidak terikat satu indikator saja.
public sealed class GrafikBatangPanel : Panel
{
    private readonly string[] label;
    private readonly double[] nilai;
    private readonly Color warnaBatang;
    private readonly Color warnaTeks;
    private readonly Func<double, string> format;

    public GrafikBatangPanel(string[] label, double[] nilai, Color warnaBatang,
                             Color warnaTeks, Func<double, string> format)
    {
        this.label = label;
        this.nilai = nilai;
        this.warnaBatang = warnaBatang;
        this.warnaTeks = warnaTeks;
        this.format = format;
        BackColor = Color.White;
        DoubleBuffered = true;
        ResizeRedraw = true;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        using (Pen border = new Pen(Tema.Garis))
            g.DrawRectangle(border, 0, 0, Width - 1, Height - 1);

        int padKiriKanan = 24, padAtas = 28, padBawah = 30;
        int areaW = Width - padKiriKanan * 2;
        int areaH = Height - padAtas - padBawah;
        int n = nilai.Length;
        int slot = areaW / n;
        int lebarBatang = slot - 24;
        double maks = 0;
        foreach (double v in nilai) if (v > maks) maks = v;
        if (maks <= 0) maks = 1;

        using (Font fTahun = new Font("Segoe UI", 9, FontStyle.Bold))
        using (Font fNilai = new Font("Segoe UI", 8.5f))
        using (SolidBrush brBatang = new SolidBrush(warnaBatang))
        using (SolidBrush brTeks = new SolidBrush(warnaTeks))
        using (StringFormat tengah = new StringFormat { Alignment = StringAlignment.Center })
        {
            for (int i = 0; i < n; i++)
            {
                int tinggi = Math.Max(4, (int)(areaH * (nilai[i] / maks)));
                int x = padKiriKanan + i * slot + (slot - lebarBatang) / 2;
                int y = padAtas + (areaH - tinggi);

                g.FillRectangle(brBatang, x, y, lebarBatang, tinggi);

                g.DrawString(format(nilai[i]), fNilai, brTeks,
                    new RectangleF(x - 10, y - 18, lebarBatang + 20, 16), tengah);

                g.DrawString(label[i], fTahun, brTeks,
                    new RectangleF(x - 10, Height - padBawah + 6, lebarBatang + 20, 18), tengah);
            }
        }
    }
}
