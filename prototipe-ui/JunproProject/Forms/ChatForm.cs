namespace Junpro_Project;

// Chat tanya-jawab seputar wilayah aktif (PRD F-05). Riwayat disimpan per
// wilayah sehingga tetap ada saat pengguna kembali ke halaman ini.
public sealed class ChatForm : FormDasar
{
    private const int BatasKarakter = 500;   // PRD §11.6

    private static readonly string[] pertanyaanSaran =
    {
        "Apakah wilayah ini makin panas?",
        "Bagaimana pola hujannya?",
        "Apa yang bisa saya lakukan?"
    };

    private readonly System.Windows.Forms.Timer timerJawab = new System.Windows.Forms.Timer { Interval = 900 };
    private readonly List<Button> tombolSaran = new();
    private FlowLayoutPanel? areaChat;
    private TextBox? txtPesan;
    private Button? btnKirim;
    private Button? btnHapus;
    private Label? lblHitung;
    private Label? lblKosong;
    private Control? barisMengetik;
    private (Wilayah Wilayah, string Pertanyaan)? permintaan;

    public ChatForm() : base("Chat AI")
    {
        // Jeda meniru waktu tunggu respons AI.
        timerJawab.Tick += (s, e) => JawabanSiap();
    }

    protected override void BangunKonten(FlowLayoutPanel kolom)
    {
        areaChat = null;
        lblKosong = null;
        tombolSaran.Clear();

        kolom.Controls.Add(BuatLabel("Chat AI", 24, true));

        if (!CobaAmbilProfil(kolom,
                "Chat AI membahas data wilayah yang sedang dibuka. Pilih wilayah dulu di Dashboard Explore.",
                out ProfilIklim? profil))
            return;

        kolom.Controls.Add(BuatLabel("Tanya jawab seputar iklim wilayah yang sedang dibuka", 11, false, Tema.Teks));

        // --- Header konteks ---
        Panel header = new Panel
        {
            Size = new Size(LebarKonten, 52),
            BackColor = Tema.Putih,
            Margin = new Padding(0, 14, 0, 8)
        };
        BeriBingkai(header, Tema.Teal);

        Label konteks = BuatLabel($"Konteks: {profil.Wilayah.Kota}, {profil.Periode}", 11, true);
        konteks.Location = new Point(18, 14);

        btnHapus = BuatTombolSekunder("Hapus Riwayat", 130, 32);
        btnHapus.Location = new Point(LebarKonten - 130 - 10, 10);
        btnHapus.Click += (s, e) => HapusRiwayat(profil.Wilayah);

        header.Controls.Add(konteks);
        header.Controls.Add(btnHapus);
        kolom.Controls.Add(header);

        kolom.Controls.Add(BuatBannerDemo());

        // --- Area percakapan ---
        areaChat = new FlowLayoutPanel
        {
            Size = new Size(LebarKonten, 330),
            AutoScroll = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = Tema.Putih,
            Padding = new Padding(12, 12, 12, 4),
            Margin = new Padding(0, 0, 0, 10)
        };
        kolom.Controls.Add(areaChat);
        TampilkanRiwayat(profil.Wilayah);

        // --- Pertanyaan saran ---
        Label lblSaran = BuatLabel("Coba tanyakan:", 9.5f, true, Tema.TeksRedup);
        lblSaran.Margin = new Padding(0, 8, 0, 0);

        foreach (string pertanyaan in pertanyaanSaran)
        {
            Button b = BuatTombolSekunder(pertanyaan, 60, 32);
            b.AutoSize = true;
            b.Font = Tema.Huruf(9.5f);
            b.Click += (s, e) => Kirim(pertanyaan);
            tombolSaran.Add(b);
        }
        kolom.Controls.Add(BuatBaris(0, new Control[] { lblSaran }.Concat(tombolSaran).ToArray()));

        // --- Input pesan ---
        txtPesan = new TextBox
        {
            Width = 520,
            MaxLength = BatasKarakter,
            Font = Tema.Huruf(11),
            PlaceholderText = $"Tulis pertanyaan tentang iklim {profil.Wilayah.Kota}…",
            Margin = new Padding(0, 2, 0, 0)
        };
        txtPesan.TextChanged += (s, e) => PerbaruiStatusInput();
        txtPesan.KeyDown += (s, e) =>
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;
            Kirim(txtPesan.Text);
        };

        btnKirim = BuatTombol("Kirim", 100, 32);
        btnKirim.Click += (s, e) => Kirim(txtPesan.Text);

        lblHitung = BuatLabel("", 9, false, Tema.TeksRedup);
        lblHitung.Margin = new Padding(0, 9, 0, 0);

        kolom.Controls.Add(BuatBaris(12, txtPesan, btnKirim, lblHitung));
        PerbaruiStatusInput();
    }

    private void TampilkanRiwayat(Wilayah wilayah)
    {
        if (areaChat == null) return;

        IReadOnlyList<PesanChat> riwayat = StatusAplikasi.RiwayatChat(wilayah);
        if (riwayat.Count == 0)
        {
            lblKosong = BuatLabel(
                "Belum ada percakapan. Tulis pertanyaan atau pilih salah satu pertanyaan saran di bawah.",
                10, false, Tema.TeksRedup, LebarKonten - 60);
            lblKosong.Margin = new Padding(4, 6, 0, 0);
            areaChat.Controls.Add(lblKosong);
        }

        foreach (PesanChat pesan in riwayat)
            TambahGelembung(pesan.Isi, pesan.DariPengguna);

        if (permintaan?.Wilayah == wilayah)
            barisMengetik = TambahGelembung("NADI sedang mengetik…", false);
    }

    private Control TambahGelembung(string isi, bool dariPengguna)
    {
        FlowLayoutPanel area = areaChat!;
        int lebarBaris = area.ClientSize.Width - area.Padding.Horizontal - SystemInformation.VerticalScrollBarWidth;

        Label gelembung = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(480, 0),
            Padding = new Padding(12, 8, 12, 8),
            Margin = new Padding(0),
            Font = Tema.Huruf(10),
            BackColor = dariPengguna ? Tema.Teal : Tema.GelembungAi,
            ForeColor = dariPengguna ? Tema.Putih : Tema.Teks,
            Text = isi
        };

        // Pesan pengguna rata kanan, jawaban NADI rata kiri.
        FlowLayoutPanel baris = new FlowLayoutPanel
        {
            FlowDirection = dariPengguna ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            MinimumSize = new Size(lebarBaris, 0),
            MaximumSize = new Size(lebarBaris, 0),
            Margin = new Padding(0, 0, 0, 8),
            BackColor = Tema.Putih
        };
        baris.Controls.Add(gelembung);

        area.Controls.Add(baris);
        area.PerformLayout();
        area.ScrollControlIntoView(baris);
        return baris;
    }

    private void Kirim(string teks)
    {
        teks = teks.Trim();
        if (permintaan != null || teks.Length == 0 || areaChat == null) return;
        if (StatusAplikasi.WilayahAktif is not { } wilayah) return;
        if (teks.Length > BatasKarakter) teks = teks[..BatasKarakter];

        StatusAplikasi.TambahPesanChat(wilayah, new PesanChat(true, teks));

        if (lblKosong != null)
        {
            lblKosong.Dispose();
            lblKosong = null;
        }

        TambahGelembung(teks, true);
        permintaan = (wilayah, teks);
        barisMengetik = TambahGelembung("NADI sedang mengetik…", false);

        if (txtPesan != null) txtPesan.Text = "";
        PerbaruiStatusInput();
        timerJawab.Start();
    }

    private void JawabanSiap()
    {
        timerJawab.Stop();
        if (permintaan is not { } p) return;
        permintaan = null;

        string jawaban = AsistenDemo.Jawab(DataIklim.Profil(p.Wilayah), p.Pertanyaan);
        StatusAplikasi.TambahPesanChat(p.Wilayah, new PesanChat(false, jawaban));

        barisMengetik?.Dispose();
        barisMengetik = null;

        if (areaChat != null && p.Wilayah == StatusAplikasi.WilayahAktif)
            TambahGelembung(jawaban, false);

        PerbaruiStatusInput();
    }

    private void HapusRiwayat(Wilayah wilayah)
    {
        if (permintaan != null || StatusAplikasi.RiwayatChat(wilayah).Count == 0) return;

        DialogResult jawab = MessageBox.Show(
            $"Hapus seluruh riwayat chat untuk {wilayah.Kota}?",
            "Hapus Riwayat", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (jawab != DialogResult.Yes) return;

        StatusAplikasi.HapusRiwayatChat(wilayah);
        BangunUlang();
    }

    private void PerbaruiStatusInput()
    {
        bool bebas = permintaan == null;
        int panjang = txtPesan?.Text.Length ?? 0;

        if (btnKirim != null) btnKirim.Enabled = bebas && txtPesan?.Text.Trim().Length > 0;
        if (btnHapus != null) btnHapus.Enabled = bebas;
        foreach (Button b in tombolSaran) b.Enabled = bebas;
        if (lblHitung != null) lblHitung.Text = $"{panjang}/{BatasKarakter}";
    }
}
