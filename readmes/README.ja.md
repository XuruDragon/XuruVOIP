#シュルヴォイプ

<p align="center">
  <a href="https://github.com/XuruDragon/XuruVOIP/actions/workflows/tests.yml">
    <img src="https://github.com/XuruDragon/XuruVOIP/actions/workflows/tests.yml/badge.svg" alt="Tests Status" />
  </a>
  <a href="https://github.com/XuruDragon/XuruVOIP/releases">
    <img src="https://img.shields.io/github/v/release/XuruDragon/XuruVOIP?color=blue&logo=github" alt="Latest Release" />
  </a>
</p>

<p align="center">
  <b>翻訳:</b><br/>
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

XuruVoip は、**Star Citizen** とのカスタム ゲーム統合用に特別に設計された、高性能、安全、動的に空間化された **3D 音声通信 (VoIP) スイート**です。これは、Go ベースのバックエンド サーバーと、組み込みのコンパニオン アプリ (Web インターフェイス) と Elgato Stream Deck 統合を備えた最新の C# WPF クライアントで構成されています。

### 🎯 プロジェクトの目標
XuruVoip の目標は、Star Citizen ゲーム イベント、ロールプレイ組織、戦術部隊に **前例のないレベルのオーディオ没入感と操作の利便性**を提供することです。 XuruVoip は、ゲーム クライアントからリアルタイムの座標、バイザー、車両の状態を読み取ることで、3D 空間でプレイヤーの音声を動的に形成し、惑星/真空の大気をシミュレートし、手動のクライアント構成を必要とせずに戦術コミュニケーションを自動的にルーティングします。

---

### 🗺️ ナビゲーション ディレクトリ

|セクション |説明 |
| :--- | :--- |
| [📖 詳細機能ガイド](../doc/functionnalities.md) | 実装された20以上の機能に関する技術的およびユーザー向けの解説。 |
| [📖 非技術的なユーザー ガイド](#-技術以外のユーザーガイド) |クライアント、サーバー、ストリームデッキの分かりやすいステップバイステップガイド。 |
| [📸 スクリーンショットと UI](#-スクリーンショットと-ui) |クライアント画面、管理ポータル、設定の視覚的なショーケース。 |
| [🗂️ プロジェクトの構造](#️-project-structure) |リポジトリのレイアウトとフォルダーの内訳。 |
| [⚙️ システム アーキテクチャ](#️-system-architecture) | WPF クライアント、Go サーバー、および外部デバイスの完全な実際のワークフロー図。 |
| [💡 コア機能の概要](#-コア機能の概要) | 19 を超える実装された空間およびネットワーク機能の詳細な内訳。 |
| [🖥️ Go サーバー (Go)](#️-xuruvoip-server-go) |サーバーの構築、実行、展開、および構成の手順。 |
| [🎛️Discord Voice Bridge](#️-discord-voice-bridge-setup-guide) | Go サーバーのラジオ チャネルを Discord 音声チャネルに接続します。 |
| [📱 コンパニオン アプリとストリーム デッキ](#-コンパニオン-アプリとストリーム-デッキの統合) |リモートデバイスコントロールとStream Deck物理キーのセットアップ。 |
| [🛠️ WPF クライアント (C#)](#-building--running-the-client) |クライアントの要件、コンパイル、および MSI/Portable のインストール ガイド。 |

---

## 📖 技術以外のユーザーガイド

コンピューター サイエンスのバックグラウンドがない場合でも、すべてを簡単に構成して実行できるように、簡単なステップバイステップのガイドを作成しました。

* 📖 **[詳細機能ガイド](../doc/functionnalities.md)**: 実装された各機能の詳細な解説、仕組み、使用方法、およびメリット。
* 🎮 **[クライアント ユーザー ガイド](doc/client_guide.md)**: マイク/スピーカーの選択、プッシュ トゥ トークの設定、宇宙服ヘルメットの使用、および運動時の音声効果の有効化に関するフレンドリーなガイド。
* 🖥️ **[サーバー構成ガイド](doc/server_guide.md)**: サーバーをホストする方法、「.env」設定ファイルのパスワード/設定を調整する方法、および Discord Voice Bridge をセットアップする方法について説明します。
* 🎛️ **[ストリーム デッキ プラグイン ガイド](doc/streamdeck_guide.md)**: ミュート、バイザーの切り替え、アクティブな無線チャンネルの表示のための物理ボタンのインストールに関するチュートリアル。

---

## 📸 スクリーンショットと UI

<details>
<summary>📸 クリックしてスクリーンショットを表示</summary>

### 1. メインクライアントウィンドウ
![メインクライアントウィンドウ](/screenshots/main.png)

### 2. オーディオ設定タブ (3D 空間オーディオ コントロール)
![オーディオ設定タブ](/screenshots/audio.png)

### 3. [一般設定] タブ (言語と Game.log の選択)
![一般設定タブ](/screenshots/general.png)

### 4. [接続設定] タブ
![接続設定タブ](/screenshots/connection.png)

### 5. [ホットキー設定] タブ
![ホットキー設定タブ](/screenshots/hotkeys.png)

### 6. オーバーレイ設定タブ (Vulkan および DirectX HUD)
![オーバーレイ設定タブ](/screenshots/overlay.png)

### 7. OCR設定タブ(Tesseract OCR)
![OCR 設定タブ](/screenshots/ocr.png)

### 8. 管理者 Web ポータルのログイン ページ
![管理者 Web ポータルのログイン ページ](/screenshots/admin_login.png)

### 9. 管理者 Web ポータル ダッシュボード
![管理者 Web ポータル ダッシュボード](/screenshots/admin_dashboard.png)

### 10. 管理者 Web ポータル プレーヤー
![管理者 Web ポータル プレーヤー](/screenshots/admin_players_list.png)

### 11. 管理者 Web ポータル管理者リスト
![管理者 Web ポータル管理者リスト](/screenshots/admin_admin_list.png)

### 12. 管理者 Web ポータルの禁止リスト
![管理者 Web ポータル禁止リスト](/screenshots/admin_ban_list.png)

</details>

---

## 🗂️ プロジェクトの構造

- **/server**: 位置、音声、および管理サービスをホストする高性能 Go バックエンド。
- **/client**: 自動位置追跡とログ解析のために NAudio、WebRtcVad、Tesseract OCR または Game.log テールを利用する最新の C# WPF クライアント。コンパニオン アプリもこのプロジェクトに含まれています。
- **/streamdeck**: XuruVoIP クライアント用の Stream Deck プラグイン。

---

## ⚙️ システムアーキテクチャ

以下は、XuruVoip システムの完全な実際のアーキテクチャであり、WPF クライアント内のキャプチャ、位置決め、再生、および HUD レンダリング ループ、Go サーバー WebSocket ハブ、および外部統合を示しています。```mermaid
graph TB
    subgraph STIM ["ゲーム環境（Star Citizen）"]
        SC["スターシチズンクライアント"]
        LOGS["Game.log (ログファイル)"]
        SCREEN["グラフィック出力 (Vulkan/DX)"]
    end

    subgraph WPF ["XuruVOIP WPF クライアント"]
        direction TB
        subgraph CAPT ["マイクキャプチャとDSP"]
            MIC["マイク入力"] --> VAD["WebRTC VAD"]
            VAD -->|Speech Detected| VC["ボイスチェンジャー（エイリアン/サイボーグ/ロボット）"]
            VC -->|Modulated PCM| GF_FIL["G-Force ピッチ & トレモロ / 激しい喘ぎ噴射"]
            GF_FIL --> HELM_OSC["ヘルメット呼吸とベントハムオーバーレイ"]
            HELM_OSC --> OPUS_ENC["オーパスエンコーダー"]
        end

        subgraph POS_TRACK ["位置決めと状態追跡"]
            LOGS -->|Tail Scanner| LOG_PAR["Game.log パーサー"]
            SCREEN -->|showlocations Capture| OCR["Tesseract OCR エンジン"]
            LOG_PAR -->|Equip/Visor Events| HELM_DET["バイザー状態の自動同期"]
            LOG_PAR -->|G-Force & Stamina Values| GF_DET["G フォースと運動トラッカー"]
            OCR -->|Coords| POS_SEL{"ソースセレクター"}
            LOG_PAR -->|Coords & ContainerID| POS_SEL
        end

        subgraph PLAY ["空間再生とDSP"]
            OPUS_DEC["オーパスデコーダー"] --> PKT_TYPE{"パケットタイプ?"}
            PKT_TYPE -->|PA 0x03| PA_FIL["メガホン DSP (HP/LP、タンディストーション、シップリバーブ)"]
            PKT_TYPE -->|Proximity/Radio| OCC_FIL["キャラック/ヘラクレス デッキとルーム オクルージョン"]
            OCC_FIL --> REV_FIL["位置認識リバーブ (洞窟/バンカー/格納庫)"]
            REV_FIL --> RAD_FIL["無線バンドパスと長距離マルチホップ ルーティング (ダイクストラ)"]
            RAD_FIL --> CHIMES["PTTマイクチャープ&スケルチテールジェネレーター"]
            CHIMES --> PAN["空間 3D パン演算"]
            PAN --> VOL["空間距離減衰"]
            VOL --> MIXER["Nオーディオミキサー"]
            PA_FIL --> MIXER
            MIXER --> SPK["オーディオ出力デバイス"]
        end

        subgraph HUD ["HUD オーバーレイ (Win32 クリックスルー)"]
            T_RAD["戦術 2D ミニレーダー"]
            STT["Whisper.net Speech-to-Text"]
            OPUS_DEC -.->|Incoming Voice| STT
            STT -->|Subtitles| SUB["リアルタイム HUD 字幕"]
        end

        subgraph COMP ["コンパニオン Web サーバー"]
            HTTP_SRV["ローカル HTTP リスナー (カスタム ポート)"]
            DASH["Glassmorphic HTML/JS ダッシュボード"]
        end

        POS_SEL -->|Coordinates & Zone| POS_WS["WSクライアントの位置"]
        HELM_DET -->|Visor State| POS_WS
        GF_DET -->|G-Force / Exertion| GF_FIL
        OPUS_ENC -->|Audio Packets| AUD_WS["オーディオ WS クライアント"]
    end

    subgraph SERVER ["XuruVOIP Go サーバー"]
        direction TB
        WS_HUB["WebSocket接続ハブ"]
        POS_HUB["空間位置決めとゾーンハブ"]
        DB["SQLite DB と永続チャネル"]
        DISC_BRIDGE["Discord ボイスブリッジ"]
        ADM_PORT["管理者 Web ポータル (キャンバス ライブ レーダー)"]

        WS_HUB <--> POS_HUB
        POS_HUB <--> DB
        DISC_BRIDGE <--> WS_HUB
    end

    subgraph EXT ["外部インターフェース"]
        DISC["Discord ボイスチャンネル"] <-->|Bidirectional Voice Bridge| DISC_BRIDGE
        SD["ストリームデッキアプリ"] <-->|WebSocket Actions / Port Setting| HTTP_SRV
        MOB["モバイルコントローラー"] <-->|REST API Status & Toggles| HTTP_SRV
    end

    POS_WS <-->|WS Port 8888| WS_HUB
    AUD_WS <-->|WS Port 8889| WS_HUB
```
---

## 💡 コア機能の概要

### 1. 🔊 リアルタイム 3D 空間オーディオ
* **ダイナミック ステレオ パンニング:** リモート スピーカーの座標をリスナーの前方および右方向ベクトルに投影し、定電力式を使用して正確な左右のパンニングを計算します。
* **前後曖昧さの解決:** スピーカーがリスナーの後ろに立っている場合、オーディオの音量を 25% 減衰させ、標準の 2D オーディオのパン制限を解決します。
* **距離ロールオフ:** 距離に基づいて近接音声を直線的にフェードアウトし、自然な音量レベルを確保します (50 メートルで完全にゼロにフェードアウト、ささやき声の場合は 5 メートル)。

### 2. 🗺️ 位置認識音響と船舶/バンカーオクルージョン
* **デッキと壁のオクルージョン:** 空間内の内部境界を検出します。プレイヤーが異なるデッキ (例: キャラック、ヘラクレス) または部屋 (例: バンカー) にいる場合、ローパス フィルター (カットオフ周波数 300 Hz から 900 Hz) とボリューム ダンピングが動的に適用されます。
* **環境リバーブ:** プレーヤーの階層ゾーンを読み取り、**洞窟**、**バンカー**、**格納庫**のカスタム ウェット ミックス、ディレイ、フィードバック リバーブ パラメーターを自動的に適用します。

### 3. 💨 ヘルメットとEVAの大気シミュレーション
* **EVA ミューティング:** 宇宙ゾーンまたは真空ゾーン (EVA) での近接音声通信を自動的にミュートし、プレイヤーは通信に無線チャネルを使用する必要があります。
* **バイザーマスクオーバーレイ:** バイザーが下がっているときの気圧をシミュレートします。低周波の呼吸音とデュアル周波数 (50Hz + 100Hz) スーツの換気扇のハム音を、キャプチャされたマイク フィードに合成します。
* **自動バイザー同期:** 「Game.log」内の取り付けログを読み取り、ヘルメットの装着/取り外しを自動的に検出し、バイザーの状態をリアルタイムで更新します。

### 4. 🎙️ SF ボイスチェンジャーとスーツモジュレーター
* **リアルタイム DSP フィルター:** 時間領域ピッチシフト、フランジング、リングモジュレーション、ソフトタンハサチュレーション、および 8 ビットビットクラッシング。
* **雰囲気プリセット:** **エイリアン**、**サイボーグ**、**ロボット**、**カスタム ピッチ シフト** (0.5x ～ 2.0x) などのプリセット音声プロファイルを即座にロードします。

### 5. 📻 没入型ラジオの劣化とチャイム
* **バンドパス フィルタリング:** 無線チャネルを使用しているとき、またはスーツのバイザーが下がっているときに、低/高カットオフを備えた無線フィルタをモデル化します。
* **無線信号の劣化:** プレーヤー間の距離が無線送信機の制限に近づくと、カットオフ帯域が狭くなり、バンドパス フィルター処理された静的ノイズが混入します。
* **アコースティックラジオチャイム:** 無線チャンネルで送信する際に、機械的なキーダウンおよびキーアップのチャイムを再生します。設定またはコンパニオンアプリで選択可能な4つの異なる数学的プロファイルをサポート：ミリタリー（サイン波スイープ）、インダストリアル（機械的な金属音）、エイリアン（リング変調スイープ）、およびヴィンテージ（アナログリレイのクリック音）。

### 6. 💬自動船舶インターホンシステム
* **車両インターコム チャネル:** 車両に乗車すると、プレーヤーは自動的に動的な `Intercom_<ContainerID>` 無線チャンネルに登録されます。
* **パイロット優先ダッキング:** コックピットまたは運転席にいるプレイヤーがインターコムで送信すると、飛行コマンドの明瞭さを確保するために、他のすべてのプレイヤーの近接音声が 85% ダッキングされます。
* **インターホンの動的劣化:** 船のステータスに応じてインターホンチャンネルが自動的に劣化します。
  * **シールド被弾 (Shield Hits):** 一時的にノイズと音量のクラックルを注入します（2.5秒間持続）。
  * **致命的な電力不足 (Critical Power):** 低電圧のACハム音、ソフトクリッピング歪み、および再サンプリングによるピッチ低下。
  * **量子トラベル (Quantum Travel):** コームフィルター（フランジャー/フェイザー）スイープと高周波のうなり音。
  * *すべてのサブエフェクトは一般設定で個別にオン/オフでき、デフォルトでは無効になっています。*
* **コックピット環境アラームのインジェクション:** アラームインジェクションが有効で、船が警告状態（シールド被弾または致命的な電力不足）に入ると、クライアントは自動的に低振幅（<0.01）の警告アラーム（スイープする緊急サイレンループまたは高速ダブルビープ警告ループ）を合成し、送信マイクストリームに直接ミックスして、リアルなコックピット緊急フィードバックを提供します。
* **クリーンアップ クールダウン:** 最後のプレイヤーが船を離れてから 5 分間カウントダウンしてからインターコム チャネルを削除し、サーバーのパフォーマンスを最大化します。

### 7. 📡 Vulkan 互換の HUD オーバーレイと 2D 戦術レーダー
* **Win32 クリックスルー オーバーレイ:** VoIP 接続、周波数、通話状態を表示するボーダーレス HUD オーバーレイ。 Vulkan および DirectX と互換性があります (ボーダーレス ウィンドウ モードで実行)。
* **インタラクティブHUDカスタマイザー:** 設定またはコンパニオンアプリを介して、HUDのテーマ（Aegis、Anvil、Drake、RSI、Origin）、表示位置（画面の四隅または中央）、および各コンポーネント（ミニレーダー、アクティブスピーカーリスト、接続チャンネルヘッダー）の表示/非表示をリアルタイムでカスタマイズできます。
* **インターホン状態インジケーター:** インターホンの劣化がアクティブな場合、HUDオーバーレイに `⚡ INTERCOM: DEGRADED`（詳細なサブ状態 `[Power Loss]`、`[Quantum]`、または `[Static Pop]`）などの警告を表示します。
* **戦術ミニレーダー:** 進行方向に合わせた 2D HUD レーダーが特徴で、相対的に話しているプレーヤーを表示し、プレーヤーの周囲に脈動音のリングを描きます。
* **Speech-to-Text 字幕:** オフラインの軽量 Whisper モデル (`ggml-tiny.bin`) を使用して、受信ラジオ/近接音声をローカライズされた HUD 字幕に転写します。
* **ハンズフリー PTT 音声コマンド:** 専用の音声コマンドキーを押し続けている間、送信される近接/ラジオ音声が一時的にミュートされ、マイク音声がバッファリングされます。キーを離すと、音声が Whisper モデルによってローカルでテキスト化され、船の操作アクションが実行されます。
  * **対応コマンド:** バイザー/ヘルメットの切り替え、マイクミュート/解除（近接/ラジオ/プロファイル/すべて）、アクティブなラジオチャンネルの切り替え、ボイスチェンジャープロファイルの切り替え。
  * **多言語キーワードマッチング:** 8つの言語（英語、フランス語、ドイツ語、スペイン語、ポルトガル語、日本語、中国語）をサポート。
  * **信頼度しきい値フィルター:** 調整可能なスライダーにより、信頼度の低い誤認識や雑音を除外。
  * *デフォルトでは無効になっています。初めて有効にする際、オフライン Whisper 認識モデル（約140MB）が自動的にダウンロードされます（すでに存在する場合を除く）。*

### 8. 📱 コンパニオン アプリと REST API
* **ローカル HTTP Web サーバー:** 構成可能なポート (デフォルト: `8891`、デフォルトでは無効) でローカル ダッシュボードをホストします。
* **グラスモーフィック コントローラー:** 電話またはセカンダリ スクリーンから接続して、ミュート、チャンネル サイクル、ヘルメット、またはボイス チェンジャーを切り替えます。
* **REST API:** 外部統合用のエンドポイント `GET /api/status` および `POST /api/action` を公開します（インターホン状態の取得とシミュレーションの上書きを含みます）。

### 9. 🎛️ ストリームデッキプラグイン
* **ストリーム デッキ アクション パック:** マイクのミュート、オーディオのミュート、ヘルメット バイザー、無線周波数サイクルを制御する 8 つのアクションを公開します。
* **動的キー アイコン:** WebSocket は継続的にボタンのグラフィック (アクティブなシアン色とミュートされたオレンジ色) を更新して、現在のクライアントの状態を反映します。
* **ライブ周波数タイトル:** アクティブなラジオ チャンネル名を物理的なストリーム デッキ ボタンに直接表示します。

### 10. 🔌 Discord ボイスブリッジ
* **双方向オーディオ リレー:** Go サーバーのラジオ チャネルと Discord 音声チャネルの間の通信をリレーします。
* **ニックネームのマッピング:** Discord の音声をキャプチャし、SSRC ID をサーバーのニックネームにマッピングします。

### 11. 🛡️ セキュリティ、ログローテーション、管理キャンバスレーダー
* **毎日のログ ローテーション:** 最新の 5 つのログのみを保持する起動ログ アーカイバ。
* **管理者ダッシュボード:** ロックアウト セキュリティ、レート制限、およびインタラクティブな 2D HTML5 キャンバス ライブ レーダー マップを備えたリアルタイム Web 管理パネルにより、管理者はプレイヤーの軌跡をズーム、パン、トレースできます。

### 12. 🤢 G フォースと身体的運動による音声の歪み
* **トレモロとピッチシフト:** 高い重力加速度の下では、送信マイクオーディオはトレモロ LFO (4 ～ 10Hz、最大 40% の深さ) で動的に変調され、ピッチダウン (係数: 1.0 から 0.85 まで) され、物理的な緊張、ブラックアウト、またはリアウト状態をシミュレートします。
* **激しい呼吸オーバーレイ:** ランダム化された喘ぎ/呼吸ノイズを自動的にオーバーレイし、「Game.log」からリアルタイムで解析されたプレイヤーのスタミナ レベルに基づいて呼吸サイクル速度を調整します。
* **手動/API コントロール:** ロールプレイまたは模擬テスト用に、クライアント設定およびコンパニオン アプリの Web UI スライダーを介して切り替えることができます。

### 13. 📡 戦術無線リレーおよびマルチホップリピータービーコン
* **マルチホップ信号ルーティング:** プレーヤーは「ビーコン モード」を切り替えて、無線リピーター ビーコンとして機能できます。 2 人のプレーヤーが直接の無線範囲外 (1500 m を超える) にある場合、受信側クライアントはゾーン内のすべてのアクティブなリピーターに対してダイクストラの最短パス アルゴリズムを実行します。
* **最悪ホップの品質低下:** マルチホップ パスが 8000 メートルのシングルホップ制限未満に存在する場合、システムは通信をルーティングし、総直線距離ではなく最悪ホップの劣化係数 (信号品質) を適用し、長距離の惑星/軌道無線ネットワークを可能にします。
* **動的 WebSocket 状態:** アクティブなリピーターの状態は、サーバーの WebSocket 制御チャネルを介してリアルタイムで同期されます。

### 14. 📢 船舶のパブリック アドレス (PA) ブロードキャスト システム
* **船全体の音声ブロードキャスト:** 複数乗組員の船のパイロット or 船長は、同じゾーン内で同じ「ContainerID」(船)を共有するすべての乗組員に音声アナウンスをブロードキャストできます。
* **PA DSP と Klaxon チャイム:** PA 送信は、ローカル近接およびラジオのミュート (マスター ボリューム/ミュートを除く) をバイパスし、モノラル センター パンを再生し、SF デュアルトーン チャイム/クラクション アラートを先頭に追加し、中空船の内部音響をシミュレートするメガホン バンドパスと残響フィルターを適用します。

### 15. 🔌 外部ハードウェアテレメトリ (Sim-Pit UDP Sync)
* **リアルタイムUDP同期:** クライアントは100msごとにVoIPとヘルメットの状態をJSON形式で`127.0.0.1:8895`に送信します。
* **ハードウェア統合:** コックピット製作者が通信状態に連動する物理LEDやインジケーターを統合できるようにします。

### 16. 🪐 惑星大気密度シミュレーション
* **範囲スケーリング:** 近接音声の減衰が惑星や衛星の大気密度に応じてスケーリングされます（例：Cellinでは3.5倍早く減衰）。
* **大気マッフル:** 大気が薄い屋外環境では、デジタルローパスフィルターを適用して音声をくぐもらせます（与圧された船内等では自動バイパス）。

### 17. 🎙️ ポストオプボイスレコーダー＆AARポータル
* **低オーバーヘッドOgg/Opusコンテナ:** サーバーのエンコード負荷ゼロで、生のOpusパケットを直接ブラウザ再生可能な`.ogg`ファイルに保存します。
* **インタラクティブタイムライン:** 管理者が管理ポータルでミッションの音声ログを視覚化、再生、削除できるようにします。

### 18. 📞 艦船間ヘイリング＆コールシステム
* **コックピット間通話:** 5,000mの範囲制限内で艦船同士のプライベートな通話ループを確立します。
* **ハンズフリー通話:** 通話中は自動的にVAD音声送信が有効になり、標準のPTTキー操作をバイパスします。
* **リアルなチャイム:** NAudioを使用してダイヤル音、呼び出し音、接続・切断チャイムをリアルに合成します。

### 19. 🔤 バイザーHUDリアルタイム翻訳字幕
* **動的翻訳機能:** 7つの言語に対応した軍事・フライト用語辞书を使用して、受信した外国語の音声ストリームをリアルタイムで翻訳します。
* **HUD字幕プレフィックス:** 翻訳されたテキストを`[送信元 -> 送信先]`プレフィックス付きでバイザーHUDに直接表示します。
* **オンデマンドWhisperローダー:** 有効化時にモデルが存在しない場合、警告を表示し、バックグラウンドでWhisperモデル（~75MB）を自動ダウンロードします。

### 20. 🎧 バイノーラル HRTF 空間オーディオ
* **物理耳シミュレーション:** ITD (両耳間時間差) と ILD (両耳間レベル差) ローパス減衰を使用して、人間の耳の形状と頭部シャドウ効果をシミュレートします。
* **ステレオ互換性:** サラウンドサウンド ハードウェアを必要とせず、標準のステレオ ヘッドフォンで高忠実度の 3D オーディオ キューを提供します。

### 21. 📊 バイザー HUD 3D スペクトログラム
* **FFT テレメトリ オーバーレイ:** 受信した話者の音声ストリームに対して、リアルタイムで Radix-2 64 ポイント高速フーリエ変換 (FFT) を計算します。
* **動的 HUD ビジュアライゼーション:** Vulkan/DX HUD 上のアクティブな話者の横に、オーディオ周波数を 8 つのスペクトル バンドにグループ化し、スムーズな減衰とともに表示します。

### 22. 🎙️ 音声起動式シップコントロール
* **音声コマンドからキーバインドへの変換:** 音声コマンド (「ドアを開けて」など) を聞き取り、8 つの言語のローカライズされた辞書と照合します。
* **直接のハードウェア キー入力:** 低レベルの Win32 `keybd_event` API 呼び出し (ゲームでの確実な入力登録のため、修飾キーを含めて 50 ミリ秒間キーを保持) を使用して、物理的なキープレスをシミュレートします。

### 23. 🛰️ サーバー側 AAR 3D 再生
* **座標ログ記録:** サーバーは、プレイヤーの座標とゾーンを 500 ミリ秒ごとに `<session_id>_positions.jsonl` ファイルに記録します。
* **同期された再生キャンバス:** 記録された Ogg/Opus オーディオと完全に同期した、Web ベース of HTML5 Canvas マップ上にプレイヤーの軌跡と発話パルス リングを視覚化します。

---

## 🎮 XuruVoip クライアント設定タブの内訳

WPF 設定ウィンドウは、次の 6 つの構成カテゴリで構成されています。
1. **全般**: 言語を構成し、`Game.log` ファイルを末尾に追加し、一般的なファイル ログを切り替えて、ローカル **コンパニオン アプリの HTTP サーバー** とポートを有効/構成します。
2. **接続**: ターゲット サーバー IP、位置およびオーディオ ポート、ユーザー名、ユーザー パスワード、およびサーバー パスワードを編集します。
3. **位置**: 位置ソース (「OCR スクリーン スキャナー」と「Game.log リーダー (GRTPR)」) を切り替え、モニター インデックス、トリミング領域、OCR 間隔を構成し、ライブ座標テキストをプレビューします。
4. **オーディオ**: 入出力ハードウェアの選択、dB ゲインの調整、送信モード (PTT 対 VAD) の選択、VAD しきい値の構成、**3D 空間オーディオの有効化**の切り替え、無線劣化の構成、合成ローカル チャイム、バイザー モジュレータ、**ボイス チェンジャー** プリセットの選択。
5. **ホットキー**: キーを近接 PTT、無線 PTT、プロファイル PTT、ヘルメット バイザー、無線チャネル サイクル、および個別のマイクとオーディオ チャネルのミュート スイッチにバインドします。
6. **オーバーレイ**: HUD オーバーレイを切り替え、コーナー配置を設定し、**戦術ミニレーダー** (最大範囲を設定可能) を有効にし、リアルタイム **Speech-to-Text キャプション**を切り替えます。

---

## 🖥️ XuruVoip サーバー (Go)

サーバーはプレーヤーの位置を調整し、安全な認証を処理し、空間距離と無線チャネルに基づいてオーディオ パケットを動的にルーティングします。

### 主な機能

* **サーバー側の近接制御**: 範囲内 (50 メートルのデフォルト、または 5 メートルのささやき) 内のプレーヤーにのみ近接オーディオを動的に中継します。
* **空間構成**: 座標をクライアントに送信するか距離のみを送信するかを決定する、切り替え可能なサーバー側オプション (`.env` の `XURUVOIP_SPATIAL_AUDIO`)。
* **マルチチャンネル ラジオ ルーティング**: プレーヤーは、アクティブなチャンネルで送信しながら複数のラジオ チャンネルを同時に聞くことができます。
* **オーディオ プロファイル システム**: オーディオ エフェクト (ラジオ フィルター、エコーなど) をプレーヤーに割り当てます。
* **SQLite Persistence**: サーバーの再起動後のプレーヤーのチャネル設定とプロファイル マッピングを保存します。
* **アンチバイパス セキュリティ**: ユーザー名、IP、およびハードウェア フィンガープリント (HWID/MachineGuid) によってトラブルメーカーを禁止し、禁止回避を防ぎます。
* **Web 管理ポータル**: リアルタイム ダッシュボード、ログ ストリーミング、チャネル/プロファイル構成、および禁止管理のための安全な Web インターフェイス (HTTPS/WebSocket)。
* **サーバー管理レーダー マップ**: 管理ダッシュボードに統合された 2D HTML5 キャンバス リアルタイム プレーヤー レーダー。クリック アンド ドラッグ パン、マウス ホイール ズーム、アクティブ ゾーン フィルタリング、履歴プレーヤーのウォーキング トレイル (ブレッドクラム)、および会話プレーヤーの周囲のライブ パルス同心音波リングをサポートします。
* **起動ログのローテーション**: 起動時にサーバー ログ (`xuruvoip.log`) をチェックします。ログ ファイルに前日のエントリが含まれている場合、ログ ファイルは「xuruvoip.YYYY-MM-DD.log」にローテーションされます。サーバーは、ディスクの過度の使用を防ぐために、ローテーションされた最新の 5 つのファイルのみを保持し、古いファイルは削除します。

### サーバー構成 (`.env`)

最初の起動時に、サーバーは次のデフォルト値を含む `.env` ファイルを自動的に生成します。```env
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
### 🎛️ Discord Voice Bridge セットアップガイド

ローカル Go サーバーの無線チャネルを Discord 音声チャネルにブリッジするには、次のセットアップ手順に従います。

1. **Discord ボット アプリケーションを作成します:**
   ※ [Discord Developer Portal](https://discord.com/developers/applications) にアクセスしてサインインしてください。
   * [**新しいアプリケーション**] をクリックし、名前を付けて (例: 「XuruVOIP Bridge」)、**[作成]** をクリックします。
   * 左側のサイドバーの **Bot** タブに移動し、**Reset Token** をクリックして、生成された **Bot Token** をコピーします。これをサーバーの `.env` ファイルに `XURUVOIP_DISCORD_TOKEN` として貼り付けます。
   * 同じボット ページの **Privileged Gateway Intents** で、**Message Content Intent** (特定のコマンドの読み取りに必要) を有効にします。

2. **ボットを Discord サーバーに招待します:**
   * [**OAuth2**] タブに移動し、**URL ジェネレーター** を選択します。
   * **スコープ** で、`bot` と `applications.commands` をチェックします。
   * [**ボットのアクセス許可**] で、次の権限を選択します。
     * *一般権限:* `チャンネルの表示`
     * *テキスト権限:* `メッセージを送信`
     * *音声権限:* `接続`、`話す`、`音声アクティビティを使用`
   * ページの下部にある生成された URL をコピーして Web ブラウザに貼り付け、ターゲットの Discord サーバー (ギルド) を選択して、**承認** をクリックします。

3. **サーバー (ギルド) と音声チャネル ID を取得します:**
   * Discord を開き、**ユーザー設定** -> **詳細** に移動し、**開発者モード** をオンに切り替えます。
   * サーバーリストで Discord サーバーアイコンを右クリックし、**サーバー ID をコピー** (これはギルド ID です) を選択します。これを「.env」に「XURUVOIP_DISCORD_GUILD_ID」として貼り付けます。
   * ボットを参加させたい対象の Discord 音声チャネルを右クリックし、**チャネル ID をコピー** を選択します。これを「.env」に「XURUVOIP_DISCORD_CHANNEL_ID」として貼り付けます。

4. **Map Go サーバーのラジオ チャネル:**
   * `XURUVOIP_DISCORD_BRIDGE_CHANNEL` をブリッジしたい無線チャンネルの正確な名前に設定します (例: `General`、`Bravo`、`Alpha` など)。この Go サーバーの無線周波数で送信される音声はすべて、Discord 音声チャンネルに双方向でブロードキャストされます。

### ソースからサーバーを構築する

#### Linux```bash
cd server


GOOS="linux" GOARCH="amd64" go build .
# a "server" linux binary will be created in the current directory
```
#### Windows```powershell
cd server 

$env:GOOS="windows"
$env:GOARCH="amd64"
go build .
# a "server.exe" windows binary will be created in the current directory
```
### サーバーの実行

#### ソースより:```bash
cd server
go run .
```
#### バイナリから:
##### Windows```powershell
.\server.exe
```
##### Linux```bash
./server
```
### 🖥️ ヘッドレスサーバーのセットアップと展開

永続的な運用準備が整ったヘッドレス インストールの場合、サーバーは、起動時に自動的に開始され、障害が発生した場合には再起動されるバックグラウンド システム デーモン/サービスとして実行する必要があります。

#### 1. ネットワークとファイアウォールの構成
`.env` ファイルで定義されている受信 TCP ポート (デフォルトは、位置/管理ポータルの場合は `8888`、空間オーディオの場合は `8889`) がホスト ファイアウォールで開いていることを確認します。
* **Linux (UFW):**  ```bash
  sudo ufw allow 8888/tcp
  sudo ufw allow 8889/tcp
  sudo ufw reload
  ```
* **Linux (ファイアウォール):**  ```bash
  sudo firewall-cmd --zone=public --add-port=8888/tcp --permanent
  sudo firewall-cmd --zone=public --add-port=8889/tcp --permanent
  sudo firewall-cmd --reload
  ```
---

#### 2. Linux デプロイメント (systemd)

Go サーバーを systemd サービスとしてデプロイするには、次の手順に従います。

##### ステップ A: ディレクトリと権限のセットアップ
セキュリティを分離するための専用のシステム ユーザーと作業ディレクトリを作成します。```bash
# Create a system user without login privileges
sudo useradd -r -s /bin/false xuruvoip

# Create installation directory and copy the binary
sudo mkdir -p /opt/xuruvoip
sudo cp xuruvoip-server-linux-x64 /opt/xuruvoip/xuruvoip-server
sudo chmod +x /opt/xuruvoip/xuruvoip-server

# Set ownership to the system user
sudo chown -R xuruvoip:xuruvoip /opt/xuruvoip
```
##### ステップ B: `.env` の生成と構成
システム ユーザーでサーバーを 1 回実行して、デフォルトの `.env` 設定ファイルとデータベースを生成します。```bash
sudo -u xuruvoip /opt/xuruvoip/xuruvoip-server -port 8888 -audio-port 8889
```
*コンソールが生成されたパスワードを出力した後、`Ctrl+C` を押します。* 次に、生成された `.env` ファイルを編集して設定 (パスワード、バインド IP、空間オーディオの切り替えなど) をカスタマイズします。```bash
sudo nano /opt/xuruvoip/.env
```
##### ステップ C: systemd サービス ファイルを作成する
サービス ファイルをリポジトリ `server/xuruvoip.service` から `/etc/systemd/system/xuruvoip-server.service` にコピーするか、次の内容で新しいサービス構成ファイル `/etc/systemd/system/xuruvoip-server.service` を作成します。```ini
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
##### ステップ D: サービスを有効にして開始する```bash
# Reload systemd daemon to pick up the new unit file
sudo systemctl daemon-reload

# Enable the service to run on startup
sudo systemctl enable xuruvoip-server

# Start the service immediately
sudo systemctl start xuruvoip-server
```
##### ステップ E: 監視とログ
サービスのステータスを確認し、ログをストリーミングするには:```bash
# Check status
sudo systemctl status xuruvoip-server

# Stream log files in real-time
journalctl -u xuruvoip-server -f -n 100
```
---

#### 3. Windows 導入 (NSSM)

サーバーをヘッドレス モードでネイティブ Windows サービスとして実行するには、**Non-Sucking Service Manager (NSSM)** を使用することをお勧めします。

##### ステップ A: ディレクトリのセットアップ
`xuruvoip-server-windows-x64.exe` を専用サーバー フォルダー (例: C:\XuruVoipServer`) に抽出/コピーします。

##### ステップ B: 構成の初期化
管理者として PowerShell ターミナルを開き、バイナリを 1 回実行してファイルを生成します。```powershell
cd C:\XuruVoipServer
.\xuruvoip-server-windows-x64.exe
```
*起動が完了したら、`Ctrl+C` を押します。* 必要に応じて、生成された `.env` ファイルをカスタマイズします。

##### ステップ C: NSSM 経由でサービスをインストールする
NSSM をダウンロードし、次のコマンドを実行してサービスをインストールします。```powershell
# Open NSSM GUI installer
.\nssm.exe install XuruVoipServer "C:\XuruVoipServer\xuruvoip-server-windows-x64.exe"
```
NSSM ポップアップで、以下を設定します。
* **パス:** `C:\XuruVoipServer\xuruvoip-server-windows-x64.exe`
* **起動ディレクトリ:** `C:\XuruVoipServer`
* [**サービスのインストール**] をクリックします。

##### ステップ D: サービスを開始する
PowerShell またはサービス マネージャー (`services.msc`) を使用してサービスを開始します。```powershell
Start-Service -Name XuruVoipServer
```
---

### クライアントの構築と実行

#### 要件
- Windows 10/11
- .NET 9.0 SDK (WPF サポート)

#### コンパイルして実行:```powershell
cd client
dotnet run
```
### リリースパッケージのインストール

インストーラーと実行可能ファイルはデジタル署名されていないため、Windows SmartScreen によって最初はブロックされる場合があります。プロパティ メニューを使用して簡単にブロックを解除できます。

* **オプション A: Windows パッケージ マネージャー (winget) - (推奨)**
  1. ターミナル (PowerShell またはコマンド プロンプト) を開きます。
  2. 次のコマンドを実行してクライアントをインストールします。
     ```powershell
     winget install XuruDragon.XuruVOIPClient
     ```

* **オプション B: MSI インストーラー**
  1. [リリース ページ](https://github.com/XuruDragon/XuruVOIP/releases) から `XuruVoipClient-win-x64.msi` をダウンロードします。
  2. Windows SmartScreen がインストールをブロックしないようにするには、次の手順を実行します。
     - ダウンロードした「XuruVoipClient-win-x64.msi」ファイルを右クリックし、**プロパティ**を選択します。
     - [*全般*] タブのプロパティ ウィンドウで、下部にある [**ブロックを解除**] チェックボックスをオンにします。
     - [**適用**] をクリックし、[プロパティ] ウィンドウを閉じます。
  3. ファイルをダブルクリックしてインストーラーを実行し、プロンプトの指示に従います。
     *(注: 標準の Windows ユーザー アカウント制御の「不明な発行者」プロンプトが表示されます。続行するには、**はい** または **実行** をクリックしてください。)*

* **オプション C: ポータブル ZIP バージョン**
  1. [リリース ページ](https://github.com/XuruDragon/XuruVOIP/releases) から `XuruVoipClient-win-x64.zip` をダウンロードします。
  2. ZIP パッケージ内のファイルを任意のフォルダー (例: `C:\Games\XuruVoip`) に抽出します。
  3. 次に、抽出した「XuruVoipClient.exe」ファイルを右クリックし、**プロパティ**を選択します。
     - [*全般*] タブのプロパティ ウィンドウで、下部にある [**ブロックを解除**] チェックボックスをオンにします。
     - [**適用**] をクリックし、[プロパティ] ウィンドウを閉じます。
  4. 「XuruVoipClient.exe」をダブルクリックして、クライアントをインストールせずに直接実行します。

## 📱 コンパニオン アプリとストリーム デッキの統合

XuruVOIP には、組み込みの Companion App Web サービスと公式 Stream Deck プラグインが含まれており、セカンダリ デバイスまたは物理キーから直接音声アクションを監視およびトリガーできます。

### 1. コンパニオン アプリと戦術マップ MFD を有効にする
デフォルトでは、システム リソースを節約するために、コンパニオン アプリのローカル HTTP サーバーと戦術マップ モードは無効になっています。これらを有効にするには:
1. XuruVOIP クライアントを開き、**設定** アイコンをクリックします。
2. [**全般**] タブで、**コンパニオン HTTP サーバーを有効にする** チェックボックスをオンにします（デフォルトポート：`8891`）。
3. レーダー表示を有効にするには、ネストされた **戦術コパイロットマップ (MFD) を有効にする** チェックボックスをオンにします。
4. [**保存して閉じる**] をクリックして適用します。
5. ダッシュボードへのアクセス：PC、タブレット、またはスマートフォンのブラウザで `http://localhost:8891` を開きます。マップモードが有効になっている場合、新しい **🗺️ 戦術マップ** タブが利用可能になり、キャラクターのリアルタイムの位置、進行方向、同じゾーン内の乗組員の連絡先、およびアクティブな発言インジケーターを追跡する Canvas ベースの HUD レーダー画面が表示されます。

---

### 2. Stream Deck プラグインのインストール
リリース パッケージには、事前にパッケージ化された `.streamDeckPlugin` ファイルが含まれています。
1. [リリース ページ](https://github.com/XuruDragon/XuruVOIP/releases) から `com.xuru.voip.streamDeckPlugin` をダウンロードします。
2. ファイルをダブルクリックして、Elgato Stream Deck ソフトウェアに直接インストールします。 
   *(あるいは、`com.xuru.voip.sdPlugin` フォルダーを手動で抽出して `%appdata%\Elgato\StreamDeck\Plugins\` にコピーすることもできます)*
3. インストールすると、**XuruVOIP** という新しいアクション カテゴリが Stream Deck デスクトップ アプリの右側のリストに表示されます。

---

### 3. アクションの追加と構成
次の 19 のアクションのいずれかを Stream Deck キーにドラッグ アンド ドロップできます。
* 🎤 **近接ミュート**: 発信近接マイクのミュートを切り替えます。
* 📻 **ラジオ ミュート**: 送信ラジオ マイクのミュートを切り替えます。
* 👤 **プロファイル ミュート**: 送信プロファイル マイクのミュートを切り替えます。
* 🔊 **オーディオ近接ミュート**: 受信近接再生のミュートを切り替えます。
* 🔊 **オーディオラジオミュート**: 受信ラジオ再生のミュートを切り替えます。
* 🔊 **オーディオ プロファイルのミュート**: 受信プロファイル再生のミュートを切り替えます。
* 🪖 **ヘルメットの切り替え**: 宇宙服のヘルメットのバイザーを上下に切り替えます。
* 🔄 **サイクルラジオ**: 利用可能なラジオチャンネルを循環します。
* 📢 **PA Broadcast**: 船内放送システム (PA) に送信するための Push-to-Talk キー。
* 📡 **Beacon Mode**: ラジオ中継 / ビーコンモードを切り替えます。
* 🎙️ **Voice Command Macro**: バックグラウンドでシミュレートされるカスタム音声コマンド マクロを実行します (設定で構成可能)。
* 💬 **Intercom Status**: 宇宙船のインターホン状態 (`NORMAL`, `SHIELD HIT`, `CRIT PWR`, `QUANTUM`) を表示し、キーを押すとシミュレーション状態を循環します。
* 🗺️ **Location Telemetry**: 現在のシステムゾーンと座標データ $(X, Y, Z)$ をキー上に表示します。
* 📞 **Initiate Hail**: 最も近いプレイヤーへの艦船間コールを開始します。
* 📞 **Accept/Answer Hail**: 受信した艦船間コールに応答します。
* 📞 **Decline/End Hail**: 受信したコールを拒否するか、アクティブな通話を終了します。
* 🔤 **Toggle Translation**: HUDのリアルタイム翻訳字幕のオン/オフを切り替えます。
* 🎧 **Toggle HRTF**: リアルタイム HRTF 空間オーディオレンダリングのオン/オフを切り替えます。
* 📊 **Toggle Spectrogram**: リアルタイムバイザー HUD 3D スペクトログラムのオン/オフを切り替えます。

#### 構成 (プロパティ インスペクター):
キーにドラッグするアクションごとに、それをクリックし、下部にある **プロパティ インスペクター** パネルで構成します。
* **コンパニオン ポート**: WPF クライアント設定で構成されたポートと一致するように設定します (デフォルト: `8891`)。
* **Voice Command** (Voice Command Macro のみ): 実行するテキスト コマンドを入力します (例: `"close visor"`, `"open hangar"`)。
* **動的フィードバック**: アクションはアイコンと状態をリアルタイムで更新します。トグルはシアン/赤で表示され、Intercom Status は 4 つの状態を循環し、Location Telemetry は座標を表示します。
* **ライブ周波数表示**: **Cycle Radio** キーは、現在アクティブな周波数名を物理ボタン上にリアルタイムで直接動的に表示します。

---

## 👥 クレジット

**[@XuruDragon](https://github.com/XuruDragon)** が **Antigravity IDE** と共同で開発しました。