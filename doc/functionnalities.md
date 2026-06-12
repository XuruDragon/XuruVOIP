# XuruVOIP Features & Functionalities

Welcome to the comprehensive features and functionalities guide for XuruVOIP. This document provides non-technical and technical overviews of all systems designed to bring an unprecedented level of audio immersion and tactical convenience to **Star Citizen** operations.

Go back to <a href="../README.md">Readme file</a>

---

## 🗺️ Quick Navigation

Use the summary table below to navigate between the various features implemented in the XuruVOIP suite.

| Feature Number | Feature Name | Primary Impact | Configuration Tab |
| :---: | :--- | :--- | :--- |
| **1** | [🔊 Real-Time 3D Spatial Audio](#1-real-time-3d-spatial-audio) | Directional Voice Tracking | Audio / Position |
| **2** | [🏢 Location-Aware Acoustics & Occlusion](#2-location-aware-acoustics--shipbunker-occlusion) | Ship/Room Audio Muffles | *Automatic (Zone)* |
| **3** | [💨 Helmet & EVA Atmospheric Simulation](#3-helmet--eva-atmospheric-simulation) | Vacuum Muting & Suit Breathing | Hotkeys / General |
| **4** | [👽 Sci-Fi Voice Changer & Modulators](#4-sci-fi-voice-changer--suit-modulators) | Robot/Alien voice FX | Audio / Companion |
| **5** | [📻 Immersive Radio Degradation & Chimes](#5-immersive-radio-degradation--chimes) | Authentic Radio Static & Chirps | Audio |
| **6** | [💬 Automatic Ship Intercom System](#6-automatic-ship-intercom-system) | Fleet Comms & Pilot Priority | General |
| **7** | [📡 Vulkan-Compatible HUD Overlay & Radar](#7-vulkan-compatible-hud-overlay--2d-tactical-radar) | Real-Time UI telemetry | Overlay |
| **8** | [🎙️ Hands-Free PTT Voice Commands](#8-hands-free-ptt-voice-commands) | Offline Speech Ship control | General / Hotkeys |
| **9** | [📱 Companion App & REST API](#9-companion-app--rest-api) | Mobile control dashboard | General |
| **10** | [🎛️ Stream Deck Plugin](#10-stream-deck-plugin) | Physical key bindings & MFD titles | *External Software* |
| **11** | [🔌 Discord Voice Bridge](#11-discord-voice-bridge) | Out-of-game Discord relay | *Server Setup (.env)* |
| **12** | [🤢 G-Force & Exertion Voice Distortion](#12-g-force--physical-exertion-voice-distortion) | Breathiness under G-force strain | Settings / General |
| **13** | [📡 Tactical Radio Relay & Beacons](#13-tactical-radio-relay--multi-hop-repeater-beacons) | Long-range multi-hop daisy-chains | Settings |
| **14** | [📢 Ship Public Address (PA) Broadcast](#14-ship-public-address-pa-broadcast-system) | Fleet-wide alert announcements | Hotkeys |
| **15** | [🔌 External Hardware UDP Telemetry](#15-external-hardware-telemetry-sim-pit-udp-sync) | Sim-pit hardware integration | General |
| **16** | [🛡️ Security, Log Rotation & Admin Radar](#16-security-log-rotation--admin-canvas-radar) | Server moderation & zoomable radar | *Server Setup (.env)* |
| **17** | [🪐 Planetary Atmosphere Simulation](#17-planetary-atmosphere-density-simulation) | Client-side muffling & range scaling | Audio |
| **18** | [🎙️ Post-Op Voice Recorder & AAR Portal](#18-post-op-voice-recorder--aar-portal) | Server-side Ogg/Opus recording & admin timeline | *Server Setup (.env)* |
| **19** | [📞 Ship-to-Ship Hailing & Calling](#19-ship-to-ship-hailing--calling-system) | Cockpit-to-cockpit private calls | Hotkeys |
| **20** | [🔤 Visor HUD Real-Time Translation Subtitles](#20-visor-hud-real-time-translation-subtitles) | Real-time speech translation on HUD | Overlay |
| **21** | [🎧 Binaural HRTF Spatial Audio](#21-binaural-hrtf-spatial-audio) | Physical human hearing simulation | Audio |
| **22** | [📊 Visor HUD 3D Spectrogram](#22-visor-hud-3d-spectrogram) | Real-time speaker frequency overlay | Overlay |
| **23** | [🎙️ Voice-Activated Ship Controls](#23-voice-activated-ship-controls) | Simulated keystrokes via speech | Hotkeys / Settings |
| **24** | [🛰️ Server-Side AAR 3D Playback](#24-server-side-aar-3d-playback) | Synchronized path replay Canvas | *Web Portal* |

---

## 🔊 1. Real-Time 3D Spatial Audio

### Description
Calculates the spatial relation between players and dynamically applies stereo panning and distance-based volume attenuation. This makes other players sound like they are speaking from their actual in-game locations relative to you.

### How It Works
* **Dynamic Stereo Panning:** The client takes the remote speaker's 3D coordinate vector $(X, Y, Z)$ and projects the delta relative to the local listener's position onto the listener's Forward and Right direction vectors. A constant-power panning formula is then applied to calculate precise left/right speaker gains.
* **Front-Back Ambiguity Resolution:** Traditional stereo panning makes it difficult to differentiate if a sound is directly in front of or directly behind you. To resolve this, XuruVOIP attenuates the volume by 25% if the remote player is standing behind you.
* **Distance Roll-Off:** Volume decays linearly as distance increases. Proximity communication fades out completely at 50 meters, while whispers fade to zero at 5 meters.

### How to Use
1. Open the Client **Settings** window.
2. Under the **Audio** tab, ensure **Enable 3D Spatial Audio** is checked.
3. Coordinate tracking must be active via **OCR Screen Scanner** or **Game.log Reader (GRTPR)** in the **Position** tab.

### Why It's Good to Have
It eliminates standard flat VoIP chat limitations. Instead of guessing who is talking, you can immediately identify the location of a calling squadmate by sound alone—whether they are on your left, behind a crate, or calling from the upper deck.

---

## 🏢 2. Location-Aware Acoustics & Ship/Bunker Occlusion

### Description
Modulates sound waves as they pass through ship hulls, bulkheads, or cave passages. It dynamically dampens and adds environmental reverb depending on where players are standing.

### How It Works
* **Deck & Wall Occlusion:** When players are in the same vehicle zone but on different decks (e.g. inside a *Carrack* or *Hercules*) or separated by bunker walls, a low-pass filter (cutoff frequency between 300Hz and 900Hz) is applied to their voice. The volume is also dampened to simulate acoustic absorption by structural materials.
* **Environmental Reverb:** Reads the player's current location zone (e.g., Caves, Bunkers, Hangars) and passes the audio through NAudio DSP delay lines. Custom wet-mix levels, feedback, and decay delays are automatically adjusted to match room sizes.

### How to Use
* **No Manual Trigger Needed:** This feature is fully automated once position/zone tracking is active.

### Why It's Good to Have
It provides realistic sound boundaries. You won't hear crew members on the lower deck as if they are standing right next to you; their voices will sound naturally muffled and localized. Similarly, shouting in a cave will produce deep echoes, enhancing the immersion of ground exploration.

---

## 💨 3. Helmet & EVA Atmospheric Simulation

### Description
Replicates atmospheric sound constraints. Sound cannot travel through the vacuum of space, and wearing a pressurized space suit helmet alters the sound of your own breathing and incoming audio.

### How It Works
* **EVA Muting:** If the system detects a player is in vacuum/space (EVA), proximity voice chat is automatically muted. Players must use radio channels to communicate.
* **Visor Respirator Overlay:** When the helmet visor is down, the C# client synthesizes a low-frequency breathing sound whoosh and overlays a continuous dual-frequency suit ventilator fan hum (50Hz + 100Hz harmonics) on the mic capture channel.
* **Visor Sync:** The client tails the Star Citizen `Game.log` file, parsing attachment logs to automatically detect when a helmet is equipped or unequipped.

### How to Use
* Go to the **Hotkeys** tab in Settings to bind a key for **Toggle Helmet** (defaults to `H`). You can toggle your visor manually, or let `Game.log` sync it automatically.

### Why It's Good to Have
It enforces space survival mechanics. Forgetting to switch to radio before stepping into the vacuum of space means your squad won't hear your warnings. The faint, persistent hum of the visor fan also adds a claustrophobic space-suit feel.

---

## 👽 4. Sci-Fi Voice Changer & Suit Modulators

### Description
Applies real-time vocal modulations to voice feeds, allowing players to sound like robots, cyborgs, or alien species.

### How It Works
* Uses time-domain digital signal processing (DSP) chains:
  * **Pitch Shifting:** Formant-preserving resampling from 0.5x (deep alien) to 2.0x (high-pitched).
  * **Ring Modulation:** Multiplies the audio carrier signal by a sine wave to create metallic, robotic tones.
  * **Bitcrushing:** Reduces sample depth to 8-bit or less to simulate low-fidelity synthesized voice lines.
  * **Flanger & Distortion:** Applies micro-delays and hyperbolic tangent (`tanh`) soft clipping for grit.

### How to Use
1. Go to the **Audio** tab in Settings.
2. Select an active preset under **Voice Changer**: **Alien**, **Cyborg**, or **Robotic**.
3. You can also toggle and adjust settings in real-time via the Companion App dashboard or Stream Deck keys.

### Why It's Good to Have
Perfect for roleplay organizations, hostile alien scenarios, or simply adding a unique audio signature to players operating heavy mechanized power suits.

---

## 📻 5. Immersive Radio Degradation & Chimes

### Description
Simulates analog radio transmissions with bandpass filters, static noise, and mechanical chimes.

### How It Works
* **Radio Bandpass:** Incoming radio packets are restricted to a communication frequency band (e.g. 400Hz to 3400Hz) to replicate radio hardware.
* **Signal Degradation:** Calculates distance between the speaker and listener. As they approach the transmitter's maximum range, the bandpass limits narrow and static white noise is dynamically blended in.
* **PTT Chimes & Squelch Tail:** Triggers mechanical key-down and key-up chimes when transmitting on radio channels. Supports four distinct mathematical profiles selectable in settings or the Companion App:
  * **Military:** Clean, authentic sine sweeps (900Hz to 700Hz key-down, 3.5ms squelch tail).
  * **Industrial:** Heavy mechanical clanks (metallic frequency modulations and resonant bandpassed noise).
  * **Alien:** Harmonic ring-modulated sweeps simulating bio-organic neural link clicks.
  * **Vintage:** Distorted, low-fidelity analog relay clicks with slow decay.

### How to Use
* Set a key for **Radio PTT** in the **Hotkeys** tab and communicate on an active radio channel.

### Why It's Good to Have
Brings military-grade communication realism. The audio cues (chimes and squelch) provide positive feedback that a transmission has started or ended, preventing overlapping communication.

---

## 💬 6. Automatic Ship Intercom System

### Description
Automatically groups ship crews into private, localized intercom channels that reflect the ship's physical status (e.g., taking damage or losing power).

### How It Works
* **Auto-Subscription:** Boarding a vehicle zone automatically subscribes players to a private `Intercom_<ContainerID>` radio channel.
* **Pilot Priority Ducking:** If a player sitting in a cockpit or pilot/driver seat transmits on the intercom, the proximity playback of all other crew members is ducked by 85%.
* **Dynamic Degradation:** Treads state logs in `Game.log` to apply DSP filters:
  * **Shield Hits (Static Burst):** Blends in sharp, random voltage spikes and white noise (lasts 2.5 seconds).
  * **Critical Power (Power Loss):** Heavy soft-clipping saturation, pitch resampled down to 0.78x, and a 60Hz AC hum.
  * **Quantum Travel (Quantum Wave):** Flanger/phaser sweep via an LFO and an 1800Hz resonant whine.
* **Cleanup Cooldown:** Unused intercom channels are deleted by the Go server 5 minutes after the last player disembarks.

### How to Use
* Enable **Enable Intercom Degradation (Ship Damage)** in Settings -> General.
* You can individually toggle **Shield Hits**, **Critical Power**, and **Quantum Travel** sub-effects.

### Why It's Good to Have
Keeps bridge crew communication organized without manual radio switching. During combat, pilot commands cut through chatter automatically. Hearing the ship intercom crackle as shield hits land or whine as the quantum drive spools adds unmatched mechanical feedback.

---

## 📡 7. Vulkan-Compatible HUD Overlay & 2D Tactical Radar

### Description
An in-game HUD overlay displaying active channels, speaker states, real-time speech-to-text subtitles, and a 2D tactical radar.

### How It Works
* **Win32 Click-Through Overlay:** A borderless overlay window positioned over the game. It uses low-level Win32 window flags (`WS_EX_TRANSPARENT` and `WS_EX_NOACTIVATE`) to remain clickable-through.
* **Interactive HUD Customizer:** Allows real-time theme, positioning, and component visibility customization via the client settings or companion app:
  * **Themes:** Selectable color schemes matching manufacturer aesthetics: Aegis (Cyan), Anvil (Orange), Drake (Green), RSI (Light Blue), and Origin (Magenta).
  * **Positioning:** Instantly align the HUD panel to any screen corner or center (Top Left, Top Center, Top Right, Bottom Left, Bottom Center, Bottom Right).
  * **Visibility Toggles:** Independent control to show/hide the mini-radar, active speakers list, or the connection channel header.
* **Tactical Mini-Radar:** Resolves the player's heading and relative speaker coordinates to render a 2D radar overlay with pulsating rings representing voice activity.
* **Speech-to-Text Subtitles:** Decoded incoming audio packets are sent to a background thread running a lightweight, offline Whisper model (`ggml-tiny.bin`) to generate real-time HUD subtitles.

### How to Use
* Configure this in the **Overlay** tab in Settings. Set the HUD placement (e.g., Bottom Center, Top Left) and adjust the maximum radar scale.

### Why It's Good to Have
Allows players to keep track of communications visually without minimizing the game client. Real-time subtitles are invaluable for hearing-impaired players or during chaotic combat operations when audio is cluttered with explosions.

---

## 🎙️ 8. Hands-Free PTT Voice Commands

### Description
Allows players to control client and ship states by speaking commands directly into an offline voice recognizer without other players hearing them.

### How It Works
* **Press to Listen:** Holding the dedicated hotkey suppresses all outgoing proximity and radio audio, and records local microphone input at 16kHz.
* **Speech Processing:** Upon release, the buffered samples are transcribed offline using the Whisper model.
* **Localized Match Dictionaries:** Supports English, French, German, Spanish, Portuguese, Japanese, and Chinese.
* **Action Routing:** Compares match ratios against a confidence threshold to trigger actions (Visor toggle, Mic mute, Active radio channel selection, Voice changer preset).
* **HUD Banners:** Displays status banners (`AEGIS LISTENING...`, `CMD: Visor Toggle`, or `CMD NOT RECOGNIZED`).

### How to Use
1. In General Settings, check **Enable Voice Commands (Hands-Free PTT)**.
   *(Note: This downloads the 140MB Whisper model on first activation).*
2. Bind a key to **Voice Command Key (PTT)** in Hotkeys (default is `V`).
3. Hold the key and speak a command (e.g., *"Computer, toggle visor"*, German: *"Kanal auf Alpha"*), then release the key.

### Why It's Good to Have
Provides hands-free utility. You can quickly switch radio frequencies, toggle your helmet, or mute communications silently without using complex keyboard bindings or interrupting active squad communications.

---

## 📱 9. Companion App & REST API

### Description
Hosts a local web server displaying a glassmorphic dashboard to monitor status or trigger client actions remotely.

### How It Works
* **HTTP Web Server:** The client spins up a local HTTP listener on a configurable port (default: `8891`).
* **MFD Radar Mode:** Hosts a canvas-based HUD radar screen tracking position, heading, same-zone crew contacts, and speaker activity.
* **REST API:** Exposes endpoints:
  * `GET /api/status`: Returns current client configuration and intercom states.
  * `POST /api/action`: Triggers manual overrides (like simulated ship failures, voice commands, or mute toggles).

### How to Use
* Check **Enable Companion HTTP Server** in General Settings. Open `http://localhost:8891` in a browser on any local device.

### Why It's Good to Have
Enables secondary screens (tablets or smartphones) to serve as physical ship MFDs or co-pilot radar maps. Also provides integration points for external developers.

---

## 🎛️ 10. Stream Deck Plugin

### Description
Integrates physical Stream Deck buttons to display status, cycle radio frequencies, trigger voice command macros, broadcast on ship PA, toggle beacon relays, cycle intercom simulation states, and display live GPS telemetry.

### How It Works
* Connects via WebSockets to the client's Companion App REST API.
* **Dynamic Multi-State Actions:**
  * **Mute Buttons:** Toggles update icons in real-time (cyan glow for active, red strike-through for muted).
  * **PA Broadcast:** Functions as a Push-to-Talk (PTT) key. Holding it triggers `/api/action` with `start_pa`, and releasing it triggers `stop_pa`. The icon glows active cyan during broadcast.
  * **Beacon Mode:** Toggles ship beacon relay. Displays cyan when transmitting, and dim grey/red when disabled.
  * **Voice Command Macro:** Simulates voice commands headlessly. Users configure a text string (e.g., `"close visor"`, `"open cargo bay"`) in the Property Inspector. When pressed, the command executes. Shows a listening indicator if voice recognition is active.
  * **Intercom Status:** A 4-state action displaying the ship intercom status (`NORMAL`, `SHIELD HIT`, `CRIT PWR`, `QUANTUM`). Pressing the key cycles through these states in the simulation.
  * **Location Telemetry:** A read-only MFD button displaying the current system zone and player coordinates $(X, Y, Z)$ dynamically formatted with newline characters:
    ```
    [Zone]
    X: [value]
    Y: [value]
    Z: [value]
    ```
  * **Live Frequency Titles:** The "Cycle Radio" button displays the active channel name directly on the key in real-time.

### How to Use
1. Install the `com.xurudragon.xuruvoip.sdPlugin` in the Stream Deck app.
2. Drag and drop any of the 13 available actions onto your Stream Deck keys.
3. Configure the **Companion Port** (default: `8891`) in the key settings to match your client.
4. For the **Voice Command Macro** action, enter the custom text command you want to trigger (e.g., `"open hangar"`).

### Why It's Good to Have
Allows hands-on physical access to communication controls and essential ship telemetry. You can control your comms, trigger voice macros, and check your space coordinates at a glance without having to open overlays or look away from your flight stick.

---

## 🔌 11. Discord Voice Bridge

### Description
Connects community members on Discord directly with in-game tactical channels.

### How It Works
* Integrates a Discord bot client inside the Go server.
* Relays audio bidirectionally between the Go server radio channel and a designated Discord voice channel.
* Maps Discord SSRC audio packets to Go server player nicknames.

### How to Use
* Configure the bot credentials, server ID, and target channel name inside the server's `.env` configuration file.

### Why It's Good to Have
Allows coordinators, command staffs, or community members to participate in operations directly from Discord without running the Star Citizen game client.

---

## 🤢 12. G-Force & Exertion Voice Distortion

### Description
Modulates outgoing voice and overlays breathing audio based on in-game physical stress.

### How It Works
* **G-Force Distortion:** Under high G-forces (high accelerations, blackouts), the client applies a tremolo LFO (4-10Hz, up to 40% depth) and pitches down voice feeds (down to 0.85x).
* **Exertion Heavy Breathing:** Reads player stamina levels from `Game.log`. If stamina is low, the client overlays randomized panting/breathing audio, scaling respiration speed dynamically.

### How to Use
* Enable G-Force tracking under Settings. The system reads telemetry from `Game.log` automatically.

### Why It's Good to Have
Brings physical strain to life. Combat maneuvers or sprint runs will make your character sound exhausted or strained, adding dramatic realism to flight dogfights.

---

## 📡 13. Tactical Radio Relay & Multi-Hop Repeater Beacons

### Description
Enables long-range communications over planetary scales by routing voice traffic through players acting as repeater beacons.

### How It Works
* **Repeater Beacon Mode:** Active beacons announce their positions to the Go server.
* **Dijkstra's Shortest Path:** If two players are beyond direct radio range (1500m), the receiving client calculates the shortest path over active repeaters (up to an 8000m total limit).
* **Signal Quality Degradation:** The voice is routed through the path and degraded based on the worst quality hop in the chain.

### How to Use
* Toggle **Beacon Mode** via Settings or the Companion App.

### Why It's Good to Have
Allows organizing communication chains across large operational zones. A player on a high-altitude ship can act as a relay, connecting ground teams in deep canyons to commanders in orbit.

---

## 📢 14. Ship Public Address (PA) Broadcast System

### Description
Enables captains or pilots to make ship-wide announcements that bypass local proximity and radio mutes.

### How It Works
* Relays audio to all crew members sharing the same `ContainerID` (ship) in the same Zone.
* Applies a megaphone filter (high-pass/low-pass) and reverberation to simulate ship corridor acoustics.
* Prepend a dual-tone Sci-Fi chime/klaxon alert before the audio plays.

### How to Use
* Bind a hotkey for the **PA Broadcast** and transmit.

### Why It's Good to Have
Allows captains to issue orders (e.g. "Prepare for jump," "Abandon ship") that are clearly heard by all crew members on board, regardless of local channel selections.

---

## 🔌 15. External Hardware Telemetry (Sim-Pit UDP Sync)

### Description
Broadcasts client VoIP and helmet states in JSON over UDP to integrate custom sim-pit hardware.

### How It Works
* Broadcasts JSON payloads (containing transmission states, visor status, active channel) to `127.0.0.1:8895` every 100ms.

### How to Use
* Enable **External Telemetry Sync** in General Settings and configure the target port.

### Why It's Good to Have
Enables sim-pit builders to connect physical LEDs, warning lights, or analog meters that react to active communication states.

---

## 🛡️ 16. Security, Log Rotation & Admin Canvas Radar

### Description
Provides server administrators with tools for monitoring, log rotation, and real-time visual tracking of players.

### How It Works
* **Daily Log Rotation:** Rotates `xuruvoip.log` on startup if it contains entries from a previous day. Retains only the 5 most recent logs.
* **Admin Radar Map:** Integrates an interactive 2D HTML5 Canvas radar into the web portal, displaying active zones, speaking indicators, and player walking trails (breadcrumbs) with zoom and pan controls.
* **Anti-Bypass Security:** Implements lockout mechanisms and bans players by Username, IP, and hardware fingerprint (HWID/MachineGuid).

### How to Use
* Access the Admin Web Portal on `https://[Server_IP]:[Server_Port]/admin` using your secure admin password.

### Why It's Good to Have
Essential for large gaming events or public servers, allowing admins to moderate, track player activity, and manage server health securely.

---

## 🪐 17. Planetary Atmosphere Density Simulation

### Description
Simulates real-time voice range scaling and frequency dampening depending on the local atmospheric density of the moon or planet.

### How It Works
* **Volume Range Scaling:** Based on the listener's planetary zone, range is scaled by an atmospheric multiplier:
  * Moon with trace/very thin atmosphere (e.g., Cellin, Ita): `3.5` (very rapid volume decay, sound dies quickly).
  * Moon with thin atmosphere (e.g., Daymar, Yela, Lyria): `2.6` or `2.1` (moderate decay).
  * Standard planet (e.g., MicroTech, Hurston, ArcCorp): `1.0` (standard decay).
  * Dense gas atmosphere (e.g., Crusader, Arial): `0.75` (sound travels further).
* **Gas Muffling (Low-Pass Filtering):** Applies low-pass filters to outdoor moon environments to model thin gas sound transmission constraints (e.g., Cellin outdoor zone applies an 800Hz low-pass cutoff).
* **Pressurized Bypasses:** The simulation is bypassed entirely when players disembark their spacesuits or stand inside pressurized environments, such as facilities, outposts, hangars, and ship cabins.

### How to Use
1. Open the Client **Settings** window.
2. In the **Audio** tab, check **Enable Planetary Atmosphere Simulation (Muffling & Range)**.

### Why It's Good to Have
Reinforces realism during ground operations. Squad members on airless moons will sound muffled and will need to stand closer to each other to communicate via proximity, while voices inside pressurized ships remain clear and standard.

---

## 🎙️ 18. Post-Op Voice Recorder & AAR Portal

### Description
An administrative voice recording suite that writes direct Opus frames to browser-playable Ogg container files, integrated with a timeline view on the admin dashboard.

### How It Works
* **Zero-Overhead Ogg/Opus Writer:** Saves incoming Opus voice packets directly into standard Ogg page files with correct CRC-32 checksums, requiring **zero server transcoding CPU load** and saving 5x more disk space than MP3 (~12 MB/hour of audio).
* **Target-Based Activation:** Does not record everything automatically. Admins specify exactly which targets (Proximity chat, specific Radio Channels, or Audio Profiles) are actively recorded.
* **PTT-Framed Sessions:** Instantiates a new recording when a player begins transmitting, and automatically closes/commits the segment to the database on PTT release or timeout.
* **Canvas Timeline & Portal:** Displays an interactive canvas timeline of speaking periods. Admins can click on voice blocks directly on the timeline canvas to trigger a floating audio player and play back segments, or delete them.

### How to Use
1. Enable `XURUVOIP_ENABLE_AAR_RECORDING=1` in the server's `.env` config file.
2. Open the Admin Web Portal and navigate to the **Archives** tab.
3. Check target boxes to enable active recording (e.g., Proximity or a specific Radio Channel).
4. View recorded clips and speak blocks dynamically plotted on the Canvas Timeline. Click on them to play or review.

---

## 📞 19. Ship-to-Ship Hailing & Calling System

### Description
Enables cockpit-to-cockpit private calling between ships with a maximum communication range limit of 5,000 meters.

### How It Works
* **Protocol-Level Calling:** Active at the position server socket level. When a player initiates a hail to another online player, the server verifies range constraints and busy status. If clear, the target receives an incoming call notification.
* **Audio Loop Routing:** Once connected, a private audio stream is established. Microphones are captured and transmitted using custom audio frames (`AudioTypeHail = 0x04`) which bypass standard proximity/radio channels.
* **Interactive Hotkeys:** Players can configure separate keybindings to Initiate, Accept, and Decline/End calls.
* **VAD Auto-Transmission:** While in an active call, the voice capture service overrides PTT checks to enable hands-free voice transmission.
* **Synthesized Audio Chimes:** Uses NAudio signal generators to play realistic dialing/ringing sweeps (900Hz to 600Hz) and connection/disconnection tones, providing clear acoustic feedback.

### How to Use
1. Set bindings for **Initiate Hail**, **Accept Hail**, and **Decline/End Hail** under the **Hotkeys** tab in Settings.
2. When close to another ship, use your Initiate hotkey. The target will hear a ringing sound and see a HUD prompt.
3. The target can press their Accept key to connect, or Decline key to reject the call. Press Decline during an active call to hang up.

---

## 🔤 20. Visor HUD Real-Time Translation Subtitles

### Description
Translates incoming foreign-language voice streams in real-time and displays them on your Visor HUD overlay.

### How It Works
* **Foreign Language Sync:** The client synchronizes your selected language (e.g., English, French, German, Spanish, Portuguese, Japanese, or Chinese) to the server.
* **Multi-Language Transcription:** When a remote player speaks, the server forwards their spoken language metadata. The listening client uses this to transcribe the voice stream in the speaker's language using the offline Whisper model.
* **Dynamic Translation Engine:** The transcription is mapped against a comprehensive military/flight phrase translator and displayed on the HUD with the source and target languages prefixed: `[FROM -> TO] Subtitle text`.
* **Whisper Model Integration:** Requires the `ggml-tiny.bin` model. If not present, enabling the feature will prompt a download warning and fetch it in the background.

### How to Use
1. Go to the **Overlay** tab in Settings.
2. Check **Enable Real-Time HUD Translation Subtitles**.
3. If the Whisper model is missing, a yellow notice appears and the model will download automatically in the background.
4. Incoming foreign speech will now be translated and displayed on your screen!

---

## 🎧 21. Binaural HRTF Spatial Audio

### Description
Uses Head-Related Transfer Function (HRTF) algorithms to simulate the physical properties of human hearing. Instead of simple stereo panning, it modifies frequencies and delays to simulate how sound waves bounce off the listener's head and ears.

### How It Works
* **Woodworth's ITD (Interaural Time Difference):** Calculates the sub-millisecond delay between the left and right ears based on the speaker's angle relative to the listener's head.
* **ILD (Interaural Level Difference):** Attenuates the volume of the sound reaching the opposite ear by simulating the head shadow effect using a dynamic low-pass filter.
* **Stereo Headphone Optimization:** Custom-designed to work on all standard stereo headphones, allowing players to distinguish height, depth, and distance accurately.

### How to Use
1. Open the Client **Settings** window.
2. In the **Audio** tab, check **Enable HRTF Binaural Rendering**.
3. (Optional) Toggle this setting in real-time via the Stream Deck or Companion App.

---

## 📊 22. Visor HUD 3D Spectrogram

### Description
Renders a real-time, 3D Radix-2 64-point FFT spectral visualizer overlay directly on the in-game HUD next to each active speaker.

### How It Works
* **Fast Fourier Transform (FFT):** Calculates the frequency distribution of the incoming audio stream in real-time.
* **Spectral Mapping:** Groups frequencies into 8 distinct visualizer bands with a leaky integrator decay simulation to create smooth motion.
* **HUD Overlay Integration:** Displays the visualizer bars in the overlay window next to the speaker's name tag.

### How to Use
1. Open the Client **Settings** window.
2. In the **Overlay** tab, check **Enable Visor HUD 3D Spectrogram**.
3. When other players speak, the bars will dance next to their name tags.

---

## 🎙️ 23. Voice-Activated Ship Controls

### Description
Simulates keyboard presses in Star Citizen when the player speaks commands like "open doors" or "power up shields".

### How It Works
* **Voice Command Service:** Analyzes the microphone input in real-time. Supports match dictionaries in 8 languages.
* **Virtual Key Simulation:** Simulates direct hardware keystrokes using low-level Win32 `keybd_event` API calls (supporting keys held down for 50ms for reliable game registration, and modifier keys like Alt).
* **Configurable Hotkeys:** Keybind mappings can be configured in the Hotkeys tab in settings.

### How to Use
1. Open the Client **Settings** window.
2. In the **Hotkeys** tab, bind custom keys for Power, Doors, Shields, and Landing Gear.
3. Check **Enable Voice Commands** under Settings.
4. Hold your Voice Command PTT hotkey (default: `V`) and speak a command (e.g. "open doors").

---

## 🛰️ 24. Server-Side AAR 3D Playback

### Description
Integrates real-time player coordinates logging and spatial 3D visualization within the After Action Review (AAR) web admin portal.

### How It Works
* **Positions Log:** The Go server records player coordinates and zones to a `<session_id>_positions.jsonl` file every 500ms during active recording.
* **WebGL (Three.js) 3D Replay:** An interactive WebGL 3D tactical holographic dashboard powered by Three.js in the admin portal. It fetches coordinates log data and projects 3D spatial points, rendering a full 3D interactive flight path.
* **OrbitControls Navigation:** Allows administrators to rotate, pan, and zoom the 3D space with the mouse to analyze movement from any angle.
* **Synchronized Playback:** Animates player positions (wireframe 3D meshes) and speaking pulse rings (concentric wireframe circles radiating in 3D space) synchronized with the playback timeline of the recorded Ogg/Opus audio.

### How to Use
1. Log into the Admin Web Portal.
2. Under the **Aar Archives** tab, click **▶ 3D Replay** on any recorded segment.
3. The 3D Playback modal will open, displaying the player's path and speaking pulses in interactive 3D. Drag to rotate, right-click to pan, and scroll to zoom.


