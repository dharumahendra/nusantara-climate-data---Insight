namespace Junpro_Project;

// Dialog berbahasa Indonesia dengan gaya NADI. MessageBox memakai teks tombol dari
// bahasa Windows (Yes/No di Windows berbahasa Inggris), sehingga tidak konsisten
// dengan antarmuka NADI. Dialog ini dibuat sendiri agar teks tombol pasti "Ya"/"Tidak".
//
// Aman untuk aksi menghapus: tombol bawaan (Enter), Esc, dan tombol tutup jendela
// semuanya berarti "Tidak". Hanya klik "Ya" yang mengembalikan true.
public static class DialogNadi
{
    public const string TeksYa = "Ya";
    public const string TeksTidak = "Tidak";
    public const string TeksTutup = "Tutup";

    public static bool Konfirmasi(IWin32Window? pemilik, string pesan, string judul)
    {
        using Form dialog = BuatKonfirmasi(pesan, judul);
        return Tampilkan(dialog, pemilik) == DialogResult.Yes;
    }

    public static void Info(IWin32Window? pemilik, string pesan, string judul)
    {
        using Form dialog = BuatDialog(pesan, judul, (TeksTutup, DialogResult.OK, true));
        Tampilkan(dialog, pemilik);
    }

    // Public agar pengujian bisa membuat dialog lalu menekan tombolnya tanpa interaksi manusia.
    public static Form BuatKonfirmasi(string pesan, string judul) =>
        BuatDialog(pesan, judul, (TeksYa, DialogResult.Yes, false), (TeksTidak, DialogResult.No, true));

    private static DialogResult Tampilkan(Form dialog, IWin32Window? pemilik) =>
        pemilik == null ? dialog.ShowDialog() : dialog.ShowDialog(pemilik);

    // Tombol yang ditandai 'aman' menjadi tombol bawaan (Enter), tombol batal (Esc), dan fokus awal.
    private static Form BuatDialog(string pesan, string judul, params (string Teks, DialogResult Hasil, bool Aman)[] tombol)
    {
        Form dialog = new()
        {
            Text = judul,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            MaximizeBox = false,
            MinimizeBox = false,
            ShowInTaskbar = false,
            ShowIcon = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Tema.Putih,
            Font = Tema.Huruf(10),
            Padding = new Padding(24, 20, 24, 18),
        };

        TableLayoutPanel isi = new()
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 2,
            Dock = DockStyle.Fill,
        };

        Label teks = new()
        {
            Text = pesan,
            AutoSize = true,
            MaximumSize = new Size(380, 0),
            MinimumSize = new Size(280, 0),
            ForeColor = Tema.Teks,
            Font = Tema.Huruf(10.5f),
            Margin = new Padding(0, 0, 0, 18),
        };
        isi.Controls.Add(teks, 0, 0);

        FlowLayoutPanel baris = new()
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Right,
            Anchor = AnchorStyles.Right,
            WrapContents = false,
            Margin = new Padding(0),
        };

        Button? tombolAman = null;
        // RightToLeft: tombol terakhir di daftar tampil paling kiri, jadi urutan dibalik.
        foreach (var (label, hasil, aman) in tombol.Reverse())
        {
            Button b = new()
            {
                Text = label,
                DialogResult = hasil,
                AutoSize = true,
                MinimumSize = new Size(96, 36),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false,
                Margin = new Padding(8, 0, 0, 0),
                BackColor = aman ? Tema.Putih : Tema.TealTeks,
                ForeColor = aman ? Tema.TealTeks : Tema.Putih,
            };
            b.FlatAppearance.BorderSize = 1;
            b.FlatAppearance.BorderColor = Tema.TealTeks;
            b.FlatAppearance.MouseOverBackColor = aman ? Tema.TealMuda : Tema.TealTeksHover;
            baris.Controls.Add(b);
            if (aman) tombolAman = b;
        }
        isi.Controls.Add(baris, 0, 1);
        dialog.Controls.Add(isi);

        if (tombolAman != null)
        {
            dialog.AcceptButton = tombolAman;
            dialog.CancelButton = tombolAman;
            dialog.Shown += (s, e) => tombolAman.Focus();
        }
        // Menutup jendela lewat tombol X menghasilkan DialogResult.Cancel, bukan Yes.
        return dialog;
    }
}
