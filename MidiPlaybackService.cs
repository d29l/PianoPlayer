using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MidiToKeyboard.Controllers;

namespace MidiToKeyboard;

/// <summary>
/// Service for playing back MIDI files with humanized timing and keyboard input.
/// Depends on: ParsedMidiFile and MidiNote from MidiToKeyboard.Controllers namespace.
/// Uses: KeyboardInputSender for sending keyboard events to Windows.
/// </summary>
public class MidiPlaybackService
{
    private readonly Random _random = new Random();
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _playbackTask;
    private bool _isPaused;
    private readonly object _pauseLock = new object();
    private DateTime _pauseStartTime;
    private TimeSpan _totalPauseDuration;
    private readonly List<System.Threading.Timer> _activeTimers = new List<System.Threading.Timer>();
    private readonly object _timerLock = new object();
    private readonly HashSet<char> _heldKeys = new HashSet<char>();
    private bool _humanizeEnabled;
    private bool _waitingForConfirmation;
    private readonly object _confirmationLock = new object();

    public bool IsPlaying { get; private set; }
    public bool IsPaused => _isPaused;
    public bool IsWaitingForConfirmation => _waitingForConfirmation;
    public double CurrentPosition { get; private set; } // In beats
    public double CurrentTime { get; private set; } // In seconds
    public int CurrentNoteIndex { get; private set; }
    public int TotalNotes { get; private set; }

    public event EventHandler<PlaybackProgressEventArgs>? ProgressChanged;
    public event EventHandler? PlaybackCompleted;
    public event EventHandler<PlaybackErrorEventArgs>? PlaybackError;
    public event EventHandler? WaitingForConfirmation;

    /// <summary>
    /// Starts playback of a MIDI file
    /// </summary>
    public async Task StartPlaybackAsync(ParsedMidiFile midiFile, bool humanize = false, bool waitForConfirmation = false)
    {
        if (IsPlaying)
        {
            throw new InvalidOperationException("Playback is already in progress");
        }

        _cancellationTokenSource = new CancellationTokenSource();
        IsPlaying = true;
        _isPaused = false;
        _waitingForConfirmation = waitForConfirmation;
        _totalPauseDuration = TimeSpan.Zero;
        CurrentPosition = 0;
        CurrentTime = 0;
        CurrentNoteIndex = 0;
        TotalNotes = midiFile.Notes.Count;
        _humanizeEnabled = humanize;

        _playbackTask = Task.Run(() => PlaybackLoop(midiFile, _cancellationTokenSource.Token));
        await Task.CompletedTask;
    }

    /// <summary>
    /// Confirms playback start (used when waitForConfirmation is true)
    /// </summary>
    public void ConfirmStart()
    {
        lock (_confirmationLock)
        {
            if (_waitingForConfirmation)
            {
                _waitingForConfirmation = false;
                Monitor.PulseAll(_confirmationLock);
                Debug.WriteLine("[MidiPlayback] Playback confirmed and started");
            }
        }
    }

    /// <summary>
    /// Pauses the current playback
    /// </summary>
    public void Pause()
    {
        if (!IsPlaying)
            return;

        lock (_pauseLock)
        {
            _isPaused = true;
            _pauseStartTime = DateTime.UtcNow;
        }

        // Release all currently held keys and cancel pending timers
        ReleaseAllHeldKeys();
        CancelAllTimers();
    }

    /// <summary>
    /// Resumes the current playback
    /// </summary>
    public void Resume()
    {
        if (!IsPlaying)
            return;

        lock (_pauseLock)
        {
            if (_isPaused)
            {
                // Add the time spent paused to total pause duration
                _totalPauseDuration += DateTime.UtcNow - _pauseStartTime;
            }
            _isPaused = false;
            Monitor.PulseAll(_pauseLock);
        }
    }

    /// <summary>
    /// Stops the current playback - optimized for immediate UI response
    /// </summary>
    public async Task StopAsync()
    {
        if (!IsPlaying)
            return;

        // Immediately release all keys and cancel timers - this is the critical part for responsiveness
        ReleaseAllHeldKeys();
        CancelAllTimers();

        // Update state immediately so UI responds instantly
        IsPlaying = false;
        _isPaused = false;
        _waitingForConfirmation = false;
        CurrentPosition = 0;
        CurrentTime = 0;
        CurrentNoteIndex = 0;
        TotalNotes = 0;

        // Cancel the playback task
        _cancellationTokenSource?.Cancel();

        // Clean up the task in the background - don't block the UI
        if (_playbackTask != null)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await _playbackTask;
                }
                catch (OperationCanceledException)
                {
                    // Expected when stopping
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error during playback cleanup: {ex.Message}");
                }
            });
        }
    }

    /// <summary>
    /// Main playback loop
    /// </summary>
    private void PlaybackLoop(ParsedMidiFile midiFile, CancellationToken cancellationToken)
    {
        try
        {
            // Wait for confirmation if needed
            if (_waitingForConfirmation)
            {
                Debug.WriteLine("[MidiPlayback] Waiting for user confirmation to start...");
                WaitingForConfirmation?.Invoke(this, EventArgs.Empty);

                lock (_confirmationLock)
                {
                    while (_waitingForConfirmation && !cancellationToken.IsCancellationRequested)
                    {
                        Monitor.Wait(_confirmationLock, 100);
                    }
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    OnPlaybackCompleted();
                    return;
                }

                Debug.WriteLine("[MidiPlayback] Confirmation received, starting playback");
            }

            var notes = midiFile.Notes.Where(n => n.IsKeyPress).OrderBy(n => n.Timing).ToList();
            if (!notes.Any())
            {
                OnPlaybackCompleted();
                return;
            }

            var startTime = DateTime.UtcNow;
            var tempo = midiFile.Tempo;
            var beatsPerSecond = tempo / 60.0;
            var millisecondsPerBeat = 60000.0 / tempo;

            // Apply humanization if enabled
            List<HumanizedNote>? humanizedNotes = null;
            if (_humanizeEnabled)
            {
                var humanizationService = new HumanizationService();
                humanizedNotes = humanizationService.HumanizeNotes(notes, tempo);
                Debug.WriteLine($"[Humanization] Applied to {humanizedNotes.Count} notes");
            }

            for (int i = 0; i < notes.Count; i++)
            {
                // Check for pause
                lock (_pauseLock)
                {
                    while (_isPaused && !cancellationToken.IsCancellationRequested)
                    {
                        Monitor.Wait(_pauseLock, 100);
                    }
                }

                if (cancellationToken.IsCancellationRequested)
                    break;

                var note = notes[i];
                var humanizedNote = humanizedNotes?[i];

                // Calculate when this note should be played (in seconds)
                var noteTiming = humanizedNote?.GetAdjustedTiming(tempo) ?? note.Timing;
                var noteTimeSeconds = noteTiming / beatsPerSecond;

                // Wait until it's time to play this note
                // Subtract total pause duration from elapsed time
                var elapsed = (DateTime.UtcNow - startTime - _totalPauseDuration).TotalSeconds;
                var waitTime = noteTimeSeconds - elapsed;

                if (waitTime > 0)
                {
                    Thread.Sleep((int)(waitTime * 1000));
                }

                // Play the note/chord with actual MIDI duration (humanization doesn't affect key presses, only timing)
                PlayNote(note, millisecondsPerBeat);

                // Update progress
                CurrentPosition = note.Timing;
                CurrentTime = noteTimeSeconds;
                CurrentNoteIndex = i;

                OnProgressChanged(new PlaybackProgressEventArgs
                {
                    CurrentNoteIndex = i,
                    TotalNotes = notes.Count,
                    CurrentTime = noteTimeSeconds,
                    TotalTime = notes.Last().Timing / beatsPerSecond,
                    PercentComplete = (double)i / notes.Count * 100,
                    CurrentTempo = tempo,
                    AverageInterval = 0
                });
            }

            // Wait for all notes to finish their hold duration before completing playback
            // Find the note with the latest end time (timing + duration)
            if (notes.Any())
            {
                var lastNoteEndTime = notes.Max(n => n.Timing + n.Duration);
                var lastNoteStartTime = notes.Last().Timing;
                var waitDuration = lastNoteEndTime - lastNoteStartTime;
                var waitTimeMs = (int)(waitDuration * millisecondsPerBeat);

                if (waitTimeMs > 0)
                {
                    Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Waiting {waitTimeMs}ms for all notes to finish (longest note ends at {lastNoteEndTime:F3} beats)...");
                    Thread.Sleep(waitTimeMs);
                }
            }

            OnPlaybackCompleted();
        }
        catch (Exception ex)
        {
            OnPlaybackError(new PlaybackErrorEventArgs { Exception = ex });
        }
        finally
        {
            IsPlaying = false;
            _isPaused = false;
            ReleaseAllHeldKeys();
            CancelAllTimers();
        }
    }

    /// <summary>
    /// Plays a single note or chord with actual MIDI duration from note-off events
    /// Non-blocking - schedules release on timer to allow overlapping notes
    /// </summary>
    private void PlayNote(MidiNote note, double millisecondsPerBeat)
    {
        if (string.IsNullOrEmpty(note.Key))
            return;

        // Use exact MIDI duration from note-off events - pure MIDI replication
        var holdTimeMs = (int)(note.Duration * millisecondsPerBeat);

        Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Playing: '{note.Key}' @ {note.Timing:F3} | Duration: {note.Duration:F3} beats ({holdTimeMs}ms)");

        // Press all keys down as a chord using optimized method (no delays between notes)
        KeyboardInputSender.SendChordDown(note.Key.ToCharArray());

        // Track all held keys
        lock (_timerLock)
        {
            foreach (var key in note.Key)
            {
                _heldKeys.Add(key);
            }
        }

        // Schedule release after the hold time (non-blocking)
        var keysToRelease = note.Key;
        System.Threading.Timer? releaseTimer = null;
        releaseTimer = new System.Threading.Timer(_ =>
        {
            // Release all keys
            foreach (var key in keysToRelease)
            {
                KeyboardInputSender.SendKeyUp(key);
                lock (_timerLock)
                {
                    _heldKeys.Remove(key);
                }
            }

            // Remove timer from active list
            lock (_timerLock)
            {
                _activeTimers.Remove(releaseTimer!);
            }
            releaseTimer?.Dispose();
        }, null, holdTimeMs, Timeout.Infinite);

        // Track the timer
        lock (_timerLock)
        {
            _activeTimers.Add(releaseTimer);
        }
    }

    /// <summary>
    /// Releases all currently held keys
    /// </summary>
    private void ReleaseAllHeldKeys()
    {
        lock (_timerLock)
        {
            foreach (var key in _heldKeys.ToList())
            {
                try
                {
                    KeyboardInputSender.SendKeyUp(key);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error releasing key {key}: {ex.Message}");
                }
            }
            _heldKeys.Clear();
        }
    }

    /// <summary>
    /// Cancels all active note release timers
    /// </summary>
    private void CancelAllTimers()
    {
        lock (_timerLock)
        {
            foreach (var timer in _activeTimers.ToList())
            {
                try
                {
                    timer?.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error disposing timer: {ex.Message}");
                }
            }
            _activeTimers.Clear();
        }
    }

    protected virtual void OnProgressChanged(PlaybackProgressEventArgs e)
    {
        ProgressChanged?.Invoke(this, e);
    }

    protected virtual void OnPlaybackCompleted()
    {
        IsPlaying = false;
        _isPaused = false;
        _waitingForConfirmation = false;
        PlaybackCompleted?.Invoke(this, EventArgs.Empty);
    }

    protected virtual void OnPlaybackError(PlaybackErrorEventArgs e)
    {
        IsPlaying = false;
        _isPaused = false;
        _waitingForConfirmation = false;
        PlaybackError?.Invoke(this, e);
    }
}

public class PlaybackProgressEventArgs : EventArgs
{
    public int CurrentNoteIndex { get; set; }
    public int TotalNotes { get; set; }
    public double CurrentTime { get; set; }
    public double TotalTime { get; set; }
    public double PercentComplete { get; set; }
    public int CurrentTempo { get; set; }
    public double AverageInterval { get; set; }
}

public class PlaybackErrorEventArgs : EventArgs
{
    public Exception? Exception { get; set; }
}
