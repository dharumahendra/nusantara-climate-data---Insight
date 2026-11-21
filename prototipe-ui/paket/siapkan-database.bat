@echo off
rem Pembungkus agar skrip PowerShell bisa dijalankan dengan klik ganda
rem tanpa mengubah Execution Policy komputer.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0siapkan-database.ps1" %*
echo.
pause
