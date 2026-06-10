package audio

import (
	"encoding/binary"
	"fmt"
	"math"
	"net"
	"strings"
	"sync"
	"sync/atomic"
	"time"

	"xuruvoip/server/voip/core"
)

// Statistics counters (using atomic operations)
var (
	udpConn            *net.UDPConn
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
	// TLS certs are not used for raw UDP audio packets, but parameters are kept to preserve main.go interface
	addr, err := net.ResolveUDPAddr("udp", fmt.Sprintf("%s:%d", core.BindIP, port))
	if err != nil {
		core.Log(fmt.Sprintf("Failed to resolve UDP address: %v", err), core.ColorRed)
		return
	}

	conn, err := net.ListenUDP("udp", addr)
	if err != nil {
		core.Log(fmt.Sprintf("Failed to listen on UDP port %d: %v", port, err), core.ColorRed)
		return
	}
	udpConn = conn
	defer conn.Close()

	core.Log(fmt.Sprintf("Starting UDP audio server on %s:%d...", core.BindIP, port), core.ColorBlue)
	go reportStatsLoop()
	go sweepTalkingLoop()

	buf := make([]byte, 65535)
	for {
		n, remoteAddr, err := conn.ReadFromUDP(buf)
		if err != nil {
			if strings.Contains(err.Error(), "use of closed network connection") {
				break
			}
			continue
		}

		if n < 1 {
			continue
		}

		packet := make([]byte, n)
		copy(packet, buf[:n])
		go handleUDPPacket(packet, remoteAddr)
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
			if p.SafeGetUDPAddr() != nil {
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

func handleUDPPacket(packet []byte, remoteAddr *net.UDPAddr) {
	ip := remoteAddr.IP.String()
	packetType := packet[0]

	// 1. Handshake & Registration packet: [0xFF] [NameLen] [Name (NameLen)] [AudioTicket (32)]
	if packetType == 0xFF {
		if len(packet) < 2 {
			return
		}
		nameLen := int(packet[1])
		if len(packet) < 2+nameLen+32 {
			return
		}
		name := string(packet[2 : 2+nameLen])
		ticket := string(packet[2+nameLen : 2+nameLen+32])

		if core.AudioLockout.IsBanned(ip) {
			core.Log(fmt.Sprintf("REJECT UDP Audio: IP %s temporarily banned", ip), core.ColorRed)
			return
		}

		core.ActiveHub.Mu.RLock()
		player, exists := core.ActiveHub.Players[name]
		core.ActiveHub.Mu.RUnlock()

		if !exists {
			core.AudioLockout.RecordFailure(ip)
			core.Log(fmt.Sprintf("REJECT UDP Audio: Invalid registration from %s (unknown client: %s)", ip, name), core.ColorRed)
			return
		}

		// Validate ticket
		if player.AudioTicket == "" || !core.ConstantTimeCompare(ticket, player.AudioTicket) {
			core.AudioLockout.RecordFailure(ip)
			core.Log(fmt.Sprintf("REJECT UDP Audio: Invalid/expired ticket from %s (client: %s). Got: '%s', Expected: '%s'", ip, name, ticket, player.AudioTicket), core.ColorRed)
			return
		}

		core.AudioLockout.RecordSuccess(ip)
		player.SafeSetUDPAddr(remoteAddr)
		core.Log(fmt.Sprintf("JOIN UDP Audio: %s (%s)", name, ip), core.ColorGreen)

		// Reply ACK: [0xFE]
		if udpConn != nil {
			_, _ = udpConn.WriteToUDP([]byte{0xFE}, remoteAddr)
		}
		return
	}

	// 2. Audio Processing (Rate Limited)
	if !core.AudioLimit.Allow(ip) {
		return
	}

	// Audio Format: [Seq (2)] [AudioType (1)] [OpusPayload]
	if len(packet) < 3 {
		return
	}

	seq := binary.BigEndian.Uint16(packet[0:2])
	audioType := packet[2]
	audioData := packet[3:]

	// Resolve sender by UDP address
	var senderName string
	var sender *core.ActivePlayer

	core.ActiveHub.Mu.RLock()
	for name, p := range core.ActiveHub.Players {
		addr := p.SafeGetUDPAddr()
		if addr != nil && addr.String() == remoteAddr.String() {
			senderName = name
			sender = p
			break
		}
	}
	core.ActiveHub.Mu.RUnlock()

	if sender == nil {
		// Drop packets from unregistered UDP ports
		return
	}

	// Update player talking state
	core.ActiveHub.Mu.Lock()
	var broadcastTalkingMsg bool
	var isTalkingVal bool
	if len(audioData) > 0 {
		if !sender.IsTalking {
			sender.IsTalking = true
			broadcastTalkingMsg = true
			isTalkingVal = true
		}
		sender.LastTalkTime = time.Now()
	} else {
		if sender.IsTalking {
			sender.IsTalking = false
			broadcastTalkingMsg = true
			isTalkingVal = false
		}
	}
	core.ActiveHub.Mu.Unlock()

	if broadcastTalkingMsg {
		core.ActiveHub.BroadcastToAdmins(core.MsgPlayerTalking{
			Type:      "talking",
			Name:      senderName,
			IsTalking: isTalkingVal,
		})
	}

	atomic.AddUint64(&audioTotalBytes, uint64(len(packet)))
	atomic.AddUint64(&audioTotalFrames, 1)

	// Resolve recipients
	var targets []*core.ActivePlayer
	switch audioType {
	case core.AudioTypeProximity:
		targets = core.ActiveHub.GetAudioPlayersInProximity(senderName)
		if core.VerboseLogs >= 2 {
			atomic.AddUint64(&proxFramesTotal, 1)
		}
	case core.AudioTypeRadio:
		targets = core.ActiveHub.GetAudioPlayersInRadioChannel(senderName)
		if core.VerboseLogs >= 2 {
			atomic.AddUint64(&radioFramesTotal, 1)
		}
		if core.VerboseLogs >= 3 {
			core.ActiveHub.Mu.RLock()
			ch := sender.ActiveChannel
			core.ActiveHub.Mu.RUnlock()
			if ch != "" {
				statsMu.Lock()
				radioChannelFrames[ch]++
				statsMu.Unlock()
			}
		}
	case core.AudioTypeProfile:
		targets = core.ActiveHub.GetAudioPlayersInProfile(senderName)
		if core.VerboseLogs >= 2 {
			atomic.AddUint64(&profileFramesTotal, 1)
		}
		if core.VerboseLogs >= 3 {
			core.ActiveHub.Mu.RLock()
			prof := sender.Profile
			core.ActiveHub.Mu.RUnlock()
			if prof != "" {
				statsMu.Lock()
				profileFrames[prof]++
				statsMu.Unlock()
			}
		}
	}

	if len(targets) == 0 {
		return
	}

	nameBytes := []byte(senderName)
	nameLen := len(nameBytes)
	if nameLen > 255 {
		nameBytes = nameBytes[:255]
		nameLen = 255
	}

	if udpConn == nil {
		return
	}

	if audioType == core.AudioTypeProximity {
		// Proximity audio: package sender coordinates and relative distance
		core.ActiveHub.Mu.RLock()
		if sender.Pos == nil {
			core.ActiveHub.Mu.RUnlock()
			return
		}
		senderPos := *sender.Pos
		senderProxShort := sender.ProxShort
		core.ActiveHub.Mu.RUnlock()

		metaSize := 1 + 4 + 4
		if core.SpatialAudioEnabled {
			metaSize += 12
		}

		for _, targetPlayer := range targets {
			targetAddr := targetPlayer.SafeGetUDPAddr()
			core.ActiveHub.Mu.RLock()
			targetPos := targetPlayer.Pos
			targetProxShort := targetPlayer.ProxShort
			core.ActiveHub.Mu.RUnlock()

			if targetAddr == nil || targetPos == nil {
				continue
			}

			dx := senderPos.X - targetPos.X
			dy := senderPos.Y - targetPos.Y
			dz := senderPos.Z - targetPos.Z
			dist := math.Sqrt(dx*dx + dy*dy + dz*dz)

			maxRange := 50.0
			if senderProxShort || targetProxShort {
				maxRange = 5.0
			}

			// Format: [Seq (2)] [AudioType (1)] [NameLen (1)] [Name (NameLen)] [SpatialEnabled (1)] [Distance (4)] [MaxRange (4)] [SpeakerX/Y/Z (12 - optional)] [AudioData]
			packetToSend := make([]byte, 2+1+1+nameLen+metaSize+len(audioData))
			binary.BigEndian.PutUint16(packetToSend[0:2], seq)
			packetToSend[2] = core.AudioTypeProximity
			packetToSend[3] = byte(nameLen)
			copy(packetToSend[4:], nameBytes)

			offset := 4 + nameLen
			if core.SpatialAudioEnabled {
				packetToSend[offset] = 1
			} else {
				packetToSend[offset] = 0
			}

			binary.LittleEndian.PutUint32(packetToSend[offset+1:], math.Float32bits(float32(dist)))
			binary.LittleEndian.PutUint32(packetToSend[offset+5:], math.Float32bits(float32(maxRange)))

			if core.SpatialAudioEnabled {
				binary.LittleEndian.PutUint32(packetToSend[offset+9:], math.Float32bits(float32(senderPos.X)))
				binary.LittleEndian.PutUint32(packetToSend[offset+13:], math.Float32bits(float32(senderPos.Y)))
				binary.LittleEndian.PutUint32(packetToSend[offset+17:], math.Float32bits(float32(senderPos.Z)))
			}

			copy(packetToSend[offset+metaSize:], audioData)

			_, _ = udpConn.WriteToUDP(packetToSend, targetAddr)
		}
	} else {
		// Radio / Profile audio: Broadcast directly
		// Format: [Seq (2)] [AudioType (1)] [NameLen (1)] [Name (NameLen)] [AudioData]
		packetToSend := make([]byte, 2+1+1+nameLen+len(audioData))
		binary.BigEndian.PutUint16(packetToSend[0:2], seq)
		packetToSend[2] = audioType
		packetToSend[3] = byte(nameLen)
		copy(packetToSend[4:], nameBytes)
		copy(packetToSend[4+nameLen:], audioData)

		for _, targetPlayer := range targets {
			targetAddr := targetPlayer.SafeGetUDPAddr()
			if targetAddr == nil {
				continue
			}
			_, _ = udpConn.WriteToUDP(packetToSend, targetAddr)
		}
	}
}

func sweepTalkingLoop() {
	for {
		time.Sleep(200 * time.Millisecond)
		var timedOutPlayers []string

		core.ActiveHub.Mu.Lock()
		now := time.Now()
		for _, player := range core.ActiveHub.Players {
			if player.IsTalking && now.Sub(player.LastTalkTime) > 400*time.Millisecond {
				player.IsTalking = false
				timedOutPlayers = append(timedOutPlayers, player.Name)
			}
		}
		core.ActiveHub.Mu.Unlock()

		for _, name := range timedOutPlayers {
			core.ActiveHub.BroadcastToAdmins(core.MsgPlayerTalking{
				Type:      "talking",
				Name:      name,
				IsTalking: false,
			})
		}
	}
}
