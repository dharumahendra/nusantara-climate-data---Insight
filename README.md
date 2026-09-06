# NADI (Nusantara Climate Data & Insight)

**KELOMPOK 4**

Ketua Kelompok: Muhammad Zakiyyuddin Abdul Adhiim - 24/545668/TK/60719  
Anggota 1: Dharu Bintang Mahendratama - 24/535960/TK/59484   
Anggota 2: Razaqi Alkautsar - 24/544958/TK/60570

## Deskripsi
NADI adalah aplikasi desktop Windows yang mengubah data iklim historis Indonesia menjadi grafik dan penjelasan bahasa manusia, lalu menerjemahkannya jadi rekomendasi aksi iklim yang bisa dilakukan pengguna. Aplikasi ini terinspirasi dari proyek open-source ClimateExplorer, tetapi dikembangkan dengan fokus pada wilayah Indonesia dan aspek Climate Action.

Dokumen spesifikasi lengkap (scope, rumus, kontrak data, acceptance criteria) ada di [`docs/PRD.md`](docs/PRD.md) itu adalah satu-satunya sumber kebenaran untuk keputusan produk dan teknis. Progres pengerjaan saat ini dicatat di [`PROGRESS.md`](PROGRESS.md).

## Fitur utama

| Tahap | Fitur |
|---|---|
| **Explore** | Pilih wilayah (provinsi/kota) dan lihat kondisi iklimnya |
| **Understand** | Grafik tren historis 30 tahun + penjelasan naratif berbasis AI |
| **Act** | Rekomendasi aksi iklim yang relevan dengan kondisi wilayah tersebut |

## Tumpukan teknologi

- **Platform:** Windows Desktop, .NET 8 + WPF (MVVM dengan CommunityToolkit.Mvvm)
- **Grafik:** LiveCharts2
- **Database lokal:** SQLite via EF Core
- **Sumber data iklim:** [Open-Meteo Historical Weather API](https://open-meteo.com/) (reanalisis ERA5)
- **AI narasi:** Google Gemini API

## Struktur proyek

```text
NADI/
├── src/
│   ├── NADI.App/             # WPF UI — Views, ViewModels, entry point (composition root)
│   ├── NADI.Core/            # Model, interface, dan logika bisnis (tanpa dependensi eksternal)
│   ├── NADI.Infrastructure/  # Akses data: Open-Meteo provider, EF Core + SQLite
│   └── NADI.AI/              # Klien Gemini + prompt template
├── tests/
│   └── NADI.Tests/           # Unit test (xUnit)
└── docs/
    ├── PRD.md                # Product Requirements Document
    ├── diagrams/
    └── screenshots/
```

Aturan ketergantungan antar-project mengikuti `NADI.App / NADI.Infrastructure / NADI.AI → NADI.Core` (lihat PRD §7). Detail lengkap struktur ada di PRD §14.

## Prasyarat

- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022 (17.8+) dengan workload **.NET desktop development**, atau `dotnet` CLI + editor pilihan Anda
- Koneksi internet (untuk unduhan data iklim pertama kali dan pemanggilan AI)
- (Opsional untuk fitur AI) API key Google Gemini — lihat [Konfigurasi API key](#konfigurasi-api-key-fitur-ai)

## Cara menjalankan

1. **Clone repository**

   ```bash
   git clone https://github.com/dharumahendra/nusantara-climate-data---Insight.git
   cd nusantara-climate-data---Insight
   ```

2. **Restore dependencies**

   ```bash
   dotnet restore
   ```

3. **Build solution**

   ```bash
   dotnet build
   ```

4. **Jalankan aplikasi**

   ```bash
   dotnet run --project src/NADI.App
   ```

   Atau buka `NADI.sln` di Visual Studio, set `NADI.App` sebagai *Startup Project*, lalu tekan `F5`.

5. **Jalankan unit test**

   ```bash
   dotnet test
   ```

## Konfigurasi API key (fitur AI)

Fitur AI Climate Explainer (F-04) dan AI Climate Chat (F-05) membutuhkan API key Gemini. Tanpa API key, seluruh fitur lain tetap berjalan normal.

1. Salin `appsettings.example.json` menjadi `appsettings.local.json` (file ini sudah masuk `.gitignore` — **jangan pernah commit API key**).
2. Isi `Gemini.ApiKey` dengan API key dari [Google AI Studio](https://aistudio.google.com/).
3. Buka aplikasi → halaman **Pengaturan** → masukkan API key → tekan **Uji koneksi**.

## Database

Data disimpan lokal di `%LOCALAPPDATA%/NADI/nadi.db` (SQLite) setelah unduhan pertama per wilayah, sehingga aplikasi tetap bisa dipakai offline untuk wilayah yang datanya sudah pernah diunduh. Migrasi dikelola dengan `dotnet ef migrations`.

## Disclaimer

> NADI adalah aplikasi edukasi. Data iklim bersumber dari reanalisis ERA5 melalui Open-Meteo dan dapat berbeda dari pengamatan stasiun cuaca resmi. Penjelasan naratif dihasilkan oleh model bahasa AI berdasarkan statistik yang dihitung aplikasi, dan dapat mengandung kekeliruan. NADI bukan sumber peringatan dini bencana. Untuk informasi cuaca dan peringatan resmi, rujuk BMKG.
