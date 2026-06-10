using Xunit;
using System.Windows.Input;
using System.Threading.Tasks;
using XuruVoipClient.ViewModels;
using XuruVoipClient.Models;

namespace XuruVoipClient.Tests;

public class HotkeyTests
{
    static HotkeyTests()
    {
        UiTests.InitializeWpfApplication();
    }

    [StaFact]
    public async Task Hotkey_ToggleHelmet_ShouldToggleHelmetState()
    {
        // GIVEN
        await using var vm = new MainViewModel();
        vm.Config.Config.PttProximityKey = "CapsLock";
        vm.Config.Config.PttRadioKey = "NumPad1";
        vm.Config.Config.PttProfileKey = "NumPad2";
        vm.Config.Config.HelmetToggleKey = "H";
        vm.Config.Config.RadioCycleKey = "NumPad3";
        vm.Config.Config.MuteProximityKey = "M";
        vm.Config.Config.MuteRadioKey = "OemComma";
        vm.Config.Config.MuteProfileKey = "OemPeriod";
        var initialState = vm.IsHelmetOn;

        // WHEN (Default HelmetToggleKey is "H")
        vm.KeyHook.SimulateKeyEvent(Key.H, true);

        // THEN
        Assert.NotEqual(initialState, vm.IsHelmetOn);

        // WHEN (Press again)
        vm.KeyHook.SimulateKeyEvent(Key.H, true);

        // THEN
        Assert.Equal(initialState, vm.IsHelmetOn);
    }

    [StaFact]
    public async Task Hotkey_MuteMicProximity_ShouldMuteMicrophone()
    {
        // GIVEN
        await using var vm = new MainViewModel();
        vm.Config.Config.MuteProximityKey = "M";
        
        // WHEN (Default MuteProximityKey is "M")
        vm.KeyHook.SimulateKeyEvent(Key.M, true);

        // THEN
        Assert.True(vm.MicProximityMuted);
        Assert.Contains("MUTED", vm.StatusMessage);

        // WHEN (Press again)
        vm.KeyHook.SimulateKeyEvent(Key.M, true);

        // THEN
        Assert.False(vm.MicProximityMuted);
        Assert.Contains("UNMUTED", vm.StatusMessage);
    }

    [StaFact]
    public async Task Hotkey_MuteAudioProximity_ShouldMutePlayback()
    {
        // GIVEN
        await using var vm = new MainViewModel();
        vm.Config.Config.MuteAudioProximityKey = "K";
        Assert.False(vm.AudioProximityMuted);
        
        // WHEN
        vm.KeyHook.SimulateKeyEvent(Key.K, true);

        // THEN
        Assert.True(vm.AudioProximityMuted);
        Assert.Contains("MUTED", vm.StatusMessage);

        // WHEN (Press again)
        vm.KeyHook.SimulateKeyEvent(Key.K, true);

        // THEN
        Assert.False(vm.AudioProximityMuted);
        Assert.Contains("UNMUTED", vm.StatusMessage);
    }

    [StaFact]
    public async Task Hotkey_MuteAudioRadio_ShouldMutePlayback()
    {
        // GIVEN
        await using var vm = new MainViewModel();
        vm.Config.Config.MuteAudioRadioKey = "L";
        Assert.False(vm.AudioRadioMuted);
        
        // WHEN
        vm.KeyHook.SimulateKeyEvent(Key.L, true);

        // THEN
        Assert.True(vm.AudioRadioMuted);
        Assert.Contains("MUTED", vm.StatusMessage);

        // WHEN (Press again)
        vm.KeyHook.SimulateKeyEvent(Key.L, true);

        // THEN
        Assert.False(vm.AudioRadioMuted);
        Assert.Contains("UNMUTED", vm.StatusMessage);
    }

    [StaFact]
    public async Task Hotkey_MuteAudioProfile_ShouldMutePlayback()
    {
        // GIVEN
        await using var vm = new MainViewModel();
        vm.Config.Config.MuteAudioProfileKey = "U";
        Assert.False(vm.AudioProfileMuted);
        
        // WHEN
        vm.KeyHook.SimulateKeyEvent(Key.U, true);

        // THEN
        Assert.True(vm.AudioProfileMuted);
        Assert.Contains("MUTED", vm.StatusMessage);

        // WHEN (Press again)
        vm.KeyHook.SimulateKeyEvent(Key.U, true);

        // THEN
        Assert.False(vm.AudioProfileMuted);
        Assert.Contains("UNMUTED", vm.StatusMessage);
    }

    [StaFact]
    public async Task MicModeText_ShouldChangeDynamically()
    {
        // GIVEN
        await using var vm = new MainViewModel();
        vm.Config.Config.AudioMode = AudioMode.PTT;
        vm.Config.Config.PttProximityKey = "Capital";
        vm.Config.Config.PttRadioKey = "NumPad1";
        vm.Config.Config.PttProfileKey = "NumPad2";

        // Default
        Assert.Contains("Proximity PTT (Off)", vm.MicModeText);

        // Hold Proximity PTT
        vm.KeyHook.SimulateKeyEvent(Key.Capital, true);
        Assert.Contains("Proximity PTT (On)", vm.MicModeText);

        // Release Proximity PTT
        vm.KeyHook.SimulateKeyEvent(Key.Capital, false);
        Assert.Contains("Proximity PTT (Off)", vm.MicModeText);

        // Hold Radio PTT
        vm.KeyHook.SimulateKeyEvent(Key.NumPad1, true);
        Assert.Contains("Radio Channel PTT (On)", vm.MicModeText);
        vm.KeyHook.SimulateKeyEvent(Key.NumPad1, false);

        // Hold Profile PTT
        vm.KeyHook.SimulateKeyEvent(Key.NumPad2, true);
        Assert.Contains("Profile PTT (On)", vm.MicModeText);
        vm.KeyHook.SimulateKeyEvent(Key.NumPad2, false);

        // VAD mode
        vm.Config.Config.AudioMode = AudioMode.VAD;
        Assert.Contains("Proximity VAD (Off)", vm.MicModeText);

        vm.IsTalking = true;
        Assert.Contains("Proximity VAD (On)", vm.MicModeText);

        // Mute in VAD mode
        vm.MicProximityMuted = true;
        Assert.Contains("Proximity VAD (Muted)", vm.MicModeText);
    }
}
