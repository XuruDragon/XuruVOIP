package admin

import (
	"fmt"
	"strings"

	"xuruvoip/server/voip/audio"
	"xuruvoip/server/voip/core"
)

// ExecuteAdminCommand executes administrative commands
func ExecuteAdminCommand(cmd core.AdminCommand) (bool, string) {
	switch cmd.Cmd {
	case "add_channel":
		name := strings.TrimSpace(cmd.Name)
		if name == "" {
			return false, "Empty channel name"
		}
		for _, c := range core.ServerConfig.ChannelsList {
			if strings.EqualFold(c, name) {
				return false, "This channel already exists"
			}
		}
		channels := append(core.ServerConfig.ChannelsList, name)
		if err := core.SaveChannels(channels); err != nil {
			return false, "Error saving data"
		}
		core.Log(fmt.Sprintf("ADMIN: Channel '%s' added", name), core.ColorBlue)
		core.ActiveHub.BroadcastPosMessageToAll(core.MsgChannelsList{
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
		for i, c := range core.ServerConfig.ChannelsList {
			if c == oldName {
				found = true
				idx = i
				break
			}
		}
		if !found {
			return false, "Channel not found"
		}
		for _, c := range core.ServerConfig.ChannelsList {
			if strings.EqualFold(c, newName) && c != oldName {
				return false, "The new name already exists"
			}
		}

		core.ServerConfig.ChannelsList[idx] = newName
		if err := core.SaveChannels(core.ServerConfig.ChannelsList); err != nil {
			return false, "Error saving data"
		}

		core.Log(fmt.Sprintf("ADMIN: Rename channel '%s' -> '%s'", oldName, newName), core.ColorBlue)
		core.ActiveHub.BroadcastPosMessageToAll(core.MsgChannelsList{
			Type:     "channels_list",
			Channels: core.ServerConfig.ChannelsList,
		})

		// Migrate players on that channel
		core.ActiveHub.Mu.Lock()
		for _, p := range core.ActiveHub.Players {
			modified := false
			if p.ActiveChannel == oldName {
				p.ActiveChannel = newName
				modified = true
				go core.ActiveHub.BroadcastPosMessageToAll(core.MsgPlayerChannel{
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
				_ = core.DBSavePlayerState(p.Name, p.Profile, p.ActiveChannel, p.ListeningChannels)
			}
		}
		core.ActiveHub.Mu.Unlock()
		return true, ""

	case "remove_channel":
		name := strings.TrimSpace(cmd.Name)
		if name == "General" {
			return false, "Cannot delete the default channel 'General'"
		}
		found := false
		var idx int
		for i, c := range core.ServerConfig.ChannelsList {
			if c == name {
				found = true
				idx = i
				break
			}
		}
		if !found {
			return false, "Channel not found"
		}

		channels := append(core.ServerConfig.ChannelsList[:idx], core.ServerConfig.ChannelsList[idx+1:]...)
		if err := core.SaveChannels(channels); err != nil {
			return false, "Error saving data"
		}

		core.Log(fmt.Sprintf("ADMIN: Channel '%s' removed", name), core.ColorBlue)
		core.ActiveHub.BroadcastPosMessageToAll(core.MsgChannelsList{
			Type:     "channels_list",
			Channels: channels,
		})

		// Reset players on that channel to General
		core.ActiveHub.Mu.Lock()
		for _, p := range core.ActiveHub.Players {
			modified := false
			if p.ActiveChannel == name {
				p.ActiveChannel = "General"
				modified = true
				go core.ActiveHub.BroadcastPosMessageToAll(core.MsgPlayerChannel{
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
				_ = core.DBSavePlayerState(p.Name, p.Profile, p.ActiveChannel, p.ListeningChannels)
			}
		}
		core.ActiveHub.Mu.Unlock()
		return true, ""

	case "add_profile":
		name := strings.TrimSpace(cmd.Name)
		if name == "" {
			return false, "Empty profile name"
		}
		for _, p := range core.ServerConfig.ProfilesList {
			if strings.EqualFold(p, name) {
				return false, "This profile already exists"
			}
		}
		profiles := append(core.ServerConfig.ProfilesList, name)
		if err := core.SaveProfiles(profiles); err != nil {
			return false, "Error saving data"
		}
		core.Log(fmt.Sprintf("ADMIN: Profile '%s' added", name), core.ColorBlue)
		core.ActiveHub.BroadcastPosMessageToAll(core.MsgProfilesList{
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
		for i, p := range core.ServerConfig.ProfilesList {
			if p == oldName {
				found = true
				idx = i
				break
			}
		}
		if !found {
			return false, "Profile not found"
		}
		for _, p := range core.ServerConfig.ProfilesList {
			if strings.EqualFold(p, newName) && p != oldName {
				return false, "The new name already exists"
			}
		}

		core.ServerConfig.ProfilesList[idx] = newName
		if err := core.SaveProfiles(core.ServerConfig.ProfilesList); err != nil {
			return false, "Error saving data"
		}

		core.Log(fmt.Sprintf("ADMIN: Rename profile '%s' -> '%s'", oldName, newName), core.ColorBlue)
		core.ActiveHub.BroadcastPosMessageToAll(core.MsgProfilesList{
			Type:     "profiles_list",
			Profiles: core.ServerConfig.ProfilesList,
		})

		// Migrate players
		core.ActiveHub.Mu.Lock()
		for _, p := range core.ActiveHub.Players {
			if p.Profile == oldName {
				p.Profile = newName
				_ = core.DBSavePlayerState(p.Name, p.Profile, p.ActiveChannel, p.ListeningChannels)
				go core.ActiveHub.BroadcastPosMessageToAll(core.MsgPlayerProfile{
					Type:    "player_profile",
					Name:    p.Name,
					Profile: newName,
				})
			}
		}
		core.ActiveHub.Mu.Unlock()
		return true, ""

	case "remove_profile":
		name := strings.TrimSpace(cmd.Name)
		found := false
		var idx int
		for i, p := range core.ServerConfig.ProfilesList {
			if p == name {
				found = true
				idx = i
				break
			}
		}
		if !found {
			return false, "Profile not found"
		}

		profiles := append(core.ServerConfig.ProfilesList[:idx], core.ServerConfig.ProfilesList[idx+1:]...)
		if err := core.SaveProfiles(profiles); err != nil {
			return false, "Error saving data"
		}

		core.Log(fmt.Sprintf("ADMIN: Profile '%s' removed", name), core.ColorBlue)
		core.ActiveHub.BroadcastPosMessageToAll(core.MsgProfilesList{
			Type:     "profiles_list",
			Profiles: profiles,
		})

		// Reset players with this profile to empty
		core.ActiveHub.Mu.Lock()
		for _, p := range core.ActiveHub.Players {
			if p.Profile == name {
				p.Profile = ""
				_ = core.DBSavePlayerState(p.Name, p.Profile, p.ActiveChannel, p.ListeningChannels)
				go core.ActiveHub.BroadcastPosMessageToAll(core.MsgPlayerProfile{
					Type:    "player_profile",
					Name:    p.Name,
					Profile: "",
				})
			}
		}
		core.ActiveHub.Mu.Unlock()
		return true, ""

	case "assign_profile":
		player := strings.TrimSpace(cmd.User)
		profile := strings.TrimSpace(cmd.Name)

		// Check if profile exists (or is empty to clear)
		if profile != "" {
			found := false
			for _, pr := range core.ServerConfig.ProfilesList {
				if pr == profile {
					found = true
					break
				}
			}
			if !found {
				return false, "Invalid profile"
			}
		}

		if ok := core.ActiveHub.AssignProfile(player, profile); !ok {
			return false, "Player not found"
		}

		core.Log(fmt.Sprintf("ADMIN: Profile of '%s' -> '%s'", player, profile), core.ColorBlue)
		core.ActiveHub.BroadcastPosMessageToAll(core.MsgPlayerProfile{
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
			for _, c := range core.ServerConfig.ChannelsList {
				if c == channel {
					valid = channel
					break
				}
			}
			if valid == "" {
				return false, "Invalid channel"
			}
		}
		if ok := core.ActiveHub.UpdateChannel(player, valid); !ok {
			return false, "Player not found"
		}
		core.ActiveHub.Mu.RLock()
		p, pExists := core.ActiveHub.Players[player]
		if pExists {
			_ = p.SafeWritePosJSON(core.MsgPlayerChannel{
				Type:    "player_channel",
				Name:    player,
				Channel: valid,
			})
		}
		core.ActiveHub.Mu.RUnlock()
		core.ActiveHub.BroadcastPosMessageToAll(core.MsgPlayerChannel{
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
				for _, c := range core.ServerConfig.ChannelsList {
					if c == chStr {
						channels = append(channels, chStr)
						break
					}
				}
			}
		}
		if ok := core.ActiveHub.UpdateListeningChannels(player, channels); !ok {
			return false, "Player not found"
		}
		core.ActiveHub.Mu.RLock()
		p, pExists := core.ActiveHub.Players[player]
		if pExists {
			_ = p.SafeWritePosJSON(core.MsgPlayerListening{
				Type:     "player_listening",
				Name:     player,
				Channels: channels,
			})
		}
		core.ActiveHub.Mu.RUnlock()
		core.ActiveHub.BroadcastPosMessageToAll(core.MsgPlayerListening{
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
		core.ActiveHub.SetAnonymousMode(active)
		core.Log(fmt.Sprintf("ADMIN: Global anonymous mode -> %t", active), core.ColorBlue)
		return true, ""

	case "kick_player":
		player := strings.TrimSpace(cmd.Name)
		if ok := core.ActiveHub.KickPlayer(player); !ok {
			return false, "Player not found"
		}
		core.Log(fmt.Sprintf("ADMIN: Kicked '%s'", player), core.ColorOrange)
		return true, ""

	case "set_server_token":
		pwd := strings.TrimSpace(cmd.Token)
		if err := core.SetServerPassword(pwd); err != nil {
			return false, err.Error()
		}
		if pwd == "" {
			core.Log("ADMIN: Server token removed (server is now public)", core.ColorBlue)
		} else {
			core.Log("ADMIN: Server token changed", core.ColorBlue)
		}
		return true, ""

	case "set_admin_token":
		pwd := strings.TrimSpace(cmd.Token)
		if pwd == "" {
			return false, "Empty token"
		}
		if err := core.SetAdminToken(pwd); err != nil {
			return false, err.Error()
		}
		core.Log("ADMIN: Admin token changed", core.ColorBlue)
		return true, ""

	case "ban_player":
		player := strings.TrimSpace(cmd.Name)
		ban, ok := cmd.Value.(bool)
		if !ok {
			return false, "Ban value required"
		}
		if err := core.DBBanPlayer(player, ban); err != nil {
			return false, err.Error()
		}
		status := "unbanned"
		if ban {
			status = "banned"
			var lastIP, hwid string
			_ = core.DB.QueryRow("SELECT last_ip, hwid FROM users WHERE LOWER(username) = LOWER(?)", player).Scan(&lastIP, &hwid)
			core.ActiveHub.KickPlayer(player)
			if lastIP != "" {
				core.ActiveHub.KickIP(lastIP)
			}
			if hwid != "" {
				core.ActiveHub.KickHwid(hwid)
			}
		}
		core.Log(fmt.Sprintf("ADMIN: Player '%s' was %s", player, status), core.ColorOrange)
		core.ActiveHub.BroadcastToAdmins(core.MsgAdminRefresh{Type: "admin_refresh", Tab: "accounts"})
		core.ActiveHub.BroadcastToAdmins(core.MsgAdminRefresh{Type: "admin_refresh", Tab: "bans"})
		return true, ""

	case "delete_player":
		player := strings.TrimSpace(cmd.Name)
		if err := core.DBDeletePlayerAccount(player); err != nil {
			return false, err.Error()
		}
		core.ActiveHub.KickPlayer(player)
		core.Log(fmt.Sprintf("ADMIN: Player account '%s' deleted", player), core.ColorOrange)
		core.ActiveHub.BroadcastToAdmins(core.MsgAdminRefresh{Type: "admin_refresh", Tab: "accounts"})
		return true, ""

	case "reset_player_password":
		player := strings.TrimSpace(cmd.Name)
		pwd := strings.TrimSpace(cmd.New)
		if pwd == "" {
			return false, "Empty password"
		}
		if err := core.DBResetPlayerPassword(player, pwd); err != nil {
			return false, err.Error()
		}
		core.Log(fmt.Sprintf("ADMIN: Password of '%s' reset", player), core.ColorBlue)
		core.ActiveHub.BroadcastToAdmins(core.MsgAdminRefresh{Type: "admin_refresh", Tab: "accounts"})
		return true, ""

	case "create_admin":
		username := strings.TrimSpace(cmd.Name)
		pwd := strings.TrimSpace(cmd.New)
		if username == "" || pwd == "" {
			return false, "Empty credentials"
		}
		if err := core.DBCreateAdmin(username, pwd); err != nil {
			return false, err.Error()
		}
		core.Log(fmt.Sprintf("ADMIN: New admin created: '%s'", username), core.ColorBlue)
		core.ActiveHub.BroadcastToAdmins(core.MsgAdminRefresh{Type: "admin_refresh", Tab: "admins"})
		return true, ""

	case "delete_admin":
		username := strings.TrimSpace(cmd.Name)
		if err := core.DBDeleteAdmin(username); err != nil {
			return false, err.Error()
		}
		core.Log(fmt.Sprintf("ADMIN: Admin deleted: '%s'", username), core.ColorOrange)
		core.ActiveHub.BroadcastToAdmins(core.MsgAdminRefresh{Type: "admin_refresh", Tab: "admins"})
		return true, ""

	case "change_admin_password":
		username := strings.TrimSpace(cmd.Name)
		pwd := strings.TrimSpace(cmd.New)
		if username == "" || pwd == "" {
			return false, "Empty credentials"
		}
		if err := core.DBChangeAdminPassword(username, pwd); err != nil {
			return false, err.Error()
		}
		core.Log(fmt.Sprintf("ADMIN: Password of admin '%s' changed", username), core.ColorBlue)
		core.ActiveHub.BroadcastToAdmins(core.MsgAdminRefresh{Type: "admin_refresh", Tab: "admins"})
		return true, ""

	case "add_banned_ip":
		ip := strings.TrimSpace(cmd.Name)
		reason := strings.TrimSpace(cmd.New)
		if ip == "" {
			return false, "IP required"
		}
		if err := core.DBAddBannedIP(ip, reason); err != nil {
			return false, err.Error()
		}
		core.ActiveHub.KickIP(ip)
		core.Log(fmt.Sprintf("ADMIN: IP banned: '%s'", ip), core.ColorOrange)
		core.ActiveHub.BroadcastToAdmins(core.MsgAdminRefresh{Type: "admin_refresh", Tab: "bans"})
		return true, ""

	case "remove_banned_ip":
		ip := strings.TrimSpace(cmd.Name)
		if err := core.DBRemoveBannedIP(ip); err != nil {
			return false, err.Error()
		}
		core.Log(fmt.Sprintf("ADMIN: IP unbanned: '%s'", ip), core.ColorBlue)
		core.ActiveHub.BroadcastToAdmins(core.MsgAdminRefresh{Type: "admin_refresh", Tab: "bans"})
		return true, ""

	case "add_banned_hwid":
		hwid := strings.TrimSpace(cmd.Name)
		reason := strings.TrimSpace(cmd.New)
		if hwid == "" {
			return false, "HWID required"
		}
		if err := core.DBAddBannedHwid(hwid, reason); err != nil {
			return false, err.Error()
		}
		core.ActiveHub.KickHwid(hwid)
		core.Log(fmt.Sprintf("ADMIN: HWID banned: '%s'", hwid), core.ColorOrange)
		core.ActiveHub.BroadcastToAdmins(core.MsgAdminRefresh{Type: "admin_refresh", Tab: "bans"})
		return true, ""

	case "remove_banned_hwid":
		hwid := strings.TrimSpace(cmd.Name)
		if err := core.DBRemoveBannedHwid(hwid); err != nil {
			return false, err.Error()
		}
		core.Log(fmt.Sprintf("ADMIN: HWID unbanned: '%s'", hwid), core.ColorBlue)
		core.ActiveHub.BroadcastToAdmins(core.MsgAdminRefresh{Type: "admin_refresh", Tab: "bans"})
		return true, ""

	case "set_aar_recording_target":
		target := strings.TrimSpace(cmd.Name)
		if target == "" {
			return false, "Empty target name"
		}
		active, ok := cmd.Value.(bool)
		if !ok {
			return false, "Boolean value required"
		}
		audio.SetAarRecordingTarget(target, active)
		core.Log(fmt.Sprintf("ADMIN: AAR Recording Target '%s' active -> %t", target, active), core.ColorBlue)
		core.ActiveHub.BroadcastToAdmins(core.MsgAdminRefresh{Type: "admin_refresh", Tab: "aar"})
		return true, ""

	case "get_players_list", "get_admins_list", "get_banned_ips_list", "get_banned_hwids_list", "get_aar_recording_status":
		return true, ""
	}

	return false, "Unknown command"
}

// ExecuteAdminQuery executes administrative queries
func ExecuteAdminQuery(cmd core.AdminCommand) interface{} {
	switch cmd.Cmd {
	case "get_players_list":
		list, err := core.DBGetPlayersList()
		if err != nil {
			return nil
		}
		return list
	case "get_admins_list":
		list, err := core.DBGetAdminsList()
		if err != nil {
			return nil
		}
		return list
	case "get_banned_ips_list":
		list, err := core.DBGetBannedIPsList()
		if err != nil {
			return nil
		}
		return list
	case "get_banned_hwids_list":
		list, err := core.DBGetBannedHwidsList()
		if err != nil {
			return nil
		}
		return list
	case "get_aar_recording_status":
		return audio.GetAarRecordingStatus()
	}
	return nil
}
