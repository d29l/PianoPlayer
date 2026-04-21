using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace MidiToKeyboard;

/// <summary>
/// Manages configurable hotkeys for the application.
/// Handles auto-play confirmation and manual playback advancement keys.
/// </summary>
public class HotkeyManager
{
    private static HotkeyManager? _instance;
    private static readonly object _lock = new object();

    // Default hotkeys (can be customized via settings)
    public char AutoPlayConfirmKey { get; set; } = '='; // '=' key by default
    public char ManualAdvanceForwardKey { get; set; } = ']';
    public char ManualAdvanceBackwardKey { get; set; } = '[';

    // Track active playback services
    private readonly ConcurrentDictionary<string, MidiPlaybackService> _autoPlayServices;
    private string? _currentAutoPlayId;

    private HotkeyManager()
    {
        _autoPlayServices = new ConcurrentDictionary<string, MidiPlaybackService>();
    }

    /// <summary>
    /// Gets the singleton instance
    /// </summary>
    public static HotkeyManager Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new HotkeyManager();
                }
            }
            return _instance;
        }
    }

    /// <summary>
    /// Registers an auto-play service for hotkey handling
    /// </summary>
    public void RegisterAutoPlayService(string midiId, MidiPlaybackService service)
    {
        _autoPlayServices[midiId] = service;
        _currentAutoPlayId = midiId;
        Debug.WriteLine($"[HotkeyManager] Registered auto-play service for {midiId}");
    }

    /// <summary>
    /// Unregisters an auto-play service
    /// </summary>
    public void UnregisterAutoPlayService(string midiId)
    {
        _autoPlayServices.TryRemove(midiId, out _);

        if (_currentAutoPlayId == midiId)
        {
            _currentAutoPlayId = null;
        }

        Debug.WriteLine($"[HotkeyManager] Unregistered auto-play service for {midiId}");
    }

    /// <summary>
    /// Handles a global key press
    /// </summary>
    public void HandleKeyPress(char key)
    {
        Debug.WriteLine($"[HotkeyManager] Key pressed: {key}");

        // Check if this is the auto-play confirmation key
        if (key == AutoPlayConfirmKey)
        {
            HandleAutoPlayConfirmation();
            return;
        }

        // Check if this is a manual advance key
        if (key == ManualAdvanceForwardKey || key == ManualAdvanceBackwardKey)
        {
            HandleManualAdvance(key);
            return;
        }
    }

    /// <summary>
    /// Handles auto-play confirmation key press
    /// </summary>
    private void HandleAutoPlayConfirmation()
    {
        if (string.IsNullOrEmpty(_currentAutoPlayId))
        {
            Debug.WriteLine($"[HotkeyManager] Auto-play confirmation pressed but no active service");
            return;
        }

        if (!_autoPlayServices.TryGetValue(_currentAutoPlayId, out var service))
        {
            Debug.WriteLine($"[HotkeyManager] Service not found for {_currentAutoPlayId}");
            return;
        }

        if (!service.IsWaitingForConfirmation)
        {
            Debug.WriteLine($"[HotkeyManager] Service {_currentAutoPlayId} is not waiting for confirmation");
            return;
        }

        Debug.WriteLine($"[HotkeyManager] Confirming auto-play start for {_currentAutoPlayId}");

        try
        {
            service.ConfirmStart();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[HotkeyManager] Error confirming start: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles manual playback advancement key press
    /// </summary>
    private void HandleManualAdvance(char key)
    {
        // Forward to the ManualPlaybackManager
        ManualPlaybackManager.Instance.HandleKeyPress(key);
    }

    /// <summary>
    /// Sets the auto-play confirmation hotkey
    /// </summary>
    public void SetAutoPlayConfirmKey(char key)
    {
        AutoPlayConfirmKey = key;
        Debug.WriteLine($"[HotkeyManager] Auto-play confirm key set to: {key}");
    }

    /// <summary>
    /// Sets the manual advance forward hotkey
    /// </summary>
    public void SetManualAdvanceForwardKey(char key)
    {
        ManualAdvanceForwardKey = key;
        Debug.WriteLine($"[HotkeyManager] Manual advance forward key set to: {key}");
    }

    /// <summary>
    /// Sets the manual advance backward hotkey
    /// </summary>
    public void SetManualAdvanceBackwardKey(char key)
    {
        ManualAdvanceBackwardKey = key;
        Debug.WriteLine($"[HotkeyManager] Manual advance backward key set to: {key}");
    }

    /// <summary>
    /// Gets the current hotkey configuration as a dictionary
    /// </summary>
    public HotkeyConfig GetConfiguration()
    {
        return new HotkeyConfig
        {
            AutoPlayConfirmKey = AutoPlayConfirmKey,
            ManualAdvanceForwardKey = ManualAdvanceForwardKey,
            ManualAdvanceBackwardKey = ManualAdvanceBackwardKey
        };
    }

    /// <summary>
    /// Sets the hotkey configuration from a dictionary
    /// </summary>
    public void SetConfiguration(HotkeyConfig config)
    {
        AutoPlayConfirmKey = config.AutoPlayConfirmKey;
        ManualAdvanceForwardKey = config.ManualAdvanceForwardKey;
        ManualAdvanceBackwardKey = config.ManualAdvanceBackwardKey;

        Debug.WriteLine($"[HotkeyManager] Configuration updated:");
        Debug.WriteLine($"  Auto-play confirm: {AutoPlayConfirmKey}");
        Debug.WriteLine($"  Manual forward: {ManualAdvanceForwardKey}");
        Debug.WriteLine($"  Manual backward: {ManualAdvanceBackwardKey}");
    }
}

/// <summary>
/// Configuration class for hotkeys
/// </summary>
public class HotkeyConfig
{
    public char AutoPlayConfirmKey { get; set; } = '=';
    public char ManualAdvanceForwardKey { get; set; } = ']';
    public char ManualAdvanceBackwardKey { get; set; } = '[';
}
