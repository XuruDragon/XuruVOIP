package main

import (
	"crypto/tls"
	"encoding/json"
	"fmt"
	"os"
	"path/filepath"
	"testing"
	"time"

	"github.com/gorilla/websocket"
)

func TestAdminCommandsWorkflow(t *testing.T) {
	// 1. Setup temporary database environment
	tempDir, err := os.MkdirTemp("", "xuruvoip-admincmdtest-*")
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
	if err := LoadOrCreateConfig(); err != nil {
		t.Fatalf("Failed to load or create config: %v", err)
	}
	InitSecurityManagers()
	defer CloseLogger()

	// Ensure certs exist in temp directory
	certPath := filepath.Join(tempDir, "cert.pem")
	keyPath := filepath.Join(tempDir, "key.pem")
	ok, _ := EnsureSelfSignedCert(certPath, keyPath, "xuruvoip-server", 365)
	if !ok {
		t.Fatal("Failed to generate test certificates")
	}

	// Create test administrator credentials
	err = DBCreateAdmin("admin_cmd_user", "admin_cmd_pass")
	if err != nil {
		t.Fatalf("Failed to create test admin user: %v", err)
	}

	// 2. Start positions server in background on test port
	posPort := 19998
	go StartPositionsServer(posPort, certPath, keyPath)

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
	authMsg := MsgAuthAdmin{
		Type:           "auth_admin",
		Username:       "admin_cmd_user",
		Password:       "admin_cmd_pass",
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

	// Start background reader to demux responses
	respChan := make(chan MsgAdminResponse, 20)
	chanListChan := make(chan MsgChannelsList, 20)

	go func() {
		for {
			_, payload, err := adminConn.ReadMessage()
			if err != nil {
				return
			}
			var base struct {
				Type string `json:"type"`
			}
			if err := json.Unmarshal(payload, &base); err != nil {
				continue
			}
			switch base.Type {
			case "admin_response":
				var resp MsgAdminResponse
				if err := json.Unmarshal(payload, &resp); err == nil {
					respChan <- resp
				}
			case "channels_list":
				var list MsgChannelsList
				if err := json.Unmarshal(payload, &list); err == nil {
					chanListChan <- list
				}
			}
		}
	}()

	// Helper to get next response with timeout
	waitForResponse := func(reqID string) MsgAdminResponse {
		timeout := time.After(2 * time.Second)
		for {
			select {
			case resp := <-respChan:
				if resp.ReqID == reqID {
					return resp
				}
			case <-timeout:
				t.Fatalf("Timeout waiting for admin_response for reqID %s", reqID)
				return MsgAdminResponse{}
			}
		}
	}

	// Helper to get next channels list with timeout
	waitForChannelsList := func() MsgChannelsList {
		select {
		case list := <-chanListChan:
			return list
		case <-time.After(2 * time.Second):
			t.Fatalf("Timeout waiting for channels_list")
			return MsgChannelsList{}
		}
	}

	// 4. Command: Add Channel
	addCmd := AdminCommand{
		Cmd:   "add_channel",
		ReqID: "req-add-1",
		Name:  "Tactical",
	}
	if err := adminConn.WriteJSON(addCmd); err != nil {
		t.Fatalf("Failed to send add_channel command: %v", err)
	}

	addResp := waitForResponse("req-add-1")
	if !addResp.Ok {
		t.Errorf("Expected add_channel to succeed, but got fail. Reason: %s", addResp.Reason)
	}

	addList := waitForChannelsList()
	hasTactical := false
	for _, c := range addList.Channels {
		if c == "Tactical" {
			hasTactical = true
			break
		}
	}
	if !hasTactical {
		t.Error("Expected channels_list to contain 'Tactical' after adding it")
	}

	// 5. Command: Rename Channel
	renameCmd := AdminCommand{
		Cmd:   "rename_channel",
		ReqID: "req-rename-1",
		Old:   "Tactical",
		New:   "Ops",
	}
	if err := adminConn.WriteJSON(renameCmd); err != nil {
		t.Fatalf("Failed to send rename_channel command: %v", err)
	}

	renameResp := waitForResponse("req-rename-1")
	if !renameResp.Ok {
		t.Errorf("Expected rename_channel to succeed, but got fail. Reason: %s", renameResp.Reason)
	}

	renameList := waitForChannelsList()
	hasOps := false
	hasTactical = false
	for _, c := range renameList.Channels {
		if c == "Ops" {
			hasOps = true
		}
		if c == "Tactical" {
			hasTactical = true
		}
	}
	if !hasOps || hasTactical {
		t.Errorf("Expected channels_list to contain 'Ops' and NOT 'Tactical'. Got: ops=%t, tactical=%t", hasOps, hasTactical)
	}

	// 6. Command: Remove Channel
	removeCmd := AdminCommand{
		Cmd:   "remove_channel",
		ReqID: "req-remove-1",
		Name:  "Ops",
	}
	if err := adminConn.WriteJSON(removeCmd); err != nil {
		t.Fatalf("Failed to send remove_channel command: %v", err)
	}

	removeResp := waitForResponse("req-remove-1")
	if !removeResp.Ok {
		t.Errorf("Expected remove_channel to succeed, but got fail. Reason: %s", removeResp.Reason)
	}

	removeList := waitForChannelsList()
	hasOps = false
	for _, c := range removeList.Channels {
		if c == "Ops" {
			hasOps = true
			break
		}
	}
	if hasOps {
		t.Error("Expected channels_list to NOT contain 'Ops' after removing it")
	}

	// 7. Command Validation: Cannot delete default 'General' channel
	removeGeneralCmd := AdminCommand{
		Cmd:   "remove_channel",
		ReqID: "req-remove-general",
		Name:  "General",
	}
	if err := adminConn.WriteJSON(removeGeneralCmd); err != nil {
		t.Fatalf("Failed to send remove_channel for General: %v", err)
	}

	removeGeneralResp := waitForResponse("req-remove-general")
	if removeGeneralResp.Ok {
		t.Error("Expected remove_channel for 'General' to fail, but it succeeded")
	} else if removeGeneralResp.Reason != "Cannot delete the default channel 'General'" {
		t.Errorf("Expected specific error message, got: %s", removeGeneralResp.Reason)
	}
}
