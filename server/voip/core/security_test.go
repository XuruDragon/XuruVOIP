package core

import (
	"testing"
	"time"
)

func TestAuthLockoutFlow(t *testing.T) {
	// Limit to 3 failures, sliding window of 2 seconds, ban for 2 seconds
	lockout := NewAuthLockout(3, 2, 2)
	ip := "192.168.1.100"

	// 1. Initial state: not banned
	if lockout.IsBanned(ip) {
		t.Error("IP should not be banned initially")
	}

	// 2. First failure
	banned := lockout.RecordFailure(ip)
	if banned || lockout.IsBanned(ip) {
		t.Error("IP should not be banned after 1 failure")
	}

	// 3. Second failure
	banned = lockout.RecordFailure(ip)
	if banned || lockout.IsBanned(ip) {
		t.Error("IP should not be banned after 2 failures")
	}

	// 4. Third failure -> triggers lockout
	banned = lockout.RecordFailure(ip)
	if !banned || !lockout.IsBanned(ip) {
		t.Error("IP should be banned after 3 failures")
	}

	// 5. Success reset -> unbans
	lockout.RecordSuccess(ip)
	if lockout.IsBanned(ip) {
		t.Error("IP should be unbanned after success record")
	}

	// 6. Test expiration
	lockout.RecordFailure(ip)
	lockout.RecordFailure(ip)
	lockout.RecordFailure(ip)
	if !lockout.IsBanned(ip) {
		t.Error("IP should be banned again")
	}

	// Wait for ban duration to expire
	time.Sleep(2100 * time.Millisecond)
	if lockout.IsBanned(ip) {
		t.Error("IP should be automatically unbanned after ban duration expires")
	}
}

func TestRateLimiterHubFlow(t *testing.T) {
	// Rate: 200.0 tokens/sec, Burst: 3.0 tokens
	hub := NewRateLimiterHub(200.0, 3.0)
	clientKey := "client-conn-1"

	// Initial check: should allow up to burst (3) immediately
	if !hub.Allow(clientKey) {
		t.Error("Expected token 1 to be allowed")
	}
	if !hub.Allow(clientKey) {
		t.Error("Expected token 2 to be allowed")
	}
	if !hub.Allow(clientKey) {
		t.Error("Expected token 3 to be allowed")
	}

	// 4th token should be blocked (rate limited)
	if hub.Allow(clientKey) {
		t.Error("Expected token 4 to be blocked (rate limited)")
	}

	// Wait 10ms -> recovers tokens (0.01s * 200.0 rate = 2.0 tokens)
	time.Sleep(15 * time.Millisecond)
	if !hub.Allow(clientKey) {
		t.Error("Expected recovered token to be allowed")
	}

	// Forget connection
	hub.Forget(clientKey)

	// Since we forgot the client, a new token bucket is instantiated with burst capacity (3)
	if !hub.Allow(clientKey) {
		t.Error("Expected forgot client to have reset burst capacity")
	}
}
