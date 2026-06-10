package position

import (
	"encoding/json"
	"fmt"
	"math"
	"net/http"
	"strings"
	"time"

	"github.com/gorilla/websocket"
	"xuruvoip/server/voip/admin"
	"xuruvoip/server/voip/core"
)

var upgrader = websocket.Upgrader{
	CheckOrigin: func(r *http.Request) bool {
		return true // Allow all origins for local gaming networks
	},
}

// StartPositionsServer starts the position server on the specified port
func StartPositionsServer(port int, certFile, keyFile string) {
	mux := http.NewServeMux()
	admin.RegisterWebAdminHandlers(mux)
	mux.HandleFunc("/", handlePositionsWS)

	server := &http.Server{
		Addr:    fmt.Sprintf("%s:%d", core.BindIP, port),
		Handler: mux,
	}

	core.Log(fmt.Sprintf("Starting positions server on %s:%d (WSS)...", core.BindIP, port), core.ColorBlue)
	var err error
	if certFile != "" && keyFile != "" {
		err = server.ListenAndServeTLS(certFile, keyFile)
	} else {
		err = server.ListenAndServe()
	}

	if err != nil {
		core.Log(fmt.Sprintf("Positions server error: %v", err), core.ColorRed)
	}
}

func handlePositionsWS(w http.ResponseWriter, r *http.Request) {
	conn, err := upgrader.Upgrade(w, r, nil)
	if err != nil {
		return
	}
	defer conn.Close()

	ip := core.ExtractIP(r.RemoteAddr)

	// Check brute force lockout
	if core.PosLockout.IsBanned(ip) {
		core.Log(fmt.Sprintf("REJECT Positions: IP %s temporarily banned", ip), core.ColorRed)
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

	var base core.MessageBase
	if err := json.Unmarshal(payload, &base); err != nil {
		return
	}

	switch base.Type {
	case "auth_admin":
		var msg core.MsgAuthAdmin
		if err := json.Unmarshal(payload, &msg); err != nil {
			return
		}
		handleAdminAuth(conn, ip, msg)

	case "join":
		var msg core.MsgJoin
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

func handleAdminAuth(conn *websocket.Conn, ip string, msg core.MsgAuthAdmin) {
	if core.ServerConfig.AdminServerToken != "" && !core.ConstantTimeCompare(msg.ServerPassword, core.ServerConfig.AdminServerToken) {
		core.PosLockout.RecordFailure(ip)
		core.Log(fmt.Sprintf("REJECT Admin: Incorrect server password from %s (user: %s)", ip, msg.Username), core.ColorRed)
		_ = conn.WriteJSON(core.MsgError{
			Type:    "error",
			Reason:  "invalid_server_password",
			Message: "Incorrect server password",
		})
		return
	}

	ok, err := core.AuthenticateAdmin(msg.Username, msg.Password)
	if err != nil || !ok {
		core.PosLockout.RecordFailure(ip)
		core.Log(fmt.Sprintf("REJECT Admin: Invalid credentials from %s (user: %s)", ip, msg.Username), core.ColorRed)
		_ = conn.WriteJSON(core.MsgError{
			Type:    "error",
			Reason:  "invalid_admin_credentials",
			Message: "Invalid administrator credentials",
		})
		return
	}

	// Send admin welcome state
	welcome := core.MsgAdminWelcome{
		Type:          "admin_welcome",
		Channels:      core.ServerConfig.ChannelsList,
		Profiles:      core.ServerConfig.ProfilesList,
		Players:       core.ActiveHub.GetAllPlayerStates(),
		AnonymousMode: core.ActiveHub.GetAnonymousMode(),
	}
	if err := conn.WriteJSON(welcome); err != nil {
		return
	}

	core.PosLockout.RecordSuccess(ip)
	core.ActiveHub.RegisterAdmin(conn)
	defer core.ActiveHub.UnregisterAdmin(conn)

	core.Log(fmt.Sprintf("ADMIN connected from %s", ip), core.ColorGreen)

	// Admin Command Loop
	for {
		_, payload, err := conn.ReadMessage()
		if err != nil {
			break
		}

		var cmd core.AdminCommand
		if err := json.Unmarshal(payload, &cmd); err != nil {
			continue
		}

		ok, reason := admin.ExecuteAdminCommand(cmd)
		val := admin.ExecuteAdminQuery(cmd)
		response := core.MsgAdminResponse{
			Type:   "admin_response",
			ReqID:  cmd.ReqID,
			Cmd:    cmd.Cmd,
			Ok:     ok,
			Reason: reason,
			Value:  val,
		}
		_ = conn.WriteJSON(response)
	}

	core.Log("ADMIN disconnected", core.ColorOrange)
}

func handlePlayerJoin(conn *websocket.Conn, ip string, msg core.MsgJoin) {
	if !core.PublicServer && core.ServerConfig.ServerToken != "" && !core.ConstantTimeCompare(msg.Token, core.ServerConfig.ServerToken) {
		core.PosLockout.RecordFailure(ip)
		core.Log(fmt.Sprintf("REJECT: Invalid token from %s (client: %s)", ip, msg.Name), core.ColorRed)
		_ = conn.WriteJSON(core.MsgError{
			Type:    "error",
			Reason:  "invalid_token",
			Message: "Invalid server token",
		})
		return
	}

	core.PosLockout.RecordSuccess(ip)

	// Limit capacity
	core.ActiveHub.Mu.RLock()
	playerCount := len(core.ActiveHub.Players)
	core.ActiveHub.Mu.RUnlock()

	if playerCount >= core.MaxPlayers {
		core.Log(fmt.Sprintf("REJECT: Server full from %s (client: %s)", ip, msg.Name), core.ColorOrange)
		_ = conn.WriteJSON(core.MsgError{
			Type:    "error",
			Reason:  "server_full",
			Message: fmt.Sprintf("Server full (%d players max)", core.MaxPlayers),
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
	if core.EnableIntercom && strings.HasPrefix(channel, "Intercom_") {
		validCh = channel
	} else {
		for _, c := range core.ServerConfig.ChannelsList {
			if c == channel {
				validCh = channel
				break
			}
		}
	}

	if strings.TrimSpace(msg.Password) == "" {
		core.PosLockout.RecordFailure(ip)
		core.Log(fmt.Sprintf("REJECT: Empty password for player '%s' from %s", name, ip), core.ColorRed)
		_ = conn.WriteJSON(core.MsgError{
			Type:    "error",
			Reason:  "invalid_password",
			Message: "User password is required to secure account",
		})
		return
	}

	// Authenticate or auto-register player account
	ok, isBanned, err := core.AuthenticatePlayer(name, msg.Password, validCh, ip, msg.Hwid)
	if err != nil {
		core.PosLockout.RecordFailure(ip)
		_ = conn.WriteJSON(core.MsgError{
			Type:    "error",
			Reason:  "db_error",
			Message: "Internal database error",
		})
		return
	}
	if isBanned {
		core.PosLockout.RecordFailure(ip)
		core.Log(fmt.Sprintf("REJECT: Player '%s' banned (IP: %s, HWID: %s)", name, ip, msg.Hwid), core.ColorOrange)
		_ = conn.WriteJSON(core.MsgError{
			Type:    "error",
			Reason:  "player_banned",
			Message: "This player account, IP, or HWID is banned from this server",
		})
		return
	}
	if !ok {
		core.PosLockout.RecordFailure(ip)
		core.Log(fmt.Sprintf("REJECT: Incorrect password for player '%s' from %s", name, ip), core.ColorRed)
		_ = conn.WriteJSON(core.MsgError{
			Type:    "error",
			Reason:  "invalid_password",
			Message: "Incorrect player password",
		})
		return
	}

	// Register player and issue ticket
	ticket := core.ActiveHub.RegisterPlayer(name, conn, validCh, ip, msg.Hwid)

	core.Log(fmt.Sprintf("JOIN Positions: %s (%s)", name, ip), core.ColorGreen)

	// Send welcome packet
	core.ActiveHub.Mu.RLock()
	myPlayer, myPlayerExists := core.ActiveHub.Players[name]
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
	core.ActiveHub.Mu.RUnlock()

	welcome := core.MsgWelcome{
		Type:                  "welcome",
		Players:               core.ActiveHub.GetPlayerStateList(name),
		AnonymousMode:         core.ActiveHub.GetAnonymousMode(),
		Channels:              core.ServerConfig.ChannelsList,
		Profiles:              core.ServerConfig.ProfilesList,
		MyActiveChannel:       myActiveChan,
		MyListeningChs:        myListeningChs,
		MyProfile:             myProfile,
		AudioTicket:           ticket,
		SpatialAudioSupported: core.SpatialAudioEnabled,
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
	joinMsg := core.MsgPlayerJoin{
		Type:              "join",
		Name:              name,
		ActiveChannel:     myActiveChan,
		ListeningChannels: myListeningChs,
		Profile:           myProfile,
		ProxShort:         false,
	}
	core.ActiveHub.BroadcastPosMessage(name, joinMsg)
	core.ActiveHub.BroadcastToAdmins(joinMsg)

	// Message Loop for this player
	for {
		_, payload, err := conn.ReadMessage()
		if err != nil {
			break
		}

		// Rate Limiting
		if !core.PosLimit.Allow(conn) {
			continue
		}

		var base core.MessageBase
		if err := json.Unmarshal(payload, &base); err != nil {
			continue
		}

		switch base.Type {
		case "pos":
			var m core.MsgPos
			if err := json.Unmarshal(payload, &m); err != nil {
				continue
			}
			// Validate position inputs (prevent NaNs / Infs)
			if mathIsInvalid(m.Pos.X) || mathIsInvalid(m.Pos.Y) || mathIsInvalid(m.Pos.Z) {
				continue
			}

			if ok := core.ActiveHub.UpdatePosition(name, m.Pos); ok {
				posMsg := core.MsgPlayerPos{
					Type:      "pos",
					Name:      name,
					Pos:       m.Pos,
					TsCapture: m.TsCapture,
				}
				core.ActiveHub.BroadcastPosMessage(name, posMsg)
				core.ActiveHub.BroadcastToAdmins(posMsg)
			}

		case "ping":
			core.ActiveHub.UpdateActivity(name)
			core.ActiveHub.Mu.RLock()
			p, ok := core.ActiveHub.Players[name]
			core.ActiveHub.Mu.RUnlock()
			if ok {
				_ = p.SafeWritePosJSON(core.MsgPong{Type: "pong"})
			}

		case "sc_offline":
			if ok := core.ActiveHub.UpdateScOnline(name, false); ok {
				core.Log(fmt.Sprintf("%s: Game closed (OCR inactive)", name), core.ColorOrange)
				core.ActiveHub.BroadcastPosMessage(name, core.MsgScStatus{
					Type: "sc_offline",
					Name: name,
				})
			}

		case "sc_online":
			if ok := core.ActiveHub.UpdateScOnline(name, true); ok {
				core.Log(fmt.Sprintf("%s: Game detected (OCR active)", name), core.ColorGreen)
				core.ActiveHub.BroadcastPosMessage(name, core.MsgScStatus{
					Type: "sc_online",
					Name: name,
				})
			}

		case "helmet":
			var m core.MsgHelmet
			if err := json.Unmarshal(payload, &m); err != nil {
				continue
			}
			if ok := core.ActiveHub.UpdateHelmet(name, m.HelmetOn); ok {
				status := "OFF"
				if m.HelmetOn {
					status = "ON"
				}
				core.Log(fmt.Sprintf("%s: Helmet %s", name, status), core.ColorBlue)
				core.ActiveHub.BroadcastPosMessage(name, core.MsgPlayerHelmet{
					Type:     "helmet",
					Name:     name,
					HelmetOn: m.HelmetOn,
				})
			}

		case "set_channel":
			var m core.MsgSetChannel
			if err := json.Unmarshal(payload, &m); err != nil {
				continue
			}
			ch := strings.TrimSpace(m.Channel)
			valid := ""
			if ch != "" {
				if core.EnableIntercom && strings.HasPrefix(ch, "Intercom_") {
					valid = ch
				} else {
					for _, c := range core.ServerConfig.ChannelsList {
						if c == ch {
							valid = ch
							break
						}
					}
				}
			}
			if ok := core.ActiveHub.UpdateChannel(name, valid); ok {
				core.Log(fmt.Sprintf("%s: Radio -> %s", name, valid), core.ColorBlue)
				core.ActiveHub.BroadcastPosMessageToAll(core.MsgPlayerChannel{
					Type:    "player_channel",
					Name:    name,
					Channel: valid,
				})
			}

		case "listen_channels":
			var m core.MsgListenChannels
			if err := json.Unmarshal(payload, &m); err != nil {
				continue
			}
			var valid []string
			for _, ch := range m.Channels {
				trimmed := strings.TrimSpace(ch)
				if core.EnableIntercom && strings.HasPrefix(trimmed, "Intercom_") {
					valid = append(valid, trimmed)
				} else {
					for _, c := range core.ServerConfig.ChannelsList {
						if c == trimmed {
							valid = append(valid, trimmed)
							break
						}
					}
				}
			}
			if ok := core.ActiveHub.UpdateListeningChannels(name, valid); ok {
				core.Log(fmt.Sprintf("%s: Listening -> %v", name, valid), core.ColorBlue)
				core.ActiveHub.BroadcastPosMessageToAll(core.MsgPlayerListening{
					Type:     "player_listening",
					Name:     name,
					Channels: valid,
				})
			}

		case "prox_short":
			var m core.MsgProxShort
			if err := json.Unmarshal(payload, &m); err != nil {
				continue
			}
			if ok := core.ActiveHub.UpdateProxShort(name, m.Active); ok {
				status := "50m"
				if m.Active {
					status = "5m"
				}
				core.Log(fmt.Sprintf("%s: Proximity -> %s", name, status), core.ColorBlue)
				core.ActiveHub.BroadcastPosMessageToAll(core.MsgPlayerProxShort{
					Type:   "player_prox_short",
					Name:   name,
					Active: m.Active,
				})
			}
		}
	}

	// Disconnection cleanup
	if leftName, fullyLeft := core.ActiveHub.UnregisterPosConn(conn); leftName != "" {
		core.PosLimit.Forget(conn)
		if fullyLeft {
			core.Log(fmt.Sprintf("LEAVE: %s (disconnected)", leftName), core.ColorOrange)
			core.ActiveHub.BroadcastPosMessageToAll(core.MsgPlayerLeave{
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
