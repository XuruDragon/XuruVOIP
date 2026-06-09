using System;

namespace XuruVoipClient.Services;

/// <summary>
/// Simulates physical acoustics (occlusion low-pass filter and echo/reverberation)
/// based on the player's zone.
/// </summary>
public class EnvironmentalAcousticFilter
{
    private const int SampleRate = 48000;

    // Occlusion Low-Pass Filter
    private readonly BiquadFilter _lpFilter = new();
    private float _currentVolumeFactor = 1.0f;
    private float _targetVolumeFactor = 1.0f;
    private double _currentCutoff = 20000.0;
    private double _targetCutoff = 20000.0;

    // Reverberation Delay Line (comb filter)
    private readonly float[] _delayBuffer = new float[14400]; // Holds up to 300ms of audio at 48kHz
    private int _writeIdx = 0;

    private int _currentDelaySamples = 0;
    private int _targetDelaySamples = 0;
    private float _currentFeedback = 0f;
    private float _targetFeedback = 0f;
    private float _currentWetMix = 0f;
    private float _targetWetMix = 0f;

    public EnvironmentalAcousticFilter()
    {
        _lpFilter.SetLpCoefficients(_currentCutoff, SampleRate);
    }

    /// <summary>
    /// Evaluates zones to set target acoustic parameter presets.
    /// </summary>
    public void UpdateZoneInfo(string speakerZone, string listenerZone)
    {
        // 1. Determine Occlusion (muffled audio if players are in different sub-zones)
        bool occluded = false;
        if (!string.IsNullOrEmpty(speakerZone) && !string.IsNullOrEmpty(listenerZone))
        {
            if (speakerZone != listenerZone)
            {
                occluded = true;
            }
        }

        _targetVolumeFactor = occluded ? 0.65f : 1.0f;
        _targetCutoff = occluded ? 600.0 : 20000.0;

        // 2. Determine Reverb Presets based on listener's zone
        string zoneLower = (listenerZone ?? "").ToLowerInvariant();

        if (zoneLower.Contains("cave") || zoneLower.Contains("mine") || zoneLower.Contains("chasm") || zoneLower.Contains("tunnel") || zoneLower.Contains("ruin"))
        {
            _targetDelaySamples = 4800; // 100ms delay
            _targetFeedback = 0.6f;
            _targetWetMix = 0.45f;
        }
        else if (zoneLower.Contains("bunker") || zoneLower.Contains("facility") || zoneLower.Contains("ugf") || zoneLower.Contains("station") || zoneLower.Contains("outpost"))
        {
            _targetDelaySamples = 2400; // 50ms delay
            _targetFeedback = 0.4f;
            _targetWetMix = 0.25f;
        }
        else if (zoneLower.Contains("hangar"))
        {
            _targetDelaySamples = 7200; // 150ms delay
            _targetFeedback = 0.5f;
            _targetWetMix = 0.35f;
        }
        else
        {
            _targetDelaySamples = 0;
            _targetFeedback = 0f;
            _targetWetMix = 0f;
        }
    }

    /// <summary>
    /// Processes floating-point mono audio samples.
    /// Applies low-pass filtering and reverberation with smooth interpolation.
    /// </summary>
    public void Process(float[] buffer, int count)
    {
        // Smoothly interpolate low-pass cutoff frequency
        if (Math.Abs(_currentCutoff - _targetCutoff) > 1.0)
        {
            _currentCutoff = _currentCutoff * 0.9 + _targetCutoff * 0.1;
            _lpFilter.SetLpCoefficients(_currentCutoff, SampleRate);
        }

        // Smoothly interpolate volume factor
        if (Math.Abs(_currentVolumeFactor - _targetVolumeFactor) > 0.01f)
        {
            _currentVolumeFactor = _currentVolumeFactor * 0.9f + _targetVolumeFactor * 0.1f;
        }

        // Smoothly interpolate reverb parameters
        if (Math.Abs(_currentWetMix - _targetWetMix) > 0.01f)
        {
            _currentWetMix = _currentWetMix * 0.9f + _targetWetMix * 0.1f;
        }

        // Switch delay settings immediately when wet mix is zero to prevent pitch warping
        if (_currentWetMix < 0.01f && _targetWetMix > 0.01f)
        {
            _currentDelaySamples = _targetDelaySamples;
            _currentFeedback = _targetFeedback;
        }
        else
        {
            if (Math.Abs(_currentFeedback - _targetFeedback) > 0.01f)
            {
                _currentFeedback = _currentFeedback * 0.9f + _targetFeedback * 0.1f;
            }
            if (_currentDelaySamples != _targetDelaySamples)
            {
                _currentDelaySamples = _targetDelaySamples; // Integer steps
            }
        }

        for (int i = 0; i < count; i++)
        {
            float sample = buffer[i];

            // 1. Apply Occlusion low-pass filter
            if (_currentCutoff < 15000.0)
            {
                sample = _lpFilter.Process(sample);
            }

            // Apply occlusion volume factor
            sample *= _currentVolumeFactor;

            // 2. Apply Reverberation
            if (_currentWetMix > 0.005f && _currentDelaySamples > 0)
            {
                int readIdx = (_writeIdx - _currentDelaySamples + _delayBuffer.Length) % _delayBuffer.Length;
                float delayedSample = _delayBuffer[readIdx];

                float reverbOut = sample + _currentWetMix * delayedSample;
                _delayBuffer[_writeIdx] = sample + _currentFeedback * delayedSample;

                sample = reverbOut;
            }
            else
            {
                // Clear buffer if reverb is inactive to avoid hanging feedback tails
                _delayBuffer[_writeIdx] = 0f;
            }

            _writeIdx = (_writeIdx + 1) % _delayBuffer.Length;
            buffer[i] = Math.Clamp(sample, -1.0f, 1.0f);
        }
    }
}
