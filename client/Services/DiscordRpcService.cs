using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace XuruVoipClient.Services;

/// <summary>
/// A zero-dependency Discord Rich Presence (RPC) client using Windows named pipes.
/// </summary>
public class DiscordRpcService : IDisposable
{
    private const string ClientId = "1380922880024227911";
    private const string PipeName = "discord-ipc-0";
    
    private NamedPipeClientStream? _pipeStream;
    private readonly CancellationTokenSource _cts = new();
    private readonly object _lock = new();
    private bool _isConnected;
    private long _startTimestamp;
    private bool _enabled = true;
    
    private string? _currentState;
    private string? _currentDetails;

    public bool Enabled
    {
        get
        {
            lock (_lock) return _enabled;
        }
        set
        {
            lock (_lock)
            {
                if (_enabled == value) return;
                _enabled = value;
                if (!_enabled)
                {
                    _isConnected = false;
                    _pipeStream?.Dispose();
                    _pipeStream = null;
                }
            }
        }
    }

    public void Start()
    {
        _startTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        Task.Run(ConnectionLoop);
    }

    public void UpdatePresence(string? details, string? state)
    {
        lock (_lock)
        {
            _currentDetails = details;
            _currentState = state;
            if (!_enabled) return;
        }
        
        if (_isConnected)
        {
            SendPresence();
        }
    }

    private async Task ConnectionLoop()
    {
        while (!_cts.Token.IsCancellationRequested)
        {
            try
            {
                bool isEnabled;
                lock (_lock)
                {
                    isEnabled = _enabled;
                }

                if (isEnabled)
                {
                    if (_pipeStream == null || !_pipeStream.IsConnected)
                    {
                        _isConnected = false;
                        _pipeStream?.Dispose();
                        _pipeStream = null;

                        // Try to connect to any active Discord IPC pipe index (0 to 9)
                        for (int i = 0; i < 10; i++)
                        {
                            try
                            {
                                var stream = new NamedPipeClientStream(".", $"discord-ipc-{i}", PipeDirection.InOut, PipeOptions.Asynchronous);
                                await stream.ConnectAsync(500, _cts.Token); // 500ms timeout per pipe attempt
                                _pipeStream = stream;

                                if (SendHandshake())
                                {
                                    byte[] response = await ReadFrameAsync(_cts.Token);
                                    if (response.Length > 0)
                                    {
                                        _isConnected = true;
                                        SendPresence();
                                        break; // Successfully connected and authenticated
                                    }
                                }
                            }
                            catch
                            {
                                // Clean up and try the next pipe index
                                _pipeStream?.Dispose();
                                _pipeStream = null;
                            }
                        }
                    }
                }
                else
                {
                    if (_pipeStream != null)
                    {
                        _isConnected = false;
                        _pipeStream.Dispose();
                        _pipeStream = null;
                    }
                }

                await Task.Delay(5000, _cts.Token);
            }
            catch (Exception)
            {
                _isConnected = false;
                _pipeStream?.Dispose();
                _pipeStream = null;
                
                try
                {
                    await Task.Delay(5000, _cts.Token);
                }
                catch
                {
                    break;
                }
            }
        }
    }

    private bool SendHandshake()
    {
        if (_pipeStream == null || !_pipeStream.IsConnected) return false;
        
        var payload = JsonSerializer.Serialize(new
        {
            v = 1,
            client_id = ClientId
        });

        return WriteFrame(0, payload);
    }

    private void SendPresence()
    {
        string? details;
        string? state;
        lock (_lock)
        {
            details = _currentDetails;
            state = _currentState;
        }

        var activityPayload = new
        {
            cmd = "SET_ACTIVITY",
            args = new
            {
                pid = Environment.ProcessId,
                activity = new
                {
                    state = state ?? "Idle",
                    details = details ?? "Disconnected",
                    timestamps = new
                    {
                        start = _startTimestamp
                    },
                    assets = new
                    {
                        large_image = "logo",
                        large_text = "XuruVOIP Suite"
                    }
                }
            },
            nonce = Guid.NewGuid().ToString()
        };

        var payload = JsonSerializer.Serialize(activityPayload);
        WriteFrame(1, payload);
    }

    private bool WriteFrame(int opcode, string json)
    {
        try
        {
            if (_pipeStream == null || !_pipeStream.IsConnected) return false;
            
            byte[] jsonBytes = Encoding.UTF8.GetBytes(json);
            byte[] header = new byte[8];
            
            header[0] = (byte)(opcode & 0xFF);
            header[1] = (byte)((opcode >> 8) & 0xFF);
            header[2] = (byte)((opcode >> 16) & 0xFF);
            header[3] = (byte)((opcode >> 24) & 0xFF);
            
            int len = jsonBytes.Length;
            header[4] = (byte)(len & 0xFF);
            header[5] = (byte)((len >> 8) & 0xFF);
            header[6] = (byte)((len >> 16) & 0xFF);
            header[7] = (byte)((len >> 24) & 0xFF);
            
            _pipeStream.Write(header, 0, header.Length);
            _pipeStream.Write(jsonBytes, 0, jsonBytes.Length);
            _pipeStream.Flush();
            return true;
        }
        catch
        {
            _isConnected = false;
            return false;
        }
    }

    private async Task<byte[]> ReadFrameAsync(CancellationToken token)
    {
        try
        {
            if (_pipeStream == null || !_pipeStream.IsConnected) return Array.Empty<byte>();
            
            byte[] header = new byte[8];
            int read = await _pipeStream.ReadAsync(header, 0, header.Length, token);
            if (read < header.Length) return Array.Empty<byte>();
            
            int len = BitConverter.ToInt32(header, 4);
            if (len <= 0 || len > 65536) return Array.Empty<byte>();
            
            byte[] payload = new byte[len];
            int totalRead = 0;
            while (totalRead < len)
            {
                int currentRead = await _pipeStream.ReadAsync(payload, totalRead, len - totalRead, token);
                if (currentRead <= 0) break;
                totalRead += currentRead;
            }
            return payload;
        }
        catch
        {
            return Array.Empty<byte>();
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
        _pipeStream?.Dispose();
        GC.SuppressFinalize(this);
    }
}
