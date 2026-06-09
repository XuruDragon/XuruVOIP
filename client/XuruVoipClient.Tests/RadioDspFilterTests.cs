using System;
using Xunit;
using XuruVoipClient.Services;

namespace XuruVoipClient.Tests;

public class RadioDspFilterTests
{
    [Fact]
    public void BiquadFilter_HighPass_ShouldProcessSignal()
    {
        // GIVEN
        var filter = new BiquadFilter();
        filter.SetHpCoefficients(1000, 48000);

        // WHEN
        float output = filter.Process(1.0f);

        // THEN: Output should be processed and not result in NaN or Infinity
        Assert.False(float.IsNaN(output));
        Assert.False(float.IsInfinity(output));
    }

    [Fact]
    public void BiquadFilter_LowPass_ShouldProcessSignal()
    {
        // GIVEN
        var filter = new BiquadFilter();
        filter.SetLpCoefficients(1000, 48000);

        // WHEN
        float output = filter.Process(1.0f);

        // THEN
        Assert.False(float.IsNaN(output));
        Assert.False(float.IsInfinity(output));
    }

    [Fact]
    public void RadioDspFilter_Process_ShouldModifyAudioBuffer()
    {
        // GIVEN
        var filter = new RadioDspFilter();
        float[] buffer = new float[100];
        for (int i = 0; i < buffer.Length; i++)
        {
            buffer[i] = (float)Math.Sin(2.0 * Math.PI * 440.0 * i / 48000.0);
        }
        float[] original = (float[])buffer.Clone();

        // WHEN
        filter.Process(buffer, buffer.Length);

        // THEN
        Assert.NotEqual(original, buffer);
        foreach (var sample in buffer)
        {
            Assert.InRange(sample, -1.0f, 1.0f);
        }
    }

    [Fact]
    public void RadioDspFilter_WithDegradation_ShouldApplyStrongerEffects()
    {
        // GIVEN
        var filterClean = new RadioDspFilter { DegradationFactor = 0.0 };
        var filterNoisy = new RadioDspFilter { DegradationFactor = 1.0 };

        float[] bufferClean = new float[240];
        float[] bufferNoisy = new float[240];

        for (int i = 0; i < 240; i++)
        {
            float val = (float)Math.Sin(2.0 * Math.PI * 1000.0 * i / 48000.0);
            bufferClean[i] = val;
            bufferNoisy[i] = val;
        }

        // WHEN
        filterClean.Process(bufferClean, bufferClean.Length);
        filterNoisy.Process(bufferNoisy, bufferNoisy.Length);

        // THEN: Clean and noisy outputs should differ due to dynamic noise mixing, Saturation and EQ
        Assert.NotEqual(bufferClean, bufferNoisy);
    }
}
