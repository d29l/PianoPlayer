using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace MidiToKeyboard;

/// <summary>
/// Sends keyboard inputs using Windows keybd_event API for game compatibility
/// Following Python approach: pre-release keys, convert symbols to base keys, wrap shift per key
/// </summary>
public class KeyboardInputSender
{
    // Windows API constants
    private const int KEYEVENTF_KEYUP = 0x0002;
    private const byte VK_SHIFT = 0x10;

    // Windows API functions
    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    private static extern short VkKeyScan(char ch);

    [DllImport("user32.dll")]
    private static extern uint MapVirtualKey(uint uCode, uint uMapType);

    // Symbol to base key conversion (like Python's conversionCases)
    // Maps shifted symbols back to their base keys
    private static readonly Dictionary<char, char> ConversionCases = new Dictionary<char, char>
    {
        { '!', '1' },
        { '@', '2' },
        { '#', '3' },
        { '$', '4' },
        { '%', '5' },
        { '^', '6' },
        { '&', '7' },
        { '*', '8' },
        { '(', '9' },
        { ')', '0' },
        { '_', '-' },
        { '+', '=' },
        { '{', '[' },
        { '}', ']' },
        { '|', '\\' },
        { ':', ';' },
        { '"', '\'' },
        { '<', ',' },
        { '>', '.' },
        { '?', '/' },
        { '~', '`' }
    };

    /// <summary>
    /// Check if a character needs shift (like Python's isShifted)
    /// </summary>
    private static bool IsShifted(char ch)
    {
        // Uppercase letters (ASCII 65-90)
        if (ch >= 'A' && ch <= 'Z')
            return true;

        // Special shifted symbols
        if ("!@#$%^&*()_+{}|:\"<>?~".Contains(ch))
            return true;

        return false;
    }

    /// <summary>
    /// Press a letter (like Python's pressLetter)
    /// Pre-releases the key, then presses with correct shift handling
    /// </summary>
    private static void PressLetter(char key)
    {
        char workingKey = key;

        if (IsShifted(key))
        {
            // Convert symbols to their base keys (! → 1, @ → 2, etc.)
            if (ConversionCases.ContainsKey(key))
            {
                workingKey = ConversionCases[key];
            }

            // Always work with lowercase
            workingKey = char.ToLower(workingKey);

            // Get virtual key code for the lowercase key
            short vkAndShift = VkKeyScan(workingKey);
            if (vkAndShift == -1)
                return;

            byte vkCode = (byte)(vkAndShift & 0xFF);
            byte scanCode = (byte)MapVirtualKey(vkCode, 0);
            byte shiftScanCode = (byte)MapVirtualKey(VK_SHIFT, 0);

            // Pre-release the key to prevent stuck keys
            keybd_event(vkCode, scanCode, KEYEVENTF_KEYUP, UIntPtr.Zero);

            // Press shift
            keybd_event(VK_SHIFT, shiftScanCode, 0, UIntPtr.Zero);

            // Press the lowercase key (while shift is held)
            keybd_event(vkCode, scanCode, 0, UIntPtr.Zero);

            // Immediately release shift (don't leave it held)
            keybd_event(VK_SHIFT, shiftScanCode, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }
        else
        {
            // Lowercase or unshifted character
            workingKey = char.ToLower(workingKey);

            short vkAndShift = VkKeyScan(workingKey);
            if (vkAndShift == -1)
                return;

            byte vkCode = (byte)(vkAndShift & 0xFF);
            byte scanCode = (byte)MapVirtualKey(vkCode, 0);

            // Pre-release the key
            keybd_event(vkCode, scanCode, KEYEVENTF_KEYUP, UIntPtr.Zero);

            // Press the key
            keybd_event(vkCode, scanCode, 0, UIntPtr.Zero);
        }
    }

    /// <summary>
    /// Release a letter (like Python's releaseLetter)
    /// </summary>
    private static void ReleaseLetter(char key)
    {
        char workingKey = key;

        if (IsShifted(key))
        {
            // Convert symbols to their base keys
            if (ConversionCases.ContainsKey(key))
            {
                workingKey = ConversionCases[key];
            }
        }

        // Always work with lowercase
        workingKey = char.ToLower(workingKey);

        short vkAndShift = VkKeyScan(workingKey);
        if (vkAndShift == -1)
            return;

        byte vkCode = (byte)(vkAndShift & 0xFF);
        byte scanCode = (byte)MapVirtualKey(vkCode, 0);
        byte shiftScanCode = (byte)MapVirtualKey(VK_SHIFT, 0);

        // Release the lowercase key
        keybd_event(vkCode, scanCode, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }

    /// <summary>
    /// Sends multiple keys down as a chord with optimized shift handling
    /// Groups shifted and non-shifted keys to minimize delays
    /// </summary>
    public static void SendChordDown(char[] keys)
    {
        if (keys == null || keys.Length == 0)
            return;

        // Separate shifted and non-shifted keys
        var shiftedKeys = new List<char>();
        var nonShiftedKeys = new List<char>();

        foreach (var key in keys)
        {
            if (IsShifted(key))
                shiftedKeys.Add(key);
            else
                nonShiftedKeys.Add(key);
        }

        // Press all non-shifted keys first (no shift needed, instant)
        foreach (var key in nonShiftedKeys)
        {
            char workingKey = char.ToLower(key);
            short vkAndShift = VkKeyScan(workingKey);
            if (vkAndShift == -1)
                continue;

            byte vkCode = (byte)(vkAndShift & 0xFF);
            byte scanCode = (byte)MapVirtualKey(vkCode, 0);

            // Pre-release and press
            keybd_event(vkCode, scanCode, KEYEVENTF_KEYUP, UIntPtr.Zero);
            keybd_event(vkCode, scanCode, 0, UIntPtr.Zero);
        }

        // Press all shifted keys (with shift per key, but grouped)
        foreach (var key in shiftedKeys)
        {
            char workingKey = key;

            // Convert symbols to their base keys
            if (ConversionCases.ContainsKey(key))
            {
                workingKey = ConversionCases[key];
            }

            workingKey = char.ToLower(workingKey);

            short vkAndShift = VkKeyScan(workingKey);
            if (vkAndShift == -1)
                continue;

            byte vkCode = (byte)(vkAndShift & 0xFF);
            byte scanCode = (byte)MapVirtualKey(vkCode, 0);
            byte shiftScanCode = (byte)MapVirtualKey(VK_SHIFT, 0);

            // Pre-release
            keybd_event(vkCode, scanCode, KEYEVENTF_KEYUP, UIntPtr.Zero);

            // Press shift + key + release shift in quick succession
            keybd_event(VK_SHIFT, shiftScanCode, 0, UIntPtr.Zero);
            keybd_event(vkCode, scanCode, 0, UIntPtr.Zero);
            keybd_event(VK_SHIFT, shiftScanCode, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }
    }

    /// <summary>
    /// Sends a key down event
    /// </summary>
    public static void SendKeyDown(char key)
    {
        PressLetter(key);
    }

    /// <summary>
    /// Sends a key up event
    /// </summary>
    public static void SendKeyUp(char key)
    {
        ReleaseLetter(key);
    }

    /// <summary>
    /// Presses and releases a key with optional hold duration
    /// </summary>
    public static void PressKey(char key, int holdMs = 50)
    {
        PressLetter(key);
        if (holdMs > 0)
            Thread.Sleep(holdMs);
        ReleaseLetter(key);
    }

    /// <summary>
    /// Presses multiple keys as a chord - each key pressed and released individually with proper shift
    /// </summary>
    public static void PressChord(string keys, int holdMs)
    {
        if (string.IsNullOrEmpty(keys))
            return;

        // Press each key individually with its own shift handling
        // This ensures correct shift state per key but keys are slightly staggered
        foreach (char key in keys)
        {
            PressKey(key, holdMs);
        }
    }
}
