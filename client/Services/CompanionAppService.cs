using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using XuruVoipClient.ViewModels;
using XuruVoipClient.Models;
using System.Windows;

namespace XuruVoipClient.Services;

public class CompanionAppService : IDisposable
{
    private HttpListener? _listener;
    private CancellationTokenSource? _cts;
    private readonly MainViewModel _viewModel;
    private bool _running;

    public int ActivePort { get; private set; }

    public CompanionAppService(MainViewModel viewModel)
    {
        _viewModel = viewModel;
    }

    public void Start()
    {
        if (_running) return;
        _running = true;
        _cts = new CancellationTokenSource();

        int port = _viewModel.Config.Config.CompanionAppPort;
        if (port <= 0 || port > 65535) port = 8891;
        ActivePort = port;

        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{port}/");
        _listener.Prefixes.Add($"http://127.0.0.1:{port}/");

        try
        {
            _listener.Start();
            LogService.Info($"Companion HTTP API Server started on http://localhost:{port}/");
            Task.Run(() => ListenAsync(_cts.Token));
        }
        catch (Exception ex)
        {
            LogService.Error("Failed to start Companion HTTP API Server", ex);
        }
    }

    public void Stop()
    {
        if (!_running) return;
        _running = false;
        _cts?.Cancel();
        try
        {
            _listener?.Stop();
            _listener?.Close();
        }
        catch { }
        _cts?.Dispose();
        _cts = null;
        _listener = null;
        LogService.Info("Companion HTTP API Server stopped.");
    }

    private async Task ListenAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _listener != null && _listener.IsListening)
        {
            try
            {
                var context = await _listener.GetContextAsync();
                _ = Task.Run(() => HandleRequestAsync(context), ct);
            }
            catch (Exception ex)
            {
                if (ct.IsCancellationRequested) break;
                LogService.Error("Error in HttpListener loop", ex);
            }
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        // CORS headers
        response.Headers.Add("Access-Control-Allow-Origin", "*");
        response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");
        response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");

        if (request.HttpMethod == "OPTIONS")
        {
            response.StatusCode = (int)HttpStatusCode.NoContent;
            response.Close();
            return;
        }

        try
        {
            string urlPath = request.Url?.AbsolutePath ?? "/";
            if (request.HttpMethod == "GET" && urlPath == "/")
            {
                // Serve Dashboard HTML page
                response.ContentType = "text/html; charset=utf-8";
                byte[] htmlBytes = Encoding.UTF8.GetBytes(HtmlDashboard);
                response.ContentLength64 = htmlBytes.Length;
                await response.OutputStream.WriteAsync(htmlBytes, 0, htmlBytes.Length);
            }
            else if (request.HttpMethod == "GET" && urlPath == "/api/status")
            {
                // Serve JSON status
                response.ContentType = "application/json";
                
                var remotePositions = new System.Collections.Generic.Dictionary<string, object>();
                object? localPos = null;
                double headingX = 0;
                double headingY = 1.0;

                bool mapEnabled = _viewModel.Config.Config.EnableCompanionMap;
                if (mapEnabled)
                {
                    bool copied = false;
                    if (Application.Current != null && !Application.Current.Dispatcher.CheckAccess())
                    {
                        try
                        {
                            var op = Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                foreach (var kvp in _viewModel.RemotePositions)
                                {
                                    remotePositions[kvp.Key] = new
                                    {
                                        x = kvp.Value.X,
                                        y = kvp.Value.Y,
                                        z = kvp.Value.Z,
                                        zone = kvp.Value.Zone,
                                        containerId = kvp.Value.ContainerID,
                                        containerName = kvp.Value.ContainerName
                                    };
                                }
                                var lp = _viewModel.LastSentPos;
                                if (lp != null && !lp.IsEmpty)
                                {
                                    localPos = new
                                    {
                                        x = lp.X,
                                        y = lp.Y,
                                        z = lp.Z,
                                        zone = lp.Zone,
                                        containerId = lp.ContainerID,
                                        containerName = lp.ContainerName
                                    };
                                }
                                headingX = _viewModel.Playback.ListenerHeadingX;
                                headingY = _viewModel.Playback.ListenerHeadingY;
                            });

                            if (op.Task.Wait(100))
                            {
                                copied = true;
                            }
                        }
                        catch
                        {
                            // Ignore and fallback
                        }
                    }

                    if (!copied)
                    {
                        try
                        {
                            foreach (var kvp in _viewModel.RemotePositions)
                            {
                                remotePositions[kvp.Key] = new
                                {
                                    x = kvp.Value.X,
                                    y = kvp.Value.Y,
                                    z = kvp.Value.Z,
                                    zone = kvp.Value.Zone,
                                    containerId = kvp.Value.ContainerID,
                                    containerName = kvp.Value.ContainerName
                                };
                            }
                            var lp = _viewModel.LastSentPos;
                            if (lp != null && !lp.IsEmpty)
                            {
                                localPos = new
                                {
                                    x = lp.X,
                                    y = lp.Y,
                                    z = lp.Z,
                                    zone = lp.Zone,
                                    containerId = lp.ContainerID,
                                    containerName = lp.ContainerName
                                };
                            }
                            headingX = _viewModel.Playback.ListenerHeadingX;
                            headingY = _viewModel.Playback.ListenerHeadingY;
                        }
                        catch
                        {
                            // If dictionary is modified during direct read, we just return whatever we got
                        }
                    }
                }

                var status = new
                {
                    username = _viewModel.Config.Config.Username,
                    activeChannel = _viewModel.ActiveChannelName,
                    availableChannels = _viewModel.AvailableChannels,
                    micProximityMuted = _viewModel.MicProximityMuted,
                    micRadioMuted = _viewModel.MicRadioMuted,
                    micProfileMuted = _viewModel.MicProfileMuted,
                    audioProximityMuted = _viewModel.AudioProximityMuted,
                    audioRadioMuted = _viewModel.AudioRadioMuted,
                    audioProfileMuted = _viewModel.AudioProfileMuted,
                    isHelmetOn = _viewModel.IsHelmetOn,
                    activeSpeakers = _viewModel.Playback.GetActiveSpeakers(400),
                    voiceChangerType = _viewModel.Config.Config.VoiceChangerType,
                    voiceChangerEnabled = _viewModel.Config.Config.EnableVoiceChanger,
                    posConnected = _viewModel.PosConnected,
                    audioConnected = _viewModel.AudioConnected,
                    gforce = _viewModel.GForce,
                    exertion = _viewModel.Exertion,
                    enableExertionDistortion = _viewModel.Config.Config.EnableExertionDistortion,
                    isRadioRepeater = _viewModel.Config.Config.IsRadioRepeater,
                    enableRadioRepeaters = _viewModel.Config.Config.EnableRadioRepeaters,
                    enableShipPa = _viewModel.Config.Config.EnableShipPa,
                    enableCompanionMap = mapEnabled,
                    localPos = mapEnabled ? localPos : null,
                    heading = mapEnabled ? new { x = headingX, y = headingY } : null,
                    remotePositions = mapEnabled ? remotePositions : null
                };

                string json = JsonSerializer.Serialize(status);
                byte[] jsonBytes = Encoding.UTF8.GetBytes(json);
                response.ContentLength64 = jsonBytes.Length;
                await response.OutputStream.WriteAsync(jsonBytes, 0, jsonBytes.Length);
            }
            else if (request.HttpMethod == "POST" && urlPath == "/api/action")
            {
                // Process JSON action
                using var reader = new StreamReader(request.InputStream);
                string body = await reader.ReadToEndAsync();
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                if (root.TryGetProperty("action", out var actionProp))
                {
                    string action = actionProp.GetString() ?? "";
                    await ExecuteActionAsync(action, root);
                }

                response.StatusCode = (int)HttpStatusCode.OK;
                response.ContentType = "application/json";
                byte[] okBytes = Encoding.UTF8.GetBytes("{\"status\":\"ok\"}");
                response.ContentLength64 = okBytes.Length;
                await response.OutputStream.WriteAsync(okBytes, 0, okBytes.Length);
            }
            else
            {
                response.StatusCode = (int)HttpStatusCode.NotFound;
            }
        }
        catch (Exception ex)
        {
            LogService.Error("Error handling companion API request", ex);
            response.StatusCode = (int)HttpStatusCode.InternalServerError;
        }
        finally
        {
            try
            {
                response.Close();
            }
            catch { }
        }
    }

    private async Task ExecuteActionAsync(string action, JsonElement root)
    {
        switch (action)
        {
            case "toggle_proximity_mute":
                _viewModel.MicProximityMuted = !_viewModel.MicProximityMuted;
                break;
            case "toggle_radio_mute":
                _viewModel.MicRadioMuted = !_viewModel.MicRadioMuted;
                break;
            case "toggle_profile_mute":
                _viewModel.MicProfileMuted = !_viewModel.MicProfileMuted;
                break;
            case "toggle_audio_proximity_mute":
                _viewModel.AudioProximityMuted = !_viewModel.AudioProximityMuted;
                break;
            case "toggle_audio_radio_mute":
                _viewModel.AudioRadioMuted = !_viewModel.AudioRadioMuted;
                break;
            case "toggle_audio_profile_mute":
                _viewModel.AudioProfileMuted = !_viewModel.AudioProfileMuted;
                break;
            case "toggle_helmet":
                _viewModel.ToggleHelmet();
                break;
            case "set_channel":
                if (root.TryGetProperty("channel", out var chanProp))
                {
                    string ch = chanProp.GetString() ?? "";
                    await _viewModel.ChangeRadioChannelAsync(ch);
                }
                break;
            case "set_voice_changer":
                if (root.TryGetProperty("type", out var typeProp))
                {
                    string type = typeProp.GetString() ?? "None";
                    _viewModel.Config.Config.EnableVoiceChanger = (type != "None");
                    _viewModel.Config.Config.VoiceChangerType = type;
                    _viewModel.SaveConfig();
                }
                break;
            case "set_exertion":
                if (root.TryGetProperty("gforce", out var gfProp) && gfProp.ValueKind == JsonValueKind.Number)
                {
                    _viewModel.GForce = gfProp.GetDouble();
                }
                if (root.TryGetProperty("exertion", out var exProp) && exProp.ValueKind == JsonValueKind.Number)
                {
                    _viewModel.Exertion = exProp.GetDouble();
                }
                break;
            case "toggle_exertion_distortion":
                _viewModel.Config.Config.EnableExertionDistortion = !_viewModel.Config.Config.EnableExertionDistortion;
                _viewModel.SaveConfig();
                break;
            case "toggle_repeater":
                _viewModel.Config.Config.IsRadioRepeater = !_viewModel.Config.Config.IsRadioRepeater;
                _viewModel.SaveConfig();
                _viewModel.ApplySettings();
                break;
            case "start_pa":
                _viewModel.SetMockPttPaState(true);
                break;
            case "stop_pa":
                _viewModel.SetMockPttPaState(false);
                break;
        }
    }

    public void Dispose()
    {
        Stop();
    }

    private const string HtmlDashboard = """
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>XuruVOIP Companion</title>
    <link href="https://fonts.googleapis.com/css2?family=Outfit:wght@400;600;800&display=swap" rel="stylesheet">
    <style>
        :root {
            --bg-color: #0b0c10;
            --card-bg: rgba(26, 26, 36, 0.45);
            --border-color: rgba(255, 255, 255, 0.08);
            --primary: #00f2fe;
            --primary-glow: rgba(0, 242, 254, 0.4);
            --accent: #4facfe;
            --danger: #ff4e6a;
            --danger-glow: rgba(255, 78, 106, 0.4);
            --text-color: #c5c6c7;
            --text-title: #ffffff;
            --green: #3ddb85;
            --green-glow: rgba(61, 219, 133, 0.4);
        }
        * {
            box-sizing: border-box;
            margin: 0;
            padding: 0;
            font-family: 'Outfit', sans-serif;
            -webkit-tap-highlight-color: transparent;
        }
        body {
            background-color: var(--bg-color);
            background-image: 
                radial-gradient(at 10% 20%, rgba(0, 242, 254, 0.08) 0px, transparent 50%),
                radial-gradient(at 90% 80%, rgba(79, 172, 254, 0.08) 0px, transparent 50%);
            color: var(--text-color);
            min-height: 100vh;
            display: flex;
            flex-direction: column;
            align-items: center;
            justify-content: center;
            padding: 20px;
        }
        .container {
            width: 100%;
            max-width: 480px;
            background: var(--card-bg);
            backdrop-filter: blur(20px);
            -webkit-backdrop-filter: blur(20px);
            border: 1px solid var(--border-color);
            border-radius: 24px;
            padding: 24px;
            box-shadow: 0 20px 40px rgba(0, 0, 0, 0.5);
        }
        h1 {
            color: var(--text-title);
            font-weight: 800;
            font-size: 24px;
            text-align: center;
            margin-bottom: 20px;
            letter-spacing: 1px;
            text-transform: uppercase;
            background: linear-gradient(135deg, var(--primary), var(--accent));
            -webkit-background-clip: text;
            -webkit-text-fill-color: transparent;
        }
        
        /* Tabs navigation */
        .tabs {
            display: flex;
            background: rgba(255, 255, 255, 0.03);
            border-radius: 14px;
            padding: 4px;
            margin-bottom: 20px;
            border: 1px solid rgba(255, 255, 255, 0.05);
            gap: 4px;
        }
        .tab {
            flex: 1;
            text-align: center;
            padding: 10px;
            font-size: 14px;
            font-weight: 600;
            cursor: pointer;
            border-radius: 10px;
            color: var(--text-color);
            transition: all 0.2s ease;
            display: flex;
            align-items: center;
            justify-content: center;
            gap: 6px;
        }
        .tab.active {
            background: rgba(0, 242, 254, 0.12);
            color: var(--primary);
            box-shadow: 0 0 15px rgba(0, 242, 254, 0.1);
        }
        .tab-view {
            display: none;
        }
        .tab-view.active {
            display: block;
        }

        .status-header {
            display: flex;
            justify-content: space-between;
            align-items: center;
            padding: 12px 16px;
            background: rgba(255, 255, 255, 0.03);
            border-radius: 12px;
            margin-bottom: 24px;
            border: 1px solid rgba(255, 255, 255, 0.03);
        }
        .user-info {
            display: flex;
            flex-direction: column;
        }
        .user-name {
            color: var(--text-title);
            font-weight: 600;
            font-size: 16px;
        }
        .connection-dot {
            width: 10px;
            height: 10px;
            border-radius: 50%;
            background-color: var(--danger);
            box-shadow: 0 0 10px var(--danger-glow);
            transition: all 0.3s ease;
        }
        .connection-dot.connected {
            background-color: var(--green);
            box-shadow: 0 0 10px var(--green-glow);
        }
        .section-title {
            font-size: 12px;
            font-weight: 800;
            color: rgba(255, 255, 255, 0.4);
            text-transform: uppercase;
            letter-spacing: 1.5px;
            margin-bottom: 12px;
        }
        .grid-toggles {
            display: grid;
            grid-template-columns: repeat(2, 1fr);
            gap: 16px;
            margin-bottom: 24px;
        }
        .btn {
            background: rgba(255, 255, 255, 0.04);
            border: 1px solid var(--border-color);
            border-radius: 16px;
            padding: 16px;
            color: var(--text-color);
            font-size: 14px;
            font-weight: 600;
            cursor: pointer;
            display: flex;
            flex-direction: column;
            align-items: center;
            justify-content: center;
            gap: 8px;
            transition: all 0.25s cubic-bezier(0.4, 0, 0.2, 1);
        }
        .btn:hover {
            background: rgba(255, 255, 255, 0.08);
            transform: translateY(-2px);
        }
        .btn:active {
            transform: translateY(0);
        }
        .btn.active {
            background: rgba(0, 242, 254, 0.1);
            border-color: var(--primary);
            color: var(--primary);
            box-shadow: 0 0 15px rgba(0, 242, 254, 0.15);
        }
        .btn.muted {
            background: rgba(255, 78, 106, 0.1);
            border-color: var(--danger);
            color: var(--danger);
            box-shadow: 0 0 15px rgba(255, 78, 106, 0.15);
        }
        .icon {
            font-size: 20px;
        }
        .controls-list {
            display: flex;
            flex-direction: column;
            gap: 16px;
            margin-bottom: 24px;
        }
        .control-row {
            display: flex;
            flex-direction: column;
            gap: 8px;
        }
        select {
            width: 100%;
            background: rgba(255, 255, 255, 0.04);
            border: 1px solid var(--border-color);
            border-radius: 12px;
            padding: 12px;
            color: var(--text-title);
            font-size: 14px;
            font-weight: 600;
            outline: none;
            cursor: pointer;
            appearance: none;
            background-image: url("data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='10' height='6' fill='none'><path stroke='white' stroke-width='1.5' d='M1 1l4 4 4-4'/></svg>");
            background-repeat: no-repeat;
            background-position: right 16px center;
        }
        select:focus {
            border-color: var(--primary);
            background-color: rgba(26, 26, 36, 0.95);
        }
        .speakers-card {
            background: rgba(255, 255, 255, 0.02);
            border: 1px solid var(--border-color);
            border-radius: 16px;
            padding: 16px;
            min-height: 80px;
        }
        .speaker-item {
            display: flex;
            align-items: center;
            gap: 8px;
            font-size: 14px;
            color: var(--text-title);
            padding: 6px 0;
            border-bottom: 1px solid rgba(255, 255, 255, 0.04);
        }
        .speaker-item:last-child {
            border-bottom: none;
        }
        .speaker-dot {
            width: 8px;
            height: 8px;
            border-radius: 50%;
            background-color: var(--green);
            box-shadow: 0 0 8px var(--green-glow);
            animation: pulse 1.5s infinite;
        }
        .no-speakers {
            font-size: 14px;
            color: rgba(255, 255, 255, 0.2);
            text-align: center;
            padding-top: 14px;
        }

        /* Tactical Map elements */
        .map-container {
            display: flex;
            flex-direction: column;
            align-items: center;
            gap: 16px;
        }
        .canvas-container {
            position: relative;
            width: 100%;
            aspect-ratio: 1;
            background: rgba(5, 7, 12, 0.8);
            border-radius: 20px;
            border: 1px solid rgba(0, 242, 254, 0.15);
            box-shadow: inset 0 0 25px rgba(0, 242, 254, 0.05), 0 10px 30px rgba(0, 0, 0, 0.4);
            overflow: hidden;
        }
        #radar-canvas {
            width: 100%;
            height: 100%;
            display: block;
        }
        .map-controls {
            width: 100%;
            display: flex;
            flex-direction: column;
            gap: 16px;
            background: rgba(255, 255, 255, 0.02);
            border: 1px solid var(--border-color);
            border-radius: 16px;
            padding: 16px;
        }
        .map-toggle-group {
            display: flex;
            gap: 8px;
            margin-top: 4px;
        }
        .map-toggle-btn {
            flex: 1;
            background: rgba(255, 255, 255, 0.03);
            border: 1px solid var(--border-color);
            padding: 10px;
            font-size: 11px;
            font-weight: 700;
            color: var(--text-color);
            border-radius: 10px;
            cursor: pointer;
            transition: all 0.2s ease;
            letter-spacing: 0.5px;
        }
        .map-toggle-btn.active {
            background: rgba(0, 242, 254, 0.1);
            border-color: var(--primary);
            color: var(--primary);
            box-shadow: 0 0 10px rgba(0, 242, 254, 0.1);
        }

        @keyframes pulse {
            0% { transform: scale(1); opacity: 1; }
            50% { transform: scale(1.2); opacity: 0.5; }
            100% { transform: scale(1); opacity: 1; }
        }
    </style>
</head>
<body>
    <div class="container">
        <h1>XuruVOIP Companion</h1>

        <!-- Tab Navigation -->
        <div class="tabs" id="tab-container" style="display: none;">
            <div class="tab active" id="tab-controls" onclick="switchTab('controls')">🎛️ Controls</div>
            <div class="tab" id="tab-map" onclick="switchTab('map')">🗺️ Tactical Map</div>
        </div>
        
        <div class="status-header">
            <div class="user-info">
                <span class="user-name" id="lbl-username">Player</span>
                <span class="section-title" style="margin-bottom:0; font-size:10px;" id="lbl-channel">General</span>
            </div>
            <div class="connection-dot" id="dot-connection"></div>
        </div>

        <!-- VIEW 1: CONTROLS -->
        <div id="view-controls" class="tab-view active">
            <div class="section-title">Microphone Control</div>
            <div class="grid-toggles">
                <button class="btn" id="btn-mute-prox" onclick="postAction('toggle_proximity_mute')">
                    <span class="icon">🎙️</span>
                    <span>Proximity</span>
                </button>
                <button class="btn" id="btn-mute-radio" onclick="postAction('toggle_radio_mute')">
                    <span class="icon">📻</span>
                    <span>Radio</span>
                </button>
            </div>

            <div class="section-title">Hardware & Systems</div>
            <div class="grid-toggles">
                <button class="btn" id="btn-helmet" onclick="postAction('toggle_helmet')">
                    <span class="icon">🪖</span>
                    <span>Visor Down</span>
                </button>
                <button class="btn" id="btn-mute-audio-prox" onclick="postAction('toggle_audio_proximity_mute')">
                    <span class="icon">🔊</span>
                    <span>Hear Prox</span>
                </button>
            </div>

            <div class="controls-list">
                <div class="control-row">
                    <div class="section-title">Active Radio Channel</div>
                    <select id="sel-channel" onchange="postAction('set_channel', { channel: this.value })">
                        <option value="">General</option>
                    </select>
                </div>
                <div class="control-row">
                    <div class="section-title">Voice Changer profile</div>
                    <select id="sel-voice" onchange="postAction('set_voice_changer', { type: this.value })">
                        <option value="None">None</option>
                        <option value="Alien">Alien</option>
                        <option value="Cyborg">Cyborg</option>
                        <option value="Robotic">Robotic</option>
                        <option value="PitchShift">PitchShift</option>
                    </select>
                </div>
                <div class="control-row" style="margin-top:12px;">
                    <div class="section-title">Immersive Distortion</div>
                    <button class="btn" id="btn-exertion-dist" onclick="postAction('toggle_exertion_distortion')" style="width:100%; margin-bottom:12px; flex-direction:row; padding:12px;">
                        <span class="icon">🎚️</span>
                        <span>Enable Exertion & G-Force</span>
                    </button>
                    <div style="display:flex; flex-direction:column; gap:12px;">
                        <div>
                            <label style="font-size:12px; color:rgba(255,255,255,0.6); display:flex; justify-content:space-between; margin-bottom:4px;">
                                <span>Mock G-Force:</span>
                                <span id="val-gforce" style="color:var(--primary); font-weight:600;">0.0G</span>
                            </label>
                            <input type="range" id="slide-gforce" min="0" max="1" step="0.05" value="0" style="width:100%; accent-color:var(--primary); cursor:pointer;" oninput="updateMockExertion()"/>
                        </div>
                        <div>
                            <label style="font-size:12px; color:rgba(255,255,255,0.6); display:flex; justify-content:space-between; margin-bottom:4px;">
                                <span>Mock Exertion:</span>
                                <span id="val-exertion" style="color:var(--primary); font-weight:600;">0.0</span>
                            </label>
                            <input type="range" id="slide-exertion" min="0" max="1" step="0.05" value="0" style="width:100%; accent-color:var(--primary); cursor:pointer;" oninput="updateMockExertion()"/>
                        </div>
                    </div>
                </div>
                <div class="control-row" style="margin-top:12px;">
                    <div class="section-title">Tactical Radio Relay</div>
                    <button class="btn" id="btn-repeater-mode" onclick="postAction('toggle_repeater')" style="width:100%; flex-direction:row; padding:12px;">
                        <span class="icon">📡</span>
                        <span>Beacon Mode (Repeater)</span>
                    </button>
                </div>
                <div class="control-row" style="margin-top:12px;">
                    <div class="section-title">Ship Public Address (PA)</div>
                    <button class="btn" id="btn-pa" style="width:100%; height:80px; font-size:18px; font-weight:800; text-transform:uppercase; letter-spacing:1px; background: rgba(0, 242, 254, 0.04); border-color: rgba(0, 242, 254, 0.2);" 
                            onmousedown="postAction('start_pa')" onmouseup="postAction('stop_pa')" 
                            ontouchstart="postAction('start_pa')" ontouchend="postAction('stop_pa')">
                        📢 PA Broadcast
                    </button>
                </div>
            </div>

            <div class="section-title">Active Speakers</div>
            <div class="speakers-card" id="list-speakers">
                <div class="no-speakers">No active transmissions</div>
            </div>
        </div>

        <!-- VIEW 2: TACTICAL MAP -->
        <div id="view-map" class="tab-view">
            <div class="map-container">
                <div class="canvas-container">
                    <canvas id="radar-canvas"></canvas>
                </div>
                
                <div class="map-controls">
                    <div class="control-row">
                        <label style="font-size:12px; color:rgba(255,255,255,0.6); display:flex; justify-content:space-between; margin-bottom:4px;">
                            <span>Radar Range:</span>
                            <span id="val-range" style="color:var(--primary); font-weight:600;">100m</span>
                        </label>
                        <input type="range" id="slide-range" min="10" max="1000" step="10" value="100" style="width:100%; accent-color:var(--primary); cursor:pointer;" oninput="updateRange(this.value)"/>
                    </div>
                    
                    <div class="control-row">
                        <span class="section-title">MFD Orientation</span>
                        <div class="map-toggle-group">
                            <button id="btn-orient-heading" class="map-toggle-btn active" onclick="setOrientation('heading')">HEADING-UP</button>
                            <button id="btn-orient-north" class="map-toggle-btn" onclick="setOrientation('north')">NORTH-UP</button>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <script>
        let isDraggingGForce = false;
        let isDraggingExertion = false;

        // Tactical Map state
        let currentRange = 100;
        let mapOrientation = 'heading';
        let localPos = null;
        let heading = { x: 0, y: 1 };
        let remotePositions = {};
        let activeSpeakers = [];

        function switchTab(tabId) {
            document.querySelectorAll('.tab').forEach(t => t.classList.remove('active'));
            document.querySelectorAll('.tab-view').forEach(v => v.classList.remove('active'));
            
            if (tabId === 'controls') {
                document.getElementById('tab-controls').classList.add('active');
                document.getElementById('view-controls').classList.add('active');
            } else if (tabId === 'map') {
                document.getElementById('tab-map').classList.add('active');
                document.getElementById('view-map').classList.add('active');
            }
        }

        function updateRange(val) {
            currentRange = parseInt(val);
            document.getElementById('val-range').textContent = val + 'm';
        }

        function setOrientation(mode) {
            mapOrientation = mode;
            document.getElementById('btn-orient-heading').classList.toggle('active', mode === 'heading');
            document.getElementById('btn-orient-north').classList.toggle('active', mode === 'north');
        }

        async function fetchStatus() {
            try {
                const res = await fetch('/api/status');
                if (!res.ok) return;
                const data = await res.json();
                
                // Update username and connection dot
                document.getElementById('lbl-username').textContent = data.username || 'Offline';
                document.getElementById('lbl-channel').textContent = data.activeChannel ? `Radio: ${data.activeChannel}` : 'Proximity Only';
                
                const dot = document.getElementById('dot-connection');
                if (data.posConnected && data.audioConnected) {
                    dot.className = 'connection-dot connected';
                } else {
                    dot.className = 'connection-dot';
                }

                // Update toggle buttons
                setButtonState('btn-mute-prox', data.micProximityMuted, true);
                setButtonState('btn-mute-radio', data.micRadioMuted, true);
                setButtonState('btn-mute-audio-prox', data.audioProximityMuted, true);
                setButtonState('btn-helmet', data.isHelmetOn, false);

                // Update Active Channel Dropdown
                const selChan = document.getElementById('sel-channel');
                const prevVal = selChan.value;
                selChan.innerHTML = '';
                if (data.availableChannels && data.availableChannels.length > 0) {
                    data.availableChannels.forEach(ch => {
                        const opt = document.createElement('option');
                        opt.value = ch;
                        opt.textContent = ch;
                        selChan.appendChild(opt);
                    });
                    selChan.value = data.activeChannel;
                } else {
                    const opt = document.createElement('option');
                    opt.value = '';
                    opt.textContent = 'Proximity Only';
                    selChan.appendChild(opt);
                }

                // Update Voice Changer Profile
                const selVoice = document.getElementById('sel-voice');
                selVoice.value = data.voiceChangerType || 'None';

                // Update voice distortion
                const btnEx = document.getElementById('btn-exertion-dist');
                if (data.enableExertionDistortion) {
                    btnEx.classList.add('active');
                } else {
                    btnEx.classList.remove('active');
                }

                // Update repeater mode
                const btnRep = document.getElementById('btn-repeater-mode');
                if (btnRep) {
                    if (data.isRadioRepeater) {
                        btnRep.classList.add('active');
                    } else {
                        btnRep.classList.remove('active');
                    }
                    if (data.enableRadioRepeaters) {
                        btnRep.parentElement.style.display = 'block';
                    } else {
                        btnRep.parentElement.style.display = 'none';
                    }
                }

                // Update PA button
                const btnPa = document.getElementById('btn-pa');
                if (btnPa) {
                    if (data.enableShipPa) {
                        btnPa.parentElement.style.display = 'block';
                    } else {
                        btnPa.parentElement.style.display = 'none';
                    }
                }
                
                // If not actively dragging, update mock sliders from status
                if (!isDraggingGForce) {
                    document.getElementById('slide-gforce').value = data.gforce;
                    document.getElementById('val-gforce').textContent = (data.gforce * 9.0).toFixed(1) + 'G';
                }
                if (!isDraggingExertion) {
                    document.getElementById('slide-exertion').value = data.exertion;
                    document.getElementById('val-exertion').textContent = data.exertion.toFixed(2);
                }

                // Update Active Speakers
                const list = document.getElementById('list-speakers');
                list.innerHTML = '';
                if (data.activeSpeakers && data.activeSpeakers.length > 0) {
                    data.activeSpeakers.forEach(spk => {
                        const item = document.createElement('div');
                        item.className = 'speaker-item';
                        item.innerHTML = `<span class="speaker-dot"></span><span>${spk}</span>`;
                        list.appendChild(item);
                    });
                } else {
                    list.innerHTML = '<div class="no-speakers">No active transmissions</div>';
                }

                // Update tab bar visibility and map data
                const tabContainer = document.getElementById('tab-container');
                if (data.enableCompanionMap) {
                    tabContainer.style.display = 'flex';
                } else {
                    tabContainer.style.display = 'none';
                    switchTab('controls');
                }

                localPos = data.localPos;
                heading = data.heading || { x: 0, y: 1 };
                remotePositions = data.remotePositions || {};
                activeSpeakers = data.activeSpeakers || [];

            } catch (err) {
                console.error(err);
            }
        }

        function setButtonState(id, isMutedOrActive, isMuteBtn) {
            const btn = document.getElementById(id);
            if (btn) {
                if (isMuteBtn) {
                    if (isMutedOrActive) {
                        btn.classList.add('muted');
                        btn.classList.remove('active');
                    } else {
                        btn.classList.add('active');
                        btn.classList.remove('muted');
                    }
                } else {
                    if (isMutedOrActive) {
                        btn.classList.add('active');
                    } else {
                        btn.classList.remove('active');
                    }
                }
            }
        }

        async function postAction(action, params = {}) {
            try {
                await fetch('/api/action', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ action, ...params })
                });
                setTimeout(fetchStatus, 50);
            } catch (err) {
                console.error(err);
            }
        }

        function updateMockExertion() {
            const gf = parseFloat(document.getElementById('slide-gforce').value);
            const ex = parseFloat(document.getElementById('slide-exertion').value);
            document.getElementById('val-gforce').textContent = (gf * 9.0).toFixed(1) + 'G';
            document.getElementById('val-exertion').textContent = ex.toFixed(2);
            postAction('set_exertion', { gforce: gf, exertion: ex });
        }

        // Radar Canvas drawing loop
        function initRadar() {
            const canvas = document.getElementById('radar-canvas');
            if (!canvas) return;
            const ctx = canvas.getContext('2d');
            
            function draw() {
                const dpr = window.devicePixelRatio || 1;
                const rect = canvas.getBoundingClientRect();
                canvas.width = rect.width * dpr;
                canvas.height = rect.height * dpr;
                ctx.scale(dpr, dpr);

                const width = rect.width;
                const height = rect.height;
                const cx = width / 2;
                const cy = height / 2;
                const radius = Math.min(width, height) / 2 - 20;

                ctx.clearRect(0, 0, width, height);

                // 1. Draw static background grid (rings)
                ctx.strokeStyle = 'rgba(0, 242, 254, 0.08)';
                ctx.lineWidth = 1;
                for (let r = 0.25; r <= 1.00; r += 0.25) {
                    ctx.beginPath();
                    ctx.arc(cx, cy, radius * r, 0, Math.PI * 2);
                    ctx.stroke();
                    
                    // Draw range label
                    ctx.fillStyle = 'rgba(0, 242, 254, 0.3)';
                    ctx.font = '8px monospace';
                    ctx.textBaseline = 'middle';
                    ctx.textAlign = 'center';
                    ctx.fillText(`${Math.round(currentRange * r)}m`, cx, cy - radius * r + 10);
                }

                // Calculate heading angle
                let theta = Math.atan2(-heading.y, heading.x); // screen angle: North is Y+ (canvas Y-)

                // 2. Rotatable elements
                ctx.save();
                if (mapOrientation === 'heading') {
                    ctx.translate(cx, cy);
                    ctx.rotate(-theta - Math.PI / 2);
                    ctx.translate(-cx, -cy);
                }

                // Draw crosshairs (radial axes)
                ctx.strokeStyle = 'rgba(0, 242, 254, 0.05)';
                ctx.beginPath();
                ctx.moveTo(cx - radius, cy); ctx.lineTo(cx + radius, cy);
                ctx.moveTo(cx, cy - radius); ctx.lineTo(cx, cy + radius);
                ctx.stroke();

                // Draw compass letters
                ctx.fillStyle = 'rgba(0, 242, 254, 0.5)';
                ctx.font = 'bold 10px Outfit';
                ctx.textAlign = 'center';
                ctx.textBaseline = 'middle';
                ctx.fillText('N', cx, cy - radius + 8);
                ctx.fillText('S', cx, cy + radius - 8);
                ctx.fillText('E', cx + radius - 8, cy);
                ctx.fillText('W', cx - radius + 8, cy);

                // Draw remote players
                if (localPos && remotePositions) {
                    const scale = radius / currentRange;
                    Object.keys(remotePositions).forEach(name => {
                        const rp = remotePositions[name];
                        // Check if in same zone
                        if (rp.zone !== localPos.zone) return;
                        
                        const dx = rp.x - localPos.x;
                        const dy = rp.y - localPos.y;
                        const dist = Math.sqrt(dx*dx + dy*dy);
                        if (dist > currentRange) return; // out of range

                        // Screen coordinates for remote player
                        const rx = cx + dx * scale;
                        const ry = cy - dy * scale; // negative because game Y+ is North, canvas Y+ is South

                        // Check if speaker is active
                        const isSpeaking = activeSpeakers.includes(name);

                        // Draw speaker waves if speaking
                        if (isSpeaking) {
                            const t = (Date.now() / 400) % 1.0;
                            ctx.strokeStyle = `rgba(61, 219, 133, ${1.0 - t})`;
                            ctx.lineWidth = 1.5;
                            ctx.beginPath();
                            ctx.arc(rx, ry, 6 + t * 12, 0, Math.PI * 2);
                            ctx.stroke();
                        }

                        // Draw contact marker
                        ctx.beginPath();
                        ctx.arc(rx, ry, 4, 0, Math.PI * 2);
                        ctx.fillStyle = isSpeaking ? 'var(--green)' : 'var(--primary)';
                        ctx.fill();
                        ctx.strokeStyle = '#ffffff';
                        ctx.lineWidth = 1;
                        ctx.stroke();

                        // Draw label
                        ctx.fillStyle = '#ffffff';
                        ctx.font = '9px Outfit';
                        ctx.textAlign = 'left';
                        ctx.textBaseline = 'middle';
                        ctx.fillText(`${name} (${Math.round(dist)}m)`, rx + 8, ry);
                    });
                }

                ctx.restore();

                // 3. Draw sweep line (in screen space)
                let sweepAngle = (Date.now() / 2500) % (Math.PI * 2);
                let trailSteps = 20;
                let arcSize = 40 * Math.PI / 180;
                for (let i = 0; i < trailSteps; i++) {
                    let alpha = (i / trailSteps) * 0.08;
                    let angle = sweepAngle - (trailSteps - i) * (arcSize / trailSteps);
                    ctx.beginPath();
                    ctx.moveTo(cx, cy);
                    ctx.arc(cx, cy, radius, angle, angle + (arcSize / trailSteps));
                    ctx.closePath();
                    ctx.fillStyle = `rgba(0, 242, 254, ${alpha})`;
                    ctx.fill();
                }
                ctx.beginPath();
                ctx.moveTo(cx, cy);
                ctx.lineTo(cx + Math.cos(sweepAngle) * radius, cy + Math.sin(sweepAngle) * radius);
                ctx.strokeStyle = 'rgba(0, 242, 254, 0.25)';
                ctx.lineWidth = 1;
                ctx.stroke();

                // 4. Draw local player at center
                ctx.save();
                ctx.translate(cx, cy);
                if (mapOrientation === 'north') {
                    ctx.rotate(theta + Math.PI / 2);
                }
                ctx.beginPath();
                ctx.moveTo(0, -8);
                ctx.lineTo(-6, 6);
                ctx.lineTo(0, 3);
                ctx.lineTo(6, 6);
                ctx.closePath();
                ctx.fillStyle = 'var(--primary)';
                ctx.fill();
                ctx.strokeStyle = '#ffffff';
                ctx.lineWidth = 1;
                ctx.stroke();
                ctx.restore();

                // 5. HUD Text Overlay (Zone name, Coordinates, etc.)
                ctx.fillStyle = 'rgba(255, 255, 255, 0.4)';
                ctx.font = '9px monospace';
                ctx.textAlign = 'left';
                ctx.textBaseline = 'top';
                if (localPos) {
                    const truncatedZone = localPos.zone ? (localPos.zone.length > 18 ? localPos.zone.substring(0,18) + '...' : localPos.zone) : 'SYSTEM';
                    ctx.fillText(`ZONE: ${truncatedZone.toUpperCase()}`, 15, 20);
                    ctx.fillText(`POS: ${Math.round(localPos.x)}, ${Math.round(localPos.y)}, ${Math.round(localPos.z)}`, 15, 32);
                } else {
                    ctx.fillText('SYS: NO POSITION DATA', 15, 20);
                }
                
                ctx.textAlign = 'right';
                ctx.fillText(`RANGE: ${currentRange}M`, width - 15, 20);
                ctx.fillText(`MODE: ${mapOrientation.toUpperCase()}-UP`, width - 15, 32);

                requestAnimationFrame(draw);
            }
            
            requestAnimationFrame(draw);
        }

        // Add event listeners for dragging
        document.getElementById('slide-gforce').addEventListener('mousedown', () => isDraggingGForce = true);
        document.getElementById('slide-gforce').addEventListener('mouseup', () => { isDraggingGForce = false; updateMockExertion(); });
        document.getElementById('slide-gforce').addEventListener('touchstart', () => isDraggingGForce = true);
        document.getElementById('slide-gforce').addEventListener('touchend', () => { isDraggingGForce = false; updateMockExertion(); });

        document.getElementById('slide-exertion').addEventListener('mousedown', () => isDraggingExertion = true);
        document.getElementById('slide-exertion').addEventListener('mouseup', () => { isDraggingExertion = false; updateMockExertion(); });
        document.getElementById('slide-exertion').addEventListener('touchstart', () => isDraggingExertion = true);
        document.getElementById('slide-exertion').addEventListener('touchend', () => { isDraggingExertion = false; updateMockExertion(); });

        // Poll every 500ms
        setInterval(fetchStatus, 500);
        fetchStatus();
        initRadar();
    </script>
</body>
</html>
""";
}
