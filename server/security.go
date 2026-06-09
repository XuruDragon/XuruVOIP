package main

import (
	"crypto/rand"
	"crypto/rsa"
	"crypto/subtle"
	"crypto/x509"
	"crypto/x509/pkix"
	"encoding/pem"
	"math/big"
	"os"
	"sync"
	"time"
)

// ConstantTimeCompare compares two strings in constant time to prevent timing attacks
func ConstantTimeCompare(a, b string) bool {
	return subtle.ConstantTimeCompare([]byte(a), []byte(b)) == 1
}

// AuthLockout tracks authentication failures per IP and bans IPs temporarily
type AuthLockout struct {
	maxFailures int
	windowSec   int64
	banSec      int64
	failures    map[string][]int64
	banned      map[string]int64
	mu          sync.Mutex
}

// NewAuthLockout creates a new AuthLockout instance
func NewAuthLockout(maxFailures int, windowSec int64, banSec int64) *AuthLockout {
	return &AuthLockout{
		maxFailures: maxFailures,
		windowSec:   windowSec,
		banSec:      banSec,
		failures:    make(map[string][]int64),
		banned:      make(map[string]int64),
	}
}

// IsBanned returns true if the IP is currently banned. Expirations are cleaned up lazily.
func (al *AuthLockout) IsBanned(ip string) bool {
	al.mu.Lock()
	defer al.mu.Unlock()

	until, exists := al.banned[ip]
	if !exists {
		return false
	}

	now := time.Now().Unix()
	if now >= until {
		delete(al.banned, ip)
		delete(al.failures, ip)
		return false
	}
	return true
}

// RecordFailure records a failure for the IP. Returns true if it triggered a new ban.
func (al *AuthLockout) RecordFailure(ip string) bool {
	al.mu.Lock()
	defer al.mu.Unlock()

	now := time.Now().Unix()
	fails := al.failures[ip]

	// Keep only failures within the sliding window
	var activeFails []int64
	for _, t := range fails {
		if now-t < al.windowSec {
			activeFails = append(activeFails, t)
		}
	}
	activeFails = append(activeFails, now)
	al.failures[ip] = activeFails

	if len(activeFails) >= al.maxFailures {
		al.banned[ip] = now + al.banSec
		return true
	}
	return false
}

// RecordSuccess clears failure counters for the IP on successful authentication
func (al *AuthLockout) RecordSuccess(ip string) {
	al.mu.Lock()
	defer al.mu.Unlock()
	delete(al.failures, ip)
	delete(al.banned, ip)
}

// ClientRateLimiter stores token-bucket data for a single client connection
type ClientRateLimiter struct {
	rate   float64
	burst  float64
	tokens float64
	last   time.Time
}

// RateLimiterHub manages rate limiters for multiple connections
type RateLimiterHub struct {
	rate     float64
	burst    float64
	limiters map[interface{}]*ClientRateLimiter
	mu       sync.Mutex
}

// NewRateLimiterHub creates a new RateLimiterHub
func NewRateLimiterHub(rate float64, burst float64) *RateLimiterHub {
	return &RateLimiterHub{
		rate:     rate,
		burst:    burst,
		limiters: make(map[interface{}]*ClientRateLimiter),
	}
}

// Allow consumes 1 token for the specified key. Returns true if allowed, false if rate limited.
func (rlh *RateLimiterHub) Allow(key interface{}) bool {
	rlh.mu.Lock()
	defer rlh.mu.Unlock()

	now := time.Now()
	limiter, exists := rlh.limiters[key]
	if !exists {
		rlh.limiters[key] = &ClientRateLimiter{
			rate:   rlh.rate,
			burst:  rlh.burst,
			tokens: rlh.burst - 1.0,
			last:   now,
		}
		return true
	}

	elapsed := now.Sub(limiter.last).Seconds()
	limiter.tokens = limiter.tokens + elapsed*limiter.rate
	if limiter.tokens > rlh.burst {
		limiter.tokens = rlh.burst
	}
	limiter.last = now

	if limiter.tokens < 1.0 {
		return false
	}
	limiter.tokens -= 1.0
	return true
}

// Forget removes rate limiter state for a disconnected client
func (rlh *RateLimiterHub) Forget(key interface{}) {
	rlh.mu.Lock()
	defer rlh.mu.Unlock()
	delete(rlh.limiters, key)
}

// EnsureSelfSignedCert checks if self-signed TLS cert/key files exist. If not, generates them.
func EnsureSelfSignedCert(certPath, keyPath, commonName string, daysValid int) (bool, string) {
	if _, err := os.Stat(certPath); err == nil {
		if _, err := os.Stat(keyPath); err == nil {
			return true, "existing"
		}
	}

	// Generate private key
	privateKey, err := rsa.GenerateKey(rand.Reader, 2048)
	if err != nil {
		return false, "rsa keygen: " + err.Error()
	}

	// Generate serial number
	serialNumberLimit := new(big.Int).Lsh(big.NewInt(1), 128)
	serialNumber, err := rand.Int(rand.Reader, serialNumberLimit)
	if err != nil {
		return false, "serial num generation: " + err.Error()
	}

	now := time.Now()
	notAfter := now.AddDate(0, 0, daysValid)

	template := x509.Certificate{
		SerialNumber: serialNumber,
		Subject: pkix.Name{
			CommonName:   commonName,
			Organization: []string{"XuruVoip"},
		},
		NotBefore:             now,
		NotAfter:              notAfter,
		KeyUsage:              x509.KeyUsageKeyEncipherment | x509.KeyUsageDigitalSignature,
		ExtKeyUsage:           []x509.ExtKeyUsage{x509.ExtKeyUsageServerAuth},
		BasicConstraintsValid: true,
	}

	derBytes, err := x509.CreateCertificate(rand.Reader, &template, &template, &privateKey.PublicKey, privateKey)
	if err != nil {
		return false, "x509 create cert: " + err.Error()
	}

	// Write certificate file
	certFile, err := os.Create(certPath)
	if err != nil {
		return false, "create cert file: " + err.Error()
	}
	defer certFile.Close()

	if err := pem.Encode(certFile, &pem.Block{Type: "CERTIFICATE", Bytes: derBytes}); err != nil {
		_ = os.Remove(certPath)
		return false, "pem encode cert: " + err.Error()
	}

	// Write private key file
	keyFile, err := os.OpenFile(keyPath, os.O_WRONLY|os.O_CREATE|os.O_TRUNC, 0600)
	if err != nil {
		_ = os.Remove(certPath)
		return false, "create key file: " + err.Error()
	}
	defer keyFile.Close()

	privateKeyBytes, err := x509.MarshalPKCS8PrivateKey(privateKey)
	if err != nil {
		_ = os.Remove(certPath)
		_ = os.Remove(keyPath)
		return false, "pkcs8 marshal key: " + err.Error()
	}

	if err := pem.Encode(keyFile, &pem.Block{Type: "PRIVATE KEY", Bytes: privateKeyBytes}); err != nil {
		_ = os.Remove(certPath)
		_ = os.Remove(keyPath)
		return false, "pem encode key: " + err.Error()
	}

	return true, "generated"
}
