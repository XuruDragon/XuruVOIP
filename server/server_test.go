package main

import (
	"fmt"
	"os"
	"path/filepath"
	"testing"
	"time"

	"github.com/gorilla/websocket"
)

// Helper to cleanup and close the global DB connection
func cleanupDB() {
	if db != nil {
		_ = db.Close()
		db = nil
	}
}

// TestEnsureSelfSignedCert verifies that self-signed certificates are correctly generated
func TestEnsureSelfSignedCert(t *testing.T) {
	tempDir, err := os.MkdirTemp("", "xuruvoip-test-tls-*")
	if err != nil {
		t.Fatalf("Failed to create temp dir: %v", err)
	}
	defer os.RemoveAll(tempDir)

	certPath := filepath.Join(tempDir, "cert.pem")
	keyPath := filepath.Join(tempDir, "key.pem")

	// 1. First generation should generate new files
	ok, detail := EnsureSelfSignedCert(certPath, keyPath, "test-common-name", 365)
	if !ok {
		t.Fatalf("Generation failed: %s", detail)
	}
	if detail != "generated" {
		t.Errorf("Expected detail 'generated', got '%s'", detail)
	}

	// Verify files exist
	if _, err := os.Stat(certPath); os.IsNotExist(err) {
		t.Error("Certificate file was not created")
	}
	if _, err := os.Stat(keyPath); os.IsNotExist(err) {
		t.Error("Private key file was not created")
	}

	// 2. Second run should reuse existing files
	ok, detail = EnsureSelfSignedCert(certPath, keyPath, "test-common-name", 365)
	if !ok {
		t.Fatalf("Reuse failed: %s", detail)
	}
	if detail != "existing" {
		t.Errorf("Expected detail 'existing', got '%s'", detail)
	}
}

// TestSQLiteInitAndConfig verifies database table creation and config loading
func TestSQLiteInitAndConfig(t *testing.T) {
	tempDir, err := os.MkdirTemp("", "xuruvoip-test-sqlite-*")
	if err != nil {
		t.Fatalf("Failed to create temp dir: %v", err)
	}
	defer os.RemoveAll(tempDir)

	os.Setenv("XURUVOIP_DATA_DIR", tempDir)
	defer os.Unsetenv("XURUVOIP_DATA_DIR")
	defer cleanupDB()

	// Initialize config which initializes the database
	err = LoadOrCreateConfig()
	if err != nil {
		t.Fatalf("LoadOrCreateConfig failed: %v", err)
	}

	// Verify default tables exist and can be queried
	var count int
	err = db.QueryRow("SELECT COUNT(*) FROM settings").Scan(&count)
	if err != nil {
		t.Fatalf("Failed to query settings table: %v", err)
	}
	// At least admin_token and anonymous_mode should be set in SQLite settings table
	if count < 2 {
		t.Errorf("Expected at least 2 settings, got %d", count)
	}

	// Check ServerToken is populated
	if len(serverConfig.ServerToken) != 32 {
		t.Errorf("Expected server token of length 32, got %d", len(serverConfig.ServerToken))
	}

	// Verify default channels
	if len(serverConfig.ChannelsList) != 1 || serverConfig.ChannelsList[0] != "General" {
		t.Errorf("Expected default channel 'General', got %v", serverConfig.ChannelsList)
	}

	// Check that we can change server password
	newPwd := "supersecret12345"
	err = SetServerPassword(newPwd)
	if err != nil {
		t.Fatalf("SetServerPassword failed: %v", err)
	}

	if serverConfig.ServerToken != newPwd {
		t.Errorf("Expected ServerToken to be updated, got '%s'", serverConfig.ServerToken)
	}

	// Simulating restart, reload config
	cleanupDB()
	_ = os.Unsetenv("XURUVOIP_SERVER_PASSWORD")
	_ = os.Unsetenv("XURUVOIP_ADMIN_SERVER_PASSWORD")
	_ = os.Unsetenv("XURUVOIP_PUBLIC_SERVER")
	err = LoadOrCreateConfig()
	if err != nil {
		t.Fatalf("Re-load config failed: %v", err)
	}

	if serverConfig.ServerToken != newPwd {
		t.Errorf("Expected reloaded ServerToken to match modified password, got '%s'", serverConfig.ServerToken)
	}
}

// TestJSONMigration verifies migration of legacy JSON configuration files into SQLite
func TestJSONMigration(t *testing.T) {
	tempDir, err := os.MkdirTemp("", "xuruvoip-test-migration-*")
	if err != nil {
		t.Fatalf("Failed to create temp dir: %v", err)
	}
	defer os.RemoveAll(tempDir)

	os.Setenv("XURUVOIP_DATA_DIR", tempDir)
	defer os.Unsetenv("XURUVOIP_DATA_DIR")
	defer cleanupDB()

	// Write mock JSON files
	configData := `{
		"server_token": "migrated-server-token-123",
		"admin_token": "migrated-admin-token-456"
	}`
	err = os.WriteFile(filepath.Join(tempDir, "xuruvoip_server_config.json"), []byte(configData), 0644)
	if err != nil {
		t.Fatalf("Failed to write mock config json: %v", err)
	}

	channelsData := `["General", "Command", "Logistics"]`
	err = os.WriteFile(filepath.Join(tempDir, "xuruvoip_channels.json"), []byte(channelsData), 0644)
	if err != nil {
		t.Fatalf("Failed to write mock channels json: %v", err)
	}

	profilesData := `["Pilot", "Medic"]`
	err = os.WriteFile(filepath.Join(tempDir, "xuruvoip_profiles.json"), []byte(profilesData), 0644)
	if err != nil {
		t.Fatalf("Failed to write mock profiles json: %v", err)
	}

	persistData := `{
		"OldPlayer": {
			"profile": "Pilot",
			"active_channel": "Command",
			"listening_channels": ["Logistics"]
		}
	}`
	err = os.WriteFile(filepath.Join(tempDir, "xuruvoip_persistence.json"), []byte(persistData), 0644)
	if err != nil {
		t.Fatalf("Failed to write mock persistence json: %v", err)
	}

	// Trigger load config which handles DB initialization and JSON migration
	err = LoadOrCreateConfig()
	if err != nil {
		t.Fatalf("LoadOrCreateConfig migration failed: %v", err)
	}

	// 1. Verify migrated configuration and tokens in Env
	serverToken := os.Getenv("XURUVOIP_SERVER_PASSWORD")
	if serverToken != "migrated-server-token-123" {
		t.Errorf("Expected server token 'migrated-server-token-123', got '%s'", serverToken)
	}

	// 2. Verify admin account default seed using old admin token
	authOk, err := AuthenticateAdmin("admin", "migrated-admin-token-456")
	if err != nil {
		t.Fatalf("AuthenticateAdmin failed: %v", err)
	}
	if !authOk {
		t.Error("Expected admin account to authenticate successfully with old admin token")
	}

	// 3. Verify channels list migrated
	channels, err := DBGetChannels()
	if err != nil {
		t.Fatalf("DBGetChannels failed: %v", err)
	}
	if len(channels) != 3 {
		t.Errorf("Expected 3 migrated channels, got %d: %v", len(channels), channels)
	}

	// 4. Verify profiles list migrated
	profiles, err := DBGetProfiles()
	if err != nil {
		t.Fatalf("DBGetProfiles failed: %v", err)
	}
	if len(profiles) != 2 {
		t.Errorf("Expected 2 migrated profiles, got %d: %v", len(profiles), profiles)
	}

	// 5. Verify migrated player state exists in db
	state, hasState := DBGetPlayerState("OldPlayer")
	if !hasState {
		t.Fatal("Expected migrated Player state to exist in DB")
	}
	if state.Profile != "Pilot" || state.ActiveChannel != "Command" || len(state.ListeningChannels) != 1 || state.ListeningChannels[0] != "Logistics" {
		t.Errorf("Migrated state mismatch: %+v", state)
	}

	// 6. Verify original files have been renamed to .bak
	expectedBakFiles := []string{
		"xuruvoip_server_config.json.bak",
		"xuruvoip_channels.json.bak",
		"xuruvoip_profiles.json.bak",
		"xuruvoip_persistence.json.bak",
	}
	for _, f := range expectedBakFiles {
		p := filepath.Join(tempDir, f)
		if _, err := os.Stat(p); os.IsNotExist(err) {
			t.Errorf("Expected backup file '%s' to exist on disk", f)
		}
	}
}

// TestPlayerAuthentication verifies player auto-claiming, registration and authentication checks
func TestPlayerAuthentication(t *testing.T) {
	tempDir, err := os.MkdirTemp("", "xuruvoip-test-player-auth-*")
	if err != nil {
		t.Fatalf("Failed to create temp dir: %v", err)
	}
	defer os.RemoveAll(tempDir)

	os.Setenv("XURUVOIP_DATA_DIR", tempDir)
	defer os.Unsetenv("XURUVOIP_DATA_DIR")
	defer cleanupDB()

	if err := InitDB(); err != nil {
		t.Fatalf("InitDB failed: %v", err)
	}

	// Test case 1: Auto-claiming nickname (first login)
	success, isBanned, err := AuthenticatePlayer("Bob", "bobsecretpass", "General", "127.0.0.1", "hwid-bob")
	if err != nil {
		t.Fatalf("First join auth failed: %v", err)
	}
	if !success {
		t.Error("Expected Bob's auto-registration to succeed")
	}
	if isBanned {
		t.Error("Expected Bob not to be banned")
	}

	// Test case 2: Authentication check - success
	success, isBanned, err = AuthenticatePlayer("Bob", "bobsecretpass", "General", "127.0.0.1", "hwid-bob")
	if err != nil {
		t.Fatalf("Subsequent auth failed: %v", err)
	}
	if !success {
		t.Error("Expected Bob's subsequent authentication with correct password to succeed")
	}

	// Test case 3: Authentication check - failure (wrong password)
	success, _, err = AuthenticatePlayer("Bob", "wrongpass", "General", "127.0.0.1", "hwid-bob")
	if err != nil {
		t.Fatalf("Auth failed: %v", err)
	}
	if success {
		t.Error("Expected Bob's auth with wrong password to fail")
	}

	// Test case 4: Claiming migrated player password (empty password hash)
	_, _ = db.Exec(
		"INSERT INTO users (username, password_hash, profile, active_channel, listening_channels, is_banned, hwid, last_ip, created_at, updated_at) VALUES (?, ?, ?, ?, ?, 0, '', '', ?, ?)",
		"MigratedUser", "", "Pilot", "General", "[]", time.Now(), time.Now(),
	)

	success, isBanned, err = AuthenticatePlayer("MigratedUser", "newclaimpass", "General", "127.0.0.1", "hwid-migrated")
	if err != nil {
		t.Fatalf("Migrated user claim failed: %v", err)
	}
	if !success {
		t.Error("Expected MigratedUser to successfully claim their password")
	}

	// Verify password is now set and validated properly
	success, _, err = AuthenticatePlayer("MigratedUser", "newclaimpass", "General", "127.0.0.1", "hwid-migrated")
	if err != nil || !success {
		t.Errorf("Subsequent auth for MigratedUser failed: %v", err)
	}

	success, _, err = AuthenticatePlayer("MigratedUser", "wrongpass", "General", "127.0.0.1", "hwid-migrated")
	if success {
		t.Error("Expected wrong password verification for claimed MigratedUser to fail")
	}
}

// TestPlayerBanning verifies the behavior of player banning
func TestPlayerBanning(t *testing.T) {
	tempDir, err := os.MkdirTemp("", "xuruvoip-test-player-ban-*")
	if err != nil {
		t.Fatalf("Failed to create temp dir: %v", err)
	}
	defer os.RemoveAll(tempDir)

	os.Setenv("XURUVOIP_DATA_DIR", tempDir)
	defer os.Unsetenv("XURUVOIP_DATA_DIR")
	defer cleanupDB()

	if err := InitDB(); err != nil {
		t.Fatalf("InitDB failed: %v", err)
	}

	// Register a player
	success, _, err := AuthenticatePlayer("Alice", "alicepass", "General", "192.168.1.10", "hwid-alice-pc")
	if err != nil || !success {
		t.Fatalf("Failed to register Alice: %v", err)
	}

	// Ban the player
	err = DBBanPlayer("Alice", true)
	if err != nil {
		t.Fatalf("Failed to ban Alice: %v", err)
	}

	// Verify lists contains player is_banned=true
	list, err := DBGetPlayersList()
	if err != nil {
		t.Fatalf("Failed to get players list: %v", err)
	}
	found := false
	for _, p := range list {
		if p.Username == "Alice" {
			found = true
			if !p.IsBanned {
				t.Error("Expected Alice to be marked as banned in DBGetPlayersList")
			}
			if p.LastIP != "192.168.1.10" {
				t.Errorf("Expected LastIP '192.168.1.10', got '%s'", p.LastIP)
			}
			if p.Hwid != "hwid-alice-pc" {
				t.Errorf("Expected Hwid 'hwid-alice-pc', got '%s'", p.Hwid)
			}
		}
	}
	if !found {
		t.Error("Alice was not found in players list")
	}

	// Authentication check should fail with isBanned=true
	success, isBanned, err := AuthenticatePlayer("Alice", "alicepass", "General", "192.168.1.10", "hwid-alice-pc")
	if err != nil {
		t.Fatalf("AuthenticatePlayer failed: %v", err)
	}
	if success {
		t.Error("Expected banned Alice auth to fail")
	}
	if !isBanned {
		t.Error("Expected isBanned flag to be true")
	}

	// Verify new registrations from same HWID are banned
	success, isBanned, err = AuthenticatePlayer("Mallory", "mallorypass", "General", "192.168.1.55", "hwid-alice-pc")
	if err != nil {
		t.Fatalf("AuthenticatePlayer for Mallory failed: %v", err)
	}
	if success || !isBanned {
		t.Errorf("Expected connection from banned HWID to be rejected: success=%t, banned=%t", success, isBanned)
	}

	// Verify new registrations from same IP are banned
	success, isBanned, err = AuthenticatePlayer("Eve", "evepass", "General", "192.168.1.10", "hwid-eve-pc")
	if err != nil {
		t.Fatalf("AuthenticatePlayer for Eve failed: %v", err)
	}
	if success || !isBanned {
		t.Errorf("Expected connection from banned IP to be rejected: success=%t, banned=%t", success, isBanned)
	}

	// Unban the player
	err = DBBanPlayer("Alice", false)
	if err != nil {
		t.Fatalf("Failed to unban Alice: %v", err)
	}

	// Auth should succeed again
	success, isBanned, err = AuthenticatePlayer("Alice", "alicepass", "General", "192.168.1.10", "hwid-alice-pc")
	if err != nil || !success || isBanned {
		t.Errorf("Auth failed after unbanning: success=%t, banned=%t, err=%v", success, isBanned, err)
	}

	// Mallory should succeed now since unbanning Alice also unbanned the HWID
	success, isBanned, err = AuthenticatePlayer("Mallory", "mallorypass", "General", "192.168.1.55", "hwid-alice-pc")
	if err != nil || !success || isBanned {
		t.Errorf("Mallory auth failed after Alice unbanning: success=%t, banned=%t, err=%v", success, isBanned, err)
	}
}

// TestAdminCRUD verifies administrator account CRUD operations and safety limits
func TestAdminCRUD(t *testing.T) {
	tempDir, err := os.MkdirTemp("", "xuruvoip-test-admin-*")
	if err != nil {
		t.Fatalf("Failed to create temp dir: %v", err)
	}
	defer os.RemoveAll(tempDir)

	os.Setenv("XURUVOIP_DATA_DIR", tempDir)
	defer os.Unsetenv("XURUVOIP_DATA_DIR")
	defer cleanupDB()

	if err := InitDB(); err != nil {
		t.Fatalf("InitDB failed: %v", err)
	}

	// 1. Create a second admin
	err = DBCreateAdmin("moderator", "modsecret123")
	if err != nil {
		t.Fatalf("Failed to create admin: %v", err)
	}

	// 2. Authenticate newly created admin
	ok, err := AuthenticateAdmin("moderator", "modsecret123")
	if err != nil {
		t.Fatalf("AuthenticateAdmin error: %v", err)
	}
	if !ok {
		t.Error("Expected moderator authentication to succeed")
	}

	// Fail with wrong password
	ok, _ = AuthenticateAdmin("moderator", "wrongpass")
	if ok {
		t.Error("Expected moderator auth with wrong password to fail")
	}

	// 3. Update password
	err = DBChangeAdminPassword("moderator", "newmodsecret456")
	if err != nil {
		t.Fatalf("Failed to change admin password: %v", err)
	}

	ok, _ = AuthenticateAdmin("moderator", "newmodsecret456")
	if !ok {
		t.Error("Expected authentication with updated password to succeed")
	}

	// 4. Verify admin list
	admins, err := DBGetAdminsList()
	if err != nil {
		t.Fatalf("DBGetAdminsList failed: %v", err)
	}
	// Default 'admin' + 'moderator'
	if len(admins) != 2 {
		t.Errorf("Expected 2 administrators, got %d", len(admins))
	}

	// 5. Safety: Prevent deleting default 'admin'
	err = DBDeleteAdmin("admin")
	if err == nil {
		t.Error("Expected error when attempting to delete the main 'admin' account")
	}

	// 6. Delete admin account successfully
	err = DBDeleteAdmin("moderator")
	if err != nil {
		t.Fatalf("Failed to delete admin: %v", err)
	}

	// Check they can't authenticate anymore
	ok, _ = AuthenticateAdmin("moderator", "newmodsecret456")
	if ok {
		t.Error("Expected deleted admin auth to fail")
	}

	// 7. Safety: Prevent deleting last administrator
	// Current is only 'admin'. Let's try to delete a custom admin we create
	err = DBCreateAdmin("lastadmin", "pass")
	if err != nil {
		t.Fatalf("Failed to create lastadmin: %v", err)
	}
	// Delete main admin (should fail due to default name check)
	err = DBDeleteAdmin("admin")
	if err == nil {
		t.Error("Expected error when deleting 'admin'")
	}
	// Delete 'admin' by changing its name check? No, the code checks name "admin" explicitly.
	// Let's delete the default admin if it's possible... wait, DBDeleteAdmin blocks deletion of "admin" explicitly.
	// But what if we try to delete 'lastadmin'? There are 2 admins now (admin and lastadmin). It should succeed.
	err = DBDeleteAdmin("lastadmin")
	if err != nil {
		t.Fatalf("Expected deleting lastadmin to succeed: %v", err)
	}

	// Now try to delete "admin" when it's the only one left.
	err = DBDeleteAdmin("admin")
	if err == nil {
		t.Error("Expected error trying to delete only remaining admin 'admin'")
	}
}

// TestPersistence verifies saving and loading player configs cross-sessions using SQLite functions
func TestPersistence(t *testing.T) {
	tempDir, err := os.MkdirTemp("", "xuruvoip-test-persist-*")
	if err != nil {
		t.Fatalf("Failed to create temp dir: %v", err)
	}
	defer os.RemoveAll(tempDir)

	os.Setenv("XURUVOIP_DATA_DIR", tempDir)
	defer os.Unsetenv("XURUVOIP_DATA_DIR")
	defer cleanupDB()

	err = InitDB()
	if err != nil {
		t.Fatalf("InitDB failed: %v", err)
	}

	name := "XuruPlayer"
	profile := "SquadLeader"
	activeCh := "Alpha"
	listenChs := []string{"Beta", "Gamma"}

	// Must register player before saving their state (due to foreign constraints or SQLite row existence)
	_, _, err = AuthenticatePlayer(name, "playerpassword", "Alpha", "127.0.0.1", "hwid-xuru")
	if err != nil {
		t.Fatalf("AuthenticatePlayer registration failed: %v", err)
	}

	// 1. Save state for player
	err = DBSavePlayerState(name, profile, activeCh, listenChs)
	if err != nil {
		t.Fatalf("DBSavePlayerState failed: %v", err)
	}

	// 2. Retrieve state
	state, exists := DBGetPlayerState(name)
	if !exists {
		t.Fatal("Expected player persistence state to exist")
	}

	if state.Profile != profile {
		t.Errorf("Expected profile '%s', got '%s'", profile, state.Profile)
	}
	if state.ActiveChannel != activeCh {
		t.Errorf("Expected active channel '%s', got '%s'", activeCh, state.ActiveChannel)
	}
	if len(state.ListeningChannels) != 2 || state.ListeningChannels[0] != "Beta" || state.ListeningChannels[1] != "Gamma" {
		t.Errorf("Expected listening channels %v, got %v", listenChs, state.ListeningChannels)
	}

	// 3. Re-initialize database (simulate server restart) and verify it reloads
	cleanupDB()
	err = InitDB()
	if err != nil {
		t.Fatalf("Re-init database failed: %v", err)
	}

	stateReloaded, existsReloaded := DBGetPlayerState(name)
	if !existsReloaded {
		t.Fatal("Expected player persistence state to exist after restart simulation")
	}

	if stateReloaded.Profile != profile {
		t.Errorf("Reloaded: Expected profile '%s', got '%s'", profile, stateReloaded.Profile)
	}
	if stateReloaded.ActiveChannel != activeCh {
		t.Errorf("Reloaded: Expected active channel '%s', got '%s'", activeCh, stateReloaded.ActiveChannel)
	}
}

// TestProximityFiltering verifies server-side 3D distance and container proximity calculations
func TestProximityFiltering(t *testing.T) {
	// Initialize standard Hub state
	testHub := Hub{
		players: make(map[string]*ActivePlayer),
		admins:  make(map[*websocket.Conn]*AdminSession),
	}

	// Setup mock players
	// Player A at Lorville City Center (Container 1)
	testHub.players["Alice"] = &ActivePlayer{
		Name: "Alice",
		Pos: &Position{
			X:           100.0,
			Y:           200.0,
			Z:           300.0,
			ContainerID: "lorville_city_center",
		},
		AudioConn: &websocket.Conn{}, // dummy non-nil conn
	}

	// Player B at 10m away from A in same container (within audible range)
	testHub.players["Bob"] = &ActivePlayer{
		Name: "Bob",
		Pos: &Position{
			X:           106.0,
			Y:           208.0,
			Z:           300.0, // distance = sqrt(6^2 + 8^2 + 0^2) = 10m
			ContainerID: "lorville_city_center",
		},
		AudioConn: &websocket.Conn{},
	}

	// Player C at 60m away from A in same container (outside default 50m range)
	testHub.players["Charlie"] = &ActivePlayer{
		Name: "Charlie",
		Pos: &Position{
			X:           160.0,
			Y:           200.0,
			Z:           300.0, // distance = 60m
			ContainerID: "lorville_city_center",
		},
		AudioConn: &websocket.Conn{},
	}

	// Player D at 5m away from A but in a DIFFERENT container (e.g. inside an elevator)
	testHub.players["David"] = &ActivePlayer{
		Name: "David",
		Pos: &Position{
			X:           103.0,
			Y:           204.0,
			Z:           300.0, // distance = 5m
			ContainerID: "elevator_cab_01",
		},
		AudioConn: &websocket.Conn{},
	}

	// 1. Proximity range audit for Alice (default 50m range)
	// Alice should hear Bob (10m, same container)
	// Alice should NOT hear Charlie (60m, same container)
	// Alice should NOT hear David (5m, different container)
	players := testHub.GetAudioPlayersInProximity("Alice")
	if len(players) != 1 {
		t.Fatalf("Expected 1 player in proximity, got %d", len(players))
	}
	if players[0] != testHub.players["Bob"] {
		t.Error("Expected Bob to be audible to Alice")
	}

	// 2. Proximity range audit with ProxShort active
	// Enable whisper mode (ProxShort=true) on Alice. Audible range drops to 5m.
	// Bob (10m) is now outside of audible range.
	testHub.players["Alice"].ProxShort = true
	players = testHub.GetAudioPlayersInProximity("Alice")
	if len(players) != 0 {
		t.Errorf("Expected 0 players in proximity under whisper mode, got %d", len(players))
	}
}

// BenchmarkProximityFiltering benchmarks GetAudioConnectionsInProximity under heavy player loads
func BenchmarkProximityFiltering(b *testing.B) {
	// Create mock player hubs for different loads
	loads := []int{10, 50, 100, 500, 1000}

	for _, count := range loads {
		b.Run(fmt.Sprintf("Players-%d", count), func(b *testing.B) {
			testHub := Hub{
				players: make(map[string]*ActivePlayer),
				admins:  make(map[*websocket.Conn]*AdminSession),
			}

			// Generate sender
			senderName := "Alice"
			testHub.players[senderName] = &ActivePlayer{
				Name: senderName,
				Pos: &Position{
					X:           0.0,
					Y:           0.0,
					Z:           0.0,
					ContainerID: "stanton_stanton",
				},
				AudioConn: &websocket.Conn{},
			}

			// Generate other players
			for i := 0; i < count; i++ {
				name := fmt.Sprintf("Player_%d", i)

				// Alternate containers to make it realistic
				containerID := "stanton_stanton"
				if i%2 == 0 {
					containerID = "microtech_interior"
				}

				// Place them at random distances
				dist := float64(i % 50) // distances from 0 to 49 meters

				testHub.players[name] = &ActivePlayer{
					Name: name,
					Pos: &Position{
						X:           dist,
						Y:           0.0,
						Z:           0.0,
						ContainerID: containerID,
					},
					AudioConn: &websocket.Conn{},
				}
			}

			b.ResetTimer()
			for i := 0; i < b.N; i++ {
				_ = testHub.GetAudioPlayersInProximity(senderName)
			}
		})
	}
}

// TestMultiChannelRouting verifies that players receive audio when they listen to the sender's active channel
func TestMultiChannelRouting(t *testing.T) {
	testHub := Hub{
		players: make(map[string]*ActivePlayer),
		admins:  make(map[*websocket.Conn]*AdminSession),
	}

	// Alice speaking on Command
	testHub.players["Alice"] = &ActivePlayer{
		Name:          "Alice",
		ActiveChannel: "Command",
		AudioConn:     &websocket.Conn{},
	}

	// Bob is active on Command
	testHub.players["Bob"] = &ActivePlayer{
		Name:          "Bob",
		ActiveChannel: "Command",
		AudioConn:     &websocket.Conn{},
	}

	// Charlie is active on General, but listens to Command
	testHub.players["Charlie"] = &ActivePlayer{
		Name:              "Charlie",
		ActiveChannel:     "General",
		ListeningChannels: []string{"Command", "Squad"},
		AudioConn:         &websocket.Conn{},
	}

	// David is active on General, listens to nothing
	testHub.players["David"] = &ActivePlayer{
		Name:          "David",
		ActiveChannel: "General",
		AudioConn:     &websocket.Conn{},
	}

	players := testHub.GetAudioPlayersInRadioChannel("Alice")
	// Should contain Bob and Charlie (2 players)
	if len(players) != 2 {
		t.Fatalf("Expected 2 radio audio players, got %d", len(players))
	}

	hasBob := false
	hasCharlie := false
	for _, p := range players {
		if p == testHub.players["Bob"] {
			hasBob = true
		}
		if p == testHub.players["Charlie"] {
			hasCharlie = true
		}
	}

	if !hasBob {
		t.Error("Expected Bob (on same active channel) to receive Alice's audio")
	}
	if !hasCharlie {
		t.Error("Expected Charlie (listening to Command) to receive Alice's audio")
	}
}

// TestWebAdminSessions verifies admin session lifetime and token validation
func TestWebAdminSessions(t *testing.T) {
	// Clear sessions
	webSessionsMu.Lock()
	webSessions = make(map[string]AdminWebSession)
	webSessionsMu.Unlock()

	// 1. Create a session
	token := CreateSession()
	if token == "" {
		t.Fatal("Expected non-empty session token")
	}

	// 2. Validate session
	if !ValidateSession(token) {
		t.Error("Expected session to be valid immediately after creation")
	}

	// 3. Make session expired and sweep
	webSessionsMu.Lock()
	sess := webSessions[token]
	sess.ExpiresAt = time.Now().Add(-1 * time.Second) // expired
	webSessions[token] = sess
	webSessionsMu.Unlock()

	// Validate should fail
	if ValidateSession(token) {
		t.Error("Expected session to be invalid after expiration")
	}

	// Sweep
	CleanupExpiredSessions()

	webSessionsMu.RLock()
	_, exists := webSessions[token]
	webSessionsMu.RUnlock()

	if exists {
		t.Error("Expected expired session to be deleted by CleanupExpiredSessions")
	}
}

// TestIPAndHwidBanning verifies manual IP and HWID bans
func TestIPAndHwidBanning(t *testing.T) {
	tempDir, err := os.MkdirTemp("", "xuruvoip-test-ip-hwid-ban-*")
	if err != nil {
		t.Fatalf("Failed to create temp dir: %v", err)
	}
	defer os.RemoveAll(tempDir)

	os.Setenv("XURUVOIP_DATA_DIR", tempDir)
	defer os.Unsetenv("XURUVOIP_DATA_DIR")
	defer cleanupDB()

	if err := InitDB(); err != nil {
		t.Fatalf("InitDB failed: %v", err)
	}

	// 1. Manually ban IP 10.0.0.5
	err = DBAddBannedIP("10.0.0.5:54321", "Spammer IP")
	if err != nil {
		t.Fatalf("Failed to add banned IP: %v", err)
	}

	// Check IP list
	ipList, err := DBGetBannedIPsList()
	if err != nil || len(ipList) != 1 || ipList[0].IP != "10.0.0.5" {
		t.Errorf("IP list mismatch: %v", ipList)
	}

	// Try to connect from 10.0.0.5
	success, isBanned, err := AuthenticatePlayer("George", "gpass", "General", "10.0.0.5:9999", "hwid-george")
	if success || !isBanned {
		t.Errorf("Expected join from banned IP to fail: success=%t, banned=%t", success, isBanned)
	}

	// Remove IP ban
	err = DBRemoveBannedIP("10.0.0.5")
	if err != nil {
		t.Fatalf("Failed to remove banned IP: %v", err)
	}

	// Should succeed now
	success, isBanned, err = AuthenticatePlayer("George", "gpass", "General", "10.0.0.5:9999", "hwid-george")
	if !success || isBanned {
		t.Errorf("Expected join to succeed after unbanning IP: success=%t, banned=%t", success, isBanned)
	}

	// 2. Manually ban HWID "hwid-hacker"
	err = DBAddBannedHwid("hwid-hacker", "Hacked client detected")
	if err != nil {
		t.Fatalf("Failed to add banned HWID: %v", err)
	}

	// Check HWID list
	hwidList, err := DBGetBannedHwidsList()
	if err != nil || len(hwidList) != 1 || hwidList[0].Hwid != "hwid-hacker" {
		t.Errorf("HWID list mismatch: %v", hwidList)
	}

	// Try to connect with banned HWID
	success, isBanned, err = AuthenticatePlayer("Harry", "hpass", "General", "127.0.0.1", "hwid-hacker")
	if success || !isBanned {
		t.Errorf("Expected join from banned HWID to fail: success=%t, banned=%t", success, isBanned)
	}

	// Remove HWID ban
	err = DBRemoveBannedHwid("hwid-hacker")
	if err != nil {
		t.Fatalf("Failed to remove banned HWID: %v", err)
	}

	// Should succeed now
	success, isBanned, err = AuthenticatePlayer("Harry", "hpass", "General", "127.0.0.1", "hwid-hacker")
	if !success || isBanned {
		t.Errorf("Expected join to succeed after unbanning HWID: success=%t, banned=%t", success, isBanned)
	}
}

// TestServerPasswordModes verifies that server password checking behavior (private vs public modes)
func TestServerPasswordModes(t *testing.T) {
	tempDir, err := os.MkdirTemp("", "xuruvoip-test-password-mode-*")
	if err != nil {
		t.Fatalf("Failed to create temp dir: %v", err)
	}
	defer os.RemoveAll(tempDir)

	os.Setenv("XURUVOIP_DATA_DIR", tempDir)
	defer os.Unsetenv("XURUVOIP_DATA_DIR")
	defer cleanupDB()

	// Force reload from .env
	_ = os.Unsetenv("XURUVOIP_SERVER_PASSWORD")
	_ = os.Unsetenv("XURUVOIP_ADMIN_SERVER_PASSWORD")
	_ = os.Unsetenv("XURUVOIP_PUBLIC_SERVER")

	err = LoadOrCreateConfig()
	if err != nil {
		t.Fatalf("LoadOrCreateConfig failed: %v", err)
	}

	token := serverConfig.ServerToken
	if len(token) != 32 {
		t.Errorf("Expected server token to be generated (length 32), got '%s'", token)
	}

	// 1. Verify SetServerPassword updates config
	err = SetServerPassword("my-secret-server-pwd")
	if err != nil {
		t.Fatalf("SetServerPassword failed: %v", err)
	}
	if serverConfig.ServerToken != "my-secret-server-pwd" {
		t.Errorf("Expected ServerToken to be updated, got '%s'", serverConfig.ServerToken)
	}

	// 2. Test reload
	cleanupDB()
	_ = os.Unsetenv("XURUVOIP_SERVER_PASSWORD")
	_ = os.Unsetenv("XURUVOIP_ADMIN_SERVER_PASSWORD")
	_ = os.Unsetenv("XURUVOIP_PUBLIC_SERVER")
	err = LoadOrCreateConfig()
	if err != nil {
		t.Fatalf("LoadOrCreateConfig reload failed: %v", err)
	}
	if serverConfig.ServerToken != "my-secret-server-pwd" {
		t.Errorf("Expected reloaded ServerToken to match modified password, got '%s'", serverConfig.ServerToken)
	}

	// 3. Test PublicServer mode loading
	_ = os.Setenv("XURUVOIP_PUBLIC_SERVER", "1")
	err = LoadOrCreateConfig()
	if err != nil {
		t.Fatalf("LoadOrCreateConfig failed: %v", err)
	}
	if !PublicServer {
		t.Error("Expected PublicServer to be true when XURUVOIP_PUBLIC_SERVER=1")
	}

	// 4. Verify that empty user password is always rejected
	ok, _, err := AuthenticatePlayer("TestEmptyUser", "", "General", "127.0.0.1", "hwid-empty")
	if err != nil {
		t.Fatalf("AuthenticatePlayer error: %v", err)
	}
	if ok {
		t.Error("Expected AuthenticatePlayer to reject empty user password")
	}

	// Same for whitespace-only passwords
	ok, _, err = AuthenticatePlayer("TestEmptyUser", "   ", "General", "127.0.0.1", "hwid-empty")
	if err != nil {
		t.Fatalf("AuthenticatePlayer error: %v", err)
	}
	if ok {
		t.Error("Expected AuthenticatePlayer to reject whitespace user password")
	}
}
