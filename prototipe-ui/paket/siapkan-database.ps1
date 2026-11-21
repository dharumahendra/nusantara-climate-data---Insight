<#
.SYNOPSIS
    Menyiapkan database PostgreSQL untuk NADI dan menulis appsettings.local.json.

.DESCRIPTION
    Skrip ini: (1) memeriksa koneksi ke PostgreSQL, (2) membuat database bila belum ada,
    (3) menjalankan database\schema.sql (aman dijalankan ulang), lalu (4) menulis
    appsettings.local.json di folder yang sama dengan NADI.exe.

    Password diminta lewat prompt dan tidak ditulis ke layar. Untuk otomatisasi, isi
    variabel lingkungan PGPASSWORD sebelum menjalankan skrip.

.EXAMPLE
    .\siapkan-database.ps1
    .\siapkan-database.ps1 -Server localhost -Port 5432 -Database nadi -User postgres
    .\siapkan-database.ps1 -Timpa      # menimpa appsettings.local.json yang sudah ada
#>
[CmdletBinding()]
param(
    [string]$Server = "localhost",
    [ValidateRange(1, 65535)][int]$Port = 5432,
    [ValidatePattern('^[A-Za-z_][A-Za-z0-9_]*$')][string]$Database = "nadi",
    [string]$User = "postgres",
    [string]$Psql,
    [switch]$Timpa
)

$ErrorActionPreference = "Stop"

$folderPaket = Split-Path -Parent $MyInvocation.MyCommand.Path
$fileSkema = Join-Path $folderPaket "database\schema.sql"
$fileKonfig = Join-Path $folderPaket "appsettings.local.json"

function Gagal([string]$pesan) {
    Write-Host ""
    Write-Host "GAGAL: $pesan" -ForegroundColor Red
    exit 1
}

function Cari-Psql([string]$diberikan) {
    if ($diberikan) {
        if (Test-Path $diberikan) { return $diberikan }
        Gagal "psql tidak ditemukan di '$diberikan'."
    }
    $perintah = Get-Command psql.exe -ErrorAction SilentlyContinue
    if ($perintah) { return $perintah.Source }

    $kandidat = Get-ChildItem "$env:ProgramFiles\PostgreSQL\*\bin\psql.exe" -ErrorAction SilentlyContinue |
        Sort-Object FullName -Descending
    if ($kandidat) { return $kandidat[0].FullName }

    Gagal ("psql.exe tidak ditemukan. Pasang PostgreSQL dari https://www.postgresql.org/download/windows/ " +
           "atau jalankan ulang dengan -Psql 'C:\jalan\ke\psql.exe'.")
}

# Nilai connection string yang mengandung spasi, titik koma, tanda sama dengan, atau kutip harus dikutip.
function Format-Nilai([string]$nilai) {
    if ($nilai -match '[;"''=\s]') { return '"' + ($nilai -replace '"', '""') + '"' }
    return $nilai
}

if (-not (Test-Path $fileSkema)) { Gagal "Berkas skema tidak ditemukan: $fileSkema" }

$psql = Cari-Psql $Psql
Write-Host "NADI - penyiapan database" -ForegroundColor Cyan
Write-Host "  psql     : $psql"
Write-Host "  server   : ${Server}:${Port}   pengguna: $User   database: $Database"
Write-Host ""

if ($env:PGPASSWORD) {
    $sandi = $env:PGPASSWORD
} else {
    $aman = Read-Host "Password PostgreSQL untuk pengguna '$User'" -AsSecureString
    $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($aman)
    try { $sandi = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr) }
    finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr) }
    $env:PGPASSWORD = $sandi
}
$env:PGOPTIONS = "-c client_min_messages=warning"   # sembunyikan NOTICE "already exists"

function Psql-Jalankan([string]$db, [string[]]$argumen) {
    # Windows PowerShell 5.1 mengubah stderr program native menjadi error fatal bila
    # ErrorActionPreference = Stop. Dilonggarkan di sini agar pesan psql bisa kita tangkap
    # dan ditampilkan dengan rapi lewat Gagal().
    $semula = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    try {
        $hasil = & $psql -h $Server -p $Port -U $User -d $db -w -v ON_ERROR_STOP=1 -q -t -A @argumen 2>&1
        $kode = $LASTEXITCODE
    } finally {
        $ErrorActionPreference = $semula
    }
    $teks = ($hasil | ForEach-Object { "$_" } | Out-String).Trim()
    return @{ Berhasil = ($kode -eq 0); Keluaran = $teks }
}

Write-Host "[1/4] Memeriksa koneksi ke PostgreSQL..."
$cek = Psql-Jalankan "postgres" @("-c", "SELECT 1")
if (-not $cek.Berhasil) {
    Gagal ("Tidak bisa terhubung ke PostgreSQL.`n$($cek.Keluaran)`n`n" +
           "Periksa: layanan PostgreSQL berjalan, password benar, serta server/port sesuai.")
}

Write-Host "[2/4] Memeriksa database '$Database'..."
$ada = Psql-Jalankan "postgres" @("-c", "SELECT 1 FROM pg_database WHERE datname = '$Database'")
if (-not $ada.Berhasil) { Gagal $ada.Keluaran }
if ($ada.Keluaran -ne "1") {
    $buat = Psql-Jalankan "postgres" @("-c", "CREATE DATABASE `"$Database`"")
    if (-not $buat.Berhasil) { Gagal "Database '$Database' tidak bisa dibuat.`n$($buat.Keluaran)" }
    Write-Host "      database '$Database' dibuat."
} else {
    Write-Host "      database '$Database' sudah ada, dipakai."
}

Write-Host "[3/4] Menjalankan skema (tabel dan 22 wilayah awal)..."
$skema = Psql-Jalankan $Database @("-f", $fileSkema)
if (-not $skema.Berhasil) { Gagal "Skema gagal dijalankan.`n$($skema.Keluaran)" }
$jumlah = Psql-Jalankan $Database @("-c", "SELECT count(*) FROM wilayah")
Write-Host "      tabel siap, $($jumlah.Keluaran) wilayah tersedia."

Write-Host "[4/4] Menulis appsettings.local.json..."
if ((Test-Path $fileKonfig) -and -not $Timpa) {
    Write-Host "      appsettings.local.json sudah ada dan TIDAK diubah." -ForegroundColor Yellow
    Write-Host "      Jalankan ulang dengan -Timpa bila ingin memperbarui connection string."
} else {
    $apiKeyLama = ""
    if (Test-Path $fileKonfig) {
        try { $lama = Get-Content $fileKonfig -Raw | ConvertFrom-Json; if ($lama.Gemini.ApiKey) { $apiKeyLama = $lama.Gemini.ApiKey } }
        catch { }   # file lama rusak: abaikan, tulis baru
    }

    $connectionString = "Host=$(Format-Nilai $Server);Port=$Port;Database=$(Format-Nilai $Database);" +
                        "Username=$(Format-Nilai $User);Password=$(Format-Nilai $sandi)"
    $konfig = [ordered]@{
        ConnectionStrings = [ordered]@{ Nadi = $connectionString }
        Gemini            = [ordered]@{ ApiKey = $apiKeyLama }
    }
    $json = $konfig | ConvertTo-Json -Depth 4
    [IO.File]::WriteAllText($fileKonfig, $json, (New-Object Text.UTF8Encoding($false)))
    Write-Host "      ditulis ke $fileKonfig"
}

Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
Remove-Item Env:\PGOPTIONS -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "Selesai. Jalankan NADI.exe, lalu buka Pengaturan > Uji Koneksi Database untuk memastikan." -ForegroundColor Green
Write-Host "API key Gemini (opsional) bisa diisi di Pengaturan atau di appsettings.local.json (Gemini > ApiKey)."
