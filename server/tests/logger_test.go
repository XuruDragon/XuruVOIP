package tests

import (
	"fmt"
	"os"
	"path/filepath"
	"strings"
	"testing"
	"time"

	"xuruvoip/server/voip/core"
)

func TestRotateLogs(t *testing.T) {
	// GIVEN: Set up a temporary directory to act as the data directory
	tempDir, err := os.MkdirTemp("", "xuruvoip_test_*")
	if err != nil {
		t.Fatalf("Failed to create temp directory: %v", err)
	}
	defer os.RemoveAll(tempDir)

	// Set env var to point to temp dir
	originalDataDir := os.Getenv("XURUVOIP_DATA_DIR")
	defer os.Setenv("XURUVOIP_DATA_DIR", originalDataDir)
	os.Setenv("XURUVOIP_DATA_DIR", tempDir)

	logPath := filepath.Join(tempDir, "xuruvoip.log")

	// Scenario 1: No log file exists
	core.RotateLogs()
	if _, err := os.Stat(logPath); err == nil {
		t.Error("Expected xuruvoip.log to not exist yet")
	}

	// Scenario 2: Log file exists but modification date is today -> should NOT rotate
	err = os.WriteFile(logPath, []byte("today logs"), 0600)
	if err != nil {
		t.Fatalf("Failed to write active log file: %v", err)
	}
	core.RotateLogs()

	// Active log file should still exist and no rotated files should be created
	if _, err := os.Stat(logPath); err != nil {
		t.Error("Active log file should still exist")
	}

	files, _ := os.ReadDir(tempDir)
	for _, f := range files {
		if f.Name() != "xuruvoip.log" {
			t.Errorf("Unexpected file created: %s", f.Name())
		}
	}

	// Scenario 3: Log file exists and modification date is yesterday -> should rotate
	yesterday := time.Now().AddDate(0, 0, -1)
	err = os.Chtimes(logPath, yesterday, yesterday)
	if err != nil {
		t.Fatalf("Failed to change modification time: %v", err)
	}

	core.RotateLogs()

	// Active log file should no longer exist (rotated away)
	if _, err := os.Stat(logPath); err == nil {
		t.Error("Active log file should have been rotated away")
	}

	// Rotated log file should exist
	rotatedName := fmt.Sprintf("xuruvoip.%s.log", yesterday.Format("2006-01-02"))
	rotatedPath := filepath.Join(tempDir, rotatedName)
	if _, err := os.Stat(rotatedPath); err != nil {
		t.Errorf("Expected rotated log file to exist: %s", rotatedName)
	}

	content, err := os.ReadFile(rotatedPath)
	if err != nil || string(content) != "today logs" {
		t.Errorf("Rotated file has incorrect content: %s", string(content))
	}

	// Scenario 4: Pruning log files (exceeding 5 rotated files)
	// Let's create 6 rotated logs in the past
	for i := 1; i <= 6; i++ {
		pastDate := time.Now().AddDate(0, 0, -10+i)
		pastName := fmt.Sprintf("xuruvoip.%s.log", pastDate.Format("2006-01-02"))
		pastPath := filepath.Join(tempDir, pastName)
		err = os.WriteFile(pastPath, []byte(fmt.Sprintf("Log from %d", i)), 0600)
		if err != nil {
			t.Fatalf("Failed to write past log: %v", err)
		}
		err = os.Chtimes(pastPath, pastDate, pastDate)
		if err != nil {
			t.Fatalf("Failed to change modification time: %v", err)
		}
	}

	// Create yesterday's active log to trigger rotation again
	err = os.WriteFile(logPath, []byte("trigger logs"), 0600)
	if err != nil {
		t.Fatalf("Failed to write active log: %v", err)
	}
	err = os.Chtimes(logPath, yesterday, yesterday)
	if err != nil {
		t.Fatalf("Failed to change modification time: %v", err)
	}

	core.RotateLogs()

	// Read rotated files in tempDir
	dirEntries, _ := os.ReadDir(tempDir)
	var rotatedCount int
	for _, entry := range dirEntries {
		if strings.HasPrefix(entry.Name(), "xuruvoip.") && entry.Name() != "xuruvoip.log" {
			rotatedCount++
		}
	}

	// Should have exactly 5 rotated logs
	if rotatedCount != 5 {
		t.Errorf("Expected exactly 5 rotated logs, got %d", rotatedCount)
	}

	// The oldest one (i = 1 and i = 2) should have been pruned
	prunedDate1 := time.Now().AddDate(0, 0, -10+1)
	prunedName1 := fmt.Sprintf("xuruvoip.%s.log", prunedDate1.Format("2006-01-02"))
	if _, err := os.Stat(filepath.Join(tempDir, prunedName1)); err == nil {
		t.Errorf("Expected oldest log %s to be pruned", prunedName1)
	}

	prunedDate2 := time.Now().AddDate(0, 0, -10+2)
	prunedName2 := fmt.Sprintf("xuruvoip.%s.log", prunedDate2.Format("2006-01-02"))
	if _, err := os.Stat(filepath.Join(tempDir, prunedName2)); err == nil {
		t.Errorf("Expected second oldest log %s to be pruned", prunedName2)
	}

	// The newer ones (e.g. i = 3) should still exist
	keptDate3 := time.Now().AddDate(0, 0, -10+3)
	keptName3 := fmt.Sprintf("xuruvoip.%s.log", keptDate3.Format("2006-01-02"))
	if _, err := os.Stat(filepath.Join(tempDir, keptName3)); err != nil {
		t.Errorf("Expected log %s to be kept", keptName3)
	}
}
