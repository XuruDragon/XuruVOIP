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
    VoiceChangerProfile,
    ShipPowerToggle,
    ShipDoorsToggle,
    ShipShieldsFront,
    ShipLandingGearToggle,
    ShipEnginesToggle,
    ShipWeaponsToggle,
    ShipShieldsToggle,
    ShipShieldsReset,
    ShipVtolToggle,
    ShipQuantumSpool,
    ShipCruiseControl,
    ShipLandingRequest,
    ShipFlyMode,
    ShipScanMode,
    ShipMiningMode,
    ShipSalvageMode,
    ShipPowerWeapons,
    ShipPowerShields,
    ShipPowerEngines,
    ShipPowerReset,
    ShipDecoy,
    ShipNoise,
    ShipLights,
    ShipTargetHostile,
    ShipCycleSubsystems,
    ShipGimbalMode,
    ShipPinTarget,
    ShipDecoupledMode,
    ShipGSafeToggle,
    ShipSpeedLimiterToggle,
    ShipDecoyBurstIncrease,
    ShipDecoyBurstReset,
    ShipWipeVisor,
    ShipHailTarget
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

    private static readonly Dictionary<string, string[]> ShipPowerTriggers = new(StringComparer.OrdinalIgnoreCase)
    {
        { "en", new[] { "power", "toggle power", "power on", "power off", "systems on", "systems off" } },
        { "fr", new[] { "alimentation", "basculer alimentation", "allumer systemes", "eteindre systemes", "demarrer systemes", "couper systemes" } },
        { "de", new[] { "energie", "energie umschalten", "systeme an", "systeme aus", "strom an", "strom aus" } },
        { "es", new[] { "energia", "alternar energia", "sistemas encendidos", "sistemas apagados" } },
        { "pt", new[] { "energia", "alternar energia", "sistemas ligados", "sistemas desligados" } },
        { "ja", new[] { "パワー", "電源切り替え", "システム起動", "システム停止" } },
        { "zh", new[] { "电源", "切换电源", "系统开启", "系统关闭" } }
    };

    private static readonly Dictionary<string, string[]> ShipDoorsTriggers = new(StringComparer.OrdinalIgnoreCase)
    {
        { "en", new[] { "doors", "exterior", "open doors", "close doors", "open exterior", "close exterior", "toggle doors", "toggle exterior" } },
        { "fr", new[] { "portes", "exterieur", "ouvrir portes", "fermer portes", "ouvrir exterieur", "fermer exterieur", "basculer portes" } },
        { "de", new[] { "tueren", "tueren oeffnen", "tueren schliessen", "aussen oeffnen", "aussen schliessen" } },
        { "es", new[] { "puertas", "exterior", "abrir puertas", "cerrar puertas", "abrir exterior", "cerrar exterior" } },
        { "pt", new[] { "portas", "exterior", "abrir portas", "fechar portas", "abrir exterior", "fechar exterior" } },
        { "ja", new[] { "ドア", "外部", "ドア開閉", "ドアを開ける", "ドアを閉める", "外部開放", "外部閉鎖" } },
        { "zh", new[] { "舱门", "外部", "开启舱门", "关闭舱门", "开启外部", "关闭外部" } }
    };

    private static readonly Dictionary<string, string[]> ShipShieldsTriggers = new(StringComparer.OrdinalIgnoreCase)
    {
        { "en", new[] { "shields", "shields front", "shields forward", "divert shields", "shields ahead" } },
        { "fr", new[] { "boucliers", "boucliers avant", "devier boucliers", "boucliers devant" } },
        { "de", new[] { "schilde", "schilde vorne", "schilde vorwaerts", "schilde umleiten" } },
        { "es", new[] { "escudos", "escudos al frente", "escudos adelante", "desviar escudos" } },
        { "pt", new[] { "escudos", "escudos na frente", "escudos para frente", "desviar escudos" } },
        { "ja", new[] { "シールド", "シールド前方", "シールド前", "シールド偏向" } },
        { "zh", new[] { "护盾", "护盾向前", "前部护盾", "偏转护盾" } }
    };

    private static readonly Dictionary<string, string[]> ShipLandingGearTriggers = new(StringComparer.OrdinalIgnoreCase)
    {
        { "en", new[] { "landing gear", "deploy landing gear", "retract landing gear", "toggle landing gear", "gear" } },
        { "fr", new[] { "train d'atterrissage", "deployer train", "rentrer train", "train atterrissage" } },
        { "de", new[] { "fahrwerk", "fahrwerk ausfahren", "fahrwerk einfahren" } },
        { "es", new[] { "tren de aterrizaje", "desplegar tren", "retraer tren" } },
        { "pt", new[] { "trem de pouso", "desdobrar trem", "recolher trem" } },
        { "ja", new[] { "ランディングギア", "ギア", "ランディングギア展開", "ギア展開", "ギア格納" } },
        { "zh", new[] { "起落架", "收放起落架", "放下起落架", "收起起落架" } }
    };

    private static readonly Dictionary<string, string[]> ShipEnginesTriggers = new(StringComparer.OrdinalIgnoreCase)
    {
        { "en", new[] { "engines", "toggle engines", "engines on", "engines off", "power engines" } },
        { "fr", new[] { "moteurs", "basculer moteurs", "allumer moteurs", "eteindre moteurs", "demarrer moteurs", "couper moteurs" } },
        { "de", new[] { "motoren", "motoren umschalten", "motoren an", "motoren aus" } },
        { "es", new[] { "motores", "alternar motores", "motores encendidos", "motores apagados" } },
        { "pt", new[] { "motores", "alternar motores", "motores ligados", "motores desligados" } },
        { "ja", new[] { "エンジン", "エンジン切り替え", "エンジン起動", "エンジン停止" } },
        { "zh", new[] { "引擎", "发动机", "切换引擎", "切换发动机", "引擎开启", "引擎关闭" } }
    };

    private static readonly Dictionary<string, string[]> ShipWeaponsTriggers = new(StringComparer.OrdinalIgnoreCase)
    {
        { "en", new[] { "weapons", "toggle weapons", "weapons on", "weapons off", "power weapons", "weapons online", "weapons offline" } },
        { "fr", new[] { "armes", "basculer armes", "allumer armes", "eteindre armes", "activer armes", "desactiver armes" } },
        { "de", new[] { "waffen", "waffen umschalten", "waffen an", "waffen aus", "waffen online", "waffen offline" } },
        { "es", new[] { "armas", "alternar armas", "armas encendidas", "armas apagadas" } },
        { "pt", new[] { "armas", "alternar armas", "armas ligadas", "armas desligadas" } },
        { "ja", new[] { "武器", "武器切り替え", "兵装", "ウェポン" } },
        { "zh", new[] { "武器", "切换武器", "武器开启" , "武器关闭" } }
    };

    private static readonly Dictionary<string, string[]> ShipShieldsToggleTriggers = new(StringComparer.OrdinalIgnoreCase)
    {
        { "en", new[] { "shields toggle", "toggle shields", "power shields", "shields on", "shields off" } },
        { "fr", new[] { "boucliers basculer", "basculer boucliers", "allumer boucliers", "eteindre boucliers", "boucliers" } },
        { "de", new[] { "schilde umschalten", "schilde an", "schilde aus" } },
        { "es", new[] { "escudos alternar", "alternar escudos", "escudos encendidos", "escudos apagados" } },
        { "pt", new[] { "escudos alternar", "alternar escudos", "escudos ligados", "escudos desligados" } },
        { "ja", new[] { "シールド切り替え", "シールド電源" } },
        { "zh", new[] { "切换护盾", "护盾开启", "护盾关闭" } }
    };

    private static readonly Dictionary<string, string[]> ShipShieldsResetTriggers = new(StringComparer.OrdinalIgnoreCase)
    {
        { "en", new[] { "reset shields", "balance shields", "equalize shields", "shields reset", "shields equalize", "shields balance" } },
        { "fr", new[] { "equilibrer boucliers", "reinitialiser boucliers", "boucliers equilibrer", "boucliers reset" } },
        { "de", new[] { "schilde zuruecksetzen", "schilde ausgleichen", "schilde reset" } },
        { "es", new[] { "restablecer escudos", "equilibrar escudos", "escudos equilibrar", "escudos reset" } },
        { "pt", new[] { "restabelecer escudos", "equilibrar escudos", "escudos equilibrar", "escudos reset" } },
        { "ja", new[] { "シールドリセット", "シールド均等", "シールドイコライズ" } },
        { "zh", new[] { "重置护盾", "均等护盾", "平衡护盾" } }
    };

    private static readonly Dictionary<string, string[]> ShipVtolTriggers = new(StringComparer.OrdinalIgnoreCase)
    {
        { "en", new[] { "vtol", "toggle vtol", "vtol mode", "vertical takeoff", "vtol engines" } },
        { "fr", new[] { "vtol", "basculer vtol", "mode vtol", "train vtol" } },
        { "de", new[] { "vtol", "vtol umschalten", "vtol modus" } },
        { "es", new[] { "vtol", "alternar vtol", "modo vtol" } },
        { "pt", new[] { "vtol", "alternar vtol", "modo vtol" } },
        { "ja", new[] { "vtol", "ブイトール", "vtol切り替え", "垂直起降切り替え" } },
        { "zh", new[] { "vtol", "垂直起降", "切换垂直起降" } }
    };

    private static readonly Dictionary<string, string[]> ShipQuantumSpoolTriggers = new(StringComparer.OrdinalIgnoreCase)
    {
        { "en", new[] { "quantum", "spool quantum", "spool drive", "quantum drive", "quantum spool", "quantum drive toggle" } },
        { "fr", new[] { "quantum", "quantum spouler", "moteur quantum", "activer quantum", "spouler quantum" } },
        { "de", new[] { "quantum", "quantum spoolen", "quantenantrieb", "quantum antrieb" } },
        { "es", new[] { "quantum", "iniciar quantum", "spool quantum", "motor quantum" } },
        { "pt", new[] { "quantum", "iniciar quantum", "motor quântico", "modo quântico" } },
        { "ja", new[] { "クアンタム", "クアンタムスプール", "量子ドライブ" } },
        { "zh", new[] { "量子", "量子启动", "量子引擎", "启动量子" } }
    };

    private static readonly Dictionary<string, string[]> ShipCruiseControlTriggers = new(StringComparer.OrdinalIgnoreCase)
    {
        { "en", new[] { "cruise", "cruise control", "toggle cruise", "cruise speed", "cruise lock" } },
        { "fr", new[] { "croisiere", "vitesse de croisiere", "regulateur de vitesse", "basculer croisiere" } },
        { "de", new[] { "tempomat", "tempomat umschalten", "cruise control" } },
        { "es", new[] { "crucero", "control de crucero", "alternar crucero" } },
        { "pt", new[] { "cruzeiro", "piloto automático", "alternar cruzeiro" } },
        { "ja", new[] { "クルーズ", "クルーズコントロール", "巡航切り替え" } },
        { "zh", new[] { "巡航", "定速巡航", "巡航控制", "切换巡航" } }
    };

    private static readonly Dictionary<string, string[]> ShipLandingRequestTriggers = new(StringComparer.OrdinalIgnoreCase)
    {
        { "en", new[] { "request landing", "landing request", "open hangar", "request hangar", "call atc" } },
        { "fr", new[] { "demande d'atterrissage", "demande atterrissage", "ouvrir hangar", "demander hangar", "appeler atc" } },
        { "de", new[] { "landing anfordern", "landung anfordern", "hangar oeffnen", "atc rufen" } },
        { "es", new[] { "solicitar aterrizaje", "pedir aterrizaje", "abrir hangar", "llamar atc" } },
        { "pt", new[] { "solicitar pouso", "pedir pouso", "abrir hangar", "chamar atc" } },
        { "ja", new[] { "着陸要請", "着陸リクエスト", "ハンガー開放", "atc呼出" } },
        { "zh", new[] { "请求降落", "申请降落", "开启机库", "请求机库", "呼叫atc" } }
    };

    private static readonly Dictionary<string, string[]> ShipFlyModeTriggers = new(StringComparer.OrdinalIgnoreCase)
    {
        { "en", new[] { "flight mode", "fly mode", "scm mode", "navigation mode", "nav mode" } },
        { "fr", new[] { "mode de vol", "mode vol", "navigation", "mode navigation" } },
        { "de", new[] { "flugmodus", "navigationsmodus", "nav modus" } },
        { "es", new[] { "modo de vuelo", "modo vuelo", "modo navegacion" } },
        { "pt", new[] { "modo de voo", "modo voo", "modo navegacao" } },
        { "ja", new[] { "飛行モード", "フライトモード", "ナビゲーションモード" } },
        { "zh", new[] { "飞行模式", "航行模式", "导航模式" } }
    };

    private static readonly Dictionary<string, string[]> ShipScanModeTriggers = new(StringComparer.OrdinalIgnoreCase)
    {
        { "en", new[] { "scan mode", "scanning mode", "scanner", "activate scanner" } },
        { "fr", new[] { "mode scan", "mode scanneur", "scanneur", "activer le scanneur" } },
        { "de", new[] { "scanmodus", "scanner aktivieren", "scan modus" } },
        { "es", new[] { "modo de escaneo", "modo escaneo", "escaner", "activar escaner" } },
        { "pt", new[] { "modo de varredura", "modo escaneamento", "escaner", "ativar escaner" } },
        { "ja", new[] { "スキャンモード", "スキャナー起動" } },
        { "zh", new[] { "扫描模式", "开启扫描", "扫描仪" } }
    };

    private static readonly Dictionary<string, string[]> ShipMiningModeTriggers = new(StringComparer.OrdinalIgnoreCase)
    {
        { "en", new[] { "mining mode", "start mining", "mining" } },
        { "fr", new[] { "mode minage", "activer minage", "commencer le minage", "minage" } },
        { "de", new[] { "bergbaumodus", "minenmodus", "mining" } },
        { "es", new[] { "modo mineria", "iniciar mineria", "mineria" } },
        { "pt", new[] { "modo mineracao", "iniciar mineracao", "mineracao" } },
        { "ja", new[] { "採掘モード", "マイニングモード" } },
        { "zh", new[] { "采矿模式", "开启采矿" } }
    };

    private static readonly Dictionary<string, string[]> ShipSalvageModeTriggers = new(StringComparer.OrdinalIgnoreCase)
    {
        { "en", new[] { "salvage mode", "start salvage", "salvage" } },
        { "fr", new[] { "mode recyclage", "mode salvage", "commencer le recyclage", "recyclage" } },
        { "de", new[] { "bergungsmodus", "recyclingmodus", "salvage" } },
        { "es", new[] { "modo de desguace", "modo salvamento", "desguace" } },
        { "pt", new[] { "modo de reciclagem", "modo salvamento", "reciclagem" } },
        { "ja", new[] { "回収モード", "サルベージモード" } },
        { "zh", new[] { "回收模式", "打捞模式" } }
    };

    private static readonly Dictionary<string, string[]> ShipPowerWeaponsTriggers = new(StringComparer.OrdinalIgnoreCase)
    {
        { "en", new[] { "power weapons", "max weapons", "full weapons", "power to weapons" } },
        { "fr", new[] { "puissance armes", "max armes", "armes au maximum", "energie aux armes" } },
        { "de", new[] { "energie waffen", "max waffen", "volle waffen", "energie auf waffen" } },
        { "es", new[] { "energia a las armas", "armas al maximo", "maximo armas" } },
        { "pt", new[] { "energia para armas", "armas no maximo", "maximo armas" } },
        { "ja", new[] { "武器電力", "武器マックス", "ウェポンマックス" } },
        { "zh", new[] { "武器分配", "武器最大", "最大武器" } }
    };

    private static readonly Dictionary<string, string[]> ShipPowerShieldsTriggers = new(StringComparer.OrdinalIgnoreCase)
    {
        { "en", new[] { "power shields", "max shields", "full shields", "power to shields" } },
        { "fr", new[] { "puissance boucliers", "max boucliers", "boucliers au maximum", "energie aux boucliers" } },
        { "de", new[] { "energie schilde", "max schilde", "volle schilde", "energie auf schilde" } },
        { "es", new[] { "energia a los escudos", "escudos al maximo", "maximo escudos" } },
        { "pt", new[] { "energia para escudos", "escudos no maximo", "maximo escudos" } },
        { "ja", new[] { "シールド電力", "シールドマックス" } },
        { "zh", new[] { "护盾分配", "护盾最大", "最大护盾" } }
    };

    private static readonly Dictionary<string, string[]> ShipPowerEnginesTriggers = new(StringComparer.OrdinalIgnoreCase)
    {
        { "en", new[] { "power engines", "max engines", "full engines", "power to engines", "power thrusters" } },
        { "fr", new[] { "puissance moteurs", "max moteurs", "moteurs au maximum", "energie aux moteurs" } },
        { "de", new[] { "energie motoren", "max motoren", "volle motoren", "energie auf triebwerke" } },
        { "es", new[] { "energia a los motores", "motores al maximo", "maximo motores" } },
        { "pt", new[] { "energia para motores", "motores no maximo", "maximo motores" } },
        { "ja", new[] { "エンジン電力", "エンジンマックス" } },
        { "zh", new[] { "引擎分配", "引擎最大", "最大引擎" } }
    };

    private static readonly Dictionary<string, string[]> ShipPowerResetTriggers = new(StringComparer.OrdinalIgnoreCase)
    {
        { "en", new[] { "reset power", "balance power", "equalize power", "power reset", "power balance" } },
        { "fr", new[] { "reinitialiser puissance", "equilibrer puissance", "energie equilibree" } },
        { "de", new[] { "energie zuruecksetzen", "energie ausgleichen", "energie reset" } },
        { "es", new[] { "restablecer energia", "equilibrar energia", "energia reset" } },
        { "pt", new[] { "restabelecer energia", "equilibrar energia", "energia reset" } },
        { "ja", new[] { "電力リセット", "電力バランス" } },
        { "zh", new[] { "重置分配", "平衡分配" } }
    };

    private static readonly Dictionary<string, string[]> ShipDecoyTriggers = new(StringComparer.OrdinalIgnoreCase)
    {
        { "en", new[] { "decoy", "launch decoy", "flare", "launch flare" } },
        { "fr", new[] { "leurre", "lancer leurre", "lancer un leurre" } },
        { "de", new[] { "taeuschkoerper", "taeuschkoerper abfeuern", "flare" } },
        { "es", new[] { "señuelo", "lanzar señuelo", "bengala", "lanzar bengala" } },
        { "pt", new[] { "isca", "lancar isca", "lancar chamariz" } },
        { "ja", new[] { "デコイ", "フレア", "デコイ射出", "フレア射出" } },
        { "zh", new[] { "干扰弹", "发射干扰弹", "诱饵" } }
    };

    private static readonly Dictionary<string, string[]> ShipNoiseTriggers = new(StringComparer.OrdinalIgnoreCase)
    {
        { "en", new[] { "noise", "launch noise", "chaff", "launch chaff" } },
        { "fr", new[] { "brouillage", "lancer brouillage", "paillettes", "lancer paillettes" } },
        { "de", new[] { "chaff", "chaff abfeuern", "rauschen" } },
        { "es", new[] { "ruido", "lanzar ruido", "chaff", "lanzar chaff" } },
        { "pt", new[] { "ruido", "lancar ruido", "chaff", "lancar chaff" } },
        { "ja", new[] { "ノイズ", "チャフ", "ノイズ射出", "チャフ射出" } },
        { "zh", new[] { "噪声弹", "发射噪声弹", "铝箔" } }
    };

    private static readonly Dictionary<string, string[]> ShipLightsTriggers = new(StringComparer.OrdinalIgnoreCase)
    {
        { "en", new[] { "lights", "headlights", "toggle lights", "lights on", "lights off", "ship lights" } },
        { "fr", new[] { "phares", "lumiere", "allumer phares", "eteindre phares", "lumiere vaisseau" } },
        { "de", new[] { "licht", "scheinwerfer", "licht an", "licht aus" } },
        { "es", new[] { "luces", "faros", "encender luces", "apagar luces" } },
        { "pt", new[] { "luzes", "farois", "ligar luzes", "desligar luzes" } },
        { "ja", new[] { "ライト", "ライト切り替え", "ライトオン", "ライトオフ" } },
        { "zh", new[] { "大灯", "切换大灯", "开启大灯", "关闭大灯" } }
    };

    private static readonly Dictionary<string, string[]> ShipTargetHostileTriggers = new(StringComparer.OrdinalIgnoreCase)
    {
        { "en", new[] { "target hostile", "nearest hostile", "lock hostile", "target nearest hostile" } },
        { "fr", new[] { "cible hostile", "hostile le plus proche", "verrouiller hostile" } },
        { "de", new[] { "feind anvisieren", "naechster feind", "feind aufschalten" } },
        { "es", new[] { "fijar enemigo", "enemigo mas cercano", "bloquear enemigo" } },
        { "pt", new[] { "mirar inimigo", "inimigo mais proximo", "travar inimigo" } },
        { "ja", new[] { "敵ターゲット", "最寄りの敵", "敵をロック" } },
        { "zh", new[] { "锁定敌机", "最近敌机", "锁定最近敌机" } }
    };

    private static readonly Dictionary<string, string[]> ShipCycleSubsystemsTriggers = new(StringComparer.OrdinalIgnoreCase)
    {
        { "en", new[] { "cycle subsystems", "target subsystem", "subsystem", "target engines", "target shields", "target weapons" } },
        { "fr", new[] { "cibler sous systeme", "sous systeme", "cibler moteurs", "cibler boucliers", "cibler armes" } },
        { "de", new[] { "subsysteme durchwechseln", "subsystem anvisieren", "triebwerke anvisieren", "schilde anvisieren" } },
        { "es", new[] { "ciclar subsistemas", "cibler subsistema", "apuntar motores", "apuntar escudos" } },
        { "pt", new[] { "ciclar subsistemas", "mirar subsistema", "mirar motores", "mirar escudos" } },
        { "ja", new[] { "サブシステム切り替え", "サブシステムターゲット", "エンジンターゲット" } },
        { "zh", new[] { "循环子系统", "瞄准子系统", "瞄准引擎", "瞄准护盾" } }
    };

    private static readonly Dictionary<string, string[]> ShipGimbalModeTriggers = new(StringComparer.OrdinalIgnoreCase)
    {
        { "en", new[] { "gimbal mode", "toggle gimbal", "gimbals" } },
        { "fr", new[] { "mode cardan", "basculer cardan", "gimbal" } },
        { "de", new[] { "gimbal modus", "gimbal umschalten", "gimbal" } },
        { "es", new[] { "modo cardan", "alternar cardan", "gimbal" } },
        { "pt", new[] { "modo cardan", "alternar cardan", "gimbal" } },
        { "ja", new[] { "ジンバルモード", "ジンバル切り替え" } },
        { "zh", new[] { "辅助瞄准", "切换辅助瞄准", "云台模式" } }
    };

    private static readonly Dictionary<string, string[]> ShipPinTargetTriggers = new(StringComparer.OrdinalIgnoreCase)
    {
        { "en", new[] { "pin target", "save target", "pin current target" } },
        { "fr", new[] { "epingler cible", "sauvegarder cible" } },
        { "de", new[] { "ziel anpinnen", "ziel speichern" } },
        { "es", new[] { "fijar blanco", "guardar blanco" } },
        { "pt", new[] { "fixar alvo", "salvar alvo" } },
        { "ja", new[] { "ターゲット固定", "ターゲット保存" } },
        { "zh", new[] { "钉住目标", "保存目标" } }
    };

    private static readonly Dictionary<string, string[]> ShipDecoupledModeTriggers = new(StringComparer.OrdinalIgnoreCase)
    {
        { "en", new[] { "decoupled mode", "toggle coupling", "decouple" } },
        { "fr", new[] { "mode decouple", "decouple", "basculer couplage" } },
        { "de", new[] { "entkoppelter modus", "kopplung aufheben", "entkoppeln" } },
        { "es", new[] { "modo desacoplado", "desacoplar" } },
        { "pt", new[] { "modo desacoplado", "desacoplar" } },
        { "ja", new[] { "デカップルモード", "デカップル" } },
        { "zh", new[] { "无辅助驾驶", "脱钩模式", "解耦模式" } }
    };

    private static readonly Dictionary<string, string[]> ShipGSafeToggleTriggers = new(StringComparer.OrdinalIgnoreCase)
    {
        { "en", new[] { "gsafe", "g safe", "toggle gsafe", "g force safety" } },
        { "fr", new[] { "g safe", "securite g", "basculer gsafe" } },
        { "de", new[] { "gsafe umschalten", "g safe" } },
        { "es", new[] { "seguridad g", "alternar gsafe", "gsafe" } },
        { "pt", new[] { "seguranca g", "alternar gsafe", "gsafe" } },
        { "ja", new[] { "gセーフ", "gセーフ切り替え" } },
        { "zh", new[] { "g力安全", "切换g力安全", "防晕厥安全" } }
    };

    private static readonly Dictionary<string, string[]> ShipSpeedLimiterToggleTriggers = new(StringComparer.OrdinalIgnoreCase)
    {
        { "en", new[] { "speed limiter", "toggle speed limiter", "speed limit" } },
        { "fr", new[] { "limiteur de vitesse", "basculer limiteur", "limite vitesse" } },
        { "de", new[] { "geschwindigkeitsbegrenzer", "tempobegrenzer" } },
        { "es", new[] { "limitador de velocidad", "alternar limitador" } },
        { "pt", new[] { "limitador de velocidade", "alternar limitador" } },
        { "ja", new[] { "速度制限", "スピードリミッター" } },
        { "zh", new[] { "速度限制", "切换速度限制", "限速器" } }
    };

    private static readonly Dictionary<string, string[]> ShipDecoyBurstIncreaseTriggers = new(StringComparer.OrdinalIgnoreCase)
    {
        { "en", new[] { "increase decoy burst", "more decoys", "decoy burst size" } },
        { "fr", new[] { "augmenter leurres", "plus de leurres" } },
        { "de", new[] { "taeuschkoerper anzahl erhoehen", "mehr taeuschkoerper" } },
        { "es", new[] { "aumentar señuelos", "mas señuelos" } },
        { "pt", new[] { "aumentar iscas", "mais iscas" } },
        { "ja", new[] { "デコイバースト増加", "デコイ増加" } },
        { "zh", new[] { "增加干扰弹数量", "更多干扰弹" } }
    };

    private static readonly Dictionary<string, string[]> ShipDecoyBurstResetTriggers = new(StringComparer.OrdinalIgnoreCase)
    {
        { "en", new[] { "reset decoy burst", "clear decoy burst", "reset decoys" } },
        { "fr", new[] { "reinitialiser leurres", "remettre les leurres" } },
        { "de", new[] { "taeuschkoerper zuruecksetzen" } },
        { "es", new[] { "restablecer señuelos" } },
        { "pt", new[] { "restabelecer iscas" } },
        { "ja", new[] { "デコイバーストクリア", "デコイリセット" } },
        { "zh", new[] { "重置干扰弹数量", "重置干扰弹" } }
    };

    private static readonly Dictionary<string, string[]> ShipWipeVisorTriggers = new(StringComparer.OrdinalIgnoreCase)
    {
        { "en", new[] { "wipe visor", "clean visor", "wipe helmet" } },
        { "fr", new[] { "essuyer visiere", "nettoyer visiere", "essuyer le casque" } },
        { "de", new[] { "visier wischen", "visier reinigen" } },
        { "es", new[] { "limpiar visera", "limpiar casco" } },
        { "pt", new[] { "limpar visera", "limpar capacete" } },
        { "ja", new[] { "バイザーを拭く", "バイザーをきれいにする" } },
        { "zh", new[] { "擦拭面罩", "清洁面罩", "擦面罩" } }
    };

    private static readonly Dictionary<string, string[]> ShipHailTargetTriggers = new(StringComparer.OrdinalIgnoreCase)
    {
        { "en", new[] { "hail target", "hail ship", "call target", "call locked ship" } },
        { "fr", new[] { "appeler la cible", "appeler vaisseau" } },
        { "de", new[] { "ziel rufen", "schiff rufen" } },
        { "es", new[] { "llamar objetivo", "llamar nave" } },
        { "pt", new[] { "chamar alvo", "chamar nave" } },
        { "ja", new[] { "ターゲット呼出", "通信開始" } },
        { "zh", new[] { "呼叫目标", "呼叫飞船" } }
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
    public event Action? ShipPowerToggleRequested;
    public event Action? ShipDoorsToggleRequested;
    public event Action? ShipShieldsFrontRequested;
    public event Action? ShipLandingGearToggleRequested;
    public event Action? ShipEnginesToggleRequested;
    public event Action? ShipWeaponsToggleRequested;
    public event Action? ShipShieldsToggleRequested;
    public event Action? ShipShieldsResetRequested;
    public event Action? ShipVtolToggleRequested;
    public event Action? ShipQuantumSpoolRequested;
    public event Action? ShipCruiseControlRequested;
    public event Action? ShipLandingRequestRequested;
    public event Action? ShipFlyModeRequested;
    public event Action? ShipScanModeRequested;
    public event Action? ShipMiningModeRequested;
    public event Action? ShipSalvageModeRequested;
    public event Action? ShipPowerWeaponsRequested;
    public event Action? ShipPowerShieldsRequested;
    public event Action? ShipPowerEnginesRequested;
    public event Action? ShipPowerResetRequested;
    public event Action? ShipDecoyRequested;
    public event Action? ShipNoiseRequested;
    public event Action? ShipLightsRequested;
    public event Action? ShipTargetHostileRequested;
    public event Action? ShipCycleSubsystemsRequested;
    public event Action? ShipGimbalModeRequested;
    public event Action? ShipPinTargetRequested;
    public event Action? ShipDecoupledModeRequested;
    public event Action? ShipGSafeToggleRequested;
    public event Action? ShipSpeedLimiterToggleRequested;
    public event Action? ShipDecoyBurstIncreaseRequested;
    public event Action? ShipDecoyBurstResetRequested;
    public event Action? ShipWipeVisorRequested;
    public event Action? ShipHailTargetRequested;

    public VoiceCommandResult ParseAndExecute(string text, string appLang, IEnumerable<string> availableChannels, double confidence = 0.5)
    {
        if (string.IsNullOrEmpty(text))
            return new VoiceCommandResult { Action = VoiceCommandAction.None };

        string cleanText = NormalizeText(text);
        string lang = GetLanguageKey(appLang);

        LogService.Info($"VoiceCommandService Parsing: \"{cleanText}\" (Lang: {lang}, Conf: {confidence})");

        var result = new VoiceCommandResult { RawText = text };

        // 1a. Wipe Visor (evaluate before generic Visor Toggle to avoid substring collision)
        if (MatchesTrigger(cleanText, lang, ShipWipeVisorTriggers, confidence, out double sim))
        {
            result.Action = VoiceCommandAction.ShipWipeVisor;
            result.Similarity = sim;
            ShipWipeVisorRequested?.Invoke();
            return result;
        }

        // 1. Visor Toggle
        if (MatchesTrigger(cleanText, lang, VisorTriggers, confidence, out sim))
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

        // 5. Voice Ship Controls
        if (MatchesTrigger(cleanText, lang, ShipTargetHostileTriggers, confidence, out sim))
        {
            result.Action = VoiceCommandAction.ShipTargetHostile;
            result.Similarity = sim;
            ShipTargetHostileRequested?.Invoke();
            return result;
        }
        if (MatchesTrigger(cleanText, lang, ShipCycleSubsystemsTriggers, confidence, out sim))
        {
            result.Action = VoiceCommandAction.ShipCycleSubsystems;
            result.Similarity = sim;
            ShipCycleSubsystemsRequested?.Invoke();
            return result;
        }
        if (MatchesTrigger(cleanText, lang, ShipGimbalModeTriggers, confidence, out sim))
        {
            result.Action = VoiceCommandAction.ShipGimbalMode;
            result.Similarity = sim;
            ShipGimbalModeRequested?.Invoke();
            return result;
        }
        if (MatchesTrigger(cleanText, lang, ShipPinTargetTriggers, confidence, out sim))
        {
            result.Action = VoiceCommandAction.ShipPinTarget;
            result.Similarity = sim;
            ShipPinTargetRequested?.Invoke();
            return result;
        }
        if (MatchesTrigger(cleanText, lang, ShipDecoupledModeTriggers, confidence, out sim))
        {
            result.Action = VoiceCommandAction.ShipDecoupledMode;
            result.Similarity = sim;
            ShipDecoupledModeRequested?.Invoke();
            return result;
        }
        if (MatchesTrigger(cleanText, lang, ShipGSafeToggleTriggers, confidence, out sim))
        {
            result.Action = VoiceCommandAction.ShipGSafeToggle;
            result.Similarity = sim;
            ShipGSafeToggleRequested?.Invoke();
            return result;
        }
        if (MatchesTrigger(cleanText, lang, ShipSpeedLimiterToggleTriggers, confidence, out sim))
        {
            result.Action = VoiceCommandAction.ShipSpeedLimiterToggle;
            result.Similarity = sim;
            ShipSpeedLimiterToggleRequested?.Invoke();
            return result;
        }
        if (MatchesTrigger(cleanText, lang, ShipDecoyBurstIncreaseTriggers, confidence, out sim))
        {
            result.Action = VoiceCommandAction.ShipDecoyBurstIncrease;
            result.Similarity = sim;
            ShipDecoyBurstIncreaseRequested?.Invoke();
            return result;
        }
        if (MatchesTrigger(cleanText, lang, ShipDecoyBurstResetTriggers, confidence, out sim))
        {
            result.Action = VoiceCommandAction.ShipDecoyBurstReset;
            result.Similarity = sim;
            ShipDecoyBurstResetRequested?.Invoke();
            return result;
        }
        if (MatchesTrigger(cleanText, lang, ShipHailTargetTriggers, confidence, out sim))
        {
            result.Action = VoiceCommandAction.ShipHailTarget;
            result.Similarity = sim;
            ShipHailTargetRequested?.Invoke();
            return result;
        }

        if (MatchesTrigger(cleanText, lang, ShipFlyModeTriggers, confidence, out sim))
        {
            result.Action = VoiceCommandAction.ShipFlyMode;
            result.Similarity = sim;
            ShipFlyModeRequested?.Invoke();
            return result;
        }
        if (MatchesTrigger(cleanText, lang, ShipScanModeTriggers, confidence, out sim))
        {
            result.Action = VoiceCommandAction.ShipScanMode;
            result.Similarity = sim;
            ShipScanModeRequested?.Invoke();
            return result;
        }
        if (MatchesTrigger(cleanText, lang, ShipMiningModeTriggers, confidence, out sim))
        {
            result.Action = VoiceCommandAction.ShipMiningMode;
            result.Similarity = sim;
            ShipMiningModeRequested?.Invoke();
            return result;
        }
        if (MatchesTrigger(cleanText, lang, ShipSalvageModeTriggers, confidence, out sim))
        {
            result.Action = VoiceCommandAction.ShipSalvageMode;
            result.Similarity = sim;
            ShipSalvageModeRequested?.Invoke();
            return result;
        }
        if (MatchesTrigger(cleanText, lang, ShipPowerWeaponsTriggers, confidence, out sim))
        {
            result.Action = VoiceCommandAction.ShipPowerWeapons;
            result.Similarity = sim;
            ShipPowerWeaponsRequested?.Invoke();
            return result;
        }
        if (MatchesTrigger(cleanText, lang, ShipPowerShieldsTriggers, confidence, out sim))
        {
            result.Action = VoiceCommandAction.ShipPowerShields;
            result.Similarity = sim;
            ShipPowerShieldsRequested?.Invoke();
            return result;
        }
        if (MatchesTrigger(cleanText, lang, ShipPowerEnginesTriggers, confidence, out sim))
        {
            result.Action = VoiceCommandAction.ShipPowerEngines;
            result.Similarity = sim;
            ShipPowerEnginesRequested?.Invoke();
            return result;
        }
        if (MatchesTrigger(cleanText, lang, ShipPowerResetTriggers, confidence, out sim))
        {
            result.Action = VoiceCommandAction.ShipPowerReset;
            result.Similarity = sim;
            ShipPowerResetRequested?.Invoke();
            return result;
        }
        if (MatchesTrigger(cleanText, lang, ShipDecoyTriggers, confidence, out sim))
        {
            result.Action = VoiceCommandAction.ShipDecoy;
            result.Similarity = sim;
            ShipDecoyRequested?.Invoke();
            return result;
        }
        if (MatchesTrigger(cleanText, lang, ShipNoiseTriggers, confidence, out sim))
        {
            result.Action = VoiceCommandAction.ShipNoise;
            result.Similarity = sim;
            ShipNoiseRequested?.Invoke();
            return result;
        }
        if (MatchesTrigger(cleanText, lang, ShipLightsTriggers, confidence, out sim))
        {
            result.Action = VoiceCommandAction.ShipLights;
            result.Similarity = sim;
            ShipLightsRequested?.Invoke();
            return result;
        }

        if (MatchesTrigger(cleanText, lang, ShipPowerTriggers, confidence, out sim))
        {
            result.Action = VoiceCommandAction.ShipPowerToggle;
            result.Similarity = sim;
            ShipPowerToggleRequested?.Invoke();
            return result;
        }
        if (MatchesTrigger(cleanText, lang, ShipDoorsTriggers, confidence, out sim))
        {
            result.Action = VoiceCommandAction.ShipDoorsToggle;
            result.Similarity = sim;
            ShipDoorsToggleRequested?.Invoke();
            return result;
        }
        if (MatchesTrigger(cleanText, lang, ShipShieldsTriggers, confidence, out sim))
        {
            result.Action = VoiceCommandAction.ShipShieldsFront;
            result.Similarity = sim;
            ShipShieldsFrontRequested?.Invoke();
            return result;
        }
        if (MatchesTrigger(cleanText, lang, ShipLandingGearTriggers, confidence, out sim))
        {
            result.Action = VoiceCommandAction.ShipLandingGearToggle;
            result.Similarity = sim;
            ShipLandingGearToggleRequested?.Invoke();
            return result;
        }
        if (MatchesTrigger(cleanText, lang, ShipEnginesTriggers, confidence, out sim))
        {
            result.Action = VoiceCommandAction.ShipEnginesToggle;
            result.Similarity = sim;
            ShipEnginesToggleRequested?.Invoke();
            return result;
        }
        if (MatchesTrigger(cleanText, lang, ShipWeaponsTriggers, confidence, out sim))
        {
            result.Action = VoiceCommandAction.ShipWeaponsToggle;
            result.Similarity = sim;
            ShipWeaponsToggleRequested?.Invoke();
            return result;
        }
        if (MatchesTrigger(cleanText, lang, ShipShieldsToggleTriggers, confidence, out sim))
        {
            result.Action = VoiceCommandAction.ShipShieldsToggle;
            result.Similarity = sim;
            ShipShieldsToggleRequested?.Invoke();
            return result;
        }
        if (MatchesTrigger(cleanText, lang, ShipShieldsResetTriggers, confidence, out sim))
        {
            result.Action = VoiceCommandAction.ShipShieldsReset;
            result.Similarity = sim;
            ShipShieldsResetRequested?.Invoke();
            return result;
        }
        if (MatchesTrigger(cleanText, lang, ShipVtolTriggers, confidence, out sim))
        {
            result.Action = VoiceCommandAction.ShipVtolToggle;
            result.Similarity = sim;
            ShipVtolToggleRequested?.Invoke();
            return result;
        }
        if (MatchesTrigger(cleanText, lang, ShipQuantumSpoolTriggers, confidence, out sim))
        {
            result.Action = VoiceCommandAction.ShipQuantumSpool;
            result.Similarity = sim;
            ShipQuantumSpoolRequested?.Invoke();
            return result;
        }
        if (MatchesTrigger(cleanText, lang, ShipCruiseControlTriggers, confidence, out sim))
        {
            result.Action = VoiceCommandAction.ShipCruiseControl;
            result.Similarity = sim;
            ShipCruiseControlRequested?.Invoke();
            return result;
        }
        if (MatchesTrigger(cleanText, lang, ShipLandingRequestTriggers, confidence, out sim))
        {
            result.Action = VoiceCommandAction.ShipLandingRequest;
            result.Similarity = sim;
            ShipLandingRequestRequested?.Invoke();
            return result;
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
