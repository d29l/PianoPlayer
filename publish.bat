@echo off
REM Batch script to publish PianoApp as a standalone executable
REM This creates a self-contained, single-file executable that users can run without installing .NET

echo ========================================
echo   MIDI to Virtual Piano Converter
echo   Building Standalone Executable
echo ========================================
echo.

REM Clean previous builds
echo Cleaning previous builds...
if exist "bin\Release" rmdir /s /q "bin\Release"
if exist "publish" rmdir /s /q "publish"
echo Done!
echo.

REM Publish the application
echo Publishing application...
echo This may take a few minutes...
echo.

dotnet publish --configuration Release --runtime win-x64 --self-contained true --output publish /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true /p:EnableCompressionInSingleFile=true /p:PublishReadyToRun=true

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ========================================
    echo   Build Successful!
    echo ========================================
    echo.
    echo Output location: .\publish\PianoApp.exe
    echo.
    echo The executable is fully self-contained and can be distributed to users.
    echo Users do NOT need to install .NET Runtime.
    echo.
    echo Note: WebView2 Runtime is still required ^(most Windows 10/11 systems have it^).
    echo.
) else (
    echo.
    echo ========================================
    echo   Build Failed!
    echo ========================================
    echo.
    echo Please check the error messages above.
    pause
    exit /b 1
)

pause
