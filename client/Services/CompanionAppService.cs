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
                    audioConnected = _viewModel.AudioConnected
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
        
        <div class="status-header">
            <div class="user-info">
                <span class="user-name" id="lbl-username">Player</span>
                <span class="section-title" style="margin-bottom:0; font-size:10px;" id="lbl-channel">General</span>
            </div>
            <div class="connection-dot" id="dot-connection"></div>
        </div>

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
        </div>

        <div class="section-title">Active Speakers</div>
        <div class="speakers-card" id="list-speakers">
            <div class="no-speakers">No active transmissions</div>
        </div>
    </div>

    <script>
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
            } catch (err) {
                console.error(err);
            }
        }

        function setButtonState(id, isMutedOrActive, isMuteBtn) {
            const btn = document.getElementById(id);
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

        // Poll every 500ms
        setInterval(fetchStatus, 500);
        fetchStatus();
    </script>
</body>
</html>
""";
}
