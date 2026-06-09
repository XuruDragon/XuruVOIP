# XuruVoip (Deutsch)

<p align="center">
  <a href="https://github.com/XuruDragon/XuruVOIP/actions/workflows/tests.yml">
    <img src="https://github.com/XuruDragon/XuruVOIP/actions/workflows/tests.yml/badge.svg" alt="Teststatus" />
  </a>
  <a href="https://github.com/XuruDragon/XuruVOIP/releases">
    <img src="https://img.shields.io/github/v/release/XuruDragon/XuruVOIP?color=blue&logo=github" alt="Neuestes Release" />
  </a>
  <a href="https://github.com/XuruDragon/XuruVOIP/releases">
    <img src="https://img.shields.io/github/downloads/XuruDragon/XuruVOIP/total?color=green&logo=github" alt="Downloads Gesamt" />
  </a>
</p>

<p align="center">
  <b>Übersetzungen:</b><br/>
  <a href="../README.md">English</a> •
  <a href="README.fr.md">Français</a> •
  <a href="README.de.md">Deutsch</a> •
  <a href="README.es.md">Español</a> •
  <a href="README.pt-BR.md">Português (Brasil)</a> •
  <a href="README.pt-PT.md">Português (Portugal)</a> •
  <a href="README.ja.md">日本語</a> •
  <a href="README.zh.md">简体中文</a>
</p>

<p align="center">
  <img src="../logo.png" alt="XuruVoip Logo" width="400" height="400" />
</p>

XuruVoip ist eine hochleistungsfähige, sichere und dynamisch spatialisierte **3D-Sprachkommunikations-Suite (VoIP)**, die speziell für die benutzerdefinierte Spieleintegration mit **Star Citizen** entwickelt wurde. Sie besteht aus einem Go-basierten Backend-Server und einem modernen C# WPF-Client.

---

## 📸 Screenshots & Benutzeroberfläche

### 1. Client-Hauptfenster
![Client-Hauptfenster](/screenshots/main.png)

### 2. Audio-Einstellungen (3D-Raumklang-Steuerung)
![Audio-Einstellungen](/screenshots/audio.png)

### 3. Allgemeine Einstellungen (Sprache & Game.log Pfad)
![Allgemeine Einstellungen](/screenshots/general.png)

### 4. Verbindungs-Einstellungen
![Verbindungs-Einstellungen](/screenshots/connection.png)

### 5. Hotkeys-Einstellungen
![Hotkeys-Einstellungen](/screenshots/hotkeys.png)

### 6. Admin-Webportal Login-Seite
![Admin-Webportal Login-Seite](/screenshots/admin_login.png)

### 7. Admin-Webportal Dashboard
![Admin-Webportal Dashboard](/screenshots/admin_dashboard.png)

### 8. Admin-Webportal Spielerliste
![Admin-Webportal Spielerliste](/screenshots/admin_players_list.png)

### 9. Admin-Webportal Administrator-Liste
![Admin-Webportal Administrator-Liste](/screenshots/admin_admin_list.png)

### 10. Admin-Webportal Sperrliste
![Admin-Webportal Sperrliste](/screenshots/admin_ban_list.png)

---

## 🗂️ Projektstruktur

- **/server**: Hochleistungs-Go-Backend, das die Positionierungs-, Audio- und Administrationsdienste hostet.
- **/client**: Moderner C# WPF-Client, der NAudio, WebRtcVad und Tesseract OCR für die automatische Standortverfolgung und Log-Dateianalyse verwendet.

---

## ⚙️ Funktionsweise der Anwendung (Client-Architektur)

Der C# WPF-Client läuft parallel zu Star Citizen und führt Audioerfassung, Sprachaktivierungserkennung, Texterkennung von Koordinaten sowie Audio-Wiedergabe in Echtzeit aus. Unten ist das Ablaufdiagramm des Clients dargestellt:

```mermaid
graph TD
    subgraph Audioerfassung & Übertragung
        Mic[Mikrofoneingang] -->|PCM-Audio| VAD[WebRTC Sprachaktivierung]
        VAD -->|Aktive Sprache| OpusEnc[Opus-Codierer]
        OpusEnc -->|Opus-Pakete| AudioWS[Audio-WebSocket-Client]
        AudioWS -->|WebSocket-Port 8889| Server[Go-Server]
    end

    subgraph Positionsbestimmung & Helmerkennung
        SC[Star Citizen-Prozess] -->|r_DisplaySessionInfo| Screen[Bildschirmaufnahme]
        Screen -->|Vorverarbeitung| Tess[Tesseract OCR-Engine]
        
        SC -->|Echtzeit-Logfile| GameLog[Game.log-Datei]
        GameLog -->|Logfile-Reader| LogParser[Log-Service-Parser]
        
        Tess -->|Koordinaten| PosSelector{Positionsquellen-Wahl}
        LogParser -->|Koordinaten| PosSelector
        
        PosSelector -->|Gewählte Koordinaten| Zone[Hierarchischer Zonenfilter]
        Zone -->|Empfängerkoordinaten & Zone| PosWS[Position-WebSocket-Client]
        PosWS -->|WebSocket-Port 8888| Server

        LogParser -->|Auf-/Absetz-Ereignisse| Helmet[Helmmodus-Synchronisation]
        Helmet -->|Helmstatus-Paket| PosWS
    end

    subgraph Stereo-3D-Raumklang-Mischung & DSP
        Server -->|Proximity-Audio + Metadaten| AudioWS
        AudioWS -->|Opus-Frame + ProximityMetadata| Decoder[Opus-Decodierer]
        Decoder -->|Mono-Float-PCM| DSP[Radio-DSP-Filter & Signalverschlechterung]
        DSP -->|Mono| Panner[PanningSampleProvider]
        Panner -->|Stereo| Volume[VolumeSampleProvider]
        
        LogParser -.->|Lokaler Helmstatus| DSP
        Zone -.->|Hörerposition & Blickrichtung| MixerMath[Raumklang- & Verschlechterungsmathematik]
        
        MixerMath -->|Pan-Parameter| Panner
        MixerMath -->|Entfernungs- & Rückdämpfung| Volume
        MixerMath -->|Verschlechterungsfaktor| DSP
        
        Volume -->|Links/Rechts Stereo| Mixer[MixingSampleProvider]
        Mixer -->|Audiowiedergabe| Speakers[Audiowiedergabegerät]
    end
```

### 1. Audioerfassung, VAD und Komprimierung
* **Audioerfassung:** Der Client erfasst Mikrofon-Audio über die **NAudio**-API mit 48.000 Hz, 16-Bit Mono.
* **Sprachaktivierungserkennung (VAD):** Audiodaten werden mittels des nativen **WebRtcVad** bewertet. Sinkt die Sprachkonfidenz unter den Schwellenwert, stoppt die Übertragung. So werden Tastaturgeräusche oder Lüfterrauschen ausgefiltert.
* **Komprimierung:** Aktive Audiodaten werden in hochkomprimierte **Opus**-Frames codiert (über **Concentus** C#) und direkt als binäre WebSocket-Frames an den Server gesendet.

### 2. Positionsverfolgung und Richtungsbestimmung
* **Positionsquellen-Umschalter:** Spieler können in den Client-Einstellungen zwischen zwei Methoden wählen:
  * **OCR-Bildschirmscanner:** Erstellt regelmäßig ein Foto des konfigurierten Bildschirmbereichs (auf dem die Koordinaten per `/showlocations` oder `r_DisplaySessionInfo` angezeigt werden), verarbeitet das Bild vor und leitet es an die **Tesseract OCR**-Engine weiter.
  * **Game.log-Leser (GRTPR):** Scannt die Star Citizen `Game.log`-Datei direkt nach vom Spiel ausgegebenen Koordinaten. Hierfür muss `r_DisplaySessionInfo = 3` (oder `1`) in der Datei `user.cfg` eingetragen sein. Die Auswahl von GRTPR stoppt und deinitialisiert die Tesseract OCR-Engine vollständig, um wertvolle CPU- und RAM-Ressourcen des Hostsystems freizusetzen.
* **Hierarchischer Zonenfilter:** Die Koordinaten enthalten hierarchische Zonen (z.B. Planeten, Raumschiffe). Der Client filtert Zonenunterschiede (wie Aufzüge, Sitze) heraus, damit sich Spieler in angrenzenden Zonen unterbrechungsfrei hören.
* **Richtungsbestimmung:** Da Star Citizen die Blickrichtung nicht ausgibt, errechnet der Client die Bewegungsrichtung aus der Positionsänderung ($Position_{aktuell} - Position_{vorherig$). Im Stillstand bleibt der letzte Wert erhalten.

### 3. Helm-Erkennung in Echtzeit (Logfile-Scanner)
* **Tail Scanner:** Ein Hintergrundprozess liest die Star Citizen `Game.log`-Datei in Echtzeit.
* **Ausrüstungsverfolgung:** Der Scanner filtert Logs wie `<AttachmentReceived>` nach Helmkomponenten (`FP_Visor`, `helmethook_attach`).
* **Auto-Synchronisation:** Wird ein Helm im Spiel auf- oder abgesetzt, ändert sich der Helmmodus des Clients sofort und vollautomatisch.

### 4. Stereo-3D-Raumklang-Mischung & DSP
* **Empfangsschleife:** Der Client empfängt Opus-Audiopakete mit Metadaten (Entfernung, Reichweite, Koordinaten des Sprechers).
* **Raumklang-Berechnung:** Das Signal wird auf die Vektoren des Hörers projiziert:
  * **Stereo Panning (Pan):** Regelt die Links-Rechts-Balance von `-1.0` (voll links) bis `+1.0` (voll rechts).
  * **Hintergrunddämpfung:** Schallquellen von hinten werden um bis zu 25% gedämpft, um die Vorne-Hinten-Verortung im Kopfhörer zu unterstützen.
  * **Entfernungsdämpfung:** Die Lautstärke sinkt linear und erreicht bei maximaler Reichweite (Standard: 50m) null.
* **Wiedergabe & Radio-DSP:** Die decodierten Opus-Frames laufen durch einen **Radio-DSP-Filter** (falls Sprecher oder Hörer den Helm aufhaben oder auf einem Funkkanal sprechen), werden räumlich verteilt, gedämpft und gemischt.
  * **Dynamische Funkverschlechterung:** Falls aktiviert, verengt der DSP-Filter die Hoch- und Tiefpass-Grenzfrequenzen und mischt bandpassgefiltertes weißes Rauschen hinzu, wenn sich Spieler der maximalen Funkreichweite nähern, um Funksignalschwankungen zu simulieren.
  * **Authentische PTT- & Funktöne:** NAudio erzeugt Synthesizer-Töne für Sendeaktivierungen. Der Sendestart spielt einen 50ms pitch-sweep **Mic Key Chirp** (900Hz bis 700Hz). Das Sendeende löst ein 180ms **Squelch-Rauschen** (Squelch Tail) aus, sobald ein leeres 0-Byte-Opus-Frame empfangen wird. Ein lokaler Loopback-Ton ermöglicht das Hören der eigenen Funktöne.

### 6. Vulkan-kompatibles rahmenloses HUD-Overlay
* **HUD-Overlay-Fenster**: Der Client bietet ein optionales, transparentes WPF-Overlay, das im Vordergrund läuft. Es zeigt den VoIP-Status, die aktive Funkfrequenz und eine Echtzeitliste der aktiven Sprecher mit Funksignalsymbolen.
* **Win32-Durchklick-Integration**: Durch Win32-Window-Styles (`WS_EX_TRANSPARENT` und `WS_EX_NOACTIVATE`) stiehlt das Overlay keinen Fokus und lässt Mausereignisse direkt zum Spiel durch.
* **API-unabhängiges Rendering**: Da transparente WPF-Fenster auf DWM-Komposition (Desktop Window Manager) basieren, greift das Overlay nicht in die Grafikpipeline ein. Das garantiert volle Kompatibilität mit **Vulkan** und **DirectX**, sofern Star Citizen im **"rahmenlosen Fenstermodus"** (Borderless Windowed) ausgeführt wird.

### 7. Umgebungsakustik (Okklusion & Nachhall)
* **Okklusionsfilter:** Wenn sich Sprecher und Hörer in unterschiedlichen Zonen oder Abteilen befinden, wendet der Client automatisch einen Tiefpassfilter (Grenzfrequenz 600Hz, Lautstärke 65%) an, um eine physische Blockade/Okklusion zu simulieren. Die Grenzfrequenz wird weich interpoliert, um Knackgeräusche zu vermeiden.
* **Ortsabhängiger Nachhall:** Wenn sich der Hörer in einer bestimmten Umgebung (Höhle, Bunker oder Hangar) befindet, wendet ein Feedback-Delay-Comb-Filter spezifische Hallparameter an:
  * *Höhlen / Tunnel:* 45% Wet-Mix, 100ms Verzögerung, 0.6 Feedback.
  * *Bunker / Stationen:* 25% Wet-Mix, 50ms Verzögerung, 0.4 Feedback.
  * *Hangars:* 35% Wet-Mix, 150ms Verzögerung, 0.5 Feedback.

### 8. Discord Rich Presence ohne externe Abhängigkeiten (RPC)
* **Named Pipe Verbindung:** Der Client verbindet sich ohne schwere externe NuGet-Bibliotheken über lokale Windows Named Pipes (`\\.\pipe\discord-ipc-0`) direkt mit Discord.
* **Dynamische Status-Updates:** Aktualisiert die Discord-Aktivität in Echtzeit:
  * **Details:** Aktuelle In-Game-Zone (z. B. `"In einer Höhle auf MicroTech"`).
  * **Status:** Aktiver Funkkanal und Helm-Status (z. B. `"Auf Funkkanal: Bravo (Helm auf)"` oder `"In der Nähe"`).
  * **Vergangene Zeit:** Zeigt die Dauer seit dem Verbindungsaufbau zum VoIP-Server an.

---

## 🖥️ XuruVoip Server (Go)

Der Server koordiniert die Positionen, authentifiziert Verbindungen und leitet Audiopakete basierend auf Distanzen und Funkkanälen weiter.

### Features
* **Serverseitige Proximity-Steuerung**: Leitet Proximity-Audio nur an Spieler innerhalb der Reichweite (Standard 50m) weiter.
* **Raumklang-Modus**: Umschaltbar über `.env` (`XURUVOIP_SPATIAL_AUDIO`). Bestimmt, ob echte Koordinaten oder nur Entfernungen gesendet werden.
* **Mehrkanal-Funkrouting**: Erlaubt das gleichzeitige Hören mehrerer Funkkanäle bei Übertragung auf dem aktiven Kanal.
* **Audioprofil-System**: Weist Spielern Audioeffekte (z.B. Radio-Effekt, Echo) zu.
* **SQLite-Datenbank**: Speichert Kanäle und Profile dauerhaft.
* **Sicherheitssystem**: Sperrt (bannt) Störenfriede nach Benutzername, IP-Adresse und Hardware-Fingerabdruck (HWID/MachineGuid).
* **Webportal für Admins**: Sichere Weboberfläche (HTTPS/WebSockets) mit Echtzeit-Logs, Dashboard und Ban-Verwaltung.
* **Server-Admin-Radarkarte**: Echtzeit-2D-Radarkarte über HTML5-Canvas im Admin-Dashboard zur Verfolgung von Spielerpositionen mit Zoom per Mausrad, Panning per Klick-und-Drag, Zonenfilterung, Verfolgung historischer Gehpfade (Breadcrumbs) und konzentrisch pulsierenden Schallwellenringen um aktive Sprecher.

### Server-Konfiguration (`.env`)
Beim ersten Start wird eine Standard-`.env`-Datei generiert:
```env
XURUVOIP_SERVER_IP=
XURUVOIP_PORT=8888
XURUVOIP_AUDIO_PORT=8889
XURUVOIP_DATA_DIR=.
XURUVOIP_MAX_PLAYERS=500
XURUVOIP_SPATIAL_AUDIO=1
XURUVOIP_PUBLIC_SERVER=0
XURUVOIP_SERVER_PASSWORD=auto_generated_32_chars_token
XURUVOIP_ADMIN_SERVER_PASSWORD=auto_generated_32_chars_token
XURUVOIP_VERBOSE_LOGS=1
XURUVOIP_LIMIT_RATE_POS=50.0
XURUVOIP_LIMIT_BURST_POS=100
XURUVOIP_LIMIT_RATE_AUDIO=60.0
XURUVOIP_LIMIT_BURST_AUDIO=120
XURUVOIP_LOCKOUT_ATTEMPTS=5
XURUVOIP_LOCKOUT_WINDOW=60
XURUVOIP_LOCKOUT_DURATION=600
```

### Server kompilieren

#### Linux
```bash
cd server
GOOS="linux" GOARCH="amd64" go build .
```

#### Windows
```powershell
cd server
$env:GOOS="windows"
$env:GOARCH="amd64"
go build .
```

### Server starten

#### Aus Quellcode:
```bash
cd server
go run .
```

#### Aus Binärdatei:
##### Windows
```powershell
.\server.exe
```

##### Linux
```bash
./server
```

### 🖥️ Headless Server-Einrichtung & Bereitstellung

In Produktivumgebungen sollte der Go-Server im Hintergrund als Systemdienst (Daemon) laufen, um Neustarts bei Abstürzen und Bootstarts zu automatisieren.

#### 1. Netzwerk- & Firewall-Konfiguration
Geben Sie die TCP-Ports der `.env`-Datei (Standard `8888` und `8889`) in der Firewall frei:
* **Linux (UFW):**
  ```bash
  sudo ufw allow 8888/tcp
  sudo ufw allow 8889/tcp
  sudo ufw reload
  ```
* **Linux (firewalld):**
  ```bash
  sudo firewall-cmd --zone=public --add-port=8888/tcp --permanent
  sudo firewall-cmd --zone=public --add-port=8889/tcp --permanent
  sudo firewall-cmd --reload
  ```

---

#### 2. Linux-Bereitstellung (systemd)

So richten Sie den Server als systemd-Dienst ein:

##### Schritt A: Verzeichnisse & Berechtigungen anlegen
Erstellen Sie einen Systembenutzer und das Arbeitsverzeichnis zur Sicherheitsisolation:
```bash
# Systembenutzer ohne Login-Rechte anlegen
sudo useradd -r -s /bin/false xuruvoip

# Ordner anlegen und Binärdatei kopieren
sudo mkdir -p /opt/xuruvoip
sudo cp xuruvoip-server-linux-x64 /opt/xuruvoip/xuruvoip-server
sudo chmod +x /opt/xuruvoip/xuruvoip-server

# Besitzerrechte übertragen
sudo chown -R xuruvoip:xuruvoip /opt/xuruvoip
```

##### Schritt B: Konfigurationsdatei `.env` erstellen
Führen Sie den Server einmalig als Systembenutzer aus, um die Standard-Konfiguration zu erstellen:
```bash
sudo -u xuruvoip /opt/xuruvoip/xuruvoip-server -port 8888 -audio-port 8889
```
*Drücken Sie `Ctrl+C`, sobald die Passwörter ausgegeben wurden.* Passen Sie die `.env`-Datei an:
```bash
sudo nano /opt/xuruvoip/.env
```

##### Schritt C: systemd-Service-Datei erstellen
Kopieren Sie die Servicedatei aus dem Git-Repository `server/xuruvoip.service` nach `/etc/systemd/system/xuruvoip-server.service` oder erstellen Sie sie mit folgendem Inhalt:
```ini
[Unit]
Description=XuruVoip Star Citizen Spatial VOIP Server
After=network.target

[Service]
Type=simple
User=xuruvoip
Group=xuruvoip
WorkingDirectory=/opt/xuruvoip
ExecStart=/opt/xuruvoip/xuruvoip-server
Restart=always
RestartSec=5
LimitNOFILE=65536

[Install]
WantedBy=multi-user.target
```

##### Schritt D: Dienst aktivieren & starten
```bash
sudo systemctl daemon-reload
sudo systemctl enable xuruvoip-server
sudo systemctl start xuruvoip-server
```

##### Schritt E: Logs und Überwachung
```bash
# Dienst-Status anzeigen
sudo systemctl status xuruvoip-server

# Log-Ausgabe in Echtzeit verfolgen
journalctl -u xuruvoip-server -f -n 100
```

---

#### 3. Windows-Bereitstellung (NSSM)

Um den Server als vollwertigen Windows-Dienst im Hintergrund laufen zu lassen, empfiehlt sich das Tool **NSSM (Non-Sucking Service Manager)**:

##### Schritt A: Verzeichnis erstellen
Verschieben Sie die Datei `xuruvoip-server-windows-x64.exe` in einen Ordner (z.B. `C:\XuruVoipServer`).

##### Schritt B: Erste Konfiguration
Führen Sie die Datei einmalig in PowerShell aus, um die Konfigurationen anzulegen. Beenden Sie sie mit `Ctrl+C` und bearbeiten Sie die `.env`.

##### Schritt C: Dienst mit NSSM installieren
```powershell
.\nssm.exe install XuruVoipServer "C:\XuruVoipServer\xuruvoip-server-windows-x64.exe"
```
Geben Sie das Arbeitsverzeichnis `C:\XuruVoipServer` an und installieren Sie den Dienst.

##### Schritt D: Dienst starten
```powershell
Start-Service -Name XuruVoipServer
```

---

## 🎮 Übersicht der XuruVoip-Client-Einstellungen

Das Einstellungsfenster bietet sechs Abschnitte:
1. **General**: Sprachauswahl, Pfad der Star Citizen `Game.log`-Datei und Umschalter für das lokale Logging.
2. **Connection**: Serveradresse, Audio- und Positionsports, Benutzername, Passwort und Serverpasswort/-token.
3. **Position**: Wahl der Positionsquelle ("OCR Screen Scanner" vs. "Game.log Reader (GRTPR)"), Monitorauswahl, Scanintervall (ms), Scanbereich festlegen und Vorschau der letzten Texterkennung (OCR-Optionen werden ausgeblendet, wenn GRTPR aktiv ist).
4. **Audio**: Audiogeräte auswählen, Lautstärke anpassen, Sendemodus (PTT / VAD) festlegen, VAD-Empfindlichkeit einstellen, **3D Spatial Audio** aktivieren sowie erweiterte Einstellungen für Funkverschlechterung und Funktöne.
5. **Hotkeys**: Belegung von Tasten für PTT (Nähe, Funk, Profil), Helm ein/aus, Funkkanal-Umschaltung sowie Mute-Tasten für Ausgang (Mikrofon) und Eingang (Wiedergabe).
6. **Overlay**: Aktivierung des transparenten HUD-Overlays und Einstellung der Bildschirmecke für die Platzierung (z. B. Oben links, Oben rechts).

### Client kompilieren und ausführen

#### Anforderungen
- Windows 10 oder Windows 11
- .NET 9.0 SDK (mit WPF-Komponenten)

#### Kompilieren & Starten:
```powershell
cd client
dotnet run
```

### Installation des Release-Pakets

Da die Installationsdateien und ausführbaren Dateien nicht digital signiert sind, blockiert Windows SmartScreen den Start standardmäßig. Sie müssen die Sperre in den Dateieigenschaften aufheben.

* **Option A: MSI-Installer (Empfohlen)**
  1. Laden Sie `XuruVoipClient-win-x64.msi` von der [Release-Seite](https://github.com/XuruDragon/XuruVOIP/releases) herunter.
  2. Klicken Sie mit der rechten Maustaste auf die Datei und wählen Sie **Eigenschaften**.
  3. Aktivieren Sie im Reiter *Allgemein* unten das Kontrollkästchen **Zulassen** (oder "Sicherheit: Blockierung aufheben") und klicken Sie auf **Übernehmen**.
  4. Starten Sie das Setup durch Doppelklick.

* **Option B: Portable ZIP-Version**
  1. Laden Sie `XuruVoipClient-win-x64.zip` von der [Release-Seite](https://github.com/XuruDragon/XuruVOIP/releases) herunter.
  2. Machen Sie einen Rechtsklick auf das ZIP-Archiv und heben Sie die Blockierung unter **Eigenschaften** auf. Klicken Sie auf **Übernehmen**.
  3. Entpacken Sie die ZIP-Datei in einen beliebigen Ordner (z.B. `C:\Games\XuruVoip`).
  4. Starten Sie das Programm direkt per Doppelklick auf `XuruVoipClient.exe`.

---

## 👥 Mitwirkende

Entwickelt von **[@XuruDragon](https://github.com/XuruDragon)** in Zusammenarbeit mit **Antigravity IDE**.
