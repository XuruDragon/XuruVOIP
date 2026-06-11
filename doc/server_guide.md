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

* **`XURUVOIP_PORT`** (default: `8888`): The main port number players will enter in their clients to connect.
* **`XURUVOIP_AUDIO_PORT`** (default: `8889`): The port used specifically for transmitting voice data.
* **`XURUVOIP_SERVER_PASSWORD`**: If you want to make your server private, write a password here. Anyone trying to connect will need to enter this password in their client app. /!\ You will also have to set the `XURUVOIP_PUBLIC_SERVER` variable to `0` to make the server private and the `XURUVOIP_SERVER_PASSWORD` variable usable.
* **`XURUVOIP_ADMIN_SERVER_PASSWORD`**: A separate password used exclusively to log in to the administrator web dashboard.
* **`XURUVOIP_SPATIAL_AUDIO`** (`1` = On, `0` = Off): Enables 3D audio. When on, voices are automatically panned left/right and sound quieter the further away players are from you in-game.
* **`XURUVOIP_ENABLE_INTERCOM`** (`1` = On, `0` = Off): Enables deck intercoms. When on, crew members inside the same spaceship can hear each other clearly even if they are in different rooms or lack line-of-sight.
* **`XURUVOIP_ENABLE_EVA_MUTING`** (`1` = On, `0` = Off): Vacuum muting. When on, if a player goes out into the vacuum of space (EVA) without putting their space suit helmet on, their microphone will be automatically muted to simulate lack of air for sound transmission.
* **`XURUVOIP_ENABLE_DISCORD_BRIDGE`** (`1` = On, `0` = Off): Toggles the Discord voice connection gateway.
* **`XURUVOIP_ENABLE_RADIO_REPEATERS`** (`1` = On, `0` = Off): Enables or disables the multi-hop radio repeater beacon system.
* **`XURUVOIP_ENABLE_SHIP_PA`** (`1` = On, `0` = Off): Enables or disables the ship-wide Public Address (PA) broadcast system.
* **`XURUVOIP_ENABLE_AAR_RECORDING`** (`1` = On, `0` = Off): Enforce global AAR voice recording capability (disabled by default). When enabled, administrators can toggle recording of specific proximity/channels/profiles via the Admin Dashboard.

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

## 👑 Step 4: Accessing and Using the Admin Web Portal

The server hosts a web-based **Admin Portal** that allows administrators to manage channels, players, and enforce rules through a web browser.

### How to access the Admin page:
1. Open any web browser (Chrome, Edge, Firefox, etc.) on your PC.
2. In the address bar, type:
   * **`http://localhost:8888/admin`** (if you are on the same computer hosting the server).
   * **`http://[Your-Server-IP]:8888/admin`** (if you are accessing it from another computer).
   * *(Replace `8888` with the custom port number you specified in the `.env` file under `XURUVOIP_PORT` if you changed it)*.
3. **Log in:**
   * **Username & Password:** Enter your admin credentials. *(A default administrator account is created the first time the server is run. The credentials will be displayed in the server console window. to reset the admin password, you will have to edit or remove the server's database file `xuruvoip.db`.)*.
   * **Server Password:** Enter the token matching the **`XURUVOIP_ADMIN_SERVER_PASSWORD`** in your `.env` settings file.

### Admin Portal Capabilities:

Once logged in, the dashboard gives you complete control over your voice server:

* **Channel Management:**
  * **Add Channel:** Create new radio frequency rooms (e.g. `SquadB`, `FlightLead`).
  * **Rename / Remove Channels:** Edit channel labels or delete them. If a channel is deleted, players currently in that channel are automatically moved back to the default `General` channel.
* **Voice Profiles:**
  * Define and manage specialized group audio settings.
* **Player Administration:**
  * **Force Move:** Manually shift a player into a different active radio frequency or select which frequencies they are allowed to listen to.
  * **Assign Profile:** Apply specific voice profiles to active users.
* **Moderation Tools:**
  * **Kick Player:** Instantly disconnect a player from the voice server.
  * **Reset Password:** Set a new password for user accounts if a player forgets theirs.
  * **Ban Player:** Permanently block a player. You can choose to ban them by their username, their network **IP Address**, or their hardware **HWID** (preventing them from bypassing the ban by changing their account/IP).
  * **Delete Account:** Remove user profiles from the server database completely.
  * **Toggle Anonymous Mode:** Turn on global anonymous mode to hide usernames and protect player identities during open matches.
* **Admin Management:**
  * Create new administrator logins, remove existing ones, or update admin passwords.
* **AAR Archives (Mission Recording & Timeline):**
  * **Recording Controls:** Toggle voice recording dynamically for Proximity Chat, specific Radio Channels, or Audio Profiles.
  * **Interactive Voice Timeline:** Visually trace speaking blocks for each player on a graphical time canvas.
  * **Audio Segment Playback:** Click on any timeline block or browse the segments list to play back `.ogg` voice clips directly in the browser, or permanently delete recordings from disk.

## 📞 Ship-to-Ship Hailing & Calling Protocol

The server handles hailing and private calling automatically at the protocol level, requiring no additional `.env` settings:
* **Distance Tracking:** The server regularly tracks coordinates of calling peers. If the distance between two connected players exceeds 5,000 meters, the server automatically disconnects the call and sends a warning message (`out_of_range`) to both clients.
* **Busy States:** If a player is already dialing or in an active call, the server automatically rejects any other incoming hailing requests, replying with a `busy` status to the caller.
* **Audio Routing:** The UDP audio server directly routes voice frames marked as `AudioTypeHail (0x04)` between calling peers, bypassing standard radio channels and proximity logic.

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
