using System;
using Xunit;
using XuruVoipClient.Services;
using NAudio.Wave;

namespace XuruVoipClient.Tests;

public class FftAnalysisTests
{
    [Fact]
    public void FftAnalysis_ComputeFft_ShouldThrowOnNull()
    {
        Assert.Throws<ArgumentNullException>(() => FftAnalysis.ComputeFft(null!, new float[64], new float[64]));
    }

    [Fact]
    public void FftAnalysis_ComputeFft_ShouldThrowOnInvalidSize()
    {
        Assert.Throws<ArgumentException>(() => FftAnalysis.ComputeFft(new float[63], new float[64], new float[64]));
    }

    [Fact]
    public void FftAnalysis_ComputeFft_ShouldDetectSineFrequency()
    {
        // GIVEN
        float[] input = new float[64];
        double sampleRate = 48000;
        // Let's create a sine wave at the frequency of bin 4: 
        // binFreq = bin * sampleRate / N
        // For N=64, sampleRate=48000, bin=4 -> 4 * 48000 / 64 = 3000 Hz.
        double targetFreq = 4.0 * sampleRate / 64.0;
        for (int i = 0; i < 64; i++)
        {
            input[i] = (float)Math.Sin(2.0 * Math.PI * targetFreq * i / sampleRate);
        }

        float[] realOut = new float[64];
        float[] imagOut = new float[64];

        // WHEN
        FftAnalysis.ComputeFft(input, realOut, imagOut);

        // THEN: Bin 4 should have a large magnitude compared to other bins
        double mag4 = Math.Sqrt(realOut[4] * realOut[4] + imagOut[4] * imagOut[4]);
        
        // Bin 0 (DC) should be close to 0
        double mag0 = Math.Sqrt(realOut[0] * realOut[0] + imagOut[0] * imagOut[0]);
        // Bin 10 should be close to 0
        double mag10 = Math.Sqrt(realOut[10] * realOut[10] + imagOut[10] * imagOut[10]);

        Assert.True(mag4 > 10.0, $"Expected bin 4 magnitude to be large, was {mag4}");
        Assert.True(mag0 < 1.0, $"Expected bin 0 magnitude to be small, was {mag0}");
        Assert.True(mag10 < 1.0, $"Expected bin 10 magnitude to be small, was {mag10}");
    }
}
