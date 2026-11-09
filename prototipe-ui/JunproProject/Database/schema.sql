CREATE TABLE IF NOT EXISTS wilayah (
    id                  INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    provinsi            TEXT    NOT NULL,
    kota                TEXT    NOT NULL,
    suhu_normal         NUMERIC(4, 1) NOT NULL,   -- °C, rata-rata tahunan
    curah_hujan_normal  NUMERIC(6, 0) NOT NULL,   -- mm per tahun
    CONSTRAINT uq_wilayah_provinsi_kota UNIQUE (provinsi, kota)
);

CREATE TABLE IF NOT EXISTS pesan_chat (
    id          BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    wilayah_id  INT         NOT NULL REFERENCES wilayah (id) ON DELETE CASCADE,
    peran       TEXT        NOT NULL CHECK (peran IN ('pengguna', 'asisten')),
    isi         TEXT        NOT NULL,
    dibuat_pada TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_pesan_chat_wilayah ON pesan_chat (wilayah_id, id);

INSERT INTO wilayah (provinsi, kota, suhu_normal, curah_hujan_normal) VALUES
    ('DI Yogyakarta',       'Kota Yogyakarta', 26.8, 2100),
    ('DI Yogyakarta',       'Sleman',          26.3, 2400),
    ('DI Yogyakarta',       'Bantul',          26.9, 1900),
    ('DKI Jakarta',         'Jakarta Pusat',   28.0, 1850),
    ('DKI Jakarta',         'Jakarta Selatan', 27.6, 2100),
    ('DKI Jakarta',         'Jakarta Utara',   28.3, 1700),
    ('Jawa Barat',          'Bandung',         23.6, 2300),
    ('Jawa Barat',          'Bogor',           25.4, 3900),
    ('Jawa Barat',          'Bekasi',          27.8, 1900),
    ('Jawa Tengah',         'Semarang',        27.9, 2200),
    ('Jawa Tengah',         'Surakarta',       27.1, 2100),
    ('Jawa Tengah',         'Magelang',        25.2, 2700),
    ('Bali',                'Denpasar',        27.6, 1700),
    ('Bali',                'Badung',          27.4, 1800),
    ('Bali',                'Gianyar',         26.5, 2200),
    ('Sumatera Utara',      'Medan',           27.2, 2200),
    ('Kalimantan Timur',    'Samarinda',       27.3, 2400),
    ('Kalimantan Timur',    'Balikpapan',      27.1, 2700),
    ('Sulawesi Selatan',    'Makassar',        27.5, 2900),
    ('Nusa Tenggara Timur', 'Kupang',          27.8, 1400),
    ('Maluku',              'Ambon',           26.6, 3400),
    ('Papua',               'Jayapura',        27.4, 2500)
ON CONFLICT (provinsi, kota) DO NOTHING;
