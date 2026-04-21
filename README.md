# MIDI to Virtual Piano Sheet Converter

A Windows desktop application that converts MIDI files into virtual piano sheet notation, using the same key mapping as popular virtual piano games.

## 📥 Installation (For Users)

**No installation required!** Just download and run:

1. **Download** the latest `PianoApp.exe` from the [Releases](../../releases) page
2. **Double-click** to run the application
3. **That's it!** Start converting MIDI files

### System Requirements
- Windows 10 or Windows 11 (64-bit)
- WebView2 Runtime (pre-installed on Windows 11, usually present on Windows 10)

> **Note:** If the app doesn't start, you may need to install [WebView2 Runtime](https://go.microsoft.com/fwlink/p/?LinkId=2124703) (small ~1MB download).

### ✅ What's Included
- Complete self-contained application
- No .NET installation required
- No manual dependency installation
- All features ready to use

---

## Features

- 🎹 **MIDI File Parsing** - Uploads and parses MIDI files
- 📄 **Sheet Music Display** - Shows converted notes in readable format
- 🎼 **Virtual Piano Mapping** - Uses standard `1!2@34$5%6^78*9(0qQwWeErtTyYuiIoOpPasSdDfgGhHjJklLzZxcCvVbBnm` key layout
- 📋 **Copy to Clipboard** - Easy copying of sheet and note data
- 🎵 **Chord Support** - Displays chords in brackets `[abc]`
- 🎮 **Auto-Play** - Automatically plays MIDI files with humanized keyboard input
- 💾 **Library System** - Save and manage your converted sheets
- 🎨 **Catppuccin Mocha Theme** - Beautiful, modern dark theme

## How It Works

1. **Upload** a MIDI file (.mid, .midi)
2. **Convert** - The app parses the MIDI and extracts all notes
3. **View** the converted sheet music notation
4. **Auto-Play** - Let the app play it for you with humanized timing
5. **Copy** the sheet or note list to use in virtual piano games
6. **Save** - Store your favorite conversions in the library

## Virtual Piano Key Mapping

The converter uses a 61-key virtual piano layout:

```
1!2@34$5%6^78*9(0qQwWeErtTyYuiIoOpPasSdDfgGhHjJklLzZxcCvVbBnm
```

### Key Layout
- **1234567890** - Numbers (lower octave)
- **!@$%^*()** - Shift + numbers (black keys/sharps)
- **qwertyuiop** - QWERTY row (middle range)
- **QWERTYIOP** - Shift + QWERTY (uppercase)
- **asdfghjkl** - ASDF row
- **SDGHJL** - Shift + ASDF (uppercase)
- **zxcvbnm** - ZXCV row (lower range)
- **ZCVB** - Shift + ZXCV (uppercase)

### Mapping Algorithm
```
virtualKeyIndex = (midiNoteNumber - 23 - 12 - 1) % 61
```
This wraps any MIDI note (0-127) into the 61-key virtual piano range.

## Sheet Format

### Single Notes
```
a b c d e f g h
i j k l m n o p
```

### Chords (multiple keys pressed together)
```
[abc] d [ef] g h [ijk] l m
```

### Layout
- 8 notes per line
- Extra blank line every 32 notes (every 4 lines)

## Output Sections

### 1. MIDI Information
Displays:
- Track count
- Duration (in seconds)
- Tempo (BPM)
- Total note count

### 2. Converted Sheet
The main sheet music display:
- Visual representation like sheet music
- Single notes and chords
- Easy to read and copy

### 3. Note List (with timing)
Detailed list showing:
- Timing value
- Virtual piano key
- Original MIDI note number

Format: `<timing> <key> (MIDI: <noteNumber>)`

Example:
```
0.00    1       (MIDI: 60)
0.50    [abc]   (MIDI: 62)
1.00    d       (MIDI: 64)
```

## Auto-Play Feature

The application can automatically play MIDI files by simulating keyboard inputs with humanized timing that mimics natural human playing.

### How Auto-Play Works

1. Click **"Auto-Play"** button after converting a MIDI file
2. The app reads the timing data from the MIDI
3. Keyboard inputs are sent with humanization:
   - Adaptive hold times based on tempo
   - Random chord order (arpeggiation)
   - Micro-delays between chord notes (2-8ms)
   - Release timing jitter (6-26ms)
   - Tempo-responsive adjustments

### Humanization Features

#### Adaptive Timing
- **Fast sections** (< 240ms between notes): Shorter hold times (85-140ms)
- **Medium sections** (240-400ms): Interpolated timing
- **Slow sections** (> 400ms): Longer hold times (180-320ms)

#### Chord Playing
- Keys pressed in random order
- 2-8ms delay between each key
- ±6ms release time variation
- Synchronized releases with natural variance

#### Natural Variations
- Release jitter: 6-26ms added to single notes
- Timing adjustments based on recent playing speed
- Real-time tempo adaptation

### Playback Controls

- **Auto-Play** - Start automatic playback
- **Stop** - Stop playback at any time
- Status polling every 500ms for progress updates
- Visual feedback via toast notifications

### Use Cases

1. **Practice Helper** - Listen to how the piece should sound
2. **Virtual Piano Player** - Automatically play on virtualpiano.net or similar sites
3. **Demonstration** - Show others what the converted sheet sounds like
4. **Testing** - Verify the conversion is correct

### Technical Implementation

- Uses Windows SendInput API for keyboard simulation
- Runs on background thread to avoid blocking UI
- Calculates precise timing from MIDI data
- Maintains drift compensation for accuracy
- Thread-safe pause/resume functionality

For detailed technical documentation, see [AUTO_PLAY.md](AUTO_PLAY.md).

## Technical Details

### Built With
- **.NET 8.0** - Windows desktop framework
- **WPF** - Native Windows UI
- **ASP.NET Core** - Embedded web server
- **WebView2** - Modern web rendering
- **NAudio** - MIDI file parsing
- **Tailwind CSS** - UI styling (Catppuccin Mocha theme)

### Architecture
```
┌─────────────────┐
│   WPF Window    │
│  (WebView2)     │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  Web Server     │
│  (localhost)    │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  MIDI Parser    │
│  (NAudio)       │
└─────────────────┘
```

### API Endpoints

#### `POST /api/midi/upload`
Uploads and converts a MIDI file.
- **Input**: MIDI file (multipart/form-data)
- **Returns**: 
  - File ID
  - MIDI info (tracks, duration, tempo, note count)
  - Converted sheet
  - Note list with timing

#### `GET /api/midi/sheet/{id}`
Retrieves the converted sheet for a previously uploaded file.
- **Input**: File ID
- **Returns**: Sheet and note list

#### `POST /api/midi/play/{id}`
Starts automatic playback of a MIDI file.
- **Input**: File ID
- **Returns**: Playback started confirmation

#### `POST /api/midi/pause/{id}`
Pauses current playback.

#### `POST /api/midi/resume/{id}`
Resumes paused playback.

#### `POST /api/midi/stop/{id}`
Stops playback and resets position.

#### `GET /api/midi/status/{id}`
Gets current playback status.
- **Returns**: isPlaying, isPaused, currentPosition, currentNoteIndex

## Project Structure

```
PianoApp/
├── Controllers/
│   └── MidiController.cs       # MIDI parsing, conversion, and playback
├── KeyboardInputSender.cs      # Windows SendInput API wrapper
├── MidiPlaybackService.cs      # Humanized playback engine
├── wwwroot/
│   ├── index.html              # UI layout
│   ├── js/
│   │   └── app.js              # Frontend logic
│   └── css/
│       ├── input.css           # Tailwind source
│       └── output.css          # Compiled styles
├── MainWindow.xaml             # WPF window definition
├── MainWindow.xaml.cs          # Window code-behind
├── WebServer.cs                # ASP.NET Core server setup
├── App.xaml                    # WPF application
└── PianoApp.csproj             # Project configuration
```

## Usage

### Running the Application
1. Build and run `PianoApp.exe`
2. The application window will open
3. Click "Click to upload" or drag a MIDI file
4. Click "Upload & Convert to Sheet"
5. View the converted sheet music

### Auto-Playing
1. Convert a MIDI file
2. Click **"Auto-Play"** button
3. Focus the target application (e.g., Virtual Piano website)
4. The app will automatically play the notes
5. Click **"Stop"** to stop playback

### Copying the Sheet
- Click **"Copy Sheet"** to copy the sheet music
- Paste into your virtual piano practice app or text editor

### Saving to Library
1. After converting, a save dialog appears
2. Enter a title for your sheet
3. Click **"Save"** to add to library
4. Access saved sheets from the sidebar
5. Click any saved sheet to view it instantly

### Best Practices
- **Simple melodies** work best for conversion
- **Piano arrangements** are ideal
- Files with **1-2 tracks** are easier to read
- **Classical pieces** often convert well

## 🛠️ Development

### For End Users
See the **Installation** section above - just download and run the executable!

### For Developers

#### Prerequisites
- .NET 8.0 SDK
- Windows 10/11

#### Building from Source
```bash
# Clone the repository
git clone <repository-url>
cd PianoApp

# Restore dependencies
dotnet restore

# Build
dotnet build

# Run
dotnet run
```

#### Creating Standalone Executable
```bash
# Using PowerShell (recommended)
.\publish.ps1

# OR using Command Prompt
.\publish.bat

# OR manually
dotnet publish --configuration Release --runtime win-x64 --self-contained true --output publish
```

The executable will be created at `.\publish\PianoApp.exe` (~100-150 MB, includes everything).

For detailed publishing instructions, see [DEPLOYMENT.md](DEPLOYMENT.md).

#### Compiling Tailwind CSS
```bash
./tailwindcss.exe -i wwwroot/css/input.css -o wwwroot/css/output.css
```

## Comparison with Python Version

This C# implementation provides the same MIDI-to-virtual-piano conversion as the Python reference implementation, with these enhancements:

| Feature | Python | C# Version |
|---------|--------|------------|
| Key Mapping | ✅ | ✅ Same |
| Sheet Generation | ✅ | ✅ Same format |
| Note Extraction | ✅ | ✅ Same algorithm |
| Chord Detection | ✅ | ✅ Enhanced |
| GUI | ❌ CLI only | ✅ Modern UI |
| Copy to Clipboard | ❌ | ✅ Yes |
| Real-time Display | ❌ | ✅ Yes |
| Platform | 🐍 Cross-platform | 🪟 Windows only |

## License

This project is for educational and personal use.

## Troubleshooting

### MIDI file won't upload
- Ensure the file has a `.mid` or `.midi` extension
- Check that the file isn't corrupted
- Try a different MIDI file to verify the app works

### Sheet looks wrong
- Some MIDI files have multiple tracks - the converter combines all tracks
- Complex orchestral MIDIs may produce very dense sheets
- Try piano-only MIDI files for best results

### Keys don't match my virtual piano
- This uses the standard 61-key layout
- Some virtual piano apps may use different mappings
- Check your virtual piano's key layout documentation

## Future Enhancements

Potential features to add:
- [ ] Visual keyboard display
- [ ] Export to text file (song.txt format)
- [ ] Track selection (choose which tracks to convert)
- [ ] Transposition (shift all notes up/down)
- [ ] Difficulty rating
- [ ] Preview audio playback
- [ ] Adjustable playback speed
- [ ] Custom humanization parameters
- [ ] Practice mode with looping
- [ ] MIDI output device support
- [ ] Cross-platform input simulation

## Credits

Based on the Python MIDI-to-virtual-piano converter algorithm, adapted for Windows with a modern UI.