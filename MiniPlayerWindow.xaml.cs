using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Web.WebView2.Core;

namespace MidiToKeyboardApp
{
    public partial class MiniPlayerWindow : Window
    {
        private System.Windows.Threading.DispatcherTimer? _updateTimer;
        private HttpClient? _httpClient;
        private string? _currentMidiId;
        private bool _isPlaying = false;
        private bool _isPaused = false;
        private bool _isManualMode = false;
        private bool _isPinned = true;
        private int _totalNotes = 0;
        private int _currentNoteIndex = 0;

        public MiniPlayerWindow()
        {
            InitializeComponent();
            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("http://localhost:5000/");

            // Initialize WebView2
            try
            {
                await pianoRollWebView.EnsureCoreWebView2Async(null);
                pianoRollWebView.CoreWebView2.Navigate("http://localhost:5000/mini-player.html");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing WebView2: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // Setup update timer - 100ms for smooth updates without overwhelming the system
            // Piano roll rendering is still 60fps, this only controls status polling
            _updateTimer = new System.Windows.Threading.DispatcherTimer();
            _updateTimer.Interval = TimeSpan.FromMilliseconds(100);
            _updateTimer.Tick += UpdateTimer_Tick;
            _updateTimer.Start();
        }

        private async void UpdateTimer_Tick(object? sender, EventArgs e)
        {
            await UpdatePlaybackStatus();
        }

        private async Task UpdatePlaybackStatus()
        {
            try
            {
                // Poll the main app's playback status
                var response = await _httpClient.GetAsync("api/midi/status");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var status = JsonSerializer.Deserialize<PlaybackStatus>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (status != null)
                    {
                        _isPlaying = status.IsPlaying;
                        _isPaused = status.IsPaused;
                        _isManualMode = status.IsManualMode;
                        _currentMidiId = status.CurrentMidiId;
                        _currentNoteIndex = status.CurrentNoteIndex;
                        _totalNotes = status.TotalNotes;

                        // Update UI
                        UpdateUI(status);
                    }
                }
            }
            catch
            {
                // Silently fail - server might not be ready yet
            }
        }

        private void UpdateUI(PlaybackStatus status)
        {
            // Update now playing title
            if (!string.IsNullOrEmpty(status.Title))
            {
                NowPlayingTitle.Text = status.Title;
            }
            else
            {
                NowPlayingTitle.Text = "Untitled";
            }

            // Update music display (upcoming notes)
            UpdateMusicDisplay(status.UpcomingNotes, status.CurrentNoteIndex);

            // Progress info is now shown in the time display at the bottom (consolidated)

            // Update progress bar
            if (_totalNotes > 0 && ProgressFill.Parent is System.Windows.FrameworkElement parent)
            {
                var progressWidth = (double)_currentNoteIndex / _totalNotes * parent.ActualWidth;
                AnimateProgressBar(progressWidth);
            }

            // Update time display
            CurrentTimeText.Text = _currentNoteIndex.ToString();
            TotalTimeText.Text = _totalNotes.ToString();

            // Update play/pause button state
            // Disable button in manual mode (can't pause manual playback)
            PlayPauseBtn.IsEnabled = !_isManualMode;

            // Update play/pause button icons (only matters when enabled)
            if (PlayPauseBtn.Template?.FindName("PlayIcon", PlayPauseBtn) is System.Windows.Shapes.Path playIcon &&
                PlayPauseBtn.Template?.FindName("PauseIcon", PlayPauseBtn) is System.Windows.Shapes.Path pauseIcon)
            {
                // Show pause icon when actively playing (not paused), show play icon otherwise
                if (_isPlaying && !_isPaused)
                {
                    playIcon.Visibility = Visibility.Collapsed;
                    pauseIcon.Visibility = Visibility.Visible;
                }
                else
                {
                    playIcon.Visibility = Visibility.Visible;
                    pauseIcon.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void UpdateMusicDisplay(string upcomingNotes, int currentNoteIndex)
        {
            if (!string.IsNullOrEmpty(upcomingNotes))
            {
                // Format like the AHK script: ► [current]upcoming...
                MusicDisplay.Text = "► " + upcomingNotes;

                // Auto-scroll to keep the beginning visible (where current note is)
                // This ensures the user always sees the current/next notes
                MusicDisplayScrollViewer.ScrollToLeftEnd();
            }
            else
            {
                MusicDisplay.Text = "(No music loaded)";
            }
        }

        private void AnimateProgressBar(double targetWidth)
        {
            var animation = new DoubleAnimation
            {
                To = targetWidth,
                Duration = TimeSpan.FromMilliseconds(100),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            ProgressFill.BeginAnimation(WidthProperty, animation);
        }

        // Window control handlers
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                // Double-click to maximize/restore (optional)
                WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            }
            else
            {
                DragMove();
            }
        }

        private void PinButton_Click(object sender, RoutedEventArgs e)
        {
            _isPinned = !_isPinned;
            Topmost = _isPinned;

            // Optional: change the pin icon appearance when unpinned
            if (!_isPinned)
            {
                PinIcon.Opacity = 0.5;
            }
            else
            {
                PinIcon.Opacity = 1.0;
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            _updateTimer?.Stop();
            _httpClient?.Dispose();
            Close();
        }

        // Player control handlers
        private async void PlayPauseBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Button should be disabled in manual mode, but double-check
                if (_isManualMode)
                {
                    return;
                }

                if (!string.IsNullOrEmpty(_currentMidiId))
                {
                    if (_isPlaying && !_isPaused)
                    {
                        // Currently playing - pause it
                        await _httpClient.PostAsync($"api/midi/pause/{_currentMidiId}", null);
                    }
                    else if (_isPaused)
                    {
                        // Currently paused - resume it
                        await _httpClient.PostAsync($"api/midi/resume/{_currentMidiId}", null);
                    }
                    else
                    {
                        // Not playing - start playback
                        await _httpClient.PostAsync($"api/midi/play/{_currentMidiId}", null);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error controlling playback: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void StopBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(_currentMidiId))
                {
                    // Call appropriate stop endpoint based on mode
                    if (_isManualMode)
                    {
                        await _httpClient.PostAsync($"api/midi/manual/stop/{_currentMidiId}", null);
                    }
                    else
                    {
                        await _httpClient.PostAsync($"api/midi/stop/{_currentMidiId}", null);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error stopping playback: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void SkipBackBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(_currentMidiId))
                {
                    // Stop current playback
                    if (_isManualMode)
                    {
                        await _httpClient.PostAsync($"api/midi/manual/stop/{_currentMidiId}", null);
                        await Task.Delay(100);
                        await _httpClient.PostAsync($"api/midi/manual/start/{_currentMidiId}", null);
                    }
                    else
                    {
                        await _httpClient.PostAsync($"api/midi/stop/{_currentMidiId}", null);
                        await Task.Delay(100);
                        await _httpClient.PostAsync($"api/midi/play/{_currentMidiId}", null);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error skipping to start: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _updateTimer?.Stop();
            _httpClient?.Dispose();
        }

        // Helper class for JSON deserialization
        private class PlaybackStatus
        {
            public bool IsPlaying { get; set; }
            public bool IsPaused { get; set; }
            public bool IsManualMode { get; set; }
            public string? CurrentMidiId { get; set; }
            public int CurrentNoteIndex { get; set; }
            public int TotalNotes { get; set; }
            public string? Title { get; set; }
            public string? UpcomingNotes { get; set; }
        }
    }
}
