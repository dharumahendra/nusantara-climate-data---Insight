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

// Pengganti sementara Gemini (mode demo). Jawaban disusun dari template + angka
// profil dan mengikuti aturan prompt PRD §11: hanya memakai angka yang ada,
// tidak memprediksi masa depan, dan tidak memberi peringatan bencana.
public static class AsistenDemo
{
    public static string Jelaskan(ProfilIklim p, Indikator i)
    {
        HasilTren t = p.Tren(i);
        string kota = p.Wilayah.Kota;
        string nama = Format.NamaIndikator(i).ToLowerInvariant();

        string kalimat1 = $"Berdasarkan data {kota} periode {p.Periode}, {nama} tahunan rata-rata tercatat " +
                          $"{Format.Nilai(i, p.RataRata(i))}.";
        string kalimat2 = t.Arah == "Stabil"
            ? $"Sepanjang periode itu perubahannya hanya sekitar {Format.Perubahan(i, t.PerubahanTotal)}, " +
              "sehingga kondisinya tergolong relatif stabil."
            : $"Garis tren menunjukkan {nama} cenderung {Narasi.ArahKata(t)} sekitar " +
              $"{Format.Nilai(i, Math.Abs(t.SlopePerDekade))} per dekade, atau " +
              $"{Format.Nilai(i, Math.Abs(t.PerubahanTotal))} sepanjang periode tersebut.";
        string kalimat3 = t.Keyakinan switch
        {
            "Lemah" => "Namun, naik-turun data dari tahun ke tahun cukup besar sehingga polanya belum jelas " +
                       "dan belum bisa disimpulkan dengan tegas.",
            "Sedang" => "Pola ini cukup terlihat, meskipun tidak selalu konsisten setiap tahun.",
            _ => "Pola ini terlihat cukup konsisten dari tahun ke tahun.",
        };

        return string.Join(" ", kalimat1, kalimat2, kalimat3, DampakSehariHari(i, t, kota));
    }

    public static string Jawab(ProfilIklim p, string pertanyaan)
    {
        string q = pertanyaan.ToLowerInvariant();
        string kota = p.Wilayah.Kota;

        Wilayah? lain = DataIklim.SemuaWilayah.FirstOrDefault(
            w => !w.Equals(p.Wilayah) && q.Contains(w.Kota.ToLowerInvariant()));
        if (lain != null)
            return $"Maaf, data {lain.Kota} belum dimuat di percakapan ini, jadi aku tidak bisa menyebut angkanya. " +
                   $"Pilih {lain.Kota} lewat menu Dashboard Explore, lalu tanyakan lagi di sini ya.";

        if (Memuat(q, "prediksi", "ramal", "besok", "tahun depan", "masa depan", "akan datang"))
            return $"NADI hanya menjelaskan data {kota} periode {p.Periode} yang sudah terjadi, jadi aku tidak bisa " +
                   "memprediksi cuaca atau iklim ke depan. Untuk prakiraan resmi, silakan cek BMKG.";

        if (Memuat(q, "banjir", "longsor", "gempa", "bencana", "badai", "peringatan", "tsunami"))
        {
            HasilTren lebat = p.Tren(Indikator.HariHujanLebat);
            string data = lebat.Arah == "Naik"
                ? $" Dari data {p.Periode}, hari hujan lebat di {kota} cenderung bertambah " +
                  $"{Format.Nilai(Indikator.HariHujanLebat, lebat.PerubahanTotal)}."
                : "";
            return "NADI bukan sumber peringatan dini bencana. Untuk informasi dan peringatan resmi, " +
                   "rujuk BMKG atau BPBD setempat." + data;
        }

        if (Memuat(q, "lakukan", "aksi", "saran", "rekomendasi", "bisa saya", "harus apa"))
        {
            List<KartuAksi> aksi = MesinRekomendasi.Buat(p);
            return $"Untuk {kota}, aksi yang paling relevan adalah \"{aksi[0].Judul}\" dan \"{aksi[1].Judul}\". " +
                   $"{aksi[0].Alasan} Daftar lengkapnya ada di halaman Aksi Iklim.";
        }

        if (Memuat(q, "hujan", "basah", "kering", "kemarau", "air"))
        {
            HasilTren t = p.Tren(Indikator.CurahHujan);
            string arah = t.Arah == "Stabil"
                ? $"relatif stabil (perubahan {Format.Perubahan(Indikator.CurahHujan, t.PerubahanTotal)})"
                : $"cenderung {Narasi.ArahKata(t)} {Format.Nilai(Indikator.CurahHujan, Math.Abs(t.PerubahanTotal))}";
            return $"Curah hujan {kota} pada {p.Periode} rata-rata " +
                   $"{Format.Nilai(Indikator.CurahHujan, p.RataRata(Indikator.CurahHujan))} per tahun, dengan sekitar " +
                   $"{Format.Nilai(Indikator.HariHujan, p.RataRata(Indikator.HariHujan))} hujan per tahun. " +
                   $"Sepanjang periode itu, curah hujannya {arah}." + Ragu(t);
        }

        if (Memuat(q, "panas", "suhu", "gerah", "hangat", "dingin", "sejuk", "terik"))
        {
            HasilTren suhu = p.Tren(Indikator.SuhuRataRata);
            HasilTren panas = p.Tren(Indikator.HariPanas);
            string kalimat1 = suhu.Arah == "Stabil"
                ? $"Menurut data {p.Periode}, suhu rata-rata {kota} relatif stabil, dengan perubahan hanya " +
                  $"{Format.Perubahan(Indikator.SuhuRataRata, suhu.PerubahanTotal)}."
                : $"Menurut data {p.Periode}, suhu rata-rata {kota} cenderung {Narasi.ArahKata(suhu)} " +
                  $"{Format.Nilai(Indikator.SuhuRataRata, Math.Abs(suhu.PerubahanTotal))}, atau sekitar " +
                  $"{Format.Nilai(Indikator.SuhuRataRata, Math.Abs(suhu.SlopePerDekade))} per dekade.";
            string kalimat2 = panas.Arah == "Stabil"
                ? " Jumlah hari panas per tahun relatif stabil."
                : $" Jumlah hari panas cenderung {Narasi.ArahKata(panas)} " +
                  $"{Format.Nilai(Indikator.HariPanas, Math.Abs(panas.PerubahanTotal))} selama periode yang sama.";
            return kalimat1 + kalimat2 + Ragu(suhu);
        }

        if (Memuat(q, "iklim", "cuaca", "lingkungan", "kondisi", "ringkas"))
            return Narasi.RingkasanDashboard(p);

        if (Memuat(q, "halo", "hai", "selamat", "terima kasih", "makasih"))
            return $"Halo! Aku asisten iklim NADI. Kamu bisa bertanya tentang tren suhu, pola hujan, " +
                   $"atau aksi iklim untuk {kota} ({p.Periode}).";

        return "Maaf, aku hanya bisa membahas iklim, cuaca, lingkungan, dan aksi iklim. " +
               $"Mau tahu tren suhu atau pola hujan di {kota}?";
    }

    private static string DampakSehariHari(Indikator i, HasilTren t, string kota)
    {
        if (t.BelumJelas)
            return $"Bagi warga {kota}, data ini menjadi pengingat untuk tetap memperhatikan kondisi iklim di sekitarnya.";

        if (Format.AdalahSuhu(i))
            return t.Arah switch
            {
                "Naik" => $"Bagi warga {kota}, ini berarti hari-hari terasa lebih gerah dan kebutuhan pendingin ruangan cenderung bertambah.",
                "Turun" => $"Bagi warga {kota}, ini berarti udara cenderung sedikit lebih sejuk dibanding awal periode.",
                _ => $"Bagi warga {kota}, suhu sehari-hari relatif mirip dengan kondisi beberapa dekade lalu.",
            };

        if (i == Indikator.CurahHujan)
            return t.Arah switch
            {
                "Naik" => $"Bagi warga {kota}, ini berarti saluran air dan potensi genangan perlu lebih diperhatikan saat musim hujan.",
                "Turun" => $"Bagi warga {kota}, kebiasaan menghemat dan menampung air menjadi makin penting.",
                _ => $"Bagi warga {kota}, pola kebutuhan air dan musim tanam relatif tidak banyak berubah.",
            };

        return t.Arah == "Naik"
            ? $"Bagi warga {kota}, ini berarti makin banyak hari terik yang perlu diantisipasi saat beraktivitas di luar ruangan."
            : $"Bagi warga {kota}, jumlah hari terik dalam setahun tidak banyak bertambah.";
    }

    private static string Ragu(HasilTren t) =>
        t.BelumJelas ? " Namun polanya masih belum jelas, jadi belum bisa disimpulkan dengan tegas." : "";

    private static bool Memuat(string teks, params string[] kata) => kata.Any(teks.Contains);
}
