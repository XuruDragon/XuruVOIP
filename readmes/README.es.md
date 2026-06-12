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
  <b>Traducciones:</b><br/>
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

XuruVoip es una **suite de comunicación de voz (VoIP) 3D** de alto rendimiento, segura y espacializada dinámicamente diseñada específicamente para integraciones de juegos personalizados con **Star Citizen**. Consiste en un servidor backend basado en Go y un cliente WPF C# moderno con una aplicación complementaria incorporada (interfaz web) e integración de Elgato Stream Deck.

### 🎯 Objetivo del proyecto
El objetivo de XuruVoip es proporcionar eventos de juegos de Star Citizen, organizaciones de juegos de rol y escuadrones tácticos con un **nivel sin precedentes de inmersión de audio y conveniencia operativa**. Al leer en tiempo real los estados de coordenadas, visores y vehículos del cliente del juego, XuruVoip da forma dinámicamente a las voces de los jugadores en el espacio 3D, simula atmósferas planetarias/de vacío y enruta las comunicaciones tácticas automáticamente sin necesidad de configuraciones manuales del cliente.

---

### 🗺️ Directorio de navegación

| Sección | Descripción |
| :--- | :--- |
| [📖 Guía detallada de funciones](../doc/functionnalities.md) | Explicación técnica y de usuario de las más de 20 funciones implementadas. |
| [📖 Guías de usuario no técnicas](#-guías-de-usuario-no-técnicas) | Guías paso a paso fáciles de entender para Cliente, Servidor y Stream Deck. |
| [📸 Capturas de pantalla y interfaz de usuario](#-capturas-de-pantalla-y-interfaz-de-usuario) | Muestra visual de las pantallas de los clientes, el portal de administración y la configuración. |
| [🗂️ Estructura del proyecto](#️-project-structure) | Diseño del repositorio y desglose de carpetas. |
| [⚙️ Arquitectura del sistema](#️-system-architecture) | El diagrama de flujo de trabajo real completo del cliente WPF, el servidor Go y los dispositivos externos. |
| [💡 Descripción general de las funciones principales](#-descripción-general-de-las-funciones-principales) | Desglose detallado de las más de 19 funciones espaciales y de redes implementadas. |
| [🖥️ Ir al servidor (Ir)](#️-xuruvoip-server-go) | Instrucciones de construcción, ejecución, implementación y configuración del servidor. |
| [🎛️ Puente de voz de Discord](#️-discord-voice-bridge-setup-guide) | Conexión de canales de radio del servidor Go a un canal de voz de Discord. |
| [📱 Aplicación complementaria y plataforma de transmisión](#-integración-de-aplicación-complementaria-y-plataforma-de-transmisión) | Control remoto de dispositivos y configuración de teclas físicas de Stream Deck. |
| [🛠️ Cliente WPF (C#)](#-building--running-the-client) | Requisitos del cliente, compilación y guías de instalación de MSI/portátil. |

---

## 📖 Guías de usuario no técnicas

Si no tiene experiencia en informática, hemos escrito guías sencillas paso a paso para ayudarle a configurar todo y ejecutarlo fácilmente:

* 📖 **[Guía detallada de funciones](../doc/functionnalities.md)**: Explicación detallada de cada función implementada, cómo funcionan, cómo usarlas y por qué son útiles.
* 🎮 **[Guía del usuario del cliente](doc/client_guide.md)**: Guía sencilla sobre cómo elegir micrófonos/altavoces, configurar Push-to-Talk, usar cascos de trajes espaciales y activar efectos de voz de esfuerzo.
* 🖥️ **[Guía de configuración del servidor](doc/server_guide.md)**: Explica cómo alojar un servidor, ajustar contraseñas/configuraciones en el archivo de configuración `.env` y configurar Discord Voice Bridge.
* 🎛️ **[Guía del complemento Stream Deck](doc/streamdeck_guide.md)**: Tutorial sobre la instalación de botones físicos para silenciar, alternar la visera y mostrar canales de radio activos.

---

## 📸 Capturas de pantalla y interfaz de usuario

<details>
<summary>📸 Haga clic para ver capturas de pantalla</summary>

### 1. Ventana principal del cliente
![Ventana principal del cliente](/screenshots/main.png)

### 2. Pestaña Configuración de audio (Control de audio espacial 3D)
![Pestaña Configuración de audio](/screenshots/audio.png)

### 3. Pestaña Configuración general (selección de idioma y registro de juego)
![Pestaña Configuración general](/screenshots/general.png)

### 4. Pestaña Configuración de conexión
![Pestaña Configuración de conexión](/screenshots/connection.png)

### 5. Pestaña Configuración de teclas de acceso rápido
![Pestaña Configuración de teclas de acceso rápido](/screenshots/hotkeys.png)

### 6. Pestaña Configuración de superposición (Vulkan y DirectX HUD)
![Pestaña Configuración de superposición](/screenshots/overlay.png)

### 7. Pestaña Configuración de OCR (Tesseract OCR)
![Pestaña Configuración de OCR](/screenshots/ocr.png)

### 8. Página de inicio de sesión del portal web de administración
![Página de inicio de sesión del portal web de administración](/screenshots/admin_login.png)

### 9. Panel de control del portal web de administración
![Panel del portal web de administración](/screenshots/admin_dashboard.png)

### 10. Administrador de jugadores del portal web
![Reproductores del portal web de administración](/screenshots/admin_players_list.png)

### 11. Lista de administradores del portal web de administración
![Lista de administradores del portal web de administración](/screenshots/admin_admin_list.png)

### 12. Lista de prohibiciones del portal web de administración
![Lista de prohibiciones del portal web de administración](/screenshots/admin_ban_list.png)

</details>

---

## 🗂️ Estructura del proyecto

- **/servidor**: Backend Go de alto rendimiento que aloja los servicios de puesto, audio y administración.
- **/client**: cliente WPF de C# moderno que utiliza NAudio, WebRtcVad y Tesseract OCR o Game.log tail para el seguimiento automatizado de la ubicación y el análisis de registros. La aplicación complementaria también se incluye en este proyecto.
- **/streamdeck**: complemento Stream Deck para el cliente XuruVoIP.

---

## ⚙️ Arquitectura del sistema

A continuación se muestra la arquitectura real completa del sistema XuruVoip, que ilustra los bucles de captura, posicionamiento, reproducción y representación de HUD dentro del cliente WPF, los concentradores websocket del servidor Go y las integraciones externas:```mermaid
graph TB
    subgraph STIM ["Entorno de juego (Star Citizen)"]
        SC["Cliente Star Citizen"]
        LOGS["Game.log (archivo de registro)"]
        SCREEN["Salida de gráficos (Vulkan/DX)"]
    end

    subgraph WPF ["Cliente XuruVOIP WPF"]
        direction TB
        subgraph CAPT ["Captura de micrófono y DSP"]
            MIC["Entrada de micrófono"] --> VAD["WebRTC VAD"]
            VAD -->|Speech Detected| VC["Cambiador de voz (Alien/Cyborg/Robot)"]
            VC -->|Modulated PCM| GF_FIL["G-Force Pitch & Tremolo / Inyección de jadeo de esfuerzo"]
            GF_FIL --> HELM_OSC["Superposición de zumbidos de ventilación y respiración del casco"]
            HELM_OSC --> OPUS_ENC["Codificador de obra"]
        end

        subgraph POS_TRACK ["Posicionamiento y seguimiento de estado"]
            LOGS -->|Tail Scanner| LOG_PAR["Analizador de Game.log"]
            SCREEN -->|showlocations Capture| OCR["Motor OCR Tesseract"]
            LOG_PAR -->|Equip/Visor Events| HELM_DET["Sincronización automática del estado del visor"]
            LOG_PAR -->|G-Force & Stamina Values| GF_DET["Rastreador de esfuerzo y fuerza G"]
            OCR -->|Coords| POS_SEL{"Selector de fuente"}
            LOG_PAR -->|Coords & ContainerID| POS_SEL
        end

        subgraph PLAY ["Reproducción espacial y DSP"]
            OPUS_DEC["Decodificador de obra"] --> PKT_TYPE{"¿Tipo de paquete?"}
            PKT_TYPE -->|PA 0x03| PA_FIL["Megáfono DSP (HP/LP, distorsión tanh, reverberación de barco)"]
            PKT_TYPE -->|Proximity/Radio| OCC_FIL["Cubierta Carrack/Hercules y oclusión de habitaciones"]
            OCC_FIL --> REV_FIL["Reverberación con reconocimiento de ubicación (cuevas/bunkers/hangares)"]
            REV_FIL --> RAD_FIL["Paso de banda de radio y enrutamiento multisalto de largo alcance (Dijkstra)"]
            RAD_FIL --> CHIMES["Generador de chirridos de micrófono PTT y cola de silenciamiento"]
            CHIMES --> PAN["Matemáticas de panorámica espacial 3D"]
            PAN --> VOL["Atenuación de distancia espacial"]
            VOL --> MIXER["Mezclador de audio"]
            PA_FIL --> MIXER
            MIXER --> SPK["Dispositivos de salida de audio"]
        end

        subgraph HUD ["Superposición de HUD (clic de Win32)"]
            T_RAD["Miniradar táctico 2D"]
            STT["Whisper.net Voz a Texto"]
            OPUS_DEC -.->|Incoming Voice| STT
            STT -->|Subtitles| SUB["Subtítulos HUD en tiempo real"]
        end

        subgraph COMP ["Servidor web complementario"]
            HTTP_SRV["Escucha HTTP local (puerto personalizado)"]
            DASH["Panel de control Glassmorphic HTML/JS"]
        end

        POS_SEL -->|Coordinates & Zone| POS_WS["Posición cliente WS"]
        HELM_DET -->|Visor State| POS_WS
        GF_DET -->|G-Force / Exertion| GF_FIL
        OPUS_ENC -->|Audio Packets| AUD_WS["Cliente WS de audio"]
    end

    subgraph SERVER ["Servidor XuruVOIP Go"]
        direction TB
        WS_HUB["Centro de conexión Websocket"]
        POS_HUB["Posicionamiento espacial y centro de zonas"]
        DB["Base de datos SQLite y canales persistentes"]
        DISC_BRIDGE["Puente de voz de discordia"]
        ADM_PORT["Portal web de administración (Canvas Live Radar)"]

        WS_HUB <--> POS_HUB
        POS_HUB <--> DB
        DISC_BRIDGE <--> WS_HUB
    end

    subgraph EXT ["Interfaces externas"]
        DISC["Canal de voz de discordia"] <-->|Bidirectional Voice Bridge| DISC_BRIDGE
        SD["Aplicación Stream Deck"] <-->|WebSocket Actions / Port Setting| HTTP_SRV
        MOB["Controlador móvil"] <-->|REST API Status & Toggles| HTTP_SRV
    end

    POS_WS <-->|WS Port 8888| WS_HUB
    AUD_WS <-->|WS Port 8889| WS_HUB
```
---

## 💡 Descripción general de las funciones principales

### 1. 🔊 Audio espacial 3D en tiempo real
* **Panorámica estéreo dinámica:** PROYECTA las coordenadas del altavoz remoto en los vectores de dirección hacia adelante y hacia la derecha del oyente para calcular la panorámica izquierda/derecha exacta usando una fórmula de potencia constante.
* **Resolución de ambigüedad frontal-posterior:** Atenúa el volumen de audio en un 25% si un orador está detrás del oyente, resolviendo las limitaciones estándar de panorámica de audio 2D.
* **Reducción de distancia:** Desvanece las voces de proximidad linealmente según la distancia, lo que garantiza niveles de volumen naturales (se desvanece completamente a cero a 50 metros o 5 metros para susurros).

### 2. 🗺️ Acústica basada en la ubicación y oclusión de barcos/búnkeres
* **Oclusión de terrazas y paredes:** Detecta límites internos dentro de espacios. Si los jugadores están en diferentes decks (por ejemplo, Carrack, Hercules) o salas (por ejemplo, Bunkers), se aplican dinámicamente el filtrado de paso bajo (frecuencias de corte de 300 Hz a 900 Hz) y la amortiguación de volumen.
* **Reverberación ambiental:** Lee la zona jerárquica del reproductor y aplica automáticamente parámetros personalizados de mezcla húmeda, retardo y reverberación de retroalimentación para **Cuevas**, **Bunkers** y **Hangars**.

### 3. 💨 Simulación atmosférica de casco y EVA
* **Silenciamiento de EVA:** Silencia automáticamente las comunicaciones de voz de proximidad en el espacio o zonas de vacío (EVA), lo que obliga a los jugadores a utilizar canales de radio para comunicarse.
* **Superposición del respirador con visera:** Simula la presión del aire cuando la visera está bajada. Sintetiza un silbido de respiración de baja frecuencia y un zumbido de ventilador de traje de doble frecuencia (50 Hz + 100 Hz) en la alimentación del micrófono capturado.
* **Sincronización automática de la visera:** Lee los registros adjuntos en `Game.log` para detectar automáticamente cuándo se coloca o se quita un casco y actualiza el estado de la visera en tiempo real.

### 4. 🎙️ Cambiador de voz y moduladores de traje de ciencia ficción
* **Filtros DSP en tiempo réel :** Cambio de tono en el dominio del tiempo, flanger, modulación en anillo, saturación soft-tanh y bitcrushing de 8 bits.
* **Ajustes preestablecidos atmosféricos :** Cargue instantáneamente perfiles de voz predefinidos, incluidos **Alien**, **Cyborg**, **Robotic** o **Custom Pitch Shift** (0.5x a 2.0x).
* **Deslizadores de modulador personalizados :** Ajuste con precisión el cambio de tono, la frecuencia/mezcla del modulador en anillo, la profundidad/tasa/retroalimentación del flanger y la configuración del bitcrush mediante los deslizadores de parámetros.

### 5. 📻 Degradación de radio inmersiva y timbres
* **Filtrado de paso de banda:** Modelos con filtros de radio con cortes bajos/altos cuando se usan canales de radio o cuando los visores del traje están bajados.
* **Degradación de la señal de radio:** Bandas de corte estrechas y mezclas de ruido estático filtrado por paso de banda a medida que la distancia entre los reproductores se acerca al límite del transmisor de radio.
* **Timbres acústicos de radio:** Reproduce timbres mecánicos al activar y desactivar la transmisión en canales de radio. Admite cuatro perfiles matemáticos distintos seleccionables en la configuración o la aplicación complementaria: Militar (barridos senoidales), Industrial (clics mecánicos pesados), Alienígena (barridos modulados en anillo) y Vintage (clics de relé analógico).

### 6. 💬 Sistema automático de intercomunicación para barcos
* **Canales de intercomunicación del vehículo:** Al abordar un vehículo, los jugadores se suscriben automáticamente a un canal de radio dinámico `Intercom_<ContainerID>`.
* **Agachamiento de prioridad del piloto:** Cuando un jugador en la cabina o en el asiento del conductor transmite por el intercomunicador, el audio de proximidad de todos los demás jugadores se reduce en un 85% para garantizar la claridad del comando de vuelo.
* **Degradación dinámica del intercomunicador:** Los canales del intercomunicador se degradan automáticamente según el estado del vehículo:
  * **Impactos en el escudo (Shield Hits):** Inyecta temporalmente ráfagas de estática y crujidos de volumen (dura 2,5 segundos).
  * **Energía crítica (Critical Power):** Zumbido de CA de bajo voltaje, distorsión de saturación y caída del tono de voz (resampling).
  * **Viaje cuántico (Quantum Travel):** Barrido de filtro en peine (flanger/phaser) y pitido de alta frecuencia.
  * *Todos los sub-efectos se pueden activar/desactivar individualmente en la configuración general y están desactivados por defecto.*
* **Enfriamiento de limpieza:** Cuenta regresiva 5 minutos después de que el último jugador abandona el barco antes de eliminar el canal de intercomunicación, lo que maximiza el rendimiento del servidor.

### 7. 📡 Superposición de HUD y radar táctico 2D compatible con Vulkan
* **Superposición de Win32 Click-Through :** Una superposición de HUD sin bordes que muestra las conexiones de VoIP, las frecuencias y los estados de voz. Compatible con Vulkan y DirectX (ejecutándose en modo de ventana sin bordes).
* **Personalizador interactivo de HUD:** Permite la personalización en tiempo real del tema (Aegis, Anvil, Drake, RSI, Origin), la posición (esquinas/centro) y la visibilidad de los componentes (mini radar, lista de altavoces, encabezado de conexión) a través de los ajustes o la aplicación complementaria.
* **Indicador de estado del intercomunicador:** Muestra advertencias como `⚡ INTERCOM: DEGRADED` (con detalles de subestado como `[Power Loss]`, `[Quantum]` o `[Static Pop]`) en la superposición de HUD cuando la degradación del intercomunicador está activa.
* **Mini radar táctico :** Cuenta con un radar HUD 2D alineado con el rumbo que muestra a los jugadores que hablan en relación con usted, dibujando anillos de sonido pulsantes a su alrededor.
* **Indicadores de elevación 3D :** Agrega flechas de dirección verticales y deltas de altura de cubierta (p. ej., `Bob (▲ 12m)`) junto a los iconos del radar cuando la separación vertical supera los 2 metros.
* **Subtítulos de voz a texto:** Transcribe el audio entrante de radio/proximidad a subtítulos de HUD localizados utilizando un modelo de Whisper ligero y sin conexión (`ggml-tiny.bin`).
* **Comandos de voz PTT manos libres:** Mantener presionada la tecla dedicada de comandos de voz silencia temporalmente las transmisiones salientes de proximidad/radio y almacena el audio del micrófono en búfer. Al soltarla, la voz se transcribe localmente mediante el modelo Whisper para activar acciones de la nave:
  * **Comandos compatibles:** Alternar visor/casco, silenciar/activar micrófono (proximidad/radio/perfil/todo), selección de canal de radio activo y preajustes del modulador de voz.
  * **Coincidencia de palabras clave multilingüe:** Compatible con 8 idiomas (inglés, francés, alemán, español, portugués, japonés y chino).
  * **Filtro de umbral de confianza:** Un control deslizante configurable filtra coincidencias de baja confianza o habla irrelevante.
  * *Desactivado por defecto; habilitarlo descargará el modelo de transcripción Whisper fuera de línea (~140 MB) si aún no está presente.*

### 8. 📱 Aplicación complementaria y API REST
* **Servidor web HTTP local:** Alberga un panel local en un puerto configurable (predeterminado: `8891`, deshabilitado de forma predeterminada).
* **Controlador Glassmorphic:** Se conecta desde teléfonos o pantallas secundarias para alternar silencios, ciclos de canales, cascos o cambiadores de voz.
* **API REST:** Expone los puntos finales `GET /api/status` y `POST /api/action` para integraciones externas (incluyendo el estado del intercomunicador y anulaciones de simulación).

### 9. 🎛️ Complemento Stream Deck
* **Paquete de acción Stream Deck:** Expone 8 acciones para controlar el silenciamiento del micrófono, el silenciamiento del audio, los visores del casco y los ciclos de radiofrecuencia.
* **Iconos de teclas dinámicas:** Gráficos del botón de actualización continua de WebSockets (cian activo frente a ámbar apagado) para reflejar el estado actual del cliente.
* **Título de frecuencia en vivo:** Muestra los nombres de los canales de radio activos directamente en los botones físicos de Stream Deck.

### 10. 🔌 Puente de voz de Discord
* **Relé de audio bidireccional :** Transmite las comunicaciones entre un canal de radio del servidor Go y un canal de voz de Discord.
* **Mapeo de apodos :** Captura el habla de Discord y mapea los ID de SSRC a los apodos del servidor.
* **Seguimiento dinámico de frecuencia :** Mueve automáticamente la conexión de voz del puente de Discord para seguir y reflejar el canal activo del líder configurado o los perfiles Command/Leader.

### 11. 🛡️ Seguridad, rotación de registros y radar de lienzo de administración
* **Rotación diaria de registros:** El archivador de registros de inicio conserva solo los 5 registros más recientes.
* **Panel de administración:** Panel de administración web en tiempo real con seguridad de bloqueo, limitación de velocidad y un mapa interactivo 2D HTML5 Canvas Live Radar que permite a los administradores hacer zoom, desplazarse y rastrear rutas históricas de jugadores.

### 12. 🤢 Distorsión de la voz por fuerza G y esfuerzo físico
* **Trémolo y cambio de tono:** Bajo fuerzas G altas, el audio del micrófono saliente se modula dinámicamente con un LFO de trémolo (4-10 Hz, hasta 40 % de profundidad) y se afina (factor: 1,0 hasta 0,85) para simular estados de tensión física, apagón o redout.
* **Superposición de respiración intensa:** Superpone automáticamente el ruido aleatorio de jadeo/respiración, escalando la velocidad del ciclo de respiración según los niveles de resistencia del jugador analizados en tiempo real desde `Game.log`.
* **Controles manuales/API:** Se puede alternar a través de la configuración del cliente y los controles deslizantes de la interfaz de usuario web de la aplicación complementaria para juegos de rol o pruebas simuladas.

### 13. 📡 Retransmisión de radio táctica y balizas repetidoras de saltos múltiples
* **Enrutamiento de señal de múltiples saltos:** Los jugadores pueden alternar el "Modo de baliza" para que actúe como una baliza repetidora de radio. Si dos jugadores están fuera del alcance de radio directo (más de 1500 m), el cliente receptor ejecuta el algoritmo de ruta más corta de Dijkstra en todos los repetidores activos en la zona.
* **Degradación de la calidad del peor salto:** Si existe una ruta de múltiples saltos por debajo del límite de un solo salto de 8000 m, el sistema enruta la comunicación y aplica el factor de degradación del peor salto (calidad de la señal) en lugar de la distancia total en línea recta, lo que permite redes de radio planetarias/orbitales de largo alcance.
* **Estado dinámico de WebSocket:** Los estados del repetidor activo se sincronizan en tiempo real a través del canal de control WebSocket del servidor.

### 14. 📢 Sistema de transmisión de megafonía (PA) para barcos
* **Transmisión de audio en todo el barco:** Los pilotos o capitanes de barcos con tripulación múltiple pueden transmitir anuncios de voz a todos los miembros de la tripulación que comparten el mismo "ContainerID" (barco) en la misma zona.
* **PA DSP y Klaxon Chime:** Las transmisiones de megafonía evitan los silenciadores de proximidad y de radio locales (excepto el volumen maestro/silencio), reproducen mono con panorámica central, anteponen una alerta de timbre/klaxon de doble tono de ciencia ficción y aplican un filtro de reverberación y paso de banda de megáfono que simula la acústica interior de un barco hueco.

### 15. 🔌 Telemetría de hardware externa (Sim-Pit UDP Sync)
* **Sincronización UDP en tiempo real:** El cliente transmite estados de VoIP y casco en formato JSON a `127.0.0.1:8895` cada 100 ms.
* **Integración de hardware:** Permite a los constructores de cabinas integrar luces LED o indicadores físicos que reaccionen a las comunicaciones.

### 16. 🪐 Simulación de la densidad de la atmósfera planetaria
* **Escala de rango:** El rango de voz de proximidad se adapta a la densidad atmosférica del planeta o luna (por ejemplo, caída de volumen 3.5 veces más rápida en Cellin).
* **Silenciamiento de gas delgado:** Aplica filtros de paso bajo en el exterior bajo atmósferas delgadas, con derivación automática en interiores presurizados.

### 17. 🎙️ Grabador de voz Post-Op y portal AAR
* **Contenedor Ogg/Opus sin sobrecarga:** Guarda paquetes Opus directamente en archivos `.ogg` sin sobrecarga de codificación en el servidor.
* **Línea de tiempo Canvas interactiva:** Permite a los administradores visualizar, reproducir y eliminar clips de voz grabados desde el panel de administración.

### 18. 📞 Sistema de llamadas y hailing de nave a nave
* **Llamadas de cabina a cabina:** Establece conexiones de voz privadas entre naves dentro de un límite de 5000 m.
* **Transmisión manos libres:** Activa la transmisión VAD automáticamente durante la llamada, evitando las teclas PTT estándar.
* **Tonos realistas:** Sintetiza tonos de marcado, llamada y desconexión realistas a través de NAudio.

### 19. 🔤 Traducción de subtítulos en tiempo real en HUD de visor
* **Traductor de frases dinámico:** Traduce transmisiones de voz extranjeras en tiempo real usando diccionarios militares/de vuelo para 7 idiomas.
* **Prefijo de subtítulos HUD:** Muestra el texto traducido en el HUD del visor, con el prefijo `[DE -> A]`.
* **Carga de modelo Whisper:** Descarga automáticamente el modelo Whisper (~75 MB) en segundo plano si no está presente al activarlo.

### 20. 🎧 Audio espacial HRTF binaural
* **Simulación física del oído:** Simula la forma del oído humano y los efectos de sombra de la cabeza usando ITD (Diferencia de tiempo interaural) y atenuación de paso bajo ILD (Diferencia de nivel interaural).
* **Compatibilidad estéreo:** Ofrece indicaciones de audio 3D de alta fidelidad sobre auriculares estéreo estándar sin necesidad de hardware de sonido envolvente.

### 21. 📊 Espectrograma 3D del HUD del visor
* **Superposition de telemetría FFT:** Calcula transformadas rápidas de Fourier (FFT) Radix-2 de 64 puntos en tiempo real en las transmisiones de voz de los altavoces entrantes.
* **Visualización dinámica de HUD:** Agrupa las frecuencias de audio en 8 bandas espectrales junto a los altavoces activos en el HUD Vulkan/DX, con un decaimiento suave.

### 22. 🎙️ Controles de nave activados por voz
* **Traducción de comando de voz a combinación de teclas:** Escucha comandos de voz (por ejemplo, "open doors") y los compara con diccionarios localizados en 8 idiomas.
* **Pulsaciones de teclas de hardware directas:** Simula pulsaciones de teclas físicas con la API de bajo nivel de Win32 `keybd_event` (teclas mantenidas presionadas durante 50 ms para un registro confiable del juego, admitiendo teclas modificadoras).

### 23. 🛰️ Reproducción 3D AAR en el servidor
* **Registro de coordenadas:** El servidor registra las coordenadas y zonas de los jugadores en un archivo `<session_id>_positions.jsonl` cada 500 ms.
* **Reproducción WebGL 3D sincronizada:** Visualiza la ruta 3D del jugador y los anillos de pulso de habla en un mapa interactivo Three.js WebGL 3D con desplazamiento, zoom y rotación de mouse, totalmente sincronizado con el audio Ogg/Opus grabado.

---

## 🎮 Desglose de la pestaña Configuración del cliente XuruVoip

La ventana de configuración de WPF está estructurada en seis categorías de configuración:
1. **General**: configure idiomas, siga los archivos `Game.log`, alterne el registro general de archivos y habilite/configure el **servidor HTTP de la aplicación complementaria** y el puerto locales.
2. **Conexión**: edite la IP del servidor de destino, los puertos de audio y posición, el nombre de usuario, la contraseña de usuario y la contraseña del servidor.
3. **Posición**: cambie la fuente de ubicación ("OCR Screen Scanner" frente a "Game.log Reader (GRTPR)"), configure índices de monitor, regiones de recorte, intervalos de OCR y obtenga una vista previa del texto de coordenadas en vivo.
4. **Audio**: elija el hardware de entrada/salida, ajuste las ganancias de dB, seleccione el modo de transmisión (PTT vs VAD), configure los umbrales de VAD, active **Habilitar audio espacial 3D**, configure la degradación de radio, los timbres locales sintetizados, el modulador de visor y seleccione los ajustes preestablecidos de **Cambiador de voz**.
5. **Teclas de acceso rápido**: Vincula teclas a PTT de proximidad, PTT de radio, PTT de perfil, visor del casco, ciclo de canales de radio e interruptores de silencio de canales de audio y micrófono individuales.
6. **Superposición**: alterna la superposición de HUD, establece la ubicación de las esquinas, activa el **miniradar táctico** (con alcance máximo configurable) y alterna los **subtítulos de voz a texto** en tiempo real.

---

## 🖥️ Servidor XuruVoip (Ir)

El servidor coordina las posiciones de los jugadores, maneja la autenticación segura y enruta dinámicamente paquetes de audio según la distancia espacial y los canales de radio.

### Características clave

* **Control de proximidad del lado del servidor**: transmite dinámicamente audio de proximidad solo a los reproductores dentro del alcance (50 m por defecto o 5 m en susurro).
* **Configuración espacial**: opción conmutable del lado del servidor (`XURUVOIP_SPATIAL_AUDIO` en `.env`) que determina si se deben enviar coordenadas o solo la distancia a los clientes.
* **Enrutamiento de radio multicanal**: permite a los jugadores escuchar múltiples canales de radio simultáneamente mientras transmiten en su canal activo.
* **Sistema de perfil de audio**: Asigna efectos de audio (por ejemplo, filtro de radio, eco) a los reproductores.
* **SQLite Persistence**: almacena las preferencias del canal del jugador y las asignaciones de perfiles cuando se reinicia el servidor.
* **Seguridad anti-bypass**: prohíbe a los alborotadores por nombre de usuario, IP y huella digital de hardware (HWID/MachineGuid) para evitar eludir la prohibición.
* **Portal de administración web**: interfaz web segura (HTTPS/WebSockets) para paneles en tiempo real, transmisión de registros, configuración de canales/perfiles y gestión de prohibiciones.
* **Mapa de radar de administración del servidor**: radar de reproductor en tiempo real 2D HTML5 Canvas integrado en el panel de administración, que admite desplazamiento panorámico con hacer clic y arrastrar, zoom con la rueda del mouse, filtrado de zonas activas, senderos históricos para caminar de los jugadores (migas de pan) y anillos de ondas sonoras concéntricas pulsantes en vivo alrededor de los jugadores que hablan.
* **Rotación del registro de inicio**: Comprueba el registro del servidor (`xuruvoip.log`) al inicio. Si el archivo de registro contiene entradas de un día anterior, se rota a `xuruvoip.AAAA-MM-DD.log`. El servidor conserva sólo los 5 archivos rotados más recientes y elimina los más antiguos para evitar el uso excesivo del disco.

### Configuración del servidor (`.env`)

En el primer inicio, el servidor genera automáticamente un archivo `.env` que contiene estos valores predeterminados:```env
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
### 🎛️ Guía de configuración del puente de voz de Discord

Para conectar un canal de radio del servidor Go local a un canal de voz de Discord, siga estos pasos de configuración:

1. **Crea una aplicación de Discord Bot:**
   * Visite el [Portal para desarrolladores de Discord](https://discord.com/developers/applications) e inicie sesión.
   * Haga clic en **Nueva aplicación**, asígnele un nombre (por ejemplo, `Puente XuruVOIP`) y haga clic en **Crear**.
   * Navegue a la pestaña **Bot** en la barra lateral izquierda, haga clic en **Restablecer token** y copie el **Bot Token** generado. Pegue esto como `XURUVOIP_DISCORD_TOKEN` en el archivo `.env` de su servidor.
   * En **Intenciones de puerta de enlace privilegiada** en la misma página del Bot, habilite la **Intención de contenido del mensaje** (obligatoria para leer comandos específicos).

2. **Invita al Bot a tu servidor de Discord:**
   * Vaya a la pestaña **OAuth2** y luego seleccione **Generador de URL**.
   * En **Ámbitos**, marque `bot` y `applications.commands`.
   * En **Permisos de bot**, seleccione los siguientes privilegios:
     * *Permisos generales:* `Ver canales`
     * *Permisos de texto:* `Enviar mensajes`
     * *Permisos de voz:* `Conectar`, `Hablar`, `Usar actividad de voz`
   * Copie la URL generada en la parte inferior de la página, péguela en un navegador web, seleccione su servidor de Discord de destino (Gremio) y haga clic en **Autorizar**.

3. **Obtener ID de servidor (gremio) y canal de voz:**
   * Abra Discord, vaya a **Configuración de usuario** -> **Avanzado** y active el **Modo de desarrollador**.
   * Haga clic derecho en el ícono de su servidor Discord en la lista de servidores y seleccione **Copiar ID de servidor** (esta es su ID de gremio). Pégalo como `XURUVOIP_DISCORD_GUILD_ID` en `.env`.
   * Haga clic con el botón derecho en el canal de voz de Discord objetivo al que desea que se una el bot y seleccione **Copiar ID de canal**. Pégalo como `XURUVOIP_DISCORD_CHANNEL_ID` en `.env`.

4. ** Canal de radio del servidor Map Go: **
   * Configure `XURUVOIP_DISCORD_BRIDGE_CHANNEL` con el nombre exacto del canal de radio que desea conectar (por ejemplo, `General`, `Bravo`, `Alpha`, etc.). ¡Cualquier audio transmitido en la frecuencia de radio de este servidor Go se transmitirá bidireccionalmente al canal de voz de Discord!

### Construyendo el servidor desde la fuente

####Linux```bash
cd server


GOOS="linux" GOARCH="amd64" go build .
# a "server" linux binary will be created in the current directory
```
#### ventanas```powershell
cd server 

$env:GOOS="windows"
$env:GOARCH="amd64"
go build .
# a "server.exe" windows binary will be created in the current directory
```
### Ejecutando el servidor

#### De la fuente:```bash
cd server
go run .
```
#### Desde binario:
##### Ventanas```powershell
.\server.exe
```
#####Linux```bash
./server
```
### 🖥️ Configuración e implementación del servidor sin cabeza

Para instalaciones headless permanentes y listas para producción, el servidor debe ejecutarse como un demonio/servicio del sistema en segundo plano que se inicia automáticamente al arrancar y se reinicia en caso de falla.

#### 1. Configuración de red y firewall
Asegúrese de que los puertos TCP entrantes definidos en su archivo `.env` (los valores predeterminados son `8888` para posiciones/portal de administración y `8889` para audio espacial) estén abiertos en su firewall host:
* **Linux (UFW):**  ```bash
  sudo ufw allow 8888/tcp
  sudo ufw allow 8889/tcp
  sudo ufw reload
  ```
* **Linux (cortafuegos):**  ```bash
  sudo firewall-cmd --zone=public --add-port=8888/tcp --permanent
  sudo firewall-cmd --zone=public --add-port=8889/tcp --permanent
  sudo firewall-cmd --reload
  ```
---

#### 2. Implementación de Linux (systemd)

Siga estos pasos para implementar el servidor Go como un servicio systemd:

##### Paso A: Configurar directorio y permisos
Cree un usuario del sistema dedicado y un directorio de trabajo para el aislamiento de seguridad:```bash
# Create a system user without login privileges
sudo useradd -r -s /bin/false xuruvoip

# Create installation directory and copy the binary
sudo mkdir -p /opt/xuruvoip
sudo cp xuruvoip-server-linux-x64 /opt/xuruvoip/xuruvoip-server
sudo chmod +x /opt/xuruvoip/xuruvoip-server

# Set ownership to the system user
sudo chown -R xuruvoip:xuruvoip /opt/xuruvoip
```
##### Paso B: Generar y configurar `.env`
Ejecute el servidor una vez bajo el usuario del sistema para generar el archivo de configuración y la base de datos predeterminados `.env`:```bash
sudo -u xuruvoip /opt/xuruvoip/xuruvoip-server -port 8888 -audio-port 8889
```
*Presione `Ctrl+C` después de que la consola imprima las contraseñas generadas.* Luego, edite el archivo `.env` generado para personalizar la configuración (por ejemplo, contraseñas, IP vinculante, alternancia de audio espacial):```bash
sudo nano /opt/xuruvoip/.env
```
##### Paso C: Crear el archivo de servicio systemd
Copie el archivo de servicio del repositorio `server/xuruvoip.service` a `/etc/systemd/system/xuruvoip-server.service` o cree un nuevo archivo de configuración de servicio `/etc/systemd/system/xuruvoip-server.service` con el siguiente contenido:```ini
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
##### Paso D: habilitar e iniciar el servicio```bash
# Reload systemd daemon to pick up the new unit file
sudo systemctl daemon-reload

# Enable the service to run on startup
sudo systemctl enable xuruvoip-server

# Start the service immediately
sudo systemctl start xuruvoip-server
```
##### Paso E: Monitoreo y registros
Para verificar el estado del servicio y los registros de transmisión:```bash
# Check status
sudo systemctl status xuruvoip-server

# Stream log files in real-time
journalctl -u xuruvoip-server -f -n 100
```
---

#### 3. Implementación de Windows (NSSM)

Para ejecutar el servidor como un servicio nativo de Windows en modo sin cabeza, se recomienda utilizar **Administrador de servicios sin succión (NSSM)**:

##### Paso A: Configurar directorios
Extraiga/copie `xuruvoip-server-windows-x64.exe` a una carpeta de servidor dedicada (por ejemplo, `C:\XuruVoipServer`).

##### Paso B: Inicializar la configuración
Abra una terminal de PowerShell como administrador y ejecute el binario una vez para generar archivos:```powershell
cd C:\XuruVoipServer
.\xuruvoip-server-windows-x64.exe
```
*Presione `Ctrl+C` una vez que finalice el inicio.* Personalice el archivo `.env` generado según sea necesario.

##### Paso C: Instalar el servicio a través de NSSM
Descargue NSSM e instale el servicio ejecutando:```powershell
# Open NSSM GUI installer
.\nssm.exe install XuruVoipServer "C:\XuruVoipServer\xuruvoip-server-windows-x64.exe"
```
En la ventana emergente NSSM, configure:
* **Ruta:** `C:\XuruVoipServer\xuruvoip-server-windows-x64.exe`
* **Directorio de inicio:** `C:\XuruVoipServer`
* Haga clic en **Instalar servicio**.

##### Paso D: Iniciar el servicio
Inicie el servicio usando PowerShell o Administrador de servicios (`services.msc`):```powershell
Start-Service -Name XuruVoipServer
```
---

### Construyendo y ejecutando el cliente

#### Requisitos
-Windows 10/11
- SDK .NET 9.0 (soporte WPF)

#### Compilar y ejecutar:```powershell
cd client
dotnet run
```
### Instalación del paquete de lanzamiento

Dado que el instalador y los ejecutables no están firmados digitalmente, Windows SmartScreen puede bloquearlos inicialmente. Puedes desbloquearlos fácilmente usando el menú de propiedades.

* **Opción A: Administrador de paquetes de Windows (winget) - (Recomendado)**
  1. Abra una terminal (PowerShell o símbolo del sistema).
  2. Ejecute el siguiente comando para instalar el cliente:
     ```powershell
     winget install XuruDragon.XuruVOIPClient
     ```

* **Opción B: Instalador MSI**
  1. Descargue `XuruVoipClient-win-x64.msi` desde la [página de lanzamientos](https://github.com/XuruDragon/XuruVOIP/releases).
  2. Para evitar que Windows SmartScreen bloquee la instalación:
     - Haga clic derecho en el archivo `XuruVoipClient-win-x64.msi` descargado y seleccione **Propiedades**.
     - En la ventana de propiedades en la pestaña *General*, marque la casilla **Desbloquear** en la parte inferior.
     - Haga clic en **Aplicar** y luego cierre la ventana Propiedades.
  3. Haga doble clic en el archivo para ejecutar el instalador y siga las instrucciones.
     *(Nota: verá el mensaje estándar "Editor desconocido" del Control de cuentas de usuario de Windows; simplemente haga clic en **Sí** o **Ejecutar** para continuar).*

* **Opción C: Versión ZIP portátil**
  1. Descargue `XuruVoipClient-win-x64.zip` desde la [página de lanzamientos](https://github.com/XuruDragon/XuruVOIP/releases).
  2. Extraiga los archivos del paquete ZIP a cualquier carpeta de su elección (por ejemplo, `C:\Games\XuruVoip`):
  3. Luego haga clic derecho en el archivo extraído `XuruVoipClient.exe` y seleccione **Propiedades**.
     - En la ventana de propiedades en la pestaña *General*, marque la casilla **Desbloquear** en la parte inferior.
     - Haga clic en **Aplicar** y luego cierre la ventana Propiedades.
  4. Haga doble clic en `XuruVoipClient.exe` para ejecutar el cliente directamente sin instalarlo.

## 📱 Integración de aplicación complementaria y plataforma de transmisión

XuruVOIP incluye un servicio web de aplicación complementaria integrado y un complemento Stream Deck oficial que le permite monitorear y activar acciones de voz directamente desde dispositivos secundarios o claves físicas.

### 1. Habilitación de la aplicación complementaria y MFD de mapa táctico
De forma predeterminada, el servidor HTTP local de la aplicación complementaria y el modo de mapa táctico están deshabilitados para ahorrar recursos del sistema. Para habilitarlos:
1. Abra el cliente XuruVOIP y haga clic en el icono **Configuración**.
2. En la pestaña **General**, marque la casilla **Habilitar servidor HTTP complementario** (puerto predeterminado: `8891`).
3. Para habilitar la pantalla de radar, marque la casilla de verificación anidada **Habilitar mapa táctico de copiloto (MFD)**.
4. Haga clic en **Guardar y cerrar** para aplicar.
5. Acceder al panel de control: Abra `http://localhost:8891` en cualquier navegador de su PC, tableta o teléfono móvil. Si el modo de mapa está habilitado, estará disponible una nueva pestaña **🗺️ Mapa táctico**, que muestra una pantalla de radar HUD basada en Canvas que rastrea la posición en tiempo real de su personaje, el rumbo, los contactos de la tripulación en la misma zona y los indicadores de actividad del altavoz.

---

### 2. Instalación del complemento Stream Deck
El paquete de lanzamiento incluye el archivo `.streamDeckPlugin` preempaquetado.
1. Descargue `com.xuru.voip.streamDeckPlugin` desde la [página de lanzamientos](https://github.com/XuruDragon/XuruVOIP/releases).
2. Haga doble clic en el archivo para instalarlo directamente en su software Elgato Stream Deck. 
   *(Como alternativa, puede extraer y copiar manualmente la carpeta `com.xuru.voip.sdPlugin` a `%appdata%\Elgato\StreamDeck\Plugins\`)*
3. Una vez instalada, aparecerá una nueva categoría de acción llamada **XuruVOIP** en la lista del lado derecho de su aplicación de escritorio Stream Deck.

---

### 3. Agregar y configurar acciones
Puedes arrastrar y soltar cualquiera de las siguientes 19 acciones en las teclas de tu Stream Deck:
* 🎤 **Silenciamiento de proximidad**: alterna el silenciamiento del micrófono de proximidad saliente.
* 📻 **Silenciar radio**: alterna el silenciamiento del micrófono de radio saliente.
* 👤 **Silenciar perfil**: alterna el silenciamiento del micrófono del perfil saliente.
* 🔊 **Silenciamiento de proximidad de audio**: alterna el silenciamiento de la reproducción de proximidad entrante.
* 🔊 **Silenciar audio de radio**: alterna el silenciamiento de la reproducción de radio entrante.
* 🔊 **Silenciar perfil de audio**: alterna el silenciamiento de la reproducción del perfil entrante.
* 🪖 **Alternar casco**: alterna la visera del casco de tu traje espacial hacia abajo o hacia arriba.
* 🔄 **Ciclo de radio**: recorre los canales de radio disponibles.
* 📢 **PA Broadcast**: Tecla Push-to-Talk para transmitir en el sistema de megafonía pública del barco (PA).
* 📡 **Beacon Mode**: Alterna el modo repetidor de radio / baliza.
* 🎙️ **Voice Command Macro**: Activa una macro de comando de voz personalizada simulada en segundo plano (configurable en los ajustes).
* 💬 **Intercom Status**: Muestra el estado del intercomunicador de la nave (`NORMAL`, `SHIELD HIT`, `CRIT PWR`, `QUANTUM`) y recorre los estados de la simulación al presionarla.
* 🗺️ **Location Telemetry**: Muestra la zona actual del sistema y la telemetría de coordenadas $(X, Y, Z)$ en la tecla.
* 📞 **Initiate Hail**: Inicia una llamada de nave a nave al jugador más cercano.
* 📞 **Accept/Answer Hail**: Acepta una llamada de hailing ya recibida.
* 📞 **Decline/End Hail**: Rechaza una llamada entrante o finaliza una llamada activa.
* 🔤 **Toggle Translation**: Activa o desactiva la traducción de subtítulos en el HUD.
* 🎧 **Toggle HRTF**: Alterna la reproducción de audio espacial HRTF en tiempo real.
* 📊 **Toggle Spectrogram**: Alterna el espectrograma 3D en el HUD del visor en tiempo real.

#### Configuración (inspector de propiedades):
Para cada acción que arrastre a una tecla, haga clic en ella y configure las opciones en el panel **Inspector de propiedades** en la parte inferior:
* **Companion Port**: Configúrelo para que coincida con el puerto configurado en la configuración de su cliente WPF (predeterminado: `8891`).
* **Voice Command** (Solo para Voice Command Macro): Ingrese el comando de texto a ejecutar (ej. `"close visor"`, `"open hangar"`).
* **Comentarios dinámicos**: Las acciones actualizan sus íconos y estados en tiempo real. Los conmutadores muestran cian/rojo, el Intercom Status recorre 4 estados y la telemetría de ubicación muestra las coordenadas.
* **Visualización de frecuencia en vivo**: La tecla **Cycle Radio** mostrará dinámicamente el nombre de la frecuencia actualmente activa directamente en el botón físico en tiempo real.

---

## 👥 Créditos

Desarrollado por **[@XuruDragon](https://github.com/XuruDragon)** en colaboración con **Antigravity IDE**.