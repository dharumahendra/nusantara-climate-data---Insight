namespace Junpro_Project;

public static class MesinRekomendasi
{
    private const int MaksKartu = 5;
    private const int MinKartu = 3;

    private static readonly AturanAksi[] aturanSpesifik =
    {
        new AturanSuhuNaikTajam(),
        new AturanSuhuNaik(),
        new AturanHariPanasBertambah(),
        new AturanCurahHujanMenurun(),
        new AturanCurahHujanTinggi(),
        new AturanCurahHujanRendah(),
        new AturanHujanLebatBertambah(),
    };

    private static readonly AturanAksi[] aturanDasar =
    {
        new AturanAksiDasar("Energi", "Hemat listrik di rumah",
            "Matikan perangkat yang tidak dipakai, cabut pengisi daya, dan pilih lampu LED."),
        new AturanAksiDasar("Transportasi", "Pilih transportasi rendah emisi",
            "Gunakan transportasi publik, bersepeda, atau berjalan kaki untuk perjalanan dekat."),
        new AturanAksiDasar("Vegetasi", "Olah sisa makanan jadi kompos",
            "Kurangi sampah makanan dan jadikan sisa organik kompos untuk tanaman di rumah."),
    };

    public static List<KartuAksi> Buat(ProfilIklim profil)
    {
        List<KartuAksi> hasil = aturanSpesifik
            .Where(aturan => aturan.BerlakuUntuk(profil))
            .OrderByDescending(aturan => aturan.Dampak)
            .Take(MaksKartu)
            .Select(aturan => aturan.BuatKartu(profil))
            .ToList();

        int kekurangan = Math.Max(0, MinKartu - hasil.Count);
        hasil.AddRange(aturanDasar
            .Where(aturan => aturan.BerlakuUntuk(profil))
            .Take(kekurangan)
            .Select(aturan => aturan.BuatKartu(profil)));

        return hasil;
    }
}
