# Hotkey System Documentation

This document describes the configurable hotkey system in the Piano App, which allows you to customize keyboard shortcuts for auto-play confirmation and manual playback control.

## Overview

The hotkey system provides three configurable keyboard shortcuts:

1. **Auto-Play Confirmation Key** - Start auto-play after switching to Virtual Piano
2. **Manual Forward Key** - Advance to next note in manual play mode
3. **Manual Backward Key** - Go back to previous note in manual play mode

## Features

### Wait-for-Confirmation Mode

When enabled, auto-play will **not** start immediately. Instead:

1. You click "Auto-Play" in the app
2. The app enters a "waiting" state
3. You switch to the Virtual Piano window (Alt+Tab)
4. Press the **Auto-Play Confirmation Key** (default: `=`)
5. Playback begins immediately

**Benefits:**
- No missed notes at the start of playback
- Time to position your window focus correctly
- Works with anti-cheat systems (no programmatic window focus)

### Manual Play Mode

In manual play mode, you control when each note is played:

- Press **Manual Forward Key** (default: `]`) to play the next note
- Press **Manual Backward Key** (default: `[`) to replay the previous note
- These keys work globally (even when Virtual Piano has focus)

## Configuration

### Via Settings UI

1. Click the **Settings** button (⚙️) in the sidebar
2. Scroll to the **Hotkeys** section
3. Click in the hotkey input field
4. Press the key you want to use
5. Settings are saved automatically

### Default Hotkeys

| Function | Default Key | Description |
|----------|-------------|-------------|
| Auto-Play Confirmation | `=` | Start playback when ready |
| Manual Forward | `]` | Next note in manual mode |
| Manual Backward | `[` | Previous note in manual mode |

### Recommended Keys

Good choices for hotkeys:
- **Number keys**: `1`, `2`, `3`, etc.
- **Bracket keys**: `[`, `]`, `{`, `}`
- **Symbols**: `=`, `-`, `;`, `'`
- **Letters**: Any letter that doesn't conflict with Virtual Piano

**Avoid:**
- Keys used by Virtual Piano (A-Z, 0-9 on the main keyboard)
- System shortcuts (Alt, Ctrl, Windows key)
- Commonly used application shortcuts

## How It Works

### Global Keyboard Hook

The app uses a low-level Windows keyboard hook to capture key presses even when the application is not focused. This allows you to:

- Press hotkeys while Virtual Piano has focus
- Control playback without switching windows
- Maintain natural playing position

### Backend Architecture

```
User presses key
    ↓
GlobalKeyboardHook captures event
    ↓
HotkeyManager routes to appropriate handler
    ↓
┌─────────────────────────────┐
│ Auto-Play Confirmation?     │ → MidiPlaybackService.ConfirmStart()
│ Manual Forward?             │ → ManualPlaybackManager.AdvanceNote()
│ Manual Backward?            │ → ManualPlaybackManager.PreviousNote()
└─────────────────────────────┘
```

## Usage Examples

### Example 1: Auto-Play with Confirmation

```
1. Load a MIDI file
2. Enable "Wait for Confirmation" in Settings
3. Click "Auto-Play"
4. Switch to Virtual Piano (Alt+Tab)
5. Press '=' key
6. Playback starts!
```

### Example 2: Manual Play Mode

```
1. Load a MIDI file
2. Select "Manual Play" from play mode dropdown
3. Click "Manual Play" to start
4. Press ']' to play each note at your own pace
5. Press '[' to replay notes if needed
```

### Example 3: Custom Hotkeys

```
1. Go to Settings → Hotkeys
2. Change Auto-Play Confirmation to '1'
3. Change Manual Forward to '2'
4. Change Manual Backward to '3'
5. Now use number keys for convenient control
```

## Technical Details

### Supported Keys

The following keys are supported for hotkeys:

- **Letters**: A-Z (uppercase)
- **Numbers**: 0-9
- **Symbols**: `;`, `=`, `,`, `-`, `.`, `/`, `` ` ``, `[`, `\`, `]`, `'`
- **Space**: Space bar

### Key Code Mapping

The app uses Windows Virtual Key (VK) codes and maps them to characters:

| Virtual Key | Character | Description |
|-------------|-----------|-------------|
| 0x30-0x39 | 0-9 | Number keys |
| 0x41-0x5A | A-Z | Letter keys |
| 0xBA | ; | Semicolon |
| 0xBB | = | Equals/Plus |
| 0xBC | , | Comma |
| 0xBD | - | Minus/Underscore |
| 0xBE | . | Period |
| 0xBF | / | Forward slash |
| 0xC0 | \` | Backtick/Tilde |
| 0xDB | [ | Left bracket |
| 0xDC | \\ | Backslash |
| 0xDD | ] | Right bracket |
| 0xDE | ' | Quote/Apostrophe |
| 0x20 | Space | Space bar |

### Storage

Hotkey settings are stored in two places:

1. **Browser localStorage** - For UI persistence
2. **Backend configuration** - For actual hotkey handling

Both are synchronized automatically when you change settings.

## Troubleshooting

### Hotkeys Not Working

**Problem**: Pressing hotkeys doesn't do anything

**Solutions**:
1. Check that the app is running (not just the browser)
2. Verify hotkey settings in Settings → Hotkeys
3. Make sure you're not using a system-reserved key
4. Restart the application

### Wrong Key Being Detected

**Problem**: Different key than expected triggers action

**Solutions**:
1. Use a simple key (letters, numbers, brackets)
2. Avoid modifier keys (Shift, Ctrl, Alt)
3. Check keyboard layout (US vs International)

### Confirmation Not Starting Playback

**Problem**: Pressing confirmation key doesn't start playback

**Solutions**:
1. Verify "Wait for Confirmation" is enabled
2. Check that auto-play is in "waiting" state
3. Press the correct confirmation key
4. Look for toast notifications showing status

## Best Practices

1. **Choose Unique Keys**: Pick keys that don't conflict with Virtual Piano
2. **Test First**: Try your hotkeys before starting a long song
3. **Muscle Memory**: Stick with the same hotkeys for consistency
4. **Ergonomic**: Choose keys that are easy to reach while playing
5. **Visual Cues**: Watch for toast notifications confirming actions

## API Reference

### Backend Endpoints

#### Get Hotkey Configuration
```http
GET /api/midi/hotkeys
```

Response:
```json
{
  "autoPlayConfirmKey": "=",
  "manualAdvanceForwardKey": "]",
  "manualAdvanceBackwardKey": "["
}
```

#### Set Hotkey Configuration
```http
POST /api/midi/hotkeys
Content-Type: application/json

{
  "autoPlayConfirmKey": "1",
  "manualAdvanceForwardKey": "2",
  "manualAdvanceBackwardKey": "3"
}
```

### Frontend API

```javascript
// Load hotkeys
const response = await fetch('/api/midi/hotkeys');
const config = await response.json();

// Save hotkeys
await fetch('/api/midi/hotkeys', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    autoPlayConfirmKey: '=',
    manualAdvanceForwardKey: ']',
    manualAdvanceBackwardKey: '['
  })
});
```

## Future Enhancements

Potential future improvements:

- [ ] Modifier key support (Ctrl+Key, Alt+Key)
- [ ] Multiple hotkey profiles (Gaming, Practice, Performance)
- [ ] Import/Export hotkey configurations
- [ ] Visual hotkey tester in settings
- [ ] Hotkey conflict detection
- [ ] Per-song hotkey overrides
- [ ] Gamepad/MIDI controller support

## See Also

- [README.md](README.md) - Main application documentation
- [AUTO_PLAY.md](AUTO_PLAY.md) - Auto-play feature details
- [PERFORMANCE_OPTIMIZATIONS.md](PERFORMANCE_OPTIMIZATIONS.md) - Performance tuning

## Support

If you encounter issues with hotkeys:

1. Check this documentation
2. Review the troubleshooting section
3. Verify your settings configuration
4. Check browser console for errors
5. Report bugs with detailed reproduction steps