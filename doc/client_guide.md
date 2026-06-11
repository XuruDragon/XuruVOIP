# 🎮 XuruVOIP Client User Guide

Welcome to the **XuruVOIP Client Guide**! This guide is designed to help you set up and use the voice client while playing Star Citizen, even if you are not a computer expert.

---

## 🌟 What is the XuruVOIP Client?

The **XuruVOIP Client** is an application that runs on your gaming computer. It works in the background to:
1. Capture your voice from your microphone.
2. Track your in-game character's position and environment.
3. Apply realistic sound effects (like making you sound like you are inside a space suit helmet or broadcasting your voice ship-wide).
4. Send your voice to other players near you or on your radio channels.

---

## 🛠️ Step 1: Initial Audio Setup

When you open the client for the first time, you need to select your audio devices.

1. **Select your Microphone:**
   * Look for the **Microphone (Input Device)** dropdown menu.
   * Select the microphone you use for gaming.
2. **Select your Headphones/Speakers:**
   * Look for the **Playback (Output Device)** dropdown menu.
   * Select your headphones or speakers.
3. **Adjust Volume & Boost:**
   * **Microphone Gain (dB):** If your friends say you are too quiet, drag this slider to the right to boost your voice. If you are too loud, drag it to the left.
   * **Output Volume (%):** Adjusts how loud other players sound in your ears.

---

## 🎤 Step 2: Choosing How to Talk (PTT vs. VAD)

You can choose between two methods to transmit your voice:

### Method A: Push-to-Talk (PTT)
* **What it is:** You only transmit your voice when holding down a specific key on your keyboard.
* **How to use it:**
  1. Set the **Audio Mode** setting to **PTT**.
  2. Click in the key binding box for the channel you want (e.g., **Proximity PTT Key** for people close to you, or **Radio PTT Key** for radio channels).
  3. Press the key on your keyboard you want to use (like `Caps Lock` or `NumPad1`).
  4. Hold that key down while speaking, and release it when you are done.

### Method B: Voice Activation (VAD)
* **What it is:** The application listens to your voice and automatically transmits when it detects you speaking.
* **How to use it:**
  1. Set the **Audio Mode** setting to **VAD**.
  2. Adjust the **VAD Sensitivity** slider.
     * **High Sensitivity:** Captures quiet whispers, but might accidentally transmit background noises (like keyboard clicks or fan noise).
     * **Low Sensitivity:** Prevents background noise, but you will need to speak louder to trigger it.

---

## 🚀 Immersive Features & How to Configure Them

XuruVOIP includes special features that make voice communication match your gameplay:

### 🪖 1. Space Suit Helmet Modulation
* **What it does:** When you put your helmet on in Star Citizen, it automatically alters your voice to sound like a digital radio transmission. It also overlays subtle suit breathing sounds.
* **How to configure it:**
  * Go to **Settings** and check **Enable Helmet Modulator**.
  * You can manually toggle the helmet state using the **Helmet Toggle Key** (default is `H`).
  * If using automated logging, the app will read Star Citizen's game log to detect when you equip or remove your helmet and toggle the effect automatically!

### 🤢 2. G-Force & Physical Exertion voice distortion
* **What it does:** Simulates physical strain on your character's voice.
  * **Tremolo:** If your character experiences high G-forces (e.g., during sharp turns in a spacecraft), your voice will shake/tremble.
  * **Pitch Shift:** High G-forces will make your voice sound deeper.
  * **Exertion Gasping:** If your character runs out of stamina (sprinted too much), the app automatically overlays heavy breathing/panting sounds over your microphone feed.
* **How to configure it:**
  * Go to **Settings** and check **Enable G-Force & Exertion Voice Distortion**.
  * Make sure your Star Citizen directory configuration is correct so the app can read your character's stamina status in real-time.

### 📢 3. Ship Public Address (PA) System
* **What it does:** Allows ship captains or crew members to broadcast a ship-wide message to everyone inside the same spaceship.
  * PA broadcasts bypass local proximity mutes (so everyone on board hears you).
  * Plays a Sci-Fi chime alert before you start speaking.
  * Adds a megaphone distortion filter to make it sound like it is coming from ceiling speakers.
* **How to configure it:**
  * Go to **Settings** and check **Enable Ship PA System**.
  * Set a hotkey in the **PA Broadcast Key** box (default is `P`).
  * Press and hold the key to make a ship-wide announcement.

### 📡 4. Tactical Radio Repeaters
* **What it does:** If you are too far away from someone to reach them directly over the radio, you can turn on **Repeater Mode**. This turns your client into a radio beacon, allowing other players to relay their signals through you to reach further distances.
* **How to configure it:**
  * Go to **Settings** and check **Enable Radio Repeaters**.
  * To act as a repeater for your team, check the **Act as Radio Repeater Beacon** box.

### ⚡ 5. Dynamic Intercom Degradation (Ship Damage/Status)
* **What it does:** Simulates communication interference when your spaceship takes shield damage, experiences power grid failures, or engages in Quantum Travel. This only affects intercom channels (channels starting with `Intercom_` that you automatically join when boarding a ship).
  * **Shield Hits Effect:** Injects a burst of static noise and crackles for 2.5 seconds when the ship's shields are hit.
  * **Critical Power Effect:** Injects an electrical AC hum (60Hz + harmonics), drops voice pitch (speed factor 0.78x), and applies heavy saturation distortion during power losses.
  * **Quantum Travel Effect:** Applies a flanger/phaser comb-filter sweep and a high-frequency whine when traveling in quantum.
* **How to configure it:**
  * Go to **Settings** -> **General** tab.
  * Check the global option **Enable Intercom Degradation (Ship Damage)** (disabled by default).
  * Under it, you can toggle the individual sub-effects (**Shield Hits**, **Critical Power**, **Quantum Travel**) depending on your preferences.
  * When active, warning messages like `⚡ INTERCOM: POWER LOSS` or `⚡ INTERCOM: QUANTUM WAVE` will display on your HUD overlay.

### 🎙️ 6. Offline Voice Commands (Hands-Free PTT)
* **What it does:** Allows you to hold a dedicated key to speak commands directly to your ship's onboard computer (like lowering/raising your helmet visor, muting/unmuting your microphone, changing radio channels, or switching voice changer profiles) without other players hearing you.
  * **Suppressed Transmit:** Holding the Voice Command key silences your proximity and radio voice streams, keeping your instructions private.
  * **Offline Transcription:** Uses an offline Whisper model to translate your speech into actions.
  * **Confidence Threshold:** A slider filter allows you to adjust how strictly the computer matches your voice commands.
  * **Supported Languages:** Localized matching dictionaries support English, French, German, Spanish, Portuguese, Japanese, and Chinese.
* **How to configure it:**
  * Go to **Settings** -> **General** tab.
  * Check **Enable Voice Commands (Hands-Free PTT)** (disabled by default).
  * *Notice: Enabling this for the first time will automatically download the required Whisper speech-to-text model (~140MB) in the background.*
  * Go to the **Hotkeys** tab.
  * Assign a hotkey in the **Voice Command Key (PTT)** box (default is `V`).
  * Hold the key to speak (e.g. *"Computer, toggle visor"*, French: *"Ordinateur, basculer le casque"*, German: *"Kanal auf Alpha"*), and release it to execute.

---

## 📱 Using the Companion App

The Companion App allows you to control XuruVOIP from your smartphone, tablet, or secondary monitor browser.

1. **Enable it:** Go to **Settings**, check **Enable Companion HTTP Server**, and set a port (default: `8891`).
2. **Enable Tactical Map Mode (Optional):** Check the box **Enable Tactical Co-Pilot Map (MFD)** (disabled by default) to stream location coordinates and enable the radar screen tab on the companion interface.
3. **Access it:** Open any web browser on your device and type `http://localhost:8891` (if on the same PC) or `http://[Your-PC-IP-Address]:8891` (if on your phone connected to the same Wi-Fi).
4. **Features & Layout:**
   * **🎛️ Controls Tab:** Toggle mic/audio mutes, helmet modulation, PA broadcasts, select active radio channels, select voice changer profiles, or mock test G-Force/Exertion stress levels.
   * **🗺️ Tactical Map Tab (MFD):**
     * Displays a sci-fi glassmorphic radar screen tracking your local position.
     * Renders other crew members and players inside your same container/zone.
     * Displays real-time speaking status (pulsating green rings around active speakers).
     * Includes **Heading-Up** (map rotates with your movement direction) and **North-Up** orientation modes.
     * Range slider to adjust radar zoom levels from 10 meters up to 1000 meters.

---

## 🔌 External Hardware Telemetry (Sim-Pits & Custom Hardware)

For players with custom cockpit simulator setups (sim-pits) or external displays, XuruVOIP can broadcast real-time telemetry.

1. **Enable it:** Go to **Settings**, check **Enable Telemetry Broadcast (UDP)**, and set a port (default: `8895`).
2. **How it works:** The client will broadcast a JSON payload over UDP to `127.0.0.1:8895` every 100ms.
3. **Use cases:** Cockpit builders can use simple Arduino, Raspberry Pi, or Stream Deck plugins to read these packets and light up physical LEDs when receiving or transmitting radio messages, or when the suit visor is down.

---

## ❓ Troubleshooting (FAQ)

### My friends cannot hear me!
* Make sure your **Microphone (Input Device)** selection matches your active microphone.
* If using **PTT**, make sure you are holding the correct key and that the microphone level bar (VU meter) lights up when you speak.
* If using **VAD**, try increasing the **VAD Sensitivity** so it triggers more easily.

### The space suit helmet mode does not toggle automatically.
* Verify that your game directory path is set correctly in settings. The app needs access to Star Citizen's `Game.log` file to detect helmet actions automatically.

### The overlay is blocking my clicks in the game.
* The HUD overlay is designed to be completely click-through, but make sure Star Citizen is running in **Borderless Windowed** mode (not Exclusive Fullscreen) for the best results.
