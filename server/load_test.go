package main

import (
	"crypto/tls"
	"encoding/json"
	"fmt"
	"math"
	"os"
	"path/filepath"
	"sync"
	"sync/atomic"
	"testing"
	"time"

	"github.com/gorilla/websocket"
)

func TestServerLoadSimulation(t *testing.T) {
	// 1. Setup temporary database environment
	tempDir, err := os.MkdirTemp("", "xuruvoip-loadtest-*")
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

	// Set rate limiters extremely high for load tests so mock clients are not throttled
	t.Setenv("XURUVOIP_LIMIT_RATE_POS", "1000.0")
	t.Setenv("XURUVOIP_LIMIT_BURST_POS", "2000")
	t.Setenv("XURUVOIP_LIMIT_RATE_AUDIO", "1000.0")
	t.Setenv("XURUVOIP_LIMIT_BURST_AUDIO", "2000")

	// Initialize database and logger
	if err := LoadOrCreateConfig(); err != nil {
		t.Fatalf("Failed to load or create config: %v", err)
	}
	InitSecurityManagers()
	defer CloseLogger()

	// Ensure certificates exist in temp directory
	certPath := filepath.Join(tempDir, "cert.pem")
	keyPath := filepath.Join(tempDir, "key.pem")
	ok, _ := EnsureSelfSignedCert(certPath, keyPath, "xuruvoip-server", 365)
	if !ok {
		t.Fatal("Failed to generate test certificates")
	}

	// 2. Start servers in background on test ports
	posPort := 18888
	audioPort := 18889

	go StartPositionsServer(posPort, certPath, keyPath)
	go StartAudioServer(audioPort, certPath, keyPath)

	// Allow servers time to spin up
	time.Sleep(300 * time.Millisecond)

	// 3. Spawning mock clients
	numClients := 20 // 20 clients is plenty to test concurrency, message routing, and load simulation
	var wg sync.WaitGroup

	posURL := fmt.Sprintf("wss://localhost:%d/", posPort)
	audioURL := fmt.Sprintf("wss://localhost:%d/audio", audioPort)

	dialer := websocket.Dialer{
		TLSClientConfig: &tls.Config{InsecureSkipVerify: true},
	}

	var connectedPos int64
	var connectedAudio int64
	var totalMessagesReceived int64
	var totalAudioReceived int64

	// Start simulating clients
	for i := 0; i < numClients; i++ {
		wg.Add(1)
		go func(id int) {
			defer wg.Done()

			clientName := fmt.Sprintf("MockPlayer_%d", id)
			clientPassword := "playerpass"

			// 3.1 Connect to Positions Server
			posConn, _, err := dialer.Dial(posURL, nil)
			if err != nil {
				t.Logf("[%s] Position WebSocket dial failed: %v", clientName, err)
				return
			}
			defer posConn.Close()

			atomic.AddInt64(&connectedPos, 1)

			// Authenticate on Position Server
			joinMsg := MsgJoin{
				Type:     "join",
				Token:    "testsecret32characterlongtokenok",
				Name:     clientName,
				Password: clientPassword,
				Channel:  "General",
			}
			if err := posConn.WriteJSON(joinMsg); err != nil {
				t.Logf("[%s] MsgJoin write failed: %v", clientName, err)
				return
			}

			// Read Welcome response
			var welcome MsgWelcome
			_, welcomePayload, err := posConn.ReadMessage()
			if err != nil {
				t.Logf("[%s] Failed to read MsgWelcome: %v", clientName, err)
				return
			}
			if err := json.Unmarshal(welcomePayload, &welcome); err != nil {
				t.Logf("[%s] Failed to unmarshal MsgWelcome: %v", clientName, err)
				return
			}

			audioTicket := welcome.AudioTicket

			// 3.2 Connect to Audio Server
			audioConn, _, err := dialer.Dial(audioURL, nil)
			if err != nil {
				t.Logf("[%s] Audio WebSocket dial failed: %v", clientName, err)
				return
			}
			defer audioConn.Close()

			atomic.AddInt64(&connectedAudio, 1)

			// Authenticate on Audio Server using the ticket
			audioJoinMsg := MsgJoin{
				Type:        "join",
				Token:       "testsecret32characterlongtokenok",
				Name:        clientName,
				AudioTicket: audioTicket,
			}
			if err := audioConn.WriteJSON(audioJoinMsg); err != nil {
				t.Logf("[%s] Audio MsgJoin write failed: %v", clientName, err)
				return
			}

			// Spawn reader goroutine for Position Server
			go func() {
				for {
					_, _, err := posConn.ReadMessage()
					if err != nil {
						break
					}
					atomic.AddInt64(&totalMessagesReceived, 1)
				}
			}()

			// Spawn reader goroutine for Audio Server
			go func() {
				for {
					_, _, err := audioConn.ReadMessage()
					if err != nil {
						break
					}
					atomic.AddInt64(&totalAudioReceived, 1)
				}
			}()

			// Send periodic updates
			ticker := time.NewTicker(50 * time.Millisecond)
			defer ticker.Stop()

			// Run simulation for 1.5 seconds
			stopTime := time.Now().Add(1500 * time.Millisecond)
			step := 0.0

			for time.Now().Before(stopTime) {
				<-ticker.C
				step += 0.1

				// 1. Send simulated coordinate update (moving in a circle)
				x := 100.0 + 10.0*math.Cos(step+float64(id))
				y := 100.0 + 10.0*math.Sin(step+float64(id))
				z := 0.0

				posMsg := MsgPos{
					Type: "pos",
					Pos: Position{
						X:    x,
						Y:    y,
						Z:    z,
						Zone: "Stanton",
					},
					TsCapture: float64(time.Now().UnixNano()) / 1e9,
				}
				_ = posConn.WriteJSON(posMsg)

				// 2. Send simulated talking audio frame (25% chance of talking each tick)
				if id%4 == 0 {
					audioFrame := make([]byte, 21) // [Type (1)] + [Dummy voice data (20)]
					audioFrame[0] = AudioTypeProximity
					for idx := 1; idx < len(audioFrame); idx++ {
						audioFrame[idx] = byte(id + idx)
					}
					_ = audioConn.WriteMessage(websocket.BinaryMessage, audioFrame)
				}
			}
		}(i)
	}

	wg.Wait()

	// 4. Assertions on Load test outcomes
	t.Logf("Load Simulation Completed:")
	t.Logf("  Clients Spawned: %d", numClients)
	t.Logf("  Connected to Position: %d", connectedPos)
	t.Logf("  Connected to Audio: %d", connectedAudio)
	t.Logf("  Total Pos updates received: %d", atomic.LoadInt64(&totalMessagesReceived))
	t.Logf("  Total Audio frames routed/received: %d", atomic.LoadInt64(&totalAudioReceived))

	if connectedPos < int64(numClients) {
		t.Errorf("Expected all %d clients to connect to position server, but only %d succeeded", numClients, connectedPos)
	}
	if connectedAudio < int64(numClients) {
		t.Errorf("Expected all %d clients to connect to audio server, but only %d succeeded", numClients, connectedAudio)
	}
	if atomic.LoadInt64(&totalMessagesReceived) == 0 {
		t.Error("Expected to receive broadcast position updates, but received 0")
	}
	if atomic.LoadInt64(&totalAudioReceived) == 0 {
		t.Error("Expected to receive routed audio frames, but received 0")
	}
}
