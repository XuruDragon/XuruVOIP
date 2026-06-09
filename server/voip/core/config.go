package core

import (
	"crypto/rand"
	"fmt"
	"math/big"
	"os"
	"path/filepath"
	"strconv"
	"strings"
)

var MaxPlayers = 64
var BindIP = "0.0.0.0"
var SpatialAudioEnabled = true

// ParseEnvInt parses an integer from the environment
func ParseEnvInt(key string, defaultVal int) int {
	valStr := os.Getenv(key)
	if valStr == "" {
		return defaultVal
	}
	val, err := strconv.Atoi(valStr)
	if err != nil {
		return defaultVal
	}
	return val
}

// ParseEnvFloat parses a float from the environment
func ParseEnvFloat(key string, defaultVal float64) float64 {
	valStr := os.Getenv(key)
	if valStr == "" {
		return defaultVal
	}
	val, err := strconv.ParseFloat(valStr, 64)
	if err != nil {
		return defaultVal
	}
	return val
}

// Config holds the server config values
type Config struct {
	DataDir          string
	ServerToken      string
	AdminServerToken string
	ChannelsList     []string
	ProfilesList     []string
}

// Global configuration instance
var ServerConfig Config

var PublicServer bool
var VerboseLogs int = 1

// ResolveDataDir returns the directory where runtime data is stored
func ResolveDataDir() string {
	dir := os.Getenv("XURUVOIP_DATA_DIR")
	if dir != "" {
		_ = os.MkdirAll(dir, 0755)
		return dir
	}
	return "."
}

// GenerateRandomString generates a secure random alphanumeric string
func GenerateRandomString(length int) string {
	const alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"
	result := make([]byte, length)
	for i := 0; i < length; i++ {
		num, err := rand.Int(rand.Reader, big.NewInt(int64(len(alphabet))))
		if err != nil {
			result[i] = alphabet[0]
			continue
		}
		result[i] = alphabet[num.Int64()]
	}
	return string(result)
}


// LoadOrCreateConfig loads the server configurations from the SQLite database
func LoadOrCreateConfig() error {
	LoadEnv()
	dataDir := ResolveDataDir()
	ServerConfig.DataDir = dataDir

	// 1. Initialize SQLite Database (and run migrations / import old files)
	if err := InitDB(); err != nil {
		return err
	}

	// 2. Load settings solely from Env (delete legacy DB settings)
	_, _ = db.Exec("DELETE FROM settings WHERE key = 'server_token'")

	envServerPassword := os.Getenv("XURUVOIP_SERVER_PASSWORD")
	if envServerPassword == "" {
		envServerPassword = GenerateRandomString(32)
		_ = os.Setenv("XURUVOIP_SERVER_PASSWORD", envServerPassword)
		UpdateEnvFile("XURUVOIP_SERVER_PASSWORD", envServerPassword)
	}
	ServerConfig.ServerToken = envServerPassword

	envAdminServerPassword := os.Getenv("XURUVOIP_ADMIN_SERVER_PASSWORD")
	if envAdminServerPassword == "" {
		envAdminServerPassword = GenerateRandomString(32)
		_ = os.Setenv("XURUVOIP_ADMIN_SERVER_PASSWORD", envAdminServerPassword)
		UpdateEnvFile("XURUVOIP_ADMIN_SERVER_PASSWORD", envAdminServerPassword)
	}
	ServerConfig.AdminServerToken = envAdminServerPassword

	publicServerStr := os.Getenv("XURUVOIP_PUBLIC_SERVER")
	if publicServerStr == "" {
		publicServerStr = "0"
		_ = os.Setenv("XURUVOIP_PUBLIC_SERVER", publicServerStr)
		UpdateEnvFile("XURUVOIP_PUBLIC_SERVER", publicServerStr)
	}
	PublicServer = publicServerStr == "1" || strings.ToLower(publicServerStr) == "true"

	verboseLogsStr := os.Getenv("XURUVOIP_VERBOSE_LOGS")
	if verboseLogsStr == "" {
		verboseLogsStr = "1"
		_ = os.Setenv("XURUVOIP_VERBOSE_LOGS", verboseLogsStr)
		UpdateEnvFile("XURUVOIP_VERBOSE_LOGS", verboseLogsStr)
	}
	VerboseLogs = ParseEnvInt("XURUVOIP_VERBOSE_LOGS", 1)

	MaxPlayers = ParseEnvInt("XURUVOIP_MAX_PLAYERS", 64)
	BindIP = strings.TrimSpace(os.Getenv("XURUVOIP_SERVER_IP"))
	if BindIP == "" {
		BindIP = "0.0.0.0"
	}

	spatialAudioStr := os.Getenv("XURUVOIP_SPATIAL_AUDIO")
	if spatialAudioStr == "" {
		spatialAudioStr = "1"
		_ = os.Setenv("XURUVOIP_SPATIAL_AUDIO", spatialAudioStr)
		UpdateEnvFile("XURUVOIP_SPATIAL_AUDIO", spatialAudioStr)
	}
	SpatialAudioEnabled = spatialAudioStr == "1" || strings.ToLower(spatialAudioStr) == "true"
	// 3. Load Channels
	chList, err := DBGetChannels()
	if err != nil {
		return err
	}
	if len(chList) == 0 {
		chList = []string{"General"}
		_ = DBSaveChannel("General")
	}
	ServerConfig.ChannelsList = chList

	// 4. Load Profiles
	prList, err := DBGetProfiles()
	if err != nil {
		return err
	}
	ServerConfig.ProfilesList = prList

	return nil
}

// SetServerPassword changes the server player password
func SetServerPassword(pwd string) error {
	pwd = strings.TrimSpace(pwd)
	ServerConfig.ServerToken = pwd
	_ = os.Setenv("XURUVOIP_SERVER_PASSWORD", pwd)
	UpdateEnvFile("XURUVOIP_SERVER_PASSWORD", pwd)
	return nil
}

// SetAdminToken changes the password of the default admin account (legacy endpoint adapter)
func SetAdminToken(pwd string) error {
	pwd = strings.TrimSpace(pwd)
	if pwd == "" {
		return DBResetPlayerPassword("admin", "adminpass")
	}
	return DBChangeAdminPassword("admin", pwd)
}

// SaveChannels persists the current channels list to the database
func SaveChannels(channels []string) error {
	_, err := db.Exec("DELETE FROM channels")
	if err != nil {
		return err
	}

	for _, c := range channels {
		if err := DBSaveChannel(c); err != nil {
			return err
		}
	}
	ServerConfig.ChannelsList = channels
	return nil
}

// SaveProfiles persists the current profiles list to the database
func SaveProfiles(profiles []string) error {
	_, err := db.Exec("DELETE FROM profiles")
	if err != nil {
		return err
	}

	for _, p := range profiles {
		if err := DBSaveProfile(p); err != nil {
			return err
		}
	}
	ServerConfig.ProfilesList = profiles
	return nil
}

// LoadEnv initializes env variables from .env file
func LoadEnv() {
	_ = EnsureEnvFile()
	envMap := LoadEnvFile(filepath.Join(ResolveDataDir(), ".env"))
	for k, v := range envMap {
		if os.Getenv(k) == "" {
			_ = os.Setenv(k, v)
		}
	}
}

// EnsureEnvFile creates a default .env file if none exists
func EnsureEnvFile() error {
	envPath := filepath.Join(ResolveDataDir(), ".env")
	if _, err := os.Stat(envPath); err == nil {
		return nil
	}

	serverPassword := GenerateRandomString(32)
	adminServerPassword := GenerateRandomString(32)

	content := fmt.Sprintf(`# XuruVoip Server Configuration

# Bind address and ports settings
# Leave IP empty to bind to all interfaces (0.0.0.0)
XURUVOIP_SERVER_IP=
XURUVOIP_PORT=8888
XURUVOIP_AUDIO_PORT=8889
XURUVOIP_DATA_DIR=.

# Server Capacity
XURUVOIP_MAX_PLAYERS=64

# Public server setting (1 = server password not required, 0 = required)
XURUVOIP_PUBLIC_SERVER=0

# Verbose logging level (0 = none, 1 = default, 2 = global frames per type, 3 = detailed channels/profiles)
XURUVOIP_VERBOSE_LOGS=1

# Server Password / Token for player connections
XURUVOIP_SERVER_PASSWORD=%s

# Admin Server Password / Token for admin portal connections
XURUVOIP_ADMIN_SERVER_PASSWORD=%s

# Security and Rate Limiting
XURUVOIP_LIMIT_RATE_POS=50.0
XURUVOIP_LIMIT_BURST_POS=100
XURUVOIP_LIMIT_RATE_AUDIO=60.0
XURUVOIP_LIMIT_BURST_AUDIO=120

# Brute-force Lockout settings (failures, window seconds, ban seconds)
XURUVOIP_LOCKOUT_ATTEMPTS=5
XURUVOIP_LOCKOUT_WINDOW=60
XURUVOIP_LOCKOUT_DURATION=600

# Spatial Audio (1 = enabled, 0 = disabled)
XURUVOIP_SPATIAL_AUDIO=1
`, serverPassword, adminServerPassword)

	return os.WriteFile(envPath, []byte(content), 0600)
}

// LoadEnvFile reads a simple key=value file
func LoadEnvFile(filename string) map[string]string {
	envMap := make(map[string]string)
	data, err := os.ReadFile(filename)
	if err != nil {
		return envMap
	}

	// Normalize line endings
	content := strings.ReplaceAll(string(data), "\r\n", "\n")
	lines := strings.Split(content, "\n")
	for _, line := range lines {
		line = strings.TrimSpace(line)
		if line == "" || strings.HasPrefix(line, "#") {
			continue
		}
		parts := strings.SplitN(line, "=", 2)
		if len(parts) == 2 {
			key := strings.TrimSpace(parts[0])
			val := strings.TrimSpace(parts[1])
			if (strings.HasPrefix(val, "\"") && strings.HasSuffix(val, "\"")) ||
				(strings.HasPrefix(val, "'") && strings.HasSuffix(val, "'")) {
				val = val[1 : len(val)-1]
			}
			envMap[key] = val
		}
	}
	return envMap
}

// UpdateEnvFile changes a variable inside the .env file
func UpdateEnvFile(key, val string) {
	envPath := filepath.Join(ResolveDataDir(), ".env")
	data, err := os.ReadFile(envPath)
	if err != nil {
		return
	}

	content := strings.ReplaceAll(string(data), "\r\n", "\n")
	lines := strings.Split(content, "\n")
	found := false
	for i, line := range lines {
		trimmed := strings.TrimSpace(line)
		if strings.HasPrefix(trimmed, key+"=") {
			lines[i] = fmt.Sprintf("%s=%s", key, val)
			found = true
			break
		}
	}
	if !found {
		lines = append(lines, fmt.Sprintf("%s=%s", key, val))
	}

	_ = os.WriteFile(envPath, []byte(strings.Join(lines, "\n")), 0600)
}
