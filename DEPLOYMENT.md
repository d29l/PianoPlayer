# Deployment Guide

This guide explains how to build and distribute the PianoApp as a standalone executable that users can run without installing .NET or any dependencies.

## Quick Start

### For Developers - Building the Executable

1. **Open a terminal in the PianoApp directory**
2. **Run the publish script:**
   ```bash
   # Using PowerShell (recommended)
   .\publish.ps1
   
   # OR using Command Prompt
   .\publish.bat
   
   # OR manually with dotnet CLI
   dotnet publish --configuration Release --runtime win-x64 --self-contained true --output publish
   ```

3. **Find your executable:**
   - Location: `.\publish\PianoApp.exe`
   - Size: ~100-150 MB (includes .NET runtime and all dependencies)
   - Ready to distribute!

### For End Users - Running the Application

**No installation required!** Just:
1. Download `PianoApp.exe`
2. Double-click to run
3. That's it!

**System Requirements:**
- Windows 10/11 (64-bit)
- WebView2 Runtime (automatically installed on Windows 11, usually present on Windows 10)

## Publishing Configuration

The application is configured in `PianoApp.csproj` with the following settings:

### Self-Contained Deployment
- **SelfContained**: `true` - Includes .NET 8.0 runtime
- **RuntimeIdentifier**: `win-x64` - Windows 64-bit
- **PublishSingleFile**: `true` - Everything in one .exe file
- **PublishReadyToRun**: `true` - Faster startup time
- **IncludeNativeLibrariesForSelfExtract**: `true` - Includes native DLLs
- **EnableCompressionInSingleFile**: `true` - Reduces file size

### What's Included
✅ .NET 8.0 Runtime
✅ All NuGet packages (NAudio, InputSimulatorCore, WebView2)
✅ All application code and resources
✅ Web assets (HTML, CSS, JS)
✅ Tailwind CSS executable

## Build Configurations

### Release Build (Recommended for Distribution)
```bash
dotnet publish --configuration Release --runtime win-x64 --self-contained true --output publish
```
- Optimized for performance
- Smallest file size
- No debug symbols
- Ready for end users

### Debug Build (For Testing)
```bash
dotnet publish --configuration Debug --runtime win-x64 --self-contained true --output publish-debug
```
- Includes debug symbols
- Easier to troubleshoot
- Larger file size

### Platform-Specific Builds

#### Windows 64-bit (default)
```bash
--runtime win-x64
```

#### Windows 32-bit
```bash
--runtime win-x86
```

#### Windows ARM64
```bash
--runtime win-arm64
```

## Distribution Options

### Option 1: Direct Download
1. Build the executable using `publish.ps1` or `publish.bat`
2. Upload `PianoApp.exe` to:
   - GitHub Releases
   - Your website
   - File sharing service
3. Users download and run directly

**Pros:** Simple, no installer needed
**Cons:** Large file size (~100-150 MB)

### Option 2: ZIP Archive
1. Build the executable
2. Create a ZIP file:
   ```powershell
   Compress-Archive -Path publish\PianoApp.exe -DestinationPath PianoApp-v1.0.0.zip
   ```
3. Distribute the ZIP file

**Pros:** Slightly smaller download, users can extract anywhere
**Cons:** Extra step for users

### Option 3: Installer (MSI/NSIS)
Create a Windows installer using:
- **WiX Toolset** - Creates MSI installers
- **Inno Setup** - Free installer creator
- **NSIS** - Nullsoft Scriptable Install System

**Pros:** Professional appearance, Start Menu shortcuts, uninstaller
**Cons:** More complex setup, requires installer creation

### Option 4: Microsoft Store
Package as an MSIX and publish to Microsoft Store

**Pros:** Automatic updates, trusted distribution
**Cons:** Requires Microsoft Store account, submission process

## Optimization Tips

### Reduce File Size

1. **Trim Unused Assemblies**
   ```xml
   <PropertyGroup>
     <PublishTrimmed>true</PublishTrimmed>
   </PropertyGroup>
   ```
   ⚠️ Warning: May break reflection-based code

2. **Use Framework-Dependent Deployment**
   ```xml
   <SelfContained>false</SelfContained>
   ```
   ⚠️ Users must install .NET 8.0 Runtime

3. **Compress Further**
   ```xml
   <EnableCompressionInSingleFile>true</EnableCompressionInSingleFile>
   ```
   Already enabled in current configuration

### Improve Startup Time

1. **ReadyToRun Compilation** (already enabled)
   ```xml
   <PublishReadyToRun>true</PublishReadyToRun>
   ```

2. **Ahead-of-Time (AOT) Compilation**
   Not currently supported for WPF applications

## WebView2 Runtime Requirement

The application requires **Microsoft Edge WebView2 Runtime**.

### Checking if WebView2 is Installed
Most modern Windows systems (Windows 11, updated Windows 10) have it pre-installed.

Users can check:
1. Look for "Microsoft Edge WebView2 Runtime" in installed programs
2. Check: `C:\Program Files (x86)\Microsoft\EdgeWebView\Application`

### Installing WebView2 (If Needed)
If a user doesn't have WebView2:
1. Download the Evergreen Bootstrapper:
   - https://go.microsoft.com/fwlink/p/?LinkId=2124703
2. Run the installer (small ~1MB download)
3. Restart PianoApp

### Bundling WebView2 with Your App
You can include the WebView2 Runtime installer with your distribution:

1. Download the Evergreen Standalone Installer
2. Include it in your distribution package
3. Add instructions for users to run it if the app doesn't start

## Troubleshooting

### "This app can't run on your PC"
- User needs 64-bit Windows
- Try building for `win-x86` for 32-bit systems

### "Missing DLL" errors
- Make sure `IncludeNativeLibrariesForSelfExtract` is `true`
- Rebuild with `--self-contained true`

### Large file size
- Expected: 100-150 MB for self-contained deployment
- Includes entire .NET runtime
- Consider framework-dependent deployment if size is critical

### Slow startup
- First run is slower (extraction of embedded files)
- Enable `PublishReadyToRun` for faster startup
- Subsequent runs are faster

### Antivirus false positives
- Self-contained executables may trigger antivirus
- Code sign your executable (requires certificate)
- Submit to antivirus vendors for whitelisting

## Code Signing (Optional but Recommended)

Code signing prevents "Unknown Publisher" warnings and builds trust.

### Get a Code Signing Certificate
- Purchase from: DigiCert, Sectigo, GlobalSign
- Cost: ~$100-500/year

### Sign the Executable
```powershell
signtool sign /f certificate.pfx /p password /t http://timestamp.digicert.com publish\PianoApp.exe
```

### Benefits
- No "Unknown Publisher" warning
- Builds user trust
- Required for some antivirus whitelisting

## Version Management

Update version number in `PianoApp.csproj`:

```xml
<PropertyGroup>
  <Version>1.0.0</Version>
</PropertyGroup>
```

Version appears in:
- File properties
- About dialog (if implemented)
- Installer metadata

## Release Checklist

Before releasing a new version:

- [ ] Update version number in `PianoApp.csproj`
- [ ] Test the application thoroughly
- [ ] Run `publish.ps1` to build release executable
- [ ] Test the published executable on a clean system
- [ ] Verify WebView2 is handled gracefully
- [ ] Check file size is reasonable
- [ ] Test on different Windows versions (if possible)
- [ ] Update README.md with any new features
- [ ] Create release notes (CHANGELOG.md)
- [ ] Tag the release in version control
- [ ] Upload to distribution platform (GitHub Releases, etc.)
- [ ] (Optional) Code sign the executable
- [ ] (Optional) Create installer package

## GitHub Releases Example

1. **Build the executable:**
   ```bash
   .\publish.ps1
   ```

2. **Create a GitHub release:**
   - Go to your repository
   - Click "Releases" → "Create a new release"
   - Tag: `v1.0.0`
   - Title: `PianoApp v1.0.0`
   - Description: Release notes

3. **Upload assets:**
   - `PianoApp.exe` (or `PianoApp-v1.0.0.exe`)
   - `PianoApp-v1.0.0.zip` (optional)
   - `CHANGELOG.md` (optional)

4. **Add installation instructions:**
   ```markdown
   ## Installation
   
   1. Download `PianoApp.exe`
   2. Run the executable (no installation required!)
   3. If prompted, install WebView2 Runtime
   
   ## What's New
   - Feature 1
   - Feature 2
   - Bug fixes
   ```

## Continuous Integration (Optional)

### GitHub Actions Example

Create `.github/workflows/build.yml`:

```yaml
name: Build Release

on:
  push:
    tags:
      - 'v*'

jobs:
  build:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: 8.0.x
      
      - name: Publish
        run: |
          dotnet publish PianoApp/PianoApp.csproj `
            --configuration Release `
            --runtime win-x64 `
            --self-contained true `
            --output publish
      
      - name: Upload Release Asset
        uses: actions/upload-artifact@v3
        with:
          name: PianoApp
          path: publish/PianoApp.exe
```

## Support and Updates

### Automatic Updates
Currently not implemented. Consider:
- **ClickOnce** deployment
- **Squirrel.Windows** - Open-source updater
- **AutoUpdater.NET** - NuGet package

### Manual Updates
Users must:
1. Download new version
2. Replace old executable
3. Restart application

### Preserving User Data
- Library data is stored separately (not in executable)
- Users can safely replace the .exe file
- Settings/library should persist across updates

## Summary

✅ **Self-contained deployment** - No .NET installation required
✅ **Single-file executable** - Easy distribution
✅ **WebView2 required** - Usually pre-installed on modern Windows
✅ **~100-150 MB file size** - Includes everything needed
✅ **Ready to distribute** - Just run `publish.ps1` and share the .exe

Users can simply download and run `PianoApp.exe` without any manual installation of dependencies!