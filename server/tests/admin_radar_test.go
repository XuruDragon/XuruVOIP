package tests

import (
	"crypto/tls"
	"encoding/json"
	"fmt"
	"os"
	"path/filepath"
	"testing"
	"time"

	"github.com/gorilla/websocket"

	"xuruvoip/server/voip/core"
	"xuruvoip/server/voip/position"
)

func TestAdminRadarCoordinateBroadcast(t *testing.T) {
	// 1. Setup temporary database environment
	tempDir, err := os.MkdirTemp("", "xuruvoip-radartest-*")
	if err != nil {
		t.Fatalf("Failed to create temp dir: %v", err)
	}
	defer os.RemoveAll(tempDir)

	t.Setenv("XURUVOIP_DATA_DIR", tempDir)
	t.Setenv("XURUVOIP_SERVER_PASSWORD", "testsecret32characterlongtokenok")
	t.Setenv("XURUVOIP_ADMIN_SERVER_PASSWORD", "testadminsecret32charstokenok123")
	t.Setenv("XURUVOIP_PUBLIC_SERVER", "0")
	t.Setenv("XURUVOIP_SPATIAL_AUDIO", "1")
	t.Setenv("XURUVOIP_VERBOSE_LOGS", "0")

	// Initialize database and logger
	if err := core.LoadOrCreateConfig(); err != nil {
		t.Fatalf("Failed to load or create config: %v", err)
	}
	core.InitSecurityManagers()
	defer core.CloseLogger()

	// Ensure certs exist in temp directory
	certPath := filepath.Join(tempDir, "cert.pem")
	keyPath := filepath.Join(tempDir, "key.pem")
	ok, _ := core.EnsureSelfSignedCert(certPath, keyPath, "xuruvoip-server", 365)
	if !ok {
		t.Fatal("Failed to generate test certificates")
	}

	// Create test administrator credentials
	err = core.DBCreateAdmin("test_admin_user", "test_admin_pass")
	if err != nil {
		t.Fatalf("Failed to create test admin user: %v", err)
	}

	// 2. Start positions server in background on test port
	posPort := 19999
	go position.StartPositionsServer(posPort, certPath, keyPath)

	// Allow server time to spin up
	time.Sleep(200 * time.Millisecond)

	posURL := fmt.Sprintf("wss://localhost:%d/", posPort)
	dialer := websocket.Dialer{
		TLSClientConfig: &tls.Config{InsecureSkipVerify: true},
	}

	// 3. Connect Admin WebSocket
	adminConn, _, err := dialer.Dial(posURL, nil)
	if err != nil {
		t.Fatalf("Admin WebSocket dial failed: %v", err)
	}
	defer adminConn.Close()

	// Authenticate Admin
	authMsg := core.MsgAuthAdmin{
		Type:           "auth_admin",
		Username:       "test_admin_user",
		Password:       "test_admin_pass",
		ServerPassword: "testadminsecret32charstokenok123",
	}
	if err := adminConn.WriteJSON(authMsg); err != nil {
		t.Fatalf("Failed to write MsgAuthAdmin: %v", err)
	}

	// Read welcome response to verify auth succeeded
	_, welcomePayload, err := adminConn.ReadMessage()
	if err != nil {
		t.Fatalf("Failed to read admin welcome message: %v", err)
	}
	var welcome BaseMessage
	if err := json.Unmarshal(welcomePayload, &welcome); err != nil {
		t.Fatalf("Failed to unmarshal BaseMessage: %v", err)
	}
	if welcome.Type != "admin_welcome" {
		t.Fatalf("Expected admin_welcome type, but got: %s", welcome.Type)
	}

	// 4. Connect Player WebSocket
	playerConn, _, err := dialer.Dial(posURL, nil)
	if err != nil {
		t.Fatalf("Player WebSocket dial failed: %v", err)
	}
	defer playerConn.Close()

	joinMsg := core.MsgJoin{
		Type:     "join",
		Token:    "testsecret32characterlongtokenok",
		Name:     "TestPlayer1",
		Password: "playerpass",
		Channel:  "General",
	}
	if err := playerConn.WriteJSON(joinMsg); err != nil {
		t.Fatalf("Failed to write player MsgJoin: %v", err)
	}

	// Read player welcome
	_, playerWelcomePayload, err := playerConn.ReadMessage()
	if err != nil {
		t.Fatalf("Failed to read player welcome: %v", err)
	}
	var playerWelcome core.MsgWelcome
	if err := json.Unmarshal(playerWelcomePayload, &playerWelcome); err != nil {
		t.Fatalf("Failed to unmarshal player welcome: %v", playerWelcome)
	}
	if playerWelcome.Type != "welcome" {
		t.Fatalf("Expected welcome type, but got: %s", playerWelcome.Type)
	}

	// 5. Send player coordinate update
	posMsg := core.MsgPos{
		Type: "pos",
		Pos: core.Position{
			X:    123.45,
			Y:    678.90,
			Z:    -999.0,
			Zone: "Stanton",
		},
		TsCapture: 12345.67,
	}
	if err := playerConn.WriteJSON(posMsg); err != nil {
		t.Fatalf("Failed to write player coordinates: %v", err)
	}

	// 6. Admin should receive coordinate broadcast for radar map
	var radarPos core.MsgPlayerPos
	foundPos := false

	// Set a read deadline to prevent hanging forever
	_ = adminConn.SetReadDeadline(time.Now().Add(2 * time.Second))

	for i := 0; i < 20; i++ {
		_, radarPayload, err := adminConn.ReadMessage()
		if err != nil {
			t.Fatalf("Failed to read admin message: %v", err)
		}

		var temp BaseMessage
		if err := json.Unmarshal(radarPayload, &temp); err != nil {
			continue
		}

		if temp.Type == "pos" {
			if err := json.Unmarshal(radarPayload, &radarPos); err == nil {
				foundPos = true
				break
			}
		}
	}

	if !foundPos {
		t.Fatal("Failed to receive 'pos' message broadcast on admin channel within 20 messages")
	}

	// 7. Verify correctness of radar coordinates broadcast
	if radarPos.Type != "pos" {
		t.Errorf("Expected broadcast type 'pos', got: %s", radarPos.Type)
	}
	if radarPos.Name != "TestPlayer1" {
		t.Errorf("Expected coordinate owner name 'TestPlayer1', got: %s", radarPos.Name)
	}
	if radarPos.Pos.X != 123.45 || radarPos.Pos.Y != 678.90 || radarPos.Pos.Z != -999.0 {
		t.Errorf("Expected coordinate values (123.45, 678.90, -999.0), got: (%f, %f, %f)", radarPos.Pos.X, radarPos.Pos.Y, radarPos.Pos.Z)
	}
	if radarPos.Pos.Zone != "Stanton" {
		t.Errorf("Expected coordinate zone 'Stanton', got: %s", radarPos.Pos.Zone)
	}
	if radarPos.TsCapture != 12345.67 {
		t.Errorf("Expected coordinate capture timestamp 12345.67, got: %f", radarPos.TsCapture)
	}
}

// Helper struct matching positions server welcome types
type BaseMessage struct {
	Type string `json:"type"`
}
