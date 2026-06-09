using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Hardcodet.Wpf.TaskbarNotification;
using XuruVoipClient.ViewModels;
using XuruVoipClient.Views;
using XuruVoipClient.Services;

namespace XuruVoipClient;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;
    private readonly DispatcherTimer _vuTimer;
    private readonly OverlayWindow _overlayWindow;

    // Cached brushes
    private static readonly SolidColorBrush BrushGreen = new(Color.FromRgb(0x3D, 0xDB, 0x85));
    private static readonly SolidColorBrush BrushRed   = new(Color.FromRgb(0xFF, 0x4E, 0x6A));

    public MainWindow()
    {
        InitializeComponent();
        _vm = App.ViewModel;
        DataContext = _vm;

        // Initialize Overlay HUD Window
        _overlayWindow = new OverlayWindow(_vm);
        _overlayWindow.Show();

        // Set window title with version
        var version = typeof(MainWindow).Assembly.GetName().Version;
        string versionStr = version != null ? (version.Revision > 0 ? $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}" : $"{version.Major}.{version.Minor}.{version.Build}") : "1.0.0";
        string titleBase = FindResource("TitleMain") as string ?? "XuruVoip";
        string titleText = $"{titleBase} - version {versionStr}";
        Title = titleText;
        WindowTitleText.Text = titleText;

        // Start background update check
        _ = CheckUpdatesAsync();

        // Watch connection state changes
        _vm.PropertyChanged += (_, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(MainViewModel.PosConnected):
                    PosLed.Fill = _vm.PosConnected ? BrushGreen : BrushRed;
                    break;
                case nameof(MainViewModel.AudioConnected):
                    AudioLed.Fill = _vm.AudioConnected ? BrushGreen : BrushRed;
                    break;
                case nameof(MainViewModel.IsTalking):
                    UpdateTalkingState(_vm.IsTalking);
                    break;
                case nameof(MainViewModel.InputLevel):
                    UpdateVuMeter(_vm.InputLevel);
                    break;
            }
        };

        // VU meter timer
        _vuTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
        _vuTimer.Tick += (_, _) => UpdateVuMeter(_vm.InputLevel);
        _vuTimer.Start();
    }

    private void UpdateTalkingState(bool talking)
    {
        var sb = (Storyboard)FindResource(talking ? "PulseAnim" : "StopPulse");
        sb.Begin(this, true);
    }

    private void UpdateVuMeter(float level)
    {
        // VuBar max width = container width; clamp level 0..1
        double maxW = VuBar.Parent is FrameworkElement parent ? parent.ActualWidth : 200;
        VuBar.Width = Math.Max(2, level * maxW);

        // Colour: green → yellow → red
        VuBar.Background = level switch
        {
            > 0.85f => BrushRed,
            > 0.6f  => new SolidColorBrush(Color.FromRgb(0xFF, 0xC1, 0x07)),
            _       => BrushGreen
        };
    }

    // ─── Window chrome ───────────────────────────────────────────────────────
    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left) DragMove();
    }

    private void Minimize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        _vuTimer.Stop();
        _overlayWindow.CloseOverlay();
        Application.Current.Shutdown();
    }

    // ─── Actions ─────────────────────────────────────────────────────────────
    private async void Connect_Click(object sender, RoutedEventArgs e)
    {
        await _vm.ConnectAsync();
    }

    private void Disconnect_Click(object sender, RoutedEventArgs e)
    {
        _vm.Disconnect();
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        var win = new SettingsWindow(_vm) { Owner = this };
        win.ShowDialog();
    }

    private async System.Threading.Tasks.Task CheckUpdatesAsync()
    {
        var currentVer = typeof(MainWindow).Assembly.GetName().Version;
        if (currentVer == null) return;

        string currentVerStr = $"{currentVer.Major}.{currentVer.Minor}.{currentVer.Build}";
        var (available, latest) = await UpdateService.CheckForUpdatesAsync(currentVerStr);
        if (available)
        {
            _vm.IsUpdateAvailable = true;
            _vm.LatestVersion = latest;
        }
    }

    private void DownloadUpdate_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://github.com/XuruDragon/XuruVOIP/releases",
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(psi);
        }
        catch (Exception ex)
        {
            LogService.Error("Failed to open update URL", ex);
        }
    }
}