# Development startup script for DocN (Windows PowerShell)
# This script starts both the Backend API (DocN.Server) and Frontend (DocN.Client) servers

Write-Host "🚀 Starting DocN Development Environment..." -ForegroundColor Cyan
Write-Host ""

# Check if .NET SDK is installed
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host "❌ Error: .NET SDK is not installed." -ForegroundColor Red
    Write-Host "Please install .NET 10.0 SDK from https://dotnet.microsoft.com/download" -ForegroundColor Yellow
    exit 1
}

# Get the script directory
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

Write-Host "📦 Building projects..." -ForegroundColor Cyan
Set-Location $ScriptDir

# Build both projects to ensure they're up to date
Write-Host "Building DocN.Server..." -ForegroundColor Yellow
dotnet build DocN.Server\DocN.Server.csproj --no-incremental
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Failed to build DocN.Server" -ForegroundColor Red
    exit 1
}

Write-Host "Building DocN.Client..." -ForegroundColor Yellow
dotnet build DocN.Client\DocN.Client.csproj --no-incremental
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Failed to build DocN.Client" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Build completed successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "🌐 Starting servers in parallel..." -ForegroundColor Cyan
Write-Host "   - Backend API: https://localhost:5211" -ForegroundColor Yellow
Write-Host "   - Frontend UI: https://localhost:7114" -ForegroundColor Yellow
Write-Host ""
Write-Host "Press Ctrl+C to stop both servers" -ForegroundColor Yellow
Write-Host ""

# Start both servers in parallel using background jobs
$serverJob = Start-Job -ScriptBlock {
    param($dir)
    Set-Location "$dir\DocN.Server"
    dotnet run
} -ArgumentList $ScriptDir

# Wait a bit for the server to start
Start-Sleep -Seconds 3

$clientJob = Start-Job -ScriptBlock {
    param($dir)
    Set-Location "$dir\DocN.Client"
    dotnet run
} -ArgumentList $ScriptDir

# Monitor both jobs and display their output
try {
    while ($true) {
        # Check if any job failed
        if ($serverJob.State -eq 'Failed') {
            Write-Host "❌ Backend server failed!" -ForegroundColor Red
            Receive-Job $serverJob
            break
        }
        if ($clientJob.State -eq 'Failed') {
            Write-Host "❌ Frontend server failed!" -ForegroundColor Red
            Receive-Job $clientJob
            break
        }
        
        # Receive output from both jobs
        Receive-Job $serverJob
        Receive-Job $clientJob
        
        Start-Sleep -Seconds 1
    }
}
finally {
    Write-Host ""
    Write-Host "🛑 Stopping servers..." -ForegroundColor Yellow
    Stop-Job $serverJob, $clientJob
    Remove-Job $serverJob, $clientJob
    Write-Host "✅ Servers stopped." -ForegroundColor Green
}
