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
        ComboBox cmbA = BuatCombo(200);
        ComboBox cmbB = BuatCombo(200);
        ComboBox cmbIndikator = BuatCombo(150);
        cmbA.DropDownWidth = cmbB.DropDownWidth = 280;

        cmbA.Items.AddRange(DataIklim.SemuaWilayah);
        cmbB.Items.AddRange(DataIklim.SemuaWilayah);
        foreach (Indikator i in pilihanIndikator)
            cmbIndikator.Items.Add(Format.NamaIndikator(i));

        cmbA.SelectedItem = a;
        cmbB.SelectedItem = b;
        cmbIndikator.SelectedIndex = Array.IndexOf(pilihanIndikator, indikator);

        Button bandingkan = BuatTombol("Bandingkan", 120, 32);
        bandingkan.Margin = new Padding(0, 22, 0, 0);

        Label peringatan = BuatLabel(
            "Pilih dua wilayah yang berbeda untuk dibandingkan.", 9.5f, true, Tema.OranyeTua);
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
        filter.Controls.Add(BuatGrupField("Wilayah A", cmbA));
        filter.Controls.Add(BuatGrupField("Wilayah B", cmbB));
        filter.Controls.Add(BuatGrupField("Indikator", cmbIndikator));
        filter.Controls.Add(bandingkan);
        kolom.Controls.Add(filter);
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

        GrafikGarisPanel grafik = new GrafikGarisPanel(
            pa.Tahun,
            new[]
            {
                new SeriGrafik(a.Kota, pa.Nilai(ind), Tema.Teal, Baseline: pa.Baseline(ind)),
                new SeriGrafik(b.Kota, pb.Nilai(ind), Tema.Oranye, PutusPutus: true, Baseline: pb.Baseline(ind)),
            },
            v => Format.Nilai(ind, v),
            v => Format.Perubahan(ind, v),
            Format.PolaSumbu(ind),
            tampilkanTren: false)
        {
            Size = new Size(LebarKonten, 280),
            Margin = new Padding(0, 6, 0, 4)
        };
        kolom.Controls.Add(grafik);
        kolom.Controls.Add(BuatCatatan(
            "Garis dibedakan warna dan gaya (utuh vs putus-putus) agar tetap terbaca oleh pengguna buta warna."));

        kolom.Controls.Add(BuatJudulBagian("Tabel Perbandingan"));
        kolom.Controls.Add(BuatTabel(pa, pb));
        kolom.Controls.Add(BuatCatatan("▲ menandai nilai yang lebih tinggi pada baris tersebut."));

        Button dashboard = BuatTombolSekunder("Kembali ke Dashboard", 200, 40);
        dashboard.Click += (s, e) => Pindah<DashboardForm>();
        Button insight = BuatTombol("Lihat Climate Insight", 200, 40);
        insight.Click += (s, e) => Pindah<InsightForm>();
        kolom.Controls.Add(BuatBaris(18, insight, dashboard));
    }

    // Belum diminta / sedang diunduh / gagal: ketiganya ditangani tanpa memblokir form (PRD §13).
    private void TampilkanKeadaanMuat(FlowLayoutPanel kolom)
    {
        if (sedangMemuat)
        {
            FlowLayoutPanel kotak = BuatKotak();
            kotak.Padding = new Padding(26, 22, 26, 22);
            kotak.Controls.Add(BuatLabel("Memuat data kedua wilayah…", 14, true));
            lblProgres = BuatParagraf(teksProgres, LebarKonten - 60);
            kotak.Controls.Add(lblProgres);
            kolom.Controls.Add(kotak);
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

    private static Control BuatTabel(ProfilIklim a, ProfilIklim b)
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

        const int tinggiBaris = 38;
        TableLayoutPanel tabel = new TableLayoutPanel
        {
            ColumnCount = 3,
            RowCount = baris.Length + 1,
            Size = new Size(LebarKonten, tinggiBaris * (baris.Length + 1) + 2),
            BackColor = Tema.Garis,
            Padding = new Padding(1),
            Margin = new Padding(0, 4, 0, 6)
        };
        tabel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 278));
        tabel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
        tabel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
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
            tabel.Controls.Add(Sel(teksA + (aLebih ? "  ▲" : ""), aLebih, latar), 1, i + 1);
            tabel.Controls.Add(Sel(teksB + (bLebih ? "  ▲" : ""), bLebih, latar), 2, i + 1);
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
            Padding = new Padding(14, 0, 8, 0),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = Tema.Huruf(10, tebal ? FontStyle.Bold : FontStyle.Regular),
            ForeColor = tebal ? Tema.Navy : Tema.Teks,
            BackColor = latar
        };
    }
}
