let websocket = null;
let actionUUID = null; // context ID of this action

function connectElgatoStreamDeckSocket(inPort, inUUID, inRegisterEvent, inInfo) {
    actionUUID = inUUID;
    websocket = new WebSocket("ws://127.0.0.1:" + inPort);

    websocket.onopen = function() {
        // Register the Property Inspector
        const json = {
            "event": inRegisterEvent,
            "uuid": inUUID
        };
        websocket.send(JSON.stringify(json));
        
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
        websocket.send(JSON.stringify({
            "event": "setSettings",
            "context": actionUUID,
            "payload": {
                "port": portVal
            }
        }));
    }
}

// Bind input change events to save settings instantly
document.getElementById("port-input").addEventListener("change", saveSettings);
document.getElementById("port-input").addEventListener("keyup", saveSettings);

// Entry point called by Elgato Stream Deck
window.connectElgatoStreamDeckSocket = function(inPort, inUUID, inRegisterEvent, inInfo) {
    connectElgatoStreamDeckSocket(inPort, inUUID, inRegisterEvent, inInfo);
};
