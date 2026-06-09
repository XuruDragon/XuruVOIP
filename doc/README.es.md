# XuruVoip (Español)

<p align="center">
  <a href="https://github.com/XuruDragon/XuruVOIP/actions/workflows/tests.yml">
    <img src="https://github.com/XuruDragon/XuruVOIP/actions/workflows/tests.yml/badge.svg" alt="Estado de las Pruebas" />
  </a>
  <a href="https://github.com/XuruDragon/XuruVOIP/releases">
    <img src="https://img.shields.io/github/v/release/XuruDragon/XuruVOIP?color=blue&logo=github" alt="Último Lanzamiento" />
  </a>
  <a href="https://github.com/XuruDragon/XuruVOIP/releases">
    <img src="https://img.shields.io/github/downloads/XuruDragon/XuruVOIP/total?color=green&logo=github" alt="Descargas Totales" />
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
  <img src="../logo.png" alt="Logo de XuruVoip" width="400" height="400" />
</p>

XuruVoip es una suite de comunicación de voz 3D (VoIP) de alto rendimiento, segura y espacializada dinámicamente, diseñada específicamente para integraciones de juego personalizadas con **Star Citizen**. Consta de un servidor backend en Go y de un cliente moderno en C# WPF.

---

## 📸 Capturas de Pantalla e Interfaz de Usuario

### 1. Ventana Principal del Cliente
![Ventana Principal del Cliente](/screenshots/main.png)

### 2. Pestaña de Ajustes de Audio (Control Espacial 3D)
![Pestaña de Ajustes de Audio](/screenshots/audio.png)

### 3. Pestaña de Ajustes Generales (Idioma y Ruta de Game.log)
![Pestaña de Ajustes Generales](/screenshots/general.png)

### 4. Pestaña de Ajustes de Conexión
![Pestaña de Ajustes de Conexión](/screenshots/connection.png)

### 5. Pestaña de Raccourcis / Atajos de Teclado
![Pestaña de Raccourcis / Atajos de Teclado](/screenshots/hotkeys.png)

### 6. Página de Inicio de Sesión del Portal Web de Administración
![Página de Inicio de Sesión del Portal Web de Administración](/screenshots/admin_login.png)

### 7. Panel de Control del Portal Web de Administración
![Panel de Control del Portal Web de Administración](/screenshots/admin_dashboard.png)

### 8. Lista de Jugadores del Portal Web de Administración
![Lista de Jugadores del Portal Web de Administración](/screenshots/admin_players_list.png)

### 9. Lista de Administradores del Portal Web de Administración
![Lista de Administradores del Portal Web de Administración](/screenshots/admin_admin_list.png)

### 10. Lista de Bloqueados (Baneos) del Portal Web de Administración
![Lista de Bloqueados (Baneos) del Portal Web de Administración](/screenshots/admin_ban_list.png)

---

## 🗂️ Estructura del Proyecto

- **/server**: Servidor backend en Go de alto rendimiento que aloja los servicios de posición, audio y administración.
- **/client**: Cliente moderno en C# WPF que utiliza NAudio, WebRtcVad y Tesseract OCR para el seguimiento automatizado de ubicaciones y el análisis de registros (logs).

---

## ⚙️ Cómo Funciona la Aplicación (Arquitectura del Cliente)

El cliente C# WPF se ejecuta en paralelo con Star Citizen y realiza captura de audio, procesamiento, reconocimiento de coordenadas y reproducción en tiempo real. A continuación se detalla el flujo de trabajo del sistema cliente:

```mermaid
graph TD
    subgraph Captura de Audio, VAD y Compresión
        Mic[Micrófono] -->|Audio PCM| VAD[Detección de Actividad de Voz VAD]
        VAD -->|Voz Activa| OpusEnc[Codificador Opus]
        OpusEnc -->|Paquetes Opus| AudioWS[Cliente WebSocket de Audio]
        AudioWS -->|Puerto WebSocket 8889| Server[Servidor Go]
    end

    subgraph Posicionamiento & Detección de Casque
        SC[Proceso Star Citizen] -->|r_DisplaySessionInfo| Screen[Captura de Pantalla]
        Screen -->|Preprocesamiento| Tess[Motor OCR Tesseract]
        
        SC -->|Log en Tiempo Real| GameLog[Archivo Game.log]
        GameLog -->|Analizador de logs| LogParser[Analizador de logs]
        
        Tess -->|Coordenadas| PosSelector{Selector de Fuente}
        LogParser -->|Coordenadas| PosSelector
        
        PosSelector -->|Coordenadas Seleccionadas| Zone[Filtro Jerárquico de Zona]
        Zone -->|Coordonadas & Zona del Oyente| PosWS[Cliente WebSocket de Posición]
        PosWS -->|Puerto WebSocket 8888| Server

        LogParser -->|Equipar/Quitar casco| Helmet[Sincronización de Casco]
        Helmet -->|Paquete de Estado del Casco| PosWS
    end

    subgraph Mezcla Espacial Stéréo 3D & DSP
        Server -->|Audio Proximidad + Metadatos| AudioWS
        AudioWS -->|Trama Opus + Metadatos| Decoder[Decodificador Opus]
        Decoder -->|Mono Float PCM| DSP[Filtro DSP de Radio & Degradación]
        DSP -->|Mono| Panner[PanningSampleProvider]
        Panner -->|Estéreo| Volume[VolumeSampleProvider]
        
        LogParser -.->|Estado local del casco| DSP
        Zone -.->|Posición & orientación del oyente| MixerMath[Matemáticas de Spatialización & Degradación]
        
        MixerMath -->|Parámetro Balance Pan| Panner
        MixerMath -->|Atenuación por Distancia & Trasera| Volume
        MixerMath -->|Factor de Degradación| DSP
        
        Volume -->|Estéreo Izquierda/Derecha| Mixer[MixingSampleProvider]
        Mixer -->|Reproducción de Audio| Speakers[Dispositivo de Salida Audio]
    end
```

### 1. Captura de Audio, VAD y Compresión
* **Captura de Audio:** El cliente captura el micrófono usando la API **NAudio** a una frecuencia de 48,000 Hz, 16 bits mono.
* **Detección de Actividad de Voz (VAD):** Los búferes de audio son evaluados por el wrapper nativo **WebRtcVad**. Si la confianza de voz cae por debajo del umbral, la transmisión se detiene para evitar emitir el ruido del teclado o ventiladores.
* **Compresión:** La voz activa es codificada en paquetes **Opus** (usando la biblioteca C# **Concentus**) y se transmite por WebSockets al servidor.

### 2. Seguimiento de Ubicación y Orientación
* **Selector de Fuente de Posición:** Los jugadores pueden elegir entre dos métodos de posicionamiento en la configuración:
  * **Escáner de Pantalla OCR:** Realiza periódicamente capturas de pantalla de la región configurada (donde se muestran las coordenadas con `/showlocations` o `r_DisplaySessionInfo`), preprocesa la imagen y la envía al motor **Tesseract OCR**.
  * **Lector Game.log (GRTPR):** Escanea directamente el archivo `Game.log` de Star Citizen para obtener las coordenadas registradas. Para habilitar esto, se debe añadir `r_DisplaySessionInfo = 3` (or `1`) al archivo `user.cfg` del juego. Al seleccionar GRTPR, el motor Tesseract OCR se detiene y se libera por completo, reduciendo sustancialmente el uso de CPU y RAM del equipo.
* **Filtro de Zona Jerárquico:** El texto parseado contiene jerarquías (ej. planetas, naves, ascensores). El cliente filtra variaciones menores (como ascensores o asientos) para que los jugadores en zonas adyacentes se escuchen de forma fluida.
* **Estimación de la Orientación:** El cliente calcula el vector de movimiento entre coordenadas consecutivas ($Posición_{actual} - Posición_{anterior}$). Si el desplazamiento supera los 0,5 metros, actualiza la dirección estimada.

### 3. Detección de Casco en Tiempo Real
* **Tail Scanner (Lectura en Continuo):** Un hilo en segundo plano escanea el archivo `Game.log` de Star Citizen.
* **Monitoreo de Accesorios:** Busca las líneas de equipamiento del casco (`FP_Visor`, `helmethook_attach`) y sincroniza el estado (Casco Puesto/Quitado) de manera instantánea.

### 4. Mezcla Espacial Estéreo 3D & DSP
* **Recepción:** El cliente recibe audio de Opus junto con metadatos de proximidad (distancia, rango, coordenadas).
* **Cálculos Espaciales:** Proyecta la posición del emisor sobre los vectores del oyente:
  * **Balance Estéreo (Pan):** Distribuye el volumen de `-1.0` (izquierda) a `+1.0` (derecha).
  * **Atenuación Trasera:** Si el emisor está detrás, el volumen disminuye hasta un 25% para resolver la ambigüedad espacial.
  * **Atenuación por Distancia:** Disminuye el volumen linealmente hasta llegar a cero en la distancia máxima.
* **Reproducción & DSP de Radio:** El flujo decodificado pasa por un **filtro DSP de radio** (si alguno de los jugadores usa casco o está en un canal de radio), se balancea, atenúa y mezcla.
  * **Degradación de Radio Dinámica:** Si está habilitada, el filtro DSP reduce dinámicamente las frecuencias de corte paso-alto y paso-bajo y mezcla ruido blanco filtrado a medida que la distancia entre los jugadores se aproxima al rango máximo de comunicación, simulando interferencias y pérdida de señal de radio.
  * **Tonos de Radio PTT Realistas:** NAudio sintetiza tonos de radio para la activación y desactivación de la transmisión. El inicio de la transmisión reproduce un chirp de 50ms (barrido de frecuencia de 900Hz a 700Hz). El fin de la transmisión activa una cola de squelch (ruido estático de 180ms) al recibir una trama Opus vacía (0 bytes). Un tono de retorno local opcional permite a los jugadores escuchar sus propios tonos.

### 6. Superposición HUD (Overlay) Sin Bordes Compatible con Vulkan y DirectX
* **Superposición HUD**: El cliente proporciona una ventana de superposición WPF transparente y siempre visible que muestra el estado de VoIP, la frecuencia del canal y los hablantes activos con indicadores de señal.
* **Integración Transparente Win32**: Utilizando estilos de ventana Win32 (`WS_EX_TRANSPARENT` y `WS_EX_NOACTIVATE`), la superposición no captura el enfoque y permite que los clics del mouse pasen directamente al juego.
* **Renderizado Independiente de la API**: Debido a que las ventanas transparentes de WPF dependen del compositor DWM (Desktop Window Manager) de Windows, el overlay no se inyecta en el pipeline gráfico del juego. Esto garantiza compatibilidad absoluta con **Vulkan** y **DirectX**, siempre que el juego se ejecute en modo **"Ventana sin Bordes"** (Borderless Windowed).

### 7. Acústica Ambiental (Oclusión y Reverberación)
* **Filtro de oclusión:** Si el emisor y el receptor están en diferentes zonas o compartimentos, el cliente aplica automáticamente un filtro de paso bajo (frecuencia de corte de 600 Hz, volumen al 65%) para simular la obstrucción física. La frecuencia de corte realiza una transición suave para evitar chasquidos.
* **Reverberación inteligente:** Si el receptor se encuentra en un entorno específico (cuevas, búnkeres o hangares), un filtro de peine de línea de retardo con retroalimentación aplica parámetros de reverberación específicos:
  * *Cuevas / Túneles:* 45% wet, 100 ms de retardo, 0.6 de retroalimentación.
  * *Búnkeres / Estaciones:* 25% wet, 50 ms de retardo, 0.4 de retroalimentación.
  * *Hangares:* 35% wet, 150 ms de retardo, 0.5 de retroalimentación.

### 8. Discord Rich Presence sin Dependencias (RPC)
* **Conexión por tubería con nombre:** El cliente se conecta directamente a Discord a través de tuberías con nombre de Windows (`\\.\pipe\discord-ipc-0`) sin requerir bibliotecas NuGet externas pesadas.
* **Actualización dinámica de actividad:** Actualiza tu presencia en Discord en tiempo real con:
  * **Detalles:** Zona de ubicación actual en el juego (ej. `"En una cueva en MicroTech"`).
  * **Estado:** Canal activo y estado del casco (ej. `"En radio: Canal Bravo (Casco equipado)"` o `"En proximidad"`).
  * **Tiempo transcurrido:** Muestra el tiempo transcurrido desde que se conectó al servidor VoIP.

---

## 🖥️ Servidor XuruVoip (Go)

El servidor gestiona la posición de los jugadores, la autenticación y enruta paquetes de audio basándose en la distancia espacial y los canales de radio.

### Características Clave
* **Control de Proximidad en Servidor**: Envía audio de proximidad solo a jugadores dentro del rango (50m por defecto).
* **Modo Espacial**: Variable `.env` (`XURUVOIP_SPATIAL_AUDIO`) que decide si se envían coordenadas reales o solo la distancia al cliente.
* **Enrutamiento de Radio Multicanal**: Permite escuchar múltiples canales de radio a la vez mientras se habla por el canal activo.
* **Sistema de Perfiles de Audio**: Aplica filtros de audio (efecto de radio, eco) a los perfiles de los jugadores.
* **Persistencia SQLite**: Guarda los canales y perfiles de los jugadores permanentemente.
* **Seguridad Avanzada**: Bloquea y banea usuarios por Username, IP y huella de hardware (HWID/MachineGuid).
* **Portal de Administración Web**: Interfaz web segura bajo HTTPS/WebSockets para monitoreo en tiempo real y administración.
* **Mapa de Radar para Administradores**: Un mapa de radar 2D Canvas HTML5 integrado en el panel de control web, con soporte para arrastrar, zoom con rueda del mouse, filtros de zona, trazado de rutas históricas (breadcrumbs) y anillos concéntricos de ondas sonoras pulsantes alrededor de los jugadores activos.

### Configuración del Servidor (`.env`)
En su primera ejecución, el servidor autogenera un archivo `.env`:
```env
XURUVOIP_SERVER_IP=
XURUVOIP_PORT=8888
XURUVOIP_AUDIO_PORT=8889
XURUVOIP_DATA_DIR=.
XURUVOIP_MAX_PLAYERS=500
XURUVOIP_SPATIAL_AUDIO=1
XURUVOIP_PUBLIC_SERVER=0
XURUVOIP_SERVER_PASSWORD=auto_generated_32_chars_token
XURUVOIP_ADMIN_SERVER_PASSWORD=auto_generated_32_chars_token
XURUVOIP_VERBOSE_LOGS=1
XURUVOIP_LIMIT_RATE_POS=50.0
XURUVOIP_LIMIT_BURST_POS=100
XURUVOIP_LIMIT_RATE_AUDIO=60.0
XURUVOIP_LIMIT_BURST_AUDIO=120
XURUVOIP_LOCKOUT_ATTEMPTS=5
XURUVOIP_LOCKOUT_WINDOW=60
XURUVOIP_LOCKOUT_DURATION=600
```

### Compilación desde las fuentes

#### Linux
```bash
cd server
GOOS="linux" GOARCH="amd64" go build .
```

#### Windows
```powershell
cd server
$env:GOOS="windows"
$env:GOARCH="amd64"
go build .
```

### Ejecución del Servidor

#### Desde el código fuente:
```bash
cd server
go run .
```

#### Desde el binario:
##### Windows
```powershell
.\server.exe
```

##### Linux
```bash
./server
```

### 🖥️ Configuración y Despliegue del Servidor sin Cabecera (Headless)

Para servidores de producción headless permanentes, se recomienda configurar el servidor para que se ejecute en segundo plano como un servicio del sistema.

#### 1. Configuración de Red y Cortafuegos
Asegúrese de abrir los puertos TCP configurados en el archivo `.env` (puertos 8888 y 8889 por defecto) en el cortafuegos de su sistema:
* **Linux (UFW):**
  ```bash
  sudo ufw allow 8888/tcp
  sudo ufw allow 8889/tcp
  sudo ufw reload
  ```
* **Linux (firewalld):**
  ```bash
  sudo firewall-cmd --zone=public --add-port=8888/tcp --permanent
  sudo firewall-cmd --zone=public --add-port=8889/tcp --permanent
  sudo firewall-cmd --reload
  ```

---

#### 2. Despliegue en Linux (systemd)

Siga estos pasos para desplegar el servidor en Go como un servicio de systemd:

##### Paso A: Crear Directorios y Permisos
Cree un usuario de sistema dedicado y un directorio de trabajo para aislar la seguridad:
```bash
# Crear un usuario de sistema sin permisos de inicio de sesión
sudo useradd -r -s /bin/false xuruvoip

# Crear el directorio de instalación y copiar el binario
sudo mkdir -p /opt/xuruvoip
sudo cp xuruvoip-server-linux-x64 /opt/xuruvoip/xuruvoip-server
sudo chmod +x /opt/xuruvoip/xuruvoip-server

# Establecer la propiedad al usuario del sistema
sudo chown -R xuruvoip:xuruvoip /opt/xuruvoip
```

##### Paso B: Inicializar y Configurar `.env`
Ejecute el servidor una vez bajo el usuario del sistema para generar el archivo `.env` y la base de datos por defecto:
```bash
sudo -u xuruvoip /opt/xuruvoip/xuruvoip-server -port 8888 -audio-port 8889
```
*Presione `Ctrl+C` después de que la consola imprima los tokens generados.* Luego, edite el archivo `.env`:
```bash
sudo nano /opt/xuruvoip/.env
```

##### Paso C: Crear el archivo de servicio de systemd
Copie el archivo de servicio del repositorio `server/xuruvoip.service` a `/etc/systemd/system/xuruvoip-server.service` o créelo con el siguiente contenido:
```ini
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

##### Paso D: Habilitar y Iniciar el servicio
```bash
sudo systemctl daemon-reload
sudo systemctl enable xuruvoip-server
sudo systemctl start xuruvoip-server
```

##### Paso E: Monitoreo y Registros
```bash
# Comprobar el estado del servicio
sudo systemctl status xuruvoip-server

# Monitorear los registros del servicio en tiempo real
journalctl -u xuruvoip-server -f -n 100
```

---

#### 3. Despliegue en Windows (NSSM)

Para ejecutar el servidor como un servicio de Windows en segundo plano, se recomienda utilizar **NSSM (Non-Sucking Service Manager)**:

##### Paso A: Configurar el Directorio
Mueva el archivo `xuruvoip-server-windows-x64.exe` a una carpeta dedicada (ej. `C:\XuruVoipServer`).

##### Paso B: Configuración Inicial
Ejecute el archivo una vez en PowerShell para generar los archivos de configuración iniciales, deténgalo con `Ctrl+C` y edite el archivo `.env`.

##### Paso C: Instalar el Servicio con NSSM
```powershell
.\nssm.exe install XuruVoipServer "C:\XuruVoipServer\xuruvoip-server-windows-x64.exe"
```
Defina el directorio de trabajo como `C:\XuruVoipServer` e instale el servicio.

##### Paso D: Iniciar el Servicio
```powershell
Start-Service -Name XuruVoipServer
```

---

## 🎮 Pestañas de Ajustes de XuruVoip Client

El panel de configuración incluye seis secciones:
1. **General**: Selección de idioma, ruta del archivo `Game.log` de Star Citizen y activación del registro local.
2. **Connection**: Dirección IP del servidor, puertos de audio y posición, nombre de usuario, contraseña del perfil y contraseña del servidor.
3. **Position**: Elección de la fuente de posición ("Escáner de Pantalla OCR" vs. "Lector Game.log (GRTPR)"), selección del monitor, frecuencia de escaneo (ms), definición del área de escaneo de pantalla y vista previa del texto parseado (las opciones OCR se ocultan cuando GRTPR está activo).
4. **Audio**: Dispositivos de audio, ajuste de ganancias de volumen, modo de transmisión (PTT / VAD), sensibilidad del VAD, activación de **3D Spatial Audio**, así como opciones avanzadas de degradación de radio y tonos PTT de micro.
5. **Hotkeys**: Registro de las teclas de atajo de teclado para el PTT, silenciar canales de transmisión y silenciar canales de audio recibidos.
6. **Overlay**: Activación de la superposición HUD transparente y configuración de la ubicación en pantalla (ej. Arriba a la izquierda, Arriba a la derecha).

### Compilación y Ejecución del Cliente

#### Requisitos
- Windows 10 o Windows 11
- .NET 9.0 SDK (con componentes WPF)

#### Compilar y ejecutar:
```powershell
cd client
dotnet run
```

### Instalación del Paquete de Lanzamiento (Release)

Debido a que el instalador y ejecutables no están firmados digitalmente, Windows SmartScreen los bloqueará en su primera ejecución. Puede desbloquearlos en sus propiedades.

* **Opción A: Instalador MSI (Recomendado)**
  1. Descargue `XuruVoipClient-win-x64.msi` de la [página de versiones (releases)](https://github.com/XuruDragon/XuruVOIP/releases).
  2. Haga clic derecho sobre el archivo `.msi` y seleccione **Propiedades**.
  3. En la pestaña *General* marque la casilla **Desbloquear** en la parte inferior y haga clic en **Aplicar**.
  4. Inicie el instalador y siga las instrucciones del asistente.

* **Opción B: Versión Portable (ZIP)**
  1. Descargue `XuruVoipClient-win-x64.zip` de la [página de versiones (releases)](https://github.com/XuruDragon/XuruVOIP/releases).
  2. Haga clic derecho sobre el archivo `.zip`, seleccione **Propiedades** y marque la casilla **Desbloquear** en la pestaña *General*. Haga clic en **Aplicar**.
  3. Extraiga la carpeta en el directorio que desee (ej. `C:\Games\XuruVoip`).
  4. Abra `XuruVoipClient.exe` para arrancar el cliente.

---

## 👥 Créditos

Desarrollado por **[@XuruDragon](https://github.com/XuruDragon)** en colaboración con **Antigravity IDE**.
