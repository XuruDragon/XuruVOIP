#XuruVoip

<p align="center">
  <a href="https://github.com/XuruDragon/XuruVOIP/actions/workflows/tests.yml">
    <img src="https://github.com/XuruDragon/XuruVOIP/actions/workflows/tests.yml/badge.svg" alt="Tests Status" />
  </a>
  <a href="https://github.com/XuruDragon/XuruVOIP/releases">
    <img src="https://img.shields.io/github/v/release/XuruDragon/XuruVOIP?color=blue&logo=github" alt="Latest Release" />
  </a>
</p>

<p align="center">
  <b>Traductions :</b><br/>
  <a href="../README.md">English</a> •
  <a href="README.fr.md">Français</a> •
  <a href="README.de.md">Deutsch</a> •
  <a href="README.es.md">Español</a> •
  <a href="README.pt-BR.md">Português (Brasil)</a> •
  <a href="README.pt-PT.md">Português (Portugal)</a> •
  <a href="README.ja.md">日本語</a> •
  <a href="README.zh.md">简体中文</a>
</p>

<p align="center">
  <img src="../logo.png" alt="XuruVoip Logo" width="400" height="400" />
</p>

XuruVoip est une suite de **communication vocale 3D (VoIP)** hautes performances, sécurisée et spatialisée de manière dynamique, conçue spécifiquement pour les intégrations de jeux personnalisées avec **Star Citizen**. Il se compose d'un serveur backend basé sur Go et d'un client C# WPF moderne avec une application Companion intégrée (interface Web) et une intégration Elgato Stream Deck.

### 🎯 Objectif du projet
L'objectif de XuruVoip est de fournir aux événements de jeu Star Citizen, aux organisations de jeu de rôle et aux équipes tactiques un **niveau sans précédent d'immersion audio et de commodité opérationnelle**. En lisant en temps réel les coordonnées, la visière et l'état du véhicule à partir du client du jeu, XuruVoip façonne dynamiquement les voix des joueurs dans l'espace 3D, simule les atmosphères planétaires/vide et achemine automatiquement les communications tactiques sans nécessiter de configuration manuelle du client.

---

### 🗺️ Annuaire de navigation

| Rubrique | Descriptif |
| :--- | :--- |
| [📖 Guide détaillé des fonctionnalités](../doc/functionnalities.md) | Explication technique et utilisateur de l'ensemble des 16+ fonctionnalités implémentées. |
| [📖 Guides de l'utilisateur non techniques](#-guides-dutilisation-non-techniques) | Guides étape par étape faciles à comprendre pour le client, le serveur et Stream Deck. |
| [📸 Captures d'écran et interface utilisateur](#-captures-décran-et-interface-utilisateur) | Vitrine visuelle des écrans clients, du portail d'administration et des paramètres. |
| [🗂️ Structure du projet](#️-project-structure) | Disposition du référentiel et répartition des dossiers. |
| [⚙️Architecture système](#️-system-architecture) | Le diagramme de flux de travail complet du client WPF, du serveur Go et des périphériques externes. |
| [💡 Aperçu des fonctionnalités principales](#-aperçu-des-fonctionnalités-principales) | Répartition détaillée des 11+ fonctionnalités spatiales et de réseau mises en œuvre. |
| [🖥️ Go Serveur (Go)](#️-xuruvoip-server-go) | Instructions de création, d'exécution, de déploiement et de configuration du serveur. |
| [🎛️ Pont vocal Discord](#️-discord-voice-bridge-setup-guide) | Connexion des chaînes radio du serveur Go à une chaîne vocale Discord. |
| [📱 Application compagnon et Stream Deck](#-intégration-de-lapplication-companion-et-du-stream-deck) | Contrôle des appareils à distance et configuration des touches physiques du Stream Deck. |
| [🛠️Client WPF (C#)](#-building--running-the-client) | Exigences du client, compilation et guides d'installation MSI/Portable. |

---

## 📖 Guides d'utilisation non techniques

Si vous n'avez pas de formation en informatique, nous avons rédigé des guides simples, étape par étape, pour vous aider à tout configurer et à tout faire fonctionner facilement :

* 📖 **[Guide détaillé des fonctionnalités](../doc/functionnalities.md)** : Explications approfondies de chaque fonctionnalité, de leur fonctionnement, de leur utilisation et de leur utilité.
* 🎮 **[Guide de l'utilisateur client](doc/client_guide.md)** : guide convivial sur le choix des microphones/haut-parleurs, la configuration de Push-to-Talk, l'utilisation de casques de combinaison spatiale et l'activation des effets vocaux d'effort.
* 🖥️ **[Guide de configuration du serveur](doc/server_guide.md)** : Explique comment héberger un serveur, ajuster les mots de passe/paramètres dans le fichier de paramètres `.env` et configurer le Discord Voice Bridge.
* 🎛️ **[Stream Deck Plugin Guide](doc/streamdeck_guide.md)** : Procédure pas à pas sur l'installation de boutons physiques pour la mise en sourdine, le basculement de la visière et l'affichage des canaux radio actifs.

---

## 📸 Captures d'écran et interface utilisateur

<details>
<summary>📸 Cliquez pour voir les captures d'écran</summary>

### 1. Fenêtre principale du client
![Fenêtre principale du client](/screenshots/main.png)

### 2. Onglet Paramètres audio (Contrôle audio spatial 3D)
![Onglet Paramètres audio](/screenshots/audio.png)

### 3. Onglet Paramètres généraux (sélection de la langue et du journal de jeu)
![Onglet Paramètres généraux](/screenshots/general.png)

### 4. Onglet Paramètres de connexion
![Onglet Paramètres de connexion](/screenshots/connection.png)

### 5. Onglet Paramètres des raccourcis clavier
![Onglet Paramètres des raccourcis clavier](/screenshots/hotkeys.png)

### 6. Onglet Paramètres de superposition (Vulkan et DirectX HUD)
![Onglet Paramètres de superposition](/screenshots/overlay.png)

### 7. Onglet Paramètres OCR (Tesseract OCR)
![Onglet Paramètres OCR](/screenshots/ocr.png)

### 8. Page de connexion au portail Web d'administration
![Page de connexion au portail Web d'administration](/screenshots/admin_login.png)

### 9. Tableau de bord du portail Web d'administration
![Tableau de bord du portail Web d'administration](/screenshots/admin_dashboard.png)

### 10. Lecteurs du portail Web d'administration
![Joueurs du portail Web d'administration](/screenshots/admin_players_list.png)

### 11. Liste des administrateurs du portail Web d'administration
![Liste des administrateurs du portail Web](/screenshots/admin_admin_list.png)

### 12. Liste des interdictions du portail Web d'administration
![Liste des interdictions du portail Web d'administration](/screenshots/admin_ban_list.png)

</details>

---

## 🗂️ Structure du projet

- **/server** : Backend Go hautes performances hébergeant les services de position, d'audio et d'administration.
- **/client** : client C# WPF moderne utilisant NAudio, WebRtcVad et Tesseract OCR ou Game.log tail pour le suivi automatisé de la localisation et l'analyse des journaux. L'application compagnon est également incluse dans ce projet.
- **/streamdeck** : plugin Stream Deck pour le client XuruVoIP.

---

## ⚙️Architecture système

Vous trouverez ci-dessous l'architecture réelle complète du système XuruVoip, illustrant les boucles de capture, de positionnement, de lecture et de rendu HUD à l'intérieur du client WPF, des hubs Websocket du serveur Go et des intégrations externes :```mermaid
graph TB
    subgraph STIM ["Environnement de jeu (Star Citizen)"]
        SC["Client citoyen étoile"]
        LOGS["Game.log (fichier journal)"]
        SCREEN["Sortie graphique (Vulkan/DX)"]
    end

    subgraph WPF ["Client WPF XuruVOIP"]
        direction TB
        subgraph CAPT ["Capture de microphone et DSP"]
            MIC["Entrée micro"] --> VAD["WebRTC VAD"]
            VAD -->|Speech Detected| VC["Changeur de voix (Alien/Cyborg/Robot)"]
            VC -->|Modulated PCM| GF_FIL["G-Force Pitch & Tremolo / Injection d'haleine d'effort"]
            GF_FIL --> HELM_OSC["Respiration du casque et superposition de bourdonnement d'aération"]
            HELM_OSC --> OPUS_ENC["Encodeur Opus"]
        end

        subgraph POS_TRACK ["Positionnement et suivi de l'état"]
            LOGS -->|Tail Scanner| LOG_PAR["Analyseur Game.log"]
            SCREEN -->|showlocations Capture| OCR["Moteur OCR Tesseract"]
            LOG_PAR -->|Equip/Visor Events| HELM_DET["Synchronisation automatique de l'état de la visière"]
            LOG_PAR -->|G-Force & Stamina Values| GF_DET["Suivi de la force G et de l'effort"]
            OCR -->|Coords| POS_SEL{"Sélecteur de source"}
            LOG_PAR -->|Coords & ContainerID| POS_SEL
        end

        subgraph PLAY ["Lecture spatiale et DSP"]
            OPUS_DEC["Décodeur d'opus"] --> PKT_TYPE{"Type de paquet ?"}
            PKT_TYPE -->|PA 0x03| PA_FIL["Megaphone DSP (HP/LP, distorsion tanh, réverbération navale)"]
            PKT_TYPE -->|Proximity/Radio| OCC_FIL["Occlusion du pont et de la pièce de Carrack/Hercules"]
            OCC_FIL --> REV_FIL["Réverbération géolocalisée (grottes/bunkers/hangars)"]
            REV_FIL --> RAD_FIL["Passe-bande radio et routage multi-sauts longue portée (Dijkstra)"]
            RAD_FIL --> CHIMES["Générateur de gazouillis de micro PTT et de queue de silencieux"]
            CHIMES --> PAN["Mathématiques de panoramique spatial 3D"]
            PAN --> VOL["Atténuation de la distance spatiale"]
            VOL --> MIXER["NMixeur audio"]
            PA_FIL --> MIXER
            MIXER --> SPK["Périphériques de sortie audio"]
        end

        subgraph HUD ["Superposition HUD (clic Win32)"]
            T_RAD["Mini-radar tactique 2D"]
            STT["Parole en texte Whisper.net"]
            OPUS_DEC -.->|Incoming Voice| STT
            STT -->|Subtitles| SUB["Sous-titres HUD en temps réel"]
        end

        subgraph COMP ["Serveur Web compagnon"]
            HTTP_SRV["Écouteur HTTP local (port personnalisé)"]
            DASH["Tableau de bord HTML/JS Glassmorphic"]
        end

        POS_SEL -->|Coordinates & Zone| POS_WS["Positionner le client WS"]
        HELM_DET -->|Visor State| POS_WS
        GF_DET -->|G-Force / Exertion| GF_FIL
        OPUS_ENC -->|Audio Packets| AUD_WS["Client WS audio"]
    end

    subgraph SERVER ["Serveur XuruVOIP Go"]
        direction TB
        WS_HUB["Hub de connexion Websocket"]
        POS_HUB["Positionnement spatial et hub de zones"]
        DB["Base de données SQLite et canaux persistants"]
        DISC_BRIDGE["Pont vocal Discord"]
        ADM_PORT["Portail Web d'administration (Canvas Live Radar)"]

        WS_HUB <--> POS_HUB
        POS_HUB <--> DB
        DISC_BRIDGE <--> WS_HUB
    end

    subgraph EXT ["Interfaces externes"]
        DISC["Chaîne vocale Discord"] <-->|Bidirectional Voice Bridge| DISC_BRIDGE
        SD["Application Stream Deck"] <-->|WebSocket Actions / Port Setting| HTTP_SRV
        MOB["Contrôleur mobile"] <-->|REST API Status & Toggles| HTTP_SRV
    end

    POS_WS <-->|WS Port 8888| WS_HUB
    AUD_WS <-->|WS Port 8889| WS_HUB
```
---

## 💡 Aperçu des fonctionnalités principales

### 1. 🔊 Audio spatial 3D en temps réel
* **Panoramique stéréo dynamique :** PROJETE les coordonnées du haut-parleur distant sur les vecteurs de direction avant et droit de l'auditeur pour calculer le panoramique gauche/droite exact à l'aide d'une formule à puissance constante.
* **Résolution d'ambiguïté avant-arrière :** Atténue le volume audio de 25 % si un haut-parleur se tient derrière l'auditeur, résolvant ainsi les limitations standard de panoramique audio 2D.
* **Distance Roll-Off :** Atténue les voix de proximité de manière linéaire en fonction de la distance, garantissant des niveaux sonores naturels (fondu complètement jusqu'à zéro à 50 mètres, ou 5 mètres pour les chuchotements).

### 2. 🗺️ Acoustique géolocalisée et occlusion de navire/bunker
* **Occlusion des terrasses et des murs :** Détecte les limites internes à l'intérieur des espaces. Si les joueurs se trouvent sur différents ponts (par exemple Carrack, Hercules) ou salles (par exemple Bunkers), le filtrage passe-bas (fréquences de coupure de 300 Hz à 900 Hz) et l'amortissement du volume sont appliqués dynamiquement.
* **Réverbération environnementale :** Lit la zone hiérarchique du lecteur et applique automatiquement les paramètres personnalisés de mixage humide, de délai et de réverbération de feedback pour **Caves**, **Bunkers** et **Hangars**.

### 3. 💨 Simulation atmosphérique casque et EVA
* **EVA Muting :** Coupe automatiquement le son des communications vocales de proximité dans l'espace ou dans les zones de vide (EVA), obligeant les joueurs à utiliser les canaux radio pour communiquer.
* **Superposition du respirateur à visière :** Simule la pression de l'air lorsque la visière est abaissée. Synthétise un sifflement respiratoire à basse fréquence et un bourdonnement de ventilateur de ventilation à double fréquence (50 Hz + 100 Hz) sur le flux du micro capturé.
* **Synchronisation automatique de la visière :** Lit les journaux de pièces jointes dans `Game.log` pour détecter automatiquement lorsqu'un casque est équipé/retiré et met à jour l'état de la visière en temps réel.

### 4. 🎙️ Changeur de voix et modulateurs de combinaison de science-fiction
* **Filtres DSP en temps réel :** Changement de hauteur dans le domaine temporel, flanger, modulation en anneau, saturation soft-tanh et bitcrushing 8 bits.
* **Préréglages atmosphériques :** Chargez instantanément des profils vocaux prédéfinis, notamment **Alien**, **Cyborg**, **Robotic** ou **Custom Pitch Shift** (0,5x à 2,0x).

### 5. 📻 Dégradation radio immersive et carillons
* **Filtrage passe-bande :** Modèles de filtres radio avec coupures basse/haute lors de l'utilisation de canaux radio ou lorsque les visières des combinaisons sont baissées.
* **Dégradation du signal radio :** Bandes de coupure étroites et mélanges de bruit statique filtré passe-bande à mesure que la distance entre les joueurs s'approche de la limite de l'émetteur radio.
* **Carillons radio acoustiques :** Lit un gazouillis de micro-touche à balayage de tonalité (900 Hz à 700 Hz) lorsque la touche est enfoncée et une queue statique de silencieux lorsque la touche est relevée.

### 6. 💬 Système d'interphone automatique pour navire
* **Canaux d'interphone des véhicules :** Monter à bord d'un véhicule abonne automatiquement les joueurs à un canal radio dynamique « Intercom_<ContainerID> ».
* **Pilote Priority Ducking :** Lorsqu'un joueur dans un cockpit ou un siège conducteur transmet sur l'interphone, l'audio de proximité de tous les autres joueurs est réduit de 85 % pour garantir la clarté des commandes de vol.
* **Dégradation dynamique de l'interphone :** Les canaux d'interphone se dégradent automatiquement selon l'état du véhicule :
  * **Impacts de bouclier (Shield Hits) :** Injecte temporairement des rafales de parasites et des craquements de volume (dure 2,5 secondes).
  * **Alimentation critique (Critical Power) :** Bourdonnement électrique basse tension, distorsion de saturation et baisse de hauteur de ton par rééchantillonnage.
  * **Voyage quantique (Quantum Travel) :** Balayage de filtre en peigne (flanger/phaser) et sifflement haute fréquence.
  * *Tous ces sous-effets peuvent être activés/désactivés individuellement dans les paramètres généraux et sont désactivés par défaut.*
* **Cooldown de nettoyage :** compte à rebours 5 minutes après que le dernier joueur ait quitté le navire avant de supprimer le canal intercom, maximisant ainsi les performances du serveur.

### 7. 📡 Superposition HUD et radar tactique 2D compatibles Vulkan
* **Superposition Click-Through Win32 :** Une superposition HUD sans bordure affichant les connexions VoIP, les fréquences et les états de parole. Compatible Vulkan et DirectX (fonctionnant en mode fenêtré sans bordure).
* **Indicateur d'état de l'interphone :** Affiche des avertissements tels que `⚡ INTERCOM: DEGRADED` (avec détails du sous-état comme `[Power Loss]`, `[Quantum]` ou `[Static Pop]`) sur l'affichage tête haute (HUD) lorsque la dégradation de l'interphone est active.
* **Mini-radar tactique :** Comprend un radar HUD 2D aligné sur le cap qui affiche les joueurs parlant de manière relative, dessinant des anneaux sonores pulsés autour d'eux.
* **Sous-titres parole-texte :** transcrit l'audio de radio/proximité entrant en sous-titres HUD localisés à l'aide d'un modèle Whisper léger et hors ligne (`ggml-tiny.bin`).
* **Commandes vocales PTT mains libres :** Maintenir la touche dédiée aux commandes vocales désactive temporairement les flux vocaux de proximité/radio sortants et met en mémoire tampon l'audio du micro. Au relâchement, la voix est transcrite localement via le modèle Whisper pour déclencher des actions du vaisseau :
  * **Commandes prises en charge :** Bascule visière/casque, sourdine micro (proximité/radio/profil/tout), sélection du canal radio actif et préréglages du changeur de voix.
  * **Correspondance de mots-clés multilingue :** Prise en charge dans 8 langues (anglais, français, allemand, espagnol, portugais, japonais et chinois).
  * **Filtre de seuil de confiance :** Un curseur configurable permet de filtrer les correspondances à faible confiance ou les bruits de fond.
  * *Désactivé par défaut ; l'activer lancera le téléchargement en arrière-plan du modèle de transcription hors ligne Whisper (~140 Mo) s'il n'est pas déjà présent.*

### 8. 📱 Application compagnon et API REST
* **Serveur Web HTTP local :** Héberge un tableau de bord local sur un port configurable (par défaut : `8891`, désactivé par défaut).
* **Contrôleur Glassmorphic :** Se connecte à partir de téléphones ou d'écrans secondaires pour activer les sourdines, les cycles de canaux, les casques ou les changeurs de voix.
* **API REST :** Expose les points de terminaison `GET /api/status` et `POST /api/action` pour les intégrations externes (y compris l'état de l'interphone et la simulation des pannes).

### 9. 🎛️ Plugin Stream Deck
* **Stream Deck Action Pack :** Présente 8 actions pour contrôler la sourdine du microphone, la sourdine audio, les visières de casque et les cycles de fréquence radio.
* **Icônes de touches dynamiques :** Graphiques des boutons de mise à jour continue des WebSockets (cyan actif ou ambre muet) pour refléter l'état actuel du client.
* **Titre de la fréquence en direct :** Affiche les noms des chaînes radio actives directement sur les boutons physiques du Stream Deck.

### 10. 🔌 Pont vocal Discord
* **Relais audio bidirectionnel :** Relaye les communications entre un canal radio du serveur Go et un canal vocal Discord.
* **Mappage des surnoms :** capture le discours Discord et mappe les identifiants SSRC aux surnoms du serveur.

### 11. 🛡️ Radar de sécurité, de rotation des journaux et de toile d'administration
* **Rotation quotidienne des journaux :** L'archiveur de journaux de démarrage ne conserve que les 5 journaux les plus récents.
* **Tableau de bord d'administration :** Panneau d'administration Web en temps réel avec sécurité de verrouillage, limitation de débit et carte interactive 2D HTML5 Canvas Live Radar permettant aux administrateurs de zoomer, de faire un panoramique et de tracer l'historique des traces des joueurs.

### 12. 🤢 Distorsion vocale due à la force G et à l'effort physique
* **Tremolo et Pitch Shifting :** Sous des forces G élevées, le son du microphone sortant est modulé dynamiquement avec un trémolo LFO (4-10 Hz, jusqu'à 40 % de profondeur) et diminué (facteur : 1,0 jusqu'à 0,85) pour simuler une contrainte physique, une panne d'électricité ou des états de redout.
* **Superposition de respiration lourde :** Superpose automatiquement les bruits de respiration/halètement aléatoires, ajustant la vitesse du cycle de respiration en fonction des niveaux d'endurance du joueur analysés en temps réel à partir de « Game.log ».
* **Contrôles manuels/API :** Basculable via les paramètres du client et les curseurs de l'interface utilisateur Web de l'application Companion pour le jeu de rôle ou les tests simulés.

### 13. 📡 Relais radio tactique et balises répéteurs multi-sauts
* **Routage du signal multi-sauts :** Les joueurs peuvent activer le « Mode balise » pour agir comme une balise de répéteur radio. Si deux joueurs sont hors de portée radio directe (au-delà de 1 500 m), le client récepteur exécute l'algorithme du chemin le plus court de Dijkstra sur tous les répéteurs actifs de la zone.
* **Dégradation de la qualité du pire saut :** Si un chemin multi-sauts existe en dessous de la limite de 8 000 m pour un seul saut, le système achemine la communication et applique le facteur de dégradation du pire saut (qualité du signal) au lieu de la distance totale en ligne droite, permettant ainsi des réseaux radio planétaires/orbitaux à longue portée.
* **État WebSocket dynamique :** Les états des répéteurs actifs sont synchronisés en temps réel via le canal de contrôle WebSocket du serveur.

### 14. 📢 Expédier le système de diffusion de sonorisation publique (PA)
* **Diffusion audio à l'échelle du navire :** Les pilotes ou capitaines de navires à équipage multiple peuvent diffuser des annonces vocales à tous les membres d'équipage partageant le même « ContainerID » (navire) dans la même zone.
* **PA DSP et Klaxon Chime :** Les transmissions PA contournent la proximité locale et les sourdines de la radio (sauf le volume principal/la sourdine), jouent en mono avec panoramique central, ajoutent une alerte carillon/klaxon bicolore de science-fiction et appliquent un filtre passe-bande et de réverbération pour mégaphone simulant l'acoustique intérieure d'un navire creux.

---

## 🎮 Répartition de l'onglet Paramètres du client XuruVoip

La fenêtre des paramètres WPF est structurée en six catégories de configuration :
1. **Général** : configurez les langues, suivez les fichiers `Game.log`, activez/configurez la journalisation générale des fichiers et activez/configurez le **serveur HTTP et le port de l'application Companion** locaux.
2. **Connexion** : modifiez l'adresse IP du serveur cible, la position et les ports audio, le nom d'utilisateur, le mot de passe utilisateur et le mot de passe du serveur.
3. **Position** : basculez la source de localisation ("OCR Screen Scanner" ou "Game.log Reader (GRTPR)"), configurez les index du moniteur, les zones de recadrage, les intervalles OCR et prévisualisez le texte de coordonnées en direct.
4. **Audio** : choisissez le matériel d'entrée/sortie, ajustez les gains en dB, sélectionnez le mode de transmission (PTT vs VAD), configurez les seuils VAD, activez **Activer l'audio spatial 3D**, configurez la dégradation radio, les carillons locaux synthétisés, le modulateur de visière et sélectionnez les préréglages **Voice Changer**.
5. **Touches de raccourci** : associez les touches au PTT de proximité, au PTT radio, au profil PTT, à la visière du casque, au cycle de canal radio et aux commutateurs de sourdine de microphone et de canal audio individuels.
6. **Superposition** : activez la superposition du HUD, définissez l'emplacement des coins, activez le **Mini-radar tactique** (avec portée maximale configurable) et activez les **Légendes parole-texte** en temps réel.

---

## 🖥️ Serveur XuruVoip (Go)

Le serveur coordonne les positions des joueurs, gère l'authentification sécurisée et achemine dynamiquement les paquets audio en fonction de la distance spatiale et des canaux radio.

### Principales fonctionnalités

* **Contrôle de proximité côté serveur** : relaie dynamiquement l'audio de proximité uniquement aux joueurs à portée (50 m par défaut ou 5 m en mode chuchoté).
* **Configuration spatiale** : option basculable côté serveur (`XURUVOIP_SPATIAL_AUDIO` dans `.env`) qui détermine si les coordonnées ou uniquement la distance doivent être envoyées aux clients.
* **Routage radio multicanal** : permet aux joueurs d'écouter plusieurs chaînes radio simultanément tout en transmettant sur leur canal actif.
* **Système de profil audio** : attribue des effets audio (par exemple, filtre radio, écho) aux lecteurs.
* **SQLite Persistence** : stocke les préférences des canaux des joueurs et les mappages de profils lors des redémarrages du serveur.
* **Sécurité anti-contournement** : interdit les fauteurs de troubles par nom d'utilisateur, adresse IP et empreinte matérielle (HWID/MachineGuid) pour empêcher l'esquive des interdictions.
* **Portail d'administration Web** : interface Web sécurisée (HTTPS/WebSockets) pour les tableaux de bord en temps réel, la diffusion de journaux, la configuration des canaux/profils et la gestion des interdictions.
* **Carte radar d'administration du serveur** : radar de lecteur en temps réel 2D HTML5 Canvas intégré au tableau de bord d'administration, prenant en charge le panoramique par clic-glisser, le zoom avec la molette de la souris, le filtrage de zone active, les sentiers de randonnée historiques des joueurs (fil d'Ariane) et les anneaux d'ondes sonores concentriques pulsés en direct autour des joueurs qui parlent.
* **Rotation du journal de démarrage** : vérifie le journal du serveur (`xuruvoip.log`) au démarrage. Si le fichier journal contient des entrées d'un jour précédent, il est converti en « xuruvoip.YYYY-MM-DD.log ». Le serveur conserve uniquement les 5 fichiers pivotés les plus récents et supprime les plus anciens pour éviter une utilisation excessive du disque.

### Configuration du serveur (`.env`)

Au premier démarrage, le serveur génère automatiquement un fichier `.env` contenant ces valeurs par défaut :```env
# BIND IP address and server ports
# Leave IP empty to listen on all interfaces (0.0.0.0)
XURUVOIP_SERVER_IP=
XURUVOIP_PORT=8888
XURUVOIP_AUDIO_PORT=8889
XURUVOIP_DATA_DIR=.

# Maximum Server Capacity (can be higher, depends on server performances)
XURUVOIP_MAX_PLAYERS=500

# Spatial Audio (1 = enabled and transmits coordinates, 0 = disabled and transmits distance only)
XURUVOIP_SPATIAL_AUDIO=1

# Public Server Settings (1 = players will not need to enter the server password to connect, 0 = required)
XURUVOIP_PUBLIC_SERVER=0

# Server Password / Token for player connections (only if public server is disabled)
XURUVOIP_SERVER_PASSWORD=auto_generated_32_chars_token

# Admin Server Password / Token for the admin portal page (https://[XURUVOIP_SERVER_IP]:[XURUVOIP_PORT]/admin)
XURUVOIP_ADMIN_SERVER_PASSWORD=auto_generated_32_chars_token

# Verbose logging level (0 = none, 1 = default, 2 = global frames per type, 3 = detailed channels/profiles)
XURUVOIP_VERBOSE_LOGS=1

# Security Settings (Rate Limiting and IP Lockout)
XURUVOIP_LIMIT_RATE_POS=50.0
XURUVOIP_LIMIT_BURST_POS=100
XURUVOIP_LIMIT_RATE_AUDIO=60.0
XURUVOIP_LIMIT_BURST_AUDIO=120

XURUVOIP_LOCKOUT_ATTEMPTS=5
XURUVOIP_LOCKOUT_WINDOW=60
XURUVOIP_LOCKOUT_DURATION=600

# Dynamic Intercom and Immersion features (1 = enabled, 0 = disabled)
XURUVOIP_ENABLE_INTERCOM=1
XURUVOIP_ENABLE_EVA_MUTING=1
XURUVOIP_ENABLE_RADIO_REPEATERS=1
XURUVOIP_ENABLE_SHIP_PA=1

# Discord Voice Bridge Settings (1 = enabled, 0 = disabled)
XURUVOIP_ENABLE_DISCORD_BRIDGE=1
XURUVOIP_DISCORD_TOKEN=your_discord_bot_token
XURUVOIP_DISCORD_GUILD_ID=your_discord_guild_id
XURUVOIP_DISCORD_CHANNEL_ID=your_discord_channel_id
XURUVOIP_DISCORD_BRIDGE_CHANNEL=General
```
### 🎛️ Guide de configuration du pont vocal Discord

Pour relier un canal radio du serveur Go local à un canal vocal Discord, suivez ces étapes de configuration :

1. **Créez une application Discord Bot :**
   * Visitez le [Portail des développeurs Discord](https://discord.com/developers/applications) et connectez-vous.
   * Cliquez sur **Nouvelle application**, donnez-lui un nom (par exemple, « XuruVOIP Bridge ») et cliquez sur **Créer**.
   * Accédez à l'onglet **Bot** dans la barre latérale gauche, cliquez sur **Réinitialiser le jeton** et copiez le **Bot Token** généré. Collez-le sous le nom « XURUVOIP_DISCORD_TOKEN » dans le fichier « .env » de votre serveur.
   * Sous **Privileged Gateway Intents** sur la même page du bot, activez **Message Content Intent** (obligatoire pour lire des commandes spécifiques).

2. **Invitez le bot sur votre serveur Discord :**
   * Accédez à l'onglet **OAuth2**, puis sélectionnez **Générateur d'URL**.
   * Sous **Scopes**, cochez « bot » et « applications.commands ».
   * Sous **Autorisations du robot**, sélectionnez les privilèges suivants :
     * *Autorisations générales :* « Afficher les chaînes »
     * *Autorisations de texte :* « Envoyer des messages »
     * *Autorisations vocales :* « Se connecter », « Parler », « Utiliser l'activité vocale »
   * Copiez l'URL générée en bas de la page, collez-la dans un navigateur Web, sélectionnez votre serveur Discord cible (guilde) et cliquez sur **Autoriser**.

3. **Obtenir les identifiants du serveur (guilde) et des canaux vocaux :**
   * Ouvrez Discord, accédez à **Paramètres utilisateur** -> **Avancé** et activez le **Mode développeur**.
   * Cliquez avec le bouton droit sur l'icône de votre serveur Discord dans la liste des serveurs et sélectionnez **Copier l'ID du serveur** (il s'agit de votre identifiant de guilde). Collez-le sous `XURUVOIP_DISCORD_GUILD_ID` dans `.env`.
   * Cliquez avec le bouton droit sur le canal vocal Discord cible auquel vous souhaitez que le bot rejoigne et sélectionnez **Copier l'ID du canal**. Collez-le sous `XURUVOIP_DISCORD_CHANNEL_ID` dans `.env`.

4. **Canal radio du serveur Map Go :**
   * Configurez `XURUVOIP_DISCORD_BRIDGE_CHANNEL` avec le nom exact du canal radio que vous souhaitez ponter (par exemple `Général`, `Bravo`, `Alpha`, etc.). Tout audio transmis sur la fréquence radio de ce serveur Go sera diffusé de manière bidirectionnelle sur le canal vocal Discord !

### Construire le serveur à partir des sources

####Linux```bash
cd server


GOOS="linux" GOARCH="amd64" go build .
# a "server" linux binary will be created in the current directory
```
#### Fenêtres```powershell
cd server 

$env:GOOS="windows"
$env:GOARCH="amd64"
go build .
# a "server.exe" windows binary will be created in the current directory
```
### Exécution du serveur

#### De la source :```bash
cd server
go run .
```
#### Depuis le binaire :
##### Fenêtres```powershell
.\server.exe
```
#####Linux```bash
./server
```
### 🖥️ Configuration et déploiement de serveurs sans tête

Pour les installations sans tête permanentes et prêtes pour la production, le serveur doit s'exécuter en tant que démon/service système en arrière-plan qui démarre automatiquement au démarrage et redémarre en cas de panne.

#### 1. Configuration du réseau et du pare-feu
Assurez-vous que les ports TCP entrants définis dans votre fichier `.env` (les valeurs par défaut sont `8888` pour le portail positions/admin et `8889` pour l'audio spatial) sont ouverts sur votre pare-feu hôte :
* **Linux (UFW) :**  ```bash
  sudo ufw allow 8888/tcp
  sudo ufw allow 8889/tcp
  sudo ufw reload
  ```
* **Linux (pare-feu) :**  ```bash
  sudo firewall-cmd --zone=public --add-port=8888/tcp --permanent
  sudo firewall-cmd --zone=public --add-port=8889/tcp --permanent
  sudo firewall-cmd --reload
  ```
---

#### 2. Déploiement Linux (systemd)

Suivez ces étapes pour déployer le serveur Go en tant que service systemd :

##### Étape A : Configurer le répertoire et les autorisations
Créez un utilisateur système dédié et un répertoire de travail pour l'isolation de sécurité :```bash
# Create a system user without login privileges
sudo useradd -r -s /bin/false xuruvoip

# Create installation directory and copy the binary
sudo mkdir -p /opt/xuruvoip
sudo cp xuruvoip-server-linux-x64 /opt/xuruvoip/xuruvoip-server
sudo chmod +x /opt/xuruvoip/xuruvoip-server

# Set ownership to the system user
sudo chown -R xuruvoip:xuruvoip /opt/xuruvoip
```
##### Étape B : Générer et configurer `.env`
Exécutez le serveur une fois sous l'utilisateur système pour générer le fichier de configuration et la base de données `.env` par défaut :```bash
sudo -u xuruvoip /opt/xuruvoip/xuruvoip-server -port 8888 -audio-port 8889
```
*Appuyez sur « Ctrl+C » une fois que la console a imprimé les mots de passe générés.* Ensuite, modifiez le fichier « .env » généré pour personnaliser les paramètres (par exemple, mots de passe, adresse IP de liaison, bascule audio spatiale) :```bash
sudo nano /opt/xuruvoip/.env
```
##### Étape C : Créer le fichier de service systemd
Copiez le fichier de service du dépôt `server/xuruvoip.service` vers `/etc/systemd/system/xurvoip-server.service` ou créez un nouveau fichier de configuration de service `/etc/systemd/system/xurvoip-server.service` avec le contenu suivant :```ini
[Unit]
Description=XuruVoip Star Citizen Spatial VOIP Server
After=network.target

[Service]
Type=simple
User=xuruvoip
Group=xuruvoip
WorkingDirectory=/opt/xuruvoip
ExecStart=/opt/xuruvoip/xuruvoip-server
Restart=always
RestartSec=5
LimitNOFILE=65536

[Install]
WantedBy=multi-user.target
```
##### Étape D : Activer et démarrer le service```bash
# Reload systemd daemon to pick up the new unit file
sudo systemctl daemon-reload

# Enable the service to run on startup
sudo systemctl enable xuruvoip-server

# Start the service immediately
sudo systemctl start xuruvoip-server
```
##### Étape E : Surveillance et journaux
Pour vérifier l'état du service et diffuser les journaux :```bash
# Check status
sudo systemctl status xuruvoip-server

# Stream log files in real-time
journalctl -u xuruvoip-server -f -n 100
```
---

#### 3. Déploiement Windows (NSSM)

Pour exécuter le serveur en tant que service Windows natif en mode sans tête, il est recommandé d'utiliser le **Non-Sucking Service Manager (NSSM)** :

##### Étape A : Configurer les répertoires
Extrayez/copiez « xuruvoip-server-windows-x64.exe » dans un dossier de serveur dédié (par exemple « C:\XuruVoipServer »).

##### Étape B : initialiser la configuration
Ouvrez un terminal PowerShell en tant qu'administrateur et exécutez le binaire une fois pour générer les fichiers :```powershell
cd C:\XuruVoipServer
.\xuruvoip-server-windows-x64.exe
```
*Appuyez sur « Ctrl+C » une fois le démarrage terminé.* Personnalisez le fichier « .env » généré selon vos besoins.

##### Étape C : Installer le service via NSSM
Téléchargez NSSM et installez le service en exécutant :```powershell
# Open NSSM GUI installer
.\nssm.exe install XuruVoipServer "C:\XuruVoipServer\xuruvoip-server-windows-x64.exe"
```
Dans la fenêtre contextuelle NSSM, configurez :
* **Chemin :** `C:\XuruVoipServer\xuruvoip-server-windows-x64.exe`
* **Répertoire de démarrage :** `C:\XuruVoipServer`
* Cliquez sur **Installer le service**.

##### Étape D : Démarrer le service
Démarrez le service à l'aide de PowerShell ou de Services Manager (`services.msc`) :```powershell
Start-Service -Name XuruVoipServer
```
---

### Créer et exécuter le client

#### Exigences
- Windows 10/11
- SDK .NET 9.0 (prise en charge WPF)

#### Compiler et exécuter :```powershell
cd client
dotnet run
```
### Installation du package de version

Étant donné que le programme d'installation et les exécutables ne sont pas signés numériquement, Windows SmartScreen peut les bloquer initialement. Vous pouvez facilement les débloquer en utilisant le menu des propriétés.

* **Option A : programme d'installation MSI (recommandé)**
  1. Téléchargez « XuruVoipClient-win-x64.msi » depuis la [page des versions](https://github.com/XuruDragon/XuruVOIP/releases).
  2. Pour empêcher Windows SmartScreen de bloquer l'installation :
     - Cliquez avec le bouton droit sur le fichier téléchargé « XuruVoipClient-win-x64.msi » et sélectionnez **Propriétés**.
     - Dans la fenêtre des propriétés sous l'onglet *Général*, cochez la case **Débloquer** en bas.
     - Cliquez sur **Appliquer**, puis fermez la fenêtre Propriétés.
  3. Double-cliquez sur le fichier pour exécuter le programme d'installation et suivez les instructions affichées.
     *(Remarque : vous verrez une invite standard de contrôle de compte d'utilisateur Windows « Éditeur inconnu » ; cliquez simplement sur **Oui** ou **Exécuter** pour continuer.)*

* **Option B : version ZIP portable**
  1. Téléchargez « XuruVoipClient-win-x64.zip » depuis la [page des versions](https://github.com/XuruDragon/XuruVOIP/releases).
  2. Extrayez les fichiers du package ZIP dans n'importe quel dossier de votre choix (par exemple, `C:\Games\XuruVoip`) :
  3. Cliquez ensuite avec le bouton droit sur le fichier extrait « XuruVoipClient.exe » et sélectionnez **Propriétés**.
     - Dans la fenêtre des propriétés sous l'onglet *Général*, cochez la case **Débloquer** en bas.
     - Cliquez sur **Appliquer**, puis fermez la fenêtre Propriétés.
  4. Double-cliquez sur « XuruVoipClient.exe » pour exécuter le client directement sans l'installer.

## 📱 Intégration de l'application Companion et du Stream Deck

XuruVOIP comprend un service Web Companion App intégré et un plugin Stream Deck officiel vous permettant de surveiller et de déclencher des actions vocales directement à partir d'appareils secondaires ou de clés physiques.

### 1. Activation de l'application compagnon et du MFD de la carte tactique
Par défaut, le serveur HTTP local de l'application compagnon et le mode carte tactique sont désactivés pour économiser les ressources système. Pour les activer :
1. Ouvrez le client XuruVOIP et cliquez sur l'icône **Paramètres**.
2. Dans l'onglet **Général**, cochez la case **Activer le serveur HTTP compagnon** (port par défaut : `8891`).
3. Pour activer l'affichage radar, cochez la case imbriquée **Activer la carte de copilote tactique (MFD)**.
4. Cliquez sur **Enregistrer et fermer** pour appliquer.
5. Accéder au tableau de bord : Ouvrez `http://localhost:8891` dans un navigateur sur votre PC, tablette ou téléphone. Si le mode carte est activé, un nouvel onglet **🗺️ Carte tactique** sera disponible, affichant un écran radar HUD basé sur Canvas qui suit la position en temps réel de votre personnage, son cap, les contacts de l'équipage dans la même zone et les indicateurs d'activité du haut-parleur.

---

### 2. Installation du plugin Stream Deck
Le package de version inclut le fichier `.streamDeckPlugin` préemballé.
1. Téléchargez « com.xuru.voip.streamDeckPlugin » depuis la [page des versions](https://github.com/XuruDragon/XuruVOIP/releases).
2. Double-cliquez sur le fichier pour l'installer directement sur votre logiciel Elgato Stream Deck. 
   *(Vous pouvez également extraire et copier manuellement le dossier `com.xuru.voip.sdPlugin` dans `%appdata%\Elgato\StreamDeck\Plugins\`)*
3. Une fois installée, une nouvelle catégorie d'action appelée **XuruVOIP** apparaîtra dans la liste de droite de votre application de bureau Stream Deck.

---

### 3. Ajout et configuration d'actions
Vous pouvez glisser et déposer l'une des 13 actions suivantes sur vos touches Stream Deck :
* 🎤 **Proximity Mute** : active la mise en sourdine du microphone de proximité sortant.
* 📻 **Radio Mute** : active la mise en sourdine du microphone radio sortant.
* 👤 **Profile Mute** : active la désactivation du microphone du profil sortant.
* 🔊 **Audio Proximity Mute** : active la mise en sourdine de la lecture de proximité entrante.
* 🔊 **Audio Radio Mute** : active la sourdine de la lecture de la radio entrante.
* 🔊 **Audio Profile Mute** : active la mise en sourdine de la lecture du profil entrant.
* 🪖 **Basculer le casque** : Bascule la visière de votre casque de combinaison spatiale vers le bas ou vers le haut.
* 🔄 **Cycle Radio** : passe d'une chaîne de radio disponible à l'autre.
* 📢 **PA Broadcast** : Touche Push-to-Talk pour diffuser sur le système d'adresse publique (PA) du vaisseau.
* 📡 **Beacon Mode** : Active/désactive le mode relais radio / balise.
* 🎙️ **Voice Command Macro** : Déclenche une macro de commande vocale personnalisée simulée en arrière-plan (configurable via les paramètres).
* 💬 **Intercom Status** : Affiche l'état de l'interphone du vaisseau (`NORMAL`, `SHIELD HIT`, `CRIT PWR`, `QUANTUM`) et fait défiler les états de simulation lorsque vous appuyez dessus.
* 🗺️ **Location Telemetry** : Affiche votre zone système actuelle et la télémétrie des coordonnées $(X, Y, Z)$ sur la touche.

#### Configuration (inspecteur de propriétés) :
Pour chaque action que vous faites glisser sur une touche, cliquez dessus et configurez les paramètres dans le panneau **Property Inspector** en bas :
* **Companion Port** : Définissez-le pour qu'il corresponde au port configuré dans les paramètres de votre client WPF (par défaut : `8891`).
* **Voice Command** (Voice Command Macro uniquement) : Saisissez la commande textuelle à exécuter (ex. `"close visor"`, `"open hangar"`).
* **Rétroaction dynamique** : Les actions mettent à jour leurs icônes et leurs états en temps réel. Les bascules s'affichent en cyan/rouge, l'état de l'interphone fait défiler 4 états et la télémétrie de localisation affiche les coordonnées.
* **Affichage de la fréquence en direct** : La touche **Cycle Radio** affichera dynamiquement le nom de la fréquence actuellement active (par exemple « 120,5 » ou « Général ») directement sur le bouton physique en temps réel !

---

## 👥 Crédits

Développé par **[@XuruDragon](https://github.com/XuruDragon)** en collaboration avec **Antigravity IDE**.