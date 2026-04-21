# Build Guide

Quick reference for building and publishing the PianoApp.

## For End Users

**Just download and run!** No building required.

Download `PianoApp.exe` from the [Releases](../../releases) page and double-click to run.

---

## For Developers

### Quick Start

```bash
# Clone and build
git clone <repository-url>
cd PianoApp
dotnet build
dotnet run
```

### Create Standalone Executable

#### Option 1: Using Scripts (Easiest)

**PowerShell (Recommended):**
```powershell
.\publish.ps1
```

**Command Prompt:**
```cmd
.\publish.bat
```

**Output:** `.\publish\PianoApp.exe`

#### Option 2: Manual Command

```bash
dotnet publish --configuration Release --runtime win-x64 --self-contained true --output publish
```

### What Gets Built

- **Self-contained executable**: Includes .NET 8.0 runtime
- **Single file**: Everything bundled into one .exe
- **Size**: ~100-150 MB
- **No dependencies**: Users don't need to install anything (except WebView2)

### Build Configurations

#### Release Build (For Distribution)
```bash
dotnet publish -c Release -r win-x64 --self-contained true -o publish
```
- Optimized performance
- Smaller size
- No debug symbols

#### Debug Build (For Testing)
```bash
dotnet publish -c Debug -r win-x64 --self-contained true -o publish-debug
```
- Includes debug symbols
- Easier troubleshooting
- Larger file size

### Platform Targets

#### Windows 64-bit (Default)
```bash
-r win-x64
```

#### Windows 32-bit
```bash
-r win-x86
```

#### Windows ARM64
```bash
-r win-arm64
```

### Development Workflow

1. **Make code changes**
2. **Test locally:**
   ```bash
   dotnet run
   ```
3. **Build release executable:**
   ```bash
   .\publish.ps1
   ```
4. **Test the published executable:**
   - Find it at `.\publish\PianoApp.exe`
   - Run on a clean system if possible

### Cleaning Build Artifacts

```bash
# Remove build outputs
dotnet clean

# Remove publish folder
rmdir /s /q publish
```

### Tailwind CSS

If you modify the CSS, recompile:

```bash
.\tailwindcss.exe -i wwwroot\css\input.css -o wwwroot\css\output.css
```

Watch mode (auto-recompile on changes):
```bash
.\tailwindcss.exe -i wwwroot\css\input.css -o wwwroot\css\output.css --watch
```

### Project Structure

```
PianoApp/
├── Controllers/          # API endpoints
├── wwwroot/             # Web assets (HTML, CSS, JS)
├── bin/                 # Build output (gitignored)
├── obj/                 # Build intermediates (gitignored)
├── publish/             # Published executable (gitignored)
├── PianoApp.csproj      # Project configuration
├── publish.ps1          # Build script (PowerShell)
└── publish.bat          # Build script (Batch)
```

### Prerequisites

- **.NET 8.0 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Windows 10/11** - Required for WPF
- **Visual Studio 2022** (optional) - For IDE development
- **VS Code** (optional) - Lightweight alternative

### IDE Setup

#### Visual Studio 2022
1. Open `PianoApp.csproj`
2. Build → Build Solution (Ctrl+Shift+B)
3. Debug → Start Debugging (F5)

#### Visual Studio Code
1. Install C# extension
2. Open folder in VS Code
3. Press F5 to run

#### Rider
1. Open `PianoApp.csproj`
2. Run → Run 'PianoApp'

### Troubleshooting

#### "SDK not found"
Install .NET 8.0 SDK from Microsoft

#### "Cannot find tailwindcss.exe"
The executable is included in the repo at `.\tailwindcss.exe`

#### "WebView2 not found" when running
Install WebView2 Runtime: https://go.microsoft.com/fwlink/p/?LinkId=2124703

#### Build succeeds but exe won't run
- Check you're building for correct platform (win-x64)
- Ensure all native libraries are included
- Try running on different Windows machine

#### Large exe size
This is normal for self-contained deployment (~100-150 MB includes .NET runtime)

### Performance Optimizations

Already enabled in `PianoApp.csproj`:
- ✅ `PublishSingleFile` - Single executable
- ✅ `PublishReadyToRun` - Faster startup
- ✅ `IncludeNativeLibrariesForSelfExtract` - Include all DLLs
- ✅ `EnableCompressionInSingleFile` - Compress embedded files

### Additional Resources

- **Full Deployment Guide**: See [DEPLOYMENT.md](DEPLOYMENT.md)
- **Application Documentation**: See [README.md](README.md)
- **Hotkey Reference**: See [HOTKEYS.md](HOTKEYS.md)

### Common Commands Cheat Sheet

```bash
# Development
dotnet build                    # Build project
dotnet run                      # Run application
dotnet clean                    # Clean build outputs

# Publishing
.\publish.ps1                   # Build standalone exe (PowerShell)
.\publish.bat                   # Build standalone exe (CMD)

# CSS
.\tailwindcss.exe -i wwwroot\css\input.css -o wwwroot\css\output.css         # Compile CSS
.\tailwindcss.exe -i wwwroot\css\input.css -o wwwroot\css\output.css --watch # Watch mode

# Testing
dotnet test                     # Run tests (if any)
```

---

**Ready to distribute?** See [DEPLOYMENT.md](DEPLOYMENT.md) for release checklist and distribution options.