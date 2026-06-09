using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using Microsoft.Win32;
using NAudio.Wave;
using XuruVoipClient.Models;
using XuruVoipClient.ViewModels;

namespace XuruVoipClient.Views;

public partial class SettingsWindow : Window
{
    private readonly MainViewModel _vm;
    public MainViewModel ViewModel => _vm;
    private AppConfig Cfg => _vm.Config.Config;

    public SettingsWindow(MainViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = _vm.Config;

        LoadDevices();
        LoadMonitors();
        LoadLanguages();
        LoadCurrentValues();
    }

    private void LoadLanguages()
    {
        CbLanguage.Items.Clear();
        CbLanguage.Items.Add(new { Code = "en", Name = "English" });
        CbLanguage.Items.Add(new { Code = "fr", Name = "Français" });
        CbLanguage.Items.Add(new { Code = "de", Name = "Deutsch" });
        CbLanguage.Items.Add(new { Code = "es", Name = "Español" });
        CbLanguage.Items.Add(new { Code = "pt-BR", Name = "Português (BR)" });
        CbLanguage.Items.Add(new { Code = "pt-PT", Name = "Português (PT)" });
        CbLanguage.Items.Add(new { Code = "zh", Name = "简体中文" });
        CbLanguage.Items.Add(new { Code = "ja", Name = "日本語" });
        CbLanguage.DisplayMemberPath = "Name";
        CbLanguage.SelectedValuePath = "Code";

        string currentLang = Cfg.Language;
        if (string.IsNullOrEmpty(currentLang)) currentLang = "en";
        CbLanguage.SelectedValue = currentLang;
    }

    private void LoadDevices()
    {
        // Input devices
        CbInput.Items.Clear();
        for (int i = 0; i < WaveInEvent.DeviceCount; i++)
        {
            var cap = WaveInEvent.GetCapabilities(i);
            CbInput.Items.Add(cap.ProductName);
        }
        if (CbInput.Items.Count > 0)
            CbInput.SelectedIndex = Math.Min(Cfg.InputDeviceIndex, CbInput.Items.Count - 1);

        // Output devices
        CbOutput.Items.Clear();
        for (int i = 0; i < WaveOut.DeviceCount; i++)
        {
            var cap = WaveOut.GetCapabilities(i);
            CbOutput.Items.Add(cap.ProductName);
        }
        if (CbOutput.Items.Count > 0)
            CbOutput.SelectedIndex = Math.Min(Cfg.OutputDeviceIndex, CbOutput.Items.Count - 1);
    }

    private void LoadMonitors()
    {
        CbMonitor.Items.Clear();
        var screens = Screen.AllScreens;
        for (int i = 0; i < screens.Length; i++)
        {
            var s = screens[i];
            CbMonitor.Items.Add($"Monitor {i + 1}: {s.Bounds.Width}×{s.Bounds.Height}" +
                                 (s.Primary ? " (Primary)" : ""));
        }
        CbMonitor.SelectedIndex = Math.Min(Cfg.OcrMonitorIndex, screens.Length - 1);
    }

    private void LoadCurrentValues()
    {
        // Passwords (not bound directly by WPF security model)
        PbPassword.Password = Cfg.ServerPassword;
        PbUserPassword.Password = Cfg.UserPassword;

        // Sliders
        SlInputGain.Value = Cfg.InputGainDb;
        SlOutputGain.Value = Cfg.OutputGainPercent;
        SlVad.Value = Cfg.VadSensitivity;

        // Audio mode radio buttons
        if (Cfg.AudioMode == AudioMode.PTT) { RbPtt.IsChecked = true; ShowPttPanel(true); }
        else { RbVad.IsChecked = true; ShowPttPanel(false); }

        // Gain labels
        InputGainLabel.Text = $"{Cfg.InputGainDb:+0;-0;0} dB";
        OutputGainLabel.Text = $"{Cfg.OutputGainPercent:F0}%";

        // Position Tracking Source
        CbPosSource.Items.Clear();
        CbPosSource.Items.Add(new { Code = false, Name = System.Windows.Application.Current.TryFindResource("OptOcr") as string ?? "OCR Screen Scanner" });
        CbPosSource.Items.Add(new { Code = true, Name = System.Windows.Application.Current.TryFindResource("OptGrtpr") as string ?? "Game.log Reader (GRTPR)" });
        CbPosSource.DisplayMemberPath = "Name";
        CbPosSource.SelectedValuePath = "Code";
        CbPosSource.SelectedValue = Cfg.UseGrtpr;
        UpdateTrackingPanels(Cfg.UseGrtpr);

        // Overlay Position
        CbOverlayPosition.Items.Clear();
        CbOverlayPosition.Items.Add(new { Code = "TopLeft", Name = System.Windows.Application.Current.TryFindResource("OptTopLeft") as string ?? "Top-Left" });
        CbOverlayPosition.Items.Add(new { Code = "TopCenter", Name = System.Windows.Application.Current.TryFindResource("OptTopCenter") as string ?? "Top-Center" });
        CbOverlayPosition.Items.Add(new { Code = "TopRight", Name = System.Windows.Application.Current.TryFindResource("OptTopRight") as string ?? "Top-Right" });
        CbOverlayPosition.Items.Add(new { Code = "BottomLeft", Name = System.Windows.Application.Current.TryFindResource("OptBottomLeft") as string ?? "Bottom-Left" });
        CbOverlayPosition.Items.Add(new { Code = "BottomCenter", Name = System.Windows.Application.Current.TryFindResource("OptBottomCenter") as string ?? "Bottom-Center" });
        CbOverlayPosition.Items.Add(new { Code = "BottomRight", Name = System.Windows.Application.Current.TryFindResource("OptBottomRight") as string ?? "Bottom-Right" });
        CbOverlayPosition.DisplayMemberPath = "Name";
        CbOverlayPosition.SelectedValuePath = "Code";
        CbOverlayPosition.SelectedValue = Cfg.OverlayPosition;

        // OCR region display
        UpdateRegionDisplay();
    }

    private void UpdateTrackingPanels(bool useGrtpr)
    {
        if (OcrControlsPanel != null) OcrControlsPanel.Visibility = useGrtpr ? Visibility.Collapsed : Visibility.Visible;
        if (GrtprControlsPanel != null) GrtprControlsPanel.Visibility = useGrtpr ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateRegionDisplay()
    {
        var r = Cfg.OcrRegion;
        if (TbRegion != null)
            TbRegion.Text = $"X={r.X:F0} Y={r.Y:F0} W={r.Width:F0} H={r.Height:F0}";
    }

    private void CbPosSource_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (Cfg == null || CbPosSource.SelectedValue == null) return;
        bool useGrtpr = (bool)CbPosSource.SelectedValue;
        Cfg.UseGrtpr = useGrtpr;
        UpdateTrackingPanels(useGrtpr);
    }

    private void CbOverlayPosition_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (Cfg == null || CbOverlayPosition.SelectedValue == null) return;
        Cfg.OverlayPosition = (string)CbOverlayPosition.SelectedValue;
    }

    // ─── Events ──────────────────────────────────────────────────────────────
    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left) DragMove();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        string serverAddr = TxServer.Text.Trim();
        if (!System.Net.IPAddress.TryParse(serverAddr, out _))
        {
            string msg = System.Windows.Application.Current.TryFindResource("MsgInvalidServerAddress") as string 
                         ?? "The Server Address must be a valid IP address (e.g., 127.0.0.1).";
            string title = System.Windows.Application.Current.TryFindResource("TitleInvalidServerAddress") as string 
                           ?? "Invalid Server Address";
            System.Windows.MessageBox.Show(
                msg,
                title,
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
            return;
        }

        Cfg.ServerPassword = PbPassword.Password;
        Cfg.UserPassword = PbUserPassword.Password;
        _vm.SaveConfig();
        _vm.ApplySettings();
        Close();
    }

    private void CbLanguage_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (Cfg == null || CbLanguage.SelectedValue == null) return;
        string selectedLang = (string)CbLanguage.SelectedValue;
        Cfg.Language = selectedLang;
        App.SetLanguage(selectedLang);
        _vm.RefreshLocalizedStrings();
    }

    private void CbMonitor_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        => Cfg.OcrMonitorIndex = CbMonitor.SelectedIndex;

    private void CbInput_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        => Cfg.InputDeviceIndex = CbInput.SelectedIndex;

    private void CbOutput_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        => Cfg.OutputDeviceIndex = CbOutput.SelectedIndex;

    private void SlInputGain_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (InputGainLabel != null) InputGainLabel.Text = $"{e.NewValue:+0;-0;0} dB";
        if (Cfg != null) Cfg.InputGainDb = e.NewValue;
    }

    private void SlOutputGain_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (OutputGainLabel != null) OutputGainLabel.Text = $"{e.NewValue:F0}%";
        if (Cfg != null) Cfg.OutputGainPercent = e.NewValue;
    }

    private void SlVad_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (Cfg != null) Cfg.VadSensitivity = (int)e.NewValue;
    }

    private void AudioMode_Changed(object sender, RoutedEventArgs e)
    {
        if (Cfg == null) return;
        Cfg.AudioMode = RbPtt.IsChecked == true ? AudioMode.PTT : AudioMode.VAD;
        ShowPttPanel(Cfg.AudioMode == AudioMode.PTT);
    }

    private void ShowPttPanel(bool ptt)
    {
        if (PttPanel != null) PttPanel.Visibility = ptt ? Visibility.Visible : Visibility.Collapsed;
        if (VadPanel != null) VadPanel.Visibility = ptt ? Visibility.Collapsed : Visibility.Visible;
    }

    private void Hotkey_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        e.Handled = true;
        var keyName = e.Key == Key.System ? e.SystemKey.ToString() : e.Key.ToString();
        if (sender is System.Windows.Controls.TextBox tb)
        {
            tb.Text = keyName;
            var binding = tb.GetBindingExpression(System.Windows.Controls.TextBox.TextProperty);
            binding?.UpdateSource();
        }
    }

    private void PickRegion_Click(object sender, RoutedEventArgs e)
    {
        var picker = new OcrRegionPicker(Cfg.OcrMonitorIndex, Cfg.OcrRegion);
        if (picker.ShowDialog() == true)
        {
            Cfg.OcrRegion = picker.SelectedRegion;
            UpdateRegionDisplay();
        }
    }

    private void BrowseLogPath_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Game Log (Game.log)|Game.log|All Files (*.*)|*.*",
            Title = "Select Star Citizen Game.log File"
        };
        if (dialog.ShowDialog() == true)
        {
            TxGameLogPath.Text = dialog.FileName;
            var binding = TxGameLogPath.GetBindingExpression(System.Windows.Controls.TextBox.TextProperty);
            binding?.UpdateSource();
        }
    }

    // ─── OCR preview refresh ─────────────────────────────────────────────────
    private void RefreshOcrPreview()
    {
        // Display last OCR raw text from the service (via ViewModel)
        TbOcrPreview.Text = "(reconnect OCR service to preview)";
    }
}
