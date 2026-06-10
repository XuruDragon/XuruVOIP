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
    public bool UseGrtpr { get; set; } = false;
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
    public bool EnableRadioDegradation { get; set; } = true;
    public bool EnablePttChimes { get; set; } = true;
    public bool EnableEnvironmentalAcoustics { get; set; } = true;
    public bool EnableHelmetModulator { get; set; } = true;
    public bool EnableVoiceChanger { get; set; } = false;
    public string VoiceChangerType { get; set; } = "None"; // None, Alien, Cyborg, Robotic, PitchShift
    public float VoicePitchFactor { get; set; } = 1.0f;

    // --- Borderless Overlay ---
    public bool EnableOverlay { get; set; } = false;
    public string OverlayPosition { get; set; } = "TopLeft";
    public bool EnableRadar { get; set; } = true;
    public double RadarRange { get; set; } = 50.0;
    public bool EnableStt { get; set; } = false;

    // --- Discord RPC ---
    public bool EnableDiscordRpc { get; set; } = true;
}

