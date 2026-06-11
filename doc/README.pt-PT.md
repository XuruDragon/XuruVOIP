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
  <img src="../logo.png" alt="XuruVoip Logo" width="400" height="400" />
</p>

XuruVoip é um conjunto de **comunicação de voz 3D (VoIP)** de alto desempenho, seguro e dinamicamente espacializado, projetado especificamente para integrações de jogos personalizadas com **Star Citizen**. Ele consiste em um servidor backend baseado em Go e um cliente C# WPF moderno com um aplicativo Companion integrado (interface web) e integração Elgato Stream Deck.

### 🎯 Meta do Projeto
O objetivo do XuruVoip é fornecer aos eventos de jogos Star Citizen, organizações de roleplay e esquadrões táticos um **nível sem precedentes de imersão de áudio e conveniência operacional**. Ao ler coordenadas em tempo real, visor e estados do veículo no cliente do jogo, o XuruVoip molda dinamicamente as vozes dos jogadores no espaço 3D, simula atmosferas planetárias/de vácuo e roteia comunicações táticas automaticamente sem exigir configurações manuais do cliente.

---

### 🗺️ Diretório de navegação

| Seção | Descrição |
| :--- | :--- |
| [📖 Guias de usuário não técnicos](#-guias-do-usuário-não-técnicos) | Guias passo a passo fáceis de entender para cliente, servidor e stream deck. |
| [📸 Capturas de tela e IU](#-capturas-de-tela-e-interface-do-usuário) | Vitrine visual de telas de clientes, portal de administração e configurações. |
| [🗂️ Estrutura do Projeto](#️-project-structure) | Layout do repositório e divisão de pastas. |
| [⚙️ Arquitetura do Sistema](#️-system-architecture) | O diagrama de fluxo de trabalho real completo do cliente WPF, servidor Go e dispositivos externos. |
| [💡 Visão geral dos principais recursos](#-visão-geral-dos-principais-recursos) | Análise detalhada dos mais de 11 recursos espaciais e de rede implementados. |
| [🖥️ Servidor Go (Go)](#️-xuruvoip-server-go) | Instruções de construção, execução, implantação e configuração do servidor. |
| [🎛️Ponte de voz do Discord](#️-discord-voice-bridge-setup-guide) | Conectando canais de rádio do servidor Go a um canal de voz Discord. |
| [📱 Aplicativo complementar e plataforma de transmissão](#-integração-de-aplicativo-complementar-e-stream-deck) | Controle remoto de dispositivos e configuração de chaves físicas do Stream Deck. |
| [🛠️ Cliente WPF (C#)](#-building--running-the-client) | Requisitos do cliente, compilação e guias de instalação MSI/Portátil. |

---

## 📖 Guias do usuário não técnicos

Se você não tem experiência em ciência da computação, escrevemos guias simples e passo a passo para ajudá-lo a configurar e executar tudo facilmente:

* 🎮 **[Guia do usuário do cliente](doc/client_guide.md)**: Guia amigável sobre como escolher microfones/alto-falantes, configurar Push-to-Talk, usar capacetes de trajes espaciais e ativar efeitos de voz de esforço.
* 🖥️ **[Guia de configuração do servidor](doc/server_guide.md)**: Explica como hospedar um servidor, ajustar senhas/configurações no arquivo de configurações `.env` e configurar o Discord Voice Bridge.
* 🎛️ **[Guia de plug-in do Stream Deck](doc/streamdeck_guide.md)**: Passo a passo sobre como instalar botões físicos para silenciar, alternar o visor e exibir canais de rádio ativos.

---

## 📸 Capturas de tela e interface do usuário

<details>
<summary>📸 Clique para ver as capturas de tela</summary>

### 1. Janela principal do cliente
![Janela principal do cliente](/screenshots/main.png)

### 2. Guia Configurações de áudio (controle de áudio espacial 3D)
![Guia Configurações de áudio](/screenshots/audio.png)

### 3. Guia Configurações Gerais (Seleção de Idioma e Game.log)
![Guia Configurações Gerais](/screenshots/general.png)

### 4. Guia Configurações de conexão
![Guia Configurações de conexão](/screenshots/connection.png)

### 5. Guia de configurações de teclas de atalho
![Guia Configurações de teclas de atalho](/screenshots/hotkeys.png)

### 6. Guia Configurações de sobreposição (Vulkan e DirectX HUD)
![Guia Configurações de sobreposição](/screenshots/overlay.png)

### 7. Guia Configurações de OCR (Tesseract OCR)
![Guia Configurações de OCR](/screenshots/ocr.png)

### 8. Página de login do portal da web de administração
![Página de login do portal da Web do administrador](/screenshots/admin_login.png)

### 9. Painel do Portal de Administração da Web
![Painel do Portal do Administrador](/screenshots/admin_dashboard.png)

### 10. Jogadores do portal da Web de administração
![Jogadores do portal da Web do administrador](/screenshots/admin_players_list.png)

### 11. Lista de administradores do portal da Web de administração
![Lista de administradores do portal da Web do administrador](/screenshots/admin_admin_list.png)

### 12. Lista de banimentos do portal de administração
![Lista de banimentos do portal da Web do administrador](/screenshots/admin_ban_list.png)

</details>

---

## 🗂️ Estrutura do Projeto

- **/servidor**: back-end Go de alto desempenho que hospeda os serviços de posição, áudio e administração.
- **/client**: cliente C# WPF moderno utilizando NAudio, WebRtcVad e Tesseract OCR ou Game.log tail para rastreamento automatizado de localização e análise de log. O aplicativo complementar também está incluído neste projeto.
- **/streamdeck**: Plug-in Stream Deck para cliente XuruVoIP.

---

## ⚙️ Arquitetura do Sistema

Abaixo está a arquitetura real completa do sistema XuruVoip, ilustrando os loops de captura, posicionamento, reprodução e renderização de HUD dentro do cliente WPF, os hubs de websocket do servidor Go e as integrações externas:```mermaid
graph TB
    subgraph STIM ["Ambiente de jogo (Star Citizen)"]
        SC["Cliente Star Citizen"]
        LOGS["Game.log (arquivo de registro)"]
        SCREEN["Saída gráfica (Vulkan/DX)"]
    end

    subgraph WPF ["Cliente XuruVOIP WPF"]
        direction TB
        subgraph CAPT ["Captura de microfone e DSP"]
            MIC["Entrada de microfone"] --> VAD["WebRTC VAD"]
            VAD -->|Speech Detected| VC["Trocador de voz (alienígena/ciborgue/robô)"]
            VC -->|Modulated PCM| GF_FIL["Injeção de pitch e tremolo / esforço ofegante G-Force"]
            GF_FIL --> HELM_OSC["Sobreposição de respiração e zumbido do capacete"]
            HELM_OSC --> OPUS_ENC["Codificador Opus"]
        end

        subgraph POS_TRACK ["Posicionamento e rastreamento de estado"]
            LOGS -->|Tail Scanner| LOG_PAR["Analisador Game.log"]
            SCREEN -->|showlocations Capture| OCR["Mecanismo de OCR Tesseract"]
            LOG_PAR -->|Equip/Visor Events| HELM_DET["Sincronização automática do estado da viseira"]
            LOG_PAR -->|G-Force & Stamina Values| GF_DET["Rastreador de força G e esforço"]
            OCR -->|Coords| POS_SEL{"Seletor de Fonte"}
            LOG_PAR -->|Coords & ContainerID| POS_SEL
        end

        subgraph PLAY ["Reprodução Espacial e DSP"]
            OPUS_DEC["Decodificador Opus"] --> PKT_TYPE{"Tipo de pacote?"}
            PKT_TYPE -->|PA 0x03| PA_FIL["Megafone DSP (HP/LP, distorção tanh, reverberação de navio)"]
            PKT_TYPE -->|Proximity/Radio| OCC_FIL["Convés de Carraca/Hércules e Oclusão de Sala"]
            OCC_FIL --> REV_FIL["Reverberação com reconhecimento de localização (cavernas/bunkers/hangares)"]
            REV_FIL --> RAD_FIL["Passagem de banda de rádio e roteamento multi-hop de longo alcance (Dijkstra)"]
            RAD_FIL --> CHIMES["PTT Mic Chirps e gerador de cauda silenciadora"]
            CHIMES --> PAN["Matemática panorâmica 3D espacial"]
            PAN --> VOL["Atenuação de distância espacial"]
            VOL --> MIXER["Mixer de áudio"]
            PA_FIL --> MIXER
            MIXER --> SPK["Dispositivos de saída de áudio"]
        end

        subgraph HUD ["Sobreposição de HUD (clique em Win32)"]
            T_RAD["Mini-radar tático 2D"]
            STT["Whisper.net conversão de fala em texto"]
            OPUS_DEC -.->|Incoming Voice| STT
            STT -->|Subtitles| SUB["Legendas HUD em tempo real"]
        end

        subgraph COMP ["Servidor Web complementar"]
            HTTP_SRV["Ouvinte HTTP local (porta personalizada)"]
            DASH["Painel HTML/JS Glassmorphic"]
        end

        POS_SEL -->|Coordinates & Zone| POS_WS["Posição do cliente WS"]
        HELM_DET -->|Visor State| POS_WS
        GF_DET -->|G-Force / Exertion| GF_FIL
        OPUS_ENC -->|Audio Packets| AUD_WS["Cliente WS de áudio"]
    end

    subgraph SERVER ["Servidor XuruVOIP Go"]
        direction TB
        WS_HUB["Hub de conexão Websocket"]
        POS_HUB["Posicionamento Espacial e Zona Hub"]
        DB["Banco de dados SQLite e canais persistentes"]
        DISC_BRIDGE["Ponte de Voz Discord"]
        ADM_PORT["Portal da Web de administração (Canvas Live Radar)"]

        WS_HUB <--> POS_HUB
        POS_HUB <--> DB
        DISC_BRIDGE <--> WS_HUB
    end

    subgraph EXT ["Interfaces Externas"]
        DISC["Canal de voz de discórdia"] <-->|Bidirectional Voice Bridge| DISC_BRIDGE
        SD["Aplicativo Stream Deck"] <-->|WebSocket Actions / Port Setting| HTTP_SRV
        MOB["Controlador móvel"] <-->|REST API Status & Toggles| HTTP_SRV
    end

    POS_WS <-->|WS Port 8888| WS_HUB
    AUD_WS <-->|WS Port 8889| WS_HUB
```
---

## 💡 Visão geral dos principais recursos

### 1. 🔊 Áudio espacial 3D em tempo real
* **Panorâmica Estéreo Dinâmica:** PROJETA as coordenadas do alto-falante remoto nos vetores de direção Direita e Direita do ouvinte para calcular a panorâmica esquerda/direita exata usando uma fórmula de potência constante.
* **Resolução de ambiguidade frente-trás:** Atenua o volume do áudio em 25% se um alto-falante estiver atrás do ouvinte, resolvendo as limitações de panorâmica de áudio 2D padrão.
* **Distance Roll-Off:** Desvanece as vozes de proximidade linearmente com base na distância, garantindo níveis de volume naturais (desvanece completamente até zero a 50 metros, ou 5 metros para sussurros).

### 2. 🗺️ Acústica com reconhecimento de localização e oclusão de navio/bunker
* **Oclusão de deck e parede:** Detecta limites internos dentro de espaços. Se os jogadores estiverem em decks diferentes (por exemplo, Carrack, Hercules) ou salas (por exemplo, Bunkers), a filtragem passa-baixa (frequências de corte de 300 Hz a 900 Hz) e o amortecimento de volume são aplicados dinamicamente.
* **Reverberação Ambiental:** Lê a zona hierárquica do player e aplica automaticamente parâmetros personalizados de mixagem molhada, atraso e reverberação de feedback para **Caves**, **Bunkers** e **Hangares**.

### 3. 💨 Simulação atmosférica de capacete e EVA
* **EVA Muting:** Silencia automaticamente as comunicações de voz de proximidade em zonas espaciais ou de vácuo (EVA), forçando os jogadores a usar canais de rádio para se comunicar.
* **Sobreposição do respirador da viseira:** Simula a pressão do ar quando a viseira está abaixada. Sintetiza um ruído respiratório de baixa frequência e um zumbido do ventilador de ventilação de dupla frequência (50 Hz + 100 Hz) na alimentação do microfone capturada.
* **Sincronização automática da viseira:** Lê registros de anexos em `Game.log` para detectar automaticamente quando um capacete é equipado/removido e atualiza o estado da viseira em tempo real.

### 4. 🎙️ Trocador de voz de ficção científica e moduladores de terno
* **Filtros DSP em tempo real:** Mudança de pitch no domínio do tempo, flangeamento, modulação em anel, saturação soft-tanh e bitcrushing de 8 bits.
* **Predefinições atmosféricas:** Carregue instantaneamente perfis de voz predefinidos, incluindo **Alien**, **Cyborg**, **Robotic** ou **Custom Pitch Shift** (0,5x a 2,0x).

### 5. 📻 Degradação imersiva de rádio e sinos
* **Filtragem passa-banda:** Modela filtros de rádio com cortes baixos/altos ao usar canais de rádio ou quando os visores do traje estão abaixados.
* **Degradação do sinal de rádio:** Faixas de corte estreitas e misturas de ruído estático filtrado por passagem de banda conforme a distância entre os jogadores se aproxima do limite do transmissor de rádio.
* **Acoustic Radio Chimes:** Reproduz um som de mic-key de arrebatamento (900Hz a 700Hz) na tecla pressionada e uma cauda estática de silenciador na tecla levantada.

### 6. 💬 Sistema de intercomunicação automática de navio
* **Canais de intercomunicação de veículos:** Embarcar em um veículo inscreve automaticamente os jogadores em um canal de rádio dinâmico `Intercom_<ContainerID>`.
* **Redução de prioridade do piloto:** Quando um jogador na cabine ou no assento do motorista transmite no intercomunicador, o áudio de proximidade de todos os outros jogadores é reduzido em 85% para garantir a clareza do comando de vôo.
* **Degradação Dinâmica do Intercomunicador:** Os canais de intercomunicador degradam-se automaticamente com base no status da nave:
  * **Impactos no Escudo (Shield Hits):** Injeta temporariamente rajadas de estática e estalos de volume (dura 2,5 segundos).
  * **Energia Crítica (Critical Power):** Zumbido elétrico de corrente alternada de baixa tensão, distorção de saturação (soft-clipping) e queda de tom de voz (resampling).
  * **Viagem Quântica (Quantum Travel):** Varredura de filtro comb (flanger/phaser) e zumbido de alta frequência.
  * *Todos os subefeitos podem ser alternados individualmente nas Configurações Gerais e vêm desativados por padrão.*
* **Recarga de limpeza:** Faz uma contagem regressiva de 5 minutos após o último jogador deixar a nave antes de excluir o canal de intercomunicação, maximizando o desempenho do servidor.

### 7. 📡 Sobreposição de HUD compatível com Vulkan e radar tático 2D
* **Sobreposição Click-Through do Win32:** Uma sobreposição de HUD sem bordas que mostra conexões VoIP, frequências e estados de fala. Compatível com Vulkan e DirectX (executando em modo de janela sem borda).
* **Indicador de Status do Intercomunicador:** Exibe avisos como `⚡ INTERCOM: DEGRADED` (com detalhes de substatus como `[Power Loss]`, `[Quantum]` ou `[Static Pop]`) na sobreposição do HUD quando a degradação do intercomunicador está ativa.
* **Mini-Radar Tático:** Apresenta um radar HUD 2D alinhado ao rumo que exibe jogadores que falam relativamente, desenhando anéis sonoros pulsantes ao redor deles.
* **Legendas de fala para texto:** Transcreve áudio de rádio/proximidade recebido para legendas localizadas do HUD usando um modelo Whisper leve e off-line (`ggml-tiny.bin`).

### 8. 📱 Aplicativo complementar e API REST
* **Servidor Web HTTP local:** Hospeda um painel local em uma porta configurável (padrão: `8891`, desabilitado por padrão).
* **Controlador Glassmorphic:** Conecta-se a telefones ou telas secundárias para alternar mudos, ciclos de canal, capacetes ou trocadores de voz.
* **API REST:** Expõe endpoints `GET /api/status` e `POST /api/action` para integrações externas (incluindo o status do intercomunicador e simulação de estados).

### 9. 🎛️ Plug-in de plataforma de streaming
* **Stream Deck Action Pack:** Expõe 8 ações para controlar silenciamentos de microfone, silenciamentos de áudio, viseiras de capacete e ciclos de radiofrequência.
* **Ícones de teclas dinâmicas:** Gráficos de botão de atualização contínua de WebSockets (ciano ativo versus âmbar silenciado) para refletir o estado atual do cliente.
* **Título de frequência ao vivo:** Exibe os nomes dos canais de rádio ativos diretamente nos botões físicos do Stream Deck.

### 10. 🔌 Ponte de Voz Discord
* **Retransmissão de áudio bidirecional:** Retransmite as comunicações entre um canal de rádio do servidor Go e um canal de voz do Discord.
* **Mapeamento de apelidos:** Captura a fala do Discord e mapeia IDs SSRC para apelidos de servidor.

### 11. 🛡️ Segurança, rotação de log e radar de tela de administração
* **Rotação diária de logs:** Arquivador de logs de inicialização que retém apenas os 5 logs mais recentes.
* **Painel de administração:** Painel de administração da Web em tempo real com segurança de bloqueio, limitação de taxa e um mapa interativo de radar ao vivo em tela HTML5 2D que permite aos administradores ampliar, deslocar e rastrear trilhas históricas de jogadores.

### 12. 🤢 Distorção de voz por força G e esforço físico
* **Tremolo e mudança de tom:** Sob altas forças G, o áudio de saída do microfone é modulado dinamicamente com um tremolo LFO (4-10 Hz, até 40% de profundidade) e afinado (fator: 1,0 até 0,85) para simular tensão física, blecaute ou estados de redout.
* **Sobreposição de respiração pesada:** Sobrepõe automaticamente ruídos aleatórios de respiração ofegante/respiratória, aumentando a velocidade do ciclo respiratório com base nos níveis de resistência do jogador analisados ​​em tempo real no `Game.log`.
* **Controles manuais/API:** Alternáveis ​​por meio das configurações do cliente e dos controles deslizantes da interface da Web do aplicativo complementar para roleplay ou testes simulados.

### 13. 📡 Relé de rádio tático e sinalizadores repetidores multi-hop
* **Roteamento de sinal multi-hop:** Os jogadores podem alternar o "Modo Beacon" para atuar como um farol repetidor de rádio. Se dois jogadores estiverem fora do alcance direto do rádio (além de 1.500 m), o cliente receptor executa o algoritmo de caminho mais curto de Dijkstra em todos os repetidores ativos na zona.
* **Degradação da qualidade do pior salto:** Se existir um caminho de vários saltos abaixo do limite de salto único de 8.000 m, o sistema roteia a comunicação e aplica o fator de degradação do pior salto (qualidade do sinal) em vez da distância total em linha reta, permitindo redes de rádio planetárias/orbitais de longo alcance.
* **Estado WebSocket dinâmico:** Os estados do repetidor ativo são sincronizados em tempo real por meio do canal de controle WebSocket do servidor.

### 14. 📢 Sistema de transmissão de endereço público (PA) do navio
* **Transmissão de áudio para todo o navio:** Pilotos ou capitães de navios com tripulação múltipla podem transmitir anúncios de voz para todos os membros da tripulação que compartilham o mesmo `ContainerID` (navio) na mesma Zona.
* **PA DSP e campainha de buzina:** As transmissões de PA ignoram a proximidade local e os mudos de rádio (exceto volume/mudo principal), reproduzem panorâmica central mono, acrescentam um alerta de buzina/carrilhão de tom duplo Sci-Fi e aplicam um passa-banda de megafone e filtro de reverberação simulando a acústica interior de um navio oco.

---

## 🎮 Detalhamento da guia de configurações do cliente XuruVoip

A janela de configurações do WPF está estruturada em seis categorias de configuração:
1. **Geral**: Configure idiomas, acompanhe os arquivos `Game.log`, alterne o registro geral de arquivos e habilite/configure o **Servidor HTTP do aplicativo Companion** local e a porta.
2. **Conexão**: Edite o IP do servidor de destino, as portas de posição e áudio, o nome de usuário, a senha do usuário e a senha do servidor.
3. **Posição**: alterne a fonte de localização ("OCR Screen Scanner" vs "Game.log Reader (GRTPR)"), configure índices de monitor, regiões de corte, intervalos de OCR e visualize texto de coordenadas ao vivo.
4. **Áudio**: escolha o hardware de entrada/saída, ajuste os ganhos de dB, selecione o modo de transmissão (PTT vs VAD), configure os limites do VAD, alterne **Ativar áudio espacial 3D**, configure a degradação do rádio, sinos locais sintetizados, modulador do visor e selecione as predefinições do **Trocador de voz**.
5. **Teclas de atalho**: Vincule teclas ao PTT de proximidade, PTT de rádio, PTT de perfil, viseira de capacete, ciclo de canal de rádio e interruptores individuais de mudo de microfone e canal de áudio.
6. **Sobreposição**: alterne a sobreposição do HUD, defina posicionamentos nos cantos, ative o **Mini-radar tático** (com alcance máximo configurável) e alterne **Legendas de fala para texto** em tempo real.

---

## 🖥️ Servidor XuruVoip (Go)

O servidor coordena as posições dos jogadores, lida com autenticação segura e roteia pacotes de áudio dinamicamente com base na distância espacial e nos canais de rádio.

### Principais recursos

* **Controle de proximidade do lado do servidor**: retransmite dinamicamente o áudio de proximidade apenas para jogadores dentro do alcance (padrão de 50m ou sussurro de 5m).
* **Configuração Espacial**: Opção alternável do lado do servidor (`XURUVOIP_SPATIAL_AUDIO` em `.env`) que determina se as coordenadas ou apenas a distância devem ser enviadas aos clientes.
* **Roteamento de rádio multicanal**: permite que os jogadores ouçam vários canais de rádio simultaneamente enquanto transmitem em seu canal ativo.
* **Sistema de perfil de áudio**: atribui efeitos de áudio (por exemplo, filtro de rádio, eco) aos players.
* **Persistência SQLite**: Armazena preferências de canal do jogador e mapeamentos de perfil nas reinicializações do servidor.
* **Segurança anti-bypass**: Bane criadores de problemas por nome de usuário, IP e impressão digital de hardware (HWID/MachineGuid) para evitar evasão de banimentos.
* **Portal de administração da Web**: Interface da Web segura (HTTPS/WebSockets) para painéis em tempo real, streaming de logs, configuração de canal/perfil e gerenciamento de banimentos.
* **Mapa de radar de administração do servidor**: Radar de jogador em tempo real do HTML5 Canvas 2D integrado ao painel de administração, com suporte para movimento panorâmico de clicar e arrastar, zoom da roda do mouse, filtragem de zona ativa, trilhas históricas de caminhada do jogador (trilhas de navegação) e anéis de ondas sonoras concêntricas pulsantes ao vivo em torno de jogadores falantes.
* **Rotação do Log de Inicialização**: Verifica o log do servidor (`xuruvoip.log`) na inicialização. Se o arquivo de log contiver entradas de um dia anterior, ele será rotacionado para `xuruvoip.YYYY-MM-DD.log`. O servidor retém apenas os 5 arquivos rotacionados mais recentes e exclui os mais antigos para evitar o uso excessivo do disco.

### Configuração do servidor (`.env`)

Na primeira inicialização, o servidor gera automaticamente um arquivo `.env` contendo estes valores padrão:```env
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
### 🎛️ Guia de configuração do Discord Voice Bridge

Para conectar um canal de rádio do servidor Go local a um canal de voz Discord, siga estas etapas de configuração:

1. **Crie um aplicativo Discord Bot:**
   * Visite o [Portal do Desenvolvedor Discord](https://discord.com/developers/applications) e faça login.
   * Clique em **Novo Aplicativo**, dê um nome a ele (por exemplo, `XuruVOIP Bridge`) e clique em **Criar**.
   * Navegue até a guia **Bot** na barra lateral esquerda, clique em **Reset Token** e copie o **Bot Token** gerado. Cole isto como `XURUVOIP_DISCORD_TOKEN` no arquivo `.env` do seu servidor.
   * Em **Privileged Gateway Intents** na mesma página do bot, habilite **Message Content Intent** (necessário para ler comandos específicos).

2. **Convide o bot para o seu servidor Discord:**
   * Vá para a guia **OAuth2** e selecione **Gerador de URL**.
   * Em **Escopos**, marque `bot` e `applications.commands`.
   * Em **Permissões do bot**, selecione os seguintes privilégios:
     * *Permissões Gerais:* `Ver Canais`
     * *Permissões de texto:* `Enviar mensagens`
     * *Permissões de voz:* `Conectar`, `Falar`, `Usar atividade de voz`
   * Copie o URL gerado na parte inferior da página, cole-o em um navegador da web, selecione o servidor Discord de destino (Guilda) e clique em **Autorizar**.

3. **Obter IDs de servidor (guilda) e canal de voz:**
   * Abra o Discord, vá para **Configurações do usuário** -> **Avançado** e ative o **Modo de desenvolvedor**.
   * Clique com o botão direito no ícone do servidor Discord na lista de servidores e selecione **Copiar ID do servidor** (este é o seu ID da guilda). Cole-o como `XURUVOIP_DISCORD_GUILD_ID` em `.env`.
   * Clique com o botão direito no canal de voz do Discord de destino onde deseja que o bot entre e selecione **Copiar ID do canal**. Cole-o como `XURUVOIP_DISCORD_CHANNEL_ID` em `.env`.

4. **Canal de rádio do servidor Map Go:**
   * Configure `XURUVOIP_DISCORD_BRIDGE_CHANNEL` para o nome exato do canal de rádio que você deseja fazer a ponte (por exemplo, `General`, `Bravo`, `Alpha`, etc.). Qualquer áudio transmitido nesta frequência de rádio do servidor Go será transmitido bidirecionalmente para o Discord Voice Channel!

### Construindo o servidor a partir da fonte

####Linux```bash
cd server


GOOS="linux" GOARCH="amd64" go build .
# a "server" linux binary will be created in the current directory
```
####Janelas```powershell
cd server 

$env:GOOS="windows"
$env:GOARCH="amd64"
go build .
# a "server.exe" windows binary will be created in the current directory
```
### Executando o Servidor

#### Da fonte:```bash
cd server
go run .
```
#### Do binário:
#####Janelas```powershell
.\server.exe
```
#####Linux```bash
./server
```
### 🖥️ Configuração e implantação de servidor sem cabeça

Para instalações headless permanentes e prontas para produção, o servidor deve ser executado como um daemon/serviço de sistema em segundo plano que inicia automaticamente na inicialização e reinicia em caso de falha.

#### 1. Configuração de rede e firewall
Certifique-se de que as portas TCP de entrada definidas em seu arquivo `.env` (os padrões são `8888` para posições/portal de administração e `8889` para áudio espacial) estão abertas no firewall do host:
* **Linux (UFW):**  ```bash
  sudo ufw allow 8888/tcp
  sudo ufw allow 8889/tcp
  sudo ufw reload
  ```
* **Linux (firewall):**  ```bash
  sudo firewall-cmd --zone=public --add-port=8888/tcp --permanent
  sudo firewall-cmd --zone=public --add-port=8889/tcp --permanent
  sudo firewall-cmd --reload
  ```
---

#### 2. Implantação do Linux (systemd)

Siga estas etapas para implantar o servidor Go como um serviço systemd:

##### Etapa A: Diretório de configuração e permissões
Crie um usuário de sistema dedicado e um diretório de trabalho para isolamento de segurança:```bash
# Create a system user without login privileges
sudo useradd -r -s /bin/false xuruvoip

# Create installation directory and copy the binary
sudo mkdir -p /opt/xuruvoip
sudo cp xuruvoip-server-linux-x64 /opt/xuruvoip/xuruvoip-server
sudo chmod +x /opt/xuruvoip/xuruvoip-server

# Set ownership to the system user
sudo chown -R xuruvoip:xuruvoip /opt/xuruvoip
```
##### Etapa B: Gerar e configurar `.env`
Execute o servidor uma vez sob o usuário do sistema para gerar o arquivo de configuração e banco de dados `.env` padrão:```bash
sudo -u xuruvoip /opt/xuruvoip/xuruvoip-server -port 8888 -audio-port 8889
```
*Pressione `Ctrl+C` depois que o console imprimir as senhas geradas.* Em seguida, edite o arquivo `.env` gerado para personalizar as configurações (por exemplo, senhas, IP de ligação, alternância de áudio espacial):```bash
sudo nano /opt/xuruvoip/.env
```
##### Etapa C: Crie o arquivo de serviço systemd
Copie o arquivo de serviço do repositório `server/xuruvoip.service` para `/etc/systemd/system/xuruvoip-server.service` ou crie um novo arquivo de configuração de serviço `/etc/systemd/system/xuruvoip-server.service` com o seguinte conteúdo:```ini
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
##### Etapa D: ativar e iniciar o serviço```bash
# Reload systemd daemon to pick up the new unit file
sudo systemctl daemon-reload

# Enable the service to run on startup
sudo systemctl enable xuruvoip-server

# Start the service immediately
sudo systemctl start xuruvoip-server
```
##### Etapa E: Monitorar e registrar
Para verificar o status do serviço e os logs de streaming:```bash
# Check status
sudo systemctl status xuruvoip-server

# Stream log files in real-time
journalctl -u xuruvoip-server -f -n 100
```
---

#### 3. Implantação do Windows (NSSM)

Para executar o servidor como um serviço nativo do Windows no modo headless, é recomendado usar o **Non-Sucking Service Manager (NSSM)**:

##### Etapa A: Diretórios de configuração
Extraia/copie `xuruvoip-server-windows-x64.exe` para uma pasta de servidor dedicada (por exemplo, `C:\XuruVoipServer`).

##### Etapa B: inicializar a configuração
Abra um terminal PowerShell como administrador e execute o binário uma vez para gerar arquivos:```powershell
cd C:\XuruVoipServer
.\xuruvoip-server-windows-x64.exe
```
*Pressione `Ctrl+C` quando a inicialização terminar.* Personalize o arquivo `.env` gerado conforme necessário.

##### Etapa C: Instale o serviço via NSSM
Baixe o NSSM e instale o serviço executando:```powershell
# Open NSSM GUI installer
.\nssm.exe install XuruVoipServer "C:\XuruVoipServer\xuruvoip-server-windows-x64.exe"
```
No pop-up NSSM, configure:
* **Caminho:** `C:\XuruVoipServer\xuruvoip-server-windows-x64.exe`
* **Diretório de inicialização:** `C:\XuruVoipServer`
* Clique em **Instalar serviço**.

##### Etapa D: iniciar o serviço
Inicie o serviço usando PowerShell ou Services Manager (`services.msc`):```powershell
Start-Service -Name XuruVoipServer
```
---

### Construindo e executando o cliente

#### Requisitos
-Janelas 10/11
- SDK do .NET 9.0 (suporte WPF)

#### Compilar e executar:```powershell
cd client
dotnet run
```
### Instalando o pacote de lançamento

Como o instalador e os executáveis não são assinados digitalmente, o Windows SmartScreen pode bloqueá-los inicialmente. Você pode desbloqueá-los facilmente usando o menu de propriedades.

* **Opção A: Instalador MSI (recomendado)**
  1. Baixe `XuruVoipClient-win-x64.msi` da [página de lançamentos](https://github.com/XuruDragon/XuruVOIP/releases).
  2. Para evitar que o Windows SmartScreen bloqueie a instalação:
     - Clique com o botão direito no arquivo `XuruVoipClient-win-x64.msi` baixado e selecione **Propriedades**.
     - Na janela de propriedades na guia *Geral*, marque a caixa de seleção **Desbloquear** na parte inferior.
     - Clique em **Aplicar** e feche a janela Propriedades.
  3. Clique duas vezes no arquivo para executar o instalador e siga as instruções solicitadas.
     *(Observação: você verá um prompt padrão do Controle de Conta de Usuário do Windows "Editor Desconhecido"; basta clicar em **Sim** ou **Executar** para continuar.)*

* **Opção B: Versão ZIP portátil**
  1. Baixe `XuruVoipClient-win-x64.zip` da [página de lançamentos](https://github.com/XuruDragon/XuruVOIP/releases).
  2. Extraia os arquivos do pacote ZIP para qualquer pasta de sua preferência (por exemplo, `C:\Games\XuruVoip`):
  3. Em seguida, clique com o botão direito no arquivo `XuruVoipClient.exe` extraído e selecione **Propriedades**.
     - Na janela de propriedades na guia *Geral*, marque a caixa de seleção **Desbloquear** na parte inferior.
     - Clique em **Aplicar** e feche a janela Propriedades.
  4. Clique duas vezes em `XuruVoipClient.exe` para executar o cliente diretamente sem instalá-lo.

## 📱 Integração de aplicativo complementar e stream deck

O XuruVOIP inclui um serviço web integrado do Companion App e um plugin oficial do Stream Deck que permite monitorar e acionar ações de voz diretamente de dispositivos secundários ou chaves físicas.

### 1. Habilitando o aplicativo complementar e o MFD do mapa tático
Por padrão, o servidor HTTP local do Companion App e o modo do mapa tático estão desabilitados para economizar recursos do sistema. Para habilitá-los:
1. Abra o cliente XuruVOIP e clique no ícone **Configurações**.
2. Na guia **Geral**, marque a caixa **Habilitar servidor HTTP Companion** (porta padrão: `8891`).
3. Para habilitar a exibição do radar, marque a caixa de seleção aninhada **Habilitar mapa tático do copiloto (MFD)**.
4. Clique em **Salvar e Fechar** para aplicar.
5. Acesse o painel: Você pode abrir `http://localhost:8891` em qualquer navegador em seu PC, tablet ou celular. Se o modo de mapa estiver habilitado, uma nova guia **🗺️ Mapa Tático** estará disponível, exibindo uma tela de radar HUD baseada em Canvas que rastreia a posição em tempo real do seu personagem, rumo, contatos da tripulação na mesma zona e indicadores de fala ativa.

---

### 2. Instalação do plug-in Stream Deck
O pacote de lançamento inclui o arquivo `.streamDeckPlugin` pré-empacotado.
1. Baixe `com.xuru.voip.streamDeckPlugin` da [página de lançamentos](https://github.com/XuruDragon/XuruVOIP/releases).
2. Clique duas vezes no arquivo para instalá-lo diretamente no software Elgato Stream Deck. 
   *(Como alternativa, você pode extrair e copiar manualmente a pasta `com.xuru.voip.sdPlugin` para `%appdata%\Elgato\StreamDeck\Plugins\`)*
3. Depois de instalada, uma nova categoria de ação chamada **XuruVOIP** aparecerá na lista do lado direito do seu aplicativo de desktop Stream Deck.

---

### 3. Adicionando e configurando ações
Você pode arrastar e soltar qualquer uma das 8 ações a seguir nas teclas do Stream Deck:
* 🎤 **Silenciar proximidade**: Alterna o silenciamento do microfone de proximidade de saída.
* 📻 **Rádio Mudo**: Alterna o silenciamento do microfone do rádio de saída.
* 👤 **Perfil mudo**: Alterna o silenciamento do microfone do perfil de saída.
* 🔊 **Audio Proximity Mute**: Alterna o silenciamento da reprodução de proximidade de entrada.
* 🔊 **Audio Radio Mute**: Alterna o silenciamento da reprodução do rádio recebido.
* 🔊 **Desativar perfil de áudio**: alterna o silenciamento da reprodução do perfil de entrada.
* 🪖 **Alternar capacete**: alterna a viseira do capacete do traje espacial para baixo ou para cima.
* 🔄 **Cycle Radio**: percorre os canais de rádio disponíveis.

#### Configuração (Inspetor de Propriedade):
Para cada ação arrastada para uma tecla, clique nela e configure a porta de destino no painel **Property Inspector** na parte inferior:
* Defina **Companion Port** para corresponder à porta configurada nas configurações do cliente WPF (padrão: `8891`).
* **Feedback dinâmico:** Alternadores (como Proximity Mute) atualizam automaticamente seu ícone em tempo real no seu dispositivo para exibir se o estado está ativo (ícone de brilho ciano) ou mudo (ícone âmbar riscado).
* **Exibição de frequência ao vivo:** A tecla **Cycle Radio** exibirá dinamicamente o nome da frequência atualmente ativa (por exemplo, `120.5` ou `General`) diretamente no botão físico em tempo real!

---

## 👥 Créditos

Desenvolvido por **[@XuruDragon](https://github.com/XuruDragon)** em colaboração com **Antigravity IDE**.