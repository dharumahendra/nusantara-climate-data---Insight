using System.Drawing.Drawing2D;

namespace Junpro_Project;

public enum JenisTombol { Utama, Sekunder, Bahaya, Gelap, Chip, Hapus }

// Tombol bersudut bulat sesuai desain NADI. Enabled = false menampilkan gaya Nonaktif.
public class TombolNadi : Button
{
    private bool diAtas, ditekan;

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public JenisTombol Jenis { get; set; }

    // Glyph Segoe MDL2 di kiri teks (opsional), seperti tombol pada desain.
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public string? Ikon { get; set; }

    // Tombol pilihan yang tidak aktif (mis. tab indikator) tampil tanpa bingkai.
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public bool TanpaBingkai { get; set; }

    public override Size GetPreferredSize(Size proposedSize)
    {
        if (Ikon == null) return base.GetPreferredSize(proposedSize);
        // Dihitung langsung dari teks: base (AutoSize GrowOnly) ikut memakai ukuran sekarang,
        // sehingga menambah lebar ikon di atasnya membuat tombol melebar setiap kali layout.
        Size teks = TextRenderer.MeasureText(Text, Font);
        return new Size(Math.Max(MinimumSize.Width, teks.Width + Padding.Horizontal + 34),
                        Math.Max(MinimumSize.Height, teks.Height + 16));
    }

    public TombolNadi(JenisTombol jenis = JenisTombol.Utama)
    {
        Jenis = jenis;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        UseVisualStyleBackColor = false;
        Cursor = Cursors.Hand;
        Font = Tema.Huruf(10, FontStyle.Bold);
        Padding = new Padding(12, 0, 12, 0);
    }

    protected override void OnMouseEnter(EventArgs e) { diAtas = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { diAtas = ditekan = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { ditekan = true; Invalidate(); base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { ditekan = false; Invalidate(); base.OnMouseUp(e); }
    protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }

    private (Color Latar, Color Garis, Color Teks) Warna()
    {
        if (!Enabled) return (Tema.Nonaktif, Tema.Nonaktif, Tema.TeksRedup);
        return Jenis switch
        {
            JenisTombol.Sekunder => (diAtas ? Tema.TealMuda : Tema.Putih,
                TanpaBingkai ? (diAtas ? Tema.TealMuda : Tema.Putih) : Tema.Garis, Tema.Navy),
            JenisTombol.Chip => (diAtas ? Tema.Aksen : Tema.TealMuda, diAtas ? Tema.Aksen : Tema.TealMuda, Tema.TealTeks),
            JenisTombol.Hapus => (diAtas ? Color.FromArgb(150, 28, 18) : Tema.Merah, Tema.Merah, Tema.Putih),
            JenisTombol.Gelap => (diAtas ? Color.FromArgb(44, 64, 68) : Tema.Navy, Tema.Navy, Tema.Putih),
            JenisTombol.Bahaya => (diAtas ? Color.FromArgb(255, 226, 222) : Tema.MerahMuda, Tema.MerahGaris, Tema.Merah),
            _ => (ditekan ? Tema.AksenTekan : diAtas ? Tema.AksenHover : Tema.Aksen,
                  ditekan ? Tema.AksenTekan : diAtas ? Tema.AksenHover : Tema.Aksen, Tema.Navy),
        };
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.Clear(Tema.LatarInduk(this));
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var (latar, garis, teks) = Warna();

        using (GraphicsPath p = Tema.Bulat(new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f), 9))
        {
            using (SolidBrush b = new(latar)) g.FillPath(b, p);
            using (Pen pen = new(garis)) g.DrawPath(pen, p);
        }
        if (Focused && ShowFocusCues)
        {
            using GraphicsPath f = Tema.Bulat(new RectangleF(2.5f, 2.5f, Width - 6, Height - 6), 7);
            using Pen pen = new(Tema.TealTeks, 2f);
            g.DrawPath(pen, f);
        }
        if (Ikon == null)
        {
            TextRenderer.DrawText(g, Text, Font, ClientRectangle, teks,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            return;
        }
        using Font fi = Tema.Ikon(Font.Size * 0.95f);
        int lebarTeks = TextRenderer.MeasureText(Text, Font).Width;
        int x = Math.Max(6, (Width - lebarTeks - 24) / 2);
        TextRenderer.DrawText(g, Ikon, fi, new Rectangle(x, 0, 20, Height), teks,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        TextRenderer.DrawText(g, Text, Font, new Rectangle(x + 24, 0, Width - x - 26, Height), teks,
            TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }
}

// Item menu sidebar: ikon + teks, blok teal muda saat aktif.
public sealed class ItemNav : Control
{
    // Ikon kompas (desain menu Dashboard Explore), digambar sendiri.
    public const string Kompas = "kompas";

    private readonly string ikon;
    private readonly bool aktif;
    private bool diAtas;

    public ItemNav(string teks, string ikon, bool aktif)
    {
        Text = teks;
        this.ikon = ikon;
        this.aktif = aktif;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
        TabStop = true;
        Size = new Size(202, 42);
        Margin = new Padding(9, 2, 9, 2);
        Cursor = Cursors.Hand;
        Font = Tema.Huruf(9.5f, aktif ? FontStyle.Bold : FontStyle.Regular);
        AccessibleRole = AccessibleRole.PushButton;
        AccessibleName = teks;
    }

    protected override void OnMouseEnter(EventArgs e) { diAtas = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { diAtas = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
    protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode is Keys.Enter or Keys.Space) OnClick(EventArgs.Empty);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.Clear(Tema.LatarInduk(this));
        g.SmoothingMode = SmoothingMode.AntiAlias;
        if (aktif || diAtas)
        {
            using GraphicsPath p = Tema.Bulat(new RectangleF(0, 0, Width - 1, Height - 1), 10);
            using SolidBrush b = new(aktif ? Tema.NavAktif : Tema.TealMuda);
            g.FillPath(b, p);
        }
        if (Focused && ShowFocusCues)
        {
            using GraphicsPath p = Tema.Bulat(new RectangleF(1, 1, Width - 3, Height - 3), 9);
            using Pen pen = new(Tema.TealTeks, 2f);
            g.DrawPath(pen, p);
        }
        Color warna = aktif ? Tema.Navy : Tema.Teks;
        if (ikon == Kompas)
        {
            float cx = 22, cy = Height / 2f;
            using Pen pen = new(warna, 1.4f);
            g.DrawEllipse(pen, cx - 8, cy - 8, 16, 16);
            using SolidBrush b = new(warna);
            g.FillPolygon(b, new PointF[] { new(cx + 4.5f, cy - 4.5f), new(cx + 1.2f, cy + 1.2f), new(cx - 1.2f, cy - 1.2f) });
            g.DrawPolygon(pen, new PointF[] { new(cx - 4.5f, cy + 4.5f), new(cx - 1.2f, cy - 1.2f), new(cx + 1.2f, cy + 1.2f) });
        }
        else
        {
            using Font f = Tema.Ikon(13);
            TextRenderer.DrawText(g, ikon, f, new Rectangle(8, 0, 28, Height), warna,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
        TextRenderer.DrawText(g, Text, Font, new Rectangle(40, 0, Width - 42, Height), warna,
            TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
    }
}

// Latar halaman dengan lingkaran lembut di kanan atas, seperti desain. Ikut bergulir
// bersama isi agar tidak "tercoreng" saat halaman di-scroll.
public sealed class PanelLatar : Panel
{
    public PanelLatar()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
    }

    protected override void OnScroll(ScrollEventArgs se)
    {
        base.OnScroll(se);
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        int dy = AutoScrollPosition.Y;
        float cx = ClientSize.Width - 60, cy = 40 + dy;
        using (SolidBrush isi = new SolidBrush(Color.FromArgb(232, 241, 242)))
            g.FillEllipse(isi, cx - 230, cy - 230, 460, 460);
        using Pen cincin = new Pen(Color.FromArgb(222, 234, 235), 1.2f);
        foreach (int r in new[] { 300, 380, 470 })
            g.DrawEllipse(cincin, cx - r, cy - r, r * 2, r * 2);
        using SolidBrush titik = new SolidBrush(Color.FromArgb(214, 226, 228));
        float bx = ClientSize.Width - 130, by = Math.Max(ClientSize.Height, DisplayRectangle.Height) - 120 + dy;
        for (int a = 0; a < 6; a++)
            for (int b = 0; b < 5; b++)
                if (a + b >= 4) g.FillEllipse(titik, bx + b * 22, by + a * 18, 3, 3);
    }
}

// Bilah progres tak tentu: segmen teal bergerak bolak-balik selama data dimuat.
public sealed class BilahMemuat : Control
{
    private readonly System.Windows.Forms.Timer pewaktu = new() { Interval = 30 };
    private float posisi;
    private float arah = 0.012f;

    public BilahMemuat()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        pewaktu.Tick += (s, e) =>
        {
            posisi += arah;
            if (posisi > 0.65f || posisi < 0) arah = -arah;
            Invalidate();
        };
        pewaktu.Start();
        AccessibleRole = AccessibleRole.ProgressBar;
        AccessibleName = "Memuat";
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) pewaktu.Dispose();
        base.Dispose(disposing);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.Clear(Tema.LatarInduk(this));
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using (GraphicsPath p = Tema.Bulat(new RectangleF(0, 0, Width - 1, Height - 1), Height / 2f))
        using (SolidBrush b = new(Tema.Garis))
            g.FillPath(b, p);
        using (GraphicsPath p = Tema.Bulat(new RectangleF(Width * posisi, 0, Width * 0.35f, Height - 1), Height / 2f))
        using (SolidBrush b = new(Tema.TealTeks))
            g.FillPath(b, p);
    }
}

// Penanda alur Explore > Understand > Act di kanan atas halaman.
public sealed class JejakTahap : Control
{
    private static readonly string[] tahap = { "Explore", "Understand", "Act" };
    private readonly int aktif;

    public JejakTahap(int aktif)
    {
        this.aktif = aktif;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        Font = Tema.Huruf(9.5f);
        Size = new Size(262, 34);
        AccessibleName = "Tahap: " + tahap[aktif];
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.Clear(Tema.LatarInduk(this));
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using (GraphicsPath p = Tema.Bulat(new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f), 10))
        {
            g.FillPath(Brushes.White, p);
            using Pen pen = new(Tema.Garis);
            g.DrawPath(pen, p);
        }

        int x = 6;
        for (int i = 0; i < tahap.Length; i++)
        {
            using Font f = Tema.Huruf(9.5f, i == aktif ? FontStyle.Bold : FontStyle.Regular);
            int w = TextRenderer.MeasureText(tahap[i], f).Width + 12;
            if (i == aktif)
            {
                using GraphicsPath p = Tema.Bulat(new RectangleF(x, 4, w, Height - 9), 8);
                using SolidBrush b = new(Tema.TealMuda);
                g.FillPath(b, p);
            }
            TextRenderer.DrawText(g, tahap[i], f, new Rectangle(x, 0, w, Height),
                i == aktif ? Tema.TealTeks : Tema.Teks, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            x += w;
            if (i < tahap.Length - 1)
            {
                TextRenderer.DrawText(g, "›", Font, new Rectangle(x, 0, 16, Height), Tema.TeksRedup,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                x += 16;
            }
        }
    }
}

// Kotak kecil berwarna berisi ikon (dipakai kartu angka dan logo).
public sealed class ChipIkon : Control
{
    // Logo NADI: garis denyut, digambar sendiri karena tidak ada di font ikon Windows.
    public const string Denyut = "denyut";
    // Avatar asisten AI: kilau bintang empat sudut.
    public const string Kilau = "kilau";

    private readonly string ikon;
    private readonly Color warnaIkon;

    public ChipIkon(string ikon, Color latar, Color warnaIkon, int ukuran = 26)
    {
        this.ikon = ikon;
        this.warnaIkon = warnaIkon;
        BackColor = latar;
        Size = new Size(ukuran, ukuran);
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.Clear(Tema.LatarInduk(this));
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using (GraphicsPath p = Tema.Bulat(new RectangleF(0, 0, Width - 1, Height - 1), Width / 4f))
        using (SolidBrush b = new(BackColor))
            g.FillPath(b, p);
        if (ikon == Kilau)
        {
            void Bintang(float x, float y, float r)
            {
                float k = r * 0.28f;
                PointF[] p = { new(x, y - r), new(x + k, y - k), new(x + r, y), new(x + k, y + k),
                               new(x, y + r), new(x - k, y + k), new(x - r, y), new(x - k, y - k) };
                using SolidBrush b = new(warnaIkon);
                g.FillPolygon(b, p);
            }
            Bintang(Width * 0.44f, Height * 0.56f, Width * 0.28f);
            Bintang(Width * 0.70f, Height * 0.30f, Width * 0.13f);
            return;
        }
        if (ikon == Denyut)
        {
            float w = Width, h = Height;
            PointF[] titik =
            {
                new(w * 0.20f, h * 0.52f), new(w * 0.36f, h * 0.52f), new(w * 0.45f, h * 0.30f),
                new(w * 0.56f, h * 0.72f), new(w * 0.65f, h * 0.44f), new(w * 0.80f, h * 0.44f),
            };
            using Pen pen = new(warnaIkon, Math.Max(1.6f, w * 0.06f)) { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawLines(pen, titik);
            return;
        }
        using Font f = Tema.Ikon(Width * 0.42f);
        TextRenderer.DrawText(g, ikon, f, ClientRectangle, warnaIkon,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }
}
