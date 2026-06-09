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

    // Rolling buffer to accumulate exactly one Opus frame worth of PCM
    private readonly short[] _frameBuffer = new short[FrameSamples];
    private int _frameBufferPos = 0;
    private readonly object _bufLock = new();

    private bool _isPttActive = false;
    private byte _currentTxType = 0x00;
    private bool _disposed;
    private volatile bool _isRecording = false;

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
        if (_encoder == null) return;
        if (Mode == AudioMode.VAD && _vad == null) return;

        bool shouldTransmit = false;
        byte txType = _currentTxType;

        if (txType == 0x00 && ProximityMuted)
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

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
    }
}
