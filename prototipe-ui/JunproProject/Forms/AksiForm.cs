namespace Junpro_Project;

// Kartu rekomendasi aksi iklim (PRD F-06), dari rule engine MesinRekomendasi.
public sealed class AksiForm : FormDasar
{
    public AksiForm() : base("Aksi Iklim")
    {
    }

    protected override void BangunKonten(FlowLayoutPanel kolom)
    {
        kolom.Controls.Add(BuatLabel("Aksi Iklim", 24, true));

        if (!CobaAmbilProfil(kolom,
                "Rekomendasi aksi disusun dari kondisi iklim suatu wilayah. Pilih wilayah dulu di Dashboard Explore.",
                out ProfilIklim? profil))
            return;

        List<KartuAksi> daftar = MesinRekomendasi.Buat(profil);

        kolom.Controls.Add(BuatLabel(
            $"Rekomendasi aksi untuk {profil.Wilayah.NamaLengkap}", 11, false, Tema.Teks));

        FlowLayoutPanel info = BuatKotak();
        info.Padding = new Padding(16, 10, 16, 8);
        info.Margin = new Padding(0, 8, 0, 12);
        info.Controls.Add(BuatJudulIkon(
            $"{daftar.Count} rekomendasi dipilih otomatis oleh aturan tetap dari angka tren {profil.Periode} " +
            "— bukan dikarang oleh AI. Urutan berdasarkan tingkat dampak.", "\uE8FD", 9.5f, LebarKonten - 70));
        kolom.Controls.Add(info);

        foreach (KartuAksi k in daftar)
            kolom.Controls.Add(BuatKartuAksi(k));

        Button insight = BuatTombolGelap("Lihat Climate Insight", "\uE9D2");
        insight.Click += (s, e) => Pindah<InsightForm>();

        Button chat = BuatTombolIkon("Tanyakan di Chat AI", "\uE8BD");
        chat.Click += (s, e) => Pindah<ChatForm>();

        Button dashboard = BuatTombolIkon("Kembali ke Dashboard", "\uE128");
        dashboard.Click += (s, e) => Pindah<DashboardForm>();

        kolom.Controls.Add(BuatBaris(8, insight, chat, dashboard));
    }

    private static Control BuatKartuAksi(KartuAksi k)
    {
        (Color latarTag, Color teksTag) = k.Dampak switch
        {
            TingkatDampak.Tinggi => (Color.FromArgb(255, 236, 222), Tema.OranyeTua),
            TingkatDampak.Sedang => (Tema.KuningMuda, Color.FromArgb(138, 90, 0)),
            _ => (Tema.HijauMuda, Tema.HijauTua),
        };
        string kategori = k.Kategori.ToUpperInvariant();
        (string ikon, Color latarIkon, Color warnaIkon) =
            kategori.Contains("AIR") ? ("\uE753", Tema.TealMuda, Tema.TealTeks)
            : kategori.Contains("ENERGI") ? ("\uE945", Tema.HijauMuda, Tema.HijauTua)
            : kategori.Contains("SIAP") ? ("\uE7BA", Color.FromArgb(255, 236, 222), Tema.OranyeTua)
            : ("\uE9D9", Tema.TealMuda, Tema.TealTeks);

        FlowLayoutPanel kartu = BuatKotak();
        FlowLayoutPanel baris = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Margin = new Padding(0) };
        baris.Controls.Add(new ChipIkon(ikon, latarIkon, warnaIkon, 36) { Margin = new Padding(0, 2, 14, 0) });

        int lebarIsi = LebarKonten - 42 - 50;
        FlowLayoutPanel isi = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Margin = new Padding(0) };
        FlowLayoutPanel atas = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Margin = new Padding(0, 0, 0, 4) };
        Label kat = BuatLabel(kategori, 8.5f, true, Tema.Teks);
        kat.Margin = new Padding(0, 3, 10, 0);
        Label tag = BuatLabel("DAMPAK " + k.Dampak.ToString().ToUpperInvariant(), 8, true, teksTag);
        tag.BackColor = latarTag;
        tag.Padding = new Padding(6, 2, 6, 2);
        atas.Controls.Add(kat);
        atas.Controls.Add(tag);
        isi.Controls.Add(atas);
        isi.Controls.Add(BuatLabel(k.Judul, 13, true));
        isi.Controls.Add(BuatParagraf(k.Deskripsi, lebarIsi));

        Label alasan = BuatLabel(k.Alasan, 9, false, Tema.Teks, lebarIsi);
        alasan.Font = Tema.Huruf(9, FontStyle.Italic);
        alasan.BackColor = Tema.Latar;
        alasan.Padding = new Padding(8, 4, 8, 4);
        alasan.Margin = new Padding(0, 4, 0, 0);
        isi.Controls.Add(alasan);

        baris.Controls.Add(isi);
        kartu.Controls.Add(baris);
        return kartu;
    }
}
