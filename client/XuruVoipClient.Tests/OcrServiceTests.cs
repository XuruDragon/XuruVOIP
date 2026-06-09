using System;
using System.Reflection;
using Xunit;
using XuruVoipClient.Models;
using XuruVoipClient.Services;

namespace XuruVoipClient.Tests;

public class OcrServiceTests
{
    private readonly MethodInfo _tryParseMethod;

    public OcrServiceTests()
    {
        _tryParseMethod = typeof(OcrService).GetMethod(
            "TryParse",
            BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("TryParse method not found in OcrService");
    }

    [Fact]
    public void TryParse_StandardCoordinates_ShouldParseCorrectly()
    {
        // GIVEN
        var service = new OcrService();
        string text = "Zone: Hangar XLTop Area18 854875740883 Pos: -4.98m -10.00m -114.03m";

        // WHEN
        var pos = (PlayerPosition?)_tryParseMethod.Invoke(service, new object[] { text });

        // THEN
        Assert.NotNull(pos);
        Assert.Equal("Hangar XLTop Area18 854875740883", pos.Zone);
        Assert.Equal(-4.98, pos.X);
        Assert.Equal(-10.00, pos.Y);
        Assert.Equal(-114.03, pos.Z);
    }

    [Fact]
    public void TryParse_KmCoordinates_ShouldConvertToMeters()
    {
        // GIVEN
        var service = new OcrService();
        string text = "Zone: Area18 Pos: 12.5km 0.0m -34.78km";

        // WHEN
        var pos = (PlayerPosition?)_tryParseMethod.Invoke(service, new object[] { text });

        // THEN
        Assert.NotNull(pos);
        Assert.Equal("Area18", pos.Zone);
        Assert.Equal(12500.0, pos.X);
        Assert.Equal(0.0, pos.Y);
        Assert.Equal(-34780.0, pos.Z);
    }

    [Fact]
    public void TryParse_MultipleLines_ShouldPrioritizeCleanZoneOverTooSpecificZone()
    {
        // GIVEN
        var service = new OcrService();
        // The first line has "elevator" (too specific keyword), the second is a normal zone.
        string text = "Zone: elevator_cabin_02 Pos: 1.0m 2.0m 3.0m\r\nZone: Area18 Hangar 1 Pos: 10.0m 20.0m 30.0m";

        // WHEN
        var pos = (PlayerPosition?)_tryParseMethod.Invoke(service, new object[] { text });

        // THEN: Should select the second line because it is not "too specific"
        Assert.NotNull(pos);
        Assert.Equal("Area18 Hangar 1", pos.Zone);
        Assert.Equal(10.0, pos.X);
    }

    [Fact]
    public void TryParse_MultipleLines_ShouldPrioritizeCleanZoneOverTooLargeZone()
    {
        // GIVEN
        var service = new OcrService();
        // The first line has "solarsystem" (too large keyword), the second is a normal zone.
        string text = "Zone: Stanton_solarsystem Pos: 1000.0m 2000.0m 3000.0m\nZone: Area18 Pos: 10.0m 20.0m 30.0m";

        // WHEN
        var pos = (PlayerPosition?)_tryParseMethod.Invoke(service, new object[] { text });

        // THEN: Should select the second line
        Assert.NotNull(pos);
        Assert.Equal("Area18", pos.Zone);
        Assert.Equal(10.0, pos.X);
    }

    [Fact]
    public void TryParse_AllSpecificZones_ShouldFallbackToFirstNonLargeSpecificZone()
    {
        // GIVEN
        var service = new OcrService();
        // Both lines contain specific keywords ("elevator" and "transit") but none are large.
        string text = "Zone: elevator_cabin_02 Pos: 1.0m 2.0m 3.0m\nZone: transit_carriage_01 Pos: 10.0m 20.0m 30.0m";

        // WHEN
        var pos = (PlayerPosition?)_tryParseMethod.Invoke(service, new object[] { text });

        // THEN: Should fallback to the first one that is not large
        Assert.NotNull(pos);
        Assert.Equal("elevator_cabin_02", pos.Zone);
        Assert.Equal(1.0, pos.X);
    }

    [Fact]
    public void TryParse_AllLargeZones_ShouldFallbackToFirstOne()
    {
        // GIVEN
        var service = new OcrService();
        // All lines are large (system / solarsystem)
        string text = "Zone: Stanton_SolarSystem Pos: 1000.0m 2000.0m 3000.0m\r\nZone: Pyro_System Pos: 50.0m 50.0m 50.0m";

        // WHEN
        var pos = (PlayerPosition?)_tryParseMethod.Invoke(service, new object[] { text });

        // THEN: Should select the first line
        Assert.NotNull(pos);
        Assert.Equal("Stanton_SolarSystem", pos.Zone);
        Assert.Equal(1000.0, pos.X);
    }

    [Fact]
    public void TryParse_InvalidFormat_ReturnsNull()
    {
        // GIVEN
        var service = new OcrService();
        string text = "Random text without matching keys";

        // WHEN
        var pos = (PlayerPosition?)_tryParseMethod.Invoke(service, new object[] { text });

        // THEN
        Assert.Null(pos);
    }
}
