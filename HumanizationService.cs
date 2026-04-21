using System;
using System.Collections.Generic;
using System.Linq;
using MidiToKeyboard.Controllers;

namespace MidiToKeyboard;

/// <summary>
/// Provides humanization for MIDI playback by adding natural timing variations.
/// Implements phrase-aware, correlated timing offsets for more realistic playback.
/// </summary>
public class HumanizationService
{
    private readonly Random _random = new Random();
    private const double RARE_TIMING_SLIP_PROBABILITY = 0.002; // 0.2%

    /// <summary>
    /// Context for humanizing a phrase of notes
    /// </summary>
    private class PhraseContext
    {
        public double PhraseBias { get; set; } // -10 to +10 ms
        public double HandOffset { get; set; } // -15 to +15 ms
        public int ChordStaggerDirection { get; set; } // -1 or 1
    }

    /// <summary>
    /// Applies humanization to a list of MIDI notes
    /// </summary>
    public List<HumanizedNote> HumanizeNotes(List<MidiNote> notes, int tempo)
    {
        if (notes == null || !notes.Any())
            return new List<HumanizedNote>();

        // Split into phrases (simple approach: every 8 beats or silence gap > 1 beat)
        var phrases = SplitIntoPhrases(notes);
        var humanizedNotes = new List<HumanizedNote>();
        var tempoScale = CalculateTempoScale(tempo);

        foreach (var phrase in phrases)
        {
            var context = CreatePhraseContext(tempoScale);
            humanizedNotes.AddRange(HumanizePhrase(phrase, context, tempoScale, tempo));
        }

        return humanizedNotes;
    }

    /// <summary>
    /// Splits notes into phrases for consistent humanization
    /// </summary>
    private List<List<MidiNote>> SplitIntoPhrases(List<MidiNote> notes)
    {
        var phrases = new List<List<MidiNote>>();
        var currentPhrase = new List<MidiNote>();
        double lastTiming = 0;

        foreach (var note in notes.OrderBy(n => n.Timing))
        {
            // Start new phrase if gap > 1 beat or every 8 beats
            if (currentPhrase.Any() &&
                (note.Timing - lastTiming > 1.0 ||
                 currentPhrase.First().Timing + 8.0 < note.Timing))
            {
                phrases.Add(currentPhrase);
                currentPhrase = new List<MidiNote>();
            }

            currentPhrase.Add(note);
            lastTiming = note.Timing;
        }

        if (currentPhrase.Any())
            phrases.Add(currentPhrase);

        return phrases;
    }

    /// <summary>
    /// Creates a new phrase context with random biases
    /// </summary>
    private PhraseContext CreatePhraseContext(double tempoScale)
    {
        return new PhraseContext
        {
            PhraseBias = RandomRange(-10, 10) * tempoScale,
            HandOffset = RandomRange(-15, 15) * tempoScale,
            ChordStaggerDirection = _random.Next(2) == 0 ? -1 : 1
        };
    }

    /// <summary>
    /// Scales timing offsets based on tempo (reduce at high BPM)
    /// </summary>
    private double CalculateTempoScale(int tempo)
    {
        // At 120 BPM: scale = 1.0
        // At 240 BPM: scale = 0.5
        // At 60 BPM: scale = 1.5
        return Math.Max(0.3, Math.Min(1.5, 120.0 / tempo));
    }

    /// <summary>
    /// Humanizes a phrase of notes
    /// </summary>
    private List<HumanizedNote> HumanizePhrase(List<MidiNote> phrase, PhraseContext context, double tempoScale, int tempo)
    {
        var humanized = new List<HumanizedNote>();

        // Group notes by timing (chords)
        var chordGroups = phrase.GroupBy(n => n.Timing).OrderBy(g => g.Key);

        foreach (var chord in chordGroups)
        {
            var chordNotes = chord.ToList();

            if (chordNotes.Count == 1)
            {
                // Single note - apply jitter only
                humanized.Add(HumanizeSingleNote(chordNotes[0], context, tempoScale));
            }
            else
            {
                // Chord - apply stagger
                humanized.AddRange(HumanizeChord(chordNotes, context, tempoScale, tempo));
            }
        }

        return humanized;
    }

    /// <summary>
    /// Humanizes a single note
    /// </summary>
    private HumanizedNote HumanizeSingleNote(MidiNote note, PhraseContext context, double tempoScale)
    {
        // Phrase-level bias + hand offset + single-note jitter
        var timingOffset = context.PhraseBias + context.HandOffset + GaussianNoise(0, 6) * tempoScale;

        // Rare timing slip (0.2% chance)
        if (_random.NextDouble() < RARE_TIMING_SLIP_PROBABILITY)
        {
            timingOffset += RandomRange(30, 60);
        }

        return new HumanizedNote
        {
            OriginalNote = note,
            TimingOffsetMs = timingOffset
        };
    }

    /// <summary>
    /// Humanizes a chord with stagger
    /// </summary>
    private List<HumanizedNote> HumanizeChord(List<MidiNote> chordNotes, PhraseContext context, double tempoScale, int tempo)
    {
        var humanized = new List<HumanizedNote>();

        // Sort by note number to identify roles
        var sortedNotes = chordNotes.OrderBy(n => n.NoteNumber).ToList();
        var bassNote = sortedNotes.First();
        var topNote = sortedNotes.Last();

        // Choose anchor note (usually bass or top)
        var anchorNote = _random.Next(2) == 0 ? bassNote : topNote;
        var anchorOffset = context.PhraseBias + context.HandOffset;

        foreach (var note in chordNotes)
        {
            double timingOffset;

            if (note == anchorNote)
            {
                // Anchor note - base timing
                timingOffset = anchorOffset;
            }
            else
            {
                // Other notes - staggered from anchor
                var stagger = Math.Abs(GaussianNoise(8, 3));
                stagger = Math.Clamp(stagger, 0, 15) * tempoScale;
                timingOffset = anchorOffset + (stagger * context.ChordStaggerDirection);
            }

            // Add small per-note jitter
            timingOffset += GaussianNoise(0, 2) * tempoScale;

            // Rare timing slip
            if (_random.NextDouble() < RARE_TIMING_SLIP_PROBABILITY)
            {
                timingOffset += RandomRange(30, 60);
            }

            humanized.Add(new HumanizedNote
            {
                OriginalNote = note,
                TimingOffsetMs = timingOffset
            });
        }

        return humanized;
    }

    /// <summary>
    /// Generates Gaussian-distributed random noise
    /// </summary>
    private double GaussianNoise(double mean, double stdDev)
    {
        // Box-Muller transform
        double u1 = 1.0 - _random.NextDouble();
        double u2 = 1.0 - _random.NextDouble();
        double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        return mean + stdDev * randStdNormal;
    }

    /// <summary>
    /// Generates random value in range
    /// </summary>
    private double RandomRange(double min, double max)
    {
        return min + _random.NextDouble() * (max - min);
    }
}

/// <summary>
/// Represents a note with humanization applied
/// </summary>
public class HumanizedNote
{
    public MidiNote OriginalNote { get; set; } = null!;
    public double TimingOffsetMs { get; set; }

    /// <summary>
    /// Gets the adjusted timing in beats
    /// </summary>
    public double GetAdjustedTiming(int tempo)
    {
        // Convert ms offset to beats
        var beatsPerSecond = tempo / 60.0;
        var offsetInSeconds = TimingOffsetMs / 1000.0;
        var offsetInBeats = offsetInSeconds * beatsPerSecond;
        return OriginalNote.Timing + offsetInBeats;
    }
}
