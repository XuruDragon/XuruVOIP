using System;
using Xunit;
using XuruVoipClient.Services;

namespace XuruVoipClient.Tests;

public class JitterBufferTests
{
    [Fact]
    public void CompareSequenceNumbers_ShouldHandleNormalAndWrapAroundCorrectly()
    {
        // Standard comparisons
        Assert.Equal(1, JitterBuffer.CompareSequenceNumbers(10, 5));
        Assert.Equal(-1, JitterBuffer.CompareSequenceNumbers(5, 10));
        Assert.Equal(0, JitterBuffer.CompareSequenceNumbers(7, 7));

        // Wrap-around boundary comparisons
        // 0 is newer than 65535
        Assert.Equal(1, JitterBuffer.CompareSequenceNumbers(0, 65535));
        Assert.Equal(-1, JitterBuffer.CompareSequenceNumbers(65535, 0));

        // 5 is newer than 65530
        Assert.Equal(1, JitterBuffer.CompareSequenceNumbers(5, 65530));
        Assert.Equal(-1, JitterBuffer.CompareSequenceNumbers(65530, 5));

        // Large difference but not wrapping (less than 32768)
        Assert.Equal(1, JitterBuffer.CompareSequenceNumbers(30000, 10));
        Assert.Equal(-1, JitterBuffer.CompareSequenceNumbers(10, 30000));
    }

    [Fact]
    public void JitterBuffer_ShouldPrebufferBeforeReleasingPackets()
    {
        var buffer = new JitterBuffer();

        // 1. First packet: still buffering, should return null
        buffer.Enqueue(new AudioPacket { SequenceNumber = 10, OpusData = new byte[] { 1 } });
        var p1 = buffer.Dequeue(out bool plc1);
        Assert.Null(p1);
        Assert.False(plc1);

        // 2. Second packet: still buffering, should return null
        buffer.Enqueue(new AudioPacket { SequenceNumber = 11, OpusData = new byte[] { 2 } });
        var p2 = buffer.Dequeue(out bool plc2);
        Assert.Null(p2);
        Assert.False(plc2);

        // 3. Third packet (completing TargetDelayFrames = 3): should now release packets in order
        buffer.Enqueue(new AudioPacket { SequenceNumber = 12, OpusData = new byte[] { 3 } });
        
        var p3 = buffer.Dequeue(out bool plc3);
        Assert.NotNull(p3);
        Assert.Equal(10, p3.SequenceNumber);
        Assert.False(plc3);

        var p4 = buffer.Dequeue(out bool plc4);
        Assert.NotNull(p4);
        Assert.Equal(11, p4.SequenceNumber);
        Assert.False(plc4);

        var p5 = buffer.Dequeue(out bool plc5);
        Assert.NotNull(p5);
        Assert.Equal(12, p5.SequenceNumber);
        Assert.False(plc5);
    }

    [Fact]
    public void JitterBuffer_ShouldSortOutofOrderPackets()
    {
        var buffer = new JitterBuffer();

        // Enqueue 3 packets out of order
        buffer.Enqueue(new AudioPacket { SequenceNumber = 102, OpusData = new byte[] { 2 } });
        buffer.Enqueue(new AudioPacket { SequenceNumber = 101, OpusData = new byte[] { 1 } });
        buffer.Enqueue(new AudioPacket { SequenceNumber = 103, OpusData = new byte[] { 3 } });

        // Dequeue should release them sorted: 101, 102, 103
        var p1 = buffer.Dequeue(out bool plc1);
        Assert.NotNull(p1);
        Assert.Equal(101, p1.SequenceNumber);

        var p2 = buffer.Dequeue(out bool plc2);
        Assert.NotNull(p2);
        Assert.Equal(102, p2.SequenceNumber);

        var p3 = buffer.Dequeue(out bool plc3);
        Assert.NotNull(p3);
        Assert.Equal(103, p3.SequenceNumber);
    }

    [Fact]
    public void JitterBuffer_ShouldDetectGapAndTriggerPlc()
    {
        var buffer = new JitterBuffer();

        // Enqueue packets 10 and 12 (missing 11)
        buffer.Enqueue(new AudioPacket { SequenceNumber = 10, OpusData = new byte[] { 10 } });
        buffer.Enqueue(new AudioPacket { SequenceNumber = 12, OpusData = new byte[] { 12 } });
        
        // Wait, pre-buffering needs 3 packets or underflow reset.
        // Let's add 13 to satisfy the 3-packet target delay
        buffer.Enqueue(new AudioPacket { SequenceNumber = 13, OpusData = new byte[] { 13 } });

        // First dequeue: should get packet 10
        var p1 = buffer.Dequeue(out bool plc1);
        Assert.NotNull(p1);
        Assert.Equal(10, p1.SequenceNumber);
        Assert.False(plc1);

        // Second dequeue: expected is 11, which is missing, but newer packets (12, 13) are present.
        // This should trigger PLC.
        var p2 = buffer.Dequeue(out bool plc2);
        Assert.Null(p2);
        Assert.True(plc2); // PLC triggered!

        // Third dequeue: should now receive packet 12
        var p3 = buffer.Dequeue(out bool plc3);
        Assert.NotNull(p3);
        Assert.Equal(12, p3.SequenceNumber);
        Assert.False(plc3);
    }

    [Fact]
    public void JitterBuffer_ShouldResetBufferingAfterUnderflow()
    {
        var buffer = new JitterBuffer();

        // Enqueue 3 packets and read them all
        buffer.Enqueue(new AudioPacket { SequenceNumber = 1, OpusData = new byte[] { 1 } });
        buffer.Enqueue(new AudioPacket { SequenceNumber = 2, OpusData = new byte[] { 2 } });
        buffer.Enqueue(new AudioPacket { SequenceNumber = 3, OpusData = new byte[] { 3 } });

        Assert.NotNull(buffer.Dequeue(out _));
        Assert.NotNull(buffer.Dequeue(out _));
        Assert.NotNull(buffer.Dequeue(out _));

        // Now buffer is empty (underflow).
        // Enqueueing a new packet should require pre-buffering again.
        buffer.Enqueue(new AudioPacket { SequenceNumber = 4, OpusData = new byte[] { 4 } });
        var p = buffer.Dequeue(out bool plc);
        Assert.Null(p);
        Assert.False(plc); // Should be buffering, not PLC
    }
}
