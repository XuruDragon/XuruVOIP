using System;
using System.Reflection;
using Xunit;
using XuruVoipClient.Models;
using XuruVoipClient.Services;

namespace XuruVoipClient.Tests;

public class GameDetectionServiceTests
{
    private readonly MethodInfo _processLogLineMethod;

    public GameDetectionServiceTests()
    {
        _processLogLineMethod = typeof(GameDetectionService).GetMethod(
            "ProcessLogLine",
            BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("ProcessLogLine method not found");
    }

    [Fact]
    public void ProcessLogLine_WithMetersCoordinates_ShouldParseCorrectly()
    {
        // GIVEN
        var service = new GameDetectionService();
        PlayerPosition? parsedPos = null;
        service.PositionReceived += pos => parsedPos = pos;

        // WHEN
        string logLine = "Zone: Stanton (Stanton) Pos: 1000.5m -2000.25m 50.0m";
        _processLogLineMethod.Invoke(service, new object[] { logLine });

        // THEN
        Assert.NotNull(parsedPos);
        Assert.Equal("Stanton (Stanton)", parsedPos.Zone);
        Assert.Equal(1000.5, parsedPos.X);
        Assert.Equal(-2000.25, parsedPos.Y);
        Assert.Equal(50.0, parsedPos.Z);
    }

    [Fact]
    public void ProcessLogLine_WithKmCoordinates_ShouldConvertToMeters()
    {
        // GIVEN
        var service = new GameDetectionService();
        PlayerPosition? parsedPos = null;
        service.PositionReceived += pos => parsedPos = pos;

        // WHEN
        string logLine = "Zone: MicroTech Pos: 1.5km -2.0km 0.0km";
        _processLogLineMethod.Invoke(service, new object[] { logLine });

        // THEN
        Assert.NotNull(parsedPos);
        Assert.Equal("MicroTech", parsedPos.Zone);
        Assert.Equal(1500.0, parsedPos.X);
        Assert.Equal(-2000.0, parsedPos.Y);
        Assert.Equal(0.0, parsedPos.Z);
    }

    [Fact]
    public void ProcessLogLine_WithInvalidCoordinates_ShouldNotTriggerEvent()
    {
        // GIVEN
        var service = new GameDetectionService();
        PlayerPosition? parsedPos = null;
        service.PositionReceived += pos => parsedPos = pos;

        // WHEN
        string logLine = "Zone: MicroTech Pos: invalid coordinates";
        _processLogLineMethod.Invoke(service, new object[] { logLine });

        // THEN
        Assert.Null(parsedPos);
    }

    [Fact]
    public void ProcessLogLine_HelmetEquipped_ShouldTriggerStateChangedTrue()
    {
        // GIVEN
        var service = new GameDetectionService();
        bool? helmetOn = null;
        service.HelmetStateChanged += state => helmetOn = state;

        // WHEN: Helmet attached to Armor_Helmet port
        string logLine = "<AttachmentReceived> Player[xurud] Attachment[FP_Helmet_Default] Status[Equipped] Port[Armor_Helmet]";
        _processLogLineMethod.Invoke(service, new object[] { logLine });

        // THEN
        Assert.True(helmetOn);
    }

    [Fact]
    public void ProcessLogLine_HelmetUnequipped_ShouldTriggerStateChangedFalse()
    {
        // GIVEN
        var service = new GameDetectionService();
        bool? helmetOn = null;
        service.HelmetStateChanged += state => helmetOn = state;

        // WHEN: Helmet attached to another port or removed
        string logLine = "<AttachmentReceived> Player[xurud] Attachment[FP_Helmet_Default] Status[Unequipped] Port[None]";
        _processLogLineMethod.Invoke(service, new object[] { logLine });

        // THEN
        Assert.False(helmetOn);
    }

    [Fact]
    public void ProcessLogLine_VisorEquipped_ShouldBeIgnored()
    {
        // GIVEN
        var service = new GameDetectionService();
        bool? helmetOn = null;
        service.HelmetStateChanged += state => helmetOn = state;

        // WHEN: Visor attached (not the main helmet)
        string logLine = "<AttachmentReceived> Player[xurud] Attachment[FP_Visor_Helmet] Status[Equipped] Port[Visor]";
        _processLogLineMethod.Invoke(service, new object[] { logLine });

        // THEN: Event should not be raised since visor is excluded
        Assert.Null(helmetOn);
    }
}
