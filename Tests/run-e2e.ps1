<#
.SYNOPSIS
    Futbolio uçtan uca testini tek komutla çalıştırır.

.DESCRIPTION
    1. API ve WebUI'ı geçici bir klasöre derler (Visual Studio açık olsa da çakışmaz).
    2. Boş bir test veritabanı kullanır (varsa önce silinir). Asıl veritabanına dokunmaz.
    3. API'yi 7291, WebUI'ı 7288 portunda başlatır (Visual Studio'nun portlarından farklı).
       API ilk açılışta tabloları kurar ve örnek verileri ekler.
    4. Tests/e2e_test.py'yi çalıştırır.
    5. Sunucuları kapatır, test veritabanını ve geçici dosyaları siler.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File .\Tests\run-e2e.ps1

.EXAMPLE
    # Farklı SQL Server ve test sonrası veritabanını incelemek için silmeden bırakma
    powershell -ExecutionPolicy Bypass -File .\Tests\run-e2e.ps1 -Server "localhost" -KeepDatabase
#>
param(
    # SQL Server adresi (bilgisayarındaki SQL Express için .\SQLEXPRESS)
    [string]$Server = ".\SQLEXPRESS",

    # Test veritabanı adı. Güvenlik için adında "Test" ya da "E2E" geçmeli.
    [string]$Database = "FutbolioDb_E2E",

    # Test bitince veritabanını silme (içine bakmak istersen)
    [switch]$KeepDatabase
)

$ErrorActionPreference = "Stop"

if ($Database -notmatch "Test|E2E") {
    throw "Güvenlik: test veritabanının adında 'Test' ya da 'E2E' geçmeli (verilen: $Database). Asıl veritabanı silinmesin diye."
}

$root = Split-Path -Parent $PSScriptRoot           # solution klasörü
$work = Join-Path $env:TEMP "futbolio-e2e"          # geçici derleme + log klasörü
$apiUrl = "https://localhost:7291"
$webUrl = "https://localhost:7288"
$connection = "Server=$Server;Database=$Database;Trusted_Connection=True;TrustServerCertificate=True;"

function Step($text) { Write-Host "`n==> $text" -ForegroundColor Cyan }

function Invoke-Sql($query) {
    # sqlcmd SQL Server ile birlikte kurulur. -b: hata olursa çıkış kodu 1
    & sqlcmd -S $Server -E -C -b -Q $query | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "sqlcmd başarısız: $query" }
}

function Remove-TestDatabase {
    Invoke-Sql "IF DB_ID('$Database') IS NOT NULL BEGIN ALTER DATABASE [$Database] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$Database]; END"
}

# Python: Windows'ta 'py' başlatıcısı ya da 'python'
$python = @("py", "python", "python3") | Where-Object { Get-Command $_ -ErrorAction SilentlyContinue } | Select-Object -First 1
if (-not $python) { throw "Python bulunamadı. https://www.python.org adresinden kurun (pip paketi gerekmez)." }
if (-not (Get-Command sqlcmd -ErrorAction SilentlyContinue)) { throw "sqlcmd bulunamadı. SQL Server komut satırı araçlarını kurun." }

$processes = @()
$savedEnvironment = $env:ASPNETCORE_ENVIRONMENT
$exitCode = 1

try {
    Step "Derleniyor"
    if (Test-Path $work) { Remove-Item -Recurse -Force $work }
    & dotnet build (Join-Path $root "Futbolio.API\Futbolio.API.csproj") -nologo -v q -o (Join-Path $work "api")
    if ($LASTEXITCODE -ne 0) { throw "API derlenemedi." }
    & dotnet build (Join-Path $root "Futbolio.WebUI\Futbolio.WebUI.csproj") -nologo -v q -o (Join-Path $work "web")
    if ($LASTEXITCODE -ne 0) { throw "WebUI derlenemedi." }

    Step "Test veritabanı hazırlanıyor: $Database ($Server)"
    Remove-TestDatabase

    Step "Sunucular başlatılıyor (API $apiUrl, WebUI $webUrl)"
    # Ortam değişkenleri appsettings.json'daki değerlerin yerine geçer; dosyalar değişmez.
    # Başlatılan süreçler bu değişkenleri bu PowerShell oturumundan devralır.
    $savedEnvironment = $env:ASPNETCORE_ENVIRONMENT
    $env:ASPNETCORE_ENVIRONMENT = "Development"
    $env:ConnectionStrings__DefaultConnection = $connection
    $env:ApiSettings__BaseUrl = "$apiUrl/api/"

    $processes += Start-Process dotnet -PassThru -NoNewWindow `
        -WorkingDirectory (Join-Path $work "api") `
        -ArgumentList "Futbolio.API.dll", "--urls", $apiUrl `
        -RedirectStandardOutput (Join-Path $work "api.log") -RedirectStandardError (Join-Path $work "api.err.log")

    # WebUI'ın wwwroot ve view'ları proje klasöründe; --contentRoot ile oraya bakmasını söylüyoruz
    $processes += Start-Process dotnet -PassThru -NoNewWindow `
        -WorkingDirectory (Join-Path $work "web") `
        -ArgumentList "Futbolio.WebUI.dll", "--urls", $webUrl, "--contentRoot", "`"$(Join-Path $root 'Futbolio.WebUI')`"" `
        -RedirectStandardOutput (Join-Path $work "web.log") -RedirectStandardError (Join-Path $work "web.err.log")

    Step "Testler çalışıyor"
    & $python (Join-Path $PSScriptRoot "e2e_test.py") --web $webUrl --api "$apiUrl/api"
    $exitCode = $LASTEXITCODE
}
finally {
    Step "Temizlik"
    foreach ($p in $processes) {
        if ($p -and -not $p.HasExited) { Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue }
    }
    # Bu oturumdaki ortam değişkenlerini eski haline getir
    Remove-Item Env:ConnectionStrings__DefaultConnection, Env:ApiSettings__BaseUrl -ErrorAction SilentlyContinue
    $env:ASPNETCORE_ENVIRONMENT = $savedEnvironment

    if ($KeepDatabase) {
        Write-Host "Veritabanı bırakıldı: $Database"
    }
    else {
        Start-Sleep -Milliseconds 500
        try { Remove-TestDatabase; Write-Host "Test veritabanı silindi: $Database" } catch { Write-Warning $_ }
    }

    if ($exitCode -eq 0) {
        Remove-Item -Recurse -Force $work -ErrorAction SilentlyContinue
    }
    else {
        Write-Host "Sunucu logları: $work (api.log, web.log)" -ForegroundColor Yellow
    }
}

if ($exitCode -eq 0) { Write-Host "`nTÜM TESTLER GEÇTİ" -ForegroundColor Green }
else { Write-Host "`nBAŞARISIZ TEST VAR" -ForegroundColor Red }
exit $exitCode
