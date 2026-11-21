namespace Junpro_Project;

// Desain praktik "Climate Insight", dilengkapi pemilih indikator, grafik
// garis + garis tren (PRD F-02), dan tombol penjelasan AI (PRD F-04).
public sealed class InsightForm : FormDasar
{
    private static readonly Indikator[] pilihanIndikator =
    {
        Indikator.SuhuRataRata, Indikator.SuhuMaksimum, Indikator.SuhuMinimum,
        Indikator.CurahHujan, Indikator.HariPanas
    };

    private readonly Dictionary<(Wilayah, Indikator), (string Teks, string Sumber, bool DariAi)> penjelasanAi = new();
    private Indikator indikator = Indikator.SuhuRataRata;
    private (Wilayah Wilayah, Indikator Indikator)? permintaanAi;
    private FlowLayoutPanel? wadahAi;

    public InsightForm() : base("Climate Insight")
    {
    }

    protected override void BangunKonten(FlowLayoutPanel kolom)
    {
        wadahAi = null;
        kolom.Controls.Add(BuatLabel("Climate Insight", 24, true));

        if (!CobaAmbilProfil(kolom,
                "Pilih wilayah di Dashboard Explore untuk melihat grafik tren iklim historisnya.",
                out ProfilIklim? profil))
            return;

        Indikator ind = indikator;
        HasilTren tren = profil.Tren(ind);

        kolom.Controls.Add(BuatLabel(
            $"Analisis Tren {Format.NamaIndikator(ind)} Historis ({profil.Periode})", 13, true, Tema.Teks));
        kolom.Controls.Add(BuatCatatan(profil.Wilayah.NamaLengkap + "  ·  sumber: ERA5 via Open-Meteo"));
        kolom.Controls.Add(BuatPemilihIndikator());

        // --- Grafik garis + garis tren ---
        GrafikGarisPanel grafik = new GrafikGarisPanel(
            profil.Tahun,
            new[] { new SeriGrafik(profil.Wilayah.Kota, profil.Nilai(ind), Tema.Teal, Baseline: profil.Baseline(ind)) },
            v => Format.Nilai(ind, v),
            v => Format.Perubahan(ind, v),
            Format.PolaSumbu(ind),
            tampilkanTren: profil.TrenTersedia)
        {
            Size = new Size(LebarKonten, 280),
            Margin = new Padding(0, 4, 0, 6)
        };
        kolom.Controls.Add(grafik);
        kolom.Controls.Add(BuatCatatan(
            (profil.TrenTersedia ? "Garis putus-putus oranye = garis tren (regresi linear). " : "") +
            "Arahkan kursor ke grafik untuk melihat nilai dan anomali tiap tahun " +
            "terhadap rata-rata normal 1991–2020."));
        if (ind == Indikator.HariPanas)
            kolom.Controls.Add(BuatCatatan(
                $"Hari panas = hari dengan suhu maksimum ≥ {Format.Angka(profil.AmbangHariPanas, "0.0")} °C, " +
                "yaitu persentil ke-90 suhu maksimum harian wilayah ini pada 1991–2020."));
        if (profil.TahunTidakValid.Length > 0)
            kolom.Controls.Add(BuatCatatan(
                "Tahun dikecualikan (data kurang dari 330 hari): " + string.Join(", ", profil.TahunTidakValid) + "."));

        if (!profil.TrenTersedia)
        {
            kolom.Controls.Add(BuatJudulBagian("Temuan Utama"));
            kolom.Controls.Add(BuatParagraf(
                $"Data belum cukup untuk analisis tren: hanya {profil.Tahun.Length} dari " +
                $"{profil.JumlahTahunPeriode} tahun yang valid (minimal {ProfilIklim.MinTahunUntukTren})."));
        }
        else
        {
            // --- Statistik tren ---
            FlowLayoutPanel kartu = BuatBarisKartu();
            kartu.Controls.Add(BuatKartu("ARAH TREN", tren.Arah,
                tren.BelumJelas ? "tren belum jelas" : "periode " + profil.Periode));
            kartu.Controls.Add(BuatKartu("TREN PER DEKADE",
                Format.Perubahan(ind, tren.SlopePerDekade), "setiap 10 tahun"));
            kartu.Controls.Add(BuatKartu("PERUBAHAN TOTAL",
                Format.Perubahan(ind, tren.PerubahanTotal), $"dalam {profil.TahunAkhir - profil.TahunAwal} tahun"));
            kartu.Controls.Add(BuatKartu("KEYAKINAN TREN", tren.Keyakinan,
                "R² = " + Format.Angka(tren.RSquared, "0.00")));
            kolom.Controls.Add(kartu);

            kolom.Controls.Add(BuatJudulBagian("Temuan Utama"));
            kolom.Controls.Add(BuatParagraf(Narasi.TemuanUtama(profil, ind)));
        }

        // --- Penjelasan AI ---
        kolom.Controls.Add(BuatJudulBagian("Penjelasan AI"));
        kolom.Controls.Add(BuatBannerDemo());

        wadahAi = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Margin = new Padding(0)
        };
        kolom.Controls.Add(wadahAi);
        IsiWadahAi(profil);

        Button aksi = BuatTombol("Lihat Rekomendasi Aksi", 210, 40);
        aksi.Click += (s, e) => Pindah<AksiForm>();

        Button chat = BuatTombolSekunder("Tanya Chat AI", 160, 40);
        chat.Click += (s, e) => Pindah<ChatForm>();

        Button banding = BuatTombolSekunder("Bandingkan Wilayah", 180, 40);
        banding.Click += (s, e) => Pindah<PerbandinganForm>();

        kolom.Controls.Add(BuatBaris(16, aksi, chat, banding));

        Label catatan = BuatCatatan(
            "Catatan: data iklim bersumber dari reanalisis ERA5 melalui Open-Meteo dan dapat berbeda dari " +
            "pengamatan stasiun cuaca resmi. Untuk informasi resmi, rujuk BMKG.");
        catatan.Margin = new Padding(0, 20, 0, 0);
        kolom.Controls.Add(catatan);
    }

    private Control BuatPemilihIndikator()
    {
        FlowLayoutPanel baris = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0, 12, 0, 10)
        };

        foreach (Indikator i in pilihanIndikator)
        {
            bool aktif = i == indikator;
            Button b = aktif
                ? BuatTombol(Format.NamaIndikator(i), 60, 32)
                : BuatTombolSekunder(Format.NamaIndikator(i), 60, 32);
            b.AutoSize = true;
            b.Padding = new Padding(8, 0, 8, 0);
            b.Margin = new Padding(0, 0, 8, 0);

            Indikator pilihan = i;
            b.Click += (s, e) =>
            {
                if (indikator == pilihan) return;
                indikator = pilihan;
                BangunUlang();
            };
            baris.Controls.Add(b);
        }
        return baris;
    }

    // Bagian penjelasan AI diperbarui di tempat (tanpa membangun ulang halaman)
    // agar posisi scroll pengguna tidak melompat ke atas.
    private void IsiWadahAi(ProfilIklim profil)
    {
        if (wadahAi == null) return;

        wadahAi.SuspendLayout();
        foreach (Control c in wadahAi.Controls.Cast<Control>().ToList())
            c.Dispose();

        var kunci = (profil.Wilayah, indikator);
        if (penjelasanAi.TryGetValue(kunci, out var hasil))
        {
            FlowLayoutPanel kotak = BuatKotak(Tema.Teal);
            kotak.Controls.Add(BuatParagraf(hasil.Teks, LebarKonten - 50));

            Label sumber = BuatCatatan(
                "Penjelasan dibuat AI berdasarkan angka yang dihitung aplikasi. Angka bersumber dari " +
                "ERA5/Open-Meteo. Disusun oleh: " + hasil.Sumber +
                (hasil.DariAi ? "." : " — teks disusun dari template NADI, bukan oleh model AI."), LebarKonten - 50);
            sumber.Margin = new Padding(0, 8, 0, 0);
            kotak.Controls.Add(sumber);
            wadahAi.Controls.Add(kotak);
        }
        else
        {
            bool menunggu = permintaanAi is { } p && p.Wilayah == profil.Wilayah && p.Indikator == indikator;
            Button jelaskan = BuatTombol(menunggu ? "Menyusun penjelasan…" : "Jelaskan data ini dengan AI", 260, 42);
            jelaskan.Enabled = permintaanAi == null;
            jelaskan.Margin = new Padding(0, 2, 0, 6);
            jelaskan.Click += (s, e) => MintaPenjelasan(profil);
            wadahAi.Controls.Add(jelaskan);

            if (menunggu)
                wadahAi.Controls.Add(BuatCatatan("Menunggu jawaban AI…"));
        }

        wadahAi.ResumeLayout();
    }

    private async void MintaPenjelasan(ProfilIklim profil)
    {
        // Tombol dinonaktifkan selama permintaan berjalan (PRD §11.6).
        if (permintaanAi != null) return;

        permintaanAi = (profil.Wilayah, indikator);
        IsiWadahAi(profil);

        // Polymorphism: JelaskanAsync() milik kelas dasar AsistenIklim memanggil
        // SusunPenjelasanAsync() versi subclass yang sedang aktif.
        AsistenIklim asisten = StatusAplikasi.Asisten;
        string teks = await asisten.JelaskanAsync(profil, indikator);
        PenjelasanSiap(teks, asisten);
    }

    private void PenjelasanSiap(string teks, AsistenIklim asisten)
    {
        if (permintaanAi is not { } p) return;

        penjelasanAi[(p.Wilayah, p.Indikator)] = (teks, asisten.Nama, asisten.MemakaiAi);
        permintaanAi = null;

        if (StatusAplikasi.Profil is { } profil)
            IsiWadahAi(profil);
    }
}
