using System;

namespace XuruVoipClient.Services;

/// <summary>
/// Real-time time-domain Pitch Shifter using two overlapping cross-fading delay lines.
/// </summary>
public class PitchShifter
{
    private readonly float[] _delayBuffer;
    private int _writePtr = 0;
    private double _readPtr1 = 0;
    private double _readPtr2 = 0;
    private readonly int _bufSize;
    private readonly int _halfBufSize;

    public PitchShifter(int size = 8192)
    {
        _bufSize = size;
        _halfBufSize = size / 2;
        _delayBuffer = new float[size];
        _readPtr1 = 0;
        _readPtr2 = _halfBufSize;
    }

    public void Process(float[] buffer, int count, float pitchFactor)
    {
        if (Math.Abs(pitchFactor - 1.0f) < 0.02f) return;

        for (int i = 0; i < count; i++)
        {
            float inSample = buffer[i];
            _delayBuffer[_writePtr] = inSample;

            int rp1 = (int)_readPtr1 % _bufSize;
            int rp2 = (int)_readPtr2 % _bufSize;

            float s1 = _delayBuffer[rp1];
            float s2 = _delayBuffer[rp2];

            // Linear cross-fade factor based on distance from write pointer
            double dist1 = (_readPtr1 - _writePtr + _bufSize) % _bufSize;
            float fade = (float)(dist1 / _bufSize);

            // Compute output sample
            float outSample = s1 * (1.0f - fade) + s2 * fade;
            buffer[i] = outSample;

            _writePtr = (_writePtr + 1) % _bufSize;
            _readPtr1 = (_readPtr1 + pitchFactor) % _bufSize;
            _readPtr2 = (_readPtr2 + pitchFactor) % _bufSize;
        }
    }
}

/// <summary>
/// Multiplies the audio signal by a carrier wave to produce metallic, robotic sci-fi tones.
/// </summary>
public class RingModulator
{
    private double _phase = 0;

    public float Process(float sample, float freq, float sampleRate, float mix)
    {
        double carrier = Math.Sin(2.0 * Math.PI * freq * _phase / sampleRate);
        _phase = (_phase + 1) % sampleRate;
        return (float)((1.0f - mix) * sample + mix * (sample * carrier));
    }
}

/// <summary>
/// Comb filter with an LFO-modulated delay line to produce sweeping swoosh effects.
/// </summary>
public class Flanger
{
    private readonly float[] _delayBuffer = new float[4800]; // 100ms max delay at 48kHz
    private int _writePtr = 0;
    private double _lfoPhase = 0;

    public float Process(float sample, float sampleRate, float depth, float rate, float feedback)
    {
        _delayBuffer[_writePtr] = sample;

        // LFO sine wave (-1 to 1)
        double lfoVal = Math.Sin(2.0 * Math.PI * rate * _lfoPhase / sampleRate);
        _lfoPhase = (_lfoPhase + 1) % sampleRate;

        // Map LFO to delay between 1ms and 5ms (48 to 240 samples)
        double delaySamples = 48 + (lfoVal + 1.0) * 0.5 * 192;
        double readPtr = (_writePtr - delaySamples + _delayBuffer.Length) % _delayBuffer.Length;

        int r1 = (int)readPtr;
        int r2 = (r1 + 1) % _delayBuffer.Length;
        float frac = (float)(readPtr - r1);

        // Linear interpolation
        float delayedSample = _delayBuffer[r1] * (1.0f - frac) + _delayBuffer[r2] * frac;

        // Mix dry + wet
        float output = sample + depth * delayedSample;

        // Feedback
        _delayBuffer[_writePtr] = Math.Clamp(sample + feedback * delayedSample, -1.0f, 1.0f);
        _writePtr = (_writePtr + 1) % _delayBuffer.Length;

        return output;
    }
}

/// <summary>
/// Coordinator class for applying voice changer presets and custom pitch modifications.
/// </summary>
public class VoiceModulator
{
    private const int SampleRate = 48000;
    private readonly PitchShifter _pitchShifter = new();
    private readonly RingModulator _ringMod = new();
    private readonly Flanger _flanger = new();
    private readonly Random _random = new();

    public void Process(float[] buffer, int count, string changerType, float pitchFactor)
    {
        if (string.IsNullOrEmpty(changerType) || changerType.Equals("None", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        switch (changerType.ToLowerInvariant())
        {
            case "pitchshift":
                _pitchShifter.Process(buffer, count, pitchFactor);
                break;

            case "alien":
                // Very deep voice + Ring Modulation + Flanger
                _pitchShifter.Process(buffer, count, 0.65f);
                for (int i = 0; i < count; i++)
                {
                    float sample = buffer[i];
                    sample = _ringMod.Process(sample, 85f, SampleRate, 0.3f);
                    sample = _flanger.Process(sample, SampleRate, 0.45f, 0.6f, 0.25f);
                    buffer[i] = sample;
                }
                break;

            case "cyborg":
                // Slightly metallic deep voice + Bitcrusher + Saturation
                _pitchShifter.Process(buffer, count, 0.82f);
                for (int i = 0; i < count; i++)
                {
                    float sample = buffer[i];
                    sample = _ringMod.Process(sample, 65f, SampleRate, 0.35f);
                    
                    // Soft saturation (tanh distortion)
                    sample = (float)Math.Tanh(sample * 1.5f) * 0.9f;

                    // Bitcrushing (reduce resolution to 8-bit equivalent)
                    sample = MathF.Round(sample * 128f) / 128f;

                    buffer[i] = sample;
                }
                break;

            case "robotic":
                // High-freq Ring modulation + Pitch shifted up + Flanger
                _pitchShifter.Process(buffer, count, 1.25f);
                for (int i = 0; i < count; i++)
                {
                    float sample = buffer[i];
                    sample = _ringMod.Process(sample, 140f, SampleRate, 0.45f);
                    sample = _flanger.Process(sample, SampleRate, 0.3f, 1.2f, 0.3f);
                    buffer[i] = sample;
                }
                break;
        }
    }
}
