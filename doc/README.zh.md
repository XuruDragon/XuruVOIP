# XuruVoip (简体中文)

<p align="center">
  <a href="https://github.com/XuruDragon/XuruVOIP/actions/workflows/tests.yml">
    <img src="https://github.com/XuruDragon/XuruVOIP/actions/workflows/tests.yml/badge.svg" alt="测试状态" />
  </a>
  <a href="https://github.com/XuruDragon/XuruVOIP/releases">
    <img src="https://img.shields.io/github/v/release/XuruDragon/XuruVOIP?color=blue&logo=github" alt="最新发布" />
  </a>
  <a href="https://github.com/XuruDragon/XuruVOIP/releases">
    <img src="https://img.shields.io/github/downloads/XuruDragon/XuruVOIP/total?color=green&logo=github" alt="总下载量" />
  </a>
</p>

<p align="center">
  <b>语言翻译:</b><br/>
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
  <img src="../logo.png" alt="XuruVoip 徽标" width="400" height="400" />
</p>

XuruVoip 是一款专为 **星际公民 (Star Citizen)** 自定义游戏集成设计的高性能、安全且支持动态 3D 空间音效的**语音通信 (VoIP) 套件**。它由基于 Go 的后端服务端和现代 C# WPF 客户端组成。

---

## 📸 屏幕截图与用户界面

### 1. 客户端主窗口
![客户端主窗口](/screenshots/main.png)

### 2. 音频设置选项卡 (3D 空间音频控制)
![音频设置选项卡](/screenshots/audio.png)

### 3. 常规设置选项卡 (语言与 Game.log 选择)
![常规设置选项卡](/screenshots/general.png)

### 4. 连接设置选项卡
![连接设置选项卡](/screenshots/connection.png)

### 5. 快捷键设置选项卡
![快捷键设置选项卡](/screenshots/hotkeys.png)

### 6. 管理员网页后台登录页面
![管理员网页后台登录页面](/screenshots/admin_login.png)

### 7. 管理员网页后台仪表盘
![管理员网页后台仪表盘](/screenshots/admin_dashboard.png)

### 8. 管理员网页后台玩家列表
![管理员网页后台玩家列表](/screenshots/admin_players_list.png)

### 9. 管理员网页后台管理员列表
![管理员网页后台管理员列表](/screenshots/admin_admin_list.png)

### 10. 管理员网页后台封禁列表
![管理员网页后台封禁列表](/screenshots/admin_ban_list.png)

---

## 🗂️ 项目结构

- **/server**: 基于 Go 的高性能后端，托管位置同步、音频转发及网页后台管理服务。
- **/client**: 现代 C# WPF 客户端，利用 NAudio、WebRtcVad 以及 Tesseract OCR 进行自动位置跟踪和日志解析。

---

## ⚙️ 应用程序工作原理 (客户端架构)

C# WPF 客户端与星际公民进程并发运行，执行实时音频捕获、语音检测、屏幕坐标识别以及混音回放。以下是客户端工作原理的工作流拓扑：

```mermaid
graph TD
    subgraph 音频捕获与发送
        Mic[麦克风输入] -->|PCM 音频| VAD[WebRTC 语音活动检测]
        VAD -->|活动语音| OpusEnc[Opus 编码器]
        OpusEnc -->|Opus 数据包| AudioWS[音频 WebSocket 客户端]
        AudioWS -->|WebSocket 端口 8889| Server[Go 服务端]
    end

    subgraph 位置与头盔状态同步
        SC[星际公民进程] -->|r_DisplaySessionInfo| Screen[截屏]
        Screen -->|预处理| Tess[Tesseract OCR 引擎]
        Tess -->|多行文本解析| Zone[层级区域过滤器]
        Zone -->|听众坐标与区域| PosWS[位置 WebSocket 客户端]
        PosWS -->|WebSocket 端口 8888| Server

        SC -->|实时日志| GameLog[Game.log 文件]
        GameLog -->|实时读取任务| LogParser[日志服务解析器]
        LogParser -->|头盔装备/卸下事件| Helmet[头盔状态同步]
        Helmet -->|头盔状态数据包| PosWS
    end

    subgraph 回放与空间混音
        Server -->|目标邻近音频 + 元数据| AudioWS
        AudioWS -->|Opus 帧 + 邻近元数据| Decoder[Opus 解码器]
        Decoder -->|单声道浮点 PCM| DSP[电台 DSP 滤波器]
        DSP -->|单声道| Panner[PanningSampleProvider]
        Panner -->|双声道立体声| Volume[VolumeSampleProvider]
        
        LogParser -.->|本地头盔状态| DSP
        Zone -.->|听众位置与朝向| MixerMath[空间声学计算]
        
        MixerMath -->|声相平衡参数| Panner
        MixerMath -->|距离与脑后声衰减| Volume
        
        Volume -->|左右立体声| Mixer[MixingSampleProvider]
        Mixer -->|混音输出| Speakers[音频输出设备]
    end
```

### 1. 音频捕获、VAD 语音活动检测和压缩
* **音频捕获：** 客户端使用 **NAudio** API 以高保真度 48,000 Hz、16-bit 单声道录制麦克风。
* **语音活动检测 (VAD)：** 通过内置原生 **WebRtcVad** 进行实时评估。若语音置信度低于预设阈值则停止发送，有效隔离键盘打字声和风扇噪音。
* **数据压缩：** 活跃的语音段通过 C# **Concentus** 库压缩为 **Opus** 数据包，并通过 WebSockets 直接传输至音频服务器。

### 2. 位置跟踪和朝向估算
* **截屏与 OCR 识别：** 客户端定时截取屏幕中输出游戏坐标（`/showlocations` 或 `r_DisplaySessionInfo`）的区域。图像经预处理后送入 **Tesseract OCR** 识别。
* **层级区域过滤：** 识别结果包含层级区域（行星、飞船舱室、电梯等）。客户端自动忽略细微区域波动（如电梯内或座椅上），从而保证邻近区域的玩家依然可以连贯无阻地通话。
* **朝向估算：** 客户端通过坐标差（$Position_{当前} - Position_{历史}$）自动计算位移向量作为视角朝向。在角色静止时，朝向将维持最后移动时的状态。

### 3. 头盔装备状态实时检测
* **日志尾部读取 (Tail Scan)：** 后台线程实时监测星际公民生成的 `Game.log` 文件。
* **状态同步：** 检测到头盔部件装备日志（`FP_Visor`、`helmethook_attach`）时，立即自动更新头盔模式（开启/关闭），无须手动按键。

### 4. 3D 空间立体声混音与 DSP
* **数据接收：** 接收来自服务端的 Opus 音频流，附带发声者坐标、距离和最大作用范围。
* **空间混音声学投影：** 音频将被投影至听众的相对坐标系中：
  * **立体声声相 (Pan)：** 控制左右声道的平衡比（`-1.0` 极左至 `+1.0` 极右）。
  * **脑后声衰减：** 若发声者位于听众身后，音量自动降低最多 25%，用于克服声学前/后定位模糊。
  * **距离衰减：** 随距离成线性比例淡出，到达最大传播范围（默认 50m）时降低为零。
* **回放处理：** Opus 帧经解码后，通过 **电台 DSP 滤波器**（在任意一方戴头盔或使用电台频道时启用），进行空间定位和距离衰减，最终通过 NAudio 的 `MixingSampleProvider` 回放。

---

## 🖥️ XuruVoip 服务端 (Go)

服务端整合各玩家的位置，进行鉴权，并根据距离和电台频道动态投递音频数据包。

### 核心功能
* **服务端邻近控制**：仅向处于近接范围内（默认 50m）的玩家转发音频包。
* **空间数据共享**：在 `.env` 中通过 `XURUVOIP_SPATIAL_AUDIO` 调节是共享物理坐标，还是仅向客户端共享两者的相对距离。
* **多电台频道**：允许玩家在主力电台频道通话时，同时旁听其他多个设定的电台广播。
* **音频 Profile 特效**：为玩家应用电台滤波、回声等声学特效。
* **SQLite 数据库持久化**：长期保存服务器频道结构和玩家的偏好。
* **高强度封禁安全保障**：支持按 Username、IP 以及硬件指纹 (HWID/MachineGuid) 进行封禁，防止绕过。
* **Web 后台管理系统**：支持 HTTPS/WebSockets，可在浏览器端实时监视日志、活动和配置封禁列表。

### 服务端配置选项 (`.env`)
初次运行服务端时，将自动创建如下默认设置文件：
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

### 从源码编译

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

### 运行服务端

#### 源码运行：
```bash
cd server
go run .
```

#### 运行二进制文件：
##### Windows
```powershell
.\server.exe
```

##### Linux
```bash
./server
```

### 🖥️ 无头 (Headless) 服务端安装与部署

对于永久运行的生产服务器，建议在无图形界面的系统下，将服务端注册为后台守护进程 (Service/Daemon) 以支持自启动和崩溃自动拉起。

#### 1. 网络与防火墙配置
确保放行在 `.env` 中定义的 TCP 端口（默认端口：`8888` 网页端/数据端口，`8889` 音频端口）：
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

#### 2. Linux 部署环境 (systemd)

按照以下步骤将 Go 服务端封装为 systemd 独立服务：

##### 步骤 A: 准备独立运行用户与目录
创建独立的系统服务账号和目录，防止提权安全漏洞：
```bash
# 创建无登录壳的系统账号
sudo useradd -r -s /bin/false xuruvoip

# 创建工作文件夹并拷贝运行文件
sudo mkdir -p /opt/xuruvoip
sudo cp xuruvoip-server-linux-x64 /opt/xuruvoip/xuruvoip-server
sudo chmod +x /opt/xuruvoip/xuruvoip-server

# 转移文件夹所有权
sudo chown -R xuruvoip:xuruvoip /opt/xuruvoip
```

##### 步骤 B: 初始化生成 `.env` 配置文件
以该独立系统用户身份，手动拉起运行一次生成默认配置文件：
```bash
sudo -u xuruvoip /opt/xuruvoip/xuruvoip-server -port 8888 -audio-port 8889
```
*在控制台输出完成随机密码后按 `Ctrl+C` 结束运行。* 然后调整 `.env` 变量配置：
```bash
sudo nano /opt/xuruvoip/.env
```

##### 步骤 C: 创建 systemd 服务配置文件
拷贝仓库中的配置文件 `server/xuruvoip.service` 至 `/etc/systemd/system/xuruvoip-server.service` 或新建文件写入以下内容：
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

##### 步骤 D: 激活并开启服务
```bash
sudo systemctl daemon-reload
sudo systemctl enable xuruvoip-server
sudo systemctl start xuruvoip-server
```

##### 步骤 E: 查询状态与日志监控
```bash
# 查询当前运行状态
sudo systemctl status xuruvoip-server

# 追踪实时日志输出
journalctl -u xuruvoip-server -f -n 100
```

---

#### 3. Windows 部署环境 (NSSM)

建议使用 **NSSM (Non-Sucking Service Manager)** 工具将该服务端注册为 Windows 后台服务运行：

##### 步骤 A: 新建工作目录
将程序 `xuruvoip-server-windows-x64.exe` 移入专用空目录（如 `C:\XuruVoipServer`）。

##### 步骤 B: 初始化生成设置
在管理员 PowerShell 中手动执行该文件一次，自动生成设置文件后按 `Ctrl+C` 退出，并调整 `.env`。

##### 步骤 C: 通过 NSSM 安装服务
```powershell
# 打开 NSSM 图形化服务安装向导
.\nssm.exe install XuruVoipServer "C:\XuruVoipServer\xuruvoip-server-windows-x64.exe"
```
在向导界面中配置 *Startup directory* 为 `C:\XuruVoipServer`，确认安装。

##### 步骤 D: 运行服务
```powershell
Start-Service -Name XuruVoipServer
```

---

## 🎮 XuruVoip 客户端设置选项卡详解

客户端的配置被规划为五个功能选项卡：
1. **General (常规)**: 选择界面语言、星际公民日志（`Game.log`）路径以及是否允许生成本地诊断日志。
2. **Connection (连接)**: 配置后端 IP、音频与位置同步端口、游戏代号、本地存储密码和登录服务器的鉴权 Password。
3. **OCR (识别器)**: 指派截屏显示器、设定扫描精度（ms）、拖动规划扫描区域以及读取分析器输出预览。
4. **Audio (音频)**: 绑定耳麦设备、调整麦克风与回放增益、指定激活机制（按键 PTT / 门限 VAD）、调整 VAD 灵敏度并启用 **3D 空间音频** (在服务器允许的前提下可用)。
5. **Hotkeys (快捷键)**: 绑定近接、电台、群组的 PTT 键，开关头盔的按键，切换电台频道键和各项信道的禁音 (Mute) 键。

### 编译并启动客户端

#### 运行环境
- Windows 10 或 Windows 11
- .NET 9.0 SDK（含 WPF 开发框架支持）

#### 编译与运行：
```powershell
cd client
dotnet run
```

### 安装发布包

因为没有购买商业数字证书对二进制文件签名，双击安装可能会被 Windows SmartScreen 拦截阻碍。您需要手动解除阻止属性。

* **选项 A: MSI 安装包 (推荐)**
  1. 从 [Releases 页面](https://github.com/XuruDragon/XuruVOIP/releases) 下载 `XuruVoipClient-win-x64.msi`。
  2. 右击该文件选择 **属性**。
  3. 在 *常规* 选项卡的最底部，勾选 **解除锁定** 选框，并点击 **应用**。
  4. 双击运行安装，按照向导完成即可。

* **选项 B: 便携式 ZIP 压缩包**
  1. 下载 `XuruVoipClient-win-x64.zip`。
  2. 右键查看属性，勾选 **解除锁定** 确认。
  3. 将压缩包解压至指定空目录中 (例如 `C:\Games\XuruVoip`)。
  4. 双击双击 `XuruVoipClient.exe` 直接运行使用。

---

## 👥 鸣谢

由 **[@XuruDragon](https://github.com/XuruDragon)** 与 **Antigravity IDE** 合作开发。
