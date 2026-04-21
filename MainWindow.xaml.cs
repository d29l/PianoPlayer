using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Input;
using Microsoft.Web.WebView2.Core;

namespace MidiToKeyboardApp
{
    public partial class MainWindow : Window
    {
        private readonly WebServer _webServer;
        private readonly MidiToKeyboard.GlobalKeyboardHook _keyboardHook;
        private MiniPlayerWindow? _miniPlayerWindow;

        // Win32 API declarations for proper window handling with custom status bars
        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        [DllImport("user32.dll")]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        private const int SM_CXFRAME = 32;
        private const int SM_CYFRAME = 33;
        private const int SM_CXPADDEDBORDER = 92;
        private const uint MONITOR_DEFAULTTONEAREST = 2;

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        private static int GetResizeBorderThickness()
        {
            return GetSystemMetrics(SM_CXFRAME) + GetSystemMetrics(SM_CXPADDEDBORDER);
        }

        public MainWindow()
        {
            InitializeComponent();
            _webServer = new WebServer();
            _keyboardHook = new MidiToKeyboard.GlobalKeyboardHook();

            // Set main window reference for API access
            MidiToKeyboard.Controllers.MidiController.SetMainWindow(this);

            // Subscribe to keyboard events
            _keyboardHook.KeyPressed += KeyboardHook_KeyPressed;

            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
            StateChanged += MainWindow_StateChanged;
            SourceInitialized += MainWindow_SourceInitialized;
        }

        private void KeyboardHook_KeyPressed(object? sender, MidiToKeyboard.KeyPressedEventArgs e)
        {
            // Forward the key press to the hotkey manager
            System.Diagnostics.Debug.WriteLine($"[MainWindow] Global key detected: {e.Key}");

            // Handle the key press through the hotkey manager (which routes to appropriate handlers)
            MidiToKeyboard.HotkeyManager.Instance.HandleKeyPress(e.Key);
        }

        private void MainWindow_SourceInitialized(object? sender, EventArgs e)
        {
            // Hook into window messages to handle maximization properly
            IntPtr handle = new WindowInteropHelper(this).Handle;
            HwndSource.FromHwnd(handle)?.AddHook(WindowProc);

            // Apply initial state adjustments
            UpdateWindowMargins();
        }

        private IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_GETMINMAXINFO = 0x0024;

            switch (msg)
            {
                case WM_GETMINMAXINFO:
                    // Handle window maximization to respect working area (YASB, taskbars, etc.)
                    MINMAXINFO mmi = Marshal.PtrToStructure<MINMAXINFO>(lParam);

                    IntPtr monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
                    if (monitor != IntPtr.Zero)
                    {
                        MONITORINFO monitorInfo = new MONITORINFO();
                        monitorInfo.cbSize = Marshal.SizeOf(typeof(MONITORINFO));

                        if (GetMonitorInfo(monitor, ref monitorInfo))
                        {
                            RECT workArea = monitorInfo.rcWork;
                            RECT monitorArea = monitorInfo.rcMonitor;

                            // Set max size to working area (respects YASB and other overlays)
                            mmi.ptMaxPosition.X = workArea.Left - monitorArea.Left;
                            mmi.ptMaxPosition.Y = workArea.Top - monitorArea.Top;
                            mmi.ptMaxSize.X = workArea.Right - workArea.Left;
                            mmi.ptMaxSize.Y = workArea.Bottom - workArea.Top;

                            Marshal.StructureToPtr(mmi, lParam, true);
                        }
                    }
                    handled = true;
                    break;
            }

            return IntPtr.Zero;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Initialize WebView2
                await webView.EnsureCoreWebView2Async(null);

                // Start the web server
                await _webServer.StartAsync();

                // Start global keyboard hook
                _keyboardHook.Start();

                // Navigate to the app
                webView.CoreWebView2.Navigate("http://localhost:5000");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting application:\n\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private async void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            _keyboardHook.Stop();
            _keyboardHook.Dispose();
            _miniPlayerWindow?.Close();
            await _webServer.StopAsync();
        }

        public void OpenMiniPlayer()
        {
            if (_miniPlayerWindow == null || !_miniPlayerWindow.IsLoaded)
            {
                _miniPlayerWindow = new MiniPlayerWindow();
                _miniPlayerWindow.Show();
            }
            else
            {
                _miniPlayerWindow.Activate();
            }
        }

        // Custom title bar drag functionality
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                MaximizeButton_Click(sender, e);
            }
            else if (e.ChangedButton == MouseButton.Left)
            {
                try
                {
                    DragMove();
                }
                catch
                {
                    // DragMove can throw if called at wrong time, ignore
                }
            }
        }

        // Minimize button
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        // Maximize/Restore button
        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
            }
            else
            {
                WindowState = WindowState.Maximized;
            }
        }

        // Close button
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // Update maximize icon and window margins when window state changes
        private void MainWindow_StateChanged(object? sender, EventArgs e)
        {
            UpdateWindowMargins();

            if (WindowState == WindowState.Maximized)
            {
                // Change to restore icon (two overlapping squares)
                MaximizeIcon.Data = System.Windows.Media.Geometry.Parse("M2,2 L8,2 L8,8 L2,8 Z M0,0 L0,2 L2,2 M8,0 L10,0 L10,8 L8,8");
            }
            else
            {
                // Change to maximize icon (single square)
                MaximizeIcon.Data = System.Windows.Media.Geometry.Parse("M0,0 L10,0 L10,10 L0,10 Z");
            }
        }

        private void UpdateWindowMargins()
        {
            if (WindowState == WindowState.Maximized)
            {
                // Get the proper border thickness for maximized state
                int borderThickness = GetResizeBorderThickness();
                MainBorder.Padding = new Thickness(borderThickness);
            }
            else
            {
                MainBorder.Padding = new Thickness(0);
            }
        }
    }
}
