package admin

import (
	"crypto/rand"
	"embed"
	"encoding/hex"
	"encoding/json"
	"fmt"
	"html/template"
	"net/http"
	"os"
	"path/filepath"
	"strings"
	"sync"
	"time"

	"github.com/gorilla/websocket"
	"xuruvoip/server/voip/core"
)

var upgrader = websocket.Upgrader{
	CheckOrigin: func(r *http.Request) bool {
		return true // Allow all origins for local gaming networks
	},
}

//go:embed logo.png
var logoFS embed.FS

// AdminWebSession holds session information
type AdminWebSession struct {
	Token     string
	ExpiresAt time.Time
}

var (
	webSessions   = make(map[string]AdminWebSession)
	webSessionsMu sync.RWMutex
)

// GenerateSecureToken creates a secure random 32-character token
func GenerateSecureToken() string {
	b := make([]byte, 16)
	_, _ = rand.Read(b)
	return hex.EncodeToString(b)
}

// CreateSession registers a new session valid for 2 hours
func CreateSession() string {
	token := GenerateSecureToken()
	webSessionsMu.Lock()
	defer webSessionsMu.Unlock()
	webSessions[token] = AdminWebSession{
		Token:     token,
		ExpiresAt: time.Now().Add(2 * time.Hour),
	}
	return token
}

// ValidateSession verifies if a session token is valid
func ValidateSession(token string) bool {
	webSessionsMu.RLock()
	defer webSessionsMu.RUnlock()
	sess, ok := webSessions[token]
	if !ok {
		return false
	}
	return time.Now().Before(sess.ExpiresAt)
}

// CleanupExpiredSessions periodically sweeps the session map
func CleanupExpiredSessions() {
	webSessionsMu.Lock()
	defer webSessionsMu.Unlock()
	now := time.Now()
	for k, sess := range webSessions {
		if now.After(sess.ExpiresAt) {
			delete(webSessions, k)
		}
	}
}

// RegisterWebAdminHandlers mounts the administration endpoints on the serve mux
func RegisterWebAdminHandlers(mux *http.ServeMux) {
	mux.HandleFunc("/admin", handleWebAdmin)
	mux.HandleFunc("/admin/", handleWebAdmin)
	mux.HandleFunc("/admin/login", handleWebAdminLogin)
	mux.HandleFunc("/admin/logout", handleWebAdminLogout)
	mux.HandleFunc("/admin/ws", handleWebAdminWS)
	mux.HandleFunc("/admin/aar/list", handleAarList)
	mux.HandleFunc("/admin/aar/recordings/", handleAarServe)
	mux.HandleFunc("/admin/aar/delete", handleAarDelete)
	mux.HandleFunc("/admin/logo.png", func(w http.ResponseWriter, r *http.Request) {
		w.Header().Set("Content-Type", "image/png")
		logoBytes, _ := logoFS.ReadFile("logo.png")
		_, _ = w.Write(logoBytes)
	})

	// Start a ticker to clean up expired sessions every 10 minutes
	go func() {
		ticker := time.NewTicker(10 * time.Minute)
		for range ticker.C {
			CleanupExpiredSessions()
		}
	}()
}

func handleWebAdmin(w http.ResponseWriter, r *http.Request) {
	// Restrict to exact matching path /admin or /admin/
	path := r.URL.Path
	if path != "/admin" && path != "/admin/" {
		http.NotFound(w, r)
		return
	}

	// Persist lang parameter to cookie if present
	if qLang := r.URL.Query().Get("lang"); qLang != "" && isSupportedLanguage(qLang) {
		http.SetCookie(w, &http.Cookie{
			Name:     "lang",
			Value:    qLang,
			Path:     "/",
			HttpOnly: false,
			MaxAge:   31536000,
		})
	}

	cookie, err := r.Cookie("xuruvoip_session")
	authorized := false
	if err == nil && ValidateSession(cookie.Value) {
		authorized = true
	}

	if authorized {
		serveDashboard(w, r)
	} else {
		serveLogin(w, r, "")
	}
}

func serveLogin(w http.ResponseWriter, r *http.Request, errMsg string) {
	w.Header().Set("Content-Type", "text/html; charset=utf-8")
	tmpl, err := template.New("login").Parse(loginHTML)
	if err != nil {
		http.Error(w, "Internal Server Error", http.StatusInternalServerError)
		return
	}
	data := GetLoginTemplateData(r, errMsg, core.ServerConfig.AdminServerToken != "")
	_ = tmpl.Execute(w, data)
}

func serveDashboard(w http.ResponseWriter, r *http.Request) {
	w.Header().Set("Content-Type", "text/html; charset=utf-8")
	tmpl, err := template.New("dashboard").Parse(dashboardHTML)
	if err != nil {
		http.Error(w, "Internal Server Error", http.StatusInternalServerError)
		return
	}
	data := GetDashboardTemplateData(r)
	_ = tmpl.Execute(w, data)
}

func handleWebAdminLogin(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodPost {
		http.Error(w, "Method not allowed", http.StatusMethodNotAllowed)
		return
	}

	// Persist lang parameter to cookie if present
	if qLang := r.URL.Query().Get("lang"); qLang != "" && isSupportedLanguage(qLang) {
		http.SetCookie(w, &http.Cookie{
			Name:     "lang",
			Value:    qLang,
			Path:     "/",
			HttpOnly: false,
			MaxAge:   31536000,
		})
	}

	ip := core.ExtractIP(r.RemoteAddr)
	if core.PosLockout.IsBanned(ip) {
		serveLogin(w, r, "Too many login attempts. IP temporarily banned.")
		return
	}

	if err := r.ParseForm(); err != nil {
		serveLogin(w, r, "Invalid request.")
		return
	}

	username := r.FormValue("username")
	password := r.FormValue("password")
	serverPassword := r.FormValue("server_password")

	if core.ServerConfig.AdminServerToken != "" && !core.ConstantTimeCompare(serverPassword, core.ServerConfig.AdminServerToken) {
		core.PosLockout.RecordFailure(ip)
		core.Log(fmt.Sprintf("REJECT Web Admin: Incorrect server password from %s", ip), core.ColorRed)
		serveLogin(w, r, "Incorrect server password.")
		return
	}

	ok, err := core.AuthenticateAdmin(username, password)
	if err == nil && ok {
		core.PosLockout.RecordSuccess(ip)
		sessToken := CreateSession()

		// Determine if accessing over HTTPS to mark cookie secure
		isSecure := r.TLS != nil || r.Header.Get("X-Forwarded-Proto") == "https"

		http.SetCookie(w, &http.Cookie{
			Name:     "xuruvoip_session",
			Value:    sessToken,
			Path:     "/",
			HttpOnly: true,
			Secure:   isSecure,
			SameSite: http.SameSiteLaxMode,
			MaxAge:   7200, // 2 hours
		})

		http.Redirect(w, r, "/admin", http.StatusSeeOther)
	} else {
		core.PosLockout.RecordFailure(ip)
		core.Log(fmt.Sprintf("REJECT Web Admin: Failed login attempt for '%s' from %s", username, ip), core.ColorRed)
		serveLogin(w, r, "Incorrect admin username or password.")
	}
}

func handleWebAdminLogout(w http.ResponseWriter, r *http.Request) {
	cookie, err := r.Cookie("xuruvoip_session")
	if err == nil {
		webSessionsMu.Lock()
		delete(webSessions, cookie.Value)
		webSessionsMu.Unlock()
	}

	http.SetCookie(w, &http.Cookie{
		Name:     "xuruvoip_session",
		Value:    "",
		Path:     "/",
		HttpOnly: true,
		MaxAge:   -1,
	})

	http.Redirect(w, r, "/admin", http.StatusSeeOther)
}

func handleWebAdminWS(w http.ResponseWriter, r *http.Request) {
	// Authenticate session token from cookie or query parameter
	token := ""
	cookie, err := r.Cookie("xuruvoip_session")
	if err == nil {
		token = cookie.Value
	}
	if token == "" {
		token = r.URL.Query().Get("token")
	}

	if token == "" || !ValidateSession(token) {
		http.Error(w, "Unauthorized", http.StatusUnauthorized)
		return
	}

	conn, err := upgrader.Upgrade(w, r, nil)
	if err != nil {
		return
	}

	ip := core.ExtractIP(r.RemoteAddr)
	core.Log(fmt.Sprintf("WEB ADMIN connected from %s", ip), core.ColorGreen)

	// Send admin welcome state
	welcome := core.MsgAdminWelcome{
		Type:          "admin_welcome",
		Channels:      core.ServerConfig.ChannelsList,
		Profiles:      core.ServerConfig.ProfilesList,
		Players:       core.ActiveHub.GetAllPlayerStates(),
		AnonymousMode: core.ActiveHub.GetAnonymousMode(),
	}
	if err := conn.WriteJSON(welcome); err != nil {
		return
	}

	core.ActiveHub.RegisterAdmin(conn)
	defer core.ActiveHub.UnregisterAdmin(conn)

	// Admin websocket command loop
	for {
		_, payload, err := conn.ReadMessage()
		if err != nil {
			break
		}

		var cmd core.AdminCommand
		if err := json.Unmarshal(payload, &cmd); err != nil {
			continue
		}

		ok, reason := ExecuteAdminCommand(cmd)
		val := ExecuteAdminQuery(cmd)
		response := core.MsgAdminResponse{
			Type:   "admin_response",
			ReqID:  cmd.ReqID,
			Cmd:    cmd.Cmd,
			Ok:     ok,
			Reason: reason,
			Value:  val,
		}
		
		core.ActiveHub.Mu.RLock()
		admin, hasAdmin := core.ActiveHub.Admins[conn]
		core.ActiveHub.Mu.RUnlock()
		if hasAdmin {
			_ = admin.SafeWriteJSON(response)
		} else {
			_ = conn.WriteJSON(response)
		}
	}

	core.Log("WEB ADMIN disconnected", core.ColorOrange)
}

func handleAarList(w http.ResponseWriter, r *http.Request) {
	cookie, err := r.Cookie("xuruvoip_session")
	if err != nil || !ValidateSession(cookie.Value) {
		http.Error(w, "Unauthorized", http.StatusUnauthorized)
		return
	}
	if !core.EnableAarRecording {
		http.Error(w, "AAR Recording feature is disabled", http.StatusForbidden)
		return
	}
	list, err := core.DBGetAarRecordings()
	if err != nil {
		http.Error(w, err.Error(), http.StatusInternalServerError)
		return
	}
	w.Header().Set("Content-Type", "application/json")
	_ = json.NewEncoder(w).Encode(list)
}

func handleAarServe(w http.ResponseWriter, r *http.Request) {
	cookie, err := r.Cookie("xuruvoip_session")
	if err != nil || !ValidateSession(cookie.Value) {
		http.Error(w, "Unauthorized", http.StatusUnauthorized)
		return
	}
	if !core.EnableAarRecording {
		http.Error(w, "AAR Recording feature is disabled", http.StatusForbidden)
		return
	}
	parts := strings.Split(r.URL.Path, "/")
	if len(parts) < 5 {
		http.NotFound(w, r)
		return
	}
	filename := parts[len(parts)-1]
	if !strings.HasSuffix(filename, ".ogg") && !strings.HasSuffix(filename, ".jsonl") {
		http.NotFound(w, r)
		return
	}
	filename = filepath.Base(filename)
	dataDir := core.ResolveDataDir()
	filePath := filepath.Join(dataDir, "recordings", filename)

	if _, err := os.Stat(filePath); os.IsNotExist(err) {
		http.NotFound(w, r)
		return
	}

	if strings.HasSuffix(filename, ".jsonl") {
		w.Header().Set("Content-Type", "application/x-jsonlines")
	} else {
		w.Header().Set("Content-Type", "audio/ogg")
	}
	http.ServeFile(w, r, filePath)
}

func handleAarDelete(w http.ResponseWriter, r *http.Request) {
	cookie, err := r.Cookie("xuruvoip_session")
	if err != nil || !ValidateSession(cookie.Value) {
		http.Error(w, "Unauthorized", http.StatusUnauthorized)
		return
	}
	if r.Method != http.MethodPost {
		http.Error(w, "Method not allowed", http.StatusMethodNotAllowed)
		return
	}
	var req struct {
		ID string `json:"id"`
	}
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		http.Error(w, "Invalid body", http.StatusBadRequest)
		return
	}
	req.ID = filepath.Base(req.ID)
	if req.ID == "" {
		http.Error(w, "ID required", http.StatusBadRequest)
		return
	}

	dataDir := core.ResolveDataDir()
	filePath := filepath.Join(dataDir, "recordings", req.ID+".ogg")
	_ = os.Remove(filePath)
	posFilePath := filepath.Join(dataDir, "recordings", req.ID+"_positions.jsonl")
	_ = os.Remove(posFilePath)

	if err := core.DBDeleteAarRecording(req.ID); err != nil {
		http.Error(w, err.Error(), http.StatusInternalServerError)
		return
	}

	w.Header().Set("Content-Type", "application/json")
	_, _ = w.Write([]byte(`{"ok":true}`))
}

