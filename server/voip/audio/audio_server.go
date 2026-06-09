package audio

import (
	"encoding/binary"
	"encoding/json"
	"fmt"
	"math"
	"net/http"
	"strings"
	"sync"
	"sync/atomic"
	"time"

	"github.com/gorilla/websocket"
	"xuruvoip/server/voip/core"
)

var upgrader = websocket.Upgrader{
	CheckOrigin: func(r *http.Request) bool {
		return true // Allow all origins for local gaming networks
	},
}

// Statistics counters (using atomic operations)
var (
	audioTotalBytes    uint64
	audioTotalFrames   uint64
	proxFramesTotal    uint64
	radioFramesTotal   uint64
	profileFramesTotal uint64
	statsMu            sync.Mutex
	radioChannelFrames = make(map[string]uint64)
	profileFrames      = make(map[string]uint64)
)

// StartAudioServer starts the audio server on the specified port
func StartAudioServer(port int, certFile, keyFile string) {
	mux := http.NewServeMux()
	mux.HandleFunc("/", handleAudioWS)

	server := &http.Server{
		Addr:    fmt.Sprintf("%s:%d", core.BindIP, port),
		Handler: mux,
	}

	core.Log(fmt.Sprintf("Starting audio server on %s:%d (WSS)...", core.BindIP, port), core.ColorBlue)
	go reportStatsLoop()

	var err error
	if certFile != "" && keyFile != "" {
		err = server.ListenAndServeTLS(certFile, keyFile)
	} else {
		err = server.ListenAndServe()
	}

	if err != nil {
		core.Log(fmt.Sprintf("Audio server error: %v", err), core.ColorRed)
	}
}

func reportStatsLoop() {
	for {
		time.Sleep(5 * time.Second)
		if core.VerboseLogs == 0 {
			atomic.SwapUint64(&audioTotalBytes, 0)
			atomic.SwapUint64(&audioTotalFrames, 0)
			atomic.SwapUint64(&proxFramesTotal, 0)
			atomic.SwapUint64(&radioFramesTotal, 0)
			atomic.SwapUint64(&profileFramesTotal, 0)
			statsMu.Lock()
			radioChannelFrames = make(map[string]uint64)
			profileFrames = make(map[string]uint64)
			statsMu.Unlock()
			continue
		}

		bytes := atomic.SwapUint64(&audioTotalBytes, 0)
		frames := atomic.SwapUint64(&audioTotalFrames, 0)

		kbs := float64(bytes) / 5.0 / 1024.0
		fps := float64(frames) / 5.0

		core.ActiveHub.Mu.RLock()
		clientCount := 0
		for _, p := range core.ActiveHub.Players {
			p.AudioMu.Lock()
			hasAudio := (p.AudioConn != nil)
			p.AudioMu.Unlock()
			if hasAudio {
				clientCount++
			}
		}
		core.ActiveHub.Mu.RUnlock()

		if clientCount == 0 && kbs == 0 {
			continue
		}

		if core.VerboseLogs == 1 {
			core.Log(fmt.Sprintf("[STATS] %d audio client(s) | %.1f frames/s | %.1f kB/s", clientCount, fps, kbs), core.ColorPurple)
		} else if core.VerboseLogs == 2 {
			prox := atomic.SwapUint64(&proxFramesTotal, 0)
			rad := atomic.SwapUint64(&radioFramesTotal, 0)
			prof := atomic.SwapUint64(&profileFramesTotal, 0)

			proxFps := float64(prox) / 5.0
			radFps := float64(rad) / 5.0
			profFps := float64(prof) / 5.0

			core.Log(fmt.Sprintf("[STATS] %d audio client(s) | Proximity: %.1f frames/s | Radio (all): %.1f frames/s | Profile (all): %.1f frames/s | %.1f kB/s",
				clientCount, proxFps, radFps, profFps, kbs), core.ColorPurple)
		} else if core.VerboseLogs >= 3 {
			prox := atomic.SwapUint64(&proxFramesTotal, 0)
			atomic.SwapUint64(&radioFramesTotal, 0)
			atomic.SwapUint64(&profileFramesTotal, 0)

			proxFps := float64(prox) / 5.0

			statsMu.Lock()
			channelsSnapshot := radioChannelFrames
			profilesSnapshot := profileFrames
			radioChannelFrames = make(map[string]uint64)
			profileFrames = make(map[string]uint64)
			statsMu.Unlock()

			var details []string
			details = append(details, fmt.Sprintf("Proximity: %.1f frames/s", proxFps))

			if len(channelsSnapshot) > 0 {
				var chDetails []string
				for ch, count := range channelsSnapshot {
					chDetails = append(chDetails, fmt.Sprintf("%s: %.1f frames/s", ch, float64(count)/5.0))
				}
				details = append(details, fmt.Sprintf("Radio (%s)", strings.Join(chDetails, ", ")))
			} else {
				details = append(details, "Radio: 0.0 frames/s")
			}

			if len(profilesSnapshot) > 0 {
				var profDetails []string
				for prof, count := range profilesSnapshot {
					profDetails = append(profDetails, fmt.Sprintf("%s: %.1f frames/s", prof, float64(count)/5.0))
				}
				details = append(details, fmt.Sprintf("Profile (%s)", strings.Join(profDetails, ", ")))
			} else {
				details = append(details, "Profile: 0.0 frames/s")
			}

			core.Log(fmt.Sprintf("[STATS] %d audio client(s) | %s | %.1f kB/s",
				clientCount, strings.Join(details, " | "), kbs), core.ColorPurple)
		}
	}
}

func handleAudioWS(w http.ResponseWriter, r *http.Request) {
	conn, err := upgrader.Upgrade(w, r, nil)
	if err != nil {
		return
	}
	defer conn.Close()

	ip := core.ExtractIP(r.RemoteAddr)

	// Check brute force lockout
	if core.AudioLockout.IsBanned(ip) {
		core.Log(fmt.Sprintf("REJECT Audio: IP %s temporarily banned", ip), core.ColorRed)
		_ = conn.WriteControl(
			websocket.CloseMessage,
			websocket.FormatCloseMessage(websocket.ClosePolicyViolation, "banned"),
			time.Now().Add(time.Second),
		)
		return
	}

	// 1. Read first authentication join message
	_, payload, err := conn.ReadMessage()
	if err != nil {
		return
	}

	var base core.MessageBase
	if err := json.Unmarshal(payload, &base); err != nil {
		return
	}

	if base.Type != "join" {
		_ = conn.WriteControl(
			websocket.CloseMessage,
			websocket.FormatCloseMessage(websocket.ClosePolicyViolation, "invalid_initial_message"),
			time.Now().Add(time.Second),
		)
		return
	}

	var msg core.MsgJoin
	if err := json.Unmarshal(payload, &msg); err != nil {
		return
	}

	// Validate token
	if !core.PublicServer && core.ServerConfig.ServerToken != "" && !core.ConstantTimeCompare(msg.Token, core.ServerConfig.ServerToken) {
		core.AudioLockout.RecordFailure(ip)
		core.Log(fmt.Sprintf("REJECT Audio: Invalid token from %s (client: %s)", ip, msg.Name), core.ColorRed)
		_ = conn.WriteJSON(core.MsgError{
			Type:    "error",
			Reason:  "invalid_token",
			Message: "Invalid server token",
		})
		return
	}

	// Validate ticket and bind audio socket
	name := strings.TrimSpace(msg.Name)
	if ok := core.ActiveHub.BindAudioConn(name, msg.AudioTicket, conn); !ok {
		core.AudioLockout.RecordFailure(ip)
		core.Log(fmt.Sprintf("REJECT Audio: Invalid or expired ticket from %s (client: %s)", ip, name), core.ColorRed)
		_ = conn.WriteJSON(core.MsgError{
			Type:    "error",
			Reason:  "invalid_ticket",
			Message: "Invalid or expired ticket. Connect to the position server first.",
		})
		return
	}

	core.AudioLockout.RecordSuccess(ip)
	core.Log(fmt.Sprintf("JOIN Audio: %s (%s)", name, ip), core.ColorGreen)

	// Binary packet header buffer preparation
	nameBytes := []byte(name)
	nameLen := len(nameBytes)
	if nameLen > 255 {
		nameBytes = nameBytes[:255]
		nameLen = 255
	}

	// Audio message processing loop
	for {
		mt, payload, err := conn.ReadMessage()
		if err != nil {
			break
		}

		// Rate Limiting
		if !core.AudioLimit.Allow(conn) {
			continue
		}

		if mt == websocket.BinaryMessage {
			if len(payload) < 2 {
				continue
			}

			// Packet structure: [AudioType (1 byte)] + [AudioData]
			audioType := payload[0]
			audioData := payload[1:]

			atomic.AddUint64(&audioTotalBytes, uint64(len(payload)))
			atomic.AddUint64(&audioTotalFrames, 1)

			// Resolve recipient audio sockets based on transmission type
			var targets []*core.ActivePlayer
			switch audioType {
			case core.AudioTypeProximity:
				targets = core.ActiveHub.GetAudioPlayersInProximity(name)
				if core.VerboseLogs >= 2 {
					atomic.AddUint64(&proxFramesTotal, 1)
				}
			case core.AudioTypeRadio:
				targets = core.ActiveHub.GetAudioPlayersInRadioChannel(name)
				if core.VerboseLogs >= 2 {
					atomic.AddUint64(&radioFramesTotal, 1)
				}
				if core.VerboseLogs >= 3 {
					core.ActiveHub.Mu.RLock()
					p, ok := core.ActiveHub.Players[name]
					ch := ""
					if ok {
						ch = p.ActiveChannel
					}
					core.ActiveHub.Mu.RUnlock()
					if ch != "" {
						statsMu.Lock()
						radioChannelFrames[ch]++
						statsMu.Unlock()
					}
				}
			case core.AudioTypeProfile:
				targets = core.ActiveHub.GetAudioPlayersInProfile(name)
				if core.VerboseLogs >= 2 {
					atomic.AddUint64(&profileFramesTotal, 1)
				}
				if core.VerboseLogs >= 3 {
					core.ActiveHub.Mu.RLock()
					p, ok := core.ActiveHub.Players[name]
					prof := ""
					if ok {
						prof = p.Profile
					}
					core.ActiveHub.Mu.RUnlock()
					if prof != "" {
						statsMu.Lock()
						profileFrames[prof]++
						statsMu.Unlock()
					}
				}
			}

			if len(targets) == 0 {
				continue
			}

			if audioType == core.AudioTypeProximity {
				// Proximity audio: send custom packet to each target with distance/maxRange/position metadata
				core.ActiveHub.Mu.RLock()
				sender, senderExists := core.ActiveHub.Players[name]
				core.ActiveHub.Mu.RUnlock()
				if !senderExists || sender.Pos == nil {
					continue
				}

				// Metadata size: SpatialEnabled (1), Distance (4), MaxRange (4)
				metaSize := 1 + 4 + 4
				if core.SpatialAudioEnabled {
					metaSize += 12 // SpeakerX, SpeakerY, SpeakerZ (float32)
				}

				for _, targetPlayer := range targets {
					if targetPlayer.Pos == nil {
						continue
					}

					dx := sender.Pos.X - targetPlayer.Pos.X
					dy := sender.Pos.Y - targetPlayer.Pos.Y
					dz := sender.Pos.Z - targetPlayer.Pos.Z
					dist := math.Sqrt(dx*dx + dy*dy + dz*dz)

					maxRange := 50.0
					if sender.ProxShort || targetPlayer.ProxShort {
						maxRange = 5.0
					}

					// Build custom packet
					packet := make([]byte, 2+nameLen+metaSize+len(audioData))
					packet[0] = core.AudioTypeProximity
					packet[1] = byte(nameLen)
					copy(packet[2:], nameBytes)

					offset := 2 + nameLen
					if core.SpatialAudioEnabled {
						packet[offset] = 1
					} else {
						packet[offset] = 0
					}

					binary.LittleEndian.PutUint32(packet[offset+1:], math.Float32bits(float32(dist)))
					binary.LittleEndian.PutUint32(packet[offset+5:], math.Float32bits(float32(maxRange)))

					if core.SpatialAudioEnabled {
						binary.LittleEndian.PutUint32(packet[offset+9:], math.Float32bits(float32(sender.Pos.X)))
						binary.LittleEndian.PutUint32(packet[offset+13:], math.Float32bits(float32(sender.Pos.Y)))
						binary.LittleEndian.PutUint32(packet[offset+17:], math.Float32bits(float32(sender.Pos.Z)))
					}

					copy(packet[2+nameLen+metaSize:], audioData)

					_ = targetPlayer.SafeWriteAudioMessage(websocket.BinaryMessage, packet)
				}
			} else {
				// Radio or Profile audio: send same broadcastPacket to all targets
				broadcastPacket := make([]byte, 2+nameLen+len(audioData))
				broadcastPacket[0] = audioType
				broadcastPacket[1] = byte(nameLen)
				copy(broadcastPacket[2:], nameBytes)
				copy(broadcastPacket[2+nameLen:], audioData)

				// Relaying audio packet to target clients
				for _, targetPlayer := range targets {
					_ = targetPlayer.SafeWriteAudioMessage(websocket.BinaryMessage, broadcastPacket)
				}
			}

		} else if mt == websocket.TextMessage {
			var m core.MessageBase
			if err := json.Unmarshal(payload, &m); err == nil && m.Type == "ping" {
				core.ActiveHub.Mu.RLock()
				p, ok := core.ActiveHub.Players[name]
				core.ActiveHub.Mu.RUnlock()
				if ok {
					_ = p.SafeWriteAudioJSON(core.MsgPong{Type: "pong"})
				}
			}
		}
	}

	// Disconnection cleanup
	if leftName, fullyLeft := core.ActiveHub.UnregisterAudioConn(conn); leftName != "" {
		core.AudioLimit.Forget(conn)
		if fullyLeft {
			core.Log(fmt.Sprintf("LEAVE: %s (disconnected)", leftName), core.ColorOrange)
			core.ActiveHub.BroadcastPosMessageToAll(core.MsgPlayerLeave{
				Type: "leave",
				Name: leftName,
			})
		}
	}
}
