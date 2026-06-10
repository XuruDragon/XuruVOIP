using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace XuruVoipClient.Services;

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
/// Manages the UDP connection to the audio server (port 8889).
/// Sends encoded Opus frames prepended with sequence numbers, handles periodic registration,
/// and receives/decodes audio packets with jitter buffer support.
/// </summary>
public class AudioUdpService : IAsyncDisposable
{
    private UdpClient? _udpClient;
    private CancellationTokenSource? _cts;
    private Task? _receiveTask;
    private Task? _registrationTask;
    private TaskCompletionSource<bool>? _connectTcs;
    private IPEndPoint? _serverEndpoint;
    private string _username = "";
    private string _audioTicket = "";
    private bool _isAuthenticated = false;
    private int _sequenceNumber = 0;

    /// <summary>
    /// Fired when an audio packet is received: (senderName, audioType, opusData, metadata, sequenceNumber)
    /// </summary>
    public event Action<string, byte, byte[], ProximityMetadata?, ushort>? AudioPacketReceived;
    public event Action? Connected;
    public event Action<string>? Disconnected;

    public bool IsConnected => _udpClient != null && _isAuthenticated;

    public async Task<bool> ConnectAsync(string serverAddress, int audioPort, string name, string token, string audioTicket)
    {
        LogService.Info($"Initiating UDP connection to Audio Server for user '{name}'...");
        Disconnect();
        
        _username = name;
        _audioTicket = audioTicket;
        _cts = new CancellationTokenSource();
        _connectTcs = new TaskCompletionSource<bool>();

        try
        {
            string host = serverAddress.Trim();
            if (host.Contains("://"))
            {
                host = new Uri(host).Host;
            }

            IPAddress[] addresses = await Dns.GetHostAddressesAsync(host, _cts.Token);
            if (addresses.Length == 0)
            {
                throw new Exception($"Could not resolve host: {host}");
            }

            // Prefer IPv4 for local/LAN gaming setups if available
            IPAddress? targetAddress = null;
            foreach (var addr in addresses)
            {
                if (addr.AddressFamily == AddressFamily.InterNetwork)
                {
                    targetAddress = addr;
                    break;
                }
            }
            targetAddress ??= addresses[0];

            _serverEndpoint = new IPEndPoint(targetAddress, audioPort);
            LogService.Info($"Audio Server resolved to: {_serverEndpoint}");

            // Create UdpClient bound to ephemeral local port (port 0)
            _udpClient = new UdpClient(0, targetAddress.AddressFamily);
            
            // Start receive loop
            _receiveTask = ReceiveLoopAsync(_cts.Token);

            // Start periodic registration loop
            _registrationTask = RegistrationLoopAsync(_cts.Token);

            // Wait for handshake completion with 5-second timeout
            using var delayCts = new CancellationTokenSource(5000);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, delayCts.Token);
            
            // Handle cancellation or timeout
            linkedCts.Token.Register(() => _connectTcs.TrySetResult(false));

            bool success = await _connectTcs.Task;
            if (success && _isAuthenticated)
            {
                LogService.Info("Successfully authenticated and bound UDP port with Audio Server.");
                Connected?.Invoke();
                return true;
            }
            else
            {
                LogService.Error("Failed to authenticate with UDP Audio Server (handshake timeout or rejected).");
                Disconnect();
                Disconnected?.Invoke("UDP handshake timeout or rejected");
                return false;
            }
        }
        catch (Exception ex)
        {
            LogService.Error("Exception during UDP connection setup", ex);
            Disconnect();
            Disconnected?.Invoke(ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Sends an Opus-encoded audio frame.
    /// Packet format: [Seq (2 bytes - BigEndian)] + [audioType (1 byte)] + [opusData...]
    /// </summary>
    public async Task SendAudioFrameAsync(byte audioType, byte[] opusData)
    {
        var client = _udpClient;
        var endpoint = _serverEndpoint;
        if (client == null || endpoint == null || !_isAuthenticated) return;

        try
        {
            ushort seq = (ushort)(Interlocked.Increment(ref _sequenceNumber) & 0xFFFF);
            
            byte[] packet = new byte[3 + opusData.Length];
            packet[0] = (byte)((seq >> 8) & 0xFF);
            packet[1] = (byte)(seq & 0xFF);
            packet[2] = audioType;
            Buffer.BlockCopy(opusData, 0, packet, 3, opusData.Length);

            await client.SendAsync(packet, packet.Length, endpoint);
        }
        catch (Exception ex)
        {
            LogService.Error("Error sending audio frame over UDP", ex);
        }
    }

    private async Task RegistrationLoopAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                await SendRegistrationPacketAsync();
                await Task.Delay(1000, ct);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            LogService.Error("Error in UDP registration/keep-alive loop", ex);
        }
    }

    private async Task SendRegistrationPacketAsync()
    {
        var client = _udpClient;
        var endpoint = _serverEndpoint;
        if (client == null || endpoint == null) return;

        try
        {
            byte[] nameBytes = Encoding.UTF8.GetBytes(_username);
            byte[] ticketBytes = Encoding.UTF8.GetBytes(_audioTicket);

            byte[] packet = new byte[2 + nameBytes.Length + 32];
            packet[0] = 0xFF; // Registration packet identifier
            packet[1] = (byte)nameBytes.Length;
            Buffer.BlockCopy(nameBytes, 0, packet, 2, nameBytes.Length);

            int ticketLen = Math.Min(ticketBytes.Length, 32);
            Buffer.BlockCopy(ticketBytes, 0, packet, 2 + nameBytes.Length, ticketLen);

            await client.SendAsync(packet, packet.Length, endpoint);
        }
        catch (Exception ex)
        {
            LogService.Error("Failed to send UDP registration/keep-alive packet", ex);
        }
    }

    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var client = _udpClient;
            if (client == null) break;

            try
            {
                var result = await client.ReceiveAsync(ct);
                byte[] data = result.Buffer;

                if (data.Length == 0) continue;

                // 1. Handshake ACK verification
                if (data.Length == 1 && data[0] == 0xFE)
                {
                    if (!_isAuthenticated)
                    {
                        _isAuthenticated = true;
                        _connectTcs?.TrySetResult(true);
                    }
                    continue;
                }

                // 2. Relayed Audio Packets: [Seq (2)] [AudioType (1)] [NameLen (1)] [Name (NameLen)] [Metadata/Payload]
                if (data.Length < 4) continue;

                ushort seq = (ushort)((data[0] << 8) | data[1]);
                byte audioType = data[2];
                int nameLen = data[3];

                if (data.Length < 4 + nameLen) continue;
                string senderName = Encoding.UTF8.GetString(data, 4, nameLen);
                int offset = 4 + nameLen;

                ProximityMetadata? metadata = null;
                int opusStart = offset;

                if (audioType == 0x00) // Proximity Audio with Spatial Metadata
                {
                    // Check minimum: [SpatialEnabled (1)] [Distance (4)] [MaxRange (4)] = 9 bytes
                    if (data.Length < offset + 9) continue;

                    byte spatialEnabledByte = data[offset];
                    bool spatialEnabled = spatialEnabledByte != 0;
                    float distance = BitConverter.ToSingle(data, offset + 1);
                    float maxRange = BitConverter.ToSingle(data, offset + 5);

                    metadata = new ProximityMetadata
                    {
                        SpatialEnabled = spatialEnabled,
                        Distance = distance,
                        MaxRange = maxRange
                    };

                    if (spatialEnabled)
                    {
                        // Needs additional: [SpeakerX (4)] [SpeakerY (4)] [SpeakerZ (4)] = 12 bytes
                        if (data.Length < offset + 21) continue;
                        metadata.SpeakerX = BitConverter.ToSingle(data, offset + 9);
                        metadata.SpeakerY = BitConverter.ToSingle(data, offset + 13);
                        metadata.SpeakerZ = BitConverter.ToSingle(data, offset + 17);
                        opusStart = offset + 21;
                    }
                    else
                    {
                        opusStart = offset + 9;
                    }
                }

                int opusLen = data.Length - opusStart;
                if (opusLen <= 0 && data.Length > 0)
                {
                    // Keep-alive or end-of-transmission empty frame
                    AudioPacketReceived?.Invoke(senderName, audioType, Array.Empty<byte>(), metadata, seq);
                    continue;
                }

                byte[] opusData = new byte[opusLen];
                Buffer.BlockCopy(data, opusStart, opusData, 0, opusLen);

                AudioPacketReceived?.Invoke(senderName, audioType, opusData, metadata, seq);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                // Don't crash receive thread on socket errors
                if (client != _udpClient) break; // Client was replaced/closed
                LogService.Error("Error in UDP receive loop", ex);
                
                if (ex is InvalidOperationException || ex is ObjectDisposedException)
                {
                    await Task.Delay(100, ct);
                }
            }
        }
    }

    public void Disconnect()
    {
        if (_udpClient != null)
        {
            LogService.Info("Disconnecting UDP Audio client...");
        }
        _isAuthenticated = false;
        _connectTcs?.TrySetResult(false);
        _cts?.Cancel();
        _udpClient?.Dispose();
        _udpClient = null;
    }

    public async ValueTask DisposeAsync()
    {
        Disconnect();
        if (_receiveTask != null)
        {
            try { await _receiveTask; } catch { }
        }
        if (_registrationTask != null)
        {
            try { await _registrationTask; } catch { }
        }
    }
}
