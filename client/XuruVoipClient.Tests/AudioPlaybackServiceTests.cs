using System;
using System.Linq;
using System.Reflection;
using Xunit;
using XuruVoipClient.Services;

namespace XuruVoipClient.Tests;

public class AudioPlaybackServiceTests
{
    [Fact]
    public void GenerateKeyDownChime_ShouldCreateSinePitchSweep()
    {
        // GIVEN
        var method = typeof(AudioPlaybackService).GetMethod(
            "GenerateKeyDownChime",
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("GenerateKeyDownChime method not found");

        // WHEN
        float[] chime = (float[])method.Invoke(null, null)!;

        // THEN: Ensure length is 50ms at 48000Hz = 2400 samples
        Assert.NotNull(chime);
        Assert.Equal(2400, chime.Length);

        // Ensure there is actual signal generated (not all zeros)
        Assert.True(chime.Any(s => s != 0f));

        // Ensure samples are within quiet range (<= 15% gain as designed)
        foreach (var sample in chime)
        {
            Assert.InRange(sample, -0.16f, 0.16f);
        }
    }

    [Fact]
    public void GenerateKeyUpChime_ShouldCreateBandpassNoiseSquelch()
    {
        // GIVEN
        var method = typeof(AudioPlaybackService).GetMethod(
            "GenerateKeyUpChime",
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("GenerateKeyUpChime method not found");

        // WHEN
        float[] chime = (float[])method.Invoke(null, null)!;

        // THEN: Ensure length is 180ms at 48000Hz = 8640 samples
        Assert.NotNull(chime);
        Assert.Equal(8640, chime.Length);

        // Ensure there is noise generated
        Assert.True(chime.Any(s => s != 0f));

        // Ensure samples are within squelch tail gain limits (<= 25% gain)
        foreach (var sample in chime)
        {
            Assert.InRange(sample, -0.26f, 0.26f);
        }
    }

    [Fact]
    public void EnvironmentalAcousticFilter_ShouldApplyOcclusionAndReverb()
    {
        // GIVEN
        var filter = new EnvironmentalAcousticFilter();
        float[] buffer = new float[1000];
        for (int i = 0; i < buffer.Length; i++)
        {
            buffer[i] = 0.5f;
        }

        // WHEN: Under same zone
        filter.UpdateZoneInfo("ZoneA", "ZoneA");
        // Run multiple times to let smooth interpolation converge
        for (int i = 0; i < 50; i++)
        {
            filter.Process(buffer, buffer.Length);
        }
        // Signal should be close to original
        Assert.True(buffer[0] > 0.4f);

        // WHEN: Under different zones (Occlusion)
        filter.UpdateZoneInfo("ZoneA", "ZoneB");
        for (int i = 0; i < 100; i++)
        {
            filter.Process(buffer, buffer.Length);
        }
        // Occluded volume factor target is 0.65f, so output should be reduced
        Assert.True(buffer[0] < 0.45f);

        // WHEN: Listener is in a Cave (Reverb active)
        filter.UpdateZoneInfo("ZoneA", "Cave_System");
        for (int i = 0; i < 100; i++)
        {
            filter.Process(buffer, buffer.Length);
        }
        // Ensure samples are within safe clipping bounds
        foreach (var sample in buffer)
        {
            Assert.InRange(sample, -1.0f, 1.0f);
        }
    }
}
