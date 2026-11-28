<#
.SYNOPSIS
    Membuat paket NADI siap kumpul: folder portabel + berkas zip.

.DESCRIPTION
    Menjalankan dotnet publish (self-contained, satu berkas NADI.exe), menyalin
    skrip penyiapan database, skema, contoh konfigurasi, dan panduan, lalu
    membuat zip. Hasilnya di <root repo>\dist\ (tidak ikut di-commit).

.EXAMPLE
    .\prototipe-ui\buat-paket.ps1

.EXAMPLE
    .\prototipe-ui\buat-paket.ps1 -KonfigDemo konfigurasi-demo\appsettings.local.json
    Menyertakan konfigurasi demo (database cloud, user terbatas) sebagai
    appsettings.local.json di paket, agar NADI.exe langsung tersambung.
#>
[CmdletBinding()]
param(
    [string]$Versi = "1.0.0",
    [string]$Keluaran = "dist",
    [string]$KonfigDemo
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$proyek = Join-Path $PSScriptRoot "JunproProject\JunproProject.csproj"
$folderDist = Join-Path $root $Keluaran
$folderPaket = Join-Path $folderDist "NADI"
$zip = Join-Path $folderDist "NADI-v$Versi-win-x64.zip"

if (Test-Path $folderDist) { Remove-Item $folderDist -Recurse -Force }
New-Item -ItemType Directory -Path $folderPaket | Out-Null

Write-Host "[1/4] Publish (self-contained, satu berkas)..." -ForegroundColor Cyan
dotnet publish $proyek -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true -p:DebugType=none `
    -p:Version=$Versi -o $folderPaket -nologo -v q
if ($LASTEXITCODE -ne 0) { throw "dotnet publish gagal." }

Write-Host "[2/4] Menyalin berkas pendamping..." -ForegroundColor Cyan
New-Item -ItemType Directory -Path (Join-Path $folderPaket "database") | Out-Null
Copy-Item (Join-Path $PSScriptRoot "JunproProject\Database\schema.sql") (Join-Path $folderPaket "database\schema.sql")
Copy-Item (Join-Path $PSScriptRoot "paket\*") $folderPaket
Copy-Item (Join-Path $root "appsettings.example.json") $folderPaket
$panduan = Join-Path $root "docs\PANDUAN.md"
if (Test-Path $panduan) {
    Copy-Item $panduan $folderPaket
    $gambar = Join-Path $root "docs\screenshots\panduan"
    if (Test-Path $gambar) {
        # Jalur sama dengan di repo (screenshots/panduan/...) agar gambar di PANDUAN.md tetap tampil.
        New-Item -ItemType Directory -Path (Join-Path $folderPaket "screenshots\panduan") -Force | Out-Null
        Copy-Item (Join-Path $gambar "*") (Join-Path $folderPaket "screenshots\panduan")
    }
} else {
    Write-Warning "docs\PANDUAN.md belum ada, paket dibuat tanpa panduan."
}

Write-Host "[3/4] Memeriksa paket tidak membawa rahasia lokal..." -ForegroundColor Cyan
$rahasia = Get-ChildItem $folderPaket -Recurse -Filter "appsettings.local.json" -ErrorAction SilentlyContinue
if ($rahasia) { $rahasia | Remove-Item -Force; Write-Warning "appsettings.local.json ditemukan di paket dan dihapus." }
Get-ChildItem $folderPaket -Recurse -Include *.pdb | Remove-Item -Force

# Konfigurasi demo disalin SETELAH pemeriksaan di atas, karena memang sengaja dibawa.
if ($KonfigDemo) {
    $sumberKonfig = if ([IO.Path]::IsPathRooted($KonfigDemo)) { $KonfigDemo } else { Join-Path $root $KonfigDemo }
    if (-not (Test-Path $sumberKonfig)) { throw "Konfigurasi demo tidak ditemukan: $sumberKonfig" }
    try { $isiKonfig = Get-Content $sumberKonfig -Raw | ConvertFrom-Json } catch { throw "Konfigurasi demo bukan JSON valid: $sumberKonfig" }
    if (-not $isiKonfig.ConnectionStrings.Nadi) { throw "Konfigurasi demo tidak berisi ConnectionStrings.Nadi." }
    if ($isiKonfig.ConnectionStrings.Nadi -match 'Host=(localhost|127\.0\.0\.1)') {
        throw "Konfigurasi demo masih mengarah ke database lokal."
    }
    Copy-Item $sumberKonfig (Join-Path $folderPaket "appsettings.local.json")
    Write-Host "      Konfigurasi demo disertakan sebagai appsettings.local.json." -ForegroundColor Yellow
}

Write-Host "[4/4] Membuat zip..." -ForegroundColor Cyan
Compress-Archive -Path (Join-Path $folderPaket "*") -DestinationPath $zip -CompressionLevel Optimal

Write-Host ""
Write-Host "Paket siap:" -ForegroundColor Green
Get-ChildItem $folderPaket -Recurse -File | ForEach-Object {
    "  {0,10:N0} KB  {1}" -f ($_.Length / 1KB), $_.FullName.Substring($folderPaket.Length + 1)
}
"  zip: {0}  ({1:N1} MB)" -f $zip, ((Get-Item $zip).Length / 1MB)
