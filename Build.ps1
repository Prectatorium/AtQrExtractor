#!/usr/bin/env pwsh
#
# Build script for AtQrExtractor
# This script builds the release executable for Windows
#

$ErrorActionPreference = "Stop"

# Color codes for output
$Green = "`e[32m"
$Yellow = "`e[33m"
$Red = "`e[31m"
$Reset = "`e[0m"

function Write-Header {
    param([string]$Message)
    Write-Host ""
    Write-Host ("=" * 60) -ForegroundColor Cyan
    Write-Host "  $Message" -ForegroundColor Cyan
    Write-Host ("=" * 60) -ForegroundColor Cyan
    Write-Host ""
}

function Write-Success {
    param([string]$Message)
    Write-Host "$Green[OK]$Reset $Message"
}

function Write-Warning {
    param([string]$Message)
    Write-Host "$Yellow[WARNING]$Reset $Message"
}

function Write-Error {
    param([string]$Message)
    Write-Host "$Red[ERROR]$Reset $Message"
}

# ============================================================================
# Build Script
# ============================================================================

Write-Header "AT QR Extractor - Build Script"

# Step 1: Check prerequisites
Write-Host "Step 1: Checking prerequisites..." -ForegroundColor Magenta

# Check .NET SDK
try {
    $dotnetVersion = dotnet --version 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Success ".NET SDK version: $dotnetVersion"
    }
    else {
        Write-Error ".NET SDK not found. Please install .NET 8.0 SDK from https://dotnet.microsoft.com/download"
        exit 1
    }
}
catch {
    Write-Error ".NET SDK not found. Please install .NET 8.0 SDK from https://dotnet.microsoft.com/download"
    exit 1
}

# Step 2: Clean previous build artifacts
Write-Host "`nStep 2: Cleaning previous build artifacts..." -ForegroundColor Magenta

$artifactsDir = "artifacts"
if (Test-Path $artifactsDir) {
    Write-Host "  Removing $artifactsDir..." -ForegroundColor Yellow
    Remove-Item -Recurse -Force $artifactsDir -ErrorAction SilentlyContinue | Out-Null
}

$binDir = "bin"
if (Test-Path $binDir) {
    Write-Host "  Removing $binDir..." -ForegroundColor Yellow
    Remove-Item -Recurse -Force $binDir -ErrorAction SilentlyContinue | Out-Null
}

Write-Success "Cleaned build artifacts"

# Step 3: Restore dependencies
Write-Host "`nStep 3: Restoring dependencies..." -ForegroundColor Magenta

try {
    dotnet restore
    if ($LASTEXITCODE -eq 0) {
        Write-Success "Dependencies restored successfully"
    }
    else {
        Write-Error "Failed to restore dependencies"
        exit 1
    }
}
catch {
    Write-Error "Failed to restore dependencies: $_"
    exit 1
}

# Step 4: Build the project
Write-Host "`nStep 4: Building the project..." -ForegroundColor Magenta

try {
    dotnet build --configuration Release --no-restore
    if ($LASTEXITCODE -eq 0) {
        Write-Success "Project built successfully"
    }
    else {
        Write-Error "Build failed"
        exit 1
    }
}
catch {
    Write-Error "Build failed: $_"
    exit 1
}

# Step 5: Publish self-contained executable
Write-Host "`nStep 5: Publishing self-contained Windows executable..." -ForegroundColor Magenta

$publishDir = "$artifactsDir\publish"

try {
    dotnet publish `
        --configuration Release `
        --runtime win-x64 `
        --self-contained true `
        --no-build `
        --output $publishDir

    if ($LASTEXITCODE -eq 0) {
        Write-Success "Published to $publishDir"
    }
    else {
        Write-Error "Publish failed"
        exit 1
    }
}
catch {
    Write-Error "Publish failed: $_"
    exit 1
}

# Step 6: Verify output
Write-Host "`nStep 6: Verifying output..." -ForegroundColor Magenta

$exePath = "$publishDir\atqr.exe"
if (Test-Path $exePath) {
    $exeSize = (Get-Item $exePath).Length / 1MB
    Write-Success "Executable created: $exePath ($([math]::Round($exeSize, 2)) MB)"
}
else {
    Write-Error "Executable not found at expected location: $exePath"
    exit 1
}

# Step 7: Display summary
Write-Header "Build Complete"

Write-Host "Output files:" -ForegroundColor White
Get-ChildItem $publishDir | ForEach-Object {
    $size = $_.Length / 1KB
    Write-Host "  - $($_.Name) ($([math]::Round($size, 1)) KB)" -ForegroundColor Gray
}

Write-Host ""
Write-Host "Usage examples:" -ForegroundColor White
Write-Host "  # Extract QR codes from files" -ForegroundColor Gray
Write-Host "  $exePath extract --input 'C:\invoices' --output 'C:\reports'" -ForegroundColor Gray
Write-Host ""
Write-Host "  # Show help" -ForegroundColor Gray
Write-Host "  $exePath extract --help" -ForegroundColor Gray
Write-Host ""

# Run help command to verify it works
Write-Host "Verifying executable works:" -ForegroundColor Magenta
try {
    & $exePath extract --help 2>&1 | Out-Null
    Write-Success "Executable is functional"
}
catch {
    Write-Warning "Could not verify executable: $_"
}

Write-Host ""
Write-Host "Build completed successfully!" -ForegroundColor Green
