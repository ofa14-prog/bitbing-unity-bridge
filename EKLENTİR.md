# EKLENTİR.md — AI Game Dev Platform: Unity Editor Eklentisi Teknik Spesifikasyonu

> **Versiyon:** 0.1
> **Dağıtım Yöntemi:** Unity Package Manager (UPM)
> **Hedef Unity Sürümleri:** Unity 2022 LTS, Unity 2023 LTS, Unity 6 (6000.x)
> **Bağlı Döküman:** KONU.md
> **Mimari Kural:** Bu eklenti KONU.md §3.4'te tanımlanan Unity entegrasyon bridge'inin Unity tarafını oluşturur. Platform tarafı (Electron Main Process) KONU.md kurallarına tabidir. Bu iki dosya birbirinin otoritesidir — başka hiçbir dosyaya bağımlılık yoktur.

---

## 1. Eklentinin Amacı ve Kapsamı

Bu eklenti iki bağımsız ama birlikte çalışan sorumluluk üstlenir:

**1. Agent Bridge (Dış Kontrol):** KONU.md'deki `ahbab` agent'ının Unity Editor'ü programatik olarak kontrol etmesini sağlar. Agent; sahne oluşturabilir, GameObject ekleyip kaldırabilir, C# script yazıp derleyebilir, Play Mode'u başlatıp durdurabilir — tüm bunları Electron Main Process'ten gelen komutlarla otomatik olarak gerçekleştirir.

**2. Editor UI (Kullanıcı Arayüzü):** Unity Editor içinde açılan bir panel aracılığıyla kullanıcı platform ile doğrudan etkileşime girebilir: pipeline durumunu izleyebilir, agent loglarını görebilir, komut gönderebilir.

---

## 2. Paket Kimliği ve Metadata

```json
{
  "name": "com.aigamedev.unity-bridge",
  "displayName": "AI GameDev Platform — Unity Bridge",
  "version": "0.1.0",
  "unity": "2022.3",
  "unityRelease": "0f1",
  "description": "Connects the AI GameDev Platform agent pipeline to Unity Editor. Provides programmatic editor control for the ahbab agent and an in-editor UI panel for monitoring and interaction.",
  "author": {
    "name": "AI GameDev Platform",
    "email": "dev@aigamedev.local"
  },
  "license": "MIT",
  "keywords": ["ai", "gamedev", "automation", "agent", "editor"],
  "dependencies": {
    "com.unity.nuget.newtonsoft-json": "3.2.1"
  }
}
```

> `unity: "2022.3"` — Unity 2022 LTS, 2023 LTS ve Unity 6 bu versiyonu okur ve uyumlu kabul eder.

---

## 3. UPM Kurulum Yöntemleri

### 3.1 Git URL ile Kurulum (Önerilen)

Unity Editor'de: **Window → Package Manager → + → Add package from git URL**

```
https://github.com/{org}/aigamedev-unity-bridge.git#main
```

Belirli bir sürümü kilitlemek için:
```
https://github.com/{org}/aigamedev-unity-bridge.git#v0.1.0
```

### 3.2 manifest.json ile Kurulum

Projenin `Packages/manifest.json` dosyasına eklenir:

```json
{
  "dependencies": {
    "com.aigamedev.unity-bridge": "https://github.com/{org}/aigamedev-unity-bridge.git#main"
  }
}
```

### 3.3 Lokal Geliştirme Kurulumu

```json
{
  "dependencies": {
    "com.aigamedev.unity-bridge": "file:/absolute/path/to/aigamedev-unity-bridge"
  }
}
```

### 3.4 Otomatik Gelen Bağımlılıklar

| Paket | Sürüm | Amaç |
|-------|-------|------|
| `com.unity.nuget.newtonsoft-json` | 3.2.1 | JSON serileştirme (bridge mesajları) |

---

## 4. Paket Dosya Yapısı

```
com.aigamedev.unity-bridge/
├── package.json
├── README.md
├── CHANGELOG.md
├── LICENSE.md
│
├── Editor/
│   ├── com.aigamedev.unity-bridge.editor.asmdef
│   │
│   ├── Bridge/
│   │   ├── AgentBridgeServer.cs         # TCP / Named Pipe sunucu
│   │   ├── BridgeMessageHandler.cs      # Gelen komutları ayrıştırır ve yönlendirir
│   │   ├── BridgeMessage.cs             # Mesaj veri modeli (JSON şeması)
│   │   └── BridgeConnectionState.cs     # Bağlantı durum makinesi
│   │
│   ├── Commands/
│   │   ├── IAgentCommand.cs
│   │   ├── CreateGameObjectCommand.cs
│   │   ├── DeleteGameObjectCommand.cs
│   │   ├── AddComponentCommand.cs
│   │   ├── WriteScriptCommand.cs        # Dosya yaz + AssetDatabase.Refresh()
│   │   ├── CompileScriptsCommand.cs
│   │   ├── CreateSceneCommand.cs
│   │   ├── OpenSceneCommand.cs
│   │   ├── EnterPlayModeCommand.cs
│   │   ├── ExitPlayModeCommand.cs
│   │   ├── TakeScreenshotCommand.cs
│   │   └── RunTestsCommand.cs
│   │
│   ├── UI/
│   │   ├── AgentPanelWindow.cs          # Ana EditorWindow
│   │   ├── AgentPanelWindow.uxml        # UI Toolkit layout
│   │   ├── AgentPanelWindow.uss         # UI Toolkit stil
│   │   ├── AgentStatusCard.cs
│   │   ├── LogStreamView.cs
│   │   └── ConnectionSettingsView.cs
│   │
│   └── Settings/
│       ├── BridgeSettings.cs            # ScriptableSingleton — kalıcı ayarlar
│       └── BridgeSettingsProvider.cs    # Project Settings entegrasyonu
│
├── Runtime/
│   ├── com.aigamedev.unity-bridge.runtime.asmdef
│   └── AgentRuntimeBridge.cs
│
└── Tests/
    ├── Editor/
    │   ├── com.aigamedev.unity-bridge.tests.editor.asmdef
    │   ├── AgentBridgeServerTests.cs
    │   ├── BridgeMessageHandlerTests.cs
    │   └── CommandTests.cs
    └── Runtime/
        ├── com.aigamedev.unity-bridge.tests.runtime.asmdef
        └── RuntimeBridgeTests.cs
```

---

## 5. Assembly Definition Dosyaları

### 5.1 Editor Assembly

```json
{
  "name": "AIGameDev.UnityBridge.Editor",
  "rootNamespace": "AIGameDev.UnityBridge.Editor",
  "references": [
    "GUID:d8b63aba3a73b4e6d9a2c5f01234567"
  ],
  "includePlatforms": ["Editor"],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": false,
  "precompiledReferences": [
    "Newtonsoft.Json.dll"
  ],
  "autoReferenced": false,
  "versionDefines": [
    {
      "name": "com.unity.nuget.newtonsoft-json",
      "expression": "3.2.1",
      "define": "AIGAMEDEV_NEWTONSOFT"
    }
  ]
}
```

### 5.2 Runtime Assembly

```json
{
  "name": "AIGameDev.UnityBridge.Runtime",
  "rootNamespace": "AIGameDev.UnityBridge.Runtime",
  "references": [],
  "includePlatforms": [],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "autoReferenced": true
}
```

---

## 6. İletişim Protokolü (Agent Bridge)

### 6.1 Taşıma Katmanı

KONU.md §3.4'te tanımlanan bridge istemcisinin karşı ucudur. İki taşıma modu desteklenir:

| Mod | Protokol | Port / Yol | Açıklama |
|-----|----------|-----------|---------|
| TCP Socket (birincil) | localhost TCP | **57432** | Platform varsayılan bağlantı noktası |
| Named Pipe (opsiyonel) | Windows Named Pipe | `\\.\pipe\aigamedev-unity` | Daha düşük gecikme |

Aktif mod `BridgeSettings` ScriptableObject'ten okunur.

### 6.2 Mesaj Formatı

Tüm mesajlar UTF-8 JSON, `\n` ile sonlandırılır (line-delimited JSON):

```json
{
  "messageId": "uuid-v4",
  "type": "command | response | event | ping",
  "agentId": "ahbab | obsidere | patientia | vates | diafor | magnumpus",
  "timestamp": "2025-01-01T00:00:00.000Z",
  "payload": { }
}
```

`agentId` alanı KONU.md §4.2'deki agent listesiyle birebir eşleşir.

### 6.3 Komut Tipleri ve Payload Şemaları

#### `create_gameobject`
```json
{
  "type": "command",
  "payload": {
    "command": "create_gameobject",
    "name": "Player",
    "parentPath": "/Scene/Characters",
    "position": { "x": 0, "y": 0, "z": 0 },
    "components": ["Rigidbody", "BoxCollider"]
  }
}
```

#### `write_script`
```json
{
  "type": "command",
  "payload": {
    "command": "write_script",
    "path": "Assets/Scripts/Player/PlayerController.cs",
    "content": "using UnityEngine;\n\npublic class PlayerController : MonoBehaviour { }",
    "refreshAssets": true,
    "waitForCompile": true
  }
}
```

#### `enter_play_mode`
```json
{
  "type": "command",
  "payload": {
    "command": "enter_play_mode",
    "waitForLoad": true,
    "timeoutSeconds": 30
  }
}
```

#### `take_screenshot`
```json
{
  "type": "command",
  "payload": {
    "command": "take_screenshot",
    "outputPath": "Temp/AgentScreenshots/frame_001.png",
    "width": 1920,
    "height": 1080,
    "includeUI": true
  }
}
```

#### Response Formatı
```json
{
  "type": "response",
  "messageId": "orijinal-mesaj-id",
  "success": true,
  "data": { },
  "error": null
}
```

#### Hata Response Formatı
```json
{
  "type": "response",
  "messageId": "orijinal-mesaj-id",
  "success": false,
  "data": null,
  "error": {
    "code": "COMPILE_ERROR | NOT_FOUND | TIMEOUT | INVALID_PATH",
    "message": "Hata açıklaması",
    "details": { }
  }
}
```

### 6.4 Olay (Event) Akışı

Unity'den platforma gönderilen olaylar — KONU.md §3.4 olay tablosuyla birebir eşleşir:

| Olay | Tetikleyici | Payload |
|------|------------|---------|
| `compile_started` | Derleme başladı | `{ "scriptCount": 12 }` |
| `compile_finished` | Derleme bitti | `{ "success": true, "errors": [] }` |
| `play_mode_entered` | Play Mode başladı | `{ "sceneName": "Main" }` |
| `play_mode_exited` | Play Mode bitti | `{ "duration": 4.2 }` |
| `scene_changed` | Sahne değişti | `{ "sceneName": "Level1" }` |
| `asset_imported` | Asset import tamamlandı | `{ "paths": ["Assets/..."] }` |
| `log_message` | Console log | `{ "type": "Log\|Warning\|Error", "message": "..." }` |

---

## 7. Komut Implementasyon Detayları

### 7.1 `WriteScriptCommand.cs` — Kritik Akış

```csharp
// Temsili implementasyon — gerçek kod bu akışı izler
public class WriteScriptCommand : IAgentCommand
{
    public async Task<CommandResult> ExecuteAsync(BridgeMessage message)
    {
        var payload = message.Payload.ToObject<WriteScriptPayload>();

        // 1. Yol doğrulama — Assets/ dışına çıkma engellenir
        if (!IsPathSafe(payload.Path))
            return CommandResult.Failure("INVALID_PATH", "Path must be within Assets/");

        // 2. Dizin oluştur
        var dir = Path.GetDirectoryName(payload.Path);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        // 3. Dosyayı yaz
        await File.WriteAllTextAsync(payload.Path, payload.Content, Encoding.UTF8);

        // 4. AssetDatabase'i bilgilendir
        AssetDatabase.ImportAsset(payload.Path, ImportAssetOptions.ForceUpdate);

        // 5. Derleme bekle
        if (payload.WaitForCompile)
        {
            await WaitForCompilationAsync(timeoutSeconds: 60);
        }

        return CommandResult.Success();
    }
}
```

### 7.2 Yol Güvenlik Kuralları

- Tüm dosya yazma işlemleri `Assets/` diziniyle sınırlandırılır
- `../` traversal girişimleri reddedilir — `INVALID_PATH` hatası döner
- `Path.GetFullPath()` ile canonical yol hesaplanır, `Application.dataPath` ile karşılaştırılır
- Sistem dizinlerine (`Editor/`, `Packages/`, `ProjectSettings/`) yazma engellenir

---

## 8. Editor UI Paneli

### 8.1 Panel Açma

**Window → AI GameDev → Agent Panel** menüsünden açılır.

```csharp
[MenuItem("Window/AI GameDev/Agent Panel %#g")]
public static void ShowWindow()
{
    var window = GetWindow<AgentPanelWindow>("AI GameDev");
    window.minSize = new Vector2(300, 400);
    window.Show();
}
```

### 8.2 UI Toolkit Yerleşimi

Panel **Unity UI Toolkit** (UIElements) ile yazılır — IMGUI kullanılmaz.

Agent renk kodlaması KONU.md §4.2'deki agent tanımlarıyla birebir eşleşir:

| Agent | Renk | HEX |
|-------|------|-----|
| vates | Kırmızı | `#FF3B3B` |
| Diafor | Sarı | `#FFD600` |
| ahbab | Yeşil | `#00E676` |
| obsidere | Mavi | `#2979FF` |
| patientia | Mor | `#AA00FF` |
| magnumpus | Turuncu | `#FF6D00` |

```
┌────────────────────────────────────────────┐
│  AI GAMEDEV PLATFORM              [⚙] [?]  │
├────────────────────────────────────────────┤
│  BAĞLANTI                                  │
│  ● Bağlı — localhost:57432        [Kes]    │
├────────────────────────────────────────────┤
│  AGENT DURUMLARI                           │
│  ● vates    [RED]   Tamamlandı    ✓        │
│  ● Diafor   [YEL]   Tamamlandı    ✓        │
│  ⟳ ahbab    [GRN]   Çalışıyor...  ━━━━░    │
│  ◌ obsidere [BLU]   Bekliyor               │
│  ◌ patientia[PUR]   Bekliyor               │
│  ◌ magnumpus[ORN]   Bekliyor               │
├────────────────────────────────────────────┤
│  LOG AKIŞI                    [Temizle]    │
│  [GRN] ahbab: PlayerController.cs yazıldı │
│  [GRN] ahbab: Derleme başlatıldı           │
│  [GRN] ahbab: Derleme başarılı (2.3s)      │
│  [RED] vates: Task graph: 14 görev         │
│                                            │
├────────────────────────────────────────────┤
│  HIZLI KOMUTLAR                            │
│  [▶ Play Mode]  [⏹ Stop]  [📸 Ekran Al]   │
│  [🔄 Asset Refresh]  [🧹 Clear Console]    │
└────────────────────────────────────────────┘
```

### 8.3 UXML Yapısı

```xml
<!-- Editor/UI/AgentPanelWindow.uxml -->
<ui:UXML xmlns:ui="UnityEngine.UIElements">
  <ui:VisualElement class="root">

    <ui:VisualElement class="section connection-section">
      <ui:Label text="BAĞLANTI" class="section-title"/>
      <ui:VisualElement class="connection-row">
        <ui:VisualElement class="status-dot" name="connectionDot"/>
        <ui:Label name="connectionLabel" text="Bağlı değil"/>
        <ui:Button name="connectButton" text="Bağlan"/>
      </ui:VisualElement>
    </ui:VisualElement>

    <ui:VisualElement class="section agents-section">
      <ui:Label text="AGENT DURUMLARI" class="section-title"/>
      <ui:VisualElement name="agentCardsContainer"/>
    </ui:VisualElement>

    <ui:VisualElement class="section log-section">
      <ui:VisualElement class="section-header">
        <ui:Label text="LOG AKIŞI" class="section-title"/>
        <ui:Button name="clearLogsButton" text="Temizle"/>
      </ui:VisualElement>
      <ui:ScrollView name="logScrollView" class="log-view">
        <ui:VisualElement name="logContainer"/>
      </ui:ScrollView>
    </ui:VisualElement>

    <ui:VisualElement class="section commands-section">
      <ui:Label text="HIZLI KOMUTLAR" class="section-title"/>
      <ui:VisualElement class="command-buttons">
        <ui:Button name="playButton"         text="▶ Play Mode"/>
        <ui:Button name="stopButton"         text="⏹ Stop"/>
        <ui:Button name="screenshotButton"   text="📸 Ekran Al"/>
        <ui:Button name="refreshButton"      text="🔄 Refresh"/>
        <ui:Button name="clearConsoleButton" text="🧹 Clear Console"/>
      </ui:VisualElement>
    </ui:VisualElement>

  </ui:VisualElement>
</ui:UXML>
```

### 8.4 USS Stil

```css
/* Editor/UI/AgentPanelWindow.uss */
:root {
  --color-vates:     rgb(255, 59, 59);
  --color-diafor:    rgb(255, 214, 0);
  --color-ahbab:     rgb(0, 230, 118);
  --color-obsidere:  rgb(41, 121, 255);
  --color-patientia: rgb(170, 0, 255);
  --color-magnumpus: rgb(255, 109, 0);

  --bg-surface:   rgb(30, 30, 30);
  --bg-elevated:  rgb(40, 40, 40);
  --text-primary: rgb(220, 220, 220);
  --text-dim:     rgb(120, 120, 120);

  --font-size-sm: 11px;
  --font-size-md: 12px;
  --spacing-sm: 4px;
  --spacing-md: 8px;
  --spacing-lg: 12px;
}

.root {
  background-color: var(--bg-surface);
  padding: var(--spacing-md);
  flex-grow: 1;
}

.section {
  margin-bottom: var(--spacing-lg);
  border-width: 1px;
  border-color: rgba(255,255,255,0.08);
  border-radius: 4px;
  padding: var(--spacing-md);
}

.section-title {
  font-size: var(--font-size-sm);
  color: var(--text-dim);
  -unity-font-style: bold;
  margin-bottom: var(--spacing-sm);
}

.status-dot {
  width: 8px;
  height: 8px;
  border-radius: 4px;
  background-color: var(--text-dim);
}

.status-dot.connected { background-color: var(--color-ahbab); }
.status-dot.running   { background-color: var(--color-ahbab); }
.status-dot.error     { background-color: var(--color-vates); }
.status-dot.waiting   { background-color: var(--text-dim);    }

.log-view {
  max-height: 200px;
  background-color: var(--bg-elevated);
  border-radius: 3px;
}

.log-line {
  font-size: var(--font-size-sm);
  padding: 2px var(--spacing-sm);
  -unity-font-definition: resource("Fonts/RobotoMono");
}

.command-buttons {
  flex-direction: row;
  flex-wrap: wrap;
  justify-content: flex-start;
}

.command-buttons > .unity-button {
  margin: 2px;
  font-size: var(--font-size-sm);
}
```

---

## 9. Kalıcı Ayarlar — `BridgeSettings`

`BridgeSettings` bir `ScriptableSingleton`'dır. **Project Settings → AI GameDev** menüsünden düzenlenir, `ProjectSettings/AIGameDevBridgeSettings.asset` olarak kaydedilir (Git'e eklenir).

```csharp
[FilePath("ProjectSettings/AIGameDevBridgeSettings.asset",
          FilePathAttribute.Location.ProjectFolder)]
public class BridgeSettings : ScriptableSingleton<BridgeSettings>
{
    public TransportMode transportMode = TransportMode.Tcp;
    public int           tcpPort       = 57432;   // KONU.md §3.4 ile eşleşir
    public string        pipeName      = "aigamedev-unity";
    public bool          autoConnect   = true;
    public bool          logVerbose    = false;
    public int           timeoutMs     = 10000;
    public string        screenshotDir = "Temp/AgentScreenshots";

    public enum TransportMode { Tcp, NamedPipe }

    public void Save() => Save(true);
}
```

---

## 10. Unity Sürümü Uyumluluk Matrisi

| Özellik | Unity 2022 LTS | Unity 2023 LTS | Unity 6 (6000.x) |
|---------|:--------------:|:--------------:|:----------------:|
| UI Toolkit EditorWindow | ✅ | ✅ | ✅ |
| UPM Git URL kurulum | ✅ | ✅ | ✅ |
| `ScriptableSingleton` | ✅ | ✅ | ✅ |
| `EditorApplication.isCompiling` | ✅ | ✅ | ✅ |
| `CompilationPipeline` | ✅ | ✅ | ✅ |
| `EnterPlayModeOptions` | ✅ | ✅ | ✅ |
| Named Pipe (`System.IO.Pipes`) | ✅ | ✅ | ✅ |
| `Awaitable` (async Unity) | ❌ | ✅ | ✅ |

> `Awaitable` kullanılırken Unity 2022 için `#if UNITY_2023_1_OR_NEWER` define koruması eklenir; 2022'de Task tabanlı async kullanılır.

---

## 11. Define Sabitleri (Conditional Compilation)

```csharp
#if UNITY_2023_1_OR_NEWER
    await Awaitable.NextFrameAsync();
#else
    await Task.Yield();
#endif

#if UNITY_6000_0_OR_NEWER
    // Unity 6'ya özgü API
#endif
```

---

## 12. Güvenlik Kuralları

| Kural | Uygulama |
|-------|---------|
| Dosya yazma izni | Yalnızca `Assets/` — `Path.GetFullPath` + `Application.dataPath` kontrolü |
| `../` traversal | Reddedilir — `INVALID_PATH` hatası döner |
| Sistem dizinleri | `Editor/`, `Packages/`, `ProjectSettings/` yazma engellenir |
| Komut doğrulama | Tüm payload'lar §6.3 şemasına göre doğrulanır; bilinmeyen komutlar reddedilir |
| Bağlantı güvenliği | Yalnızca `127.0.0.1` kabul edilir — dış ağ erişimi engellenir |
| İzin listesi | Sadece tanımlı komut tipleri çalıştırılır — dinamik kod çalıştırma yoktur |

---

## 13. Test Yapısı

### 13.1 Editor Testleri (Unity Test Runner — EditMode)

| Test Sınıfı | Kapsam |
|-------------|--------|
| `AgentBridgeServerTests` | TCP bağlantısı kur/kes, ping/pong |
| `BridgeMessageHandlerTests` | JSON ayrıştırma, hata formatları |
| `CommandTests` | Her komutun unit testi (mock AssetDatabase) |
| `PathSafetyTests` | Traversal saldırı senaryoları |

### 13.2 Runtime Testleri (PlayMode)

| Test Sınıfı | Kapsam |
|-------------|--------|
| `PlayModeCommandTests` | `enter_play_mode` / `exit_play_mode` döngüsü |
| `ScreenshotCommandTests` | Ekran görüntüsü doğrulama |

### 13.3 Test Çalıştırma

```
Unity Editor → Window → General → Test Runner → EditMode / PlayMode → Run All
```

---

## 14. CHANGELOG Formatı

```markdown
# Changelog

## [Unreleased]

## [0.1.0] - 2025-01-01
### Added
- İlk UPM paket yapısı
- TCP socket agent bridge (port 57432)
- Named Pipe desteği
- EditorWindow UI Toolkit paneli
- Komut seti: create_gameobject, delete_gameobject, add_component,
  write_script, compile_scripts, create_scene, open_scene,
  enter_play_mode, exit_play_mode, take_screenshot, run_tests
- BridgeSettings Project Settings entegrasyonu
```

---

## 15. Proje Belgeleri

| Dosya | İçerik |
|-------|--------|
| `EKLENTİR.md` | Bu döküman — Unity eklentisi spesifikasyonu |
| `KONU.md` | Platform mimari ve teknoloji yığını — bağlayıcı otorite |

---

*Bu eklenti KONU.md §3.4'te tanımlanan Unity bridge'inin Unity tarafıdır. Port numaraları, mesaj formatı ve güvenlik kuralları bu iki dosya arasında tutarlıdır ve değiştirilemez.*
