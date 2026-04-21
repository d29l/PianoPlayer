using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace MidiToKeyboard;

/// <summary>
/// Manages the connection between global keyboard hooks and manual playback services.
/// Acts as a bridge to handle [ and ] key presses for manual note advancement.
/// </summary>
public class ManualPlaybackManager
{
    private static ManualPlaybackManager? _instance;
    private static readonly object _lock = new object();

    private readonly ConcurrentDictionary<string, ManualPlaybackService> _activeServices;
    private string? _currentActiveId;

    private ManualPlaybackManager()
    {
        _activeServices = new ConcurrentDictionary<string, ManualPlaybackService>();
    }

    /// <summary>
    /// Gets the singleton instance of the manager
    /// </summary>
    public static ManualPlaybackManager Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new ManualPlaybackManager();
                    }
                }
            }
            return _instance;
        }
    }

    /// <summary>
    /// Registers a manual playback service for a MIDI file
    /// </summary>
    public void RegisterService(string midiId, ManualPlaybackService service)
    {
        _activeServices[midiId] = service;
        _currentActiveId = midiId;
        Debug.WriteLine($"[ManualPlaybackManager] Registered service for {midiId}");
    }

    /// <summary>
    /// Unregisters a manual playback service
    /// </summary>
    public void UnregisterService(string midiId)
    {
        _activeServices.TryRemove(midiId, out _);

        if (_currentActiveId == midiId)
        {
            _currentActiveId = null;
        }

        Debug.WriteLine($"[ManualPlaybackManager] Unregistered service for {midiId}");
    }

    /// <summary>
    /// Handles a key press from the global keyboard hook
    /// </summary>
    public void HandleKeyPress(char key)
    {
        // Only handle [ and ] keys
        if (key != '[' && key != ']')
        {
            return;
        }

        // Get the currently active service
        if (string.IsNullOrEmpty(_currentActiveId))
        {
            Debug.WriteLine($"[ManualPlaybackManager] Key {key} pressed but no active service");
            return;
        }

        if (!_activeServices.TryGetValue(_currentActiveId, out var service))
        {
            Debug.WriteLine($"[ManualPlaybackManager] Service not found for {_currentActiveId}");
            return;
        }

        if (!service.IsActive)
        {
            Debug.WriteLine($"[ManualPlaybackManager] Service {_currentActiveId} is not active");
            return;
        }

        Debug.WriteLine($"[ManualPlaybackManager] Advancing note for {_currentActiveId} (key: {key})");

        // Advance the note
        try
        {
            service.AdvanceNote();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ManualPlaybackManager] Error advancing note: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the currently active manual playback service
    /// </summary>
    public ManualPlaybackService? GetActiveService()
    {
        if (string.IsNullOrEmpty(_currentActiveId))
        {
            return null;
        }

        _activeServices.TryGetValue(_currentActiveId, out var service);
        return service;
    }

    /// <summary>
    /// Sets which MIDI file is currently active for manual playback
    /// </summary>
    public void SetActive(string midiId)
    {
        if (_activeServices.ContainsKey(midiId))
        {
            _currentActiveId = midiId;
            Debug.WriteLine($"[ManualPlaybackManager] Set active: {midiId}");
        }
    }

    /// <summary>
    /// Clears the current active service
    /// </summary>
    public void ClearActive()
    {
        _currentActiveId = null;
        Debug.WriteLine($"[ManualPlaybackManager] Cleared active service");
    }

    /// <summary>
    /// Gets whether there is an active manual playback session
    /// </summary>
    public bool HasActivePlayback()
    {
        if (string.IsNullOrEmpty(_currentActiveId))
        {
            return false;
        }

        if (_activeServices.TryGetValue(_currentActiveId, out var service))
        {
            return service.IsActive;
        }

        return false;
    }
}
