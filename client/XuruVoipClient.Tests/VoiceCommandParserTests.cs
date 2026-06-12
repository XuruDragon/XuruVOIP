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

    [Fact]
    public void ParseAndExecute_EnglishShipEngines_ShouldTriggerEvent()
    {
        var service = new VoiceCommandService();
        bool eventFired = false;
        service.ShipEnginesToggleRequested += () => eventFired = true;

        var result = service.ParseAndExecute("toggle engines", "en", new List<string>(), 0.5);

        Assert.True(eventFired);
        Assert.Equal(VoiceCommandAction.ShipEnginesToggle, result.Action);
    }

    [Fact]
    public void ParseAndExecute_FrenchShipWeapons_ShouldTriggerEvent()
    {
        var service = new VoiceCommandService();
        bool eventFired = false;
        service.ShipWeaponsToggleRequested += () => eventFired = true;

        var result = service.ParseAndExecute("desactiver armes", "fr", new List<string>(), 0.5);

        Assert.True(eventFired);
        Assert.Equal(VoiceCommandAction.ShipWeaponsToggle, result.Action);
    }

    [Fact]
    public void ParseAndExecute_GermanShipShieldsToggle_ShouldTriggerEvent()
    {
        var service = new VoiceCommandService();
        bool eventFired = false;
        service.ShipShieldsToggleRequested += () => eventFired = true;

        var result = service.ParseAndExecute("schilde umschalten", "de", new List<string>(), 0.5);

        Assert.True(eventFired);
        Assert.Equal(VoiceCommandAction.ShipShieldsToggle, result.Action);
    }

    [Fact]
    public void ParseAndExecute_SpanishShipShieldsReset_ShouldTriggerEvent()
    {
        var service = new VoiceCommandService();
        bool eventFired = false;
        service.ShipShieldsResetRequested += () => eventFired = true;

        var result = service.ParseAndExecute("equilibrar escudos", "es", new List<string>(), 0.5);

        Assert.True(eventFired);
        Assert.Equal(VoiceCommandAction.ShipShieldsReset, result.Action);
    }

    [Fact]
    public void ParseAndExecute_PortugueseShipVtol_ShouldTriggerEvent()
    {
        var service = new VoiceCommandService();
        bool eventFired = false;
        service.ShipVtolToggleRequested += () => eventFired = true;

        var result = service.ParseAndExecute("alternar vtol", "pt-PT", new List<string>(), 0.5);

        Assert.True(eventFired);
        Assert.Equal(VoiceCommandAction.ShipVtolToggle, result.Action);
    }

    [Fact]
    public void ParseAndExecute_JapaneseShipQuantum_ShouldTriggerEvent()
    {
        var service = new VoiceCommandService();
        bool eventFired = false;
        service.ShipQuantumSpoolRequested += () => eventFired = true;

        var result = service.ParseAndExecute("量子ドライブ", "ja", new List<string>(), 0.5);

        Assert.True(eventFired);
        Assert.Equal(VoiceCommandAction.ShipQuantumSpool, result.Action);
    }

    [Fact]
    public void ParseAndExecute_ChineseShipCruise_ShouldTriggerEvent()
    {
        var service = new VoiceCommandService();
        bool eventFired = false;
        service.ShipCruiseControlRequested += () => eventFired = true;

        var result = service.ParseAndExecute("定速巡航", "zh", new List<string>(), 0.5);

        Assert.True(eventFired);
        Assert.Equal(VoiceCommandAction.ShipCruiseControl, result.Action);
    }

    [Fact]
    public void ParseAndExecute_EnglishShipLandingRequest_ShouldTriggerEvent()
    {
        var service = new VoiceCommandService();
        bool eventFired = false;
        service.ShipLandingRequestRequested += () => eventFired = true;

        var result = service.ParseAndExecute("request landing", "en", new List<string>(), 0.5);

        Assert.True(eventFired);
        Assert.Equal(VoiceCommandAction.ShipLandingRequest, result.Action);
    }

    [Fact]
    public void ParseAndExecute_FlyScanMiningSalvage_ShouldTriggerEvents()
    {
        var service = new VoiceCommandService();
        
        bool flyFired = false;
        service.ShipFlyModeRequested += () => flyFired = true;
        var r1 = service.ParseAndExecute("flight mode", "en", new List<string>(), 0.5);
        Assert.True(flyFired);
        Assert.Equal(VoiceCommandAction.ShipFlyMode, r1.Action);

        bool scanFired = false;
        service.ShipScanModeRequested += () => scanFired = true;
        var r2 = service.ParseAndExecute("scan mode", "en", new List<string>(), 0.5);
        Assert.True(scanFired);
        Assert.Equal(VoiceCommandAction.ShipScanMode, r2.Action);

        bool miningFired = false;
        service.ShipMiningModeRequested += () => miningFired = true;
        var r3 = service.ParseAndExecute("start mining", "en", new List<string>(), 0.5);
        Assert.True(miningFired);
        Assert.Equal(VoiceCommandAction.ShipMiningMode, r3.Action);

        bool salvageFired = false;
        service.ShipSalvageModeRequested += () => salvageFired = true;
        var r4 = service.ParseAndExecute("salvage mode", "en", new List<string>(), 0.5);
        Assert.True(salvageFired);
        Assert.Equal(VoiceCommandAction.ShipSalvageMode, r4.Action);
    }

    [Fact]
    public void ParseAndExecute_PowerTriangle_ShouldTriggerEvents()
    {
        var service = new VoiceCommandService();

        bool weaponsFired = false;
        service.ShipPowerWeaponsRequested += () => weaponsFired = true;
        var r1 = service.ParseAndExecute("max weapons", "en", new List<string>(), 0.5);
        Assert.True(weaponsFired);
        Assert.Equal(VoiceCommandAction.ShipPowerWeapons, r1.Action);

        bool shieldsFired = false;
        service.ShipPowerShieldsRequested += () => shieldsFired = true;
        var r2 = service.ParseAndExecute("power shields", "en", new List<string>(), 0.5);
        Assert.True(shieldsFired);
        Assert.Equal(VoiceCommandAction.ShipPowerShields, r2.Action);

        bool enginesFired = false;
        service.ShipPowerEnginesRequested += () => enginesFired = true;
        var r3 = service.ParseAndExecute("max engines", "en", new List<string>(), 0.5);
        Assert.True(enginesFired);
        Assert.Equal(VoiceCommandAction.ShipPowerEngines, r3.Action);

        bool resetFired = false;
        service.ShipPowerResetRequested += () => resetFired = true;
        var r4 = service.ParseAndExecute("reset power", "en", new List<string>(), 0.5);
        Assert.True(resetFired);
        Assert.Equal(VoiceCommandAction.ShipPowerReset, r4.Action);
    }

    [Fact]
    public void ParseAndExecute_DecoyNoiseLights_ShouldTriggerEvents()
    {
        var service = new VoiceCommandService();

        bool decoyFired = false;
        service.ShipDecoyRequested += () => decoyFired = true;
        var r1 = service.ParseAndExecute("launch decoy", "en", new List<string>(), 0.5);
        Assert.True(decoyFired);
        Assert.Equal(VoiceCommandAction.ShipDecoy, r1.Action);

        bool noiseFired = false;
        service.ShipNoiseRequested += () => noiseFired = true;
        var r2 = service.ParseAndExecute("launch noise", "en", new List<string>(), 0.5);
        Assert.True(noiseFired);
        Assert.Equal(VoiceCommandAction.ShipNoise, r2.Action);

        bool lightsFired = false;
        service.ShipLightsRequested += () => lightsFired = true;
        var r3 = service.ParseAndExecute("lights on", "en", new List<string>(), 0.5);
        Assert.True(lightsFired);
        Assert.Equal(VoiceCommandAction.ShipLights, r3.Action);
    }
}
