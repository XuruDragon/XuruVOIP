package audio

import (
	"os"
	"path/filepath"
	"testing"
	"time"

	"xuruvoip/server/voip/core"
)

func TestAarRecordingTargets(t *testing.T) {
	// GIVEN
	target := "radio:StrikeForce"

	// WHEN
	SetAarRecordingTarget(target, true)

	// THEN
	status := GetAarRecordingStatus()
	if !status[target] {
		t.Errorf("Expected target %s to be active, got inactive", target)
	}

	// WHEN
	SetAarRecordingTarget(target, false)

	// THEN
	status = GetAarRecordingStatus()
	if status[target] {
		t.Errorf("Expected target %s to be inactive, got active", target)
	}
}

func TestAarDatabaseOperations(t *testing.T) {
	// Setup a temporary data directory for SQLite database
	tempDir, err := os.MkdirTemp("", "aar_db_test")
	if err != nil {
		t.Fatalf("Failed to create temp dir: %v", err)
	}
	defer os.RemoveAll(tempDir)

	os.Setenv("XURUVOIP_DATA_DIR", tempDir)
	defer os.Unsetenv("XURUVOIP_DATA_DIR")

	// Initialize database
	if err := core.InitDB(); err != nil {
		t.Fatalf("Failed to initialize database: %v", err)
	}

	// GIVEN a mock recording
	rec := core.AarRecording{
		ID:         "mock_rec_123",
		PlayerName: "XuruDragon",
		StartTime:  time.Now().Truncate(time.Second), // Truncate to match SQLite resolution
		DurationMs: 4200,
		Channel:    "StrikeForce",
		AudioType:  1,
		FilePath:   "recordings/mock_rec_123.ogg",
	}

	// WHEN saving the recording
	if err := core.DBSaveAarRecording(rec); err != nil {
		t.Fatalf("Failed to save AAR recording: %v", err)
	}

	// THEN we should retrieve it correctly
	list, err := core.DBGetAarRecordings()
	if err != nil {
		t.Fatalf("Failed to get AAR recordings: %v", err)
	}

	if len(list) != 1 {
		t.Fatalf("Expected 1 recording, got %d", len(list))
	}

	retrieved := list[0]
	if retrieved.ID != rec.ID {
		t.Errorf("Expected ID %s, got %s", rec.ID, retrieved.ID)
	}
	if retrieved.PlayerName != rec.PlayerName {
		t.Errorf("Expected PlayerName %s, got %s", rec.PlayerName, retrieved.PlayerName)
	}
	if retrieved.DurationMs != rec.DurationMs {
		t.Errorf("Expected DurationMs %d, got %d", rec.DurationMs, retrieved.DurationMs)
	}
	if retrieved.Channel != rec.Channel {
		t.Errorf("Expected Channel %s, got %s", rec.Channel, retrieved.Channel)
	}
	if retrieved.AudioType != rec.AudioType {
		t.Errorf("Expected AudioType %d, got %d", rec.AudioType, retrieved.AudioType)
	}

	// WHEN deleting the recording
	if err := core.DBDeleteAarRecording(rec.ID); err != nil {
		t.Fatalf("Failed to delete recording: %v", err)
	}

	// THEN list should be empty
	list, err = core.DBGetAarRecordings()
	if err != nil {
		t.Fatalf("Failed to get AAR recordings after delete: %v", err)
	}
	if len(list) != 0 {
		t.Errorf("Expected 0 recordings after delete, got %d", len(list))
	}
}

func TestAarPositionLogging(t *testing.T) {
	// Setup a temporary data directory
	tempDir, err := os.MkdirTemp("", "aar_pos_test")
	if err != nil {
		t.Fatalf("Failed to create temp dir: %v", err)
	}
	defer os.RemoveAll(tempDir)

	os.Setenv("XURUVOIP_DATA_DIR", tempDir)
	defer os.Unsetenv("XURUVOIP_DATA_DIR")

	// Ensure recordings directory exists
	recDir := filepath.Join(tempDir, "recordings")
	if err := os.MkdirAll(recDir, 0755); err != nil {
		t.Fatalf("Failed to create recordings dir: %v", err)
	}

	// Initialize database
	if err := core.InitDB(); err != nil {
		t.Fatalf("Failed to initialize database: %v", err)
	}

	sessionID := "test_pos_session_123"
	playerName := "TestPilot"

	posFilePath := filepath.Join(recDir, sessionID+"_positions.jsonl")
	posFile, err := os.Create(posFilePath)
	if err != nil {
		t.Fatalf("Failed to create positions file: %v", err)
	}

	// Write mock positions
	_, _ = posFile.WriteString("{\"t\": 0, \"x\": 100, \"y\": 200, \"z\": 300, \"zone\": \"Stanton\"}\n")
	_, _ = posFile.WriteString("{\"t\": 500, \"x\": 105, \"y\": 205, \"z\": 305, \"zone\": \"Stanton\"}\n")

	oggPath := filepath.Join(recDir, sessionID+".ogg")
	writer, err := NewOggWriter(oggPath)
	if err != nil {
		t.Fatalf("Failed to create OggWriter: %v", err)
	}

	// Create and register session
	session := &RecordingSession{
		Writer:      writer,
		ID:          sessionID,
		PlayerName:  playerName,
		StartTime:   time.Now().Add(-500 * time.Millisecond), // Make sure duration > 100ms
		Channel:     "General",
		AudioType:   0,
		FilePath:    filepath.Join("recordings", sessionID+".ogg"),
		PosFile:     posFile,
		LastLogTime: time.Now(),
	}

	aarMu.Lock()
	if activeRecordings == nil {
		activeRecordings = make(map[string]*RecordingSession)
	}
	activeRecordings[playerName] = session
	aarMu.Unlock()

	// STOP the recording session
	stopRecordingSession(playerName)

	// Verify session was removed from active recordings
	aarMu.Lock()
	_, active := activeRecordings[playerName]
	aarMu.Unlock()
	if active {
		t.Error("Expected session to be inactive after stopRecordingSession")
	}

	// Verify position file exists and contains the written lines
	if _, err := os.Stat(posFilePath); os.IsNotExist(err) {
		t.Error("Expected positions file to exist, but it was not found")
	}

	content, err := os.ReadFile(posFilePath)
	if err != nil {
		t.Fatalf("Failed to read positions file: %v", err)
	}
	expectedContent := "{\"t\": 0, \"x\": 100, \"y\": 200, \"z\": 300, \"zone\": \"Stanton\"}\n{\"t\": 500, \"x\": 105, \"y\": 205, \"z\": 305, \"zone\": \"Stanton\"}\n"
	if string(content) != expectedContent {
		t.Errorf("Expected content %q, got %q", expectedContent, string(content))
	}

	// Verify record exists in DB
	list, err := core.DBGetAarRecordings()
	if err != nil {
		t.Fatalf("Failed to get AAR recordings: %v", err)
	}
	if len(list) != 1 {
		t.Fatalf("Expected 1 AAR recording in DB, got %d", len(list))
	}
	if list[0].ID != sessionID {
		t.Errorf("Expected recording ID %s, got %s", sessionID, list[0].ID)
	}
}
