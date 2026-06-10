# XuruVoip (日本語)

<p align="center">
  <a href="https://github.com/XuruDragon/XuruVOIP/actions/workflows/tests.yml">
    <img src="https://github.com/XuruDragon/XuruVOIP/actions/workflows/tests.yml/badge.svg" alt="テストステータス" />
  </a>
  <a href="https://github.com/XuruDragon/XuruVOIP/releases">
    <img src="https://img.shields.io/github/v/release/XuruDragon/XuruVOIP?color=blue&logo=github" alt="最新リリース" />
  </a>
  <a href="https://github.com/XuruDragon/XuruVOIP/releases">
    <img src="https://img.shields.io/github/downloads/XuruDragon/XuruVOIP/total?color=green&logo=github" alt="総ダウンロード数" />
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
  <img src="../logo.png" alt="XuruVoip ロゴ" width="400" height="400" />
</p>

XuruVoipは、**Star Citizen**とのカスタム統合向けに特別に設計された、高性能、安全、かつ動的に空間化された**3D音声通信（VoIP）スイート**です。Goベースのバックエンドサーバーと、モダンなC# WPFクライアントで構成されています。

---

## 📸 スクリーンショットとUI

<details>
<summary>📸 クリックしてスクリーンショットを表示</summary>

### 1. メインクライアントウィンドウ
![メインクライアントウィンドウ](/screenshots/main.png)

### 2. オーディオ設定タブ（3D空間オーディオ制御）
![オーディオ設定タブ](/screenshots/audio.png)

### 3. 一般設定タブ（言語とGame.logの選択）
![一般設定タブ](/screenshots/general.png)

### 4. 接続設定タブ
![接続設定タブ](/screenshots/connection.png)

### 5. ホットキー設定タブ
![ホットキー設定タブ](/screenshots/hotkeys.png)

### 6. オーバーレイ設定タブ（Vulkan & DirectX HUD）
![オーバーレイ設定タブ](/screenshots/overlay.png)

### 7. 管理者Webポータル ログインページ
![管理者Webポータル ログインページ](/screenshots/admin_login.png)

### 8. 管�```mermaid
graph TD
    subgraph キャプチャと送信
        Mic[マイク入力] -->|PCMオーディオ| VAD[WebRTC音声活動検出]
        VAD -->|有効な音声| VoiceChanger[ボイスチェンジャー & スーツDSP]
        VoiceChanger -->|変更されたPCM| OpusEnc[Opusエンコーダー]
        OpusEnc -->|Opusパケット| AudioWS[音声WebSocketクライアント]
        AudioWS -->|WebSocket ポート 8889| Server[Goサーバー]
    end

    subgraph 位置検出 & ヘルメット検出
        SC[Star Citizenプロセス] -->|r_DisplaySessionInfo/r_DisplayInfo| Screen[スクリーンキャプチャ]
        Screen -->|前処理| Tess[Tesseract OCRエンジン]
        
        SC -->|リアルタイムログ| GameLog[Game.logファイル]
        GameLog -->|ログスキャナー| LogParser[ログサービスパーサー]
        
        Tess -->|解析された座標| PosSelector{位置ソース切り替え}
        LogParser -->|解析された座標| PosSelector
        
        PosSelector -->|選択された座標| Zone[階層型ゾーンフィルタ]
        Zone -->|リスナー座標とゾーン| PosWS[位置WebSocketクライアント]
        PosWS -->|WebSocket ポート 8888| Server

        LogParser -->|装備/脱着イベント| Helmet[ヘルメット状態同期]
        Helmet -->|ヘルメット状態パケット| PosWS
    end

    subgraph 再生 & 空間ミキシング
        Server -->|対象の近接音声 + メタデータ| AudioWS
        AudioWS -->|Opusフレーム + 近接メタデータ| Decoder[Opusデコーダ]
        Decoder -->|モノラル Float PCM| OcclusionFilter[デッキ & キャビン遮蔽フィルタ]
        OcclusionFilter -->|こもったPCM| DSP[無線DSPフィルタ & 劣化]
        DSP -->|モノラル| Panner[PanningSampleProvider]
        Panner -->|ステレオ| Volume[VolumeSampleProvider]
        
        LogParser -.->|ローカルヘルメット状態| DSP
        Zone -.->|リスナーの位置と向き| MixerMath[空間定位 & 劣化計算]
        
        MixerMath -->|定位パラメータ| Panner
        MixerMath -->|距離 & 後方減衰| Volume
        MixerMath -->|劣化因子| DSP
        
        Volume -->|左右ステレオ| Mixer[MixingSampleProvider]
        Mixer -->|音声再生| Speakers[オーディオ出力デバイス]
    end

    subgraph HUDオーバーレイ & STT
        Decoder -->|モノラル Float PCM| STT[Speech-to-Text Whisper.net]
        STT -->|文字起こしされたテキスト| Overlay[HUDオーバーレイウィンドウ]
        Zone -.->|リスナーの位置| Overlay
        AudioWS -.->|リモートスピーカー座標| Overlay
        Overlay -->|動的な字幕 & 2Dミニレーダー| ScreenOverlay[画面表示]
    end
```

### 1. 音声キャプチャ、VAD、および圧縮
* **音声キャプチャ：** **NAudio** APIを使用し、マイク音声を48,000 Hz、16ビットモノラルでキャプチャします。
* **音声活動検出 (VAD)：** キャプチャされた音声は、ネイティブの **WebRtcVad** ラッパーを使用して評価されます。音声の信頼度がしきい値を下回ると送信を停止し、キーボードの打鍵音やファンノイズなどの環境音をカットします。
* **圧縮：** 有効な音声バッファは、高度に圧縮された **Opus** フレームにエンコード（C# **Concentus** ラッパー経由）され、WebSocketを通じてサーバーに直接送信されます。

### 2. 位置追跡と向きの推定
* **位置ソースの切り替え：** プレイヤーはクライアント設定で2つの位置決定方法を選択できます：
  * **OCR画面スキャナー：** セッション座標（`/showlocations` または `r_DisplaySessionInfo` で出力される座標）が表示される設定された画面領域を定期的にキャプチャし、画像前処理を行ってから **Tesseract OCR** エンジンに送ります。
  * **Game.logリーダー (GRTPR)：** ゲームによって記録される座標を Star Citizen の `Game.log` ファイルから直接読み取ります。これを有効にするには、ゲームの `user.cfg` ファイルに `r_DisplaySessionInfo = 3`（または `1`）を追加する必要があります。GRTPR を選択すると、Tesseract OCR エンジンは完全に停止・解放され、ホストマシンの CPU と RAM リソースを大幅に節約します。t PCM| DSP[無線DSPフィルタ & 劣化]
        DSP -->|モノラル| Panner[PanningSampleProvider]
        Panner -->|ステレオ| Volume[VolumeSampleProvider]
        
        LogParser -.->|ローカルヘルメット状態| DSP
        Zone -.->|リスナーの位置と向き| MixerMath[空間定位 & 劣化計算]
        
        MixerMath -->|定位パラメータ| Panner
        MixerMath -->|距離 & 後方減衰| Volume
        MixerMath -->|劣化因子| DSP
        
        Volume -->|左右ステレオ| Mixer[MixingSampleProvider]
        Mixer -->|音声再生| Speakers[オーディオ出力デバイス]
    end
```

### 1. 音声キャプチャ、VAD、および圧縮
* **音声キャプチャ：** **NAudio** APIを使用し、マイク音声を48,000 Hz、16ビットモノラルでキャプチャします。
* **音声活動検出 (VAD)：** キャプチャされた音声は、ネイティブの **WebRtcVad** ラッパーを使用して評価されます。音声の信頼度がしきい値を下回ると送信を停止し、キーボードの打鍵音やファンノイズなどの環境音をカットします。
* **単一ファイル実行ファイルのネイティブ依存関係解決:** クライアント起動時にカスタムアセンブリ解決コールバックを使用し、`runtimes\win-x64\native` サブディレクトリからネイティブの `WebRtcVad.dll` を動的にロードすることで、単一ファイル（Single-File）として発行されたパッケージでのDLL読み込みエラーを解決します。
* **圧縮：** 有効な音声バッファは、高度に圧縮された **Opus** フレームにエンコード（C# **Concentus** ラッパー経由）され、WebSocketを通じてサーバーに直接送信されます。

### 2. 位置追跡と向きの推定
* **位置ソースの切り替え：** プレイヤーはクライアント設定で2つの位置決定方法を選択できます：
  * **OCR画面スキャナー：** セッション座標（`/showlocations` または `r_DisplaySessionInfo` で出力される座標）が表示される設定された画面領域を定期的にキャプチャし、画像前処理を行ってから **Tesseract OCR** エンジンに送ります。
  * **単一ファイル実行ファイルのネイティブ依存関係解決:** Tesseractのネイティブバイナリ（`tesseract50.dll` および `leptonica-1.82.0.dll`）を実行時に `x64` サブディレクトリからプログラム的に解決し、自己完結型の単一ファイル実行ファイルとしてデプロイされた場合でも安定したOCR動作を保証します。
  * **Game.logリーダー (GRTPR)：** ゲームによって記録される座標を Star Citizen の `Game.log` ファイルから直接読み取ります。これを有効にするには、ゲームの `user.cfg` ファイルに `r_DisplaySessionInfo = 3`（または `1`）を追加する必要があります。GRTPR を選択すると、Tesseract OCR エンジンは完全に停止・解放され、ホストマシンの CPU と RAM リソースを大幅に節約します。
* **階層型ゾーンフィルタ：** 座標には階層的なゾーン情報（惑星、宇宙船、エレベーター等）が含まれます。クライアントはこれらを判別し、隣接する部屋にいるプレイヤー同士が途切れることなくスムーズに会話できるように制御します。
* **向きの推定：** 前後の位置情報の変化（$Position_{現在} - Position_{過去}$）から移動ベクトルを算出し、向きを自動推定します。

### 3. リアルタイムヘルメット検出（ログスキャン）
* **Game.logスキャナー：** バックグラウンドタスクがStar Citizenの `Game.log` ファイルをリアルタイムに監視します。
* **装備追跡：** ヘルメットの装備ログ（`FP_Visor`、`helmethook_attach`）を検出すると、即座にヘルメットモード（ON/OFF）を同期します。

### 4. ステレオ3D空間ミキシングとDSP
* **受信処理：** 音声とともに発言者の座標、距離、最大範囲を受け取ります。
* **空間計算：** リスナーのベクトルに音源を投影します：
  * **ステレオパン：** 左右のバランスを `-1.0`（左極）から `+1.0`（右極）で調節します。
  * **背面減衰：** 後方からの音声に対して音量を最大25%減衰させ、前後の位置関係を明確にします。
  * **距離減衰：** 距離に応じて線形に減衰し、最大会話範囲（標準50m）でゼロになります。
* **再生＆無線DSP：** デコードされたOpusフレームは、必要に応じて無線DSPフィルタを通され、位置調整と音量調整後にミキシング再生されます。
  * **動的な無線信号 of 劣化：** 有効にすると、プレイヤー間の距離が最大通信範囲に近づくにつれて、DSPフィルタがハイパスおよびローパスのカットオフ周波数を自動的に絞り込み、バンドパスフィルタリングされたホワイトノイズをブレンドして、リアルな無線信号の劣化をシミュレートします。
  * **本格的なPTT＆無線チャイム音：** NAudioは送信開始・終了時の無線チャイム音を合成します。送信開始時には50msのピッチスイープ **マイクキークリック音**（900Hzから700Hz）を再生します。送信終了時には、キャプチャサービスから送信される最後の0バイトOpus空フレームを検出し、180msのバンドパスホワイトノイズ **スケルチテール音** をトリガーします。ローカルループバックオプションにより、プレイヤーは自分のマイク切り替え音をローカルで聞くことができます。

### 6. VulkanおよびDirectX対応の境界線なしHUDオーバーレイ
* **HUDオーバーレイウィンドウ** : クライアントは、最前面に描画されるオプション of 軽量な透明WPFオーバーレイウィンドウを提供します。VoIP接続ステータス、現在のチャンネル周波数、および無線信号インジケータ付きのリアルタイムアクティブスピーカーリストを表示します。
* **Win32クリックスルー統合** : Win32 APIウィンドウスタイル（`WS_EX_TRANSPARENT` および `WS_EX_NOACTIVATE`）を使用することで、オーバーレイはフォーカスを奪わず、マウスクリックをゲームに直接透過させます。
* **APIに依存しないレンダリング** : 標準の透明なWPFウィンドウはWindowsのDWM（Desktop Window Manager）コンポジションによって描画されるため、ゲームのグラフィックスパイプラインにフックしません。これにより、ゲームを **「境界線なしウィンドウモード（Borderless Windowed）」** で実行している限り、**Vulkan** および **DirectX** の両方で動作します。
* **📡 タクティカルHUDミニレーダー**: HUD上に円形のミニレーダーを描画し、プレイヤーの位置を表示します。
  * **ヘディングアップ整列**: レーダーはプレイヤーの移動方向（推定視線方向）に基づいて自動的に回転します。
  * **相対投影**: 近接範囲内で発言中のプレイヤーの座標をレーダー上に投影します。発言中のスピーカーの周囲には同心円状のパルスリングが表示されます。
  * **設定**: 設定画面でオン/オフを切り替え可能で、最大表示範囲を10mから200mの間で調整できます。
* **💬 リアルタイムHUD字幕 (Speech-to-Text)**: 音声通信をリアルタイムで自動的にテキスト化し、オーバーレイ上に字幕として表示します。
  * **オフライン文字起こし**: ローカルで実行される軽量なWhisperモデル (`ggml-tiny.bin`) を使用します (Whisper.net経由)。
  * **動的な言語適応**: 認識言語をユーザーが選択したクライアントUI表示言語に自動的に同期します。
  * **オンデマンドダウンロード**: 初回有効化時のみ、HuggingFaceから75MBのモデルをバックグラウンドでダウンロードします。ダウンロードの進捗はHUD上に表示されます。

### 7. 環境音響 (遮蔽 & 残響)
* **遮蔽 (オクルージョン) フィルター:** 送信者と受信者が異なるゾーンまたは部屋にいる場合、クライアントは自動的にローパスフィルター (カットオフ周波数 600Hz、音量 65%) を適用し、物理的な障害物による遮蔽をシミュレートします。カットオフ周波数はポップノイズを防ぐため滑らかに遷移します。
* **ゾーン対応の残響 (リバーブ):** 受信者が特定の環境 (洞窟、バンカー、または格納庫など) にいる場合、フィードバック遅延ラインコームフィルターがそれぞれに応じた残響パラメーターを適用します:
  * *洞窟 / トンネル:* ウェットミックス 45%、ディレイ 100ms、フィードバック 0.6。
  * *バンカー / ステーション / 地下施設:* ウェットミックス 25%、ディレイ 50ms、フィードバック 0.4。
  * *格納庫 (ハンガー):* ウェットミックス 35%、ディレイ 150ms、フィードバック 0.5。
* **🗺️ 船内コンパートメントおよびデッキ別の遮蔽**: 船内レイアウトや施設構造を認識し、物理的な隔壁やフロアに基づいて音声を減衰させます：
  * *Carrack デッキ*: Z座標の区切り（コマンドデッキ vs. ハビテーション vs. テクニカルデッキ）で強力なローパスフィルターを適用します（カットオフ350Hz、音量35%）。
  * *Carrack コンパートメント*: Y座標の区切り（コックピット vs. ハビテーション vs. エンジンルーム）で音声を減衰します（カットオフ900Hz、音量65%）。
  * *バンカー レベル*: Z座標の区切り（エレベーターロビー vs. 中間レベル vs. メインレベル）で音声を減衰します（カットオフ300Hz、音量30%）。
  * *バンカー 部屋*: X座標の区切り（カットオフ800Hz、音量60%）。
  * *Hercules デッキ*: Z座標の区切り（ハビテーション vs. カーゴホールド）で音声を減衰します（カットオフ400Hz、音量45%）。
  * *Cutlass コンパートメント*: Y座標の区切り（コックピット vs. カーゴホールド）で音声を減衰します（カットオフ1000Hz、音量70%）。
  * *全般的な高度ヘーリスティック*: 同一ゾーン内のプレイヤー間で4.5m以上の高低差がある場合、自動的に床/天井による遮蔽を適用します（カットオフ500Hz、音量45%）。

### 8. 外部依存なしの Discord Rich Presence (RPC)
* **堅牢な名前付きパイプ接続:** 重い外部の依存関係を必要とせずに、Discordと統合します。様々なDiscordの構成や複数起動しているインスタンスに対しても堅牢な接続を確保するため、`discord-ipc-0` から `discord-ipc-9` までのすべての名前付きパイプのインデックスをスキャンして接続を試行します。
* **動的なアクティビティ更新:** プレイヤーのステータスをリアルタイムで Discord に同期します:
  * **詳細:** ゲーム内の現在位置ゾーン (例: `"At MicroTech Cave"` など)。
  * **状態:** 接続チャネルとヘルメット状態 (例: `"On Radio: Bravo Channel (Helmet On)"` または `"In Proximity"`)。
  * **経過時間:** VoIP サーバーに接続してからの経過時間を表示します。

### 9. 起動時のログローテーション
* **日次ログローテーション:** クライアント起動時に、アクティブなログファイルの変更日を確認します。前日に変更されたものである場合、`xuru_voip.YYYY-MM-DD.log` としてアーカイブされます。
* **ログのクリーンアップと保持:** ディスク容量의消費を抑えるため、クライアントはログディレクトリをスキャンし、最新の5つのローテーションログファイルのみを保持し、それ以前の古いログファイルを削除します。

### 10. 🎙️ リアルタイムボイスチェンジャー＆スーツモジュレーター
* **ボイスモジュレーターDSP**: Opus圧縮を行う前に、送信マイク音声に対してリアルタイムにデジタル信号処理エフェクトを適用します：
  * **ピッチシフター**: クロスフェードする2本のディレイラインを使用して時間領域でピッチをリアルタイム変更します。
  * **リングモジュレーター**: 音声信号に搬送波を乗算し、金属的でロボット風のSFチックなトーンを生成します。
  * **フランジャー**: LFOで変調されたディレイラインを持つコームフィルターにより、うねるような宇宙風のスウィッシュ効果を作ります。
* **ボイスチェンジャー・プリセット**:
  * *エイリアン*: 深いピッチシフト (0.65x) にリングモジュレーター (85Hz) とフランジャーを組み合わせた重低音の宇宙人風音声。
  * *サイボーグ*: 金属的なシフト (0.82x)、リングモジュレーター (65Hz)、穏やかなtanh歪み、および8ビット相当への解像度低減 (ビットクラッシャー) を適用した音声。
  * *ロボット*: 高いピッチシフト (1.25x)、リングモジュレーター (140Hz)、およびフランジャーを適用した音声。
  * *カスタムピッチシフト*: 0.5xから2.0xの間でピッチ係数を手動調整できます。
* **ヘルメット/スーツモジュレーター**: 有効にすると、送信開始/終了時にリアルな呼吸音（シュー音）およびマイクチャイム音を重ねて送信します（呼吸音とチャイムはそれぞれオン/オフ可能です）。

### 11. 💨 ヘルメット＆EVA（宇宙空間）気圧シミュレーション
* **EVA/真空ミュート:** プレイヤーが宇宙空間や真空ゾーン（EVA）にいる場合、大気が存在しない状態をシミュレートするため、プロキシミティ（近接）音声通信は自動的に無効化（ミュート）されます。通信は無線チャンネルを介してのみ可能です。
* **バイザー呼吸音＆スーツ排気ノイズ:** ヘルメットのバイザーが装備され閉じられている状態のとき、リアルな呼吸音とスーツの排気ハム音（50Hz/100Hz発振器）がマイク入力にミックスされます。これはクライアント設定でオン／オフを切り替えることができます。

### 12. 💬 動的宇宙船インターコム＆パイロット優先ダッキング
* **自動インターコムチャンネル:** プレイヤーが船内に入ると、サーバーは専用のインターコムチャンネル（`Intercom_<ContainerID>`）を自動的に作成し、船内のすべてのプレイヤーを自動的にこのチャンネルに参加させます。
* **インターコムのクールドアウン削除:** 最後のプレイヤーが船を出た後、サーバーは5分間のカウントダウンを開始し、クールドアウン後にインターコムチャンネルを削除します。これにより、頻繁な出入りによるサーバー負荷を防ぎます。
* **パイロット優先ダッキング:** パイロットまたはドライバー席に座っているプレイヤーがインターコムチャンネルで話すとき、船内の他のすべてのプレイヤーの近接音声の音量が自動的に85%減衰（ダッキング）され、パイロットの指示がクリアに聞こえるようにします。

### 13. 📱 コンパニオンアプリ＆Webダッシュボード
* **ローカルHTTPサーバー:** クライアントはポート`8891`で軽量なWebサーバーを起動します（設定で有効化されている場合）。
* **ネオン調グラスモフィズムWeb UI:** ローカルネットワーク内の任意のデバイス（スマートフォンやタブレットなど）から `http://localhost:8891/` にアクセスして、洗練された光るネオンデザインのダッシュボードを表示できます。
* **APIによるコントロール:** リアルタイムのステータス取得（GET `/api/status`）や、ミュート切り替え、バイザーの開閉、アクティブチャンネルの変更、ボイスチェンジャープロファイルの切り替えを行う制御用エンドポイント（POST `/api/action`）を提供します（Stream Deck等と連携可能）。

### 14. 🎛️ Discordボイスブリッジ (Discord Voice Bridge)
* **双方向オーディオリレー:** 指定されたGoサーバーの無線チャンネルとDiscordのボイスチャンネル間で、音声をリアルタイムに双方向中継するサーバー側の機能です。
* **SSRCメンバーマッピング:** DiscordのユーザーIDを自動的にゲームサーバー内のニックネームにマッピングし、受信したDiscord音声を `「<ニックネーム> (Discord)」` という表示で再生します。

---

## 🖥️ XuruVoip サーバー (Go)

各プレイヤーの位置情報を統合し、距離や無線チャネルに基づいてオーディオパケットを動的に配信します。

### 主な機能
* **サーバーサイドの範囲制御**：会話範囲内にいるプレイヤーにのみ音声を中継します。
* **空間構成の切り替え**：`.env` 内の `XURUVOIP_SPATIAL_AUDIO` で、座標を渡すか、単に距離情報のみを渡すかを変更可能です。
* **マルチチャネル無線**：アクティブな無線チャネルで発言しつつ、複数のチャネルを同時に傍受できます。
* **オーディオプロファイル**：無線エフェクトやエコーなどの効果音をプレイヤーに適用します。
* **SQLiteデータベース**：再起動後もチャンネル設定やプロファイルを保持します。
* **セキュリティ機能**：Username、IP、およびハードウェアフィンガープリント（HWID/MachineGuid）によるBAN処理に対応。
* **管理者ポータル**：リアルタイムダッシュボード、ログストリーム、BAN管理機能を備えたHTTPS/WebSockets対応のWeb管理画面。
* **管理者用レーダーマップ**：管理者ダッシュボードに統合された2D HTML5 Canvasリアルタイムプレイヤーレーダー。ドラッグによるスクロール、マウスホイールによるズーム、アクティブなゾーンフィルタ、移動履歴を示す歩行軌跡（ブレッドクラム）、および発言中のプレイヤーの周囲に広がる同心円状のパルス音波アニメーションに対応します。
* **起動時のログローテーション**: 起動時にサーバーログ（`xuruvoip.log`）を確認します。ログファイルに前日のエントリが含まれている場合、`xuruvoip.YYYY-MM-DD.log` にローテーションされます。ディスクの過度な使用を防ぐため、サーバーは最新の5つのローテーションファイルのみを保持し、古いファイルを削除します。

### サーバー構成 (`.env`)
初回起動時に自動生成されます：
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

# インターコム＆EVA設定 (1 = 有効, 0 = 無効)
XURUVOIP_ENABLE_INTERCOM=1
XURUVOIP_ENABLE_EVA_MUTING=1

# Discordボイスブリッジ設定 (1 = 有効, 0 = 無効)
XURUVOIP_ENABLE_DISCORD_BRIDGE=1
XURUVOIP_DISCORD_TOKEN=your_discord_bot_token
XURUVOIP_DISCORD_GUILD_ID=your_discord_guild_id
XURUVOIP_DISCORD_CHANNEL_ID=your_discord_channel_id
XURUVOIP_DISCORD_BRIDGE_CHANNEL=General
```

### 🎛️ Discordボイスブリッジ設定ガイド

ローカルのGoサーバー無線チャンネルをDiscordのボイスチャンネルにブリッジするには、以下の設定手順に従ってください。

1. **Discordボットアプリケーションの作成:**
   * [Discord Developer Portal](https://discord.com/developers/applications)にアクセスし、サインインします。
   * **New Application**をクリックし、名前（例: `XuruVOIP Bridge`）を入力して**Create**をクリックします。
   * 左メニューの**Bot**タブへ移動し、**Reset Token**をクリックして生成された**ボットトークン**をコピーします。これをサーバーの`.env`ファイルの`XURUVOIP_DISCORD_TOKEN`に貼り付けます。
   * 同ページの**Privileged Gateway Intents**の下にある**Message Content Intent**を有効化します（特定のコマンドの読み取りに必要）。

2. **ボットをDiscordサーバーに招待する:**
   * **OAuth2**タブに移動し、**URL Generator**を選択します。
   * **Scopes**の下にある`bot`と`applications.commands`をチェックします。
   * **Bot Permissions**の下で、以下の権限を選択します。
     * *全般権限:* `View Channels`
     * *テキスト権限:* `Send Messages`
     * *音声権限:* `Connect`, `Speak`, `Use Voice Activity`
   * ページ最下部に生成されたURLをコピーし、ウェブブラウザに貼り付けて対象のDiscordサーバーを選択し、**認証**をクリックします。

3. **サーバー（ギルド）およびボイスチャンネルのIDを取得する:**
   * Discordを開き、**ユーザー設定** -> **詳細設定**に移動し、**開発者モード**をオンにします。
   * サーバーリスト内のDiscordサーバーアイコンを右クリックし、**サーバーIDをコピー**（これがギルドIDになります）を選択し、`.env`の`XURUVOIP_DISCORD_GUILD_ID`に貼り付けます。
   * 接続先となるDiscordボイスチャンネルを右クリックし、**チャンネルIDをコピー**を選択し、`.env`の`XURUVOIP_DISCORD_CHANNEL_ID`に貼り付けます。

4. **Goサーバー無線チャンネルのマッピング:**
   * `XURUVOIP_DISCORD_BRIDGE_CHANNEL`にブリッジしたい無線チャンネル名（例: `General`、`Bravo`、`Alpha`など）を正確に入力します。このGoサーバー無線周波数で送信された音声は、双方向でDiscordボイスチャンネルにブロードキャストされます！

### ソースからのコンパイル

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

### サーバーの起動

#### ソースから起動：
```bash
cd server
go run .
```

#### バイナリから起動：
##### Windows
```powershell
.\server.exe
```

##### Linux
```bash
./server
```

### 🖥️ ヘッドレスサーバーのセットアップとデプロイ

恒久的な本番環境のセットアップでは、サーバーが自動的に起動し、クラッシュから復旧できるようにバックグラウンドのシステムサービス（デーモン）として動作させることをお勧めします。

#### 1. ネットワークとファイアウォールの設定
`.env` ファイルで設定された着信TCPポート（標準はポータル/座標用に `8888`、音声用に `8889`）をファイアウォールで開放してください：
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

#### 2. Linuxへのデプロイ (systemd)

Goサーバーを systemd サービスとして動作させるには、以下の手順に従います：

##### 手順A：ディレクトリと権限の設定
セキュリティを確保するため、専用のシステムユーザーと作業ディレクトリを作成します：
```bash
# ログイン権限のないシステムユーザーを作成
sudo useradd -r -s /bin/false xuruvoip

# インストールディレクトリを作成してバイナリをコピー
sudo mkdir -p /opt/xuruvoip
sudo cp xuruvoip-server-linux-x64 /opt/xuruvoip/xuruvoip-server
sudo chmod +x /opt/xuruvoip/xuruvoip-server

# 所有者をシステムユーザーに変更
sudo chown -R xuruvoip:xuruvoip /opt/xuruvoip
```

##### 手順B：`.env` 設定ファイルの生成
システムユーザーの権限でサーバーを一度実行し、初期設定ファイルを生成します：
```bash
sudo -u xuruvoip /opt/xuruvoip/xuruvoip-server -port 8888 -audio-port 8889
```
*コンソールにパスワードが表示されたら `Ctrl+C` で終了します。* その後、生成された `.env` ファイルを編集します：
```bash
sudo nano /opt/xuruvoip/.env
```

##### 手順C：systemd サービスファイルの作成
リポジトリ内の `server/xuruvoip.service` を `/etc/systemd/system/xuruvoip-server.service` にコピーするか、以下の内容で作成します：
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

##### 手順D：サービスの有効化と起動
```bash
sudo systemctl daemon-reload
sudo systemctl enable xuruvoip-server
sudo systemctl start xuruvoip-server
```

##### 手順E：ログと動作確認
```bash
# サービスの状態を確認
sudo systemctl status xuruvoip-server

# ログをリアルタイムで追跡
journalctl -u xuruvoip-server -f -n 100
```

---

#### 3. Windowsへのデプロイ (NSSM)

Windows上でバックグラウンドサービスとして恒久的に動作させるには、**NSSM (Non-Sucking Service Manager)** を使用することをお勧めします：

##### 手順A：フォルダの作成
`xuruvoip-server-windows-x64.exe` を任意のフォルダ（例: `C:\XuruVoipServer`）に配置します。

##### 手順B：初期設定
PowerShellを管理者として開き、バイナリを一度実行して設定ファイルを生成します。`Ctrl+C` で停止させ、必要に応じて `.env` を調整します。

##### 手順C：NSSMによるサービスのインストール
```powershell
.\nssm.exe install XuruVoipServer "C:\XuruVoipServer\xuruvoip-server-windows-x64.exe"
```
作業ディレクトリを `C:\XuruVoipServer` に設定し、サービスをインストールします。

##### 手順D：サービスの開始
```powershell
Start-Service -Name XuruVoipServer
```

---

## 🎮 クライアント設定タブの解説

設定ウィンドウは6つのタブで構成されています：
1. **General (全般)**：表示言語の選択、Star Citizenの `Game.log` パスの指定、クライアントログ出力のオン/オフ。
2. **Connection (接続)**：サーバーアドレス、音声/位置ポート、ユーザー名、パスワード、およびサーバーパスワード。
3. **Position (位置情報)**：位置ソースの切り替え（「OCR画面スキャナー」vs.「Game.logリーダー (GRTPR)」）、対象モニターの選択、キャプチャ間隔（ms）、スキャン領域の指定、文字認識結果プレビュー（GRTPRアクティブ時はOCRオプション非表示）。
4. **Audio (音声)**：入出力デバイスの選択、音量ゲイン、発話モード（PTT / VAD）、VAD感度設定、**3D空間オーディオ**の有効化、無線信号の劣化、PTTマイクチャイム音の設定、スーツモジュレーターの有効化、および**ボイスチェンジャー・プリセット**（Alien, Cyborg, Robotic, PitchShift）の選択・構成。
5. **Hotkeys (ホットキー)**：PTTホットキー、ヘルメット切り替え、無線チャネル変更、マイクミュート、受話ミュートキーの割り当て。
6. **Overlay (オーバーレイ)**：透明HUDオーバーレイの有効化、表示位置の指定、**タクティカルHUDミニレーダー**（表示範囲調整可能）の有効化、および**リアルタイム字幕（Speech-to-Text）**の有効化（モデルダウンロード警告あり）。

### クライアントのビルドと起動

#### 必要要件
- Windows 10 または Windows 11
- .NET 9.0 SDK (WPFサポート)

#### コンパイルと起動:
```powershell
cd client
dotnet run
```

### リリースパッケージのインストール

配布バイナリはデジタル署名されていないため、初回実行時にWindows SmartScreenが警告を表示する場合があります。その場合は、ファイルのプロパティからロックを解除できます。

* **オプションA：MSIインストーラー（推奨）**
  1. [リリース一覧](https://github.com/XuruDragon/XuruVOIP/releases)から `XuruVoipClient-win-x64.msi` をダウンロードします。
  2. ファイルを右クリックして **プロパティ** を開きます。
  3. *全般*タブの最下部にある **許可する** (または「ブロックの解除」) にチェックを入れ、**適用** をクリックします。
  4. ダブルクリックしてインストーラーを起動します。

* **オプションB：ポータブルZIP版**
  1. [リリース一覧](https://github.com/XuruDragon/XuruVOIP/releases)から `XuruVoipClient-win-x64.zip` をダウンロードします。
  2. ZIPパッケージ内のファイルを任意のフォルダ（例: `C:\Games\XuruVoip`）に展開します。
  3. 次に、展開された `XuruVoipClient.exe` ファイルを右クリックして **プロパティ** を選択します。
     - プロパティウィンドウの *全般* タブの下部にある **ブロックの解除** (または「許可する」) チェックボックスをオンにします。
     - **適用** をクリックし、プロパティウィンドウを閉じます。
  4. `XuruVoipClient.exe` をダブルクリックして、インストールせずに直接クライアントを実行します。

---

## 📱 CompanionアプリとStream Deckの統合

XuruVOIPにはローカルのCompanionアプリWebサービスと公式のStream Deckプラグインが組み込まれており、セカンダリデバイスや物理キーから音声アクションを直接監視およびトリガーすることができます。

### 1. Companionアプリの有効化
デフォルトでは、システムリソースを節約するためにCompanionアプリのローカルHTTPサーバーは無効になっています。有効化するには：
1. XuruVOIPクライアントを開き、**Settings**（設定）アイコンをクリックします。
2. **General**タブで、**Enable Companion HTTP Server**（Companion HTTPサーバーを有効化）チェックボックスにチェックを入れます。
3. **Companion Server Port**で、ポート番号をカスタマイズできます（デフォルト: `8891`）。
4. **Save & Close**（保存して閉じる）をクリックして適用します。ローカルでHTTPサーバーが起動します。PCやモバイルデバイスのブラウザで`http://localhost:8891`を開くと、Webコントローラーのダッシュボードにアクセスできます。

---

### 2. Stream Deckプラグインのインストール
リリースパッケージには、あらかじめパッケージ化された`.streamDeckPlugin`ファイルが含まれています。
1. [リリースージ](https://github.com/XuruDragon/XuruVOIP/releases)から`com.xuru.voip.streamDeckPlugin`をダウンロードします。
2. ファイルをダブルクリックして、Elgato Stream Deckソフトウェアに直接インストールします。
   *(または、`com.xuru.voip.sdPlugin`フォルダを手動で解凍し、`%appdata%\Elgato\StreamDeck\Plugins\`にコピーすることもできます)*
3. インストールされると、Stream Deckデスクトップアプリの右側のアクションリストに**XuruVOIP**という新しいアクションカテゴリが表示されます。

---

### 3. アクションの追加と設定
以下の8つのアクションをStream Deckのキーにドラッグ＆ドロップできます。
* 🎤 **Proximity Mute**: 送信近接マイクのミュートを切り替えます。
* 📻 **Radio Mute**: 送信無線マイクのミュートを切り替えます。
* 👤 **Profile Mute**: 送信プロファイルマイクのミュートを切り替えます。
* 🔊 **Audio Proximity Mute**: 受信近接再生のミュートを切り替えます。
* 🔊 **Audio Radio Mute**: 受信無線再生のミュートを切り替えます。
* 🔊 **Audio Profile Mute**: 受信プロファイル再生のミュートを切り替えます。
* 🪖 **Toggle Helmet**: 宇宙服ヘルメットのバイザーの開閉を切り替えます。
* 🔄 **Cycle Radio**: 利用可能な無線チャンネルを順番に切り替えます。

#### 設定（Property Inspector）:
キーに配置した各アクションについて、クリックして最下部の**Property Inspector**パネルでターゲットポートを設定します：
* **Companion Port**に、WPFクライアントの設定で構成したポートを指定します（デフォルト: `8891`）。
* **ダイナミックフィードバック:** ミュートのトグル（例: Proximity Mute）は、デバイス上のアイコンをリアルタイムに更新して、アクティブ状態（水色に光るアイコン）かミュート状態（オレンジ色の斜線付きアイコン）かを表示します。
* **ライブ周波数表示:** **Cycle Radio**キーは、現在アクティブな無線周波数名（例: `120.5`または`General`）をリアルタイムに物理キー上に動的に表示します！## 👥 クレジット

**[@XuruDragon](https://github.com/XuruDragon)** が **Antigravity IDE** と共同で開発しました。
