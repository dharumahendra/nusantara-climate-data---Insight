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

        kolom.Controls.Add(BuatCatatan(
            $"Analisis Tren {Format.NamaIndikator(ind)} Historis ({profil.Periode})  ·  " +
            profil.Wilayah.NamaLengkap + "  ·  sumber: ERA5 via Open-Meteo"));
        kolom.Controls.Add(BuatPemilihIndikator());

        // --- Grafik (kiri) + kartu tren bertumpuk (kanan) ---
        const int lebarKiri = 530;
        FlowLayoutPanel kiri = BuatPanel(Format.NamaIndikator(ind) + " tahunan", null, lebarKiri);
        kiri.Controls.Add(new GrafikGarisPanel(
            profil.Tahun,
            new[] { new SeriGrafik(profil.Wilayah.Kota, profil.Nilai(ind), Tema.Oranye, Baseline: profil.Baseline(ind)) },
            v => Format.Nilai(ind, v),
            v => Format.Perubahan(ind, v),
            Format.PolaSumbu(ind),
            tampilkanTren: profil.TrenTersedia)
        {
            Size = new Size(lebarKiri - 36, 250),
            Margin = new Padding(0, 0, 0, 6)
        });
        int lebarCatatan = lebarKiri - 40;
        kiri.Controls.Add(BuatCatatan(
            (profil.TrenTersedia ? "Garis putus-putus gelap = garis tren (regresi linear). " : "") +
            "Arahkan kursor ke grafik untuk melihat nilai dan anomali tiap tahun " +
            "terhadap rata-rata normal 1991–2020.", lebarCatatan));
        if (ind == Indikator.HariPanas)
            kiri.Controls.Add(BuatCatatan(
                $"Hari panas = hari dengan suhu maksimum ≥ {Format.Angka(profil.AmbangHariPanas, "0.0")} °C, " +
                "yaitu persentil ke-90 suhu maksimum harian wilayah ini pada 1991–2020.", lebarCatatan));
        if (profil.TahunTidakValid.Length > 0)
            kiri.Controls.Add(BuatCatatan(
                "Tahun dikecualikan (data kurang dari 330 hari): " + string.Join(", ", profil.TahunTidakValid) + ".", lebarCatatan));

        int lebarKanan = LebarKonten - lebarKiri - 14;
        FlowLayoutPanel kanan = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            Margin = new Padding(0)
        };
        if (profil.TrenTersedia)
        {
            kanan.Controls.Add(KartuTren("ARAH TREN", tren.Arah,
                tren.BelumJelas ? "tren belum jelas" : "periode " + profil.Periode, Color.FromArgb(255, 240, 228), lebarKanan));
            kanan.Controls.Add(KartuTren("TREN PER DEKADE",
                Format.Perubahan(ind, tren.SlopePerDekade), "setiap 10 tahun", Tema.KuningMuda, lebarKanan));
            kanan.Controls.Add(KartuTren("PERUBAHAN TOTAL",
                Format.Perubahan(ind, tren.PerubahanTotal), $"dalam {profil.TahunAkhir - profil.TahunAwal} tahun",
                Tema.HijauMuda, lebarKanan));
            Panel keyakinan = KartuTren("KEYAKINAN TREN", tren.Keyakinan,
                "R² = " + Format.Angka(tren.RSquared, "0.00"), Tema.Putih, lebarKanan);
            int tingkat = tren.Keyakinan.StartsWith("Kuat") ? 3 : tren.Keyakinan.StartsWith("Sedang") ? 2 : 1;
            keyakinan.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                for (int k = 0; k < 3; k++)
                {
                    using SolidBrush b = new SolidBrush(k < tingkat ? Tema.TealTeks : Tema.Garis);
                    using var jalur = Tema.Bulat(new RectangleF(lebarKanan - 70 + k * 18, 32, 14, 5), 2.5f);
                    e.Graphics.FillPath(b, jalur);
                }
            };
            kanan.Controls.Add(keyakinan);
        }
        else
        {
            kanan.Controls.Add(KartuTren("TREN", "—", "data belum cukup", Tema.Putih, lebarKanan));
        }
        kolom.Controls.Add(BuatDuaKolom(kiri, kanan));

        // --- Temuan Utama (kiri) + Penjelasan AI (kanan) ---
        int lebarSetengah = (LebarKonten - 14) / 2;
        FlowLayoutPanel temuan = BuatPanel("Temuan Utama", "\uE8A5", lebarSetengah, Tema.KuningMuda, Tema.KuningGaris);
        temuan.Controls.Add(BuatParagraf(profil.TrenTersedia
            ? Narasi.TemuanUtama(profil, ind)
            : $"Data belum cukup untuk analisis tren: hanya {profil.Tahun.Length} dari " +
              $"{profil.JumlahTahunPeriode} tahun yang valid (minimal {ProfilIklim.MinTahunUntukTren}).", lebarSetengah - 36));
        temuan.Controls.Add(BuatCatatan("Dihitung oleh aplikasi dari data ERA5 via Open-Meteo.", lebarSetengah - 36));

        FlowLayoutPanel ai = BuatPanel("Penjelasan AI", "\uE9D9", lebarSetengah, Tema.TealMuda, Color.FromArgb(196, 230, 232));
        AsistenIklim asisten = StatusAplikasi.Asisten;
        ai.Controls.Add(BuatCatatan("Asisten aktif: " + asisten.Nama, lebarSetengah - 36));
        if (!asisten.MemakaiAi)
        {
            ai.Controls.Add(BuatStatus("Fitur AI belum aktif — masukkan API key di Pengaturan. " +
                "Sementara itu, penjelasan di sini berupa contoh dari template NADI.", 'p', lebarSetengah - 36));
            Button pengaturan = BuatTombolIkon("Buka Pengaturan", "\uE713");
            pengaturan.Margin = new Padding(0, 4, 0, 8);
            pengaturan.Click += (s, e) => Pindah<PengaturanForm>();
            ai.Controls.Add(pengaturan);
        }
        lebarAi = lebarSetengah - 36;
        wadahAi = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Margin = new Padding(0)
        };
        ai.Controls.Add(wadahAi);
        IsiWadahAi(profil);
        kolom.Controls.Add(BuatDuaKolom(temuan, ai));

        Button aksi = BuatTombolGelap("Lihat Rekomendasi Aksi", "\uEA80");
        aksi.Click += (s, e) => Pindah<AksiForm>();

        Button chat = BuatTombolIkon("Tanya Chat AI", "\uE8BD");
        chat.Click += (s, e) => Pindah<ChatForm>();

        Button banding = BuatTombolIkon("Bandingkan Wilayah", "\uE8AB");
        banding.Click += (s, e) => Pindah<PerbandinganForm>();

        kolom.Controls.Add(BuatBaris(4, aksi, chat, banding));

        Label catatan = BuatCatatan(
            "Catatan: data iklim bersumber dari reanalisis ERA5 melalui Open-Meteo dan dapat berbeda dari " +
            "pengamatan stasiun cuaca resmi. Untuk informasi resmi, rujuk BMKG.");
        catatan.Margin = new Padding(0, 16, 0, 0);
        kolom.Controls.Add(catatan);
    }

    private int lebarAi = LebarKonten;

    // Kartu tren bertumpuk di kanan grafik, berlatar lembut sesuai desain.
    private static Panel KartuTren(string judul, string nilai, string keterangan, Color latar, int lebar)
    {
        Panel kartu = new Panel { Size = new Size(lebar, 72), BackColor = latar, Margin = new Padding(0, 0, 0, 8) };
        BeriBingkai(kartu, latar == Tema.Putih ? null : latar, 12);
        Label j = BuatLabel(judul, 8, true, Tema.Teks);
        j.Location = new Point(14, 8);
        Label n = BuatLabel(nilai, 14, true, Tema.Navy, lebar - 20);
        n.Location = new Point(12, 24);
        Label k = BuatLabel(keterangan, 8.5f, false, Tema.TeksRedup, lebar - 20);
        k.Location = new Point(14, 51);
        kartu.Controls.Add(j);
        kartu.Controls.Add(n);
        kartu.Controls.Add(k);
        return kartu;
    }

    private Control BuatPemilihIndikator()
    {
        FlowLayoutPanel baris = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0, 10, 0, 12),
            BackColor = Tema.Putih,
            Padding = new Padding(6)
        };
        BeriBingkai(baris, null, 12);

        foreach (Indikator i in pilihanIndikator)
        {
            bool aktif = i == indikator;
            Button b = BuatTombolIkon(Format.NamaIndikator(i), IkonIndikator(i),
                aktif ? JenisTombol.Utama : JenisTombol.Sekunder);
            b.Size = new Size(60, 32);
            if (!aktif) ((TombolNadi)b).TanpaBingkai = true;
            b.AutoSize = true;
            b.Padding = new Padding(8, 0, 8, 0);
            b.Margin = new Padding(0, 0, 4, 0);

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

    private static string IkonIndikator(Indikator i) => i switch
    {
        Indikator.CurahHujan => "\uE753",
        Indikator.HariPanas => "\uE706",
        _ => "\uE9CA",
    };

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
            FlowLayoutPanel kotak = wadahAi;
            kotak.Controls.Add(BuatParagraf(hasil.Teks, lebarAi));

            Label sumber = BuatCatatan(
                (hasil.DariAi ? "Penjelasan dibuat AI" : "Penjelasan disusun") +
                " berdasarkan angka yang dihitung aplikasi. Angka bersumber dari " +
                "ERA5/Open-Meteo. Disusun oleh: " + hasil.Sumber +
                (hasil.DariAi ? "." : " — teks disusun dari template NADI, bukan oleh model AI."), lebarAi);
            sumber.Margin = new Padding(0, 8, 0, 0);
            kotak.Controls.Add(sumber);
        }
        else
        {
            bool menunggu = permintaanAi is { } p && p.Wilayah == profil.Wilayah && p.Indikator == indikator;
            Button jelaskan = BuatTombolIkon(menunggu ? "Sedang menyusun penjelasan…" : "Jelaskan data ini dengan AI",
                "\uE9D9", JenisTombol.Utama);
            jelaskan.Enabled = permintaanAi == null;
            jelaskan.Margin = new Padding(0, 2, 0, 6);
            jelaskan.Click += (s, e) => MintaPenjelasan(profil);
            wadahAi.Controls.Add(jelaskan);

            wadahAi.Controls.Add(BuatCatatan(menunggu
                ? "Biasanya memerlukan 5–30 detik. Anda tetap bisa melihat grafik sambil menunggu."
                : "Tekan tombol di atas untuk meminta penjelasan. AI hanya berjalan saat diminta.", lebarAi));
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

        // Sumber diambil dari penjelasan yang baru selesai, bukan dari nama asisten: bila Gemini
        // gagal dan jawaban jatuh ke mode demo, label "Disusun oleh" harus menyebut mode demo.
        penjelasanAi[(p.Wilayah, p.Indikator)] =
            (teks, asisten.SumberPenjelasanTerakhir, asisten.PenjelasanTerakhirDariAi);
        permintaanAi = null;

        if (StatusAplikasi.Profil is { } profil)
            IsiWadahAi(profil);
    }
}
