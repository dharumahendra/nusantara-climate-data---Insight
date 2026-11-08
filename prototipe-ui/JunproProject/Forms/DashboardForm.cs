namespace Junpro_Project;

// Halaman utama (desain praktik "Dashboard Explore"): pilih wilayah, lihat
// kartu ringkasan, lalu lanjut ke halaman lain.
public sealed class DashboardForm : FormDasar
{
    private static readonly int[] tahunGrafik = { 1995, 2000, 2005, 2010, 2015, 2020, 2024 };

    private readonly System.Windows.Forms.Timer timerMuat = new System.Windows.Forms.Timer { Interval = 800 };
    private string provinsiDipilih = DataIklim.DaftarProvinsi().First();
    private string kotaDipilih = "";
    private bool sedangMemuat;

    public DashboardForm() : base("Dashboard Explore")
    {
        // Jeda singkat meniru unduhan data pertama kali (PRD F-01).
        timerMuat.Tick += (s, e) => SelesaiMemuat();
    }

    protected override void BangunKonten(FlowLayoutPanel kolom)
    {
        kolom.Controls.Add(BuatLabel("Dashboard Explore", 24, true));
        kolom.Controls.Add(BuatLabel("Ringkasan kondisi iklim Indonesia", 11, false, Tema.Teks));
        kolom.Controls.Add(BuatFilter());

        if (sedangMemuat)
        {
            kolom.Controls.Add(BuatKeadaanKosong("Memuat data iklim…",
                $"Mengunduh data iklim {kotaDipilih} {DataIklim.TahunAwal}–{DataIklim.TahunAkhir}. " +
                "Proses ini hanya terjadi sekali; selanjutnya data dibaca dari penyimpanan lokal."));
            return;
        }

        if (StatusAplikasi.Profil is not { } profil)
        {
            kolom.Controls.Add(BuatKeadaanKosong("Pilih wilayah untuk mulai",
                "Tentukan provinsi dan kota di atas, lalu tekan Tampilkan Data untuk melihat " +
                "ringkasan iklim wilayah tersebut."));
            return;
        }

        Label judulWilayah = BuatLabel(profil.Wilayah.NamaLengkap, 16, true);
        judulWilayah.Margin = new Padding(0, 0, 0, 2);
        kolom.Controls.Add(judulWilayah);
        kolom.Controls.Add(BuatCatatan($"Periode data {profil.Periode}  ·  data ilustrasi"));

        // --- Kartu statistik ---
        HasilTren trenSuhu = profil.Tren(Indikator.SuhuRataRata);

        FlowLayoutPanel kartu = BuatBarisKartu();
        kartu.Controls.Add(BuatKartu("SUHU RATA-RATA",
            Format.Nilai(Indikator.SuhuRataRata, profil.RataRata(Indikator.SuhuRataRata)),
            "rata-rata " + profil.Periode));
        kartu.Controls.Add(BuatKartu("CURAH HUJAN",
            Format.Nilai(Indikator.CurahHujan, profil.RataRata(Indikator.CurahHujan)),
            "rata-rata per tahun"));
        kartu.Controls.Add(BuatKartu("HARI PANAS",
            Format.Nilai(Indikator.HariPanas, profil.Terakhir(Indikator.HariPanas)),
            "pada tahun " + profil.TahunAkhir));
        kartu.Controls.Add(BuatKartu("PERUBAHAN SUHU",
            Format.Perubahan(Indikator.SuhuRataRata, trenSuhu.PerubahanTotal),
            trenSuhu.BelumJelas ? "tren belum jelas" : "tren " + profil.Periode));
        kolom.Controls.Add(kartu);

        // --- Grafik batang perubahan suhu ---
        kolom.Controls.Add(BuatJudulBagian("Perubahan Suhu Sejak " + profil.TahunAwal));

        double awal = trenSuhu.NilaiPada(profil.TahunAwal);
        GrafikBatangPanel grafik = new GrafikBatangPanel(
            tahunGrafik.Select(t => t.ToString()).ToArray(),
            tahunGrafik.Select(t => trenSuhu.NilaiPada(t) - awal).ToArray(),
            Tema.Teal, Tema.Navy,
            v => (v < 0 ? "−" : "+") + Format.Angka(Math.Abs(v), "0.0") + "°")
        {
            Size = new Size(LebarKonten, 190),
            Margin = new Padding(0, 4, 0, 6)
        };
        kolom.Controls.Add(grafik);
        kolom.Controls.Add(BuatCatatan(
            $"Nilai dibaca dari garis tren suhu rata-rata tahunan, relatif terhadap tahun {profil.TahunAwal}."));

        // --- Ringkasan ---
        kolom.Controls.Add(BuatJudulBagian("Ringkasan Kondisi Iklim"));
        kolom.Controls.Add(BuatParagraf(Narasi.RingkasanDashboard(profil)));

        Button insight = BuatTombol("Buka Climate Insight", 200, 42);
        insight.Click += (s, e) => Pindah<InsightForm>();

        Button aksi = BuatTombolSekunder("Lihat Aksi Iklim", 170, 42);
        aksi.Click += (s, e) => Pindah<AksiForm>();

        Button banding = BuatTombolSekunder("Bandingkan Wilayah", 180, 42);
        banding.Click += (s, e) => Pindah<PerbandinganForm>();

        kolom.Controls.Add(BuatBaris(14, insight, aksi, banding));
    }

    private Control BuatFilter()
    {
        FlowLayoutPanel filter = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0, 16, 0, 16)
        };

        ComboBox cmbProvinsi = BuatCombo(210);
        ComboBox cmbKota = BuatCombo(210);

        cmbProvinsi.Items.AddRange(DataIklim.DaftarProvinsi().Cast<object>().ToArray());
        cmbProvinsi.SelectedIndexChanged += (s, e) =>
        {
            provinsiDipilih = cmbProvinsi.Text;
            cmbKota.Items.Clear();
            foreach (Wilayah w in DataIklim.WilayahDi(provinsiDipilih))
                cmbKota.Items.Add(w.Kota);

            int idx = cmbKota.Items.IndexOf(kotaDipilih);
            cmbKota.SelectedIndex = idx >= 0 ? idx : 0;
        };
        cmbKota.SelectedIndexChanged += (s, e) => kotaDipilih = cmbKota.Text;
        cmbProvinsi.SelectedItem = provinsiDipilih;

        Button tampilkan = BuatTombol(sedangMemuat ? "Memuat…" : "Tampilkan Data", 160, 32);
        tampilkan.Margin = new Padding(0, 22, 0, 0);
        tampilkan.Enabled = !sedangMemuat;
        tampilkan.Click += (s, e) => MulaiMemuat();

        filter.Controls.Add(BuatGrupField("Pilih Provinsi", cmbProvinsi));
        filter.Controls.Add(BuatGrupField("Pilih Kota", cmbKota));
        filter.Controls.Add(tampilkan);
        return filter;
    }

    private void MulaiMemuat()
    {
        if (DataIklim.Cari(provinsiDipilih, kotaDipilih) is not { } wilayah) return;

        // Data yang sudah pernah dimuat langsung tampil tanpa "unduh" ulang (AC-01.2).
        if (DataIklim.SudahTersimpan(wilayah))
        {
            StatusAplikasi.PilihWilayah(wilayah);
            BangunUlang();
            return;
        }

        sedangMemuat = true;
        BangunUlang();
        timerMuat.Start();
    }

    private void SelesaiMemuat()
    {
        timerMuat.Stop();
        sedangMemuat = false;

        if (DataIklim.Cari(provinsiDipilih, kotaDipilih) is { } wilayah)
        {
            DataIklim.Profil(wilayah);
            StatusAplikasi.PilihWilayah(wilayah);
        }
        BangunUlang();
    }
}
