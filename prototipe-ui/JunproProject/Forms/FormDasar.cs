using System.Diagnostics.CodeAnalysis;

namespace Junpro_Project;

// Kerangka bersama semua halaman, diambil dari desain praktik (Form1):
// sidebar navigasi di kiri + area konten yang bisa di-scroll di kanan.
// Setiap halaman adalah form tersendiri yang cukup mengisi BangunKonten().
public abstract class FormDasar : Form
{
    protected const int LebarKonten = 780;

    private static readonly (string Teks, string Ikon, Type Tujuan)[] menuNavigasi =
    {
        ("Dashboard Explore", ItemNav.Kompas, typeof(DashboardForm)),
        ("Climate Insight", "\uE9D2", typeof(InsightForm)),
        ("Perbandingan Wilayah", "\uE8AB", typeof(PerbandinganForm)),
        ("Chat AI", "\uE8BD", typeof(ChatForm)),
        ("Aksi Iklim", "\uEA80", typeof(AksiForm)),
        ("Pengaturan", "\uE713", typeof(PengaturanForm)),
    };

    private readonly Panel content;
    private readonly Label lblWilayahAktif;
    private Label lblProvinsiAktif = null!;
    private Panel? halaman;
    private int versiTerbangun = -1;

    protected FormDasar(string judulHalaman)
    {
        Text = "NADI - Climate Data & Insight  |  " + judulHalaman;
        if (Tema.IkonAplikasi != null) Icon = Tema.IkonAplikasi;
        Size = new Size(1180, 740);
        MinimumSize = new Size(1100, 640);
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
        PasangJejakTahap(kolom);

        content.SuspendLayout();
        Panel? lama = halaman;
        content.Controls.Clear();
        content.Controls.Add(baru);
        content.ResumeLayout();
        lama?.Dispose();
        halaman = baru;

        Wilayah? w = StatusAplikasi.WilayahAktif;
        lblWilayahAktif.Text = w == null ? "Belum dipilih" : w.Kota;
        lblProvinsiAktif.Text = w?.Provinsi ?? "Pilih di Dashboard";
    }

    // Judul halaman (label 24pt pertama) dipasangkan dengan penanda tahap
    // Explore > Understand > Act di kanan, seperti desain.
    private void PasangJejakTahap(FlowLayoutPanel kolom)
    {
        int tahap = this switch
        {
            DashboardForm => 0,
            AksiForm => 2,
            PengaturanForm => -1,
            _ => 1,
        };
        if (tahap < 0 || kolom.Controls.Count == 0 || kolom.Controls[0] is not Label judul) return;

        Panel baris = new() { Width = LebarKonten, Height = Math.Max(judul.PreferredHeight, 40), Margin = judul.Margin };
        kolom.Controls.Remove(judul);
        judul.Location = new Point(0, 0);
        judul.Margin = new Padding(0);
        JejakTahap jejak = new(tahap) { Anchor = AnchorStyles.Top | AnchorStyles.Right };
        jejak.Location = new Point(LebarKonten - jejak.Width, 4);
        baris.Controls.Add(judul);
        baris.Controls.Add(jejak);
        kolom.Controls.Add(baris);
        kolom.Controls.SetChildIndex(baris, 0);
    }

    protected void Pindah<T>() where T : FormDasar => Navigasi.Buka<T>(this);

    // =====================================================
    //  SIDEBAR
    // =====================================================

    private Panel BuatSidebar(out Label lblWilayah)
    {
        Panel panel = new Panel { Dock = DockStyle.Left, Width = 220, BackColor = Tema.Putih };
        panel.Paint += (s, e) =>
        {
            using Pen pen = new Pen(Tema.Garis);
            e.Graphics.DrawLine(pen, panel.Width - 1, 0, panel.Width - 1, panel.Height);
        };

        // Header logo (Top)
        Panel header = new Panel { Dock = DockStyle.Top, Height = 104, BackColor = Tema.Putih };
        header.Controls.Add(new ChipIkon(ChipIkon.Denyut, Tema.Aksen, Tema.Navy, 40) { Location = new Point(18, 26) });
        Label logo = BuatLabel("NADI", 15, true, Tema.Navy);
        logo.Location = new Point(66, 22);
        Label subjudul = BuatLabel("Climate Intelligence", 9, false, Tema.TeksRedup);
        subjudul.Location = new Point(67, 50);
        header.Controls.Add(logo);
        header.Controls.Add(subjudul);

        // Menu navigasi (Fill)
        FlowLayoutPanel menu = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = Tema.Putih
        };
        Label judulMenu = BuatLabel("MENU", 8, true, Tema.TeksRedup);
        judulMenu.Margin = new Padding(20, 0, 0, 6);
        menu.Controls.Add(judulMenu);

        // Bawah: Pengaturan + wilayah aktif (pengingat konteks di semua halaman)
        Panel bawah = new Panel { Dock = DockStyle.Bottom, Height = 160, BackColor = Tema.Putih };

        foreach (var (teks, ikon, tujuan) in menuNavigasi)
        {
            ItemNav item = new ItemNav(teks, ikon, tujuan == GetType());
            item.Click += (s, e) => Navigasi.Buka(tujuan, this);
            if (tujuan == typeof(PengaturanForm))
            {
                item.Location = new Point(9, 0);
                bawah.Controls.Add(item);
            }
            else menu.Controls.Add(item);
        }

        Panel kartuWilayah = new Panel { Location = new Point(12, 56), Size = new Size(196, 90), BackColor = Tema.Latar };
        BeriBingkai(kartuWilayah, null, 12);
        Label judulFooter = BuatLabel("WILAYAH AKTIF", 8, true, Tema.TeksRedup);
        judulFooter.Location = new Point(14, 10);
        kartuWilayah.Controls.Add(judulFooter);
        kartuWilayah.Controls.Add(new ChipIkon("\uE707", Tema.TealMuda, Tema.TealTeks, 32) { Location = new Point(14, 40) });

        lblWilayah = BuatLabel("", 10, true, Tema.Navy, 134);
        lblWilayah.Location = new Point(54, 36);
        lblProvinsiAktif = BuatLabel("", 8.5f, false, Tema.TeksRedup, 134);
        lblProvinsiAktif.Location = new Point(54, 58);
        kartuWilayah.Controls.Add(lblWilayah);
        kartuWilayah.Controls.Add(lblProvinsiAktif);
        bawah.Controls.Add(kartuWilayah);

        panel.Controls.Add(menu);
        panel.Controls.Add(header);
        panel.Controls.Add(bawah);
        menu.BringToFront();
        return panel;
    }

    // =====================================================
    //  HELPER UI
    // =====================================================

    // Halaman = panel scroll dengan padding, berisi satu kolom vertikal
    private static Panel BuatHalaman(out FlowLayoutPanel kolom)
    {
        Panel halaman = new PanelLatar
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
            BackColor = Color.Transparent
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

    protected static Button BuatTombol(string teks, int lebar = 180, int tinggi = 40) =>
        new TombolNadi(JenisTombol.Utama) { Text = teks, Size = new Size(lebar, tinggi), AutoSize = true };

    protected static Button BuatTombolSekunder(string teks, int lebar = 180, int tinggi = 40) =>
        new TombolNadi(JenisTombol.Sekunder) { Text = teks, Size = new Size(lebar, tinggi), AutoSize = true };

    protected static Button BuatTombolGelap(string teks, string? ikon = null) =>
        new TombolNadi(JenisTombol.Gelap) { Text = teks, Ikon = ikon, Size = new Size(120, 38), AutoSize = true };

    // Tombol dengan ikon; ukuran mengikuti teks.
    protected static Button BuatTombolIkon(string teks, string ikon, JenisTombol jenis = JenisTombol.Sekunder) =>
        new TombolNadi(jenis) { Text = teks, Ikon = ikon, Size = new Size(100, 38), AutoSize = true };

    // Dua kolom berdampingan selebar konten.
    protected static FlowLayoutPanel BuatDuaKolom(Control kiri, Control kanan)
    {
        FlowLayoutPanel baris = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0, 4, 0, 0)
        };
        kiri.Margin = new Padding(0, 0, 14, 14);
        kanan.Margin = new Padding(0, 0, 0, 14);
        baris.Controls.Add(kiri);
        baris.Controls.Add(kanan);
        return baris;
    }

    // Kartu dengan judul berikon (Temuan Utama, Penjelasan AI, Disclaimer, ...).
    protected static FlowLayoutPanel BuatPanel(string judul, string? ikon, int lebar,
                                               Color? latar = null, Color? garis = null)
    {
        FlowLayoutPanel panel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            MinimumSize = new Size(lebar, 0),
            MaximumSize = new Size(lebar, 0),
            BackColor = latar ?? Tema.Putih,
            Padding = new Padding(18, 16, 16, 14),
        };
        BeriBingkai(panel, garis);
        panel.Controls.Add(BuatJudulIkon(judul, ikon));
        return panel;
    }

    protected static Control BuatJudulIkon(string judul, string? ikon, float ukuran = 11.5f, int lebarMaks = 0)
    {
        FlowLayoutPanel baris = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0, 0, 0, 8)
        };
        if (ikon != null)
        {
            Label i = new Label { Text = ikon, Font = Tema.Ikon(ukuran), ForeColor = Tema.Navy, AutoSize = true, Margin = new Padding(0, 4, 4, 0) };
            baris.Controls.Add(i);
        }
        Label l = BuatLabel(judul, ukuran, lebarMaks == 0, lebarMaks == 0 ? Tema.Navy : Tema.Teks, lebarMaks);
        l.Margin = new Padding(0);
        baris.Controls.Add(l);
        return baris;
    }

    // Kotak status hijau (berhasil) / merah (gagal) / kuning (peringatan), seperti desain Pengaturan.
    protected static Panel BuatStatus(string teks, char jenis, int lebar)
    {
        (Color latar, Color garis, Color warna, string ikon) = jenis switch
        {
            'g' => (Tema.MerahMuda, Tema.MerahGaris, Tema.Merah, "\uEA39"),
            'p' => (Tema.KuningMuda, Tema.KuningGaris, Tema.OranyeTua, "\uE7BA"),
            _ => (Tema.HijauMuda, Color.FromArgb(205, 230, 180), Tema.HijauTua, "\uE930"),
        };
        Panel kotak = new Panel { Width = lebar, BackColor = latar, Margin = new Padding(0, 8, 0, 6) };
        BeriBingkai(kotak, garis, 10);
        Label i = new Label { Text = ikon, Font = Tema.Ikon(10), ForeColor = warna, AutoSize = true, Location = new Point(12, 11) };
        Label l = BuatLabel(teks, 9.5f, false, warna, lebar - 46);
        l.Location = new Point(34, 9);
        kotak.Controls.Add(i);
        kotak.Controls.Add(l);
        kotak.Height = l.PreferredHeight + 20;
        return kotak;
    }

    protected static Button BuatTombolBahaya(string teks, int lebar = 180, int tinggi = 40) =>
        new TombolNadi(JenisTombol.Bahaya) { Text = teks, Size = new Size(lebar, tinggi), AutoSize = true };

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

    protected static Control BuatGrupField(string judul, Control isi, Color? penanda = null)
    {
        isi = BuatKotakIsian(isi, judul.StartsWith("Cari") ? "\uE721" : null, penanda);
        FlowLayoutPanel grup = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Margin = new Padding(0, 0, 14, 0)
        };
        grup.Controls.Add(BuatLabel(judul, 10.5f, true));
        grup.Controls.Add(isi);
        return grup;
    }

    // Baris horizontal untuk tombol/field yang berjajar.
    // Isian bergaya desain: kotak abu muda bersudut bulat, dengan ikon atau penanda warna di kiri
    // dan kontrol tambahan (mis. penghitung karakter) di kanan.
    protected static Control BuatKotakIsian(Control isi, string? ikon = null, Color? penanda = null, Control? ekor = null)
    {
        Panel kotak = new Panel { BackColor = Tema.Latar, Margin = isi.Margin, Cursor = Cursors.IBeam };
        BeriBingkai(kotak, null, 10);
        int x = 10;
        if (ikon != null)
        {
            kotak.Controls.Add(new Label { Text = ikon, Font = Tema.Ikon(10), ForeColor = Tema.TeksRedup, AutoSize = true, Location = new Point(10, 0) });
            x = 32;
        }
        else if (penanda is Color warna)
        {
            Panel garis = new Panel { BackColor = warna, Size = new Size(14, 3), Location = new Point(10, 0) };
            kotak.Controls.Add(garis);
            x = 30;
        }

        isi.Margin = new Padding(0);
        isi.BackColor = Tema.Latar;
        if (isi is TextBox t) t.BorderStyle = BorderStyle.None;
        if (isi is ComboBox c) c.FlatStyle = FlatStyle.Flat;
        int tinggi = Math.Max(36, isi.Height + 14);
        isi.Location = new Point(x, (tinggi - isi.Height) / 2);
        kotak.Controls.Add(isi);

        int lebar = x + isi.Width + 10;
        if (ekor != null)
        {
            ekor.Margin = new Padding(0);
            ekor.BackColor = Tema.Latar;
            ekor.Location = new Point(lebar, (tinggi - ekor.Height) / 2);
            kotak.Controls.Add(ekor);
            lebar += ekor.Width + 10;
        }
        kotak.Size = new Size(lebar, tinggi);
        foreach (Control anak in kotak.Controls)
            if (anak != isi) anak.Top = (tinggi - anak.Height) / 2;
        kotak.Click += (s, e) => isi.Focus();
        return kotak;
    }

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
            Size = new Size(183, 118),
            BackColor = Tema.Putih,
            Margin = new Padding(0, 0, 16, 16)
        };
        BeriBingkai(kartu);

        var (ikon, latar, warnaIkon) = IkonKartu(judul);
        kartu.Controls.Add(new ChipIkon(ikon, latar, warnaIkon) { Location = new Point(14, 14) });

        Label lblJudul = BuatLabel(judul, 8, true, Tema.Teks, 112);
        lblJudul.Location = new Point(46, 19);

        Label lblNilai = BuatLabel(nilai, 17, true, Tema.Navy, 150);
        lblNilai.Location = new Point(12, 46);

        Label lblKeterangan = BuatLabel(keterangan, 8.5f, false, Tema.TeksRedup, 150);
        lblKeterangan.Location = new Point(14, 90);

        kartu.Controls.Add(lblJudul);
        kartu.Controls.Add(lblNilai);
        kartu.Controls.Add(lblKeterangan);
        return kartu;
    }

    // Ikon dan warna chip kartu, sesuai desain (suhu oranye, hujan teal, panas kuning, tren hijau).
    private static (string Ikon, Color Latar, Color Warna) IkonKartu(string judul)
    {
        string j = judul.ToUpperInvariant();
        if (j.Contains("HUJAN")) return ("\uE753", Tema.Aksen, Tema.Navy);
        if (j.Contains("PANAS")) return ("\uE706", Tema.Kuning, Tema.Navy);
        if (j.Contains("SUHU RATA")) return ("\uE9CA", Tema.OranyeTerang, Tema.Putih);
        if (j.Contains("PERUBAHAN")) return ("\uE9D9", Tema.Hijau, Tema.Putih);
        if (j.Contains("KEYAKINAN")) return ("\uE946", Tema.TealMuda, Tema.TealTeks);
        return ("\uE9D2", Tema.TealMuda, Tema.TealTeks);
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
            Padding = new Padding(22, 18, 20, 16),
            Margin = new Padding(0, 4, 0, 14)
        };
        BeriBingkai(kotak, aksen is Color a ? Campur(a, Tema.Putih, 0.55) : null);
        return kotak;
    }

    private static Color Campur(Color a, Color b, double t) => Color.FromArgb(
        (int)(a.R + (b.R - a.R) * t), (int)(a.G + (b.G - a.G) * t), (int)(a.B + (b.B - a.B) * t));

    // Bingkai bersudut bulat: sudut di luar bentuk diisi warna induk.
    protected static void BeriBingkai(Control c, Color? garis = null, int jari = 14)
    {
        c.Resize += (s, e) => c.Invalidate();
        c.Paint += (s, e) =>
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var p = Tema.Bulat(new RectangleF(0.5f, 0.5f, c.Width - 1.5f, c.Height - 1.5f), jari);
            using (Region sudut = new Region(new Rectangle(0, 0, c.Width, c.Height)))
            using (SolidBrush b = new SolidBrush(Tema.LatarInduk(c)))
            {
                sudut.Exclude(p);
                g.FillRegion(b, sudut);
            }
            using Pen pen = new Pen(garis ?? Tema.Garis);
            g.DrawPath(pen, p);
        };
    }

    // Keadaan kosong (PRD §13): dipakai saat belum ada data untuk ditampilkan.
    protected static Control BuatKeadaanKosong(string judul, string pesan,
                                               string? teksTombol = null, Action? aksi = null)
    {
        bool galat = teksTombol == "Coba Lagi";
        return BuatKartuTengah(galat ? "\uE7BA" : "\uE707",
            galat ? Tema.MerahMuda : Tema.TealMuda, galat ? Tema.Merah : Tema.TealTeks,
            judul, pesan, teksTombol, galat ? "\uE895" : "\uE707", aksi, false, null, out _);
    }

    // Keadaan memuat (PRD §13): kartu tengah dengan bilah progres beranimasi.
    protected static Control BuatKeadaanMemuat(string judul, string pesan, string? catatan, out Label lblPesan) =>
        BuatKartuTengah("\uE895", Tema.TealMuda, Tema.TealTeks, judul, pesan, null, null, null, true, catatan, out lblPesan);

    private static Panel BuatKartuTengah(string ikon, Color latarIkon, Color warnaIkon, string judul, string pesan,
        string? teksTombol, string? ikonTombol, Action? aksi, bool bilah, string? catatan, out Label lblPesan)
    {
        Panel kartu = new Panel { Width = LebarKonten, BackColor = Tema.Putih, Margin = new Padding(0, 4, 0, 14) };
        BeriBingkai(kartu);
        const int tepi = 80;
        int lebarTeks = LebarKonten - tepi * 2;
        int y = 44;
        kartu.Controls.Add(new ChipIkon(ikon, latarIkon, warnaIkon, 40) { Location = new Point((LebarKonten - 40) / 2, y) });
        y += 56;

        Label Tengah(string teks, float ukuran, bool tebal, Color warna, int barisMin)
        {
            Label l = BuatLabel(teks, ukuran, tebal, warna, lebarTeks);
            Size ukuranTeks = l.GetPreferredSize(new Size(lebarTeks, 0));
            l.AutoSize = false;
            l.TextAlign = ContentAlignment.TopCenter;
            l.Size = new Size(lebarTeks, Math.Max(ukuranTeks.Height, barisMin * (l.Font.Height + 1)));
            l.Location = new Point(tepi, y);
            y += l.Height + 6;
            kartu.Controls.Add(l);
            return l;
        }

        Tengah(judul, 13, true, Tema.Navy, 1);
        lblPesan = Tengah(pesan, 10, false, Tema.Teks, bilah ? 2 : 1);
        if (bilah)
        {
            kartu.Controls.Add(new BilahMemuat { Size = new Size(260, 6), Location = new Point((LebarKonten - 260) / 2, y + 4) });
            y += 22;
        }
        if (teksTombol != null && aksi != null)
        {
            Button tombol = BuatTombolIkon(teksTombol, ikonTombol ?? "\uE707", JenisTombol.Utama);
            Size p = tombol.GetPreferredSize(Size.Empty);
            tombol.AutoSize = false;
            tombol.Size = new Size(p.Width, 38);
            tombol.Location = new Point((LebarKonten - p.Width) / 2, y + 8);
            tombol.Click += (s, e) => aksi();
            kartu.Controls.Add(tombol);
            y += 56;
        }
        if (catatan != null)
        {
            y += 4;
            Tengah(catatan, 8.5f, false, Tema.TeksRedup, 1);
        }
        kartu.Height = Math.Max(280, y + 44);
        return kartu;
    }

    // Halaman yang butuh data wilayah menampilkan keadaan kosong bila belum ada
    // wilayah yang dipilih, lengkap dengan tombol menuju Dashboard Explore.
    protected bool CobaAmbilProfil(FlowLayoutPanel kolom, string pesan,
                                   [NotNullWhen(true)] out ProfilIklim? profil)
    {
        profil = StatusAplikasi.Profil;
        if (profil != null) return true;

        kolom.Controls.Add(BuatKeadaanKosong("Belum ada wilayah aktif", pesan,
            "Pilih Wilayah di Dashboard", () => Pindah<DashboardForm>()));
        return false;
    }

    // Penanda asisten yang sedang menjawab: Gemini atau mode demo.
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
        BeriBingkai(banner, Tema.KuningGaris, 12);

        AsistenIklim asisten = StatusAplikasi.Asisten;
        bool aiAktif = asisten.MemakaiAi;
        string pesan = aiAktif
            ? $"Asisten aktif: {asisten.Nama}. Jawaban disusun AI dari angka yang dihitung aplikasi; " +
              "bila Gemini tidak dapat dihubungi, NADI memakai jawaban mode demo."
            : $"Asisten aktif: {asisten.Nama}. Fitur AI belum aktif — masukkan API key di Pengaturan. " +
              "Sementara itu, jawaban di halaman ini berupa contoh.";

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
