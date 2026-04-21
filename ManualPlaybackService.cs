using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using MidiToKeyboard.Controllers;

namespace MidiToKeyboard;

/// <summary>
/// Service for managing manual playback mode where notes are advanced by user input.
/// Tracks playback state, timing intervals, and handles note advancement.
/// </summary>
public class ManualPlaybackService
{
    private ParsedMidiFile? _currentMidiFile;
    private int _currentNoteIndex;
    private DateTime _lastKeyPressTime;
    private readonly List<double> _intervalHistory = new List<double>();
    private const int MaxHistorySize = 5;
    private readonly object _stateLock = new object();

    public bool IsActive { get; private set; }
    public int CurrentNoteIndex => _currentNoteIndex;
    public int TotalNotes { get; private set; }
    public double AverageInterval { get; private set; }
    public int EstimatedBPM { get; private set; }
    public double CurrentTime { get; private set; }

    public event EventHandler<ManualNotePlayedEventArgs>? NotePlayed;
    public event EventHandler? PlaybackCompleted;

    /// <summary>
    /// Starts manual playback for a MIDI file
    /// </summary>
    public void Start(ParsedMidiFile midiFile)
    {
        lock (_stateLock)
        {
            _currentMidiFile = midiFile;
            _currentNoteIndex = 0;
            _lastKeyPressTime = DateTime.MinValue;
            _intervalHistory.Clear();
            AverageInterval = 0;
            EstimatedBPM = 0;
            CurrentTime = 0;
            TotalNotes = midiFile.Notes.Count(n => n.IsKeyPress);
            IsActive = true;

            Debug.WriteLine($"[ManualPlayback] Started - {TotalNotes} notes to play");
        }
    }

    /// <summary>
    /// Advances to the next note and plays it
    /// </summary>
    public void AdvanceNote()
    {
        lock (_stateLock)
        {
            if (!IsActive || _currentMidiFile == null)
            {
                Debug.WriteLine("[ManualPlayback] Cannot advance - not active");
                return;
            }

            var notes = _currentMidiFile.Notes.Where(n => n.IsKeyPress).ToList();

            if (_currentNoteIndex >= notes.Count)
            {
                Debug.WriteLine("[ManualPlayback] No more notes to play");
                Stop();
                return;
            }

            // Track timing interval
            DateTime now = DateTime.UtcNow;
            if (_lastKeyPressTime != DateTime.MinValue)
            {
                double interval = (now - _lastKeyPressTime).TotalMilliseconds;
                _intervalHistory.Add(interval);

                if (_intervalHistory.Count > MaxHistorySize)
                {
                    _intervalHistory.RemoveAt(0);
                }

                // Calculate average interval and estimated BPM
                if (_intervalHistory.Count > 0)
                {
                    AverageInterval = _intervalHistory.Average();
                    EstimatedBPM = (int)Math.Round(60000.0 / AverageInterval);
                }

                Debug.WriteLine($"[ManualPlayback] Interval: {interval:F0}ms, Avg: {AverageInterval:F0}ms, BPM: {EstimatedBPM}");
            }
            _lastKeyPressTime = now;

            var note = notes[_currentNoteIndex];

            Debug.WriteLine($"[ManualPlayback] Playing note {_currentNoteIndex + 1}/{notes.Count}: {note.Key}");

            // Update current time based on note timing
            var beatsPerSecond = _currentMidiFile.Tempo / 60.0;
            CurrentTime = note.Timing / beatsPerSecond;

            // Play the note
            PlayNote(note);

            // Advance index
            _currentNoteIndex++;

            // Raise event
            NotePlayed?.Invoke(this, new ManualNotePlayedEventArgs
            {
                Note = note,
                NoteIndex = _currentNoteIndex - 1,
                TotalNotes = notes.Count,
                AverageInterval = AverageInterval,
                EstimatedBPM = EstimatedBPM,
                CurrentTime = CurrentTime
            });

            // Check if completed
            if (_currentNoteIndex >= notes.Count)
            {
                Debug.WriteLine("[ManualPlayback] Playback completed");
                Stop();
            }
        }
    }

    /// <summary>
    /// Stops manual playback - optimized for immediate response
    /// </summary>
    public void Stop()
    {
        lock (_stateLock)
        {
            if (!IsActive)
                return;

            Debug.WriteLine("[ManualPlayback] Stopping");

            // Immediately update state for instant UI response
            IsActive = false;
            _currentNoteIndex = 0;
            _lastKeyPressTime = DateTime.MinValue;
            _intervalHistory.Clear();
            AverageInterval = 0;
            EstimatedBPM = 0;
            CurrentTime = 0;
            TotalNotes = 0;

            // Clear reference
            _currentMidiFile = null;

            // Fire event asynchronously to avoid blocking
            var handler = PlaybackCompleted;
            if (handler != null)
            {
                System.Threading.Tasks.Task.Run(() => handler.Invoke(this, EventArgs.Empty));
            }
        }
    }


    /// <summary>
    /// Plays a single note or chord
    /// </summary>
    private void PlayNote(MidiNote note)
    {
        if (string.IsNullOrEmpty(note.Key))
            return;

        var chars = note.Key.ToCharArray();

        // Calculate hold time based on tempo and average interval
        int holdTimeMs = CalculateHoldTime(note);

        Debug.WriteLine($"[ManualPlayback] Pressing keys: {note.Key} (hold: {holdTimeMs}ms)");

        // Press all keys down immediately (chord)
        KeyboardInputSender.SendChordDown(chars);

        // Schedule release after hold time
        System.Threading.Timer? releaseTimer = null;
        releaseTimer = new System.Threading.Timer(_ =>
        {
            foreach (var key in chars)
            {
                KeyboardInputSender.SendKeyUp(key);
            }
            releaseTimer?.Dispose();
        }, null, holdTimeMs, System.Threading.Timeout.Infinite);
    }

    /// <summary>
    /// Calculates hold time based on note duration and user timing
    /// </summary>
    private int CalculateHoldTime(MidiNote note)
    {
        if (_currentMidiFile == null)
            return 200; // Default fallback

        var tempo = _currentMidiFile.Tempo;
        var millisecondsPerBeat = 60000.0 / tempo;

        // Use MIDI duration if available
        if (note.Duration > 0)
        {
            return (int)(note.Duration * millisecondsPerBeat);
        }

        // Fallback: use average interval or tempo-based timing
        if (AverageInterval > 0)
        {
            return (int)(AverageInterval * 0.8); // 80% of interval
        }

        // Default based on tempo
        if (tempo >= 240)
            return 100;
        else if (tempo >= 120)
            return 150;
        else
            return 250;
    }

    /// <summary>
    /// Resets to the beginning without stopping
    /// </summary>
    public void Reset()
    {
        lock (_stateLock)
        {
            if (!IsActive)
                return;

            Debug.WriteLine("[ManualPlayback] Reset to beginning");

            _currentNoteIndex = 0;
            _lastKeyPressTime = DateTime.MinValue;
            _intervalHistory.Clear();
            AverageInterval = 0;
            EstimatedBPM = 0;
            CurrentTime = 0;
        }
    }
}

/// <summary>
/// Event arguments for when a note is played in manual mode
/// </summary>
public class ManualNotePlayedEventArgs : EventArgs
{
    public MidiNote Note { get; set; } = null!;
    public int NoteIndex { get; set; }
    public int TotalNotes { get; set; }
    public double AverageInterval { get; set; }
    public int EstimatedBPM { get; set; }
    public double CurrentTime { get; set; }
}
