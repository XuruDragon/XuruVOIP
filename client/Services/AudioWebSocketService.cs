using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace XuruVoipClient.Services;

// ─── Protocol (matches audio_server.go) ──────────────────────────────────────
// SEND:    [AudioType (1 byte)] + [Opus frame data...]
// RECEIVE: [AudioType (1 byte)] + [NameLen (1 byte)] + [Name (UTF-8)] + [Opus data...]
// AUTH:    First message is JSON { "type":"join", "name":..., "token":..., "audio_ticket":... }

file record AudioJoin(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("token")] string Token,
    [property: JsonPropertyName("audio_ticket")] string AudioTicket);

public class ProximityMetadata
{
    public bool SpatialEnabled { get; set; }
    public float Distance { get; set; }
    public float MaxRange { get; set; }
    public float SpeakerX { get; set; }
    public float SpeakerY { get; set; }
    public float SpeakerZ { get; set; }
}

/// <summary>
/// Manages the binary WebSocket connection to the audio server (port 8889).
/// Sends encoded Opus frames and receives frames for decoding/playback.
/// </summary>
public class AudioWebSocketService : IAsyncDisposable
{
    private ClientWebSocket? _ws;
    private CancellationTokenSource? _cts;
    private Task? _receiveTask;
    private readonly SemaphoreSlim _sendLock = new(1, 1);

    /// <summary>
    /// Fired when an audio packet is received: (senderName, audioType, opusData, metadata)
    /// </summary>
    public event Action<string, byte, byte[], ProximityMetadata?>? AudioPacketReceived;
    public event Action? Connected;
    public event Action<string>? Disconnected;

    public bool IsConnected => _ws?.State == WebSocketState.Open;

    public async Task<bool> ConnectAsync(string serverAddress, int audioPort, string name, string token, string audioTicket)
    {
        LogService.Info($"Initiating connection to Audio Server for user '{name}'...");
        Disconnect();
        _cts = new CancellationTokenSource();
        _ws = new ClientWebSocket();

        // Bypass certificate validation for self-signed certificates
        _ws.Options.RemoteCertificateValidationCallback = (sender, cert, chain, errors) => true;

        try
        {
            string serverAddr = serverAddress.Trim();
            if (!serverAddr.StartsWith("ws://") && !serverAddr.StartsWith("wss://"))
            {
                serverAddr = "wss://" + serverAddr;
            }
            var baseUri = new Uri(serverAddr);
            var audioUri = new UriBuilder(baseUri) { Port = audioPort }.Uri;

            LogService.Info($"Connecting to Audio Server endpoint: {audioUri}");
            await _ws.ConnectAsync(audioUri, _cts.Token);
            LogService.Info("WebSocket connected to Audio Server. Sending join authentication frame...");

            // Send JSON auth message first
            var join = new AudioJoin("join", name, token, audioTicket);
            var json = JsonSerializer.Serialize(join);
            var bytes = Encoding.UTF8.GetBytes(json);
            await _ws.SendAsync(bytes, WebSocketMessageType.Text, true, _cts.Token);

            LogService.Info("Successfully authenticated with Audio Server. Starting receive loop...");
            Connected?.Invoke();
            _receiveTask = ReceiveLoopAsync(_cts.Token);
            return true;
        }
        catch (Exception ex)
        {
            LogService.Error("Failed to connect or authenticate with Audio Server", ex);
            Disconnected?.Invoke(ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Sends an Opus-encoded audio frame.
    /// Packet format: [audioType (1 byte)] + [opusData...]
    /// </summary>
    public async Task SendAudioFrameAsync(byte audioType, byte[] opusData)
    {
        if (_ws?.State != WebSocketState.Open) return;

        var packet = new byte[1 + opusData.Length];
        packet[0] = audioType;
        Buffer.BlockCopy(opusData, 0, packet, 1, opusData.Length);

        await _sendLock.WaitAsync();
        try
        {
            await _ws.SendAsync(packet, WebSocketMessageType.Binary, true, CancellationToken.None);
        }
        catch (Exception ex)
        {
            LogService.Error("Error sending audio frame to Audio Server", ex);
        }
        finally { _sendLock.Release(); }
    }

    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        var buf = new byte[16384];
        try
        {
            while (!ct.IsCancellationRequested && _ws?.State == WebSocketState.Open)
            {
                var result = await _ws.ReceiveAsync(buf, ct);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    LogService.Info("Audio Server closed connection via Close frame.");
                    Disconnected?.Invoke("Server closed audio connection");
                    return;
                }

                if (result.MessageType == WebSocketMessageType.Binary && result.Count >= 3)
                {
                    // Packet: [audioType(1)] + [nameLen(1)] + [name(nameLen)] + metadata + [opusData]
                    byte audioType = buf[0];
                    int nameLen = buf[1];
                    if (result.Count < 2 + nameLen) continue;

                    string senderName = Encoding.UTF8.GetString(buf, 2, nameLen);
                    int offset = 2 + nameLen;

                    ProximityMetadata? metadata = null;
                    int opusStart = offset;

                    if (audioType == 0x00) // Proximity
                    {
                        // Needs at least: spatialEnabled(1) + distance(4) + maxRange(4) = 9 bytes
                        if (result.Count < offset + 9) continue;

                        byte spatialEnabledByte = buf[offset];
                        bool spatialEnabled = spatialEnabledByte != 0;
                        float distance = BitConverter.ToSingle(buf, offset + 1);
                        float maxRange = BitConverter.ToSingle(buf, offset + 5);

                        metadata = new ProximityMetadata
                        {
                            SpatialEnabled = spatialEnabled,
                            Distance = distance,
                            MaxRange = maxRange
                        };

                        if (spatialEnabled)
                        {
                            if (result.Count < offset + 21) continue;
                            metadata.SpeakerX = BitConverter.ToSingle(buf, offset + 9);
                            metadata.SpeakerY = BitConverter.ToSingle(buf, offset + 13);
                            metadata.SpeakerZ = BitConverter.ToSingle(buf, offset + 17);
                            opusStart = offset + 21;
                        }
                        else
                        {
                            opusStart = offset + 9;
                        }
                    }

                    int opusLen = result.Count - opusStart;
                    if (opusLen <= 0) continue;

                    var opusData = new byte[opusLen];
                    Buffer.BlockCopy(buf, opusStart, opusData, 0, opusLen);

                    AudioPacketReceived?.Invoke(senderName, audioType, opusData, metadata);
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            LogService.Error("Exception in Audio Server ReceiveLoop", ex);
            Disconnected?.Invoke(ex.Message);
        }
    }

    public void Disconnect()
    {
        if (_ws != null)
        {
            LogService.Info("Disconnecting Audio Server client...");
        }
        _cts?.Cancel();
        _ws?.Dispose();
        _ws = null;
    }

    public async ValueTask DisposeAsync()
    {
        _cts?.Cancel();
        if (_receiveTask != null) await _receiveTask.ConfigureAwait(false);
        _ws?.Dispose();
    }
}
