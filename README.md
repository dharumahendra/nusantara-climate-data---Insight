# NADI (Nusantara Climate Data & Insight)

**KELOMPOK 4**

Ketua Kelompok: Muhammad Zakiyyuddin Abdul Adhiim - 24/545668/TK/60719  
Anggota 1: Dharu Bintang Mahendratama - 24/535960/TK/59484   
Anggota 2: Razaqi Alkautsar - 24/544958/TK/60570

## Deskripsi
NADI adalah aplikasi desktop Windows yang mengubah data iklim historis Indonesia menjadi grafik dan penjelasan bahasa manusia, lalu menerjemahkannya jadi rekomendasi aksi iklim yang bisa dilakukan pengguna. Aplikasi ini terinspirasi dari proyek open-source ClimateExplorer, tetapi dikembangkan dengan fokus pada wilayah Indonesia dan aspek Climate Action.

Dokumen spesifikasi lengkap (scope, rumus, kontrak data, acceptance criteria) ada di [`docs/PRD.md`](docs/PRD.md). Progres pengerjaan dicatat di [`PROGRESS.md`](PROGRESS.md), dan cara memakai aplikasi ada di [`docs/PANDUAN.md`](docs/PANDUAN.md).

## Fitur utama

| Tahap | Fitur | Halaman |
|---|---|---|
| **Explore** | Pilih wilayah (provinsi/kota) dan lihat kondisi iklimnya | Dashboard Explore |
| **Understand** | Grafik tren historis 30 tahun + penjelasan naratif berbasis AI | Climate Insight |
| **Understand** | Bandingkan dua wilayah | Perbandingan Wilayah |
| **Understand** | Tanya jawab seputar iklim wilayah aktif | Chat AI |
| **Act** | Rekomendasi aksi iklim yang relevan dengan kondisi wilayah tersebut | Aksi Iklim |

Fitur AI memakai Google Gemini. Bila API key belum diisi atau Gemini tidak dapat dipakai, aplikasi memakai mode demo dan menyebutkannya dengan jelas pada jawaban.

## Tumpukan teknologi

- **Platform:** aplikasi desktop Windows, C# dengan .NET 10 dan Windows Forms
- **Grafik:** digambar sendiri dengan GDI+ (`GrafikGarisPanel`, `GrafikBatangPanel`)
- **Database:** PostgreSQL lewat Npgsql
- **Sumber data iklim:** [Open-Meteo Historical Weather API](https://open-meteo.com/) (reanalisis ERA5)
- **AI narasi dan chat:** Google Gemini API (REST)
- **Pengujian:** xUnit

## Struktur proyek

```text
├── prototipe-ui/                 # Aplikasi NADI yang berjalan
│   ├── JunproProject/            # Kode aplikasi (WinForms, namespace Junpro_Project)
│   │   ├── Forms/                # Halaman: Dashboard, Insight, Perbandingan, Chat, Aksi, Pengaturan
│   │   ├── Kontrol/              # Grafik dan dialog buatan sendiri
│   │   ├── Data/                 # Klien Open-Meteo, analisis iklim, rekomendasi, asisten AI
│   │   └── Database/             # Akses PostgreSQL dan schema.sql
│   ├── JunproProject.Tests/      # Unit test (xUnit)
│   ├── paket/                    # Skrip penyiapan database untuk paket rilis
│   └── buat-paket.ps1            # Membuat NADI.exe mandiri dan berkas zip
├── src/                          # Rancangan berlapis (Core, Infrastructure, AI, App) untuk class diagram
├── tests/                        # Test untuk rancangan di src/
└── docs/                         # PRD, panduan pengguna, worksheet pengujian, tangkapan layar
```

Aplikasi yang dijalankan dan dikumpulkan adalah `prototipe-ui/JunproProject`. Folder `src/` dan `tests/` memuat rancangan arsitektur berlapis yang menjadi dasar class diagram; keduanya bukan aplikasi yang dijalankan. `NADI.sln` di root hanya memuat proyek-proyek itu.

### Penerapan OOP

Asisten AI memakai pola *template method*: kelas abstrak `AsistenIklim` menyusun alur penjelasan dan chat, sedangkan `AsistenDemo` (tanpa internet) dan `AsistenGemini` (Google Gemini) mengisi langkah yang berbeda. Kode ada di `prototipe-ui/JunproProject/Data/Asisten/`.

## Class Diagram

Berikut adalah arsitektur interaksi data dan relasi kelas dalam NADI:
<img width="8191" height="4813" alt="Climate Data Interaction-2026-08-31-074746" src="https://github.com/user-attachments/assets/d2257901-50bd-4785-88e3-152257273907" />


## Prasyarat

- Windows 10/11
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (hanya untuk membangun dari kode; paket `NADI.exe` tidak membutuhkannya)
- Visual Studio dengan workload **.NET desktop development** yang mendukung .NET 10, atau `dotnet` CLI + editor pilihan Anda
- Koneksi internet (untuk unduhan data iklim pertama kali dan pemanggilan AI)
- (Opsional) PostgreSQL untuk menyimpan riwayat chat dan data iklim secara permanen
- (Opsional untuk fitur AI) API key Google Gemini — lihat [Konfigurasi API key](#konfigurasi-api-key-fitur-ai)

## Cara menjalankan

1. **Clone repository**

   ```bash
   git clone https://github.com/dharumahendra/nusantara-climate-data---Insight.git
   cd nusantara-climate-data---Insight
   ```

2. **Jalankan aplikasi**

   ```bash
   dotnet run --project prototipe-ui/JunproProject
   ```

   Atau buka `prototipe-ui/JunproProject` di Visual Studio lalu tekan `F5`.

3. **Jalankan unit test**

   ```bash
   dotnet test prototipe-ui/JunproProject.Tests
   ```

   Test memakai respons API asli yang disimpan di `prototipe-ui/JunproProject.Tests/Fixtures`, sehingga tidak membutuhkan internet maupun database.

Tanpa database, aplikasi tetap berjalan memakai daftar wilayah bawaan; riwayat chat dan data iklim hanya disimpan di memori selama aplikasi terbuka.

## Konfigurasi API key (fitur AI)

Fitur AI Climate Explainer dan AI Climate Chat membutuhkan API key Gemini. Tanpa API key, seluruh fitur lain tetap berjalan normal dan AI memakai mode demo.

1. Salin `appsettings.example.json` menjadi `appsettings.local.json` di root repo (file ini sudah masuk `.gitignore` — **jangan pernah commit API key atau password**). Jangan mengisi key di `appsettings.example.json`: berkas itu hanya contoh dan tidak dibaca aplikasi.
2. Isi `Gemini.ApiKey` dengan API key dari [Google AI Studio](https://aistudio.google.com/).
3. Buka aplikasi → halaman **Pengaturan** → tekan **Uji Koneksi**. Key juga bisa langsung ditempel di halaman Pengaturan.

Aplikasi mencoba beberapa model Gemini secara berurutan, lalu mencari model *flash* lain yang tersedia bagi key tersebut bila semuanya gagal. Batas waktunya 15 detik per model dan 30 detik total.

## Database PostgreSQL

Aplikasi membaca daftar wilayah, menyimpan riwayat chat, dan menyimpan data iklim harian hasil unduhan Open-Meteo di PostgreSQL.

1. Buat database dan tabelnya (skrip aman dijalankan ulang):

   ```bash
   createdb -U postgres nadi
   psql -U postgres -d nadi -f prototipe-ui/JunproProject/Database/schema.sql
   ```

2. Pada `appsettings.local.json` di root repo, isi `ConnectionStrings.Nadi` dengan password PostgreSQL Anda.
3. Jalankan aplikasi, lalu buka **Pengaturan** → **Uji Koneksi Database**.

> **Database lama (Modul 8)?** Jalankan `schema.sql` sekali lagi. Skrip akan menambahkan kolom koordinat (`latitude`, `longitude`) dan tabel `data_iklim_harian`, tanpa menghapus riwayat chat yang sudah ada.

### Data iklim dari Open-Meteo

Saat pengguna menekan **Tampilkan Data**, aplikasi mengunduh data harian 1991 sampai akhir tahun kalender penuh terakhir dari [Open-Meteo](https://open-meteo.com/) (tanpa API key), menghitung statistik 30 tahun terakhir dengan baseline normal 1991–2020, lalu menyimpannya ke tabel `data_iklim_harian`. Wilayah yang sudah tersimpan dibuka dari database tanpa internet.

Open-Meteo membatasi jumlah permintaan per menit, jam, dan hari untuk pemakaian gratis. Bila batas tercapai, aplikasi menampilkan pesan agar menunggu lalu menekan **Coba Lagi**; wilayah yang sudah tersimpan tetap bisa dibuka.

## Membuat paket rilis (`NADI.exe`)

```powershell
.\prototipe-ui\buat-paket.ps1
```

Skrip ini menghasilkan folder `dist\NADI\` (satu `NADI.exe` mandiri tanpa perlu .NET, skrip `siapkan-database`, skema, contoh konfigurasi, dan panduan) serta `dist\NADI-v1.0.0-win-x64.zip`. Folder `dist/` tidak ikut di-commit, dan paket tidak pernah membawa `appsettings.local.json` milik pengembang. Panduan pemakaian untuk pengguna ada di [`docs/PANDUAN.md`](docs/PANDUAN.md).

## Pengujian

- **Unit test:** `dotnet test prototipe-ui/JunproProject.Tests` (rumus iklim, mesin rekomendasi, klien Open-Meteo, asisten Gemini, konfigurasi, kontras warna, dialog).
- **Black box:** [`docs/pengujian-black-box.md`](docs/pengujian-black-box.md).

## Disclaimer

> NADI adalah aplikasi edukasi. Data iklim bersumber dari reanalisis ERA5 melalui Open-Meteo dan dapat berbeda dari pengamatan stasiun cuaca resmi. Penjelasan naratif dihasilkan oleh model bahasa AI berdasarkan statistik yang dihitung aplikasi, dan dapat mengandung kekeliruan. NADI bukan sumber peringatan dini bencana. Untuk informasi cuaca dan peringatan resmi, rujuk BMKG.
