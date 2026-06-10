using Xunit;
using System;
using System.Reflection;
using System.Threading.Tasks;
using XuruVoipClient.Services;
using XuruVoipClient.ViewModels;
using XuruVoipClient.Models;
using NAudio.Wave;

namespace XuruVoipClient.Tests;

public class AudioCaptureServiceTests
{
    static AudioCaptureServiceTests()
    {
        UiTests.InitializeWpfApplication();
    }

    [Fact]
    public void AudioCaptureService_MuteStates_ShouldPreventTransmission()
    {
        // GIVEN
        using var capture = new AudioCaptureService();
        
        // Use reflection to initialize the Concentus encoder so ProcessFrame doesn't return early
        var encoder = Concentus.OpusCodecFactory.CreateEncoder(48000, 1, Concentus.Enums.OpusApplication.OPUS_APPLICATION_VOIP);
        var encoderField = typeof(AudioCaptureService).GetField("_encoder", BindingFlags.NonPublic | BindingFlags.Instance);
        encoderField!.SetValue(capture, encoder);

        // Track frame callbacks
        bool frameReadyCalled = false;
        capture.EncodedFrameReady += (frame, type) => {
            frameReadyCalled = true;
        };

        var processFrameMethod = typeof(AudioCaptureService).GetMethod("ProcessFrame", BindingFlags.NonPublic | BindingFlags.Instance);
        
        // Case 1: Proximity Muted and txType = 0x00 (Proximity)
        capture.ProximityMuted = true;
        capture.Mode = AudioMode.PTT;
        capture.SetPttState(true, 0x00);
        
        var pcm = new short[960];
        processFrameMethod!.Invoke(capture, new object[] { pcm });
        Assert.False(frameReadyCalled);

        // Case 2: Radio Muted and txType = 0x01 (Radio)
        capture.ProximityMuted = false;
        capture.RadioMuted = true;
        capture.SetPttState(true, 0x01);
        
        processFrameMethod!.Invoke(capture, new object[] { pcm });
        Assert.False(frameReadyCalled);

        // Case 3: Profile Muted and txType = 0x02 (Profile)
        capture.RadioMuted = false;
        capture.ProfileMuted = true;
        capture.SetPttState(true, 0x02);
        
        processFrameMethod!.Invoke(capture, new object[] { pcm });
        Assert.False(frameReadyCalled);
    }

    [StaFact]
    public async Task AudioCaptureService_VisorDown_ShouldApplyBreathingAndHumEffects()
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

        // Enable helmet and helmet modulator
        vm.IsHelmetOn = true;
        vm.Config.Config.EnableHelmetModulator = true;
        vm.Config.Config.EnableVoiceChanger = false; // Disable voice changer to isolate helmet mod
        capture.Mode = AudioMode.PTT;
        capture.SetPttState(true, 0x00);

        var processFrameMethod = typeof(AudioCaptureService).GetMethod("ProcessFrame", BindingFlags.NonPublic | BindingFlags.Instance);

        // Create a silent (all zeros) PCM frame
        var pcm = new short[960];
        
        // WHEN
        processFrameMethod!.Invoke(capture, new object[] { pcm });

        // THEN: The breathing noise and/or suit vent hum should modify the silent PCM data to contain non-zero values
        bool containsNonZero = false;
        for (int i = 0; i < pcm.Length; i++)
        {
            if (pcm[i] != 0)
            {
                containsNonZero = true;
                break;
            }
        }
        Assert.True(containsNonZero);

        // Reset and test with Helmet OFF (should remain silent)
        vm.IsHelmetOn = false;
        var silentPcm = new short[960];
        processFrameMethod!.Invoke(capture, new object[] { silentPcm });

        bool remainsSilent = true;
        for (int i = 0; i < silentPcm.Length; i++)
        {
            if (silentPcm[i] != 0)
            {
                remainsSilent = false;
                break;
            }
        }
        Assert.True(remainsSilent);
    }

    [Fact]
    public void AudioCaptureService_GainProcessing_ShouldAmplifyIncomingSamples()
    {
        // GIVEN
        using var capture = new AudioCaptureService();
        
        // Use reflection to mark the service as recording
        var recordingField = typeof(AudioCaptureService).GetField("_isRecording", BindingFlags.NonPublic | BindingFlags.Instance);
        recordingField!.SetValue(capture, true);

        // Construct 100ms worth of PCM data at 48kHz (4800 samples)
        // Set values to 1000
        int sampleCount = 960;
        var pcmBytes = new byte[sampleCount * 2];
        for (int i = 0; i < sampleCount; i++)
        {
            short sample = 1000;
            byte[] bytes = BitConverter.GetBytes(sample);
            pcmBytes[i * 2] = bytes[0];
            pcmBytes[i * 2 + 1] = bytes[1];
        }

        var onDataAvailableMethod = typeof(AudioCaptureService).GetMethod("OnDataAvailable", BindingFlags.NonPublic | BindingFlags.Instance);
        var args = new WaveInEventArgs(pcmBytes, pcmBytes.Length);

        // WHEN: Invoke with 6.02dB gain (which is a linear scale of 2.0x)
        double gainDb = 6.0205999132796239; // 20 * log10(2)
        double gainLinear = Math.Pow(10.0, gainDb / 20.0);
        
        onDataAvailableMethod!.Invoke(capture, new object[] { args, gainLinear });

        // THEN: Verify InputLevel calculation is correct
        // Linear gain = 2.0. Resulting samples should be 2000.
        // RMS of all samples equal to 2000 is 2000.
        // InputLevel = 2000 / 32768f = 0.061035f
        Assert.Equal(2000f / 32768f, capture.InputLevel, 4);
    }

    [StaFact]
    public async Task AudioCaptureService_ExertionDistortion_ShouldApplyTremoloAndPitchShifting()
    {
        // GIVEN
        await using var vm = new MainViewModel();
        
        var viewModelProp = typeof(App).GetProperty("ViewModel", BindingFlags.Public | BindingFlags.Static);
        viewModelProp!.SetValue(null, vm);

        using var capture = new AudioCaptureService();
        var encoder = Concentus.OpusCodecFactory.CreateEncoder(48000, 1, Concentus.Enums.OpusApplication.OPUS_APPLICATION_VOIP);
        var encoderField = typeof(AudioCaptureService).GetField("_encoder", BindingFlags.NonPublic | BindingFlags.Instance);
        encoderField!.SetValue(capture, encoder);

        // Enable exertion distortion and set mock values
        vm.Config.Config.EnableExertionDistortion = true;
        vm.GForce = 0.8;
        vm.Exertion = 0.5;
        capture.Mode = AudioMode.PTT;
        capture.SetPttState(true, 0x00);

        var processFrameMethod = typeof(AudioCaptureService).GetMethod("ProcessFrame", BindingFlags.NonPublic | BindingFlags.Instance);

        // Create a non-silent PCM frame to test distortion on voice
        var pcm = new short[960];
        for (int i = 0; i < pcm.Length; i++)
        {
            pcm[i] = (short)(10000 * Math.Sin(2 * Math.PI * 1000.0 * i / 48000.0)); // 1kHz sine wave
        }
        
        var originalPcm = new short[960];
        Array.Copy(pcm, originalPcm, pcm.Length);

        // WHEN
        processFrameMethod!.Invoke(capture, new object[] { pcm });

        // THEN: Audio should be modified
        bool isModified = false;
        for (int i = 0; i < pcm.Length; i++)
        {
            if (pcm[i] != originalPcm[i])
            {
                isModified = true;
                break;
            }
        }
        Assert.True(isModified);
    }
}
