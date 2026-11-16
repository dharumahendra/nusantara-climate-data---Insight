namespace Junpro_Project;

// Kalimat ringkasan yang disusun langsung dari angka profil (bukan AI).
public static class Narasi
{
    public static string ArahKata(HasilTren t) => t.Arah switch
    {
        "Naik" => "naik",
        "Turun" => "turun",
        _ => "relatif stabil",
    };

    public static string RingkasanDashboard(ProfilIklim p)
    {
        HasilTren suhu = p.Tren(Indikator.SuhuRataRata);
        HasilTren hujan = p.Tren(Indikator.CurahHujan);

        return $"Selama {p.Periode}, suhu rata-rata {p.Wilayah.Kota} adalah " +
               $"{Format.Nilai(Indikator.SuhuRataRata, p.RataRata(Indikator.SuhuRataRata))} dengan perubahan " +
               $"menurut garis tren sebesar {Format.Perubahan(Indikator.SuhuRataRata, suhu.PerubahanTotal)}. " +
               $"Curah hujan rata-rata {Format.Nilai(Indikator.CurahHujan, p.RataRata(Indikator.CurahHujan))} " +
               $"per tahun dan trennya {ArahKata(hujan)}. " +
               "Buka Climate Insight untuk melihat grafik lengkap beserta penjelasannya.";
    }

    public static string TemuanUtama(ProfilIklim p, Indikator i)
    {
        HasilTren t = p.Tren(i);
        string nama = Format.NamaIndikator(i).ToLowerInvariant();
        var (tahunMaks, maks) = p.Tertinggi(i);
        var (tahunMin, min) = p.Terendah(i);

        string kalimat1 = t.Arah == "Stabil"
            ? $"Selama {p.Periode}, {nama} di {p.Wilayah.Kota} relatif stabil dengan perubahan total " +
              $"{Format.Perubahan(i, t.PerubahanTotal)}."
            : $"Selama {p.Periode}, {nama} di {p.Wilayah.Kota} cenderung {ArahKata(t)} dengan perubahan total " +
              $"{Format.Perubahan(i, t.PerubahanTotal)} ({Format.Perubahan(i, t.SlopePerDekade)} per dekade).";
        string kalimat2 = $"Nilai tertinggi tercatat pada {tahunMaks} ({Format.Nilai(i, maks)}) " +
                          $"dan terendah pada {tahunMin} ({Format.Nilai(i, min)}).";
        string kalimat3 = $"Keyakinan tren tergolong {t.Keyakinan.ToLowerInvariant()} " +
                          $"(R² = {Format.Angka(t.RSquared, "0.00")})" +
                          (t.BelumJelas ? ", sehingga tren ini belum jelas dan perlu dibaca hati-hati." : ".");

        return string.Join(" ", kalimat1, kalimat2, kalimat3);
    }
}
