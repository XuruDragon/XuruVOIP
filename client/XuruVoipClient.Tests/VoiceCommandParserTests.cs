using System;
using System.Collections.Generic;
using Xunit;
using XuruVoipClient.Services;

namespace XuruVoipClient.Tests;

public class VoiceCommandParserTests
{
    [Fact]
    public void ParseAndExecute_EnglishVisorToggle_ShouldTriggerEvent()
    {
        // GIVEN
        var service = new VoiceCommandService();
        bool eventFired = false;
        service.VisorToggleRequested += () => eventFired = true;
        var channels = new List<string> { "General", "Alpha", "Bravo" };

        // WHEN
        var result = service.ParseAndExecute("computer, toggle visor", "en", channels, 0.4);

        // THEN
        Assert.True(eventFired);
        Assert.Equal(VoiceCommandAction.VisorToggle, result.Action);
    }

    [Fact]
    public void ParseAndExecute_FrenchVisorToggle_ShouldTriggerEvent()
    {
        // GIVEN
        var service = new VoiceCommandService();
        bool eventFired = false;
        service.VisorToggleRequested += () => eventFired = true;
        var channels = new List<string> { "General" };

        // WHEN
        var result = service.ParseAndExecute("basculer le casque s'il vous plait", "fr", channels, 0.2);

        // THEN
        Assert.True(eventFired);
        Assert.Equal(VoiceCommandAction.VisorToggle, result.Action);
    }

    [Fact]
    public void ParseAndExecute_GermanRadioChannelSwitch_ShouldChangeChannel()
    {
        // GIVEN
        var service = new VoiceCommandService();
        string targetChannel = "";
        service.ChannelChangeRequested += chan => targetChannel = chan;
        var channels = new List<string> { "General", "Alpha", "Bravo" };

        // WHEN
        var result = service.ParseAndExecute("Schalte kanal auf Alpha", "de", channels, 0.3);

        // THEN
        Assert.Equal("Alpha", targetChannel);
        Assert.Equal(VoiceCommandAction.RadioChannelSwitch, result.Action);
        Assert.Equal("Alpha", result.TargetChannel);
    }

    [Fact]
    public void ParseAndExecute_ChineseVoiceChanger_ShouldSetProfile()
    {
        // GIVEN
        var service = new VoiceCommandService();
        string targetProfile = "";
        service.VoiceChangerProfileRequested += prof => targetProfile = prof;
        var channels = new List<string> { "General" };

        // WHEN
        var result = service.ParseAndExecute("设置变声器为外星人", "zh", channels, 0.3);

        // THEN
        Assert.Equal("Alien", targetProfile);
        Assert.Equal(VoiceCommandAction.VoiceChangerProfile, result.Action);
        Assert.Equal("Alien", result.TargetProfile);
    }

    [Fact]
    public void ParseAndExecute_SpanishProximityMute_ShouldMuteProximity()
    {
        // GIVEN
        var service = new VoiceCommandService();
        VoiceCommandAction triggeredAction = VoiceCommandAction.None;
        service.MicStateChangeRequested += action => triggeredAction = action;
        var channels = new List<string> { "General" };

        // WHEN
        var result = service.ParseAndExecute("silenciar proximidad", "es", channels, 0.5);

        // THEN
        Assert.Equal(VoiceCommandAction.MicMuteProximity, triggeredAction);
        Assert.Equal(VoiceCommandAction.MicMuteProximity, result.Action);
    }

    [Fact]
    public void ParseAndExecute_PortugueseRadioUnmute_ShouldUnmuteRadio()
    {
        // GIVEN
        var service = new VoiceCommandService();
        VoiceCommandAction triggeredAction = VoiceCommandAction.None;
        service.MicStateChangeRequested += action => triggeredAction = action;
        var channels = new List<string> { "General" };

        // WHEN
        var result = service.ParseAndExecute("desmutear radio", "pt-PT", channels, 0.5);

        // THEN
        Assert.Equal(VoiceCommandAction.MicUnmuteRadio, triggeredAction);
        Assert.Equal(VoiceCommandAction.MicUnmuteRadio, result.Action);
    }

    [Fact]
    public void ParseAndExecute_LowSimilarityConfidence_ShouldIgnoreCommand()
    {
        // GIVEN
        var service = new VoiceCommandService();
        bool eventFired = false;
        service.VisorToggleRequested += () => eventFired = true;
        var channels = new List<string> { "General" };

        // WHEN: Trigger string "visor" is a small portion of the entire command, similarity is ~0.1
        var result = service.ParseAndExecute("please perform system checklist diagnostic scan and check visor status", "en", channels, 0.8);

        // THEN: High confidence setting (0.8) should reject it
        Assert.False(eventFired);
        Assert.Equal(VoiceCommandAction.None, result.Action);
    }

    [Fact]
    public void ParseAndExecute_LowSimilarityConfidenceAccepted_ShouldTriggerCommand()
    {
        // GIVEN
        var service = new VoiceCommandService();
        bool eventFired = false;
        service.VisorToggleRequested += () => eventFired = true;
        var channels = new List<string> { "General" };

        // WHEN: Low confidence setting (0.1) should accept it
        var result = service.ParseAndExecute("please perform system checklist diagnostic scan and check visor status", "en", channels, 0.05);

        // THEN
        Assert.True(eventFired);
        Assert.Equal(VoiceCommandAction.VisorToggle, result.Action);
    }

    [Fact]
    public void ParseAndExecute_EnglishShipPower_ShouldTriggerEvent()
    {
        var service = new VoiceCommandService();
        bool eventFired = false;
        service.ShipPowerToggleRequested += () => eventFired = true;

        var result = service.ParseAndExecute("computer, toggle power", "en", new List<string>(), 0.5);

        Assert.True(eventFired);
        Assert.Equal(VoiceCommandAction.ShipPowerToggle, result.Action);
    }

    [Fact]
    public void ParseAndExecute_FrenchShipDoors_ShouldTriggerEvent()
    {
        var service = new VoiceCommandService();
        bool eventFired = false;
        service.ShipDoorsToggleRequested += () => eventFired = true;

        var result = service.ParseAndExecute("ouvrir portes", "fr", new List<string>(), 0.5);

        Assert.True(eventFired);
        Assert.Equal(VoiceCommandAction.ShipDoorsToggle, result.Action);
    }

    [Fact]
    public void ParseAndExecute_GermanShipShields_ShouldTriggerEvent()
    {
        var service = new VoiceCommandService();
        bool eventFired = false;
        service.ShipShieldsFrontRequested += () => eventFired = true;

        var result = service.ParseAndExecute("schilde vorne bitte", "de", new List<string>(), 0.5);

        Assert.True(eventFired);
        Assert.Equal(VoiceCommandAction.ShipShieldsFront, result.Action);
    }

    [Fact]
    public void ParseAndExecute_JapaneseShipLandingGear_ShouldTriggerEvent()
    {
        var service = new VoiceCommandService();
        bool eventFired = false;
        service.ShipLandingGearToggleRequested += () => eventFired = true;

        var result = service.ParseAndExecute("ギア展開", "ja", new List<string>(), 0.5);

        Assert.True(eventFired);
        Assert.Equal(VoiceCommandAction.ShipLandingGearToggle, result.Action);
    }
}
