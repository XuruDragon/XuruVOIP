package main

import (
	"encoding/json"
	"fmt"
	"net/http"
	"strings"
	"time"
)

var ServerVersion = "0.0.1"

// CheckForUpdates fetches the latest release from GitHub and logs a message if a newer version exists
func CheckForUpdates() {
	client := &http.Client{
		Timeout: 5 * time.Second,
	}

	req, err := http.NewRequest("GET", "https://api.github.com/repos/XuruDragon/XuruVOIP/releases/latest", nil)
	if err != nil {
		return
	}
	req.Header.Set("User-Agent", "XuruVoipServer")

	resp, err := client.Do(req)
	if err != nil {
		return
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		return
	}

	var result struct {
		TagName string `json:"tag_name"`
	}
	if err := json.NewDecoder(resp.Body).Decode(&result); err != nil {
		return
	}

	latestTag := result.TagName
	latestClean := strings.TrimPrefix(latestTag, "v")
	currentClean := strings.TrimPrefix(ServerVersion, "v")

	if latestClean != "" && latestClean != currentClean {
		fmt.Println()
		Log(fmt.Sprintf("[UPDATE] A new server release is available: %s (Current: v%s)", latestTag, ServerVersion), ColorYellow)
		Log("         Please download the latest version from: https://github.com/XuruDragon/XuruVOIP/releases", ColorYellow)
		fmt.Println()
	}
}
