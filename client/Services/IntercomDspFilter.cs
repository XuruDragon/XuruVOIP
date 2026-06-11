using System;

namespace XuruVoipClient.Services;

public class IntercomDspFilter
{
    private const int SampleRate = 48000;
    private readonly Random _random = new();

    // Configuration sub-toggles
    public bool ShieldHitsEnabled { get; set; } = true;
    public bool CriticalPowerEnabled { get; set; } = true;
    public bool QuantumTravelEnabled { get; set; } = true;

    // Resampler fields for Pitch Bend (Critical Power)
    private readonly float[] _resampleBuffer = new float[16384];
    private int _resampleWriteIdx = 0;
    private double _resampleReadPtr = 0.0;

    // Hum phase (Critical Power)
    private int _humPhase = 0;

    // Phaser/Flanger fields (Quantum Travel)
    private readonly float[] _flangerBuffer = new float[4800];
    private int _flangerWriteIdx = 0;
    private double _flangerLfoPhase = 0.0;

    // Whine phase (Quantum Travel)
    private int _whinePhase = 0;

    public void Process(float[] buffer, int count, IntercomDegradationState state)
    {
        if (state == IntercomDegradationState.Normal)
        {
            return;
        }

        for (int i = 0; i < count; i++)
        {
            float sample = buffer[i];

            // --- 1. SHIELD HIT / STATIC BURST ---
            if (state == IntercomDegradationState.ShieldHit && ShieldHitsEnabled)
            {
                // Ingest white noise
                float noise = (float)(_random.NextDouble() * 2.0 - 1.0) * 0.15f;
                sample += noise;

                // Crackles/pops (voltage spikes) - 0.8% probability per sample
                if (_random.NextDouble() < 0.008)
                {
                    sample += _random.NextDouble() > 0.5 ? 0.75f : -0.75f;
                }
            }

            // --- 2. CRITICAL POWER (HUM, PITCH BEND, SATURATION) ---
            if (state == IntercomDegradationState.CriticalPower && CriticalPowerEnabled)
            {
                // A. Hum (60Hz + 120Hz + 180Hz)
                double hum = Math.Sin(2.0 * Math.PI * 60.0 * _humPhase / SampleRate) * 0.14
                           + Math.Sin(2.0 * Math.PI * 120.0 * _humPhase / SampleRate) * 0.07
                           + Math.Sin(2.0 * Math.PI * 180.0 * _humPhase / SampleRate) * 0.03;
                _humPhase = (_humPhase + 1) % SampleRate;
                sample += (float)hum;

                // B. Pitch resample down to 0.78x pitch
                _resampleBuffer[_resampleWriteIdx] = sample;
                _resampleWriteIdx = (_resampleWriteIdx + 1) % 16384;

                int idx0 = (int)Math.Floor(_resampleReadPtr) % 16384;
                int idx1 = (idx0 + 1) % 16384;
                double frac = _resampleReadPtr - Math.Floor(_resampleReadPtr);
                sample = (float)((1.0 - frac) * _resampleBuffer[idx0] + frac * _resampleBuffer[idx1]);

                _resampleReadPtr = (_resampleReadPtr + 0.78) % 16384;

                // Keep read pointer from lagging too far behind write pointer (auto-sync latency)
                int lag = (_resampleWriteIdx - (int)_resampleReadPtr + 16384) % 16384;
                if (lag > 2048)
                {
                    _resampleReadPtr = (_resampleWriteIdx - 512 + 16384) % 16384;
                }

                // C. Heavy Soft-clipping/saturation
                sample = (float)(Math.Tanh(sample * 3.5) * 0.75);
            }

            // --- 3. QUANTUM TRAVEL (FLANGER SWEEP & WHINE) ---
            if (state == IntercomDegradationState.QuantumTravel && QuantumTravelEnabled)
            {
                // A. Comb filter flanger sweep (0.2 Hz LFO sweep)
                _flangerBuffer[_flangerWriteIdx] = sample;

                double lfo = (Math.Sin(2.0 * Math.PI * 0.2 * _flangerLfoPhase / SampleRate) + 1.0) / 2.0;
                double delaySamples = 48.0 + lfo * 144.0; // 1ms to 4ms delay
                double readPtr = (_flangerWriteIdx - delaySamples + 4800) % 4800;

                int rIdx0 = (int)Math.Floor(readPtr) % 4800;
                int rIdx1 = (rIdx0 + 1) % 4800;
                double rFrac = readPtr - Math.Floor(readPtr);
                float delayedSample = (float)((1.0 - rFrac) * _flangerBuffer[rIdx0] + rFrac * _flangerBuffer[rIdx1]);

                sample = sample * 0.55f + delayedSample * 0.45f;

                _flangerWriteIdx = (_flangerWriteIdx + 1) % 4800;
                _flangerLfoPhase = (_flangerLfoPhase + 1) % SampleRate;

                // B. Quantum whine (1800Hz with vibrato)
                double whine = Math.Sin(2.0 * Math.PI * (1800.0 + Math.Sin(2.0 * Math.PI * 6.0 * _whinePhase / SampleRate) * 40.0) * _whinePhase / SampleRate) * 0.045;
                _whinePhase = (_whinePhase + 1) % SampleRate;
                sample += (float)whine;
            }

            buffer[i] = Math.Clamp(sample, -1.0f, 1.0f);
        }
    }
}
