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

    private static readonly float[] KeyDownChime;
    private static readonly float[] KeyUpChime;

    static AudioPlaybackService()
    {
        KeyDownChime = GenerateKeyDownChime();
        KeyUpChime = GenerateKeyUpChime();
    }

    public bool ProximityMuted { get; set; } = false;
    public bool RadioMuted { get; set; } = false;
    public bool ProfileMuted { get; set; } = false;

    // Spatial Audio & Modulation State
    public bool EnableSpatialAudio { get; set; } = true;
    public bool EnableRadioDegradation { get; set; } = true;
    public bool EnablePttChimes { get; set; } = true;
    public bool EnableEnvironmentalAcoustics { get; set; } = true;
    public bool EnableHelmetModulator { get; set; } = true;
    public bool EnableStt { get; set; } = false;

    public event Action<string, float[], byte>? SttAudioChunkReady;

    public double ListenerX { get; set; }
    public double ListenerY { get; set; }
    public double ListenerZ { get; set; }
    public double ListenerHeadingX { get; set; } = 0.0;
    public double ListenerHeadingY { get; set; } = 1.0; // Default facing North (+Y)

    private static float[] GenerateKeyDownChime()
    {
        // 50ms chime at 48000Hz = 2400 samples
        int samples = 48000 * 50 / 1000;
        float[] buffer = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            double t = (double)i / 48000.0;
            // frequency sweep from 900Hz down to 700Hz
            double freq = 900.0 - (t / 0.05) * 200.0;
            double angle = 2 * Math.PI * freq * t;
            float sample = (float)Math.Sin(angle);

            // Envelope: linear fade-in (5ms) and fade-out (5ms)
            float env = 1.0f;
            if (i < 240) env = i / 240.0f;
            else if (i > samples - 240) env = (samples - i) / 240.0f;

            buffer[i] = sample * env * 0.15f; // Quiet chime (15%)
        }
        return buffer;
    }

    private static float[] GenerateKeyUpChime()
    {
        // 180ms white noise at 48000Hz = 8640 samples
        int samples = 48000 * 180 / 1000;
        float[] buffer = new float[samples];
        var rand = new Random();
        
        // Simple biquad-like bandpass filter for squelch tail (800Hz - 2500Hz)
        var lp = new BiquadFilter();
        var hp = new BiquadFilter();
        lp.SetLpCoefficients(2500, 48000);
        hp.SetHpCoefficients(800, 48000);

        for (int i = 0; i < samples; i++)
        {
            float noise = (float)(rand.NextDouble() * 2.0 - 1.0);
            noise = lp.Process(noise);
            noise = hp.Process(noise);

            // Envelope: fast fade-in (2ms) then linear decay (178ms)
            float env;
            if (i < 96) env = i / 96.0f;
            else env = 1.0f - ((i - 96) / (float)(samples - 96));

            buffer[i] = noise * env * 0.25f; // Static hiss (25%)
        }
        return buffer;
    }

    private static void WriteFloatBuffer(BufferedWaveProvider provider, float[] samples)
    {
        byte[] bytes = new byte[samples.Length * 4];
        for (int i = 0; i < samples.Length; i++)
        {
            BitConverter.TryWriteBytes(bytes.AsSpan(i * 4), samples[i]);
        }
        provider.AddSamples(bytes, 0, bytes.Length);
    }

    private System.Threading.CancellationTokenSource? _loopCts;
    private System.Threading.Tasks.Task? _loopTask;

    public void Start(int deviceIndex, double outputGainPercent)
    {
        Stop();
        _outputGainLinear = outputGainPercent / 100.0;

        // Mixer is stereo
        _mixer = new MixingSampleProvider(_stereoFormat) { ReadFully = true };

        _waveOut = new WaveOutEvent { DeviceNumber = deviceIndex, DesiredLatency = 100 };
        _waveOut.Init(_mixer);
        _waveOut.Play();

        // Start high-precision playback tick loop (20ms interval)
        _loopCts = new System.Threading.CancellationTokenSource();
        _loopTask = System.Threading.Tasks.Task.Run(() => PlaybackLoopAsync(_loopCts.Token));
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

    /// <summary>Called from AudioUdpService when a binary packet arrives.</summary>
    public void ReceiveOpusFrame(
        string playerName,
        byte[] opusData,
        byte audioType,
        bool applyRadioEffect,
        ProximityMetadata? metadata,
        double distance = -1.0,
        string speakerZone = "",
        string listenerZone = "",
        ushort seq = 0)
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

        // Track last active time
        track.LastReceivedTime = DateTime.UtcNow;

        // Enqueue into Jitter Buffer
        track.Jitter.Enqueue(new AudioPacket
        {
            SequenceNumber = seq,
            AudioType = audioType,
            OpusData = opusData,
            ApplyRadioEffect = applyRadioEffect,
            Metadata = metadata,
            Distance = distance,
            SpeakerZone = speakerZone,
            ListenerZone = listenerZone
        });
    }

    private async System.Threading.Tasks.Task PlaybackLoopAsync(System.Threading.CancellationToken ct)
    {
        using var timer = new System.Threading.PeriodicTimer(TimeSpan.FromMilliseconds(20));
        try
        {
            while (await timer.WaitForNextTickAsync(ct))
            {
                TickPlayback();
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            LogService.Error("Exception in PlaybackLoopAsync", ex);
        }
    }

    private void TickPlayback()
    {
        lock (_lock)
        {
            foreach (var kvp in _tracks)
            {
                var playerName = kvp.Key;
                var track = kvp.Value;
                if (playerName == "__local_chime") continue;

                var packet = track.Jitter.Dequeue(out bool isPlcNeeded);

                if (packet != null)
                {
                    // 1. Process end-of-transmission or PTT chimes triggers
                    if (packet.OpusData.Length == 0)
                    {
                        if (track.IsTransmitting)
                        {
                            track.IsTransmitting = false;
                            if (EnableStt)
                            {
                                FlushSttBuffer(track, packet.AudioType);
                            }
                            if (EnablePttChimes && packet.ApplyRadioEffect)
                            {
                                WriteFloatBuffer(track.Buffer, KeyUpChime);
                            }
                        }
                        continue;
                    }

                    if (!track.IsTransmitting)
                    {
                        track.IsTransmitting = true;
                        track.LastAudioType = packet.AudioType;
                        if (EnablePttChimes && packet.ApplyRadioEffect)
                        {
                            WriteFloatBuffer(track.Buffer, KeyDownChime);
                        }
                    }
                    else
                    {
                        track.LastAudioType = packet.AudioType;
                    }

                    // 2. 3D Spatial Audio calculations (for proximity)
                    float currentPan = 0.0f;
                    float distanceVolumeFactor = 1.0f;

                    if (packet.AudioType == 0x00 && packet.Metadata != null)
                    {
                        var (pan, volume) = CalculateSpatialParams(
                            ListenerX, ListenerY, ListenerZ,
                            ListenerHeadingX, ListenerHeadingY,
                            packet.Metadata.SpeakerX, packet.Metadata.SpeakerY, packet.Metadata.SpeakerZ,
                            packet.Metadata.Distance, packet.Metadata.MaxRange,
                            packet.Metadata.SpatialEnabled, EnableSpatialAudio);
                        currentPan = pan;
                        distanceVolumeFactor = volume;
                    }

                    track.Panning.Pan = currentPan;
                    track.Volume.Volume = distanceVolumeFactor;

                    // Calculate dynamic radio degradation
                    double deg = 0.0;
                    if (packet.ApplyRadioEffect && EnableRadioDegradation && packet.Distance >= 0)
                    {
                        if (packet.Distance > 1500)
                        {
                            deg = (packet.Distance - 1500) / (8000 - 1500);
                            if (deg > 1.0) deg = 1.0;
                        }
                    }
                    track.DspFilter.DegradationFactor = deg;

                    // Cache zones for potential PLC
                    track.LastSpeakerZone = packet.SpeakerZone;
                    track.LastListenerZone = packet.ListenerZone;

                    // 3. Decode frame
                    DecodeAndWriteToBuffer(track, packet.OpusData, packet.ApplyRadioEffect, packet.SpeakerZone, packet.ListenerZone, packet.Metadata);
                }
                else if (isPlcNeeded)
                {
                    // Trigger PLC (Packet Loss Concealment)
                    DecodeAndWriteToBuffer(track, null, track.DspFilter.DegradationFactor > 0, track.LastSpeakerZone, track.LastListenerZone, null);
                }
                else
                {
                    // Underflow: play silence / stop transmission
                    if (track.IsTransmitting)
                    {
                        track.IsTransmitting = false;
                        if (EnableStt)
                        {
                            FlushSttBuffer(track, track.LastAudioType);
                        }
                        if (EnablePttChimes)
                        {
                            WriteFloatBuffer(track.Buffer, KeyUpChime);
                        }
                    }
                }
            }
        }
    }

    private void FlushSttBuffer(PlayerAudioTrack track, byte audioType)
    {
        float[]? samples = null;
        lock (track.SttLock)
        {
            if (track.SttSampleBuffer.Count > 0)
            {
                samples = track.SttSampleBuffer.ToArray();
                track.SttSampleBuffer.Clear();
            }
        }
        if (samples != null && samples.Length > 8000) // Minimum 0.5s of audio to trigger transcription
        {
            SttAudioChunkReady?.Invoke(track.PlayerName, samples, audioType);
        }
    }

    private void DecodeAndWriteToBuffer(PlayerAudioTrack track, byte[]? opusData, bool applyRadioEffect, string speakerZone, string listenerZone, ProximityMetadata? metadata)
    {
        Span<short> pcm = stackalloc short[FrameSamples];
        int decoded;
        
        if (opusData == null || opusData.Length == 0)
        {
            decoded = track.Decoder.Decode(null, pcm, FrameSamples, false);
        }
        else
        {
            decoded = track.Decoder.Decode(opusData, pcm, FrameSamples, false);
        }

        if (decoded <= 0) return;

        var floatBuf = new float[decoded];
        for (int i = 0; i < decoded; i++)
        {
            floatBuf[i] = pcm[i] / 32768f;
        }

        if (EnableEnvironmentalAcoustics)
        {
            if (metadata != null && metadata.SpatialEnabled)
            {
                track.AcousticFilter.UpdateZoneInfo(
                    speakerZone, 
                    listenerZone, 
                    ListenerX, 
                    ListenerY, 
                    ListenerZ, 
                    metadata.SpeakerX, 
                    metadata.SpeakerY, 
                    metadata.SpeakerZ);
            }
            else
            {
                track.AcousticFilter.UpdateZoneInfo(speakerZone, listenerZone, 0, 0, 0, 0, 0, 0);
            }
            track.AcousticFilter.Process(floatBuf, decoded);
        }

        if (applyRadioEffect)
        {
            track.DspFilter.EnableHelmetModulator = EnableHelmetModulator;
            track.DspFilter.Process(floatBuf, decoded);
        }

        // Downsample to 16kHz for STT if enabled
        if (EnableStt)
        {
            lock (track.SttLock)
            {
                for (int i = 0; i < decoded; i += 3)
                {
                    if (i + 2 < decoded)
                    {
                        float avg = (floatBuf[i] + floatBuf[i + 1] + floatBuf[i + 2]) / 3.0f;
                        track.SttSampleBuffer.Add(avg);
                    }
                }

                if (track.SttSampleBuffer.Count >= 80000)
                {
                    var chunk = track.SttSampleBuffer.ToArray();
                    track.SttSampleBuffer.Clear();
                    SttAudioChunkReady?.Invoke(track.PlayerName, chunk, track.LastAudioType);
                }
            }
        }

        var byteBuf = new byte[decoded * 4];
        for (int i = 0; i < decoded; i++)
        {
            float s = floatBuf[i] * (float)_outputGainLinear * track.VolumeLinear;
            BitConverter.TryWriteBytes(byteBuf.AsSpan(i * 4), s);
        }
        track.Buffer.AddSamples(byteBuf, 0, byteBuf.Length);
    }

    public void PlayLocalPttChime(bool isKeyDown)
    {
        if (!EnablePttChimes || _mixer == null) return;

        PlayerAudioTrack track;
        lock (_lock)
        {
            if (!_tracks.TryGetValue("__local_chime", out track!))
            {
                track = CreateTrack("__local_chime");
                _tracks["__local_chime"] = track;
            }
        }

        track.Panning.Pan = 0f;
        track.Volume.Volume = 0.5f; // 50% volume for local chime feedback

        if (isKeyDown)
        {
            WriteFloatBuffer(track.Buffer, KeyDownChime);
        }
        else
        {
            WriteFloatBuffer(track.Buffer, KeyUpChime);
        }
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
        var buffer = new BufferedWaveProvider(_monoFormat) { DiscardOnBufferOverflow = true };
        var panning = new PanningSampleProvider(buffer.ToSampleProvider()) { Pan = 0f };
        var volume = new VolumeSampleProvider(panning) { Volume = 1.0f };
        _mixer!.AddMixerInput(volume);
        return new PlayerAudioTrack(playerName, decoder, buffer, panning, volume);
    }

    public void Stop()
    {
        _loopCts?.Cancel();
        try
        {
            _loopTask?.GetAwaiter().GetResult();
        }
        catch { }
        _loopCts?.Dispose();
        _loopCts = null;
        _loopTask = null;

        _waveOut?.Stop();
        _waveOut?.Dispose();
        _waveOut = null;
        lock (_lock)
        {
            foreach (var track in _tracks.Values)
            {
                track.Jitter.Clear();
            }
            _tracks.Clear();
        }
        _mixer = null;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
    }

    public List<string> GetActiveSpeakers(double activeTimeoutMs = 400)
    {
        var active = new List<string>();
        lock (_lock)
        {
            foreach (var kvp in _tracks)
            {
                if (kvp.Key == "__local_chime") continue;
                if (kvp.Value.IsTransmitting && (DateTime.UtcNow - kvp.Value.LastReceivedTime).TotalMilliseconds < activeTimeoutMs)
                {
                    active.Add(kvp.Key);
                }
            }
        }
        return active;
    }

    private sealed class PlayerAudioTrack(
        string playerName,
        IOpusDecoder decoder,
        BufferedWaveProvider buffer,
        PanningSampleProvider panning,
        VolumeSampleProvider volume)
    {
        public string PlayerName { get; } = playerName;
        public IOpusDecoder Decoder { get; } = decoder;
        public BufferedWaveProvider Buffer { get; } = buffer;
        public PanningSampleProvider Panning { get; } = panning;
        public VolumeSampleProvider Volume { get; } = volume;
        public float VolumeLinear { get; set; } = 1.0f;
        public RadioDspFilter DspFilter { get; } = new();
        public EnvironmentalAcousticFilter AcousticFilter { get; } = new();
        public bool IsTransmitting { get; set; } = false;
        public DateTime LastReceivedTime { get; set; } = DateTime.MinValue;
        public JitterBuffer Jitter { get; } = new();
        public string LastSpeakerZone { get; set; } = string.Empty;
        public string LastListenerZone { get; set; } = string.Empty;

        public byte LastAudioType { get; set; } = 0x00;
        public List<float> SttSampleBuffer { get; } = new();
        public readonly object SttLock = new();
    }
}

internal class AudioPacket
{
    public ushort SequenceNumber { get; set; }
    public byte AudioType { get; set; }
    public byte[] OpusData { get; set; } = Array.Empty<byte>();
    public bool ApplyRadioEffect { get; set; }
    public ProximityMetadata? Metadata { get; set; }
    public double Distance { get; set; }
    public string SpeakerZone { get; set; } = string.Empty;
    public string ListenerZone { get; set; } = string.Empty;
}

internal class JitterBuffer
{
    private readonly SortedList<ushort, AudioPacket> _packets = new(new SequenceNumberComparer());
    private readonly object _lock = new();
    private ushort? _expectedSeq;
    private bool _isBuffering = true;
    private const int MaxBufferSize = 50;
    private const int TargetDelayFrames = 3;

    public int Count
    {
        get { lock (_lock) return _packets.Count; }
    }

    public void Enqueue(AudioPacket packet)
    {
        lock (_lock)
        {
            if (_packets.ContainsKey(packet.SequenceNumber))
            {
                return;
            }

            if (_packets.Count >= MaxBufferSize)
            {
                _packets.RemoveAt(0);
            }

            _packets.Add(packet.SequenceNumber, packet);
        }
    }

    public AudioPacket? Dequeue(out bool isPlcNeeded)
    {
        isPlcNeeded = false;
        lock (_lock)
        {
            if (_packets.Count == 0)
            {
                _expectedSeq = null;
                _isBuffering = true;
                return null;
            }

            if (_isBuffering)
            {
                if (_packets.Count >= TargetDelayFrames)
                {
                    _isBuffering = false;
                }
                else
                {
                    return null;
                }
            }

            if (!_expectedSeq.HasValue)
            {
                _expectedSeq = _packets.Keys[0];
            }

            ushort currentExpected = _expectedSeq.Value;

            if (_packets.TryGetValue(currentExpected, out var packet))
            {
                _packets.Remove(currentExpected);
                _expectedSeq = (ushort)(currentExpected + 1);
                if (_packets.Count == 0)
                {
                    _expectedSeq = null;
                    _isBuffering = true;
                }
                return packet;
            }

            // Check if we have any newer packets to decide if we should do PLC
            bool hasNewer = false;
            foreach (var seq in _packets.Keys)
            {
                if (CompareSequenceNumbers(seq, currentExpected) > 0)
                {
                    hasNewer = true;
                    break;
                }
            }

            if (hasNewer)
            {
                isPlcNeeded = true;
                _expectedSeq = (ushort)(currentExpected + 1);
                return null;
            }
            else
            {
                // Underflow, wait for next packets
                _expectedSeq = null;
                _isBuffering = true;
                return null;
            }
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _packets.Clear();
            _expectedSeq = null;
            _isBuffering = true;
        }
    }

    public static int CompareSequenceNumbers(ushort a, ushort b)
    {
        if (a == b) return 0;
        ushort diff = (ushort)(a - b);
        if (diff < 32768)
            return 1;
        else
            return -1;
    }

    private class SequenceNumberComparer : IComparer<ushort>
    {
        public int Compare(ushort x, ushort y)
        {
            return CompareSequenceNumbers(x, y);
        }
    }
}
