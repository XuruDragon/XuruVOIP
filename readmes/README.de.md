# XuruVoip (Deutsch)

<p align="center">
  <a href="https://github.com/XuruDragon/XuruVOIP/actions/workflows/tests.yml">
    <img src="https://github.com/XuruDragon/XuruVOIP/actions/workflows/tests.yml/badge.svg" alt="Tests Status" />
  </a>
  <a href="https://github.com/XuruDragon/XuruVOIP/releases">
    <img src="https://img.shields.io/github/v/release/XuruDragon/XuruVOIP?color=blue&logo=github" alt="Latest Release" />
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

XuruVoip ist eine leistungsstarke, sichere und dynamisch räumliche **3D-Sprachkommunikationssuite (VoIP)**, die speziell für benutzerdefinierte Gaming-Integrationen mit **Star Citizen** entwickelt wurde. Es besteht aus einem Go-basierten Backend-Server und einem modernen C#-WPF-Client mit integrierter Companion App (Weboberfläche) und Elgato Stream Deck-Integration.

### 🎯 Projektziel
Das Ziel von XuruVoip ist es, Star Citizen-Gaming-Events, Rollenspiel-Organisationen und taktischen Trupps ein **beispielloses Maß an Audio-Immersion und Bedienkomfort** zu bieten. Durch das Lesen von Echtzeitkoordinaten, Visier- und Fahrzeugzuständen vom Spielclient formt XuruVoip die Stimmen der Spieler dynamisch im 3D-Raum, simuliert Planeten-/Vakuumatmosphären und leitet taktische Kommunikation automatisch weiter, ohne dass manuelle Clientkonfigurationen erforderlich sind.

---

### 🗺️ Navigationsverzeichnis

| Abschnitt | Beschreibung |
| :--- | :--- |
| [📖 Detaillierter Funktionsleitfaden](../doc/functionnalities.md) | Technische und benutzerseitige Erklärung aller über 20 implementierten Funktionen. |
| [📖 Nichttechnische Benutzerhandbücher](#-nichttechnische-benutzerhandbücher) | Leicht verständliche Schritt-für-Schritt-Anleitungen für Client, Server und Stream Deck. |
| [📸 Screenshots und Benutzeroberfläche](#-screenshots-und-benutzeroberfläche) | Visuelle Darstellung der Kundenbildschirme, des Admin-Portals und der Einstellungen. |
| [🗂️ Projektstruktur](#️-project-structure) | Repository-Layout und Ordneraufteilung. |
| [⚙️ Systemarchitektur](#️-system-architecture) | Das vollständige tatsächliche Workflow-Diagramm des WPF-Clients, des Go-Servers und externer Geräte. |
| [💡 Übersicht über die Kernfunktionen](#-übersicht-über-die-kernfunktionen) | Detaillierte Aufschlüsselung der über 19 implementierten räumlichen und Netzwerkfunktionen. |
| [🖥️ Go Server (Go)](#️-xuruvoip-server-go) | Anweisungen zum Erstellen, Ausführen, Bereitstellen und Konfigurieren des Servers. |
| [🎛️ Discord Voice Bridge](#️-discord-voice-bridge-setup-guide) | Verbinden von Go-Server-Funkkanälen mit einem Discord-Sprachkanal. |
| [📱 Companion App & Stream Deck](#-companion-app--stream-deck-integration) | Fernsteuerung des Geräts und Einrichtung der physischen Tasten des Stream Decks. |
| [🛠️ WPF-Client (C#)](#-building--running-the-client) | Client-Anforderungen, Kompilierung und MSI/Portable-Installationshandbücher. |

---

## 📖 Nichttechnische Benutzerhandbücher

Wenn Sie keine Informatikkenntnisse haben, haben wir einfache Schritt-für-Schritt-Anleitungen geschrieben, die Ihnen dabei helfen, alles einfach zu konfigurieren und zum Laufen zu bringen:

* 📖 **[Detaillierter Funktionsleitfaden](../doc/functionnalities.md)**: Ausführliche Erklärung aller implementierten Funktionen, wie sie funktionieren, wie man sie benutzt und warum sie nützlich sind.
* 🎮 **[Client-Benutzerhandbuch](doc/client_guide.md)**: Freundliche Anleitung zur Auswahl von Mikrofonen/Lautsprechern, zur Einrichtung von Push-to-Talk, zur Verwendung von Raumanzughelmen und zum Einschalten von Spracheffekten bei Anstrengung.
* 🖥️ **[Server-Konfigurationshandbuch](doc/server_guide.md)**: Erklärt, wie man einen Server hostet, Passwörter/Einstellungen in der „.env“-Einstellungsdatei anpasst und die Discord Voice Bridge einrichtet.
* 🎛️ **[Stream Deck Plugin-Anleitung](doc/streamdeck_guide.md)**: Exemplarische Vorgehensweise zum Installieren physischer Tasten zum Stummschalten, Umschalten des Visiers und Anzeigen aktiver Radiokanäle.

---

## 📸 Screenshots und Benutzeroberfläche

<details>
<summary>📸 Klicken Sie hier, um Screenshots anzusehen</summary>

### 1. Hauptfenster des Clients
![Hauptfenster des Clients](/screenshots/main.png)

### 2. Registerkarte „Audioeinstellungen“ (3D Spatial Audio Control)
![Registerkarte „Audioeinstellungen“](/screenshots/audio.png)

### 3. Registerkarte „Allgemeine Einstellungen“ (Sprach- und Game.log-Auswahl)
![Registerkarte „Allgemeine Einstellungen“](/screenshots/general.png)

### 4. Registerkarte „Verbindungseinstellungen“.
![Registerkarte „Verbindungseinstellungen“](/screenshots/connection.png)

### 5. Registerkarte „Hotkeys-Einstellungen“.
![Registerkarte „Hotkeys-Einstellungen“](/screenshots/hotkeys.png)

### 6. Registerkarte „Overlay-Einstellungen“ (Vulkan & DirectX HUD)
![Registerkarte „Overlay-Einstellungen“](/screenshots/overlay.png)

### 7. Registerkarte „OCR-Einstellungen“ (Tesseract OCR)
![Registerkarte „OCR-Einstellungen“](/screenshots/ocr.png)

### 8. Anmeldeseite des Admin-Webportals
![Anmeldeseite des Admin-Webportals](/screenshots/admin_login.png)

### 9. Admin-Webportal-Dashboard
![Admin-Webportal-Dashboard](/screenshots/admin_dashboard.png)

### 10. Spieler des Admin-Webportals
![Administrator-Webportal-Spieler](/screenshots/admin_players_list.png)

### 11. Admin-Webportal-Administratorliste
![Admin-Webportal-Administratorliste](/screenshots/admin_admin_list.png)

### 12. Sperrliste für Admin-Webportale
![Administrator-Webportal-Sperrliste](/screenshots/admin_ban_list.png)

</details>

---

## 🗂️ Projektstruktur

- **/server**: Leistungsstarkes Go-Backend, das die Positions-, Audio- und Verwaltungsdienste hostet.
- **/client**: Moderner C#-WPF-Client, der NAudio, WebRtcVad und Tesseract OCR oder Game.log Tail für automatisierte Standortverfolgung und Protokollanalyse nutzt. Die Begleit-App ist ebenfalls in diesem Projekt enthalten.
- **/streamdeck**: Stream Deck-Plugin für XuruVoIP-Client.

---

## ⚙️ Systemarchitektur

Nachfolgend finden Sie die vollständige tatsächliche Architektur des XuruVoip-Systems, die die Erfassungs-, Positionierungs-, Wiedergabe- und HUD-Rendering-Schleifen innerhalb des WPF-Clients, der Go-Server-Websocket-Hubs und der externen Integrationen veranschaulicht:```mermaid
graph TB
    subgraph STIM ["Spielumgebung (Star Citizen)"]
        SC["Star Citizen-Client"]
        LOGS["Game.log (Protokolldatei)"]
        SCREEN["Grafikausgabe (Vulkan/DX)"]
    end

    subgraph WPF ["XuruVOIP WPF-Client"]
        direction TB
        subgraph CAPT ["Mikrofonaufnahme und DSP"]
            MIC["Mikrofoneingang"] --> VAD["WebRTC VAD"]
            VAD -->|Speech Detected| VC["Sprachwechsler (Alien/Cyborg/Roboter)"]
            VC -->|Modulated PCM| GF_FIL["G-Force Pitch & Tremolo / Exertion Panting Injection"]
            GF_FIL --> HELM_OSC["Overlay für Helmatmung und Entlüftungsbrummen"]
            HELM_OSC --> OPUS_ENC["Opus-Encoder"]
        end

        subgraph POS_TRACK ["Positionierung und Zustandsverfolgung"]
            LOGS -->|Tail Scanner| LOG_PAR["Game.log-Parser"]
            SCREEN -->|showlocations Capture| OCR["Tesseract OCR-Engine"]
            LOG_PAR -->|Equip/Visor Events| HELM_DET["Automatische Synchronisierung des Visierstatus"]
            LOG_PAR -->|G-Force & Stamina Values| GF_DET["G-Force- und Anstrengungs-Tracker"]
            OCR -->|Coords| POS_SEL{"Quellenauswahl"}
            LOG_PAR -->|Coords & ContainerID| POS_SEL
        end

        subgraph PLAY ["Räumliche Wiedergabe und DSP"]
            OPUS_DEC["Opus-Decoder"] --> PKT_TYPE{"Pakettyp?"}
            PKT_TYPE -->|PA 0x03| PA_FIL["Megafon-DSP (HP/LP, Tanh Distortion, Ship Reverb)"]
            PKT_TYPE -->|Proximity/Radio| OCC_FIL["Carrack/Hercules Deck & Raumverdeckung"]
            OCC_FIL --> REV_FIL["Standortbezogener Hall (Höhlen/Bunker/Hangars)"]
            REV_FIL --> RAD_FIL["Funkbandpass und Long-Range-Multi-Hop-Routing (Dijkstra)"]
            RAD_FIL --> CHIMES["PTT-Mikrofon-Chirps und Squelch-Schwanzgenerator"]
            CHIMES --> PAN["Räumliche 3D-Schwenkmathematik"]
            PAN --> VOL["Räumliche Distanzdämpfung"]
            VOL --> MIXER["NAudio-Mixer"]
            PA_FIL --> MIXER
            MIXER --> SPK["Audioausgabegeräte"]
        end

        subgraph HUD ["HUD-Overlay (Win32 Click-Through)"]
            T_RAD["Taktisches 2D-Miniradar"]
            STT["Whisper.net Speech-to-Text"]
            OPUS_DEC -.->|Incoming Voice| STT
            STT -->|Subtitles| SUB["HUD-Untertitel in Echtzeit"]
        end

        subgraph COMP ["Begleitender Webserver"]
            HTTP_SRV["Lokaler HTTP-Listener (benutzerdefinierter Port)"]
            DASH["Glassmorphisches HTML/JS-Dashboard"]
        end

        POS_SEL -->|Coordinates & Zone| POS_WS["WS-Client positionieren"]
        HELM_DET -->|Visor State| POS_WS
        GF_DET -->|G-Force / Exertion| GF_FIL
        OPUS_ENC -->|Audio Packets| AUD_WS["Audio-WS-Client"]
    end

    subgraph SERVER ["XuruVOIP Go-Server"]
        direction TB
        WS_HUB["Websocket-Verbindungs-Hub"]
        POS_HUB["Räumliche Positionierung und Zonen-Hub"]
        DB["SQLite-Datenbank und persistente Kanäle"]
        DISC_BRIDGE["Discord Voice Bridge"]
        ADM_PORT["Admin-Webportal (Canvas Live Radar)"]

        WS_HUB <--> POS_HUB
        POS_HUB <--> DB
        DISC_BRIDGE <--> WS_HUB
    end

    subgraph EXT ["Externe Schnittstellen"]
        DISC["Discord-Sprachkanal"] <-->|Bidirectional Voice Bridge| DISC_BRIDGE
        SD["Stream Deck App"] <-->|WebSocket Actions / Port Setting| HTTP_SRV
        MOB["Mobiler Controller"] <-->|REST API Status & Toggles| HTTP_SRV
    end

    POS_WS <-->|WS Port 8888| WS_HUB
    AUD_WS <-->|WS Port 8889| WS_HUB
```
---

## 💡 Übersicht über die Kernfunktionen

### 1. 🔊 3D-Raumaudio in Echtzeit
* **Dynamisches Stereo-Panning:** PROJEKTIERT die Koordinaten der entfernten Lautsprecher auf die Vorwärts- und Rechtsrichtungsvektoren des Zuhörers, um mithilfe einer Konstantleistungsformel das exakte Links-/Rechts-Panning zu berechnen.
* **Auflösung von Mehrdeutigkeiten zwischen Vorder- und Rückseite:** Reduziert die Audiolautstärke um 25 %, wenn ein Lautsprecher hinter dem Zuhörer steht, und beseitigt so die standardmäßigen 2D-Audio-Schwenkbeschränkungen.
* **Distance Roll-Off:** Blendet Annäherungsstimmen basierend auf der Entfernung linear aus und sorgt so für natürliche Lautstärkepegel (verringert sich bei 50 Metern vollständig auf Null bzw. bei Flüstern auf 5 Meter).

### 2. 🗺️ Standortbezogene Akustik und Schiffs-/Bunker-Okklusion
* **Deck- und Wandverdeckung:** Erkennt interne Grenzen innerhalb von Räumen. Wenn sich Spieler auf verschiedenen Decks (z. B. Carrack, Hercules) oder Räumen (z. B. Bunkern) befinden, werden Tiefpassfilterung (Grenzfrequenzen von 300 Hz bis 900 Hz) und Lautstärkedämpfung dynamisch angewendet.
* **Environmental Reverb:** Liest die hierarchische Zone des Players und wendet automatisch benutzerdefinierte Wet-Mix-, Delay- und Feedback-Reverb-Parameter für **Höhlen**, **Bunker** und **Hangars** an.

### 3. 💨 Helm- und EVA-Atmosphärensimulation
* **EVA-Stummschaltung:** Schaltet die Annäherungs-Sprachkommunikation im Weltraum oder in Vakuumzonen (EVA) automatisch stumm und zwingt Spieler, Funkkanäle zur Kommunikation zu verwenden.
* **Visier-Atemschutzmaskenauflage:** Simuliert den Luftdruck, wenn das Visier heruntergeklappt ist. Synthetisiert ein niederfrequentes Atemrauschen und ein Dual-Frequenz-Brummen (50 Hz + 100 Hz) des Anzugslüftungsventilators auf die aufgenommene Mikrofoneinspeisung.
* **Automatische Visiersynchronisierung:** Liest Anhangsprotokolle in „Game.log“, um automatisch zu erkennen, wann ein Helm angebracht/abgenommen wird, und aktualisiert den Visierstatus in Echtzeit.

### 4. 🎙️ Sci-Fi-Stimmenverzerrer und Anzugmodulatoren
* **Echtzeit-DSP-Filter:** Tonhöhenverschiebung im Zeitbereich, Flanger, Ringmodulation, Soft-Tanh-Sättigung und 8-Bit-Bitcrushing.
* **Atmosphärische Voreinstellungen:** Laden Sie sofort voreingestellte Sprachprofile, einschließlich **Alien**, **Cyborg**, **Robotic** oder **Custom Pitch Shift** (0,5x bis 2,0x).
* **Benutzerdefinierte Modulator-Schieberegler:** Passen Sie Tonhöhe, Ringmodulationsfrequenz/-mischung, Flanger-Tiefe/-Rate/-Feedback und Bitcrush-Einstellungen über Schieberegler an.

### 5. 📻 Immersive Radiodegradation & Glockenspiele
* **Bandpassfilterung:** Modelliert Funkfilter mit niedrigen/hohen Grenzwerten bei der Nutzung von Funkkanälen oder bei heruntergeklappten Anzugvisieren.
* **Verschlechterung des Funksignals:** Schmale Grenzbänder und Mischungen mit bandpassgefiltertem statischem Rauschen, wenn sich die Grenze des Funksenders nähert.
* **Akustische Funkglockenspiele:** Spielt mechanische Key-Down- und Key-Up-Glockenspiele beim Senden auf Funkkanälen. Unterstützt vier verschiedene mathematische Profile, die in den Einstellungen oder der Companion App ausgewählt werden können: Militär (Sinusschwankungen), Industrie (mechanisches Klacken), Alien (ringmodulierte Frequenzschwankungen) und Vintage (analoge Relais-Klicks).
* **Entfernungsabhängige Funkverzögerung:** Simuliert die Signallaufzeit basierend auf der Lichtgeschwindigkeit ($\approx 3,3\text{ ms}$ pro Kilometer, bis zu 3000 ms max.) für eine realistische Verzögerung.
* **Benutzerdefinierte PTT-Chimes:** Lädt und resampelt eigene WAV/MP3-Dateien (`radio_key_down` und `radio_key_up`) aus dem Ordner `Resources/`, um sie als benutzerdefinierte Chimes zu verwenden.

### 6. 💬 Automatisches Schiffs-Gegensprechsystem
* **Fahrzeug-Intercom-Kanäle:** Durch das Einsteigen in ein Fahrzeug abonnieren Spieler automatisch einen dynamischen „Intercom_<ContainerID>“-Funkkanal.
* **Pilot Priority Ducking:** Wenn ein Spieler in einem Cockpit oder Fahrersitz über die Gegensprechanlage sendet, wird der Annäherungston aller anderen Spieler um 85 % geduckt, um die Klarheit der Flugbefehle zu gewährleisten.
* **Dynamische Intercom-Verschlechterung:** Intercom-Kanäle verschlechtern sich automatisch basierend auf dem Schiffsstatus:
  * **Schildtreffer (Shield Hits):** Erzeugt vorübergehend statische Rauscheffekte und Lautstärkeknackser (dauert 2,5 Sekunden).
  * **Kritische Energie (Critical Power):** Niederspannungs-Wechselstrombrummen, Soft-Clipping-Verzerrung und Tonhöhenabfall (Resampling).
  * **Quantenreise (Quantum Travel):** Kammfilter-Flanger/Phaser-Effekt und hochfrequentes Pfeifen.
  * *Alle Untereffekte können in den allgemeinen Einstellungen einzeln ein- oder ausgeschaltet werden und sind standardmäßig deaktiviert.*
* **Aufräum-Abklingzeit:** Zählt 5 Minuten herunter, nachdem der letzte Spieler das Schiff verlassen hat, bevor der Intercom-Kanal gelöscht wird, um die Serverleistung zu maximieren.

### 7. 📡 Vulkan-kompatibles HUD-Overlay und taktisches 2D-Radar
* **Win32 Click-Through-Overlay:** Ein randloses HUD-Overlay, das VoIP-Verbindungen, Frequenzen und Sprechzustände anzeigt. Vulkan- und DirectX-kompatibel (läuft im randlosen Fenstermodus).
* **Interaktiver HUD-Customizer:** Ermöglicht die Echtzeit-Anpassung von Thema (Aegis, Anvil, Drake, RSI, Origin), Positionierung (Ecken/Mitte) und Sichtbarkeit der Komponenten (Miniradar, Sprecherliste, Verbindungskopf) über die Einstellungen oder die Companion App.
* **Gegensprech-Statusanzeige:** Zeigt Warnmeldungen wie `⚡ INTERCOM: DEGRADED` (mit Details wie `[Power Loss]`, `[Quantum]` oder `[Static Pop]`) auf dem HUD-Overlay an, wenn die Intercom-Verschlechterung aktiv ist.
* **Taktisches Mini-Radar:** Verfügt über ein auf den Kurs ausgerichtetes 2D-HUD-Radar, das relativ sprechende Spieler anzeigt und pulsierende Tonringe um sie herum zeichnet.
* **3D-Höhenindikatoren:** Fügt vertikale Richtungspfeile und Deckhöhen-Deltas (z. B. `Bob (▲ 12m)`) neben Radarblips hinzu, wenn die vertikale Trennung 2 Meter überschreitet.
* **Speech-to-Text-Untertitel:** Transkribiert eingehende Radio-/Proximity-Audiodaten mithilfe eines Offline-, leichten Whisper-Modells („ggml-tiny.bin“) in lokalisierte HUD-Untertitel.
* **Freihändige PTT-Sprachbefehle:** Das Halten der zugewiesenen Sprachbefehlstaste stummt vorübergehend die ausgehenden Proximity-/Radio-Sprachübertragungen und puffert das Mikrofon-Audio. Beim Loslassen wird die Sprache lokal über das Whisper-Modell transkribiert, um Schiffsaktionen auszulösen:
  * **Unterstützte Befehle:** Visier-/Helm-Umschaltung, Mikrofon-Stummschaltung (Proximity/Radio/Profil/Alle), Auswahl des aktiven Funkkanals und Stimmenverzerrer-Voreinstellungen.
  * **Mehrsprachige Schlüsselwort-Erkennung:** Unterstützt in 8 Sprachen (Englisch, Französisch, Deutsch, Spanisch, Portugiesisch, Japanisch und Chinesisch).
  * **Konfidenzschwellenwert-Filter:** Ein konfigurierbarer Schieberegler filtert Erkennungen mit geringer Zuverlässigkeit oder Nebengeräusche heraus.
  * *Standardmäßig deaktiviert; die Aktivierung lädt das Offline-Whisper-Transkriptionsmodell (~140 MB) herunter, falls es noch nicht vorhanden ist.*
  * **Referenzliste für Sprachbefehle:**
    Nachfolgend finden Sie die Liste aller unterstützten freihändigen PTT-Sprachbefehle, unterteilt in XuruVOIP-Anwendungssteuerungen und Star Citizen-Tastenbelegungen:
    
    #### XuruVOIP-Anwendungssteuerungen (Exklusiv für XuruVOIP)
    Diese Befehle steuern Ihren Helm-/Visierstatus, Kanalschaltungen, Radiofrequenzen oder Sprachmodulationen direkt in XuruVOIP:
    * **Helm/Visier umschalten:** `visier`, `helm`, `visier umschalten`, `helm umschalten`
    * **Nähe-Kanal stummschalten:** `naehe stummschalten`, `proximity stummschalten`
    * **Nähe-Kanal lautschalten:** `naehe lautschalten`, `proximity lautschalten`
    * **Funk-Kanal stummschalten:** `funk stummschalten`, `radio stummschalten`
    * **Funk-Kanal lautschalten:** `funk lautschalten`, `radio lautschalten`
    * **Profil-Kanal stummschalten:** `profil stummschalten`
    * **Profil-Kanal lautschalten:** `profil lautschalten`
    * **Mikrofon stummschalten (Global):** `alles stummschalten`, `mikrofon stummschalten`, `mikro stummschalten`
    * **Mikrofon lautschalten (Global):** `alles lautschalten`, `mikrofon lautschalten`, `mikro lautschalten`
    * **Funkkanal wechseln:** `kanal wechseln`, `schalte kanal`, `kanal auf`, `kanal` gefolgt vom Kanalnamen (z. B. *"kanal wechseln Alpha"*)
    * **Stimmenverzerrer-Profil einstellen:** `stimmenverzerrer`, `stimmprofil`, `stimme` gefolgt von einem Profil: `alien`, `cyborg`, `robotic` (oder `robot`), `pitchshift` (oder `pitch shift`, `pitch`), `none` (oder `off`, `normal`, `aus`)
    
    #### Simulierte Schiffssteuerungen (Star Citizen-bezogen)
    Diese Befehle simulieren physische Tastendrücke auf Ihrem PC, um Star Citizen-Steuerungen auszulösen:
    * **Schiffsenergie umschalten:** `energie`, `energie umschalten`, `systeme an`, `systeme aus`, `strom an`, `strom aus`
    * **Schiffstüren/-rampen umschalten:** `tueren`, `tueren oeffnen`, `tueren schliessen`, `aussen oeffnen`, `aussen schliessen`
    * **Schilde nach vorne umleiten:** `schilde`, `schilde vorne`, `schilde vorwaerts`, `schilde umleiten`
    * **Fahrwerk umschalten:** `fahrwerk`, `fahrwerk ausfahren`, `fahrwerk einfahren`

### 8. 📱 Begleit-App und REST-API
* **Lokaler HTTP-Webserver:** Hostet ein lokales Dashboard auf einem konfigurierbaren Port (Standard: „8891“, standardmäßig deaktiviert).
* **Glassmorphic Controller:** Stellt eine Verbindung zu Telefonen oder sekundären Bildschirmen her, um Stummschaltung, Kanalwechsel, Helme oder Sprachwechsler umzuschalten.
* **REST API:** Macht die Endpunkte „GET /api/status“ und „POST /api/action“ für externe Integrationen verfügbar (einschließlich Intercom-Status und Simulationsbefehlen).

### 9. 🎛️ Stream Deck Plugin
* **Stream Deck Action Pack:** Stellt 8 Aktionen zur Steuerung von Mikrofon-Stummschaltungen, Audio-Stummschaltungen, Helmvisieren und Radiofrequenzzyklen bereit.
* **Dynamische Schlüsselsymbole:** Kontinuierliche WebSockets-Update-Schaltflächengrafiken (aktives Cyan vs. gedämpftes Gelb), um den aktuellen Client-Status widerzuspiegeln.
* **Live-Frequenztitel:** Zeigt die Namen aktiver Radiosender direkt auf den physischen Stream-Deck-Schaltflächen an.

### 10. 🔌 Discord Voice Bridge
* **Bidirektionale Audioweiterleitung:** Leitet die Kommunikation zwischen einem Go-Server-Funkkanal und einem Discord-Sprachkanal weiter.
* **Spitznamenzuordnung:** Erfasst Discord-Sprache und ordnet SSRC-IDs Server-Spitznamen zu.
* **Dynamische Frequenzverfolgung:** Verschiebt die Discord-Bridge-Verbindung automatisch, um dem aktiven Kanal des konfigurierten Anführers oder Command/Leader-Profils zu folgen.

### 11. 🛡️ Sicherheit, Protokollrotation und Admin-Canvas-Radar
* **Tägliche Protokollrotation:** Startprotokollarchivierer behält nur die 5 neuesten Protokolle bei.
* **Admin-Dashboard:** Echtzeit-Web-Admin-Panel mit Sperrsicherheit, Ratenbegrenzung und einer interaktiven 2D-HTML5-Canvas-Live-Radarkarte, die es Administratoren ermöglicht, zu zoomen, zu schwenken und historische Spielerpfade zu verfolgen.

### 12. 🤢 Stimmverzerrung durch G-Kraft und körperliche Anstrengung
* **Tremolo & Pitch Shifting:** Unter hohen G-Kräften wird das ausgehende Mikrofon-Audio dynamisch mit einem Tremolo-LFO moduliert (4–10 Hz, bis zu 40 % Tiefe) und nach unten gestimmt (Faktor: 1,0 bis 0,85), um körperliche Belastung, Blackout oder Redout-Zustände zu simulieren.
* **Overlay für schweres Atmen:** Überlagert automatisch zufällige Keuch-/Atemgeräusche und skaliert die Geschwindigkeit des Atemzyklus basierend auf der Ausdauer des Spielers, die in Echtzeit aus „Game.log“ analysiert wird.
* **Manuelle / API-Steuerelemente:** Umschaltbar über Client-Einstellungen und Web-UI-Schieberegler der Companion App für Rollenspiele oder Probetests.

### 13. 📡 Taktische Funkrelais- und Multi-Hop-Repeater-Beacons
* **Multi-Hop-Signalrouting:** Spieler können den „Beacon-Modus“ umschalten, um als Funk-Repeater-Beacon zu fungieren. Befinden sich zwei Spieler außerhalb der direkten Funkreichweite (über 1500 m), führt der Empfänger-Client Dijkstras Kürzeste-Weg-Algorithmus über alle aktiven Repeater in der Zone aus.
* **Worst-Hop-Qualitätsverschlechterung:** Wenn ein Multi-Hop-Pfad unterhalb der 8000-m-Single-Hop-Grenze vorhanden ist, leitet das System die Kommunikation weiter und wendet den Verschlechterungsfaktor des Worst-Hop (Signalqualität) anstelle der gesamten geradlinigen Entfernung an, wodurch planetarische/orbitale Funknetzwerke mit großer Reichweite ermöglicht werden.
* **Dynamischer WebSocket-Status:** Aktive Repeater-Status werden in Echtzeit über den WebSocket-Steuerkanal des Servers synchronisiert.

### 14. 📢 Öffentliches Rundfunksystem (PA) des Schiffes
* **Schiffsweite Audioübertragung:** Piloten oder Kapitäne von Schiffen mit mehreren Besatzungsmitgliedern können Sprachansagen an alle Besatzungsmitglieder senden, die dieselbe „ContainerID“ (Schiff) in derselben Zone haben.
* **PA-DSP und Klaxon-Glockenspiel:** PA-Übertragungen umgehen lokale Annäherungs- und Radio-Stummschaltungen (außer Hauptlautstärke/Stummschaltung), spielen Mono mittengeschwenkt ab, stellen einen Sci-Fi-Zweiton-Glocken-/Klaxon-Alarm voran und wenden einen Megafon-Bandpass- und Nachhallfilter an, der die Innenakustik eines hohlen Schiffs simuliert.

### 15. 🔌 Externe Hardware-Telemetrie (Sim-Pit UDP Sync)
* **Echtzeit-UDP-Synchronisierung:** Der Client sendet VoIP- und Helmzustände im JSON-Format alle 100 ms an `127.0.0.1:8895`.
* **Hardware-Integration:** Ermöglicht Cockpit-Bauern die Integration physischer LEDs oder Anzeigen, die auf Kommunikationsereignisse reagieren.

### 16. 🪐 Planetare Atmosphärendichtesimulation
* **Reichweitenskalierung:** Die Reichweite von Proximity-Stimmen passt sich der Dichte der planetaren Atmosphäre an (z. B. 3,5-mal schnellerer Abfall auf Cellin).
* **Dämpfungsfilter:** Wendet einen Tiefpassfilter auf Stimmen im Freien bei dünner Atmosphäre an, der in druckbelüfteten Innenräumen automatisch umgangen wird.

### 17. 🎙️ Post-Op-Sprachrekorder & AAR-Portal
* **Ogg/Opus-Container ohne Overhead:** Speichert Opus-Pakete direkt in `.ogg`-Dateien ohne Server-Transkodierung.
* **Interaktive Canvas-Timeline:** Ermöglicht Administratoren die Visualisierung, Wiedergabe und Löschung von Missionsaufzeichnungen im Admin-Portal.

### 18. 📞 Schiff-zu-Schiff-Hailing & Anrufsystem
* **Cockpit-zu-Cockpit-Anrufe:** Richtet private Sprachverbindungen zwischen Schiffen innerhalb einer Reichweite von 5.000 m.
* **Freisprech-Streaming:** Aktiviert während des Anrufs automatisch die VAD-Sprachübertragung und umgeht Standard-PTT-Tasten.
* **Realistische Glockenspiele:** Synthetisiert Wähltöne, Rufe und Verbindungs-/Trenntöne über NAudio.

### 19. 🔤 Visier-HUD Echtzeit-Übersetzungsuntertitel
* **Dynamischer Übersetzer:** Übersetzt eingehende fremdsprachige Sprachströme in Echtzeit mithilfe militärischer Flugwörterbücher für 7 Sprachen.
* **HUD-Präfix:** Zeigt übersetzten Text auf dem Visier-HUD an, vorangestellt mit `[VON -> NACH]`.
* **Whisper-Modell-Loader:** Lädt das Whisper-Modell (~75 MB) bei Aktivierung automatisch im Hintergrund herunter, falls nicht vorhanden.

### 20. 🎧 Binaurale HRTF-Räumliche Audio
* **Physische Ohrensimulation:** Simuliert die menschliche Ohrform und Kopfschatteneffekte mittels ITD (Interaural Time Difference) und ILD (Interaural Level Difference) Tiefpassdämpfung.
* **Stereokompatibilität:** Liefert hochauflösende 3D-Audioreize über Standard-Stereokopfhörer, ohne dass Surround-Sound-Hardware erforderlich ist.

### 21. 📊 Visier-HUD 3D-Spektrogramm
* **FFT-Telemetrie-Overlay:** Berechnet in Echtzeit Radix-2 64-Punkt Fast-Fourier-Transformationen (FFT) für eingehende Sprachströme der Sprecher.
* **Dynamische HUD-Visualisierung:** Gruppiert Audiofrequenzen in 8 Spektralbänder neben aktiven Sprechern auf dem Vulkan/DX HUD, mit sanftem Abklingen.

### 22. 🎙️ Sprachaktivierte Schiffssteuerung
* **Sprachbefehl-zu-Hotkey-Übersetzung:** Hört auf Sprachbefehle (z. B. „open doors“) und gleicht sie mit lokalisierten Wörterbüchern in 8 Sprachen ab.
* **Direkte Hardware-Tastatureingaben:** Simuliert physische Tastendrücke über die Low-Level-Win32-API `keybd_event` (Tasten werden für eine zuverlässige Erfassung im Spiel 50 ms lang gedrückt gehalten, inklusive Unterstützung für Modifikatortasten).

### 23. 🛰️ Serverseitige AAR 3D-Wiedergabe
* **Koordinatenprotokollierung:** Der Server protokolliert Spielerkoordinaten und Zonen alle 500 ms in einer `<session_id>_positions.jsonl`-Datei.
* **Synchronisierte WebGL-3D-Wiedergabe:** Visualisiert den 3D-Pfad des Spielers und Sprech-Pulsringe auf einer interaktiven Three.js WebGL-3D-Karte mit Maus-Schwenken, -Zoomen und -Drehen, die vollständig mit der aufgezeichneten Ogg/Opus-Audiodatei synchronisiert ist.

---

## 🎮 Aufschlüsselung der Registerkarte „XuruVoip-Client-Einstellungen“.

Das WPF-Einstellungsfenster ist in sechs Konfigurationskategorien unterteilt:
1. **Allgemein**: Konfigurieren Sie Sprachen, verfolgen Sie „Game.log“-Dateien, schalten Sie die allgemeine Dateiprotokollierung um und aktivieren/konfigurieren Sie den lokalen **Companion App HTTP Server** und Port.
2. **Verbindung**: Bearbeiten Sie die Zielserver-IP, die Position und die Audio-Ports, den Benutzernamen, das Benutzerkennwort und das Serverkennwort.
3. **Position**: Schalten Sie die Standortquelle um („OCR Screen Scanner“ vs. „Game.log Reader (GRTPR)“), konfigurieren Sie Monitorindizes, Zuschneidebereiche, OCR-Intervalle und zeigen Sie eine Vorschau des Live-Koordinatentextes an.
4. **Audio**: Eingabe-/Ausgabe-Hardware auswählen, dB-Verstärkungen anpassen, Übertragungsmodus (PTT vs. VAD) auswählen, VAD-Schwellenwerte konfigurieren, **3D Spatial Audio aktivieren** umschalten, Funkverschlechterung konfigurieren, synthetisierte lokale Glockenspiele, Visiermodulator und **Voice Changer**-Voreinstellungen auswählen.
5. **Hotkeys**: Tasten an Näherungs-PTT, Funk-PTT, Profil-PTT, Helmvisier, Funkkanalzyklus und individuelle Mikrofon- und Audiokanal-Stummschaltschalter binden.
6. **Overlay**: HUD-Overlay ein-/ausschalten, Eckplatzierungen festlegen, das **Taktische Mini-Radar** (mit konfigurierbarer maximaler Reichweite) aktivieren und Echtzeit-**Speech-to-Text-Untertitel** umschalten.

---

## 🖥️ XuruVoip Server (Go)

Der Server koordiniert die Spielerpositionen, übernimmt die sichere Authentifizierung und leitet Audiopakete dynamisch basierend auf räumlicher Entfernung und Funkkanälen weiter.

### Hauptmerkmale

* **Serverseitige Annäherungskontrolle**: Leitet Annäherungsaudio dynamisch nur an Spieler innerhalb der Reichweite weiter (Standardeinstellung 50 m oder Flüstern 5 m).
* **Räumliche Konfiguration**: Umschaltbare serverseitige Option („XURUVOIP_SPATIAL_AUDIO“ in „.env“), die bestimmt, ob Koordinaten oder nur die Entfernung an Clients gesendet werden sollen.
* **Mehrkanal-Radio-Routing**: Ermöglicht Spielern, mehrere Radiokanäle gleichzeitig zu hören, während sie auf ihrem aktiven Kanal senden.
* **Audioprofilsystem**: Weist den Playern Audioeffekte (z. B. Radiofilter, Echo) zu.
* **SQLite-Persistenz**: Speichert Spielerkanalpräferenzen und Profilzuordnungen über Serverneustarts hinweg.
* **Anti-Bypass-Sicherheit**: Sperrt Störenfriede anhand von Benutzername, IP und Hardware-Fingerabdruck (HWID/MachineGuid), um ein Ausweichen zu verhindern.
* **Webverwaltungsportal**: Sichere Webschnittstelle (HTTPS/WebSockets) für Echtzeit-Dashboards, Protokoll-Streaming, Kanal-/Profilkonfiguration und Sperrverwaltung.
* **Server-Admin-Radarkarte**: 2D-HTML5-Canvas-Echtzeit-Spielerradar, das in das Admin-Dashboard integriert ist und das Schwenken durch Klicken und Ziehen, Zoomen mit dem Mausrad, aktive Zonenfilterung, historische Spieler-Laufpfade (Breadcrumbs) und pulsierende konzentrische Live-Schallwellenringe um sprechende Spieler unterstützt.
* **Rotation des Startprotokolls**: Überprüft das Serverprotokoll („xuruvoip.log“) beim Start. Wenn die Protokolldatei Einträge von einem vorherigen Tag enthält, wird sie in „xuruvoip.YYYY-MM-DD.log“ rotiert. Der Server behält nur die fünf zuletzt rotierten Dateien und löscht ältere, um eine übermäßige Festplattennutzung zu verhindern.

### Serverkonfiguration (`.env`)

Beim ersten Start generiert der Server automatisch eine „.env“-Datei mit diesen Standardwerten:```env
# BIND IP address and server ports
# Leave IP empty to listen on all interfaces (0.0.0.0)
XURUVOIP_SERVER_IP=
XURUVOIP_PORT=8888
XURUVOIP_AUDIO_PORT=8889
XURUVOIP_DATA_DIR=.

# Maximum Server Capacity (can be higher, depends on server performances)
XURUVOIP_MAX_PLAYERS=500

# Spatial Audio (1 = enabled and transmits coordinates, 0 = disabled and transmits distance only)
XURUVOIP_SPATIAL_AUDIO=1

# Public Server Settings (1 = players will not need to enter the server password to connect, 0 = required)
XURUVOIP_PUBLIC_SERVER=0

# Server Password / Token for player connections (only if public server is disabled)
XURUVOIP_SERVER_PASSWORD=auto_generated_32_chars_token

# Admin Server Password / Token for the admin portal page (https://[XURUVOIP_SERVER_IP]:[XURUVOIP_PORT]/admin)
XURUVOIP_ADMIN_SERVER_PASSWORD=auto_generated_32_chars_token

# Verbose logging level (0 = none, 1 = default, 2 = global frames per type, 3 = detailed channels/profiles)
XURUVOIP_VERBOSE_LOGS=1

# Security Settings (Rate Limiting and IP Lockout)
XURUVOIP_LIMIT_RATE_POS=50.0
XURUVOIP_LIMIT_BURST_POS=100
XURUVOIP_LIMIT_RATE_AUDIO=60.0
XURUVOIP_LIMIT_BURST_AUDIO=120

XURUVOIP_LOCKOUT_ATTEMPTS=5
XURUVOIP_LOCKOUT_WINDOW=60
XURUVOIP_LOCKOUT_DURATION=600

# Dynamic Intercom and Immersion features (1 = enabled, 0 = disabled)
XURUVOIP_ENABLE_INTERCOM=1
XURUVOIP_ENABLE_EVA_MUTING=1
XURUVOIP_ENABLE_RADIO_REPEATERS=1
XURUVOIP_ENABLE_SHIP_PA=1

# Discord Voice Bridge Settings (1 = enabled, 0 = disabled)
XURUVOIP_ENABLE_DISCORD_BRIDGE=1
XURUVOIP_DISCORD_TOKEN=your_discord_bot_token
XURUVOIP_DISCORD_GUILD_ID=your_discord_guild_id
XURUVOIP_DISCORD_CHANNEL_ID=your_discord_channel_id
XURUVOIP_DISCORD_BRIDGE_CHANNEL=General
```
### 🎛️ Installationsanleitung für Discord Voice Bridge

Um einen lokalen Go-Server-Funkkanal mit einem Discord-Sprachkanal zu verbinden, befolgen Sie diese Einrichtungsschritte:

1. **Erstellen Sie eine Discord-Bot-Anwendung:**
   * Besuchen Sie das [Discord Developer Portal](https://discord.com/developers/applications) und melden Sie sich an.
   * Klicken Sie auf **Neue Anwendung**, geben Sie ihr einen Namen (z. B. „XuruVOIP Bridge“) und klicken Sie auf **Erstellen**.
   * Navigieren Sie zur Registerkarte **Bot** in der linken Seitenleiste, klicken Sie auf **Token zurücksetzen** und kopieren Sie das generierte **Bot-Token**. Fügen Sie dies als „XURUVOIP_DISCORD_TOKEN“ in die „.env“-Datei Ihres Servers ein.
   * Aktivieren Sie unter **Privileged Gateway Intents** auf derselben Bot-Seite den **Message Content Intent** (erforderlich zum Lesen bestimmter Befehle).

2. **Laden Sie den Bot auf Ihren Discord-Server ein:**
   * Gehen Sie zur Registerkarte **OAuth2** und wählen Sie dann **URL-Generator** aus.
   * Aktivieren Sie unter **Umfänge** die Einträge „bot“ und „applications.commands“.
   * Wählen Sie unter **Bot-Berechtigungen** die folgenden Berechtigungen aus:
     * *Allgemeine Berechtigungen:* „Kanäle anzeigen“.
     * *Textberechtigungen:* „Nachrichten senden“.
     * *Sprachberechtigungen:* „Verbinden“, „Sprechen“, „Sprachaktivität verwenden“.
   * Kopieren Sie die generierte URL unten auf der Seite, fügen Sie sie in einen Webbrowser ein, wählen Sie Ihren Ziel-Discord-Server (Gilde) aus und klicken Sie auf **Autorisieren**.

3. **Server- (Gilden-) und Sprachkanal-IDs abrufen:**
   * Öffnen Sie Discord, gehen Sie zu **Benutzereinstellungen** -> **Erweitert** und schalten Sie den **Entwicklermodus** ein.
   * Klicken Sie mit der rechten Maustaste auf das Symbol Ihres Discord-Servers in der Serverliste und wählen Sie **Server-ID kopieren** (dies ist Ihre Gilden-ID). Fügen Sie es als „XURUVOIP_DISCORD_GUILD_ID“ in „.env“ ein.
   * Klicken Sie mit der rechten Maustaste auf den Ziel-Discord-Sprachkanal, dem der Bot beitreten soll, und wählen Sie **Kanal-ID kopieren**. Fügen Sie es als „XURUVOIP_DISCORD_CHANNEL_ID“ in „.env“ ein.

4. **Go-Server-Radiokanal zuordnen:**
   * Konfigurieren Sie „XURUVOIP_DISCORD_BRIDGE_CHANNEL“ auf den genauen Namen des Funkkanals, den Sie überbrücken möchten (z. B. „Allgemein“, „Bravo“, „Alpha“ usw.). Alle auf dieser Go-Server-Funkfrequenz übertragenen Audiodaten werden bidirektional an den Discord Voice Channel gesendet!

### Erstellen des Servers aus dem Quellcode

#### Linux```bash
cd server


GOOS="linux" GOARCH="amd64" go build .
# a "server" linux binary will be created in the current directory
```
#### Windows```powershell
cd server 

$env:GOOS="windows"
$env:GOARCH="amd64"
go build .
# a "server.exe" windows binary will be created in the current directory
```
### Ausführen des Servers

#### Aus Quelle:```bash
cd server
go run .
```
#### Von Binär:
##### Windows```powershell
.\server.exe
```
##### Linux```bash
./server
```
### 🖥️ Einrichtung und Bereitstellung von Headless-Servern

Für permanente, produktionsbereite Headless-Installationen sollte der Server als Hintergrund-System-Daemon/Dienst laufen, der beim Booten automatisch startet und im Falle eines Fehlers neu startet.

#### 1. Netzwerk- und Firewall-Konfiguration
Stellen Sie sicher, dass die in Ihrer „.env“-Datei definierten eingehenden TCP-Ports (Standardwerte sind „8888“ für das Positionen/Admin-Portal und „8889“ für räumliches Audio) auf Ihrer Host-Firewall geöffnet sind:
* **Linux (UFW):**  ```bash
  sudo ufw allow 8888/tcp
  sudo ufw allow 8889/tcp
  sudo ufw reload
  ```
* **Linux (Firewalld):**  ```bash
  sudo firewall-cmd --zone=public --add-port=8888/tcp --permanent
  sudo firewall-cmd --zone=public --add-port=8889/tcp --permanent
  sudo firewall-cmd --reload
  ```
---

#### 2. Linux-Bereitstellung (systemd)

Befolgen Sie diese Schritte, um den Go-Server als systemd-Dienst bereitzustellen:

##### Schritt A: Verzeichnis und Berechtigungen einrichten
Erstellen Sie einen dedizierten Systembenutzer und ein Arbeitsverzeichnis zur Sicherheitsisolierung:```bash
# Create a system user without login privileges
sudo useradd -r -s /bin/false xuruvoip

# Create installation directory and copy the binary
sudo mkdir -p /opt/xuruvoip
sudo cp xuruvoip-server-linux-x64 /opt/xuruvoip/xuruvoip-server
sudo chmod +x /opt/xuruvoip/xuruvoip-server

# Set ownership to the system user
sudo chown -R xuruvoip:xuruvoip /opt/xuruvoip
```
##### Schritt B: „.env“ generieren und konfigurieren
Führen Sie den Server einmal unter dem Systembenutzer aus, um die Standardkonfigurationsdatei und -datenbank „.env“ zu generieren:```bash
sudo -u xuruvoip /opt/xuruvoip/xuruvoip-server -port 8888 -audio-port 8889
```
*Drücken Sie „Strg+C“, nachdem die Konsole die generierten Passwörter gedruckt hat.* Bearbeiten Sie dann die generierte „.env“-Datei, um Einstellungen anzupassen (z. B. Passwörter, Bindungs-IP, räumliche Audioumschaltung):```bash
sudo nano /opt/xuruvoip/.env
```
##### Schritt C: Erstellen Sie die systemd-Dienstdatei
Kopieren Sie die Dienstdatei aus dem Repo „server/xuruvoip.service“ nach „/etc/systemd/system/xuruvoip-server.service“ oder erstellen Sie eine neue Dienstkonfigurationsdatei „/etc/systemd/system/xuruvoip-server.service“ mit folgendem Inhalt:```ini
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
##### Schritt D: Aktivieren und starten Sie den Dienst```bash
# Reload systemd daemon to pick up the new unit file
sudo systemctl daemon-reload

# Enable the service to run on startup
sudo systemctl enable xuruvoip-server

# Start the service immediately
sudo systemctl start xuruvoip-server
```
##### Schritt E: Überwachen und Protokollieren
So überprüfen Sie den Dienststatus und die Stream-Protokolle:```bash
# Check status
sudo systemctl status xuruvoip-server

# Stream log files in real-time
journalctl -u xuruvoip-server -f -n 100
```
---

#### 3. Windows-Bereitstellung (NSSM)

Um den Server als nativen Windows-Dienst im Headless-Modus auszuführen, wird die Verwendung des **Non-Sucking Service Manager (NSSM)** empfohlen:

##### Schritt A: Verzeichnisse einrichten
Extrahieren/kopieren Sie „xuruvoip-server-windows-x64.exe“ in einen dedizierten Serverordner (z. B. „C:\XuruVoipServer“).

##### Schritt B: Konfiguration initialisieren
Öffnen Sie ein PowerShell-Terminal als Administrator und führen Sie die Binärdatei einmal aus, um Dateien zu generieren:```powershell
cd C:\XuruVoipServer
.\xuruvoip-server-windows-x64.exe
```
*Drücken Sie „Strg+C“, sobald der Startvorgang abgeschlossen ist.* Passen Sie die generierte „.env“-Datei nach Bedarf an.

##### Schritt C: Installieren Sie den Dienst über NSSM
Laden Sie NSSM herunter und installieren Sie den Dienst, indem Sie Folgendes ausführen:```powershell
# Open NSSM GUI installer
.\nssm.exe install XuruVoipServer "C:\XuruVoipServer\xuruvoip-server-windows-x64.exe"
```
Konfigurieren Sie im NSSM-Popup Folgendes:
* **Pfad:** `C:\XuruVoipServer\xuruvoip-server-windows-x64.exe`
* **Startverzeichnis:** `C:\XuruVoipServer`
* Klicken Sie auf **Dienst installieren**.

##### Schritt D: Starten Sie den Dienst
Starten Sie den Dienst mit PowerShell oder Services Manager („services.msc“):```powershell
Start-Service -Name XuruVoipServer
```
---

### Erstellen und Ausführen des Clients

#### Anforderungen
- Windows 10/11
- .NET 9.0 SDK (WPF-Unterstützung)

#### Kompilieren und ausführen:```powershell
cd client
dotnet run
```
### Installation des Release-Pakets

Da das Installationsprogramm und die ausführbaren Dateien nicht digital signiert sind, werden sie möglicherweise zunächst von Windows SmartScreen blockiert. Sie können sie ganz einfach über das Eigenschaftenmenü entsperren.

* **Option A: Windows Package Manager (winget) - (Empfohlen)**
  1. Öffnen Sie ein Terminal (PowerShell oder Eingabeaufforderung).
  2. Führen Sie den folgenden Befehl aus, um den Client zu installieren:
     ```powershell
     winget install XuruDragon.XuruVOIPClient
     ```

* **Option B: MSI-Installer**
  1. Laden Sie „XuruVoipClient-win-x64.msi“ von der [Release-Seite](https://github.com/XuruDragon/XuruVOIP/releases) herunter.
  2. Um zu verhindern, dass Windows SmartScreen die Installation blockiert:
     - Klicken Sie mit der rechten Maustaste auf die heruntergeladene Datei „XuruVoipClient-win-x64.msi“ und wählen Sie **Eigenschaften**.
     - Aktivieren Sie im Eigenschaftenfenster auf der Registerkarte *Allgemein* unten das Kontrollkästchen **Blockierung aufheben**.
     - Klicken Sie auf **Übernehmen** und schließen Sie dann das Eigenschaftenfenster.
  3. Doppelklicken Sie auf die Datei, um das Installationsprogramm auszuführen, und befolgen Sie die Anweisungen der Eingabeaufforderung.
     *(Hinweis: Es wird die Standardaufforderung „Unbekannter Herausgeber“ der Windows-Benutzerkontensteuerung angezeigt; klicken Sie einfach auf **Ja** oder **Ausführen**, um fortzufahren.)*

* **Option C: Portable ZIP-Version**
  1. Laden Sie „XuruVoipClient-win-x64.zip“ von der [Release-Seite](https://github.com/XuruDragon/XuruVOIP/releases) herunter.
  2. Extrahieren Sie die Dateien im ZIP-Paket in einen beliebigen Ordner Ihrer Wahl (z. B. „C:\Games\XuruVoip“):
  3. Klicken Sie dann mit der rechten Maustaste auf die extrahierte Datei „XuruVoipClient.exe“ und wählen Sie **Eigenschaften**.
     - Aktivieren Sie im Eigenschaftenfenster auf der Registerkarte *Allgemein* unten das Kontrollkästchen **Blockierung aufheben**.
     - Klicken Sie auf **Übernehmen** und schließen Sie dann das Eigenschaftenfenster.
  4. Doppelklicken Sie auf „XuruVoipClient.exe“, um den Client direkt auszuführen, ohne ihn zu installieren.

## 📱 Companion App & Stream Deck-Integration

XuruVOIP umfasst einen integrierten Companion-App-Webdienst und ein offizielles Stream Deck-Plugin, mit dem Sie Sprachaktionen direkt von sekundären Geräten oder physischen Tasten aus überwachen und auslösen können.

### 1. Aktivieren der Companion-App & MFD für die taktische Karte
Standardmäßig sind der lokale HTTP-Server der Companion App und der Modus für die taktische Karte deaktiviert, um Systemressourcen zu sparen. Um sie zu aktivieren:
1. Öffnen Sie den XURUVOIP-Client und klicken Sie auf das Symbol **Einstellungen**.
2. Aktivieren Sie auf der Registerkarte **Allgemein** das Kontrollkästchen **Companion HTTP Server aktivieren** (Standardport: `8891`).
3. Um das Radar-Display zu aktivieren, aktivieren Sie das untergeordnete Kontrollkästchen **Taktische Co-Pilot-Karte (MFD) aktivieren**.
4. Klicken Sie zum Anwenden auf **Speichern und schließen**.
5. Zugriff auf das Dashboard: Sie können „http://localhost:8891“ in jedem Browser auf Ihrem PC, Tablet oder Mobiltelefon öffnen. Wenn der Kartenmodus aktiviert ist, steht eine neue Registerkarte **🗺️ Taktische Karte** zur Verfügung. Diese zeigt einen Canvas-basierten HUD-Radarbildschirm an, der die Echtzeitposition Ihres Charakters, die Ausrichtung, Besatzungskontakte in derselben Zone und Anzeigen für aktive Sprecher verfolgt.

---

### 2. Installation des Stream Deck Plugins
Das Release-Paket enthält die vorgefertigte Datei „.streamDeckPlugin“.
1. Laden Sie „com.xuru.voip.streamDeckPlugin“ von der [Release-Seite](https://github.com/XuruDragon/XuruVOIP/releases) herunter.
2. Doppelklicken Sie auf die Datei, um sie direkt in Ihrer Elgato Stream Deck-Software zu installieren. 
   *(Alternativ können Sie den Ordner „com.xuru.voip.sdPlugin“ manuell extrahieren und nach „%appdata%\Elgato\StreamDeck\Plugins\“ kopieren)*
3. Nach der Installation erscheint eine neue Aktionskategorie namens **XuruVOIP** in der rechten Liste Ihrer Stream Deck-Desktop-App.

---

### 3. Aktionen hinzufügen und konfigurieren
Sie können jede der folgenden 19 Aktionen per Drag & Drop auf Ihre Stream Deck-Tasten ziehen:
* 🎤 **Proximity Mute**: Schaltet die Stummschaltung des ausgehenden Näherungsmikrofons um.
* 📻 **Radio-Stummschaltung**: Schaltet die Stummschaltung des ausgehenden Funkmikrofons um.
* 👤 **Profil-Stummschaltung**: Schaltet die Stummschaltung des ausgehenden Profilmikrofons um.
* 🔊 **Audio Proximity Mute**: Schaltet die Stummschaltung eingehender Proximity-Wiedergabe um.
* 🔊 **Audio-Radio-Stummschaltung**: Schaltet die Stummschaltung der eingehenden Radiowiedergabe um.
* 🔊 **Audioprofil-Stummschaltung**: Schaltet die Stummschaltung der eingehenden Profilwiedergabe um.
* 🪖 **Helm umschalten**: Schaltet das Visier Ihres Raumanzug-Helms nach unten oder oben um.
* 🔄 **Radio wechseln**: Wechselt durch die verfügbaren Radiokanäle.
* 📢 **PA Broadcast**: Push-to-Talk-Taste für das schiffsweite Durchsagesystem (Public Address - PA).
* 📡 **Beacon Mode**: Schaltet den Funkrelais- / Bakenmodus um.
* 🎙️ **Voice Command Macro**: Löst ein benutzerdefiniertes Sprachbefehl-Makro aus, das im Hintergrund simuliert wird (über Einstellungen konfigurierbar).
* 💬 **Intercom Status**: Zeigt den Schiffs-Intercom-Status an (`NORMAL`, `SHIELD HIT`, `CRIT PWR`, `QUANTUM`) und schaltet Simulationszustände durch Drücken der Taste durch.
* 🗺️ **Location Telemetry**: Zeigt die aktuelle Systemzone und die Koordinatendaten $(X, Y, Z)$ auf der Taste an.
* 📞 **Initiate Hail**: Initiiert einen Schiff-zu-Schiff-Anruf zum nächsten Spieler.
* 📞 **Accept/Answer Hail**: Nimmt einen eingehenden Hailing-Anruf an.
* 📞 **Decline/End Hail**: Lehnt einen eingehenden Anruf ab oder beendet ein aktives Gespräch.
* 🔤 **Toggle Translation**: Schaltet die Echtzeit-HUD-Übersetzungsuntertitel ein oder aus.
* 🎧 **Toggle HRTF**: Schaltet die Echtzeit-HRTF-Räumliche Audio-Wiedergabe um.
* 📊 **Toggle Spectrogram**: Schaltet das Echtzeit-Visier-HUD-3D-Spektrogramm um.

#### Konfiguration (Eigenschaftsinspektor):
Klicken Sie für jede Aktion, die Sie auf eine Taste ziehen, darauf und konfigurieren Sie die Einstellungen im Bedienfeld **Eigenschafteninspektor** unten:
* **Companion Port**: Stellen Sie diesen so ein, dass er mit dem in Ihren WPF-Client-Einstellungen konfigurierten Port übereinstimmt (Standard: `8891`).
* **Voice Command** (Nur für Voice Command Macro): Geben Sie den auszuführenden Textbefehl ein (z. B. `"close visor"`, `"open hangar"`).
* **Dynamisches Feedback**: Aktionen aktualisieren ihre Symbole und Zustände in Echtzeit. Umschalter zeigen Cyan/Rot, Intercom Status durchläuft 4 Zustände und Location Telemetry zeigt Koordinaten an.
* **Live-Frequenzanzeige**: Die Taste **Cycle Radio** zeigt dynamisch und in Echtzeit den aktuell aktiven Frequenznamen direkt auf der physischen Taste an!

---

## 👥 Credits

Entwickelt von **[@XuruDragon](https://github.com/XuruDragon)** in Zusammenarbeit mit **Antigravity IDE**.