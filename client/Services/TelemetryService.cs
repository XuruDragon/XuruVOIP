using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using XuruVoipClient.ViewModels;

namespace XuruVoipClient.Services;

public class TelemetryService : IDisposable
{
    private readonly MainViewModel _viewModel;
    private UdpClient? _udpClient;
    private CancellationTokenSource? _cts;
    private Task? _broadcastTask;
    private bool _running;
    private int _lastPort;

    public int LastPort => _lastPort;

    public TelemetryService(MainViewModel viewModel)
    {
        _viewModel = viewModel;
    }

    public void Start()
    {
        if (_running) return;
        
        var cfg = _viewModel.Config.Config;
        if (!cfg.EnableTelemetry) return;

        int port = cfg.TelemetryPort;
        if (port <= 0 || port > 65535) port = 8895;

        _lastPort = port;
        _running = true;
        _cts = new CancellationTokenSource();

        try
        {
            _udpClient = new UdpClient();
            LogService.Info($"Telemetry Service started. Broadcasting to 127.0.0.1:{port}");
            _broadcastTask = Task.Run(() => BroadcastLoopAsync(_cts.Token));
        }
        catch (Exception ex)
        {
            LogService.Error($"Failed to start Telemetry Service on port {port}", ex);
            _running = false;
        }
    }

    public void Stop()
    {
        if (!_running) return;
        _running = false;
        _cts?.Cancel();
        try
        {
            _broadcastTask?.GetAwaiter().GetResult();
        }
        catch { }
        _udpClient?.Dispose();
        _udpClient = null;
        _cts?.Dispose();
        _cts = null;
        LogService.Info("Telemetry Service stopped.");
    }

    private async Task BroadcastLoopAsync(CancellationToken ct)
    {
        var endpoint = new IPEndPoint(IPAddress.Loopback, _lastPort);
        while (!ct.IsCancellationRequested)
        {
            try
            {
                if (_udpClient != null)
                {
                    var data = new TelemetryData
                    {
                        Username = _viewModel.Config.Config.Username,
                        IsTransmittingProximity = _viewModel.IsTalking && (_viewModel.Config.Config.AudioType == 0x00),
                        IsTransmittingRadio = _viewModel.IsTalking && (_viewModel.Config.Config.AudioType == 0x01),
                        IsReceivingProximity = _viewModel.Playback.IsReceivingProximity(),
                        IsReceivingRadio = _viewModel.Playback.IsReceivingRadio(),
                        HelmetVisorDown = _viewModel.IsHelmetOn,
                        ActiveChannel = _viewModel.ActiveChannelName,
                        CurrentZone = _viewModel.CurrentZone
                    };

                    string json = JsonSerializer.Serialize(data);
                    byte[] bytes = Encoding.UTF8.GetBytes(json);
                    await _udpClient.SendAsync(bytes, bytes.Length, endpoint);
                }
            }
            catch (Exception ex)
            {
                if (ct.IsCancellationRequested) break;
                LogService.Error("Error in Telemetry Service broadcast loop", ex);
            }

            try
            {
                await Task.Delay(100, ct); // Broadcast every 100ms
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    public void Dispose()
    {
        Stop();
    }
}

public class TelemetryData
{
    public string Username { get; set; } = "";
    public bool IsTransmittingProximity { get; set; }
    public bool IsTransmittingRadio { get; set; }
    public bool IsReceivingProximity { get; set; }
    public bool IsReceivingRadio { get; set; }
    public bool HelmetVisorDown { get; set; }
    public string ActiveChannel { get; set; } = "";
    public string CurrentZone { get; set; } = "";
}
