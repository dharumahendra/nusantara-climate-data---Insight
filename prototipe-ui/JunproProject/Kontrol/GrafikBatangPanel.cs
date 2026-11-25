using System.Drawing.Drawing2D;

namespace Junpro_Project;

// Grafik batang dari desain praktik (Form1.GrafikPanel). Format label nilai
// diterima dari luar supaya tidak terikat satu indikator saja, dan nilai
// negatif digambar ke bawah garis nol (tren nyata bisa turun).
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

        // Rentang selalu memuat nol agar batang positif dan negatif punya acuan yang sama.
        double atas = Math.Max(0, nilai.Max());
        double bawah = Math.Min(0, nilai.Min());
        double rentang = atas - bawah;
        if (rentang <= 0) rentang = 1;
        int yNol = padAtas + (int)Math.Round(areaH * (atas / rentang));

        using (Font fTahun = new Font("Segoe UI", 9, FontStyle.Bold))
        using (Font fNilai = new Font("Segoe UI", 8.5f))
        using (SolidBrush brBatang = new SolidBrush(warnaBatang))
        using (SolidBrush brTeks = new SolidBrush(warnaTeks))
        using (Pen penNol = new Pen(Tema.Garis))
        using (StringFormat tengah = new StringFormat { Alignment = StringAlignment.Center })
        {
            if (bawah < 0)
                g.DrawLine(penNol, padKiriKanan, yNol, padKiriKanan + areaW, yNol);

            for (int i = 0; i < n; i++)
            {
                int tinggi = Math.Max(4, (int)(areaH * Math.Abs(nilai[i]) / rentang));
                int x = padKiriKanan + i * slot + (slot - lebarBatang) / 2;
                bool negatif = nilai[i] < 0;
                int y = negatif ? yNol : yNol - tinggi;

                g.FillRectangle(brBatang, x, y, lebarBatang, tinggi);

                float yLabel = negatif ? y + tinggi + 2 : y - 18;
                g.DrawString(format(nilai[i]), fNilai, brTeks,
                    new RectangleF(x - 10, yLabel, lebarBatang + 20, 16), tengah);

                g.DrawString(label[i], fTahun, brTeks,
                    new RectangleF(x - 10, Height - padBawah + 6, lebarBatang + 20, 18), tengah);
            }
        }
    }
}
