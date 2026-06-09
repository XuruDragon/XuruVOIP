using Concentus;
using Concentus.Structs;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;

namespace XuruVoipClient.Services;

/// <summary>
/// Decodes incoming Opus frames from remote players and plays them back through the selected output device.
/// Supports per-player volume control, a global output gain, distance attenuation, and stereo 3D spatial panning.
/// </summary>
public class AudioPlaybackService : IDisposable
{
    private const int SampleRate = 48000;
    private const int Channels = 1; // Input Opus frames are mono (1 channel)
    private const int FrameSizeMs = 20;
    private const int FrameSamples = SampleRate * FrameSizeMs / 1000;

    private WaveOutEvent? _waveOut;
    private MixingSampleProvider? _mixer;
    private readonly WaveFormat _monoFormat = WaveFormat.CreateIeeeFloatWaveFormat(SampleRate, 1);
    private readonly WaveFormat _stereoFormat = WaveFormat.CreateIeeeFloatWaveFormat(SampleRate, 2);

    // Per-player: decoder + BufferedWaveProvider + PanningSampleProvider + VolumeSampleProvider
    private readonly Dictionary<string, PlayerAudioTrack> _tracks = [];
    private readonly object _lock = new();

    private double _outputGainLinear = 1.0;
    private bool _disposed;

    public bool ProximityMuted { get; set; } = false;
    public bool RadioMuted { get; set; } = false;
    public bool ProfileMuted { get; set; } = false;

    // Spatial Audio Listener State
    public bool EnableSpatialAudio { get; set; } = true;
    public double ListenerX { get; set; }
    public double ListenerY { get; set; }
    public double ListenerZ { get; set; }
    public double ListenerHeadingX { get; set; } = 0.0;
    public double ListenerHeadingY { get; set; } = 1.0; // Default facing North (+Y)

    public void Start(int deviceIndex, double outputGainPercent)
    {
        Stop();
        _outputGainLinear = outputGainPercent / 100.0;

        // Mixer is stereo
        _mixer = new MixingSampleProvider(_stereoFormat) { ReadFully = true };

        _waveOut = new WaveOutEvent { DeviceNumber = deviceIndex, DesiredLatency = 100 };
        _waveOut.Init(_mixer);
        _waveOut.Play();
    }

    /// <summary>Calculates the pan and volume factor for 3D spatial audio.</summary>
    public static (float pan, float volumeFactor) CalculateSpatialParams(
        double listenerX, double listenerY, double listenerZ,
        double listenerHeadingX, double listenerHeadingY,
        double speakerX, double speakerY, double speakerZ,
        float distance, float maxRange, bool spatialEnabled, bool clientSpatialActive)
    {
        if (maxRange <= 0) maxRange = 50.0f;

        // Distance-based volume fade (linear roll-off)
        float distanceVolumeFactor = 1.0f - (distance / maxRange);
        if (distanceVolumeFactor < 0.0f) distanceVolumeFactor = 0.0f;
        else if (distanceVolumeFactor > 1.0f) distanceVolumeFactor = 1.0f;

        float currentPan = 0.0f;

        // 3D panning (only if enabled on client and supported by server)
        if (clientSpatialActive && spatialEnabled)
        {
            double dx = speakerX - listenerX;
            double dy = speakerY - listenerY;
            double dist2D = Math.Sqrt(dx * dx + dy * dy);

            if (dist2D > 0.1)
            {
                double normDx = dx / dist2D;
                double normDy = dy / dist2D;

                // Projection onto listener's Right vector: R = (HeadingY, -HeadingX)
                double rightComponent = normDx * listenerHeadingY - normDy * listenerHeadingX;
                // Projection onto listener's Forward vector: F = (HeadingX, HeadingY)
                double forwardComponent = normDx * listenerHeadingX + normDy * listenerHeadingY;

                currentPan = (float)rightComponent;
                if (currentPan < -1.0f) currentPan = -1.0f;
                else if (currentPan > 1.0f) currentPan = 1.0f;

                // Resolve front-back ambiguity with volume reduction for speaker behind listener
                if (forwardComponent < 0)
                {
                    float behindFactor = 1.0f - 0.25f * (float)Math.Abs(forwardComponent);
                    distanceVolumeFactor *= behindFactor;
                }
            }
        }

        return (currentPan, distanceVolumeFactor);
    }

    /// <summary>Called from AudioWebSocketService when a binary packet arrives.</summary>
    public void ReceiveOpusFrame(string playerName, byte[] opusData, byte audioType, bool applyRadioEffect, ProximityMetadata? metadata)
    {
        if (_mixer == null) return;

        if (audioType == 0x00 && ProximityMuted) return;
        if (audioType == 0x01 && RadioMuted) return;
        if (audioType == 0x02 && ProfileMuted) return;

        PlayerAudioTrack track;
        lock (_lock)
        {
            if (!_tracks.TryGetValue(playerName, out track!))
            {
                track = CreateTrack(playerName);
                _tracks[playerName] = track;
            }
        }

        // 3D Spatial Audio calculations
        float currentPan = 0.0f;
        float distanceVolumeFactor = 1.0f;

        if (audioType == 0x00 && metadata != null)
        {
            var (pan, volume) = CalculateSpatialParams(
                ListenerX, ListenerY, ListenerZ,
                ListenerHeadingX, ListenerHeadingY,
                metadata.SpeakerX, metadata.SpeakerY, metadata.SpeakerZ,
                metadata.Distance, metadata.MaxRange,
                metadata.SpatialEnabled, EnableSpatialAudio);
            currentPan = pan;
            distanceVolumeFactor = volume;
        }

        // Apply pan and volume to track providers
        track.Panning.Pan = currentPan;
        track.Volume.Volume = distanceVolumeFactor;

        // Decode
        Span<short> pcm = stackalloc short[FrameSamples];
        int decoded = track.Decoder.Decode(opusData, pcm, FrameSamples, false);
        if (decoded <= 0) return;

        // Convert short PCM to float
        var floatBuf = new float[decoded];
        for (int i = 0; i < decoded; i++)
        {
            floatBuf[i] = pcm[i] / 32768f;
        }

        // Apply Radio DSP Effect if helmet or channel dictates it
        if (applyRadioEffect)
        {
            track.DspFilter.Process(floatBuf, decoded);
        }

        // Convert back and write to buffer (with volume adjustments)
        var byteBuf = new byte[decoded * 4];
        for (int i = 0; i < decoded; i++)
        {
            float s = floatBuf[i] * (float)_outputGainLinear * track.VolumeLinear;
            BitConverter.TryWriteBytes(byteBuf.AsSpan(i * 4), s);
        }
        track.Buffer.AddSamples(byteBuf, 0, byteBuf.Length);
    }

    public void SetPlayerVolume(string playerName, double percent)
    {
        lock (_lock)
        {
            if (_tracks.TryGetValue(playerName, out var t))
                t.VolumeLinear = (float)(percent / 100.0);
        }
    }

    public void RemovePlayer(string playerName)
    {
        lock (_lock)
        {
            if (_tracks.TryGetValue(playerName, out var t))
            {
                _mixer?.RemoveMixerInput(t.Volume);
                _tracks.Remove(playerName);
            }
        }
    }

    private PlayerAudioTrack CreateTrack(string playerName)
    {
        var decoder = OpusCodecFactory.CreateDecoder(SampleRate, Channels);
        // Buffer is mono (1 channel)
        var buffer = new BufferedWaveProvider(_monoFormat) { DiscardOnBufferOverflow = true };
        // Pan provider takes mono and outputs stereo (2 channels)
        var panning = new PanningSampleProvider(buffer.ToSampleProvider()) { Pan = 0f };
        // Volume provider takes stereo and outputs stereo (2 channels)
        var volume = new VolumeSampleProvider(panning) { Volume = 1.0f };
        _mixer!.AddMixerInput(volume);
        return new PlayerAudioTrack(decoder, buffer, panning, volume);
    }

    public void Stop()
    {
        _waveOut?.Stop();
        _waveOut?.Dispose();
        _waveOut = null;
        lock (_lock) { _tracks.Clear(); }
        _mixer = null;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
    }

    private sealed class PlayerAudioTrack(
        IOpusDecoder decoder,
        BufferedWaveProvider buffer,
        PanningSampleProvider panning,
        VolumeSampleProvider volume)
    {
        public IOpusDecoder Decoder { get; } = decoder;
        public BufferedWaveProvider Buffer { get; } = buffer;
        public PanningSampleProvider Panning { get; } = panning;
        public VolumeSampleProvider Volume { get; } = volume;
        public float VolumeLinear { get; set; } = 1.0f;
        public RadioDspFilter DspFilter { get; } = new();
    }
}
