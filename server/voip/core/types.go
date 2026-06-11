package core

// Constants for audio frame types
const (
	AudioTypeProximity byte = 0x00
	AudioTypeRadio     byte = 0x01
	AudioTypeProfile   byte = 0x02
	AudioTypePA        byte = 0x03
	AudioTypeHail      byte = 0x04
)

// Position represents a player's 3D coordinates and zone
type Position struct {
	X             float64 `json:"x"`
	Y             float64 `json:"y"`
	Z             float64 `json:"z"`
	Zone          string  `json:"zone"`
	Altitude      float64 `json:"altitude"`
	ContainerID   string  `json:"container_id,omitempty"`
	ContainerName string  `json:"container_name,omitempty"`
}

// MessageBase is the minimal structure to decode a message type
type MessageBase struct {
	Type string `json:"type"`
}

// Client Messages

// MsgJoin represents the connection message sent by the client
type MsgJoin struct {
	Type        string `json:"type"`
	Token       string `json:"token"`
	Name        string `json:"name"`
	Password    string `json:"password,omitempty"`
	Channel     string `json:"channel,omitempty"`
	AudioTicket string `json:"audio_ticket,omitempty"` // Used by the audio server
	Hwid        string `json:"hwid,omitempty"`
	Language    string `json:"language,omitempty"`
}

// MsgPos represents the position message sent by the client
type MsgPos struct {
	Type      string   `json:"type"`
	Pos       Position `json:"pos"`
	TsCapture float64  `json:"ts_capture"`
}

// MsgHelmet represents the helmet state message
type MsgHelmet struct {
	Type     string `json:"type"`
	HelmetOn bool   `json:"helmet_on"`
}

// MsgSetChannel allows changing the active radio channel for speaking
type MsgSetChannel struct {
	Type    string `json:"type"`
	Channel string `json:"channel"`
}

// MsgListenChannels allows subscribing to multiple channels for listening
type MsgListenChannels struct {
	Type     string   `json:"type"`
	Channels []string `json:"channels"`
}


// MsgProxShort allows enabling whisper mode (5m range)
type MsgProxShort struct {
	Type   string `json:"type"`
	Active bool   `json:"active"`
}

// MsgAuthAdmin allows an admin client to authenticate
type MsgAuthAdmin struct {
	Type           string `json:"type"`
	Username       string `json:"username"`
	Password       string `json:"password"`
	ServerPassword string `json:"server_password,omitempty"`
}


// PlayerPersistentState represents a player's persistent state stored in the database
type PlayerPersistentState struct {
	Profile           string   `json:"profile"`
	ActiveChannel     string   `json:"active_channel"`
	ListeningChannels []string `json:"listening_channels"`
}

// Server Messages

// PlayerState represents another player's state sent in the welcome message
type PlayerState struct {
	Name              string    `json:"name"`
	Pos               *Position `json:"pos"`
	HelmetOn          bool      `json:"helmet_on"`
	ActiveChannel     string    `json:"active_channel"`
	ListeningChannels []string  `json:"listening_channels"`
	Profile           string    `json:"profile"`
	ProxShort         bool      `json:"prox_short"`
	ScOnline          bool      `json:"sc_online"`
	IsTalking         bool      `json:"is_talking"`
	IsRadioRepeater   bool      `json:"is_radio_repeater"`
	Language          string    `json:"language"`
}

type MsgPlayerTalking struct {
	Type      string `json:"type"`
	Name      string `json:"name"`
	IsTalking bool   `json:"is_talking"`
}

// MsgWelcome is sent immediately after a successful authentication
type MsgWelcome struct {
	Type                  string        `json:"type"`
	Players               []PlayerState `json:"players"`
	AnonymousMode         bool          `json:"anonymous_mode"`
	Channels              []string      `json:"channels"`
	Profiles              []string      `json:"profiles"`
	MyActiveChannel       string        `json:"my_active_channel"`
	MyListeningChs        []string      `json:"my_listening_channels"`
	MyProfile             string        `json:"my_profile"`
	AudioTicket           string        `json:"audio_ticket"`
	SpatialAudioSupported bool          `json:"spatial_audio_supported"`
}

// MsgPlayerJoin notifies other players of a new client joining
type MsgPlayerJoin struct {
	Type              string   `json:"type"`
	Name              string   `json:"name"`
	ActiveChannel     string   `json:"active_channel"`
	ListeningChannels []string `json:"listening_channels"`
	Profile           string   `json:"profile"`
	ProxShort         bool     `json:"prox_short"`
	IsRadioRepeater   bool     `json:"is_radio_repeater"`
	Language          string   `json:"language"`
}

// MsgPlayerPos broadcasts an updated player position
type MsgPlayerPos struct {
	Type      string   `json:"type"`
	Name      string   `json:"name"`
	Pos       Position `json:"pos"`
	TsCapture float64  `json:"ts_capture"`
}

// MsgPlayerLeave notifies of a player's disconnection or timeout
type MsgPlayerLeave struct {
	Type string `json:"type"`
	Name string `json:"name"`
}

// MsgScStatus broadcasts Star Citizen's active/inactive game state
type MsgScStatus struct {
	Type string `json:"type"`
	Name string `json:"name"`
}

// MsgPlayerHelmet broadcasts a player's helmet state change
type MsgPlayerHelmet struct {
	Type     string `json:"type"`
	Name     string `json:"name"`
	HelmetOn bool   `json:"helmet_on"`
}

// MsgPlayerChannel broadcasts a player's active channel change
type MsgPlayerChannel struct {
	Type    string `json:"type"`
	Name    string `json:"name"`
	Channel string `json:"channel"`
}

// MsgPlayerListening broadcasts an update of the channels a player is listening to
type MsgPlayerListening struct {
	Type     string   `json:"type"`
	Name     string   `json:"name"`
	Channels []string `json:"channels"`
}


// MsgPlayerProxShort broadcasts a player's whisper mode change
type MsgPlayerProxShort struct {
	Type   string `json:"type"`
	Name   string `json:"name"`
	Active bool   `json:"active"`
}

// MsgPlayerRepeater broadcasts a player's repeater mode change
type MsgPlayerRepeater struct {
	Type   string `json:"type"`
	Name   string `json:"name"`
	Active bool   `json:"active"`
}

// MsgToggleRepeater is sent by the client to enable/disable repeater mode
type MsgToggleRepeater struct {
	Type   string `json:"type"`
	Active bool   `json:"active"`
}

// MsgAnonymousMode broadcasts the global anonymous mode change
type MsgAnonymousMode struct {
	Type   string `json:"type"`
	Active bool   `json:"active"`
}

// MsgChannelsList broadcasts an update to the channels list
type MsgChannelsList struct {
	Type     string   `json:"type"`
	Channels []string `json:"channels"`
}

// MsgProfilesList broadcasts an update to the profiles list
type MsgProfilesList struct {
	Type     string   `json:"type"`
	Profiles []string `json:"profiles"`
}

// MsgPlayerProfile broadcasts the assignment of a profile to a player
type MsgPlayerProfile struct {
	Type    string `json:"type"`
	Name    string `json:"name"`
	Profile string `json:"profile"`
}

// MsgError sends an explicit error to the client
type MsgError struct {
	Type    string `json:"type"`
	Reason  string `json:"reason"`
	Message string `json:"message"`
}

// MsgPong returns a pong in response to a ping
type MsgPong struct {
	Type string `json:"type"`
}

// Admin Commands (Client to Server)

// AdminCommand represents a raw command sent by an admin
type AdminCommand struct {
	Cmd   string      `json:"cmd"`
	ReqID string      `json:"req_id,omitempty"`
	Name  string      `json:"name,omitempty"`
	Old   string      `json:"old,omitempty"`
	New   string      `json:"new,omitempty"`
	Token string      `json:"token,omitempty"`
	User  string      `json:"user,omitempty"` // For kicks/assignments
	Value interface{} `json:"value,omitempty"`
}

// Admin Responses (Server to Client)

// MsgAdminWelcome sends the full state to a newly connected admin
type MsgAdminWelcome struct {
	Type          string        `json:"type"`
	Channels      []string      `json:"channels"`
	Profiles      []string      `json:"profiles"`
	Players       []PlayerState `json:"players"`
	AnonymousMode bool          `json:"anonymous_mode"`
}

// MsgAdminResponse returns the result of an administration command
type MsgAdminResponse struct {
	Type   string      `json:"type"`
	ReqID  string      `json:"req_id,omitempty"`
	Cmd    string      `json:"cmd"`
	Ok     bool        `json:"ok"`
	Reason string      `json:"reason,omitempty"`
	Value  interface{} `json:"value,omitempty"`
}

// MsgLogPush broadcasts server logs to connected admins
type MsgLogPush struct {
	Type  string `json:"type"`
	Msg   string `json:"msg"`
	Color string `json:"color"`
	TS    string `json:"ts"`
}

// BannedIPInfo represents a banned IP address
type BannedIPInfo struct {
	IP        string `json:"ip"`
	Reason    string `json:"reason"`
	CreatedAt string `json:"created_at"`
}

// BannedHwidInfo represents a banned hardware ID (HWID)
type BannedHwidInfo struct {
	Hwid      string `json:"hwid"`
	Reason    string `json:"reason"`
	CreatedAt string `json:"created_at"`
}

// MsgAdminRefresh tells all connected admins to refresh a specific database panel/list
type MsgAdminRefresh struct {
	Type string `json:"type"`
	Tab  string `json:"tab"`
}

// Calling State Constants
const (
	HailStateIdle      = 0
	HailStateOutgoing  = 1
	HailStateIncoming  = 2
	HailStateConnected = 3
)

// MsgHailRequest represents a request to call another player
type MsgHailRequest struct {
	Type   string `json:"type"`
	Target string `json:"target"`
}

// MsgHailIncoming represents an incoming call notification
type MsgHailIncoming struct {
	Type string `json:"type"`
	Peer string `json:"peer"`
}

// MsgHailRinging represents an outgoing ringing notification
type MsgHailRinging struct {
	Type string `json:"type"`
	Peer string `json:"peer"`
}

// MsgHailConnected represents a connected call notification
type MsgHailConnected struct {
	Type string `json:"type"`
	Peer string `json:"peer"`
}

// MsgHailDisconnected represents a disconnected call notification
type MsgHailDisconnected struct {
	Type   string `json:"type"`
	Reason string `json:"reason"`
}

// MsgHailError represents a calling error
type MsgHailError struct {
	Type   string `json:"type"`
	Reason string `json:"reason"`
}
