# =============================================
# Script: RunSetup.ps1
# Description: Script PowerShell per eseguire il setup del database DocN
# =============================================

param(
    [Parameter(Mandatory=$true)]
    [string]$ServerName,
    
    [Parameter(Mandatory=$true)]
    [string]$DatabaseName,
    
    [Parameter(Mandatory=$false)]
    [string]$Username,
    
    [Parameter(Mandatory=$false)]
    [string]$Password,
    
    [Parameter(Mandatory=$false)]
    [switch]$UseWindowsAuth
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Setup Database DocN" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Server: $ServerName" -ForegroundColor Yellow
Write-Host "Database: $DatabaseName" -ForegroundColor Yellow
Write-Host ""

# Costruisci la stringa di connessione
if ($UseWindowsAuth) {
    Write-Host "Autenticazione: Windows" -ForegroundColor Green
    $connectionString = "Server=$ServerName;Database=$DatabaseName;Integrated Security=True;"
    $sqlcmdAuth = "-E"
} else {
    if ([string]::IsNullOrEmpty($Username) -or [string]::IsNullOrEmpty($Password)) {
        Write-Host "ERRORE: Username e Password sono richiesti per l'autenticazione SQL" -ForegroundColor Red
        exit 1
    }
    Write-Host "Autenticazione: SQL Server" -ForegroundColor Green
    $sqlcmdAuth = "-U $Username -P $Password"
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan

# Array degli script da eseguire
$scripts = @(
    "01_CreateIdentityTables.sql",
    "02_CreateDocumentTables.sql",
    "03_ConfigureFullTextSearch.sql"
)

# Esegui ogni script
$success = $true
foreach ($script in $scripts) {
    $scriptPath = Join-Path $PSScriptRoot $script
    
    if (-not (Test-Path $scriptPath)) {
        Write-Host "ERRORE: Script $script non trovato in $PSScriptRoot" -ForegroundColor Red
        $success = $false
        break
    }
    
    Write-Host ""
    Write-Host "Esecuzione: $script" -ForegroundColor Yellow
    Write-Host "----------------------------------------" -ForegroundColor Gray
    
    try {
        if ($UseWindowsAuth) {
            sqlcmd -S $ServerName -d $DatabaseName -E -i $scriptPath -b
        } else {
            sqlcmd -S $ServerName -d $DatabaseName -U $Username -P $Password -i $scriptPath -b
        }
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✓ $script completato con successo" -ForegroundColor Green
        } else {
            Write-Host "✗ $script fallito con codice di errore: $LASTEXITCODE" -ForegroundColor Red
            $success = $false
            break
        }
    } catch {
        Write-Host "✗ Errore durante l'esecuzione di $script`: $_" -ForegroundColor Red
        $success = $false
        break
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan

if ($success) {
    Write-Host "✅ Setup Database DocN completato con successo!" -ForegroundColor Green
} else {
    Write-Host "❌ Setup Database DocN fallito. Verificare gli errori sopra." -ForegroundColor Red
    exit 1
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Esempi di utilizzo
Write-Host "Esempi di utilizzo:" -ForegroundColor Cyan
Write-Host ""
Write-Host "# Windows Authentication:" -ForegroundColor Gray
Write-Host ".\RunSetup.ps1 -ServerName ""localhost"" -DatabaseName ""DocN"" -UseWindowsAuth" -ForegroundColor Gray
Write-Host ""
Write-Host "# SQL Server Authentication:" -ForegroundColor Gray
Write-Host ".\RunSetup.ps1 -ServerName ""localhost"" -DatabaseName ""DocN"" -Username ""sa"" -Password ""YourPassword""" -ForegroundColor Gray
Write-Host ""
