using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace MidiToKeyboard;

/// <summary>
/// Provides global keyboard hook functionality to capture key presses even when the application is not focused.
/// Used for manual play mode to allow [ and ] keys to advance notes from any application.
/// </summary>
public class GlobalKeyboardHook : IDisposable
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;

    private LowLevelKeyboardProc _proc;
    private IntPtr _hookID = IntPtr.Zero;
    private bool _disposed = false;

    public event EventHandler<KeyPressedEventArgs>? KeyPressed;

    public GlobalKeyboardHook()
    {
        _proc = HookCallback;
    }

    /// <summary>
    /// Starts the global keyboard hook
    /// </summary>
    public void Start()
    {
        if (_hookID != IntPtr.Zero)
        {
            Debug.WriteLine("[GlobalKeyboardHook] Hook already started");
            return;
        }

        _hookID = SetHook(_proc);
        Debug.WriteLine("[GlobalKeyboardHook] Hook started successfully");
    }

    /// <summary>
    /// Stops the global keyboard hook
    /// </summary>
    public void Stop()
    {
        if (_hookID != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookID);
            _hookID = IntPtr.Zero;
            Debug.WriteLine("[GlobalKeyboardHook] Hook stopped");
        }
    }

    private IntPtr SetHook(LowLevelKeyboardProc proc)
    {
        using (var curProcess = Process.GetCurrentProcess())
        using (var curModule = curProcess.MainModule)
        {
            if (curModule == null)
                throw new InvalidOperationException("Cannot get main module");

            return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int vkCode = Marshal.ReadInt32(lParam);

            // Only handle key down events
            if (wParam == (IntPtr)WM_KEYDOWN)
            {
                char? key = VirtualKeyToChar(vkCode);

                if (key.HasValue)
                {
                    Debug.WriteLine($"[GlobalKeyboardHook] Detected key press: {key.Value} (VK: 0x{vkCode:X})");

                    // Raise event on a separate thread to avoid blocking the hook
                    System.Threading.ThreadPool.QueueUserWorkItem(_ =>
                    {
                        try
                        {
                            KeyPressed?.Invoke(this, new KeyPressedEventArgs(key.Value));
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[GlobalKeyboardHook] Error in KeyPressed event: {ex.Message}");
                        }
                    });
                }
            }
        }

        return CallNextHookEx(_hookID, nCode, wParam, lParam);
    }

    /// <summary>
    /// Converts a virtual key code to a character
    /// </summary>
    private char? VirtualKeyToChar(int vkCode)
    {
        // Numbers 0-9
        if (vkCode >= 0x30 && vkCode <= 0x39)
            return (char)vkCode;

        // Letters A-Z
        if (vkCode >= 0x41 && vkCode <= 0x5A)
            return (char)vkCode;

        // Special keys mapping
        return vkCode switch
        {
            0xBA => ';',  // VK_OEM_1
            0xBB => '=',  // VK_OEM_PLUS
            0xBC => ',',  // VK_OEM_COMMA
            0xBD => '-',  // VK_OEM_MINUS
            0xBE => '.',  // VK_OEM_PERIOD
            0xBF => '/',  // VK_OEM_2
            0xC0 => '`',  // VK_OEM_3
            0xDB => '[',  // VK_OEM_4
            0xDC => '\\', // VK_OEM_5
            0xDD => ']',  // VK_OEM_6
            0xDE => '\'', // VK_OEM_7
            0x20 => ' ',  // VK_SPACE
            _ => null
        };
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Managed resources cleanup
            }

            // Unmanaged resources cleanup
            Stop();

            _disposed = true;
        }
    }

    ~GlobalKeyboardHook()
    {
        Dispose(false);
    }

    #region Native Methods

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    #endregion
}

/// <summary>
/// Event arguments for key press events
/// </summary>
public class KeyPressedEventArgs : EventArgs
{
    public char Key { get; }

    public KeyPressedEventArgs(char key)
    {
        Key = key;
    }
}
