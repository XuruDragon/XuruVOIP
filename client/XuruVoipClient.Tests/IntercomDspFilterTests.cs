using System;
using Xunit;
using XuruVoipClient.Services;

namespace XuruVoipClient.Tests;

public class IntercomDspFilterTests
{
    [Fact]
    public void Process_NormalState_ShouldNotModifyBuffer()
    {
        // GIVEN
        var filter = new IntercomDspFilter();
        float[] buffer = { 0.1f, 0.2f, 0.3f, 0.4f, 0.5f };
        float[] original = (float[])buffer.Clone();

        // WHEN
        filter.Process(buffer, buffer.Length, IntercomDegradationState.Normal);

        // THEN: Audio buffer should remain identical
        Assert.Equal(original, buffer);
    }

    [Fact]
    public void Process_ShieldHitState_ShouldModifyBufferWhenEnabled()
    {
        // GIVEN
        var filter = new IntercomDspFilter { ShieldHitsEnabled = true };
        float[] buffer = new float[100];
        for (int i = 0; i < buffer.Length; i++) buffer[i] = 0.0f; // Silence

        // WHEN
        filter.Process(buffer, buffer.Length, IntercomDegradationState.ShieldHit);

        // THEN: Buffer should now contain noise / crackles
        Assert.Contains(buffer, x => x != 0.0f);
    }

    [Fact]
    public void Process_ShieldHitState_ShouldNotModifyBufferWhenDisabled()
    {
        // GIVEN
        var filter = new IntercomDspFilter { ShieldHitsEnabled = false };
        float[] buffer = new float[100];
        float[] original = (float[])buffer.Clone();

        // WHEN
        filter.Process(buffer, buffer.Length, IntercomDegradationState.ShieldHit);

        // THEN
        Assert.Equal(original, buffer);
    }

    [Fact]
    public void Process_CriticalPowerState_ShouldModifyBufferWhenEnabled()
    {
        // GIVEN
        var filter = new IntercomDspFilter { CriticalPowerEnabled = true };
        float[] buffer = new float[200];
        for (int i = 0; i < buffer.Length; i++)
        {
            buffer[i] = (float)Math.Sin(2.0 * Math.PI * 1000.0 * i / 48000.0);
        }
        float[] original = (float[])buffer.Clone();

        // WHEN
        filter.Process(buffer, buffer.Length, IntercomDegradationState.CriticalPower);

        // THEN: Output should differ (hum + saturation + resample)
        Assert.NotEqual(original, buffer);
        foreach (var sample in buffer)
        {
            Assert.InRange(sample, -1.0f, 1.0f);
            Assert.False(float.IsNaN(sample));
        }
    }

    [Fact]
    public void Process_CriticalPowerState_ShouldNotModifyBufferWhenDisabled()
    {
        // GIVEN
        var filter = new IntercomDspFilter { CriticalPowerEnabled = false };
        float[] buffer = new float[100];
        float[] original = (float[])buffer.Clone();

        // WHEN
        filter.Process(buffer, buffer.Length, IntercomDegradationState.CriticalPower);

        // THEN
        Assert.Equal(original, buffer);
    }

    [Fact]
    public void Process_QuantumTravelState_ShouldModifyBufferWhenEnabled()
    {
        // GIVEN
        var filter = new IntercomDspFilter { QuantumTravelEnabled = true };
        float[] buffer = new float[500];
        for (int i = 0; i < buffer.Length; i++)
        {
            buffer[i] = (float)Math.Sin(2.0 * Math.PI * 1000.0 * i / 48000.0);
        }
        float[] original = (float[])buffer.Clone();

        // WHEN
        filter.Process(buffer, buffer.Length, IntercomDegradationState.QuantumTravel);

        // THEN: Phaser and whine should modify the signal
        Assert.NotEqual(original, buffer);
        foreach (var sample in buffer)
        {
            Assert.InRange(sample, -1.0f, 1.0f);
            Assert.False(float.IsNaN(sample));
        }
    }

    [Fact]
    public void Process_QuantumTravelState_ShouldNotModifyBufferWhenDisabled()
    {
        // GIVEN
        var filter = new IntercomDspFilter { QuantumTravelEnabled = false };
        float[] buffer = new float[100];
        float[] original = (float[])buffer.Clone();

        // WHEN
        filter.Process(buffer, buffer.Length, IntercomDegradationState.QuantumTravel);

        // THEN
        Assert.Equal(original, buffer);
    }
}
