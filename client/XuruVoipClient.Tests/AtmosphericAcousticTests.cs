using System;
using Xunit;
using XuruVoipClient.Services;

namespace XuruVoipClient.Tests;

public class AtmosphericAcousticTests
{
    [Fact]
    public void GetAtmosphereDistanceMultiplier_ShouldReturnCorrectMultipliers()
    {
        // GIVEN/WHEN/THEN
        // Thin moons should return > 1.0 (rapid volume decay)
        Assert.Equal(3.5, AudioPlaybackService.GetAtmosphereDistanceMultiplier("Cellin"));
        Assert.Equal(3.5, AudioPlaybackService.GetAtmosphereDistanceMultiplier("Ita"));
        Assert.Equal(2.6, AudioPlaybackService.GetAtmosphereDistanceMultiplier("Yela"));
        Assert.Equal(2.6, AudioPlaybackService.GetAtmosphereDistanceMultiplier("Lyria"));
        Assert.Equal(2.1, AudioPlaybackService.GetAtmosphereDistanceMultiplier("Daymar"));

        // Dense gas planets should return < 1.0 (slower volume decay)
        Assert.Equal(0.75, AudioPlaybackService.GetAtmosphereDistanceMultiplier("Crusader"));
        Assert.Equal(0.75, AudioPlaybackService.GetAtmosphereDistanceMultiplier("Arial"));

        // Standard planets should return 1.0
        Assert.Equal(1.0, AudioPlaybackService.GetAtmosphereDistanceMultiplier("MicroTech"));
        Assert.Equal(1.0, AudioPlaybackService.GetAtmosphereDistanceMultiplier("Hurston"));
        Assert.Equal(1.0, AudioPlaybackService.GetAtmosphereDistanceMultiplier("ArcCorp"));

        // Indoor/Compartment locations should return 1.0, even if on a moon
        Assert.Equal(1.0, AudioPlaybackService.GetAtmosphereDistanceMultiplier("carrack_deck"));
        Assert.Equal(1.0, AudioPlaybackService.GetAtmosphereDistanceMultiplier("bunker_lobby"));
        Assert.Equal(1.0, AudioPlaybackService.GetAtmosphereDistanceMultiplier("hangar_01"));
    }

    [Fact]
    public void EnvironmentalAcousticFilter_ShouldMuffleOutdoorsOnThinMoons()
    {
        // GIVEN
        var filter = new EnvironmentalAcousticFilter();
        
        // WHEN: Outdoors on Cellin
        // UpdateZoneInfo signature: (speakerZone, listenerZone, lx, ly, lz, sx, sy, sz, enableAtmosphere, enableEnvironmentalAcoustics)
        filter.UpdateZoneInfo("Cellin", "Cellin", 0, 0, 0, 0, 0, 0, enableAtmosphere: true, enableEnvironmentalAcoustics: true);

        // THEN: Cutoff should be set to 800Hz target
        var targetCutoffField = typeof(EnvironmentalAcousticFilter).GetField("_targetCutoff", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        double targetCutoff = (double)targetCutoffField!.GetValue(filter)!;
        Assert.Equal(800.0, targetCutoff);

        // WHEN: Indoors (e.g. Hangar on Cellin)
        filter.UpdateZoneInfo("Hangar_Cellin", "Hangar_Cellin", 0, 0, 0, 0, 0, 0, enableAtmosphere: true, enableEnvironmentalAcoustics: true);
        targetCutoff = (double)targetCutoffField.GetValue(filter)!;
        // Should bypass planetary thin atmosphere and remain at standard 20000Hz (since same zone and indoors)
        Assert.Equal(20000.0, targetCutoff);
    }
}
