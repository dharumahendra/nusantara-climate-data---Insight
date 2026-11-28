namespace Junpro_Project;

// Perbandingan dua wilayah dalam satu grafik + tabel (PRD F-03).
public sealed class PerbandinganForm : FormDasar
{
    private static readonly Indikator[] pilihanIndikator =
    {
        Indikator.SuhuRataRata, Indikator.SuhuMaksimum, Indikator.SuhuMinimum,
        Indikator.CurahHujan, Indikator.HariPanas
    };

    private Wilayah? wilayahA;
    private Wilayah? wilayahB;
    private Indikator indikator = Indikator.SuhuRataRata;
    private bool sedangMemuat;
    private string teksProgres = "";
    private string? pesanGalat;
    private Label? lblProgres;

    public PerbandinganForm() : base("Perbandingan Wilayah")
    {
    }

    protected override void BangunKonten(FlowLayoutPanel kolom)
    {
        lblProgres = null;
        kolom.Controls.Add(BuatLabel("Perbandingan Wilayah", 24, true));
        kolom.Controls.Add(BuatLabel(
            "Bandingkan kondisi iklim dua wilayah pada periode yang sama", 11, false, Tema.Teks));

        Wilayah a = wilayahA ??= StatusAplikasi.WilayahAktif ?? DataIklim.SemuaWilayah[0];
        Wilayah b = wilayahB ??= DataIklim.SemuaWilayah.First(w => w.Provinsi != a.Provinsi);

        // --- Filter ---
        ComboBox cmbA = BuatCombo(178);
        ComboBox cmbB = BuatCombo(178);
        ComboBox cmbIndikator = BuatCombo(112);
        cmbA.Font = cmbB.Font = cmbIndikator.Font = Tema.Huruf(9);
        cmbA.DropDownWidth = cmbB.DropDownWidth = 280;

        cmbA.Items.AddRange(DataIklim.SemuaWilayah);
        cmbB.Items.AddRange(DataIklim.SemuaWilayah);
        foreach (Indikator i in pilihanIndikator)
            cmbIndikator.Items.Add(Format.NamaIndikator(i));

        cmbA.SelectedItem = a;
        cmbB.SelectedItem = b;
        cmbIndikator.SelectedIndex = Array.IndexOf(pilihanIndikator, indikator);

        Button bandingkan = BuatTombolIkon("Bandingkan", "\uE8AB", JenisTombol.Utama);
        bandingkan.Margin = new Padding(0, 22, 0, 0);

        Control peringatan = BuatStatus(
            "Wilayah A dan Wilayah B sama. Pilih dua wilayah yang berbeda untuk dibandingkan — " +
            "tombol Bandingkan aktif kembali setelahnya.", 'p', LebarKonten);
        peringatan.Visible = false;

        // Wilayah A = B tidak bisa dibandingkan (AC-03.3).
        void Periksa()
        {
            bool sama = Equals(cmbA.SelectedItem, cmbB.SelectedItem);
            bandingkan.Enabled = !sama;
            peringatan.Visible = sama;
        }
        cmbA.SelectedIndexChanged += (s, e) => Periksa();
        cmbB.SelectedIndexChanged += (s, e) => Periksa();

        bandingkan.Click += (s, e) =>
        {
            wilayahA = (Wilayah)cmbA.SelectedItem!;
            wilayahB = (Wilayah)cmbB.SelectedItem!;
            indikator = pilihanIndikator[cmbIndikator.SelectedIndex];
            MuatPasangan();
        };

        FlowLayoutPanel filter = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0, 16, 0, 8)
        };
        filter.Controls.Add(BuatGrupField("Wilayah A", cmbA, Tema.Oranye));
        filter.Controls.Add(BuatGrupField("Wilayah B", cmbB, Tema.HijauGrafik));
        filter.Controls.Add(BuatGrupField("Indikator", cmbIndikator));
        filter.Controls.Add(bandingkan);
        FlowLayoutPanel kartuFilter = BuatKotak();
        kartuFilter.Controls.Add(filter);
        kolom.Controls.Add(kartuFilter);
        kolom.Controls.Add(peringatan);

        // --- Hasil ---
        if (DataIklim.CobaProfil(a) is not { } profilA || DataIklim.CobaProfil(b) is not { } profilB)
        {
            TampilkanKeadaanMuat(kolom);
            return;
        }

        // Hanya tahun yang valid di kedua wilayah yang dibandingkan (AC-03.4).
        int[] tahunSama = profilA.Tahun.Intersect(profilB.Tahun).ToArray();
        if (tahunSama.Length == 0)
        {
            kolom.Controls.Add(BuatKeadaanKosong("Tidak ada tahun yang bisa dibandingkan",
                "Kedua wilayah tidak punya tahun valid yang sama, sehingga perbandingan tidak dapat ditampilkan."));
            return;
        }
        ProfilIklim pa = profilA.Batasi(tahunSama);
        ProfilIklim pb = profilB.Batasi(tahunSama);
        Indikator ind = indikator;

        kolom.Controls.Add(BuatJudulBagian($"{Format.NamaIndikator(ind)}: {a.Kota} vs {b.Kota}"));
        kolom.Controls.Add(BuatCatatan(
            $"Periode {pa.Periode} — hanya tahun yang valid di kedua wilayah ({tahunSama.Length} tahun). " +
            "Sumber: ERA5 via Open-Meteo."));

        const int lebarKiri = 430;
        FlowLayoutPanel kiri = BuatPanel(Format.NamaIndikator(ind) + " tahunan", null, lebarKiri);
        kiri.Controls.Add(new GrafikGarisPanel(
            pa.Tahun,
            new[]
            {
                new SeriGrafik(a.Kota, pa.Nilai(ind), Tema.Oranye, Baseline: pa.Baseline(ind)),
                new SeriGrafik(b.Kota, pb.Nilai(ind), Tema.HijauGrafik, PutusPutus: true, Baseline: pb.Baseline(ind)),
            },
            v => Format.Nilai(ind, v),
            v => Format.Perubahan(ind, v),
            Format.PolaSumbu(ind),
            tampilkanTren: false)
        {
            Size = new Size(lebarKiri - 36, 260),
            Margin = new Padding(0, 0, 0, 6)
        });
        kiri.Controls.Add(BuatCatatan(
            "Garis dibedakan warna dan gaya (utuh vs putus-putus) agar tetap terbaca oleh pengguna buta warna.", lebarKiri - 40));

        int lebarKanan = LebarKonten - lebarKiri - 14;
        FlowLayoutPanel kanan = BuatPanel("Tabel Perbandingan", null, lebarKanan);
        kanan.Controls.Add(BuatTabel(pa, pb, lebarKanan - 34));
        kanan.Controls.Add(BuatCatatan("▲ menandai nilai yang lebih tinggi pada baris tersebut.", lebarKanan - 36));
        kolom.Controls.Add(BuatDuaKolom(kiri, kanan));

        Button dashboard = BuatTombolIkon("Kembali ke Dashboard", "\uE128");
        dashboard.Click += (s, e) => Pindah<DashboardForm>();
        Button insight = BuatTombolGelap("Lihat Climate Insight", "\uE9D2");
        insight.Click += (s, e) => Pindah<InsightForm>();
        kolom.Controls.Add(BuatBaris(4, insight, dashboard));
    }

    // Belum diminta / sedang diunduh / gagal: ketiganya ditangani tanpa memblokir form (PRD §13).
    private void TampilkanKeadaanMuat(FlowLayoutPanel kolom)
    {
        if (sedangMemuat)
        {
            kolom.Controls.Add(BuatKeadaanMemuat("Memuat data kedua wilayah…", teksProgres,
                "Data wilayah yang belum pernah dibuka diunduh lebih dulu dari Open-Meteo.", out Label progres));
            lblProgres = progres;
        }
        else if (pesanGalat != null)
        {
            kolom.Controls.Add(BuatKeadaanKosong("Perbandingan belum dapat ditampilkan", pesanGalat,
                "Coba Lagi", () => MuatPasangan()));
        }
        else
        {
            kolom.Controls.Add(BuatKeadaanKosong("Pilih dua wilayah untuk dibandingkan",
                "Tentukan Wilayah A, Wilayah B, dan indikator di atas, lalu tekan Bandingkan. " +
                "Data wilayah yang belum pernah dibuka akan diunduh lebih dulu."));
        }
    }

    private async void MuatPasangan()
    {
        if (sedangMemuat || wilayahA is not { } a || wilayahB is not { } b) return;

        pesanGalat = null;

        if (DataIklim.SudahDimuat(a) && DataIklim.SudahDimuat(b))
        {
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

            // Kedua wilayah diunduh bersamaan; yang sudah tersimpan langsung selesai.
            await Task.WhenAll(DataIklim.MuatAsync(a, progres), DataIklim.MuatAsync(b, progres));
        }
        catch (SumberDataException ex)
        {
            pesanGalat = ex.PesanPengguna;
        }
        catch (Exception ex)
        {
            pesanGalat = "Terjadi kesalahan saat memuat data: " + ex.Message;
        }

        sedangMemuat = false;
        if (!IsDisposed) BangunUlang();
    }

    private static Control BuatTabel(ProfilIklim a, ProfilIklim b, int lebar)
    {
        HasilTren suhuA = a.Tren(Indikator.SuhuRataRata), suhuB = b.Tren(Indikator.SuhuRataRata);
        HasilTren panasA = a.Tren(Indikator.HariPanas), panasB = b.Tren(Indikator.HariPanas);

        var baris = new (string Nama, Indikator Satuan, double A, double B, bool Perubahan)[]
        {
            ("Suhu rata-rata", Indikator.SuhuRataRata,
                a.RataRata(Indikator.SuhuRataRata), b.RataRata(Indikator.SuhuRataRata), false),
            ($"Tren suhu ({a.Periode})", Indikator.SuhuRataRata,
                suhuA.PerubahanTotal, suhuB.PerubahanTotal, true),
            ("Curah hujan tahunan", Indikator.CurahHujan,
                a.RataRata(Indikator.CurahHujan), b.RataRata(Indikator.CurahHujan), false),
            ("Hari hujan per tahun", Indikator.HariHujan,
                a.RataRata(Indikator.HariHujan), b.RataRata(Indikator.HariHujan), false),
            ($"Hari panas ({a.TahunAkhir})", Indikator.HariPanas,
                a.Terakhir(Indikator.HariPanas), b.Terakhir(Indikator.HariPanas), false),
            ("Tren hari panas", Indikator.HariPanas,
                panasA.PerubahanTotal, panasB.PerubahanTotal, true),
        };

        const int tinggiBaris = 40;
        TableLayoutPanel tabel = new TableLayoutPanel
        {
            ColumnCount = 3,
            RowCount = baris.Length + 1,
            Size = new Size(lebar, tinggiBaris * (baris.Length + 1) + 2),
            BackColor = Tema.Garis,
            Padding = new Padding(1),
            Margin = new Padding(0, 4, 0, 6)
        };
        int kolomNilai = (lebar - 2) * 31 / 100;
        tabel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, lebar - 2 - kolomNilai * 2));
        tabel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, kolomNilai));
        tabel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, kolomNilai));
        for (int r = 0; r <= baris.Length; r++)
            tabel.RowStyles.Add(new RowStyle(SizeType.Absolute, tinggiBaris));

        tabel.Controls.Add(Sel("Indikator", true, Tema.TealMuda), 0, 0);
        tabel.Controls.Add(Sel(a.Wilayah.Kota, true, Tema.TealMuda), 1, 0);
        tabel.Controls.Add(Sel(b.Wilayah.Kota, true, Tema.TealMuda), 2, 0);

        for (int i = 0; i < baris.Length; i++)
        {
            var (nama, satuan, nilaiA, nilaiB, perubahan) = baris[i];
            string teksA = perubahan ? Format.Perubahan(satuan, nilaiA) : Format.Nilai(satuan, nilaiA);
            string teksB = perubahan ? Format.Perubahan(satuan, nilaiB) : Format.Nilai(satuan, nilaiB);

            // Bandingkan teks yang tampil, agar "27,4 °C" vs "27,4 °C" tidak dianggap berbeda.
            bool aLebih = teksA != teksB && nilaiA > nilaiB;
            bool bLebih = teksA != teksB && nilaiB > nilaiA;
            Color latar = i % 2 == 0 ? Tema.Putih : Tema.Latar;

            tabel.Controls.Add(Sel(nama, false, latar), 0, i + 1);
            tabel.Controls.Add(Sel(teksA + (aLebih ? " ▲" : ""), aLebih, latar), 1, i + 1);
            tabel.Controls.Add(Sel(teksB + (bLebih ? " ▲" : ""), bLebih, latar), 2, i + 1);
        }

        return tabel;
    }

    private static Label Sel(string teks, bool tebal, Color latar)
    {
        return new Label
        {
            Text = teks,
            Dock = DockStyle.Fill,
            Margin = new Padding(0),
            Padding = new Padding(6, 0, 2, 0),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = Tema.Huruf(8.5f, tebal ? FontStyle.Bold : FontStyle.Regular),
            ForeColor = tebal ? Tema.Navy : Tema.Teks,
            BackColor = latar
        };
    }
}
