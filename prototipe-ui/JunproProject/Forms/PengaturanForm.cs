using Npgsql;

namespace Junpro_Project;

// Pengaturan API key Gemini (PRD §11.5), status koneksi database,
// serta informasi & disclaimer aplikasi (PRD §22).
public sealed class PengaturanForm : FormDasar
{
    private const string Disclaimer =
        "NADI adalah aplikasi edukasi. Data iklim bersumber dari reanalisis ERA5 melalui Open-Meteo dan " +
        "dapat berbeda dari pengamatan stasiun cuaca resmi. Penjelasan naratif dihasilkan oleh model bahasa " +
        "AI berdasarkan statistik yang dihitung aplikasi, dan dapat mengandung kekeliruan. NADI bukan sumber " +
        "peringatan dini bencana. Untuk informasi cuaca dan peringatan resmi, rujuk BMKG.";

    public PengaturanForm() : base("Pengaturan")
    {
    }

    protected override void BangunKonten(FlowLayoutPanel kolom)
    {
        kolom.Controls.Add(BuatLabel("Pengaturan", 24, true));
        kolom.Controls.Add(BuatLabel("Atur koneksi AI dan lihat informasi aplikasi", 11, false, Tema.Teks));

        const int lebarKiri = 470;
        int lebarKanan = LebarKonten - lebarKiri - 14;
        int isiKiri = lebarKiri - 36, isiKanan = lebarKanan - 36;
        FlowLayoutPanel kiri = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true };
        FlowLayoutPanel kanan = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true };

        // --- API key ---
        FlowLayoutPanel api = BuatPanel("API Key Google Gemini", "\uE8D7", lebarKiri);
        api.Margin = new Padding(0, 0, 0, 14);
        api.Controls.Add(BuatParagraf(
            "API key dibutuhkan untuk fitur Penjelasan AI dan Chat AI. " +
            "Tanpa API key, semua fitur lain tetap berjalan normal.", isiKiri));

        TextBox txtKey = new TextBox
        {
            Width = isiKiri - 130,
            Font = Tema.Huruf(11),
            UseSystemPasswordChar = true,
            Text = StatusAplikasi.ApiKey,
            PlaceholderText = "Tempel API key dari Google AI Studio",
            BackColor = Tema.Latar,
            Margin = new Padding(0, 8, 0, 0)
        };
        CheckBox chkTampil = new CheckBox
        {
            Text = "Tampilkan",
            AutoSize = true,
            ForeColor = Tema.Teks,
            Margin = new Padding(0, 12, 0, 0)
        };
        chkTampil.CheckedChanged += (s, e) => txtKey.UseSystemPasswordChar = !chkTampil.Checked;
        api.Controls.Add(BuatBaris(0, BuatKotakIsian(txtKey), chkTampil));

        Label lblStatus = BuatLabel("", 9.5f, false, Tema.TeksRedup, isiKiri);
        lblStatus.Margin = new Padding(0, 10, 0, 6);
        AturStatus(lblStatus, StatusAplikasi.AiAktif
                ? "API key tersimpan untuk sesi ini."
                : "Belum ada API key — fitur AI berjalan dalam mode demo.",
            StatusAplikasi.AiAktif ? Tema.HijauTua : Tema.TeksRedup);

        Button simpan = BuatTombolIkon("Simpan", "\uE74E", JenisTombol.Utama);
        simpan.Click += (s, e) =>
        {
            StatusAplikasi.SimpanApiKey(txtKey.Text);
            AturStatus(lblStatus, StatusAplikasi.AiAktif
                ? "API key disimpan untuk sesi ini."
                : "API key dikosongkan. Fitur AI kembali ke mode demo.", Tema.HijauTua);
        };

        Button uji = BuatTombolIkon("Uji Koneksi", "\uE895");
        uji.Click += async (s, e) =>
        {
            uji.Enabled = false;   // cegah klik ganda selama permintaan berjalan (PRD §11.6)
            await UjiKoneksi(txtKey.Text, lblStatus);
            uji.Enabled = true;
        };

        api.Controls.Add(BuatBaris(10, simpan, uji));
        api.Controls.Add(lblStatus);
        api.Controls.Add(BuatCatatan(
            "API key yang diketik di sini hanya disimpan di memori (field private AsistenGemini) selama aplikasi " +
            "terbuka dan tidak pernah ditulis ke log. Agar tidak perlu mengetik ulang, isi Gemini:ApiKey di " +
            "appsettings.local.json (file ini tidak ikut di-commit).", isiKiri));
        kiri.Controls.Add(api);

        // --- Database ---
        FlowLayoutPanel db = BuatPanel("Database PostgreSQL", "\uE8F1", lebarKiri);
        db.Margin = new Padding(0, 0, 0, 14);
        db.Controls.Add(BuatParagraf(
            "Daftar wilayah dan riwayat chat disimpan di PostgreSQL. Connection string diatur di " +
            "appsettings.local.json (ConnectionStrings:Nadi) dan dibaca saat aplikasi dibuka.", isiKiri));

        Label lblDatabase = BuatLabel("", 9.5f, false, Tema.TeksRedup, isiKiri);
        lblDatabase.Margin = new Padding(0, 10, 0, 6);
        AturStatus(lblDatabase, StatusAplikasi.KeteranganDatabase,
            StatusAplikasi.Database == null ? Tema.OranyeTua : Tema.HijauTua);

        Button ujiDatabase = BuatTombolIkon("Uji Koneksi Database", "\uE895");
        ujiDatabase.Click += (s, e) => UjiKoneksiDatabase(lblDatabase);

        db.Controls.Add(BuatBaris(8, ujiDatabase));
        db.Controls.Add(lblDatabase);
        kiri.Controls.Add(db);

        // --- Data & riwayat ---
        FlowLayoutPanel data = BuatPanel("Data & Riwayat", "\uE81C", lebarKiri);
        Wilayah? wilayah = StatusAplikasi.WilayahAktif;
        data.Controls.Add(BuatParagraf(wilayah == null
            ? "Belum ada wilayah yang dipilih."
            : $"Wilayah aktif: {wilayah.NamaLengkap}.", isiKiri));

        Button ganti = BuatTombolIkon("Ganti Wilayah", "\uE707");
        ganti.Click += (s, e) => Pindah<DashboardForm>();

        Button hapusChat = BuatTombolBahaya("Hapus Semua Riwayat Chat", 230, 38);
        ((TombolNadi)hapusChat).Ikon = "\uE74D";
        hapusChat.Click += (s, e) =>
        {
            if (!DialogNadi.Konfirmasi(this, "Hapus riwayat chat untuk semua wilayah?", "Hapus Riwayat",
                    "Semua percakapan di semua wilayah akan dihapus dan tidak bisa dikembalikan."))
                return;

            StatusAplikasi.HapusSemuaRiwayatChat();
            DialogNadi.Info(this, "Riwayat chat telah dihapus.", "Informasi NADI");
        };
        data.Controls.Add(BuatBaris(8, ganti, hapusChat));
        data.Controls.Add(BuatCatatan("Menghapus semua riwayat chat tidak bisa dibatalkan.", isiKiri));
        kiri.Controls.Add(data);

        // --- Tentang + Disclaimer (kanan) ---
        FlowLayoutPanel tentang = BuatPanel("Tentang NADI", "\uE946", lebarKanan);
        tentang.Margin = new Padding(0, 0, 0, 14);
        tentang.Controls.Add(BuatParagraf(
            "NADI (Nusantara Climate Data & Insight) membantu membaca data iklim historis Indonesia, " +
            "memahami artinya, dan memulai aksi iklim. Data iklim diambil dari Open-Meteo (reanalisis ERA5) untuk 30 tahun kalender penuh terakhir, dengan baseline normal 1991–2020.", isiKanan));
        tentang.Controls.Add(BuatCatatan("Versi 1.0.0 · Windows", isiKanan));
        kanan.Controls.Add(tentang);

        FlowLayoutPanel disclaimer = BuatPanel("Disclaimer", "\uE7BA", lebarKanan, Tema.KuningMuda, Tema.KuningGaris);
        Label teksDisclaimer = BuatParagraf(Disclaimer, isiKanan);
        teksDisclaimer.ForeColor = Color.FromArgb(107, 69, 0);
        disclaimer.Controls.Add(teksDisclaimer);
        kanan.Controls.Add(disclaimer);

        kolom.Controls.Add(BuatDuaKolom(kiri, kanan));
    }

    // Memeriksa bentuk API key, lalu mengirim satu permintaan kecil ke Gemini.
    private static async Task UjiKoneksi(string apiKey, Label lblStatus)
    {
        apiKey = apiKey.Trim();

        if (apiKey.Length == 0)
            AturStatus(lblStatus, "Fitur AI belum aktif — masukkan API key terlebih dahulu.", Tema.OranyeTua);
        else if (apiKey.Length < 30 || apiKey.Contains(' '))
            AturStatus(lblStatus, "Format API key tampaknya tidak valid. Periksa kembali key dari Google AI Studio.",
                Tema.OranyeTua);
        else
        {
            AturStatus(lblStatus, "Menghubungi Gemini…", Tema.TeksRedup);
            var (berhasil, pesan) = await new AsistenGemini(apiKey).UjiKoneksiAsync();
            AturStatus(lblStatus, pesan, berhasil ? Tema.HijauTua : Tema.OranyeTua);
        }
    }

    private static void UjiKoneksiDatabase(Label lblStatus)
    {
        if (StatusAplikasi.Database is not { } database)
        {
            AturStatus(lblStatus, StatusAplikasi.KeteranganDatabase, Tema.OranyeTua);
            return;
        }

        try
        {
            AturStatus(lblStatus, database.Periksa(), Tema.HijauTua);
        }
        catch (NpgsqlException ex)
        {
            AturStatus(lblStatus, "Koneksi ke PostgreSQL gagal: " + ex.Message, Tema.OranyeTua);
        }
    }

    // Status tampil sebagai kotak berwarna: hijau = berhasil, merah = gagal/peringatan.
    private static void AturStatus(Label lbl, string teks, Color warna)
    {
        lbl.Text = teks;
        lbl.ForeColor = warna == Tema.OranyeTua ? Tema.Merah : warna;
        lbl.BackColor = warna == Tema.HijauTua ? Tema.HijauMuda
            : warna == Tema.OranyeTua ? Tema.MerahMuda
            : Color.Transparent;
        lbl.Padding = lbl.BackColor == Color.Transparent ? new Padding(0) : new Padding(10, 7, 10, 7);
    }
}
