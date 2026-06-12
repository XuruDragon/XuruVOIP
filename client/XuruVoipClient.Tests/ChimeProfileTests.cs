using System;
using System.Linq;
using System.Reflection;
using Xunit;
using XuruVoipClient.Services;

namespace XuruVoipClient.Tests;

public class ChimeProfileTests
{
    [Fact]
    public void ChimeTypeChange_ShouldRegenerateChimesCorrectly()
    {
        // GIVEN
        var service = new AudioPlaybackService();

        // WHEN: Default is Military
        Assert.Equal("Military", service.PttChimeType);

        // Retrieve private float arrays KeyDownChime and KeyUpChime via reflection
        var kdField = typeof(AudioPlaybackService).GetField("KeyDownChime", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("KeyDownChime field not found");
        var kuField = typeof(AudioPlaybackService).GetField("KeyUpChime", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("KeyUpChime field not found");

        var milKd = (float[])kdField.GetValue(service)!;
        var milKu = (float[])kuField.GetValue(service)!;

        Assert.Equal(2400, milKd.Length); // 50ms @ 48kHz
        Assert.Equal(8640, milKu.Length); // 180ms @ 48kHz

        // WHEN: Change to Industrial
        service.PttChimeType = "Industrial";
        var indKd = (float[])kdField.GetValue(service)!;
        var indKu = (float[])kuField.GetValue(service)!;

        Assert.Equal(2880, indKd.Length); // 60ms @ 48kHz
        Assert.Equal(5760, indKu.Length); // 120ms @ 48kHz
        Assert.True(indKd.Any(s => s != 0f));
        Assert.True(indKu.Any(s => s != 0f));

        // WHEN: Change to Alien
        service.PttChimeType = "Alien";
        var alienKd = (float[])kdField.GetValue(service)!;
        var alienKu = (float[])kuField.GetValue(service)!;

        Assert.Equal(3840, alienKd.Length); // 80ms @ 48kHz
        Assert.Equal(4800, alienKu.Length); // 100ms @ 48kHz
        Assert.True(alienKd.Any(s => s != 0f));
        Assert.True(alienKu.Any(s => s != 0f));

        // WHEN: Change to Vintage
        service.PttChimeType = "Vintage";
        var vinKd = (float[])kdField.GetValue(service)!;
        var vinKu = (float[])kuField.GetValue(service)!;

        Assert.Equal(1920, vinKd.Length); // 40ms @ 48kHz
        Assert.Equal(3840, vinKu.Length); // 80ms @ 48kHz
        Assert.True(vinKd.Any(s => s != 0f));
        Assert.True(vinKu.Any(s => s != 0f));
    }
}
