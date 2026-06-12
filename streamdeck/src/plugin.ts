import streamDeck, {
  action,
  SingletonAction,
  KeyDownEvent,
  KeyUpEvent,
  DialRotateEvent,
  DialDownEvent,
  TouchTapEvent
} from "@elgato/streamdeck";

// Cache for latest companion status responses by port
const latestStatusByPort: Record<number, any> = {};

// Polling interval ID
let pollingIntervalId: ReturnType<typeof setInterval> | null = null;

// Helper function to send API actions to the Companion App
async function postAction(port: number, actionName: string, body: any = {}, evAction: any) {
  try {
    const response = await fetch(`http://127.0.0.1:${port}/api/action`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json"
      },
      body: JSON.stringify({ action: actionName, ...body })
    });
    
    if (response.ok) {
      setTimeout(pollCompanionApp, 50);
    }
  } catch (e) {
    streamDeck.logger.error(`Failed to execute action ${actionName} on Companion App:`, e);
    await evAction.showAlert();
  }
}

// Fetch status from companion app and update Stream Deck states
async function pollCompanionApp() {
  const ports = new Set<number>();
  
  // Collect all unique ports configured on visible actions
  for (const actionInstance of streamDeck.actions) {
    const settings = await actionInstance.getSettings<{ port?: number }>();
    const port = settings.port || 8891;
    ports.add(port);
  }

  for (const port of ports) {
    try {
      const response = await fetch(`http://127.0.0.1:${port}/api/status`);
      if (response.ok) {
        const status = await response.json();
        latestStatusByPort[port] = status;
        await updateStreamDeckStates(port, status);
      }
    } catch (e) {
      // Silent error to prevent console spam when client is closed
    }
  }
}

// Update state/title of visible buttons matching a specific port
async function updateStreamDeckStates(port: number, status: any) {
  for (const actionInstance of streamDeck.actions) {
    const settings = await actionInstance.getSettings<{ port?: number; command?: string }>();
    const actionPort = settings.port || 8891;
    if (actionPort !== port) continue;

    const manifestId = actionInstance.manifestId;

    if (actionInstance.isDial()) {
      let titleStr = "";
      let valueStr = "";
      let indicatorVal = 0;

      switch (manifestId) {
        case "com.xurudragon.xuruvoip.action.cycle-radio-dial": {
          const channels = status.availableChannels || [];
          const activeChan = status.activeChannel || "Proximity";
          let chanIdx = channels.indexOf(activeChan);
          if (chanIdx === -1) chanIdx = 0;
          
          titleStr = "Radio Channel";
          valueStr = status.micRadioMuted ? `${activeChan} [MUTED]` : activeChan;
          indicatorVal = channels.length > 1 ? Math.round((chanIdx / (channels.length - 1)) * 100) : 0;
          break;
        }

        case "com.xurudragon.xuruvoip.action.adjust-exertion": {
          const gfVal = status.gforce !== undefined ? status.gforce : 0.0;
          const exVal = status.exertion !== undefined ? status.exertion : 0.0;
          
          titleStr = "G-Force & Exert";
          valueStr = status.enableExertionDistortion ? `G: ${gfVal.toFixed(1)}G | Ex: ${Math.round(exVal * 100)}%` : `G: ${gfVal.toFixed(1)}G | [OFF]`;
          indicatorVal = Math.round(gfVal * 100);
          break;
        }

        case "com.xurudragon.xuruvoip.action.voice-changer-dial": {
          const profiles = ["None", "Alien", "Cyborg", "Robotic", "PitchShift"];
          const curProfile = status.voiceChangerEnabled ? (status.voiceChangerType || "None") : "None";
          let profIdx = profiles.indexOf(curProfile);
          if (profIdx === -1) profIdx = 0;
          
          titleStr = "Voice Changer";
          valueStr = status.voiceChangerEnabled ? (status.voiceChangerType || "None") : "Disabled";
          indicatorVal = Math.round((profIdx / (profiles.length - 1)) * 100);
          break;
        }
        case "com.xurudragon.xuruvoip.action.theme-dial": {
          const themes = ["Aegis", "Anvil", "Drake", "RSI", "Origin"];
          const curTheme = status.hudTheme || "RSI";
          let themeIdx = themes.indexOf(curTheme);
          if (themeIdx === -1) themeIdx = 3;
          
          titleStr = "HUD Theme";
          valueStr = curTheme;
          indicatorVal = Math.round((themeIdx / (themes.length - 1)) * 100);
          break;
        }
      }

      if (titleStr) {
        await actionInstance.setFeedback({
          title: titleStr,
          value: valueStr,
          indicator: indicatorVal
        });
      }
      continue;
    }

    let state = 0;
    let title: string | null = null;

    switch (manifestId) {
      case "com.xurudragon.xuruvoip.action.proximity-mute":
        state = status.micProximityMuted ? 1 : 0;
        break;
      case "com.xurudragon.xuruvoip.action.radio-mute":
        state = status.micRadioMuted ? 1 : 0;
        break;
      case "com.xurudragon.xuruvoip.action.profile-mute":
        state = status.micProfileMuted ? 1 : 0;
        break;
      case "com.xurudragon.xuruvoip.action.audio-proximity-mute":
        state = status.audioProximityMuted ? 1 : 0;
        break;
      case "com.xurudragon.xuruvoip.action.audio-radio-mute":
        state = status.audioRadioMuted ? 1 : 0;
        break;
      case "com.xurudragon.xuruvoip.action.audio-profile-mute":
        state = status.audioProfileMuted ? 1 : 0;
        break;
      case "com.xurudragon.xuruvoip.action.toggle-helmet":
        state = status.isHelmetOn ? 1 : 0;
        break;
      case "com.xurudragon.xuruvoip.action.cycle-radio":
        state = 0;
        title = status.activeChannel || "Proximity";
        break;
      case "com.xurudragon.xuruvoip.action.pa-broadcast":
        state = status.isPttPaDown ? 1 : 0;
        break;
      case "com.xurudragon.xuruvoip.action.beacon-repeater":
        state = status.isRadioRepeater ? 1 : 0;
        break;
      case "com.xurudragon.xuruvoip.action.voice-command":
        state = status.isListening ? 1 : 0;
        title = status.isListening ? "LISTENING" : (settings.command || "Voice Cmd");
        break;
      case "com.xurudragon.xuruvoip.action.intercom-status": {
        const intercom = status.intercomState || "Normal";
        if (intercom === "Normal") {
          state = 0;
          title = "NORMAL";
        } else if (intercom === "ShieldHit") {
          state = 1;
          title = "SHIELD HIT";
        } else if (intercom === "CriticalPower") {
          state = 2;
          title = "CRIT PWR";
        } else if (intercom === "QuantumTravel") {
          state = 3;
          title = "QUANTUM";
        } else {
          state = 0;
          title = "NORMAL";
        }
        break;
      }
      case "com.xurudragon.xuruvoip.action.location-telemetry":
        state = 0;
        if (status.localPos) {
          const zoneStr = status.localPos.zone || "SYSTEM";
          const truncZone = zoneStr.length > 9 ? zoneStr.substring(0, 9) : zoneStr;
          title = `${truncZone}\nX: ${Math.round(status.localPos.x)}\nY: ${Math.round(status.localPos.y)}\nZ: ${Math.round(status.localPos.z)}`;
        } else {
          title = "NO GPS";
        }
        break;
      case "com.xurudragon.xuruvoip.action.hail-initiate":
        state = 0;
        if (status.hailState === "Outgoing") {
          title = "HAILING...";
        } else if (status.hailState === "Connected") {
          title = `ACTIVE:\n${status.hailPeerName || ""}`;
        } else {
          title = "Initiate\nHail";
        }
        break;
      case "com.xurudragon.xuruvoip.action.hail-accept":
        state = 0;
        if (status.hailState === "Incoming") {
          title = "ACCEPT\nHAIL!";
        } else {
          title = "Accept\nHail";
        }
        break;
      case "com.xurudragon.xuruvoip.action.hail-decline":
        state = 0;
        if (status.hailState === "Incoming" || status.hailState === "Outgoing" || status.hailState === "Connected") {
          title = status.hailState === "Connected" ? "END\nHAIL" : "DECLINE";
        } else {
          title = "Decline\nHail";
        }
        break;
      case "com.xurudragon.xuruvoip.action.toggle-translation":
        state = status.enableTranslationSubtitles ? 1 : 0;
        title = status.enableTranslationSubtitles ? "TRANS\nON" : "TRANS\nOFF";
        break;
      case "com.xurudragon.xuruvoip.action.toggle-hrtf":
        state = status.enableHrtf ? 1 : 0;
        title = status.enableHrtf ? "HRTF\nON" : "HRTF\nOFF";
        break;
      case "com.xurudragon.xuruvoip.action.toggle-spectrogram":
        state = status.enableVisorSpectrogram ? 1 : 0;
        title = status.enableVisorSpectrogram ? "HUD SPEC\nON" : "HUD SPEC\nOFF";
        break;
      case "com.xurudragon.xuruvoip.action.toggle-voice-commands":
        state = status.voiceCommandsEnabled ? 1 : 0;
        title = status.voiceCommandsEnabled ? "VOICE\nON" : "VOICE\nOFF";
        break;
      case "com.xurudragon.xuruvoip.action.cycle-theme":
        state = 0;
        title = `THEME:\n${status.hudTheme || "RSI"}`;
        break;
    }

    await actionInstance.setState(state);
    if (title !== null) {
      await actionInstance.setTitle(title);
    }
  }
}

// Action classes mapping to each action UUID
@action({ UUID: "com.xurudragon.xuruvoip.action.proximity-mute" })
export class ProximityMuteAction extends SingletonAction {
  async onKeyDown(ev: KeyDownEvent): Promise<void> {
    const settings = await ev.action.getSettings<{ port?: number }>();
    await postAction(settings.port || 8891, "toggle_proximity_mute", {}, ev.action);
  }
}

@action({ UUID: "com.xurudragon.xuruvoip.action.radio-mute" })
export class RadioMuteAction extends SingletonAction {
  async onKeyDown(ev: KeyDownEvent): Promise<void> {
    const settings = await ev.action.getSettings<{ port?: number }>();
    await postAction(settings.port || 8891, "toggle_radio_mute", {}, ev.action);
  }
}

@action({ UUID: "com.xurudragon.xuruvoip.action.profile-mute" })
export class ProfileMuteAction extends SingletonAction {
  async onKeyDown(ev: KeyDownEvent): Promise<void> {
    const settings = await ev.action.getSettings<{ port?: number }>();
    await postAction(settings.port || 8891, "toggle_profile_mute", {}, ev.action);
  }
}

@action({ UUID: "com.xurudragon.xuruvoip.action.audio-proximity-mute" })
export class AudioProximityMuteAction extends SingletonAction {
  async onKeyDown(ev: KeyDownEvent): Promise<void> {
    const settings = await ev.action.getSettings<{ port?: number }>();
    await postAction(settings.port || 8891, "toggle_audio_proximity_mute", {}, ev.action);
  }
}

@action({ UUID: "com.xurudragon.xuruvoip.action.audio-radio-mute" })
export class AudioRadioMuteAction extends SingletonAction {
  async onKeyDown(ev: KeyDownEvent): Promise<void> {
    const settings = await ev.action.getSettings<{ port?: number }>();
    await postAction(settings.port || 8891, "toggle_audio_radio_mute", {}, ev.action);
  }
}

@action({ UUID: "com.xurudragon.xuruvoip.action.audio-profile-mute" })
export class AudioProfileMuteAction extends SingletonAction {
  async onKeyDown(ev: KeyDownEvent): Promise<void> {
    const settings = await ev.action.getSettings<{ port?: number }>();
    await postAction(settings.port || 8891, "toggle_audio_profile_mute", {}, ev.action);
  }
}

@action({ UUID: "com.xurudragon.xuruvoip.action.toggle-helmet" })
export class ToggleHelmetAction extends SingletonAction {
  async onKeyDown(ev: KeyDownEvent): Promise<void> {
    const settings = await ev.action.getSettings<{ port?: number }>();
    await postAction(settings.port || 8891, "toggle_helmet", {}, ev.action);
  }
}

@action({ UUID: "com.xurudragon.xuruvoip.action.cycle-radio" })
export class CycleRadioAction extends SingletonAction {
  async onKeyDown(ev: KeyDownEvent): Promise<void> {
    const settings = await ev.action.getSettings<{ port?: number }>();
    await postAction(settings.port || 8891, "cycle_radio", {}, ev.action);
  }
}

@action({ UUID: "com.xurudragon.xuruvoip.action.pa-broadcast" })
export class PaBroadcastAction extends SingletonAction {
  async onKeyDown(ev: KeyDownEvent): Promise<void> {
    const settings = await ev.action.getSettings<{ port?: number }>();
    await postAction(settings.port || 8891, "start_pa", {}, ev.action);
  }
  async onKeyUp(ev: KeyUpEvent): Promise<void> {
    const settings = await ev.action.getSettings<{ port?: number }>();
    await postAction(settings.port || 8891, "stop_pa", {}, ev.action);
  }
}

@action({ UUID: "com.xurudragon.xuruvoip.action.beacon-repeater" })
export class BeaconRepeaterAction extends SingletonAction {
  async onKeyDown(ev: KeyDownEvent): Promise<void> {
    const settings = await ev.action.getSettings<{ port?: number }>();
    await postAction(settings.port || 8891, "toggle_repeater", {}, ev.action);
  }
}

@action({ UUID: "com.xurudragon.xuruvoip.action.voice-command" })
export class VoiceCommandAction extends SingletonAction {
  async onKeyDown(ev: KeyDownEvent): Promise<void> {
    const settings = await ev.action.getSettings<{ port?: number; command?: string }>();
    const command = settings.command || "status report";
    await postAction(settings.port || 8891, "simulate_voice_command", { command }, ev.action);
  }
}

@action({ UUID: "com.xurudragon.xuruvoip.action.intercom-status" })
export class IntercomStatusAction extends SingletonAction {
  async onKeyDown(ev: KeyDownEvent): Promise<void> {
    const settings = await ev.action.getSettings<{ port?: number }>();
    const port = settings.port || 8891;
    const currentStatus = latestStatusByPort[port] || {};
    const currentIntercom = currentStatus.intercomState || "Normal";
    let nextIntercom = "normal";
    
    if (currentIntercom === "Normal") nextIntercom = "shield_hit";
    else if (currentIntercom === "ShieldHit") nextIntercom = "critical_power";
    else if (currentIntercom === "CriticalPower") nextIntercom = "quantum";
    else nextIntercom = "normal";

    await postAction(port, "simulate_intercom_state", { state: nextIntercom }, ev.action);
  }
}

@action({ UUID: "com.xurudragon.xuruvoip.action.location-telemetry" })
export class LocationTelemetryAction extends SingletonAction {
  async onKeyDown(ev: KeyDownEvent): Promise<void> {
    await pollCompanionApp();
  }
}

@action({ UUID: "com.xurudragon.xuruvoip.action.hail-initiate" })
export class HailInitiateAction extends SingletonAction {
  async onKeyDown(ev: KeyDownEvent): Promise<void> {
    const settings = await ev.action.getSettings<{ port?: number }>();
    await postAction(settings.port || 8891, "hail_initiate", {}, ev.action);
  }
}

@action({ UUID: "com.xurudragon.xuruvoip.action.hail-accept" })
export class HailAcceptAction extends SingletonAction {
  async onKeyDown(ev: KeyDownEvent): Promise<void> {
    const settings = await ev.action.getSettings<{ port?: number }>();
    await postAction(settings.port || 8891, "hail_accept", {}, ev.action);
  }
}

@action({ UUID: "com.xurudragon.xuruvoip.action.hail-decline" })
export class HailDeclineAction extends SingletonAction {
  async onKeyDown(ev: KeyDownEvent): Promise<void> {
    const settings = await ev.action.getSettings<{ port?: number }>();
    await postAction(settings.port || 8891, "hail_decline", {}, ev.action);
  }
}

@action({ UUID: "com.xurudragon.xuruvoip.action.toggle-translation" })
export class ToggleTranslationAction extends SingletonAction {
  async onKeyDown(ev: KeyDownEvent): Promise<void> {
    const settings = await ev.action.getSettings<{ port?: number }>();
    await postAction(settings.port || 8891, "toggle_translation", {}, ev.action);
  }
}

@action({ UUID: "com.xurudragon.xuruvoip.action.toggle-hrtf" })
export class ToggleHrtfAction extends SingletonAction {
  async onKeyDown(ev: KeyDownEvent): Promise<void> {
    const settings = await ev.action.getSettings<{ port?: number }>();
    await postAction(settings.port || 8891, "toggle_hrtf", {}, ev.action);
  }
}

@action({ UUID: "com.xurudragon.xuruvoip.action.toggle-spectrogram" })
export class ToggleSpectrogramAction extends SingletonAction {
  async onKeyDown(ev: KeyDownEvent): Promise<void> {
    const settings = await ev.action.getSettings<{ port?: number }>();
    await postAction(settings.port || 8891, "toggle_spectrogram", {}, ev.action);
  }
}

@action({ UUID: "com.xurudragon.xuruvoip.action.toggle-voice-commands" })
export class ToggleVoiceCommandsAction extends SingletonAction {
  async onKeyDown(ev: KeyDownEvent): Promise<void> {
    const settings = await ev.action.getSettings<{ port?: number }>();
    await postAction(settings.port || 8891, "toggle_voice_commands", {}, ev.action);
  }
}

@action({ UUID: "com.xurudragon.xuruvoip.action.cycle-theme" })
export class CycleThemeAction extends SingletonAction {
  async onKeyDown(ev: KeyDownEvent): Promise<void> {
    const settings = await ev.action.getSettings<{ port?: number }>();
    await postAction(settings.port || 8891, "cycle_hud_theme", {}, ev.action);
  }
}

@action({ UUID: "com.xurudragon.xuruvoip.action.theme-dial" })
export class ThemeDialAction extends SingletonAction {
  async onDialRotate(ev: DialRotateEvent): Promise<void> {
    const settings = await ev.action.getSettings<{ port?: number }>();
    const port = settings.port || 8891;
    const status = latestStatusByPort[port] || {};
    const themes = ["Aegis", "Anvil", "Drake", "RSI", "Origin"];
    const curTheme = status.hudTheme || "RSI";
    let idx = themes.indexOf(curTheme);
    if (idx === -1) idx = 3;
    
    idx = (idx + ev.payload.ticks) % themes.length;
    if (idx < 0) idx += themes.length;

    await postAction(port, "set_hud_theme", { theme: themes[idx] }, ev.action);
  }

  async onDialDown(ev: DialDownEvent): Promise<void> {
    const settings = await ev.action.getSettings<{ port?: number }>();
    await postAction(settings.port || 8891, "cycle_hud_theme", {}, ev.action);
  }

  async onTouchTap(ev: TouchTapEvent): Promise<void> {
    const settings = await ev.action.getSettings<{ port?: number }>();
    await postAction(settings.port || 8891, "cycle_hud_theme", {}, ev.action);
  }
}

// ENCODERS (Dials)
@action({ UUID: "com.xurudragon.xuruvoip.action.cycle-radio-dial" })
export class CycleRadioDialAction extends SingletonAction {
  async onDialRotate(ev: DialRotateEvent): Promise<void> {
    const settings = await ev.action.getSettings<{ port?: number }>();
    const port = settings.port || 8891;
    const status = latestStatusByPort[port] || {};
    const channels = status.availableChannels || [];
    
    if (channels.length > 0) {
      const activeChan = status.activeChannel || "Proximity";
      let idx = channels.indexOf(activeChan);
      if (idx === -1) idx = 0;
      
      idx = (idx + ev.payload.ticks) % channels.length;
      if (idx < 0) idx += channels.length;

      await postAction(port, "set_channel", { channel: channels[idx] }, ev.action);
    }
  }

  async onDialDown(ev: DialDownEvent): Promise<void> {
    const settings = await ev.action.getSettings<{ port?: number }>();
    await postAction(settings.port || 8891, "toggle_radio_mute", {}, ev.action);
  }

  async onTouchTap(ev: TouchTapEvent): Promise<void> {
    const settings = await ev.action.getSettings<{ port?: number }>();
    await postAction(settings.port || 8891, "toggle_radio_mute", {}, ev.action);
  }
}

@action({ UUID: "com.xurudragon.xuruvoip.action.adjust-exertion" })
export class AdjustExertionAction extends SingletonAction {
  async onDialRotate(ev: DialRotateEvent): Promise<void> {
    const settings = await ev.action.getSettings<{ port?: number }>();
    const port = settings.port || 8891;
    const status = latestStatusByPort[port] || {};
    let gforce = status.gforce !== undefined ? status.gforce : 0.0;
    let exertion = status.exertion !== undefined ? status.exertion : 0.0;

    if (ev.payload.pressed) {
      exertion = Math.min(1.0, Math.max(0.0, exertion + ev.payload.ticks * 0.05));
    } else {
      gforce = Math.min(1.0, Math.max(0.0, gforce + ev.payload.ticks * 0.05));
    }

    await postAction(port, "set_exertion", { gforce, exertion }, ev.action);
  }

  async onDialDown(ev: DialDownEvent): Promise<void> {
    const settings = await ev.action.getSettings<{ port?: number }>();
    await postAction(settings.port || 8891, "toggle_exertion_distortion", {}, ev.action);
  }

  async onTouchTap(ev: TouchTapEvent): Promise<void> {
    const settings = await ev.action.getSettings<{ port?: number }>();
    await postAction(settings.port || 8891, "toggle_exertion_distortion", {}, ev.action);
  }
}

@action({ UUID: "com.xurudragon.xuruvoip.action.voice-changer-dial" })
export class VoiceChangerDialAction extends SingletonAction {
  async onDialRotate(ev: DialRotateEvent): Promise<void> {
    const settings = await ev.action.getSettings<{ port?: number }>();
    const port = settings.port || 8891;
    const status = latestStatusByPort[port] || {};
    const profiles = ["None", "Alien", "Cyborg", "Robotic", "PitchShift"];
    const currentProfile = status.voiceChangerEnabled ? (status.voiceChangerType || "None") : "None";
    let idx = profiles.indexOf(currentProfile);
    if (idx === -1) idx = 0;
    
    idx = (idx + ev.payload.ticks) % profiles.length;
    if (idx < 0) idx += profiles.length;

    await postAction(port, "set_voice_changer", { type: profiles[idx] }, ev.action);
  }

  async onDialDown(ev: DialDownEvent): Promise<void> {
    const settings = await ev.action.getSettings<{ port?: number }>();
    const port = settings.port || 8891;
    const status = latestStatusByPort[port] || {};
    const nextType = status.voiceChangerEnabled ? "None" : (status.voiceChangerType !== "None" ? status.voiceChangerType : "Cyborg");
    await postAction(port, "set_voice_changer", { type: nextType }, ev.action);
  }

  async onTouchTap(ev: TouchTapEvent): Promise<void> {
    const settings = await ev.action.getSettings<{ port?: number }>();
    const port = settings.port || 8891;
    const status = latestStatusByPort[port] || {};
    const nextType = status.voiceChangerEnabled ? "None" : (status.voiceChangerType !== "None" ? status.voiceChangerType : "Cyborg");
    await postAction(port, "set_voice_changer", { type: nextType }, ev.action);
  }
}

// Register all actions
streamDeck.actions.registerAction(new ProximityMuteAction());
streamDeck.actions.registerAction(new RadioMuteAction());
streamDeck.actions.registerAction(new ProfileMuteAction());
streamDeck.actions.registerAction(new AudioProximityMuteAction());
streamDeck.actions.registerAction(new AudioRadioMuteAction());
streamDeck.actions.registerAction(new AudioProfileMuteAction());
streamDeck.actions.registerAction(new ToggleHelmetAction());
streamDeck.actions.registerAction(new CycleRadioAction());
streamDeck.actions.registerAction(new PaBroadcastAction());
streamDeck.actions.registerAction(new BeaconRepeaterAction());
streamDeck.actions.registerAction(new VoiceCommandAction());
streamDeck.actions.registerAction(new IntercomStatusAction());
streamDeck.actions.registerAction(new LocationTelemetryAction());
streamDeck.actions.registerAction(new CycleRadioDialAction());
streamDeck.actions.registerAction(new AdjustExertionAction());
streamDeck.actions.registerAction(new VoiceChangerDialAction());
streamDeck.actions.registerAction(new HailInitiateAction());
streamDeck.actions.registerAction(new HailAcceptAction());
streamDeck.actions.registerAction(new HailDeclineAction());
streamDeck.actions.registerAction(new ToggleTranslationAction());
streamDeck.actions.registerAction(new ToggleHrtfAction());
streamDeck.actions.registerAction(new ToggleSpectrogramAction());
streamDeck.actions.registerAction(new ToggleVoiceCommandsAction());
streamDeck.actions.registerAction(new CycleThemeAction());
streamDeck.actions.registerAction(new ThemeDialAction());

// Initialize plugin and start polling
async function main() {
  pollingIntervalId = setInterval(pollCompanionApp, 1000);
  await streamDeck.connect();
}

main().catch((err) => {
  streamDeck.logger.error("Plugin failed to start:", err);
});
