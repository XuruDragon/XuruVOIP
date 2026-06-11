using System;
using System.Runtime.InteropServices;
using Concentus;
using Concentus.Enums;
using Concentus.Structs;
using NAudio.Wave;
using WebRtcVadSharp;
using XuruVoipClient.Models;

namespace XuruVoipClient.Services;

/// <summary>
/// Captures microphone audio, applies gain, runs VAD or PTT detection,
/// encodes with Opus and fires EncodedFrameReady for transmission.
/// </summary>
public class AudioCaptureService : IDisposable
{
    // Opus settings: 48kHz mono, 20ms frames = 960 samples
    private const int SampleRate = 48000;
    private const int Channels = 1;
    private const int FrameSizeMs = 20;
    private const int FrameSamples = SampleRate * FrameSizeMs / 1000; // 960

    private WaveInEvent? _waveIn;
    private IOpusEncoder? _encoder;
    private WebRtcVad? _vad;
    private readonly VoiceModulator _voiceModulator = new();

    private readonly BiquadFilter _breathHp = new();
    private readonly BiquadFilter _breathLp = new();
    private double _breathPhase = 0;
    private double _humPhase50 = 0;
    private double _humPhase100 = 0;
    private readonly Random _random = new();

    // G-Force & Exertion state
    private double _tremoloPhase = 0;
    private double _exertionBreathPhase = 0;
    private readonly PitchShifter _gforcePitchShifter = new();

    public AudioCaptureService()
    {
        _breathHp.SetHpCoefficients(650, SampleRate);
        _breathLp.SetLpCoefficients(1600, SampleRate);
    }

    // Rolling buffer to accumulate exactly one Opus frame worth of PCM
    private readonly short[] _frameBuffer = new short[FrameSamples];
    private int _frameBufferPos = 0;
    private readonly object _bufLock = new();

    private bool _isPttActive = false;
    private byte _currentTxType = 0x00;
    private bool _disposed;
    private volatile bool _isRecording = false;

    private bool _isRecordingCommand = false;
    private readonly List<float> _commandAudioBuffer = new();
    private readonly object _commandAudioLock = new();

    /// <summary>Fired with (opusData, txType) when a voice frame is ready to send.</summary>
    public event Action<byte[], byte>? EncodedFrameReady;

    /// <summary>Current RMS level 0..1 for VU meter.</summary>
    public float InputLevel { get; private set; }

    public AudioMode Mode { get; set; } = AudioMode.PTT;

    public bool IsTransmitting { get; private set; }

    public bool ProximityMuted { get; set; } = false;
    public bool RadioMuted { get; set; } = false;
    public bool ProfileMuted { get; set; } = false;

    // PTT state (called from key hook)
    public void SetPttState(bool active, byte txType)
    {
        _isPttActive = active;
        _currentTxType = txType;
    }

    public void Start(int deviceIndex, double gainDb, int vadSensitivity, AudioMode mode)
    {
        LogService.Info($"Starting AudioCaptureService: Device={deviceIndex}, Gain={gainDb}dB, VAD={vadSensitivity}, Mode={mode}");
        
        Stop();
        Mode = mode;

        try
        {
            // Build Opus encoder
            var encoder = OpusCodecFactory.CreateEncoder(SampleRate, Channels, OpusApplication.OPUS_APPLICATION_VOIP);
            encoder.Bitrate = 32000;
            encoder.SignalType = OpusSignal.OPUS_SIGNAL_VOICE;

            // VAD
            var vad = new WebRtcVad
            {
                OperatingMode = vadSensitivity switch
                {
                    0 => OperatingMode.HighQuality,
                    1 => OperatingMode.LowBitrate,
                    2 => OperatingMode.Aggressive,
                    3 => OperatingMode.VeryAggressive,
                    _ => OperatingMode.Aggressive
                }
            };

            lock (_bufLock)
            {
                _encoder = encoder;
                _vad = vad;
                _frameBufferPos = 0;
                Array.Clear(_frameBuffer, 0, _frameBuffer.Length);
                _isRecording = true;
            }

            // NAudio capture (16-bit PCM, 48kHz, mono)
            _waveIn = new WaveInEvent
            {
                DeviceNumber = deviceIndex,
                WaveFormat = new WaveFormat(SampleRate, 16, Channels),
                BufferMilliseconds = FrameSizeMs
            };

            double gainLinear = Math.Pow(10.0, gainDb / 20.0);
            _waveIn.DataAvailable += (_, e) => OnDataAvailable(e, gainLinear);
            _waveIn.StartRecording();
            LogService.Info("AudioCaptureService started recording successfully.");
        }
        catch (Exception ex)
        {
            LogService.Error("Failed to start AudioCaptureService", ex);
            throw;
        }
    }

    private void OnDataAvailable(WaveInEventArgs e, double gainLinear)
    {
        if (!_isRecording) return;

        // Convert bytes to 16-bit samples
        int sampleCount = e.BytesRecorded / 2;
        if (sampleCount <= 0) return;

        Span<short> incoming = stackalloc short[sampleCount];

        float sumSq = 0;
        for (int i = 0; i < sampleCount; i++)
        {
            short sample = BitConverter.ToInt16(e.Buffer, i * 2);
            // Apply gain
            double amplified = sample * gainLinear;
            incoming[i] = (short)Math.Clamp(amplified, short.MinValue, short.MaxValue);
            sumSq += incoming[i] * incoming[i];
        }
        InputLevel = (float)Math.Sqrt(sumSq / sampleCount) / 32768f;

        lock (_commandAudioLock)
        {
            if (_isRecordingCommand)
            {
                // Downsample from 48kHz to 16kHz by taking the average of every 3 samples
                for (int i = 0; i < sampleCount; i += 3)
                {
                    if (i + 2 < sampleCount)
                    {
                        float avg = (incoming[i] + incoming[i + 1] + incoming[i + 2]) / (3.0f * 32768f);
                        _commandAudioBuffer.Add(avg);
                    }
                }
            }
        }

        lock (_bufLock)
        {
            // Re-check since lock could have waited after Stop()
            if (!_isRecording) return;

            int srcPos = 0;
            while (srcPos < sampleCount)
            {
                int needed = FrameSamples - _frameBufferPos;
                int available = sampleCount - srcPos;
                int toCopy = Math.Min(needed, available);

                incoming.Slice(srcPos, toCopy).CopyTo(_frameBuffer.AsSpan(_frameBufferPos));
                _frameBufferPos += toCopy;
                srcPos += toCopy;

                if (_frameBufferPos >= FrameSamples)
                {
                    ProcessFrame(_frameBuffer);
                    _frameBufferPos = 0;
                }
            }
        }
    }

    private void ProcessFrame(short[] pcmFrame)
    {
        bool isCommandRecording;
        lock (_commandAudioLock)
        {
            isCommandRecording = _isRecordingCommand;
        }

        if (isCommandRecording)
        {
            bool wasTx = IsTransmitting;
            IsTransmitting = false;
            if (wasTx)
            {
                EncodedFrameReady?.Invoke(new byte[0], _currentTxType);
            }
            return;
        }

        if (_encoder == null) return;
        if (Mode == AudioMode.VAD && _vad == null) return;

        bool shouldTransmit = false;
        byte txType = _currentTxType;

        if (App.ViewModel?.CurrentHailState == HailState.Connected)
        {
            txType = 0x04;
            // Hands-free voice transmission using VAD to avoid sending continuous silence
            var bytes = new byte[pcmFrame.Length * 2];
            Buffer.BlockCopy(pcmFrame, 0, bytes, 0, bytes.Length);
            try
            {
                shouldTransmit = _vad != null && _vad.HasSpeech(bytes, WebRtcVadSharp.SampleRate.Is48kHz, WebRtcVadSharp.FrameLength.Is20ms);
            }
            catch
            {
                shouldTransmit = true; // Fallback to open mic if VAD fails
            }
        }
        else if (txType == 0x00 && ProximityMuted)
        {
            shouldTransmit = false;
        }
        else if (txType == 0x01 && RadioMuted)
        {
            shouldTransmit = false;
        }
        else if (txType == 0x02 && ProfileMuted)
        {
            shouldTransmit = false;
        }
        else if (Mode == AudioMode.PTT || txType != 0x00)
        {
            shouldTransmit = _isPttActive;
        }
        else // VAD on proximity
        {
            var bytes = new byte[pcmFrame.Length * 2];
            Buffer.BlockCopy(pcmFrame, 0, bytes, 0, bytes.Length);
            try 
            {
                shouldTransmit = _vad!.HasSpeech(bytes, WebRtcVadSharp.SampleRate.Is48kHz, WebRtcVadSharp.FrameLength.Is20ms); 
            }
            catch (Exception ex)
            {
                LogService.Error("WebRtcVad HasSpeech error", ex);
                shouldTransmit = false; 
            }
            txType = 0x00;
        }

        bool wasTransmitting = IsTransmitting;
        IsTransmitting = shouldTransmit;

        if (!shouldTransmit)
        {
            if (wasTransmitting)
            {
                // Send one final empty frame to signal end of transmission
                EncodedFrameReady?.Invoke(new byte[0], txType);
            }
            return;
        }

        // Apply helmet respirator & suit hum overlay if visor is down and helmet modulator is enabled
        var config = App.ViewModel?.Config?.Config;
        bool isHelmetOn = App.ViewModel?.IsHelmetOn ?? false;
        bool enableHelmetMod = config?.EnableHelmetModulator ?? true;

        if (shouldTransmit)
        {
            float[] floatBuf = new float[FrameSamples];
            for (int i = 0; i < FrameSamples; i++)
            {
                floatBuf[i] = pcmFrame[i] / 32768f;
            }

            double gforce = App.ViewModel?.GForce ?? 0.0;
            double exertion = App.ViewModel?.Exertion ?? 0.0;
            bool applyExertionDist = config?.EnableExertionDistortion ?? false;

            for (int i = 0; i < FrameSamples; i++)
            {
                // 1. Suit hum (only if helmet is on)
                if (isHelmetOn && enableHelmetMod)
                {
                    double hum50 = Math.Sin(_humPhase50);
                    double hum100 = Math.Sin(_humPhase100);
                    _humPhase50 += 2.0 * Math.PI * 50.0 / SampleRate;
                    _humPhase100 += 2.0 * Math.PI * 100.0 / SampleRate;
                    if (_humPhase50 > 2.0 * Math.PI) _humPhase50 -= 2.0 * Math.PI;
                    if (_humPhase100 > 2.0 * Math.PI) _humPhase100 -= 2.0 * Math.PI;
                    floatBuf[i] += (float)(hum50 * 0.005 + hum100 * 0.0025);
                }

                // 2. Breathing overlay (either standard helmet breathing, or exertion heavy panting)
                if (applyExertionDist && exertion > 0.1)
                {
                    double cycleExertion = (4.5 - exertion * 2.5) * SampleRate;
                    double exertionBreathAmp = exertion * 0.08;
                    double phaseEx = _exertionBreathPhase % cycleExertion;
                    _exertionBreathPhase = (_exertionBreathPhase + 1) % cycleExertion;
                    double secEx = phaseEx / SampleRate;
                    double lenSecEx = cycleExertion / SampleRate;
                    double halfEx = lenSecEx * 0.4;
                    double exAmp = 0;
                    if (secEx < halfEx)
                    {
                        exAmp = Math.Sin((secEx / halfEx) * Math.PI);
                    }
                    else if (secEx >= halfEx + 0.2 && secEx < lenSecEx - 0.2)
                    {
                        exAmp = 0.75 * Math.Sin(((secEx - (halfEx + 0.2)) / (lenSecEx - halfEx - 0.4)) * Math.PI);
                    }
                    if (exAmp > 0)
                    {
                        float breathNoise = (float)(_random.NextDouble() * 2.0 - 1.0);
                        breathNoise = _breathHp.Process(breathNoise);
                        breathNoise = _breathLp.Process(breathNoise);
                        floatBuf[i] += (float)(breathNoise * exAmp * exertionBreathAmp);
                    }
                }
                else if (isHelmetOn && enableHelmetMod)
                {
                    double cycleSamples = 4.5 * SampleRate;
                    double phase = _breathPhase % cycleSamples;
                    _breathPhase = (_breathPhase + 1) % cycleSamples;
                    double seconds = phase / SampleRate;
                    double breathAmp = 0;
                    if (seconds < 1.8)
                    {
                        breathAmp = Math.Sin((seconds / 1.8) * Math.PI);
                    }
                    else if (seconds >= 2.3 && seconds < 4.0)
                    {
                        breathAmp = 0.75 * Math.Sin(((seconds - 2.3) / 1.7) * Math.PI);
                    }
                    if (breathAmp > 0)
                    {
                        float breathNoise = (float)(_random.NextDouble() * 2.0 - 1.0);
                        breathNoise = _breathHp.Process(breathNoise);
                        breathNoise = _breathLp.Process(breathNoise);
                        floatBuf[i] += (float)(breathNoise * breathAmp * 0.035f);
                    }
                }

                // 3. Tremolo voice distortion
                if (applyExertionDist && (gforce > 0.05 || exertion > 0.05))
                {
                    double tremoloDepth = Math.Max(gforce * 0.4, exertion * 0.3);
                    double tremoloRate = gforce > exertion ? 4.0 + gforce * 3.0 : 6.0 + exertion * 4.0;
                    double lfo = Math.Sin(_tremoloPhase);
                    _tremoloPhase += 2.0 * Math.PI * tremoloRate / SampleRate;
                    if (_tremoloPhase > 2.0 * Math.PI) _tremoloPhase -= 2.0 * Math.PI;
                    float mod = 1.0f - (float)(tremoloDepth * (0.5 * (lfo + 1.0)));
                    floatBuf[i] *= mod;
                }
            }

            // 4. Pitch shift (G-force compression)
            if (applyExertionDist && gforce > 0.05)
            {
                float gforcePitchFactor = 1.0f - (float)(gforce * 0.15f);
                _gforcePitchShifter.Process(floatBuf, FrameSamples, gforcePitchFactor);
            }

            // Apply Voice Changer effects if enabled
            if (config != null && config.EnableVoiceChanger)
            {
                _voiceModulator.Process(floatBuf, FrameSamples, config.VoiceChangerType, config.VoicePitchFactor);
            }

            // Convert back to PCM
            for (int i = 0; i < FrameSamples; i++)
            {
                pcmFrame[i] = (short)Math.Clamp(floatBuf[i] * 32768f, short.MinValue, short.MaxValue);
            }
        }

        // Encode with Opus
        var outBuf = new byte[4000];
        int encodedLen = _encoder!.Encode(pcmFrame, FrameSamples, outBuf, outBuf.Length);
        if (encodedLen <= 0) return;

        var frame = new byte[encodedLen];
        Buffer.BlockCopy(outBuf, 0, frame, 0, encodedLen);
        EncodedFrameReady?.Invoke(frame, txType);
    }

    public void Stop()
    {
        LogService.Info("Stopping AudioCaptureService...");

        lock (_bufLock)
        {
            _isRecording = false;
        }

        if (_waveIn != null)
        {
            try
            {
                _waveIn.StopRecording();
                _waveIn.Dispose();
                LogService.Info("AudioCaptureService recording stopped and disposed.");
            }
            catch (Exception ex)
            {
                LogService.Error("Error stopping WaveInEvent recording", ex);
            }
            _waveIn = null;
        }

        lock (_bufLock)
        {
            _encoder = null;
            _vad?.Dispose();
            _vad = null;
            InputLevel = 0;
            _frameBufferPos = 0;
        }
    }

    public void StartCommandRecording()
    {
        lock (_commandAudioLock)
        {
            _commandAudioBuffer.Clear();
            _isRecordingCommand = true;
        }
    }

    public float[] StopCommandRecording()
    {
        lock (_commandAudioLock)
        {
            _isRecordingCommand = false;
            var samples = _commandAudioBuffer.ToArray();
            _commandAudioBuffer.Clear();
            return samples;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
    }
}
