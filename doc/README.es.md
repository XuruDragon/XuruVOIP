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
    subgraph Captura y Transmisión
        Mic[Entrada de Micrófono] -->|Audio PCM| VAD[Detección de Actividad de Voz de WebRTC]
        VAD -->|Voz Activa| OpusEnc[Codificador Opus]
        OpusEnc -->|Paquetes Opus| AudioWS[Cliente WebSocket de Audio]
        AudioWS -->|Puerto WebSocket 8889| Server[Servidor Go]
    end

    subgraph Posicionamiento y Detección de Casco
        SC[Proceso de Star Citizen] -->|r_DisplaySessionInfo| Screen[Captura de Pantalla]
        Screen -->|Preprocesamiento| Tess[Motor Tesseract OCR]
        Tess -->|Análisis Multilínea| Zone[Filtro de Zona Jerárquico]
        Zone -->|Coordenadas y Zona del Oyente| PosWS[Cliente WebSocket de Posición]
        PosWS -->|Puerto WebSocket 8888| Server

        SC -->|Registro en Tiempo Real| GameLog[Archivo Game.log]
        GameLog -->|Tail Scanner| LogParser[Analizador del Servicio de Registros]
        LogParser -->|Eventos Equipar/Quitar| Helmet[Sincronización del Modo Casco]
        Helmet -->|Paquete de estado del Casco| PosWS
    end

    subgraph Reproducción y Mezcla Espacial
        Server -->|Audio de Proximidad + Metadatos| AudioWS
        AudioWS -->|Frame Opus + Metadatos de Proximidad| Decoder[Decodificador Opus]
        Decoder -->|PCM Mono Float| DSP[Filtro DSP de Radio]
        DSP -->|Mono| Panner[PanningSampleProvider]
        Panner -->|Estéreo| Volume[VolumeSampleProvider]
        
        LogParser -.->|estado local de Casque| DSP
        Zone -.->|Posición y Dirección del oyente| MixerMath[Cálculo de Spatialización]
        
        MixerMath -->|Parámetro Balance| Panner
        MixerMath -->|Atenuación de Distancia y Trasera| Volume
        
        Volume -->|Estéreo Izquierda/Derecha| Mixer[MixingSampleProvider]
        Mixer -->|Reproducción| Speakers[Dispositivo de Salida de Audio]
    end
```

### 1. Captura de Audio, VAD y Compresión
* **Captura de Audio:** El cliente captura el micrófono usando la API **NAudio** a una frecuencia de 48,000 Hz, 16 bits mono.
* **Detección de Actividad de Voz (VAD):** Los búferes de audio son evaluados por el wrapper nativo **WebRtcVad**. Si la confianza de voz cae por debajo del umbral, la transmisión se detiene para evitar emitir el ruido del teclado o ventiladores.
* **Compresión:** La voz activa es codificada en paquetes **Opus** (usando la biblioteca C# **Concentus**) y se transmite por WebSockets al servidor.

### 2. Seguimiento de Ubicación y Orientación
* **Captura de Pantalla y OCR:** El cliente captura periódicamente la región de la pantalla con coordenadas (`/showlocations` o `r_DisplaySessionInfo`). La imagen es preprocesada y procesada por **Tesseract OCR**.
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
* **Reproducción:** El flujo decodificado pasa por un **filtro DSP de radio** (si alguno de los jugadores usa casco o está en un canal de radio), se balancea, atenúa y mezcla usando el `MixingSampleProvider` de NAudio.

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

El panel de configuración incluye cinco secciones:
1. **General**: Selección de idioma, ruta del archivo `Game.log` de Star Citizen y activación del registro local.
2. **Connection**: Dirección IP del servidor, puertos de audio y posición, nombre de usuario, contraseña del perfil y contraseña del servidor.
3. **OCR**: Selección del monitor, frecuencia de escaneo (ms), definición del área de escaneo de pantalla y vista previa del texto parseado.
4. **Audio**: Dispositivos de audio, ajuste de ganancias de volumen, modo de transmisión (PTT / VAD), sensibilidad del VAD y opción de activar **3D Spatial Audio**.
5. **Hotkeys**: Registro de las teclas de atajo de teclado para el PTT, silenciar canales de transmisión y silenciar canales de audio recibidos.

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
