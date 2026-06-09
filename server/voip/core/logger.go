package core

import (
	"fmt"
	"os"
	"path/filepath"
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
		logFile = nil
	}
}

// Log prints a message with timestamp and color, pushes it to connected admins, and writes to log file
func Log(msg string, color string) {
	ts := time.Now().Format("2006-01-02 15:04:05.000")
	consoleTS := time.Now().Format("15:04:05.000")
	fmt.Printf("[%s%s%s] %s%s%s\n", ColorCyan, consoleTS, ColorReset, color, msg, ColorReset)

	// Broadcast log to all connected admins
	ActiveHub.BroadcastLog(msg, color)

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
