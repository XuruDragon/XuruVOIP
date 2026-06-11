# 🎛️ XuruVOIP Stream Deck Plugin User Guide

Welcome to the **XuruVOIP Stream Deck Plugin Guide**! This guide is written to help you set up physical buttons on your Elgato Stream Deck device to control your voice settings and telemetry feeds while playing Star Citizen.

---

## 🌟 What is the Stream Deck Plugin?

The **XuruVOIP Stream Deck Plugin** links your physical Elgato Stream Deck hardware to the XuruVOIP Client running on your PC. Once set up, you can touch a physical button on your desk to:
1. Mute or unmute your microphone.
2. Mute or unmute other players' voices.
3. Open or close your space suit helmet visor.
4. Broadcast to the ship's Public Address (PA) system.
5. Toggle Radio Repeater / Beacon relay modes.
6. Trigger customized voice command macros (e.g., "open hangar", "request landing").
7. Monitor ship intercom degradation status and cycle simulation states.
8. Read real-time system zone names and X, Y, Z space coordinates on a physical key display.

---

## 🚀 Step 1: Installing the Plugin

Getting the plugin installed into your Stream Deck software is very simple.

1. **Download the Plugin:**
   * Download the file named `com.xuru.voip.streamDeckPlugin` from the releases page.
2. **Install:**
   * Double-click the downloaded file.
   * Your Elgato Stream Deck software will open and show a message asking if you want to install it. Click **Install**.
3. **Verify:**
   * In your Elgato Stream Deck desktop app, look at the list of actions on the right side.
   * Scroll down until you see a category named **XuruVOIP** containing 19 actions.

---

## 🔌 Step 2: Connecting to your XuruVOIP Client

The Stream Deck app talks to your voice client using a local network link. You must enable this link in your client application:

1. Open your **XuruVOIP Client** (the WPF app).
2. Click the **Settings** gear icon.
3. In the **General** tab, check the box **Enable Companion HTTP Server**.
4. Check the port number (default is `8891`).
5. Click **Save & Close** to apply.

---

## ⚙️ Step 3: Dragging Actions & Configuration

Now, let's configure the physical buttons on your device.

1. **Drag and Drop:**
   * Select any action from the **XuruVOIP** list on the right.
   * Drag it onto one of the empty squares representing your Stream Deck buttons.
2. **Configure (Property Inspector):**
   * Click on the button you just placed to highlight it.
   * Look at the **Property Inspector** panel at the bottom of the Stream Deck window.
   * Configure the parameters:
     * **Companion Port:** Enter the port matching your client settings (default is **`8891`**).
     * **Voice Command** *(Voice Command Macro action only)*: Enter the specific text command you want to trigger (e.g., `open visor`, `engage quantum drive`).

---

## 🎮 Available Actions & Visual Feedback

The plugin features dynamic icons and text that change in real-time based on your settings:

### 🎤 Microphone Mute Keys
* **Actions:** **Proximity Mute**, **Radio Mute**, and **Profile Mute**.
* **Visual Feedback:** 
  * Active/Ready: **Cyan glowing microphone** icon.
  * Muted: **Red crossed-out microphone** icon.

### 🔊 Audio Playback Mute Keys
* **Actions:** **Audio Proximity Mute**, **Audio Radio Mute**, and **Audio Profile Mute**.
* **Visual Feedback:** Shows a speaker icon that strikes through in red when muted.

### 🪖 Helmet Visor Toggle
* **Action:** **Toggle Helmet**.
* **Visual Feedback:** Shows a green visor-down icon when closed, and red visor-up icon when open.

### 🔄 Cycle Radio (Live Channel Display!)
* **Action:** **Cycle Radio**.
* **Live Display:** Automatically displays the active channel name (e.g., `General`, `120.5`, or `Intercom`) in clean text directly on the button's LCD screen in real-time.

### 📢 PA Broadcast (Push-to-Talk)
* **Action:** **PA Broadcast**.
* **Use:** Hold down the key to broadcast on the ship Public Address system. Release to stop.
* **Visual Feedback:** Megaphone glows active cyan when broadcasting.

### 📡 Beacon Mode (Repeater)
* **Action:** **Beacon Mode**.
* **Use:** Toggles the client's Radio Repeater / Beacon relay capability.
* **Visual Feedback:** Shows active cyan waves when broadcasting, and dim grey/red when disabled.

### 🎙️ Voice Command Macro
* **Action:** **Voice Command Macro**.
* **Use:** Executes a custom text-based voice command. Configure the command string in the settings.
* **Visual Feedback:** Displays a green listening indicator if voice recognition is currently listening.

### 💬 Intercom Status (Multi-State)
* **Action:** **Intercom Status**.
* **Use:** Displays the ship's active intercom condition. Pressing it cycles the simulation state.
* **Visual Feedback:** Displays 4 distinct states:
  * `NORMAL`: Cyan headset badge.
  * `SHIELD HIT`: Orange warning shield badge.
  * `CRIT PWR`: Red warning hazard battery badge.
  * `QUANTUM`: Purple quantum warp speed lines badge.

### 🗺️ Location Telemetry (MFD Screen)
* **Action:** **Location Telemetry**.
* **Use:** A read-only telemetry display.
* **Live Display:** Displays the current system zone and coordinate vectors $(X, Y, Z)$ directly on the key in real-time. Displays `NO GPS` if coordinates are unavailable.

### 📞 Ship-to-Ship Hailing Actions
* **Actions:** **Initiate Hail**, **Accept/Answer Hail**, and **Decline/End Hail**.
* **Use:** Controls the cockpit calling system:
  * **Initiate Hail:** Triggers a call request to the specified target. Shows dial status.
  * **Accept/Answer Hail:** Answers an incoming call.
  * **Decline/End Hail:** Rejects a incoming call or hangs up an active call.
* **Visual Feedback:** Shows active ringing, dialing, or connected calling graphics.

### 🔤 HUD Translation Subtitles Toggle
* **Action:** **Toggle Translation**.
* **Use:** Toggles real-time HUD translation subtitles.
* **Visual Feedback:** Shows green `ON` icon when enabled, and red crossed-out `OFF` icon when disabled.

### 🎧 Toggle HRTF Spatial Audio
* **Action:** **Toggle HRTF**.
* **Use:** Toggles real-time HRTF spatial audio rendering.
* **Visual Feedback:** Shows green `ON` icon (cyan ears) when enabled, and red crossed-out `OFF` icon when disabled.

### 📊 Toggle Visor Spectrogram
* **Action:** **Toggle Spectrogram**.
* **Use:** Toggles real-time visor HUD 3D spectrogram.
* **Visual Feedback:** Shows green `ON` icon (cyan equalizer bars) when enabled, and red crossed-out `OFF` icon when disabled.

---

## 🗺️ Stream Deck Layout Profiles

We have pre-designed 15 layout profiles (5 devices × 3 layouts: Pilot, Infantry, Captain) organized by device directory under `streamdeck/profiles/`. In the GitHub release, these are packaged as individual `.streamDeckProfile` files for easy deployment:

---

### 1. Stream Deck Mini (3x2 Grid)
Designed for compact setups, prioritizing the most critical buttons.

*   **Pilot:**
    *   **Row 1:** Proximity Mute, Radio Mute, Toggle Helmet
    *   **Row 2:** PA Broadcast, Intercom Status, Location Telemetry
*   **Infantry:**
    *   **Row 1:** Proximity Mute, Radio Mute, Toggle Helmet
    *   **Row 2:** Cycle Radio, *Macro: Status Report*, Location Telemetry
*   **Captain:**
    *   **Row 1:** Proximity Mute, Radio Mute, PA Broadcast
    *   **Row 2:** Intercom Status, Cycle Radio, Location Telemetry

*Profile templates folder: [streamdeck/profiles/mini](../streamdeck/profiles/mini)*

---

### 2. Stream Deck Classic (5x3 Grid)
The standard layout for a 15-key Stream Deck device.

*   **Pilot:**
    | | Column 1 | Column 2 | Column 3 | Column 4 | Column 5 |
    | :---: | :---: | :---: | :---: | :---: | :---: |
    | **Row 1** | Proximity Mute | Radio Mute | Profile Mute | Cycle Radio | Toggle Helmet |
    | **Row 2** | PA Broadcast | Beacon Mode | Intercom Status | *Macro: Open Hangar* | *Macro: Req Landing* |
    | **Row 3** | Location Telemetry | Audio Prox Mute | Audio Radio Mute | Audio Profile Mute | *Macro: Status Report* |

*   **Infantry:**
    | | Column 1 | Column 2 | Column 3 | Column 4 | Column 5 |
    | :---: | :---: | :---: | :---: | :---: | :---: |
    | **Row 1** | Proximity Mute | Radio Mute | Toggle Helmet | Cycle Radio | *Macro: Status Report* |
    | **Row 2** | Audio Prox Mute | Audio Radio Mute | Beacon Mode | *Macro: Mute Prox* | *Macro: Unmute Prox* |
    | **Row 3** | Location Telemetry | *Macro: Ch Alpha* | *Macro: Ch Beta* | *Macro: Toggle Changer* | *Macro: Voice Cyborg* |

*   **Captain:**
    | | Column 1 | Column 2 | Column 3 | Column 4 | Column 5 |
    | :---: | :---: | :---: | :---: | :---: | :---: |
    | **Row 1** | Proximity Mute | Radio Mute | PA Broadcast | Intercom Status | Toggle Helmet |
    | **Row 2** | Audio Prox Mute | Audio Radio Mute | Beacon Mode | *Macro: Power Shields* | *Macro: Status Check* |
    | **Row 3** | Location Telemetry | Cycle Radio | *Simulate Shield Hit* | *Simulate Power Loss* | *Simulate Quantum* |

*Profile templates folder: [streamdeck/profiles/classic](../streamdeck/profiles/classic)*

---

### 3. Stream Deck XL (9x4 Grid)
Provides a high-density 36-key layout for maximum physical control.

*   **Common Keypad Base (Rows 1 & 2):**
    | | Col 1 | Col 2 | Col 3 | Col 4 | Col 5 | Col 6 | Col 7 | Col 8 | Col 9 |
    | :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: |
    | **Row 1** | Prox Mute | Radio Mute | Profile Mute | Toggle Helmet | Cycle Radio | PA Broadcast | Beacon Mode | Intercom Status | Location Telemetry |
    | **Row 2** | Audio Prox Mute | Audio Radio Mute | Audio Profile Mute | Initiate Hail | Accept/Answer Hail | Decline/End Hail | Toggle Translation | Toggle HRTF | Toggle Spectrogram |

*   **Pilot Specializations (Rows 3 & 4):**
    *   **Row 3:** *Macro: Open Hangar* (Col 1), *Macro: Req Landing* (Col 2), *Macro: Status Report* (Col 3), *Macro: Close Visor* (Col 4)
    *   **Row 4:** *Macro: Power Up Shields* (Col 1)
*   **Infantry Specializations (Rows 3 & 4):**
    *   **Row 3:** *Macro: Status Report* (Col 1), *Macro: Mute Prox* (Col 2), *Macro: Unmute Prox* (Col 3)
    *   **Row 4:** *Macro: Ch Alpha* (Col 1), *Macro: Ch Beta* (Col 2), *Macro: Toggle Changer* (Col 3), *Macro: Voice Cyborg* (Col 4)
*   **Captain Specializations (Rows 3 & 4):**
    *   **Row 3:** *Macro: Power Up Shields* (Col 1), *Macro: Status Check* (Col 2)
    *   **Row 4:** *Simulate Shield Hit* (Col 1), *Simulate Power Loss* (Col 2), *Simulate Quantum* (Col 3)

*Profile templates folder: [streamdeck/profiles/xl](../streamdeck/profiles/xl)*

---

### 4. Stream Deck + (4x2 Keypad + 4 Dials)
Combines physical keys with rotary encoders and a touch strip display.

*   **Keypad Grid:**
    | | Column 1 | Column 2 | Column 3 | Column 4 |
    | :---: | :---: | :---: | :---: | :---: |
    | **Row 1 (Pilot)** | Proximity Mute | Radio Mute | Profile Mute | Toggle Helmet |
    | **Row 2 (Pilot)** | PA Broadcast | Beacon Mode | Intercom Status | Location Telemetry |
    | **Row 1 (Infantry)** | Proximity Mute | Radio Mute | Toggle Helmet | Location Telemetry |
    | **Row 2 (Infantry)** | Audio Prox Mute | Audio Radio Mute | PA Broadcast | *Macro: Status Report* |
    | **Row 1 (Captain)** | Proximity Mute | Radio Mute | PA Broadcast | Intercom Status |
    | **Row 2 (Captain)** | Toggle Helmet | Location Telemetry | Audio Prox Mute | Audio Radio Mute |

*   **Rotary Encoders (Dials) & Touch Strip:**
    1.  **Dial 1: Radio Channel Dial** (`com.xuru.voip.action.cycle_radio_dial`)
        *   *Rotate:* Select active radio frequency/channel.
        *   *Push / Touch Screen Tap:* Toggle radio microphone mute.
        *   *Display:* Active radio channel name (red `[MUTED]` tag appended if muted).
    2.  **Dial 2: Adjust Exertion/G-Force** (`com.xuru.voip.action.adjust_exertion`)
        *   *Rotate:* Adjust Mock G-Force.
        *   *Rotate while pressed:* Adjust Mock Exertion value.
        *   *Push / Touch Screen Tap:* Toggle immersive exertion distortion simulation.
        *   *Display:* G-force amount (G) and Exertion percentage (%) with state status.
    3.  **Dial 3: Voice Changer Dial** (`com.xuru.voip.action.voice_changer_dial`)
        *   *Rotate:* Cycle through active voice changer profiles (None, Alien, Cyborg, Robotic, PitchShift).
        *   *Push / Touch Screen Tap:* Toggle voice changer on/off.
        *   *Display:* Active voice profile name or `Disabled`.
    4.  **Dial 4:** Unassigned / Reserved.

*Profile templates folder: [streamdeck/profiles/plus](../streamdeck/profiles/plus)*

---

### 5. Stream Deck + XL (9x4 Keypad + 6 Dials)
The ultimate layout, offering the full 36-key keypad grid of the XL along with 6 dials.

*   **Keypad Layout:** Identical layout mapping to the standard **Stream Deck XL** keypad.
*   **Rotary Dials:** Dial 1, 2, and 3 are mapped identically to the **Stream Deck +** (Radio Channel, G-Force/Exertion, Voice Changer). Dials 4, 5, and 6 are unassigned / reserved.

*Profile templates folder: [streamdeck/profiles/plus_xl](../streamdeck/profiles/plus_xl)*

---

---

## ❓ Troubleshooting (FAQ)

### My buttons show a yellow warning triangle!
* This means the Stream Deck software cannot communicate with the XuruVOIP client.
  1. Ensure the XuruVOIP client application is running.
  2. Verify that **Enable Companion HTTP Server** is checked in the client's settings.
  3. Double-check that the **Companion Port** in your Stream Deck Property Inspector (for that button) matches the port listed in the client settings (usually `8891`).

### The active radio frequency name or location coordinate is not appearing.
* Make sure you are connected to a XuruVOIP server. If you are disconnected or game telemetry is not running, the active channel will display as "Proximity" or "General", and coordinates will display as "NO GPS".
* Check the port configuration in the button's properties.
