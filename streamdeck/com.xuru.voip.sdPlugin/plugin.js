let websocket = null;
let pluginUUID = null;

// Registry of active actions currently visible on the Stream Deck
// Key: context, Value: { action, port, settings }
const activeActions = {};

// Cache for latest companion status responses by port
const latestStatusByPort = {};

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

            case "keyUp":
                handleKeyUp(context, activeActions[context]);
                break;

            case "dialRotate":
                handleDialRotate(context, activeActions[context], payload.ticks, payload.pressed);
                break;

            case "dialPress":
                handleDialPress(context, activeActions[context], payload.pressed);
                break;

            case "touchTap":
                handleTouchTap(context, activeActions[context]);
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
    let apiBody = null;

    const currentStatus = latestStatusByPort[port] || {};

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
        case "com.xuru.voip.action.pa_broadcast":
            apiAction = "start_pa";
            break;
        case "com.xuru.voip.action.beacon_repeater":
            apiAction = "toggle_repeater";
            break;
        case "com.xuru.voip.action.voice_command":
            apiAction = "simulate_voice_command";
            apiBody = {
                action: apiAction,
                command: actionInfo.settings?.command || "status report"
            };
            break;
        case "com.xuru.voip.action.intercom_status":
            const currentIntercom = currentStatus.intercomState || "Normal";
            let nextIntercom = "normal";
            if (currentIntercom === "Normal") nextIntercom = "shield_hit";
            else if (currentIntercom === "ShieldHit") nextIntercom = "critical_power";
            else if (currentIntercom === "CriticalPower") nextIntercom = "quantum";
            else nextIntercom = "normal";

            apiAction = "simulate_intercom_state";
            apiBody = {
                action: apiAction,
                state: nextIntercom
            };
            break;
        case "com.xuru.voip.action.location_telemetry":
            // Force a status refresh
            pollCompanionApp();
            return;
        case "com.xuru.voip.action.hail_initiate":
            apiAction = "hail_initiate";
            break;
        case "com.xuru.voip.action.hail_accept":
            apiAction = "hail_accept";
            break;
        case "com.xuru.voip.action.hail_decline":
            apiAction = "hail_decline";
            break;
        case "com.xuru.voip.action.toggle_translation":
            apiAction = "toggle_translation";
            break;
        case "com.xuru.voip.action.toggle_hrtf":
            apiAction = "toggle_hrtf";
            break;
        case "com.xuru.voip.action.toggle_spectrogram":
            apiAction = "toggle_spectrogram";
            break;
    }

    if (!apiAction) return;
    if (!apiBody) {
        apiBody = { action: apiAction };
    }

    try {
        const response = await fetch(`http://127.0.0.1:${port}/api/action`, {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify(apiBody)
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

// Process key release event
async function handleKeyUp(context, actionInfo) {
    if (!actionInfo) return;

    if (actionInfo.action === "com.xuru.voip.action.pa_broadcast") {
        const port = actionInfo.port || 8891;
        try {
            const response = await fetch(`http://127.0.0.1:${port}/api/action`, {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify({ action: "stop_pa" })
            });
            
            if (response.ok) {
                setTimeout(pollCompanionApp, 50);
            }
        } catch (e) {
            console.error("Failed to execute keyUp action on Companion App", e);
        }
    }
}

// Process dial rotate event
async function handleDialRotate(context, actionInfo, ticks, pressed) {
    if (!actionInfo) return;

    const port = actionInfo.port || 8891;
    const currentStatus = latestStatusByPort[port] || {};
    let apiAction = null;
    let apiBody = null;

    switch (actionInfo.action) {
        case "com.xuru.voip.action.cycle_radio_dial":
            const channels = currentStatus.availableChannels || [];
            if (channels.length > 0) {
                const activeChan = currentStatus.activeChannel || "Proximity";
                let idx = channels.indexOf(activeChan);
                if (idx === -1) idx = 0;
                idx = (idx + ticks) % channels.length;
                if (idx < 0) idx += channels.length;
                
                apiAction = "set_channel";
                apiBody = {
                    action: apiAction,
                    channel: channels[idx]
                };
            }
            break;
            
        case "com.xuru.voip.action.adjust_exertion":
            let gforce = currentStatus.gforce !== undefined ? currentStatus.gforce : 0.0;
            let exertion = currentStatus.exertion !== undefined ? currentStatus.exertion : 0.0;
            
            if (pressed) {
                exertion = Math.min(1.0, Math.max(0.0, exertion + ticks * 0.05));
            } else {
                gforce = Math.min(1.0, Math.max(0.0, gforce + ticks * 0.05));
            }
            
            apiAction = "set_exertion";
            apiBody = {
                action: apiAction,
                gforce: gforce,
                exertion: exertion
            };
            break;
            
        case "com.xuru.voip.action.voice_changer_dial":
            const profiles = ["None", "Alien", "Cyborg", "Robotic", "PitchShift"];
            const currentProfile = currentStatus.voiceChangerEnabled ? (currentStatus.voiceChangerType || "None") : "None";
            let idx = profiles.indexOf(currentProfile);
            if (idx === -1) idx = 0;
            idx = (idx + ticks) % profiles.length;
            if (idx < 0) idx += profiles.length;
            
            apiAction = "set_voice_changer";
            apiBody = {
                action: apiAction,
                type: profiles[idx]
            };
            break;
    }

    if (!apiAction || !apiBody) return;

    try {
        const response = await fetch(`http://127.0.0.1:${port}/api/action`, {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify(apiBody)
        });
        
        if (response.ok) {
            setTimeout(pollCompanionApp, 50);
        }
    } catch (e) {
        console.error("Failed to execute dialRotate action on Companion App", e);
        showAlert(context);
    }
}

// Process dial press event
async function handleDialPress(context, actionInfo, pressed) {
    if (!actionInfo) return;
    
    // We only trigger toggle on press down, not on release
    if (!pressed) return;

    const port = actionInfo.port || 8891;
    const currentStatus = latestStatusByPort[port] || {};
    let apiAction = null;
    let apiBody = null;

    switch (actionInfo.action) {
        case "com.xuru.voip.action.cycle_radio_dial":
            apiAction = "toggle_radio_mute";
            break;
            
        case "com.xuru.voip.action.adjust_exertion":
            apiAction = "toggle_exertion_distortion";
            break;
            
        case "com.xuru.voip.action.voice_changer_dial":
            const nextType = currentStatus.voiceChangerEnabled ? "None" : (currentStatus.voiceChangerType !== "None" ? currentStatus.voiceChangerType : "Cyborg");
            apiAction = "set_voice_changer";
            apiBody = {
                action: apiAction,
                type: nextType
            };
            break;
    }

    if (!apiAction) return;
    if (!apiBody) {
        apiBody = { action: apiAction };
    }

    try {
        const response = await fetch(`http://127.0.0.1:${port}/api/action`, {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify(apiBody)
        });
        
        if (response.ok) {
            setTimeout(pollCompanionApp, 50);
        }
    } catch (e) {
        console.error("Failed to execute dialPress action on Companion App", e);
        showAlert(context);
    }
}

// Process touch tap event (touch screen tap)
async function handleTouchTap(context, actionInfo) {
    // Touch tap has the exact same behavior as push (toggles the status)
    await handleDialPress(context, actionInfo, true);
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
                latestStatusByPort[port] = status;
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

        const isEncoder = actionInfo.action.endsWith("_dial") || actionInfo.action === "com.xuru.voip.action.adjust_exertion";
        if (isEncoder) {
            let titleStr = "";
            let valueStr = "";
            let indicatorVal = 0;

            switch (actionInfo.action) {
                case "com.xuru.voip.action.cycle_radio_dial":
                    const channels = status.availableChannels || [];
                    const activeChan = status.activeChannel || "Proximity";
                    let chanIdx = channels.indexOf(activeChan);
                    if (chanIdx === -1) chanIdx = 0;
                    
                    titleStr = "Radio Channel";
                    valueStr = status.micRadioMuted ? `${activeChan} [MUTED]` : activeChan;
                    indicatorVal = channels.length > 1 ? Math.round((chanIdx / (channels.length - 1)) * 100) : 0;
                    break;

                case "com.xuru.voip.action.adjust_exertion":
                    const gfVal = status.gforce !== undefined ? status.gforce : 0.0;
                    const exVal = status.exertion !== undefined ? status.exertion : 0.0;
                    
                    titleStr = "G-Force & Exert";
                    valueStr = status.enableExertionDistortion ? `G: ${gfVal.toFixed(1)}G | Ex: ${Math.round(exVal * 100)}%` : `G: ${gfVal.toFixed(1)}G | [OFF]`;
                    indicatorVal = Math.round(gfVal * 100);
                    break;

                case "com.xuru.voip.action.voice_changer_dial":
                    const profiles = ["None", "Alien", "Cyborg", "Robotic", "PitchShift"];
                    const curProfile = status.voiceChangerEnabled ? (status.voiceChangerType || "None") : "None";
                    let profIdx = profiles.indexOf(curProfile);
                    if (profIdx === -1) profIdx = 0;
                    
                    titleStr = "Voice Changer";
                    valueStr = status.voiceChangerEnabled ? (status.voiceChangerType || "None") : "Disabled";
                    indicatorVal = Math.round((profIdx / (profiles.length - 1)) * 100);
                    break;
            }

            websocket.send(JSON.stringify({
                "event": "setFeedback",
                "context": context,
                "payload": {
                    "title": titleStr,
                    "value": valueStr,
                    "indicator": indicatorVal
                }
            }));
            continue;
        }

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
            case "com.xuru.voip.action.pa_broadcast":
                state = status.isPttPaDown ? 1 : 0;
                break;
            case "com.xuru.voip.action.beacon_repeater":
                state = status.isRadioRepeater ? 1 : 0;
                break;
            case "com.xuru.voip.action.voice_command":
                state = status.isListening ? 1 : 0;
                title = status.isListening ? "LISTENING" : (actionInfo.settings?.command || "Voice Cmd");
                break;
            case "com.xuru.voip.action.intercom_status":
                const intercom = status.intercomState || "Normal";
                if (intercom === "Normal") {
                    state = 0;
                    title = "NORMAL";
                } else if (intercom === "ShieldHit") {
                    state = 1;
                    title = "SHIELD HIT";
                } else if (intercom === "CriticalPower") {
                    state = 2;
                    title = "CRIT PWR";
                } else if (intercom === "QuantumTravel") {
                    state = 3;
                    title = "QUANTUM";
                } else {
                    state = 0;
                    title = "NORMAL";
                }
                break;
            case "com.xuru.voip.action.location_telemetry":
                state = 0;
                if (status.localPos) {
                    const zoneStr = status.localPos.zone || "SYSTEM";
                    const truncZone = zoneStr.length > 9 ? zoneStr.substring(0, 9) : zoneStr;
                    title = `${truncZone}\nX: ${Math.round(status.localPos.x)}\nY: ${Math.round(status.localPos.y)}\nZ: ${Math.round(status.localPos.z)}`;
                } else {
                    title = "NO GPS";
                }
                break;
            case "com.xuru.voip.action.hail_initiate":
                state = 0;
                if (status.hailState === "Outgoing") {
                    title = "HAILING...";
                } else if (status.hailState === "Connected") {
                    title = `ACTIVE:\n${status.hailPeerName || ""}`;
                } else {
                    title = "Initiate\nHail";
                }
                break;
            case "com.xuru.voip.action.hail_accept":
                state = 0;
                if (status.hailState === "Incoming") {
                    title = "ACCEPT\nHAIL!";
                } else {
                    title = "Accept\nHail";
                }
                break;
            case "com.xuru.voip.action.hail_decline":
                state = 0;
                if (status.hailState === "Incoming" || status.hailState === "Outgoing" || status.hailState === "Connected") {
                    title = status.hailState === "Connected" ? "END\nHAIL" : "DECLINE";
                } else {
                    title = "Decline\nHail";
                }
                break;
            case "com.xuru.voip.action.toggle_translation":
                state = status.enableTranslationSubtitles ? 1 : 0;
                title = status.enableTranslationSubtitles ? "TRANS\nON" : "TRANS\nOFF";
                break;
            case "com.xuru.voip.action.toggle_hrtf":
                state = status.enableHrtf ? 1 : 0;
                title = status.enableHrtf ? "HRTF\nON" : "HRTF\nOFF";
                break;
            case "com.xuru.voip.action.toggle_spectrogram":
                state = status.enableVisorSpectrogram ? 1 : 0;
                title = status.enableVisorSpectrogram ? "HUD SPEC\nON" : "HUD SPEC\nOFF";
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
