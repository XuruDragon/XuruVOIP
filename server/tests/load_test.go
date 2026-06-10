package tests

import (
	"crypto/tls"
	"encoding/binary"
	"encoding/json"
	"fmt"
	"math"
	"net"
	"os"
	"path/filepath"
	"sync"
	"sync/atomic"
	"testing"
	"time"

	"github.com/gorilla/websocket"

	"xuruvoip/server/voip/audio"
	"xuruvoip/server/voip/core"
	"xuruvoip/server/voip/position"
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
	if err := core.LoadOrCreateConfig(); err != nil {
		t.Fatalf("Failed to load or create config: %v", err)
	}
	core.InitSecurityManagers()
	defer core.CloseLogger()

	// Ensure certificates exist in temp directory
	certPath := filepath.Join(tempDir, "cert.pem")
	keyPath := filepath.Join(tempDir, "key.pem")
	ok, _ := core.EnsureSelfSignedCert(certPath, keyPath, "xuruvoip-server", 365)
	if !ok {
		t.Fatal("Failed to generate test certificates")
	}

	// 2. Start servers in background on test ports
	posPort := 18888
	audioPort := 18889

	go position.StartPositionsServer(posPort, certPath, keyPath)
	go audio.StartAudioServer(audioPort, certPath, keyPath)

	// Allow servers time to spin up
	time.Sleep(300 * time.Millisecond)

	// 3. Spawning mock clients
	numClients := 20
	var wg sync.WaitGroup

	posURL := fmt.Sprintf("wss://127.0.0.1:%d/", posPort)

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
			joinMsg := core.MsgJoin{
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
			var welcome core.MsgWelcome
			for {
				_, payload, err := posConn.ReadMessage()
				if err != nil {
					t.Logf("[%s] Failed to read from Position WebSocket: %v", clientName, err)
					return
				}
				var base core.MessageBase
				if err := json.Unmarshal(payload, &base); err == nil && base.Type == "welcome" {
					if err := json.Unmarshal(payload, &welcome); err != nil {
						t.Logf("[%s] Failed to unmarshal MsgWelcome: %v", clientName, err)
						return
					}
					break
				}
			}
			audioTicket := welcome.AudioTicket

			// 3.2 Connect to Audio Server (UDP)
			serverUDPAddr, err := net.ResolveUDPAddr("udp", fmt.Sprintf("127.0.0.1:%d", audioPort))
			if err != nil {
				t.Logf("[%s] UDP ResolveAddr failed: %v", clientName, err)
				return
			}

			audioConn, err := net.DialUDP("udp", nil, serverUDPAddr)
			if err != nil {
				t.Logf("[%s] UDP Dial failed: %v", clientName, err)
				return
			}
			defer audioConn.Close()

			// UDP Registration: [0xFF] [NameLen] [Name] [AudioTicket]
			nameBytes := []byte(clientName)
			nameLen := len(nameBytes)
			regPacket := make([]byte, 2+nameLen+32)
			regPacket[0] = 0xFF
			regPacket[1] = byte(nameLen)
			copy(regPacket[2:], nameBytes)
			copy(regPacket[2+nameLen:], []byte(audioTicket))

			_, err = audioConn.Write(regPacket)
			if err != nil {
				t.Logf("[%s] UDP Reg write failed: %v", clientName, err)
				return
			}

			// Read ACK: 0xFE
			_ = audioConn.SetReadDeadline(time.Now().Add(500 * time.Millisecond))
			ackBuf := make([]byte, 1)
			n, err := audioConn.Read(ackBuf)
			if err != nil || n < 1 || ackBuf[0] != 0xFE {
				t.Logf("[%s] UDP ACK read failed: %v", clientName, err)
				return
			}
			_ = audioConn.SetReadDeadline(time.Time{})

			atomic.AddInt64(&connectedAudio, 1)

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

			// Spawn reader goroutine for Audio Server (UDP)
			go func() {
				buf := make([]byte, 1500)
				for {
					n, err := audioConn.Read(buf)
					if err != nil {
						break
					}
					if n >= 3 {
						atomic.AddInt64(&totalAudioReceived, 1)
					}
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

				posMsg := core.MsgPos{
					Type: "pos",
					Pos: core.Position{
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
					audioFrame := make([]byte, 23) // [Seq (2)] [Type (1)] [Dummy voice data (20)]
					binary.BigEndian.PutUint16(audioFrame[0:2], uint16(step*10))
					audioFrame[2] = core.AudioTypeProximity
					for idx := 3; idx < len(audioFrame); idx++ {
						audioFrame[idx] = byte(id + idx)
					}
					_, _ = audioConn.Write(audioFrame)
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
