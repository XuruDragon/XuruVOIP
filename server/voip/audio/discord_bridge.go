package audio

import (
	"fmt"
	"os"
	"sync"
	"sync/atomic"
	"time"

	"github.com/bwmarrin/discordgo"
	"xuruvoip/server/voip/core"
)

var (
	dgSession          *discordgo.Session
	voiceConn          *discordgo.VoiceConnection
	voiceConnMu        sync.RWMutex
	discordBridgeChan string = "General"

	ssrcMu     sync.RWMutex
	ssrcToName = make(map[uint32]string)
	userCacheMu sync.RWMutex
	userCache   = make(map[string]string)

	discordSeq uint32
)

// StartDiscordBridge initializes the Discord session and joins the voice channel
func StartDiscordBridge() {
	if !core.EnableDiscordBridge {
		core.Log("Discord Bridge: Disabled by config", core.ColorOrange)
		return
	}

	token := os.Getenv("XURUVOIP_DISCORD_TOKEN")
	guildID := os.Getenv("XURUVOIP_DISCORD_GUILD_ID")
	channelID := os.Getenv("XURUVOIP_DISCORD_CHANNEL_ID") // Voice channel ID
	
	if token == "" || guildID == "" || channelID == "" {
		core.Log("Discord Bridge: Credentials missing, skipping bridge startup", core.ColorOrange)
		return
	}

	if ch := os.Getenv("XURUVOIP_DISCORD_BRIDGE_CHANNEL"); ch != "" {
		discordBridgeChan = ch
	}

	core.Log(fmt.Sprintf("Discord Bridge: Starting for guild %s, channel %s, bridge channel %s", guildID, channelID, discordBridgeChan), core.ColorBlue)

	var err error
	dgSession, err = discordgo.New("Bot " + token)
	if err != nil {
		core.Log(fmt.Sprintf("Discord Bridge: Failed to create session: %v", err), core.ColorRed)
		return
	}

	// Register speaking update handler
	dgSession.AddHandler(func(s *discordgo.Session, v *discordgo.VoiceSpeakingUpdate) {
		ssrcMu.Lock()
		defer ssrcMu.Unlock()
		
		// If already mapped and not changed, return
		if _, ok := ssrcToName[uint32(v.SSRC)]; ok {
			return
		}

		// Resolve username in background
		go func(uid string, ssrc uint32) {
			userCacheMu.RLock()
			cachedName, exists := userCache[uid]
			userCacheMu.RUnlock()

			if exists {
				ssrcMu.Lock()
				ssrcToName[ssrc] = cachedName + " (Discord)"
				ssrcMu.Unlock()
				return
			}

			// Fetch member
			name := "Discord User"
			member, err := s.GuildMember(guildID, uid)
			if err == nil && member.User != nil {
				name = member.Nick
				if name == "" {
					name = member.User.Username
				}
			}

			userCacheMu.Lock()
			userCache[uid] = name
			userCacheMu.Unlock()

			ssrcMu.Lock()
			ssrcToName[ssrc] = name + " (Discord)"
			ssrcMu.Unlock()
		}(v.UserID, uint32(v.SSRC))
	})

	err = dgSession.Open()
	if err != nil {
		core.Log(fmt.Sprintf("Discord Bridge: Failed to open session: %v", err), core.ColorRed)
		return
	}

	go connectVoiceLoop(guildID, channelID)
}

func connectVoiceLoop(guildID, channelID string) {
	for {
		if dgSession == nil {
			return
		}

		core.Log("Discord Bridge: Attempting to join voice channel...", core.ColorBlue)
		vc, err := dgSession.ChannelVoiceJoin(guildID, channelID, false, false)
		if err != nil {
			core.Log(fmt.Sprintf("Discord Bridge: Voice join error: %v. Retrying in 10s...", err), core.ColorRed)
			time.Sleep(10 * time.Second)
			continue
		}

		voiceConnMu.Lock()
		voiceConn = vc
		voiceConnMu.Unlock()

		core.Log("Discord Bridge: Connected to Discord Voice Channel!", core.ColorGreen)
		
		// Start receiving loop
		recvChan := vc.OpusRecv
		if recvChan != nil {
			for packet := range recvChan {
				ssrc := packet.SSRC
				ssrcMu.RLock()
				name, ok := ssrcToName[ssrc]
				ssrcMu.RUnlock()

				if !ok {
					name = "Discord Speaker"
				}

				// Generate sequence number increment
				seq := uint16(atomic.AddUint32(&discordSeq, 1) & 0xFFFF)

				// Inject to Go server clients
				InjectRadioAudio(name, seq, packet.Opus, discordBridgeChan)
			}
		}

		core.Log("Discord Bridge: Disconnected from voice channel. Reconnecting in 5s...", core.ColorOrange)
		time.Sleep(5 * time.Second)
	}
}

// SendToDiscord forwards Opus audio frames from Go server players to the Discord channel
func SendToDiscord(senderChannel string, opusData []byte) {
	if !core.EnableDiscordBridge {
		return
	}
	if senderChannel != discordBridgeChan {
		return
	}

	voiceConnMu.RLock()
	vc := voiceConn
	voiceConnMu.RUnlock()

	if vc == nil {
		return
	}

	// Non-blocking send
	select {
	case vc.OpusSend <- opusData:
	default:
		// Drop frame if buffer full to prevent blocking the audio server
	}
}

// CloseDiscordBridge closes the Discord connection
func CloseDiscordBridge() {
	voiceConnMu.Lock()
	if voiceConn != nil {
		_ = voiceConn.Disconnect()
		voiceConn = nil
	}
	voiceConnMu.Unlock()

	if dgSession != nil {
		_ = dgSession.Close()
		dgSession = nil
	}
	core.Log("Discord Bridge: Closed.", core.ColorOrange)
}
