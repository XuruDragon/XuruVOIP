using Xunit;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using XuruVoipClient.Services;
using XuruVoipClient.ViewModels;
using XuruVoipClient.Models;

namespace XuruVoipClient.Tests;

public class RadioRelayTests
{
    static RadioRelayTests()
    {
        UiTests.InitializeWpfApplication();
    }

    [Fact]
    public async Task Dijkstra_ShouldReturnDirectDistance_IfRepeatersDisabled()
    {
        await using var vm = new MainViewModel();
        
        vm.Config.Config.EnableRadioRepeaters = false;

        // Set local position to 0,0,0
        var localPosField = typeof(MainViewModel).GetField("_lastSentPos", BindingFlags.NonPublic | BindingFlags.Instance);
        localPosField!.SetValue(vm, new PlayerPosition { X = 0, Y = 0, Z = 0, Zone = "Stanton" });

        // Set sender position to 5000,0,0
        var senderPos = new PlayerPosition { X = 5000, Y = 0, Z = 0, Zone = "Stanton" };

        // Even with a repeater halfway (at 2500,0,0), it should return direct distance (5000) because repeaters are disabled
        var remotePositionsField = typeof(MainViewModel).GetField("_remotePositions", BindingFlags.NonPublic | BindingFlags.Instance);
        var remotePositions = (Dictionary<string, PlayerPosition>)remotePositionsField!.GetValue(vm)!;
        remotePositions["Repeater"] = new PlayerPosition { X = 2500, Y = 0, Z = 0, Zone = "Stanton" };

        var remoteRepeatersField = typeof(MainViewModel).GetField("_remoteRepeaters", BindingFlags.NonPublic | BindingFlags.Instance);
        var remoteRepeaters = (Dictionary<string, bool>)remoteRepeatersField!.GetValue(vm)!;
        remoteRepeaters["Repeater"] = true;

        double distance = vm.CalculateEffectiveRadioDistance("Sender", senderPos);
        Assert.Equal(5000.0, distance, 1);
    }

    [Fact]
    public async Task Dijkstra_ShouldReturnDifferentZoneValue_IfZonesDoNotMatch()
    {
        await using var vm = new MainViewModel();
        
        vm.Config.Config.EnableRadioRepeaters = true;

        var localPosField = typeof(MainViewModel).GetField("_lastSentPos", BindingFlags.NonPublic | BindingFlags.Instance);
        localPosField!.SetValue(vm, new PlayerPosition { X = 0, Y = 0, Z = 0, Zone = "Stanton" });

        var senderPos = new PlayerPosition { X = 100, Y = 0, Z = 0, Zone = "Pyro" };

        double distance = vm.CalculateEffectiveRadioDistance("Sender", senderPos);
        Assert.Equal(99999.0, distance);
    }

    [Fact]
    public async Task Dijkstra_ShouldFindMultiHopPath_AndReturnMaxHopDistance()
    {
        await using var vm = new MainViewModel();
        
        vm.Config.Config.EnableRadioRepeaters = true;

        // Local at 0, 0, 0
        var localPosField = typeof(MainViewModel).GetField("_lastSentPos", BindingFlags.NonPublic | BindingFlags.Instance);
        localPosField!.SetValue(vm, new PlayerPosition { X = 0, Y = 0, Z = 0, Zone = "Stanton" });

        // Sender at 10000, 0, 0 (direct distance is 10000m, beyond 8000m limit)
        var senderPos = new PlayerPosition { X = 10000, Y = 0, Z = 0, Zone = "Stanton" };

        // Two repeaters making a chain:
        // Sender (10000,0,0) -> RepA (6000,0,0) [hop: 4000m] -> RepB (3000,0,0) [hop: 3000m] -> Local (0,0,0) [hop: 3000m]
        // Max hop along path is 4000m.
        var remotePositionsField = typeof(MainViewModel).GetField("_remotePositions", BindingFlags.NonPublic | BindingFlags.Instance);
        var remotePositions = (Dictionary<string, PlayerPosition>)remotePositionsField!.GetValue(vm)!;
        remotePositions["RepA"] = new PlayerPosition { X = 6000, Y = 0, Z = 0, Zone = "Stanton" };
        remotePositions["RepB"] = new PlayerPosition { X = 3000, Y = 0, Z = 0, Zone = "Stanton" };

        var remoteRepeatersField = typeof(MainViewModel).GetField("_remoteRepeaters", BindingFlags.NonPublic | BindingFlags.Instance);
        var remoteRepeaters = (Dictionary<string, bool>)remoteRepeatersField!.GetValue(vm)!;
        remoteRepeaters["RepA"] = true;
        remoteRepeaters["RepB"] = true;

        double distance = vm.CalculateEffectiveRadioDistance("Sender", senderPos);
        Assert.Equal(5300.0, distance, 1);
    }

    [Fact]
    public async Task Dijkstra_ShouldApplyHopPenalty_ToMultiHopPaths()
    {
        await using var vm = new MainViewModel();
        
        vm.Config.Config.EnableRadioRepeaters = true;

        // Local at 0, 0, 0
        var localPosField = typeof(MainViewModel).GetField("_lastSentPos", BindingFlags.NonPublic | BindingFlags.Instance);
        localPosField!.SetValue(vm, new PlayerPosition { X = 0, Y = 0, Z = 0, Zone = "Stanton" });

        var remotePositionsField = typeof(MainViewModel).GetField("_remotePositions", BindingFlags.NonPublic | BindingFlags.Instance);
        var remotePositions = (Dictionary<string, PlayerPosition>)remotePositionsField!.GetValue(vm)!;

        var remoteRepeatersField = typeof(MainViewModel).GetField("_remoteRepeaters", BindingFlags.NonPublic | BindingFlags.Instance);
        var remoteRepeaters = (Dictionary<string, bool>)remoteRepeatersField!.GetValue(vm)!;

        // Case A: 1 Hop (Direct)
        // Sender at 4000, 0, 0. Direct distance is 4000m.
        var senderPos1 = new PlayerPosition { X = 4000, Y = 0, Z = 0, Zone = "Stanton" };
        double dist1 = vm.CalculateEffectiveRadioDistance("Sender", senderPos1);
        // Direct, so hops = 1. Penalty = 0.
        Assert.Equal(4000.0, dist1, 1);

        // Case B: 2 Hops (1 Repeater)
        // Sender at 6000, 0, 0.
        // Repeater at 3000, 0, 0.
        // Hops: Sender -> RepA (3000m) -> Local (3000m). Worst hop: 3000m.
        // Hops = 2. Penalty = 1 * 650.0 = 650.0m. Total = 3650.0m.
        var senderPos2 = new PlayerPosition { X = 6000, Y = 0, Z = 0, Zone = "Stanton" };
        remotePositions["RepA"] = new PlayerPosition { X = 3000, Y = 0, Z = 0, Zone = "Stanton" };
        remoteRepeaters["RepA"] = true;
        double dist2 = vm.CalculateEffectiveRadioDistance("Sender", senderPos2);
        Assert.Equal(3650.0, dist2, 1);

        // Case C: 3 Hops (2 Repeaters)
        // Sender at 9000, 0, 0.
        // RepA at 6000, 0, 0.
        // RepB at 3000, 0, 0.
        // Hops: Sender -> RepA (3000m) -> RepB (3000m) -> Local (3000m). Worst hop: 3000m.
        // Hops = 3. Penalty = 2 * 650.0 = 1300.0m. Total = 4300.0m.
        var senderPos3 = new PlayerPosition { X = 9000, Y = 0, Z = 0, Zone = "Stanton" };
        remotePositions["RepA"] = new PlayerPosition { X = 6000, Y = 0, Z = 0, Zone = "Stanton" };
        remotePositions["RepB"] = new PlayerPosition { X = 3000, Y = 0, Z = 0, Zone = "Stanton" };
        remoteRepeaters["RepB"] = true;
        double dist3 = vm.CalculateEffectiveRadioDistance("Sender", senderPos3);
        Assert.Equal(4300.0, dist3, 1);
    }

    [Fact]
    public async Task Dijkstra_ShouldFallbackToDirect_IfHopExceedsLimit()
    {
        await using var vm = new MainViewModel();
        
        vm.Config.Config.EnableRadioRepeaters = true;

        // Local at 0, 0, 0
        var localPosField = typeof(MainViewModel).GetField("_lastSentPos", BindingFlags.NonPublic | BindingFlags.Instance);
        localPosField!.SetValue(vm, new PlayerPosition { X = 0, Y = 0, Z = 0, Zone = "Stanton" });

        // Sender at 12000, 0, 0 (direct distance is 12000m)
        var senderPos = new PlayerPosition { X = 12000, Y = 0, Z = 0, Zone = "Stanton" };

        // A repeater at 3000, 0, 0
        // Sender (12000,0,0) -> RepA (3000,0,0) is 9000m (beyond 8000m hop limit)
        // So no valid path exists, it should fallback to direct distance (12000m)
        var remotePositionsField = typeof(MainViewModel).GetField("_remotePositions", BindingFlags.NonPublic | BindingFlags.Instance);
        var remotePositions = (Dictionary<string, PlayerPosition>)remotePositionsField!.GetValue(vm)!;
        remotePositions["RepA"] = new PlayerPosition { X = 3000, Y = 0, Z = 0, Zone = "Stanton" };

        var remoteRepeatersField = typeof(MainViewModel).GetField("_remoteRepeaters", BindingFlags.NonPublic | BindingFlags.Instance);
        var remoteRepeaters = (Dictionary<string, bool>)remoteRepeatersField!.GetValue(vm)!;
        remoteRepeaters["RepA"] = true;

        double distance = vm.CalculateEffectiveRadioDistance("Sender", senderPos);
        Assert.Equal(12000.0, distance, 1);
    }
}
