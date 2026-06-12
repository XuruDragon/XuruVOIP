using Xunit;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Controls;
using XuruVoipClient.Views;
using XuruVoipClient.ViewModels;
using XuruVoipClient.Models;

namespace XuruVoipClient.Tests;

public class OverlayTests
{
    static OverlayTests()
    {
        UiTests.InitializeWpfApplication();
    }

    [StaFact]
    public async Task OverlayWindow_ShouldDisplayElevationOnRadar()
    {
        // GIVEN
        await using var vm = new MainViewModel();
        var overlay = new OverlayWindow(vm);

        // Enable overlay & radar
        vm.Config.Config.EnableOverlay = true;
        vm.Config.Config.EnableRadar = true;
        vm.Config.Config.HudShowRadar = true;
        vm.Config.Config.RadarRange = 50.0;

        // Set local position to 0,0,0
        var localPosField = typeof(MainViewModel).GetField("_lastSentPos", BindingFlags.NonPublic | BindingFlags.Instance);
        localPosField!.SetValue(vm, new PlayerPosition { X = 0, Y = 0, Z = 0, Zone = "Stanton" });
        vm.AudioConnected = true;

        // Add a remote player "Bob" at 10m north, and 15m elevation (above)
        var remotePositionsField = typeof(MainViewModel).GetField("_remotePositions", BindingFlags.NonPublic | BindingFlags.Instance);
        var remotePositions = (Dictionary<string, PlayerPosition>)remotePositionsField!.GetValue(vm)!;
        remotePositions["Bob"] = new PlayerPosition { X = 0, Y = 10, Z = 15, Zone = "Stanton" };

        // Force a UI tick update by invoking UpdateRadar
        var updateRadarMethod = typeof(OverlayWindow).GetMethod("UpdateRadar", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(updateRadarMethod);
        updateRadarMethod.Invoke(overlay, new object[] { vm.Config.Config });

        // Retrieve elements from RadarCanvas
        var canvas = overlay.RadarCanvas;
        Assert.NotNull(canvas);

        // Find the TextBlock for Bob
        bool foundBobWithElevation = false;
        foreach (var child in canvas.Children)
        {
            if (child is TextBlock tb && tb.Text.Contains("Bob"))
            {
                if (tb.Text.Contains("▲") && tb.Text.Contains("15m"))
                {
                    foundBobWithElevation = true;
                }
            }
        }

        // THEN
        Assert.True(foundBobWithElevation, "Bob's tag should contain the upward elevation indicator.");

        overlay.CloseOverlay();
    }
}
