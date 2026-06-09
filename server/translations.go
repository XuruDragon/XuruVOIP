package main

import (
	"encoding/json"
	"html/template"
	"net/http"
)

func isSupportedLanguage(lang string) bool {
	switch lang {
	case "en", "fr", "de", "es", "pt-BR", "pt-PT", "zh", "ja":
		return true
	}
	return false
}

func getLanguage(r *http.Request) string {
	// 1. Check query parameter
	if qLang := r.URL.Query().Get("lang"); qLang != "" {
		if isSupportedLanguage(qLang) {
			return qLang
		}
	}
	// 2. Check cookie
	if cookie, err := r.Cookie("lang"); err == nil && isSupportedLanguage(cookie.Value) {
		return cookie.Value
	}
	// 3. Fallback to Accept-Language header
	if al := r.Header.Get("Accept-Language"); al != "" {
		for _, part := range []string{"zh", "ja", "pt-BR", "pt-PT", "pt", "fr", "de", "es"} {
			if len(al) >= len(part) && al[:len(part)] == part {
				if part == "pt" {
					return "pt-PT"
				}
				return part
			}
		}
	}
	return "en"
}

func getTranslations(lang string) map[string]string {
	if dict, ok := translations[lang]; ok {
		return dict
	}
	return translations["en"]
}

func translateError(errMsg string, lang string) string {
	if errMsg == "" {
		return ""
	}
	t := getTranslations(lang)
	switch errMsg {
	case "Incorrect server password.":
		return t["IncorrectServerPassword"]
	case "Incorrect admin username or password.":
		return t["IncorrectAdminCredentials"]
	case "Too many login attempts. IP temporarily banned.":
		return t["TooManyLoginAttempts"]
	case "Invalid request.":
		return t["InvalidRequest"]
	}
	return errMsg
}

func GetDashboardTemplateData(r *http.Request) map[string]interface{} {
	lang := getLanguage(r)
	trans := getTranslations(lang)
	transJSON, _ := json.Marshal(trans)
	return map[string]interface{}{
		"Lang":             lang,
		"T":                trans,
		"TranslationsJSON": template.JS(transJSON),
	}
}

func GetLoginTemplateData(r *http.Request, errMsg string, serverPasswordRequired bool) map[string]interface{} {
	lang := getLanguage(r)
	localizedError := translateError(errMsg, lang)
	return map[string]interface{}{
		"Error":                  localizedError,
		"ServerPasswordRequired": serverPasswordRequired,
		"Lang":                   lang,
		"T":                      getTranslations(lang),
	}
}

var translations = map[string]map[string]string{
	"en": {
		"SecurePortal":              "Secure Administration Portal",
		"Username":                  "Username",
		"AdminPassword":              "Admin Password",
		"ServerPassword":             "Server Password",
		"Optional":                  "Optional",
		"Login":                     "Login",
		"Dashboard":                 "Dashboard",
		"RadarMap":                  "Radar Map",
		"PlayerAccounts":            "Player Accounts",
		"Administrators":            "Administrators",
		"ActiveBans":                "Active Bans",
		"ServerStatus":              "Server Status",
		"OnlinePlayers":             "Online players",
		"RadioChannels":             "Radio channels",
		"AudioProfiles":             "Audio profiles",
		"AnonMode":                  "Anonymous Mode",
		"GlobalControl":             "Global Control",
		"HidesNicknames":            "Hides nicknames in game",
		"Enable":                    "Enable",
		"Disable":                   "Disable",
		"Enabled":                   "Enabled",
		"Disabled":                  "Disabled",
		"Passwords":                 "Passwords",
		"PlayerConnToken":           "Player Connection Token",
		"NewPlayerToken":            "New player token",
		"Save":                      "Save",
		"ActivePlayers":             "Active Players",
		"RealTimeList":              "List of active connections in real-time",
		"PlayersCountBadge":         "Players",
		"HeaderPlayer":              "Player",
		"HeaderActiveChannel":       "Active Channel",
		"HeaderListeningChannels":   "Listening Channels",
		"HeaderProfile":             "Profile",
		"HeaderStatus":              "Status",
		"HeaderActions":             "Actions",
		"NoPlayersConnected":        "No players currently connected.",
		"Del":                       "Del",
		"Rename":                    "Rename",
		"Add":                       "Add",
		"HELMET":                    "HELMET",
		"NOHELMET":                  "NO HELMET",
		"GAME":                      "GAME",
		"OFFLINE":                   "OFFLINE",
		"Kick":                      "Kick",
		"Container":                 "Container",
		"Global":                    "Global",
		"NoPosition":                "No position",
		"SearchNickname":            "Search a nickname...",
		"HeaderNickname":            "Nickname",
		"HeaderLastIP":              "Last IP",
		"HeaderHWID":                "HWID (Footprint)",
		"HeaderCreatedAt":           "Created At",
		"HeaderLastModified":        "Last Modified",
		"HeaderUsername":            "Username",
		"HeaderBannedAt":            "Banned At",
		"CreateAdmin":               "Create an Administrator",
		"Password":                  "Password",
		"CreateAccount":             "Create Account",
		"ChangePassword":            "Change Password",
		"Update":                    "Update",
		"IPAddressBans":             "IP Address Bans",
		"BannedIPListDesc":          "List of IP addresses banned from the server",
		"BanAnIP":                   "Ban an IP",
		"HardwareBans":              "Hardware Bans (HWID)",
		"BannedHwidListDesc":        "List of machine identifiers blocked (Windows MachineGuid)",
		"BanAHwid":                  "Ban a HWID",
		"Cancel":                    "Cancel",
		"Validate":                  "Validate",
		"Notification":              "Notification",
		"Logout":                    "Logout",
		"Disconnected":              "Disconnected",
		"Connected":                 "Connected",
		"Connecting":                "Connecting...",
		"IPAddress":                 "IP Address",
		"Reason":                    "Reason",
		"HWIDIdentifier":            "HWID Identifier",
		"AdminWelcomeMsg":           "Connection to XuruVoip server established.",
		"ConnectionLostMsg":         "Connection to server lost. Reconnecting...",
		"ToastSuccess":              "Success",
		"ToastFailed":               "Failed",
		"ToastUnknown":              "Unknown",
		"BanReasonPrompt":           "Ban reason:",
		"NewChannelPrompt":          "Channel name:",
		"RenameChannelPrompt":       "Rename channel {0} to:",
		"NewProfilePrompt":          "Audio profile name:",
		"RenameProfilePrompt":       "Rename profile {0} to:",
		"NewPassPrompt":             "New password for account:",
		"ConfirmKick":               "Kick player {0}?",
		"ConfirmBan":                "Ban player {0}?",
		"ConfirmUnban":              "Do you really want to unban player '{0}'?\nIf they are online, they will be disconnected.",
		"ConfirmDeletePlayer":       "Permanently delete player account '{0}'?\nThis will release their nickname. If they are online, they will be disconnected.",
		"ConfirmDeleteAdmin":        "Do you want to delete administrator '{0}'?",
		"ConfirmDeleteBan":          "Remove ban for {0}?",
		"ConfirmDeleteChannel":      "Delete channel '{0}'?\nPlayers in this channel will be moved to General.",
		"ConfirmDeleteProfile":      "Delete profile '{0}'?\nPlayers with this role will lose it.",
		"ConfirmChangeToken":        "Change the player access token?",
		"InvalidRequest":            "Invalid request.",
		"IncorrectServerPassword":   "Incorrect server password.",
		"IncorrectAdminCredentials":  "Incorrect admin username or password.",
		"TooManyLoginAttempts":      "Too many login attempts. IP temporarily banned.",
		"NoRegisteredPlayers":       "No registered player accounts.",
		"NoRegisteredAdmins":        "No registered administrators.",
		"NoActiveIPBans":            "No active IP address bans.",
		"NoActiveHwidBans":          "No active hardware ID bans.",
		"EnterIPPrompt":             "Enter the IP address to ban:",
		"EnterHwidPrompt":           "Enter the hardware ID (HWID) to ban:",
		"EnterNewChannelName":       "Enter the name of the new radio channel:",
		"EnterNewProfileName":       "Enter the name of the new audio profile:",
		"ChangePasswordTitle":       "Change Password",
		"EnterNewPasswordDesc":      "Enter the new password for {0}:",
		"FillAllFields":             "Please fill in all fields.",
		"EnterPassword":             "Please enter a password.",
		"Protected":                 "Protected",
		"Unban":                     "Unban",
		"Ban":                       "Ban",
		"ResetPwd":                  "Reset Pwd",
		"Delete":                    "Delete",
		"RealTimeDashboard":         "Real-time management dashboard",
		"AdminPortal":               "Secure Administration Portal",
		"LiveLogs":                  "Live Logs",
		"Filter":                    "Filter...",
		"Clear":                     "Clear",
		"PlayerAccountsDesc":        "Comprehensive list of all registered players (with defined passwords)",
		"Title":                     "Title",
		"Description":               "Description",
		"CreateChannel":             "Create Channel",
		"RenameChannel":             "Rename Channel",
		"CreateProfile":             "Create Profile",
		"RenameProfile":             "Rename Profile",
		"Command":                   "Command",
		"OperationSuccess":          "Operation completed successfully.",
		"Failed":                    "Failed",
		"General":                   "General",
		"BANNED":                    "BANNED",
		"ACTIVE":                    "ACTIVE",
	},
	"fr": {
		"SecurePortal":              "Portail d'administration sécurisé",
		"Username":                  "Nom d'utilisateur",
		"AdminPassword":              "Mot de passe administrateur",
		"ServerPassword":             "Mot de passe serveur",
		"Optional":                  "Optionnel",
		"Login":                     "Connexion",
		"Dashboard":                 "Tableau de bord",
		"RadarMap":                  "Carte Radar",
		"PlayerAccounts":            "Comptes Joueurs",
		"Administrators":            "Administrateurs",
		"ActiveBans":                "Bannissements Actifs",
		"ServerStatus":              "Statut du Serveur",
		"OnlinePlayers":             "Joueurs en ligne",
		"RadioChannels":             "Canaux radio",
		"AudioProfiles":             "Profils audio",
		"AnonMode":                  "Mode Anonyme",
		"GlobalControl":             "Contrôle Global",
		"HidesNicknames":            "Masque les pseudos en jeu",
		"Enable":                    "Activer",
		"Disable":                   "Désactiver",
		"Enabled":                   "Activé",
		"Disabled":                  "Désactivé",
		"Passwords":                 "Mots de passe",
		"PlayerConnToken":           "Token de connexion joueur",
		"NewPlayerToken":            "Nouveau token de joueur",
		"Save":                      "Enregistrer",
		"ActivePlayers":             "Joueurs Actifs",
		"RealTimeList":              "Liste des connexions actives en temps réel",
		"PlayersCountBadge":         "Joueurs",
		"HeaderPlayer":              "Joueur",
		"HeaderActiveChannel":       "Canal Actif",
		"HeaderListeningChannels":   "Canaux Écoutés",
		"HeaderProfile":             "Profil",
		"HeaderStatus":              "Statut",
		"HeaderActions":             "Actions",
		"NoPlayersConnected":        "Aucun joueur connecté actuellement.",
		"Del":                       "Suppr",
		"Rename":                    "Renommer",
		"Add":                       "Ajouter",
		"HELMET":                    "CASQUE",
		"NOHELMET":                  "SANS CASQUE",
		"GAME":                      "JEU",
		"OFFLINE":                   "HORS LIGNE",
		"Kick":                      "Exclure",
		"Container":                 "Conteneur",
		"Global":                    "Global",
		"NoPosition":                "Aucune position",
		"SearchNickname":            "Rechercher un pseudo...",
		"HeaderNickname":            "Pseudo",
		"HeaderLastIP":              "Dernière IP",
		"HeaderHWID":                "HWID (Empreinte)",
		"HeaderCreatedAt":           "Créé le",
		"HeaderLastModified":        "Dernière modification",
		"HeaderUsername":            "Nom d'utilisateur",
		"HeaderBannedAt":            "Banni le",
		"CreateAdmin":               "Créer un Administrateur",
		"Password":                  "Mot de passe",
		"CreateAccount":             "Créer le compte",
		"ChangePassword":            "Changer le mot de passe",
		"Update":                    "Mettre à jour",
		"IPAddressBans":             "Bannissements d'Adresses IP",
		"BannedIPListDesc":          "Liste des adresses IP bannies du serveur",
		"BanAnIP":                   "Bannir une IP",
		"HardwareBans":              "Bannissements Matériels (HWID)",
		"BannedHwidListDesc":        "Liste des identifiants machines bloqués (Windows MachineGuid)",
		"BanAHwid":                  "Bannir un HWID",
		"Cancel":                    "Annuler",
		"Validate":                  "Valider",
		"Notification":              "Notification",
		"Logout":                    "Déconnexion",
		"Disconnected":              "Déconnecté",
		"Connected":                 "Connecté",
		"Connecting":                "Connexion...",
		"IPAddress":                 "Adresse IP",
		"Reason":                    "Raison",
		"HWIDIdentifier":            "Identifiant HWID",
		"AdminWelcomeMsg":           "Connexion établie avec le serveur XuruVoip.",
		"ConnectionLostMsg":         "Connexion perdue avec le serveur. Reconnexion...",
		"ToastSuccess":              "Succès",
		"ToastFailed":               "Échec",
		"ToastUnknown":              "Inconnu",
		"BanReasonPrompt":           "Raison du bannissement :",
		"NewChannelPrompt":          "Nom du canal :",
		"RenameChannelPrompt":       "Renommer le canal {0} en :",
		"NewProfilePrompt":          "Nom du profil audio :",
		"RenameProfilePrompt":       "Renommer le profil {0} en :",
		"NewPassPrompt":             "Nouveau mot de passe pour le compte :",
		"ConfirmKick":               "Exclure le joueur {0} ?",
		"ConfirmBan":                "Bannir le joueur {0} ?",
		"ConfirmUnban":              "Voulez-vous vraiment débannir le joueur '{0}' ?\nS'il est en ligne, il sera déconnecté.",
		"ConfirmDeletePlayer":       "Supprimer définitivement le compte joueur de '{0}' ?\nCela libérera son pseudo. S'il est en ligne, il sera déconnecté.",
		"ConfirmDeleteAdmin":        "Voulez-vous supprimer l'administrateur '{0}' ?",
		"ConfirmDeleteBan":          "Lever le bannissement pour {0} ?",
		"ConfirmDeleteChannel":      "Supprimer le canal '{0}' ?\nLes joueurs dans ce canal seront déplacés vers General.",
		"ConfirmDeleteProfile":      "Supprimer le profil '{0}' ?\nLes joueurs avec ce rôle le perdront.",
		"ConfirmChangeToken":        "Modifier le token d'accès joueur ?",
		"InvalidRequest":            "Requête invalide.",
		"IncorrectServerPassword":   "Mot de passe serveur incorrect.",
		"IncorrectAdminCredentials":  "Identifiant ou mot de passe administrateur incorrect.",
		"TooManyLoginAttempts":      "Trop de tentatives de connexion. IP temporairement bannie.",
		"NoRegisteredPlayers":       "Aucun compte joueur enregistré.",
		"NoRegisteredAdmins":        "Aucun administrateur enregistré.",
		"NoActiveIPBans":            "Aucun bannissement d'adresse IP actif.",
		"NoActiveHwidBans":          "Aucun bannissement de HWID actif.",
		"EnterIPPrompt":             "Entrez l'adresse IP à bannir :",
		"EnterHwidPrompt":           "Entrez l'identifiant matériel (HWID) à bannir :",
		"EnterNewChannelName":       "Entrez le nom du nouveau canal radio :",
		"EnterNewProfileName":       "Entrez le nom du nouveau profil audio :",
		"ChangePasswordTitle":       "Changer le mot de passe",
		"EnterNewPasswordDesc":      "Entrez le nouveau mot de passe pour {0} :",
		"FillAllFields":             "Veuillez remplir tous les champs.",
		"EnterPassword":             "Veuillez entrer un mot de passe.",
		"Protected":                 "Protégé",
		"Unban":                     "Débannir",
		"Ban":                       "Bannir",
		"ResetPwd":                  "Réin. MDP",
		"Delete":                    "Supprimer",
		"RealTimeDashboard":         "Console de gestion en temps réel",
		"AdminPortal":               "Portail d'administration sécurisé",
		"LiveLogs":                  "Journaux en direct",
		"Filter":                    "Filtrer...",
		"Clear":                     "Effacer",
		"PlayerAccountsDesc":        "Liste complète de tous les joueurs enregistrés (avec mots de passe définis)",
		"Title":                     "Titre",
		"Description":               "Description",
		"CreateChannel":             "Créer un Canal",
		"RenameChannel":             "Renommer le Canal",
		"CreateProfile":             "Créer un Profil",
		"RenameProfile":             "Renommer le Profil",
		"Command":                   "Commande",
		"OperationSuccess":          "Opération complétée avec succès.",
		"Failed":                    "Échec",
		"General":                   "Général",
		"BANNED":                    "BANNI",
		"ACTIVE":                    "ACTIF",
	},
	"de": {
		"SecurePortal":              "Sicheres Administrationsportal",
		"Username":                  "Benutzername",
		"AdminPassword":              "Admin-Passwort",
		"ServerPassword":             "Server-Passwort",
		"Optional":                  "Optional",
		"Login":                     "Einloggen",
		"Dashboard":                 "Dashboard",
		"RadarMap":                  "Radarkarte",
		"PlayerAccounts":            "Spielerkonten",
		"Administrators":            "Administratoren",
		"ActiveBans":                "Aktive Sperren",
		"ServerStatus":              "Serverstatus",
		"OnlinePlayers":             "Online-Spieler",
		"RadioChannels":             "Funkkanäle",
		"AudioProfiles":             "Audioprofile",
		"AnonMode":                  "Anonymer Modus",
		"GlobalControl":             "Globale Steuerung",
		"HidesNicknames":            "Blendet Spitznamen im Spiel aus",
		"Enable":                    "Aktivieren",
		"Disable":                   "Deaktivieren",
		"Enabled":                   "Aktiviert",
		"Disabled":                  "Deaktiviert",
		"Passwords":                 "Passwörter",
		"PlayerConnToken":           "Spieler-Verbindungstoken",
		"NewPlayerToken":            "Neues Spieler-Token",
		"Save":                      "Speichern",
		"ActivePlayers":             "Aktive Spieler",
		"RealTimeList":              "Echtzeitliste der aktiven Verbindungen",
		"PlayersCountBadge":         "Spieler",
		"HeaderPlayer":              "Spieler",
		"HeaderActiveChannel":       "Aktiver Kanal",
		"HeaderListeningChannels":   "Abhörkanäle",
		"HeaderProfile":             "Profil",
		"HeaderStatus":              "Status",
		"HeaderActions":             "Aktionen",
		"NoPlayersConnected":        "Aktuell keine Spieler verbunden.",
		"Del":                       "Löschen",
		"Rename":                    "Umbenennen",
		"Add":                       "Hinzufügen",
		"HELMET":                    "HELM",
		"NOHELMET":                  "KEIN HELM",
		"GAME":                      "SPIEL",
		"OFFLINE":                   "OFFLINE",
		"Kick":                      "Kicken",
		"Container":                 "Container",
		"Global":                    "Global",
		"NoPosition":                "Keine Position",
		"SearchNickname":            "Spitznamen suchen...",
		"HeaderNickname":            "Spitzname",
		"HeaderLastIP":              "Letzte IP",
		"HeaderHWID":                "HWID (Fingerabdruck)",
		"HeaderCreatedAt":           "Erstellt am",
		"HeaderLastModified":        "Zuletzt geändert",
		"HeaderUsername":            "Benutzername",
		"HeaderBannedAt":            "Gebannt am",
		"CreateAdmin":               "Administrator erstellen",
		"Password":                  "Passwort",
		"CreateAccount":             "Konto erstellen",
		"ChangePassword":            "Passwort ändern",
		"Update":                    "Aktualisieren",
		"IPAddressBans":             "IP-Adressen-Sperren",
		"BannedIPListDesc":          "Liste der vom Server gesperrten IP-Adressen",
		"BanAnIP":                   "IP sperren",
		"HardwareBans":              "Hardware-Sperren (HWID)",
		"BannedHwidListDesc":        "Liste der gesperrten Geräte-IDs (Windows MachineGuid)",
		"BanAHwid":                  "HWID sperren",
		"Cancel":                    "Abbrechen",
		"Validate":                  "Bestätigen",
		"Notification":              "Benachrichtigung",
		"Logout":                    "Ausloggen",
		"Disconnected":              "Getrennt",
		"Connected":                 "Verbunden",
		"Connecting":                "Verbinden...",
		"IPAddress":                 "IP-Adresse",
		"Reason":                    "Grund",
		"HWIDIdentifier":            "HWID-Identifikator",
		"AdminWelcomeMsg":           "Verbindung zum XuruVoip-Server hergestellt.",
		"ConnectionLostMsg":         "Verbindung zum Server verloren. Verbinde erneut...",
		"ToastSuccess":              "Erfolgreich",
		"ToastFailed":               "Fehlgeschlagen",
		"ToastUnknown":              "Unbekannt",
		"BanReasonPrompt":           "Sperrgrund:",
		"NewChannelPrompt":          "Kanalname:",
		"RenameChannelPrompt":       "Kanal {0} umbenennen in:",
		"NewProfilePrompt":          "Audioprofil-Name:",
		"RenameProfilePrompt":       "Profil {0} umbenennen in:",
		"NewPassPrompt":             "Neues Passwort für Konto:",
		"ConfirmKick":               "Spieler {0} kicken?",
		"ConfirmBan":                "Spieler {0} sperren?",
		"ConfirmUnban":              "Möchten Sie Spieler '{0}' wirklich entbannen?\nWenn sie online sind, werden sie getrennt.",
		"ConfirmDeletePlayer":       "Spielerkonto '{0}' dauerhaft löschen?\nDies gibt ihren Spitznamen frei. Wenn sie online sind, werden sie getrennt.",
		"ConfirmDeleteAdmin":        "Möchten Sie Administrator '{0}' wirklich löschen?",
		"ConfirmDeleteBan":          "Sperre für {0} aufheben?",
		"ConfirmDeleteChannel":      "Kanal '{0}' löschen?\nSpieler in diesem Kanal werden nach 'General' verschoben.",
		"ConfirmDeleteProfile":      "Profil '{0}' löschen?\nSpieler mit dieser Rolle verlieren sie.",
		"ConfirmChangeToken":        "Ändern Sie das Spieler-Zugriffstoken?",
		"InvalidRequest":            "Ungültige Anfrage.",
		"IncorrectServerPassword":   "Falsches Server-Passwort.",
		"IncorrectAdminCredentials":  "Falscher Admin-Benutzername oder falsches Passwort.",
		"TooManyLoginAttempts":      "Zu viele Login-Versuche. IP temporär gesperrt.",
		"NoRegisteredPlayers":       "Keine registrierten Spielerkonten.",
		"NoRegisteredAdmins":        "Keine registrierten Administratoren.",
		"NoActiveIPBans":            "Keine aktiven IP-Sperren.",
		"NoActiveHwidBans":          "Keine aktiven HWID-Sperren.",
		"EnterIPPrompt":             "Zu sperrende IP-Adresse eingeben:",
		"EnterHwidPrompt":           "Zu sperrende HWID eingeben:",
		"EnterNewChannelName":       "Namen des neuen Funkkanals eingeben:",
		"EnterNewProfileName":       "Namen des neuen Audioprofils eingeben:",
		"ChangePasswordTitle":       "Passwort ändern",
		"EnterNewPasswordDesc":      "Geben Sie das neue Passwort für {0} ein:",
		"FillAllFields":             "Bitte alle Felder ausfüllen.",
		"EnterPassword":             "Bitte ein Passwort eingeben.",
		"Protected":                 "Geschützt",
		"Unban":                     "Entbannen",
		"Ban":                       "Sperren",
		"ResetPwd":                  "Passw. zurücks.",
		"Delete":                    "Löschen",
		"RealTimeDashboard":         "Echtzeit-Verwaltungskonsole",
		"AdminPortal":               "Sicheres Administrationsportal",
		"LiveLogs":                  "Live-Protokolle",
		"Filter":                    "Filtern...",
		"Clear":                     "Löschen",
		"PlayerAccountsDesc":        "Vollständige Liste aller registrierten Spieler (mit definierten Passwörtern)",
		"Title":                     "Titel",
		"Description":               "Beschreibung",
		"CreateChannel":             "Kanal erstellen",
		"RenameChannel":             "Kanal umbenennen",
		"CreateProfile":             "Profil erstellen",
		"RenameProfile":             "Profil umbenennen",
		"Command":                   "Befehl",
		"OperationSuccess":          "Vorgang erfolgreich abgeschlossen.",
		"Failed":                    "Fehlgeschlagen",
		"General":                   "Allgemein",
		"BANNED":                    "GESPERRT",
		"ACTIVE":                    "AKTIV",
	},
	"es": {
		"SecurePortal":              "Portal de administración seguro",
		"Username":                  "Nombre de usuario",
		"AdminPassword":              "Contraseña de administrador",
		"ServerPassword":             "Contraseña de servidor",
		"Optional":                  "Opcional",
		"Login":                     "Iniciar sesión",
		"Dashboard":                 "Panel de control",
		"RadarMap":                  "Mapa de Radar",
		"PlayerAccounts":            "Cuentas de Jugadores",
		"Administrators":            "Administradores",
		"ActiveBans":                "Baneos Activos",
		"ServerStatus":              "Estado del Servidor",
		"OnlinePlayers":             "Jugadores en línea",
		"RadioChannels":             "Canales de radio",
		"AudioProfiles":             "Perfiles de audio",
		"AnonMode":                  "Modo Anónimo",
		"GlobalControl":             "Control Global",
		"HidesNicknames":            "Oculta apodos en el juego",
		"Enable":                    "Activar",
		"Disable":                   "Desactivar",
		"Enabled":                   "Activado",
		"Disabled":                  "Desactivado",
		"Passwords":                 "Contraseñas",
		"PlayerConnToken":           "Token de conexión del jugador",
		"NewPlayerToken":            "Nuevo token de jugador",
		"Save":                      "Guardar",
		"ActivePlayers":             "Jugadores Activos",
		"RealTimeList":              "Lista de conexiones activas en tiempo real",
		"PlayersCountBadge":         "Jugadores",
		"HeaderPlayer":              "Jugador",
		"HeaderActiveChannel":       "Canal Activo",
		"HeaderListeningChannels":   "Canales Escuchados",
		"HeaderProfile":             "Perfil",
		"HeaderStatus":              "Estado",
		"HeaderActions":             "Acciones",
		"NoPlayersConnected":        "No hay jugadores conectados actualmente.",
		"Del":                       "Eliminar",
		"Rename":                    "Renombrar",
		"Add":                       "Añadir",
		"HELMET":                    "CASCO",
		"NOHELMET":                  "SIN CASCO",
		"GAME":                      "JUEGO",
		"OFFLINE":                   "DESCONECTADO",
		"Kick":                      "Expulsar",
		"Container":                 "Contenedor",
		"Global":                    "Global",
		"NoPosition":                "Sin posición",
		"SearchNickname":            "Buscar apodo...",
		"HeaderNickname":            "Apodo",
		"HeaderLastIP":              "Última IP",
		"HeaderHWID":                "HWID (Huella)",
		"HeaderCreatedAt":           "Creado el",
		"HeaderLastModified":        "Última modificación",
		"HeaderUsername":            "Nombre de usuario",
		"HeaderBannedAt":            "Baneado el",
		"CreateAdmin":               "Crear un Administrador",
		"Password":                  "Contraseña",
		"CreateAccount":             "Crear cuenta",
		"ChangePassword":            "Cambiar contraseña",
		"Update":                    "Actualizar",
		"IPAddressBans":             "Baneos de Dirección IP",
		"BannedIPListDesc":          "Lista de direcciones IP baneadas del servidor",
		"BanAnIP":                   "Banear una IP",
		"HardwareBans":              "Baneos de Hardware (HWID)",
		"BannedHwidListDesc":        "Lista de identificadores de máquina bloqueados (Windows MachineGuid)",
		"BanAHwid":                  "Banear un HWID",
		"Cancel":                    "Cancelar",
		"Validate":                  "Validar",
		"Notification":              "Notificación",
		"Logout":                    "Cerrar sesión",
		"Disconnected":              "Desconectado",
		"Connected":                 "Conectado",
		"Connecting":                "Conectando...",
		"IPAddress":                 "Dirección IP",
		"Reason":                    "Razón",
		"HWIDIdentifier":            "Identificador HWID",
		"AdminWelcomeMsg":           "Conexión establecida con el servidor XuruVoip.",
		"ConnectionLostMsg":         "Conexión perdida con el servidor. Reconectando...",
		"ToastSuccess":              "Éxito",
		"ToastFailed":               "Error",
		"ToastUnknown":              "Desconocido",
		"BanReasonPrompt":           "Razón del baneo:",
		"NewChannelPrompt":          "Nombre del canal:",
		"RenameChannelPrompt":       "Renombrar canal {0} a:",
		"NewProfilePrompt":          "Nombre del perfil de audio:",
		"RenameProfilePrompt":       "Renombrar perfil {0} a:",
		"NewPassPrompt":             "Nueva contraseña para la cuenta:",
		"ConfirmKick":               "¿Expulsar al jugador {0}?",
		"ConfirmBan":                "¿Banear al jugador {0}?",
		"ConfirmUnban":              "¿Realmente quieres desbanear al jugador '{0}'?\nSi están conectados, se desconectarán.",
		"ConfirmDeletePlayer":       "¿Eliminar permanentemente la cuenta del jugador '{0}'?\nEsto liberará su apodo. Si están en línea, se desconectarán.",
		"ConfirmDeleteAdmin":        "¿Quieres eliminar al administrador '{0}'?",
		"ConfirmDeleteBan":          "¿Eliminar baneo para {0}?",
		"ConfirmDeleteChannel":      "¿Eliminar canal '{0}'?\nLos jugadores de este canal serán movidos a General.",
		"ConfirmDeleteProfile":      "¿Eliminar perfil '{0}'?\nLos jugadores con este rol lo perderán.",
		"ConfirmChangeToken":        "¿Cambiar el token de acceso de los jugadores?",
		"InvalidRequest":            "Solicitud no válida.",
		"IncorrectServerPassword":   "Contraseña del servidor incorrecta.",
		"IncorrectAdminCredentials":  "Nombre de usuario o contraseña de administrador incorrectos.",
		"TooManyLoginAttempts":      "Demasiados intentos de inicio de sesión. IP bloqueada temporalmente.",
		"NoRegisteredPlayers":       "No hay cuentas de jugadores registradas.",
		"NoRegisteredAdmins":        "No hay administradores registrados.",
		"NoActiveIPBans":            "No hay baneos de IP activos.",
		"NoActiveHwidBans":          "No hay baneos de HWID activos.",
		"EnterIPPrompt":             "Ingrese la dirección IP a banear:",
		"EnterHwidPrompt":           "Ingrese el ID de hardware (HWID) a banear:",
		"EnterNewChannelName":       "Ingrese el nombre del nuevo canal de radio:",
		"EnterNewProfileName":       "Ingrese el nombre del nuevo perfil de audio:",
		"ChangePasswordTitle":       "Cambiar contraseña",
		"EnterNewPasswordDesc":      "Ingrese la nueva contraseña para {0}:",
		"FillAllFields":             "Por favor, complete todos los campos.",
		"EnterPassword":             "Por favor, ingrese una contraseña.",
		"Protected":                 "Protegido",
		"Unban":                     "Desbanear",
		"Ban":                       "Banear",
		"ResetPwd":                  "Rest. Contr.",
		"Delete":                    "Eliminar",
		"RealTimeDashboard":         "Panel de gestión en tiempo real",
		"AdminPortal":               "Portal de administración seguro",
		"LiveLogs":                  "Registros en vivo",
		"Filter":                    "Filtrar...",
		"Clear":                     "Limpiar",
		"PlayerAccountsDesc":        "Lista completa de todos los jugadores registrados (con contraseñas definidas)",
		"Title":                     "Título",
		"Description":               "Descripción",
		"CreateChannel":             "Crear Canal",
		"RenameChannel":             "Renombrar Canal",
		"CreateProfile":             "Crear Perfil",
		"RenameProfile":             "Renombrar Perfil",
		"Command":                   "Comando",
		"OperationSuccess":          "Operación completada con éxito.",
		"Failed":                    "Error",
		"General":                   "General",
		"BANNED":                    "BANEADO",
		"ACTIVE":                    "ACTIVO",
	},
	"pt-BR": {
		"SecurePortal":              "Portal de Administração Seguro",
		"Username":                  "Nome de usuário",
		"AdminPassword":              "Senha do Administrador",
		"ServerPassword":             "Senha do Servidor",
		"Optional":                  "Opcional",
		"Login":                     "Entrar",
		"Dashboard":                 "Painel de Controle",
		"RadarMap":                  "Mapa do Radar",
		"PlayerAccounts":            "Contas de Jogadores",
		"Administrators":            "Administradores",
		"ActiveBans":                "Banimentos Ativos",
		"ServerStatus":              "Status do Servidor",
		"OnlinePlayers":             "Jogadores online",
		"RadioChannels":             "Canais de rádio",
		"AudioProfiles":             "Perfis de áudio",
		"AnonMode":                  "Modo Anônimo",
		"GlobalControl":             "Controle Global",
		"HidesNicknames":            "Oculta apelidos no jogo",
		"Enable":                    "Habilitar",
		"Disable":                   "Desabilitar",
		"Enabled":                   "Habilitado",
		"Disabled":                  "Desabilitado",
		"Passwords":                 "Senhas",
		"PlayerConnToken":           "Token de conexão do jogador",
		"NewPlayerToken":            "Novo token de jogador",
		"Save":                      "Salvar",
		"ActivePlayers":             "Jogadores Ativos",
		"RealTimeList":              "Lista de conexões ativas em tempo real",
		"PlayersCountBadge":         "Jogadores",
		"HeaderPlayer":              "Jogador",
		"HeaderActiveChannel":       "Canal Ativo",
		"HeaderListeningChannels":   "Canais Ouvindo",
		"HeaderProfile":             "Perfil",
		"HeaderStatus":              "Status",
		"HeaderActions":             "Ações",
		"NoPlayersConnected":        "Nenhum jogador conectado no momento.",
		"Del":                       "Excluir",
		"Rename":                    "Renomear",
		"Add":                       "Adicionar",
		"HELMET":                    "CAPACETE",
		"NOHELMET":                  "SEM CAPACETE",
		"GAME":                      "JOGO",
		"OFFLINE":                   "OFFLINE",
		"Kick":                      "Expulsar",
		"Container":                 "Contêiner",
		"Global":                    "Global",
		"NoPosition":                "Sem posição",
		"SearchNickname":            "Buscar apelido...",
		"HeaderNickname":            "Apelido",
		"HeaderLastIP":              "Último IP",
		"HeaderHWID":                "HWID (Digital)",
		"HeaderCreatedAt":           "Criado em",
		"HeaderLastModified":        "Última modificação",
		"HeaderUsername":            "Nome de usuário",
		"HeaderBannedAt":            "Banido em",
		"CreateAdmin":               "Criar um Administrador",
		"Password":                  "Senha",
		"CreateAccount":             "Criar Conta",
		"ChangePassword":            "Alterar Senha",
		"Update":                    "Atualizar",
		"IPAddressBans":             "Banimentos de Endereço IP",
		"BannedIPListDesc":          "Lista de endereços IP banidos do servidor",
		"BanAnIP":                   "Banir um IP",
		"HardwareBans":              "Banimentos de Hardware (HWID)",
		"BannedHwidListDesc":        "Lista de identificadores de máquina bloqueados (Windows MachineGuid)",
		"BanAHwid":                  "Banir um HWID",
		"Cancel":                    "Cancelar",
		"Validate":                  "Validar",
		"Notification":              "Notificação",
		"Logout":                    "Sair",
		"Disconnected":              "Desconectado",
		"Connected":                 "Conectado",
		"Connecting":                "Conectando...",
		"IPAddress":                 "Endereço IP",
		"Reason":                    "Motivo",
		"HWIDIdentifier":            "Identificador HWID",
		"AdminWelcomeMsg":           "Conexão com o servidor XuruVoip estabelecida.",
		"ConnectionLostMsg":         "Conexão com o servidor perdida. Reconectando...",
		"ToastSuccess":              "Sucesso",
		"ToastFailed":               "Falha",
		"ToastUnknown":              "Desconhecido",
		"BanReasonPrompt":           "Motivo do banimento:",
		"NewChannelPrompt":          "Nome do canal:",
		"RenameChannelPrompt":       "Renomear canal {0} para:",
		"NewProfilePrompt":          "Nome do perfil de áudio:",
		"RenameProfilePrompt":       "Renomear perfil {0} para:",
		"NewPassPrompt":             "Nova senha para a conta:",
		"ConfirmKick":               "Expulsar jogador {0}?",
		"ConfirmBan":                "Banir jogador {0}?",
		"ConfirmUnban":              "Deseja realmente desbanir o jogador '{0}'?\nSe estiver online, ele será desconectado.",
		"ConfirmDeletePlayer":       "Excluir permanentemente a conta do jogador '{0}'?\nIsso liberará seu apelido. Se estiver online, ele será desconectado.",
		"ConfirmDeleteAdmin":        "Deseja excluir o administrador '{0}'?",
		"ConfirmDeleteBan":          "Remover banimento de {0}?",
		"ConfirmDeleteChannel":      "Excluir canal '{0}'?\nOs jogadores neste canal serão movidos para General.",
		"ConfirmDeleteProfile":      "Excluir perfil '{0}'?\nOs jogadores com este papel o perderão.",
		"ConfirmChangeToken":        "Alterar o token de acesso dos jogadores?",
		"InvalidRequest":            "Requisição inválida.",
		"IncorrectServerPassword":   "Senha do servidor incorreta.",
		"IncorrectAdminCredentials":  "Nome de usuário ou senha do administrador incorretos.",
		"TooManyLoginAttempts":      "Muitas tentativas de login. IP temporariamente banido.",
		"NoRegisteredPlayers":       "Nenhuma conta de jogador registrada.",
		"NoRegisteredAdmins":        "Nenhum administrador registrado.",
		"NoActiveIPBans":            "Nenhum banimento de IP ativo.",
		"NoActiveHwidBans":          "Nenhum banimento de HWID ativo.",
		"EnterIPPrompt":             "Digite o endereço IP para banir:",
		"EnterHwidPrompt":           "Digite o ID de hardware (HWID) para banir:",
		"EnterNewChannelName":       "Digite o nome do novo canal de rádio:",
		"EnterNewProfileName":       "Digite o nome do novo perfil de áudio:",
		"ChangePasswordTitle":       "Alterar Senha",
		"EnterNewPasswordDesc":      "Digite a nova senha para {0}:",
		"FillAllFields":             "Por favor, preencha todos os campos.",
		"EnterPassword":             "Por favor, digite uma senha.",
		"Protected":                 "Protegido",
		"Unban":                     "Desbanir",
		"Ban":                       "Banir",
		"ResetPwd":                  "Redef. Senha",
		"Delete":                    "Excluir",
		"RealTimeDashboard":         "Painel de controle em tempo real",
		"AdminPortal":               "Portal de Administração Seguro",
		"LiveLogs":                  "Logs em tempo real",
		"Filter":                    "Filtrar...",
		"Clear":                     "Limpar",
		"PlayerAccountsDesc":        "Lista completa de todos os jogadores registrados (com senhas definidas)",
		"Title":                     "Título",
		"Description":               "Descrição",
		"CreateChannel":             "Criar Canal",
		"RenameChannel":             "Renomear Canal",
		"CreateProfile":             "Criar Perfil",
		"RenameProfile":             "Renomear Perfil",
		"Command":                   "Comando",
		"OperationSuccess":          "Operação concluída com sucesso.",
		"Failed":                    "Falha",
		"General":                   "Geral",
		"BANNED":                    "BANIDO",
		"ACTIVE":                    "ATIVO",
	},
	"pt-PT": {
		"SecurePortal":              "Portal de Administração Seguro",
		"Username":                  "Nome de utilizador",
		"AdminPassword":              "Palavra-passe do Administrador",
		"ServerPassword":             "Palavra-passe do Servidor",
		"Optional":                  "Opcional",
		"Login":                     "Entrar",
		"Dashboard":                 "Painel de Controlo",
		"RadarMap":                  "Mapa do Radar",
		"PlayerAccounts":            "Contas de Jogadores",
		"Administrators":            "Administradores",
		"ActiveBans":                "Banimentos Ativos",
		"ServerStatus":              "Estado do Servidor",
		"OnlinePlayers":             "Jogadores online",
		"RadioChannels":             "Canais de rádio",
		"AudioProfiles":             "Perfis de áudio",
		"AnonMode":                  "Modo Anónimo",
		"GlobalControl":             "Controlo Global",
		"HidesNicknames":            "Oculta alcunhas no jogo",
		"Enable":                    "Ativar",
		"Disable":                   "Desativar",
		"Enabled":                   "Ativado",
		"Disabled":                  "Desativado",
		"Passwords":                 "Palavras-passe",
		"PlayerConnToken":           "Token de ligação do jogador",
		"NewPlayerToken":            "Novo token de jogador",
		"Save":                      "Guardar",
		"ActivePlayers":             "Jogadores Activos",
		"RealTimeList":              "Lista de ligações ativas em tempo real",
		"PlayersCountBadge":         "Jogadores",
		"HeaderPlayer":              "Jogador",
		"HeaderActiveChannel":       "Canal Ativo",
		"HeaderListeningChannels":   "Canais Ouvindo",
		"HeaderProfile":             "Perfil",
		"HeaderStatus":              "Estado",
		"HeaderActions":             "Ações",
		"NoPlayersConnected":        "Nenhum jogador ligado no momento.",
		"Del":                       "Eliminar",
		"Rename":                    "Renomear",
		"Add":                       "Adicionar",
		"HELMET":                    "CAPACETE",
		"NOHELMET":                  "SEM CAPACETE",
		"GAME":                      "JOGO",
		"OFFLINE":                   "OFFLINE",
		"Kick":                      "Expulsar",
		"Container":                 "Contentor",
		"Global":                    "Global",
		"NoPosition":                "Sem posição",
		"SearchNickname":            "Procurar alcunha...",
		"HeaderNickname":            "Alcunha",
		"HeaderLastIP":              "Último IP",
		"HeaderHWID":                "HWID (Digital)",
		"HeaderCreatedAt":           "Criado em",
		"HeaderLastModified":        "Última modificação",
		"HeaderUsername":            "Nome de utilizador",
		"HeaderBannedAt":            "Banido em",
		"CreateAdmin":               "Criar um Administrador",
		"Password":                  "Palavra-passe",
		"CreateAccount":             "Criar Conta",
		"ChangePassword":            "Alterar Palavra-passe",
		"Update":                    "Atualizar",
		"IPAddressBans":             "Banimentos de Endereço IP",
		"BannedIPListDesc":          "Lista de endereços IP banidos do servidor",
		"BanAnIP":                   "Banir um IP",
		"HardwareBans":              "Banimentos de Hardware (HWID)",
		"BannedHwidListDesc":        "Lista de identificadores de máquina bloqueados (Windows MachineGuid)",
		"BanAHwid":                  "Banir um HWID",
		"Cancel":                    "Cancelar",
		"Validate":                  "Validar",
		"Notification":              "Notificação",
		"Logout":                    "Sair",
		"Disconnected":              "Desconectado",
		"Connected":                 "Conectado",
		"Connecting":                "A ligar...",
		"IPAddress":                 "Endereço IP",
		"Reason":                    "Motivo",
		"HWIDIdentifier":            "Identificador HWID",
		"AdminWelcomeMsg":           "Ligação ao servidor XuruVoip estabelecida.",
		"ConnectionLostMsg":         "Ligação ao servidor perdida. A religar...",
		"ToastSuccess":              "Sucesso",
		"ToastFailed":               "Falha",
		"ToastUnknown":              "Desconhecido",
		"BanReasonPrompt":           "Motivo do banimento:",
		"NewChannelPrompt":          "Nome do canal:",
		"RenameChannelPrompt":       "Renomear canal {0} para:",
		"NewProfilePrompt":          "Nome do perfil de áudio:",
		"RenameProfilePrompt":       "Renomear perfil {0} para:",
		"NewPassPrompt":             "Nova palavra-passe para a conta:",
		"ConfirmKick":               "Expulsar jogador {0}?",
		"ConfirmBan":                "Banir jogador {0}?",
		"ConfirmUnban":              "Deseja realmente desbanir o jogador '{0}'?\nSe estiver online, ele será desligado.",
		"ConfirmDeletePlayer":       "Eliminar permanentemente a conta do jogador '{0}'?\nIsto libertará a sua alcunha. Se estiver online, ele será desligado.",
		"ConfirmDeleteAdmin":        "Deseja eliminar o administrador '{0}'?",
		"ConfirmDeleteBan":          "Remover banimento de {0}?",
		"ConfirmDeleteChannel":      "Eliminar canal '{0}'?\nOs jogadores neste canal serão movidos para General.",
		"ConfirmDeleteProfile":      "Eliminar perfil '{0}'?\nOs jogadores com este papel perdem-no.",
		"ConfirmChangeToken":        "Alterar o token de acesso dos jogadores?",
		"InvalidRequest":            "Pedido inválido.",
		"IncorrectServerPassword":   "Palavra-passe do servidor incorreta.",
		"IncorrectAdminCredentials":  "Nome de utilizador ou palavra-passe do administrador incorretos.",
		"TooManyLoginAttempts":      "Muitas tentativas de início de sessão. IP temporariamente banido.",
		"NoRegisteredPlayers":       "Nenhuma conta de jogador registada.",
		"NoRegisteredAdmins":        "Nenhum administrador registado.",
		"NoActiveIPBans":            "Nenhum banimento de IP ativo.",
		"NoActiveHwidBans":          "Nenhum banimento de HWID ativo.",
		"EnterIPPrompt":             "Introduza o endereço IP para banir:",
		"EnterHwidPrompt":           "Introduza o ID de hardware (HWID) para banir:",
		"EnterNewChannelName":       "Introduza o nome do novo canal de rádio:",
		"EnterNewProfileName":       "Introduza o nome do novo perfil de áudio:",
		"ChangePasswordTitle":       "Alterar Palavra-passe",
		"EnterNewPasswordDesc":      "Introduza a nova palavra-passe para {0}:",
		"FillAllFields":             "Por favor, preencha todos os campos.",
		"EnterPassword":             "Por favor, introduza uma palavra-passe.",
		"Protected":                 "Protegido",
		"Unban":                     "Desbanir",
		"Ban":                       "Banir",
		"ResetPwd":                  "Redef. Pal.-pass.",
		"Delete":                    "Eliminar",
		"RealTimeDashboard":         "Painel de controlo em tempo real",
		"AdminPortal":               "Portal de Administração Seguro",
		"LiveLogs":                  "Logs em tempo real",
		"Filter":                    "Filtrar...",
		"Clear":                     "Limpar",
		"PlayerAccountsDesc":        "Lista completa de todos os jogadores registados (com palavras-passe definidas)",
		"Title":                     "Título",
		"Description":               "Descrição",
		"CreateChannel":             "Criar Canal",
		"RenameChannel":             "Renomear Canal",
		"CreateProfile":             "Criar Perfil",
		"RenameProfile":             "Renomear Perfil",
		"Command":                   "Comando",
		"OperationSuccess":          "Operação concluída com sucesso.",
		"Failed":                    "Falha",
		"General":                   "Geral",
		"BANNED":                    "BANIDO",
		"ACTIVE":                    "ATIVO",
	},
	"zh": {
		"SecurePortal":              "安全管理门户",
		"Username":                  "用户名",
		"AdminPassword":              "管理员密码",
		"ServerPassword":             "服务器密码",
		"Optional":                  "可选",
		"Login":                     "登录",
		"Dashboard":                 "仪表板",
		"RadarMap":                  "雷达地图",
		"PlayerAccounts":            "玩家账号",
		"Administrators":            "管理员账号",
		"ActiveBans":                "封禁列表",
		"ServerStatus":              "服务器状态",
		"OnlinePlayers":             "在线玩家",
		"RadioChannels":             "无线电频道",
		"AudioProfiles":             "音频配置文件",
		"AnonMode":                  "匿名模式",
		"GlobalControl":             "全局控制",
		"HidesNicknames":            "在游戏中隐藏昵称",
		"Enable":                    "启用",
		"Disable":                   "禁用",
		"Enabled":                   "已启用",
		"Disabled":                  "已禁用",
		"Passwords":                 "密码",
		"PlayerConnToken":           "玩家连接 Token",
		"NewPlayerToken":            "新连接 Token",
		"Save":                      "保存",
		"ActivePlayers":             "活动玩家",
		"RealTimeList":              "实时活动连接列表",
		"PlayersCountBadge":         "玩家数",
		"HeaderPlayer":              "玩家",
		"HeaderActiveChannel":       "活动频道",
		"HeaderListeningChannels":   "监听频道",
		"HeaderProfile":             "配置文件",
		"HeaderStatus":              "状态",
		"HeaderActions":             "操作",
		"NoPlayersConnected":        "当前无玩家连接。",
		"Del":                       "删除",
		"Rename":                    "重命名",
		"Add":                       "添加",
		"HELMET":                    "头盔",
		"NOHELMET":                  "未戴头盔",
		"GAME":                      "游戏内",
		"OFFLINE":                   "离线",
		"Kick":                      "踢出",
		"Container":                 "容器",
		"Global":                    "全局",
		"NoPosition":                "无位置",
		"SearchNickname":            "搜索昵称...",
		"HeaderNickname":            "昵称",
		"HeaderLastIP":              "最后 IP",
		"HeaderHWID":                "HWID (指纹)",
		"HeaderCreatedAt":           "创建时间",
		"HeaderLastModified":        "最后修改",
		"HeaderUsername":            "用户名",
		"HeaderBannedAt":            "封禁时间",
		"CreateAdmin":               "创建管理员",
		"Password":                  "密码",
		"CreateAccount":             "创建账号",
		"ChangePassword":            "修改密码",
		"Update":                    "更新",
		"IPAddressBans":             "IP 地址封禁",
		"BannedIPListDesc":          "从服务器封禁的 IP 地址列表",
		"BanAnIP":                   "封禁 IP",
		"HardwareBans":              "硬件封禁 (HWID)",
		"BannedHwidListDesc":        "被封禁的机器标识符列表 (Windows MachineGuid)",
		"BanAHwid":                  "封禁 HWID",
		"Cancel":                    "取消",
		"Validate":                  "确认",
		"Notification":              "通知",
		"Logout":                    "退出登录",
		"Disconnected":              "已断开",
		"Connected":                 "已连接",
		"Connecting":                "正在连接...",
		"IPAddress":                 "IP 地址",
		"Reason":                    "原因",
		"HWIDIdentifier":            "HWID 标识符",
		"AdminWelcomeMsg":           "与 XuruVoip 服务器的连接已建立。",
		"ConnectionLostMsg":         "与服务器断开连接。正在重连...",
		"ToastSuccess":              "成功",
		"ToastFailed":               "失败",
		"ToastUnknown":              "未知",
		"BanReasonPrompt":           "封禁原因：",
		"NewChannelPrompt":          "频道名称：",
		"RenameChannelPrompt":       "将频道 {0} 重命名为：",
		"NewProfilePrompt":          "音频配置文件名称：",
		"RenameProfilePrompt":       "将配置文件 {0} 重命名为：",
		"NewPassPrompt":             "账号的新密码：",
		"ConfirmKick":               "确定踢出玩家 {0} 吗？",
		"ConfirmBan":                "确定封禁玩家 {0} 吗？",
		"ConfirmUnban":              "确定解除对玩家 '{0}' 的封禁吗？\n如果该玩家处于在线状态，将会被断开连接。",
		"ConfirmDeletePlayer":       "确定永久删除玩家账号 '{0}' 吗？\n这将释放该玩家的昵称。如果玩家在线，将会被强制下线并断开连接。",
		"ConfirmDeleteAdmin":        "确定删除管理员账号 '{0}' 吗？",
		"ConfirmDeleteBan":          "确定解除对 {0} 的封禁吗？",
		"ConfirmDeleteChannel":      "确定删除频道 '{0}' 吗？\n该频道内的玩家将被移动到 General 频道。",
		"ConfirmDeleteProfile":      "确定删除配置文件 '{0}' 吗？\n拥有此角色的玩家将失去该角色。",
		"ConfirmChangeToken":        "确定修改玩家连接 Token 吗？",
		"InvalidRequest":            "无效的请求。",
		"IncorrectServerPassword":   "服务器密码错误。",
		"IncorrectAdminCredentials":  "管理员用户名或密码错误。",
		"TooManyLoginAttempts":      "登录尝试次数过多。IP 已被临时封禁。",
		"NoRegisteredPlayers":       "无已注册的玩家账号。",
		"NoRegisteredAdmins":        "无已注册的管理员。",
		"NoActiveIPBans":            "无活动的 IP 封禁。",
		"NoActiveHwidBans":          "无活动的 HWID 封禁。",
		"EnterIPPrompt":             "输入要封禁的 IP 地址：",
		"EnterHwidPrompt":           "输入要封禁的硬件 ID (HWID)：",
		"EnterNewChannelName":       "输入新无线电频道的名称：",
		"EnterNewProfileName":       "输入新音频配置文件的名称：" ,
		"ChangePasswordTitle":       "修改密码",
		"EnterNewPasswordDesc":      "输入 {0} 的新密码：",
		"FillAllFields":             "请填写所有字段。",
		"EnterPassword":             "请输入密码。",
		"Protected":                 "已保护",
		"Unban":                     "解封",
		"Ban":                       "封禁",
		"ResetPwd":                  "重置密码",
		"Delete":                    "删除",
		"RealTimeDashboard":         "实时管理仪表板",
		"AdminPortal":               "安全管理门户",
		"LiveLogs":                  "实时日志",
		"Filter":                    "过滤...",
		"Clear":                     "清空",
		"PlayerAccountsDesc":        "所有已注册玩家的完整列表 (包含已定义密码)",
		"Title":                     "标题",
		"Description":               "描述",
		"CreateChannel":             "创建频道",
		"RenameChannel":             "重命名频道",
		"CreateProfile":             "创建配置文件",
		"RenameProfile":             "重命名配置文件",
		"Command":                   "命令",
		"OperationSuccess":          "操作已成功完成。",
		"Failed":                    "失败",
		"General":                   "通用",
		"BANNED":                    "已封禁",
		"ACTIVE":                    "活动",
	},
	"ja": {
		"SecurePortal":              "安全な管理ポータル",
		"Username":                  "ユーザー名",
		"AdminPassword":              "管理者パスワード",
		"ServerPassword":             "サーバーパスワード",
		"Optional":                  "任意",
		"Login":                     "ログイン",
		"Dashboard":                 "ダッシュボード",
		"RadarMap":                  "レーダーマップ",
		"PlayerAccounts":            "プレイヤーアカウント",
		"Administrators":            "管理者アカウント",
		"ActiveBans":                "アクティブなBAN",
		"ServerStatus":              "サーバーのステータス",
		"OnlinePlayers":             "オンラインプレイヤー",
		"RadioChannels":             "無線チャンネル",
		"AudioProfiles":             "オーディオプロフィール",
		"AnonMode":                  "匿名モード",
		"GlobalControl":             "グローバル制御",
		"HidesNicknames":            "ゲーム内のニックネームを非表示にする",
		"Enable":                    "有効にする",
		"Disable":                   "無効にする",
		"Enabled":                   "有効",
		"Disabled":                  "無効",
		"Passwords":                 "パスワード",
		"PlayerConnToken":           "プレイヤー接続トークン",
		"NewPlayerToken":            "新しい接続トークン",
		"Save":                      "保存",
		"ActivePlayers":             "アクティブなプレイヤー",
		"RealTimeList":              "リアルタイム接続プレイヤー一覧",
		"PlayersCountBadge":         "プレイヤー",
		"HeaderPlayer":              "プレイヤー",
		"HeaderActiveChannel":       "有効なチャンネル",
		"HeaderListeningChannels":   "受信チャンネル",
		"HeaderProfile":             "プロフィール",
		"HeaderStatus":              "状態",
		"HeaderActions":             "アクション",
		"NoPlayersConnected":        "現在、プレイヤーは接続していません。",
		"Del":                       "削除",
		"Rename":                    "名前変更",
		"Add":                       "追加",
		"HELMET":                    "ヘルメット",
		"NOHELMET":                  "ヘルメットなし",
		"GAME":                      "ゲーム中",
		"OFFLINE":                   "オフライン",
		"Kick":                      "キック",
		"Container":                 "コンテナ",
		"Global":                    "グローバル",
		"NoPosition":                "位置情報なし",
		"SearchNickname":            "ニックネームを検索...",
		"HeaderNickname":            "ニックネーム",
		"HeaderLastIP":              "最終IP",
		"HeaderHWID":                "HWID (フットプリント)",
		"HeaderCreatedAt":           "作成日時",
		"HeaderLastModified":        "最終更新日時",
		"HeaderUsername":            "ユーザー名",
		"HeaderBannedAt":            "BAN日時",
		"CreateAdmin":               "管理者の作成",
		"Password":                  "パスワード",
		"CreateAccount":             "アカウント作成",
		"ChangePassword":            "パスワード変更",
		"Update":                    "更新",
		"IPAddressBans":             "IPアドレスBAN",
		"BannedIPListDesc":          "サーバーからBANされたIPアドレス一覧",
		"BanAnIP":                   "IPをBAN",
		"HardwareBans":              "ハードウェアBAN (HWID)",
		"BannedHwidListDesc":        "ブロックされた機器識別子一覧 (Windows MachineGuid)",
		"BanAHwid":                  "HWIDをBAN",
		"Cancel":                    "キャンセル",
		"Validate":                  "確定",
		"Notification":              "通知",
		"Logout":                    "ログアウト",
		"Disconnected":              "切断",
		"Connected":                 "接続完了",
		"Connecting":                "接続中...",
		"IPAddress":                 "IPアドレス",
		"Reason":                    "理由",
		"HWIDIdentifier":            "HWID識別子",
		"AdminWelcomeMsg":           "XuruVoipサーバーとの接続が確立されました。",
		"ConnectionLostMsg":         "サーバーとの接続が切断されました。再接続中...",
		"ToastSuccess":              "成功",
		"ToastFailed":               "失敗",
		"ToastUnknown":              "不明",
		"BanReasonPrompt":           "BANの理由:",
		"NewChannelPrompt":          "チャンネル名:",
		"RenameChannelPrompt":       "チャンネル {0} の名前を以下に変更:",
		"NewProfilePrompt":          "オーディオプロフィール名:",
		"RenameProfilePrompt":       "プロフィール {0} の名前を以下に変更:",
		"NewPassPrompt":             "アカウントの新しいパスワード:",
		"ConfirmKick":               "プレイヤー {0} をキックしますか？",
		"ConfirmBan":                "プレイヤー {0} をBANしますか？",
		"ConfirmUnban":              "プレイヤー '{0}' のBANを解除しますか？\nオンラインの場合、切断されます。",
		"ConfirmDeletePlayer":       "プレイヤーアカウント '{0}' を永久に削除しますか？\nニックネームが解放されます。オンラインの場合、切断されます。",
		"ConfirmDeleteAdmin":        "管理者アカウント '{0}' を削除しますか？",
		"ConfirmDeleteBan":          "{0} のBANを解除しますか？",
		"ConfirmDeleteChannel":      "チャンネル '{0}' を削除しますか？\nこのチャンネル内のプレイヤーは General に移動されます。",
		"ConfirmDeleteProfile":      "プロフィール '{0}' を削除しますか？\nこのロールを持つプレイヤーからロールが削除されます。",
		"ConfirmChangeToken":        "プレイヤー接続トークンを変更しますか？",
		"InvalidRequest":            "無効なリクエスト。",
		"IncorrectServerPassword":   "サーバーパスワードが正しくありません。",
		"IncorrectAdminCredentials":  "管理者ユーザー名またはパスワードが正しくありません。",
		"TooManyLoginAttempts":      "ログイン試行回数が多すぎます。IPが一時的にBANされています。",
		"NoRegisteredPlayers":       "登録済みのプレイヤーアカウントはありません。",
		"NoRegisteredAdmins":        "登録済みの管理者はありません。",
		"NoActiveIPBans":            "有効なIP-BANはありません。",
		"NoActiveHwidBans":          "有効なHWID-BANはありません。",
		"EnterIPPrompt":             "BANするIPアドレスを入力してください:",
		"EnterHwidPrompt":           "BANするハードウェアID (HWID) を入力してください:",
		"EnterNewChannelName":       "新しい無線チャンネルの名前を入力してください:",
		"EnterNewProfileName":       "新しいオーディオプロフィールの名前を入力してください:",
		"ChangePasswordTitle":       "パスワード変更",
		"EnterNewPasswordDesc":      "{0} の新しいパスワードを入力してください:",
		"FillAllFields":             "すべてのフィールドを入力してください。",
		"EnterPassword":             "パスワードを入力してください。",
		"Protected":                 "保護",
		"Unban":                     "BAN解除",
		"Ban":                       "BAN",
		"ResetPwd":                  "パスワードリセット",
		"Delete":                    "削除",
		"RealTimeDashboard":         "リアルタイム管理ダッシュボード",
		"AdminPortal":               "安全な管理ポータル",
		"LiveLogs":                  "ライブログ",
		"Filter":                    "フィルター...",
		"Clear":                     "消去",
		"PlayerAccountsDesc":        "登録済みプレイヤーアカウント一覧 (パスワード設定済み)",
		"Title":                     "タイトル",
		"Description":               "説明",
		"CreateChannel":             "チャンネル作成",
		"RenameChannel":             "チャンネル名変更",
		"CreateProfile":             "プロフィール作成",
		"RenameProfile":             "プロフィール名変更",
		"Command":                   "コマンド",
		"OperationSuccess":          "操作が正常に完了しました。",
		"Failed":                    "失敗",
		"General":                   "ジェネラル",
		"BANNED":                    "BAN中",
		"ACTIVE":                    "アクティブ",
	},
}

const loginHTML = `<!DOCTYPE html>
<html lang="{{.Lang}}">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>XuruVoip Admin - {{index .T "Login"}}</title>
    <link rel="icon" type="image/png" href="/admin/logo.png">
    <script src="https://cdn.tailwindcss.com"></script>
    <link href="https://fonts.googleapis.com/css2?family=Outfit:wght@300;400;500;600;700&display=swap" rel="stylesheet">
    <style>
        body {
            font-family: 'Outfit', sans-serif;
            background: radial-gradient(circle at center, #111827 0%, #030712 100%);
        }
        .glass {
            background: rgba(17, 24, 39, 0.7);
            backdrop-filter: blur(12px);
            border: 1px solid rgba(255, 255, 255, 0.08);
            box-shadow: 0 8px 32px 0 rgba(0, 0, 0, 0.37);
        }
        .glow {
            box-shadow: 0 0 20px rgba(16, 185, 129, 0.2);
        }
    </style>
</head>
<body class="flex items-center justify-center min-h-screen text-slate-100 overflow-hidden">
    <div class="absolute inset-0 z-0 opacity-30">
        <div class="absolute top-10 left-10 w-96 h-96 bg-emerald-500 rounded-full filter blur-[100px] animate-pulse"></div>
        <div class="absolute bottom-10 right-10 w-96 h-96 bg-indigo-600 rounded-full filter blur-[100px] animate-pulse" style="animation-delay: 2s;"></div>
    </div>
    
    <div class="w-full max-w-md p-8 rounded-2xl glass glow z-10 relative">
        <div class="text-center mb-8">
            <div class="inline-flex mb-3">
                <img src="/admin/logo.png" alt="XuruVoip Logo" class="w-24 h-24 rounded-2xl border border-emerald-500/30 shadow-lg shadow-emerald-500/10">
            </div>
            <h1 class="text-3xl font-bold tracking-tight text-white flex items-center justify-center gap-2">XuruVoip Admin</h1>
            <p class="text-slate-400 mt-2 text-sm">{{index .T "SecurePortal"}}</p>
        </div>

        {{if .Error}}
        <div class="p-3 mb-6 rounded-lg bg-red-500/15 border border-red-500/30 text-red-200 text-sm text-center">
            {{.Error}}
        </div>
        {{end}}

        <form action="/admin/login?lang={{.Lang}}" method="POST" class="space-y-6">
            <div>
                <label for="username" class="block text-sm font-medium text-slate-300 mb-2">{{index .T "Username"}}</label>
                <input type="text" name="username" id="username" required autofocus placeholder="admin"
                    class="w-full px-4 py-3 rounded-xl bg-slate-900/80 border border-slate-700 text-white placeholder-slate-600 focus:outline-none focus:ring-2 focus:ring-emerald-500 focus:border-transparent transition">
            </div>
            <div>
                <label for="password" class="block text-sm font-medium text-slate-300 mb-2">{{index .T "AdminPassword"}}</label>
                <input type="password" name="password" id="password" required placeholder="••••••••"
                    class="w-full px-4 py-3 rounded-xl bg-slate-900/80 border border-slate-700 text-white placeholder-slate-600 focus:outline-none focus:ring-2 focus:ring-emerald-500 focus:border-transparent transition">
            </div>
            <div>
                <label for="server_password" class="block text-sm font-medium text-slate-300 mb-2">{{index .T "ServerPassword"}} {{if not .ServerPasswordRequired}}({{index .T "Optional"}}){{end}}</label>
                <input type="password" name="server_password" id="server_password" {{if .ServerPasswordRequired}}required{{end}} placeholder="••••••••"
                    class="w-full px-4 py-3 rounded-xl bg-slate-900/80 border border-slate-700 text-white placeholder-slate-600 focus:outline-none focus:ring-2 focus:ring-emerald-500 focus:border-transparent transition">
            </div>
            <button type="submit"
                class="w-full py-3 px-4 rounded-xl bg-gradient-to-r from-emerald-500 to-teal-600 hover:from-emerald-400 hover:to-teal-500 text-white font-semibold transition transform hover:-translate-y-0.5 active:translate-y-0 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-offset-slate-900 focus:ring-emerald-500">
                {{index .T "Login"}}
            </button>
        </form>

        <div class="mt-6 pt-6 border-t border-slate-800 flex justify-center">
            <select onchange="location.href='?lang='+this.value" class="bg-slate-900 border border-slate-700 rounded-lg px-3 py-1.5 text-xs text-slate-300 focus:outline-none focus:border-emerald-500">
                <option value="en" {{if eq .Lang "en"}}selected{{end}}>English</option>
                <option value="fr" {{if eq .Lang "fr"}}selected{{end}}>Français</option>
                <option value="de" {{if eq .Lang "de"}}selected{{end}}>Deutsch</option>
                <option value="es" {{if eq .Lang "es"}}selected{{end}}>Español</option>
                <option value="pt-BR" {{if eq .Lang "pt-BR"}}selected{{end}}>Português (BR)</option>
                <option value="pt-PT" {{if eq .Lang "pt-PT"}}selected{{end}}>Português (PT)</option>
                <option value="zh" {{if eq .Lang "zh"}}selected{{end}}>简体中文</option>
                <option value="ja" {{if eq .Lang "ja"}}selected{{end}}>日本語</option>
            </select>
        </div>
    </div>
</body>
</html>`

const dashboardHTML = `<!DOCTYPE html>
<html lang="{{.Lang}}">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>XuruVoip - {{index .T "AdminPortal"}}</title>
    <link rel="icon" type="image/png" href="/admin/logo.png">
    <script src="https://cdn.tailwindcss.com"></script>
    <link href="https://fonts.googleapis.com/css2?family=Outfit:wght@300;400;500;600;700&display=swap" rel="stylesheet">
    <style>
        body {
            font-family: 'Outfit', sans-serif;
            background: #0b0f19;
            color: #e2e8f0;
        }
        .glass-panel {
            background: rgba(17, 24, 39, 0.4);
            backdrop-filter: blur(12px);
            border: 1px solid rgba(255, 255, 255, 0.05);
        }
        .custom-scroll::-webkit-scrollbar {
            width: 6px;
        }
        .custom-scroll::-webkit-scrollbar-track {
            background: rgba(255, 255, 255, 0.02);
        }
        .custom-scroll::-webkit-scrollbar-thumb {
            background: rgba(255, 255, 255, 0.1);
            border-radius: 3px;
        }
        .custom-scroll::-webkit-scrollbar-thumb:hover {
            background: rgba(255, 255, 255, 0.2);
        }
    </style>
</head>
<body class="min-h-screen bg-slate-950 flex flex-col">
    <!-- Navbar -->
    <header class="glass-panel sticky top-0 z-30 px-6 py-4 flex items-center justify-between border-b border-slate-800">
        <div class="flex items-center gap-3">
            <img src="/admin/logo.png" alt="XuruVoip Logo" class="w-10 h-10 rounded-lg border border-emerald-500/20 shadow-md shadow-emerald-500/5">
            <div>
                <h1 class="text-xl font-bold tracking-tight text-white flex items-center gap-2">
                    XuruVoip <span class="text-xs px-2 py-0.5 rounded-full bg-slate-800 text-slate-400 border border-slate-700">Admin</span>
                </h1>
                <p class="text-xs text-slate-400">{{index .T "RealTimeDashboard"}}</p>
            </div>
        </div>
        
        <div class="flex items-center gap-4">
            <div class="flex items-center gap-2 text-xs bg-slate-900 px-3 py-1.5 rounded-full border border-slate-800">
                <span id="ws-status-dot" class="w-2.5 h-2.5 rounded-full bg-red-500 shadow-sm animate-ping"></span>
                <span id="ws-status-text" class="text-slate-400 font-medium">{{index .T "Disconnected"}}</span>
            </div>
            <select onchange="location.href='?lang='+this.value" class="bg-slate-900 border border-slate-800 rounded-lg px-3 py-1.5 text-xs text-slate-300 focus:outline-none focus:border-emerald-500">
                <option value="en" {{if eq .Lang "en"}}selected{{end}}>English</option>
                <option value="fr" {{if eq .Lang "fr"}}selected{{end}}>Français</option>
                <option value="de" {{if eq .Lang "de"}}selected{{end}}>Deutsch</option>
                <option value="es" {{if eq .Lang "es"}}selected{{end}}>Español</option>
                <option value="pt-BR" {{if eq .Lang "pt-BR"}}selected{{end}}>Português (BR)</option>
                <option value="pt-PT" {{if eq .Lang "pt-PT"}}selected{{end}}>Português (PT)</option>
                <option value="zh" {{if eq .Lang "zh"}}selected{{end}}>简体中文</option>
                <option value="ja" {{if eq .Lang "ja"}}selected{{end}}>日本語</option>
            </select>
            <a href="/admin/logout" class="px-4 py-1.5 text-sm font-semibold rounded-lg bg-red-500/10 hover:bg-red-500/20 text-red-400 border border-red-500/20 transition">
                {{index .T "Logout"}}
            </a>
        </div>
    </header>

    <!-- Tab Selection Bar -->
    <div class="flex border-b border-slate-800 bg-slate-950 px-6 gap-2">
        <button onclick="switchTab('dashboard')" id="tab-btn-dashboard" class="px-5 py-3.5 text-sm font-semibold border-b-2 border-emerald-500 text-white transition">{{index .T "Dashboard"}}</button>
        <button onclick="switchTab('radar')" id="tab-btn-radar" class="px-5 py-3.5 text-sm font-semibold border-b-2 border-transparent text-slate-400 hover:text-white transition">{{index .T "RadarMap"}}</button>
        <button onclick="switchTab('accounts')" id="tab-btn-accounts" class="px-5 py-3.5 text-sm font-semibold border-b-2 border-transparent text-slate-400 hover:text-white transition">{{index .T "PlayerAccounts"}}</button>
        <button onclick="switchTab('admins')" id="tab-btn-admins" class="px-5 py-3.5 text-sm font-semibold border-b-2 border-transparent text-slate-400 hover:text-white transition">{{index .T "Administrators"}}</button>
        <button onclick="switchTab('bans')" id="tab-btn-bans" class="px-5 py-3.5 text-sm font-semibold border-b-2 border-transparent text-slate-400 hover:text-white transition">{{index .T "ActiveBans"}}</button>
    </div>

    <!-- Tab 1: Dashboard Panel -->
    <div id="tab-content-dashboard" class="flex-1 min-h-0 flex flex-col">
        <main class="flex-1 p-6 grid grid-cols-1 xl:grid-cols-4 gap-6 max-w-[1800px] w-full mx-auto">
            
            <!-- Left Sidebar: Config & Stats -->
            <div class="xl:col-span-1 space-y-6 flex flex-col">
                <!-- Stats Summary -->
                <div class="glass-panel p-5 rounded-2xl space-y-4">
                    <h3 class="text-sm font-semibold text-slate-400 uppercase tracking-wider">{{index .T "ServerStatus"}}</h3>
                    <div class="grid grid-cols-2 gap-4">
                        <div class="bg-slate-900/60 p-4 rounded-xl border border-slate-800/80">
                            <div class="text-2xl font-bold text-white" id="stat-players">0</div>
                            <div class="text-xs text-slate-400 mt-1">{{index .T "OnlinePlayers"}}</div>
                        </div>
                        <div class="bg-slate-900/60 p-4 rounded-xl border border-slate-800/80">
                            <div class="text-2xl font-bold text-white" id="stat-channels">0</div>
                            <div class="text-xs text-slate-400 mt-1">{{index .T "RadioChannels"}}</div>
                        </div>
                        <div class="bg-slate-900/60 p-4 rounded-xl border border-slate-800/80">
                            <div class="text-2xl font-bold text-white" id="stat-profiles">0</div>
                            <div class="text-xs text-slate-400 mt-1">{{index .T "AudioProfiles"}}</div>
                        </div>
                    </div>
                </div>

                <!-- Server Settings Form -->
                <div class="glass-panel p-5 rounded-2xl space-y-4 flex-1">
                    <h3 class="text-sm font-semibold text-slate-400 uppercase tracking-wider">{{index .T "Passwords"}}</h3>
                    <div class="space-y-4">
                        <div class="p-4 bg-slate-900/60 rounded-xl border border-slate-800/80 space-y-2">
                            <label class="text-xs font-medium text-slate-400 block">{{index .T "PlayerConnToken"}}</label>
                            <div class="flex gap-2">
                                <input type="text" id="server-token-input" placeholder="{{index .T "NewPlayerToken"}}" class="w-full px-3 py-1.5 text-sm rounded bg-slate-950 border border-slate-800 text-white placeholder-slate-600 focus:outline-none focus:border-emerald-500">
                                <button onclick="updateServerToken()" class="px-3 py-1.5 text-xs bg-emerald-500 text-slate-950 font-bold rounded hover:bg-emerald-400 transition">{{index .T "Save"}}</button>
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            <!-- Center Area: Players List (Span 2) -->
            <div class="xl:col-span-2 flex flex-col space-y-6">
                <div class="glass-panel p-5 rounded-2xl flex-1 flex flex-col">
                    <div class="flex items-center justify-between mb-4">
                        <div>
                            <h3 class="text-lg font-bold text-white">{{index .T "ActivePlayers"}}</h3>
                            <p class="text-xs text-slate-400">{{index .T "RealTimeList"}}</p>
                        </div>
                        <div class="text-xs text-slate-500" id="players-count-badge">0 / 64 {{index .T "PlayersCountBadge"}}</div>
                    </div>

                    <div class="flex-1 overflow-x-auto custom-scroll">
                        <table class="w-full text-left text-sm border-collapse">
                            <thead>
                                <tr class="border-b border-slate-800 text-slate-400 text-xs uppercase tracking-wider">
                                    <th class="py-3 px-4">{{index .T "HeaderPlayer"}}</th>
                                    <th class="py-3 px-4">{{index .T "HeaderActiveChannel"}}</th>
                                    <th class="py-3 px-4">{{index .T "HeaderListeningChannels"}}</th>
                                    <th class="py-3 px-4">{{index .T "HeaderProfile"}}</th>
                                    <th class="py-3 px-4">{{index .T "HeaderStatus"}}</th>
                                    <th class="py-3 px-4 text-right">{{index .T "HeaderActions"}}</th>
                                </tr>
                            </thead>
                            <tbody id="players-table-body" class="divide-y divide-slate-900/50">
                                <tr id="no-players-row">
                                    <td colspan="6" class="py-8 text-center text-slate-500">{{index .T "NoPlayersConnected"}}</td>
                                </tr>
                            </tbody>
                        </table>
                    </div>
                </div>
            </div>

            <!-- Right Sidebar: Channels, Profiles & Logs -->
            <div class="xl:col-span-1 space-y-6 flex flex-col">
                <div class="glass-panel p-5 rounded-2xl space-y-4">
                    <h3 class="text-sm font-semibold text-slate-400 uppercase tracking-wider flex justify-between items-center">
                        {{index .T "RadioChannels"}}
                        <button onclick="showAddChannelPrompt()" class="text-xs text-emerald-400 hover:text-emerald-300 font-bold">+ {{index .T "Add"}}</button>
                    </h3>
                    <div id="channels-list-container" class="space-y-2 max-h-[150px] overflow-y-auto custom-scroll pr-1"></div>
                </div>

                <div class="glass-panel p-5 rounded-2xl space-y-4">
                    <h3 class="text-sm font-semibold text-slate-400 uppercase tracking-wider flex justify-between items-center">
                        {{index .T "AudioProfiles"}}
                        <button onclick="showAddProfilePrompt()" class="text-xs text-purple-400 hover:text-purple-300 font-bold">+ {{index .T "Add"}}</button>
                    </h3>
                    <div id="profiles-list-container" class="space-y-2 max-h-[150px] overflow-y-auto custom-scroll pr-1"></div>
                </div>

                <div class="glass-panel p-5 rounded-2xl flex-1 flex flex-col min-h-[300px]">
                    <div class="flex items-center justify-between mb-3 border-b border-slate-800/80 pb-2">
                        <h3 class="text-sm font-semibold text-slate-400 uppercase tracking-wider">{{index .T "LiveLogs"}}</h3>
                        <div class="flex gap-2">
                            <input type="text" id="logs-filter" placeholder="{{index .T "Filter"}}" class="px-2 py-0.5 text-xs rounded bg-slate-900 border border-slate-800 text-white focus:outline-none focus:border-emerald-500">
                            <button onclick="clearLogs()" class="text-xs text-slate-500 hover:text-slate-300">{{index .T "Clear"}}</button>
                        </div>
                    </div>
                    <div id="logs-console" class="flex-1 font-mono text-xs overflow-y-auto bg-slate-950 p-3 rounded-lg border border-slate-900 custom-scroll space-y-1 select-text"></div>
                </div>
            </div>
        </main>
    </div>

    <!-- Tab: Radar Map -->
    <div id="tab-content-radar" class="hidden flex-1 min-h-0 flex flex-col p-6 max-w-[1800px] w-full mx-auto space-y-6">
        <div class="glass-panel p-6 rounded-2xl flex-1 flex flex-col min-h-[600px] relative">
            <div class="flex items-center justify-between mb-4 flex-wrap gap-4">
                <div>
                    <h3 class="text-lg font-bold text-white">{{index .T "RadarMap"}}</h3>
                    <p class="text-xs text-slate-400">Real-time player coordinates map grouped by Zone</p>
                </div>
                <!-- Controls -->
                <div class="flex items-center gap-3">
                    <span class="text-xs font-semibold text-slate-400">Zone Filter:</span>
                    <select id="radar-zone-select" class="bg-slate-900 border border-slate-800 rounded-lg px-3 py-1.5 text-xs text-slate-300 focus:outline-none focus:border-emerald-500" onchange="onRadarZoneChange()">
                        <option value="">All Zones</option>
                    </select>
                    
                    <button onclick="zoomRadar(1.2)" class="px-3 py-1.5 text-xs bg-slate-800 text-white rounded border border-slate-700 hover:bg-slate-700 transition">Zoom +</button>
                    <button onclick="zoomRadar(0.8)" class="px-3 py-1.5 text-xs bg-slate-800 text-white rounded border border-slate-700 hover:bg-slate-700 transition">Zoom -</button>
                    <button onclick="resetRadar()" class="px-3 py-1.5 text-xs bg-slate-800 text-white rounded border border-slate-700 hover:bg-slate-700 transition">Reset View</button>
                </div>
            </div>
            
            <!-- Canvas container -->
            <div class="flex-1 min-h-0 bg-slate-950/50 rounded-xl border border-slate-900 overflow-hidden relative flex items-center justify-center">
                <canvas id="radar-canvas" class="w-full h-full cursor-grab active:cursor-grabbing"></canvas>
            </div>
        </div>
    </div>

    <!-- Tab 2: User Accounts Panel -->
    <div id="tab-content-accounts" class="hidden p-6 max-w-[1800px] w-full mx-auto space-y-6 flex-1">
        <div class="glass-panel p-6 rounded-2xl space-y-4 flex flex-col h-full">
            <div class="flex items-center justify-between">
                <div>
                    <h3 class="text-xl font-bold text-white">{{index .T "PlayerAccounts"}}</h3>
                    <p class="text-xs text-slate-400">{{index .T "PlayerAccountsDesc"}}</p>
                </div>
                <input type="text" id="accounts-search" placeholder="{{index .T "SearchNickname"}}" oninput="filterAccountsTable()" class="px-4 py-2 text-sm rounded bg-slate-900 border border-slate-800 text-white focus:outline-none focus:border-emerald-500">
            </div>
            
            <div class="overflow-x-auto custom-scroll max-h-[600px] flex-1">
                <table class="w-full text-left text-sm border-collapse">
                    <thead>
                        <tr class="border-b border-slate-800 text-slate-400 text-xs uppercase tracking-wider">
                            <th class="py-3 px-4">{{index .T "HeaderNickname"}}</th>
                            <th class="py-3 px-4">{{index .T "HeaderActiveChannel"}}</th>
                            <th class="py-3 px-4">{{index .T "HeaderListeningChannels"}}</th>
                            <th class="py-3 px-4">{{index .T "HeaderProfile"}}</th>
                            <th class="py-3 px-4">{{index .T "HeaderStatus"}}</th>
                            <th class="py-3 px-4">{{index .T "HeaderLastIP"}}</th>
                            <th class="py-3 px-4">{{index .T "HeaderHWID"}}</th>
                            <th class="py-3 px-4">{{index .T "HeaderCreatedAt"}}</th>
                            <th class="py-3 px-4">{{index .T "HeaderLastModified"}}</th>
                            <th class="py-3 px-4 text-right">{{index .T "HeaderActions"}}</th>
                        </tr>
                    </thead>
                    <tbody id="accounts-table-body" class="divide-y divide-slate-900/50"></tbody>
                </table>
            </div>
        </div>
    </div>

    <!-- Tab 3: Administrators Panel -->
    <div id="tab-content-admins" class="hidden p-6 max-w-[1200px] w-full mx-auto grid grid-cols-1 md:grid-cols-3 gap-6 flex-1">
        <div class="md:col-span-2 glass-panel p-6 rounded-2xl space-y-4 flex flex-col h-full">
            <h3 class="text-xl font-bold text-white">{{index .T "Administrators"}}</h3>
            <div class="overflow-x-auto custom-scroll flex-1">
                <table class="w-full text-left text-sm border-collapse">
                    <thead>
                        <tr class="border-b border-slate-800 text-slate-400 text-xs uppercase tracking-wider">
                            <th class="py-3 px-4">{{index .T "HeaderUsername"}}</th>
                            <th class="py-3 px-4">{{index .T "HeaderCreatedAt"}}</th>
                            <th class="py-3 px-4 text-right">{{index .T "HeaderActions"}}</th>
                        </tr>
                    </thead>
                    <tbody id="admins-table-body" class="divide-y divide-slate-900/50"></tbody>
                </table>
            </div>
        </div>
        
        <div class="md:col-span-1 space-y-6">
            <div class="glass-panel p-6 rounded-2xl space-y-4">
                <h3 class="text-lg font-bold text-white">{{index .T "CreateAdmin"}}</h3>
                <div class="space-y-3">
                    <div>
                        <label class="text-xs font-semibold text-slate-400 block mb-1">{{index .T "Username"}}</label>
                        <input type="text" id="new-admin-user" class="w-full px-3 py-2 text-sm rounded bg-slate-950 border border-slate-800 text-white focus:outline-none focus:border-emerald-500">
                    </div>
                    <div>
                        <label class="text-xs font-semibold text-slate-400 block mb-1">{{index .T "Password"}}</label>
                        <input type="password" id="new-admin-pass" class="w-full px-3 py-2 text-sm rounded bg-slate-950 border border-slate-800 text-white focus:outline-none focus:border-emerald-500">
                    </div>
                    <button onclick="createAdminAccount()" class="w-full py-2 bg-emerald-500 text-slate-950 font-bold rounded hover:bg-emerald-400 transition text-sm">
                        {{index .T "CreateAccount"}}
                    </button>
                </div>
            </div>

            <div class="glass-panel p-6 rounded-2xl space-y-4">
                <h3 class="text-lg font-bold text-white">{{index .T "ChangePassword"}}</h3>
                <div class="space-y-3">
                    <div>
                        <label class="text-xs font-semibold text-slate-400 block mb-1">{{index .T "Username"}}</label>
                        <input type="text" id="change-admin-user" placeholder="admin" class="w-full px-3 py-2 text-sm rounded bg-slate-950 border border-slate-800 text-white focus:outline-none focus:border-purple-500">
                    </div>
                    <div>
                        <label class="text-xs font-semibold text-slate-400 block mb-1">{{index .T "Password"}}</label>
                        <input type="password" id="change-admin-pass" class="w-full px-3 py-2 text-sm rounded bg-slate-950 border border-slate-800 text-white focus:outline-none focus:border-purple-500">
                    </div>
                    <button onclick="changeAdminPassword()" class="w-full py-2 bg-purple-500 text-white font-bold rounded hover:bg-purple-400 transition text-sm">
                        {{index .T "Update"}}
                    </button>
                </div>
            </div>
        </div>
    </div>

    <!-- Tab 4: Active Bans Panel -->
    <div id="tab-content-bans" class="hidden p-6 max-w-[1800px] w-full mx-auto grid grid-cols-1 lg:grid-cols-2 gap-6 flex-1">
        <!-- Col 1: Banned IPs -->
        <div class="glass-panel p-6 rounded-2xl space-y-4 flex flex-col h-full">
            <div class="flex items-center justify-between">
                <div>
                    <h3 class="text-xl font-bold text-white">{{index .T "IPAddressBans"}}</h3>
                    <p class="text-xs text-slate-400">{{index .T "BannedIPListDesc"}}</p>
                </div>
                <button onclick="addBannedIPPrompt()" class="px-3 py-1.5 text-xs font-semibold bg-red-500/10 hover:bg-red-500/20 border border-red-500/20 text-red-400 rounded-lg transition">+ {{index .T "BanAnIP"}}</button>
            </div>
            
            <div class="overflow-x-auto custom-scroll max-h-[500px] flex-1">
                <table class="w-full text-left text-sm border-collapse">
                    <thead>
                        <tr class="border-b border-slate-800 text-slate-400 text-xs uppercase tracking-wider">
                            <th class="py-3 px-4">{{index .T "IPAddress"}}</th>
                            <th class="py-3 px-4">{{index .T "Reason"}}</th>
                            <th class="py-3 px-4">{{index .T "HeaderBannedAt"}}</th>
                            <th class="py-3 px-4 text-right">{{index .T "HeaderActions"}}</th>
                        </tr>
                    </thead>
                    <tbody id="banned-ips-table-body" class="divide-y divide-slate-900/50"></tbody>
                </table>
            </div>
        </div>

        <!-- Col 2: Banned HWIDs -->
        <div class="glass-panel p-6 rounded-2xl space-y-4 flex flex-col h-full">
            <div class="flex items-center justify-between">
                <div>
                    <h3 class="text-xl font-bold text-white">{{index .T "HardwareBans"}}</h3>
                    <p class="text-xs text-slate-400">{{index .T "BannedHwidListDesc"}}</p>
                </div>
                <button onclick="addBannedHwidPrompt()" class="px-3 py-1.5 text-xs font-semibold bg-red-500/10 hover:bg-red-500/20 border border-red-500/20 text-red-400 rounded-lg transition">+ {{index .T "BanAHwid"}}</button>
            </div>
            
            <div class="overflow-x-auto custom-scroll max-h-[500px] flex-1">
                <table class="w-full text-left text-sm border-collapse">
                    <thead>
                        <tr class="border-b border-slate-800 text-slate-400 text-xs uppercase tracking-wider">
                            <th class="py-3 px-4">{{index .T "HWIDIdentifier"}}</th>
                            <th class="py-3 px-4">{{index .T "Reason"}}</th>
                            <th class="py-3 px-4">{{index .T "HeaderBannedAt"}}</th>
                            <th class="py-3 px-4 text-right">{{index .T "HeaderActions"}}</th>
                        </tr>
                    </thead>
                    <tbody id="banned-hwids-table-body" class="divide-y divide-slate-900/50"></tbody>
                </table>
            </div>
        </div>
    </div>

    <!-- UI Prompts / Modals -->
    <div id="prompt-modal" class="hidden fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm">
        <div class="bg-slate-900 border border-slate-800 p-6 rounded-2xl w-full max-w-sm shadow-2xl">
            <h4 class="text-lg font-bold text-white mb-2" id="prompt-title">{{index .T "Title"}}</h4>
            <p class="text-xs text-slate-400 mb-4" id="prompt-desc">{{index .T "Description"}}</p>
            <input type="text" id="prompt-input" class="w-full px-3 py-2 rounded bg-slate-950 border border-slate-800 text-white mb-4 focus:outline-none focus:ring-1 focus:ring-emerald-500">
            <div class="flex justify-end gap-2 text-sm font-semibold">
                <button onclick="hidePrompt()" class="px-4 py-2 rounded bg-slate-800 hover:bg-slate-700 text-slate-300">{{index .T "Cancel"}}</button>
                <button id="prompt-submit-btn" class="px-4 py-2 rounded bg-emerald-500 text-slate-950 font-bold hover:bg-emerald-400">{{index .T "Validate"}}</button>
            </div>
        </div>
    </div>

    <!-- Notification Toast -->
    <div id="toast" class="fixed bottom-5 right-5 z-50 transform translate-y-20 opacity-0 transition duration-300 flex items-center p-4 rounded-xl border max-w-md shadow-2xl">
        <div class="mr-3 p-1 rounded-full text-slate-950 font-bold" id="toast-icon"></div>
        <div>
            <p class="text-sm font-bold text-white" id="toast-title">{{index .T "Notification"}}</p>
            <p class="text-xs text-slate-300 mt-0.5" id="toast-msg"></p>
        </div>
    </div>

    <!-- Scripts -->
    <script>
        let ws;
        let channels = [];
        let profiles = [];
        let players = {};
        let anonymousMode = false;
        let activeTab = 'dashboard';

        // Inject translation JSON
        const T = {{.TranslationsJSON}};

        function switchTab(tabId) {
            document.getElementById('tab-content-dashboard').classList.add('hidden');
            document.getElementById('tab-content-radar').classList.add('hidden');
            document.getElementById('tab-content-accounts').classList.add('hidden');
            document.getElementById('tab-content-admins').classList.add('hidden');
            document.getElementById('tab-content-bans').classList.add('hidden');

            document.getElementById('tab-btn-dashboard').className = 'px-5 py-3.5 text-sm font-semibold border-b-2 border-transparent text-slate-400 hover:text-white transition';
            document.getElementById('tab-btn-radar').className = 'px-5 py-3.5 text-sm font-semibold border-b-2 border-transparent text-slate-400 hover:text-white transition';
            document.getElementById('tab-btn-accounts').className = 'px-5 py-3.5 text-sm font-semibold border-b-2 border-transparent text-slate-400 hover:text-white transition';
            document.getElementById('tab-btn-admins').className = 'px-5 py-3.5 text-sm font-semibold border-b-2 border-transparent text-slate-400 hover:text-white transition';
            document.getElementById('tab-btn-bans').className = 'px-5 py-3.5 text-sm font-semibold border-b-2 border-transparent text-slate-400 hover:text-white transition';

            document.getElementById('tab-content-' + tabId).classList.remove('hidden');
            document.getElementById('tab-btn-' + tabId).className = 'px-5 py-3.5 text-sm font-semibold border-b-2 border-emerald-500 text-white transition';

            activeTab = tabId;
            if (tabId === 'accounts') {
                sendAdminCommand({ cmd: 'get_players_list' });
            } else if (tabId === 'admins') {
                sendAdminCommand({ cmd: 'get_admins_list' });
            } else if (tabId === 'bans') {
                sendAdminCommand({ cmd: 'get_banned_ips_list' });
                sendAdminCommand({ cmd: 'get_banned_hwids_list' });
            } else if (tabId === 'radar') {
                // Resize and draw radar map immediately
                if (radarCanvas) {
                    radarCanvas.width = radarCanvas.parentElement.clientWidth * window.devicePixelRatio;
                    radarCanvas.height = radarCanvas.parentElement.clientHeight * window.devicePixelRatio;
                    drawRadar();
                }
            }
        }

        // Connect to administration websocket
        function connectWS() {
            const protocol = window.location.protocol === 'https:' ? 'wss:' : 'ws:';
            const wsUrl = protocol + '//' + window.location.host + '/admin/ws';
            
            updateConnectionStatus(false, T.Connecting || 'Connecting...');
            ws = new WebSocket(wsUrl);

            ws.onopen = () => {
                updateConnectionStatus(true, T.Connected || 'Connected');
                clearLogs();
                appendLocalLog(T.AdminWelcomeMsg || 'Connection to XuruVoip server established.', 'text-emerald-400 font-bold');
                
                // Fetch dynamic DB panels
                sendAdminCommand({ cmd: 'get_players_list' });
                sendAdminCommand({ cmd: 'get_admins_list' });
            };

            ws.onclose = () => {
                updateConnectionStatus(false, T.Disconnected || 'Disconnected');
                appendLocalLog(T.ConnectionLostMsg || 'Connection to server lost. Reconnecting...', 'text-red-400 font-bold');
                setTimeout(connectWS, 3000);
            };

            ws.onerror = (err) => {
                console.error('WS Error:', err);
            };

            ws.onmessage = (event) => {
                let data;
                try {
                    data = JSON.parse(event.data);
                } catch (e) {
                    console.error('Error parsing message:', e);
                    return;
                }

                handleServerMessage(data);
            };
        }

        function updateConnectionStatus(isConnected, text) {
            const dot = document.getElementById('ws-status-dot');
            const label = document.getElementById('ws-status-text');
            if (isConnected) {
                dot.className = 'w-2.5 h-2.5 rounded-full bg-emerald-500 shadow-emerald-500/50 shadow-md';
                label.innerText = text;
                label.className = 'text-emerald-400 font-medium';
            } else {
                dot.className = 'w-2.5 h-2.5 rounded-full bg-red-500 shadow-sm animate-ping';
                label.innerText = text;
                label.className = 'text-red-400 font-medium';
            }
        }

        // Handle incoming Web admin payloads
        function handleServerMessage(msg) {
            switch(msg.type) {
                case 'admin_welcome':
                    channels = msg.channels || [];
                    profiles = msg.profiles || [];
                    anonymousMode = msg.anonymous_mode;
                    
                    players = {};
                    if (msg.players) {
                        msg.players.forEach(p => {
                            players[p.name] = p;
                        });
                    }
                    
                    renderStats();
                    renderChannels();
                    renderProfiles();
                    renderPlayers();
                    updateRadarZonesDropdown();
                    break;

                case 'channels_list':
                    channels = msg.channels || [];
                    renderChannels();
                    renderPlayers();
                    renderStats();
                    break;

                case 'profiles_list':
                    profiles = msg.profiles || [];
                    renderProfiles();
                    renderPlayers();
                    renderStats();
                    break;

                case 'anonymous_mode':
                    anonymousMode = msg.active;
                    renderStats();
                    break;

                case 'join':
                    players[msg.name] = {
                        name: msg.name,
                        active_channel: msg.active_channel,
                        listening_channels: msg.listening_channels || [],
                        profile: msg.profile,
                        prox_short: msg.prox_short,
                        sc_online: false,
                        helmet_on: false,
                        pos: null
                    };
                    renderPlayers();
                    renderStats();
                    renderChannels();
                    renderProfiles();
                    updateRadarZonesDropdown();
                    break;

                case 'leave':
                    delete players[msg.name];
                    renderPlayers();
                    renderStats();
                    renderChannels();
                    renderProfiles();
                    updateRadarZonesDropdown();
                    break;

                case 'pos':
                    if (players[msg.name]) {
                        const oldZone = players[msg.name].pos ? players[msg.name].pos.zone : "";
                        players[msg.name].pos = msg.pos;
                        if (msg.pos && msg.pos.zone !== oldZone) {
                            updateRadarZonesDropdown();
                        }
                    }
                    break;

                case 'sc_online':
                    if (players[msg.name]) {
                        players[msg.name].sc_online = true;
                        updatePlayerRowState(msg.name);
                    }
                    break;

                case 'sc_offline':
                    if (players[msg.name]) {
                        players[msg.name].sc_online = false;
                        updatePlayerRowState(msg.name);
                    }
                    break;

                case 'helmet':
                    if (players[msg.name]) {
                        players[msg.name].helmet_on = msg.helmet_on;
                        updatePlayerRowState(msg.name);
                    }
                    break;

                case 'player_channel':
                    if (players[msg.name]) {
                        players[msg.name].active_channel = msg.channel;
                        updatePlayerRowSelectors(msg.name);
                        renderChannels();
                    }
                    break;

                case 'player_listening':
                    if (players[msg.name]) {
                        players[msg.name].listening_channels = msg.channels || [];
                        updatePlayerRowListening(msg.name);
                    }
                    break;

                case 'player_profile':
                    if (players[msg.name]) {
                        players[msg.name].profile = msg.profile;
                        updatePlayerRowSelectors(msg.name);
                        renderProfiles();
                    }
                    break;

                case 'player_prox_short':
                    if (players[msg.name]) {
                        players[msg.name].prox_short = msg.active;
                        updatePlayerRowState(msg.name);
                    }
                    break;

                case 'admin_refresh':
                    if (msg.tab === 'accounts' && activeTab === 'accounts') {
                        sendAdminCommand({ cmd: 'get_players_list' });
                    } else if (msg.tab === 'admins' && activeTab === 'admins') {
                        sendAdminCommand({ cmd: 'get_admins_list' });
                    } else if (msg.tab === 'bans' && activeTab === 'bans') {
                        sendAdminCommand({ cmd: 'get_banned_ips_list' });
                        sendAdminCommand({ cmd: 'get_banned_hwids_list' });
                    }
                    break;

                case 'log':
                    appendServerLog(msg.ts, msg.msg, msg.color);
                    break;

                case 'admin_response':
                    showToast(msg.cmd, msg.ok, msg.reason);
                    if (msg.ok && msg.value !== undefined && msg.value !== null) {
                        if (msg.cmd === 'get_players_list') {
                            renderAccountsList(msg.value);
                        } else if (msg.cmd === 'get_admins_list') {
                            renderAdminsList(msg.value);
                        } else if (msg.cmd === 'get_banned_ips_list') {
                            renderBannedIPsList(msg.value);
                        } else if (msg.cmd === 'get_banned_hwids_list') {
                            renderBannedHwidsList(msg.value);
                        }
                    }
                    // Trigger lists refresh on structural changes
                    if (msg.ok) {
                        if (['ban_player', 'delete_player', 'reset_player_password'].includes(msg.cmd)) {
                            sendAdminCommand({ cmd: 'get_players_list' });
                            if (activeTab === 'bans') {
                                sendAdminCommand({ cmd: 'get_banned_ips_list' });
                                sendAdminCommand({ cmd: 'get_banned_hwids_list' });
                            }
                        } else if (['create_admin', 'delete_admin', 'change_admin_password'].includes(msg.cmd)) {
                            sendAdminCommand({ cmd: 'get_admins_list' });
                        } else if (['add_banned_ip', 'remove_banned_ip'].includes(msg.cmd)) {
                            sendAdminCommand({ cmd: 'get_banned_ips_list' });
                        } else if (['add_banned_hwid', 'remove_banned_hwid'].includes(msg.cmd)) {
                            sendAdminCommand({ cmd: 'get_banned_hwids_list' });
                        }
                    }
                    break;
            }
        }

        // Render Stats Card
        function renderStats() {
            document.getElementById('stat-players').innerText = Object.keys(players).length;
            document.getElementById('stat-channels').innerText = channels.length;
            document.getElementById('stat-profiles').innerText = profiles.length;
            
            // Anonymous mode UI disabled for now
        }

        // Render Channels
        function renderChannels() {
            const container = document.getElementById('channels-list-container');
            container.innerHTML = '';
            
            channels.forEach(ch => {
                const div = document.createElement('div');
                div.className = 'flex items-center justify-between p-2.5 bg-slate-900/60 rounded-lg border border-slate-800/80 hover:border-slate-700 transition';
                
                let playerInCh = 0;
                Object.values(players).forEach(p => {
                    if (p.active_channel === ch) playerInCh++;
                });

                let delBtn = '';
                if (ch !== 'General') {
                    delBtn = '<button onclick="removeChannel(\'' + ch + '\')" class="text-xs text-red-500 hover:text-red-400 transition">' + (T.Del || 'Del') + '</button>';
                }

                div.innerHTML = 
                    '<div class="flex items-center gap-2">' +
                        '<span class="text-sm font-medium text-slate-200">' + (ch === 'General' ? (T.General || 'General') : ch) + '</span>' +
                        '<span class="text-[10px] px-1.5 py-0.5 rounded-full bg-slate-800 text-slate-400 border border-slate-700">' + playerInCh + ' ' + (T.OnlinePlayers || 'players') + '</span>' +
                    '</div>' +
                    '<div class="flex gap-2">' +
                        '<button onclick="showRenameChannelPrompt(\'' + ch + '\')" class="text-xs text-slate-400 hover:text-white transition">' + (T.Rename || 'Rename') + '</button>' +
                        delBtn +
                    '</div>';
                container.appendChild(div);
            });
        }

        // Render Profiles
        function renderProfiles() {
            const container = document.getElementById('profiles-list-container');
            container.innerHTML = '';
            
            profiles.forEach(pr => {
                const div = document.createElement('div');
                div.className = 'flex items-center justify-between p-2.5 bg-slate-900/60 rounded-lg border border-slate-800/80 hover:border-slate-700 transition';
                
                let playerInPr = 0;
                Object.values(players).forEach(p => {
                    if (p.profile === pr) playerInPr++;
                });

                div.innerHTML = 
                    '<div class="flex items-center gap-2">' +
                        '<span class="text-sm font-medium text-slate-200">' + pr + '</span>' +
                        '<span class="text-[10px] px-1.5 py-0.5 rounded-full bg-slate-800 text-slate-400 border border-slate-700">' + playerInPr + ' ' + (T.AudioProfiles || 'roles') + '</span>' +
                    '</div>' +
                    '<div class="flex gap-2">' +
                        '<button onclick="showRenameProfilePrompt(\'' + pr + '\')" class="text-xs text-slate-400 hover:text-white transition">' + (T.Rename || 'Rename') + '</button>' +
                        '<button onclick="removeProfile(\'' + pr + '\')" class="text-xs text-red-500 hover:text-red-400 transition">' + (T.Del || 'Del') + '</button>' +
                    '</div>';
                container.appendChild(div);
            });
        }

        // Render Players Table
        function renderPlayers() {
            const tbody = document.getElementById('players-table-body');
            tbody.innerHTML = '';
            
            const playerList = Object.values(players);
            document.getElementById('players-count-badge').innerText = playerList.length + ' / 64 ' + (T.PlayersCountBadge || 'Players');

            if (playerList.length === 0) {
                tbody.innerHTML = 
                    '<tr id="no-players-row">' +
                        '<td colspan="6" class="py-8 text-center text-slate-500">' + (T.NoPlayersConnected || 'No players currently connected.') + '</td>' +
                    '</tr>';
                return;
            }

            playerList.forEach(p => {
                const tr = document.createElement('tr');
                tr.id = 'player-row-' + p.name;
                tr.className = 'border-b border-slate-900/50 hover:bg-slate-900/10 transition';
                
                let chOptions = '<option value="">(' + (T.Optional || 'None') + ')</option>';
                channels.forEach(ch => {
                    const sel = p.active_channel === ch ? 'selected' : '';
                    chOptions += '<option value="' + ch + '" ' + sel + '>' + (ch === 'General' ? (T.General || 'General') : ch) + '</option>';
                });

                let prOptions = '<option value="">(' + (T.Optional || 'None') + ')</option>';
                profiles.forEach(pr => {
                    const sel = p.profile === pr ? 'selected' : '';
                    prOptions += '<option value="' + pr + '" ' + sel + '>' + pr + '</option>';
                });

                let listenListHtml = '';
                channels.forEach(ch => {
                    const checked = p.listening_channels && p.listening_channels.includes(ch) ? 'checked' : '';
                    listenListHtml += 
                        '<label class="flex items-center gap-1.5 text-xs text-slate-300 hover:text-white cursor-pointer select-none">' +
                            '<input type="checkbox" data-channel="' + ch + '" ' + checked + ' onchange="togglePlayerListening(\'' + p.name + '\', \'' + ch + '\', this.checked)" class="rounded bg-slate-950 border-slate-800 text-emerald-500 focus:ring-0 focus:ring-offset-0">' +
                            '<span>' + (ch === 'General' ? (T.General || 'General') : ch) + '</span>' +
                        '</label>';
                });

                const helmetHtml = p.helmet_on 
                    ? '<span class="px-2 py-0.5 rounded text-[10px] bg-emerald-500/10 text-emerald-400 border border-emerald-500/20 font-bold">' + (T.HELMET || 'HELMET') + '</span>' 
                    : '<span class="px-2 py-0.5 rounded text-[10px] bg-slate-800 text-slate-500 border border-slate-700">' + (T.NOHELMET || 'NO HELMET') + '</span>';

                const proxHtml = p.prox_short
                    ? '<span class="px-2 py-0.5 rounded text-[10px] bg-amber-500/10 text-amber-400 border border-amber-500/20 font-bold">5m</span>'
                    : '<span class="px-2 py-0.5 rounded text-[10px] bg-blue-500/10 text-blue-400 border border-blue-500/20 font-bold">50m</span>';

                const scHtml = p.sc_online
                    ? '<span class="inline-flex items-center gap-1"><span class="w-2 h-2 rounded-full bg-emerald-500"></span><span class="text-[10px] text-emerald-400 font-bold">' + (T.GAME || 'GAME') + '</span></span>'
                    : '<span class="inline-flex items-center gap-1"><span class="w-2 h-2 rounded-full bg-slate-600"></span><span class="text-[10px] text-slate-500">' + (T.OFFLINE || 'OFFLINE') + '</span></span>';

                const containerName = p.pos ? (p.pos.container_name || p.pos.container_id || (T.Global || 'Global')) : (T.NoPosition || 'No position');

                tr.innerHTML = 
                    '<td class="py-3.5 px-4 font-semibold text-white">' +
                        '<div>' +
                            '<span>' + p.name + '</span>' +
                            '<div class="text-[10px] text-slate-500 font-normal mt-0.5" id="player-subinfo-' + p.name + '">' +
                                (T.Container || 'Container') + ': ' + containerName +
                            '</div>' +
                        '</div>' +
                    '</td>' +
                    '<td class="py-3.5 px-4">' +
                        '<select onchange="assignPlayerChannel(\'' + p.name + '\', this.value)" class="bg-slate-900 border border-slate-800 rounded px-2 py-1 text-xs text-slate-200 focus:outline-none focus:border-emerald-500" id="player-sel-ch-' + p.name + '">' +
                            chOptions +
                        '</select>' +
                    '</td>' +
                    '<td class="py-3.5 px-4">' +
                        '<div class="flex flex-wrap gap-x-3 gap-y-1.5 max-w-[250px]" id="player-listening-' + p.name + '">' +
                            listenListHtml +
                        '</div>' +
                    '</td>' +
                    '<td class="py-3.5 px-4">' +
                        '<select onchange="assignPlayerProfile(\'' + p.name + '\', this.value)" class="bg-slate-900 border border-slate-800 rounded px-2 py-1 text-xs text-slate-200 focus:outline-none focus:border-purple-500" id="player-sel-pr-' + p.name + '">' +
                            prOptions +
                        '</select>' +
                    '</td>' +
                    '<td class="py-3.5 px-4">' +
                        '<div class="flex flex-col gap-1.5 items-start" id="player-badges-' + p.name + '">' +
                            '<div class="flex gap-1.5">' +
                                helmetHtml +
                                proxHtml +
                            '</div>' +
                            scHtml +
                        '</div>' +
                    '</td>' +
                    '<td class="py-3.5 px-4 text-right">' +
                        '<button onclick="kickPlayer(\'' + p.name + '\')" class="px-2.5 py-1 text-xs font-semibold bg-red-500/10 hover:bg-red-500/20 border border-red-500/20 text-red-400 rounded-lg transition">' +
                            (T.Kick || 'Kick') +
                        '</button>' +
                    '</td>';
                tbody.appendChild(tr);
            });
        }

        // Fast update row elements to avoid full table re-render and lose focus/scroll
        function updatePlayerRowState(name) {
            const p = players[name];
            if (!p) return;

            const badgeContainer = document.getElementById('player-badges-' + name);
            const subInfo = document.getElementById('player-subinfo-' + name);
            
            if (badgeContainer) {
                const helmetHtml = p.helmet_on 
                    ? '<span class="px-2 py-0.5 rounded text-[10px] bg-emerald-500/10 text-emerald-400 border border-emerald-500/20 font-bold">' + (T.HELMET || 'HELMET') + '</span>' 
                    : '<span class="px-2 py-0.5 rounded text-[10px] bg-slate-800 text-slate-500 border border-slate-700">' + (T.NOHELMET || 'NO HELMET') + '</span>';

                const proxHtml = p.prox_short
                    ? '<span class="px-2 py-0.5 rounded text-[10px] bg-amber-500/10 text-amber-400 border border-amber-500/20 font-bold">5m</span>'
                    : '<span class="px-2 py-0.5 rounded text-[10px] bg-blue-500/10 text-blue-400 border border-blue-500/20 font-bold">50m</span>';

                const scHtml = p.sc_online
                    ? '<span class="inline-flex items-center gap-1"><span class="w-2 h-2 rounded-full bg-emerald-500"></span><span class="text-[10px] text-emerald-400 font-bold">' + (T.GAME || 'GAME') + '</span></span>'
                    : '<span class="inline-flex items-center gap-1"><span class="w-2 h-2 rounded-full bg-slate-600"></span><span class="text-[10px] text-slate-500">' + (T.OFFLINE || 'OFFLINE') + '</span></span>';

                badgeContainer.innerHTML = 
                    '<div class="flex gap-1.5">' +
                        helmetHtml +
                        proxHtml +
                    '</div>' +
                    scHtml;
            }

            if (subInfo && p.pos) {
                subInfo.innerText = (T.Container || 'Container') + ': ' + (p.pos.container_name || p.pos.container_id || (T.Global || 'Global'));
            }
        }

        function updatePlayerRowSelectors(name) {
            const p = players[name];
            if (!p) return;

            const chSel = document.getElementById('player-sel-ch-' + name);
            const prSel = document.getElementById('player-sel-pr-' + name);

            if (chSel) chSel.value = p.active_channel || '';
            if (prSel) prSel.value = p.profile || '';
        }

        function updatePlayerRowListening(name) {
            const p = players[name];
            if (!p) return;

            const container = document.getElementById('player-listening-' + name);
            if (!container) return;

            const checkboxes = container.querySelectorAll('input[type="checkbox"]');
            checkboxes.forEach(cb => {
                const ch = cb.getAttribute('data-channel');
                cb.checked = p.listening_channels && p.listening_channels.includes(ch);
            });
        }

        // Render DB Accounts Tab
        function renderAccountsList(list) {
            const tbody = document.getElementById('accounts-table-body');
            tbody.innerHTML = '';
            if (!list || list.length === 0) {
                tbody.innerHTML = '<tr><td colspan="10" class="py-8 text-center text-slate-500">' + (T.NoRegisteredPlayers || 'No registered player accounts.') + '</td></tr>';
                return;
            }
            
            list.forEach(acc => {
                const tr = document.createElement('tr');
                tr.className = 'border-b border-slate-900/50 hover:bg-slate-900/10 transition';
                tr.setAttribute('data-username', acc.username);

                const banBadge = acc.is_banned 
                    ? '<span class="px-2 py-0.5 rounded text-[10px] bg-red-500/15 text-red-400 border border-red-500/30 font-bold">' + (T.BANNED || 'BANNED') + '</span>'
                    : '<span class="px-2 py-0.5 rounded text-[10px] bg-emerald-500/15 text-emerald-400 border border-emerald-500/30 font-bold">' + (T.ACTIVE || 'ACTIVE') + '</span>';

                const banBtnText = acc.is_banned ? (T.Unban || 'Unban') : (T.Ban || 'Ban');
                const banBtnClass = acc.is_banned
                    ? 'px-2 py-1 text-xs font-semibold bg-emerald-500/10 hover:bg-emerald-500/20 border border-emerald-500/20 text-emerald-400 rounded-lg transition mr-2'
                    : 'px-2 py-1 text-xs font-semibold bg-red-500/10 hover:bg-red-500/20 border border-red-500/20 text-red-400 rounded-lg transition mr-2';

                const listenStr = (acc.listening_channels && acc.listening_channels.length > 0) ? acc.listening_channels.join(', ') : '(' + (T.Optional || 'None') + ')';

                tr.innerHTML = 
                    '<td class="py-3.5 px-4 font-semibold text-white">' + acc.username + '</td>' +
                    '<td class="py-3.5 px-4 text-slate-300">' + (acc.active_channel || (T.General || 'General')) + '</td>' +
                    '<td class="py-3.5 px-4 text-slate-400 text-xs">' + listenStr + '</td>' +
                    '<td class="py-3.5 px-4 text-slate-300">' + (acc.profile || '(' + (T.Optional || 'None') + ')') + '</td>' +
                    '<td class="py-3.5 px-4">' + banBadge + '</td>' +
                    '<td class="py-3.5 px-4 text-slate-400 font-mono text-xs">' + (acc.last_ip || '-') + '</td>' +
                    '<td class="py-3.5 px-4 text-slate-400 font-mono text-[10px] break-all max-w-[150px]" title="' + (acc.hwid || '-') + '">' + (acc.hwid ? acc.hwid.substring(0, 8) + '...' : '-') + '</td>' +
                    '<td class="py-3.5 px-4 text-slate-500 text-xs">' + acc.created_at + '</td>' +
                    '<td class="py-3.5 px-4 text-slate-500 text-xs">' + acc.updated_at + '</td>' +
                    '<td class="py-3.5 px-4 text-right">' +
                        '<button onclick="togglePlayerBan(\'' + acc.username + '\', ' + !acc.is_banned + ')" class="' + banBtnClass + '">' + banBtnText + '</button>' +
                        '<button onclick="resetPlayerPasswordPrompt(\'' + acc.username + '\')" class="px-2 py-1 text-xs font-semibold bg-purple-500/10 hover:bg-purple-500/20 border border-purple-500/20 text-purple-400 rounded-lg transition mr-2">' + (T.ResetPwd || 'Reset Pwd') + '</button>' +
                        '<button onclick="deletePlayerAccount(\'' + acc.username + '\')" class="px-2 py-1 text-xs font-semibold bg-red-500/15 hover:bg-red-500/30 text-red-400 border border-red-500/20 rounded-lg transition">' + (T.Delete || 'Delete') + '</button>' +
                    '</td>';
                tbody.appendChild(tr);
            });
        }

        function filterAccountsTable() {
            const query = document.getElementById('accounts-search').value.toLowerCase().trim();
            const rows = document.querySelectorAll('#accounts-table-body tr');
            
            rows.forEach(row => {
                const username = row.getAttribute('data-username');
                if (username) {
                    if (username.toLowerCase().includes(query)) {
                        row.classList.remove('hidden');
                    } else {
                        row.classList.add('hidden');
                    }
                }
            });
        }

        // Render DB Admins Tab
        function renderAdminsList(list) {
            const tbody = document.getElementById('admins-table-body');
            tbody.innerHTML = '';
            if (!list || list.length === 0) {
                tbody.innerHTML = '<tr><td colspan="3" class="py-8 text-center text-slate-500">' + (T.NoRegisteredAdmins || 'No registered administrators.') + '</td></tr>';
                return;
            }
            
            list.forEach(adm => {
                const tr = document.createElement('tr');
                tr.className = 'border-b border-slate-900/50 hover:bg-slate-900/10 transition';

                const isMainAdmin = adm.username === 'admin';
                const delBtn = isMainAdmin 
                    ? '<span class="text-xs text-slate-600 font-semibold italic">' + (T.Protected || 'Protected') + '</span>'
                    : '<button onclick="deleteAdminAccount(\'' + adm.username + '\')" class="px-2.5 py-1 text-xs font-semibold bg-red-500/15 hover:bg-red-500/30 text-red-400 border border-red-500/20 rounded-lg transition">' + (T.Delete || 'Delete') + '</button>';

                tr.innerHTML = 
                    '<td class="py-3.5 px-4 font-semibold text-white">' + adm.username + '</td>' +
                    '<td class="py-3.5 px-4 text-slate-400 text-xs">' + adm.created_at + '</td>' +
                    '<td class="py-3.5 px-4 text-right">' + delBtn + '</td>';
                tbody.appendChild(tr);
            });
        }

        // Player accounts admin commands
        function togglePlayerBan(username, shouldBan) {
            const confirmMsg = (shouldBan 
                ? (T.ConfirmBan || "Ban player {0}?") 
                : (T.ConfirmUnban || "Unban player {0}?")).replace('{0}', username);
            if (confirm(confirmMsg)) {
                sendAdminCommand({
                    cmd: 'ban_player',
                    name: username,
                    value: shouldBan
                });
            }
        }

        function deletePlayerAccount(username) {
            const confirmMsg = (T.ConfirmDeletePlayer || "Permanently delete player account '{0}'?").replace('{0}', username);
            if (confirm(confirmMsg)) {
                sendAdminCommand({
                    cmd: 'delete_player',
                    name: username
                });
            }
        }

        // Render Banned IPs List
        function renderBannedIPsList(list) {
            const tbody = document.getElementById('banned-ips-table-body');
            tbody.innerHTML = '';
            if (!list || list.length === 0) {
                tbody.innerHTML = '<tr><td colspan="4" class="py-8 text-center text-slate-500">' + (T.NoActiveIPBans || 'No active IP address bans.') + '</td></tr>';
                return;
            }
            list.forEach(item => {
                const tr = document.createElement('tr');
                tr.className = 'border-b border-slate-900/50 hover:bg-slate-900/10 transition';
                tr.innerHTML = 
                    '<td class="py-3.5 px-4 font-mono text-xs font-semibold text-white">' + item.ip + '</td>' +
                    '<td class="py-3.5 px-4 text-slate-300 text-sm">' + (item.reason || '(' + (T.Reason || 'No reason') + ')') + '</td>' +
                    '<td class="py-3.5 px-4 text-slate-500 text-xs">' + item.created_at + '</td>' +
                    '<td class="py-3.5 px-4 text-right">' +
                        '<button onclick="removeBannedIP(\'' + item.ip + '\')" class="px-2 py-1 text-xs font-semibold bg-emerald-500/10 hover:bg-emerald-500/20 border border-emerald-500/20 text-emerald-400 rounded-lg transition">' + (T.Unban || 'Unban') + '</button>' +
                    '</td>';
                tbody.appendChild(tr);
            });
        }

        // Render Banned HWIDs List
        function renderBannedHwidsList(list) {
            const tbody = document.getElementById('banned-hwids-table-body');
            tbody.innerHTML = '';
            if (!list || list.length === 0) {
                tbody.innerHTML = '<tr><td colspan="4" class="py-8 text-center text-slate-500">' + (T.NoActiveHwidBans || 'No active hardware ID bans.') + '</td></tr>';
                return;
            }
            list.forEach(item => {
                const tr = document.createElement('tr');
                tr.className = 'border-b border-slate-900/50 hover:bg-slate-900/10 transition';
                tr.innerHTML = 
                    '<td class="py-3.5 px-4 font-mono text-xs font-semibold text-white">' + item.hwid + '</td>' +
                    '<td class="py-3.5 px-4 text-slate-300 text-sm">' + (item.reason || '(' + (T.Reason || 'No reason') + ')') + '</td>' +
                    '<td class="py-3.5 px-4 text-slate-500 text-xs">' + item.created_at + '</td>' +
                    '<td class="py-3.5 px-4 text-right">' +
                        '<button onclick="removeBannedHwid(\'' + item.hwid + '\')" class="px-2 py-1 text-xs font-semibold bg-emerald-500/10 hover:bg-emerald-500/20 border border-emerald-500/20 text-emerald-400 rounded-lg transition">' + (T.Unban || 'Unban') + '</button>' +
                    '</td>';
                tbody.appendChild(tr);
            });
        }

        // Prompts
        function addBannedIPPrompt() {
            const ip = prompt(T.EnterIPPrompt || "Enter the IP address to ban:");
            if (!ip) return;
            const reason = prompt(T.BanReasonPrompt || "Ban reason:");
            sendAdminCommand({ cmd: 'add_banned_ip', name: ip, new: reason || '' });
        }

        // Unbans
        function removeBannedIP(ip) {
            const confirmMsg = (T.ConfirmDeleteBan || "Remove ban for {0}?").replace('{0}', ip);
            if (confirm(confirmMsg)) {
                sendAdminCommand({ cmd: 'remove_banned_ip', name: ip });
            }
        }

        function addBannedHwidPrompt() {
            const hwid = prompt(T.EnterHwidPrompt || "Enter the hardware ID (HWID) to ban:");
            if (!hwid) return;
            const reason = prompt(T.BanReasonPrompt || "Ban reason:");
            sendAdminCommand({ cmd: 'add_banned_hwid', name: hwid, new: reason || '' });
        }

        function removeBannedHwid(hwid) {
            const confirmMsg = (T.ConfirmDeleteBan || "Remove ban for {0}?").replace('{0}', hwid);
            if (confirm(confirmMsg)) {
                sendAdminCommand({ cmd: 'remove_banned_hwid', name: hwid });
            }
        }

        function resetPlayerPasswordPrompt(username) {
            showPrompt(T.ChangePasswordTitle || 'Change Password', (T.EnterNewPasswordDesc || 'Enter the new password for {0}:').replace('{0}', username), '', (newPwd) => {
                sendAdminCommand({
                    cmd: 'reset_player_password',
                    name: username,
                    value: newPwd
                });
            });
        }

        // Admins account settings commands
        function createAdminAccount() {
            const user = document.getElementById('new-admin-user').value.trim();
            const pass = document.getElementById('new-admin-pass').value.trim();
            if (user === '' || pass === '') {
                alert(T.FillAllFields || 'Please fill in all fields.');
                return;
            }
            sendAdminCommand({
                cmd: 'create_admin',
                name: user,
                new: pass
            });
            document.getElementById('new-admin-user').value = '';
            document.getElementById('new-admin-pass').value = '';
        }

        function deleteAdminAccount(username) {
            const confirmMsg = (T.ConfirmDeleteAdmin || "Do you want to delete administrator '{0}'?").replace('{0}', username);
            if (confirm(confirmMsg)) {
                sendAdminCommand({
                    cmd: 'delete_admin',
                    name: username
                });
            }
        }

        // Pass changes
        function changeAdminPassword() {
            const user = document.getElementById('change-admin-user').value.trim() || 'admin';
            const pass = document.getElementById('change-admin-pass').value.trim();
            if (pass === '') {
                alert(T.EnterPassword || 'Please enter a password.');
                return;
            }
            sendAdminCommand({
                cmd: 'change_admin_password',
                name: user,
                new: pass
            });
            document.getElementById('change-admin-pass').value = '';
        }

        // Send Administrative commands via WS
        function sendAdminCommand(commandObj) {
            if (ws && ws.readyState === WebSocket.OPEN) {
                commandObj.req_id = Math.random().toString(36).substring(2, 9);
                ws.send(JSON.stringify(commandObj));
            }
        }

        function toggleAnonymousMode() {
            sendAdminCommand({
                cmd: 'set_anonymous_mode',
                value: !anonymousMode
            });
        }

        function assignPlayerChannel(user, channel) {
            sendAdminCommand({
                cmd: 'assign_channel',
                user: user,
                name: channel
            });
        }

        function togglePlayerListening(user, channel, isChecked) {
            const p = players[user];
            if (!p) return;

            let list = [...(p.listening_channels || [])];
            if (isChecked) {
                if (!list.includes(channel)) list.push(channel);
            } else {
                list = list.filter(c => c !== channel);
            }

            sendAdminCommand({
                cmd: 'assign_listening_channels',
                user: user,
                value: list
            });
        }

        // Channels Management
        function addChannel(name) {
            sendAdminCommand({
                cmd: 'add_channel',
                name: name
            });
        }

        function renameChannel(oldName, newName) {
            sendAdminCommand({
                cmd: 'rename_channel',
                old: oldName,
                new: newName
            });
        }

        function removeChannel(name) {
            const confirmMsg = (T.ConfirmDeleteChannel || "Delete channel '{0}'?\nPlayers in this channel will be moved to General.").replace('{0}', name);
            if (confirm(confirmMsg)) {
                sendAdminCommand({
                    cmd: 'remove_channel',
                    name: name
                });
            }
        }

        // Profiles Management
        function addProfile(name) {
            sendAdminCommand({
                cmd: 'add_profile',
                name: name
            });
        }

        function renameProfile(oldName, newName) {
            sendAdminCommand({
                cmd: 'rename_profile',
                old: oldName,
                new: newName
            });
        }

        function removeProfile(name) {
            const confirmMsg = (T.ConfirmDeleteProfile || "Delete profile '{0}'?\nPlayers with this role will lose it.").replace('{0}', name);
            if (confirm(confirmMsg)) {
                sendAdminCommand({
                    cmd: 'remove_profile',
                    name: name
                });
            }
        }

        function assignPlayerProfile(user, profile) {
            sendAdminCommand({
                cmd: 'assign_profile',
                user: user,
                name: profile
            });
        }

        function kickPlayer(name) {
            const confirmMsg = (T.ConfirmKick || "Kick player '{0}'?").replace('{0}', name);
            if (confirm(confirmMsg)) {
                sendAdminCommand({
                    cmd: 'kick_player',
                    name: name
                });
            }
        }

        // Settings updates
        function updateServerToken() {
            const token = document.getElementById('server-token-input').value.trim();
            if (token === '') return;
            if (confirm(T.ConfirmChangeToken || "Change the player access token?")) {
                sendAdminCommand({
                    cmd: 'set_server_token',
                    token: token
                });
                document.getElementById('server-token-input').value = '';
            }
        }

        // Interactive Prompt Modal Helper
        let activePromptAction = null;
        function showPrompt(title, desc, initialValue, actionFn) {
            document.getElementById('prompt-title').innerText = title;
            document.getElementById('prompt-desc').innerText = desc;
            
            const input = document.getElementById('prompt-input');
            input.value = initialValue;
            input.focus();

            activePromptAction = actionFn;

            document.getElementById('prompt-submit-btn').onclick = () => {
                const val = input.value.trim();
                if (val !== '') {
                    activePromptAction(val);
                    hidePrompt();
                }
            };

            document.getElementById('prompt-modal').classList.remove('hidden');
        }

        function hidePrompt() {
            document.getElementById('prompt-modal').classList.add('hidden');
            activePromptAction = null;
        }

        function showAddChannelPrompt() {
            showPrompt(T.CreateChannel || 'Create Channel', T.EnterNewChannelName || 'Enter the name of the new radio channel:', '', (val) => addChannel(val));
        }

        function showRenameChannelPrompt(oldName) {
            showPrompt(T.RenameChannel || 'Rename Channel', (T.RenameChannelPrompt || 'Enter the new name for channel {0}:').replace('{0}', oldName), oldName, (val) => renameChannel(oldName, val));
        }

        function showAddProfilePrompt() {
            showPrompt(T.CreateProfile || 'Create Profile', T.EnterNewProfileName || 'Enter the name of the new audio profile:', '', (val) => addProfile(val));
        }

        function showRenameProfilePrompt(oldName) {
            showPrompt(T.RenameProfile || 'Rename Profile', (T.RenameProfilePrompt || 'Enter the new name for profile {0}:').replace('{0}', oldName), oldName, (val) => renameProfile(oldName, val));
        }

        // Live Logs Console formatting
        function appendLocalLog(text, textClass) {
            const consoleBox = document.getElementById('logs-console');
            const div = document.createElement('div');
            const ts = new Date().toLocaleTimeString();
            div.className = textClass;
            div.innerHTML = '<span class="text-slate-500 font-bold mr-2">[' + ts + ']</span>' + text;
            consoleBox.appendChild(div);
            consoleBox.scrollTop = consoleBox.scrollHeight;
        }

        function appendServerLog(ts, text, ansiColor) {
            const consoleBox = document.getElementById('logs-console');
            if (!consoleBox) return;
            
            const filterVal = document.getElementById('logs-filter').value.toLowerCase().trim();
            if (filterVal !== '' && !text.toLowerCase().includes(filterVal)) {
                return;
            }

            const div = document.createElement('div');
            const colorClass = getLogColorClass(ansiColor);
            
            div.innerHTML = '<span class="text-slate-500 font-bold mr-2">[' + ts + ']</span><span class="' + colorClass + '">' + escapeHTML(text) + '</span>';
            consoleBox.appendChild(div);
            
            if (consoleBox.children.length > 500) {
                consoleBox.removeChild(consoleBox.firstChild);
            }
            consoleBox.scrollTop = consoleBox.scrollHeight;
        }

        function clearLogs() {
            const consoleBox = document.getElementById('logs-console');
            if (consoleBox) consoleBox.innerHTML = '';
        }

        function getLogColorClass(ansiColor) {
            if (ansiColor.includes('31m')) return 'text-red-400';
            if (ansiColor.includes('32m')) return 'text-emerald-400 font-semibold';
            if (ansiColor.includes('33m')) return 'text-amber-400';
            if (ansiColor.includes('34m')) return 'text-sky-400';
            if (ansiColor.includes('35m')) return 'text-purple-400';
            if (ansiColor.includes('36m')) return 'text-cyan-400';
            if (ansiColor.includes('208m')) return 'text-orange-400';
            if (ansiColor.includes('90m')) return 'text-slate-500';
            return 'text-slate-300';
        }

        function escapeHTML(str) {
            return str.replace(/[&<>'"]/g, 
                tag => ({
                    '&': '&amp;',
                    '<': '&lt;',
                    '>': '&gt;',
                    "'": '&#39;',
                    '"': '&quot;'
                }[tag] || tag)
            );
        }

        // Notification Toast triggers
        function showToast(cmd, ok, reason) {
            const toast = document.getElementById('toast');
            const title = document.getElementById('toast-title');
            const msg = document.getElementById('toast-msg');
            const icon = document.getElementById('toast-icon');

            title.innerText = (T.Command || 'Command') + ' ' + cmd;
            if (ok) {
                toast.className = 'fixed bottom-5 right-5 z-50 transition duration-300 flex items-center p-4 rounded-xl border bg-emerald-950/90 border-emerald-500/30 text-emerald-200 shadow-2xl translate-y-0 opacity-100';
                msg.innerText = T.OperationSuccess || 'Operation completed successfully.';
                icon.innerText = '✓';
                icon.className = 'mr-3 w-6 h-6 flex items-center justify-center rounded-full bg-emerald-500 text-slate-950 font-bold';
            } else {
                toast.className = 'fixed bottom-5 right-5 z-50 transition duration-300 flex items-center p-4 rounded-xl border bg-red-950/90 border-red-500/30 text-red-200 shadow-2xl translate-y-0 opacity-100';
                msg.innerText = (T.Failed || 'Failed') + ': ' + (reason || T.ToastUnknown || 'Unknown reason.');
                icon.innerText = '✕';
                icon.className = 'mr-3 w-6 h-6 flex items-center justify-center rounded-full bg-red-500 text-white font-bold';
            }

            setTimeout(() => {
                toast.className = 'fixed bottom-5 right-5 z-50 transition duration-300 flex items-center p-4 rounded-xl border bg-slate-900 border-slate-800 text-slate-200 shadow-2xl translate-y-20 opacity-0';
            }, 3000);
        }

        // Radar Map Variables
        let radarCanvas = null;
        let radarCtx = null;
        let radarZoom = 1.0;
        let radarPanX = 0;
        let radarPanY = 0;
        let isRadarDragging = false;
        let radarDragStartX = 0;
        let radarDragStartY = 0;
        let selectedRadarZone = "";

        // Init Radar
        function initRadar() {
            radarCanvas = document.getElementById('radar-canvas');
            if (!radarCanvas) return;
            radarCtx = radarCanvas.getContext('2d');
            
            // Handle resizing
            const resizeObserver = new ResizeObserver(entries => {
                for (let entry of entries) {
                    const width = entry.contentRect.width;
                    const height = entry.contentRect.height;
                    radarCanvas.width = width * window.devicePixelRatio;
                    radarCanvas.height = height * window.devicePixelRatio;
                    drawRadar();
                }
            });
            resizeObserver.observe(radarCanvas.parentElement);

            // Drag to pan
            radarCanvas.addEventListener('mousedown', (e) => {
                isRadarDragging = true;
                const rect = radarCanvas.getBoundingClientRect();
                const x = e.clientX - rect.left;
                const y = e.clientY - rect.top;
                radarDragStartX = x - radarPanX;
                radarDragStartY = y - radarPanY;
            });
            window.addEventListener('mouseup', () => {
                isRadarDragging = false;
            });
            radarCanvas.addEventListener('mousemove', (e) => {
                if (!isRadarDragging) return;
                const rect = radarCanvas.getBoundingClientRect();
                const x = e.clientX - rect.left;
                const y = e.clientY - rect.top;
                radarPanX = x - radarDragStartX;
                radarPanY = y - radarDragStartY;
                drawRadar();
            });

            // Wheel to zoom
            radarCanvas.addEventListener('wheel', (e) => {
                e.preventDefault();
                const zoomFactor = e.deltaY < 0 ? 1.15 : 0.85;
                const rect = radarCanvas.getBoundingClientRect();
                const x = e.clientX - rect.left;
                const y = e.clientY - rect.top;
                zoomRadar(zoomFactor, x, y);
            });
            
            // Loop for rendering
            setInterval(drawRadar, 100);
        }

        function zoomRadar(factor, mouseX, mouseY) {
            const oldZoom = radarZoom;
            radarZoom = Math.min(Math.max(radarZoom * factor, 0.05), 50.0);
            
            if (mouseX !== undefined && mouseY !== undefined) {
                // Zoom towards mouse pointer
                const dZoom = radarZoom - oldZoom;
                const centerX = radarCanvas.width / (2 * window.devicePixelRatio);
                const centerY = radarCanvas.height / (2 * window.devicePixelRatio);
                radarPanX -= (mouseX - centerX) * (dZoom / oldZoom);
                radarPanY -= (mouseY - centerY) * (dZoom / oldZoom);
            }
            drawRadar();
        }

        function resetRadar() {
            radarZoom = 1.0;
            radarPanX = 0;
            radarPanY = 0;
            drawRadar();
        }

        function onRadarZoneChange() {
            selectedRadarZone = document.getElementById('radar-zone-select').value;
            drawRadar();
        }

        function updateRadarZonesDropdown() {
            const select = document.getElementById('radar-zone-select');
            if (!select) return;
            
            // Get unique zones
            const zones = new Set();
            Object.values(players).forEach(p => {
                if (p.pos && p.pos.zone) {
                    zones.add(p.pos.zone);
                }
            });

            const currentVal = select.value;
            select.innerHTML = '<option value="">All Zones</option>';
            zones.forEach(zone => {
                const opt = document.createElement('option');
                opt.value = zone;
                opt.innerText = zone;
                if (zone === currentVal) opt.selected = true;
                select.appendChild(opt);
            });
        }

        function drawRadar() {
            if (!radarCanvas || !radarCtx || activeTab !== 'radar') return;
            
            const w = radarCanvas.width;
            const h = radarCanvas.height;
            const ratio = window.devicePixelRatio;
            
            radarCtx.clearRect(0, 0, w, h);
            
            const centerX = w / 2;
            const centerY = h / 2;
            
            radarCtx.save();
            // Scale and Translate context
            radarCtx.translate(centerX, centerY);
            radarCtx.scale(ratio, ratio);
            radarCtx.translate(radarPanX, radarPanY);
            
            // Draw grid
            drawGrid(w / ratio, h / ratio);
            
            // Draw players
            drawPlayerDots();
            
            radarCtx.restore();

            // Draw HUD Info (like Scale Legend) outside the panning context
            drawRadarHUD(w / ratio, h / ratio);
        }

        function drawGrid(viewWidth, viewHeight) {
            const step = 200 * radarZoom; // base step size
            const minX = -viewWidth / 2 - radarPanX;
            const maxX = viewWidth / 2 - radarPanX;
            const minY = -viewHeight / 2 - radarPanY;
            const maxY = viewHeight / 2 - radarPanY;
            
            // Grid lines
            radarCtx.strokeStyle = 'rgba(255, 255, 255, 0.03)';
            radarCtx.lineWidth = 1;
            
            const startX = Math.floor(minX / step) * step;
            for (let x = startX; x <= maxX; x += step) {
                radarCtx.beginPath();
                radarCtx.moveTo(x, minY);
                radarCtx.lineTo(x, maxY);
                radarCtx.stroke();
            }
            
            const startY = Math.floor(minY / step) * step;
            for (let y = startY; y <= maxY; y += step) {
                radarCtx.beginPath();
                radarCtx.moveTo(minX, y);
                radarCtx.lineTo(maxX, y);
                radarCtx.stroke();
            }
            
            // Center Axes
            radarCtx.strokeStyle = 'rgba(255, 255, 255, 0.08)';
            radarCtx.lineWidth = 1.5;
            
            radarCtx.beginPath();
            radarCtx.moveTo(0, minY);
            radarCtx.lineTo(0, maxY);
            radarCtx.stroke();
            
            radarCtx.beginPath();
            radarCtx.moveTo(minX, 0);
            radarCtx.lineTo(maxX, 0);
            radarCtx.stroke();
        }

        function drawPlayerDots() {
            Object.values(players).forEach(p => {
                if (!p.pos) return;
                if (selectedRadarZone && p.pos.zone !== selectedRadarZone) return;
                
                // Coordinates in Star Citizen (we'll project X and Y)
                const px = p.pos.x * radarZoom;
                const py = -p.pos.y * radarZoom; // Invert Y for standard 2D cartesian view in canvas
                
                // Draw dot
                radarCtx.beginPath();
                radarCtx.arc(px, py, 6, 0, 2 * Math.PI);
                
                // Dot color: active channel coloring
                let color = '#38bdf8'; // light blue default
                if (p.active_channel) {
                    color = '#10b981'; // emerald green if on a channel
                }
                
                radarCtx.fillStyle = color;
                radarCtx.fill();
                
                // Subtle white ring
                radarCtx.strokeStyle = 'rgba(255, 255, 255, 0.5)';
                radarCtx.lineWidth = 1.5;
                radarCtx.stroke();
                
                // Name label
                radarCtx.fillStyle = '#ffffff';
                radarCtx.font = '11px sans-serif';
                radarCtx.textAlign = 'center';
                radarCtx.fillText(p.name, px, py - 12);
                
                // Coordinates/Zone label
                radarCtx.fillStyle = '#94a3b8';
                radarCtx.font = '8px sans-serif';
                radarCtx.fillText(p.pos.x.toFixed(0) + ', ' + p.pos.y.toFixed(0) + ' (' + p.pos.zone + ')', px, py + 16);
            });
        }

        function drawRadarHUD(w, h) {
            // Draw scale indicator in bottom-left corner
            const scaleLen = 100 * radarZoom; // 100m in pixels
            const legendX = 20;
            const legendY = h - 20;
            
            radarCtx.strokeStyle = 'rgba(255, 255, 255, 0.4)';
            radarCtx.lineWidth = 2;
            
            radarCtx.beginPath();
            radarCtx.moveTo(legendX, legendY - 5);
            radarCtx.lineTo(legendX, legendY);
            radarCtx.lineTo(legendX + 100, legendY);
            radarCtx.lineTo(legendX + 100, legendY - 5);
            radarCtx.stroke();
            
            radarCtx.fillStyle = 'rgba(255, 255, 255, 0.6)';
            radarCtx.font = '10px sans-serif';
            radarCtx.textAlign = 'left';
            const scaleDistance = (100 / radarZoom).toFixed(0);
            radarCtx.fillText(scaleDistance + 'm', legendX + 5, legendY - 8);
        }

        // Initialize connection
        connectWS();
        initRadar();
    </script>
</body>
</html>`
