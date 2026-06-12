using System.Windows;

namespace XuruVoipClient.Models;

public enum AudioMode { PTT, VAD }

/// <summary>
/// Persisted configuration stored in %AppData%\XuruVoip\config.json
/// </summary>
public class AppConfig
{
    // --- Connection ---
    public string ServerAddress { get; set; } = "127.0.0.1";
    public string Language { get; set; } = "";
    public string ServerPassword { get; set; } = "";
    public string UserPassword { get; set; } = "";
    public int PositionPort { get; set; } = 8888;
    public int AudioPort { get; set; } = 8889;
    public string Username { get; set; } = "";
    public string Hwid { get; set; } = "";
    public bool EnableGeneralLogs { get; set; } = false;
    public string CustomGameLogPath { get; set; } = "";

    // --- OCR ---
    public bool UseGrtpr { get; set; } = true;
    public int OcrMonitorIndex { get; set; } = 0;
    public Rect OcrRegion { get; set; } = new Rect(0, 0, 900, 200);
    public int OcrIntervalMs { get; set; } = 500;

    // --- Audio Mode ---
    public AudioMode AudioMode { get; set; } = AudioMode.PTT;
    public string PttProximityKey { get; set; } = "CapsLock";
    public string PttRadioKey { get; set; } = "NumPad1";
    public string PttProfileKey { get; set; } = "NumPad2";
    public string HelmetToggleKey { get; set; } = "H";
    public string RadioCycleKey { get; set; } = "NumPad3";
    public string MuteProximityKey { get; set; } = "M";
    public string MuteRadioKey { get; set; } = "OemComma";
    public string MuteProfileKey { get; set; } = "OemPeriod";
    public string MuteAudioProximityKey { get; set; } = "None";
    public string MuteAudioRadioKey { get; set; } = "None";
    public string MuteAudioProfileKey { get; set; } = "None";
    public string InitiateHailKey { get; set; } = "None";
    public string AcceptHailKey { get; set; } = "None";
    public string DeclineHailKey { get; set; } = "None";
    public int VadSensitivity { get; set; } = 2; // 0=Very Low, 1=Low, 2=Medium, 3=High

    // --- Audio Devices & Gain ---
    public int InputDeviceIndex { get; set; } = 0;
    public double InputGainDb { get; set; } = 0.0;   // -20 to +20 dB
    public int OutputDeviceIndex { get; set; } = 0;
    public double OutputGainPercent { get; set; } = 100.0; // 0 to 200

    // --- Audio Channel ---
    public byte AudioType { get; set; } = 0x00; // 0=Proximity, 1=Radio, 2=Profile

    // --- Spatial & Radio Effects ---
    public bool EnableSpatialAudio { get; set; } = true;
    public bool EnableHrtf { get; set; } = false;
    public bool EnableRadioDegradation { get; set; } = true;
    public bool EnablePttChimes { get; set; } = true;
    public string PttChimeType { get; set; } = "Military";
    public bool EnableCustomChimes { get; set; } = false;
    public bool EnableEnvironmentalAcoustics { get; set; } = true;
    public bool EnableHelmetModulator { get; set; } = true;
    public bool EnableVoiceChanger { get; set; } = false;
    public string VoiceChangerType { get; set; } = "None"; // None, Alien, Cyborg, Robotic, PitchShift
    public float VoicePitchFactor { get; set; } = 1.0f;
    public bool EnableCustomModulator { get; set; } = false;
    public float CustomPitchShift { get; set; } = 1.0f;
    public float CustomRingModFreq { get; set; } = 100f;
    public float CustomRingModMix { get; set; } = 0.0f;
    public float CustomFlangerDepth { get; set; } = 0.0f;
    public float CustomFlangerRate { get; set; } = 0.5f;
    public float CustomFlangerFeedback { get; set; } = 0.0f;
    public bool CustomBitcrushEnabled { get; set; } = false;
    public int CustomBitcrushBits { get; set; } = 16;
    public bool EnableAtmosphereSimulation { get; set; } = false;
    public bool EnableRadioDelay { get; set; } = false;

    // --- Borderless Overlay ---
    public bool EnableOverlay { get; set; } = false;
    public string OverlayPosition { get; set; } = "TopLeft";
    public string HudTheme { get; set; } = "RSI";
    public bool HudShowRadar { get; set; } = true;
    public bool HudShowActiveSpeakers { get; set; } = true;
    public bool HudShowChannel { get; set; } = true;
    public bool EnableRadar { get; set; } = true;

    public double RadarRange { get; set; } = 50.0;
    public bool EnableStt { get; set; } = false;
    public bool EnableVisorSpectrogram { get; set; } = false;
    public bool EnableTranslationSubtitles { get; set; } = false;
    public bool EnableVoiceCommands { get; set; } = false;
    public string VoiceCommandHotkey { get; set; } = "V";
    public double VoiceCommandConfidence { get; set; } = 0.5;
    public string VoiceCommandPowerKey { get; set; } = "U";
    public string VoiceCommandDoorsKey { get; set; } = "K";
    public string VoiceCommandDoorsModifier { get; set; } = "Alt";
    public string VoiceCommandShieldsKey { get; set; } = "Up";
    public string VoiceCommandLandingGearKey { get; set; } = "N";
    public string VoiceCommandEnginesKey { get; set; } = "I";
    public string VoiceCommandWeaponsKey { get; set; } = "P";
    public string VoiceCommandShieldsToggleKey { get; set; } = "O";
    public string VoiceCommandShieldsResetKey { get; set; } = "NumPad5";
    public string VoiceCommandVtolKey { get; set; } = "K";
    public string VoiceCommandQuantumKey { get; set; } = "B";
    public string VoiceCommandCruiseKey { get; set; } = "C";
    public string VoiceCommandLandingRequestKey { get; set; } = "N";
    public string VoiceCommandLandingRequestModifier { get; set; } = "Alt";

    // --- Discord RPC ---
    public bool EnableDiscordRpc { get; set; } = true;

    // --- Companion App ---
    public bool EnableCompanionApp { get; set; } = false;
    public int CompanionAppPort { get; set; } = 8891;
    public bool EnableCompanionMap { get; set; } = false;

    // --- Telemetry Sync ---
    public bool EnableTelemetry { get; set; } = false;
    public int TelemetryPort { get; set; } = 8895;

    // --- Immersive Features ---
    public bool EnableExertionDistortion { get; set; } = false;
    public bool EnableRadioRepeaters { get; set; } = false;
    public bool IsRadioRepeater { get; set; } = false;
    public bool EnableShipPa { get; set; } = false;
    public string PttPaKey { get; set; } = "P";

    // --- Intercom Degradation ---
    public bool EnableIntercomDegradation { get; set; } = false;
    public bool IntercomShieldHitsEnabled { get; set; } = true;
    public bool IntercomCriticalPowerEnabled { get; set; } = true;
    public bool IntercomQuantumTravelEnabled { get; set; } = true;
}

