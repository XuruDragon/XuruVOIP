using System;
using System.Linq;
using System.Reflection;
using Xunit;
using NAudio.Wave;
using XuruVoipClient.Services;

namespace XuruVoipClient.Tests;

public class ChimeProfileTests
{
    [Fact]
    public void ChimeTypeChange_ShouldRegenerateChimesCorrectly()
    {
        // GIVEN
        var service = new AudioPlaybackService();

        // WHEN: Default is Military
        Assert.Equal("Military", service.PttChimeType);

        // Retrieve private float arrays KeyDownChime and KeyUpChime via reflection
        var kdField = typeof(AudioPlaybackService).GetField("KeyDownChime", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("KeyDownChime field not found");
        var kuField = typeof(AudioPlaybackService).GetField("KeyUpChime", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("KeyUpChime field not found");

        var milKd = (float[])kdField.GetValue(service)!;
        var milKu = (float[])kuField.GetValue(service)!;

        Assert.Equal(2400, milKd.Length); // 50ms @ 48kHz
        Assert.Equal(8640, milKu.Length); // 180ms @ 48kHz

        // WHEN: Change to Industrial
        service.PttChimeType = "Industrial";
        var indKd = (float[])kdField.GetValue(service)!;
        var indKu = (float[])kuField.GetValue(service)!;

        Assert.Equal(2880, indKd.Length); // 60ms @ 48kHz
        Assert.Equal(5760, indKu.Length); // 120ms @ 48kHz
        Assert.Contains(indKd, s => s != 0f);
        Assert.Contains(indKu, s => s != 0f);

        // WHEN: Change to Alien
        service.PttChimeType = "Alien";
        var alienKd = (float[])kdField.GetValue(service)!;
        var alienKu = (float[])kuField.GetValue(service)!;

        Assert.Equal(3840, alienKd.Length); // 80ms @ 48kHz
        Assert.Equal(4800, alienKu.Length); // 100ms @ 48kHz
        Assert.Contains(alienKd, s => s != 0f);
        Assert.Contains(alienKu, s => s != 0f);

        // WHEN: Change to Vintage
        service.PttChimeType = "Vintage";
        var vinKd = (float[])kdField.GetValue(service)!;
        var vinKu = (float[])kuField.GetValue(service)!;

        Assert.Equal(1920, vinKd.Length); // 40ms @ 48kHz
        Assert.Equal(3840, vinKu.Length); // 80ms @ 48kHz
        Assert.Contains(vinKd, s => s != 0f);
        Assert.Contains(vinKu, s => s != 0f);
    }

    [Fact]
    public void CustomChimes_ShouldLoadResampleAndDownmix()
    {
        // GIVEN
        string appDir = AppDomain.CurrentDomain.BaseDirectory;
        string resourcesDir = System.IO.Path.Combine(appDir, "Resources");
        System.IO.Directory.CreateDirectory(resourcesDir);

        string wavDownPath = System.IO.Path.Combine(resourcesDir, "radio_key_down.wav");
        string wavUpPath = System.IO.Path.Combine(resourcesDir, "radio_key_up.wav");

        // Write a 44100Hz stereo WAV file with a few samples to test downmixing and resampling
        var format = new WaveFormat(44100, 16, 2); // stereo, 16-bit, 44100Hz
        using (var writer = new WaveFileWriter(wavDownPath, format))
        {
            // Write 441 samples (10ms)
            for (int i = 0; i < 441; i++)
            {
                writer.WriteSample(0.5f);
                writer.WriteSample(0.5f);
            }
        }

        using (var writer = new WaveFileWriter(wavUpPath, format))
        {
            // Write 882 samples (20ms)
            for (int i = 0; i < 882; i++)
            {
                writer.WriteSample(0.3f);
                writer.WriteSample(0.3f);
            }
        }

        try
        {
            var service = new AudioPlaybackService();
            service.EnableCustomChimes = true; // triggers RegeneratePttChimes()

            // Retrieve fields
            var kdField = typeof(AudioPlaybackService).GetField("KeyDownChime", BindingFlags.NonPublic | BindingFlags.Instance)
                ?? throw new InvalidOperationException("KeyDownChime field not found");
            var kuField = typeof(AudioPlaybackService).GetField("KeyUpChime", BindingFlags.NonPublic | BindingFlags.Instance)
                ?? throw new InvalidOperationException("KeyUpChime field not found");

            var customKd = (float[])kdField.GetValue(service)!;
            var customKu = (float[])kuField.GetValue(service)!;

            // 10ms at 48000Hz = 480 samples. Allow a bit of leeway for resampler padding/flush.
            Assert.True(customKd.Length >= 470 && customKd.Length <= 500, $"Expected ~480 samples, got {customKd.Length}");
            // 20ms at 48000Hz = 960 samples.
            Assert.True(customKu.Length >= 950 && customKu.Length <= 980, $"Expected ~960 samples, got {customKu.Length}");

            // Verify they contain audio
            Assert.Contains(customKd, s => s != 0f);
            Assert.Contains(customKu, s => s != 0f);
        }
        finally
        {
            // Cleanup
            if (System.IO.File.Exists(wavDownPath)) System.IO.File.Exists(wavDownPath);
            try { System.IO.File.Delete(wavDownPath); } catch { }
            try { System.IO.File.Delete(wavUpPath); } catch { }
        }
    }
}
