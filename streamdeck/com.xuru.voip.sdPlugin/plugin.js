let websocket = null;
let pluginUUID = null;

// Registry of active actions currently visible on the Stream Deck
// Key: context, Value: { action, port, settings }
const activeActions = {};

// Polling interval ID
let pollingIntervalId = null;

// Connect to Stream Deck WebSocket
function connectElgatoStreamDeckSocket(inPort, inUUID, inRegisterEvent, inInfo) {
    pluginUUID = inUUID;
    websocket = new WebSocket("ws://127.0.0.1:" + inPort);

    websocket.onopen = function() {
        // Register the plugin
        const json = {
            "event": inRegisterEvent,
            "uuid": inUUID
        };
        websocket.send(JSON.stringify(json));
        
        // Start polling the Companion App API
        startPolling();
    };

    websocket.onmessage = function(evt) {
        const jsonObj = JSON.parse(evt.data);
        const event = jsonObj["event"];
        const action = jsonObj["action"];
        const context = jsonObj["context"];
        const payload = jsonObj["payload"] || {};

        switch (event) {
            case "willAppear":
                activeActions[context] = {
                    action: action,
                    port: payload.settings?.port || 8891,
                    settings: payload.settings || {}
                };
                // Instantly poll to get initial state
                pollCompanionApp();
                break;

            case "willDisappear":
                delete activeActions[context];
                break;

            case "didReceiveSettings":
                if (activeActions[context]) {
                    activeActions[context].port = payload.settings?.port || 8891;
                    activeActions[context].settings = payload.settings || {};
                }
                pollCompanionApp();
                break;

            case "keyDown":
                handleKeyDown(context, activeActions[context]);
                break;
        }
    };

    websocket.onclose = function() {
        stopPolling();
    };
}

// Process key press event
async function handleKeyDown(context, actionInfo) {
    if (!actionInfo) return;

    const port = actionInfo.port || 8891;
    let apiAction = null;

    switch (actionInfo.action) {
        case "com.xuru.voip.action.proximity_mute":
            apiAction = "toggle_proximity_mute";
            break;
        case "com.xuru.voip.action.radio_mute":
            apiAction = "toggle_radio_mute";
            break;
        case "com.xuru.voip.action.profile_mute":
            apiAction = "toggle_profile_mute";
            break;
        case "com.xuru.voip.action.audio_proximity_mute":
            apiAction = "toggle_audio_proximity_mute";
            break;
        case "com.xuru.voip.action.audio_radio_mute":
            apiAction = "toggle_audio_radio_mute";
            break;
        case "com.xuru.voip.action.audio_profile_mute":
            apiAction = "toggle_audio_profile_mute";
            break;
        case "com.xuru.voip.action.toggle_helmet":
            apiAction = "toggle_helmet";
            break;
        case "com.xuru.voip.action.cycle_radio":
            apiAction = "cycle_radio";
            break;
    }

    if (!apiAction) return;

    try {
        const response = await fetch(`http://127.0.0.1:${port}/api/action`, {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify({ action: apiAction })
        });
        
        if (response.ok) {
            // Force a state poll immediately after action
            setTimeout(pollCompanionApp, 50);
        }
    } catch (e) {
        console.error("Failed to execute action on Companion App", e);
        showAlert(context);
    }
}

// Show alert (triangle icon) on Stream Deck
function showAlert(context) {
    if (websocket) {
        websocket.send(JSON.stringify({
            "event": "showAlert",
            "context": context
        }));
    }
}

// Start polling all configured companion app ports
function startPolling() {
    if (pollingIntervalId) clearInterval(pollingIntervalId);
    pollingIntervalId = setInterval(pollCompanionApp, 1000);
}

function stopPolling() {
    if (pollingIntervalId) {
        clearInterval(pollingIntervalId);
        pollingIntervalId = null;
    }
}

// Fetch status from companion app and update Stream Deck states
async function pollCompanionApp() {
    // Get unique ports
    const ports = new Set();
    for (const ctx in activeActions) {
        ports.add(activeActions[ctx].port);
    }

    for (const port of ports) {
        try {
            const response = await fetch(`http://127.0.0.1:${port}/api/status`);
            if (response.ok) {
                const status = await response.json();
                updateStreamDeckStates(port, status);
            }
        } catch (e) {
            // Silent error to prevent console spam when client is closed
        }
    }
}

// Update state/title of visible buttons matching a specific port
function updateStreamDeckStates(port, status) {
    if (!websocket) return;

    for (const context in activeActions) {
        const actionInfo = activeActions[context];
        if (actionInfo.port !== port) continue;

        let state = 0;
        let title = null;

        switch (actionInfo.action) {
            case "com.xuru.voip.action.proximity_mute":
                state = status.micProximityMuted ? 1 : 0;
                break;
            case "com.xuru.voip.action.radio_mute":
                state = status.micRadioMuted ? 1 : 0;
                break;
            case "com.xuru.voip.action.profile_mute":
                state = status.micProfileMuted ? 1 : 0;
                break;
            case "com.xuru.voip.action.audio_proximity_mute":
                state = status.audioProximityMuted ? 1 : 0;
                break;
            case "com.xuru.voip.action.audio_radio_mute":
                state = status.audioRadioMuted ? 1 : 0;
                break;
            case "com.xuru.voip.action.audio_profile_mute":
                state = status.audioProfileMuted ? 1 : 0;
                break;
            case "com.xuru.voip.action.toggle_helmet":
                state = status.isHelmetOn ? 1 : 0;
                break;
            case "com.xuru.voip.action.cycle_radio":
                state = 0;
                title = status.activeChannel || "Proximity";
                break;
        }

        // Send state update
        websocket.send(JSON.stringify({
            "event": "setState",
            "context": context,
            "payload": {
                "state": state
            }
        }));

        // Send title update if applicable (e.g. for active channel display)
        if (title !== null) {
            websocket.send(JSON.stringify({
                "event": "setTitle",
                "context": context,
                "payload": {
                    "title": title,
                    "target": 0
                }
            }));
        }
    }
}

// Entry point called by Elgato Stream Deck
window.connectElgatoStreamDeckSocket = function(inPort, inUUID, inRegisterEvent, inInfo) {
    connectElgatoStreamDeckSocket(inPort, inUUID, inRegisterEvent, inInfo);
};
