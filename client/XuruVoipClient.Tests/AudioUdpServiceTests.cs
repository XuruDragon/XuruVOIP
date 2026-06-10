using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using Xunit;
using XuruVoipClient.Services;

namespace XuruVoipClient.Tests;

public class AudioUdpServiceTests
{
    [Fact(Timeout = 5000)]
    public async Task AudioUdpService_ShouldHandshakeAndTransmitCorrectly()
    {
        // GIVEN
        var service = new AudioUdpService();
        var localPort = 19123;
        using var mockServer = new UdpClient(localPort);
        using var cts = new System.Threading.CancellationTokenSource(4000);

        // Connect client to local mock server
        var connectionTask = service.ConnectAsync("127.0.0.1", localPort, "TestUser", "testtoken", "ticket12345678901234567890123456");

        // Server receives registration packet: [0xFF] [NameLen] [Name] [Ticket]
        var receiveReg = await mockServer.ReceiveAsync(cts.Token);
        byte[] regData = receiveReg.Buffer;
        Assert.Equal(0xFF, regData[0]); // Registration ID
        int nameLen = regData[1];
        Assert.Equal("TestUser", System.Text.Encoding.UTF8.GetString(regData, 2, nameLen));

        // Server replies with ACK: [0xFE]
        await mockServer.SendAsync(new byte[] { 0xFE }, 1, receiveReg.RemoteEndPoint);

        // Client connection should complete successfully
        bool connected = await connectionTask;
        Assert.True(connected);
        Assert.True(service.IsConnected);

        // Send first audio frame: Radio type with 2 dummy bytes
        await service.SendAudioFrameAsync(0x01, new byte[] { 0xAA, 0xBB });
        var frame1 = await mockServer.ReceiveAsync(cts.Token);
        byte[] data1 = frame1.Buffer;
        
        Assert.Equal(5, data1.Length); // [Seq (2)] [Type (1)] [Payload (2)]
        ushort seq1 = (ushort)((data1[0] << 8) | data1[1]);
        Assert.Equal(0x01, data1[2]); // Radio type
        Assert.Equal(0xAA, data1[3]);
        Assert.Equal(0xBB, data1[4]);

        // Send second audio frame
        await service.SendAudioFrameAsync(0x01, new byte[] { 0xCC, 0xDD });
        var frame2 = await mockServer.ReceiveAsync(cts.Token);
        byte[] data2 = frame2.Buffer;
        
        Assert.Equal(5, data2.Length);
        ushort seq2 = (ushort)((data2[0] << 8) | data2[1]);
        Assert.Equal(0x01, data2[2]);
        Assert.Equal(0xCC, data2[3]);
        Assert.Equal(0xDD, data2[4]);

        // Sequence numbers must increment sequentially
        Assert.Equal(1, (ushort)(seq2 - seq1));

        // Cleanup
        await service.DisposeAsync();
    }
}
