using System;
using System.Collections.Generic;
using System.Linq;

namespace XuruVoipClient.Services;

public enum VoiceCommandAction
{
    None,
    VisorToggle,
    MicMuteProximity,
    MicUnmuteProximity,
    MicMuteRadio,
    MicUnmuteRadio,
    MicMuteProfile,
    MicUnmuteProfile,
    MicMuteAll,
    MicUnmuteAll,
    RadioChannelSwitch,
    VoiceChangerProfile
}

public class VoiceCommandResult
{
    public VoiceCommandAction Action { get; set; } = VoiceCommandAction.None;
    public string RawText { get; set; } = "";
    public string TargetChannel { get; set; } = "";
    public string TargetProfile { get; set; } = "";
    public double Similarity { get; set; } = 0.0;
}

public class VoiceCommandService
{
    // Localized trigger dictionaries
    private static readonly Dictionary<string, string[]> VisorTriggers = new(StringComparer.OrdinalIgnoreCase)
    {
        { "en", new[] { "visor", "helmet", "toggle visor", "toggle helmet", "visor toggle" } },
        { "fr", new[] { "visiere", "casque", "basculer la visiere", "basculer le casque", "toggle visiere" } },
        { "de", new[] { "visier", "helm", "visier umschalten", "helm umschalten" } },
        { "es", new[] { "visera", "casco", "alternar visera", "alternar casco" } },
        { "pt", new[] { "visera", "capacete", "alternar visera", "alternar capacete" } },
        { "ja", new[] { "バイザー", "ヘルメット", "切り替え", "バイザー切り替え" } },
        { "zh", new[] { "头盔", "面罩", "切换头盔", "切换面罩" } }
    };

    private static readonly Dictionary<string, string[]> MicMuteProximityTriggers = new(StringComparer.OrdinalIgnoreCase)
    {
        { "en", new[] { "mute proximity", "silence proximity", "disable proximity mic", "proximity mute" } },
        { "fr", new[] { "muet proximite", "couper proximite", "silencer proximite" } },
        { "de", new[] { "naehe stummschalten", "proximity stummschalten" } },
        { "es", new[] { "silenciar proximidad", "mutear proximidad" } },
        { "pt", new[] { "silenciar proximidade", "mutear proximidade" } },
        { "ja", new[] { "近接ミュート", "プロキシミティミュート" } },
        { "zh", new[] { "静音近距", "近距离静音" } }
    };

    private static readonly Dictionary<string, string[]> MicUnmuteProximityTriggers = new(StringComparer.OrdinalIgnoreCase)
    {
        { "en", new[] { "unmute proximity", "enable proximity mic", "proximity unmute" } },
        { "fr", new[] { "activer proximite", "retablir proximite", "unmute proximite" } },
        { "de", new[] { "naehe lautschalten", "proximity lautschalten" } },
        { "es", new[] { "activar proximidad", "desmutear proximidad" } },
        { "pt", new[] { "ativar proximidade", "desmutear proximidade" } },
        { "ja", new[] { "近接ミュート解除", "プロキシミティ解除" } },
        { "zh", new[] { "取消静音近距", "近距离取消静音" } }
    };

    private static readonly Dictionary<string, string[]> MicMuteRadioTriggers = new(StringComparer.OrdinalIgnoreCase)
    {
        { "en", new[] { "mute radio", "silence radio", "disable radio mic", "radio mute" } },
        { "fr", new[] { "muet radio", "couper radio", "silencer radio" } },
        { "de", new[] { "funk stummschalten", "radio stummschalten" } },
        { "es", new[] { "silenciar radio", "mutear radio" } },
        { "pt", new[] { "silenciar radio", "mutear radio" } },
        { "ja", new[] { "ラジオミュート", "無線ミュート" } },
        { "zh", new[] { "静音无线电", "无线电静音" } }
    };

    private static readonly Dictionary<string, string[]> MicUnmuteRadioTriggers = new(StringComparer.OrdinalIgnoreCase)
    {
        { "en", new[] { "unmute radio", "enable radio mic", "radio unmute" } },
        { "fr", new[] { "activer radio", "retablir radio", "unmute radio" } },
        { "de", new[] { "funk lautschalten", "radio lautschalten" } },
        { "es", new[] { "activar radio", "desmutear radio" } },
        { "pt", new[] { "ativar radio", "desmutear radio" } },
        { "ja", new[] { "ラジオミュート解除", "無線ミュート解除" } },
        { "zh", new[] { "取消静音无线电", "无线电取消静音" } }
    };

    private static readonly Dictionary<string, string[]> MicMuteProfileTriggers = new(StringComparer.OrdinalIgnoreCase)
    {
        { "en", new[] { "mute profile", "silence profile", "disable profile mic", "profile mute" } },
        { "fr", new[] { "muet profil", "couper profil", "silencer profil" } },
        { "de", new[] { "profil stummschalten" } },
        { "es", new[] { "silenciar perfil", "mutear perfil" } },
        { "pt", new[] { "silenciar perfil", "mutear perfil" } },
        { "ja", new[] { "プロファイルミュート", "プロフィールミュート" } },
        { "zh", new[] { "静音配置文件", "配置文件静音" } }
    };

    private static readonly Dictionary<string, string[]> MicUnmuteProfileTriggers = new(StringComparer.OrdinalIgnoreCase)
    {
        { "en", new[] { "unmute profile", "enable profile mic", "profile unmute" } },
        { "fr", new[] { "activer profil", "retablir profil", "unmute profil" } },
        { "de", new[] { "profil lautschalten" } },
        { "es", new[] { "activar perfil", "desmutear perfil" } },
        { "pt", new[] { "ativar perfil", "desmutear perfil" } },
        { "ja", new[] { "プロファイル解除", "プロフィール解除" } },
        { "zh", new[] { "取消静音配置文件", "配置文件取消静音" } }
    };

    private static readonly Dictionary<string, string[]> MicMuteAllTriggers = new(StringComparer.OrdinalIgnoreCase)
    {
        { "en", new[] { "mute all", "silence all", "mute microphone", "mute mic" } },
        { "fr", new[] { "couper tout", "muet tout", "couper micro", "muet micro" } },
        { "de", new[] { "alles stummschalten", "mikrofon stummschalten", "mikro stummschalten" } },
        { "es", new[] { "silenciar todo", "mutear todo", "silenciar microfono", "mutear micro" } },
        { "pt", new[] { "silenciar tudo", "mutear tudo", "silenciar microfone", "mutear micro" } },
        { "ja", new[] { "全ミュート", "マイクミュート", "すべてミュート" } },
        { "zh", new[] { "全部静音", "静音麦克风", "麦克风静音" } }
    };

    private static readonly Dictionary<string, string[]> MicUnmuteAllTriggers = new(StringComparer.OrdinalIgnoreCase)
    {
        { "en", new[] { "unmute all", "enable microphone", "unmute mic" } },
        { "fr", new[] { "activer tout", "retablir tout", "activer micro", "unmute micro" } },
        { "de", new[] { "alles lautschalten", "mikrofon lautschalten", "mikro lautschalten" } },
        { "es", new[] { "activar todo", "desmutear todo", "activar microfono", "desmutear micro" } },
        { "pt", new[] { "ativar tudo", "desmutear tudo", "ativar microfone", "desmutear micro" } },
        { "ja", new[] { "ミュート解除", "マイク解除", "すべてミュート解除" } },
        { "zh", new[] { "取消全部静音", "启用麦克风", "麦克风取消静音" } }
    };

    private static readonly Dictionary<string, string[]> RadioChannelTriggers = new(StringComparer.OrdinalIgnoreCase)
    {
        { "en", new[] { "set channel", "change channel", "switch channel", "channel to", "channel" } },
        { "fr", new[] { "changer de canal", "canal vers", "basculer sur le canal", "canal" } },
        { "de", new[] { "kanal wechseln", "schalte kanal", "kanal auf", "kanal" } },
        { "es", new[] { "cambiar canal", "canal a", "establecer canal", "canal" } },
        { "pt", new[] { "mudar canal", "canal para", "definir canal", "canal" } },
        { "ja", new[] { "チャンネル変更", "チャンネルを", "チャンネル" } },
        { "zh", new[] { "切换频道", "设置频道", "频道" } }
    };

    private static readonly Dictionary<string, string[]> VoiceChangerTriggers = new(StringComparer.OrdinalIgnoreCase)
    {
        { "en", new[] { "voice changer", "voice profile", "voice modifier", "set voice" } },
        { "fr", new[] { "modificateur de voix", "profil de voix", "modifier voix", "voix" } },
        { "de", new[] { "stimmenverzerrer", "stimmprofil", "stimme" } },
        { "es", new[] { "modulador de voz", "perfil de voz", "voz" } },
        { "pt", new[] { "modulador de voz", "perfil de voz", "voz" } },
        { "ja", new[] { "ボイスチェンジャー", "音声プロフィール", "ボイス" } },
        { "zh", new[] { "变声器", "声音配置文件", "变声" } }
    };

    // Voice Changer Profiles localized
    private static readonly string[] ProfileAlienNames = new[] { "alien", "extraterrestre", "alienígena", "エイリアン", "外星人" };
    private static readonly string[] ProfileCyborgNames = new[] { "cyborg", "ciborg", "サイボーグ", "半机械人", "改造人" };
    private static readonly string[] ProfileRoboticNames = new[] { "robotic", "robot", "robotique", "robótico", "ロボット", "机器人" };
    private static readonly string[] ProfilePitchShiftNames = new[] { "pitchshift", "pitch shift", "hauteur", "tono", "pitch", "ピッチシフト", "音高" };
    private static readonly string[] ProfileNoneNames = new[] { "none", "off", "normal", "desactive", "aucun", "aus", "desactivado", "なし", "无", "关闭" };

    public event Action? VisorToggleRequested;
    public event Action<string>? ChannelChangeRequested;
    public event Action<string>? VoiceChangerProfileRequested;
    public event Action<VoiceCommandAction>? MicStateChangeRequested;

    public VoiceCommandResult ParseAndExecute(string text, string appLang, IEnumerable<string> availableChannels, double confidence = 0.5)
    {
        if (string.IsNullOrEmpty(text))
            return new VoiceCommandResult { Action = VoiceCommandAction.None };

        string cleanText = NormalizeText(text);
        string lang = GetLanguageKey(appLang);

        LogService.Info($"VoiceCommandService Parsing: \"{cleanText}\" (Lang: {lang}, Conf: {confidence})");

        var result = new VoiceCommandResult { RawText = text };

        // 1. Visor Toggle
        if (MatchesTrigger(cleanText, lang, VisorTriggers, confidence, out double sim))
        {
            result.Action = VoiceCommandAction.VisorToggle;
            result.Similarity = sim;
            VisorToggleRequested?.Invoke();
            return result;
        }

        // 2. Mute/Unmute Actions (evaluate Unmute first to avoid substring conflicts with Mute)
        if (MatchesTrigger(cleanText, lang, MicUnmuteProximityTriggers, confidence, out sim))
        {
            result.Action = VoiceCommandAction.MicUnmuteProximity;
            result.Similarity = sim;
            MicStateChangeRequested?.Invoke(result.Action);
            return result;
        }
        if (MatchesTrigger(cleanText, lang, MicMuteProximityTriggers, confidence, out sim))
        {
            result.Action = VoiceCommandAction.MicMuteProximity;
            result.Similarity = sim;
            MicStateChangeRequested?.Invoke(result.Action);
            return result;
        }
        if (MatchesTrigger(cleanText, lang, MicUnmuteRadioTriggers, confidence, out sim))
        {
            result.Action = VoiceCommandAction.MicUnmuteRadio;
            result.Similarity = sim;
            MicStateChangeRequested?.Invoke(result.Action);
            return result;
        }
        if (MatchesTrigger(cleanText, lang, MicMuteRadioTriggers, confidence, out sim))
        {
            result.Action = VoiceCommandAction.MicMuteRadio;
            result.Similarity = sim;
            MicStateChangeRequested?.Invoke(result.Action);
            return result;
        }
        if (MatchesTrigger(cleanText, lang, MicUnmuteProfileTriggers, confidence, out sim))
        {
            result.Action = VoiceCommandAction.MicUnmuteProfile;
            result.Similarity = sim;
            MicStateChangeRequested?.Invoke(result.Action);
            return result;
        }
        if (MatchesTrigger(cleanText, lang, MicMuteProfileTriggers, confidence, out sim))
        {
            result.Action = VoiceCommandAction.MicMuteProfile;
            result.Similarity = sim;
            MicStateChangeRequested?.Invoke(result.Action);
            return result;
        }
        if (MatchesTrigger(cleanText, lang, MicUnmuteAllTriggers, confidence, out sim))
        {
            result.Action = VoiceCommandAction.MicUnmuteAll;
            result.Similarity = sim;
            MicStateChangeRequested?.Invoke(result.Action);
            return result;
        }
        if (MatchesTrigger(cleanText, lang, MicMuteAllTriggers, confidence, out sim))
        {
            result.Action = VoiceCommandAction.MicMuteAll;
            result.Similarity = sim;
            MicStateChangeRequested?.Invoke(result.Action);
            return result;
        }

        // 3. Radio Channel Switch
        if (MatchesTrigger(cleanText, lang, RadioChannelTriggers, 0.1, out sim)) // Use lower trigger similarity to find channels
        {
            // Find which channel name is mentioned in the clean text
            foreach (var ch in availableChannels)
            {
                string normCh = NormalizeText(ch);
                if (cleanText.Contains(normCh))
                {
                    double channelSim = (double)normCh.Length / cleanText.Length;
                    // Boost similarity if it has a radio channel trigger prefix
                    if (channelSim >= confidence || sim >= confidence)
                    {
                        result.Action = VoiceCommandAction.RadioChannelSwitch;
                        result.TargetChannel = ch;
                        result.Similarity = Math.Max(channelSim, sim);
                        ChannelChangeRequested?.Invoke(ch);
                        return result;
                    }
                }
            }
        }

        // 4. Voice Changer Profile
        if (MatchesTrigger(cleanText, lang, VoiceChangerTriggers, 0.1, out sim))
        {
            string matchedProfile = "";
            string matchedKeyword = "";
            if (ContainsAny(cleanText, ProfileAlienNames, out matchedKeyword)) matchedProfile = "Alien";
            else if (ContainsAny(cleanText, ProfileCyborgNames, out matchedKeyword)) matchedProfile = "Cyborg";
            else if (ContainsAny(cleanText, ProfileRoboticNames, out matchedKeyword)) matchedProfile = "Robotic";
            else if (ContainsAny(cleanText, ProfilePitchShiftNames, out matchedKeyword)) matchedProfile = "PitchShift";
            else if (ContainsAny(cleanText, ProfileNoneNames, out matchedKeyword)) matchedProfile = "None";

            if (!string.IsNullOrEmpty(matchedProfile))
            {
                double profileSim = (double)matchedKeyword.Length / cleanText.Length;
                if (profileSim >= confidence || sim >= confidence)
                {
                    result.Action = VoiceCommandAction.VoiceChangerProfile;
                    result.TargetProfile = matchedProfile;
                    result.Similarity = Math.Max(profileSim, sim);
                    VoiceChangerProfileRequested?.Invoke(matchedProfile);
                    return result;
                }
            }
        }

        // 5. Fallback: try to see if a channel name is simply said directly (e.g. "Alpha")
        foreach (var ch in availableChannels)
        {
            string normCh = NormalizeText(ch);
            if (cleanText == normCh || cleanText.EndsWith(" " + normCh))
            {
                double channelSim = (double)normCh.Length / cleanText.Length;
                if (channelSim >= confidence)
                {
                    result.Action = VoiceCommandAction.RadioChannelSwitch;
                    result.TargetChannel = ch;
                    result.Similarity = channelSim;
                    ChannelChangeRequested?.Invoke(ch);
                    return result;
                }
            }
        }

        return result;
    }

    private bool MatchesTrigger(string text, string lang, Dictionary<string, string[]> triggers, double confidence, out double maxSimilarity)
    {
        maxSimilarity = 0.0;
        bool foundMatch = false;

        if (triggers.TryGetValue(lang, out var list))
        {
            foreach (var t in list)
            {
                string normTrigger = NormalizeText(t);
                if (text.Contains(normTrigger))
                {
                    double sim = (double)normTrigger.Length / text.Length;
                    if (sim > maxSimilarity)
                    {
                        maxSimilarity = sim;
                    }
                    if (sim >= confidence)
                    {
                        foundMatch = true;
                    }
                }
            }
        }

        if (lang != "en" && triggers.TryGetValue("en", out var enList))
        {
            foreach (var t in enList)
            {
                string normTrigger = NormalizeText(t);
                if (text.Contains(normTrigger))
                {
                    double sim = (double)normTrigger.Length / text.Length;
                    if (sim > maxSimilarity)
                    {
                        maxSimilarity = sim;
                    }
                    if (sim >= confidence)
                    {
                        foundMatch = true;
                    }
                }
            }
        }

        return foundMatch;
    }

    private bool ContainsAny(string text, IEnumerable<string> searchTerms, out string matchedKeyword)
    {
        matchedKeyword = "";
        foreach (var term in searchTerms)
        {
            string normTerm = NormalizeText(term);
            if (text.Contains(normTerm))
            {
                matchedKeyword = normTerm;
                return true;
            }
        }
        return false;
    }

    private string NormalizeText(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        var chars = text.ToCharArray();
        var sb = new System.Text.StringBuilder();
        foreach (var c in chars)
        {
            if (char.IsLetterOrDigit(c) || char.IsWhiteSpace(c))
            {
                sb.Append(char.ToLowerInvariant(c));
            }
        }
        return System.Text.RegularExpressions.Regex.Replace(sb.ToString().Trim(), @"\s+", " ");
    }

    private string GetLanguageKey(string appLang)
    {
        if (string.IsNullOrEmpty(appLang)) return "en";
        appLang = appLang.ToLowerInvariant();
        if (appLang.StartsWith("fr")) return "fr";
        if (appLang.StartsWith("de")) return "de";
        if (appLang.StartsWith("es")) return "es";
        if (appLang.StartsWith("pt")) return "pt";
        if (appLang.StartsWith("ja")) return "ja";
        if (appLang.StartsWith("zh")) return "zh";
        return "en";
    }
}
