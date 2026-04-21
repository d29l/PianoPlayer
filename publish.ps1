# PowerShell script to publish PianoApp as a standalone executable
# This creates a self-contained, single-file executable that users can run without installing .NET

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  MIDI to Virtual Piano Converter" -ForegroundColor Cyan
Write-Host "  Building Standalone Executable" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Clean previous builds
Write-Host "Cleaning previous builds..." -ForegroundColor Yellow
if (Test-Path "bin\Release") {
    Remove-Item -Path "bin\Release" -Recurse -Force
}
if (Test-Path "publish") {
    Remove-Item -Path "publish" -Recurse -Force
}

Write-Host "Done!" -ForegroundColor Green
Write-Host ""

# Publish the application
Write-Host "Publishing application..." -ForegroundColor Yellow
Write-Host "This may take a few minutes..." -ForegroundColor Gray

dotnet publish `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    --output publish `
    /p:PublishSingleFile=true `
    /p:IncludeNativeLibrariesForSelfExtract=true `
    /p:EnableCompressionInSingleFile=true `
    /p:PublishReadyToRun=true

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "  Build Successful!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Output location: " -NoNewline
    Write-Host ".\publish\PianoApp.exe" -ForegroundColor Cyan
    Write-Host ""

    # Get file size
    $fileSize = (Get-Item "publish\PianoApp.exe").Length / 1MB
    Write-Host "Executable size: " -NoNewline
    Write-Host ("{0:N2} MB" -f $fileSize) -ForegroundColor Cyan
    Write-Host ""

    Write-Host "The executable is fully self-contained and can be distributed to users." -ForegroundColor Gray
    Write-Host "Users do NOT need to install .NET Runtime." -ForegroundColor Gray
    Write-Host ""
    Write-Host "Note: WebView2 Runtime is still required (most Windows 10/11 systems have it)." -ForegroundColor Yellow
    Write-Host ""
} else {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "  Build Failed!" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please check the error messages above." -ForegroundColor Red
    exit 1
}
