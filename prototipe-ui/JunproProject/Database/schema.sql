-- Skema database NADI. Aman dijalankan ulang (idempotent), termasuk pada
-- database lama dari Modul 8 yang belum punya kolom koordinat.

CREATE TABLE IF NOT EXISTS wilayah (
    id         INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    provinsi   TEXT NOT NULL,
    kota       TEXT NOT NULL,
    latitude   DOUBLE PRECISION,
    longitude  DOUBLE PRECISION,
    CONSTRAINT uq_wilayah_provinsi_kota UNIQUE (provinsi, kota)
);

-- Migrasi dari versi lama: koordinat dibutuhkan untuk memanggil Open-Meteo,
-- sedangkan suhu/curah hujan "normal" ilustrasi tidak dipakai lagi karena data
-- iklim sekarang berasal dari Open-Meteo.
ALTER TABLE wilayah ADD COLUMN IF NOT EXISTS latitude  DOUBLE PRECISION;
ALTER TABLE wilayah ADD COLUMN IF NOT EXISTS longitude DOUBLE PRECISION;
ALTER TABLE wilayah DROP COLUMN IF EXISTS suhu_normal;
ALTER TABLE wilayah DROP COLUMN IF EXISTS curah_hujan_normal;

CREATE TABLE IF NOT EXISTS pesan_chat (
    id          BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    wilayah_id  INT         NOT NULL REFERENCES wilayah (id) ON DELETE CASCADE,
    peran       TEXT        NOT NULL CHECK (peran IN ('pengguna', 'asisten')),
    isi         TEXT        NOT NULL,
    dibuat_pada TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_pesan_chat_wilayah ON pesan_chat (wilayah_id, id);

-- Data iklim harian dari Open-Meteo (PRD §9 ClimateDailyRecords). Nilai hilang
-- disimpan sebagai NULL, bukan 0. Satu baris per wilayah per tanggal.
CREATE TABLE IF NOT EXISTS data_iklim_harian (
    wilayah_id       INT  NOT NULL REFERENCES wilayah (id) ON DELETE CASCADE,
    tanggal          DATE NOT NULL,
    suhu_rata        DOUBLE PRECISION,   -- °C, temperature_2m_mean
    suhu_maks        DOUBLE PRECISION,   -- °C, temperature_2m_max
    suhu_min         DOUBLE PRECISION,   -- °C, temperature_2m_min
    curah_hujan      DOUBLE PRECISION,   -- mm, precipitation_sum
    kecepatan_angin  DOUBLE PRECISION,   -- km/jam, wind_speed_10m_max
    PRIMARY KEY (wilayah_id, tanggal)
);

INSERT INTO wilayah (provinsi, kota, latitude, longitude) VALUES
    ('DI Yogyakarta',       'Kota Yogyakarta',  -7.7956, 110.3695),
    ('DI Yogyakarta',       'Sleman',           -7.7167, 110.3550),
    ('DI Yogyakarta',       'Bantul',           -7.8881, 110.3289),
    ('DKI Jakarta',         'Jakarta Pusat',    -6.1862, 106.8342),
    ('DKI Jakarta',         'Jakarta Selatan',  -6.2615, 106.8106),
    ('DKI Jakarta',         'Jakarta Utara',    -6.1384, 106.8636),
    ('Jawa Barat',          'Bandung',          -6.9175, 107.6191),
    ('Jawa Barat',          'Bogor',            -6.5971, 106.8060),
    ('Jawa Barat',          'Bekasi',           -6.2383, 106.9756),
    ('Jawa Tengah',         'Semarang',         -6.9667, 110.4167),
    ('Jawa Tengah',         'Surakarta',        -7.5755, 110.8243),
    ('Jawa Tengah',         'Magelang',         -7.4797, 110.2177),
    ('Bali',                'Denpasar',         -8.6705, 115.2126),
    ('Bali',                'Badung',           -8.5819, 115.1770),
    ('Bali',                'Gianyar',          -8.5389, 115.3258),
    ('Sumatera Utara',      'Medan',             3.5952,  98.6722),
    ('Kalimantan Timur',    'Samarinda',        -0.5022, 117.1536),
    ('Kalimantan Timur',    'Balikpapan',       -1.2379, 116.8529),
    ('Sulawesi Selatan',    'Makassar',         -5.1477, 119.4327),
    ('Nusa Tenggara Timur', 'Kupang',          -10.1772, 123.6070),
    ('Maluku',              'Ambon',            -3.6954, 128.1814),
    ('Papua',               'Jayapura',         -2.5337, 140.7181)
ON CONFLICT (provinsi, kota) DO UPDATE
    SET latitude = EXCLUDED.latitude, longitude = EXCLUDED.longitude;
