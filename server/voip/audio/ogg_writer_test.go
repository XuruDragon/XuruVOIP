package audio

import (
	"bytes"
	"os"
	"path/filepath"
	"testing"
)

func TestOggWriter(t *testing.T) {
	tempDir, err := os.MkdirTemp("", "ogg_test")
	if err != nil {
		t.Fatalf("failed to create temp dir: %v", err)
	}
	defer os.RemoveAll(tempDir)

	filePath := filepath.Join(tempDir, "test.ogg")
	writer, err := NewOggWriter(filePath)
	if err != nil {
		t.Fatalf("failed to create ogg writer: %v", err)
	}

	// Write 5 mock Opus packets
	packets := [][]byte{
		{0x01, 0x02, 0x03, 0x04},
		{0x05, 0x06},
		{0x07, 0x08, 0x09, 0x0A, 0x0B},
		bytes.Repeat([]byte{0xFF}, 300), // Larger packet to test segment splitting (> 255)
		{0x0C},
	}

	for _, pkt := range packets {
		if err := writer.WriteOpusPacket(pkt); err != nil {
			t.Fatalf("failed to write opus packet: %v", err)
		}
	}

	if err := writer.Close(); err != nil {
		t.Fatalf("failed to close writer: %v", err)
	}

	// Read and validate the file
	data, err := os.ReadFile(filePath)
	if err != nil {
		t.Fatalf("failed to read generated ogg file: %v", err)
	}

	// Verify size is non-zero
	if len(data) == 0 {
		t.Error("generated file is empty")
	}

	// Verify "OggS" capture pattern exists at the beginning of the file
	if !bytes.Equal(data[0:4], []byte("OggS")) {
		t.Errorf("invalid OggS signature: %v", data[0:4])
	}
}
