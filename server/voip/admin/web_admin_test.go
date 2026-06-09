package admin

import (
	"testing"
	"time"
)

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
