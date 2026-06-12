# Programmatic Stream Deck profile generation script for XuruVOIP
# Generates 15 profiles: 5 devices (mini, classic, xl, plus, plus_xl) x 3 layouts (pilot, infantry, captain)

$rootDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$profilesDir = Join-Path $rootDir "profiles"

# Ensure output dir exists
if (-not (Test-Path $profilesDir)) {
    New-Item -ItemType Directory -Path $profilesDir -Force | Out-Null
}

$port = 8891

# Helper to write manifest
function Write-ProfileManifest($device, $layout, $name, $deviceType, $keys) {
    $layoutDir = Join-Path $profilesDir "$device"
    $profileDir = Join-Path $layoutDir "$layout.sdProfile"
    
    if (-not (Test-Path $profileDir)) {
        New-Item -ItemType Directory -Path $profileDir -Force | Out-Null
    }
    
    # Structure of Rooms.0.Keys
    $roomKeys = @{}
    foreach ($keyId in $keys.Keys) {
        $idx = [int]$keyId
        $coord = ""
        
        if ($device -eq "mini") {
            $col = $idx % 3
            $row = [math]::Floor($idx / 3)
            $coord = "$col,$row"
        }
        elseif ($device -eq "classic") {
            $col = $idx % 5
            $row = [math]::Floor($idx / 5)
            $coord = "$col,$row"
        }
        elseif ($device -eq "xl") {
            $col = $idx % 9
            $row = [math]::Floor($idx / 9)
            $coord = "$col,$row"
        }
        elseif ($device -eq "plus") {
            if ($idx -lt 8) {
                $col = $idx % 4
                $row = [math]::Floor($idx / 4)
                $coord = "$col,$row"
            } else {
                $col = ($idx - 8) % 4
                $row = 2
                $coord = "$col,$row"
            }
        }
        elseif ($device -eq "plus_xl") {
            if ($idx -lt 36) {
                $col = $idx % 9
                $row = [math]::Floor($idx / 9)
                $coord = "$col,$row"
            } else {
                $col = ($idx - 36) % 6
                $row = 4
                $coord = "$col,$row"
            }
        }
        
        $btn = $keys[$keyId]
        $roomKeys[$coord] = @{
            "Action" = $btn.UUID
            "Settings" = $btn.Settings
        }
    }
    
    $manifest = @{
        "Name" = $name
        "Version" = "1.0.0"
        "DeviceModel" = "XuruVOIP_$device"
        "DeviceType" = $deviceType
        "Rooms" = @{
            "0" = @{
                "Keys" = $roomKeys
            }
        }
    }
    
    $json = ConvertTo-Json -InputObject $manifest -Depth 10
    $manifestPath = Join-Path $profileDir "manifest.json"
    $json | Out-File -FilePath $manifestPath -Encoding utf8 -Force
    Write-Host "Generated manifest for $name -> $manifestPath"
}

# --- 1. STREAM DECK MINI (DeviceType: 1) ---
$miniPilot = @{
    "0" = @{ UUID = "com.xurudragon.xuruvoip.action.proximity-mute"; Settings = @{ port = $port } }
    "1" = @{ UUID = "com.xurudragon.xuruvoip.action.radio-mute"; Settings = @{ port = $port } }
    "2" = @{ UUID = "com.xurudragon.xuruvoip.action.toggle-helmet"; Settings = @{ port = $port } }
    "3" = @{ UUID = "com.xurudragon.xuruvoip.action.pa-broadcast"; Settings = @{ port = $port } }
    "4" = @{ UUID = "com.xurudragon.xuruvoip.action.intercom-status"; Settings = @{ port = $port } }
    "5" = @{ UUID = "com.xurudragon.xuruvoip.action.location-telemetry"; Settings = @{ port = $port } }
}
$miniInfantry = @{
    "0" = @{ UUID = "com.xurudragon.xuruvoip.action.proximity-mute"; Settings = @{ port = $port } }
    "1" = @{ UUID = "com.xurudragon.xuruvoip.action.radio-mute"; Settings = @{ port = $port } }
    "2" = @{ UUID = "com.xurudragon.xuruvoip.action.toggle-helmet"; Settings = @{ port = $port } }
    "3" = @{ UUID = "com.xurudragon.xuruvoip.action.cycle-radio"; Settings = @{ port = $port } }
    "4" = @{ UUID = "com.xurudragon.xuruvoip.action.voice-command"; Settings = @{ port = $port; command = "status report" } }
    "5" = @{ UUID = "com.xurudragon.xuruvoip.action.location-telemetry"; Settings = @{ port = $port } }
}
$miniCaptain = @{
    "0" = @{ UUID = "com.xurudragon.xuruvoip.action.proximity-mute"; Settings = @{ port = $port } }
    "1" = @{ UUID = "com.xurudragon.xuruvoip.action.radio-mute"; Settings = @{ port = $port } }
    "2" = @{ UUID = "com.xurudragon.xuruvoip.action.pa-broadcast"; Settings = @{ port = $port } }
    "3" = @{ UUID = "com.xurudragon.xuruvoip.action.intercom-status"; Settings = @{ port = $port } }
    "4" = @{ UUID = "com.xurudragon.xuruvoip.action.cycle-radio"; Settings = @{ port = $port } }
    "5" = @{ UUID = "com.xurudragon.xuruvoip.action.location-telemetry"; Settings = @{ port = $port } }
}

Write-ProfileManifest "mini" "pilot" "XuruVOIP Pilot - Mini" 1 $miniPilot
Write-ProfileManifest "mini" "infantry" "XuruVOIP Infantry - Mini" 1 $miniInfantry
Write-ProfileManifest "mini" "captain" "XuruVOIP Captain - Mini" 1 $miniCaptain

# --- 2. STREAM DECK CLASSIC (DeviceType: 0) ---
$classicPilot = @{
    "0" = @{ UUID = "com.xurudragon.xuruvoip.action.proximity-mute"; Settings = @{ port = $port } }
    "1" = @{ UUID = "com.xurudragon.xuruvoip.action.radio-mute"; Settings = @{ port = $port } }
    "2" = @{ UUID = "com.xurudragon.xuruvoip.action.profile-mute"; Settings = @{ port = $port } }
    "3" = @{ UUID = "com.xurudragon.xuruvoip.action.cycle-radio"; Settings = @{ port = $port } }
    "4" = @{ UUID = "com.xurudragon.xuruvoip.action.toggle-helmet"; Settings = @{ port = $port } }
    "5" = @{ UUID = "com.xurudragon.xuruvoip.action.pa-broadcast"; Settings = @{ port = $port } }
    "6" = @{ UUID = "com.xurudragon.xuruvoip.action.toggle-translation"; Settings = @{ port = $port } }
    "7" = @{ UUID = "com.xurudragon.xuruvoip.action.intercom-status"; Settings = @{ port = $port } }
    "8" = @{ UUID = "com.xurudragon.xuruvoip.action.hail-initiate"; Settings = @{ port = $port } }
    "9" = @{ UUID = "com.xurudragon.xuruvoip.action.hail-accept"; Settings = @{ port = $port } }
    "10" = @{ UUID = "com.xurudragon.xuruvoip.action.location-telemetry"; Settings = @{ port = $port } }
    "11" = @{ UUID = "com.xurudragon.xuruvoip.action.audio-proximity-mute"; Settings = @{ port = $port } }
    "12" = @{ UUID = "com.xurudragon.xuruvoip.action.audio-radio-mute"; Settings = @{ port = $port } }
    "13" = @{ UUID = "com.xurudragon.xuruvoip.action.audio-profile-mute"; Settings = @{ port = $port } }
    "14" = @{ UUID = "com.xurudragon.xuruvoip.action.hail-decline"; Settings = @{ port = $port } }
}
$classicInfantry = @{
    "0" = @{ UUID = "com.xurudragon.xuruvoip.action.proximity-mute"; Settings = @{ port = $port } }
    "1" = @{ UUID = "com.xurudragon.xuruvoip.action.radio-mute"; Settings = @{ port = $port } }
    "2" = @{ UUID = "com.xurudragon.xuruvoip.action.toggle-helmet"; Settings = @{ port = $port } }
    "3" = @{ UUID = "com.xurudragon.xuruvoip.action.cycle-radio"; Settings = @{ port = $port } }
    "4" = @{ UUID = "com.xurudragon.xuruvoip.action.voice-command"; Settings = @{ port = $port; command = "status report" } }
    "5" = @{ UUID = "com.xurudragon.xuruvoip.action.audio-proximity-mute"; Settings = @{ port = $port } }
    "6" = @{ UUID = "com.xurudragon.xuruvoip.action.audio-radio-mute"; Settings = @{ port = $port } }
    "7" = @{ UUID = "com.xurudragon.xuruvoip.action.beacon-repeater"; Settings = @{ port = $port } }
    "8" = @{ UUID = "com.xurudragon.xuruvoip.action.voice-command"; Settings = @{ port = $port; command = "mute proximity" } }
    "9" = @{ UUID = "com.xurudragon.xuruvoip.action.voice-command"; Settings = @{ port = $port; command = "unmute proximity" } }
    "10" = @{ UUID = "com.xurudragon.xuruvoip.action.location-telemetry"; Settings = @{ port = $port } }
    "11" = @{ UUID = "com.xurudragon.xuruvoip.action.voice-command"; Settings = @{ port = $port; command = "set channel to alpha" } }
    "12" = @{ UUID = "com.xurudragon.xuruvoip.action.voice-command"; Settings = @{ port = $port; command = "set channel to beta" } }
    "13" = @{ UUID = "com.xurudragon.xuruvoip.action.voice-command"; Settings = @{ port = $port; command = "toggle voice changer" } }
    "14" = @{ UUID = "com.xurudragon.xuruvoip.action.voice-command"; Settings = @{ port = $port; command = "change voice to cyborg" } }
}
$classicCaptain = @{
    "0" = @{ UUID = "com.xurudragon.xuruvoip.action.proximity-mute"; Settings = @{ port = $port } }
    "1" = @{ UUID = "com.xurudragon.xuruvoip.action.radio-mute"; Settings = @{ port = $port } }
    "2" = @{ UUID = "com.xurudragon.xuruvoip.action.pa-broadcast"; Settings = @{ port = $port } }
    "3" = @{ UUID = "com.xurudragon.xuruvoip.action.intercom-status"; Settings = @{ port = $port } }
    "4" = @{ UUID = "com.xurudragon.xuruvoip.action.toggle-helmet"; Settings = @{ port = $port } }
    "5" = @{ UUID = "com.xurudragon.xuruvoip.action.audio-proximity-mute"; Settings = @{ port = $port } }
    "6" = @{ UUID = "com.xurudragon.xuruvoip.action.toggle-translation"; Settings = @{ port = $port } }
    "7" = @{ UUID = "com.xurudragon.xuruvoip.action.hail-decline"; Settings = @{ port = $port } }
    "8" = @{ UUID = "com.xurudragon.xuruvoip.action.hail-initiate"; Settings = @{ port = $port } }
    "9" = @{ UUID = "com.xurudragon.xuruvoip.action.hail-accept"; Settings = @{ port = $port } }
    "10" = @{ UUID = "com.xurudragon.xuruvoip.action.location-telemetry"; Settings = @{ port = $port } }
    "11" = @{ UUID = "com.xurudragon.xuruvoip.action.cycle-radio"; Settings = @{ port = $port } }
    "12" = @{ UUID = "com.xurudragon.xuruvoip.action.voice-command"; Settings = @{ port = $port; command = "simulate shield hit" } }
    "13" = @{ UUID = "com.xurudragon.xuruvoip.action.voice-command"; Settings = @{ port = $port; command = "simulate power loss" } }
    "14" = @{ UUID = "com.xurudragon.xuruvoip.action.voice-command"; Settings = @{ port = $port; command = "simulate quantum spool" } }
}

Write-ProfileManifest "classic" "pilot" "XuruVOIP Pilot - Classic" 0 $classicPilot
Write-ProfileManifest "classic" "infantry" "XuruVOIP Infantry - Classic" 0 $classicInfantry
Write-ProfileManifest "classic" "captain" "XuruVOIP Captain - Classic" 0 $classicCaptain

# --- 3. STREAM DECK XL (DeviceType: 2) ---
# Grid size: 36 keys (9 columns x 4 rows)
function Get-XLBase($rolePilot, $roleInfantry) {
    $keys = @{}
    # Map top keys
    $keys["0"] = @{ UUID = "com.xurudragon.xuruvoip.action.proximity-mute"; Settings = @{ port = $port } }
    $keys["1"] = @{ UUID = "com.xurudragon.xuruvoip.action.radio-mute"; Settings = @{ port = $port } }
    $keys["2"] = @{ UUID = "com.xurudragon.xuruvoip.action.profile-mute"; Settings = @{ port = $port } }
    $keys["3"] = @{ UUID = "com.xurudragon.xuruvoip.action.toggle-helmet"; Settings = @{ port = $port } }
    $keys["4"] = @{ UUID = "com.xurudragon.xuruvoip.action.cycle-radio"; Settings = @{ port = $port } }
    $keys["5"] = @{ UUID = "com.xurudragon.xuruvoip.action.pa-broadcast"; Settings = @{ port = $port } }
    $keys["6"] = @{ UUID = "com.xurudragon.xuruvoip.action.beacon-repeater"; Settings = @{ port = $port } }
    $keys["7"] = @{ UUID = "com.xurudragon.xuruvoip.action.intercom-status"; Settings = @{ port = $port } }
    $keys["8"] = @{ UUID = "com.xurudragon.xuruvoip.action.location-telemetry"; Settings = @{ port = $port } }
    
    # Row 2: Mutes and audios
    $keys["9"] = @{ UUID = "com.xurudragon.xuruvoip.action.audio-proximity-mute"; Settings = @{ port = $port } }
    $keys["10"] = @{ UUID = "com.xurudragon.xuruvoip.action.audio-radio-mute"; Settings = @{ port = $port } }
    $keys["11"] = @{ UUID = "com.xurudragon.xuruvoip.action.audio-profile-mute"; Settings = @{ port = $port } }
    
    # Hailing and Translation Subtitles
    $keys["12"] = @{ UUID = "com.xurudragon.xuruvoip.action.hail-initiate"; Settings = @{ port = $port } }
    $keys["13"] = @{ UUID = "com.xurudragon.xuruvoip.action.hail-accept"; Settings = @{ port = $port } }
    $keys["14"] = @{ UUID = "com.xurudragon.xuruvoip.action.hail-decline"; Settings = @{ port = $port } }
    $keys["15"] = @{ UUID = "com.xurudragon.xuruvoip.action.toggle-translation"; Settings = @{ port = $port } }
    $keys["16"] = @{ UUID = "com.xurudragon.xuruvoip.action.toggle-hrtf"; Settings = @{ port = $port } }
    $keys["17"] = @{ UUID = "com.xurudragon.xuruvoip.action.toggle-spectrogram"; Settings = @{ port = $port } }
    $keys["25"] = @{ UUID = "com.xurudragon.xuruvoip.action.toggle-voice-commands"; Settings = @{ port = $port } }
    $keys["26"] = @{ UUID = "com.xurudragon.xuruvoip.action.cycle-theme"; Settings = @{ port = $port } }
    
    return $keys
}

# Pilot XL
$xlPilot = Get-XLBase
$xlPilot["18"] = @{ UUID = "com.xurudragon.xuruvoip.action.voice-command"; Settings = @{ port = $port; command = "open hangar" } }
$xlPilot["19"] = @{ UUID = "com.xurudragon.xuruvoip.action.voice-command"; Settings = @{ port = $port; command = "request landing" } }
$xlPilot["20"] = @{ UUID = "com.xurudragon.xuruvoip.action.voice-command"; Settings = @{ port = $port; command = "status report" } }
$xlPilot["21"] = @{ UUID = "com.xurudragon.xuruvoip.action.voice-command"; Settings = @{ port = $port; command = "close visor" } }
$xlPilot["27"] = @{ UUID = "com.xurudragon.xuruvoip.action.voice-command"; Settings = @{ port = $port; command = "power up shields" } }

# Infantry XL
$xlInfantry = Get-XLBase
$xlInfantry["18"] = @{ UUID = "com.xurudragon.xuruvoip.action.voice-command"; Settings = @{ port = $port; command = "status report" } }
$xlInfantry["19"] = @{ UUID = "com.xurudragon.xuruvoip.action.voice-command"; Settings = @{ port = $port; command = "mute proximity" } }
$xlInfantry["20"] = @{ UUID = "com.xurudragon.xuruvoip.action.voice-command"; Settings = @{ port = $port; command = "unmute proximity" } }
$xlInfantry["27"] = @{ UUID = "com.xurudragon.xuruvoip.action.voice-command"; Settings = @{ port = $port; command = "set channel to alpha" } }
$xlInfantry["28"] = @{ UUID = "com.xurudragon.xuruvoip.action.voice-command"; Settings = @{ port = $port; command = "set channel to beta" } }
$xlInfantry["29"] = @{ UUID = "com.xurudragon.xuruvoip.action.voice-command"; Settings = @{ port = $port; command = "toggle voice changer" } }
$xlInfantry["30"] = @{ UUID = "com.xurudragon.xuruvoip.action.voice-command"; Settings = @{ port = $port; command = "change voice to cyborg" } }

# Captain XL
$xlCaptain = Get-XLBase
$xlCaptain["18"] = @{ UUID = "com.xurudragon.xuruvoip.action.voice-command"; Settings = @{ port = $port; command = "power up shields" } }
$xlCaptain["19"] = @{ UUID = "com.xurudragon.xuruvoip.action.voice-command"; Settings = @{ port = $port; command = "status check" } }
$xlCaptain["27"] = @{ UUID = "com.xurudragon.xuruvoip.action.voice-command"; Settings = @{ port = $port; command = "simulate shield hit" } }
$xlCaptain["28"] = @{ UUID = "com.xurudragon.xuruvoip.action.voice-command"; Settings = @{ port = $port; command = "simulate power loss" } }
$xlCaptain["29"] = @{ UUID = "com.xurudragon.xuruvoip.action.voice-command"; Settings = @{ port = $port; command = "simulate quantum spool" } }

Write-ProfileManifest "xl" "pilot" "XuruVOIP Pilot - XL" 2 $xlPilot
Write-ProfileManifest "xl" "infantry" "XuruVOIP Infantry - XL" 2 $xlInfantry
Write-ProfileManifest "xl" "captain" "XuruVOIP Captain - XL" 2 $xlCaptain

# --- 4. STREAM DECK PLUS (DeviceType: 7) ---
# 8 keys (0-7) + 4 dials (8-11)
$plusPilot = @{
    "0" = @{ UUID = "com.xurudragon.xuruvoip.action.proximity-mute"; Settings = @{ port = $port } }
    "1" = @{ UUID = "com.xurudragon.xuruvoip.action.radio-mute"; Settings = @{ port = $port } }
    "2" = @{ UUID = "com.xurudragon.xuruvoip.action.profile-mute"; Settings = @{ port = $port } }
    "3" = @{ UUID = "com.xurudragon.xuruvoip.action.toggle-helmet"; Settings = @{ port = $port } }
    "4" = @{ UUID = "com.xurudragon.xuruvoip.action.pa-broadcast"; Settings = @{ port = $port } }
    "5" = @{ UUID = "com.xurudragon.xuruvoip.action.toggle-translation"; Settings = @{ port = $port } }
    "6" = @{ UUID = "com.xurudragon.xuruvoip.action.intercom-status"; Settings = @{ port = $port } }
    "7" = @{ UUID = "com.xurudragon.xuruvoip.action.location-telemetry"; Settings = @{ port = $port } }
    # Dials:
    "8" = @{ UUID = "com.xurudragon.xuruvoip.action.cycle-radio-dial"; Settings = @{ port = $port } }
    "9" = @{ UUID = "com.xurudragon.xuruvoip.action.adjust-exertion"; Settings = @{ port = $port } }
    "10" = @{ UUID = "com.xurudragon.xuruvoip.action.voice-changer-dial"; Settings = @{ port = $port } }
    "11" = @{ UUID = "com.xurudragon.xuruvoip.action.theme-dial"; Settings = @{ port = $port } }
}
$plusInfantry = @{
    "0" = @{ UUID = "com.xurudragon.xuruvoip.action.proximity-mute"; Settings = @{ port = $port } }
    "1" = @{ UUID = "com.xurudragon.xuruvoip.action.radio-mute"; Settings = @{ port = $port } }
    "2" = @{ UUID = "com.xurudragon.xuruvoip.action.toggle-helmet"; Settings = @{ port = $port } }
    "3" = @{ UUID = "com.xurudragon.xuruvoip.action.location-telemetry"; Settings = @{ port = $port } }
    "4" = @{ UUID = "com.xurudragon.xuruvoip.action.audio-proximity-mute"; Settings = @{ port = $port } }
    "5" = @{ UUID = "com.xurudragon.xuruvoip.action.audio-radio-mute"; Settings = @{ port = $port } }
    "6" = @{ UUID = "com.xurudragon.xuruvoip.action.pa-broadcast"; Settings = @{ port = $port } }
    "7" = @{ UUID = "com.xurudragon.xuruvoip.action.voice-command"; Settings = @{ port = $port; command = "status report" } }
    # Dials:
    "8" = @{ UUID = "com.xurudragon.xuruvoip.action.cycle-radio-dial"; Settings = @{ port = $port } }
    "9" = @{ UUID = "com.xurudragon.xuruvoip.action.voice-changer-dial"; Settings = @{ port = $port } }
    "10" = @{ UUID = "com.xurudragon.xuruvoip.action.adjust-exertion"; Settings = @{ port = $port } }
    "11" = @{ UUID = "com.xurudragon.xuruvoip.action.theme-dial"; Settings = @{ port = $port } }
}
$plusCaptain = @{
    "0" = @{ UUID = "com.xurudragon.xuruvoip.action.proximity-mute"; Settings = @{ port = $port } }
    "1" = @{ UUID = "com.xurudragon.xuruvoip.action.radio-mute"; Settings = @{ port = $port } }
    "2" = @{ UUID = "com.xurudragon.xuruvoip.action.pa-broadcast"; Settings = @{ port = $port } }
    "3" = @{ UUID = "com.xurudragon.xuruvoip.action.intercom-status"; Settings = @{ port = $port } }
    "4" = @{ UUID = "com.xurudragon.xuruvoip.action.toggle-helmet"; Settings = @{ port = $port } }
    "5" = @{ UUID = "com.xurudragon.xuruvoip.action.location-telemetry"; Settings = @{ port = $port } }
    "6" = @{ UUID = "com.xurudragon.xuruvoip.action.audio-proximity-mute"; Settings = @{ port = $port } }
    "7" = @{ UUID = "com.xurudragon.xuruvoip.action.toggle-translation"; Settings = @{ port = $port } }
    # Dials:
    "8" = @{ UUID = "com.xurudragon.xuruvoip.action.cycle-radio-dial"; Settings = @{ port = $port } }
    "9" = @{ UUID = "com.xurudragon.xuruvoip.action.adjust-exertion"; Settings = @{ port = $port } }
    "10" = @{ UUID = "com.xurudragon.xuruvoip.action.voice-changer-dial"; Settings = @{ port = $port } }
    "11" = @{ UUID = "com.xurudragon.xuruvoip.action.theme-dial"; Settings = @{ port = $port } }
}

Write-ProfileManifest "plus" "pilot" "XuruVOIP Pilot - Plus" 7 $plusPilot
Write-ProfileManifest "plus" "infantry" "XuruVOIP Infantry - Plus" 7 $plusInfantry
Write-ProfileManifest "plus" "captain" "XuruVOIP Captain - Plus" 7 $plusCaptain

# --- 5. STREAM DECK PLUS XL (DeviceType: Custom/Hybrid) ---
# 36 keys (0-35) + 6 dials (36-41)
function Get-PlusXLBase($keysMap) {
    # Copy all keys from Standard XL
    $keys = @{}
    foreach ($keyId in $keysMap.Keys) {
        $keys[$keyId] = $keysMap[$keyId]
    }
    # Add dials:
    $keys["36"] = @{ UUID = "com.xurudragon.xuruvoip.action.cycle-radio-dial"; Settings = @{ port = $port } }
    $keys["37"] = @{ UUID = "com.xurudragon.xuruvoip.action.adjust-exertion"; Settings = @{ port = $port } }
    $keys["38"] = @{ UUID = "com.xurudragon.xuruvoip.action.voice-changer-dial"; Settings = @{ port = $port } }
    $keys["39"] = @{ UUID = "com.xurudragon.xuruvoip.action.theme-dial"; Settings = @{ port = $port } }
    return $keys
}

Write-ProfileManifest "plus_xl" "pilot" "XuruVOIP Pilot - Plus XL" 2 (Get-PlusXLBase $xlPilot)
Write-ProfileManifest "plus_xl" "infantry" "XuruVOIP Infantry - Plus XL" 2 (Get-PlusXLBase $xlInfantry)
Write-ProfileManifest "plus_xl" "captain" "XuruVOIP Captain - Plus XL" 2 (Get-PlusXLBase $xlCaptain)

Write-Host "All Stream Deck profiles successfully generated!"
