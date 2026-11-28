using System.Drawing.Drawing2D;

namespace Junpro_Project;

// Dialog berbahasa Indonesia dengan gaya NADI (desain export.zip): kartu putih bersudut bulat
// di atas jendela yang diredupkan. MessageBox memakai teks tombol dari bahasa Windows
// (Yes/No di Windows berbahasa Inggris), sehingga dialog ini dibuat sendiri.
//
// Aman untuk aksi menghapus: tombol bawaan (Enter), Esc, dan menutup jendela
// semuanya berarti "Tidak". Hanya klik "Ya, hapus" yang mengembalikan true.
public static class DialogNadi
{
    public const string TeksYa = "Ya, hapus";
    public const string TeksTidak = "Tidak";
    public const string TeksTutup = "Tutup";

    private static readonly string IkonHapus = ((char)0xE74D).ToString();
    private static readonly string IkonInfo = ((char)0xE946).ToString();

    public static bool Konfirmasi(IWin32Window? pemilik, string pesan, string judul, string? rincian = null)
    {
        using Form dialog = BuatKonfirmasi(pesan, judul, rincian);
        return Tampilkan(dialog, pemilik) == DialogResult.Yes;
    }

    public static void Info(IWin32Window? pemilik, string pesan, string judul)
    {
        using Form dialog = BuatDialog(pesan, null, judul, IkonInfo, Tema.TealMuda, Tema.TealTeks,
            (TeksTutup, DialogResult.OK, true, JenisTombol.Utama));
        Tampilkan(dialog, pemilik);
    }

    // Public agar pengujian bisa membuat dialog lalu menekan tombolnya tanpa interaksi manusia.
    public static Form BuatKonfirmasi(string pesan, string judul, string? rincian = null) =>
        BuatDialog(pesan, rincian, judul, IkonHapus, Tema.MerahMuda, Tema.Merah,
            (TeksTidak, DialogResult.No, true, JenisTombol.Sekunder),
            (TeksYa, DialogResult.Yes, false, JenisTombol.Hapus));

    // Jendela pemilik diredupkan selama dialog terbuka, seperti desain.
    private static DialogResult Tampilkan(Form dialog, IWin32Window? pemilik)
    {
        if (pemilik is not Form induk || !induk.Visible) return dialog.ShowDialog(pemilik);

        using Form redup = new Form
        {
            FormBorderStyle = FormBorderStyle.None,
            BackColor = Color.Black,
            Opacity = 0.35,
            ShowInTaskbar = false,
            StartPosition = FormStartPosition.Manual,
            Bounds = induk.Bounds,
        };
        redup.Show(induk);
        try { return dialog.ShowDialog(redup); }
        finally { redup.Close(); }
    }

    // Tombol yang ditandai 'aman' menjadi tombol bawaan (Enter), tombol batal (Esc), dan fokus awal.
    private static Form BuatDialog(string pesan, string? rincian, string judul, string ikon, Color latarIkon, Color warnaIkon,
                                   params (string Teks, DialogResult Hasil, bool Aman, JenisTombol Jenis)[] tombol)
    {
        const int lebar = 400;
        Form dialog = new()
        {
            Text = judul,
            FormBorderStyle = FormBorderStyle.None,
            StartPosition = FormStartPosition.CenterParent,
            ShowInTaskbar = false,
            BackColor = Tema.Putih,
            Font = Tema.Huruf(10),
            Width = lebar,
            KeyPreview = true,
        };

        int y = 24;
        dialog.Controls.Add(new ChipIkon(ikon, latarIkon, warnaIkon, 36) { Location = new Point(24, y) });
        y += 50;

        Label lblPesan = new()
        {
            Text = pesan,
            AutoSize = true,
            MaximumSize = new Size(lebar - 48, 0),
            Font = Tema.Huruf(12, FontStyle.Bold),
            ForeColor = Tema.Navy,
            Location = new Point(24, y),
            UseMnemonic = false,
        };
        dialog.Controls.Add(lblPesan);
        y += lblPesan.PreferredHeight + 8;

        if (rincian != null)
        {
            Label lblRincian = new()
            {
                Text = rincian,
                AutoSize = true,
                MaximumSize = new Size(lebar - 48, 0),
                Font = Tema.Huruf(10),
                ForeColor = Tema.Teks,
                Location = new Point(24, y),
                UseMnemonic = false,
            };
            dialog.Controls.Add(lblRincian);
            y += lblRincian.PreferredHeight + 8;
        }
        y += 12;

        // Tombol rata kanan, urut seperti daftar (mis. "Tidak" lalu "Ya, hapus").
        int x = lebar - 24;
        Button? tombolAman = null;
        foreach (var (label, hasil, aman, jenis) in tombol.Reverse())
        {
            Button b = new TombolNadi(jenis) { Text = label, DialogResult = hasil };
            Size p = b.GetPreferredSize(Size.Empty);
            b.Size = new Size(Math.Max(96, p.Width), 38);
            x -= b.Width;
            b.Location = new Point(x, y);
            x -= 10;
            dialog.Controls.Add(b);
            if (aman) tombolAman = b;
        }
        dialog.Height = y + 38 + 24;

        if (tombolAman != null)
        {
            dialog.AcceptButton = tombolAman;
            dialog.CancelButton = tombolAman;
            dialog.Shown += (s, e) => tombolAman.Focus();
        }

        // Kartu bersudut bulat dengan bingkai tipis.
        GraphicsPath BentukKartu() => Tema.Bulat(new RectangleF(0, 0, dialog.Width - 1, dialog.Height - 1), 14);
        dialog.Load += (s, e) =>
        {
            using GraphicsPath bentuk = Tema.Bulat(new RectangleF(0, 0, dialog.Width, dialog.Height), 14);
            dialog.Region = new Region(bentuk);
        };
        dialog.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using GraphicsPath bentuk = BentukKartu();
            using Pen pen = new(Tema.Garis);
            e.Graphics.DrawPath(pen, bentuk);
        };
        // Menutup jendela (Alt+F4 / WM_CLOSE) menghasilkan DialogResult.Cancel, bukan Yes.
        return dialog;
    }
}
