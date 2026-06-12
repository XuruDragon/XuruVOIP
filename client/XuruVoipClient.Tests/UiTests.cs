using Xunit;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using XuruVoipClient.Views;
using XuruVoipClient.ViewModels;

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
}
