using NAudio.Wave;
using System;
using Xunit;
using XuruVoipClient.Services;

namespace XuruVoipClient.Tests;

public class HrtfBinauralTests
{
    private class MonoTestProvider : ISampleProvider
    {
        private readonly float[] _samples;
        private int _position;

        public MonoTestProvider(float[] samples)
        {
            _samples = samples;
            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(48000, 1);
        }

        public WaveFormat WaveFormat { get; }

        public int Read(float[] buffer, int offset, int count)
        {
            int available = _samples.Length - _position;
            int toRead = Math.Min(available, count);
            if (toRead <= 0) return 0;

            Array.Copy(_samples, _position, buffer, offset, toRead);
            _position += toRead;
            return toRead;
        }
    }

    private class StereoTestProvider : ISampleProvider
    {
        public WaveFormat WaveFormat { get; } = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);

        public int Read(float[] buffer, int offset, int count)
        {
            return 0;
        }
    }

    [Fact]
    public void HrtfBinauralSampleProvider_Constructor_ShouldSetStereoFormat()
    {
        // GIVEN
        var monoSource = new MonoTestProvider(new float[100]);

        // WHEN
        var provider = new HrtfBinauralSampleProvider(monoSource);

        // THEN
        Assert.Equal(2, provider.WaveFormat.Channels);
        Assert.Equal(48000, provider.WaveFormat.SampleRate);
    }

    [Fact]
    public void HrtfBinauralSampleProvider_Constructor_ShouldThrowOnStereoSource()
    {
        // GIVEN
        var stereoSource = new StereoTestProvider();

        // WHEN & THEN
        Assert.Throws<ArgumentException>(() =>
        {
            var provider = new HrtfBinauralSampleProvider(stereoSource);
        });
    }

    [Fact]
    public void HrtfBinauralSampleProvider_Read_Disabled_ShouldApplyEqualPowerPanning()
    {
        // GIVEN
        float[] monoSamples = new float[100];
        for (int i = 0; i < monoSamples.Length; i++) monoSamples[i] = 1.0f; // constant 1.0
        var monoSource = new MonoTestProvider(monoSamples);
        var provider = new HrtfBinauralSampleProvider(monoSource)
        {
            Pan = -1.0f, // Extreme Left
            EnableHrtf = false
        };
        float[] stereoBuffer = new float[200];

        // WHEN
        int read = provider.Read(stereoBuffer, 0, stereoBuffer.Length);

        // THEN: For extreme left pan, Right channel should be 0, Left channel should be 1
        Assert.Equal(200, read);
        // Left channel: even indices, Right channel: odd indices
        for (int i = 0; i < 100; i++)
        {
            Assert.True(stereoBuffer[i * 2] > 0.99f);
            Assert.True(Math.Abs(stereoBuffer[i * 2 + 1]) < 0.01f);
        }
    }

    [Fact]
    public void HrtfBinauralSampleProvider_Read_Enabled_ShouldApplyItdAndIld()
    {
        // GIVEN
        float[] monoSamples = new float[100];
        // Impulse signal
        monoSamples[0] = 1.0f;
        var monoSource = new MonoTestProvider(monoSamples);
        var provider = new HrtfBinauralSampleProvider(monoSource)
        {
            Pan = 1.0f, // Extreme Right
            EnableHrtf = true
        };
        float[] stereoBuffer = new float[200];

        // WHEN
        int read = provider.Read(stereoBuffer, 0, stereoBuffer.Length);

        // THEN: 
        // 1. Extreme Right means Right channel is closer, Left is delayed.
        // 2. Left channel (index 0, 2, 4...) should be delayed. Since the delay is ~24 samples,
        //    the left channel should remain 0 for the first few samples.
        // 3. Right channel (index 1, 3, 5...) gets the signal earlier (index 1 is sample at t=0).
        Assert.Equal(200, read);
        
        // Right channel should have signal early on
        Assert.True(Math.Abs(stereoBuffer[1]) > 0.0f);

        // Left channel should be 0 at the very start due to delay buffer
        Assert.Equal(0.0f, stereoBuffer[0]);
        Assert.Equal(0.0f, stereoBuffer[2]);
        Assert.Equal(0.0f, stereoBuffer[4]);
    }
}
