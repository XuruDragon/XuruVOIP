using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using XuruVoipClient.Models;

namespace XuruVoipClient.Services;

// ─── Protocol types matching server types.go ───────────────────────────────

file record MsgJoin(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("token")] string Token,
    [property: JsonPropertyName("password")] string Password,
    [property: JsonPropertyName("hwid")] string Hwid);

file record MsgPos(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("pos")] PosPayload Pos,
    [property: JsonPropertyName("ts_capture")] double TsCapture);

file record PosPayload(
    [property: JsonPropertyName("x")] double X,
    [property: JsonPropertyName("y")] double Y,
    [property: JsonPropertyName("z")] double Z,
    [property: JsonPropertyName("zone")] string Zone);

file record MsgBase([property: JsonPropertyName("type")] string Type);

/// <summary>
/// Manages the WebSocket connection to the position server (port 8888).
/// Handles authentication, audio ticket retrieval and continuous position sending.
/// </summary>
public class PositionWebSocketService : IAsyncDisposable
{
    private ClientWebSocket? _ws;
    private CancellationTokenSource? _cts;
    private Task? _receiveTask;
    private Task? _pingTask;
    private readonly SemaphoreSlim _sendLock = new(1, 1);

    public bool IsConnected => _ws?.State == WebSocketState.Open;
    public string? AudioTicket { get; private set; }
    public string? WelcomeError { get; private set; }
    public string? WelcomeErrorReason { get; private set; }
    public bool IsSpatialAudioSupportedByServer { get; private set; }

    public event Action<string>? ServerMessage;  // raw JSON from server
    public event Action? Connected;
    public event Action<string>? Disconnected;
    public event Action<List<string>, string>? WelcomeReceived;

    public async Task<bool> ConnectAsync(AppConfig config)
    {
        LogService.Info($"Initiating connection to Position Server for user '{config.Username}'...");
        Disconnect();
        WelcomeError = null;
        WelcomeErrorReason = null;
        _cts = new CancellationTokenSource();
        _ws = new ClientWebSocket();

        // Bypass certificate validation for self-signed certificates
        _ws.Options.RemoteCertificateValidationCallback = (sender, cert, chain, errors) => true;

        try
        {
            // Build position server URI
            string serverAddr = config.ServerAddress.Trim();
            if (!serverAddr.StartsWith("ws://") && !serverAddr.StartsWith("wss://"))
            {
                serverAddr = "wss://" + serverAddr;
            }
            var baseUri = new Uri(serverAddr);
            var posUri = new UriBuilder(baseUri) { Port = config.PositionPort }.Uri;

            LogService.Info($"Connecting to Position Server endpoint: {posUri}");
            await _ws.ConnectAsync(posUri, _cts.Token);
            LogService.Info("WebSocket connected to Position Server. Sending join authentication frame...");

            // Send join message
            var join = new MsgJoin("join", config.Username, config.ServerPassword, config.UserPassword, config.Hwid);
            await SendJsonAsync(join);

            // Wait for welcome (with audio_ticket) — timeout 5s
            LogService.Info("Awaiting welcome frame and audio ticket...");
            using var timeout = new CancellationTokenSource(5000);
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, timeout.Token);

            AudioTicket = await ReceiveTicketAsync(linked.Token);

            if (AudioTicket == null)
            {
                WelcomeError = "Server did not provide an audio ticket.";
                LogService.Error("Authentication failed: Server did not return an audio ticket.");
                return false;
            }

            LogService.Info("Successfully authenticated with Position Server. Starting loops...");
            Connected?.Invoke();
            _receiveTask = ReceiveLoopAsync(_cts.Token);
            _pingTask = PingLoopAsync(_cts.Token);
            return true;
        }
        catch (Exception ex)
        {
            WelcomeError = ex.Message;
            LogService.Error("Failed to connect or authenticate with Position Server", ex);
            return false;
        }
    }

    public async Task SendPositionAsync(PlayerPosition pos)
    {
        if (!IsConnected) return;
        var msg = new MsgPos("pos",
            new PosPayload(pos.X, pos.Y, pos.Z, pos.Zone),
            pos.TsCapture);
        await SendJsonAsync(msg);
    }

    public async Task SetChannelAsync(string channel)
    {
        if (!IsConnected) return;
        LogService.Info($"Changing active radio channel to: {channel}");
        await SendJsonAsync(new { type = "set_channel", channel });
    }

    public async Task SetHelmetAsync(bool helmetOn)
    {
        if (!IsConnected) return;
        LogService.Info($"Sending helmet status update: equipped={helmetOn}");
        await SendJsonAsync(new { type = "helmet", helmet_on = helmetOn });
    }

    public async Task SetScOnlineAsync(bool online)
    {
        if (!IsConnected) return;
        LogService.Info($"Sending Star Citizen status update: online={online}");
        await SendJsonAsync(new { type = online ? "sc_online" : "sc_offline" });
    }

    public void Disconnect()
    {
        if (_ws != null)
        {
            LogService.Info("Disconnecting Position Server client...");
        }
        _cts?.Cancel();
        _ws?.Dispose();
        _ws = null;
        AudioTicket = null;
    }

    private async Task<string?> ReceiveTicketAsync(CancellationToken ct)
    {
        var buf = new byte[4096];
        while (!ct.IsCancellationRequested)
        {
            var result = await _ws!.ReceiveAsync(buf, ct);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                LogService.Info("Received Close frame from Position Server during authentication.");
                return null;
            }

            var json = Encoding.UTF8.GetString(buf, 0, result.Count);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("type", out var typeEl) &&
                typeEl.GetString() == "welcome")
            {
                var channelsList = new List<string>();
                if (doc.RootElement.TryGetProperty("channels", out var chanEl) && chanEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in chanEl.EnumerateArray())
                    {
                        var s = item.GetString();
                        if (s != null) channelsList.Add(s);
                    }
                }
                string activeChan = "";
                if (doc.RootElement.TryGetProperty("my_active_channel", out var actEl))
                {
                    activeChan = actEl.GetString() ?? "";
                }

                bool spatialSupported = false;
                if (doc.RootElement.TryGetProperty("spatial_audio_supported", out var spatialEl))
                {
                    spatialSupported = spatialEl.GetBoolean();
                }
                IsSpatialAudioSupportedByServer = spatialSupported;

                WelcomeReceived?.Invoke(channelsList, activeChan);

                if (doc.RootElement.TryGetProperty("audio_ticket", out var ticketEl))
                    return ticketEl.GetString();
            }
            else if (doc.RootElement.TryGetProperty("type", out var errType) &&
                     errType.GetString() == "error")
            {
                if (doc.RootElement.TryGetProperty("message", out var msgEl))
                    WelcomeError = msgEl.GetString();
                if (doc.RootElement.TryGetProperty("reason", out var reasonEl))
                    WelcomeErrorReason = reasonEl.GetString();
                LogService.Error($"Welcome error received: {WelcomeError} (reason: {WelcomeErrorReason})");
                return null;
            }
        }
        return null;
    }

    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        var buf = new byte[8192];
        try
        {
            while (!ct.IsCancellationRequested && _ws?.State == WebSocketState.Open)
            {
                var result = await _ws.ReceiveAsync(buf, ct);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    LogService.Info("Position Server closed connection via Close frame.");
                    Disconnected?.Invoke("Server closed connection");
                    return;
                }
                var json = Encoding.UTF8.GetString(buf, 0, result.Count);
                ServerMessage?.Invoke(json);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            LogService.Error("Exception in Position Server ReceiveLoop", ex);
            Disconnected?.Invoke(ex.Message);
        }
    }

    private async Task SendJsonAsync<T>(T msg)
    {
        if (_ws?.State != WebSocketState.Open) return;
        await _sendLock.WaitAsync();
        try
        {
            var json = JsonSerializer.Serialize(msg);
            var bytes = Encoding.UTF8.GetBytes(json);
            await _ws.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
        }
        catch (Exception ex)
        {
            LogService.Error("Error sending JSON message to Position Server", ex);
        }
        finally { _sendLock.Release(); }
    }

    public async ValueTask DisposeAsync()
    {
        _cts?.Cancel();
        if (_receiveTask != null) await _receiveTask.ConfigureAwait(false);
        if (_pingTask != null) await _pingTask.ConfigureAwait(false);
        _ws?.Dispose();
    }

    private async Task PingLoopAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested && IsConnected)
            {
                await Task.Delay(10000, ct); // Ping every 10 seconds
                if (IsConnected)
                {
                    await SendJsonAsync(new MsgBase("ping"));
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            LogService.Error("Exception in Position Server PingLoop", ex);
        }
    }
}
