package core

import (
	"math"
	"net"
	"strings"
	"sync"
	"time"

	"github.com/gorilla/websocket"
)

// ActivePlayer represents the runtime state of a connected player
type ActivePlayer struct {
	Name              string
	PosConn           *websocket.Conn
	PosMu             sync.Mutex
	UDPAddr           *net.UDPAddr
	UDPAddrMu         sync.RWMutex
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
	IsTalking         bool
	LastTalkTime      time.Time
}

// SafeGetUDPAddr returns the player's UDP address in a thread-safe manner
func (p *ActivePlayer) SafeGetUDPAddr() *net.UDPAddr {
	p.UDPAddrMu.RLock()
	defer p.UDPAddrMu.RUnlock()
	return p.UDPAddr
}

// SafeSetUDPAddr sets the player's UDP address in a thread-safe manner
func (p *ActivePlayer) SafeSetUDPAddr(addr *net.UDPAddr) {
	p.UDPAddrMu.Lock()
	defer p.UDPAddrMu.Unlock()
	p.UDPAddr = addr
}

// SafeWritePosJSON writes a JSON message to PosConn in a thread-safe manner
func (p *ActivePlayer) SafeWritePosJSON(msg interface{}) error {
	p.PosMu.Lock()
	defer p.PosMu.Unlock()
	if p.PosConn == nil {
		return websocket.ErrCloseSent
	}
	return p.PosConn.WriteJSON(msg)
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
	Players       map[string]*ActivePlayer
	Admins        map[*websocket.Conn]*AdminSession
	AnonymousMode bool
	Mu            sync.RWMutex
}

// Global Hub instance
var ActiveHub = Hub{
	Players: make(map[string]*ActivePlayer),
	Admins:  make(map[*websocket.Conn]*AdminSession),
}

// RegisterPlayer registers or updates a player upon joining the positions server
func (h *Hub) RegisterPlayer(name string, conn *websocket.Conn, initialChannel string, ip string, hwid string) string {
	h.Mu.Lock()
	defer h.Mu.Unlock()

	// If player already exists, preserve their existing fields (like position and profile)
	p, exists := h.Players[name]
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

	if len(listenChans) == 0 {
		listenChans = []string{"General"}
	}

	if exists {
		// Close previous position socket if it exists to avoid leakage
		p.PosMu.Lock()
		if p.PosConn != nil && p.PosConn != conn {
			_ = p.PosConn.Close()
		}
		p.PosConn = conn
		p.PosMu.Unlock()
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
		h.Players[name] = &ActivePlayer{
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

	pRef := h.Players[name]
	_ = DBSavePlayerState(name, pRef.Profile, pRef.ActiveChannel, pRef.ListeningChannels)

	return ticket
}

// UnregisterPosConn unregisters a player's position socket and cleans up
func (h *Hub) UnregisterPosConn(conn *websocket.Conn) (string, bool) {
	h.Mu.Lock()
	defer h.Mu.Unlock()

	for name, p := range h.Players {
		p.PosMu.Lock()
		isMatch := (p.PosConn == conn)
		if isMatch {
			p.PosConn = nil
		}
		p.PosMu.Unlock()
		if isMatch {
			if EnableIntercom && p.Pos != nil && p.Pos.ContainerID != "" {
				h.handleIntercomTransition(p, p.Pos.ContainerID, "")
			}
			delete(h.Players, name)
			return name, true // Fully left
		}
	}
	return "", false
}

// KickPlayer forcibly disconnects a player by closing their sockets
func (h *Hub) KickPlayer(name string) bool {
	h.Mu.Lock()
	defer h.Mu.Unlock()

	p, exists := h.Players[name]
	if !exists {
		return false
	}

	p.PosMu.Lock()
	if p.PosConn != nil {
		_ = p.PosConn.Close()
		p.PosConn = nil
	}
	p.PosMu.Unlock()

	delete(h.Players, name)
	return true
}

// KickIP forcibly disconnects any player connecting from the specified IP address
func (h *Hub) KickIP(ip string) {
	h.Mu.Lock()
	defer h.Mu.Unlock()
	ipClean := cleanIP(ip)
	if ipClean == "" {
		return
	}
	for name, p := range h.Players {
		if cleanIP(p.IP) == ipClean {
			p.PosMu.Lock()
			if p.PosConn != nil {
				_ = p.PosConn.Close()
				p.PosConn = nil
			}
			p.PosMu.Unlock()
			delete(h.Players, name)
		}
	}
}

// KickHwid forcibly disconnects any player sharing the specified hardware ID
func (h *Hub) KickHwid(hwid string) {
	h.Mu.Lock()
	defer h.Mu.Unlock()
	hwid = strings.TrimSpace(hwid)
	if hwid == "" {
		return
	}
	for name, p := range h.Players {
		if strings.EqualFold(p.Hwid, hwid) {
			p.PosMu.Lock()
			if p.PosConn != nil {
				_ = p.PosConn.Close()
				p.PosConn = nil
			}
			p.PosMu.Unlock()
			delete(h.Players, name)
		}
	}
}

// UpdatePosition updates a player's coordinate state
func (h *Hub) UpdatePosition(name string, pos Position) bool {
	h.Mu.Lock()
	defer h.Mu.Unlock()

	p, exists := h.Players[name]
	if !exists {
		return false
	}

	oldContainerID := ""
	if p.Pos != nil {
		oldContainerID = p.Pos.ContainerID
	}

	p.Pos = &pos
	p.LastSeen = time.Now()

	if EnableIntercom {
		h.handleIntercomTransition(p, oldContainerID, pos.ContainerID)
	}

	return true
}

// UpdateActivity updates a player's last seen timestamp to prevent timeouts
func (h *Hub) UpdateActivity(name string) bool {
	h.Mu.Lock()
	defer h.Mu.Unlock()

	p, exists := h.Players[name]
	if !exists {
		return false
	}
	p.LastSeen = time.Now()
	return true
}

// UpdateHelmet updates a player's helmet state
func (h *Hub) UpdateHelmet(name string, helmetOn bool) bool {
	h.Mu.Lock()
	defer h.Mu.Unlock()

	p, exists := h.Players[name]
	if !exists {
		return false
	}

	p.HelmetOn = helmetOn
	p.LastSeen = time.Now()
	return true
}

// UpdateChannel updates a player's radio channel
func (h *Hub) UpdateChannel(name string, channel string) bool {
	h.Mu.Lock()
	defer h.Mu.Unlock()

	p, exists := h.Players[name]
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
	h.Mu.Lock()
	defer h.Mu.Unlock()

	p, exists := h.Players[name]
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
	h.Mu.Lock()
	defer h.Mu.Unlock()

	p, exists := h.Players[name]
	if !exists {
		return false
	}

	p.ProxShort = active
	p.LastSeen = time.Now()
	return true
}

// UpdateScOnline updates a player's Star Citizen online status
func (h *Hub) UpdateScOnline(name string, online bool) bool {
	h.Mu.Lock()
	defer h.Mu.Unlock()

	p, exists := h.Players[name]
	if !exists {
		return false
	}

	p.ScOnline = online
	p.LastSeen = time.Now()
	return true
}

// AssignProfile sets a player's profile (role)
func (h *Hub) AssignProfile(name string, profile string) bool {
	h.Mu.Lock()
	defer h.Mu.Unlock()

	p, exists := h.Players[name]
	if !exists {
		return false
	}

	p.Profile = profile
	_ = DBSavePlayerState(name, p.Profile, p.ActiveChannel, p.ListeningChannels)
	return true
}

// GetPlayerStateList returns the states of all players except the sender
func (h *Hub) GetPlayerStateList(excludeName string) []PlayerState {
	h.Mu.RLock()
	defer h.Mu.RUnlock()

	var states []PlayerState
	for name, p := range h.Players {
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
			IsTalking:         p.IsTalking,
		})
	}
	return states
}

// GetAllPlayerStates returns the states of all registered players
func (h *Hub) GetAllPlayerStates() []PlayerState {
	h.Mu.RLock()
	defer h.Mu.RUnlock()

	var states []PlayerState
	for _, p := range h.Players {
		states = append(states, PlayerState{
			Name:              p.Name,
			Pos:               p.Pos,
			HelmetOn:          p.HelmetOn,
			ActiveChannel:     p.ActiveChannel,
			ListeningChannels: p.ListeningChannels,
			Profile:           p.Profile,
			ProxShort:         p.ProxShort,
			ScOnline:          p.ScOnline,
			IsTalking:         p.IsTalking,
		})
	}
	return states
}

func isEvaZone(zone string) bool {
	lower := strings.ToLower(zone)
	if lower == "planetary_system_stanton" {
		return true
	}
	if strings.Contains(lower, "space") || strings.Contains(lower, "orbit") || strings.Contains(lower, "void") {
		excluded := []string{"ship", "facility", "station", "hangar", "interior", "cabin", "deck", "chamber", "room", "corridor", "airlock", "cockpit", "habitation", "hab"}
		for _, ex := range excluded {
			if strings.Contains(lower, ex) {
				return false
			}
		}
		return true
	}
	return false
}

// GetAudioPlayersInProximity returns the players within proximity of sender
func (h *Hub) GetAudioPlayersInProximity(senderName string) []*ActivePlayer {
	h.Mu.RLock()
	defer h.Mu.RUnlock()

	sender, exists := h.Players[senderName]
	if !exists || sender.Pos == nil {
		return nil
	}

	if EnableEvaMuting && isEvaZone(sender.Pos.Zone) {
		return nil
	}

	var players []*ActivePlayer
	for name, p := range h.Players {
		if name == senderName || p.SafeGetUDPAddr() == nil || p.Pos == nil {
			continue
		}

		if EnableEvaMuting && isEvaZone(p.Pos.Zone) {
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
	h.Mu.RLock()
	defer h.Mu.RUnlock()

	sender, exists := h.Players[senderName]
	if !exists || sender.ActiveChannel == "" {
		return nil
	}

	var players []*ActivePlayer
	for name, p := range h.Players {
		if name == senderName || p.SafeGetUDPAddr() == nil {
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
	h.Mu.RLock()
	defer h.Mu.RUnlock()

	sender, exists := h.Players[senderName]
	if !exists || sender.Profile == "" {
		return nil
	}

	var players []*ActivePlayer
	for name, p := range h.Players {
		if name == senderName || p.SafeGetUDPAddr() == nil {
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
	h.Mu.RLock()
	defer h.Mu.RUnlock()

	for name, p := range h.Players {
		if name == senderName || p.PosConn == nil {
			continue
		}
		_ = p.SafeWritePosJSON(msg)
	}
}

// BroadcastPosMessageToAll sends a JSON message to all position clients and all admins
func (h *Hub) BroadcastPosMessageToAll(msg interface{}) {
	h.Mu.RLock()
	defer h.Mu.RUnlock()

	for _, p := range h.Players {
		if p.PosConn != nil {
			_ = p.SafeWritePosJSON(msg)
		}
	}
	for _, admin := range h.Admins {
		if admin.Conn != nil {
			_ = admin.SafeWriteJSON(msg)
		}
	}
}

// CleanupTimeouts checks for players who haven't sent coordinates/pings within timeout window
func (h *Hub) CleanupTimeouts(timeout time.Duration) []string {
	h.Mu.Lock()
	defer h.Mu.Unlock()

	var timedOut []string
	now := time.Now()
	for name, p := range h.Players {
		if now.Sub(p.LastSeen) > timeout {
			timedOut = append(timedOut, name)
			p.PosMu.Lock()
			if p.PosConn != nil {
				_ = p.PosConn.Close()
				p.PosConn = nil
			}
			p.PosMu.Unlock()
			delete(h.Players, name)
		}
	}
	return timedOut
}

// Admin actions

// RegisterAdmin registers a new admin session
func (h *Hub) RegisterAdmin(conn *websocket.Conn) {
	h.Mu.Lock()
	defer h.Mu.Unlock()
	h.Admins[conn] = &AdminSession{
		Conn:        conn,
		ConnectedAt: time.Now(),
	}
}

// UnregisterAdmin unregisters an admin session
func (h *Hub) UnregisterAdmin(conn *websocket.Conn) {
	h.Mu.Lock()
	defer h.Mu.Unlock()
	delete(h.Admins, conn)
}

// BroadcastLog sends a push log message to all active admin clients
func (h *Hub) BroadcastLog(msg string, color string) {
	h.Mu.RLock()
	defer h.Mu.RUnlock()

	if len(h.Admins) == 0 {
		return
	}

	payload := MsgLogPush{
		Type:  "log",
		Msg:   msg,
		Color: color,
		TS:    time.Now().Format("15:04:05.000"),
	}

	for _, admin := range h.Admins {
		_ = admin.SafeWriteJSON(payload)
	}
}

// GetAnonymousMode returns current anonymous mode state
func (h *Hub) GetAnonymousMode() bool {
	h.Mu.RLock()
	defer h.Mu.RUnlock()
	return h.AnonymousMode
}

// SetAnonymousMode sets global anonymous mode and broadcasts to all clients
func (h *Hub) SetAnonymousMode(active bool) {
	h.Mu.Lock()
	h.AnonymousMode = active
	h.Mu.Unlock()

	h.BroadcastPosMessageToAll(MsgAnonymousMode{
		Type:   "anonymous_mode",
		Active: active,
	})
}

// BroadcastToAdmins sends a JSON message to all active admin clients
func (h *Hub) BroadcastToAdmins(msg interface{}) {
	h.Mu.RLock()
	defer h.Mu.RUnlock()

	for _, admin := range h.Admins {
		if admin.Conn != nil {
			_ = admin.SafeWriteJSON(msg)
		}
	}
}

// Shutdown closes all active player connections and admin WebSocket connections cleanly
func (h *Hub) Shutdown() {
	h.Mu.Lock()
	defer h.Mu.Unlock()
	for _, p := range h.Players {
		p.PosMu.Lock()
		if p.PosConn != nil {
			_ = p.PosConn.Close()
			p.PosConn = nil
		}
		p.PosMu.Unlock()
	}
	for conn := range h.Admins {
		_ = conn.Close()
	}
}

// IntercomManager keeps track of dynamic ship intercom channel deletion countdown timers
type IntercomManager struct {
	mu        sync.Mutex
	deletions map[string]*time.Timer
}

var ActiveIntercoms = IntercomManager{
	deletions: make(map[string]*time.Timer),
}

func isShipZone(zone string) bool {
	lower := strings.ToLower(zone)
	shipKeywords := []string{
		"aegis", "anvil", "drake", "misc", "rsi", "origin", "crusader", "argo", "banu", "esperia", "consolidated", "gatac",
		"carrack", "reclaimer", "hammerhead", "starfarer", "freelancer", "caterpillar", "cutlass", "constellation", "avenger",
		"gladius", "arrow", "sabre", "valkyrie", "prowler", "vanguard", "buccaneer", "herald", "prospector", "mole",
		"ship_", "vehicle_",
	}
	for _, kw := range shipKeywords {
		if strings.Contains(lower, kw) {
			return true
		}
	}
	return false
}

// handleIntercomTransition manages player subscription and lifecycle of dynamic ship intercom channels
func (h *Hub) handleIntercomTransition(p *ActivePlayer, oldContainerID, newContainerID string) {
	if oldContainerID == newContainerID {
		return
	}

	// 1. Leave old container intercom if we were in one
	if oldContainerID != "" {
		oldChanName := "Intercom_" + oldContainerID
		p.ListeningChannels = removeStringSlice(p.ListeningChannels, oldChanName)
		if p.ActiveChannel == oldChanName {
			p.ActiveChannel = "General"
		}
		_ = DBSavePlayerState(p.Name, p.Profile, p.ActiveChannel, p.ListeningChannels)
		
		// Check if anyone else is left in the old ship
		shipEmpty := true
		for _, other := range h.Players {
			if other.Name != p.Name && other.Pos != nil && other.Pos.ContainerID == oldContainerID {
				shipEmpty = false
				break
			}
		}

		if shipEmpty {
			// Start 5-minute countdown to delete the intercom channel
			ActiveIntercoms.mu.Lock()
			if t, ok := ActiveIntercoms.deletions[oldChanName]; ok {
				t.Stop()
			}
			ActiveIntercoms.deletions[oldChanName] = time.AfterFunc(5*time.Minute, func() {
				h.deleteIntercomChannel(oldChanName)
			})
			ActiveIntercoms.mu.Unlock()
		}
	}

	// 2. Join new container intercom if we are entering one and it's a ship
	if newContainerID != "" && isShipZone(p.Pos.Zone) {
		newChanName := "Intercom_" + newContainerID

		// Cancel any pending deletion timer
		ActiveIntercoms.mu.Lock()
		if t, ok := ActiveIntercoms.deletions[newChanName]; ok {
			t.Stop()
			delete(ActiveIntercoms.deletions, newChanName)
		}
		ActiveIntercoms.mu.Unlock()

		// Create channel if it doesn't exist
		channelExists := false
		for _, ch := range ServerConfig.ChannelsList {
			if ch == newChanName {
				channelExists = true
				break
			}
		}

		if !channelExists {
			ServerConfig.ChannelsList = append(ServerConfig.ChannelsList, newChanName)
			h.broadcastChannelsListLocked()
		}

		// Add to p's listening channels
		if !containsStringSlice(p.ListeningChannels, newChanName) {
			p.ListeningChannels = append(p.ListeningChannels, newChanName)
			_ = DBSavePlayerState(p.Name, p.Profile, p.ActiveChannel, p.ListeningChannels)
		}
	}
}

func (h *Hub) deleteIntercomChannel(chanName string) {
	h.Mu.Lock()
	defer h.Mu.Unlock()

	// Double check if anyone has re-entered in the meantime
	containerID := strings.TrimPrefix(chanName, "Intercom_")
	shipEmpty := true
	for _, p := range h.Players {
		if p.Pos != nil && p.Pos.ContainerID == containerID {
			shipEmpty = false
			break
		}
	}

	if !shipEmpty {
		// Someone re-entered, cancel deletion
		ActiveIntercoms.mu.Lock()
		delete(ActiveIntercoms.deletions, chanName)
		ActiveIntercoms.mu.Unlock()
		return
	}

	// Remove from ServerConfig.ChannelsList
	newList := make([]string, 0, len(ServerConfig.ChannelsList))
	for _, ch := range ServerConfig.ChannelsList {
		if ch != chanName {
			newList = append(newList, ch)
		}
	}
	ServerConfig.ChannelsList = newList

	// Clean up from players' active/listening channels in memory
	for _, p := range h.Players {
		p.ListeningChannels = removeStringSlice(p.ListeningChannels, chanName)
		if p.ActiveChannel == chanName {
			p.ActiveChannel = "General"
		}
	}

	ActiveIntercoms.mu.Lock()
	delete(ActiveIntercoms.deletions, chanName)
	ActiveIntercoms.mu.Unlock()

	h.broadcastChannelsListLocked()
}

func (h *Hub) broadcastChannelsListLocked() {
	msg := struct {
		Type     string   `json:"type"`
		Channels []string `json:"channels"`
	}{
		Type:     "channels_list",
		Channels: ServerConfig.ChannelsList,
	}
	for _, p := range h.Players {
		_ = p.SafeWritePosJSON(msg)
	}
}

func removeStringSlice(slice []string, s string) []string {
	res := make([]string, 0, len(slice))
	for _, x := range slice {
		if x != s {
			res = append(res, x)
		}
	}
	return res
}

func containsStringSlice(slice []string, s string) bool {
	for _, x := range slice {
		if x == s {
			return true
		}
	}
	return false
}
