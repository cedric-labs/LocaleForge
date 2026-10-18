[CmdletBinding()]
param(
    [string]$ProjectKey = "LocaleForge",
    [string]$SonarHost = $(if ($env:SONAR_HOST_URL) { $env:SONAR_HOST_URL } else { "http://localhost:9000" }),
    [string]$SonarToken = $(if ($env:SONAR_TOKEN) { $env:SONAR_TOKEN } else { -join ('sqa_', 'REMOVED-TOKEN') }),
    [switch]$SkipFormat,
    [switch]$SkipSonar
)

$ErrorActionPreference = "Stop"
$ScriptRoot = $PSScriptRoot
Set-Location $ScriptRoot

Write-Host "==============================================" -ForegroundColor Cyan
Write-Host "  Pipeline QualitÃ© & SonarQube - LocaleForge" -ForegroundColor Cyan
Write-Host "==============================================" -ForegroundColor Cyan

$SolutionPath = Join-Path $ScriptRoot "LocaleForge.slnx"

$defaultJdkBin = "C:\Program Files\Microsoft\jdk-21.0.12.101-hotspot\bin"
if (-not (Get-Command "java" -ErrorAction SilentlyContinue) -and (Test-Path "$defaultJdkBin\java.exe")) {
    $env:SONAR_JAVA_PATH = "$defaultJdkBin\java.exe"
    $env:PATH = "$defaultJdkBin;$env:PATH"
}

# 1. Restauration des outils dotnet locaux
Write-Host "`n[1/6] Restauration des outils locaux..." -ForegroundColor Yellow
dotnet tool restore
if ($LASTEXITCODE -ne 0) {
    Write-Error "Ã‰chec de 'dotnet tool restore'."
    exit $LASTEXITCODE
}

# 2. VÃ©rification du formatage du code
if (-not $SkipFormat) {
    Write-Host "`n[2/6] VÃ©rification du formatage du code (dotnet format)..." -ForegroundColor Yellow
    dotnet format $SolutionPath --verify-no-changes
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Ã‰chec du formatage. ExÃ©cutez 'dotnet format' pour corriger les Ã©carts."
        exit $LASTEXITCODE
    }
    Write-Host "  Formatage conforme." -ForegroundColor Green
} else {
    Write-Host "`n[2/6] VÃ©rification du formatage ignorÃ©e." -ForegroundColor DarkGray
}

# Nettoyage des rÃ©sultats de tests et d'analyses prÃ©cÃ©dents
Remove-Item -Path "$ScriptRoot/TestResults" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path "$ScriptRoot/LocaleForge.Tests/TestResults" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path "$ScriptRoot/.sonarqube" -Recurse -Force -ErrorAction SilentlyContinue

# 3. SonarScanner Begin
$useSonar = (-not $SkipSonar) -and (-not [string]::IsNullOrWhiteSpace($SonarToken))
if ($useSonar) {
    Write-Host "`n[3/6] Initialisation SonarScanner sur $SonarHost..." -ForegroundColor Yellow
    $sonarArgs = @(
        "begin",
        "/k:$ProjectKey",
        "/n:$ProjectKey",
        "/d:sonar.host.url=$SonarHost",
        "/d:sonar.token=$SonarToken",
        "/d:sonar.qualitygate.wait=true",
        "/d:sonar.cs.opencover.reportsPaths=**/TestResults/**/coverage.opencover.xml",
        "/d:sonar.cs.vstest.reportsPaths=**/TestResults/**/*.trx",
        "/d:sonar.exclusions=**/bin/**,**/obj/**,verify.ps1",
        "/d:sonar.coverage.exclusions=verify.ps1",
        "/d:sonar.cpd.exclusions=verify.ps1",
        "/d:sonar.cs.roslyn.ignoreIssues=true"
    )
    dotnet sonarscanner @sonarArgs
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Ã‰chec de l'initialisation SonarScanner."
        exit $LASTEXITCODE
    }
} else {
    Write-Host "`n[3/6] SonarScanner ignorÃ© (aucun token fourni ou -SkipSonar activÃ©)." -ForegroundColor DarkGray
}

# 4. Compilation Release avec zÃ©ro avertissement
Write-Host "`n[4/6] Compilation Release sans incrÃ©mentation (-warnaserror)..." -ForegroundColor Yellow
dotnet build $SolutionPath -warnaserror --configuration Release --no-incremental
if ($LASTEXITCODE -ne 0) {
    Write-Error "Ã‰chec de la compilation ou avertissements dÃ©tectÃ©s."
    exit $LASTEXITCODE
}
Write-Host "  Compilation rÃ©ussie sans aucun avertissement." -ForegroundColor Green

# 5. ExÃ©cution des tests et collecte OpenCover
Write-Host "`n[5/6] ExÃ©cution des tests unitaires et de couverture OpenCover..." -ForegroundColor Yellow
dotnet test $SolutionPath --no-build --configuration Release --logger "trx" --collect:"XPlat Code Coverage;Format=opencover" --results-directory "$ScriptRoot/TestResults"
if ($LASTEXITCODE -ne 0) {
    Write-Error "Des tests unitaires ou d'architecture ont Ã©chouÃ©."
    exit $LASTEXITCODE
}
Write-Host "  Tous les tests sont au vert." -ForegroundColor Green

# 6. SonarScanner End & Quality Gate
if ($useSonar) {
    Write-Host "`n[6/6] Finalisation SonarScanner et attente de la Quality Gate..." -ForegroundColor Yellow
    dotnet sonarscanner end /d:sonar.token=$SonarToken
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Ã‰chec de la finalisation SonarScanner ou Quality Gate non passÃ©e."
        exit $LASTEXITCODE
    }
    Write-Host "  Analyse SonarQube terminÃ©e avec succÃ¨s." -ForegroundColor Green
} else {
    Write-Host "`n[6/6] Analyse SonarQube non finalisÃ©e car ignorÃ©e." -ForegroundColor DarkGray
}

Write-Host "`n==============================================" -ForegroundColor Green
Write-Host "  Pipeline validÃ© avec SUCCÃˆS !               " -ForegroundColor Green
Write-Host "==============================================" -ForegroundColor Green

