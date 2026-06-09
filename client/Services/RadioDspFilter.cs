using System;

namespace XuruVoipClient.Services;

public class BiquadFilter
{
    private double b0, b1, b2, a1, a2;
    private double x1, x2, y1, y2;

    public void SetHpCoefficients(double fc, double sr)
    {
        double w0 = 2 * Math.PI * fc / sr;
        double alpha = Math.Sin(w0) / (2 * 0.707);
        double cos_w0 = Math.Cos(w0);
        double a0 = 1 + alpha;
        b0 = (1 + cos_w0) / 2 / a0;
        b1 = -(1 + cos_w0) / a0;
        b2 = (1 + cos_w0) / 2 / a0;
        a1 = -2 * cos_w0 / a0;
        a2 = (1 - alpha) / a0;
    }

    public void SetLpCoefficients(double fc, double sr)
    {
        double w0 = 2 * Math.PI * fc / sr;
        double alpha = Math.Sin(w0) / (2 * 0.707);
        double cos_w0 = Math.Cos(w0);
        double a0 = 1 + alpha;
        b0 = (1 - cos_w0) / 2 / a0;
        b1 = (1 - cos_w0) / a0;
        b2 = (1 - cos_w0) / 2 / a0;
        a1 = -2 * cos_w0 / a0;
        a2 = (1 - alpha) / a0;
    }

    public float Process(float sample)
    {
        double x0 = sample;
        double y0 = b0 * x0 + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2;
        x2 = x1;
        x1 = x0;
        y2 = y1;
        y1 = y0;
        return (float)y0;
    }
}

public class RadioDspFilter
{
    private const int SampleRate = 48000;
    private readonly BiquadFilter hpIn = new();
    private readonly BiquadFilter lpIn = new();
    private readonly BiquadFilter hpOut = new();
    private readonly BiquadFilter lpOut = new();

    private int ringPhase = 0;

    private readonly float[] reverbBuf = new float[1440];
    private int reverbIdx = 0;

    public RadioDspFilter()
    {
        hpIn.SetHpCoefficients(350, SampleRate);
        lpIn.SetLpCoefficients(5000, SampleRate);
        hpOut.SetHpCoefficients(320, SampleRate);
        lpOut.SetLpCoefficients(5500, SampleRate);
    }

    public void Process(float[] buffer, int count)
    {
        for (int i = 0; i < count; i++)
        {
            float sample = buffer[i];

            // 1. Passe-bande IN
            sample = hpIn.Process(sample);
            sample = lpIn.Process(sample);

            // 2. Ring Modulator
            double carrier = Math.Sin(2.0 * Math.PI * 2700.0 * ringPhase / SampleRate);
            float modulated = (float)(sample * carrier);
            sample = (float)((1.0 - 0.17) * sample + 0.17 * modulated);
            ringPhase = (ringPhase + 1) % SampleRate;

            // 3. Saturation (drive 2.0, soft-clip tanh)
            sample = (float)(Math.Tanh(sample * 2.0) * 0.85);

            // 4. Passe-bande OUT
            sample = hpOut.Process(sample);
            sample = lpOut.Process(sample);

            // 5. Reverb/Echo (15ms delay = 720 samples, 15% wet mix)
            int readIdx = (reverbIdx - 720 + 1440) % 1440;
            float delayed = reverbBuf[readIdx];
            float reverbOut = sample + 0.15f * delayed;
            reverbBuf[reverbIdx] = sample + 0.3f * delayed;
            reverbIdx = (reverbIdx + 1) % 1440;

            sample = Math.Clamp(reverbOut, -1.0f, 1.0f);
            buffer[i] = sample;
        }
    }
}
