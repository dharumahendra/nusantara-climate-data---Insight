namespace Junpro_Project;

// Halaman utama (desain praktik "Dashboard Explore"): pilih wilayah, lihat
// kartu ringkasan, lalu lanjut ke halaman lain. Data diambil dari Open-Meteo
// atau dibaca dari database lokal bila wilayah itu pernah diunduh.
public sealed class DashboardForm : FormDasar
{
    private const int JumlahBatang = 7;

    private string provinsiDipilih = DataIklim.DaftarProvinsi().First();
    private string kotaDipilih = "";
    private bool sedangMemuat;
    private string teksProgres = "";
    private string? pesanGalat;
    private Label? lblProgres;
    private ComboBox? cmbProvinsi;
    private ComboBox? cmbKota;

    public DashboardForm() : base("Dashboard Explore")
    {
        // Pilihan terakhir dipulihkan saat aplikasi dibuka lagi (PRD F-01).
        if (PilihanTerakhir.Baca() is { } terakhir)
        {
            provinsiDipilih = terakhir.Provinsi;
            kotaDipilih = terakhir.Kota;
        }
    }

    protected override void BangunKonten(FlowLayoutPanel kolom)
    {
        lblProgres = null;

        kolom.Controls.Add(BuatLabel("Dashboard Explore", 24, true));
        kolom.Controls.Add(BuatLabel("Ringkasan kondisi iklim Indonesia", 11, false, Tema.Teks));
        kolom.Controls.Add(BuatFilter());

        if (sedangMemuat)
        {
            FlowLayoutPanel kotak = BuatKotak();
            kotak.Padding = new Padding(26, 22, 26, 22);
            kotak.Controls.Add(BuatLabel("Memuat data iklim…", 14, true));
            lblProgres = BuatParagraf(teksProgres, LebarKonten - 60);
            kotak.Controls.Add(lblProgres);
            kotak.Controls.Add(BuatCatatan(
                "Unduhan pertama untuk satu wilayah memerlukan beberapa detik. Setelah itu datanya " +
                "tersimpan dan bisa dibuka lagi tanpa internet.", LebarKonten - 60));
            kolom.Controls.Add(kotak);
            return;
        }

        if (pesanGalat != null)
        {
            kolom.Controls.Add(BuatKeadaanKosong("Data belum dapat dimuat", pesanGalat,
                "Coba Lagi", () => MulaiMemuat()));
            return;
        }

        if (StatusAplikasi.Profil is not { } profil)
        {
            kolom.Controls.Add(BuatKeadaanKosong("Pilih wilayah untuk mulai",
                "Cari atau tentukan provinsi dan kota di atas, lalu tekan Tampilkan Data untuk melihat " +
                "ringkasan iklim wilayah tersebut."));
            return;
        }

        Label judulWilayah = BuatLabel(profil.Wilayah.NamaLengkap, 16, true);
        judulWilayah.Margin = new Padding(0, 0, 0, 2);
        kolom.Controls.Add(judulWilayah);
        kolom.Controls.Add(BuatCatatan($"Periode data {profil.Periode}  ·  sumber: ERA5 via Open-Meteo"));
        if (profil.TahunTidakValid.Length > 0)
            kolom.Controls.Add(BuatCatatan(
                "Dikecualikan dari analisis (data kurang dari 330 hari): " +
                string.Join(", ", profil.TahunTidakValid) + ".", LebarKonten));

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
            $"{profil.TahunAkhir} · ≥ {Format.Angka(profil.AmbangHariPanas, "0.0")} °C"));
        kartu.Controls.Add(BuatKartu("PERUBAHAN SUHU",
            profil.TrenTersedia ? Format.Perubahan(Indikator.SuhuRataRata, trenSuhu.PerubahanTotal) : "—",
            !profil.TrenTersedia ? "data belum cukup"
                : trenSuhu.BelumJelas ? "tren belum jelas" : "tren " + profil.Periode));
        kolom.Controls.Add(kartu);

        // --- Grafik batang perubahan suhu ---
        if (profil.TrenTersedia)
        {
            kolom.Controls.Add(BuatJudulBagian("Perubahan Suhu Sejak " + profil.TahunAwal));

            int[] tahunGrafik = PilihTahun(profil.Tahun, JumlahBatang);
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
        }
        else
        {
            kolom.Controls.Add(BuatCatatan(
                $"Data belum cukup untuk analisis tren: hanya {profil.Tahun.Length} dari " +
                $"{profil.JumlahTahunPeriode} tahun yang valid (minimal {ProfilIklim.MinTahunUntukTren})."));
        }

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

    // Memilih beberapa tahun yang tersebar merata, termasuk tahun pertama dan terakhir.
    private static int[] PilihTahun(int[] tahun, int jumlah)
    {
        if (tahun.Length <= jumlah) return tahun;
        return Enumerable.Range(0, jumlah)
            .Select(k => tahun[(int)Math.Round(k * (tahun.Length - 1) / (double)(jumlah - 1))])
            .Distinct()
            .ToArray();
    }

    private Control BuatFilter()
    {
        FlowLayoutPanel filter = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Margin = new Padding(0, 14, 0, 16)
        };

        // --- Pencarian cepat dengan autocomplete (pola Lab 10.1) ---
        TextBox txtCari = new TextBox
        {
            Width = 440,
            Font = Tema.Huruf(10),
            PlaceholderText = "Ketik nama kota atau kabupaten…",
            AutoCompleteMode = AutoCompleteMode.SuggestAppend,
            AutoCompleteSource = AutoCompleteSource.CustomSource,
            AutoCompleteCustomSource = BuatDaftarKota(),
            Margin = new Padding(0)
        };
        txtCari.TextChanged += (s, e) =>
        {
            if (DataIklim.CariKota(txtCari.Text) is { } cocok) PilihDiCombo(cocok);
        };
        txtCari.KeyDown += (s, e) =>
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;
            if (DataIklim.CariKota(txtCari.Text) is { } cocok) PilihDiCombo(cocok);
            MulaiMemuat();
        };
        filter.Controls.Add(BuatGrupField("Cari Kota / Kabupaten", txtCari));

        // --- Pilihan provinsi dan kota ---
        cmbProvinsi = BuatCombo(210);
        cmbKota = BuatCombo(210);
        ComboBox provinsi = cmbProvinsi, kota = cmbKota;

        provinsi.Items.AddRange(DataIklim.DaftarProvinsi().Cast<object>().ToArray());
        provinsi.SelectedIndexChanged += (s, e) =>
        {
            // Mengosongkan daftar kota memicu SelectedIndexChanged yang menimpa
            // kotaDipilih, jadi pilihan yang diminta diamankan lebih dulu.
            string dipertahankan = kotaDipilih;
            provinsiDipilih = provinsi.Text;
            kota.Items.Clear();
            foreach (Wilayah w in DataIklim.WilayahDi(provinsiDipilih))
                kota.Items.Add(w.Kota);

            int idx = kota.Items.IndexOf(dipertahankan);
            kota.SelectedIndex = idx >= 0 ? idx : 0;
        };
        kota.SelectedIndexChanged += (s, e) =>
        {
            if (kota.SelectedIndex >= 0) kotaDipilih = kota.Text;
        };
        provinsi.SelectedItem = provinsiDipilih;

        Button tampilkan = BuatTombol("Tampilkan Data", 160, 32);
        tampilkan.Margin = new Padding(0, 22, 0, 0);
        tampilkan.Enabled = !sedangMemuat;
        tampilkan.Click += (s, e) => MulaiMemuat();

        FlowLayoutPanel baris = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0, 6, 0, 0)
        };
        baris.Controls.Add(BuatGrupField("Pilih Provinsi", provinsi));
        baris.Controls.Add(BuatGrupField("Pilih Kota", kota));
        baris.Controls.Add(tampilkan);
        filter.Controls.Add(baris);
        return filter;
    }

    private static AutoCompleteStringCollection BuatDaftarKota()
    {
        AutoCompleteStringCollection daftar = new AutoCompleteStringCollection();
        daftar.AddRange(DataIklim.SemuaWilayah.Select(w => w.Kota).ToArray());
        return daftar;
    }

    private void PilihDiCombo(Wilayah wilayah)
    {
        if (cmbProvinsi == null || cmbKota == null) return;

        kotaDipilih = wilayah.Kota;
        if (cmbProvinsi.Text == wilayah.Provinsi) cmbKota.SelectedItem = wilayah.Kota;
        else cmbProvinsi.SelectedItem = wilayah.Provinsi;
    }

    private async void MulaiMemuat()
    {
        if (sedangMemuat) return;
        if (DataIklim.Cari(provinsiDipilih, kotaDipilih) is not { } wilayah) return;

        pesanGalat = null;

        // Data yang sudah ada di memori langsung tampil tanpa pemuatan ulang (AC-01.2).
        if (DataIklim.SudahDimuat(wilayah))
        {
            StatusAplikasi.PilihWilayah(wilayah);
            BangunUlang();
            return;
        }

        sedangMemuat = true;
        teksProgres = "Memulai…";
        BangunUlang();

        try
        {
            Progress<string> progres = new(teks =>
            {
                teksProgres = teks;
                if (lblProgres is { IsDisposed: false }) lblProgres.Text = teks;
            });
            await DataIklim.MuatAsync(wilayah, progres);
            StatusAplikasi.PilihWilayah(wilayah);
        }
        catch (SumberDataException ex)
        {
            pesanGalat = ex.PesanPengguna;
        }
        catch (Exception ex)
        {
            // Jaring pengaman: kesalahan tak terduga tidak boleh membuat aplikasi crash (PRD §15).
            pesanGalat = "Terjadi kesalahan saat memuat data wilayah ini: " + ex.Message;
        }

        sedangMemuat = false;
        if (!IsDisposed) BangunUlang();
    }
}
