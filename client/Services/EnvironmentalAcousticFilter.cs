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
    /// Evaluates zones and positions to set target acoustic parameter presets.
    /// Supports compartment and deck-level occlusion based on local ship and bunker coordinates.
    /// </summary>
    public void UpdateZoneInfo(string speakerZone, string listenerZone, double lx = 0, double ly = 0, double lz = 0, double sx = 0, double sy = 0, double sz = 0, bool enableAtmosphere = false, bool enableEnvironmentalAcoustics = true)
    {
        // 1. Determine Occlusion (muffled audio if players are in different sub-zones or separated by bulkheads/walls)
        double volumeFactor = 1.0;
        double cutoff = 20000.0;

        string szLower = (speakerZone ?? "").ToLowerInvariant();
        string lzLower = (listenerZone ?? "").ToLowerInvariant();

        bool hasCoordinates = (lx != 0 || ly != 0 || lz != 0) && (sx != 0 || sy != 0 || sz != 0);

        if (enableEnvironmentalAcoustics)
        {
            if (hasCoordinates && speakerZone == listenerZone && !string.IsNullOrEmpty(speakerZone))
            {
                // Inside the same zone, check if we are in a known ship or bunker compartment layout
                if (lzLower.Contains("carrack"))
                {
                    // Z levels: Command Deck (Top: Z > 5), Habitation (Middle: -5 to 5), Technical (Bottom: Z < -5)
                    int lDeck = lz > 5 ? 3 : (lz < -5 ? 1 : 2);
                    int sDeck = sz > 5 ? 3 : (sz < -5 ? 1 : 2);

                    if (lDeck != sDeck)
                    {
                        cutoff = 350.0;
                        volumeFactor = 0.35;
                    }
                    else
                    {
                        // Same deck, check compartment partitions (using Y coordinate)
                        // Cockpit: Y > 15, Habitation: -10 to 15, Engines: Y < -10
                        int lComp = ly > 15 ? 3 : (ly < -10 ? 1 : 2);
                        int sComp = sy > 15 ? 3 : (sy < -10 ? 1 : 2);

                        if (lComp != sComp)
                        {
                            cutoff = 900.0;
                            volumeFactor = 0.65;
                        }
                    }
                }
                else if (lzLower.Contains("bunker") || lzLower.Contains("facility") || lzLower.Contains("ugf"))
                {
                    // Z levels: Lobby/Elevator (Z > 8), Intermediate (-5 to 8), Main level (Z <= -5)
                    int lLevel = lz > 8 ? 3 : (lz < -5 ? 1 : 2);
                    int sLevel = sz > 8 ? 3 : (sz < -5 ? 1 : 2);

                    if (lLevel != sLevel)
                    {
                        cutoff = 300.0;
                        volumeFactor = 0.30;
                    }
                    else
                    {
                        // Same level, check room divisions (using X coordinate)
                        int lRoom = lx > 10 ? 2 : 1;
                        int sRoom = sx > 10 ? 2 : 1;

                        if (lRoom != sRoom)
                        {
                            cutoff = 800.0;
                            volumeFactor = 0.60;
                        }
                    }
                }
                else if (lzLower.Contains("hercules"))
                {
                    // Hercules Starlifter: Habitation (Top: Z > 3), Cargo (Bottom: Z <= 3)
                    bool lTop = lz > 3;
                    bool sTop = sz > 3;
                    if (lTop != sTop)
                    {
                        cutoff = 400.0;
                        volumeFactor = 0.45;
                    }
                }
                else if (lzLower.Contains("cutlass"))
                {
                    // Cutlass Black: Cockpit (Y > 8), Cargo (Y <= 8)
                    bool lCockpit = ly > 8;
                    bool sCockpit = sy > 8;
                    if (lCockpit != sCockpit)
                    {
                        cutoff = 1000.0;
                        volumeFactor = 0.70;
                    }
                }
                else
                {
                    // General elevation heuristic: if height difference is > 4.5m, assume floor/ceiling occlusion
                    double heightDiff = Math.Abs(lz - sz);
                    if (heightDiff > 4.5)
                    {
                        cutoff = 500.0;
                        volumeFactor = 0.45;
                    }
                }
            }
            else
            {
                if (speakerZone != listenerZone)
                {
                    cutoff = 600.0;
                    volumeFactor = 0.65;
                }
            }
        }

        // Apply Atmosphere Simulation muffling (low-pass) if enabled and outdoors
        if (enableAtmosphere)
        {
            // If player is inside a ship or facility, they are in standard pressurized air
            bool isIndoors = lzLower.Contains("carrack") || lzLower.Contains("hercules") || lzLower.Contains("cutlass") ||
                              lzLower.Contains("bunker") || lzLower.Contains("facility") || lzLower.Contains("ugf") ||
                              lzLower.Contains("station") || lzLower.Contains("outpost") || lzLower.Contains("hangar");
            if (!isIndoors)
            {
                double atmosCutoff = 20000.0;
                if (lzLower.Contains("cellin") || lzLower.Contains("ita")) atmosCutoff = 800.0;
                else if (lzLower.Contains("yela") || lzLower.Contains("lyria")) atmosCutoff = 1000.0;
                else if (lzLower.Contains("daymar") || lzLower.Contains("wala") || lzLower.Contains("magda")) atmosCutoff = 1200.0;
                else if (lzLower.Contains("crusader") || lzLower.Contains("arial") || lzLower.Contains("aberdeen")) atmosCutoff = 16000.0; // slightly muffled at highs

                if (atmosCutoff < cutoff)
                {
                    cutoff = atmosCutoff;
                }
            }
        }

        _targetVolumeFactor = (float)volumeFactor;
        _targetCutoff = cutoff;

        // 2. Determine Reverb Presets based on listener's zone
        _targetDelaySamples = 0;
        _targetFeedback = 0f;
        _targetWetMix = 0f;

        if (enableEnvironmentalAcoustics)
        {
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
