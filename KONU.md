# KONU.md — AI-Driven Game Development Platform: Teknik Mimari & Teknoloji Yığını

> **Versiyon:** 0.3
> **Hedef Platform:** Windows 10 21H2 (Build 19044) ve üzeri — yalnızca x64
> **Mimari Referans Hiyerarşisi:**
> 1. **EKLENTİ.md** — Unity bridge iletişim protokolü, port tahsisi, güvenlik kuralları → bu dosya için bağlayıcı otorite
> 2. **WUS.md** — Electron uygulama kabuğu mimarisi → değiştirilemez temel
> 3. **KONU.md** — Bu dosya; yukarıdakilere aykırı hiçbir karar içermez

---

## 1. Projeye Genel Bakış

Bu platform, kullanıcıdan alınan doğal dil inputunu ayrıştırarak çok-ajanlı (multi-agent) bir orkestrasyon sistemi aracılığıyla gerçek bir oyun projesine dönüştüren **AI-native** bir oyun geliştirme ortamıdır.

Platform üç kipte çalışır:

| Kip | Açıklama |
|-----|---------|
| **Dahili Motor** | Babylon.js tabanlı yerleşik motor — kurulum gerektirmez |
| **Unity Modu** | `ahbab` agent'ı → TCP `localhost:57432` → EKLENTİ.md Unity bridge'i |
| **Unreal Modu** | `ahbab` agent'ı → TCP `localhost:57433` → UNREAL_EKLENTİ.md UE bridge'i |

Unity ve Unreal modlarında platformun render ettiği bir şey yoktur. Tüm görselleştirme ilgili editörün kendi penceresinde gerçekleşir. Platform yalnızca **komut gönderir** ve **olay alır.**

---

## 2. Temel Mimari

### 2.1 Electron Uygulama Kabuğu (WUS.md §3 — değiştirilemez)

```
┌──────────────────────────────────────────────────────┐
│                   Renderer Process                    │
│   React 18 · CSS Modules · Zustand 4.x               │
│   Monaco Editor · Babylon.js Canvas · ReactFlow       │
├──────────────────────────────────────────────────────┤
│                    Main Process                       │
│   Node.js 20 LTS · Electron 31.x                     │
│   Agent Runtime · Bridge İstemcileri · Dosya I/O     │
│   LLM API · ChromaDB · LSP Yönetimi                  │
├──────────────────────────────────────────────────────┤
│                IPC Köprüsü (Preload)                  │
│   contextBridge.exposeInMainWorld()                   │
└──────────────────────────────────────────────────────┘
          │ TCP localhost:57432          │ TCP localhost:57433
          ▼                             ▼
   Unity Editor                  Unreal Editor
   (EKLENTİ.md)                  (UNREAL_EKLENTİ.md)
```

**Değiştirilemez katman kuralları (WUS.md §3):**
- `nodeIntegration: false` — Renderer'da Node.js kapalı
- `contextIsolation: true` — Her pencerede aktif
- `sandbox: true` — Renderer sandbox modunda
- `webSecurity: true` — Kapatılamaz
- Main Process: Tüm ağ istekleri, dosya I/O, agent runtime, bridge istemcileri
- Renderer Process: Yalnızca UI ve kullanıcı etkileşimi
- Preload Script: Tek ve tek güvenli köprü

### 2.2 Zorunlu Platform Gereksinimleri

| Bileşen | Sürüm | Kaynak |
|---------|-------|--------|
| İşletim Sistemi | Windows 10 21H2+ x64 | WUS.md §1 |
| Node.js Runtime | 20 LTS x64 | WUS.md §1 |
| Electron | 31.x | WUS.md §1 |
| TypeScript | 5.5.x | WUS.md §4 |
| Visual Studio Build Tools | 2022 (17.x+) | WUS.md §2 |
| MSVC | v143 x64 | WUS.md §2 |
| Python | 3.12 x64 (node-gyp) | WUS.md §2 |
| Windows SDK | 10.0.22621.0+ | WUS.md §2 |
| Visual C++ Redistributable | 2015–2022 x64 | WUS.md §1 |
| GPU | DirectX 11+ / WebGPU | Babylon.js için |

### 2.3 Dil Kuralları (WUS.md §4 — değiştirilemez)

- Tüm kaynak dosyalar **TypeScript 5.5.x** — `.ts` veya `.tsx`, `.js` kullanılmaz
- `strict: true` — kapatılamaz
- `any` tipi kullanılmaz — `@typescript-eslint/no-explicit-any: error`
- Main Process: `CommonJS` | Renderer Process: `ESModule`
- Her katman için ayrı `tsconfig.json`

---

## 3. Teknoloji Yığını

### 3.1 UI Katmanı (Renderer Process — WUS.md §6)

| Bileşen | Teknoloji | Sürüm |
|---------|-----------|-------|
| UI Çerçevesi | React | 18.x |
| Stil | CSS Modules | — |
| Durum Yönetimi | Zustand | 4.x |
| Kod Editörü | Monaco Editor | 0.50.x |
| Monaco Sarmalayıcı | @monaco-editor/react | 4.x |
| LSP İstemcisi | monaco-languageclient | 8.x |
| LSP Transport | vscode-ws-jsonrpc | 3.x |
| Build Aracı | Vite 5.x + vite-plugin-electron | 0.28.x |
| Agent Pipeline Görselleştirme | ReactFlow | 11.x |

### 3.2 Dahili Oyun Motoru (Renderer Process)

Yalnızca **Dahili Motor** kipinde aktif olur. Unity/Unreal kiplerinde bu katman tamamen devre dışı kalır.

| Modül | Teknoloji | Sürüm |
|-------|-----------|-------|
| 3D/2D Render | Babylon.js | 7.x |
| Render Backend | WebGPU (birincil) / WebGL2 (fallback) | — |
| Fizik (3D) | @dimforge/rapier3d | 0.12.x |
| Fizik (2D) | @dimforge/rapier2d | 0.12.x |
| Ses | Howler.js | 2.x |
| Güvenli Scripting | isolated-vm | 4.x |
| Asset İzleme | chokidar | 3.x |
| Sahne Formatı | `.bscene` (JSON tabanlı) | — |

### 3.3 AI / LLM Altyapısı (Main Process)

> **Kural:** Tüm LLM API istekleri yalnızca Main Process `net` modülü üzerinden yapılır. Renderer doğrudan dış ağa erişemez.

| Bileşen | Teknoloji | Sürüm |
|---------|-----------|-------|
| Primer LLM | Anthropic Claude API (`claude-sonnet-4`) | ^0.26 SDK |
| Opsiyonel Lokal LLM | Ollama (llama3 / codestral) | — |
| Prompt Yönetimi | LangChain.js | ^0.2 |
| Vektör Bellek | ChromaDB (lokal) | ^1.8 |
| Tool Calling | Claude Tool Use / Function Calling | — |
| API Anahtarı | electron-store (DPAPI şifreli) | ^10 |

### 3.4 Unity Bridge İstemcisi (Main Process — EKLENTİ.md uyumlu)

Bu katman EKLENTİ.md'nin tanımladığı protokole **tam ve birebir uyar.** Hiçbir sapma kabul edilmez.

#### Taşıma Katmanı

| Mod | Protokol | Port | Kaynak |
|-----|----------|------|--------|
| TCP Socket (birincil) | localhost TCP | **57432** | EKLENTİ.md §6.1 |
| Named Pipe (opsiyonel) | Windows Named Pipe | `\\.\pipe\aigamedev-unity` | EKLENTİ.md §6.1 |

#### Mesaj Formatı (EKLENTİ.md §6.2 — değiştirilemez)

Tüm mesajlar UTF-8 JSON, `\n` ile sonlandırılır (line-delimited JSON):

```typescript
// src/main/integrations/unity-bridge.ts
interface BridgeMessage {
  messageId: string;   // uuid-v4
  type: 'command' | 'response' | 'event' | 'ping';
  agentId: 'ahbab' | 'obsidere' | 'patientia' | 'vates' | 'diafor' | 'magnumpus';
  timestamp: string;   // ISO 8601
  payload: Record<string, unknown>;
}
```

#### Platform Tarafından Gönderilen Komutlar (EKLENTİ.md §6.3)

| Komut | Gönderen Agent | Açıklama |
|-------|---------------|---------|
| `create_gameobject` | ahbab | Unity'de GameObject oluştur |
| `delete_gameobject` | ahbab | GameObject sil |
| `add_component` | ahbab | Bileşen ekle |
| `write_script` | ahbab | C# dosyası yaz + `AssetDatabase.Refresh()` |
| `compile_scripts` | ahbab | Derleme tetikle |
| `create_scene` | ahbab | Yeni sahne oluştur |
| `open_scene` | ahbab | Sahneyi aç |
| `enter_play_mode` | patientia | Play Mode başlat |
| `exit_play_mode` | patientia | Play Mode durdur |
| `take_screenshot` | patientia | Ekran görüntüsü al |
| `run_tests` | patientia | Unity Test Runner tetikle |

#### Platform Tarafından Alınan Olaylar (EKLENTİ.md §6.4)

| Olay | Açıklama |
|------|---------|
| `compile_started` | Derleme başladı |
| `compile_finished` | Derleme bitti (`success`, `errors[]`) |
| `play_mode_entered` | Play Mode aktif |
| `play_mode_exited` | Play Mode bitti (`duration`) |
| `scene_changed` | Sahne değişti |
| `asset_imported` | Asset import tamamlandı |
| `log_message` | Unity Console log (`Log\|Warning\|Error`) |

#### Güvenlik Kuralları (EKLENTİ.md §12 uyumlu — platform tarafı)

- Yalnızca `localhost` (127.0.0.1) bağlantısı kurulur — dış IP reddedilir
- Gönderilen tüm komutlar EKLENTİ.md §6.3 şemasına göre doğrulanır
- Bilinmeyen komut tipi gönderilmez
- Bağlantı koptuğunda otomatik yeniden bağlanma: 3 deneme, 2s aralık

### 3.5 Unreal Bridge İstemcisi (Main Process — UNREAL_EKLENTİ.md uyumlu)

| Protokol | Port |
|----------|------|
| TCP Socket (birincil) | **57433** |
| WebSocket (opsiyonel) | **57434** |

Mesaj formatı Unity bridge ile birebir aynıdır. Komut isimleri Unreal'a özgüdür (`create_actor`, `spawn_blueprint`, `write_source_file`, `start_pie`, `stop_pie`).

---

## 4. Agent Sistemi

### 4.1 Orkestrasyon Mimarisi

Agent runtime tamamen **Main Process** içinde çalışır. Renderer sadece IPC üzerinden durum güncellemelerini alır.

```
Kullanıcı Inputu (Renderer)
      │  ipcRenderer.invoke('agent:start', { input, motor })
      ▼
┌─────────────────────── MAIN PROCESS ────────────────────────────┐
│                                                                  │
│  ┌──────────┐   ┌──────────┐   ┌────────────────────────────┐  │
│  │  vates   │──►│  Diafor  │──►│           ahbab            │  │
│  │  [RED]   │   │  [YEL]   │   │          [GRN]             │  │
│  └──────────┘   └──────────┘   └───────────┬────────────────┘  │
│       ▲                                     │                   │
│       │                         ┌───────────┼───────────┐       │
│       │                         │           │           │       │
│       │                    Dahili Motor  Unity IPC   Unreal IPC │
│       │                    Babylon.js   :57432       :57433      │
│       │                                     │                   │
│       │                                     ▼                   │
│  ┌──────────┐                         ┌──────────┐              │
│  │ obsidere │◄────────────────────────│ obsidere │              │
│  │  [BLU]   │   (döngü, n kez)        │  [BLU]   │              │
│  └──────────┘                         └──────────┘              │
│                                             │                   │
│                                             ▼                   │
│                                 ┌──────────┐  ┌──────────┐     │
│                                 │patientia │─►│magnumpus │     │
│                                 │  [PUR]   │  │  [ORN]   │     │
│                                 └──────────┘  └──────────┘     │
└──────────────────────────────────────────────────────────────────┘
      │  mainWindow.webContents.send('agent:update', state)
      ▼
Renderer Process (Zustand store → UI yenileme)
```

### 4.2 Agent Tanımları

| Agent | Renk | Sorumluluk |
|-------|------|-----------|
| **vates** | RED `#FF3B3B` | Kullanıcı inputunu analiz et; DAG görev şeması oluştur |
| **Diafor** | YELLOW `#FFD600` | Bağımlılıkları kontrol et ve hazırla; motor bağlantısını doğrula |
| **ahbab** | GREEN `#00E676` | Yapım aşaması; motora komut gönder; kod yaz; asset oluştur |
| **obsidere** | BLUE `#2979FF` | ahbab çıktılarını denetle; hataları tespit et; döngüsel düzelt |
| **patientia** | PURPLE `#AA00FF` | Son testleri yap; 1-100 arası puanla; karara göre yönlendir |
| **magnumpus** | ORANGE `#FF6D00` | Tüm çıktıları birleştir; kullanıcıya teslim et |

#### patientia Puanlama Kuralı

| Puan | Karar | Eylem |
|------|-------|-------|
| 1–50 | Yetersiz | obsidere'ye geri gönder — hiçbir değişiklik yapmadan |
| 51–100 | Yeterli | magnumpus'a ilet — kullanıcıya sunmak için |

### 4.3 Agent İletişim Protokolü

```typescript
interface AgentMessage {
  agentId: 'vates' | 'diafor' | 'ahbab' | 'obsidere' | 'patientia' | 'magnumpus';
  runId:     string;    // uuid-v4 — pipeline oturum kimliği
  status:    'waiting' | 'running' | 'success' | 'error' | 'looping';
  inputRef:  string;    // önceki agent çıktı ID'si
  output:    unknown;
  toolCalls: ToolCall[];
  confidence: number;   // 0.0–1.0
  iteration:  number;   // obsidere döngü sayacı
}
```

### 4.4 Araç Kataloğu (Tool Registry)

Tüm tool'lar Claude Tool Use / Function Calling şemasıyla tanımlanır, Main Process içinde çalışır:

| Tool | Sahibi Agent | Motor Kısıtı | Açıklama |
|------|-------------|-------------|---------|
| `plan_task_graph` | vates | Tümü | Inputu DAG görev şemasına dönüştür |
| `check_dependencies` | Diafor | Tümü | npm paket + motor bağlantı kontrolü |
| `install_dependency` | Diafor | Tümü | `npm install` (Main Process) |
| `verify_bridge_connection` | Diafor | Unity/Unreal | TCP ping gönder, bağlantıyı doğrula |
| `write_code` | ahbab | Dahili / Unity | TypeScript veya C# dosyası yaz |
| `modify_scene` | ahbab | Dahili | Babylon.js sahnesini düzenle |
| `send_unity_command` | ahbab | Unity | EKLENTİ.md §6.3 komutu gönder |
| `send_unreal_command` | ahbab | Unreal | UNREAL_EKLENTİ.md §6.3 komutu gönder |
| `read_logs` | obsidere | Tümü | Build/runtime log ayrıştır |
| `apply_patch` | obsidere | Tümü | Diff uygula, dosyayı güncelle |
| `simulate_player` | patientia | Dahili | Babylon.js'e sanal input gönder |
| `trigger_unity_tests` | patientia | Unity | `run_tests` komutu gönder (EKLENTİ.md §6.3) |
| `score_build` | patientia | Tümü | 1-100 puan üret |
| `package_output` | magnumpus | Tümü | Tüm artifactları birleştir |

### 4.5 IPC Kanal Beyaz Listesi

Tüm kanallar Main Process'te açıkça tanımlanır:

| Kanal | Yön | Açıklama |
|-------|-----|---------|
| `agent:start` | Renderer → Main | Pipeline başlat |
| `agent:stop` | Renderer → Main | Graceful shutdown |
| `agent:update` | Main → Renderer | Durum güncellemesi |
| `agent:log` | Main → Renderer | Gerçek zamanlı log satırı |
| `agent:score` | Main → Renderer | patientia puanı (1-100) |
| `agent:complete` | Main → Renderer | magnumpus final çıktısı |
| `engine:status` | Main → Renderer | Motor FPS / bağlantı durumu |
| `bridge:connected` | Main → Renderer | Unity/Unreal bridge bağlandı |
| `bridge:disconnected` | Main → Renderer | Bridge bağlantısı koptu |
| `bridge:event` | Main → Renderer | Unity/Unreal'dan gelen olay |
| `slash:command` | Renderer → Main | `/illuminare`, `/sedare` vb. |
| `motor:change` | Renderer → Main | Motor kipini değiştir |

### 4.6 Özel Slash Komutları

| Komut | İşleyen | Davranış |
|-------|---------|---------|
| `/illuminare` | magnumpus | ReactFlow pipeline şemasını Renderer'da render et |
| `/sedare` | obsidere | Kısa özet çıkar, bağlam penceresini sıfırla |
| `/tekrar` | orchestrator | Son başarısız adımı yeniden çalıştır |
| `/durdur` | orchestrator | `ipcRenderer.invoke('agent:stop')` |
| `/motor [dahili\|unity\|unreal]` | Diafor + ahbab | Motor kipini değiştir |

---

## 5. Proje Dosya Yapısı

```
{proje-adı}\
├── src\
│   ├── main\
│   │   ├── index.ts
│   │   ├── window.ts
│   │   ├── menu.ts
│   │   ├── ipc\
│   │   │   ├── agentHandlers.ts
│   │   │   ├── engineHandlers.ts
│   │   │   └── bridgeHandlers.ts
│   │   ├── filesystem\
│   │   ├── compiler\
│   │   │   └── tsCompiler.ts
│   │   ├── lsp\
│   │   ├── watcher\
│   │   │   └── assetWatcher.ts        # chokidar — dahili motor için
│   │   ├── integrations\
│   │   │   ├── unity-bridge.ts        # EKLENTİ.md §6 protokolü
│   │   │   └── unreal-bridge.ts       # UNREAL_EKLENTİ.md §6 protokolü
│   │   └── agent-runtime\
│   │       ├── orchestrator.ts
│   │       ├── agents\
│   │       │   ├── vates.ts
│   │       │   ├── diafor.ts
│   │       │   ├── ahbab.ts
│   │       │   ├── obsidere.ts
│   │       │   ├── patientia.ts
│   │       │   └── magnumpus.ts
│   │       ├── tools\
│   │       │   ├── unityTools.ts      # send_unity_command, trigger_unity_tests
│   │       │   ├── unrealTools.ts     # send_unreal_command
│   │       │   ├── engineTools.ts     # modify_scene, simulate_player
│   │       │   └── commonTools.ts     # write_code, score_build, vb.
│   │       └── memory\
│   │           └── chromaClient.ts
│   ├── preload\
│   │   └── index.ts
│   └── renderer\
│       ├── index.tsx
│       ├── App.tsx
│       ├── components\
│       │   ├── Layout\
│       │   ├── AgentPanel\
│       │   ├── InputArea\
│       │   ├── LogStream\
│       │   ├── CodePreview\           # Monaco Editor
│       │   ├── GameCanvas\            # Babylon.js — yalnızca dahili kipte
│       │   ├── ContextPanel\
│       │   ├── StatusBar\
│       │   └── IlluminareFlow\        # ReactFlow
│       ├── engine\
│       │   ├── BabylonEngine.ts
│       │   ├── SceneManager.ts
│       │   ├── PhysicsManager.ts
│       │   └── ScriptRunner.ts
│       ├── store\
│       │   ├── agentStore.ts
│       │   ├── engineStore.ts
│       │   ├── bridgeStore.ts         # Unity/Unreal bağlantı durumu
│       │   ├── logStore.ts
│       │   ├── projectStore.ts
│       │   └── uiStore.ts
│       ├── hooks\
│       └── types\
├── assets\
│   ├── icons\
│   ├── themes\
│   └── engine-defaults\
├── game-projects\
│   └── [project-id]\
│       ├── assets\
│       ├── src\
│       └── scene.bscene
├── installer\
├── dist\
├── out\
├── tests\
│   ├── unit\
│   └── e2e\
├── package.json
├── package-lock.json
├── tsconfig.main.json
├── tsconfig.renderer.json
├── vite.config.ts
├── electron-builder.yml
└── DWSL.md
```

---

## 6. Bağımlılıklar

### 6.1 Üretim Bağımlılıkları

```json
{
  "dependencies": {
    "electron":                  "31.x.x",
    "react":                     "18.x.x",
    "react-dom":                 "18.x.x",
    "@monaco-editor/react":      "4.x.x",
    "monaco-editor":             "0.50.x",
    "monaco-languageclient":     "8.x.x",
    "vscode-ws-jsonrpc":         "3.x.x",
    "zustand":                   "4.x.x",
    "reactflow":                 "^11",
    "@babylonjs/core":           "^7.0",
    "@babylonjs/loaders":        "^7.0",
    "@babylonjs/materials":      "^7.0",
    "@babylonjs/gui":            "^7.0",
    "@dimforge/rapier3d":        "^0.12",
    "@dimforge/rapier2d":        "^0.12",
    "howler":                    "^2.2",
    "isolated-vm":               "^4.7",
    "chokidar":                  "^3.6",
    "electron-store":            "^10",
    "@anthropic-ai/sdk":         "^0.26",
    "langchain":                 "^0.2",
    "chromadb":                  "^1.8"
  }
}
```

### 6.2 Geliştirme Bağımlılıkları

```json
{
  "devDependencies": {
    "typescript":                       "5.5.x",
    "vite":                             "5.x.x",
    "vite-plugin-electron":             "0.28.x",
    "electron-builder":                 "24.x.x",
    "@types/react":                     "18.x.x",
    "@types/react-dom":                 "18.x.x",
    "@types/node":                      "20.x.x",
    "@types/howler":                    "^2.2",
    "vitest":                           "1.x.x",
    "playwright":                       "1.x.x",
    "@playwright/test":                 "1.x.x",
    "eslint":                           "9.x.x",
    "@typescript-eslint/parser":        "7.x.x",
    "@typescript-eslint/eslint-plugin": "7.x.x",
    "prettier":                         "3.x.x"
  }
}
```

Lisans kuralı (WUS.md §16): GPL lisanslı paket eklenmez.

---

## 7. Güvenlik

| Kural | Uygulama | Kaynak |
|-------|---------|--------|
| `nodeIntegration: false` | Tüm BrowserWindow'larda | WUS.md §11 |
| `contextIsolation: true` | Tüm BrowserWindow'larda | WUS.md §11 |
| `sandbox: true` | Tüm BrowserWindow'larda | WUS.md §11 |
| `webSecurity: true` | Değiştirilemez | WUS.md §11 |
| Ağ istekleri | Yalnızca Main Process | WUS.md §11 |
| CSP | `default-src 'self'; script-src 'self' 'wasm-unsafe-eval'` | WASM için |
| Bridge bağlantısı | Yalnızca `127.0.0.1` — dış IP engellenir | EKLENTİ.md §12 |
| Gönderilen komutlar | Yalnızca EKLENTİ.md §6.3 şemasındakiler | EKLENTİ.md §12 |
| Dosya erişimi | Yalnızca `game-projects\`; traversal engeli | WUS.md §11 |
| Agent kodu | `isolated-vm` sandbox | — |
| API anahtarı | `electron-store` DPAPI şifreli | WUS.md §11 |
| IPC kanalları | Main'de beyaz liste | WUS.md §11 |

---

## 8. Test Stratejisi

| Katman | Araç | Kapsam |
|--------|------|--------|
| Birim | Vitest 1.x | Agent logic, bridge istemcileri, store'lar — %80+ |
| E2E | Playwright 1.x | Pipeline başlatma, Unity bridge ping, slash komutları |
| CI | Her commit'te | WUS.md §14 adımları sırasıyla |

---

## 9. Kod Kalitesi (WUS.md §13)

| Araç | Yapılandırma |
|------|-------------|
| ESLint 9.x | `no-explicit-any: error`, `no-unsafe-assignment: error` |
| Prettier 3.x | `printWidth: 100`, `singleQuote: true`, `trailingComma: 'all'` |
| TypeScript | `strict: true`, `skipLibCheck: false` |

Commit öncesi: `prettier` → `eslint` → `tsc --noEmit`. Hata veren kod commit edilmez.

---

## 10. Desteklenen Oyun Türleri

| Tür | Dahili Motor | Unity Modu | Unreal Modu |
|-----|:-----------:|:----------:|:-----------:|
| 2D Platformer | ✅ | ✅ | ✅ |
| 2D Top-Down | ✅ | ✅ | ✅ |
| 2D Puzzle | ✅ | ✅ | ✅ |
| 3D First-Person | ✅ | ✅ | ✅ |
| 3D Third-Person | ✅ | ✅ | ✅ |
| 3D Strategy | ✅ | ✅ | ✅ |
| Card / Board | ✅ | ✅ | ❌ |
| Visual Novel | ✅ | ✅ | ❌ |

---

## 11. Proje Belgeleri

| Dosya | İçerik | Öncelik |
|-------|--------|---------|
| `EKLENTİ.md` | Unity bridge protokolü — KONU.md için bağlayıcı otorite | 1 |
| `KONU.md` | Bu dosya | 2 |

---

*EKLENTİ.md bu dosya için bağlayıcı otoritedir: port numaraları, mesaj formatı, komut şemaları ve güvenlik kuralları EKLENTİ.md'den alınır ve değiştirilemez. WUS.md Electron katmanı için aynı şekilde bağlayıcıdır.*
