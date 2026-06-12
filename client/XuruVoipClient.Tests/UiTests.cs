using Xunit;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using XuruVoipClient.Views;
using XuruVoipClient.ViewModels;
using XuruVoipClient.Services;
using System.Windows.Media;

namespace XuruVoipClient.Tests;

public class UiTests
{
    static UiTests()
    {
        InitializeWpfApplication();
    }

    public static void InitializeWpfApplication()
    {
        if (Application.Current == null)
        {
            _ = System.IO.Packaging.PackUriHelper.UriSchemePack;
            var app = new XuruVoipClient.App { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            app.InitializeComponent();
        }
        App.SetLanguage("en");
    }

    [StaFact]
    public async Task SettingsWindow_TabNavigation_ShouldSelectTab()
    {
        // GIVEN
        await using var vm = new MainViewModel();
        var window = new SettingsWindow(vm);

        // WHEN
        var tabControl = window.SettingsTabControl;
        Assert.NotNull(tabControl);

        // THEN
        Assert.Equal(9, tabControl.Items.Count);

        tabControl.SelectedIndex = 1;
        Assert.Equal(1, tabControl.SelectedIndex);

        tabControl.SelectedIndex = 4;
        Assert.Equal(4, tabControl.SelectedIndex);

        window.Close();
    }

    [StaFact]
    public async Task ConnectionDiagnostics_ShouldShowPlaceholdersWhenDisconnected_AndValuesWhenConnected()
    {
        // GIVEN
        await using var vm = new MainViewModel();

        // THEN (Disconnected by default)
        Assert.Equal("--", vm.Latency);
        Assert.Equal("--", vm.Jitter);
        Assert.Equal("--", vm.PacketLoss);

        // WHEN (Connected)
        vm.PosConnected = true;
        vm.AudioConnected = true;
        vm.UpdateDiagnostics();

        // THEN (Should have simulated metrics)
        Assert.NotEqual("--", vm.Latency);
        Assert.NotEqual("--", vm.Jitter);
        Assert.NotEqual("--", vm.PacketLoss);

        // WHEN (Disconnected again)
        vm.PosConnected = false;
        vm.AudioConnected = false;
        vm.UpdateDiagnostics();

        // THEN (Should reset to placeholders)
        Assert.Equal("--", vm.Latency);
        Assert.Equal("--", vm.Jitter);
        Assert.Equal("--", vm.PacketLoss);
    }

    [StaFact]
    public void ThemeManager_ApplyTheme_ShouldUpdateApplicationResources()
    {
        // GIVEN (Apply Aegis/Default theme)
        ThemeManager.ApplyTheme("Aegis");
        
        // THEN
        Assert.Equal(Color.FromRgb(0x00, 0xE6, 0x76), (Color)Application.Current.Resources["AccentColor"]);
        Assert.Equal(Color.FromRgb(0x00, 0x8E, 0x3C), (Color)Application.Current.Resources["AccentGlowColor"]);
        Assert.Equal(Color.FromRgb(0x0A, 0x0F, 0x0D), (Color)Application.Current.Resources["BgDeepColor"]);
        Assert.Equal(Color.FromArgb(0xCC, 0x0A, 0x0F, 0x0D), (Color)Application.Current.Resources["HudBackgroundColor"]);
        
        // WHEN (Apply Anvil theme)
        ThemeManager.ApplyTheme("Anvil");
        
        // THEN
        Assert.Equal(Color.FromRgb(0xFF, 0x17, 0x44), (Color)Application.Current.Resources["AccentColor"]);
        Assert.Equal(Color.FromRgb(0xB7, 0x00, 0x1E), (Color)Application.Current.Resources["AccentGlowColor"]);
        Assert.Equal(Color.FromRgb(0x10, 0x0C, 0x0D), (Color)Application.Current.Resources["BgDeepColor"]);
        Assert.Equal(Color.FromArgb(0xCC, 0x10, 0x0C, 0x0D), (Color)Application.Current.Resources["HudBackgroundColor"]);
        
        // WHEN (Apply Drake theme)
        ThemeManager.ApplyTheme("Drake");
        
        // THEN
        Assert.Equal(Color.FromRgb(0xFF, 0x73, 0x00), (Color)Application.Current.Resources["AccentColor"]);
        Assert.Equal(Color.FromRgb(0x0F, 0x0D, 0x0C), (Color)Application.Current.Resources["BgDeepColor"]);
    }
}
