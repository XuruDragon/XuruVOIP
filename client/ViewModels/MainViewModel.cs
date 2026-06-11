using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using NAudio.Wave;
using XuruVoipClient.Models;
using XuruVoipClient.Services;

namespace XuruVoipClient.ViewModels;

public class MainViewModel : INotifyPropertyChanged, IAsyncDisposable
{
    // ─── Services ────────────────────────────────────────────────────────────
    public ConfigService Config { get; } = new();
    private readonly OcrService _ocr = new();
    private readonly PositionWebSocketService _posWs = new();
    private readonly AudioUdpService _audioWs = new();
    private readonly AudioCaptureService _capture = new();
    private readonly AudioPlaybackService _playback = new();
    public AudioPlaybackService Playback => _playback;
    private readonly SpeechToTextService _stt = new();
    public SpeechToTextService Stt => _stt;
    private readonly DiscordRpcService _discordRpc = new();
    private readonly GlobalKeyHook _keyHook = new();
    public GlobalKeyHook KeyHook => _keyHook;
    private readonly DispatcherTimer _ocrTimer = new();
    private readonly GameDetectionService _gameDetector = new();
    public GameDetectionService GameDetector => _gameDetector;
    private CompanionAppService? _companionApp;
    private TelemetryService? _telemetry;

    public PlayerPosition LastSentPos => _lastSentPos;
    public Dictionary<string, PlayerPosition> RemotePositions => _remotePositions;

    private bool _isManualDisconnect = true;
    private bool _isReconnecting = false;
    private readonly object _reconnectLock = new();
    private CancellationTokenSource? _reconnectCts;

    private readonly VoiceCommandService _voiceCommand = new();
    public VoiceCommandService VoiceCommand => _voiceCommand;

    private bool _isVoiceListening;
    public bool IsVoiceListening { get => _isVoiceListening; set => Set(ref _isVoiceListening, value); }

    private string _voiceCommandStatusText = "";
    public string VoiceCommandStatusText { get => _voiceCommandStatusText; set => Set(ref _voiceCommandStatusText, value); }

    private string _voiceCommandStatusColor = "Cyan";
    public string VoiceCommandStatusColor { get => _voiceCommandStatusColor; set => Set(ref _voiceCommandStatusColor, value); }

    private bool _showVoiceCommandPanel;
    public bool ShowVoiceCommandPanel { get => _showVoiceCommandPanel; set => Set(ref _showVoiceCommandPanel, value); }

    private bool _isVoiceCommandListening = false;
    private System.Timers.Timer? _voiceCommandResetTimer;

    // ─── Observable state ────────────────────────────────────────────────────
    private readonly Dictionary<string, bool> _remoteHelmets = [];
    private readonly Dictionary<string, PlayerPosition> _remotePositions = [];
    private readonly Dictionary<string, string> _remoteLanguages = [];
    public Dictionary<string, string> RemoteLanguages => _remoteLanguages;

    private HailState _hailState = HailState.Idle;
    public HailState CurrentHailState { get => _hailState; set => Set(ref _hailState, value); }

    private string _hailPeerName = "";
    public string HailPeerName { get => _hailPeerName; set => Set(ref _hailPeerName, value); }
    private readonly Dictionary<string, string> _remoteChannels = [];
    private readonly Dictionary<string, bool> _remoteRepeaters = [];
    private bool _wasRadioTransmitting = false;
    private readonly List<string> _availableChannels = [];
    public IReadOnlyList<string> AvailableChannels => _availableChannels;
    private string _activeChannel = "";

    private bool _posConnected;
    public bool PosConnected { get => _posConnected; set => Set(ref _posConnected, value); }

    private bool _isUpdateAvailable;
    public bool IsUpdateAvailable { get => _isUpdateAvailable; set => Set(ref _isUpdateAvailable, value); }

    private string _latestVersion = "";
    public string LatestVersion { get => _latestVersion; set => Set(ref _latestVersion, value); }

    private bool _isSpatialAudioSupportedByServer = true; // default to true so it is editable offline
    public bool IsSpatialAudioSupportedByServer { get => _isSpatialAudioSupportedByServer; set => Set(ref _isSpatialAudioSupportedByServer, value); }

    private double _listenerHeadingX = 0.0;
    private double _listenerHeadingY = 1.0; // Default facing North / +Y

    private bool _audioConnected;
    public bool AudioConnected { get => _audioConnected; set => Set(ref _audioConnected, value); }

    private bool _isTalking;
    public bool IsTalking
    {
        get => _isTalking;
        set
        {
            if (Set(ref _isTalking, value))
            {
                NotifyMicStatusChanged();
            }
        }
    }

    private string _currentZone = "Waiting for SC...";
    public string CurrentZone { get => _currentZone; set => Set(ref _currentZone, value); }

    private string _currentPos = "";
    public string CurrentPos { get => _currentPos; set => Set(ref _currentPos, value); }

    private string _statusMessage = "Disconnected";
    public string StatusMessage { get => _statusMessage; set => Set(ref _statusMessage, value); }

    private float _inputLevel;
    public float InputLevel { get => _inputLevel; set => Set(ref _inputLevel, value); }

    private double _gforce = 0.0;
    public double GForce
    {
        get => _gforce;
        set => Set(ref _gforce, Math.Clamp(value, 0.0, 1.0));
    }

    private double _exertion = 0.0;
    public double Exertion
    {
        get => _exertion;
        set => Set(ref _exertion, Math.Clamp(value, 0.0, 1.0));
    }

    private IntercomDegradationState _intercomState = IntercomDegradationState.Normal;
    public IntercomDegradationState IntercomState
    {
        get => _intercomState;
        set
        {
            if (Set(ref _intercomState, value))
            {
                _playback.CurrentIntercomState = value;
            }
        }
    }

    private bool _isHelmetOn;
    public bool IsHelmetOn
    {
        get => _isHelmetOn;
        set
        {
            if (Set(ref _isHelmetOn, value))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HelmetStatusText)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HelmetStatusBrush)));
            }
        }
    }

    public string HelmetStatusText => IsHelmetOn 
        ? (Application.Current?.TryFindResource("HelmetEquipped") as string ?? "EQUIPPED") 
        : (Application.Current?.TryFindResource("HelmetUnequipped") as string ?? "UNEQUIPPED");
    private static readonly SolidColorBrush BrushGreen = new(Color.FromRgb(0x3D, 0xDB, 0x85));
    private static readonly SolidColorBrush BrushRed = new(Color.FromRgb(0xFF, 0x4E, 0x6A));
    private static readonly SolidColorBrush BrushOrange = new(Color.FromRgb(0xFF, 0x98, 0x00));
    public Brush HelmetStatusBrush => IsHelmetOn ? BrushGreen : BrushRed;

    private string _activeChannelName = "None";
    public string ActiveChannelName { get => _activeChannelName; set => Set(ref _activeChannelName, value); }

    private bool _isPttProximityDown = false;
    private bool _isPttRadioDown = false;
    private bool _isPttProfileDown = false;
    private bool _isPttPaDown = false;
    public bool IsPttPaDown => _isPttPaDown;


    private bool _micProximityMuted = false;
    public bool MicProximityMuted
    {
        get => _micProximityMuted;
        set
        {
            if (Set(ref _micProximityMuted, value))
            {
                if (_capture != null) _capture.ProximityMuted = value;
                UpdatePttState();
            }
        }
    }

    private bool _micRadioMuted = false;
    public bool MicRadioMuted
    {
        get => _micRadioMuted;
        set
        {
            if (Set(ref _micRadioMuted, value))
            {
                if (_capture != null) _capture.RadioMuted = value;
                UpdatePttState();
            }
        }
    }

    private bool _micProfileMuted = false;
    public bool MicProfileMuted
    {
        get => _micProfileMuted;
        set
        {
            if (Set(ref _micProfileMuted, value))
            {
                if (_capture != null) _capture.ProfileMuted = value;
                UpdatePttState();
            }
        }
    }

    public bool AudioProximityMuted
    {
        get => _playback.ProximityMuted;
        set
        {
            if (_playback.ProximityMuted != value)
            {
                _playback.ProximityMuted = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AudioProximityMuted)));
            }
        }
    }

    public bool AudioRadioMuted
    {
        get => _playback.RadioMuted;
        set
        {
            if (_playback.RadioMuted != value)
            {
                _playback.RadioMuted = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AudioRadioMuted)));
            }
        }
    }

    public bool AudioProfileMuted
    {
        get => _playback.ProfileMuted;
        set
        {
            if (_playback.ProfileMuted != value)
            {
                _playback.ProfileMuted = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AudioProfileMuted)));
            }
        }
    }

    private bool _isScanningHelmet = false;
    private int _helmetScanVotes = 0;
    private int _helmetScanTotal = 0;

    private PlayerPosition _lastSentPos = new();

    public MainViewModel()
    {
        Config.Load();
        InitializeServicesAsync();

        // Localize initial values if Application is running
        if (Application.Current != null)
        {
            _currentZone = Application.Current.TryFindResource("OcrWaiting") as string ?? "Waiting for SC...";
            _statusMessage = Application.Current.TryFindResource("StatusDisconnected") as string ?? "Disconnected";
        }
    }

    private async void InitializeServicesAsync()
    {
        _discordRpc.Enabled = Config.Config.EnableDiscordRpc;
        _discordRpc.Start();
        // Initialize position tracking source
        ApplyTrackingSource();

        // Hotkeys configuration
        _keyHook.KeyEvent += (key, isDown) =>
        {
            string keyStr = key.ToString();
            var cfg = Config.Config;

            if (cfg.EnableVoiceCommands && keyStr == cfg.VoiceCommandHotkey)
            {
                if (_stt.IsModelReady)
                {
                    HandleVoiceCommandHotkey(isDown);
                }
                return;
            }

            if (keyStr == cfg.PttProximityKey)
            {
                _isPttProximityDown = isDown;
                UpdatePttState();
            }
            else if (keyStr == cfg.PttRadioKey)
            {
                _isPttRadioDown = isDown;
                UpdatePttState();
            }
            else if (keyStr == cfg.PttProfileKey)
            {
                _isPttProfileDown = isDown;
                UpdatePttState();
            }
            else if (keyStr == cfg.PttPaKey)
            {
                _isPttPaDown = isDown;
                UpdatePttState();
            }
            else if (keyStr == cfg.InitiateHailKey && isDown)
            {
                InitiateHailCall();
            }
            else if (keyStr == cfg.AcceptHailKey && isDown)
            {
                AcceptHailCall();
            }
            else if (keyStr == cfg.DeclineHailKey && isDown)
            {
                DeclineHailCall();
            }
            else if (keyStr == cfg.HelmetToggleKey && isDown)
            {
                ToggleHelmet();
            }
            else if (keyStr == cfg.RadioCycleKey && isDown)
            {
                CycleRadioChannel();
            }
            else if (keyStr == cfg.MuteProximityKey && isDown)
            {
                MicProximityMuted = !MicProximityMuted;
                string msgKey = MicProximityMuted ? "MsgMicProximityMuted" : "MsgMicProximityUnmuted";
                StatusMessage = Application.Current?.TryFindResource(msgKey) as string ?? $"Microphone Proximity: {(MicProximityMuted ? "MUTED" : "UNMUTED")}";
            }
            else if (keyStr == cfg.MuteRadioKey && isDown)
            {
                MicRadioMuted = !MicRadioMuted;
                string msgKey = MicRadioMuted ? "MsgMicRadioMuted" : "MsgMicRadioUnmuted";
                StatusMessage = Application.Current?.TryFindResource(msgKey) as string ?? $"Microphone Radio: {(MicRadioMuted ? "MUTED" : "UNMUTED")}";
            }
            else if (keyStr == cfg.MuteProfileKey && isDown)
            {
                MicProfileMuted = !MicProfileMuted;
                string msgKey = MicProfileMuted ? "MsgMicProfileMuted" : "MsgMicProfileUnmuted";
                StatusMessage = Application.Current?.TryFindResource(msgKey) as string ?? $"Microphone Profile: {(MicProfileMuted ? "MUTED" : "UNMUTED")}";
            }
            else if (keyStr == cfg.MuteAudioProximityKey && isDown)
            {
                AudioProximityMuted = !AudioProximityMuted;
                string msgKey = AudioProximityMuted ? "MsgAudioProximityMuted" : "MsgAudioProximityUnmuted";
                StatusMessage = Application.Current?.TryFindResource(msgKey) as string ?? $"Proximity audio: {(AudioProximityMuted ? "MUTED" : "UNMUTED")}";
            }
            else if (keyStr == cfg.MuteAudioRadioKey && isDown)
            {
                AudioRadioMuted = !AudioRadioMuted;
                string msgKey = AudioRadioMuted ? "MsgAudioRadioMuted" : "MsgAudioRadioUnmuted";
                StatusMessage = Application.Current?.TryFindResource(msgKey) as string ?? $"Radio audio: {(AudioRadioMuted ? "MUTED" : "UNMUTED")}";
            }
            else if (keyStr == cfg.MuteAudioProfileKey && isDown)
            {
                AudioProfileMuted = !AudioProfileMuted;
                string msgKey = AudioProfileMuted ? "MsgAudioProfileMuted" : "MsgAudioProfileUnmuted";
                StatusMessage = Application.Current?.TryFindResource(msgKey) as string ?? $"Profile audio: {(AudioProfileMuted ? "MUTED" : "UNMUTED")}";
            }
        };
        _keyHook.Install();

        // Game watcher
        _gameDetector.HelmetStateChanged += helmetOn => Application.Current.Dispatcher.Invoke(() =>
        {
            SetHelmetOn(helmetOn);
        });
        _gameDetector.GameFocusChanged += async focused =>
        {
            if (_posWs.IsConnected)
            {
                if (focused)
                {
                    await _posWs.SetScOnlineAsync(true);
                    StartHelmetScan();
                }
                else
                {
                    await _posWs.SetScOnlineAsync(false);
                }
            }
        };
        _gameDetector.PositionReceived += OnGrtprPositionReceived;
        _gameDetector.GForceReceived += gforce => Application.Current?.Dispatcher.Invoke(() =>
        {
            if (Config.Config.EnableExertionDistortion)
            {
                GForce = gforce;
            }
        });
        _gameDetector.ExertionReceived += exertion => Application.Current?.Dispatcher.Invoke(() =>
        {
            if (Config.Config.EnableExertionDistortion)
            {
                Exertion = exertion;
            }
        });
        _gameDetector.IntercomStateChanged += state => Application.Current?.Dispatcher.Invoke(() =>
        {
            IntercomState = state;
        });
        _gameDetector.CustomGameLogPath = Config.Config.CustomGameLogPath;
        _gameDetector.Start();

        // Companion App
        if (Config.Config.EnableCompanionApp)
        {
            _companionApp = new CompanionAppService(this);
            _companionApp.Start();
        }

        // Telemetry Broadcast
        if (Config.Config.EnableTelemetry)
        {
            _telemetry = new TelemetryService(this);
            _telemetry.Start();
        }

        // Position tracking setup
        _ocrTimer.Tick += OnOcrTick;
        ApplyTrackingSource();

        // VU meter and transmitting indicator refresh
        var vuTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        vuTimer.Tick += (_, _) =>
        {
            var cfg = Config.Config;
            if (cfg.AudioMode == AudioMode.PTT)
            {
                bool anyPttPressed = _isPttProximityDown || _isPttRadioDown || _isPttProfileDown || _isVoiceCommandListening;
                InputLevel = anyPttPressed ? _capture.InputLevel : 0f;
            }
            else // VAD mode
            {
                InputLevel = _capture.InputLevel;
                IsTalking = _capture.IsTransmitting;
            }
        };
        vuTimer.Start();

        // Wire position server messages & state
        _posWs.Connected += () => Application.Current.Dispatcher.Invoke(async () =>
        {
            PosConnected = true;
            StatusMessage = Application.Current.TryFindResource("StatusConnecting") as string ?? "Connecting...";
            await ConnectAudioAsync();
            // Ensure server is synchronized with local Helmet Mode (OFF on start)
            await _posWs.SetHelmetAsync(IsHelmetOn);
            await _posWs.SendToggleRepeaterAsync(Config.Config.IsRadioRepeater);
        });
        _posWs.Disconnected += msg => Application.Current.Dispatcher.Invoke(() =>
        {
            PosConnected = false;
            AudioConnected = false;
            IsSpatialAudioSupportedByServer = true; // Reset to true for offline configuration editing
            string format = Application.Current.TryFindResource("StatusConnectionFailed") as string ?? "Connection failed: {0}";
            StatusMessage = string.Format(format, msg);
            HandleUnexpectedDisconnect("Position Server");
        });
        _posWs.WelcomeReceived += (channels, activeChan) => Application.Current.Dispatcher.Invoke(() =>
        {
            _availableChannels.Clear();
            _availableChannels.AddRange(channels);
            _activeChannel = activeChan;
            ActiveChannelName = string.IsNullOrEmpty(activeChan) ? "General" : activeChan;
            UpdateDiscordPresence();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AvailableChannels)));

            IsSpatialAudioSupportedByServer = _posWs.IsSpatialAudioSupportedByServer;
            if (!IsSpatialAudioSupportedByServer)
            {
                Config.Config.EnableSpatialAudio = false;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Config)));
            }
        });

        // Listen to extra metadata (like other players' helmets or leaves)
        _posWs.ServerMessage += json => Application.Current.Dispatcher.Invoke(() =>
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("type", out var typeEl)) return;
                string type = typeEl.GetString() ?? "";

                if (type == "welcome")
                {
                    if (doc.RootElement.TryGetProperty("players", out var playersEl) && playersEl.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var player in playersEl.EnumerateArray())
                        {
                            string name = player.GetProperty("name").GetString() ?? "";
                            if (name == Config.Config.Username) continue;

                            if (player.TryGetProperty("pos", out var posEl) && posEl.ValueKind == JsonValueKind.Object)
                            {
                                double x = posEl.GetProperty("x").GetDouble();
                                double y = posEl.GetProperty("y").GetDouble();
                                double z = posEl.GetProperty("z").GetDouble();
                                string zone = posEl.GetProperty("zone").GetString() ?? "";
                                string containerId = "";
                                if (posEl.TryGetProperty("container_id", out var cIdEl))
                                {
                                    containerId = cIdEl.GetString() ?? "";
                                }
                                string containerName = "";
                                if (posEl.TryGetProperty("container_name", out var cNameEl))
                                {
                                    containerName = cNameEl.GetString() ?? "";
                                }
                                _remotePositions[name] = new PlayerPosition 
                                { 
                                    X = x, 
                                    Y = y, 
                                    Z = z, 
                                    Zone = zone,
                                    ContainerID = containerId,
                                    ContainerName = containerName
                                };
                            }

                            if (player.TryGetProperty("helmet_on", out var helmEl))
                            {
                                _remoteHelmets[name] = helmEl.GetBoolean();
                            }

                            if (player.TryGetProperty("active_channel", out var chanEl))
                            {
                                _remoteChannels[name] = chanEl.GetString() ?? "";
                            }

                            if (player.TryGetProperty("is_radio_repeater", out var repEl))
                            {
                                _remoteRepeaters[name] = repEl.GetBoolean();
                            }

                            if (player.TryGetProperty("language", out var langEl))
                            {
                                _remoteLanguages[name] = langEl.GetString() ?? "en";
                            }
                        }
                    }
                }
                else if (type == "join")
                {
                    if (doc.RootElement.TryGetProperty("name", out var nameEl))
                    {
                        string remoteName = nameEl.GetString() ?? "";
                        if (doc.RootElement.TryGetProperty("active_channel", out var chanEl))
                        {
                            _remoteChannels[remoteName] = chanEl.GetString() ?? "";
                        }
                        if (doc.RootElement.TryGetProperty("is_radio_repeater", out var repEl))
                        {
                            _remoteRepeaters[remoteName] = repEl.GetBoolean();
                        }
                        if (doc.RootElement.TryGetProperty("language", out var langEl))
                        {
                            _remoteLanguages[remoteName] = langEl.GetString() ?? "en";
                        }
                    }
                }
                else if (type == "player_repeater")
                {
                    if (doc.RootElement.TryGetProperty("name", out var nameEl) &&
                        doc.RootElement.TryGetProperty("active", out var actEl))
                    {
                        string remoteName = nameEl.GetString() ?? "";
                        _remoteRepeaters[remoteName] = actEl.GetBoolean();
                    }
                }
                else if (type == "helmet")
                {
                    if (doc.RootElement.TryGetProperty("name", out var nameEl) &&
                        doc.RootElement.TryGetProperty("helmet_on", out var helmEl))
                    {
                        string remoteName = nameEl.GetString() ?? "";
                        bool remoteHelmet = helmEl.GetBoolean();
                        _remoteHelmets[remoteName] = remoteHelmet;
                    }
                }
                else if (type == "pos")
                {
                    if (doc.RootElement.TryGetProperty("name", out var nameEl) &&
                        doc.RootElement.TryGetProperty("pos", out var posEl))
                    {
                        string remoteName = nameEl.GetString() ?? "";
                        double x = posEl.GetProperty("x").GetDouble();
                        double y = posEl.GetProperty("y").GetDouble();
                        double z = posEl.GetProperty("z").GetDouble();
                        string zone = posEl.GetProperty("zone").GetString() ?? "";
                        string containerId = "";
                        if (posEl.TryGetProperty("container_id", out var cIdEl))
                        {
                            containerId = cIdEl.GetString() ?? "";
                        }
                        string containerName = "";
                        if (posEl.TryGetProperty("container_name", out var cNameEl))
                        {
                            containerName = cNameEl.GetString() ?? "";
                        }
                        _remotePositions[remoteName] = new PlayerPosition 
                        { 
                            X = x, 
                            Y = y, 
                            Z = z, 
                            Zone = zone,
                            ContainerID = containerId,
                            ContainerName = containerName
                        };
                    }
                }
                else if (type == "leave")
                {
                    if (doc.RootElement.TryGetProperty("name", out var nameEl))
                    {
                        string remoteName = nameEl.GetString() ?? "";
                        _remoteHelmets.Remove(remoteName);
                        _remotePositions.Remove(remoteName);
                        _remoteChannels.Remove(remoteName);
                        _remoteRepeaters.Remove(remoteName);
                        _remoteLanguages.Remove(remoteName);
                        _playback.RemovePlayer(remoteName);
                    }
                }
                else if (type == "hail_ringing")
                {
                    if (doc.RootElement.TryGetProperty("peer", out var peerEl))
                    {
                        string peer = peerEl.GetString() ?? "";
                        CurrentHailState = HailState.Outgoing;
                        HailPeerName = peer;
                        StatusMessage = $"Hailing {peer}...";
                        StartHailChimeLoop(HailState.Outgoing);
                    }
                }
                else if (type == "hail_incoming")
                {
                    if (doc.RootElement.TryGetProperty("peer", out var peerEl))
                    {
                        string peer = peerEl.GetString() ?? "";
                        CurrentHailState = HailState.Incoming;
                        HailPeerName = peer;
                        StatusMessage = $"Incoming hail from {peer}";
                        StartHailChimeLoop(HailState.Incoming);
                    }
                }
                else if (type == "hail_connected")
                {
                    if (doc.RootElement.TryGetProperty("peer", out var peerEl))
                    {
                        string peer = peerEl.GetString() ?? "";
                        CurrentHailState = HailState.Connected;
                        HailPeerName = peer;
                        StatusMessage = $"Hail connected to {peer}";
                        StopHailChimeLoop();
                        _playback.PlayHailConnectedFeedback();
                    }
                }
                else if (type == "hail_disconnected")
                {
                    var prev = CurrentHailState;
                    CurrentHailState = HailState.Idle;
                    HailPeerName = "";
                    StatusMessage = "Hail disconnected.";
                    StopHailChimeLoop();
                    if (prev != HailState.Idle)
                    {
                        _playback.PlayHailDisconnectedFeedback();
                    }
                }
                else if (type == "hail_error")
                {
                    if (doc.RootElement.TryGetProperty("reason", out var reasonEl))
                    {
                        string reason = reasonEl.GetString() ?? "";
                        string friendlyReason = reason switch
                        {
                            "target_offline" => "Target player is offline.",
                            "out_of_range" => "Target is out of hailing range (5,000m).",
                            "busy" => "Target line is busy.",
                            _ => $"Call failed: {reason}"
                        };
                        StatusMessage = friendlyReason;
                    }
                    var prev = CurrentHailState;
                    CurrentHailState = HailState.Idle;
                    HailPeerName = "";
                    StopHailChimeLoop();
                    if (prev != HailState.Idle)
                    {
                        _playback.PlayHailDisconnectedFeedback();
                    }
                }
                else if (type == "player_channel")
                {
                    if (doc.RootElement.TryGetProperty("name", out var nameEl) &&
                        doc.RootElement.TryGetProperty("channel", out var chanEl))
                    {
                        string name = nameEl.GetString() ?? "";
                        string channel = chanEl.GetString() ?? "";
                        _remoteChannels[name] = channel;
                        if (name == Config.Config.Username)
                        {
                            _activeChannel = channel;
                            ActiveChannelName = string.IsNullOrEmpty(channel) ? "General" : channel;
                            LogService.Info($"Active channel updated by server: {ActiveChannelName}");
                        }
                    }
                }
                else if (type == "channels_list")
                {
                    if (doc.RootElement.TryGetProperty("channels", out var chanEl) && chanEl.ValueKind == JsonValueKind.Array)
                    {
                        _availableChannels.Clear();
                        foreach (var item in chanEl.EnumerateArray())
                        {
                            var s = item.GetString();
                            if (s != null) _availableChannels.Add(s);
                        }
                        if (!_availableChannels.Contains(_activeChannel))
                        {
                            _activeChannel = _availableChannels.Contains("General") ? "General" : (_availableChannels.Count > 0 ? _availableChannels[0] : "");
                            ActiveChannelName = string.IsNullOrEmpty(_activeChannel) ? "General" : _activeChannel;
                        }
                        LogService.Info($"Available channels list updated by server. Count: {_availableChannels.Count}");
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AvailableChannels)));
                    }
                }
            }
            catch { }
        });

        // Wire audio events
        _audioWs.Connected += () => Application.Current.Dispatcher.Invoke(() =>
        {
            AudioConnected = true;
            StatusMessage = Application.Current.TryFindResource("StatusConnected") as string ?? "Connected";
            StartAudio();
            UpdateDiscordPresence();
        });
        _audioWs.Disconnected += msg => Application.Current.Dispatcher.Invoke(() =>
        {
            AudioConnected = false;
            string format = Application.Current.TryFindResource("StatusConnectionFailed") as string ?? "Connection failed: {0}";
            StatusMessage = string.Format(format, msg);
            UpdateDiscordPresence();
            HandleUnexpectedDisconnect("Audio Server");
        });

        _audioWs.AudioPacketReceived += (name, type, opus, metadata, seq) =>
        {
            bool localHelmet = IsHelmetOn;
            _remoteHelmets.TryGetValue(name, out bool remoteHelmet);
            bool applyRadio = (type == 0x01 || type == 0x02) || (type == 0x00 && (localHelmet || remoteHelmet));

            // Calculate distance for radio degradation if it is a Radio packet (type 0x01)
            double distance = -1.0;
            if (type == 0x01)
            {
                if (_remotePositions.TryGetValue(name, out var remotePos))
                {
                    distance = CalculateEffectiveRadioDistance(name, remotePos);
                }
            }

            string speakerZone = "";
            if (_remotePositions.TryGetValue(name, out var remotePos2))
            {
                speakerZone = remotePos2.Zone ?? "";
            }
            string listenerZone = _lastSentPos.Zone ?? "";

            bool isIntercom = false;
            if (type == 0x01)
            {
                _remoteChannels.TryGetValue(name, out var remoteChan);
                if (remoteChan != null && remoteChan.StartsWith("Intercom_"))
                {
                    isIntercom = true;
                }
            }

            _playback.ReceiveOpusFrame(name, opus, type, applyRadio, metadata, distance, speakerZone, listenerZone, seq, isIntercom);
        };

        _playback.SttAudioChunkReady += (name, samples, type) =>
        {
            bool wantsStt = Config.Config.EnableStt;
            bool wantsTranslation = Config.Config.EnableTranslationSubtitles;

            if (wantsStt || wantsTranslation)
            {
                _remoteLanguages.TryGetValue(name, out var remoteLang);
                if (string.IsNullOrEmpty(remoteLang))
                {
                    remoteLang = "en";
                }

                string localLang = Config.Config.Language;
                if (string.IsNullOrEmpty(localLang))
                {
                    localLang = "en";
                }

                // If translation is enabled and the remote speaker uses a different language,
                // we must transcribe using their language so the audio is correctly interpreted before translation.
                string transcriptionLang = (wantsTranslation && remoteLang.Split('-')[0].ToLowerInvariant() != localLang.Split('-')[0].ToLowerInvariant()) 
                    ? remoteLang 
                    : localLang;

                _stt.QueueTranscription(name, samples, type, transcriptionLang);
            }
        };

        _capture.EncodedFrameReady += async (frame, txType) =>
        {
            await _audioWs.SendAudioFrameAsync(txType, frame);
        };

        // Voice Command Service wiring
        _voiceCommand.VisorToggleRequested += () => ToggleHelmet();
        _voiceCommand.ChannelChangeRequested += chan => _ = ChangeRadioChannelAsync(chan);
        _voiceCommand.ShipPowerToggleRequested += () => 
            InputSimulator.SimulateKeyPress(Config.Config.VoiceCommandPowerKey);
        _voiceCommand.ShipDoorsToggleRequested += () => 
            InputSimulator.SimulateKeyPress(Config.Config.VoiceCommandDoorsKey, Config.Config.VoiceCommandDoorsModifier);
        _voiceCommand.ShipShieldsFrontRequested += () => 
            InputSimulator.SimulateKeyPress(Config.Config.VoiceCommandShieldsKey);
        _voiceCommand.ShipLandingGearToggleRequested += () => 
            InputSimulator.SimulateKeyPress(Config.Config.VoiceCommandLandingGearKey);
        _voiceCommand.VoiceChangerProfileRequested += profile =>
        {
            Config.Config.EnableVoiceChanger = (profile != "None");
            Config.Config.VoiceChangerType = profile;
            SaveConfig();
            ApplySettings();
        };
        _voiceCommand.MicStateChangeRequested += action =>
        {
            switch (action)
            {
                case VoiceCommandAction.MicMuteProximity:
                    MicProximityMuted = true;
                    break;
                case VoiceCommandAction.MicUnmuteProximity:
                    MicProximityMuted = false;
                    break;
                case VoiceCommandAction.MicMuteRadio:
                    MicRadioMuted = true;
                    break;
                case VoiceCommandAction.MicUnmuteRadio:
                    MicRadioMuted = false;
                    break;
                case VoiceCommandAction.MicMuteProfile:
                    MicProfileMuted = true;
                    break;
                case VoiceCommandAction.MicUnmuteProfile:
                    MicProfileMuted = false;
                    break;
                case VoiceCommandAction.MicMuteAll:
                    MicProximityMuted = true;
                    MicRadioMuted = true;
                    MicProfileMuted = true;
                    break;
                case VoiceCommandAction.MicUnmuteAll:
                    MicProximityMuted = false;
                    MicRadioMuted = false;
                    MicProfileMuted = false;
                    break;
            }
        };
        _stt.CaptionDecoded += OnCaptionDecoded;

        NotifyMicStatusChanged();
        IsHelmetOn = false;
    }

    private void UpdatePttState()
    {
        bool isRadioTransmitting = _isPttRadioDown && !MicRadioMuted;
        if (isRadioTransmitting != _wasRadioTransmitting)
        {
            _playback.PlayLocalPttChime(isRadioTransmitting);
            _wasRadioTransmitting = isRadioTransmitting;
        }

        if (_isPttPaDown && Config.Config.EnableShipPa)
        {
            _capture.SetPttState(true, 0x03);
            IsTalking = true;
        }
        else if (_isPttProfileDown)
        {
            _capture.SetPttState(true, 0x02);
            IsTalking = !MicProfileMuted;
        }
        else if (_isPttRadioDown)
        {
            _capture.SetPttState(true, 0x01);
            IsTalking = !MicRadioMuted;
        }
        else if (_isPttProximityDown && Config.Config.AudioMode == AudioMode.PTT)
        {
            _capture.SetPttState(true, 0x00);
            IsTalking = !MicProximityMuted;
        }
        else
        {
            _capture.SetPttState(false, 0x00);
            if (Config.Config.AudioMode == AudioMode.PTT)
            {
                IsTalking = false;
            }
        }
        NotifyMicStatusChanged();
    }

    public async void ToggleHelmet()
    {
        _isScanningHelmet = false; // Cancel active scan
        SetHelmetOn(!IsHelmetOn);
    }

    private async void SetHelmetOn(bool value)
    {
        if (IsHelmetOn == value) return;
        IsHelmetOn = value;
        await _posWs.SetHelmetAsync(value);
        UpdateDiscordPresence();
    }

    public void StartHelmetScan()
    {
        if (_isScanningHelmet) return;
        _isScanningHelmet = true;
        _helmetScanVotes = 0;
        _helmetScanTotal = 0;

        Task.Run(async () =>
        {
            for (int i = 0; i < 25; i++)
            {
                if (!_gameDetector.IsGameFocused)
                {
                    await Task.Delay(200);
                    continue;
                }

                var gameRect = _gameDetector.GetGameClientRectInScreenCoords();
                var res = _ocr.ScanHelmetCompass(Config.Config.OcrMonitorIndex, gameRect);
                if (res.HasValue)
                {
                    _helmetScanTotal++;
                    if (res.Value) _helmetScanVotes++;
                }
                await Task.Delay(200);
            }

            _isScanningHelmet = false;
            if (_helmetScanTotal > 0)
            {
                double ratio = (double)_helmetScanVotes / _helmetScanTotal;
                bool hasHelmet = ratio > 0.5;
                Application.Current.Dispatcher.Invoke(() => SetHelmetOn(hasHelmet));
            }
        });
    }

    public async void CycleRadioChannel()
    {
        if (_availableChannels.Count == 0) return;
        int idx = _availableChannels.IndexOf(_activeChannel);
        int nextIdx = (idx + 1) % _availableChannels.Count;
        _activeChannel = _availableChannels[nextIdx];
        ActiveChannelName = _activeChannel;

        await _posWs.SetChannelAsync(_activeChannel);
        UpdateDiscordPresence();
    }

    public async Task ChangeRadioChannelAsync(string channel)
    {
        if (!_availableChannels.Contains(channel)) return;
        _activeChannel = channel;
        ActiveChannelName = channel;

        await _posWs.SetChannelAsync(channel);
        UpdateDiscordPresence();
    }

    public async Task ConnectAsync()
    {
        if (Config.Config.UseGrtpr && string.IsNullOrWhiteSpace(Config.Config.CustomGameLogPath))
        {
            string warnTitle = Application.Current.TryFindResource("TitleWarning") as string ?? "Warning";
            string warnMsg = Application.Current.TryFindResource("WarningGrtprNoPath") as string ?? 
                "Game.log Reader (GRTPR) is enabled, but the Game.log file path is not set.\nPlease fill in your Star Citizen Game.log path under the General settings tab before trying to connect to the server.";
            System.Windows.MessageBox.Show(warnMsg, warnTitle, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        _isManualDisconnect = false;
        _reconnectCts?.Cancel();
        StatusMessage = Application.Current.TryFindResource("StatusConnecting") as string ?? "Connecting...";
        LogService.Info("ConnectAsync triggered. Connecting to Position Server...");
        var ok = await _posWs.ConnectAsync(Config.Config);
        if (!ok)
        {
            _isManualDisconnect = true;
            string errorKey = "ErrorUnknown";
            if (!string.IsNullOrEmpty(_posWs.WelcomeErrorReason))
            {
                errorKey = _posWs.WelcomeErrorReason switch
                {
                    "invalid_server_password" => "ErrorInvalidToken",
                    "invalid_token" => "ErrorInvalidToken",
                    "server_full" => "ErrorServerFull",
                    "invalid_password" => "ErrorInvalidPassword",
                    "player_banned" => "ErrorPlayerBanned",
                    "db_error" => "ErrorDbError",
                    _ => "ErrorUnknown"
                };
            }
            else if (_posWs.WelcomeError == "Server did not provide an audio ticket.")
            {
                errorKey = "ErrorNoTicket";
            }

            string localizedError = Application.Current.TryFindResource(errorKey) as string ?? _posWs.WelcomeError ?? "Unknown error";
            string format = Application.Current.TryFindResource("StatusConnectionFailed") as string ?? "Connection failed: {0}";
            StatusMessage = string.Format(format, localizedError);
            LogService.Error($"ConnectAsync: Position Server connection failed: {localizedError}");
        }
        else
        {
            LogService.Info("ConnectAsync: Position Server connection succeeded.");
        }
    }

    private async Task ConnectAudioAsync()
    {
        if (_posWs.AudioTicket == null)
        {
            LogService.Error("ConnectAudioAsync: Cannot connect because AudioTicket is null.");
            return;
        }
        LogService.Info("ConnectAudioAsync triggered. Connecting to Audio Server...");
        await _audioWs.ConnectAsync(
            Config.Config.ServerAddress,
            Config.Config.AudioPort,
            Config.Config.Username,
            Config.Config.ServerPassword,
            _posWs.AudioTicket);
    }

    private void StartAudio()
    {
        var cfg = Config.Config;
        LogService.Info($"StartAudio: Starting playback and capture devices. Mode={cfg.AudioMode}");
        _playback.EnableSpatialAudio = cfg.EnableSpatialAudio;
        _playback.EnableHrtfBinaural = cfg.EnableHrtf;
        _playback.EnableRadioDegradation = cfg.EnableRadioDegradation;
        _playback.EnablePttChimes = cfg.EnablePttChimes;
        _playback.EnableEnvironmentalAcoustics = cfg.EnableEnvironmentalAcoustics;
        _playback.EnableAtmosphereSimulation = cfg.EnableAtmosphereSimulation;
        _playback.EnableHelmetModulator = cfg.EnableHelmetModulator;
        _playback.EnableStt = cfg.EnableStt;
        _playback.EnableShipPa = cfg.EnableShipPa;
        _playback.EnableVisorSpectrogram = cfg.EnableVisorSpectrogram;
        _playback.Start(cfg.OutputDeviceIndex, cfg.OutputGainPercent);
        
        // Synchronize mute states
        _capture.ProximityMuted = MicProximityMuted;
        _capture.RadioMuted = MicRadioMuted;
        _capture.ProfileMuted = MicProfileMuted;
        
        // Refresh properties for bindings
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AudioProximityMuted)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AudioRadioMuted)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AudioProfileMuted)));
        
        _capture.Start(cfg.InputDeviceIndex, cfg.InputGainDb, cfg.VadSensitivity, cfg.AudioMode);
    }

    private void OnGrtprPositionReceived(PlayerPosition pos)
    {
        if (!Config.Config.UseGrtpr) return;
        Application.Current.Dispatcher.Invoke(async () =>
        {
            await ProcessNewPositionAsync(pos);
        });
    }

    private async void OnOcrTick(object? sender, EventArgs e)
    {
        if (Config.Config.UseGrtpr) return;
        if (!_gameDetector.CheckIfGameFocused()) return;

        var cfg = Config.Config;
        var gameRect = _gameDetector.GetGameClientRectInScreenCoords();
        var pos = await Task.Run(() =>
            _ocr.Capture(cfg.OcrMonitorIndex, cfg.OcrRegion, gameRect));

        if (pos == null) return;

        await ProcessNewPositionAsync(pos);
    }

    private async Task ProcessNewPositionAsync(PlayerPosition pos)
    {
        // Parse ContainerID and ContainerName from Zone if inside a vehicle
        string zone = pos.Zone;
        if (!string.IsNullOrEmpty(zone))
        {
            int lastSep = Math.Max(zone.LastIndexOf('_'), zone.LastIndexOf(' '));
            if (lastSep > 0)
            {
                string lastPart = zone.Substring(lastSep + 1);
                if (int.TryParse(lastPart, out _))
                {
                    pos.ContainerName = zone.Substring(0, lastSep);
                    pos.ContainerID = zone.Replace(' ', '_');
                }
            }
        }

        string oldContainerID = _lastSentPos.ContainerID;
        string newContainerID = pos.ContainerID;
        if (oldContainerID != newContainerID)
        {
            if (!string.IsNullOrEmpty(oldContainerID))
            {
                string oldIntercom = "Intercom_" + oldContainerID;
                if (_availableChannels.Contains(oldIntercom))
                {
                    _availableChannels.Remove(oldIntercom);
                    if (_activeChannel == oldIntercom)
                    {
                        _activeChannel = "General";
                        ActiveChannelName = "General";
                    }
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AvailableChannels)));
                }
            }
            if (!string.IsNullOrEmpty(newContainerID))
            {
                string newIntercom = "Intercom_" + newContainerID;
                if (!_availableChannels.Contains(newIntercom))
                {
                    _availableChannels.Add(newIntercom);
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AvailableChannels)));
                }
            }
        }

        CurrentZone = pos.Zone;
        CurrentPos = $"{pos.X:F1}m  {pos.Y:F1}m  {pos.Z:F1}m";

        // Update playback service with current listener position
        _playback.ListenerX = pos.X;
        _playback.ListenerY = pos.Y;
        _playback.ListenerZ = pos.Z;

        // Estimate heading if moved significantly and it's not the initial frame
        if (_lastSentPos.X != 0 || _lastSentPos.Y != 0)
        {
            double dx = pos.X - _lastSentPos.X;
            double dy = pos.Y - _lastSentPos.Y;
            double distMoved = Math.Sqrt(dx * dx + dy * dy);
            if (distMoved > 0.5)
            {
                _listenerHeadingX = dx / distMoved;
                _listenerHeadingY = dy / distMoved;
            }
        }

        _playback.ListenerHeadingX = _listenerHeadingX;
        _playback.ListenerHeadingY = _listenerHeadingY;

        // Send only if changed significantly (1m threshold)
        bool changed =
            Math.Abs(pos.X - _lastSentPos.X) > 1 ||
            Math.Abs(pos.Y - _lastSentPos.Y) > 1 ||
            Math.Abs(pos.Z - _lastSentPos.Z) > 1 ||
            pos.Zone != _lastSentPos.Zone;

        if (changed && PosConnected)
        {
            bool zoneChanged = pos.Zone != _lastSentPos.Zone || string.IsNullOrEmpty(_lastSentPos.Zone);
            await _posWs.SendPositionAsync(pos);
            _lastSentPos = pos;
            if (zoneChanged)
            {
                UpdateDiscordPresence();
            }
        }
    }

    public void Disconnect()
    {
        _isManualDisconnect = true;
        _reconnectCts?.Cancel();
        LogService.Info("Disconnect: Shutting down all network and audio services.");
        _posWs.Disconnect();
        _audioWs.Disconnect();
        _capture.Stop();
        _playback.Stop();
        PosConnected = false;
        AudioConnected = false;
        StatusMessage = Application.Current.TryFindResource("StatusDisconnected") as string ?? "Disconnected";
        UpdateDiscordPresence();
    }

    private void HandleUnexpectedDisconnect(string source)
    {
        if (_isManualDisconnect) return;

        lock (_reconnectLock)
        {
            if (_isReconnecting) return;
            _isReconnecting = true;
        }

        LogService.Info($"Unexpected disconnect from {source}. Initiating full disconnect and auto-reconnect...");

        // Perform full clean up on the dispatcher thread
        Application.Current.Dispatcher.Invoke(() =>
        {
            // Disconnect both services cleanly
            _posWs.Disconnect();
            _audioWs.Disconnect();
            _capture.Stop();
            _playback.Stop();
            PosConnected = false;
            AudioConnected = false;
        });

        // Cancel any pending reconnect attempt
        _reconnectCts?.Cancel();
        _reconnectCts = new CancellationTokenSource();
        var token = _reconnectCts.Token;

        Task.Run(async () =>
        {
            try
            {
                int delaySeconds = 3;
                while (!token.IsCancellationRequested && !_isManualDisconnect)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        string format = Application.Current.TryFindResource("StatusReconnectingIn") as string ?? "Reconnecting in {0}s...";
                        StatusMessage = string.Format(format, delaySeconds);
                    });

                    await Task.Delay(1000, token);
                    delaySeconds--;

                    if (delaySeconds <= 0)
                    {
                        if (token.IsCancellationRequested || _isManualDisconnect) break;

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            StatusMessage = Application.Current.TryFindResource("StatusConnecting") as string ?? "Connecting...";
                        });

                        LogService.Info("Auto-reconnect: Attempting to reconnect to Position Server...");
                        var ok = await _posWs.ConnectAsync(Config.Config);
                        if (ok)
                        {
                            LogService.Info("Auto-reconnect: Position Server connection succeeded.");
                            // ConnectAudioAsync will be triggered automatically by _posWs.Connected event
                            lock (_reconnectLock)
                            {
                                _isReconnecting = false;
                            }
                            return; // Reconnection succeeded/initiated
                        }
                        else
                        {
                            LogService.Error("Auto-reconnect: Position Server connection failed. Retrying...");
                            delaySeconds = 5; // Wait 5 seconds before next retry if it failed
                        }
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                LogService.Error("Error in auto-reconnect loop", ex);
            }
            finally
            {
                lock (_reconnectLock)
                {
                    _isReconnecting = false;
                }
            }
        });
    }

    public void SaveConfig() => Config.Save();

    private void ApplyTrackingSource()
    {
        var cfg = Config.Config;
        if (cfg.UseGrtpr)
        {
            LogService.Info("Position Tracking: Using Game.log Reader (GRTPR). Disabling OCR Engine.");
            _ocrTimer.Stop();
            _ocr.Dispose();
        }
        else
        {
            LogService.Info("Position Tracking: Using OCR Screen Scanner. Initializing Tesseract Engine.");
            try
            {
                var tessDir = ConfigService.EnsureTessdata();
                _ocr.Initialize(tessDir);
            }
            catch (Exception ex)
            {
                StatusMessage = $"OCR init failed: {ex.Message}";
                LogService.Error("OCR initialization failed", ex);
            }

            _ocrTimer.Interval = TimeSpan.FromMilliseconds(cfg.OcrIntervalMs);
            if (!_ocrTimer.IsEnabled)
            {
                _ocrTimer.Start();
            }
        }
    }

    public void ApplySettings()
    {
        LogService.Info("ApplySettings: Applying settings update...");
        // Update general logging switch instantly
        LogService.EnableGeneralLogs = Config.Config.EnableGeneralLogs;
        _gameDetector.CustomGameLogPath = Config.Config.CustomGameLogPath;
        _playback.EnableSpatialAudio = Config.Config.EnableSpatialAudio; // Sync spatial audio setting
        _playback.EnableHrtfBinaural = Config.Config.EnableHrtf;
        _playback.EnableRadioDegradation = Config.Config.EnableRadioDegradation;
        _playback.EnablePttChimes = Config.Config.EnablePttChimes;
        _playback.EnableEnvironmentalAcoustics = Config.Config.EnableEnvironmentalAcoustics;
        _playback.EnableAtmosphereSimulation = Config.Config.EnableAtmosphereSimulation;
        _playback.EnableHelmetModulator = Config.Config.EnableHelmetModulator;
        _playback.EnableStt = Config.Config.EnableStt;
        _playback.EnableShipPa = Config.Config.EnableShipPa;
        _playback.EnableVisorSpectrogram = Config.Config.EnableVisorSpectrogram;
        _playback.EnableIntercomDegradation = Config.Config.EnableIntercomDegradation;
        _playback.IntercomShieldHitsEnabled = Config.Config.IntercomShieldHitsEnabled;
        _playback.IntercomCriticalPowerEnabled = Config.Config.IntercomCriticalPowerEnabled;
        _playback.IntercomQuantumTravelEnabled = Config.Config.IntercomQuantumTravelEnabled;
        _discordRpc.Enabled = Config.Config.EnableDiscordRpc;

        // Sync position tracking source
        ApplyTrackingSource();

        // Sync repeater state to server
        if (PosConnected)
        {
            _ = _posWs.SendToggleRepeaterAsync(Config.Config.IsRadioRepeater);
        }

        // Sync companion app state
        if (Config.Config.EnableCompanionApp)
        {
            if (_companionApp != null && _companionApp.ActivePort != Config.Config.CompanionAppPort)
            {
                LogService.Info("Companion app port changed, restarting server...");
                _companionApp.Stop();
                _companionApp = null;
            }

            if (_companionApp == null)
            {
                _companionApp = new CompanionAppService(this);
                _companionApp.Start();
            }
        }
        else
        {
            if (_companionApp != null)
            {
                _companionApp.Stop();
                _companionApp = null;
            }
        }

        // Sync telemetry state
        if (Config.Config.EnableTelemetry)
        {
            if (_telemetry != null && _telemetry.LastPort != Config.Config.TelemetryPort)
            {
                LogService.Info("Telemetry port changed, restarting telemetry...");
                _telemetry.Stop();
                _telemetry = null;
            }

            if (_telemetry == null)
            {
                _telemetry = new TelemetryService(this);
                _telemetry.Start();
            }
        }
        else
        {
            if (_telemetry != null)
            {
                _telemetry.Stop();
                _telemetry = null;
            }
        }

        if (AudioConnected)
        {
            LogService.Info("ApplySettings: Re-initializing audio devices with new parameters.");
            _capture.Stop();
            _playback.Stop();
            StartAudio();
        }
        NotifyMicStatusChanged();
    }

    public void RefreshLocalizedStrings()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HelmetStatusText)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MicModeText)));
        
        // Update StatusMessage if it matches a localized state
        if (StatusMessage == "Disconnected" || StatusMessage == "Déconnecté" || StatusMessage == "Getrennt" || StatusMessage == "Desconectado")
        {
            StatusMessage = Application.Current.TryFindResource("StatusDisconnected") as string ?? "Disconnected";
        }
        else if (StatusMessage == "Connecting..." || StatusMessage == "Connexion en cours..." || StatusMessage == "Verbinden..." || StatusMessage == "Conectando...")
        {
            StatusMessage = Application.Current.TryFindResource("StatusConnecting") as string ?? "Connecting...";
        }
        else if (StatusMessage == "Connected" || StatusMessage == "Connecté" || StatusMessage == "Verbunden" || StatusMessage == "Conectado")
        {
            StatusMessage = Application.Current.TryFindResource("StatusConnected") as string ?? "Connected";
        }

        // Update CurrentZone if it is the default waiting message
        if (CurrentZone == "Waiting for SC..." || CurrentZone == "En attente de SC..." || CurrentZone == "Warten auf SC..." || CurrentZone == "Esperando SC...")
        {
            CurrentZone = Application.Current.TryFindResource("OcrWaiting") as string ?? "Waiting for SC...";
        }
    }

    public string MicModeText
    {
        get
        {
            var cfg = Config.Config;
            if (_isPttProfileDown)
            {
                return MicProfileMuted 
                    ? (Application.Current?.TryFindResource("MicProfilePttMuted") as string ?? "Profile PTT (Muted)")
                    : (Application.Current?.TryFindResource("MicProfilePttOn") as string ?? "Profile PTT (ON)");
            }
            else if (_isPttRadioDown)
            {
                return MicRadioMuted 
                    ? (Application.Current?.TryFindResource("MicRadioPttMuted") as string ?? "Radio Channel PTT (Muted)")
                    : (Application.Current?.TryFindResource("MicRadioPttOn") as string ?? "Radio Channel PTT (ON)");
            }
            else if (_isPttProximityDown && cfg.AudioMode == AudioMode.PTT)
            {
                return MicProximityMuted 
                    ? (Application.Current?.TryFindResource("MicProximityPttMuted") as string ?? "Proximity PTT (Muted)")
                    : (Application.Current?.TryFindResource("MicProximityPttOn") as string ?? "Proximity PTT (ON)");
            }
            else
            {
                if (cfg.AudioMode == AudioMode.VAD)
                {
                    if (MicProximityMuted)
                    {
                        return Application.Current?.TryFindResource("MicProximityVadMuted") as string ?? "Proximity VAD (Muted)";
                    }
                    else
                    {
                        return IsTalking 
                            ? (Application.Current?.TryFindResource("MicProximityVadOn") as string ?? "Proximity VAD (ON)")
                            : (Application.Current?.TryFindResource("MicProximityVadOff") as string ?? "Proximity VAD (OFF)");
                    }
                }
                else
                {
                    return MicProximityMuted 
                        ? (Application.Current?.TryFindResource("MicProximityPttMuted") as string ?? "Proximity PTT (Muted)")
                        : (Application.Current?.TryFindResource("MicProximityPttOff") as string ?? "Proximity PTT (OFF)");
                }
            }
        }
    }

    public Brush TalkLedBrush
    {
        get
        {
            var cfg = Config.Config;
            if (cfg.AudioMode == AudioMode.VAD)
            {
                return MicProximityMuted ? BrushOrange : (IsTalking ? BrushGreen : BrushOrange);
            }
            else
            {
                bool activePtt = (_isPttProfileDown && !MicProfileMuted) || 
                                 (_isPttRadioDown && !MicRadioMuted) || 
                                 (_isPttProximityDown && !MicProximityMuted);
                return activePtt ? BrushGreen : BrushOrange;
            }
        }
    }

    private void NotifyMicStatusChanged()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MicModeText)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TalkLedBrush)));
    }

    // ─── INotifyPropertyChanged ──────────────────────────────────────────────
    public event PropertyChangedEventHandler? PropertyChanged;
    private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        return true;
    }

    public async ValueTask DisposeAsync()
    {
        _voiceCommandResetTimer?.Stop();
        _voiceCommandResetTimer?.Dispose();
        _ocrTimer.Stop();
        _keyHook.Dispose();
        _gameDetector.Dispose();
        _capture.Dispose();
        _playback.Dispose();
        _stt.Dispose();
        _ocr.Dispose();
        _discordRpc.Dispose();
        _companionApp?.Dispose();
        _telemetry?.Dispose();
        await _posWs.DisposeAsync();
        await _audioWs.DisposeAsync();
    }

    public double CalculateEffectiveRadioDistance(string senderName, PlayerPosition senderPos)
    {
        // 1. If radio repeaters are disabled globally or on client, or if sender/receiver in different zones, return direct distance or infinity
        if (!Config.Config.EnableRadioRepeaters || senderPos.Zone != _lastSentPos.Zone)
        {
            if (senderPos.Zone != _lastSentPos.Zone) return 99999.0;
            double dx = senderPos.X - _lastSentPos.X;
            double dy = senderPos.Y - _lastSentPos.Y;
            double dz = senderPos.Z - _lastSentPos.Z;
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        // 2. Identify all possible node positions in the same zone
        var nodes = new List<(string Name, PlayerPosition Pos)>();
        nodes.Add((senderName, senderPos));
        
        foreach (var kvp in _remotePositions)
        {
            string name = kvp.Key;
            PlayerPosition pos = kvp.Value;
            if (name == senderName) continue;
            
            // Check if this player is a repeater in the same zone
            _remoteRepeaters.TryGetValue(name, out bool isRep);
            if (isRep && pos.Zone == senderPos.Zone)
            {
                nodes.Add((name, pos));
            }
        }
        
        // Add local player (destination)
        string localName = Config.Config.Username;
        nodes.Add((localName, _lastSentPos));

        int n = nodes.Count;
        
        // Find a path from sender (index 0) to local player (index n-1)
        // that minimizes the maximum hop distance along the path.
        double[] maxHopDist = new double[n];
        for (int i = 0; i < n; i++) maxHopDist[i] = double.MaxValue;
        maxHopDist[0] = 0;

        bool[] visited = new bool[n];

        for (int step = 0; step < n; step++)
        {
            // Find unvisited node with minimum maxHopDist
            int u = -1;
            double minDist = double.MaxValue;
            for (int i = 0; i < n; i++)
            {
                if (!visited[i] && maxHopDist[i] < minDist)
                {
                    minDist = maxHopDist[i];
                    u = i;
                }
            }

            if (u == -1) break;
            visited[u] = true;

            if (u == n - 1) break;

            // Relax neighbors
            for (int v = 0; v < n; v++)
            {
                if (visited[v]) continue;

                double dx = nodes[u].Pos.X - nodes[v].Pos.X;
                double dy = nodes[u].Pos.Y - nodes[v].Pos.Y;
                double dz = nodes[u].Pos.Z - nodes[v].Pos.Z;
                double edgeDist = Math.Sqrt(dx * dx + dy * dy + dz * dz);

                // Hops must be under 8000m
                if (edgeDist <= 8000.0)
                {
                    double currentMaxHop = Math.Max(maxHopDist[u], edgeDist);
                    if (currentMaxHop < maxHopDist[v])
                    {
                        maxHopDist[v] = currentMaxHop;
                    }
                }
            }
        }

        double routedMaxHop = maxHopDist[n - 1];
        if (routedMaxHop > 8000.0)
        {
            // No path under 8000m exists
            double directDx = senderPos.X - _lastSentPos.X;
            double directDy = senderPos.Y - _lastSentPos.Y;
            double directDz = senderPos.Z - _lastSentPos.Z;
            return Math.Sqrt(directDx * directDx + directDy * directDy + directDz * directDz);
        }

        return routedMaxHop;
    }

    public void SetMockPttPaState(bool isDown)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            _isPttPaDown = isDown;
            UpdatePttState();
        });
    }

    private void UpdateDiscordPresence()
    {
        if (!AudioConnected)
        {
            _discordRpc.UpdatePresence("Idle / Disconnected", "Disconnected");
            return;
        }

        string details = $"At {CurrentZone}";
        string state = "In Proximity";

        if (!string.IsNullOrEmpty(_activeChannel))
        {
            state = $"On Radio: {_activeChannel}";
        }

        if (IsHelmetOn)
        {
            state += " (Helmet On)";
        }

        _discordRpc.UpdatePresence(details, state);
    }

    private void HandleVoiceCommandHotkey(bool isDown)
    {
        if (isDown)
        {
            if (_isVoiceCommandListening) return; // Prevent repeats
            _isVoiceCommandListening = true;

            // Clear visual reset timer if running
            _voiceCommandResetTimer?.Stop();

            // Set HUD listening state
            ShowVoiceCommandPanel = true;
            IsVoiceListening = true;
            VoiceCommandStatusColor = "Cyan";
            VoiceCommandStatusText = Application.Current?.TryFindResource("TxtVoiceListening") as string ?? "🎙️ AEGIS LISTENING...";

            // Start capture recording
            _capture.StartCommandRecording();
        }
        else
        {
            if (!_isVoiceCommandListening) return;
            _isVoiceCommandListening = false;

            IsVoiceListening = false;

            // Stop capture recording
            float[] samples = _capture.StopCommandRecording();

            if (samples.Length > 0)
            {
                // Queue transcription with a special channel type 255 for internal parsing
                _stt.QueueTranscription("LocalPlayerCommand", samples, 255, Config.Config.Language);
            }
            else
            {
                // Hide panel if no samples
                ShowVoiceCommandPanel = false;
            }
        }
    }

    public void ProcessVoiceCommand(string text)
    {
        var result = _voiceCommand.ParseAndExecute(text, Config.Config.Language, _availableChannels, Config.Config.VoiceCommandConfidence);
        
        Application.Current?.Dispatcher.Invoke(() =>
        {
            if (result.Action != VoiceCommandAction.None)
            {
                string actionLabel = result.Action switch
                {
                    VoiceCommandAction.VisorToggle => Application.Current?.TryFindResource("LblHelmetToggle") as string ?? "Visor Toggle",
                    VoiceCommandAction.MicMuteProximity => "Mute Proximity",
                    VoiceCommandAction.MicUnmuteProximity => "Unmute Proximity",
                    VoiceCommandAction.MicMuteRadio => "Mute Radio",
                    VoiceCommandAction.MicUnmuteRadio => "Unmute Radio",
                    VoiceCommandAction.MicMuteProfile => "Mute Profile",
                    VoiceCommandAction.MicUnmuteProfile => "Unmute Profile",
                    VoiceCommandAction.MicMuteAll => "Mute All",
                    VoiceCommandAction.MicUnmuteAll => "Unmute All",
                    VoiceCommandAction.RadioChannelSwitch => $"Channel: {result.TargetChannel}",
                    VoiceCommandAction.VoiceChangerProfile => $"Voice Changer: {result.TargetProfile}",
                    _ => "Command"
                };

                string successFormat = Application.Current?.TryFindResource("TxtVoiceSuccess") as string ?? "✔️ CMD: {0}";
                VoiceCommandStatusText = string.Format(successFormat, actionLabel);
                VoiceCommandStatusColor = "Green";
            }
            else
            {
                VoiceCommandStatusText = Application.Current?.TryFindResource("TxtVoiceError") as string ?? "❌ CMD NOT RECOGNIZED";
                VoiceCommandStatusColor = "Red";
            }

            ShowVoiceCommandPanel = true;

            // Show for 2 seconds, then reset panel visibility
            _voiceCommandResetTimer?.Stop();
            _voiceCommandResetTimer = new System.Timers.Timer(2000);
            _voiceCommandResetTimer.Elapsed += (s, e) =>
            {
                _voiceCommandResetTimer.Stop();
                ShowVoiceCommandPanel = false;
            };
            _voiceCommandResetTimer.Start();
        });
    }

    private void OnCaptionDecoded(string playerName, string text, byte channelType)
    {
        if (playerName == "LocalPlayerCommand" && channelType == 255)
        {
            ProcessVoiceCommand(text);
        }
    }

    private CancellationTokenSource? _hailChimeCts;

    private void StartHailChimeLoop(HailState state)
    {
        _hailChimeCts?.Cancel();
        _hailChimeCts = new CancellationTokenSource();
        var ct = _hailChimeCts.Token;

        Task.Run(async () =>
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    if (state == HailState.Outgoing && CurrentHailState == HailState.Outgoing)
                    {
                        _playback.PlayOutgoingHailFeedback();
                        await Task.Delay(2000, ct);
                    }
                    else if (state == HailState.Incoming && CurrentHailState == HailState.Incoming)
                    {
                        _playback.PlayIncomingHailFeedback();
                        await Task.Delay(1500, ct);
                    }
                    else
                    {
                        break;
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                LogService.Error("Error in HailChimeLoop", ex);
            }
        }, ct);
    }

    private void StopHailChimeLoop()
    {
        _hailChimeCts?.Cancel();
        _hailChimeCts = null;
    }

    public async Task InitiateHailCallAsync()
    {
        if (!_posWs.IsConnected)
        {
            StatusMessage = "Cannot initiate call: Disconnected from position server.";
            return;
        }

        if (CurrentHailState != HailState.Idle)
        {
            StatusMessage = "Cannot initiate call: Already in a calling state.";
            return;
        }

        var localPos = LastSentPos;
        if (localPos == null || localPos.IsEmpty)
        {
            StatusMessage = "Cannot initiate call: Your location is unknown.";
            return;
        }

        string? closestPlayerName = null;
        double minDistance = double.MaxValue;

        foreach (var kvp in _remotePositions)
        {
            var remotePos = kvp.Value;
            if (remotePos.Zone != localPos.Zone) continue;

            double dx = remotePos.X - localPos.X;
            double dy = remotePos.Y - localPos.Y;
            double dz = remotePos.Z - localPos.Z;
            double dist = Math.Sqrt(dx * dx + dy * dy + dz * dz);

            if (dist <= 5000.0 && dist < minDistance)
            {
                minDistance = dist;
                closestPlayerName = kvp.Key;
            }
        }

        if (closestPlayerName == null)
        {
            StatusMessage = "No cockpit targets found within 5,000m.";
            return;
        }

        StatusMessage = $"Initiating private calling link to {closestPlayerName}...";
        await _posWs.SendHailRequestAsync(closestPlayerName);
    }

    public void InitiateHailCall()
    {
        _ = InitiateHailCallAsync();
    }

    public async Task AcceptHailCallAsync()
    {
        if (CurrentHailState != HailState.Incoming) return;
        StatusMessage = "Accepting incoming call...";
        await _posWs.SendHailAcceptAsync();
    }

    public void AcceptHailCall()
    {
        _ = AcceptHailCallAsync();
    }

    public async Task DeclineHailCallAsync()
    {
        if (CurrentHailState == HailState.Idle) return;
        StatusMessage = "Ending call...";
        await _posWs.SendHailDeclineAsync();
    }

    public void DeclineHailCall()
    {
        _ = DeclineHailCallAsync();
    }
}
