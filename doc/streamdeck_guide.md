# 🎛️ XuruVOIP Stream Deck Plugin User Guide

Welcome to the **XuruVOIP Stream Deck Plugin Guide**! This guide is written to help you set up physical buttons on your Elgato Stream Deck device to control your voice settings while flying in Star Citizen.

---

## 🌟 What is the Stream Deck Plugin?

The **XuruVOIP Stream Deck Plugin** links your physical Elgato Stream Deck hardware to the XuruVOIP Client running on your PC. Once set up, you can touch a physical button on your desk to:
1. Mute or unmute your microphone.
2. Mute or unmute other players' voices.
3. Open or close your space suit helmet visor.
4. Change radio frequencies and see the active channel name directly on the button's screen!

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
   * Scroll down until you see a category named **XuruVOIP** containing several actions (like Proximity Mute, Cycle Radio, etc.).

---

## 🔌 Step 2: Connecting to your XuruVOIP Client

The Stream Deck app talks to your voice client using a local network link. You must enable this link in your client application:

1. Open your **XuruVOIP Client** (the WPF app).
2. Click the **Settings** gear icon.
3. In the **General** tab, check the box **Enable Companion HTTP Server**.
4. Check the port number (default is `8891`).
5. Click **Save & Close** to apply.

---

## ⚙️ Step 3: Dragging Actions & Setting the Port

Now, let's configure the physical buttons on your device.

1. **Drag and Drop:**
   * Select any action from the **XuruVOIP** list on the right (e.g., *Proximity Mute*).
   * Drag it onto one of the empty squares representing your Stream Deck buttons.
2. **Set the Companion Port:**
   * Click on the button you just placed to highlight it.
   * Look at the **Property Inspector** panel at the bottom of the Stream Deck window.
   * Locate the field named **Companion Port**.
   * Enter the port number that matches your client settings (default is **`8891`**).
   * *(If you have multiple buttons, make sure this port number is set correctly on each one)*.

---

## 🎮 Available Actions & Visual Feedback

The plugin features dynamic icons and text that change in real-time based on your settings:

### 🎤 Microphone Mute Keys
* **Actions:** **Proximity Mute**, **Radio Mute**, and **Profile Mute**.
* **Visual Feedback:** 
  * When you are unmuted and ready to speak, the key will show a **cyan glowing microphone** icon.
  * When you mute yourself, the key automatically changes to an **amber crossed-out microphone** icon.

### 🔊 Audio Playback Mute Keys
* **Actions:** **Audio Proximity Mute**, **Audio Radio Mute**, and **Audio Profile Mute**.
* **Use:** Toggles whether you can hear other players' voices on that specific channel.
* **Visual Feedback:** Shows a speaker icon that strikes through when muted.

### 🪖 Helmet visor Toggle
* **Action:** **Toggle Helmet**.
* **Use:** Toggles your visor state. Very useful for quickly sealing or opening your helmet manually.

### 🔄 Cycle Radio (Live Channel Display!)
* **Action:** **Cycle Radio**.
* **Use:** Toggles through your available radio channels.
* **Live Display:** This button will automatically query your active channel and display the name (e.g., `General`, `120.5`, or `Intercom`) in clean text directly on the button's LCD screen in real-time!

---

## ❓ Troubleshooting (FAQ)

### My buttons show a yellow warning triangle!
* This means the Stream Deck software cannot communicate with the XuruVOIP client.
  1. Ensure the XuruVOIP client application is running.
  2. Verify that **Enable Companion HTTP Server** is checked in the client's settings.
  3. Double-check that the **Companion Port** in your Stream Deck Property Inspector (for that button) matches the port listed in the client settings (usually `8891`).

### The active radio frequency name is not appearing on the Cycle Radio button.
* Make sure you are connected to a XuruVOIP server. If you are disconnected, the active channel will display as blank or "General".
* Check the port configuration in the button's properties.
