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
)

// ANSI color codes for clean console logs
const (
	ColorReset  = "\033[0m"
	ColorRed    = "\033[31m"
	ColorGreen  = "\033[32m"
	ColorYellow = "\033[33m"
	ColorBlue   = "\033[34m"
	ColorPurple = "\033[35m"
	ColorCyan   = "\033[36m"
	ColorOrange = "\033[38;5;208m"
	ColorMuted  = "\033[90m"
)

var logFile *os.File

// InitLogger opens the xuruvoip.log file in append mode
func InitLogger() {
	dataDir := ResolveDataDir()
	logPath := filepath.Join(dataDir, "xuruvoip.log")
	f, err := os.OpenFile(logPath, os.O_CREATE|os.O_WRONLY|os.O_APPEND, 0666)
	if err == nil {
		logFile = f
	}
}

// CloseLogger closes the log file descriptor
func CloseLogger() {
	if logFile != nil {
		_ = logFile.Close()
	}
}

// InitSecurityManagers dynamically instantiates rate limiters and auth lockout policies from config/env
func InitSecurityManagers() {
	maxFailures := ParseEnvInt("XURUVOIP_LOCKOUT_ATTEMPTS", 5)
	windowSec := int64(ParseEnvInt("XURUVOIP_LOCKOUT_WINDOW", 60))
	banSec := int64(ParseEnvInt("XURUVOIP_LOCKOUT_DURATION", 600))

	posRate := ParseEnvFloat("XURUVOIP_LIMIT_RATE_POS", 50.0)
	posBurst := ParseEnvFloat("XURUVOIP_LIMIT_BURST_POS", 100.0)

	audioRate := ParseEnvFloat("XURUVOIP_LIMIT_RATE_AUDIO", 60.0)
	audioBurst := ParseEnvFloat("XURUVOIP_LIMIT_BURST_AUDIO", 120.0)

	posLockout = NewAuthLockout(maxFailures, windowSec, banSec)
	audioLockout = NewAuthLockout(maxFailures, windowSec, banSec)

	posLimit = NewRateLimiterHub(posRate, posBurst)
	audioLimit = NewRateLimiterHub(audioRate, audioBurst)
}

// Log prints a message with timestamp and color, pushes it to connected admins, and writes to log file
func Log(msg string, color string) {
	ts := time.Now().Format("2006-01-02 15:04:05.000")
	consoleTS := time.Now().Format("15:04:05.000")
	fmt.Printf("[%s%s%s] %s%s%s\n", ColorCyan, consoleTS, ColorReset, color, msg, ColorReset)

	// Broadcast log to all connected admins
	hub.BroadcastLog(msg, color)

	// Write to log file without colors
	if logFile != nil {
		_, _ = logFile.WriteString(fmt.Sprintf("[%s] %s\n", ts, msg))
	}
}

// Masked returns a masked version of a token for secure logging
func Masked(token string) string {
	if len(token) < 4 {
		return "***"
	}
	return token[:4] + "***"
}

func main() {
	// Load environment variables from .env
	LoadEnv()
	InitLogger()
	defer CloseLogger()

	// 1. Parse command line flags
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

	fmt.Println(strings.Repeat("=", 60))
	fmt.Printf("             XuruVoip Server in Go (v%s)\n", ServerVersion)
	fmt.Println(strings.Repeat("=", 60))

	// 2. Load configurations
	Log("Loading configurations...", ColorReset)
	if err := LoadOrCreateConfig(); err != nil {
		Log(fmt.Sprintf("Critical configuration error: %v", err), ColorRed)
		os.Exit(1)
	}

	// Check for updates in background
	go CheckForUpdates()

	// Initialize rate limiters and auth lockout policies from settings/env
	InitSecurityManagers()

	dataDir := ResolveDataDir()
	Log(fmt.Sprintf("Data directory: %s", dataDir), ColorMuted)
	Log(fmt.Sprintf("PLAYER TOKEN : %s (visible in full in the .env file)", Masked(serverConfig.ServerToken)), ColorBlue)
	Log("ADMIN TOKEN  : (stored encrypted in the SQLite database)", ColorPurple)

	// 3. Ensure TLS certificates exist
	certPath := filepath.Join(dataDir, "cert.pem")
	keyPath := filepath.Join(dataDir, "key.pem")
	ok, detail := EnsureSelfSignedCert(certPath, keyPath, "xuruvoip-server", 3650)
	if !ok {
		Log(fmt.Sprintf("[FATAL] Failed to ensure TLS certificate presence: %s", detail), ColorRed)
		os.Exit(1)
	}
	if detail == "generated" {
		Log("Self-signed TLS certificates generated successfully", ColorGreen)
	}
	Log("TLS enabled: servers configured with WSS://", ColorGreen)

	// 4. Start servers in separate goroutines
	publicModeStr := "DISABLED (server password required)"
	if PublicServer {
		publicModeStr = "ENABLED (server password not required)"
	}
	Log(fmt.Sprintf("Public server mode : %s", publicModeStr), ColorBlue)

	adminIP := BindIP
	if adminIP == "0.0.0.0" {
		adminIP = "localhost"
	}
	Log(fmt.Sprintf("Admin portal URL   : https://%s:%d/admin", adminIP, port), ColorPurple)

	go StartPositionsServer(port, certPath, keyPath)
	go StartAudioServer(audioPort, certPath, keyPath)

	// 5. Start player timeout cleanup loop (timeout after 30s)
	go func() {
		for {
			time.Sleep(5 * time.Second)
			timedOutPlayers := hub.CleanupTimeouts(30 * time.Second)
			for _, name := range timedOutPlayers {
				Log(fmt.Sprintf("Timeout: %s (no activity for 30s)", name), ColorOrange)
				hub.BroadcastPosMessageToAll(MsgPlayerLeave{
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
	Log("Server shutdown requested...", ColorYellow)

	// Close all connections
	hub.mu.Lock()
	for _, p := range hub.players {
		if p.PosConn != nil {
			_ = p.PosConn.Close()
		}
		if p.AudioConn != nil {
			_ = p.AudioConn.Close()
		}
	}
	for _, admin := range hub.admins {
		if admin.Conn != nil {
			_ = admin.Conn.Close()
		}
	}
	hub.mu.Unlock()

	Log("XuruVoip server stopped cleanly.", ColorGreen)
}
