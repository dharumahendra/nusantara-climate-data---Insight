# Progress Pengerjaan NADI

**Terakhir diperbarui:** 2026-11-28

Dokumen ini mencatat progres pengerjaan proyek secara ringkas. Untuk detail scope, rumus, dan acceptance criteria, lihat [`docs/PRD.md`](docs/PRD.md). Cara memakai aplikasi ada di [`docs/PANDUAN.md`](docs/PANDUAN.md).

## Ringkasan status

Aplikasi NADI **sudah berjalan** sebagai aplikasi desktop Windows (WinForms, .NET 10) dengan data iklim nyata dari Open-Meteo, penyimpanan di PostgreSQL, dan penjelasan AI lewat Google Gemini (dengan mode demo bila AI tidak tersedia). Aplikasi dikemas menjadi satu berkas `NADI.exe` mandiri, tanpa perlu memasang .NET.

Aplikasi yang berjalan berada di [`prototipe-ui/`](prototipe-ui/). Folder `src/` berisi rancangan berlapis (Core, Infrastructure, AI, App) yang dibuat untuk class diagram dan skema database; folder itu belum menjadi aplikasi yang dijalankan.

## Status fitur

| ID | Fitur | Prioritas | Status | Halaman |
|---|---|---|---|---|
| F-01 | Climate Location Explorer | P0 | ✅ Selesai | Dashboard Explore |
| F-02 | Historical Climate Visualization | P0 | ✅ Selesai | Climate Insight |
| F-03 | Regional Climate Comparison | P1 | ✅ Selesai | Perbandingan Wilayah |
| F-04 | AI Climate Explainer | P0 ⭐ | ✅ Selesai | Climate Insight |
| F-05 | AI Climate Chat | P1 ⭐ | ✅ Selesai | Chat AI |
| F-06 | Climate Action Recommendation | P0 ⭐ | ✅ Selesai | Aksi Iklim |
| F-07 | Climate Dashboard | P0 | ✅ Selesai | Dashboard Explore |

## Yang sudah dikerjakan

- **Data**: klien Open-Meteo (ERA5), rumus tren dan baseline, data harian disimpan di PostgreSQL.
- **AI**: Gemini dengan urutan model cadangan dan pencarian model otomatis, batas waktu 15 detik per model dan 30 detik total, serta mode demo yang jujur menyebut sumber jawabannya.
- **UI**: enam halaman, pemuatan data asinkron, keadaan loading/kosong/error, kontras warna ≥ 4,5:1, layar 125% dan 150%, dialog konfirmasi berbahasa Indonesia, dan ikon aplikasi.
- **Paket**: `prototipe-ui/buat-paket.ps1` menghasilkan `NADI.exe` mandiri; `siapkan-database.bat` menyiapkan database lokal.
- **Pengujian**: 129 unit test (xUnit), worksheet black box di [`docs/pengujian-black-box.md`](docs/pengujian-black-box.md), dan uji manual M-01 sampai M-07 (semua Sesuai).

## Belum / sedang dikerjakan

| Pekerjaan | Status | Catatan |
|---|---|---|
| Database bukan lokal (cloud) | ✅ Selesai | PostgreSQL 18 di AWS RDS dengan user terbatas `nadi_app`. Paket dibuat dengan `buat-paket.ps1 -KonfigDemo`. |
| Slide presentasi (PPT) | 🔲 Belum | Maksimal 10 menit. |
| Video demo | 🔲 Belum | Maksimal 3 menit, hanya demo aplikasi. |
| ERD dan Use Case Diagram sebagai gambar | 🔲 Belum | ERD baru berupa kode Mermaid di PRD §9. Class diagram sudah ada di README. |
| Laporan uji manual masuk ke repo | 🔲 Belum | `LAPORAN-UJI-MANUAL.md` dan `pengujian-manual-dharu.md` belum di-commit. |

## Catatan terbuka

- **Font**: aplikasi memakai Segoe UI (bawaan Windows), sedangkan PRD §13 menyebut Poppins dan Inter yang di-embed. Font belum di-embed; Segoe UI tersedia di semua Windows sehingga tampilan tetap konsisten.
- **Ukuran huruf isi** 10pt (sekitar 13px), sedikit di bawah target PRD ≥ 14px. Terbaca nyaman di layar 125% dan 150%.
- **Format angka** di penjelasan AI memakai titik desimal ("1.14 °C"), sedangkan kartu aplikasi memakai koma ("+1,1 °C"). Angkanya sama.
- **Peringatan Windows** "Unknown Publisher" muncul saat `NADI.exe` dari zip pertama kali dibuka (berkas belum ditandatangani).
- **Wawancara pengguna** (PRD §2) tidak tercatat di repo.
- **Open Questions PRD §21** belum dijawab secara tertulis.
