package audio

import (
	"encoding/binary"
	"net"
	"os"
	"testing"
	"time"

	"xuruvoip/server/voip/core"
)

func TestDiscordBridgeStartDisabled(t *testing.T) {
	// Disable bridge explicitly
	core.EnableDiscordBridge = false
	defer func() { core.EnableDiscordBridge = true }()

	// Ensure no panic when starting
	StartDiscordBridge()
}

func TestDiscordBridgeMissingCredentials(t *testing.T) {
	core.EnableDiscordBridge = true
	os.Unsetenv("XURUVOIP_DISCORD_TOKEN")
	os.Unsetenv("XURUVOIP_DISCORD_GUILD_ID")
	os.Unsetenv("XURUVOIP_DISCORD_CHANNEL_ID")

	// Should skip cleanly without panicking
	StartDiscordBridge()
}

func TestInjectRadioAudio(t *testing.T) {
	// Initialize ActiveHub state
	core.ActiveHub = core.Hub{
		Players: make(map[string]*core.ActivePlayer),
	}

	// Listen on a local UDP address to act as the client
	clientAddr, err := net.ResolveUDPAddr("udp", "127.0.0.1:0")
	if err != nil {
		t.Fatalf("Failed to resolve client UDP address: %v", err)
	}
	clientConn, err := net.ListenUDP("udp", clientAddr)
	if err != nil {
		t.Fatalf("Failed to listen client UDP: %v", err)
	}
	defer clientConn.Close()

	resolvedClientAddr := clientConn.LocalAddr().(*net.UDPAddr)

	// Create target player listening to "General"
	targetPlayer := &core.ActivePlayer{
		Name:              "Alice",
		ActiveChannel:     "General",
		ListeningChannels: []string{"General"},
	}
	targetPlayer.SafeSetUDPAddr(resolvedClientAddr)
	core.ActiveHub.Players["Alice"] = targetPlayer

	// Setup server UDP connection
	serverAddr, err := net.ResolveUDPAddr("udp", "127.0.0.1:0")
	if err != nil {
		t.Fatalf("Failed to resolve server UDP address: %v", err)
	}
	serverConn, err := net.ListenUDP("udp", serverAddr)
	if err != nil {
		t.Fatalf("Failed to listen server UDP: %v", err)
	}
	defer serverConn.Close()

	// Set the global package-level udpConn
	udpConn = serverConn

	// Call InjectRadioAudio to send dummy Opus data
	dummyOpus := []byte{0x12, 0x34, 0x56, 0x78}
	InjectRadioAudio("DiscordBot", 42, dummyOpus, "General")

	// Read packet from client connection
	clientConn.SetReadDeadline(time.Now().Add(1 * time.Second))
	buf := make([]byte, 1024)
	n, _, err := clientConn.ReadFrom(buf)
	if err != nil {
		t.Fatalf("Failed to read UDP packet on client: %v", err)
	}

	packet := buf[:n]
	if len(packet) < 4 {
		t.Fatalf("Packet too short: %d bytes", len(packet))
	}

	// Format: [Seq (2)] [AudioType (1)] [NameLen (1)] [Name (NameLen)] [AudioData]
	seq := binary.BigEndian.Uint16(packet[0:2])
	if seq != 42 {
		t.Errorf("Expected sequence 42, got %d", seq)
	}

	audioType := packet[2]
	if audioType != core.AudioTypeRadio {
		t.Errorf("Expected audio type %d (Radio), got %d", core.AudioTypeRadio, audioType)
	}

	nameLen := int(packet[3])
	expectedName := "DiscordBot"
	if nameLen != len(expectedName) {
		t.Fatalf("Expected name length %d, got %d", len(expectedName), nameLen)
	}

	name := string(packet[4 : 4+nameLen])
	if name != expectedName {
		t.Errorf("Expected sender name '%s', got '%s'", expectedName, name)
	}

	audioData := packet[4+nameLen:]
	if string(audioData) != string(dummyOpus) {
		t.Errorf("Expected audio data %v, got %v", dummyOpus, audioData)
	}
}
