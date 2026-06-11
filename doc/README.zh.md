# XuruVoip (简体中文)

<p align="center">
  <a href="https://github.com/XuruDragon/XuruVOIP/actions/workflows/tests.yml">
    <img src="https://github.com/XuruDragon/XuruVOIP/actions/workflows/tests.yml/badge.svg" alt="Tests Status" />
  </a>
  <a href="https://github.com/XuruDragon/XuruVOIP/releases">
    <img src="https://img.shields.io/github/v/release/XuruDragon/XuruVOIP?color=blue&logo=github" alt="Latest Release" />
  </a>
</p>

<p align="center">
  <b>翻译:</b><br/>
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

XuruVoip 是一款高性能、安全且动态空间化的 **3D 语音通信 (VoIP) 套件**，专为与 **Star Citizen** 的自定义游戏集成而设计。它由基于 Go 的后端服务器和带有内置 Companion App（Web 界面）和 Elgato Stream Deck 集成的现代 C# WPF 客户端组成。

### 🎯 项目目标
XuruVoip 的目标是为《星际公民》游戏活动、角色扮演组织和战术小队提供**前所未有的音频沉浸感和操作便利性**。通过从游戏客户端读取实时坐标、护目镜和车辆状态，XuruVoip 在 3D 空间中动态塑造玩家声音，模拟行星/真空气氛，并自动路由战术通信，无需手动客户端配置。

---

### 🗺️ 导航目录

|部分|描述 |
| :--- | :--- |
| [📖 非技术用户指南](#-非技术用户指南) |易于理解的客户端、服务器和 Stream Deck 分步指南。 |
| [📸 屏幕截图和用户界面](#-屏幕截图和用户界面) |客户端屏幕、管理门户和设置的视觉展示。 |
| [🗂️ 项目结构](#️-project-structure) |存储库布局和文件夹细分。 |
| [⚙️系统架构](#️-system-architecture) | WPF客户端、Go服务器和外部设备的完整实际工作流程图。 |
| [💡核心功能概述](#-核心功能概述) |超过 11 个已实现的空间和网络功能的详细分类。 |
| [🖥️ Go 服务器 (Go)](#️-xuruvoip-server-go) |服务器构建、运行、部署和配置说明。 |
| [🎛️ Discord 语音桥](#️-discord-voice-bridge-setup-guide) |将 Go 服务器无线电频道连接到 Discord 语音频道。 |
| [📱 配套应用程序和 Stream Deck](#-配套应用程序和-stream-deck-集成) |远程设备控制和 Stream Deck 物理键设置。 |
| [🛠️ WPF 客户端 (C#)](#-building--running-the-client) |客户端要求、编译和 MSI/便携式安装指南。 |

---

## 📖 非技术用户指南

如果您没有计算机科学背景，我们编写了简单的分步指南来帮助您轻松配置和运行所有内容：

* 🎮 **[客户端用户指南](doc/client_guide.md)**：关于选择麦克风/扬声器、设置一键通、使用太空服头盔以及打开用力语音效果的友好指南。
* 🖥️ **[服务器配置指南](doc/server_guide.md)**：解释如何托管服务器、调整 `.env` 设置文件中的密码/设置以及设置 Discord 语音桥。
* 🎛️ **[Stream Deck 插件指​​南](doc/streamdeck_guide.md)**：安装用于静音、遮阳板切换和显示活动无线电频道的物理按钮的演练。

---

## 📸 屏幕截图和用户界面

<details>
<summary>📸 点击查看截图</summary>

### 1. 主客户端窗口
![主客户端窗口](/screenshots/main.png)

### 2. 音频设置选项卡（3D 空间音频控制）
![音频设置选项卡](/screenshots/audio.png)

### 3.常规设置选项卡（语言和游戏日志选择）
![常规设置选项卡](/screenshots/general.png)

### 4. 连接设置选项卡
![连接设置选项卡](/screenshots/connection.png)

### 5.热键设置选项卡
![热键设置选项卡](/screenshots/hotkeys.png)

### 6. 叠加设置选项卡（Vulkan 和 DirectX HUD）
![叠加设置选项卡](/screenshots/overlay.png)

### 7. OCR 设置选项卡（Tesseract OCR）
![OCR 设置选项卡](/screenshots/ocr.png)

### 8. 管理门户登录页面
![管理员门户网站登录页面](/screenshots/admin_login.png)

### 9. 管理门户仪表板
![管理员门户网站仪表板](/screenshots/admin_dashboard.png)

### 10. 管理门户网站玩家
![管理员门户网站玩家](/screenshots/admin_players_list.png)

### 11. 管理门户网站管理员列表
![管理员门户网站管理员列表](/screenshots/admin_admin_list.png)

### 12. 管理员门户网站禁令列表
![管理员门户网站禁令列表](/screenshots/admin_ban_list.png)

</details>

---

## 🗂️ 项目结构

- **/server**：托管位置、音频和管理服务的高性能 Go 后端。
- **/client**：现代 C# WPF 客户端利用 NAudio、WebRtcVad 和 Tesseract OCR 或 Game.log tail 进行自动位置跟踪和日志解析。配套应用程序也包含在该项目中。
- **/streamdeck**：XuruVoIP 客户端的 Stream Deck 插件。

---

## ⚙️ 系统架构

下面是 XuruVoip 系统的完整实际架构，说明了 WPF 客户端、Go 服务器 Websocket 集线器以及外部集成内的捕获、定位、播放和 HUD 渲染循环：```mermaid
graph TB
    subgraph STIM ["游戏环境（星际公民）"]
        SC["星际公民客户端"]
        LOGS["游戏.log（日志文件）"]
        SCREEN["图形输出 (Vulkan/DX)"]
    end

    subgraph WPF ["XuruVOIP WPF 客户端"]
        direction TB
        subgraph CAPT ["麦克风采集和 DSP"]
            MIC["麦克风输入"] --> VAD["WebRTC VAD"]
            VAD -->|Speech Detected| VC["变声器（外星人/机器人/机器人）"]
            VC -->|Modulated PCM| GF_FIL["G-Force Pitch & Tremolo / Exertion Panting 注射"]
            GF_FIL --> HELM_OSC["头盔呼吸和通风嗡嗡声覆盖"]
            HELM_OSC --> OPUS_ENC["作品编码器"]
        end

        subgraph POS_TRACK ["定位和状态跟踪"]
            LOGS -->|Tail Scanner| LOG_PAR["游戏日志解析器"]
            SCREEN -->|showlocations Capture| OCR["Tesseract OCR 引擎"]
            LOG_PAR -->|Equip/Visor Events| HELM_DET["遮阳板状态自动同步"]
            LOG_PAR -->|G-Force & Stamina Values| GF_DET["G 力和运动追踪器"]
            OCR -->|Coords| POS_SEL{"源选择器"}
            LOG_PAR -->|Coords & ContainerID| POS_SEL
        end

        subgraph PLAY ["空间播放和 DSP"]
            OPUS_DEC["作品解码器"] --> PKT_TYPE{"数据包类型？"}
            PKT_TYPE -->|PA 0x03| PA_FIL["扩音器 DSP（HP/LP、tanh 失真、船舶混响）"]
            PKT_TYPE -->|Proximity/Radio| OCC_FIL["卡拉克/大力士甲板和房间遮挡"]
            OCC_FIL --> REV_FIL["位置感知混响（洞穴/掩体/机库）"]
            REV_FIL --> RAD_FIL["无线电带通和远程多跳路由 (Dijkstra)"]
            RAD_FIL --> CHIMES["PTT 麦克风线性调频声和静噪尾音发生器"]
            CHIMES --> PAN["空间 3D 平移数学"]
            PAN --> VOL["空间距离衰减"]
            VOL --> MIXER["NA音频调音台"]
            PA_FIL --> MIXER
            MIXER --> SPK["音频输出设备"]
        end

        subgraph HUD ["HUD 覆盖（Win32 点击通过）"]
            T_RAD["战术二维迷你雷达"]
            STT["Whisper.net 语音转文本"]
            OPUS_DEC -.->|Incoming Voice| STT
            STT -->|Subtitles| SUB["实时 HUD 字幕"]
        end

        subgraph COMP ["配套网络服务器"]
            HTTP_SRV["本地 HTTP 侦听器（自定义端口）"]
            DASH["Glassmorphic HTML/JS 仪表板"]
        end

        POS_SEL -->|Coordinates & Zone| POS_WS["职位 WS 客户"]
        HELM_DET -->|Visor State| POS_WS
        GF_DET -->|G-Force / Exertion| GF_FIL
        OPUS_ENC -->|Audio Packets| AUD_WS["音频 WS 客户端"]
    end

    subgraph SERVER ["XuruVOIP Go服务器"]
        direction TB
        WS_HUB["Websocket 连接集线器"]
        POS_HUB["空间定位和区域中心"]
        DB["SQLite 数据库和持久通道"]
        DISC_BRIDGE["Discord 语音桥"]
        ADM_PORT["管理门户网站（Canvas 实时雷达）"]

        WS_HUB <--> POS_HUB
        POS_HUB <--> DB
        DISC_BRIDGE <--> WS_HUB
    end

    subgraph EXT ["外部接口"]
        DISC["Discord 语音频道"] <-->|Bidirectional Voice Bridge| DISC_BRIDGE
        SD["流甲板应用程序"] <-->|WebSocket Actions / Port Setting| HTTP_SRV
        MOB["移动控制器"] <-->|REST API Status & Toggles| HTTP_SRV
    end

    POS_WS <-->|WS Port 8888| WS_HUB
    AUD_WS <-->|WS Port 8889| WS_HUB
```
---

## 💡 核心功能概述

### 1. 🔊 实时 3D 空间音频
* **动态立体声平移：** 将远程扬声器坐标投影到收听者的前向和右方向向量上，以使用恒定功率公式计算精确的左/右平移。
* **前后模糊度分辨率：** 如果扬声器站在听众后面，则将音量衰减 25%，从而解决标准 2D 音频平移限制。
* **距离滚降：** 根据距离线性淡出邻近声音，确保自然响度级别（在 50 米处完全淡入零，或在 5 米处耳语）。

### 2. 🗺️ 位置感知声学和船舶/掩体遮挡
* **甲板和墙壁遮挡：** 检测空间内的内部边界。如果玩家位于不同的牌组（例如 Carrack、Hercules）或房间（例如 Bunkers），则会动态应用低通滤波（截止频率从 300Hz 到 900Hz）和音量阻尼。
* **环境混响：** 读取播放器的分层区域，并自动为 **Caves**、**Bunkers** 和 **Hangars** 应用自定义湿混音、延迟和反馈混响参数。

### 3. 💨 头盔 & EVA 大气模拟
* **EVA 静音：** 自动静音太空或真空区域 (EVA) 中的近距离语音通信，迫使玩家使用无线电频道进行通信。
* **面罩呼吸器覆盖层：** 模拟面罩放下时的气压。将低频呼吸声和双频 (50Hz + 100Hz) 套装通风风扇嗡嗡声合成到捕获的麦克风馈送上。
* **自动遮阳板同步：** 读取“Game.log”中的附件日志，以自动检测何时佩戴/移除头盔并实时更新遮阳板状态。

### 4. 🎙️ 科幻变声器和套装调制器
* **实时 DSP 滤波器：** 时域音调移位、镶边、环形调制、软 tanh 饱和和 8 位位压缩。
* **氛围预设：** 立即加载预设语音配置文件，包括 **Alien**、**Cyborg**、**Robotic** 或 **Custom Pitch Shift**（0.5x 至 2.0x）。

### 5. 📻 沉浸式无线电衰减和提示音
* **带通滤波：** 在使用无线电频道或防护面罩放下时，对无线电滤波器进行低/高截止建模。
* **无线电信号衰减：** 当玩家之间的距离接近无线电发射器限制时，截止带变窄并混合带通滤波的静态噪声。
* **原声无线电铃声：** 按下按键时播放音调清扫的麦克风按键鸣音（900Hz 至 700Hz），按下按键时播放静噪静态尾音。

### 6. 💬 船舶自动对讲系统
* **车辆对讲频道：** 登上车辆会自动为玩家订阅动态“对讲_<ContainerID>”无线电频道。
* **飞行员优先闪避：** 当驾驶舱或驾驶员座位上的玩家通过对讲机进行传输时，所有其他玩家的邻近音频都会闪避 85%，以确保飞行命令的清晰度。
* **动态对讲衰减：** 对讲通道会根据飞船状态自动衰减：
  * **护盾受击 (Shield Hits)：** 临时注入静电噪声和音量裂音（持续 2.5 秒）。
  * **临界电力 (Critical Power)：** 低电压交流哼声、软剪切失真和重采样引起的音调下降。
  * **量子旅行 (Quantum Travel)：** 梳状滤波器（法兰/相位）扫频和高频微鸣。
  * *所有子效果均可在通用设置中单独启用或禁用，默认情况下处于禁用状态。*
* **清理冷却时间：** 最后一名玩家离开飞船后倒计时 5 分钟，然后删除对讲通道，从而最大限度地提高服务器性能。

### 7. 📡 兼容 Vulkan 的 HUD 叠加层和 2D 战术雷达
* **Win32 点击覆盖：** 无边界 HUD 覆盖，显示 VoIP 连接、频率和通话状态。 Vulkan 和 DirectX 兼容（在无边界窗口模式下运行）。
* **对讲状态指示器：** 当对讲衰减激活时，在 HUD 叠加层上显示诸如 `⚡ INTERCOM: DEGRADED`（包含子状态细节，如 `[Power Loss]`、`[Quantum]` 或 `[Static Pop]`）的警告。
* **战术迷你雷达：** 采用航向对齐的 2D HUD 雷达，可显示相对说话的玩家，并在他们周围绘制脉动的音圈。
* **语音转文本字幕：** 使用离线轻量级 Whisper 模型 (`ggml-tiny.bin`) 将传入的无线电/接近音频转录为本地化的 HUD 字幕。

### 8. 📱 配套应用程序和 REST API
* **本地 HTTP Web 服务器：** 在可配置端口上托管本地仪表板（默认值：“8891”，默认情况下禁用）。
* **Glassmorphic 控制器：** 从手机或辅助屏幕连接以切换静音、频道循环、头盔或变声器。
* **REST API：** 公开端点“GET /api/status”和“POST /api/action”以进行外部集成（包括对讲状态获取和模拟状态覆盖）。

### 9. 🎛️ Stream Deck 插件
* **Stream Deck Action Pack：** 提供 8 个操作来控制麦克风静音、音频静音、头盔面罩和射频周期。
* **动态按键图标：** 连续 WebSocket 更新按钮图形（活跃青色与静音琥珀色）以反映当前客户端状态。
* **直播频率标题：** 直接在物理 Stream Deck 按钮上显示活动无线电频道名称。

### 10. 🔌Discord 语音桥
* **双向音频中继：** 在 Go 服务器无线电通道和 Discord 语音通道之间中继通信。
* **昵称映射：** 捕获 Discord 语音并将 SSRC ID 映射到服务器昵称。

### 11.🛡️ 安全、日志轮换和管理画布雷达
* **每日日志轮换：** 启动日志归档程序仅保留 5 个最新日志。
* **管理仪表板：** 实时 Web 管理面板，具有锁定安全性、速率限制和交互式 2D HTML5 Canvas 实时雷达地图，允许管理员缩放、平移和跟踪历史玩家轨迹。

### 12. 🤢 G 力和体力消耗声音失真
* **颤音和音调变换：** 在高 G 力下，传出的麦克风音频通过颤音 LFO（4-10Hz，高达 40% 深度）进行动态调制并降低音调（因子：1.0 降至 0.85）以模拟物理应变、停电或红停状态。
* **重呼吸覆盖：** 自动覆盖随机的喘气/呼吸噪音，根据从“Game.log”实时解析的玩家耐力水平缩放呼吸周期速度。
* **手动/API 控制：** 可通过客户端设置和配套应用程序 Web UI 滑块进行切换，以进行角色扮演或模拟测试。

### 13. 📡 战术无线电中继和多跳中继器信标
* **多跳信号路由：** 玩家可以切换“信标模式”以充当无线电中继器信标。如果两个玩家超出直接无线电范围（超过 1500m），接收器客户端会对该区域中的所有活动中继器执行 Dijkstra 的最短路径算法。
* **最差跳点质量下降：** 如果在 8000m 单跳限制下存在多跳路径，系统将路由通信并应用最差跳点的下降系数（信号质量）而不是总直线距离，从而实现远程行星/轨道无线电网络。
* **动态 WebSocket 状态：** 活动转发器状态通过服务器的 WebSocket 控制通道实时同步。

### 14. 📢 船舶公共广播 (PA) 广播系统
* **全船音频广播：** 多船员船舶的飞行员或船长可以向同一区域中共享相同“ContainerID”（船舶）的所有船员广播语音公告。
* **PA DSP 和高音喇叭编钟：** PA 传输绕过本地邻近和无线电静音（主音量/静音除外），播放单声道中心声像，前置科幻双音编钟/高音喇叭警报，并应用扩音器带通和混响滤波器模拟空心船内部声学。

---

## 🎮 XuruVoip 客户端设置选项卡细分

WPF 设置窗口分为六个配置类别：
1. **常规**：配置语言、尾部“Game.log”文件、切换常规文件日志记录，以及启用/配置本地**配套应用程序 HTTP 服务器**和端口。
2. **连接**：编辑目标服务器IP、位置和音频端口、用户名、用户密码和服务器密码。
3. **位置**：切换位置源（“OCR 屏幕扫描仪”与“Game.log Reader (GRTPR)”），配置监视器索引、裁剪区域、OCR 间隔和预览实时坐标文本。
4. **音频**：选择输入/输出硬件、调整 dB 增益、选择传输模式（PTT 与 VAD）、配置 VAD 阈值、切换 **启用 3D 空间音频**、配置无线电降级、合成本地铃声、遮阳板调制器，并选择 **Voice Changer** 预设。
5. **热键**：将按键绑定到近距离 PTT、无线电 PTT、配置文件 PTT、头盔面罩、无线电通道周期以及单独的麦克风和音频通道静音开关。
6. **叠加**：切换 HUD 叠加、设置角位置、启用 **战术迷你雷达**（具有可配置的最大范围），以及切换实时 **语音转文本字幕**。

---

## 🖥️ XuruVoip 服务器（开始）

服务器协调玩家位置，处理安全身份验证，并根据空间距离和无线电信道动态路由音频数据包。

### 主要特点

* **服务器端邻近控制**：动态地将邻近音频仅转发给范围内的玩家（默认 50m，或 5m 耳语）。
* **空间配置**：可切换的服务器端选项（“.env”中的“XURUVOIP_SPATIAL_AUDIO”）确定是否应将坐标或仅距离发送到客户端。
* **多频道无线电路由**：允许玩家在其活动频道上传输时同时收听多个广播频道。
* **音频配置文件系统**：为播放器分配音频效果（例如无线电滤波器、回声）。
* **SQLite 持久性**：在服务器重新启动时存储玩家频道首选项和配置文件映射。
* **防绕过安全**：通过用户名、IP 和硬件指纹 (HWID/MachineGuid) 禁止麻烦制造者，以防止躲避禁令。
* **Web 管理门户**：用于实时仪表板、日志流、通道/配置文件配置和禁令管理的安全 Web 界面 (HTTPS/WebSockets)。
* **服务器管理雷达图**：2D HTML5 Canvas 实时玩家雷达集成到管理仪表板中，支持点击并拖动平移、鼠标滚轮缩放、活动区域过滤、历史玩家行走轨迹（面包屑）以及正在说话的玩家周围的实时脉动同心声波环。
* **启动日志轮转**：启动时检查服务器日志（`xuruvoip.log`）。如果日志文件包含前一天的条目，则会轮换为“xuruvoip.YYYY-MM-DD.log”。服务器仅保留 5 个最近轮换的文件并删除较旧的文件以防止过多的磁盘使用。

### 服务器配置（`.env`）

首次启动时，服务器会自动生成一个包含以下默认值的“.env”文件：```env
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
### 🎛️ Discord 语音桥设置指南

要将本地 Go 服务器无线电通道桥接到 Discord 语音通道，请按照以下设置步骤操作：

1. **创建一个 Discord 机器人应用程序：**
   * 访问 [Discord 开发者门户](https://discord.com/developers/applications) 并登录。
   * 单击“**新建应用程序**”，为其命名（例如“XuruVOIP Bridge”），然后单击“**创建**”。
   * 导航到左侧边栏的 **Bot** 选项卡，单击 **Reset Token**，然后复制生成的 **Bot Token**。将此作为“XURUVOIP_DISCORD_TOKEN”粘贴到服务器的“.env”文件中。
   * 在同一机器人页面上的 **Privileged Gateway Intents** 下，启用 **Message Content Intent**（读取特定命令所需）。

2. **邀请机器人到您的 Discord 服务器：**
   * 转到 **OAuth2** 选项卡，然后选择 **URL 生成器**。
   * 在**范围**下，选中“bot”和“applications.commands”。
   * 在 **机器人权限** 下，选择以下权限：
     * *一般权限：* `查看频道`
     * *文本权限：* `发送消息`
     * *语音权限：* `连接`、`讲话`、`使用语音活动`
   * 复制页面底部生成的 URL，将其粘贴到网络浏览器中，选择您的目标 Discord 服务器（公会），然后单击 **授权**。

3. **获取服务器（公会）和语音频道ID：**
   * 打开 Discord，转到 **用户设置** -> **高级**，然后打开 **开发者模式**。
   * 右键单击​​服务器列表中的 Discord 服务器图标，然后选择 **复制服务器 ID**（这是您的 Guild ID）。将其作为“XURUVOIP_DISCORD_GUILD_ID”粘贴到“.env”中。
   * 右键单击​​您希望机器人加入的目标 Discord 语音频道，然后选择 **复制频道 ID**。将其作为“XURUVOIP_DISCORD_CHANNEL_ID”粘贴到“.env”中。

4. **Map Go 服务器广播频道：**
   * 将“XURUVOIP_DISCORD_BRIDGE_CHANNEL”配置为您要桥接的无线电频道的确切名称（例如“General”、“Bravo”、“Alpha”等）。在此 Go 服务器无线电频率上传输的任何音频都将双向广播到 Discord 语音频道！

### 从源代码构建服务器

#### Linux```bash
cd server


GOOS="linux" GOARCH="amd64" go build .
# a "server" linux binary will be created in the current directory
```
#### 窗口```powershell
cd server 

$env:GOOS="windows"
$env:GOARCH="amd64"
go build .
# a "server.exe" windows binary will be created in the current directory
```
### 运行服务器

####来源：```bash
cd server
go run .
```
#### 来自二进制：
##### Windows```powershell
.\server.exe
```
##### Linux```bash
./server
```
### 🖥️ 无头服务器设置和部署

对于永久的、生产就绪的无头安装，服务器应作为后台系统守护程序/服务运行，在启动时自动启动并在出现故障时重新启动。

#### 1. 网络和防火墙配置
确保“.env”文件中定义的传入 TCP 端口（位置/管理门户的默认值为“8888”，空间音频的默认值为“8889”）在主机防火墙上打开：
* **Linux (UFW):**  ```bash
  sudo ufw allow 8888/tcp
  sudo ufw allow 8889/tcp
  sudo ufw reload
  ```
* **Linux（防火墙）：**  ```bash
  sudo firewall-cmd --zone=public --add-port=8888/tcp --permanent
  sudo firewall-cmd --zone=public --add-port=8889/tcp --permanent
  sudo firewall-cmd --reload
  ```
---

#### 2.Linux部署（systemd）

请按照以下步骤将 Go 服务器部署为 systemd 服务：

##### 步骤 A：设置目录和权限
创建专用系统用户和工作目录进行安全隔离：```bash
# Create a system user without login privileges
sudo useradd -r -s /bin/false xuruvoip

# Create installation directory and copy the binary
sudo mkdir -p /opt/xuruvoip
sudo cp xuruvoip-server-linux-x64 /opt/xuruvoip/xuruvoip-server
sudo chmod +x /opt/xuruvoip/xuruvoip-server

# Set ownership to the system user
sudo chown -R xuruvoip:xuruvoip /opt/xuruvoip
```
##### 步骤 B：生成并配置 `.env`
在系统用户下运行一次服务器，生成默认的`.env`配置文件和数据库：```bash
sudo -u xuruvoip /opt/xuruvoip/xuruvoip-server -port 8888 -audio-port 8889
```
*控制台打印生成的密码后按“Ctrl+C”。*然后，编辑生成的“.env”文件以自定义设置（例如密码、绑定IP、空间音频切换）：```bash
sudo nano /opt/xuruvoip/.env
```
##### 步骤 C：创建 systemd 服务文件
将服务文件从存储库 `server/xuruvoip.service` 复制到 `/etc/systemd/system/xuruvoip-server.service` 或使用以下内容创建一个新的服务配置文件 `/etc/systemd/system/xuruvoip-server.service`：```ini
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
##### 步骤 D：启用并启动服务```bash
# Reload systemd daemon to pick up the new unit file
sudo systemctl daemon-reload

# Enable the service to run on startup
sudo systemctl enable xuruvoip-server

# Start the service immediately
sudo systemctl start xuruvoip-server
```
##### 步骤 E：监控和日志
要检查服务状态和流日志：```bash
# Check status
sudo systemctl status xuruvoip-server

# Stream log files in real-time
journalctl -u xuruvoip-server -f -n 100
```
---

#### 3. Windows 部署 (NSSM)

要在无头模式下将服务器作为本机 Windows 服务运行，建议使用 **Non-Sucking Service Manager (NSSM)**：

##### 步骤 A：设置目录
将“xuruvoip-server-windows-x64.exe”提取/复制到专用服务器文件夹（例如“C:\XuruVoipServer”）。

##### 步骤 B：初始化配置
以管理员身份打开 PowerShell 终端并运行一次二进制文件以生成文件：```powershell
cd C:\XuruVoipServer
.\xuruvoip-server-windows-x64.exe
```
*启动完成后按“Ctrl+C”。*根据需要自定义生成的“.env”文件。

##### 步骤 C：通过 NSSM 安装服务
下载 NSSM 并通过运行以下命令安装服务：```powershell
# Open NSSM GUI installer
.\nssm.exe install XuruVoipServer "C:\XuruVoipServer\xuruvoip-server-windows-x64.exe"
```
在 NSSM 弹出窗口中，配置：
* **路径：** `C:\XuruVoipServer\xuruvoip-server-windows-x64.exe`
* **启动目录：** `C:\XuruVoipServer`
* 单击**安装服务**。

##### 步骤 D：启动服务
使用 PowerShell 或服务管理器 (`services.msc`) 启动服务：```powershell
Start-Service -Name XuruVoipServer
```
---

### 构建并运行客户端

#### 要求
- Windows 10/11
- .NET 9.0 SDK（WPF 支持）

#### 编译并运行：```powershell
cd client
dotnet run
```
### 安装发布包

由于安装程序和可执行文件没有经过数字签名，Windows SmartScreen 最初可能会阻止它们。您可以使用属性菜单轻松解锁它们。

* **选项 A：MSI 安装程序（推荐）**
  1. 从 [发布页面](https://github.com/XuruDragon/XuruVOIP/releases) 下载 `XuruVoipClient-win-x64.msi`。
  2. 要防止 Windows SmartScreen 阻止安装：
     - 右键单击下载的“XuruVoipClient-win-x64.msi”文件，然后选择“**属性**”。
     - 在“常规”选项卡下的属性窗口中，选中底部的“解除阻止”复选框。
     - 单击“**应用**”，然后关闭“属性”窗口。
  3. 双击该文件运行安装程序并按照提示进行操作。
     *（注意：您将看到标准的 Windows 用户帐户控制“未知发布者”提示；只需单击 **是** 或 **运行** 即可继续。）*

* **选项 B：便携式 ZIP 版本**
  1. 从 [发布页面](https://github.com/XuruDragon/XuruVOIP/releases) 下载 `XuruVoipClient-win-x64.zip`。
  2. 将 ZIP 包中的文件解压到您选择的任何文件夹（例如“C:\Games\XuruVoip”）：
  3. 然后右键单击解压的“XuruVoipClient.exe”文件并选择“**属性**”。
     - 在“常规”选项卡下的属性窗口中，选中底部的“解除阻止”复选框。
     - 单击“**应用**”，然后关闭“属性”窗口。
  4.双击“XuruVoipClient.exe”直接运行客户端，无需安装。

## 📱 配套应用程序和 Stream Deck 集成

XuruVOIP 包括内置的 Companion App Web 服务和官方 Stream Deck 插件，允许您直接从辅助设备或物理按键监控和触发语音操作。

### 1. 启用配套应用程序与战术地图 MFD
默认情况下，配套应用程序本地 HTTP 服务器和战术地图模式均处于禁用状态，以节省系统资源。要启用它们：
1. 打开 XuruVOIP 客户端并单击 **设置** 图标。
2. 在 **常规** 选项卡中，选中 **启用配套 HTTP 服务器** 框（默认端口：`8891`）。
3. 要启用雷达显示，请选中嵌套的 **启用战术副驾驶地图 (MFD)** 复选框。
4. 单击“**保存并关闭**”进行应用。
5. 访问仪表板：您可以在 PC、平板电脑或移动设备上的任何浏览器中打开“http://localhost:8891”。如果启用了地图模式，将显示一个新的 **🗺️ 战术地图** 选项卡，该选项卡提供一个基于 Canvas 的 HUD 雷达屏幕，可实时跟踪您角色的位置、航向、同区域的队友联系人以及发言状态指示。

---

### 2. Stream Deck 插件安装
发布包中包含预打包的“.streamDeckPlugin”文件。
1. 从 [发布页面](https://github.com/XuruDragon/XuruVOIP/releases) 下载 `com.xuru.voip.streamDeckPlugin`。
2. 双击该文件将其直接安装到您的 Elgato Stream Deck 软件中。 
   *（或者，您可以手动提取`com.xuru.voip.sdPlugin`文件夹并将其复制到`%appdata%\Elgato\StreamDeck\Plugins\`）*
3. 安装后，一个名为 **XuruVOIP** 的新操作类别将出现在 Stream Deck 桌面应用程序的右侧列表中。

---

### 3. 添加和配置操作
您可以将以下 8 个操作中的任意一个拖放到 Stream Deck 键上：
* 🎤 **接近静音**：切换传出接近麦克风静音。
* 📻 **无线电静音**：切换传出无线电麦克风静音。
* 👤 **配置文件静音**：切换传出配置文件麦克风静音。
* 🔊 **音频邻近静音**：切换传入邻近播放静音。
* 🔊 **音频广播静音**：切换传入广播播放静音。
* 🔊 **音频配置文件静音**：切换传入配置文件播放静音。
* 🪖 **切换头盔**：向下或向上切换您的宇航服头盔护目镜。
* 🔄 **循环广播**：循环播放可用的广播频道。

#### 配置（属性检查器）：
对于拖到键上的每个操作，单击它并在底部的 **Property Inspector** 面板中配置目标端口：
* 设置 **配套端口** 以匹配 WPF 客户端设置中配置的端口（默认值：“8891”）。
* **动态反馈：** 切换（如接近静音）会自动在设备上实时更新其图标，以显示状态是活动（青色发光图标）还是静音（琥珀色删除线图标）。
* **实时频率显示：** **循环收音机**键将直接在物理按钮上实时动态显示当前活动的频率名称（例如“120.5”或“常规”）！

---

## 👥 制作人员

由 **[@XuruDragon](https://github.com/XuruDragon)** 与 **Antigravity IDE** 合作开发。