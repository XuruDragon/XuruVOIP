using System;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using XuruVoipClient.ViewModels;

namespace XuruVoipClient.Views;

public partial class OverlayWindow : Window
{
    private readonly MainViewModel _vm;
    private readonly DispatcherTimer _timer;
    private readonly ObservableCollection<string> _speakers = new();

    // Win32 APIs for click-through window
    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hwnd, int index);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x20;
    private const int WS_EX_NOACTIVATE = 0x08000000;

    private static readonly SolidColorBrush BrushGreen = new(Color.FromRgb(0x3D, 0xDB, 0x85));
    private static readonly SolidColorBrush BrushOrange = new(Color.FromRgb(0xFF, 0x98, 0x00));
    private static readonly SolidColorBrush BrushRed   = new(Color.FromRgb(0xFF, 0x4E, 0x6A));

    public OverlayWindow(MainViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        
        LstSpeakers.ItemsSource = _speakers;

        // Set up refresh timer (100ms interval for positioning and speaker polling)
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _timer.Tick += Timer_Tick;
        _timer.Start();

        Loaded += OverlayWindow_Loaded;
    }

    private void OverlayWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Apply WS_EX_TRANSPARENT and WS_EX_NOACTIVATE so the window is click-through
        var hwnd = new WindowInteropHelper(this).Handle;
        int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT | WS_EX_NOACTIVATE);

        UpdateOverlayPosition();
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        var cfg = _vm.Config.Config;
        
        // 1. If overlay is disabled, hide it
        if (!cfg.EnableOverlay)
        {
            if (Visibility != Visibility.Collapsed)
            {
                Visibility = Visibility.Collapsed;
            }
            return;
        }

        // 2. Track Star Citizen's window position and visibility
        var gameRect = _vm.GameDetector.GetGameClientRectInScreenCoords();
        if (gameRect == null)
        {
            // Game is not running or window not found
            if (Visibility != Visibility.Collapsed)
            {
                Visibility = Visibility.Collapsed;
            }
            return;
        }

        // Snap window size and position to game client area
        double left = gameRect.Value.Left;
        double top = gameRect.Value.Top;
        double width = gameRect.Value.Right - gameRect.Value.Left;
        double height = gameRect.Value.Bottom - gameRect.Value.Top;

        // Prevent setting negative or tiny sizes
        if (width > 100 && height > 100)
        {
            if (Left != left) Left = left;
            if (Top != top) Top = top;
            if (Width != width) Width = width;
            if (Height != height) Height = height;

            if (Visibility != Visibility.Visible)
            {
                Visibility = Visibility.Visible;
            }
        }
        else
        {
            if (Visibility != Visibility.Collapsed)
            {
                Visibility = Visibility.Collapsed;
            }
            return;
        }

        // 3. Update connection status and active channel
        if (_vm.AudioConnected)
        {
            StatusDot.Fill = BrushGreen;
            TxtStatus.Text = _vm.ActiveChannelName;
        }
        else if (_vm.StatusMessage.Contains("Connecting"))
        {
            StatusDot.Fill = BrushOrange;
            TxtStatus.Text = "Connecting...";
        }
        else
        {
            StatusDot.Fill = BrushRed;
            TxtStatus.Text = "Disconnected";
        }

        // 4. Update dynamic positioning corner
        UpdateOverlayPosition();

        // 5. Update active speakers list
        UpdateActiveSpeakers();
    }

    private void UpdateOverlayPosition()
    {
        string pos = _vm.Config.Config.OverlayPosition ?? "TopLeft";
        switch (pos)
        {
            case "TopLeft":
                HudPanel.HorizontalAlignment = HorizontalAlignment.Left;
                HudPanel.VerticalAlignment = VerticalAlignment.Top;
                break;
            case "TopCenter":
                HudPanel.HorizontalAlignment = HorizontalAlignment.Center;
                HudPanel.VerticalAlignment = VerticalAlignment.Top;
                break;
            case "TopRight":
                HudPanel.HorizontalAlignment = HorizontalAlignment.Right;
                HudPanel.VerticalAlignment = VerticalAlignment.Top;
                break;
            case "BottomLeft":
                HudPanel.HorizontalAlignment = HorizontalAlignment.Left;
                HudPanel.VerticalAlignment = VerticalAlignment.Bottom;
                break;
            case "BottomCenter":
                HudPanel.HorizontalAlignment = HorizontalAlignment.Center;
                HudPanel.VerticalAlignment = VerticalAlignment.Bottom;
                break;
            case "BottomRight":
                HudPanel.HorizontalAlignment = HorizontalAlignment.Right;
                HudPanel.VerticalAlignment = VerticalAlignment.Bottom;
                break;
            default:
                HudPanel.HorizontalAlignment = HorizontalAlignment.Left;
                HudPanel.VerticalAlignment = VerticalAlignment.Top;
                break;
        }
    }

    private void UpdateActiveSpeakers()
    {
        var currentSpeakers = _vm.Playback.GetActiveSpeakers(400);

        // Update local speakers collection intelligently to avoid UI flickering
        bool unchanged = currentSpeakers.Count == _speakers.Count;
        if (unchanged)
        {
            for (int i = 0; i < currentSpeakers.Count; i++)
            {
                if (currentSpeakers[i] != _speakers[i])
                {
                    unchanged = false;
                    break;
                }
            }
        }

        if (!unchanged)
        {
            _speakers.Clear();
            foreach (var s in currentSpeakers)
            {
                _speakers.Add(s);
            }
        }

        // Toggle "No Transmissions" visibility
        if (_speakers.Count > 0)
        {
            TxtSpeakersHeader.Visibility = Visibility.Visible;
            LstSpeakers.Visibility = Visibility.Visible;
            TxtNoSpeakers.Visibility = Visibility.Collapsed;
        }
        else
        {
            TxtSpeakersHeader.Visibility = Visibility.Collapsed;
            LstSpeakers.Visibility = Visibility.Collapsed;
            TxtNoSpeakers.Visibility = Visibility.Visible;
        }
    }

    public void CloseOverlay()
    {
        _timer.Stop();
        Close();
    }
}
