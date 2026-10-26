@echo off
rem Futbolio uctan uca testini calistirir, ciktiyi Tests\e2e-sonuc.txt dosyasina yazar.
cd /d "%~dp0.."
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0run-e2e.ps1" > "%~dp0e2e-sonuc.txt" 2>&1
echo CIKIS KODU: %ERRORLEVEL% >> "%~dp0e2e-sonuc.txt"
