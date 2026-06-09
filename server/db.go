package main

import (
	"database/sql"
	"encoding/json"
	"errors"
	"fmt"
	"os"
	"path/filepath"
	"strings"
	"time"

	_ "modernc.org/sqlite"
	"golang.org/x/crypto/bcrypt"
)

var (
	db *sql.DB
)

// InitDB initializes the SQLite database, runs migrations, and imports old JSON configurations
func InitDB() error {
	dataDir := ResolveDataDir()
	dbPath := filepath.Join(dataDir, "xuruvoip.db")

	var err error
	db, err = sql.Open("sqlite", dbPath)
	if err != nil {
		return fmt.Errorf("failed to open database: %w", err)
	}

	// Set connection limits for SQLite concurrency
	db.SetMaxOpenConns(1)

	// Create tables
	if err := createTables(); err != nil {
		return err
	}

	// Run migrations (add hwid and last_ip columns to users table if they do not exist)
	rows, err := db.Query("PRAGMA table_info(users)")
	if err == nil {
		hasHwid := false
		hasLastIP := false
		for rows.Next() {
			var cid int
			var name, dtype string
			var notnull, pk int
			var dfltVal interface{}
			if err := rows.Scan(&cid, &name, &dtype, &notnull, &dfltVal, &pk); err == nil {
				if name == "hwid" {
					hasHwid = true
				}
				if name == "last_ip" {
					hasLastIP = true
				}
			}
		}
		rows.Close()
		if !hasHwid {
			_, _ = db.Exec("ALTER TABLE users ADD COLUMN hwid TEXT DEFAULT ''")
		}
		if !hasLastIP {
			_, _ = db.Exec("ALTER TABLE users ADD COLUMN last_ip TEXT DEFAULT ''")
		}
	}

	// Migrate old JSON configurations if database is empty
	_ = migrateJSONToDB()

	return nil
}

func createTables() error {
	queries := []string{
		`CREATE TABLE IF NOT EXISTS users (
			username TEXT PRIMARY KEY,
			password_hash TEXT NOT NULL,
			profile TEXT DEFAULT '',
			active_channel TEXT DEFAULT 'General',
			listening_channels TEXT DEFAULT '[]',
			is_banned INTEGER DEFAULT 0,
			hwid TEXT DEFAULT '',
			last_ip TEXT DEFAULT '',
			created_at DATETIME NOT NULL,
			updated_at DATETIME NOT NULL
		);`,
		`CREATE TABLE IF NOT EXISTS admins (
			username TEXT PRIMARY KEY,
			password_hash TEXT NOT NULL,
			created_at DATETIME NOT NULL,
			updated_at DATETIME NOT NULL
		);`,
		`CREATE TABLE IF NOT EXISTS channels (
			name TEXT PRIMARY KEY
		);`,
		`CREATE TABLE IF NOT EXISTS profiles (
			name TEXT PRIMARY KEY
		);`,
		`CREATE TABLE IF NOT EXISTS settings (
			key TEXT PRIMARY KEY,
			value TEXT NOT NULL
		);`,
		`CREATE TABLE IF NOT EXISTS banned_ips (
			ip TEXT PRIMARY KEY,
			reason TEXT DEFAULT '',
			created_at DATETIME NOT NULL
		);`,
		`CREATE TABLE IF NOT EXISTS banned_hwids (
			hwid TEXT PRIMARY KEY,
			reason TEXT DEFAULT '',
			created_at DATETIME NOT NULL
		);`,
	}

	for _, q := range queries {
		if _, err := db.Exec(q); err != nil {
			return fmt.Errorf("failed to execute query %q: %w", q, err)
		}
	}
	return nil
}

// migrateJSONToDB loads old JSON data into SQLite on first boot
func migrateJSONToDB() error {
	dataDir := ResolveDataDir()

	// Check if we need to migrate settings (empty db check)
	var count int
	err := db.QueryRow("SELECT COUNT(*) FROM settings").Scan(&count)
	if err != nil {
		return err
	}

	if count > 0 {
		// Already migrated/initialized
		return nil
	}

	// 1. Migrate settings from config file
	configPath := filepath.Join(dataDir, "xuruvoip_server_config.json")
	var oldServerToken string
	var oldAdminToken string

	if _, err := os.Stat(configPath); err == nil {
		data, err := os.ReadFile(configPath)
		if err == nil {
			var config struct {
				ServerToken string   `json:"server_token"`
				AdminToken  string   `json:"admin_token"`
				Channels    []string `json:"channels,omitempty"`
			}
			if err := json.Unmarshal(data, &config); err == nil {
				oldServerToken = config.ServerToken
				oldAdminToken = config.AdminToken
			}
		}
	}

	// Check secondary token file if config didn't have it
	adminTokenPath := filepath.Join(dataDir, "xuruvoip_admin_token.json")
	if oldAdminToken == "" {
		if _, err := os.Stat(adminTokenPath); err == nil {
			data, err := os.ReadFile(adminTokenPath)
			if err == nil {
				var tokenMap map[string]string
				if err := json.Unmarshal(data, &tokenMap); err == nil {
					oldAdminToken = tokenMap["admin_token"]
				}
			}
		}
	}

	// Fallbacks if files not found
	if oldServerToken == "" {
		oldServerToken = os.Getenv("XURUVOIP_SERVER_PASSWORD")
		if oldServerToken == "" {
			oldServerToken = GenerateRandomString(32)
		}
	}
	if oldAdminToken == "" {
		adminPassword := GenerateRandomString(12)
		oldAdminToken = adminPassword
		fmt.Println()
		fmt.Println("==================================================================")
		fmt.Println("   MAIN ADMINISTRATOR ACCOUNT CREATED                             ")
		fmt.Println("   Username : admin                                               ")
		fmt.Printf("   Password : %s\n", adminPassword)
		fmt.Println("   NOTE: This password is saved in the database.                  ")
		fmt.Println("         Please write it down or change it via the admin dashboard. ")
		fmt.Println("==================================================================")
		fmt.Println()
	}

	// Save settings
	if oldServerToken != "" {
		_ = os.Setenv("XURUVOIP_SERVER_PASSWORD", oldServerToken)
		UpdateEnvFile("XURUVOIP_SERVER_PASSWORD", oldServerToken)
	}
	_ = DBSetSetting("admin_token", oldAdminToken)
	_ = DBSetSetting("anonymous_mode", "false")

	// 2. Migrate Admins (Seed default account)
	hashedAdmin, err := bcrypt.GenerateFromPassword([]byte(oldAdminToken), bcrypt.DefaultCost)
	if err == nil {
		_, _ = db.Exec(
			"INSERT OR IGNORE INTO admins (username, password_hash, created_at, updated_at) VALUES (?, ?, ?, ?)",
			"admin", string(hashedAdmin), time.Now(), time.Now(),
		)
		Log("Administrator account 'admin' initialized successfully.", ColorGreen)
	}

	// 3. Migrate Channels
	channelsPath := filepath.Join(dataDir, "xuruvoip_channels.json")
	var channels []string
	if _, err := os.Stat(channelsPath); err == nil {
		data, err := os.ReadFile(channelsPath)
		if err == nil {
			_ = json.Unmarshal(data, &channels)
		}
	}
	if len(channels) == 0 {
		channels = []string{"General"}
	}
	for _, c := range channels {
		_, _ = db.Exec("INSERT OR IGNORE INTO channels (name) VALUES (?)", c)
	}

	// 4. Migrate Profiles
	profilesPath := filepath.Join(dataDir, "xuruvoip_profiles.json")
	var profiles []string
	if _, err := os.Stat(profilesPath); err == nil {
		data, err := os.ReadFile(profilesPath)
		if err == nil {
			_ = json.Unmarshal(data, &profiles)
		}
	}
	for _, p := range profiles {
		_, _ = db.Exec("INSERT OR IGNORE INTO profiles (name) VALUES (?)", p)
	}

	// 5. Migrate Player States
	persistPath := filepath.Join(dataDir, "xuruvoip_persistence.json")
	if _, err := os.Stat(persistPath); err == nil {
		data, err := os.ReadFile(persistPath)
		if err == nil {
			var oldMap map[string]struct {
				Profile           string   `json:"profile"`
				ActiveChannel     string   `json:"active_channel"`
				ListeningChannels []string `json:"listening_channels"`
			}
			if err := json.Unmarshal(data, &oldMap); err == nil {
				for name, pState := range oldMap {
					listenJSON, _ := json.Marshal(pState.ListeningChannels)
					// Players from old persistence start with empty password hash (claimable on next login)
					_, _ = db.Exec(
						`INSERT OR IGNORE INTO users 
						(username, password_hash, profile, active_channel, listening_channels, is_banned, created_at, updated_at) 
						VALUES (?, ?, ?, ?, ?, 0, ?, ?)`,
						name, "", pState.Profile, pState.ActiveChannel, string(listenJSON), time.Now(), time.Now(),
					)
				}
			}
		}
	}

	// 6. Rename old files to .bak
	oldFiles := []string{
		configPath,
		adminTokenPath,
		channelsPath,
		profilesPath,
		persistPath,
	}
	for _, f := range oldFiles {
		if _, err := os.Stat(f); err == nil {
			_ = os.Rename(f, f+".bak")
		}
	}

	return nil
}

// DBSetSetting saves a global configuration key-value
func DBSetSetting(key, val string) error {
	_, err := db.Exec("INSERT OR REPLACE INTO settings (key, value) VALUES (?, ?)", key, val)
	return err
}

// DBGetSetting retrieves a global configuration value
func DBGetSetting(key string, defaultVal string) string {
	var val string
	err := db.QueryRow("SELECT value FROM settings WHERE key = ?", key).Scan(&val)
	if err != nil {
		return defaultVal
	}
	return val
}

// AuthenticatePlayer validates credentials or registers a player (Auto-Claiming nickname)
func AuthenticatePlayer(username, password, initialChannel, ip, hwid string) (success bool, isBanned bool, err error) {
	username = strings.TrimSpace(username)
	if username == "" {
		return false, false, errors.New("empty username")
	}

	if strings.TrimSpace(password) == "" {
		return false, false, nil
	}

	// 1. Check IP and HWID bans
	ipBanned, err := DBIsIPBanned(ip)
	if err != nil {
		return false, false, err
	}
	if ipBanned {
		return false, true, nil
	}

	hwidBanned, err := DBIsHwidBanned(hwid)
	if err != nil {
		return false, false, err
	}
	if hwidBanned {
		return false, true, nil
	}

	var storedHash string
	var bannedInt int
	var profile string
	var activeChannel string
	var listeningChannels string

	row := db.QueryRow("SELECT password_hash, is_banned, profile, active_channel, listening_channels FROM users WHERE LOWER(username) = LOWER(?)", username)
	err = row.Scan(&storedHash, &bannedInt, &profile, &activeChannel, &listeningChannels)

	isBanned = (bannedInt == 1)

	if err == sql.ErrNoRows {
		// Account does not exist: auto-register (claim name)
		hashed, err := bcrypt.GenerateFromPassword([]byte(password), bcrypt.DefaultCost)
		if err != nil {
			return false, false, err
		}
		_, err = db.Exec(
			"INSERT INTO users (username, password_hash, profile, active_channel, listening_channels, is_banned, hwid, last_ip, created_at, updated_at) VALUES (?, ?, ?, ?, ?, 0, ?, ?, ?, ?)",
			username, string(hashed), "", initialChannel, "[]", hwid, ip, time.Now(), time.Now(),
		)
		if err != nil {
			return false, false, err
		}
		Log(fmt.Sprintf("ACCOUNT created (auto-registration): %s (IP: %s, HWID: %s)", username, ip, hwid), ColorGreen)
		return true, false, nil
	} else if err != nil {
		return false, false, err
	}

	if isBanned {
		return false, true, nil
	}

	// If account exists but password hash is empty (migrated from JSON), claim it now
	if storedHash == "" {
		hashed, err := bcrypt.GenerateFromPassword([]byte(password), bcrypt.DefaultCost)
		if err != nil {
			return false, false, err
		}
		_, err = db.Exec(
			"UPDATE users SET password_hash = ?, hwid = ?, last_ip = ?, updated_at = ? WHERE LOWER(username) = LOWER(?)",
			string(hashed), hwid, ip, time.Now(), username,
		)
		if err != nil {
			return false, false, err
		}
		Log(fmt.Sprintf("ACCOUNT claimed with password (migration): %s", username), ColorGreen)
		return true, false, nil
	}

	// Verify bcrypt password
	err = bcrypt.CompareHashAndPassword([]byte(storedHash), []byte(password))
	if err != nil {
		return false, false, nil // password mismatch
	}

	// Update last_ip and hwid on successful authentication
	_, _ = db.Exec(
		"UPDATE users SET hwid = ?, last_ip = ?, updated_at = ? WHERE LOWER(username) = LOWER(?)",
		hwid, ip, time.Now(), username,
	)

	return true, false, nil
}

// AuthenticateAdmin validates administrative credentials
func AuthenticateAdmin(username, password string) (bool, error) {
	username = strings.TrimSpace(username)
	if username == "" {
		return false, errors.New("empty username")
	}

	var storedHash string
	err := db.QueryRow("SELECT password_hash FROM admins WHERE LOWER(username) = LOWER(?)", username).Scan(&storedHash)
	if err == sql.ErrNoRows {
		return false, nil
	} else if err != nil {
		return false, err
	}

	err = bcrypt.CompareHashAndPassword([]byte(storedHash), []byte(password))
	if err != nil {
		return false, nil // password mismatch
	}

	return true, nil
}

// DBGetPlayerState retrieves saved persistence profile/channels
func DBGetPlayerState(username string) (PlayerPersistentState, bool) {
	var profile string
	var activeChannel string
	var listenJSON string

	err := db.QueryRow("SELECT profile, active_channel, listening_channels FROM users WHERE LOWER(username) = LOWER(?)", username).Scan(&profile, &activeChannel, &listenJSON)
	if err != nil {
		return PlayerPersistentState{}, false
	}

	var list []string
	_ = json.Unmarshal([]byte(listenJSON), &list)

	return PlayerPersistentState{
		Profile:           profile,
		ActiveChannel:     activeChannel,
		ListeningChannels: list,
	}, true
}

// DBSavePlayerState saves the player persistent settings
func DBSavePlayerState(username string, profile string, activeChannel string, listeningChannels []string) error {
	listenJSON, err := json.Marshal(listeningChannels)
	if err != nil {
		return err
	}

	_, err = db.Exec(
		`UPDATE users SET profile = ?, active_channel = ?, listening_channels = ?, updated_at = ? WHERE LOWER(username) = LOWER(?)`,
		profile, activeChannel, string(listenJSON), time.Now(), username,
	)
	return err
}

// DBGetChannels retrieves all configured radio channels
func DBGetChannels() ([]string, error) {
	rows, err := db.Query("SELECT name FROM channels")
	if err != nil {
		return nil, err
	}
	defer rows.Close()

	var list []string
	for rows.Next() {
		var name string
		if err := rows.Scan(&name); err == nil {
			list = append(list, name)
		}
	}
	return list, nil
}

// DBSaveChannel adds a new channel
func DBSaveChannel(name string) error {
	_, err := db.Exec("INSERT OR IGNORE INTO channels (name) VALUES (?)", name)
	return err
}

// DBDeleteChannel removes a channel
func DBDeleteChannel(name string) error {
	_, err := db.Exec("DELETE FROM channels WHERE name = ?", name)
	return err
}

// DBGetProfiles retrieves all configured roles/profiles
func DBGetProfiles() ([]string, error) {
	rows, err := db.Query("SELECT name FROM profiles")
	if err != nil {
		return nil, err
	}
	defer rows.Close()

	var list []string
	for rows.Next() {
		var name string
		if err := rows.Scan(&name); err == nil {
			list = append(list, name)
		}
	}
	return list, nil
}

// DBSaveProfile adds a new profile
func DBSaveProfile(name string) error {
	_, err := db.Exec("INSERT OR IGNORE INTO profiles (name) VALUES (?)", name)
	return err
}

// DBDeleteProfile removes a profile
func DBDeleteProfile(name string) error {
	_, err := db.Exec("DELETE FROM profiles WHERE name = ?", name)
	return err
}

// PlayerAdminInfo represents a player account in admin queries
type PlayerAdminInfo struct {
	Username          string   `json:"username"`
	Profile           string   `json:"profile"`
	ActiveChannel     string   `json:"active_channel"`
	ListeningChannels []string `json:"listening_channels"`
	IsBanned          bool     `json:"is_banned"`
	Hwid              string   `json:"hwid"`
	LastIP            string   `json:"last_ip"`
	CreatedAt         string   `json:"created_at"`
	UpdatedAt         string   `json:"updated_at"`
}

// DBGetPlayersList retrieves all registered user accounts
func DBGetPlayersList() ([]PlayerAdminInfo, error) {
	rows, err := db.Query("SELECT username, profile, active_channel, listening_channels, is_banned, hwid, last_ip, created_at, updated_at FROM users ORDER BY username ASC")
	if err != nil {
		return nil, err
	}
	defer rows.Close()

	var list []PlayerAdminInfo
	for rows.Next() {
		var p PlayerAdminInfo
		var listenJSON string
		var isBannedInt int
		var created, updated time.Time

		err := rows.Scan(&p.Username, &p.Profile, &p.ActiveChannel, &listenJSON, &isBannedInt, &p.Hwid, &p.LastIP, &created, &updated)
		if err == nil {
			_ = json.Unmarshal([]byte(listenJSON), &p.ListeningChannels)
			p.IsBanned = (isBannedInt == 1)
			p.CreatedAt = created.Format("2006-01-02 15:04:05")
			p.UpdatedAt = updated.Format("2006-01-02 15:04:05")
			list = append(list, p)
		}
	}
	return list, nil
}

// DBBanPlayer toggles a player's banned status and propagates to IP/HWID
func DBBanPlayer(username string, ban bool) error {
	banVal := 0
	if ban {
		banVal = 1
	}

	// 1. Get the player's last_ip and hwid before updating
	var lastIP, hwid string
	err := db.QueryRow("SELECT last_ip, hwid FROM users WHERE LOWER(username) = LOWER(?)", username).Scan(&lastIP, &hwid)
	if err != nil && err != sql.ErrNoRows {
		return err
	}

	// 2. Update player banned status
	_, err = db.Exec("UPDATE users SET is_banned = ?, updated_at = ? WHERE LOWER(username) = LOWER(?)", banVal, time.Now(), username)
	if err != nil {
		return err
	}

	// 3. Propagate to IP and HWID
	if ban {
		if lastIP != "" {
			_ = DBAddBannedIP(lastIP, fmt.Sprintf("Banned via account %s", username))
		}
		if hwid != "" {
			_ = DBAddBannedHwid(hwid, fmt.Sprintf("Banned via account %s", username))
		}
	} else {
		if lastIP != "" {
			_ = DBRemoveBannedIP(lastIP)
		}
		if hwid != "" {
			_ = DBRemoveBannedHwid(hwid)
		}
	}

	return nil
}

// AdminInfo represents an administrator account
type AdminInfo struct {
	Username  string `json:"username"`
	CreatedAt string `json:"created_at"`
}

// DBGetAdminsList retrieves all administrator accounts
func DBGetAdminsList() ([]AdminInfo, error) {
	rows, err := db.Query("SELECT username, created_at FROM admins ORDER BY username ASC")
	if err != nil {
		return nil, err
	}
	defer rows.Close()

	var list []AdminInfo
	for rows.Next() {
		var a AdminInfo
		var created time.Time
		if err := rows.Scan(&a.Username, &created); err == nil {
			a.CreatedAt = created.Format("2006-01-02 15:04:05")
			list = append(list, a)
		}
	}
	return list, nil
}



// DBDeletePlayerAccount removes a player's account (freeing their name)
func DBDeletePlayerAccount(username string) error {
	_, err := db.Exec("DELETE FROM users WHERE LOWER(username) = LOWER(?)", username)
	return err
}

// DBResetPlayerPassword changes a player's password hash
func DBResetPlayerPassword(username string, newPassword string) error {
	hashed, err := bcrypt.GenerateFromPassword([]byte(newPassword), bcrypt.DefaultCost)
	if err != nil {
		return err
	}
	_, err = db.Exec("UPDATE users SET password_hash = ?, updated_at = ? WHERE LOWER(username) = LOWER(?)", string(hashed), time.Now(), username)
	return err
}

// DBCreateAdmin registers a new administrator
func DBCreateAdmin(username, password string) error {
	username = strings.TrimSpace(username)
	if username == "" {
		return errors.New("empty username")
	}

	hashed, err := bcrypt.GenerateFromPassword([]byte(password), bcrypt.DefaultCost)
	if err != nil {
		return err
	}

	_, err = db.Exec(
		"INSERT INTO admins (username, password_hash, created_at, updated_at) VALUES (?, ?, ?, ?)",
		username, string(hashed), time.Now(), time.Now(),
	)
	return err
}

// DBDeleteAdmin deletes an administrator (preventing total lockouts)
func DBDeleteAdmin(username string) error {
	username = strings.TrimSpace(username)
	if strings.EqualFold(username, "admin") {
		return errors.New("cannot delete the main administrator 'admin'")
	}

	// Count admins to ensure at least one remains
	var count int
	err := db.QueryRow("SELECT COUNT(*) FROM admins").Scan(&count)
	if err != nil {
		return err
	}
	if count <= 1 {
		return errors.New("cannot delete the last administrator")
	}

	_, err = db.Exec("DELETE FROM admins WHERE LOWER(username) = LOWER(?)", username)
	return err
}

// DBChangeAdminPassword updates an administrator's password
func DBChangeAdminPassword(username string, newPassword string) error {
	hashed, err := bcrypt.GenerateFromPassword([]byte(newPassword), bcrypt.DefaultCost)
	if err != nil {
		return err
	}
	_, err = db.Exec("UPDATE admins SET password_hash = ?, updated_at = ? WHERE LOWER(username) = LOWER(?)", string(hashed), time.Now(), username)
	return err
}

func cleanIP(ip string) string {
	ip = strings.TrimSpace(ip)
	if idx := strings.LastIndex(ip, ":"); idx != -1 {
		host := ip[:idx]
		if strings.HasPrefix(host, "[") && strings.HasSuffix(host, "]") {
			host = host[1 : len(host)-1]
		}
		ip = host
	}
	return ip
}

// DBIsIPBanned checks if an IP is banned
func DBIsIPBanned(ip string) (bool, error) {
	ip = cleanIP(ip)
	if ip == "" {
		return false, nil
	}
	var count int
	err := db.QueryRow("SELECT COUNT(*) FROM banned_ips WHERE ip = ?", ip).Scan(&count)
	return count > 0, err
}

// DBIsHwidBanned checks if a HWID is banned
func DBIsHwidBanned(hwid string) (bool, error) {
	hwid = strings.TrimSpace(hwid)
	if hwid == "" {
		return false, nil
	}
	var count int
	err := db.QueryRow("SELECT COUNT(*) FROM banned_hwids WHERE hwid = ?", hwid).Scan(&count)
	return count > 0, err
}

// DBAddBannedIP bans an IP address
func DBAddBannedIP(ip, reason string) error {
	ip = cleanIP(ip)
	if ip == "" {
		return nil
	}
	_, err := db.Exec("INSERT OR REPLACE INTO banned_ips (ip, reason, created_at) VALUES (?, ?, ?)", ip, reason, time.Now())
	return err
}

// DBAddBannedHwid bans a hardware ID
func DBAddBannedHwid(hwid, reason string) error {
	hwid = strings.TrimSpace(hwid)
	if hwid == "" {
		return nil
	}
	_, err := db.Exec("INSERT OR REPLACE INTO banned_hwids (hwid, reason, created_at) VALUES (?, ?, ?)", hwid, reason, time.Now())
	return err
}

// DBRemoveBannedIP unbans an IP address
func DBRemoveBannedIP(ip string) error {
	ip = cleanIP(ip)
	if ip == "" {
		return nil
	}
	_, err := db.Exec("DELETE FROM banned_ips WHERE ip = ?", ip)
	return err
}

// DBRemoveBannedHwid unbans a hardware ID
func DBRemoveBannedHwid(hwid string) error {
	_, err := db.Exec("DELETE FROM banned_hwids WHERE hwid = ?", strings.TrimSpace(hwid))
	return err
}

// DBGetBannedIPsList retrieves all banned IP addresses
func DBGetBannedIPsList() ([]BannedIPInfo, error) {
	rows, err := db.Query("SELECT ip, reason, created_at FROM banned_ips ORDER BY created_at DESC")
	if err != nil {
		return nil, err
	}
	defer rows.Close()

	var list []BannedIPInfo
	for rows.Next() {
		var info BannedIPInfo
		var created time.Time
		if err := rows.Scan(&info.IP, &info.Reason, &created); err == nil {
			info.CreatedAt = created.Format("2006-01-02 15:04:05")
			list = append(list, info)
		}
	}
	return list, nil
}

// DBGetBannedHwidsList retrieves all banned hardware IDs
func DBGetBannedHwidsList() ([]BannedHwidInfo, error) {
	rows, err := db.Query("SELECT hwid, reason, created_at FROM banned_hwids ORDER BY created_at DESC")
	if err != nil {
		return nil, err
	}
	defer rows.Close()

	var list []BannedHwidInfo
	for rows.Next() {
		var info BannedHwidInfo
		var created time.Time
		if err := rows.Scan(&info.Hwid, &info.Reason, &created); err == nil {
			info.CreatedAt = created.Format("2006-01-02 15:04:05")
			list = append(list, info)
		}
	}
	return list, nil
}
