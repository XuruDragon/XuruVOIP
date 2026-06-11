let websocket = null;
let actionUUID = null; // context ID of this action
let actionName = null;

function connectElgatoStreamDeckSocket(inPort, inUUID, inRegisterEvent, inInfo) {
    actionUUID = inUUID;
    try {
        const info = JSON.parse(inInfo);
        actionName = info.actionInfo?.action || info.action;
    } catch (e) {}

    websocket = new WebSocket("ws://127.0.0.1:" + inPort);

    websocket.onopen = function() {
        // Register the Property Inspector
        const json = {
            "event": inRegisterEvent,
            "uuid": inUUID
        };
        websocket.send(JSON.stringify(json));
        
        // Show command container if it is voice command action
        if (actionName === "com.xuru.voip.action.voice_command") {
            const container = document.getElementById("command-container");
            if (container) container.style.display = "flex";
        }
        
        // Request existing settings
        getSettings();
    };

    websocket.onmessage = function(evt) {
        const jsonObj = JSON.parse(evt.data);
        const event = jsonObj["event"];
        const payload = jsonObj["payload"] || {};

        if (event === "didReceiveSettings") {
            const settings = payload.settings || {};
            if (settings.port) {
                document.getElementById("port-input").value = settings.port;
            }
            if (settings.command) {
                document.getElementById("command-input").value = settings.command;
            }
        }
    };
}

function getSettings() {
    if (websocket) {
        websocket.send(JSON.stringify({
            "event": "getSettings",
            "context": actionUUID
        }));
    }
}

function saveSettings() {
    if (websocket) {
        const portVal = parseInt(document.getElementById("port-input").value, 10) || 8891;
        const commandVal = document.getElementById("command-input").value || "";
        websocket.send(JSON.stringify({
            "event": "setSettings",
            "context": actionUUID,
            "payload": {
                "port": portVal,
                "command": commandVal
            }
        }));
    }
}

// Bind input change events to save settings instantly
document.getElementById("port-input").addEventListener("change", saveSettings);
document.getElementById("port-input").addEventListener("keyup", saveSettings);
document.getElementById("command-input").addEventListener("change", saveSettings);
document.getElementById("command-input").addEventListener("keyup", saveSettings);

// Entry point called by Elgato Stream Deck
window.connectElgatoStreamDeckSocket = function(inPort, inUUID, inRegisterEvent, inInfo) {
    connectElgatoStreamDeckSocket(inPort, inUUID, inRegisterEvent, inInfo);
};
