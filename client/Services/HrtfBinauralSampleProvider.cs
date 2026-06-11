using NAudio.Wave;
using System;

namespace XuruVoipClient.Services;

/// <summary>
/// A stereo sample provider that takes a mono source and applies binaural HRTF approximation
/// using Interaural Time Difference (ITD) and Interaural Level Difference (ILD) head shadow filters.
/// Falls back to standard equal-power panning when HRTF is disabled.
/// </summary>
public class HrtfBinauralSampleProvider : ISampleProvider
{
    private readonly ISampleProvider _source;
    public WaveFormat WaveFormat { get; }

    public float Pan { get; set; } // -1.0 (Left) to 1.0 (Right)
    public bool EnableHrtf { get; set; } = false;

    // ITD Delay buffer: maximum delay at 48kHz is ~25 samples. We use a 128-sample ring buffer.
    private readonly float[] _delayL = new float[128];
    private readonly float[] _delayR = new float[128];
    private int _writeIdxL;
    private int _writeIdxR;

    // ILD Head shadow filters (low-pass filter on the shadowed ear further from the source)
    private readonly BiquadFilter _shadowFilterL = new();
    private readonly BiquadFilter _shadowFilterR = new();
    private double _lastCutoffL = -1;
    private double _lastCutoffR = -1;

    private float[] _sourceBuffer = new float[2048];

    public HrtfBinauralSampleProvider(ISampleProvider source)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        if (source.WaveFormat.Channels != 1)
            throw new ArgumentException("Source must be mono.", nameof(source));
        
        WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(source.WaveFormat.SampleRate, 2);
    }

    public int Read(float[] buffer, int offset, int count)
    {
        int monoSamplesNeeded = count / 2;

        if (_sourceBuffer.Length < monoSamplesNeeded)
        {
            _sourceBuffer = new float[monoSamplesNeeded * 2];
        }

        int samplesRead = _source.Read(_sourceBuffer, 0, monoSamplesNeeded);
        if (samplesRead == 0) return 0;

        float currentPan = Math.Clamp(Pan, -1.0f, 1.0f);
        double sr = WaveFormat.SampleRate;

        if (!EnableHrtf)
        {
            // Standard equal-power panning
            float angle = (currentPan + 1.0f) * (float)Math.PI / 4.0f;
            float gainL = (float)Math.Cos(angle);
            float gainR = (float)Math.Sin(angle);

            for (int i = 0; i < samplesRead; i++)
            {
                float sample = _sourceBuffer[i];
                buffer[offset + i * 2] = sample * gainL;
                buffer[offset + i * 2 + 1] = sample * gainR;
            }
        }
        else
        {
            // Binaural HRTF approximation
            // 1. Woodworth's ITD (up to ~24 samples delay at 48kHz)
            int delaySamples = (int)(Math.Abs(currentPan) * 24.0f);
            
            // 2. ILD head shadow cutoffs (shadowed ear goes down to 800Hz, closer ear stays flat at 20kHz)
            double cutoffL = 20000.0;
            double cutoffR = 20000.0;

            if (currentPan > 0) // Source on the right, left ear shadowed
            {
                cutoffL = 20000.0 - currentPan * 19200.0; // 20kHz down to 800Hz
            }
            else if (currentPan < 0) // Source on the left, right ear shadowed
            {
                cutoffR = 20000.0 - Math.Abs(currentPan) * 19200.0; // 20kHz down to 800Hz
            }

            // Update low-pass filters if cutoffs changed
            if (cutoffL != _lastCutoffL)
            {
                _shadowFilterL.SetLpCoefficients(cutoffL, sr);
                _lastCutoffL = cutoffL;
            }
            if (cutoffR != _lastCutoffR)
            {
                _shadowFilterR.SetLpCoefficients(cutoffR, sr);
                _lastCutoffR = cutoffR;
            }

            // Amplitude gains based on panning
            float angle = (currentPan + 1.0f) * (float)Math.PI / 4.0f;
            float gainL = (float)Math.Cos(angle);
            float gainR = (float)Math.Sin(angle);

            for (int i = 0; i < samplesRead; i++)
            {
                float sample = _sourceBuffer[i];

                // Write into delay buffers
                _delayL[_writeIdxL] = sample;
                _delayR[_writeIdxR] = sample;

                // Retrieve samples with ITD
                int readIdxL = _writeIdxL;
                int readIdxR = _writeIdxR;

                if (currentPan > 0) // Left delayed
                {
                    readIdxL = (_writeIdxL - delaySamples + 128) % 128;
                }
                else if (currentPan < 0) // Right delayed
                {
                    readIdxR = (_writeIdxR - delaySamples + 128) % 128;
                }

                float outL = _delayL[readIdxL] * gainL;
                float outR = _delayR[readIdxR] * gainR;

                // Apply ILD head shadow filters
                outL = _shadowFilterL.Process(outL);
                outR = _shadowFilterR.Process(outR);

                buffer[offset + i * 2] = outL;
                buffer[offset + i * 2 + 1] = outR;

                _writeIdxL = (_writeIdxL + 1) % 128;
                _writeIdxR = (_writeIdxR + 1) % 128;
            }
        }

        return samplesRead * 2;
    }
}
