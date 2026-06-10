using System;

namespace XuruVoipClient.Services;

public class MegaphoneDspFilter
{
    private const int SampleRate = 48000;
    private readonly BiquadFilter hpIn = new();
    private readonly BiquadFilter lpIn = new();

    private readonly float[] reverbBuf = new float[4800];
    private int reverbIdx = 0;

    public MegaphoneDspFilter()
    {
        hpIn.SetHpCoefficients(500.0, SampleRate);
        lpIn.SetLpCoefficients(2000.0, SampleRate);
    }

    public void Process(float[] buffer, int count)
    {
        float drive = 3.5f;
        for (int i = 0; i < count; i++)
        {
            float sample = buffer[i];

            // 1. Bandpass filter
            sample = hpIn.Process(sample);
            sample = lpIn.Process(sample);

            // 2. Saturation / Distortion
            sample = (float)(Math.Tanh(sample * drive) * 0.85);

            // 3. Reverb / Echo (50ms delay = 2400 samples, 40% feedback, 25% wet mix)
            int readIdx = (reverbIdx - 2400 + 4800) % 4800;
            float delayed = reverbBuf[readIdx];
            float wetSample = sample + 0.25f * delayed;
            reverbBuf[reverbIdx] = sample + 0.40f * delayed;
            reverbIdx = (reverbIdx + 1) % 4800;

            buffer[i] = Math.Clamp(wetSample, -1.0f, 1.0f);
        }
    }
}
