using System;
using System.Collections.Generic;

namespace XuruVoipClient.Services;

public static class TranslationService
{
    // Map phrase -> English canonical key
    private static readonly Dictionary<string, string> PhraseToKey = new(StringComparer.OrdinalIgnoreCase)
    {
        // English
        ["roger that"] = "roger that",
        ["affirmative"] = "affirmative",
        ["negative"] = "negative",
        ["mayday mayday"] = "mayday mayday",
        ["hostile contact"] = "hostile contact",
        ["need backup"] = "need backup",
        ["target down"] = "target down",
        ["copy that"] = "copy that",
        ["hold position"] = "hold position",
        ["engaging target"] = "engaging target",
        ["requesting landing"] = "requesting landing",
        ["hello"] = "hello",
        ["help"] = "help",
        ["ok"] = "ok",

        // French
        ["bien reçu"] = "roger that",
        ["affirmatif"] = "affirmative",
        ["négatif"] = "negative",
        ["contact hostile"] = "hostile contact",
        ["besoin de soutien"] = "need backup",
        ["cible éliminée"] = "target down",
        ["tenez la position"] = "hold position",
        ["cible engagée"] = "engaging target",
        ["demande d'atterrissage"] = "requesting landing",
        ["bonjour"] = "hello",
        ["aide"] = "help",
        ["d'accord"] = "ok",

        // German
        ["verstanden"] = "roger that",
        ["jawohl"] = "affirmative",
        ["negativ"] = "negative",
        ["feindkontakt"] = "hostile contact",
        ["brauche unterstützung"] = "need backup",
        ["ziel ausgeschaltet"] = "target down",
        ["position halten"] = "hold position",
        ["greife ziel an"] = "engaging target",
        ["landung anfordern"] = "requesting landing",
        ["hallo"] = "hello",
        ["hilfe"] = "help",

        // Spanish
        ["recibido"] = "roger that",
        ["afirmativo"] = "affirmative",
        ["negativo"] = "negative",
        ["contacto hostil"] = "hostile contact",
        ["necesito apoyo"] = "need backup",
        ["objetivo abatido"] = "target down",
        ["entendido"] = "copy that",
        ["mantener posición"] = "hold position",
        ["atacando objetivo"] = "engaging target",
        ["solicitando aterrizaje"] = "requesting landing",
        ["hola"] = "hello",
        ["ayuda"] = "help",
        ["vale"] = "ok",

        // Portuguese
        ["contato hostil"] = "hostile contact",
        ["preciso de reforço"] = "need backup",
        ["alvo abatido"] = "target down",
        ["manter posição"] = "hold position",
        ["engajando alvo"] = "engaging target",
        ["solicitando pouso"] = "requesting landing",
        ["olá"] = "hello",
        ["ajuda"] = "help",

        // Chinese
        ["收到"] = "roger that",
        ["确认"] = "affirmative",
        ["否定"] = "negative",
        ["呼救 呼救"] = "mayday mayday",
        ["敌方接触"] = "hostile contact",
        ["需要支援"] = "need backup",
        ["目标击落"] = "target down",
        ["明白"] = "copy that",
        ["保持位置"] = "hold position",
        ["交战目标"] = "engaging target",
        ["请求着陆"] = "requesting landing",
        ["你好"] = "hello",
        ["救命"] = "help",
        ["好的"] = "ok",

        // Japanese
        ["了解"] = "roger that",
        ["肯定"] = "affirmative",
        ["メーデー メーデー"] = "mayday mayday",
        ["敌対接触"] = "hostile contact",
        ["支援が必要"] = "need backup",
        ["目標撃破"] = "target down",
        ["位置を維持"] = "hold position",
        ["目標と交戦中"] = "engaging target",
        ["着陸要請"] = "requesting landing",
        ["こんにちは"] = "hello",
        ["助けて"] = "help",
        ["オーケー"] = "ok"
    };

    // Map English canonical key -> target language phrase
    private static readonly Dictionary<string, Dictionary<string, string>> KeyToTranslation = new(StringComparer.OrdinalIgnoreCase)
    {
        ["roger that"] = new(StringComparer.OrdinalIgnoreCase) {
            ["en"] = "Roger that",
            ["fr"] = "Bien reçu",
            ["de"] = "Verstanden",
            ["es"] = "Recibido",
            ["pt"] = "Entendido",
            ["zh"] = "收到",
            ["ja"] = "了解"
        },
        ["affirmative"] = new(StringComparer.OrdinalIgnoreCase) {
            ["en"] = "Affirmative",
            ["fr"] = "Affirmatif",
            ["de"] = "Jawohl",
            ["es"] = "Afirmativo",
            ["pt"] = "Afirmativo",
            ["zh"] = "确认",
            ["ja"] = "肯定"
        },
        ["negative"] = new(StringComparer.OrdinalIgnoreCase) {
            ["en"] = "Negative",
            ["fr"] = "Négatif",
            ["de"] = "Negativ",
            ["es"] = "Negativo",
            ["pt"] = "Negativo",
            ["zh"] = "否定",
            ["ja"] = "否定"
        },
        ["mayday mayday"] = new(StringComparer.OrdinalIgnoreCase) {
            ["en"] = "Mayday mayday",
            ["fr"] = "Mayday mayday",
            ["de"] = "Mayday mayday",
            ["es"] = "Mayday mayday",
            ["pt"] = "Mayday mayday",
            ["zh"] = "呼救 呼救",
            ["ja"] = "メーデー メーデー"
        },
        ["hostile contact"] = new(StringComparer.OrdinalIgnoreCase) {
            ["en"] = "Hostile contact",
            ["fr"] = "Contact hostile",
            ["de"] = "Feindkontakt",
            ["es"] = "Contacto hostil",
            ["pt"] = "Contato hostil",
            ["zh"] = "敌方接触",
            ["ja"] = "敌対接触"
        },
        ["need backup"] = new(StringComparer.OrdinalIgnoreCase) {
            ["en"] = "Need backup",
            ["fr"] = "Besoin de soutien",
            ["de"] = "Brauche Unterstützung",
            ["es"] = "Necesito apoyo",
            ["pt"] = "Preciso de reforço",
            ["zh"] = "需要支援",
            ["ja"] = "支援が必要"
        },
        ["target down"] = new(StringComparer.OrdinalIgnoreCase) {
            ["en"] = "Target down",
            ["fr"] = "Cible éliminée",
            ["de"] = "Ziel ausgeschaltet",
            ["es"] = "Objetivo abatido",
            ["pt"] = "Alvo abatido",
            ["zh"] = "目标击落",
            ["ja"] = "目標撃破"
        },
        ["copy that"] = new(StringComparer.OrdinalIgnoreCase) {
            ["en"] = "Copy that",
            ["fr"] = "Bien reçu",
            ["de"] = "Verstanden",
            ["es"] = "Entendido",
            ["pt"] = "Entendido",
            ["zh"] = "明白",
            ["ja"] = "了解"
        },
        ["hold position"] = new(StringComparer.OrdinalIgnoreCase) {
            ["en"] = "Hold position",
            ["fr"] = "Tenez la position",
            ["de"] = "Position halten",
            ["es"] = "Mantener posición",
            ["pt"] = "Manter posição",
            ["zh"] = "保持位置",
            ["ja"] = "位置を維持"
        },
        ["engaging target"] = new(StringComparer.OrdinalIgnoreCase) {
            ["en"] = "Engaging target",
            ["fr"] = "Cible engagée",
            ["de"] = "Greife Ziel an",
            ["es"] = "Atacando objetivo",
            ["pt"] = "Engajando alvo",
            ["zh"] = "交战目标",
            ["ja"] = "目標と交戦中"
        },
        ["requesting landing"] = new(StringComparer.OrdinalIgnoreCase) {
            ["en"] = "Requesting landing",
            ["fr"] = "Demande d'atterrissage",
            ["de"] = "Landung anfordern",
            ["es"] = "Solicitando aterrizaje",
            ["pt"] = "Solicitando pouso",
            ["zh"] = "请求着陆",
            ["ja"] = "着陸要請"
        },
        ["hello"] = new(StringComparer.OrdinalIgnoreCase) {
            ["en"] = "Hello",
            ["fr"] = "Bonjour",
            ["de"] = "Hallo",
            ["es"] = "Hola",
            ["pt"] = "Olá",
            ["zh"] = "你好",
            ["ja"] = "こんにちは"
        },
        ["help"] = new(StringComparer.OrdinalIgnoreCase) {
            ["en"] = "Help",
            ["fr"] = "Aide",
            ["de"] = "Hilfe",
            ["es"] = "Ayuda",
            ["pt"] = "Ajuda",
            ["zh"] = "救命",
            ["ja"] = "助けて"
        },
        ["ok"] = new(StringComparer.OrdinalIgnoreCase) {
            ["en"] = "OK",
            ["fr"] = "D'accord",
            ["de"] = "OK",
            ["es"] = "Vale",
            ["pt"] = "OK",
            ["zh"] = "好的",
            ["ja"] = "オーケー"
        }
    };

    public static string TranslatePhrase(string text, string fromLang, string toLang)
    {
        if (string.IsNullOrEmpty(text)) return text;

        // Simplify language codes
        string from = SimplifyLangCode(fromLang);
        string to = SimplifyLangCode(toLang);

        if (from == to) return text;

        // Clean punctuation and spacing for lookup
        string clean = CleanPhrase(text);

        if (PhraseToKey.TryGetValue(clean, out var key))
        {
            if (KeyToTranslation.TryGetValue(key, out var translations))
            {
                if (translations.TryGetValue(to, out var translated))
                {
                    return translated;
                }
                // Fallback to English canonical if requested target language is not available
                if (translations.TryGetValue("en", out var englishFallback))
                {
                    return englishFallback;
                }
            }
        }

        return text;
    }

    private static string SimplifyLangCode(string code)
    {
        if (string.IsNullOrEmpty(code)) return "en";
        code = code.Split('-')[0].ToLowerInvariant();
        return code;
    }

    private static string CleanPhrase(string text)
    {
        // Remove common punctuation: . , ! ? " '
        var sb = new System.Text.StringBuilder();
        foreach (char c in text)
        {
            if (!char.IsPunctuation(c))
            {
                sb.Append(c);
            }
        }
        return sb.ToString().Trim();
    }
}
