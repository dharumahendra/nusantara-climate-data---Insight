namespace Junpro_Project;

// Chat tanya-jawab seputar wilayah aktif (PRD F-05). Riwayat disimpan per
// wilayah sehingga tetap ada saat pengguna kembali ke halaman ini.
public sealed class ChatForm : FormDasar
{
    private const int BatasKarakter = AsistenIklim.BatasKarakterPertanyaan;   // PRD §11.6

    private static readonly string[] pertanyaanSaran =
    {
        "Apakah wilayah ini makin panas?",
        "Bagaimana pola hujannya?",
        "Apa yang bisa saya lakukan?"
    };

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

        if (!StatusAplikasi.Asisten.MemakaiAi)
            kolom.Controls.Add(BuatBannerDemo());

        FlowLayoutPanel kartuChat = BuatKotak();
        kartuChat.Padding = new Padding(1);
        kartuChat.Margin = new Padding(0, 10, 0, 0);
        kolom.Controls.Add(kartuChat);
        const int lebarDalam = LebarKonten - 2;

        // --- Header konteks ---
        Panel header = new Panel
        {
            Size = new Size(lebarDalam, 66),
            BackColor = Tema.Putih,
            Margin = new Padding(0)
        };
        header.Paint += (s, e) =>
        {
            using Pen pen = new Pen(Tema.Garis);
            e.Graphics.DrawLine(pen, 0, header.Height - 1, header.Width, header.Height - 1);
        };

        header.Controls.Add(new Label { Text = "\uE707", Font = Tema.Ikon(11), ForeColor = Tema.Navy, AutoSize = true, Location = new Point(16, 14) });
        Label konteks = BuatLabel($"Konteks: {profil.Wilayah.Kota}, {profil.Periode}", 11, true);
        konteks.Location = new Point(36, 10);
        Label asistenAktif = BuatCatatan("Asisten aktif: " + StatusAplikasi.Asisten.Nama, LebarKonten - 200);
        asistenAktif.Location = new Point(36, 36);
        header.Controls.Add(asistenAktif);

        btnHapus = BuatTombolBahaya("Hapus Riwayat", 130, 34);
        ((TombolNadi)btnHapus).Ikon = "\uE74D";
        btnHapus.AutoSize = false;
        btnHapus.Size = new Size(150, 34);
        btnHapus.Location = new Point(lebarDalam - 150 - 14, 16);
        btnHapus.Click += (s, e) => HapusRiwayat(profil.Wilayah);

        header.Controls.Add(konteks);
        header.Controls.Add(btnHapus);
        kartuChat.Controls.Add(header);

        // --- Area percakapan ---
        areaChat = new FlowLayoutPanel
        {
            Size = new Size(lebarDalam, 320),
            AutoScroll = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = LatarChat,
            Padding = new Padding(14, 14, 14, 4),
            Margin = new Padding(0)
        };
        kartuChat.Controls.Add(areaChat);
        TampilkanRiwayat(profil.Wilayah);

        // --- Pertanyaan saran ---
        Label lblSaran = BuatLabel("Coba tanyakan:", 9.5f, true, Tema.TeksRedup);
        lblSaran.Margin = new Padding(0, 8, 0, 0);

        foreach (string pertanyaan in pertanyaanSaran)
        {
            Button b = new TombolNadi(JenisTombol.Chip) { Text = pertanyaan, Size = new Size(60, 30), AutoSize = true };
            b.Font = Tema.Huruf(8.5f, FontStyle.Bold);
            b.Padding = new Padding(6, 0, 6, 0);
            b.Click += (s, e) => Kirim(pertanyaan);
            tombolSaran.Add(b);
        }
        FlowLayoutPanel barisSaran = BuatBaris(0, new Control[] { lblSaran }.Concat(tombolSaran).ToArray());
        barisSaran.Margin = new Padding(16, 10, 0, 0);
        kartuChat.Controls.Add(barisSaran);

        // --- Input pesan ---
        txtPesan = new TextBox
        {
            Width = lebarDalam - 32 - 140 - 64 - 30,
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

        btnKirim = BuatTombolIkon("Kirim", "\uE72A", JenisTombol.Utama);
        btnKirim.Click += (s, e) => Kirim(txtPesan.Text);

        lblHitung = BuatLabel("", 9, false, Tema.TeksRedup);
        lblHitung.AutoSize = false;
        lblHitung.Size = new Size(54, 18);
        lblHitung.TextAlign = ContentAlignment.MiddleRight;

        btnKirim.AutoSize = false;
        btnKirim.Size = new Size(110, 38);
        FlowLayoutPanel barisInput = BuatBaris(0, BuatKotakIsian(txtPesan, null, null, lblHitung), btnKirim);
        barisInput.Margin = new Padding(16, 10, 0, 14);
        kartuChat.Controls.Add(barisInput);
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
            barisMengetik = TambahGelembung("•  •  •    Asisten sedang mengetik…", false);
    }

    private static readonly Color LatarChat = Color.FromArgb(248, 250, 250);

    private Control TambahGelembung(string isi, bool dariPengguna)
    {
        FlowLayoutPanel area = areaChat!;
        int lebarBaris = area.ClientSize.Width - area.Padding.Horizontal - SystemInformation.VerticalScrollBarWidth;

        Label gelembung = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(480, 0),
            Padding = new Padding(14, 10, 14, 10),
            Margin = new Padding(0),
            Font = Tema.Huruf(10),
            BackColor = dariPengguna ? Tema.Aksen : Tema.Putih,
            ForeColor = dariPengguna ? Tema.Navy : Tema.Teks,
            UseMnemonic = false,
            Text = isi
        };
        BeriBingkai(gelembung, dariPengguna ? Tema.Aksen : null, 12);

        // Pesan pengguna rata kanan, jawaban NADI rata kiri.
        FlowLayoutPanel baris = new FlowLayoutPanel
        {
            FlowDirection = dariPengguna ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            MinimumSize = new Size(lebarBaris, 0),
            MaximumSize = new Size(lebarBaris, 0),
            Margin = new Padding(0, 0, 0, 10),
            BackColor = LatarChat
        };
        if (!dariPengguna)
            baris.Controls.Add(new ChipIkon(ChipIkon.Kilau, Tema.TealMuda, Tema.TealTeks, 28) { Margin = new Padding(0, 2, 10, 0) });
        baris.Controls.Add(gelembung);

        area.Controls.Add(baris);
        area.PerformLayout();
        area.ScrollControlIntoView(baris);
        return baris;
    }

    private async void Kirim(string teks)
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
        barisMengetik = TambahGelembung("•  •  •    Asisten sedang mengetik…", false);

        if (txtPesan != null) txtPesan.Text = "";
        PerbaruiStatusInput();

        // Polymorphism: yang menjawab bisa AsistenDemo atau AsistenGemini,
        // tergantung objek yang sedang tersimpan di StatusAplikasi.Asisten.
        string jawaban = await StatusAplikasi.Asisten.JawabAsync(DataIklim.Profil(wilayah), teks);
        JawabanSiap(jawaban);
    }

    private void JawabanSiap(string jawaban)
    {
        if (permintaan is not { } p) return;
        permintaan = null;

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

        if (!DialogNadi.Konfirmasi(this, $"Hapus seluruh riwayat chat untuk {wilayah.Kota}?", "Hapus Riwayat",
                $"Semua percakapan untuk wilayah {wilayah.Kota} akan dihapus dan tidak bisa dikembalikan."))
            return;

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
