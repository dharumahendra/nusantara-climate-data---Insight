using System.Diagnostics.CodeAnalysis;

namespace Junpro_Project;

// Kerangka bersama semua halaman, diambil dari desain praktik (Form1):
// sidebar navigasi di kiri + area konten yang bisa di-scroll di kanan.
// Setiap halaman adalah form tersendiri yang cukup mengisi BangunKonten().
public abstract class FormDasar : Form
{
    protected const int LebarKonten = 720;

    private static readonly (string Teks, Type Tujuan)[] menuNavigasi =
    {
        ("Dashboard Explore", typeof(DashboardForm)),
        ("Climate Insight", typeof(InsightForm)),
        ("Perbandingan Wilayah", typeof(PerbandinganForm)),
        ("Chat AI", typeof(ChatForm)),
        ("Aksi Iklim", typeof(AksiForm)),
        ("Pengaturan", typeof(PengaturanForm)),
    };

    private readonly Panel content;
    private readonly Label lblWilayahAktif;
    private Panel? halaman;
    private int versiTerbangun = -1;

    protected FormDasar(string judulHalaman)
    {
        Text = "NADI - Climate Data & Insight  |  " + judulHalaman;
        Size = new Size(1100, 720);
        MinimumSize = new Size(1020, 640);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Tema.Latar;
        Font = Tema.Huruf(10);

        content = new Panel { Dock = DockStyle.Fill, BackColor = Tema.Latar };
        Panel sidebar = BuatSidebar(out lblWilayahAktif);

        Controls.Add(content);
        Controls.Add(sidebar);

        // Panel Fill harus diproses TERAKHIR oleh layout docking,
        // sehingga sidebar tidak menimpa konten.
        content.BringToFront();
    }

    protected abstract void BangunKonten(FlowLayoutPanel kolom);

    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);
        if (Visible && versiTerbangun != StatusAplikasi.Versi)
            BangunUlang();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        base.OnFormClosed(e);

        // Form lain hanya disembunyikan saat berpindah halaman, jadi menutup
        // jendela yang sedang tampil harus mengakhiri seluruh aplikasi.
        if (e.CloseReason == CloseReason.UserClosing)
            Application.Exit();
    }

    protected void BangunUlang()
    {
        versiTerbangun = StatusAplikasi.Versi;

        Panel baru = BuatHalaman(out FlowLayoutPanel kolom);
        BangunKonten(kolom);

        content.SuspendLayout();
        Panel? lama = halaman;
        content.Controls.Clear();
        content.Controls.Add(baru);
        content.ResumeLayout();
        lama?.Dispose();
        halaman = baru;

        Wilayah? w = StatusAplikasi.WilayahAktif;
        lblWilayahAktif.Text = w == null ? "Belum dipilih" : w.Kota + "\n" + w.Provinsi;
    }

    protected void Pindah<T>() where T : FormDasar => Navigasi.Buka<T>(this);

    // =====================================================
    //  SIDEBAR
    // =====================================================

    private Panel BuatSidebar(out Label lblWilayah)
    {
        Panel panel = new Panel
        {
            Dock = DockStyle.Left,
            Width = 210,
            BackColor = Tema.Navy
        };

        // Menu navigasi (Fill) - ditambahkan lebih dulu agar diproses terakhir
        FlowLayoutPanel menu = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = Tema.Navy,
            Padding = new Padding(0, 10, 0, 0)
        };

        foreach (var (teks, tujuan) in menuNavigasi)
        {
            Button b = BuatTombolNav(teks, tujuan == GetType());
            b.Click += (s, e) => Navigasi.Buka(tujuan, this);
            menu.Controls.Add(b);
        }

        // Header logo (Top)
        Panel header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 120,
            BackColor = Tema.Navy
        };

        Label logo = new Label
        {
            Text = "NADI",
            AutoSize = true,
            Location = new Point(22, 22),
            Font = Tema.Huruf(28, FontStyle.Bold),
            ForeColor = Tema.Putih
        };

        Label subjudul = new Label
        {
            Text = "Climate Intelligence",
            AutoSize = true,
            Location = new Point(25, 76),
            Font = Tema.Huruf(9),
            ForeColor = Color.LightGray
        };

        header.Controls.Add(logo);
        header.Controls.Add(subjudul);

        // Wilayah aktif (Bottom) - pengingat konteks di semua halaman
        Panel footer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 96,
            BackColor = Tema.Navy
        };
        footer.Paint += (s, e) =>
        {
            using Pen pen = new Pen(Tema.NavAktif);
            e.Graphics.DrawLine(pen, 22, 0, footer.Width - 22, 0);
        };

        Label judulFooter = new Label
        {
            Text = "WILAYAH AKTIF",
            AutoSize = true,
            Location = new Point(22, 16),
            Font = Tema.Huruf(8, FontStyle.Bold),
            ForeColor = Color.LightGray
        };

        lblWilayah = new Label
        {
            AutoSize = true,
            Location = new Point(22, 38),
            Font = Tema.Huruf(10, FontStyle.Bold),
            ForeColor = Tema.Putih
        };

        footer.Controls.Add(judulFooter);
        footer.Controls.Add(lblWilayah);

        panel.Controls.Add(menu);
        panel.Controls.Add(header);
        panel.Controls.Add(footer);
        menu.BringToFront();

        return panel;
    }

    private static Button BuatTombolNav(string teks, bool aktif)
    {
        Button b = new Button
        {
            Text = teks,
            Size = new Size(210, 48),
            BackColor = aktif ? Tema.NavAktif : Tema.Navy,
            ForeColor = Tema.Putih,
            FlatStyle = FlatStyle.Flat,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(22, 0, 0, 0),
            Font = Tema.Huruf(10.5f, aktif ? FontStyle.Bold : FontStyle.Regular),
            Cursor = Cursors.Hand,
            Margin = new Padding(0),
            UseVisualStyleBackColor = false
        };
        b.FlatAppearance.BorderSize = 0;
        b.FlatAppearance.MouseOverBackColor = Tema.NavAktif;

        if (aktif)
            b.Paint += (s, e) =>
            {
                using SolidBrush br = new SolidBrush(Tema.Teal);
                e.Graphics.FillRectangle(br, 0, 0, 5, b.Height);
            };

        return b;
    }

    // =====================================================
    //  HELPER UI
    // =====================================================

    // Halaman = panel scroll dengan padding, berisi satu kolom vertikal
    private static Panel BuatHalaman(out FlowLayoutPanel kolom)
    {
        Panel halaman = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Tema.Latar,
            AutoScroll = true,
            Padding = new Padding(32, 24, 32, 24)
        };

        kolom = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = Tema.Latar
        };

        halaman.Controls.Add(kolom);
        return halaman;
    }

    protected static Label BuatLabel(string teks, float ukuran, bool bold,
                                     Color? warna = null, int lebarMaks = 0)
    {
        return new Label
        {
            Text = teks,
            AutoSize = true,
            MaximumSize = new Size(lebarMaks, 0),
            Font = Tema.Huruf(ukuran, bold ? FontStyle.Bold : FontStyle.Regular),
            ForeColor = warna ?? Tema.Navy,
            Margin = new Padding(0, 0, 0, 6),
            UseMnemonic = false   // agar "&" tampil apa adanya
        };
    }

    protected static Label BuatJudulBagian(string teks)
    {
        Label l = BuatLabel(teks, 16, true);
        l.Margin = new Padding(0, 18, 0, 6);
        return l;
    }

    protected static Label BuatParagraf(string teks, int lebar = LebarKonten) =>
        BuatLabel(teks, 10.5f, false, Tema.Teks, lebar);

    protected static Label BuatCatatan(string teks, int lebar = LebarKonten) =>
        BuatLabel(teks, 9, false, Tema.TeksRedup, lebar);

    protected static Button BuatTombol(string teks, int lebar = 180, int tinggi = 40)
    {
        Button tombol = new Button
        {
            Text = teks,
            Size = new Size(lebar, tinggi),
            AutoSize = true,   // lebar/tinggi di atas jadi ukuran minimum; teks tidak terpotong
            BackColor = Tema.Teal,
            ForeColor = Tema.Putih,
            FlatStyle = FlatStyle.Flat,
            Font = Tema.Huruf(10),
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false
        };
        tombol.FlatAppearance.BorderSize = 0;
        tombol.FlatAppearance.MouseOverBackColor = Tema.TealHover;
        return tombol;
    }

    protected static Button BuatTombolSekunder(string teks, int lebar = 180, int tinggi = 40)
    {
        Button tombol = BuatTombol(teks, lebar, tinggi);
        tombol.BackColor = Tema.Putih;
        tombol.ForeColor = Tema.Teal;
        tombol.FlatAppearance.BorderSize = 1;
        tombol.FlatAppearance.BorderColor = Tema.Teal;
        tombol.FlatAppearance.MouseOverBackColor = Tema.TealMuda;
        return tombol;
    }

    protected static ComboBox BuatCombo(int lebar)
    {
        return new ComboBox
        {
            Width = lebar,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = Tema.Huruf(10),
            Margin = new Padding(0)
        };
    }

    protected static Control BuatGrupField(string judul, Control isi)
    {
        FlowLayoutPanel grup = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Margin = new Padding(0, 0, 20, 0)
        };
        grup.Controls.Add(BuatLabel(judul, 10.5f, true));
        grup.Controls.Add(isi);
        return grup;
    }

    // Baris horizontal untuk tombol/field yang berjajar.
    protected static FlowLayoutPanel BuatBaris(int jarakAtas, params Control[] isi)
    {
        FlowLayoutPanel baris = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0, jarakAtas, 0, 0)
        };
        foreach (Control c in isi)
        {
            c.Margin = new Padding(c.Margin.Left, c.Margin.Top, 12, c.Margin.Bottom);
            baris.Controls.Add(c);
        }
        return baris;
    }

    protected static FlowLayoutPanel BuatBarisKartu()
    {
        return new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            MaximumSize = new Size(LebarKonten + 20, 0),
            Margin = new Padding(0, 12, 0, 0)
        };
    }

    protected static Panel BuatKartu(string judul, string nilai, string keterangan)
    {
        Panel kartu = new Panel
        {
            Size = new Size(168, 104),
            BackColor = Tema.Putih,
            Margin = new Padding(0, 0, 16, 16)
        };
        BeriBingkai(kartu, Tema.Teal);

        Label lblJudul = BuatLabel(judul, 9, true, Tema.TeksRedup);
        lblJudul.Location = new Point(16, 14);

        Label lblNilai = BuatLabel(nilai, 14, true, Tema.Navy);
        lblNilai.Location = new Point(16, 38);

        Label lblKeterangan = BuatLabel(keterangan, 8.5f, false, Tema.TeksRedup);
        lblKeterangan.Location = new Point(16, 74);

        kartu.Controls.Add(lblJudul);
        kartu.Controls.Add(lblNilai);
        kartu.Controls.Add(lblKeterangan);
        return kartu;
    }

    // Kotak putih selebar konten yang isinya tersusun vertikal.
    protected static FlowLayoutPanel BuatKotak(Color? aksen = null, Color? latar = null)
    {
        FlowLayoutPanel kotak = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            MinimumSize = new Size(LebarKonten, 0),
            MaximumSize = new Size(LebarKonten, 0),
            BackColor = latar ?? Tema.Putih,
            Padding = new Padding(22, 16, 18, 14),
            Margin = new Padding(0, 4, 0, 12)
        };
        BeriBingkai(kotak, aksen);
        return kotak;
    }

    protected static void BeriBingkai(Control c, Color? aksenKiri = null)
    {
        c.Paint += (s, e) =>
        {
            using (Pen pen = new Pen(Tema.Garis))
                e.Graphics.DrawRectangle(pen, 0, 0, c.Width - 1, c.Height - 1);
            if (aksenKiri is Color warna)
                using (SolidBrush br = new SolidBrush(warna))
                    e.Graphics.FillRectangle(br, 0, 0, 4, c.Height);
        };
    }

    // Keadaan kosong (PRD §13): dipakai saat belum ada data untuk ditampilkan.
    protected static Control BuatKeadaanKosong(string judul, string pesan,
                                               string? teksTombol = null, Action? aksi = null)
    {
        FlowLayoutPanel kotak = BuatKotak();
        kotak.Padding = new Padding(26, 22, 26, 22);
        kotak.Controls.Add(BuatLabel(judul, 14, true));
        kotak.Controls.Add(BuatParagraf(pesan, LebarKonten - 60));

        if (teksTombol != null && aksi != null)
        {
            Button tombol = BuatTombol(teksTombol, 200, 38);
            tombol.Margin = new Padding(0, 10, 0, 0);
            tombol.Click += (s, e) => aksi();
            kotak.Controls.Add(tombol);
        }
        return kotak;
    }

    // Halaman yang butuh data wilayah menampilkan keadaan kosong bila belum ada
    // wilayah yang dipilih, lengkap dengan tombol menuju Dashboard Explore.
    protected bool CobaAmbilProfil(FlowLayoutPanel kolom, string pesan,
                                   [NotNullWhen(true)] out ProfilIklim? profil)
    {
        profil = StatusAplikasi.Profil;
        if (profil != null) return true;

        kolom.Controls.Add(BuatKeadaanKosong("Belum ada wilayah yang dipilih", pesan,
            "Pilih Wilayah", () => Pindah<DashboardForm>()));
        return false;
    }

    // Penanda bahwa jawaban AI masih contoh (belum tersambung ke Gemini).
    protected Control BuatBannerDemo()
    {
        FlowLayoutPanel banner = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            MinimumSize = new Size(LebarKonten, 0),
            MaximumSize = new Size(LebarKonten, 0),
            BackColor = Tema.KuningMuda,
            Padding = new Padding(16, 10, 12, 10),
            Margin = new Padding(0, 4, 0, 10)
        };
        BeriBingkai(banner, Tema.Oranye);

        bool aiAktif = StatusAplikasi.AiAktif;
        string pesan = aiAktif
            ? "Mode demo: API key sudah tersimpan, tetapi NADI belum tersambung ke Gemini. " +
              "Jawaban AI di halaman ini masih berupa contoh."
            : "Fitur AI belum aktif — masukkan API key di Pengaturan. " +
              "Sementara itu, jawaban AI di halaman ini berupa contoh (mode demo).";

        Label lbl = BuatLabel(pesan, 9.5f, false, Tema.Teks, aiAktif ? LebarKonten - 40 : LebarKonten - 190);
        lbl.Margin = new Padding(0, 4, 12, 0);
        banner.Controls.Add(lbl);

        if (!aiAktif)
        {
            Button tombol = BuatTombolSekunder("Buka Pengaturan", 140, 32);
            tombol.Margin = new Padding(0);
            tombol.Click += (s, e) => Pindah<PengaturanForm>();
            banner.Controls.Add(tombol);
        }
        return banner;
    }
}
