package main

import (
	"math"
	"strings"
	"sync"
	"time"

	"github.com/gorilla/websocket"
)

// ActivePlayer represents the runtime state of a connected player
type ActivePlayer struct {
	Name              string
	PosConn           *websocket.Conn
	posMu             sync.Mutex
	AudioConn         *websocket.Conn
	audioMu           sync.Mutex
	Pos               *Position
	HelmetOn          bool
	ActiveChannel     string
	ListeningChannels []string
	Profile           string
	ProxShort         bool
	ScOnline          bool
	LastSeen          time.Time
	AudioTicket       string
	TicketExpires     time.Time
	IP                string
	Hwid              string
}

// SafeWritePosJSON writes a JSON message to PosConn in a thread-safe manner
func (p *ActivePlayer) SafeWritePosJSON(msg interface{}) error {
	p.posMu.Lock()
	defer p.posMu.Unlock()
	if p.PosConn == nil {
		return websocket.ErrCloseSent
	}
	return p.PosConn.WriteJSON(msg)
}

// SafeWriteAudioJSON writes a JSON message to AudioConn in a thread-safe manner
func (p *ActivePlayer) SafeWriteAudioJSON(msg interface{}) error {
	p.audioMu.Lock()
	defer p.audioMu.Unlock()
	if p.AudioConn == nil {
		return websocket.ErrCloseSent
	}
	return p.AudioConn.WriteJSON(msg)
}

// SafeWriteAudioMessage writes a binary/text message to AudioConn in a thread-safe manner
func (p *ActivePlayer) SafeWriteAudioMessage(messageType int, data []byte) error {
	p.audioMu.Lock()
	defer p.audioMu.Unlock()
	if p.AudioConn == nil {
		return websocket.ErrCloseSent
	}
	return p.AudioConn.WriteMessage(messageType, data)
}

// AdminSession represents a connected administrator connection
type AdminSession struct {
	Conn        *websocket.Conn
	mu          sync.Mutex
	ConnectedAt time.Time
}

// SafeWriteJSON writes a JSON message to Admin connection in a thread-safe manner
func (a *AdminSession) SafeWriteJSON(msg interface{}) error {
	a.mu.Lock()
	defer a.mu.Unlock()
	if a.Conn == nil {
		return websocket.ErrCloseSent
	}
	return a.Conn.WriteJSON(msg)
}

// Hub manages all connected clients (players & admins) and global server state
type Hub struct {
	players       map[string]*ActivePlayer
	admins        map[*websocket.Conn]*AdminSession
	anonymousMode bool
	mu            sync.RWMutex
}

// Global Hub instance
var hub = Hub{
	players: make(map[string]*ActivePlayer),
	admins:  make(map[*websocket.Conn]*AdminSession),
}

// RegisterPlayer registers or updates a player upon joining the positions server
func (h *Hub) RegisterPlayer(name string, conn *websocket.Conn, initialChannel string, ip string, hwid string) string {
	h.mu.Lock()
	defer h.mu.Unlock()

	// If player already exists, preserve their existing fields (like position and profile)
	p, exists := h.players[name]
	ticket := GenerateRandomString(32)
	expires := time.Now().Add(120 * time.Second)

	// Load persistence
	persist, hasPersist := DBGetPlayerState(name)

	activeChan := initialChannel
	var listenChans []string
	profile := ""

	if hasPersist {
		if persist.ActiveChannel != "" {
			activeChan = persist.ActiveChannel
		}
		listenChans = persist.ListeningChannels
		profile = persist.Profile
	}

	if exists {
		// Close previous position socket if it exists to avoid leakage
		p.posMu.Lock()
		if p.PosConn != nil && p.PosConn != conn {
			_ = p.PosConn.Close()
		}
		p.PosConn = conn
		p.posMu.Unlock()
		// Keep current runtime active channel, listening channels, and profile if they are already set
		// But if they are empty, we can use the persistent/initial ones.
		if p.ActiveChannel == "" {
			p.ActiveChannel = activeChan
		}
		if len(p.ListeningChannels) == 0 {
			p.ListeningChannels = listenChans
		}
		if p.Profile == "" {
			p.Profile = profile
		}
		p.LastSeen = time.Now()
		p.AudioTicket = ticket
		p.TicketExpires = expires
		p.ScOnline = true
		p.IP = ip
		p.Hwid = hwid
	} else {
		h.players[name] = &ActivePlayer{
			Name:              name,
			PosConn:           conn,
			ActiveChannel:     activeChan,
			ListeningChannels: listenChans,
			Profile:           profile,
			LastSeen:          time.Now(),
			AudioTicket:       ticket,
			TicketExpires:     expires,
			ScOnline:          true,
			IP:                ip,
			Hwid:              hwid,
		}
	}

	pRef := h.players[name]
	_ = DBSavePlayerState(name, pRef.Profile, pRef.ActiveChannel, pRef.ListeningChannels)

	return ticket
}

// BindAudioConn validates an audio ticket and binds the audio socket to the player
func (h *Hub) BindAudioConn(name string, ticket string, conn *websocket.Conn) bool {
	h.mu.Lock()
	defer h.mu.Unlock()

	p, exists := h.players[name]
	if !exists {
		return false
	}

	// Validate ticket in constant time and check expiration
	if p.AudioTicket == "" || time.Now().After(p.TicketExpires) {
		return false
	}

	if !ConstantTimeCompare(p.AudioTicket, ticket) {
		return false
	}

	// Ticket is valid, bind connection and revoke ticket (one-time use)
	p.audioMu.Lock()
	if p.AudioConn != nil && p.AudioConn != conn {
		_ = p.AudioConn.Close()
	}
	p.AudioConn = conn
	p.audioMu.Unlock()
	p.AudioTicket = "" // Revoke
	return true
}

// UnregisterPosConn unregisters a player's position socket and cleans up if both sockets are gone
func (h *Hub) UnregisterPosConn(conn *websocket.Conn) (string, bool) {
	h.mu.Lock()
	defer h.mu.Unlock()

	for name, p := range h.players {
		p.posMu.Lock()
		isMatch := (p.PosConn == conn)
		if isMatch {
			p.PosConn = nil
		}
		p.posMu.Unlock()
		if isMatch {
			// If both sockets are gone, or if they haven't bound audio yet and pos is gone, cleanup
			p.audioMu.Lock()
			hasAudio := (p.AudioConn != nil)
			p.audioMu.Unlock()
			if !hasAudio {
				delete(h.players, name)
				return name, true // Fully left
			}
			return name, false // Only position socket disconnected
		}
	}
	return "", false
}

// UnregisterAudioConn unregisters a player's audio socket
func (h *Hub) UnregisterAudioConn(conn *websocket.Conn) (string, bool) {
	h.mu.Lock()
	defer h.mu.Unlock()

	for name, p := range h.players {
		p.audioMu.Lock()
		isMatch := (p.AudioConn == conn)
		if isMatch {
			p.AudioConn = nil
		}
		p.audioMu.Unlock()
		if isMatch {
			p.posMu.Lock()
			hasPos := (p.PosConn != nil)
			p.posMu.Unlock()
			if !hasPos {
				delete(h.players, name)
				return name, true // Fully left
			}
			return name, false // Only audio socket disconnected
		}
	}
	return "", false
}

// KickPlayer forcibly disconnects a player by closing their sockets
func (h *Hub) KickPlayer(name string) bool {
	h.mu.Lock()
	defer h.mu.Unlock()

	p, exists := h.players[name]
	if !exists {
		return false
	}

	p.posMu.Lock()
	if p.PosConn != nil {
		_ = p.PosConn.Close()
		p.PosConn = nil
	}
	p.posMu.Unlock()

	p.audioMu.Lock()
	if p.AudioConn != nil {
		_ = p.AudioConn.Close()
		p.AudioConn = nil
	}
	p.audioMu.Unlock()

	delete(h.players, name)
	return true
}

// KickIP forcibly disconnects any player connecting from the specified IP address
func (h *Hub) KickIP(ip string) {
	h.mu.Lock()
	defer h.mu.Unlock()
	ipClean := cleanIP(ip)
	if ipClean == "" {
		return
	}
	for name, p := range h.players {
		if cleanIP(p.IP) == ipClean {
			p.posMu.Lock()
			if p.PosConn != nil {
				_ = p.PosConn.Close()
				p.PosConn = nil
			}
			p.posMu.Unlock()

			p.audioMu.Lock()
			if p.AudioConn != nil {
				_ = p.AudioConn.Close()
				p.AudioConn = nil
			}
			p.audioMu.Unlock()
			delete(h.players, name)
		}
	}
}

// KickHwid forcibly disconnects any player sharing the specified hardware ID
func (h *Hub) KickHwid(hwid string) {
	h.mu.Lock()
	defer h.mu.Unlock()
	hwid = strings.TrimSpace(hwid)
	if hwid == "" {
		return
	}
	for name, p := range h.players {
		if strings.EqualFold(p.Hwid, hwid) {
			p.posMu.Lock()
			if p.PosConn != nil {
				_ = p.PosConn.Close()
				p.PosConn = nil
			}
			p.posMu.Unlock()

			p.audioMu.Lock()
			if p.AudioConn != nil {
				_ = p.AudioConn.Close()
				p.AudioConn = nil
			}
			p.audioMu.Unlock()
			delete(h.players, name)
		}
	}
}

// UpdatePosition updates a player's coordinate state
func (h *Hub) UpdatePosition(name string, pos Position) bool {
	h.mu.Lock()
	defer h.mu.Unlock()

	p, exists := h.players[name]
	if !exists {
		return false
	}

	p.Pos = &pos
	p.LastSeen = time.Now()
	return true
}

// UpdateActivity updates a player's last seen timestamp to prevent timeouts
func (h *Hub) UpdateActivity(name string) bool {
	h.mu.Lock()
	defer h.mu.Unlock()

	p, exists := h.players[name]
	if !exists {
		return false
	}
	p.LastSeen = time.Now()
	return true
}

// UpdateHelmet updates a player's helmet state
func (h *Hub) UpdateHelmet(name string, helmetOn bool) bool {
	h.mu.Lock()
	defer h.mu.Unlock()

	p, exists := h.players[name]
	if !exists {
		return false
	}

	p.HelmetOn = helmetOn
	p.LastSeen = time.Now()
	return true
}

// UpdateChannel updates a player's radio channel
func (h *Hub) UpdateChannel(name string, channel string) bool {
	h.mu.Lock()
	defer h.mu.Unlock()

	p, exists := h.players[name]
	if !exists {
		return false
	}

	p.ActiveChannel = channel
	p.LastSeen = time.Now()
	_ = DBSavePlayerState(name, p.Profile, p.ActiveChannel, p.ListeningChannels)
	return true
}

// UpdateListeningChannels updates a player's listening radio channels
func (h *Hub) UpdateListeningChannels(name string, channels []string) bool {
	h.mu.Lock()
	defer h.mu.Unlock()

	p, exists := h.players[name]
	if !exists {
		return false
	}

	p.ListeningChannels = channels
	p.LastSeen = time.Now()
	_ = DBSavePlayerState(name, p.Profile, p.ActiveChannel, p.ListeningChannels)
	return true
}

// UpdateProxShort updates a player's proximity short status
func (h *Hub) UpdateProxShort(name string, active bool) bool {
	h.mu.Lock()
	defer h.mu.Unlock()

	p, exists := h.players[name]
	if !exists {
		return false
	}

	p.ProxShort = active
	p.LastSeen = time.Now()
	return true
}

// UpdateScOnline updates a player's Star Citizen online status
func (h *Hub) UpdateScOnline(name string, online bool) bool {
	h.mu.Lock()
	defer h.mu.Unlock()

	p, exists := h.players[name]
	if !exists {
		return false
	}

	p.ScOnline = online
	p.LastSeen = time.Now()
	return true
}

// AssignProfile sets a player's profile (role)
func (h *Hub) AssignProfile(name string, profile string) bool {
	h.mu.Lock()
	defer h.mu.Unlock()

	p, exists := h.players[name]
	if !exists {
		return false
	}

	p.Profile = profile
	_ = DBSavePlayerState(name, p.Profile, p.ActiveChannel, p.ListeningChannels)
	return true
}

// GetPlayerStateList returns the states of all players except the sender
func (h *Hub) GetPlayerStateList(excludeName string) []PlayerState {
	h.mu.RLock()
	defer h.mu.RUnlock()

	var states []PlayerState
	for name, p := range h.players {
		if name == excludeName {
			continue
		}
		states = append(states, PlayerState{
			Name:              p.Name,
			Pos:               p.Pos,
			HelmetOn:          p.HelmetOn,
			ActiveChannel:     p.ActiveChannel,
			ListeningChannels: p.ListeningChannels,
			Profile:           p.Profile,
			ProxShort:         p.ProxShort,
			ScOnline:          p.ScOnline,
		})
	}
	return states
}

// GetAllPlayerStates returns the states of all registered players
func (h *Hub) GetAllPlayerStates() []PlayerState {
	h.mu.RLock()
	defer h.mu.RUnlock()

	var states []PlayerState
	for _, p := range h.players {
		states = append(states, PlayerState{
			Name:              p.Name,
			Pos:               p.Pos,
			HelmetOn:          p.HelmetOn,
			ActiveChannel:     p.ActiveChannel,
			ListeningChannels: p.ListeningChannels,
			Profile:           p.Profile,
			ProxShort:         p.ProxShort,
			ScOnline:          p.ScOnline,
		})
	}
	return states
}

// GetAudioPlayersInProximity returns the players within proximity of sender
func (h *Hub) GetAudioPlayersInProximity(senderName string) []*ActivePlayer {
	h.mu.RLock()
	defer h.mu.RUnlock()

	sender, exists := h.players[senderName]
	if !exists || sender.Pos == nil {
		return nil
	}

	var players []*ActivePlayer
	for name, p := range h.players {
		if name == senderName || p.AudioConn == nil || p.Pos == nil {
			continue
		}

		// Proximity filtering: must be in same container
		if sender.Pos.ContainerID != p.Pos.ContainerID {
			continue
		}

		// Calculate 3D Euclidean distance
		dx := sender.Pos.X - p.Pos.X
		dy := sender.Pos.Y - p.Pos.Y
		dz := sender.Pos.Z - p.Pos.Z
		dist := math.Sqrt(dx*dx + dy*dy + dz*dz)

		// Set max audible range (5m if whisper/short is active on either, 50m default)
		maxRange := 50.0
		if sender.ProxShort || p.ProxShort {
			maxRange = 5.0
		}

		if dist <= maxRange {
			players = append(players, p)
		}
	}
	return players
}

// GetAudioPlayersInRadioChannel returns the players listening to the sender's active channel
func (h *Hub) GetAudioPlayersInRadioChannel(senderName string) []*ActivePlayer {
	h.mu.RLock()
	defer h.mu.RUnlock()

	sender, exists := h.players[senderName]
	if !exists || sender.ActiveChannel == "" {
		return nil
	}

	var players []*ActivePlayer
	for name, p := range h.players {
		if name == senderName || p.AudioConn == nil {
			continue
		}

		isListening := (p.ActiveChannel == sender.ActiveChannel)
		if !isListening {
			for _, ch := range p.ListeningChannels {
				if ch == sender.ActiveChannel {
					isListening = true
					break
				}
			}
		}

		if isListening {
			players = append(players, p)
		}
	}
	return players
}

// GetAudioPlayersInProfile returns the players sharing the sender's profile
func (h *Hub) GetAudioPlayersInProfile(senderName string) []*ActivePlayer {
	h.mu.RLock()
	defer h.mu.RUnlock()

	sender, exists := h.players[senderName]
	if !exists || sender.Profile == "" {
		return nil
	}

	var players []*ActivePlayer
	for name, p := range h.players {
		if name == senderName || p.AudioConn == nil {
			continue
		}
		if p.Profile == sender.Profile {
			players = append(players, p)
		}
	}
	return players
}

// BroadcastPosMessage sends a JSON message to all position clients except sender
func (h *Hub) BroadcastPosMessage(senderName string, msg interface{}) {
	h.mu.RLock()
	defer h.mu.RUnlock()

	for name, p := range h.players {
		if name == senderName || p.PosConn == nil {
			continue
		}
		_ = p.SafeWritePosJSON(msg)
	}
}

// BroadcastPosMessageToAll sends a JSON message to all position clients and all admins
func (h *Hub) BroadcastPosMessageToAll(msg interface{}) {
	h.mu.RLock()
	defer h.mu.RUnlock()

	for _, p := range h.players {
		if p.PosConn != nil {
			_ = p.SafeWritePosJSON(msg)
		}
	}
	for _, admin := range h.admins {
		if admin.Conn != nil {
			_ = admin.SafeWriteJSON(msg)
		}
	}
}

// CleanupTimeouts checks for players who haven't sent coordinates/pings within timeout window
func (h *Hub) CleanupTimeouts(timeout time.Duration) []string {
	h.mu.Lock()
	defer h.mu.Unlock()

	var timedOut []string
	now := time.Now()
	for name, p := range h.players {
		if now.Sub(p.LastSeen) > timeout {
			timedOut = append(timedOut, name)
			p.posMu.Lock()
			if p.PosConn != nil {
				_ = p.PosConn.Close()
				p.PosConn = nil
			}
			p.posMu.Unlock()

			p.audioMu.Lock()
			if p.AudioConn != nil {
				_ = p.AudioConn.Close()
				p.AudioConn = nil
			}
			p.audioMu.Unlock()
			delete(h.players, name)
		}
	}
	return timedOut
}

// Admin actions

// RegisterAdmin registers a new admin session
func (h *Hub) RegisterAdmin(conn *websocket.Conn) {
	h.mu.Lock()
	defer h.mu.Unlock()
	h.admins[conn] = &AdminSession{
		Conn:        conn,
		ConnectedAt: time.Now(),
	}
}

// UnregisterAdmin unregisters an admin session
func (h *Hub) UnregisterAdmin(conn *websocket.Conn) {
	h.mu.Lock()
	defer h.mu.Unlock()
	delete(h.admins, conn)
}

// BroadcastLog sends a push log message to all active admin clients
func (h *Hub) BroadcastLog(msg string, color string) {
	h.mu.RLock()
	defer h.mu.RUnlock()

	if len(h.admins) == 0 {
		return
	}

	payload := MsgLogPush{
		Type:  "log",
		Msg:   msg,
		Color: color,
		TS:    time.Now().Format("15:04:05.000"),
	}

	for _, admin := range h.admins {
		_ = admin.SafeWriteJSON(payload)
	}
}

// GetAnonymousMode returns current anonymous mode state
func (h *Hub) GetAnonymousMode() bool {
	h.mu.RLock()
	defer h.mu.RUnlock()
	return h.anonymousMode
}

// SetAnonymousMode sets global anonymous mode and broadcasts to all clients
func (h *Hub) SetAnonymousMode(active bool) {
	h.mu.Lock()
	h.anonymousMode = active
	h.mu.Unlock()

	h.BroadcastPosMessageToAll(MsgAnonymousMode{
		Type:   "anonymous_mode",
		Active: active,
	})
}

// BroadcastToAdmins sends a JSON message to all active admin clients
func (h *Hub) BroadcastToAdmins(msg interface{}) {
	h.mu.RLock()
	defer h.mu.RUnlock()

	for _, admin := range h.admins {
		if admin.Conn != nil {
			_ = admin.SafeWriteJSON(msg)
		}
	}
}
