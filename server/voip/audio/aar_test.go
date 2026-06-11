package audio

import (
	"os"
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
