using System;
using Xunit;
using XuruVoipClient.Services;

namespace XuruVoipClient.Tests;

public class VoiceModulatorTests
{
    private float[] CreateSineWaveBuffer(int length, float freq = 1000f)
    {
        float[] buffer = new float[length];
        for (int i = 0; i < length; i++)
        {
            buffer[i] = (float)Math.Sin(2.0 * Math.PI * freq * i / 48000.0);
        }
        return buffer;
    }

    [Fact]
    public void ProcessCustom_ShouldModifyBuffer_WhenPitchShifting()
    {
        // GIVEN
        var modulator = new VoiceModulator();
        var buffer = CreateSineWaveBuffer(240);
        var original = (float[])buffer.Clone();

        // WHEN: Pitch factor is changed
        modulator.ProcessCustom(
            buffer: buffer,
            count: buffer.Length,
            pitchFactor: 1.5f,
            ringModFreq: 100f,
            ringModMix: 0f,
            flangerDepth: 0f,
            flangerRate: 0.5f,
            flangerFeedback: 0f,
            bitcrushEnabled: false,
            bitcrushBits: 16
        );

        // THEN
        Assert.NotEqual(original, buffer);
        foreach (var sample in buffer)
        {
            Assert.InRange(sample, -1.0f, 1.0f);
        }
    }

    [Fact]
    public void ProcessCustom_ShouldModifyBuffer_WhenRingModulationIsActive()
    {
        // GIVEN
        var modulator = new VoiceModulator();
        var buffer = CreateSineWaveBuffer(240);
        var original = (float[])buffer.Clone();

        // WHEN: Ring modulator mix is 50%
        modulator.ProcessCustom(
            buffer: buffer,
            count: buffer.Length,
            pitchFactor: 1.0f,
            ringModFreq: 250f,
            ringModMix: 0.5f,
            flangerDepth: 0f,
            flangerRate: 0.5f,
            flangerFeedback: 0f,
            bitcrushEnabled: false,
            bitcrushBits: 16
        );

        // THEN
        Assert.NotEqual(original, buffer);
        foreach (var sample in buffer)
        {
            Assert.InRange(sample, -1.0f, 1.0f);
        }
    }

    [Fact]
    public void ProcessCustom_ShouldModifyBuffer_WhenFlangerIsActive()
    {
        // GIVEN
        var modulator = new VoiceModulator();
        var buffer = CreateSineWaveBuffer(240);
        var original = (float[])buffer.Clone();

        // WHEN: Flanger depth is 60%
        modulator.ProcessCustom(
            buffer: buffer,
            count: buffer.Length,
            pitchFactor: 1.0f,
            ringModFreq: 100f,
            ringModMix: 0f,
            flangerDepth: 0.6f,
            flangerRate: 1.5f,
            flangerFeedback: 0.3f,
            bitcrushEnabled: false,
            bitcrushBits: 16
        );

        // THEN
        Assert.NotEqual(original, buffer);
    }

    [Fact]
    public void ProcessCustom_ShouldModifyBuffer_WhenBitcrushIsActive()
    {
        // GIVEN
        var modulator = new VoiceModulator();
        var buffer = CreateSineWaveBuffer(240);
        var original = (float[])buffer.Clone();

        // WHEN: Bitcrusher is enabled at 4 bits
        modulator.ProcessCustom(
            buffer: buffer,
            count: buffer.Length,
            pitchFactor: 1.0f,
            ringModFreq: 100f,
            ringModMix: 0f,
            flangerDepth: 0f,
            flangerRate: 0.5f,
            flangerFeedback: 0f,
            bitcrushEnabled: true,
            bitcrushBits: 4
        );

        // THEN: Output values should be discretized (only a few discrete steps)
        Assert.NotEqual(original, buffer);
        foreach (var sample in buffer)
        {
            // Verify step size: 4 bits means 2^(4-1) = 8 steps. 1/8 = 0.125.
            // Samples should be multiples of 0.125f (within float precision).
            float step = sample * 8.0f;
            float rounded = MathF.Round(step);
            Assert.Equal(rounded, step, 3);
        }
    }
}
