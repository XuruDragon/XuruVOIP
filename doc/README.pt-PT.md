# XuruVoip (Português - Portugal)

<p align="center">
  <a href="https://github.com/XuruDragon/XuruVOIP/actions/workflows/tests.yml">
    <img src="https://github.com/XuruDragon/XuruVOIP/actions/workflows/tests.yml/badge.svg" alt="Estado dos Testes" />
  </a>
  <a href="https://github.com/XuruDragon/XuruVOIP/releases">
    <img src="https://img.shields.io/github/v/release/XuruDragon/XuruVOIP?color=blue&logo=github" alt="Último Lançamento" />
  </a>
  <a href="https://github.com/XuruDragon/XuruVOIP/releases">
    <img src="https://img.shields.io/github/downloads/XuruDragon/XuruVOIP/total?color=green&logo=github" alt="Downloads Totais" />
  </a>
</p>

<p align="center">
  <b>Traduções:</b><br/>
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
  <img src="../logo.png" alt="Logótipo do XuruVoip" width="400" height="400" />
</p>

XuruVoip é uma suite de comunicação de voz 3D (VoIP) de alto desempenho, segura e espacializada dinamicamente, concebida especificamente para integrações personalizadas com o **Star Citizen**. É composta por um servidor backend escrito em Go e um cliente desktop moderno em C# WPF.

---

## 📸 Capturas de Ecrã e Interface

### 1. Janela Principal do Cliente
![Janela Principal do Cliente](/screenshots/main.png)

### 2. Painel de Definições de Áudio (Controlo de Áudio Espacial 3D)
![Painel de Definições de Áudio](/screenshots/audio.png)

### 3. Painel de Definições Gerais (Idioma & Caminho do Game.log)
![Painel de Definições Gerais](/screenshots/general.png)

### 4. Painel de Definições de Conexão
![Painel de Definições de Conexão](/screenshots/connection.png)

### 5. Painel de Definições de Atalhos de Teclado
![Painel de Definições de Atalhos de Teclado](/screenshots/hotkeys.png)

### 6. Página de Login do Portal Web Administrativo
![Página de Login do Portal Web Administrativo](/screenshots/admin_login.png)

### 7. Painel Geral (Dashboard) do Portal Web Administrativo
![Painel Geral do Portal Web Administrativo](/screenshots/admin_dashboard.png)

### 8. Lista de Jogadores do Portal Web Administrativo
![Lista de Jogadores do Portal Web Administrativo](/screenshots/admin_players_list.png)

### 9. Lista de Administradores do Portal Web Administrativo
![Lista de Administradores do Portal Web Administrativo](/screenshots/admin_admin_list.png)

### 10. Lista de Bloqueios (Banimentos) do Portal Web Administrativo
![Lista de Bloqueios do Portal Web Administrativo](/screenshots/admin_ban_list.png)

---

## 🗂️ Estrutura do Projeto

- **/server**: Servidor backend de alto desempenho escrito em Go que gere as posições dos jogadores, sessões de áudio e os serviços de administração web.
- **/client**: Cliente moderno em C# WPF que utiliza as bibliotecas NAudio, WebRtcVad e Tesseract OCR para localização automática e leitura de ficheiros de log do jogo.

---

## ⚙️ Como a Aplicação Funciona (Arquitetura do Cliente)

O cliente C# WPF corre em paralelo com o Star Citizen para capturar áudio, processar pacotes, reconhecer as coordenadas no ecrã e reproduzir o som em tempo real. Veja o fluxo detalhado da arquitetura:

```mermaid
graph TD
    subgraph Captura & Transmissão
        Mic[Entrada de Microfone] -->|Áudio PCM| VAD[Detecção de Actividade de Voz VAD]
        VAD -->|Voz Activa| OpusEnc[Codificador Opus]
        OpusEnc -->|Pacotes Opus| AudioWS[Cliente WebSocket de Áudio]
        AudioWS -->|Porta WebSocket 8889| Server[Servidor Go]
    end

    subgraph Posicionamento & Detecção de Capacete
        SC[Processo Star Citizen] -->|r_DisplaySessionInfo| Screen[Captura de Ecrã]
        Screen -->|Pré-processamento| Tess[Motor OCR Tesseract]
        
        SC -->|Log em Tempo Real| GameLog[Ficheiro Game.log]
        GameLog -->|Analisador de logs| LogParser[Analisador de logs]
        
        Tess -->|Coordenadas| PosSelector{Alternador de Fonte}
        LogParser -->|Coordenadas| PosSelector
        
        PosSelector -->|Coordenadas Seleccionadas| Zone[Filtro Hierárquico de Zonas]
        Zone -->|Coordenadas & Zonas do Ouvinte| PosWS[Cliente WebSocket de Posição]
        PosWS -->|Porta WebSocket 8888| Server

        LogParser -->|Equipar/Retirar capacete| Helmet[Sincronização de Capacete]
        Helmet -->|Pacote de Estado do Capacete| PosWS
    end

    subgraph Mixagem e Processamento Espacial 3D & DSP
        Server -->|Áudio de Proximidade + Metadados| AudioWS
        AudioWS -->|Frame Opus + Metadatos| Decoder[Decodificador Opus]
        Decoder -->|Mono Float PCM| DSP[Filtro DSP de Rádio & Degradação]
        DSP -->|Mono| Panner[PanningSampleProvider]
        Panner -->|Estéreo| Volume[VolumeSampleProvider]
        
        LogParser -.->|Estado local do capacete| DSP
        Zone -.->|Posição & direcção do ouvinte| MixerMath[Matemática de Espacialização & Degradação]
        
        MixerMath -->|Parâmetro Pan Estéreo| Panner
        MixerMath -->|Atenuação por Distância & Traseira| Volume
        MixerMath -->|Fator de Degradação| DSP
        
        Volume -->|Estéreo Esquerdo/Direito| Mixer[MixingSampleProvider]
        Mixer -->|Reprodução de Áudio| Speakers[Dispositivo de Saída de Som]
    end
```

### 1. Captura de Som, VAD e Compressão
* **Captura de Som:** O microfone é capturado pela biblioteca **NAudio** em 48.000 Hz, 16-bit mono.
* **Detecção de Actividade de Voz (VAD):** O wrapper nativo do **WebRtcVad** analisa o áudio em tempo real. Se o som cair abaixo do limite definido, a transmissão cessa para evitar a difusão de ruídos do teclado ou ventoinha.
* **Compression:** O áudio é codificado no formato **Opus** (através da biblioteca C# **Concentus**) e transmitido via WebSockets para o servidor.

### 2. Localização e Orientação 3D
* **Alternador de Fonte de Posição:** Os utilizadores podem escolher entre dois métodos de posicionamento nas definições do cliente:
  * **Scanner de Ecrã OCR:** Captura periodicamente a região configurada do ecrã (onde as coordenadas são exibidas via `/showlocations` ou `r_DisplaySessionInfo`), pré-processa a imagem e envia-a para o motor **Tesseract OCR**.
  * **Leitor de Game.log (GRTPR):** Monitoriza diretamente o ficheiro `Game.log` do Star Citizen para ler as coordenadas registadas pelo jogo. Para habilitar isso, o utilizador deve adicionar `r_DisplaySessionInfo = 3` (ou `1`) ao seu ficheiro `user.cfg`. A seleção do GRTPR desativa e descarta completamente o motor Tesseract OCR, poupando recursos significativos de CPU e RAM no computador do utilizador.
* **Filtro Hierárquico de Zonas:** O texto detectado contém localizações em árvore (planetas, naves, cabinas). O sistema remove ruídos (como cabinas de elevador ou assentos) para que utilizadores próximos em diferentes divisões continuem a conversar de forma contínua.
* **Estimativa de Orientação:** A direcção do jogador é estimada a partir da variação espacial consecutiva ($Posição_{atual} - Posição_{anterior}$). Quando parado, a última orientação é mantida.

### 3. Leitura e Monitorização de Capacete em Tempo Real
* **Log Tail Scanner:** Um serviço analisa o final do ficheiro `Game.log` do Star Citizen continuamente.
* **Detecção de Itens:** Identifica linhas de equipar capacete (`FP_Visor`, `helmethook_attach`) e altera o modo de processamento de áudio do jogador instantaneamente de forma autónoma.

### 4. Mixagem e Processamento Espacial 3D
* **Recepção:** Recebe pacotes Opus do servidor junto com dados espaciais do emissor (distância, coordenadas e alcance).
* **Cálculos de Áudio Espacial:** O sinal é decomposto nos eixos relativos do ouvinte:
  * **Pan Estéreo:** Controla o balanço de volume esquerdo/direito de `-1.0` a `+1.0`.
  * **Atenuação Traseira:** Sons vindos de trás sofrem uma redução de até 25% no volume para auxiliar na percepção física da direcção do som.
  * **Atenuação por Distância:** O volume diminui linearmente até zerar no raio máximo definido para a conversa (50 metros).
* **Reprodução & Filtro DSP de Rádio:** Os dados Opus descodificados passam por um **filtro DSP de rádio** (caso o emissor ou o receptor estejam com capacete ou a usar um canal de rádio), recebem o balanço espacial e são mixados com as demais fontes.
  * **Degradação de Rádio Dinâmica:** Se ativada, o filtro DSP estreita dinamicamente as frequências de corte passa-alta e passa-baixa e mistura ruído branco filtrado à medida que a distância entre os utilizadores se aproxima do alcance máximo de comunicação, simulando perda de sinal e interferência de rádio.
  * **Chimes de PTT & Rádio Realistas:** A biblioteca NAudio sintetiza chimes de rádio para o início e fim da transmissão. O início da transmissão toca um chirp de 50ms (varrimento de frequência de 900Hz a 700Hz). O fim da transmissão aciona uma cauda de squelch (ruído estático de 180ms) ao receber um frame Opus vazio (0 bytes). Um retorno de áudio local opcional permite que o utilizador ouça os seus próprios bipes de PTT.

### 6. HUD Overlay Sem Bordas Compatível com Vulkan e DirectX
* **HUD Overlay**: O cliente fornece uma janela de overlay WPF transparente e sempre visível que exibe o status do VoIP, a frequência do canal ativo e a lista de interlocutores a falar em tempo real com indicadores de sinal.
* **Integração Transparente Win32**: Utilizando estilos de janela Win32 (`WS_EX_TRANSPARENT` e `WS_EX_NOACTIVATE`), o overlay não rouba o foco do ecrã e permite que os cliques do rato passem direto para o jogo.
* **Renderização Independente de API**: Como as janelas transparentes do WPF dependem da composição DWM (Desktop Window Manager) do Windows, o overlay não se injeta na pipeline de renderização do jogo. Isso garante compatibilidade total com **Vulkan** e **DirectX**, contanto que o Star Citizen seja executado em modo **"Janela sem Bordas"** (Borderless Windowed).

### 7. Acústica Ambiental (Oclusão e Reverberação)
* **Filtro de Oclusão:** Se o emissor e o recetor estiverem em zonas ou compartimentos diferentes, o cliente aplica automaticamente um filtro passa-baixo (corte de 600Hz, volume de 65%) para simular obstrução física/oclusão. A frequência de corte faz uma transição suave para evitar cliques.
* **Reverberação Baseada na Localização:** Se o recetor estiver num ambiente específico (Cavernas, Bunkers ou Hangares), um filtro de linha de atraso de feedback (comb filter) aplica parâmetros de mistura (wet mix), atraso (delay) e feedback específicos do ambiente:
  * *Cavernas / Túneis:* 45% wet, 100ms de atraso, 0.6 de feedback.
  * *Bunkers / Estações:* 25% wet, 50ms de atraso, 0.4 de feedback.
  * *Hangares:* 35% wet, 150ms de atraso, 0.5 de feedback.

### 8. Discord Rich Presence Sem Dependências (RPC)
* **Conexão por Named Pipe:** O cliente integra-se com o Discord através de named pipes locais do Windows (`\\.\pipe\discord-ipc-0`) sem a necessidade de dependências externas pesadas.
* **Atualizações Dinâmicas de Atividade:** Atualiza instantaneamente a sua presença no Discord com:
  * **Detalhes:** Zona de localização atual no jogo (ex: `"Na Caverna de MicroTech"`).
  * **Estado:** Canal conectado e estado (ex: `"Na Rádio: Canal Bravo (Capacete Equipado)"` ou `"Em Proximidade"`).
  * **Tempo Decorrido:** Mostra o tempo decorrido desde que a conexão com o servidor foi estabelecida.

---

## 🖥️ Servidor XuruVoip (Go)

Garante o encaminhamento dinâmico de áudio com base na distância de proximidade e canais de rádio, além de gerir a segurança e persistência dos dados.

### Principais Recursos
* **Controlo de Proximidade no Servidor**: O servidor entrega os pacotes de som apenas para jogadores dentro do raio de alcance.
* **Modo Espacial Personalizado**: Através do `.env` (`XURUVOIP_SPATIAL_AUDIO`), define se as coordenadas físicas reais serão partilhadas com outros clientes ou se apenas a distância será informada.
* **Encaminhamento de Canais de Rádio Simultâneos**: O jogador pode ouvir múltiplos canais de rádio ao mesmo tempo enquanto transmite no canal activo.
* **Efeitos e Perfis de Áudio**: Adiciona distorções (rádio clássico, eco) de acordo com o perfil registado.
* **Persistência SQLite**: Grava todos os canais e atribuições dos jogadores de forma nativa.
* **Sistema de Segurança e Banimento**: Bloqueia utilizadores por Username, IP e assinatura física de hardware (HWID/MachineGuid).
* **Painel Administrativo Web**: Interface segura (HTTPS/WebSockets) com acompanhamento de logs ao vivo e painel de banimentos.
* **Mapa de Radar de Administração**: Mapa de radar 2D em HTML5 Canvas integrado no painel web para monitorizar as coordenadas dos utilizadores em tempo real, com rolagem por arrastar, zoom pela roda do rato, filtros de zona, trilhos históricos de caminhada (breadcrumbs) e ondas sonoras concêntricas pulsantes ao redor de utilizadores a falar.

### Configuração do Servidor (`.env`)
No primeiro arranque, o servidor gera automaticamente um ficheiro de configurações padrão:
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

### Compilar o Servidor a partir das fontes

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

### Inicializar o Servidor

#### A partir das fontes:
```bash
cd server
go run .
```

#### A partir do executável compilado:
##### Windows
```powershell
.\server.exe
```

##### Linux
```bash
./server
```

### 🖥️ Configuração e Instalação de Servidor Sem Interface (Headless)

Para servidores dedicados permanentes de produção, recomenda-se configurar a aplicação para correr como um serviço ou daemon do sistema operativo.

#### 1. Portas de Rede e Firewall
Configure a sua firewall para abrir as portas de entrada especificadas no ficheiro `.env` (padrões `8888` para dados/painel de controlo e `8889` para fluxo de áudio):
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

#### 2. Instalação no Linux (systemd)

Siga estas instruções para configurar o servidor escrito em Go como um serviço do systemd:

##### Passo A: Criar Diretórios e Utilizador
Crie um utilizador exclusivo sem privilégios de login para isolar a segurança do processo:
```bash
# Criar utilizador do sistema sem consola de login
sudo useradd -r -s /bin/false xuruvoip

# Criar a pasta de instalação e mover o executável
sudo mkdir -p /opt/xuruvoip
sudo cp xuruvoip-server-linux-x64 /opt/xuruvoip/xuruvoip-server
sudo chmod +x /opt/xuruvoip/xuruvoip-server

# Definir as permissões da pasta para o utilizador
sudo chown -R xuruvoip:xuruvoip /opt/xuruvoip
```

##### Passo B: Inicializar Ficheiro `.env`
Execute o binário uma primeira vez como o utilizador restrito para gerar o `.env` padrão e a base de dados SQLite:
```bash
sudo -u xuruvoip /opt/xuruvoip/xuruvoip-server -port 8888 -audio-port 8889
```
*Aperte `Ctrl+C` após a exibição das chaves de acesso geradas automaticamente.* Edite as variáveis no ficheiro `.env`:
```bash
sudo nano /opt/xuruvoip/.env
```

##### Passo C: Criar o Ficheiro de Serviço systemd
Copie o ficheiro de serviço do repositório `server/xuruvoip.service` para `/etc/systemd/system/xuruvoip-server.service` ou crie-o com as seguintes configurações:
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

##### Passo D: Registar e Iniciar o Serviço
```bash
sudo systemctl daemon-reload
sudo systemctl enable xuruvoip-server
sudo systemctl start xuruvoip-server
```

##### Passo E: Logs e Acompanhamento
```bash
# Consultar o status do serviço
sudo systemctl status xuruvoip-server

# Monitorizar a saída do terminal ao vivo
journalctl -u xuruvoip-server -f -n 100
```

---

#### 3. Instalação no Windows (NSSM)

Para registar e correr a aplicação em segundo plano no Windows, é indicado o uso do gestor **NSSM (Non-Sucking Service Manager)**:

##### Passo A: Organizar as Pastas
Mova o ficheiro `xuruvoip-server-windows-x64.exe` para uma pasta de sua escolha (como `C:\XuruVoipServer`).

##### Passo B: Execução Inicial
Inicie o executável num terminal PowerShell como administrador para criar a estrutura inicial. Feche-o pressionando `Ctrl+C` e personalize as configurações no `.env`.

##### Passo C: Registar o Serviço com NSSM
```powershell
# Executar a interface gráfica de instalação do NSSM
.\nssm.exe install XuruVoipServer "C:\XuruVoipServer\xuruvoip-server-windows-x64.exe"
```
Preencha a pasta de trabalho como `C:\XuruVoipServer` e clique em *Install service*.

##### Passo D: Iniciar o Serviço
```powershell
Start-Service -Name XuruVoipServer
```

---

## 🎮 Visão Geral das Definições do Cliente

A janela de definições está dividida em 6 secções principais:
1. **General**: Defina o idioma, informe o caminho do ficheiro `Game.log` do Star Citizen e ative gravações locais de log do cliente.
2. **Connection**: Configura IP do servidor, portas de áudio/posição, utilizador, palavras-passe da conta e do servidor.
3. **Position**: Escolha a fonte de posição ("Scanner de Ecrã OCR" vs. "Leitor de Game.log (GRTPR)"), selecciona monitor, frequência de varrimento (ms), delimita a região de leitura e visualiza a última extração de texto (opções de OCR são ocultadas se o GRTPR estiver activo).
4. **Audio**: Escolhe os dispositivos de áudio, ganhos, modo de ativação de voz (PTT / VAD), limiar de ruído, ativação de **3D Spatial Audio**, bem como configurações avançadas de degradação de rádio e chimes de microfone PTT.
5. **Hotkeys**: Regista as teclas físicas para falar no PTT, alternar capacete, mudar canal activo de rádio e as teclas de mute.
6. **Overlay (Incrustação)**: Ativação do HUD overlay transparente e configuração do canto do ecrã para posicionamento (ex. Superior esquerdo, Superior direito).

### Compilar e Executar o Cliente

#### Requisitos
- Windows 10 ou Windows 11
- SDK .NET 9.0 (com recursos WPF)

#### Compilar & Correr:
```powershell
cd client
dotnet run
```

### Instalar o Pacote de Lançamento (Release)

Como os ficheiros não possuem assinatura digital comercial, o SmartScreen do Windows pode alertar que o software provém de uma fonte desconhecida. Siga as instruções abaixo para desbloquear:

* **Option A: Instalador MSI (Recomendado)**
  1. Transfira o ficheiro `XuruVoipClient-win-x64.msi` na [página de versões (releases)](https://github.com/XuruDragon/XuruVOIP/releases).
  2. Clique com o botão direito no instalador `.msi` e abra **Propriedades**.
  3. Na guia *Geral*, marque a caixa **Desbloquear** no rodapé e clique em **Aplicar**.
  4. Execute o ficheiro e siga os passos do assistente de instalação.

* **Option B: Versão Portátil (ZIP)**
  1. Transfira o ficheiro `XuruVoipClient-win-x64.zip` na [página de versões (releases)](https://github.com/XuruDragon/XuruVOIP/releases).
  2. Clique com o botão direito no ficheiro `.zip`, seleccione **Propriedades** e marque a opção **Desbloquear** na guia *Geral*. Aplique as alterações.
  3. Extraia o conteúdo para a pasta desejada (ex: `C:\Games\XuruVoip`).
  4. Dê um clique duplo em `XuruVoipClient.exe` para usar o cliente.

---

## 👥 Créditos

Desenvolvido por **[@XuruDragon](https://github.com/XuruDragon)** em colaboração com **Antigravity IDE**.
