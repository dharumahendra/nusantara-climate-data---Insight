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

        // --- API key ---
        kolom.Controls.Add(BuatJudulBagian("API Key Google Gemini"));
        kolom.Controls.Add(BuatParagraf(
            "API key dibutuhkan untuk fitur Penjelasan AI dan Chat AI. " +
            "Tanpa API key, semua fitur lain tetap berjalan normal."));

        TextBox txtKey = new TextBox
        {
            Width = 440,
            Font = Tema.Huruf(11),
            UseSystemPasswordChar = true,
            Text = StatusAplikasi.ApiKey,
            PlaceholderText = "Tempel API key dari Google AI Studio",
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
        kolom.Controls.Add(BuatBaris(0, txtKey, chkTampil));

        Label lblStatus = BuatLabel(
            StatusAplikasi.AiAktif
                ? "API key tersimpan untuk sesi ini."
                : "Belum ada API key — fitur AI berjalan dalam mode demo.",
            10, true, Tema.TeksRedup, LebarKonten);
        lblStatus.Margin = new Padding(0, 12, 0, 6);

        Button simpan = BuatTombol("Simpan", 120, 38);
        simpan.Click += (s, e) =>
        {
            StatusAplikasi.SimpanApiKey(txtKey.Text);
            AturStatus(lblStatus, StatusAplikasi.AiAktif
                ? "API key disimpan untuk sesi ini."
                : "API key dikosongkan. Fitur AI kembali ke mode demo.", Tema.HijauTua);
        };

        Button uji = BuatTombolSekunder("Uji Koneksi", 140, 38);
        uji.Click += (s, e) => UjiKoneksi(txtKey.Text, lblStatus);

        kolom.Controls.Add(BuatBaris(12, simpan, uji));
        kolom.Controls.Add(lblStatus);
        kolom.Controls.Add(BuatCatatan(
            "Pada prototipe ini API key hanya disimpan di memori selama aplikasi berjalan dan tidak pernah " +
            "ditulis ke log. Versi akhir menyimpannya di file konfigurasi lokal yang tidak ikut di-commit."));

        // --- Database ---
        kolom.Controls.Add(BuatJudulBagian("Database PostgreSQL"));
        kolom.Controls.Add(BuatParagraf(
            "Daftar wilayah dan riwayat chat disimpan di PostgreSQL. Connection string diatur di " +
            "appsettings.local.json (ConnectionStrings:Nadi) dan dibaca saat aplikasi dibuka."));

        Label lblDatabase = BuatLabel(StatusAplikasi.KeteranganDatabase, 10, true,
            StatusAplikasi.Database == null ? Tema.OranyeTua : Tema.HijauTua, LebarKonten);
        lblDatabase.Margin = new Padding(0, 12, 0, 6);

        Button ujiDatabase = BuatTombolSekunder("Uji Koneksi Database", 200, 38);
        ujiDatabase.Click += (s, e) => UjiKoneksiDatabase(lblDatabase);

        kolom.Controls.Add(BuatBaris(8, ujiDatabase));
        kolom.Controls.Add(lblDatabase);

        // --- Data & riwayat ---
        kolom.Controls.Add(BuatJudulBagian("Data & Riwayat"));
        Wilayah? wilayah = StatusAplikasi.WilayahAktif;
        kolom.Controls.Add(BuatParagraf(wilayah == null
            ? "Belum ada wilayah yang dipilih."
            : $"Wilayah aktif: {wilayah.NamaLengkap}."));

        Button ganti = BuatTombolSekunder("Ganti Wilayah", 150, 38);
        ganti.Click += (s, e) => Pindah<DashboardForm>();

        Button hapusChat = BuatTombolSekunder("Hapus Semua Riwayat Chat", 230, 38);
        hapusChat.Click += (s, e) =>
        {
            DialogResult jawab = MessageBox.Show(
                "Hapus riwayat chat untuk semua wilayah?", "Hapus Riwayat",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (jawab != DialogResult.Yes) return;

            StatusAplikasi.HapusSemuaRiwayatChat();
            MessageBox.Show("Riwayat chat telah dihapus.", "Informasi NADI",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        };
        kolom.Controls.Add(BuatBaris(8, ganti, hapusChat));

        // --- Tentang ---
        kolom.Controls.Add(BuatJudulBagian("Tentang NADI"));
        kolom.Controls.Add(BuatParagraf(
            "NADI (Nusantara Climate Data & Insight) membantu membaca data iklim historis Indonesia, " +
            "memahami artinya, dan memulai aksi iklim. Versi ini adalah prototipe antarmuka dengan data ilustrasi."));

        FlowLayoutPanel kotakDisclaimer = BuatKotak(Tema.Oranye, Tema.KuningMuda);
        kotakDisclaimer.Margin = new Padding(0, 10, 0, 0);
        kotakDisclaimer.Controls.Add(BuatLabel("Disclaimer", 10.5f, true));
        kotakDisclaimer.Controls.Add(BuatParagraf(Disclaimer, LebarKonten - 50));
        kolom.Controls.Add(kotakDisclaimer);
    }

    // Belum ada panggilan ke Gemini; yang diperiksa baru bentuk API key-nya.
    private static void UjiKoneksi(string apiKey, Label lblStatus)
    {
        apiKey = apiKey.Trim();

        if (apiKey.Length == 0)
            AturStatus(lblStatus, "Fitur AI belum aktif — masukkan API key terlebih dahulu.", Tema.OranyeTua);
        else if (apiKey.Length < 30 || apiKey.Contains(' '))
            AturStatus(lblStatus, "Format API key tampaknya tidak valid. Periksa kembali key dari Google AI Studio.",
                Tema.OranyeTua);
        else
            AturStatus(lblStatus, "Format API key valid. Uji koneksi langsung ke Gemini akan aktif " +
                                  "setelah fitur AI tersambung.", Tema.HijauTua);
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

    private static void AturStatus(Label lbl, string teks, Color warna)
    {
        lbl.Text = teks;
        lbl.ForeColor = warna;
    }
}
