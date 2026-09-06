# 🌿 NADI — Product Requirements Document

> **NADI (Nusantara Climate Data & Insight)**
> *Membaca Iklim. Memahami Dampak. Memulai Aksi.*

| | |
|---|---|
| **Versi dokumen** | 2.0 (MVP) |
| **Status** | Draft — siap dieksekusi |
| **Durasi proyek** | 12 minggu (6 sprint × 2 minggu) |
| **Platform** | Windows Desktop — .NET 8 + WPF |
| **Tim** | 3 orang |
| **Repository** | `https://github.com/dharumahendra/nusantara-climate-data---Insight.git` |

---

## 0. Cara Membaca Dokumen Ini

**Untuk manusia (anggota tim):**
Baca §1–§4 untuk memahami produk, lalu langsung ke bagian yang jadi tanggung jawab kamu (§18 Tim & Kepemilikan).

**Untuk AI agent / coding assistant:**
Dokumen ini adalah *satu-satunya sumber kebenaran* untuk scope dan definisi. Sebelum menulis kode, baca dengan urutan:

1. **§5 Keputusan Teknis** — jangan mengusulkan alternatif yang sudah ditolak di sini.
2. **§6 Kamus Data & Rumus** — semua angka di aplikasi harus dihitung dengan rumus di bagian ini, bukan dengan interpretasi sendiri.
3. **§8 Kontrak Data Eksternal** — bentuk request/response API sudah ditentukan.
4. **§9 Skema Database** — nama tabel dan kolom bersifat mengikat.
5. **§10 Spesifikasi Fitur** — setiap fitur punya *acceptance criteria* yang bisa diuji. Fitur dianggap selesai hanya jika semua kriteria lulus.
6. **§11 Desain AI** — prompt template dan guardrail bersifat wajib.

**Aturan konflik:** jika ada pertanyaan yang tidak terjawab dokumen ini, jangan menebak. Catat di §21 Open Questions dan tanyakan ke Project Manager.

**Konvensi penulisan:**
- `MUST` / **wajib** = tidak boleh dilanggar.
- `SHOULD` / *sebaiknya* = boleh dilanggar dengan alasan yang dicatat di commit message.
- `MAY` / opsional = terserah implementer.

---

## 1. Ringkasan Produk

### Satu kalimat

> NADI adalah aplikasi desktop Windows yang mengubah data iklim historis Indonesia menjadi grafik dan penjelasan bahasa manusia, lalu menerjemahkannya jadi rekomendasi aksi iklim yang bisa dilakukan pengguna.

### Prinsip inti

```
Explore  →  Understand  →  Act
 (data)     (visual+AI)     (rekomendasi)
```

| Tahap | Yang dilakukan aplikasi | Fitur pendukung |
|---|---|---|
| **Explore** | Menampilkan kondisi iklim wilayah pilihan pengguna | F-01, F-02, F-03 |
| **Understand** | Menghitung tren, memvisualkan, menjelaskan lewat AI | F-04, F-05 |
| **Act** | Menurunkan kondisi iklim jadi saran tindakan konkret | F-06 |

### Elevator pitch untuk demo (30 detik)

"Data iklim Indonesia itu ada, tapi bentuknya CSV dan tabel yang cuma bisa dibaca peneliti. NADI mengambil data iklim 30 tahun terakhir untuk kota kamu, menggambarnya jadi grafik, lalu AI menjelaskan artinya dalam satu paragraf bahasa Indonesia — 'suhu rata-rata Sleman naik 0,8°C sejak 1994' — dan menutupnya dengan tiga aksi yang relevan dengan kondisi itu."

---

## 2. Masalah & Solusi

### Masalah yang divalidasi

| # | Masalah | Bukti / asumsi |
|---|---|---|
| P1 | Data iklim tersedia publik tapi formatnya tidak ramah orang awam (CSV, API, tabel mentah) | Asumsi — perlu divalidasi lewat 5 wawancara pengguna di Sprint 1 |
| P2 | Informasi iklim Indonesia tersebar di banyak sumber (BMKG, NOAA, Copernicus) | Terverifikasi dari desk research |
| P3 | Grafik iklim tidak disertai penjelasan; pengguna melihat garis naik tapi tidak tahu implikasinya | Asumsi |
| P4 | Isu perubahan iklim terasa abstrak dan global, bukan lokal dan personal | Asumsi |

> ⚠️ **Catatan kejujuran:** P1, P3, P4 masih asumsi tim, belum riset pengguna. Sprint 1 memuat tugas validasi ringan (5 wawancara). Jika asumsi runtuh, scope §10 harus ditinjau ulang, bukan dipaksakan.

### Solusi NADI

NADI **tidak** membuat data iklim baru dan **tidak** memprediksi masa depan. NADI melakukan tiga hal:

1. **Agregasi** — menarik satu sumber data terpercaya, menyimpannya lokal.
2. **Transformasi** — menghitung statistik dan tren dengan rumus yang transparan (§6).
3. **Narasi** — menggunakan LLM untuk menerjemahkan angka jadi kalimat, dan kondisi jadi rekomendasi.

---

## 3. Pengguna Sasaran

### Persona utama

**Rani, 20 tahun, mahasiswa Teknik Lingkungan di Yogyakarta**
- Butuh data iklim daerahnya untuk tugas kuliah dan kegiatan komunitas.
- Nyaman dengan aplikasi desktop, tapi tidak mau memproses CSV manual.
- *Job to be done:* "Saya perlu menunjukkan bahwa Jogja memang makin panas, dengan angka, dalam waktu 10 menit."

### Persona sekunder

**Pak Budi, 45 tahun, guru IPA SMA**
- Ingin memperlihatkan perubahan iklim ke murid dengan contoh daerah sendiri.
- *Job to be done:* "Saya butuh satu grafik dan satu paragraf penjelasan yang bisa saya proyeksikan ke kelas."

### Kebutuhan pengguna → fitur

| Kebutuhan pengguna | Fitur yang menjawab |
|---|---|
| "Bagaimana kondisi iklim daerah saya?" | F-01 Region Explorer |
| "Apakah berubah dari dulu?" | F-02 Historical Chart, F-04 AI Explainer |
| "Dibanding kota lain gimana?" | F-03 Regional Comparison |
| "Saya tidak paham grafik ini" | F-04 AI Explainer, F-05 AI Chat |
| "Terus saya harus apa?" | F-06 Climate Action |

---

## 4. Scope

### ✅ In Scope (MVP v1.0)

| ID | Fitur | Prioritas |
|---|---|---|
| F-01 | Climate Location Explorer | P0 — wajib |
| F-02 | Historical Climate Visualization | P0 — wajib |
| F-03 | Regional Climate Comparison | P1 |
| F-04 | AI Climate Explainer | P0 — wajib (pembeda produk) |
| F-05 | AI Climate Chat | P1 |
| F-06 | Climate Action Recommendation | P0 — wajib (pembeda produk) |
| F-07 | Climate Dashboard (shell + insight ringkas) | P0 — wajib |

**Aturan prioritas:** jika jadwal meleset, potong P1 lebih dulu (F-03, F-05). Jangan pernah memotong F-04 atau F-06 — dua fitur itu yang membedakan NADI dari grafik BMKG biasa.

### ❌ Out of Scope (jangan dikerjakan di MVP)

| Tidak dikerjakan | Alasan |
|---|---|
| Login / akun pengguna | Aplikasi single-user desktop, tidak ada data yang perlu dilindungi per-user |
| Prediksi iklim berbasis ML | Butuh keahlian & validasi ilmiah di luar kapasitas tim; risiko misinformasi tinggi |
| Aplikasi mobile | Platform sudah ditetapkan desktop |
| Integrasi sensor IoT | Tidak ada perangkat keras |
| Peringatan dini bencana realtime | **Berbahaya.** Aplikasi ini bukan sumber resmi; salah alarm bisa fatal. Jangan pernah menambahkan tanpa kerja sama resmi BMKG/BNPB |
| Peta interaktif (choropleth) | Nice-to-have, dipindah ke v2.0 |
| Export PDF | v2.0 |

---

## 5. Keputusan Teknis (Decision Log)

Bagian ini mengunci pilihan yang sudah didiskusikan. **AI agent tidak boleh mengusulkan alternatif untuk keputusan ini kecuali diminta eksplisit.**

| # | Keputusan | Alasan | Alternatif yang ditolak |
|---|---|---|---|
| D-01 | **Sumber data utama = Open-Meteo Historical Weather API (ERA5)** | Gratis, tanpa API key, cakupan global termasuk Indonesia, data harian sejak 1940, format JSON stabil | BMKG: tidak punya REST API publik yang terdokumentasi untuk data historis harian; sebagian data hanya via portal/permintaan manual. BMKG tetap dirujuk sebagai sumber rujukan dan target integrasi v2.0 |
| D-02 | **Tidak ada backend server.** Aplikasi desktop memanggil API langsung | Menghemat biaya & waktu; scope 12 minggu | Backend ASP.NET Core sebagai proxy |
| D-03 | **API key Gemini disimpan di file konfigurasi lokal**, bukan di source code | Mencegah kebocoran di repo publik | Hardcode (dilarang), Windows Credential Manager (overkill untuk MVP tapi boleh) |
| D-04 | **Periode analisis default = 30 tahun terakhir** (mis. 1995–2024) | Standar klimatologi WMO menggunakan periode normal 30 tahun | 10 tahun (terlalu bising), 50 tahun (kualitas data reanalisis kurang konsisten) |
| D-05 | **Baseline normal iklim = 1991–2020** | Periode normal WMO yang berlaku saat ini | 1961–1990 |
| D-06 | **Data disimpan permanen di SQLite** setelah unduhan pertama; tidak diunduh ulang kecuali diminta | Memenuhi kebutuhan offline & mempercepat load | Cache memori saja |
| D-07 | **Library grafik = LiveCharts2** | Mendukung WPF + .NET 8, MVVM-friendly, gratis, dokumentasi memadai | OxyPlot (boleh dipakai jika LiveCharts2 bermasalah — catat penggantinya) |
| D-08 | **Semua perhitungan statistik dilakukan di C#, bukan oleh AI** | Determinisme, bisa diuji, mencegah halusinasi angka | Meminta LLM menghitung tren dari data mentah |
| D-09 | **Wilayah MVP dibatasi ~40 kota/kabupaten** yang dimuat dari file seed | Menghindari kerumitan data 514 kabupaten di MVP; cukup untuk demo | Semua kabupaten Indonesia |
| D-10 | **Bahasa antarmuka & output AI = Bahasa Indonesia** | Target pengguna Indonesia | Bilingual (v2.0) |

> ℹ️ **Verifikasi sebelum coding:** detail endpoint dan nama parameter Open-Meteo maupun nama model Gemini bisa berubah. Developer wajib mengecek dokumentasi resmi masing-masing saat Sprint 2 dan Sprint 4, lalu memperbarui §8 dan §11 jika berbeda.

---

## 6. Kamus Data & Rumus

**Ini bagian terpenting untuk konsistensi.** Semua angka yang tampil di UI atau dikirim ke AI wajib dihitung dengan definisi di bawah. Semua rumus di sini diimplementasikan di `NADI.Core/Services/ClimateAnalysisService.cs` dan **wajib punya unit test**.

### 6.1 Istilah

| Istilah | Definisi operasional |
|---|---|
| **Wilayah (Region)** | Satu titik koordinat (lat, lon) yang mewakili pusat kota/kabupaten. Data iklim diambil untuk titik ini, bukan rata-rata area |
| **Tahun valid** | Satu tahun kalender yang memiliki ≥ 330 hari data tidak-null (≈90%). Tahun tidak valid **dikecualikan** dari perhitungan tren dan diberi tanda di UI |
| **Periode analisis** | 30 tahun kalender penuh terakhir yang tersedia. Tahun berjalan dikecualikan karena belum lengkap |
| **Baseline normal** | Rata-rata 1991–2020 untuk indikator yang bersangkutan (D-05) |
| **Anomali** | `nilai_tahun − nilai_baseline`. Positif = di atas normal |

### 6.2 Indikator dan rumus

| Indikator | Satuan | Rumus |
|---|---|---|
| Suhu rata-rata tahunan | °C | Rata-rata aritmetik `temperature_2m_mean` seluruh hari dalam tahun valid |
| Suhu maksimum tahunan | °C | Nilai `temperature_2m_max` tertinggi dalam tahun tersebut |
| Suhu minimum tahunan | °C | Nilai `temperature_2m_min` terendah dalam tahun tersebut |
| Curah hujan tahunan | mm | Jumlah `precipitation_sum` seluruh hari dalam tahun |
| Hari hujan | hari | Jumlah hari dengan `precipitation_sum ≥ 1,0 mm` |
| Hari hujan lebat | hari | Jumlah hari dengan `precipitation_sum ≥ 50 mm` (mengacu kategori BMKG: lebat 50–100 mm/hari, sangat lebat > 100 mm/hari — **verifikasi ke publikasi BMKG sebelum ditampilkan sebagai istilah resmi**) |
| **Hari panas** | hari | Jumlah hari dengan `temperature_2m_max ≥ P90`, di mana `P90` = persentil ke-90 dari seluruh nilai suhu maksimum harian pada baseline 1991–2020 **untuk wilayah tersebut**. Nilai `P90` disimpan per wilayah dan ditampilkan di tooltip agar transparan |
| Kelembapan rata-rata | % | Rata-rata `relative_humidity_2m` per jam, diagregasi ke harian lalu ke tahunan (lihat catatan §8.2) |
| Kecepatan angin rata-rata | km/jam | Rata-rata `wind_speed_10m_max` harian |

### 6.3 Perhitungan tren

Tren dihitung dengan **regresi linear kuadrat terkecil (ordinary least squares)** pada deret nilai tahunan:

```
x = tahun (mis. 1995, 1996, …)
y = nilai indikator tahunan

slope (b) = Σ((xᵢ − x̄)(yᵢ − ȳ)) / Σ((xᵢ − x̄)²)
```

Yang dilaporkan ke UI dan ke AI:

| Field | Perhitungan | Contoh tampilan |
|---|---|---|
| `SlopePerDecade` | `slope × 10` | `+0,27 °C / dekade` |
| `TotalChange` | `slope × (tahun_akhir − tahun_awal)` | `+0,8 °C dalam 30 tahun` |
| `RSquared` | Koefisien determinasi | `0,62` |
| `Direction` | `Naik` jika slope > 0, `Turun` jika < 0, `Stabil` jika \|TotalChange\| di bawah ambang tabel 6.4 | `Naik` |
| `Confidence` | `Kuat` jika R² ≥ 0,5; `Sedang` jika 0,25 ≤ R² < 0,5; `Lemah` jika R² < 0,25 | `Kuat` |
| `ValidYearCount` | Jumlah tahun valid yang dipakai | `29 dari 30` |

### 6.4 Ambang "stabil" (agar tidak melebih-lebihkan tren kecil)

| Indikator | Ambang perubahan total dianggap Stabil |
|---|---|
| Suhu | \|perubahan\| < 0,2 °C |
| Curah hujan tahunan | \|perubahan\| < 5% dari baseline |
| Hari panas | \|perubahan\| < 3 hari |

### 6.5 Aturan penyajian angka

- Suhu: **1 angka desimal**, pemisah desimal koma (`27,4 °C`).
- Curah hujan: **bulat**, pemisah ribuan titik (`2.145 mm`).
- Persentase: 1 desimal.
- Jika `Confidence = Lemah`, UI **wajib** menampilkan label "tren belum jelas" dan AI **wajib** diberi tahu agar tidak menyimpulkan secara tegas (§11.4).

---

## 7. Arsitektur Sistem

```text
┌──────────────────────────────────────────────────────────┐
│                     NADI.App (WPF)                        │
│   Views ──binding──> ViewModels ──DI──> Interfaces        │
│   Dashboard · Charts · Comparison · Chat · Actions        │
└───────────────────────────┬──────────────────────────────┘
                            │ (hanya lewat interface)
┌───────────────────────────▼──────────────────────────────┐
│                       NADI.Core                           │
│   Models · Interfaces · Business Logic                    │
│   ├─ ClimateAnalysisService   (§6 semua rumus)           │
│   ├─ RecommendationEngine     (§10 F-06 rule engine)     │
│   └─ RegionService                                        │
└───────────┬───────────────────────────────┬──────────────┘
            │                               │
┌───────────▼─────────────┐   ┌─────────────▼──────────────┐
│  NADI.Infrastructure    │   │        NADI.AI             │
│  ├─ OpenMeteoProvider   │   │  ├─ GeminiClient           │
│  ├─ ClimateRepository   │   │  ├─ PromptTemplates        │
│  └─ NadiDbContext (EF)  │   │  └─ AiResponseCache        │
└───────────┬─────────────┘   └─────────────┬──────────────┘
            │                               │
      ┌─────▼─────┐                  ┌──────▼──────┐
      │  SQLite   │                  │ Gemini API  │
      │ nadi.db   │                  └─────────────┘
      └───────────┘
            ▲
      ┌─────┴───────────┐
      │ Open-Meteo API  │
      └─────────────────┘
```

### Aturan ketergantungan (wajib)

```
NADI.App          →  NADI.Core
NADI.Infrastructure →  NADI.Core
NADI.AI           →  NADI.Core
NADI.Core         →  (tidak bergantung pada apa pun)
```

- `NADI.Core` **tidak boleh** mereferensikan `NADI.App`, `NADI.Infrastructure`, atau `NADI.AI`.
- `NADI.App` **tidak boleh** mereferensikan `NADI.Infrastructure` atau `NADI.AI` secara langsung dalam ViewModel — hanya lewat interface yang didefinisikan di `NADI.Core/Interfaces`. Registrasi implementasi konkret hanya terjadi di composition root (`App.xaml.cs`).
- Code-behind View (`*.xaml.cs`) **hanya** boleh berisi `InitializeComponent()` dan penanganan visual murni. Nol logika bisnis.

### Alur data satu permintaan (contoh: pengguna memilih Sleman)

```
1. RegionViewModel.SelectedRegion berubah
2. → IClimateService.GetClimateProfileAsync(regionId, periode)
3. → ClimateRepository cek SQLite: apakah data 1995–2024 lengkap?
   ├─ Ya  → ambil dari DB
   └─ Tidak → OpenMeteoProvider.FetchAsync() → simpan ke DB → ambil dari DB
4. → ClimateAnalysisService hitung statistik & tren (§6)
5. → ClimateProfile dikembalikan ke ViewModel → binding ke UI
6. → (aksi terpisah, saat tombol ditekan) IAiExplainerService.ExplainAsync(profile)
7. → RecommendationEngine.Generate(profile) → daftar rekomendasi
```

**Langkah 6 tidak boleh otomatis dijalankan saat wilayah berubah** — hanya saat pengguna menekan tombol, agar hemat kuota API.

---

## 8. Kontrak Data Eksternal

### 8.1 Open-Meteo — data historis harian

```
GET https://archive-api.open-meteo.com/v1/archive
```

| Parameter | Nilai |
|---|---|
| `latitude` | dari tabel `Regions` |
| `longitude` | dari tabel `Regions` |
| `start_date` | `YYYY-MM-DD` (awal periode analisis) |
| `end_date` | `YYYY-MM-DD` |
| `daily` | `temperature_2m_mean,temperature_2m_max,temperature_2m_min,precipitation_sum,wind_speed_10m_max` |
| `timezone` | `Asia/Jakarta` |

Bentuk respons (disederhanakan):

```json
{
  "latitude": -7.75, "longitude": 110.375,
  "daily": {
    "time": ["1995-01-01", "1995-01-02"],
    "temperature_2m_mean": [26.4, 26.9],
    "temperature_2m_max": [31.2, 32.0],
    "temperature_2m_min": [22.8, 23.1],
    "precipitation_sum": [12.4, 0.0],
    "wind_speed_10m_max": [14.2, 11.8]
  }
}
```

**Penanganan wajib:**
- Array paralel — indeks ke-*i* dari setiap array merujuk tanggal yang sama. Validasi panjang array sama; jika tidak, batalkan dan lempar `ClimateDataFormatException`.
- Nilai `null` di array = data hilang. Simpan sebagai `NULL` di DB, jangan diganti 0.
- Data reanalisis biasanya tertinggal beberapa hari dari hari ini. Jangan meminta `end_date` = hari ini; gunakan akhir tahun kalender penuh terakhir.
- Rentang 30 tahun menghasilkan ~11.000 baris per wilayah. Unduh **per wilayah**, jangan seluruh wilayah sekaligus di startup.
- Terapkan timeout 30 detik dan retry maksimal 2× dengan jeda naik (1s, 3s).

### 8.2 Kelembapan — catatan penting

API harian tidak menyediakan agregat kelembapan relatif. Pilih salah satu, catat di kode:

- **Opsi A (disarankan MVP):** ambil `relative_humidity_2m` dari endpoint per jam, agregasi jadi rata-rata harian di sisi klien. Konsekuensi: ukuran respons ~24× lebih besar; ambil hanya untuk 5 tahun terakhir.
- **Opsi B:** tampilkan kelembapan hanya untuk **kondisi saat ini** (dari endpoint forecast/current), bukan sebagai deret historis. Tandai di UI "kondisi terkini", bukan "rata-rata 30 tahun".

**Dilarang** menampilkan kelembapan historis jika sumbernya tidak benar-benar historis.

### 8.3 Kondisi terkini (untuk kartu ringkasan dashboard)

```
GET https://api.open-meteo.com/v1/forecast
    ?latitude=..&longitude=..
    &current=temperature_2m,relative_humidity_2m,precipitation,wind_speed_10m
    &timezone=Asia/Jakarta
```

Cache 1 jam. Jika gagal, dashboard tetap tampil dengan kartu historis dan kartu terkini menunjukkan status "tidak tersedia" — **bukan** error blocking.

### 8.4 Data wilayah (seed)

File `NADI.Infrastructure/Database/Seed/regions.json` berisi ~40 wilayah:

```json
[
  { "province": "DI Yogyakarta", "city": "Sleman",  "latitude": -7.7167, "longitude": 110.3550 },
  { "province": "DKI Jakarta",   "city": "Jakarta Pusat", "latitude": -6.1862, "longitude": 106.8342 }
]
```

Cakupan minimum: ≥1 wilayah dari setiap pulau besar (Sumatera, Jawa, Kalimantan, Sulawesi, Bali–Nusa Tenggara, Maluku, Papua) agar demo perbandingan bermakna.

---

## 9. Skema Database

SQLite via EF Core, file `%LOCALAPPDATA%/NADI/nadi.db`. Migrasi dikelola dengan `dotnet ef migrations`.

### `Regions`

| Kolom | Tipe | Keterangan |
|---|---|---|
| `RegionId` | INTEGER PK | auto increment |
| `Province` | TEXT NOT NULL | |
| `City` | TEXT NOT NULL | |
| `Latitude` | REAL NOT NULL | |
| `Longitude` | REAL NOT NULL | |
| `HotDayThreshold` | REAL NULL | P90 hasil hitung (§6.2), diisi setelah data terunduh |

*Unique index:* (`Province`, `City`)

### `ClimateDailyRecords`

| Kolom | Tipe | Keterangan |
|---|---|---|
| `Id` | INTEGER PK | |
| `RegionId` | INTEGER FK → Regions | ON DELETE CASCADE |
| `Date` | TEXT NOT NULL | ISO `YYYY-MM-DD` |
| `TemperatureAvg` | REAL NULL | |
| `TemperatureMax` | REAL NULL | |
| `TemperatureMin` | REAL NULL | |
| `Rainfall` | REAL NULL | mm |
| `Humidity` | REAL NULL | %, boleh NULL (§8.2) |
| `WindSpeed` | REAL NULL | km/jam |

*Unique index:* (`RegionId`, `Date`) — mencegah duplikasi saat unduh ulang.
*Index:* (`RegionId`, `Date`) untuk query rentang.

> Tabel ini yang paling besar (~11.000 baris × 40 wilayah ≈ 440.000 baris). Gunakan `AsNoTracking()` untuk query baca dan agregasi di SQL bila memungkinkan.

### `ClimateYearlySummaries` (tabel turunan, cache perhitungan)

| Kolom | Tipe |
|---|---|
| `Id` | INTEGER PK |
| `RegionId` | INTEGER FK |
| `Year` | INTEGER |
| `TempAvg`, `TempMax`, `TempMin` | REAL |
| `RainfallTotal` | REAL |
| `RainyDays`, `HeavyRainDays`, `HotDays` | INTEGER |
| `IsValidYear` | INTEGER (0/1) — §6.1 |
| `ComputedAt` | TEXT |

*Unique index:* (`RegionId`, `Year`). Dihitung ulang saat data harian wilayah berubah.

### `ChatMessages`

| Kolom | Tipe |
|---|---|
| `Id` | INTEGER PK |
| `RegionId` | INTEGER FK |
| `Role` | TEXT — `user` \| `assistant` |
| `Content` | TEXT |
| `Timestamp` | TEXT (ISO 8601) |

> Perhatikan: berbeda dari draf sebelumnya, satu baris = satu pesan (bukan pasangan tanya-jawab), agar riwayat percakapan bisa dikirim ulang ke API sebagai konteks multi-turn.

### `AiResponseCache`

| Kolom | Tipe | Keterangan |
|---|---|---|
| `Id` | INTEGER PK | |
| `CacheKey` | TEXT UNIQUE | SHA-256 dari JSON statistik + versi prompt |
| `ResponseText` | TEXT | |
| `CreatedAt` | TEXT | |

Menghemat kuota: penjelasan untuk wilayah dan periode yang sama tidak perlu diminta ulang.

### `Recommendations` (katalog statis, di-seed)

| Kolom | Tipe | Keterangan |
|---|---|---|
| `Id` | INTEGER PK | |
| `TriggerCode` | TEXT | mis. `TEMP_RISING`, `RAINFALL_HIGH` (§10 F-06) |
| `Category` | TEXT | Energi \| Air \| Transportasi \| Vegetasi \| Kesiapsiagaan |
| `Title` | TEXT | |
| `Description` | TEXT | |
| `ImpactLevel` | TEXT | Rendah \| Sedang \| Tinggi |

Rekomendasi **tidak** disimpan per wilayah. Wilayah dipetakan ke rekomendasi lewat `TriggerCode` yang dihasilkan rule engine saat runtime.

---

## 10. Spesifikasi Fitur

Format setiap fitur: tujuan → user story → perilaku → acceptance criteria (AC) yang bisa diuji.

---

### F-01 — Climate Location Explorer · P0

**Tujuan:** pengguna memilih wilayah dan melihat kondisi iklimnya.

**User story:** Sebagai pengguna, saya ingin memilih provinsi lalu kota, sehingga dashboard menampilkan data iklim wilayah itu.

**Perilaku:**
- Dropdown Provinsi → dropdown Kota (bergantung pilihan provinsi) → tombol/otomatis muat.
- Kotak pencarian dengan filter ketik-untuk-cari.
- Pilihan terakhir disimpan dan dipulihkan saat aplikasi dibuka lagi.
- Saat data belum ada di lokal, tampilkan progress ("Mengunduh data iklim Sleman 1995–2024…") — bukan aplikasi membeku.

**Acceptance criteria:**

| AC | Given / When / Then |
|---|---|
| AC-01.1 | **Given** aplikasi baru diinstal, **when** pengguna memilih "DI Yogyakarta" → "Sleman", **then** aplikasi mengunduh data, menyimpannya ke SQLite, dan menampilkan dashboard dalam ≤ 15 detik pada koneksi normal |
| AC-01.2 | **Given** data Sleman sudah tersimpan, **when** wilayah dipilih ulang, **then** dashboard tampil dalam ≤ 3 detik **tanpa** panggilan jaringan |
| AC-01.3 | **Given** tidak ada koneksi internet dan data belum pernah diunduh, **when** wilayah dipilih, **then** muncul pesan jelas "Butuh koneksi internet untuk mengunduh data wilayah ini pertama kali" dengan tombol Coba Lagi — aplikasi tidak crash |
| AC-01.4 | **Given** tidak ada koneksi tapi data sudah tersimpan, **when** wilayah dipilih, **then** semua fitur non-AI berfungsi normal |

---

### F-02 — Historical Climate Visualization · P0

**Tujuan:** menunjukkan perubahan iklim wilayah secara visual.

**Perilaku:**
- Line chart deret tahunan untuk indikator terpilih (default: suhu rata-rata).
- Selector indikator: Suhu rata-rata · Suhu maks · Suhu min · Curah hujan · Hari panas.
- Filter rentang tahun (slider ganda), default = periode analisis penuh.
- **Garis tren** ditumpangkan pada chart, dihitung dari §6.3.
- Tooltip per titik: tahun, nilai, anomali terhadap baseline.
- Titik tertinggi & terendah diberi penanda visual.
- Tahun tidak valid (§6.1) ditampilkan sebagai celah/putus, bukan garis interpolasi, dan dicatat di keterangan chart.

**Acceptance criteria:**

| AC | Given / When / Then |
|---|---|
| AC-02.1 | **Given** wilayah dengan 30 tahun data, **when** chart dimuat, **then** jumlah titik = jumlah tahun valid, dan label sumbu X menampilkan tahun |
| AC-02.2 | **Given** garis tren aktif, **when** dibandingkan dengan hasil unit test `ClimateAnalysisService`, **then** slope yang digambar sama dengan slope yang dihitung (toleransi 0,001) |
| AC-02.3 | **Given** pengguna menggeser filter ke 2010–2024, **then** statistik tren dihitung ulang untuk rentang itu saja dan label periode di UI ikut berubah |
| AC-02.4 | **Given** indikator diganti, **then** chart diperbarui tanpa mengunduh ulang data |

---

### F-03 — Regional Climate Comparison · P1

**Tujuan:** menempatkan kondisi wilayah dalam konteks.

**Perilaku:**
- Dua selector wilayah (A dan B).
- Tabel perbandingan: suhu rata-rata, tren suhu, curah hujan tahunan, hari hujan, hari panas, tren hari panas.
- Chart garis ganda (dua warna berbeda, ada legenda).
- Setiap baris tabel menandai wilayah mana yang lebih tinggi.

**Acceptance criteria:**

| AC | Given / When / Then |
|---|---|
| AC-03.1 | **Given** wilayah A dan B dipilih, **then** kedua deret muncul di satu chart dengan legenda dan skala sumbu Y yang sama |
| AC-03.2 | **Given** salah satu wilayah datanya belum diunduh, **then** aplikasi mengunduhnya dengan indikator progres, tanpa memblokir wilayah yang sudah siap |
| AC-03.3 | **Given** wilayah A = wilayah B, **then** tombol bandingkan nonaktif dengan pesan penjelas |
| AC-03.4 | Perbandingan **hanya** menggunakan tahun yang valid di **kedua** wilayah; periode yang dipakai ditulis eksplisit di UI |

---

### F-04 — AI Climate Explainer · P0 ⭐

**Tujuan:** menerjemahkan statistik jadi satu paragraf yang dipahami orang awam.

**Perilaku:**
- Tombol "Jelaskan data ini" di bawah chart.
- Aplikasi menyusun objek statistik (§11.2), mengirim ke Gemini, menampilkan hasil.
- Selama menunggu: indikator loading dan tombol dinonaktifkan.
- Hasil di-cache (§9 `AiResponseCache`).
- Di bawah hasil selalu ada label: *"Penjelasan dibuat AI berdasarkan angka yang dihitung aplikasi. Angka bersumber dari ERA5/Open-Meteo."*

**Acceptance criteria:**

| AC | Given / When / Then |
|---|---|
| AC-04.1 | **Given** profil iklim tersedia, **when** tombol ditekan, **then** muncul penjelasan 3–5 kalimat Bahasa Indonesia yang menyebut nama wilayah dan periode |
| AC-04.2 | **Given** respons AI diterima, **then** setiap angka dalam teks AI dapat ditemukan pada objek statistik yang dikirim (diuji manual pada 10 wilayah; target 10/10 lolos) |
| AC-04.3 | **Given** API key kosong atau tidak valid, **then** muncul pesan "Fitur AI belum aktif — masukkan API key di Pengaturan", dan seluruh fitur lain tetap berfungsi |
| AC-04.4 | **Given** permintaan yang sama diulang, **then** jawaban diambil dari cache tanpa panggilan API |
| AC-04.5 | **Given** `Confidence = Lemah` (§6.3), **then** teks AI mengandung kualifikasi ketidakpastian, bukan pernyataan tegas |

---

### F-05 — AI Climate Chat · P1 ⭐

**Tujuan:** pengguna bertanya bebas tentang wilayah yang sedang dibuka.

**Perilaku:**
- Panel chat dengan konteks wilayah aktif ditampilkan di header ("Konteks: Sleman, 1995–2024").
- Setiap permintaan mengirim: system prompt + objek statistik + hingga 6 pesan terakhir + pertanyaan baru.
- Riwayat disimpan per wilayah di `ChatMessages`, dipulihkan saat wilayah dibuka lagi.
- Tersedia 3 pertanyaan saran ("Apakah wilayah ini makin panas?", "Bagaimana pola hujannya?", "Apa yang bisa saya lakukan?").
- Tombol hapus riwayat.

**Acceptance criteria:**

| AC | Given / When / Then |
|---|---|
| AC-05.1 | **Given** wilayah Sleman aktif, **when** pengguna bertanya "Apakah makin panas?", **then** jawaban merujuk angka tren Sleman yang sama dengan yang tampil di chart |
| AC-05.2 | **Given** pertanyaan di luar topik iklim/lingkungan, **then** AI menolak dengan sopan dan mengarahkan kembali ke topik (§11.4) |
| AC-05.3 | **Given** pertanyaan tentang wilayah yang datanya tidak dimuat, **then** AI menyatakan tidak punya data untuk wilayah itu dan menyarankan memilihnya lewat Explorer — **tidak** mengarang angka |
| AC-05.4 | **Given** aplikasi ditutup dan dibuka lagi, **then** riwayat chat wilayah tersebut tetap ada |
| AC-05.5 | **Given** panggilan API gagal, **then** pesan pengguna tetap tampil dengan status gagal dan tombol kirim ulang |

---

### F-06 — Climate Action Recommendation · P0 ⭐

**Tujuan:** menutup lingkaran Explore→Understand→**Act**.

**Perilaku:** rule engine deterministik di `NADI.Core/Services/RecommendationEngine.cs`. **Bukan** dihasilkan AI (AI hanya boleh memparafrase, tidak menentukan).

**Tabel aturan:**

| TriggerCode | Kondisi | Contoh rekomendasi |
|---|---|---|
| `TEMP_RISING` | Tren suhu `Naik` dan `TotalChange ≥ 0,5 °C` | Kurangi beban pendingin ruangan; tanam peneduh; gunakan atap/cat reflektif |
| `TEMP_RISING_STRONG` | `TotalChange ≥ 1,0 °C` | Semua di atas + advokasi ruang terbuka hijau di lingkungan |
| `HOTDAYS_RISING` | Tren hari panas `Naik` dan bertambah ≥ 5 hari | Transportasi rendah emisi; hindari aktivitas luar ruang siang hari; kenali gejala heat stress |
| `RAINFALL_HIGH` | Curah hujan tahunan ≥ 2.500 mm | Panen air hujan; pastikan drainase & biopori |
| `RAINFALL_LOW` | Curah hujan tahunan ≤ 1.500 mm | Hemat air; mulsa tanaman; tampung air |
| `RAINFALL_DECLINING` | Tren curah hujan `Turun` ≥ 10% | Konservasi air jangka panjang |
| `HEAVYRAIN_RISING` | Tren hari hujan lebat `Naik` | Kesiapsiagaan banjir; jangan buang sampah ke saluran air |
| `BASELINE` | Selalu aktif | Aksi umum: hemat listrik, kurangi sampah makanan, transportasi publik |

**Perilaku tampilan:**
- Tampilkan maksimal **5 kartu**, urut berdasarkan `ImpactLevel` lalu spesifisitas trigger.
- Setiap kartu menyebutkan **alasan** ("Karena hari panas di Sleman bertambah 9 hari sejak 1995").
- Kartu `BASELINE` hanya ditampilkan bila jumlah kartu spesifik < 3.

**Acceptance criteria:**

| AC | Given / When / Then |
|---|---|
| AC-06.1 | **Given** profil dengan tren suhu +0,8 °C, **then** `TEMP_RISING` aktif dan minimal satu kartu kategori Energi atau Vegetasi muncul |
| AC-06.2 | **Given** profil apa pun, **then** minimal 3 rekomendasi selalu tampil (fallback `BASELINE`) |
| AC-06.3 | **Given** input profil yang sama, **then** output rekomendasi identik setiap kali dijalankan (deterministik — diuji dengan unit test) |
| AC-06.4 | Setiap kartu menampilkan kalimat alasan yang memuat angka dari profil |

---

### F-07 — Climate Dashboard · P0

**Tujuan:** halaman utama yang merangkum semuanya.

**Tata letak:**

```
┌────────────┬──────────────────────────────────────────────┐
│            │  Sleman, DI Yogyakarta          [ganti ▾]    │
│  SIDEBAR   ├──────────────────────────────────────────────┤
│            │  ┌────────┐┌────────┐┌────────┐┌────────┐   │
│ ◉ Dashboard│  │Suhu    ││Curah   ││Hari    ││Tren    │   │
│ ○ Grafik   │  │rata²   ││hujan   ││panas   ││30 thn  │   │
│ ○ Banding  │  │27,4°C  ││2.145mm ││34 hari ││+0,8°C  │   │
│ ○ Chat AI  │  └────────┘└────────┘└────────┘└────────┘   │
│ ○ Aksi     │  ┌──────────────────────────────────────┐   │
│ ○ Pengatur.│  │  Grafik suhu 1995–2024 + garis tren  │   │
│            │  └──────────────────────────────────────┘   │
│            │  [ Jelaskan data ini dengan AI ]             │
│            │  ┌──────────────────────────────────────┐   │
│            │  │  Penjelasan AI muncul di sini        │   │
│            │  └──────────────────────────────────────┘   │
│            │  Rekomendasi aksi:  [kartu] [kartu] [kartu] │
└────────────┴──────────────────────────────────────────────┘
```

**Acceptance criteria:**

| AC | Given / When / Then |
|---|---|
| AC-07.1 | **Given** data lokal tersedia, **then** dashboard render penuh ≤ 3 detik |
| AC-07.2 | **Given** belum ada wilayah dipilih, **then** tampil empty state yang mengarahkan memilih wilayah — bukan kartu kosong atau "0" |
| AC-07.3 | Semua kartu KPI menampilkan periode data sebagai teks kecil |

---

## 11. Desain AI

### 11.1 Prinsip (wajib)

1. **AI tidak menghitung.** Semua angka datang dari `ClimateAnalysisService`. AI hanya menerjemahkan.
2. **AI tidak boleh menyebut angka yang tidak ada di input.** Jika butuh angka yang tak tersedia, AI harus bilang tidak tahu.
3. **AI tidak memprediksi masa depan.** Boleh menjelaskan tren yang sudah terjadi, tidak boleh meramal tahun depan.
4. **AI tidak memberi nasihat medis, hukum, atau peringatan bencana.**
5. **Bahasa Indonesia sederhana**, hindari jargon; jika terpaksa, jelaskan sekali.
6. **Selalu sebut nama wilayah dan periode** dalam jawaban.

### 11.2 Kontrak input ke AI

Objek yang dikirim (bukan data mentah harian):

```json
{
  "wilayah": "Sleman, DI Yogyakarta",
  "periode": { "mulai": 1995, "akhir": 2024, "tahun_valid": 29 },
  "sumber_data": "ERA5 via Open-Meteo",
  "suhu": {
    "rata_rata_c": 27.4,
    "rata_rata_baseline_1991_2020_c": 26.9,
    "tren_per_dekade_c": 0.27,
    "perubahan_total_c": 0.8,
    "arah": "Naik",
    "keyakinan": "Kuat"
  },
  "curah_hujan": {
    "tahunan_mm": 2145,
    "baseline_mm": 2210,
    "hari_hujan": 141,
    "tren_arah": "Stabil",
    "keyakinan": "Lemah"
  },
  "hari_panas": {
    "ambang_c": 32.6,
    "jumlah_tahun_terakhir": 34,
    "rata_rata_baseline": 25,
    "tren_arah": "Naik",
    "keyakinan": "Sedang"
  }
}
```

### 11.3 Prompt template — Explainer

Disimpan di `NADI.AI/PromptTemplates/explainer.txt`, diberi nomor versi (`v1`, `v2`, …) karena ikut jadi bagian cache key.

```
Kamu adalah asisten edukasi iklim untuk aplikasi NADI.

TUGAS
Jelaskan data iklim berikut dalam 3-5 kalimat Bahasa Indonesia yang mudah
dipahami pelajar SMA.

ATURAN KETAT
- Gunakan HANYA angka yang ada di data JSON di bawah. Dilarang menghitung,
  memperkirakan, atau menambah angka lain.
- Sebutkan nama wilayah dan rentang tahun.
- Jika "keyakinan" bernilai "Lemah", gunakan kata seperti "belum jelas" atau
  "belum menunjukkan pola tegas". Jangan menyimpulkan dengan tegas.
- Jangan memprediksi kondisi masa depan.
- Jangan memberi nasihat kesehatan atau peringatan bencana.
- Tutup dengan satu kalimat tentang apa arti data ini bagi kehidupan
  sehari-hari warga wilayah tersebut.
- Jangan gunakan bullet point. Tulis sebagai paragraf mengalir.

DATA
{climate_json}
```

### 11.4 Prompt template — Chat

```
Kamu adalah asisten iklim NADI. Kamu hanya membahas iklim, cuaca, lingkungan,
dan aksi iklim.

KONTEKS WILAYAH AKTIF
{climate_json}

ATURAN KETAT
- Jawab hanya berdasarkan data konteks di atas. Jika pengguna menanyakan
  wilayah, indikator, atau periode yang tidak ada di konteks, katakan terus
  terang bahwa datanya belum dimuat dan sarankan memilih wilayah itu di menu
  Jelajah Wilayah. JANGAN mengarang angka.
- Jika pertanyaan di luar topik iklim/lingkungan, tolak dengan sopan dalam
  satu kalimat dan tawarkan kembali topik iklim.
- Jangan memprediksi cuaca atau iklim masa depan.
- Jangan memberi peringatan dini bencana; arahkan ke BMKG untuk informasi resmi.
- Jawaban maksimal 4 kalimat kecuali pengguna meminta detail.
- Bahasa Indonesia yang santai tapi sopan.
```

### 11.5 Konfigurasi & keamanan API key

- File `appsettings.local.json` di folder data aplikasi, **wajib** masuk `.gitignore`.
- Sediakan `appsettings.example.json` di repo sebagai contoh.
- Halaman Pengaturan menyediakan input API key + tombol "Uji koneksi".
- Jika key tidak ada: seluruh aplikasi tetap jalan, hanya F-04 dan F-05 nonaktif dengan pesan yang jelas.
- **Dilarang** mencatat API key ke log.

### 11.6 Pengendalian kuota

- Cache respons (§9).
- Panggilan AI **hanya** dipicu aksi eksplisit pengguna.
- Debounce tombol; nonaktifkan saat request berjalan.
- Batasi pesan chat 500 karakter.
- Tangani respons rate-limit dengan pesan "Kuota AI sedang penuh, coba lagi beberapa saat lagi" — jangan retry agresif.

> **Verifikasi saat Sprint 4:** nama model, endpoint, dan batas kuota gratis Gemini bisa berubah. Cek dokumentasi resmi Google AI dan perbarui bagian ini.

---

## 12. Non-Functional Requirements

| Kategori | Requirement | Cara mengukur |
|---|---|---|
| Performa — dashboard | Render ≤ 3 detik dengan data lokal | Stopwatch di 3 wilayah berbeda |
| Performa — unduhan awal | 30 tahun data satu wilayah ≤ 15 detik | Uji pada koneksi ≥ 10 Mbps |
| Performa — UI | Tidak ada freeze; semua I/O `async` | Tidak ada pemanggilan `.Result` / `.Wait()` di codebase |
| Offline | Semua fitur non-AI berfungsi tanpa internet setelah unduhan pertama | Uji dengan adapter jaringan dimatikan |
| Ketahanan | Kegagalan jaringan/API tidak pernah menyebabkan crash | Uji: matikan internet, masukkan API key salah, putus koneksi di tengah unduhan |
| Keamanan | Tidak ada rahasia di repo | `git log -p` dicek sebelum rilis; `.gitignore` diverifikasi |
| Maintainability | Pola MVVM + DI; nol logika bisnis di code-behind | Code review |
| Testability | `ClimateAnalysisService` dan `RecommendationEngine` tercakup unit test | Target coverage ≥ 70% untuk dua kelas itu |
| Aksesibilitas | Kontras teks memenuhi rasio ≥ 4,5:1; ukuran font body ≥ 14px | Cek dengan contrast checker |
| Ukuran instalasi | ≤ 200 MB tanpa data | Cek output publish |

---

## 13. Panduan UI/UX

### Tema

**Minimalist Earth Tone Dashboard** — banyak whitespace, data sebagai bintang utama.

### Palet warna

| Peran | Hex | Catatan penggunaan |
|---|---|---|
| Primary | `#452829` | Heading, teks utama, garis chart utama |
| Secondary | `#57595B` | Teks sekunder, label sumbu |
| Accent | `#E8D1C5` | Permukaan kartu, highlight |
| Background | `#F3E8DF` | Latar aplikasi |
| Success | `#4A6B4A` | Tren membaik / aksi positif |
| Warning | `#B5722F` | Tren memburuk / peringatan lembut |
| Muted | `#9A9086` | Data tidak valid, keadaan nonaktif |

> ⚠️ **Wajib dicek:** `#57595B` di atas `#F3E8DF` perlu diverifikasi rasio kontrasnya. Jika di bawah 4,5:1, gelapkan Secondary. Aksesibilitas menang atas estetika.

**Aturan warna pada chart:** jangan pernah mengandalkan warna saja untuk membedakan deret — selalu tambahkan legenda, dan bedakan pula gaya garis (solid vs putus-putus) demi pengguna buta warna.

### Tipografi

| Peran | Font | Ukuran |
|---|---|---|
| H1 | Poppins Bold | 28 |
| H2 | Poppins SemiBold | 20 |
| Body | Inter Regular (fallback Segoe UI) | 14 |
| Angka KPI | Inter Medium | 32 |
| Caption / sumber data | Inter Regular | 12 |

Font wajib di-*embed* sebagai resource agar tampilan konsisten di komputer yang belum memasangnya.

### Keadaan UI yang wajib dirancang

Setiap layar yang menampilkan data harus punya empat keadaan — jangan hanya membangun happy path:

1. **Loading** — skeleton/progress dengan teks yang menjelaskan apa yang sedang terjadi.
2. **Empty** — belum ada wilayah dipilih / belum ada riwayat chat.
3. **Error** — pesan manusiawi + aksi pemulihan (Coba Lagi / Buka Pengaturan).
4. **Loaded** — keadaan normal.

### Nada bahasa

Netral, faktual, tidak menakut-nakuti. Hindari "krisis", "bencana", "kiamat iklim". Fokus pada *apa yang terjadi* dan *apa yang bisa dilakukan*.

---

## 14. Struktur Proyek & Konvensi Kode

```text
NADI/
├── src/
│   ├── NADI.App/                       # WPF UI (entry point)
│   │   ├── Views/                      # *.xaml + code-behind minimal
│   │   ├── ViewModels/                 # satu VM per View
│   │   ├── Controls/                   # UserControl reusable
│   │   ├── Converters/
│   │   ├── Resources/                  # Styles.xaml, Colors.xaml, Fonts/
│   │   └── App.xaml.cs                 # composition root (registrasi DI)
│   │
│   ├── NADI.Core/                      # nol dependensi eksternal
│   │   ├── Models/                     # Region, ClimateProfile, TrendResult…
│   │   ├── Interfaces/                 # IClimateService, IAiExplainerService…
│   │   └── Services/
│   │       ├── ClimateAnalysisService.cs   # §6
│   │       └── RecommendationEngine.cs     # §10 F-06
│   │
│   ├── NADI.Infrastructure/
│   │   ├── Providers/OpenMeteoProvider.cs
│   │   ├── Repositories/ClimateRepository.cs
│   │   └── Database/  NadiDbContext.cs · Migrations/ · Seed/
│   │
│   └── NADI.AI/
│       ├── GeminiClient.cs
│       ├── ClimateExplainer.cs
│       └── PromptTemplates/
│
├── tests/NADI.Tests/
│   ├── ClimateAnalysisServiceTests.cs
│   ├── RecommendationEngineTests.cs
│   └── OpenMeteoParsingTests.cs
│
├── docs/
│   ├── PRD.md            ← dokumen ini
│   ├── diagrams/         # use case, activity, class, ERD
│   └── screenshots/
│
├── .gitignore
├── appsettings.example.json
└── README.md
```

### Konvensi kode

| Aspek | Aturan |
|---|---|
| Penamaan | PascalCase untuk kelas/method/properti publik; `_camelCase` untuk field privat |
| Async | Semua I/O `async`; akhiri nama dengan `Async`; **dilarang** `.Result` dan `.Wait()` |
| DI | Constructor injection; tidak ada `new` untuk service di dalam ViewModel |
| ViewModel | Turunan `ObservableObject` (CommunityToolkit.Mvvm); aksi lewat `IRelayCommand` |
| Nullability | `<Nullable>enable</Nullable>` di semua project |
| Bahasa | Nama kode dalam bahasa Inggris; teks yang dilihat pengguna dalam Bahasa Indonesia (lewat file resource, bukan literal tersebar) |
| Komentar | Jelaskan *mengapa*, bukan *apa*. Semua rumus di §6 diberi komentar rujukan ke bagian PRD ini |

### Git

- Branch: `main` (stabil) · `dev` (integrasi) · `feature/F-04-ai-explainer`
- Commit: Conventional Commits — `feat(ai): tambah climate explainer`, `fix(chart): perbaiki celah tahun tidak valid`
- Pull request wajib direview minimal satu anggota lain sebelum merge ke `dev`.
- **Dilarang** commit langsung ke `main`.

---

## 15. Penanganan Error — Katalog

| Kode | Situasi | Perilaku aplikasi |
|---|---|---|
| E-01 | Tidak ada internet, data belum ada | Pesan + tombol Coba Lagi; wilayah lain yang sudah terunduh tetap bisa dibuka |
| E-02 | API iklim mengembalikan 4xx/5xx | Retry 2× lalu tampilkan "Sumber data sedang tidak tersedia" |
| E-03 | Respons API tidak sesuai format | Log detail, tampilkan pesan umum, jangan simpan data parsial |
| E-04 | Data wilayah tidak lengkap (< 10 tahun valid) | Tetap tampilkan, sembunyikan garis tren, beri catatan "data belum cukup untuk analisis tren" |
| E-05 | API key AI kosong/salah | Fitur AI nonaktif dengan CTA ke Pengaturan; fitur lain normal |
| E-06 | Rate limit AI | Pesan tunggu; jangan retry otomatis |
| E-07 | Database terkunci/korup | Tawarkan reset database dengan konfirmasi |

**Aturan umum:** aplikasi tidak boleh menampilkan stack trace ke pengguna. Semua exception dicatat ke `%LOCALAPPDATA%/NADI/logs/`.

---

## 16. Roadmap 12 Minggu

Setiap sprint punya **exit criteria** — sprint tidak dianggap selesai sebelum semuanya terpenuhi.

### Sprint 1 (Minggu 1–2) — Fondasi
- Repo, branch strategy, `.gitignore`, README kerangka.
- Solution 4 project + referensi sesuai §7.
- DI container, navigasi sidebar, tema & resource warna/font.
- EF Core + SQLite + migrasi awal + seed `regions.json`.
- 5 wawancara pengguna singkat untuk memvalidasi asumsi §2.

**Exit criteria:** aplikasi bisa dijalankan, berpindah antar halaman kosong, database terbentuk dan berisi ~40 wilayah, semua anggota bisa build dari clean clone.

### Sprint 2 (Minggu 3–4) — Data & Dashboard
- `OpenMeteoProvider` + parsing + penanganan error (§8, §15).
- `ClimateRepository`, simpan & baca data harian.
- `ClimateAnalysisService` + unit test (§6).
- F-01 Region Explorer, F-07 kartu KPI dashboard.

**Exit criteria:** memilih Sleman → data terunduh, tersimpan, kartu KPI menampilkan angka yang cocok dengan hasil unit test. AC-01.1 s/d AC-01.4 lulus.

### Sprint 3 (Minggu 5–6) — Visualisasi
- F-02 chart historis + garis tren + filter tahun.
- F-03 perbandingan wilayah.
- Keadaan loading/empty/error di semua layar.

**Exit criteria:** AC-02.x dan AC-03.x lulus. Demo internal dengan 5 wilayah berbeda tanpa error.

### Sprint 4 (Minggu 7–8) — AI
- Verifikasi dokumentasi Gemini, `GeminiClient`, halaman Pengaturan + uji koneksi.
- F-04 Explainer + cache.
- F-05 Chat + persistensi riwayat.
- Iterasi prompt: uji 10 wilayah, catat kegagalan, revisi template.

**Exit criteria:** AC-04.x dan AC-05.x lulus. Uji halusinasi angka 10/10 lolos (AC-04.2).

### Sprint 5 (Minggu 9–10) — Climate Action & Polish
- F-06 rule engine + katalog rekomendasi + unit test determinisme.
- Integrasi rekomendasi ke dashboard.
- Perbaikan visual, konsistensi tipografi, aksesibilitas kontras.

**Exit criteria:** AC-06.x lulus. Alur Explore→Understand→Act bisa didemokan utuh dalam satu sesi.

### Sprint 6 (Minggu 11–12) — Finalisasi
- Pengujian menyeluruh (§17), perbaikan bug.
- Diagram: use case, activity, class, ERD.
- README lengkap: latar, screenshot, cara instal, cara build, cara isi API key, atribusi sumber data.
- Paket rilis + rehearsal demo.

**Exit criteria:** §19 Definition of Done terpenuhi untuk semua fitur P0.

### Buffer

Minggu 12 menyisakan ~3 hari buffer. Jika Sprint 4 molor (paling berisiko), potong F-05 sesuai aturan prioritas §4.

---

## 17. Strategi Pengujian

| Level | Cakupan | Alat |
|---|---|---|
| Unit test | `ClimateAnalysisService` (rumus §6), `RecommendationEngine` (determinisme), parsing respons API | xUnit |
| Integration | Repository ↔ SQLite; provider ↔ respons JSON tersimpan (fixture) | xUnit + SQLite in-memory |
| Manual — happy path | Skenario User Journey §18 untuk 5 wilayah | Checklist |
| Manual — kegagalan | E-01 s/d E-07 (§15) | Checklist |
| Manual — kualitas AI | 10 wilayah × 3 pertanyaan; catat halusinasi angka, jawaban di luar topik | Lembar penilaian |

**Fixture wajib:** simpan minimal 2 respons API asli sebagai file JSON di `tests/Fixtures/` agar test tidak bergantung jaringan.

---

## 18. Tim & Kepemilikan

| Anggota | Peran | Kepemilikan kode | Fitur |
|---|---|---|---|
| **M1** | PM & Backend | `NADI.Core`, `NADI.Infrastructure`, arsitektur, DI, database, integrasi API, manajemen repo | F-01 (data), §6, §8, §9 |
| **M2** | Frontend & UI/UX | `NADI.App` (Views, styles, charts, navigasi), keadaan UI, aksesibilitas | F-02, F-03, F-07 |
| **M3** | AI & Data | `NADI.AI`, prompt engineering, rule engine rekomendasi, evaluasi kualitas AI | F-04, F-05, F-06 |

**Titik rawan koordinasi:**
- Model `ClimateProfile` (§11.2) dipakai bertiga → **M1 mendefinisikan lebih dulu di Sprint 2**, M2 dan M3 tidak mengubahnya tanpa kesepakatan.
- Interface di `NADI.Core/Interfaces` adalah kontrak antar-anggota. Perubahan interface wajib diumumkan di grup, bukan diam-diam di-commit.

**Ritual:** stand-up async harian di grup chat (3 kalimat: kemarin, hari ini, hambatan). Review sprint tiap dua minggu dengan demo aplikasi berjalan — bukan slide.

---

## 19. Definition of Done

### Per fitur

- [ ] Semua acceptance criteria di §10 lulus dan diperagakan ke satu anggota lain.
- [ ] Logika bisnis berada di `NADI.Core`, bukan di ViewModel atau code-behind.
- [ ] Keadaan loading, empty, dan error sudah ditangani.
- [ ] Tidak ada exception yang tidak tertangani pada jalur normal maupun jalur gagal.
- [ ] Unit test ditulis untuk logika perhitungan (jika ada).
- [ ] Tidak ada `TODO` atau kode mati yang tertinggal.
- [ ] Sudah di-merge ke `dev` lewat PR yang direview.

### Per rilis MVP

- [ ] Semua fitur P0 selesai.
- [ ] Aplikasi bisa di-build dari clone bersih dengan instruksi README saja.
- [ ] Tidak ada rahasia di repo.
- [ ] Semua diagram di `docs/diagrams/` lengkap.
- [ ] README memuat atribusi sumber data dan disclaimer.
- [ ] Demo 5 menit sudah dilatih dan berjalan tanpa error.

---

## 20. Risiko

| Risiko | Dampak | Kemungkinan | Mitigasi |
|---|---|---|---|
| API iklim berubah/tidak tersedia | Tinggi | Rendah | Data tersimpan lokal; fixture untuk demo; provider di balik interface sehingga bisa diganti |
| Kuota AI gratis habis saat demo | Tinggi | Sedang | Cache respons; siapkan wilayah demo yang penjelasannya sudah ter-cache |
| AI mengarang angka | Tinggi | Sedang | Guardrail prompt (§11.4), uji halusinasi wajib (AC-04.2), disclaimer di UI |
| Tim baru mengenal WPF/MVVM | Sedang | Tinggi | Sprint 1 khusus fondasi; pair programming untuk view pertama |
| Data ERA5 berbeda dari data stasiun BMKG | Sedang | Tinggi | **Nyatakan sumber data secara jelas di UI dan README.** Jangan mengklaim data BMKG jika bukan |
| Scope melebar (peta, PDF, prediksi) | Sedang | Tinggi | §4 Out of Scope bersifat mengikat; usulan baru masuk v2.0 |

---

## 21. Open Questions

Tanyakan ke PM; jangan diputuskan sendiri oleh AI agent.

| # | Pertanyaan | Pemilik | Batas waktu |
|---|---|---|---|
| Q1 | Apakah 40 wilayah cukup, atau demo butuh kota tertentu yang belum masuk daftar? | M1 | Sprint 1 |
| Q2 | Kelembapan pakai Opsi A atau B (§8.2)? | M1 | Sprint 2 |
| Q3 | Perlukah tampilan gelap (dark mode)? | M2 | Sprint 3 |
| Q4 | Apakah rekomendasi perlu ditinjau ahli lingkungan sebelum demo? | M3 | Sprint 5 |
| Q5 | Format distribusi: installer MSI atau folder portabel? | M1 | Sprint 6 |

---

## 22. Disclaimer Produk

Teks berikut **wajib** muncul di README dan di halaman Tentang aplikasi:

> NADI adalah aplikasi edukasi. Data iklim bersumber dari reanalisis ERA5 melalui Open-Meteo dan dapat berbeda dari pengamatan stasiun cuaca resmi. Penjelasan naratif dihasilkan oleh model bahasa AI berdasarkan statistik yang dihitung aplikasi, dan dapat mengandung kekeliruan. NADI bukan sumber peringatan dini bencana. Untuk informasi cuaca dan peringatan resmi, rujuk BMKG.

---

## Lampiran A — User Journey Utama

```
1. Buka aplikasi
   → Empty state: "Pilih wilayah untuk mulai"
2. Pilih Provinsi → Kota
   → Progress unduhan (pertama kali) atau langsung tampil
3. Dashboard: 4 kartu KPI + grafik suhu 30 tahun + garis tren
4. Klik "Jelaskan data ini"
   → Paragraf AI muncul dalam ≤ 10 detik
5. Scroll ke rekomendasi
   → 3–5 kartu aksi dengan alasan berbasis angka
6. (Opsional) Buka Chat AI, tanya "Kenapa bisa makin panas?"
7. (Opsional) Buka Perbandingan, pilih kota kedua
```

## Lampiran B — Glosarium

| Istilah | Arti |
|---|---|
| **Reanalisis / ERA5** | Dataset iklim global hasil penggabungan observasi dan model cuaca, menghasilkan data konsisten sejak 1940 |
| **Baseline / normal iklim** | Rata-rata jangka panjang (di sini 1991–2020) sebagai pembanding |
| **Anomali** | Selisih nilai suatu tahun terhadap baseline |
| **Tren** | Arah perubahan jangka panjang, dihitung dengan regresi linear |
| **R²** | Ukuran seberapa baik garis tren mewakili data (0–1); makin tinggi makin meyakinkan |
| **Hari panas** | Hari dengan suhu maksimum melampaui persentil ke-90 baseline wilayah tersebut |
| **MVVM** | Pola arsitektur Model–View–ViewModel yang memisahkan tampilan dari logika |
| **DI** | Dependency Injection — komponen menerima ketergantungannya dari luar |

---

*Dokumen versi 2.0 — perbarui nomor versi setiap ada perubahan yang memengaruhi scope, rumus, atau kontrak data.*
