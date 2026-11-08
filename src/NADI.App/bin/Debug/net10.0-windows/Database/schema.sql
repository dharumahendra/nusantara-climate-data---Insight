PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS Regions (
    RegionId        INTEGER PRIMARY KEY AUTOINCREMENT,
    Province        TEXT    NOT NULL,
    City            TEXT    NOT NULL,
    Latitude        REAL    NOT NULL CHECK (Latitude BETWEEN -90 AND 90),
    Longitude       REAL    NOT NULL CHECK (Longitude BETWEEN -180 AND 180),
    HotDayThreshold REAL,
    UNIQUE (Province, City)
);

CREATE TABLE IF NOT EXISTS ClimateDailyRecords (
    Id             INTEGER PRIMARY KEY AUTOINCREMENT,
    RegionId       INTEGER NOT NULL REFERENCES Regions (RegionId) ON DELETE CASCADE,
    Date           TEXT    NOT NULL CHECK (Date GLOB '[0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9]'),
    TemperatureAvg REAL,
    TemperatureMax REAL,
    TemperatureMin REAL,
    Rainfall       REAL CHECK (Rainfall >= 0),
    Humidity       REAL CHECK (Humidity BETWEEN 0 AND 100),
    WindSpeed      REAL CHECK (WindSpeed >= 0),
    UNIQUE (RegionId, Date)
);

CREATE TABLE IF NOT EXISTS ClimateYearlySummaries (
    Id            INTEGER PRIMARY KEY AUTOINCREMENT,
    RegionId      INTEGER NOT NULL REFERENCES Regions (RegionId) ON DELETE CASCADE,
    Year          INTEGER NOT NULL,
    TempAvg       REAL,
    TempMax       REAL,
    TempMin       REAL,
    RainfallTotal REAL    CHECK (RainfallTotal >= 0),
    RainyDays     INTEGER NOT NULL DEFAULT 0 CHECK (RainyDays BETWEEN 0 AND 366),
    HeavyRainDays INTEGER NOT NULL DEFAULT 0 CHECK (HeavyRainDays BETWEEN 0 AND 366),
    HotDays       INTEGER NOT NULL DEFAULT 0 CHECK (HotDays BETWEEN 0 AND 366),
    IsValidYear   INTEGER NOT NULL DEFAULT 0 CHECK (IsValidYear IN (0, 1)),
    ComputedAt    TEXT    NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%SZ', 'now')),
    UNIQUE (RegionId, Year)
);

CREATE TABLE IF NOT EXISTS ChatMessages (
    Id        INTEGER PRIMARY KEY AUTOINCREMENT,
    RegionId  INTEGER NOT NULL REFERENCES Regions (RegionId) ON DELETE CASCADE,
    Role      TEXT    NOT NULL CHECK (Role IN ('user', 'assistant')),
    Content   TEXT    NOT NULL,
    Timestamp TEXT    NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%SZ', 'now'))
);

CREATE INDEX IF NOT EXISTS IX_ChatMessages_Region_Timestamp ON ChatMessages (RegionId, Timestamp);

CREATE TABLE IF NOT EXISTS AiResponseCache (
    Id           INTEGER PRIMARY KEY AUTOINCREMENT,
    CacheKey     TEXT    NOT NULL UNIQUE CHECK (length(CacheKey) = 64),
    ResponseText TEXT    NOT NULL,
    CreatedAt    TEXT    NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%SZ', 'now'))
);

CREATE TABLE IF NOT EXISTS Recommendations (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    TriggerCode TEXT NOT NULL,
    Category    TEXT NOT NULL,
    Title       TEXT NOT NULL,
    Description TEXT NOT NULL,
    ImpactLevel TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS IX_Recommendations_TriggerCode ON Recommendations (TriggerCode);
