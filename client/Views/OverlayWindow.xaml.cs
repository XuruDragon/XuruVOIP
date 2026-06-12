using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using XuruVoipClient.ViewModels;
using XuruVoipClient.Models;
using XuruVoipClient.Services;

namespace XuruVoipClient.Views;

public class HudCaptionItem
{
    public string ChannelTag { get; set; } = "";
    public string SpeakerName { get; set; } = "";
    public string Text { get; set; } = "";
    public Brush ChannelBrush { get; set; } = Brushes.White;
    public DateTime Timestamp { get; set; }
}

public partial class OverlayWindow : Window
{
    private readonly MainViewModel _vm;
    private readonly DispatcherTimer _timer;
    private readonly ObservableCollection<OverlaySpeakerItem> _speakers = new();
    private readonly ObservableCollection<HudCaptionItem> _captions = new();

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
        LstCaptions.ItemsSource = _captions;

        // Set up refresh timer (100ms interval for positioning and speaker polling)
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _timer.Tick += Timer_Tick;
        _timer.Start();

        _vm.Stt.CaptionDecoded += OnCaptionDecoded;

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

        // Apply theme colors and layout visibility
        ApplyThemeColors(cfg.HudTheme);
        ConnectionHeader.Visibility = cfg.HudShowChannel ? Visibility.Visible : Visibility.Collapsed;
        ConnectionSeparator.Visibility = cfg.HudShowChannel ? Visibility.Visible : Visibility.Collapsed;

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

        // Update Intercom degradation state indicator on HUD
        if (cfg.EnableIntercomDegradation && _vm.IntercomState != IntercomDegradationState.Normal)
        {
            TxtIntercomStatus.Visibility = Visibility.Visible;
            TxtIntercomStatus.Text = _vm.IntercomState switch
            {
                IntercomDegradationState.ShieldHit => "⚡ INTERCOM: STATIC BURST",
                IntercomDegradationState.CriticalPower => "⚡ INTERCOM: POWER LOSS",
                IntercomDegradationState.QuantumTravel => "⚡ INTERCOM: QUANTUM WAVE",
                _ => ""
            };
        }
        else
        {
            TxtIntercomStatus.Visibility = Visibility.Collapsed;
        }

        // Update voice command status on HUD
        if (_vm.ShowVoiceCommandPanel)
        {
            VoiceCommandPanel.Visibility = Visibility.Visible;
            TxtVoiceCommandStatus.Text = _vm.VoiceCommandStatusText;
            
            var brush = _vm.VoiceCommandStatusColor switch
            {
                "Green" => BrushGreen,
                "Red" => BrushRed,
                _ => new SolidColorBrush(Color.FromRgb(0x00, 0xD2, 0xFF)) // Cyan / Tech Blue
            };
            
            VoiceCommandPanel.BorderBrush = brush;
            TxtVoiceCommandStatus.Foreground = brush;
        }
        else
        {
            VoiceCommandPanel.Visibility = Visibility.Collapsed;
        }

        // 4. Update dynamic positioning corner
        UpdateOverlayPosition();

        // 5. Update active speakers list
        UpdateActiveSpeakers();

        // 6. Update HUD captions / subtitles
        UpdateCaptions(cfg);

        // 7. Update Tactical Radar Overlay
        UpdateRadar(cfg);

        // 8. Update Hailing HUD panel
        UpdateHailPanel(cfg);
    }

    private void UpdateHailPanel(AppConfig cfg)
    {
        var state = _vm.CurrentHailState;
        if (state == HailState.Idle)
        {
            if (HailPanel.Visibility != Visibility.Collapsed)
            {
                HailPanel.Visibility = Visibility.Collapsed;
            }
            return;
        }

        if (HailPanel.Visibility != Visibility.Visible)
        {
            HailPanel.Visibility = Visibility.Visible;
        }

        string peerName = _vm.HailPeerName;
        switch (state)
        {
            case HailState.Outgoing:
                HailPanel.BorderBrush = BrushOrange;
                TxtHailHeader.Text = "OUTGOING HAIL";
                TxtHailHeader.Foreground = BrushOrange;
                TxtHailStatus.Text = peerName;
                TxtHailAction.Text = $"Calling pilot... Press {cfg.DeclineHailKey} to Cancel";
                break;

            case HailState.Incoming:
                HailPanel.BorderBrush = BrushOrange;
                TxtHailHeader.Text = "INCOMING HAIL";
                TxtHailHeader.Foreground = BrushOrange;
                TxtHailStatus.Text = peerName;
                TxtHailAction.Text = $"Press {cfg.AcceptHailKey} to Accept / {cfg.DeclineHailKey} to Decline";
                break;

            case HailState.Connected:
                HailPanel.BorderBrush = BrushGreen;
                TxtHailHeader.Text = "HAIL LINK ACTIVE";
                TxtHailHeader.Foreground = BrushGreen;
                TxtHailStatus.Text = peerName;
                TxtHailAction.Text = $"Hands-free open mic · Press {cfg.DeclineHailKey} to End";
                break;
        }
    }

    private void OnCaptionDecoded(string playerName, string text, byte channelType)
    {
        var cfg = _vm.Config.Config;
        bool wantsStt = cfg.EnableStt;
        bool wantsTranslation = cfg.EnableTranslationSubtitles;

        if (!wantsStt && !wantsTranslation) return;

        // Determine remote language, default to "en"
        string fromLang = "en";
        if (_vm.RemoteLanguages.TryGetValue(playerName, out var rlang) && !string.IsNullOrEmpty(rlang))
        {
            fromLang = rlang;
        }

        string toLang = cfg.Language;
        if (string.IsNullOrEmpty(toLang)) toLang = "en";

        string fromUpper = fromLang.Split('-')[0].ToUpperInvariant();
        string toUpper = toLang.Split('-')[0].ToUpperInvariant();

        bool isForeign = fromUpper != toUpper;

        if (isForeign && wantsTranslation)
        {
            string translated = TranslationService.TranslatePhrase(text, fromLang, toLang);
            text = $"[{fromUpper} -> {toUpper}] \"{translated}\"";
        }
        else if (wantsStt)
        {
            text = $"\"{text}\"";
        }
        else
        {
            return;
        }

        Dispatcher.Invoke(() =>
        {
            string channelTag = channelType switch
            {
                0x00 => "[Prox]",
                0x01 => "[Radio]",
                0x02 => "[Squad]",
                0x04 => "[Hail]",
                _ => "[Voip]"
            };

            Brush channelBrush = channelType switch
            {
                0x00 => BrushGreen,
                0x01 => new SolidColorBrush(Color.FromRgb(0x4E, 0x9F, 0xFF)),
                0x02 => new SolidColorBrush(Color.FromRgb(0xD8, 0x00, 0x64)),
                0x04 => BrushOrange,
                _ => Brushes.White
            };

            _captions.Add(new HudCaptionItem
            {
                ChannelTag = channelTag,
                SpeakerName = playerName + ":",
                Text = text,
                ChannelBrush = channelBrush,
                Timestamp = DateTime.UtcNow
            });

            while (_captions.Count > 4)
            {
                _captions.RemoveAt(0);
            }
        });
    }

    private void UpdateCaptions(AppConfig cfg)
    {
        if (!cfg.EnableStt && !cfg.EnableTranslationSubtitles)
        {
            if (LstCaptions.Visibility != Visibility.Collapsed)
            {
                LstCaptions.Visibility = Visibility.Collapsed;
                _captions.Clear();
            }
            return;
        }

        if (LstCaptions.Visibility != Visibility.Visible)
        {
            LstCaptions.Visibility = Visibility.Visible;
        }

        // If Whisper model is currently downloading, display status message
        if (_vm.Stt.IsDownloading)
        {
            _captions.Clear();
            _captions.Add(new HudCaptionItem
            {
                ChannelTag = "[System]",
                SpeakerName = "Speech-to-Text:",
                Text = _vm.Stt.DownloadStatusText,
                ChannelBrush = BrushOrange,
                Timestamp = DateTime.UtcNow.AddSeconds(10) // prevent immediate expiration
            });
            return;
        }

        // Expire captions older than 5 seconds
        var expired = new List<HudCaptionItem>();
        foreach (var cap in _captions)
        {
            if (cap.ChannelTag != "[System]" && (DateTime.UtcNow - cap.Timestamp).TotalSeconds > 5.0)
            {
                expired.Add(cap);
            }
        }
        foreach (var cap in expired)
        {
            _captions.Remove(cap);
        }
    }

    private void UpdateRadar(AppConfig cfg)
    {
        if (!cfg.EnableRadar || !cfg.HudShowRadar)
        {
            if (RadarPanel.Visibility != Visibility.Collapsed)
            {
                RadarPanel.Visibility = Visibility.Collapsed;
            }
            return;
        }

        if (RadarPanel.Visibility != Visibility.Visible)
        {
            RadarPanel.Visibility = Visibility.Visible;
        }

        double range = cfg.RadarRange;
        if (range <= 0) range = 50.0;

        TxtRadarRange.Text = $"{range:F0}m";
        RadarCanvas.Children.Clear();

        // Draw local player center arrow (pointing straight up)
        var centerArrow = new System.Windows.Shapes.Path
        {
            Fill = BrushGreen,
            Data = Geometry.Parse("M 0 -7 L -4 4 L 0 1 L 4 4 Z")
        };
        Canvas.SetLeft(centerArrow, 80);
        Canvas.SetTop(centerArrow, 80);
        RadarCanvas.Children.Add(centerArrow);

        var localPos = _vm.LastSentPos;
        if (localPos == null || localPos.IsEmpty)
        {
            return;
        }

        var activeSpeakers = _vm.Playback.GetActiveSpeakers(400);

        // Get listener heading vector
        double hx = _vm.Playback.ListenerHeadingX;
        double hy = _vm.Playback.ListenerHeadingY;
        if (hx == 0 && hy == 0)
        {
            hy = 1.0;
        }

        foreach (var kvp in _vm.RemotePositions)
        {
            string remoteName = kvp.Key;
            var remotePos = kvp.Value;

            // Only show players in the same zone/compartment layout
            if (remotePos.Zone != localPos.Zone) continue;

            double dx = remotePos.X - localPos.X;
            double dy = remotePos.Y - localPos.Y;
            double dist = Math.Sqrt(dx * dx + dy * dy);

            if (dist > range) continue; // Out of radar range

            // Rotate coordinates relative to local player heading (heading-up display)
            double yLocal = dx * hx + dy * hy;
            double xLocal = dx * hy - dy * hx;

            // Scale coordinates to fit 160x160 canvas (80px radius)
            double scale = 80.0 / range;
            double cx = 80.0 + xLocal * scale;
            double cy = 80.0 - yLocal * scale; // Invert Y for canvas

            // Boundary clipping (ensure blip stays within radar circle)
            if (Math.Sqrt((cx - 80) * (cx - 80) + (cy - 80) * (cy - 80)) > 78) continue;

            bool isSpeaking = activeSpeakers.Contains(remoteName);
            Brush blipBrush = isSpeaking ? BrushGreen : Brushes.White;

            if (isSpeaking)
            {
                // Speaking indicator pulsating ring
                var ring = new System.Windows.Shapes.Ellipse
                {
                    Width = 14,
                    Height = 14,
                    Stroke = BrushGreen,
                    StrokeThickness = 1,
                    Opacity = 0.6
                };
                Canvas.SetLeft(ring, cx - 7);
                Canvas.SetTop(ring, cy - 7);
                RadarCanvas.Children.Add(ring);
            }

            // Draw blip dot
            var blip = new System.Windows.Shapes.Ellipse
            {
                Width = 6,
                Height = 6,
                Fill = blipBrush
            };
            Canvas.SetLeft(blip, cx - 3);
            Canvas.SetTop(blip, cy - 3);
            RadarCanvas.Children.Add(blip);

            // Draw player name tag
            var nameText = new TextBlock
            {
                Text = remoteName,
                Foreground = Brushes.White,
                FontSize = 8.5,
                FontWeight = FontWeights.SemiBold,
                Opacity = 0.85
            };
            Canvas.SetLeft(nameText, cx + 5);
            Canvas.SetTop(nameText, cy - 6);
            RadarCanvas.Children.Add(nameText);
        }
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
        var cfg = _vm.Config.Config;
        var activeTelemetry = _vm.Playback.GetActiveSpeakersTelemetry(400);

        var existingMap = new Dictionary<string, OverlaySpeakerItem>();
        foreach (var item in _speakers)
        {
            existingMap[item.PlayerName] = item;
        }

        var newSpeakers = new List<OverlaySpeakerItem>();

        foreach (var tel in activeTelemetry)
        {
            Brush channelBrush = tel.AudioType switch
            {
                0x00 => BrushGreen,
                0x01 => new SolidColorBrush(Color.FromRgb(0x4E, 0x9F, 0xFF)),
                0x02 => new SolidColorBrush(Color.FromRgb(0xD8, 0x00, 0x64)),
                0x03 => BrushOrange,
                0x04 => BrushOrange,
                _ => Brushes.White
            };

            if (existingMap.TryGetValue(tel.PlayerName, out var item))
            {
                item.ChannelBrush = channelBrush;
                item.SpectrogramVisibility = cfg.EnableVisorSpectrogram ? Visibility.Visible : Visibility.Collapsed;
                for (int b = 0; b < 8; b++)
                {
                    item.SpectralBars[b].Height = tel.SpectralBands[b] * 16.0;
                    item.SpectralBars[b].Brush = channelBrush;
                }
                newSpeakers.Add(item);
            }
            else
            {
                var newItem = new OverlaySpeakerItem
                {
                    PlayerName = tel.PlayerName,
                    ChannelBrush = channelBrush,
                    SpectrogramVisibility = cfg.EnableVisorSpectrogram ? Visibility.Visible : Visibility.Collapsed
                };
                for (int b = 0; b < 8; b++)
                {
                    newItem.SpectralBars.Add(new SpectralBarItem
                    {
                        Height = tel.SpectralBands[b] * 16.0,
                        Brush = channelBrush
                    });
                }
                newSpeakers.Add(newItem);
            }
        }

        bool listChanged = _speakers.Count != newSpeakers.Count;
        if (!listChanged)
        {
            for (int i = 0; i < newSpeakers.Count; i++)
            {
                if (_speakers[i].PlayerName != newSpeakers[i].PlayerName)
                {
                    listChanged = true;
                    break;
                }
            }
        }

        if (listChanged)
        {
            _speakers.Clear();
            foreach (var s in newSpeakers)
            {
                _speakers.Add(s);
            }
        }

        // Toggle "No Transmissions" visibility
        if (_speakers.Count > 0)
        {
            TxtSpeakersHeader.Visibility = cfg.HudShowActiveSpeakers ? Visibility.Visible : Visibility.Collapsed;
            LstSpeakers.Visibility = cfg.HudShowActiveSpeakers ? Visibility.Visible : Visibility.Collapsed;
            TxtNoSpeakers.Visibility = Visibility.Collapsed;
        }
        else
        {
            TxtSpeakersHeader.Visibility = Visibility.Collapsed;
            LstSpeakers.Visibility = Visibility.Collapsed;
            TxtNoSpeakers.Visibility = cfg.HudShowActiveSpeakers ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private string _currentAppliedTheme = "";
    private void ApplyThemeColors(string themeName)
    {
        if (_currentAppliedTheme == themeName) return;
        _currentAppliedTheme = themeName;

        Color themeColor = themeName switch
        {
            "Anvil" => Color.FromRgb(0xFF, 0x98, 0x00), // Orange
            "Drake" => Color.FromRgb(0x3D, 0xDB, 0x85), // Green
            "RSI" => Color.FromRgb(0x00, 0xE5, 0xFF),   // Cyan / Light Blue
            "Origin" => Color.FromRgb(0xD8, 0x00, 0x64), // Magenta/Purple
            _ => Color.FromRgb(0x00, 0xD2, 0xFF)       // Aegis / Default (Cyan)
        };

        var borderBrush = new SolidColorBrush(Color.FromArgb(0x80, themeColor.R, themeColor.G, themeColor.B));
        var textBrush = new SolidColorBrush(themeColor);
        var ringOuterBrush = new SolidColorBrush(Color.FromArgb(0x20, themeColor.R, themeColor.G, themeColor.B));
        var ringInnerBrush = new SolidColorBrush(Color.FromArgb(0x10, themeColor.R, themeColor.G, themeColor.B));
        var gridBrush = new SolidColorBrush(Color.FromArgb(0x15, themeColor.R, themeColor.G, themeColor.B));

        HudPanel.BorderBrush = new SolidColorBrush(Color.FromArgb(0x40, themeColor.R, themeColor.G, themeColor.B));
        RadarPanel.BorderBrush = borderBrush;
        
        RadarRingOuter.Stroke = ringOuterBrush;
        RadarRingInner.Stroke = ringInnerBrush;
        RadarLineVert.Stroke = gridBrush;
        RadarLineHoriz.Stroke = gridBrush;
        TxtRadarRange.Foreground = borderBrush;
    }

    public void CloseOverlay()
    {
        _timer.Stop();
        _vm.Stt.CaptionDecoded -= OnCaptionDecoded;
        Close();
    }
}

public class SpectralBarItem : System.ComponentModel.INotifyPropertyChanged
{
    private double _height;
    private Brush _brush = Brushes.Green;

    public double Height
    {
        get => _height;
        set { if (_height != value) { _height = value; OnPropertyChanged(); } }
    }

    public Brush Brush
    {
        get => _brush;
        set { if (_brush != value) { _brush = value; OnPropertyChanged(); } }
    }

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
}

public class OverlaySpeakerItem : System.ComponentModel.INotifyPropertyChanged
{
    private string _playerName = "";
    private Brush _channelBrush = Brushes.Green;
    private Visibility _spectrogramVisibility = Visibility.Collapsed;

    public string PlayerName
    {
        get => _playerName;
        set { if (_playerName != value) { _playerName = value; OnPropertyChanged(); } }
    }

    public Brush ChannelBrush
    {
        get => _channelBrush;
        set { if (_channelBrush != value) { _channelBrush = value; OnPropertyChanged(); } }
    }

    public Visibility SpectrogramVisibility
    {
        get => _spectrogramVisibility;
        set { if (_spectrogramVisibility != value) { _spectrogramVisibility = value; OnPropertyChanged(); } }
    }

    public ObservableCollection<SpectralBarItem> SpectralBars { get; } = new();

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
}
