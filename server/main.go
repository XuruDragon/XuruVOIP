package main

import (
	"flag"
	"fmt"
	"os"
	"os/signal"
	"path/filepath"
	"strings"
	"syscall"
	"time"

	"xuruvoip/server/voip/audio"
	"xuruvoip/server/voip/core"
	"xuruvoip/server/voip/position"
)

func main() {
	portFlag := flag.Int("port", 8888, "Position server port")
	audioPortFlag := flag.Int("audio-port", 8889, "Audio server port")
	flag.Parse()

	port := *portFlag
	if envPort := os.Getenv("XURUVOIP_PORT"); envPort != "" && port == 8888 {
		var p int
		if _, err := fmt.Sscanf(envPort, "%d", &p); err == nil {
			port = p
		}
	}

	audioPort := *audioPortFlag
	if envAudioPort := os.Getenv("XURUVOIP_AUDIO_PORT"); envAudioPort != "" && audioPort == 8889 {
		var ap int
		if _, err := fmt.Sscanf(envAudioPort, "%d", &ap); err == nil {
			audioPort = ap
		}
	}

	// 1. Initialize environments and logs
	core.LoadEnv()
	core.RotateLogs()
	core.InitLogger()
	defer core.CloseLogger()

	fmt.Println(strings.Repeat("=", 60))
	fmt.Printf("             XuruVoip Server in Go (v%s)\n", core.ServerVersion)
	fmt.Println(strings.Repeat("=", 60))

	// 2. Load configurations
	core.Log("Loading configurations...", core.ColorReset)
	if err := core.LoadOrCreateConfig(); err != nil {
		core.Log(fmt.Sprintf("Critical configuration error: %v", err), core.ColorRed)
		os.Exit(1)
	}

	// Check for updates in background
	go core.CheckForUpdates()

	// Initialize rate limiters and auth lockout policies from settings/env
	core.InitSecurityManagers()

	dataDir := core.ResolveDataDir()
	core.Log(fmt.Sprintf("Data directory: %s", dataDir), core.ColorMuted)
	core.Log(fmt.Sprintf("PLAYER TOKEN : %s (visible in full in the .env file)", core.Masked(core.ServerConfig.ServerToken)), core.ColorBlue)
	core.Log("ADMIN TOKEN  : (stored encrypted in the SQLite database)", core.ColorPurple)

	// 3. Ensure TLS certificates exist
	certPath := filepath.Join(dataDir, "cert.pem")
	keyPath := filepath.Join(dataDir, "key.pem")
	ok, detail := core.EnsureSelfSignedCert(certPath, keyPath, "xuruvoip-server", 3650)
	if !ok {
		core.Log(fmt.Sprintf("[FATAL] Failed to ensure TLS certificate presence: %s", detail), core.ColorRed)
		os.Exit(1)
	}
	if detail == "generated" {
		core.Log("Self-signed TLS certificates generated successfully", core.ColorGreen)
	}
	core.Log("TLS enabled: servers configured with WSS://", core.ColorGreen)

	// 4. Start servers in separate goroutines
	publicModeStr := "DISABLED (server password required)"
	if core.PublicServer {
		publicModeStr = "ENABLED (server password not required)"
	}
	core.Log(fmt.Sprintf("Public server mode : %s", publicModeStr), core.ColorBlue)

	adminIP := core.BindIP
	if adminIP == "0.0.0.0" {
		adminIP = "localhost"
	}
	core.Log(fmt.Sprintf("Admin portal URL   : https://%s:%d/admin", adminIP, port), core.ColorPurple)

	go position.StartPositionsServer(port, certPath, keyPath)
	go audio.StartAudioServer(audioPort, certPath, keyPath)
	go audio.StartDiscordBridge()

	// 5. Start player timeout cleanup loop (timeout after 30s)
	go func() {
		for {
			time.Sleep(5 * time.Second)
			timedOutPlayers := core.ActiveHub.CleanupTimeouts(30 * time.Second)
			for _, name := range timedOutPlayers {
				core.Log(fmt.Sprintf("Timeout: %s (no activity for 30s)", name), core.ColorOrange)
				core.ActiveHub.BroadcastPosMessageToAll(core.MsgPlayerLeave{
					Type: "leave",
					Name: name,
				})
			}
		}
	}()

	// 6. Handle clean shutdown signals
	stop := make(chan os.Signal, 1)
	signal.Notify(stop, os.Interrupt, syscall.SIGTERM)

	<-stop
	fmt.Println()
	core.Log("Server shutdown requested...", core.ColorYellow)

	// Close all connections cleanly
	audio.CloseDiscordBridge()
	core.ActiveHub.Shutdown()

	core.Log("XuruVoip server stopped cleanly.", core.ColorGreen)
}
