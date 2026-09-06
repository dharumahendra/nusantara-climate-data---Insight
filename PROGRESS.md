# Progress Pengerjaan NADI

**Terakhir diperbarui:** 2026-09-06

Dokumen ini mencatat progres pengerjaan proyek secara ringkas. Untuk detail scope, rumus, dan acceptance criteria, lihat [`docs/PRD.md`](docs/PRD.md).

## Ringkasan status

Struktur solution (.NET 8, 4 project + test) dan scaffolding awal setiap layer sudah dibuat. Belum ada logika bisnis, UI, integrasi API, atau AI yang diimplementasikan, proyek belum bisa dijalankan sebagai aplikasi fungsional.

## Sprint 1 Fondasi

Exit criteria (PRD §16): *aplikasi bisa dijalankan, berpindah antar halaman kosong, database terbentuk dan berisi ~40 wilayah, semua anggota bisa build dari clean clone.*

| Tugas | Status | Catatan |
|---|---|---|
| Repo, branch strategy, `.gitignore`, README kerangka | ✅ Selesai | `.gitignore` & README lengkap dengan panduan build/run |
| Solution 4 project + referensi sesuai §7 | ✅ Selesai | `NADI.sln` + `NADI.Core/.Infrastructure/.AI/.App` + `NADI.Tests`, dibuat manual (belum divalidasi `dotnet build` — lihat Risiko) |
| DI container, navigasi sidebar, tema & resource warna/font | 🔲 Belum | Palet warna (`Resources/Colors.xaml`) sudah ada; DI container, navigasi sidebar, dan `Styles.xaml` masih kosong |
| EF Core + SQLite + migrasi awal + seed `regions.json` | 🔲 Belum | `NadiDbContext` masih kosong (belum ada `DbSet`), belum ada migrasi; `regions.json` baru berisi 2 dari ~40 wilayah target |
| 5 wawancara pengguna singkat (validasi asumsi §2) | 🔲 Belum | |

## Sprint 2–6

Belum dimulai. Urutan sesuai roadmap PRD §16: Data & Dashboard → Visualisasi → AI → Climate Action & Polish → Finalisasi.

## Status fitur

| ID | Fitur | Prioritas | Status |
|---|---|---|---|
| F-01 | Climate Location Explorer | P0 | 🔲 Belum dimulai |
| F-02 | Historical Climate Visualization | P0 | 🔲 Belum dimulai |
| F-03 | Regional Climate Comparison | P1 | 🔲 Belum dimulai |
| F-04 | AI Climate Explainer | P0 ⭐ | 🔲 Belum dimulai |
| F-05 | AI Climate Chat | P1 ⭐ | 🔲 Belum dimulai |
| F-06 | Climate Action Recommendation | P0 ⭐ | 🔲 Belum dimulai |
| F-07 | Climate Dashboard | P0 | 🔲 Belum dimulai |

## Yang sudah tersedia di repo

- Solution `.sln` + 5 csproj (`NADI.Core`, `NADI.Infrastructure`, `NADI.AI`, `NADI.App`, `NADI.Tests`) dengan referensi antar-project sesuai aturan §7.
- Stub kelas/interface kosong untuk `Region`, `ClimateProfile`, `TrendResult`, `IClimateService`, `IAiExplainerService`, `IRecommendationEngine`, `ClimateAnalysisService`, `RecommendationEngine`, `OpenMeteoProvider`, `ClimateRepository`, `NadiDbContext`, `GeminiClient`, `ClimateExplainer`.
- Template prompt AI (`explainer.v1.txt`, `chat.v1.txt`) sesuai PRD §11.3–§11.4.
- Shell WPF minimal (`App.xaml`, `MainWindow.xaml` kosong) + palet warna §13.
- `regions.json` (seed) berisi 2 contoh wilayah.
- `appsettings.example.json`, `.gitignore` (mengecualikan `appsettings.local.json` dan file database lokal).

## Risiko / catatan terbuka

- **Build belum tervalidasi.** File `.sln`/`.csproj` ditulis manual di lingkungan tanpa .NET SDK. Perlu dibuka & dibuild di Visual Studio/Windows untuk memastikan referensi package (LiveCharts2, CommunityToolkit.Mvvm, EF Core Sqlite) resolve dengan benar.
- **Open Questions PRD §21** (jumlah wilayah, opsi kelembapan, dark mode, dll.) belum dijawab PM.
- Wawancara validasi asumsi (§2) untuk Sprint 1 belum dilakukan.
