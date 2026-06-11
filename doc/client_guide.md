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

---

## 📱 Using the Companion App

The Companion App allows you to control XuruVOIP from your smartphone, tablet, or secondary monitor browser.

1. **Enable it:** Go to **Settings**, check **Enable Companion HTTP Server**, and set a port (default: `8891`).
2. **Access it:** Open any web browser on your device and type `http://localhost:8891` (if on the same PC) or `http://[Your-PC-IP-Address]:8891` (if on your phone connected to the same Wi-Fi).
3. **Controls:**
   * Mute or unmute different audio feeds.
   * View live compass and radar coordinates.
   * Trigger the PA system using the big red button.
   * Drag sliders to mock-test G-force/Exertion voice effects.

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
