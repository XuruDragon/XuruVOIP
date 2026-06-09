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
    private readonly AudioWebSocketService _audioWs = new();
    private readonly AudioCaptureService _capture = new();
    private readonly AudioPlaybackService _playback = new();
    private readonly GlobalKeyHook _keyHook = new();
    public GlobalKeyHook KeyHook => _keyHook;
    private readonly DispatcherTimer _ocrTimer = new();
    private readonly GameDetectionService _gameDetector = new();

    // ─── Observable state ────────────────────────────────────────────────────
    private readonly Dictionary<string, bool> _remoteHelmets = [];
    private readonly List<string> _availableChannels = [];
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
        // Extract tessdata and initialize OCR
        try
        {
            LogService.Info("Initializing OCR Service...");
            var tessDir = ConfigService.EnsureTessdata();
            _ocr.Initialize(tessDir);
            LogService.Info("OCR Service initialized successfully.");
        }
        catch (Exception ex)
        {
            StatusMessage = $"OCR init failed: {ex.Message}";
            LogService.Error("OCR initialization failed", ex);
        }

        // Hotkeys configuration
        _keyHook.KeyEvent += (key, isDown) =>
        {
            string keyStr = key.ToString();
            var cfg = Config.Config;

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
        _gameDetector.CustomGameLogPath = Config.Config.CustomGameLogPath;
        _gameDetector.Start();

        // OCR timer
        _ocrTimer.Interval = TimeSpan.FromMilliseconds(Config.Config.OcrIntervalMs);
        _ocrTimer.Tick += OnOcrTick;
        _ocrTimer.Start();

        // VU meter and transmitting indicator refresh
        var vuTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        vuTimer.Tick += (_, _) =>
        {
            var cfg = Config.Config;
            if (cfg.AudioMode == AudioMode.PTT)
            {
                bool anyPttPressed = _isPttProximityDown || _isPttRadioDown || _isPttProfileDown;
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
        });
        _posWs.Disconnected += msg => Application.Current.Dispatcher.Invoke(() =>
        {
            PosConnected = false;
            AudioConnected = false;
            IsSpatialAudioSupportedByServer = true; // Reset to true for offline configuration editing
            string format = Application.Current.TryFindResource("StatusConnectionFailed") as string ?? "Connection failed: {0}";
            StatusMessage = string.Format(format, msg);
        });
        _posWs.WelcomeReceived += (channels, activeChan) => Application.Current.Dispatcher.Invoke(() =>
        {
            _availableChannels.Clear();
            _availableChannels.AddRange(channels);
            _activeChannel = activeChan;
            ActiveChannelName = string.IsNullOrEmpty(activeChan) ? "General" : activeChan;

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

                if (type == "helmet")
                {
                    if (doc.RootElement.TryGetProperty("name", out var nameEl) &&
                        doc.RootElement.TryGetProperty("helmet_on", out var helmEl))
                    {
                        string remoteName = nameEl.GetString() ?? "";
                        bool remoteHelmet = helmEl.GetBoolean();
                        _remoteHelmets[remoteName] = remoteHelmet;
                    }
                }
                else if (type == "leave")
                {
                    if (doc.RootElement.TryGetProperty("name", out var nameEl))
                    {
                        string remoteName = nameEl.GetString() ?? "";
                        _remoteHelmets.Remove(remoteName);
                        _playback.RemovePlayer(remoteName);
                    }
                }
                else if (type == "player_channel")
                {
                    if (doc.RootElement.TryGetProperty("name", out var nameEl) &&
                        doc.RootElement.TryGetProperty("channel", out var chanEl))
                    {
                        string name = nameEl.GetString() ?? "";
                        string channel = chanEl.GetString() ?? "";
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
        });
        _audioWs.Disconnected += msg => Application.Current.Dispatcher.Invoke(() =>
        {
            AudioConnected = false;
            string format = Application.Current.TryFindResource("StatusConnectionFailed") as string ?? "Connection failed: {0}";
            StatusMessage = string.Format(format, msg);
        });

        _audioWs.AudioPacketReceived += (name, type, opus, metadata) =>
        {
            bool localHelmet = IsHelmetOn;
            _remoteHelmets.TryGetValue(name, out bool remoteHelmet);
            bool applyRadio = (type == 0x01 || type == 0x02) || (type == 0x00 && (localHelmet || remoteHelmet));
            _playback.ReceiveOpusFrame(name, opus, type, applyRadio, metadata);
        };

        _capture.EncodedFrameReady += async (frame, txType) =>
        {
            await _audioWs.SendAudioFrameAsync(txType, frame);
        };
        NotifyMicStatusChanged();
        IsHelmetOn = false;
    }

    private void UpdatePttState()
    {
        if (_isPttProfileDown)
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
    }

    public async Task ConnectAsync()
    {
        StatusMessage = Application.Current.TryFindResource("StatusConnecting") as string ?? "Connecting...";
        LogService.Info("ConnectAsync triggered. Connecting to Position Server...");
        var ok = await _posWs.ConnectAsync(Config.Config);
        if (!ok)
        {
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

    private async void OnOcrTick(object? sender, EventArgs e)
    {
        if (!_gameDetector.CheckIfGameFocused()) return;

        var cfg = Config.Config;
        var gameRect = _gameDetector.GetGameClientRectInScreenCoords();
        var pos = await Task.Run(() =>
            _ocr.Capture(cfg.OcrMonitorIndex, cfg.OcrRegion, gameRect));

        if (pos == null) return;

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
            await _posWs.SendPositionAsync(pos);
            _lastSentPos = pos;
        }
    }

    public void Disconnect()
    {
        LogService.Info("Disconnect: Shutting down all network and audio services.");
        _posWs.Disconnect();
        _audioWs.Disconnect();
        _capture.Stop();
        _playback.Stop();
        PosConnected = false;
        AudioConnected = false;
        StatusMessage = Application.Current.TryFindResource("StatusDisconnected") as string ?? "Disconnected";
    }

    public void SaveConfig() => Config.Save();

    public void ApplySettings()
    {
        LogService.Info("ApplySettings: Applying settings update...");
        // Update general logging switch instantly
        LogService.EnableGeneralLogs = Config.Config.EnableGeneralLogs;
        _gameDetector.CustomGameLogPath = Config.Config.CustomGameLogPath;
        _playback.EnableSpatialAudio = Config.Config.EnableSpatialAudio; // Sync spatial audio setting

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
        _ocrTimer.Stop();
        _keyHook.Dispose();
        _gameDetector.Dispose();
        _capture.Dispose();
        _playback.Dispose();
        _ocr.Dispose();
        await _posWs.DisposeAsync();
        await _audioWs.DisposeAsync();
    }
}
