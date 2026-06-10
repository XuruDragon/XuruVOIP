package core

import (
	"fmt"
	"os"
	"path/filepath"
	"sort"
	"strings"
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

// RotateLogs checks the xuruvoip.log file and rotates it if its modification time is from a previous day.
func RotateLogs() {
	dataDir := ResolveDataDir()
	logPath := filepath.Join(dataDir, "xuruvoip.log")

	fi, err := os.Stat(logPath)
	if err != nil {
		if os.IsNotExist(err) {
			return
		}
		return
	}

	modTime := fi.ModTime()
	now := time.Now()

	// Compare dates (year, month, day)
	if modTime.Year() < now.Year() || 
		(modTime.Year() == now.Year() && modTime.Month() < now.Month()) || 
		(modTime.Year() == now.Year() && modTime.Month() == now.Month() && modTime.Day() < now.Day()) {
		
		// Rename xuruvoip.log to xuruvoip.yyyy-MM-dd.log using the date the log was written
		dateStr := modTime.Format("2006-01-02")
		rotatedPath := filepath.Join(dataDir, fmt.Sprintf("xuruvoip.%s.log", dateStr))

		// Remove existing rotated file if it exists, then rename the current active log to it
		_ = os.Remove(rotatedPath)
		_ = os.Rename(logPath, rotatedPath)

		// Keep only the last 5 rotated files
		files, err := os.ReadDir(dataDir)
		if err != nil {
			return
		}

		var rotatedFiles []os.DirEntry
		for _, file := range files {
			if !file.IsDir() && strings.HasPrefix(file.Name(), "xuruvoip.") && strings.HasSuffix(file.Name(), ".log") {
				if file.Name() != "xuruvoip.log" {
					rotatedFiles = append(rotatedFiles, file)
				}
			}
		}

		// Sort rotated files alphabetically (since YYYY-MM-DD name layout yields chronological order)
		sort.Slice(rotatedFiles, func(i, j int) bool {
			return rotatedFiles[i].Name() < rotatedFiles[j].Name()
		})

		// Delete oldest rotated files until we have at most 5
		for len(rotatedFiles) > 5 {
			oldestPath := filepath.Join(dataDir, rotatedFiles[0].Name())
			_ = os.Remove(oldestPath)
			rotatedFiles = rotatedFiles[1:]
		}
	}
}

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
