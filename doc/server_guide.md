# 🖥️ XuruVOIP Server Hosting & Configuration Guide

Welcome to the **XuruVOIP Server Guide**! This guide is written to help you set up, run, and customize your own voice server for you and your friends, without requiring any background in computer programming.

---

## 🌟 What is the XuruVOIP Server?

The **XuruVOIP Server** is a helper program that runs in the background (either on your gaming computer or on a separate machine). It behaves like a traffic controller:
1. It listens to client applications sending player positions, helmets, and volume settings.
2. It groups players together who are in the same zone or inside the same spaceship.
3. It relays voice data between nearby players and handles the radio repeaters.
4. It can optionally connect to Discord to bridge your in-game chat to a Discord channel.

---

## 🚀 Step 1: Starting the Server

The server is lightweight and does not have a complex interface.

1. **Locate the Server folder:**
   * Go to the directory where you extracted XuruVOIP.
   * Open the `server` folder.
2. **Launch the Server:**
   * Double-click `starcitizen-voip-server.exe` (or run `./voip-server` on Linux/macOS).
   * A command window (black screen with text) will open. This means the server is running!
   * Keep this window open. If you close it, the server will shut down and disconnect everyone.

---

## ⚙️ Step 2: Customizing Settings (The `.env` File)

You can customize how your server behaves by editing a simple settings file named `.env`.

### How to edit the settings:
1. Locate the file named `.env` in the `server` folder.
2. Right-click the `.env` file, select **Open With**, and choose **Notepad** (or any text editor).
3. Change the settings you want (see the list below).
4. Save the file (`Ctrl + S`) and close Notepad.
5. **Important:** Restart the server program for the changes to take effect.

### Key Settings Explained:

* **`PORT`** (default: `8888`): The main port number players will enter in their clients to connect.
* **`AUDIO_PORT`** (default: `8889`): The port used specifically for transmitting voice data.
* **`SERVER_PASSWORD`**: If you want to make your server private, write a password here. Anyone trying to connect will need to enter this password in their client app.
* **`XURUVOIP_ENABLE_RADIO_REPEATERS`** (`1` = On, `0` = Off): Enables or disables the multi-hop radio repeater beacon system.
* **`XURUVOIP_ENABLE_SHIP_PA`** (`1` = On, `0` = Off): Enables or disables the ship-wide Public Address (PA) broadcast system.

---

## 🎙️ Step 3: Setting Up the Discord Voice Bridge

The server includes an optional feature called the **Discord Voice Bridge**. When enabled, players in-game can have their radio voices linked to a Discord voice channel, allowing players out-of-game to listen and communicate.

### How to configure it (Step-by-Step):

1. **Create a Discord Bot:**
   * Go to the [Discord Developer Portal](https://discord.com/developers/applications).
   * Click **New Application** and give it a name (e.g. `XuruVOIP Bridge`).
   * Go to the **Bot** tab on the left, click **Add Bot**, and confirm.
   * Under the bot settings, find **Token** and click **Copy**. Save this token!
   * Scroll down to **Privileged Gateway Intents** and enable **Guild Members Intent** and **Message Content Intent**.
2. **Invite the Bot to your Discord Server:**
   * Go to the **OAuth2** tab, then **URL Generator**.
   * Under **Scopes**, check `bot`.
   * Under **Bot Permissions**, check `Connect`, `Speak`, `Use Voice Activity`, and `Send Messages`.
   * Copy the URL at the bottom, paste it into your web browser, and invite the bot to your Discord server.
3. **Configure the Server Settings:**
   * Open your `.env` settings file in Notepad.
   * Set **`XURUVOIP_ENABLE_DISCORD_BRIDGE`** to `1`.
   * Paste your bot token into **`DISCORD_BOT_TOKEN`**.
   * Copy your Discord Server's ID (Right-click your server icon in Discord, click **Copy Server ID**) and paste it into **`DISCORD_GUILD_ID`**.
   * Set **`DISCORD_CHANNEL_MAPPINGS`** to define which radio channels map to which Discord voice channels.
     * *Example format:* `General:123456789012345678,SquadA:876543210987654321` (where the numbers are the Discord Voice Channel IDs).
4. **Save and Restart:**
   * Save the `.env` file and restart the server program. The bot should now appear online in your Discord server and join voice channels automatically!

---

## ❓ Troubleshooting (FAQ)

### My friends cannot connect to my server!
* **Check your IP Address:** Make sure you gave them your public IP address (you can find it by searching "what is my IP" on Google).
* **Firewall blocks:** Ensure that ports `8888` (TCP) and `8889` (UDP) are allowed through your Windows Defender Firewall.
* **Port Forwarding:** If you are hosting the server from home, you must configure your home router to forward TCP port `8888` and UDP port `8889` to the local IP address of your gaming PC.

### The Discord Bot is online but won't join channels.
* Ensure the bot has permission to see and join the specific voice channels you mapped in the settings.
* Verify that the Voice Channel IDs in your mappings are correct (Right-click the voice channel name in Discord and click **Copy ID**).
* Check the black server window for any error messages starting with `[Discord Bridge]`.
