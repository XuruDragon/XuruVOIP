package main

import (
	"encoding/json"
	"fmt"
	"math"
	"net"
	"net/http"
	"strings"
	"time"

	"github.com/gorilla/websocket"
)

var upgrader = websocket.Upgrader{
	CheckOrigin: func(r *http.Request) bool {
		return true // Allow all origins for local gaming networks
	},
}

// Security managers
var (
	posLockout *AuthLockout
	posLimit   *RateLimiterHub
)

// StartPositionsServer starts the position server on the specified port
func StartPositionsServer(port int, certFile, keyFile string) {
	mux := http.NewServeMux()
	RegisterWebAdminHandlers(mux)
	mux.HandleFunc("/", handlePositionsWS)

	server := &http.Server{
		Addr:    fmt.Sprintf("%s:%d", BindIP, port),
		Handler: mux,
	}

	Log(fmt.Sprintf("Starting positions server on %s:%d (WSS)...", BindIP, port), ColorBlue)
	var err error
	if certFile != "" && keyFile != "" {
		err = server.ListenAndServeTLS(certFile, keyFile)
	} else {
		err = server.ListenAndServe()
	}

	if err != nil {
		Log(fmt.Sprintf("Positions server error: %v", err), ColorRed)
	}
}

// ExtractIP extracts the client IP address from a remote address string
func ExtractIP(remoteAddr string) string {
	ip, _, err := net.SplitHostPort(remoteAddr)
	if err != nil {
		return remoteAddr
	}
	return ip
}

func handlePositionsWS(w http.ResponseWriter, r *http.Request) {
	conn, err := upgrader.Upgrade(w, r, nil)
	if err != nil {
		return
	}
	defer conn.Close()

	ip := ExtractIP(r.RemoteAddr)

	// Check brute force lockout
	if posLockout.IsBanned(ip) {
		Log(fmt.Sprintf("REJECT Positions: IP %s temporarily banned", ip), ColorRed)
		_ = conn.WriteControl(
			websocket.CloseMessage,
			websocket.FormatCloseMessage(websocket.ClosePolicyViolation, "banned"),
			time.Now().Add(time.Second),
		)
		return
	}

	// 1. Read first authentication/join message
	_, payload, err := conn.ReadMessage()
	if err != nil {
		return
	}

	var base MessageBase
	if err := json.Unmarshal(payload, &base); err != nil {
		return
	}

	switch base.Type {
	case "auth_admin":
		var msg MsgAuthAdmin
		if err := json.Unmarshal(payload, &msg); err != nil {
			return
		}
		handleAdminAuth(conn, ip, msg)

	case "join":
		var msg MsgJoin
		if err := json.Unmarshal(payload, &msg); err != nil {
			return
		}
		handlePlayerJoin(conn, ip, msg)

	default:
		_ = conn.WriteControl(
			websocket.CloseMessage,
			websocket.FormatCloseMessage(websocket.ClosePolicyViolation, "invalid_initial_message"),
			time.Now().Add(time.Second),
		)
	}
}

func handleAdminAuth(conn *websocket.Conn, ip string, msg MsgAuthAdmin) {
	if serverConfig.AdminServerToken != "" && !ConstantTimeCompare(msg.ServerPassword, serverConfig.AdminServerToken) {
		posLockout.RecordFailure(ip)
		Log(fmt.Sprintf("REJECT Admin: Incorrect server password from %s (user: %s)", ip, msg.Username), ColorRed)
		_ = conn.WriteJSON(MsgError{
			Type:    "error",
			Reason:  "invalid_server_password",
			Message: "Incorrect server password",
		})
		return
	}

	ok, err := AuthenticateAdmin(msg.Username, msg.Password)
	if err != nil || !ok {
		posLockout.RecordFailure(ip)
		Log(fmt.Sprintf("REJECT Admin: Invalid credentials from %s (user: %s)", ip, msg.Username), ColorRed)
		_ = conn.WriteJSON(MsgError{
			Type:    "error",
			Reason:  "invalid_admin_credentials",
			Message: "Invalid administrator credentials",
		})
		return
	}

	// Send admin welcome state
	welcome := MsgAdminWelcome{
		Type:          "admin_welcome",
		Channels:      serverConfig.ChannelsList,
		Profiles:      serverConfig.ProfilesList,
		Players:       hub.GetAllPlayerStates(),
		AnonymousMode: hub.GetAnonymousMode(),
	}
	if err := conn.WriteJSON(welcome); err != nil {
		return
	}

	posLockout.RecordSuccess(ip)
	hub.RegisterAdmin(conn)
	defer hub.UnregisterAdmin(conn)

	Log(fmt.Sprintf("ADMIN connected from %s", ip), ColorGreen)

	// Admin Command Loop
	for {
		_, payload, err := conn.ReadMessage()
		if err != nil {
			break
		}

		var cmd AdminCommand
		if err := json.Unmarshal(payload, &cmd); err != nil {
			continue
		}

		ok, reason := executeAdminCommand(cmd)
		val := executeAdminQuery(cmd)
		response := MsgAdminResponse{
			Type:   "admin_response",
			ReqID:  cmd.ReqID,
			Cmd:    cmd.Cmd,
			Ok:     ok,
			Reason: reason,
			Value:  val,
		}
		_ = conn.WriteJSON(response)
	}

	Log("ADMIN disconnected", ColorOrange)
}

func executeAdminCommand(cmd AdminCommand) (bool, string) {
	switch cmd.Cmd {
	case "add_channel":
		name := strings.TrimSpace(cmd.Name)
		if name == "" {
			return false, "Empty channel name"
		}
		for _, c := range serverConfig.ChannelsList {
			if strings.EqualFold(c, name) {
				return false, "This channel already exists"
			}
		}
		channels := append(serverConfig.ChannelsList, name)
		if err := SaveChannels(channels); err != nil {
			return false, "Error saving data"
		}
		Log(fmt.Sprintf("ADMIN: Channel '%s' added", name), ColorBlue)
		hub.BroadcastPosMessageToAll(MsgChannelsList{
			Type:     "channels_list",
			Channels: channels,
		})
		return true, ""

	case "rename_channel":
		oldName := strings.TrimSpace(cmd.Old)
		newName := strings.TrimSpace(cmd.New)
		if oldName == "" || newName == "" {
			return false, "Invalid names"
		}
		found := false
		var idx int
		for i, c := range serverConfig.ChannelsList {
			if c == oldName {
				found = true
				idx = i
				break
			}
		}
		if !found {
			return false, "Channel not found"
		}
		for _, c := range serverConfig.ChannelsList {
			if strings.EqualFold(c, newName) && c != oldName {
				return false, "The new name already exists"
			}
		}

		serverConfig.ChannelsList[idx] = newName
		if err := SaveChannels(serverConfig.ChannelsList); err != nil {
			return false, "Error saving data"
		}

		Log(fmt.Sprintf("ADMIN: Rename channel '%s' -> '%s'", oldName, newName), ColorBlue)
		hub.BroadcastPosMessageToAll(MsgChannelsList{
			Type:     "channels_list",
			Channels: serverConfig.ChannelsList,
		})

		// Migrate players on that channel
		hub.mu.Lock()
		for _, p := range hub.players {
			modified := false
			if p.ActiveChannel == oldName {
				p.ActiveChannel = newName
				modified = true
				go hub.BroadcastPosMessageToAll(MsgPlayerChannel{
					Type:    "player_channel",
					Name:    p.Name,
					Channel: newName,
				})
			}
			for idx, ch := range p.ListeningChannels {
				if ch == oldName {
					p.ListeningChannels[idx] = newName
					modified = true
				}
			}
			if modified {
				_ = DBSavePlayerState(p.Name, p.Profile, p.ActiveChannel, p.ListeningChannels)
			}
		}
		hub.mu.Unlock()
		return true, ""

	case "remove_channel":
		name := strings.TrimSpace(cmd.Name)
		if name == "General" {
			return false, "Cannot delete the default channel 'General'"
		}
		found := false
		var idx int
		for i, c := range serverConfig.ChannelsList {
			if c == name {
				found = true
				idx = i
				break
			}
		}
		if !found {
			return false, "Channel not found"
		}

		channels := append(serverConfig.ChannelsList[:idx], serverConfig.ChannelsList[idx+1:]...)
		if err := SaveChannels(channels); err != nil {
			return false, "Error saving data"
		}

		Log(fmt.Sprintf("ADMIN: Channel '%s' removed", name), ColorBlue)
		hub.BroadcastPosMessageToAll(MsgChannelsList{
			Type:     "channels_list",
			Channels: channels,
		})

		// Reset players on that channel to General
		hub.mu.Lock()
		for _, p := range hub.players {
			modified := false
			if p.ActiveChannel == name {
				p.ActiveChannel = "General"
				modified = true
				go hub.BroadcastPosMessageToAll(MsgPlayerChannel{
					Type:    "player_channel",
					Name:    p.Name,
					Channel: "General",
				})
			}
			var remaining []string
			for _, ch := range p.ListeningChannels {
				if ch != name {
					remaining = append(remaining, ch)
				} else {
					modified = true
				}
			}
			if modified {
				p.ListeningChannels = remaining
				_ = DBSavePlayerState(p.Name, p.Profile, p.ActiveChannel, p.ListeningChannels)
			}
		}
		hub.mu.Unlock()
		return true, ""

	case "add_profile":
		name := strings.TrimSpace(cmd.Name)
		if name == "" {
			return false, "Empty profile name"
		}
		for _, p := range serverConfig.ProfilesList {
			if strings.EqualFold(p, name) {
				return false, "This profile already exists"
			}
		}
		profiles := append(serverConfig.ProfilesList, name)
		if err := SaveProfiles(profiles); err != nil {
			return false, "Error saving data"
		}
		Log(fmt.Sprintf("ADMIN: Profile '%s' added", name), ColorBlue)
		hub.BroadcastPosMessageToAll(MsgProfilesList{
			Type:     "profiles_list",
			Profiles: profiles,
		})
		return true, ""

	case "rename_profile":
		oldName := strings.TrimSpace(cmd.Old)
		newName := strings.TrimSpace(cmd.New)
		if oldName == "" || newName == "" {
			return false, "Invalid names"
		}
		found := false
		var idx int
		for i, p := range serverConfig.ProfilesList {
			if p == oldName {
				found = true
				idx = i
				break
			}
		}
		if !found {
			return false, "Profile not found"
		}
		for _, p := range serverConfig.ProfilesList {
			if strings.EqualFold(p, newName) && p != oldName {
				return false, "The new name already exists"
			}
		}

		serverConfig.ProfilesList[idx] = newName
		if err := SaveProfiles(serverConfig.ProfilesList); err != nil {
			return false, "Error saving data"
		}

		Log(fmt.Sprintf("ADMIN: Rename profile '%s' -> '%s'", oldName, newName), ColorBlue)
		hub.BroadcastPosMessageToAll(MsgProfilesList{
			Type:     "profiles_list",
			Profiles: serverConfig.ProfilesList,
		})

		// Migrate players
		hub.mu.Lock()
		for _, p := range hub.players {
			if p.Profile == oldName {
				p.Profile = newName
				_ = DBSavePlayerState(p.Name, p.Profile, p.ActiveChannel, p.ListeningChannels)
				go hub.BroadcastPosMessageToAll(MsgPlayerProfile{
					Type:    "player_profile",
					Name:    p.Name,
					Profile: newName,
				})
			}
		}
		hub.mu.Unlock()
		return true, ""

	case "remove_profile":
		name := strings.TrimSpace(cmd.Name)
		found := false
		var idx int
		for i, p := range serverConfig.ProfilesList {
			if p == name {
				found = true
				idx = i
				break
			}
		}
		if !found {
			return false, "Profile not found"
		}

		profiles := append(serverConfig.ProfilesList[:idx], serverConfig.ProfilesList[idx+1:]...)
		if err := SaveProfiles(profiles); err != nil {
			return false, "Error saving data"
		}

		Log(fmt.Sprintf("ADMIN: Profile '%s' removed", name), ColorBlue)
		hub.BroadcastPosMessageToAll(MsgProfilesList{
			Type:     "profiles_list",
			Profiles: profiles,
		})

		// Reset players with this profile to empty
		hub.mu.Lock()
		for _, p := range hub.players {
			if p.Profile == name {
				p.Profile = ""
				_ = DBSavePlayerState(p.Name, p.Profile, p.ActiveChannel, p.ListeningChannels)
				go hub.BroadcastPosMessageToAll(MsgPlayerProfile{
					Type:    "player_profile",
					Name:    p.Name,
					Profile: "",
				})
			}
		}
		hub.mu.Unlock()
		return true, ""

	case "assign_profile":
		player := strings.TrimSpace(cmd.User)
		profile := strings.TrimSpace(cmd.Name)

		// Check if profile exists (or is empty to clear)
		if profile != "" {
			found := false
			for _, pr := range serverConfig.ProfilesList {
				if pr == profile {
					found = true
					break
				}
			}
			if !found {
				return false, "Invalid profile"
			}
		}

		if ok := hub.AssignProfile(player, profile); !ok {
			return false, "Player not found"
		}

		Log(fmt.Sprintf("ADMIN: Profile of '%s' -> '%s'", player, profile), ColorBlue)
		hub.BroadcastPosMessageToAll(MsgPlayerProfile{
			Type:    "player_profile",
			Name:    player,
			Profile: profile,
		})
		return true, ""

	case "assign_channel":
		player := strings.TrimSpace(cmd.User)
		channel := strings.TrimSpace(cmd.Name)
		valid := ""
		if channel != "" {
			for _, c := range serverConfig.ChannelsList {
				if c == channel {
					valid = channel
					break
				}
			}
			if valid == "" {
				return false, "Invalid channel"
			}
		}
		if ok := hub.UpdateChannel(player, valid); !ok {
			return false, "Player not found"
		}
		hub.mu.RLock()
		p, pExists := hub.players[player]
		if pExists {
			_ = p.SafeWritePosJSON(MsgPlayerChannel{
				Type:    "player_channel",
				Name:    player,
				Channel: valid,
			})
		}
		hub.mu.RUnlock()
		hub.BroadcastPosMessageToAll(MsgPlayerChannel{
			Type:    "player_channel",
			Name:    player,
			Channel: valid,
		})
		return true, ""

	case "assign_listening_channels":
		player := strings.TrimSpace(cmd.User)
		valSlice, ok := cmd.Value.([]interface{})
		if !ok {
			return false, "Channels array required"
		}
		var channels []string
		for _, item := range valSlice {
			if chStr, ok := item.(string); ok {
				for _, c := range serverConfig.ChannelsList {
					if c == chStr {
						channels = append(channels, chStr)
						break
					}
				}
			}
		}
		if ok := hub.UpdateListeningChannels(player, channels); !ok {
			return false, "Player not found"
		}
		hub.mu.RLock()
		p, pExists := hub.players[player]
		if pExists {
			_ = p.SafeWritePosJSON(MsgPlayerListening{
				Type:     "player_listening",
				Name:     player,
				Channels: channels,
			})
		}
		hub.mu.RUnlock()
		hub.BroadcastPosMessageToAll(MsgPlayerListening{
			Type:     "player_listening",
			Name:     player,
			Channels: channels,
		})
		return true, ""

	case "set_anonymous_mode":
		active, ok := cmd.Value.(bool)
		if !ok {
			return false, "Boolean value required"
		}
		hub.SetAnonymousMode(active)
		Log(fmt.Sprintf("ADMIN: Global anonymous mode -> %t", active), ColorBlue)
		return true, ""

	case "kick_player":
		player := strings.TrimSpace(cmd.Name)
		if ok := hub.KickPlayer(player); !ok {
			return false, "Player not found"
		}
		Log(fmt.Sprintf("ADMIN: Kicked '%s'", player), ColorOrange)
		return true, ""

	case "set_server_token":
		pwd := strings.TrimSpace(cmd.Token)
		if err := SetServerPassword(pwd); err != nil {
			return false, err.Error()
		}
		if pwd == "" {
			Log("ADMIN: Server token removed (server is now public)", ColorBlue)
		} else {
			Log("ADMIN: Server token changed", ColorBlue)
		}
		return true, ""

	case "set_admin_token":
		pwd := strings.TrimSpace(cmd.Token)
		if pwd == "" {
			return false, "Empty token"
		}
		if err := SetAdminToken(pwd); err != nil {
			return false, err.Error()
		}
		Log("ADMIN: Admin token changed", ColorBlue)
		return true, ""

	case "ban_player":
		player := strings.TrimSpace(cmd.Name)
		ban, ok := cmd.Value.(bool)
		if !ok {
			return false, "Ban value required"
		}
		if err := DBBanPlayer(player, ban); err != nil {
			return false, err.Error()
		}
		status := "unbanned"
		if ban {
			status = "banned"
			var lastIP, hwid string
			_ = db.QueryRow("SELECT last_ip, hwid FROM users WHERE LOWER(username) = LOWER(?)", player).Scan(&lastIP, &hwid)
			hub.KickPlayer(player)
			if lastIP != "" {
				hub.KickIP(lastIP)
			}
			if hwid != "" {
				hub.KickHwid(hwid)
			}
		}
		Log(fmt.Sprintf("ADMIN: Player '%s' was %s", player, status), ColorOrange)
		hub.BroadcastToAdmins(MsgAdminRefresh{Type: "admin_refresh", Tab: "accounts"})
		hub.BroadcastToAdmins(MsgAdminRefresh{Type: "admin_refresh", Tab: "bans"})
		return true, ""

	case "delete_player":
		player := strings.TrimSpace(cmd.Name)
		if err := DBDeletePlayerAccount(player); err != nil {
			return false, err.Error()
		}
		hub.KickPlayer(player)
		Log(fmt.Sprintf("ADMIN: Player account '%s' deleted", player), ColorOrange)
		hub.BroadcastToAdmins(MsgAdminRefresh{Type: "admin_refresh", Tab: "accounts"})
		return true, ""

	case "reset_player_password":
		player := strings.TrimSpace(cmd.Name)
		pwd := strings.TrimSpace(cmd.New)
		if pwd == "" {
			return false, "Empty password"
		}
		if err := DBResetPlayerPassword(player, pwd); err != nil {
			return false, err.Error()
		}
		Log(fmt.Sprintf("ADMIN: Password of '%s' reset", player), ColorBlue)
		hub.BroadcastToAdmins(MsgAdminRefresh{Type: "admin_refresh", Tab: "accounts"})
		return true, ""

	case "create_admin":
		username := strings.TrimSpace(cmd.Name)
		pwd := strings.TrimSpace(cmd.New)
		if username == "" || pwd == "" {
			return false, "Empty credentials"
		}
		if err := DBCreateAdmin(username, pwd); err != nil {
			return false, err.Error()
		}
		Log(fmt.Sprintf("ADMIN: New admin created: '%s'", username), ColorBlue)
		hub.BroadcastToAdmins(MsgAdminRefresh{Type: "admin_refresh", Tab: "admins"})
		return true, ""

	case "delete_admin":
		username := strings.TrimSpace(cmd.Name)
		if err := DBDeleteAdmin(username); err != nil {
			return false, err.Error()
		}
		Log(fmt.Sprintf("ADMIN: Admin deleted: '%s'", username), ColorOrange)
		hub.BroadcastToAdmins(MsgAdminRefresh{Type: "admin_refresh", Tab: "admins"})
		return true, ""

	case "change_admin_password":
		username := strings.TrimSpace(cmd.Name)
		pwd := strings.TrimSpace(cmd.New)
		if username == "" || pwd == "" {
			return false, "Empty credentials"
		}
		if err := DBChangeAdminPassword(username, pwd); err != nil {
			return false, err.Error()
		}
		Log(fmt.Sprintf("ADMIN: Password of admin '%s' changed", username), ColorBlue)
		hub.BroadcastToAdmins(MsgAdminRefresh{Type: "admin_refresh", Tab: "admins"})
		return true, ""

	case "add_banned_ip":
		ip := strings.TrimSpace(cmd.Name)
		reason := strings.TrimSpace(cmd.New)
		if ip == "" {
			return false, "IP required"
		}
		if err := DBAddBannedIP(ip, reason); err != nil {
			return false, err.Error()
		}
		hub.KickIP(ip)
		Log(fmt.Sprintf("ADMIN: IP banned: '%s'", ip), ColorOrange)
		hub.BroadcastToAdmins(MsgAdminRefresh{Type: "admin_refresh", Tab: "bans"})
		return true, ""

	case "remove_banned_ip":
		ip := strings.TrimSpace(cmd.Name)
		if err := DBRemoveBannedIP(ip); err != nil {
			return false, err.Error()
		}
		Log(fmt.Sprintf("ADMIN: IP unbanned: '%s'", ip), ColorBlue)
		hub.BroadcastToAdmins(MsgAdminRefresh{Type: "admin_refresh", Tab: "bans"})
		return true, ""

	case "add_banned_hwid":
		hwid := strings.TrimSpace(cmd.Name)
		reason := strings.TrimSpace(cmd.New)
		if hwid == "" {
			return false, "HWID required"
		}
		if err := DBAddBannedHwid(hwid, reason); err != nil {
			return false, err.Error()
		}
		hub.KickHwid(hwid)
		Log(fmt.Sprintf("ADMIN: HWID banned: '%s'", hwid), ColorOrange)
		hub.BroadcastToAdmins(MsgAdminRefresh{Type: "admin_refresh", Tab: "bans"})
		return true, ""

	case "remove_banned_hwid":
		hwid := strings.TrimSpace(cmd.Name)
		if err := DBRemoveBannedHwid(hwid); err != nil {
			return false, err.Error()
		}
		Log(fmt.Sprintf("ADMIN: HWID unbanned: '%s'", hwid), ColorBlue)
		hub.BroadcastToAdmins(MsgAdminRefresh{Type: "admin_refresh", Tab: "bans"})
		return true, ""

	case "get_players_list", "get_admins_list", "get_banned_ips_list", "get_banned_hwids_list":
		return true, ""
	}

	return false, "Unknown command"
}

func executeAdminQuery(cmd AdminCommand) interface{} {
	switch cmd.Cmd {
	case "get_players_list":
		list, err := DBGetPlayersList()
		if err != nil {
			return nil
		}
		return list
	case "get_admins_list":
		list, err := DBGetAdminsList()
		if err != nil {
			return nil
		}
		return list
	case "get_banned_ips_list":
		list, err := DBGetBannedIPsList()
		if err != nil {
			return nil
		}
		return list
	case "get_banned_hwids_list":
		list, err := DBGetBannedHwidsList()
		if err != nil {
			return nil
		}
		return list
	}
	return nil
}

func handlePlayerJoin(conn *websocket.Conn, ip string, msg MsgJoin) {
	if !PublicServer && serverConfig.ServerToken != "" && !ConstantTimeCompare(msg.Token, serverConfig.ServerToken) {
		posLockout.RecordFailure(ip)
		Log(fmt.Sprintf("REJECT: Invalid token from %s (client: %s)", ip, msg.Name), ColorRed)
		_ = conn.WriteJSON(MsgError{
			Type:    "error",
			Reason:  "invalid_token",
			Message: "Invalid server token",
		})
		return
	}

	posLockout.RecordSuccess(ip)

	// Limit capacity
	hub.mu.RLock()
	playerCount := len(hub.players)
	hub.mu.RUnlock()

	if playerCount >= MaxPlayers {
		Log(fmt.Sprintf("REJECT: Server full from %s (client: %s)", ip, msg.Name), ColorOrange)
		_ = conn.WriteJSON(MsgError{
			Type:    "error",
			Reason:  "server_full",
			Message: fmt.Sprintf("Server full (%d players max)", MaxPlayers),
		})
		return
	}

	name := strings.TrimSpace(msg.Name)
	if name == "" {
		name = fmt.Sprintf("Player_%d", playerCount+1)
	}

	// Validate channel
	channel := strings.TrimSpace(msg.Channel)
	validCh := "General"
	for _, c := range serverConfig.ChannelsList {
		if c == channel {
			validCh = channel
			break
		}
	}

	if strings.TrimSpace(msg.Password) == "" {
		posLockout.RecordFailure(ip)
		Log(fmt.Sprintf("REJECT: Empty password for player '%s' from %s", name, ip), ColorRed)
		_ = conn.WriteJSON(MsgError{
			Type:    "error",
			Reason:  "invalid_password",
			Message: "User password is required to secure account",
		})
		return
	}

	// Authenticate or auto-register player account
	ok, isBanned, err := AuthenticatePlayer(name, msg.Password, validCh, ip, msg.Hwid)
	if err != nil {
		posLockout.RecordFailure(ip)
		_ = conn.WriteJSON(MsgError{
			Type:    "error",
			Reason:  "db_error",
			Message: "Internal database error",
		})
		return
	}
	if isBanned {
		posLockout.RecordFailure(ip)
		Log(fmt.Sprintf("REJECT: Player '%s' banned (IP: %s, HWID: %s)", name, ip, msg.Hwid), ColorOrange)
		_ = conn.WriteJSON(MsgError{
			Type:    "error",
			Reason:  "player_banned",
			Message: "This player account, IP, or HWID is banned from this server",
		})
		return
	}
	if !ok {
		posLockout.RecordFailure(ip)
		Log(fmt.Sprintf("REJECT: Incorrect password for player '%s' from %s", name, ip), ColorRed)
		_ = conn.WriteJSON(MsgError{
			Type:    "error",
			Reason:  "invalid_password",
			Message: "Incorrect player password",
		})
		return
	}

	// Register player and issue ticket
	ticket := hub.RegisterPlayer(name, conn, validCh, ip, msg.Hwid)

	Log(fmt.Sprintf("JOIN Positions: %s (%s)", name, ip), ColorGreen)

	// Send welcome packet
	hub.mu.RLock()
	myPlayer, myPlayerExists := hub.players[name]
	var myActiveChan string
	var myListeningChs []string
	var myProfile string
	if myPlayerExists {
		myActiveChan = myPlayer.ActiveChannel
		myListeningChs = myPlayer.ListeningChannels
		myProfile = myPlayer.Profile
	} else {
		myActiveChan = validCh
	}
	hub.mu.RUnlock()

	welcome := MsgWelcome{
		Type:                  "welcome",
		Players:               hub.GetPlayerStateList(name),
		AnonymousMode:         hub.GetAnonymousMode(),
		Channels:              serverConfig.ChannelsList,
		Profiles:              serverConfig.ProfilesList,
		MyActiveChannel:       myActiveChan,
		MyListeningChs:        myListeningChs,
		MyProfile:             myProfile,
		AudioTicket:           ticket,
		SpatialAudioSupported: SpatialAudioEnabled,
	}

	if myPlayerExists {
		if err := myPlayer.SafeWritePosJSON(welcome); err != nil {
			return
		}
	} else {
		if err := conn.WriteJSON(welcome); err != nil {
			return
		}
	}

	// Notify other players and admins
	joinMsg := MsgPlayerJoin{
		Type:              "join",
		Name:              name,
		ActiveChannel:     myActiveChan,
		ListeningChannels: myListeningChs,
		Profile:           myProfile,
		ProxShort:         false,
	}
	hub.BroadcastPosMessage(name, joinMsg)
	hub.BroadcastToAdmins(joinMsg)

	// Message Loop for this player
	for {
		_, payload, err := conn.ReadMessage()
		if err != nil {
			break
		}

		// Rate Limiting
		if !posLimit.Allow(conn) {
			continue
		}

		var base MessageBase
		if err := json.Unmarshal(payload, &base); err != nil {
			continue
		}

		switch base.Type {
		case "pos":
			var m MsgPos
			if err := json.Unmarshal(payload, &m); err != nil {
				continue
			}
			// Validate position inputs (prevent NaNs / Infs)
			if mathIsInvalid(m.Pos.X) || mathIsInvalid(m.Pos.Y) || mathIsInvalid(m.Pos.Z) {
				continue
			}

			if ok := hub.UpdatePosition(name, m.Pos); ok {
				hub.BroadcastPosMessage(name, MsgPlayerPos{
					Type:      "pos",
					Name:      name,
					Pos:       m.Pos,
					TsCapture: m.TsCapture,
				})
			}

		case "ping":
			hub.UpdateActivity(name)
			hub.mu.RLock()
			p, ok := hub.players[name]
			hub.mu.RUnlock()
			if ok {
				_ = p.SafeWritePosJSON(MsgPong{Type: "pong"})
			}

		case "sc_offline":
			if ok := hub.UpdateScOnline(name, false); ok {
				Log(fmt.Sprintf("%s: Game closed (OCR inactive)", name), ColorOrange)
				hub.BroadcastPosMessage(name, MsgScStatus{
					Type: "sc_offline",
					Name: name,
				})
			}

		case "sc_online":
			if ok := hub.UpdateScOnline(name, true); ok {
				Log(fmt.Sprintf("%s: Game detected (OCR active)", name), ColorGreen)
				hub.BroadcastPosMessage(name, MsgScStatus{
					Type: "sc_online",
					Name: name,
				})
			}

		case "helmet":
			var m MsgHelmet
			if err := json.Unmarshal(payload, &m); err != nil {
				continue
			}
			if ok := hub.UpdateHelmet(name, m.HelmetOn); ok {
				status := "OFF"
				if m.HelmetOn {
					status = "ON"
				}
				Log(fmt.Sprintf("%s: Helmet %s", name, status), ColorBlue)
				hub.BroadcastPosMessage(name, MsgPlayerHelmet{
					Type:     "helmet",
					Name:     name,
					HelmetOn: m.HelmetOn,
				})
			}

		case "set_channel":
			var m MsgSetChannel
			if err := json.Unmarshal(payload, &m); err != nil {
				continue
			}
			ch := strings.TrimSpace(m.Channel)
			valid := ""
			if ch != "" {
				for _, c := range serverConfig.ChannelsList {
					if c == ch {
						valid = ch
						break
					}
				}
			}
			if ok := hub.UpdateChannel(name, valid); ok {
				Log(fmt.Sprintf("%s: Radio -> %s", name, valid), ColorBlue)
				hub.BroadcastPosMessageToAll(MsgPlayerChannel{
					Type:    "player_channel",
					Name:    name,
					Channel: valid,
				})
			}

		case "listen_channels":
			var m MsgListenChannels
			if err := json.Unmarshal(payload, &m); err != nil {
				continue
			}
			var valid []string
			for _, ch := range m.Channels {
				trimmed := strings.TrimSpace(ch)
				for _, c := range serverConfig.ChannelsList {
					if c == trimmed {
						valid = append(valid, trimmed)
						break
					}
				}
			}
			if ok := hub.UpdateListeningChannels(name, valid); ok {
				Log(fmt.Sprintf("%s: Listening -> %v", name, valid), ColorBlue)
				hub.BroadcastPosMessageToAll(MsgPlayerListening{
					Type:     "player_listening",
					Name:     name,
					Channels: valid,
				})
			}

		case "prox_short":
			var m MsgProxShort
			if err := json.Unmarshal(payload, &m); err != nil {
				continue
			}
			if ok := hub.UpdateProxShort(name, m.Active); ok {
				status := "50m"
				if m.Active {
					status = "5m"
				}
				Log(fmt.Sprintf("%s: Proximity -> %s", name, status), ColorBlue)
				hub.BroadcastPosMessageToAll(MsgPlayerProxShort{
					Type:   "player_prox_short",
					Name:   name,
					Active: m.Active,
				})
			}
		}
	}

	// Disconnection cleanup
	if leftName, fullyLeft := hub.UnregisterPosConn(conn); leftName != "" {
		posLimit.Forget(conn)
		if fullyLeft {
			Log(fmt.Sprintf("LEAVE: %s (disconnected)", leftName), ColorOrange)
			hub.BroadcastPosMessageToAll(MsgPlayerLeave{
				Type: "leave",
				Name: leftName,
			})
		}
	}
}

// mathIsInvalid checks if float is NaN or Infinite
func mathIsInvalid(f float64) bool {
	return f != f || f == math.MaxFloat64 || f == -math.MaxFloat64
}
