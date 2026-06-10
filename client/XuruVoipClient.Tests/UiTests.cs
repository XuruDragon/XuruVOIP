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
        Assert.Equal(6, tabControl.Items.Count);

        tabControl.SelectedIndex = 1;
        Assert.Equal(1, tabControl.SelectedIndex);

        tabControl.SelectedIndex = 4;
        Assert.Equal(4, tabControl.SelectedIndex);

        window.Close();
    }
}
