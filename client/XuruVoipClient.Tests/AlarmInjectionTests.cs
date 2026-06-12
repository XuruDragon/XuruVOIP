using Xunit;
using System;
using System.Reflection;
using System.Threading.Tasks;
using XuruVoipClient.Services;
using XuruVoipClient.ViewModels;
using XuruVoipClient.Models;

namespace XuruVoipClient.Tests;

public class AlarmInjectionTests
{
    [StaFact]
    public async Task AudioCaptureService_AlarmInjection_ShouldInjectSirenAndBeeps()
    {
        // GIVEN
        await using var vm = new MainViewModel();
        
        // Set App.ViewModel using reflection so the static property is set for the capture service
        var viewModelProp = typeof(App).GetProperty("ViewModel", BindingFlags.Public | BindingFlags.Static);
        viewModelProp!.SetValue(null, vm);

        using var capture = new AudioCaptureService();
        var encoder = Concentus.OpusCodecFactory.CreateEncoder(48000, 1, Concentus.Enums.OpusApplication.OPUS_APPLICATION_VOIP);
        var encoderField = typeof(AudioCaptureService).GetField("_encoder", BindingFlags.NonPublic | BindingFlags.Instance);
        encoderField!.SetValue(capture, encoder);

        vm.Config.Config.EnableAlarmInjection = true;
        capture.Mode = AudioMode.PTT;
        capture.SetPttState(true, 0x00);

        var processFrameMethod = typeof(AudioCaptureService).GetMethod("ProcessFrame", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("ProcessFrame method not found");

        // Create a silent (all zeros) PCM frame
        var pcm = new short[960];

        // 1. Test Normal state (no alarm injected)
        vm.IntercomState = IntercomDegradationState.Normal;
        processFrameMethod.Invoke(capture, new object[] { pcm });
        Assert.All(pcm, sample => Assert.Equal(0, sample));

        // 2. Test ShieldHit state (sweeping siren alarm)
        vm.IntercomState = IntercomDegradationState.ShieldHit;
        processFrameMethod.Invoke(capture, new object[] { pcm });
        // The siren should modify the silent PCM data
        Assert.Contains(pcm, sample => sample != 0);

        // 3. Reset to silent and test CriticalPower state (double beeps)
        Array.Clear(pcm, 0, pcm.Length);
        vm.IntercomState = IntercomDegradationState.CriticalPower;
        processFrameMethod.Invoke(capture, new object[] { pcm });
        // The beeps should modify the silent PCM data
        Assert.Contains(pcm, sample => sample != 0);

        // 4. Test Alarm disabled (should remain silent even if in warning state)
        Array.Clear(pcm, 0, pcm.Length);
        vm.Config.Config.EnableAlarmInjection = false;
        vm.IntercomState = IntercomDegradationState.ShieldHit;
        processFrameMethod.Invoke(capture, new object[] { pcm });
        Assert.All(pcm, sample => Assert.Equal(0, sample));
    }
}
