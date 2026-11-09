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

        Label info = BuatCatatan(
            $"{daftar.Count} rekomendasi dipilih otomatis oleh aturan tetap dari angka tren {profil.Periode} " +
            "— bukan dikarang oleh AI. Urutan berdasarkan tingkat dampak.");
        info.Margin = new Padding(0, 2, 0, 14);
        kolom.Controls.Add(info);

        foreach (KartuAksi k in daftar)
            kolom.Controls.Add(BuatKartuAksi(k));

        Button insight = BuatTombol("Lihat Climate Insight", 200, 40);
        insight.Click += (s, e) => Pindah<InsightForm>();

        Button chat = BuatTombolSekunder("Tanyakan di Chat AI", 180, 40);
        chat.Click += (s, e) => Pindah<ChatForm>();

        Button dashboard = BuatTombolSekunder("Kembali ke Dashboard", 200, 40);
        dashboard.Click += (s, e) => Pindah<DashboardForm>();

        kolom.Controls.Add(BuatBaris(8, insight, chat, dashboard));
    }

    private static Control BuatKartuAksi(KartuAksi k)
    {
        Color aksen = k.Dampak switch
        {
            TingkatDampak.Tinggi => Tema.Oranye,
            TingkatDampak.Sedang => Tema.Teal,
            _ => Tema.TeksRedup,
        };

        FlowLayoutPanel kartu = BuatKotak(aksen);
        kartu.Controls.Add(BuatLabel(
            $"{k.Kategori.ToUpperInvariant()}   ·   DAMPAK {k.Dampak.ToString().ToUpperInvariant()}",
            8.5f, true, Tema.TeksRedup));
        kartu.Controls.Add(BuatLabel(k.Judul, 13, true));
        kartu.Controls.Add(BuatParagraf(k.Deskripsi, LebarKonten - 50));

        Label alasan = BuatLabel(k.Alasan, 9.5f, false, Tema.Navy, LebarKonten - 50);
        alasan.Font = Tema.Huruf(9.5f, FontStyle.Italic);
        alasan.Margin = new Padding(0, 4, 0, 0);
        kartu.Controls.Add(alasan);

        return kartu;
    }
}
