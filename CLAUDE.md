# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Status: production-ready (LLM-driven)

The chat panel is fully functional with **real LLM responses** end-to-end:

- Gating: Vates (LLM) classifies every prompt → chat_only or pipeline_start.
  Greetings, "evet", "ne yapabilirsin" stay as chat. Concrete production verbs
  (oluştur/yap/ekle/build/…) trigger the 6-agent pipeline.
- Pipeline: when triggered, runs vates(plan) → diafor → ahbab → obsidere →
  patientia → magnumpus. Magnumpus also uses the LLM to write a 2-3 sentence
  Türkçe summary that lands in the chat (no markdown tables anymore).
- Sliding 12-turn chat history is fed back into the gate so follow-up "evet"
  is read in context, not as a fresh build request.

Default model: `google/gemini-3.1-flash-lite-preview` (configured via
`com.bitbing.unity-bridge/setting_referance.json`, git-ignored — see
`setting_referance.example.json` for the schema). Any other OpenRouter
chat-completions model can be dropped in; a server restart picks it up.

## Repository layout

This is a **monorepo** containing three related pieces. The top-level folder is **not** a Unity project:

- `com.bitbing.unity-bridge/` — Unity UPM package (C#). The Unity-side Editor plugin.
- `unity-mcp-server/` — Python MCP server (6-agent orchestrator). Talks to Unity over HTTP/localhost.
- `KONU.md`, `EKLENTİR.md`, `BUARDA.md` — Turkish-language design specs. **These are binding authority** — the protocol, port numbers, and agent list defined there cannot be violated by code changes (see "Authority" below).

## Installation quirk (critical)

The UPM package lives in a **subfolder**, not at the repo root — same pattern as COPLAY's `unity-mcp`. Users install it with the `?path=` query parameter:

```
https://github.com/ofa14-prog/bitbing-unity-bridge.git?path=/com.bitbing.unity-bridge#main
```

When iterating on the package, the user has to **Remove + re-Add** it from Unity's Package Manager for each commit. Unity does not auto-refresh git URL packages.

## Common commands

### Python chat server (`unity-mcp-server/`)

The primary way to run the server (minimal deps, no pip install needed):

```bash
cd unity-mcp-server
# Install minimal deps (one-time):
pip install fastapi uvicorn httpx

# Start the chat server (Unity Panel connects here):
PYTHONPATH=src python -m uvicorn unity_mcp_server.chat_server:app --host 127.0.0.1 --port 8001

# Windows CMD/PowerShell:
set PYTHONPATH=src && python -m uvicorn unity_mcp_server.chat_server:app --host 127.0.0.1 --port 8001
```

Full install (includes MCP server + all agents):

```bash
cd unity-mcp-server
pip install -e .[dev]          # editable install + dev deps
pytest                         # run all tests
ruff check src tests           # lint
unity-mcp                      # MCP server (stdio, for Claude Desktop/Cursor)
unity-mcp-chat                 # chat server (HTTP :8001, for Unity Panel)
```

### Unity package (`com.bitbing.unity-bridge/`)

There is no CLI build. Unity itself compiles the package. Typical loop:

1. Edit `.cs` / `.uxml` / `.uss` files.
2. `git commit && git push`.
3. In Unity: Package Manager → Remove → Add package from git URL (same `?path=` URL as above).
4. Tests live in `Tests/Editor` and `Tests/Runtime`; run via **Window → General → Test Runner**.

## Architecture

### Full flow (chat UI path)

```
Unity Editor
  └─ Agent Panel (Ctrl+Shift+G)
       └─ User types prompt
            │
            │  POST /api/run  (NDJSON stream)
            ▼
       Python Chat Server  ──localhost:8001──
            │
            │  run_with_progress() — 6-agent pipeline
            │  emits agent_event JSON per step
            ▼
       orchestrator.py
            │
            │  ahbab sends Unity commands
            ▼
       Unity McpListener  ──localhost:8080──  Unity Editor API
```

- **Agent Panel** (`Window → BitBing → Agent Panel`, `Ctrl+Shift+G`): chat UI inside Unity Editor. Sends prompts, receives NDJSON stream, updates agent status cards live.
- **Python chat server** (`chat_server.py`, port 8001): FastAPI + WebSocket. Receives prompt, runs 6-agent pipeline, streams progress events back to Unity. Auto-detected and launched by the Agent Panel on startup.
- **Unity McpListener** (port 8080): receives Unity commands from ahbab during pipeline execution.

### Legacy MCP path (Claude Desktop / Cursor)

```
AI client (Claude/Cursor)  ──MCP/JSON-RPC──▶  Python MCP Server  ──HTTP localhost:8080──▶  Unity Editor (C# bridge)
```

### 6-agent orchestration (do not rename these)

Agent IDs are load-bearing — they appear verbatim in the wire protocol (`agentId` field) and in UI color mapping. Dictionary keys in C# must be **lowercase** to match what Python emits.

| Agent | ID (wire) | Color | Role |
|-------|-----------|-------|------|
| `vates` | `vates` | `#FF3B3B` | Parse user input → DAG of tasks |
| `Diafor` | `diafor` | `#FFD600` | Check/prepare dependencies, validate bridge connection |
| `ahbab` | `ahbab` | `#00E676` | Execute: send Unity commands, write scripts, modify assets |
| `obsidere` | `obsidere` | `#2979FF` | Audit ahbab's output, detect errors, loop back for fixes |
| `patientia` | `patientia` | `#AA00FF` | Score the build 1-100; <50 → back to obsidere, ≥51 → forward |
| `magnumpus` | `magnumpus` | `#FF6D00` | Package final output, deliver to user |

Python: `unity-mcp-server/src/unity_mcp_server/agents/<name>.py` — one file per agent, plus `orchestrator.py` driving the pipeline.

### Python server internals

- `agents/orchestrator.py` — drives the pipeline. Key method: `run_with_progress(prompt, game_type, callback)` — emits `{"type":"agent_event","agentId":…,"status":…,"message":…}` per step, then `{"type":"pipeline_complete",…}`.
- `agents/diafor.py` — checks Unity bridge by sending a JSON-RPC `initialize` POST to `127.0.0.1:8080/mcp`. A healthy bridge returns `{"result":…}`. **Do not use GET** — McpListener only handles POST.
- `chat_server.py` — FastAPI app. `POST /api/run` → NDJSON stream (one JSON line per agent event). `GET /` → web fallback UI.

### Unity bridge internals

- `Editor/Bridge/McpListener.cs` — HTTP listener for MCP commands (port 8080, POST only, `127.0.0.1` only).
- `Editor/Bridge/McpToolRegistry.cs` — maps incoming tool names to `IAgentCommand` implementations.
- `Editor/Commands/*.cs` — one file per command (`create_gameobject`, `write_script`, `enter_play_mode`, etc.). All implement `IAgentCommand.Execute()` → `CommandResult`.
- `Editor/Settings/BridgeSettings.cs` — `ScriptableObject` at `Assets/Settings/BridgeSettings.asset`. Menu: **Edit → Project Settings → BitBing Unity Bridge**.
- `Editor/UI/AgentPanelWindow.cs` — EditorWindow (UI Toolkit). Chat UI + Python server auto-launch + NDJSON stream reader. Menu: **Window → BitBing → Agent Panel** (`Ctrl+Shift+G`).
- `Editor/UI/AgentStatusCard.cs` — compact agent row: colored dot + name + live status/message. Call `SetRunning(msg)` / `SetSuccess(msg)` / `SetError(msg)` / `SetIdle()`.
- `Editor/UI/AgentPanelWindow.uxml` + `.uss` — layout: server bar → connection bar → agent grid (2×3) → chat ScrollView (flex-grow:1) → input row → quick commands.

Namespaces: `BitBing.UnityBridge.{Editor,Runtime,Editor.Commands,Editor.UI,Editor.Settings}`. Never use `AIGameDev.*`.

## Agent Panel — how it works

1. `OnEnable` → starts McpListener (if `autoConnect=true`) → `EditorApplication.delayCall` → `CheckAndStartServer()`
2. `CheckAndStartServer()` — TCP-probes port 8001. If down, calls `StartPythonServer()` which tries:
   - `unity-mcp-chat` (if pip-installed and in PATH)
   - `python -m uvicorn unity_mcp_server.chat_server:app …` (if `unity-mcp-server/` found by walking up from `Application.dataPath`)
3. User types prompt → `OnSend()` → `RunPipelineAsync()` — HTTP POST to `localhost:8001/api/run`
4. Reads NDJSON stream line-by-line on background thread → marshals UI updates to main thread via `ConcurrentQueue<Action>` drained by `EditorApplication.update`
5. `agent_event` lines → `UpdateAgentCard(agentId, status, message)` — case-insensitive lookup
6. `pipeline_complete` → chat bubble with score/grade/summary
7. `pipeline_failed` → error bubble

## LLM gating (Vates is the gatekeeper)

Every prompt the user types in the Agent Panel is **classified by Vates first** —
the 6-agent pipeline does NOT start automatically.

```
prompt → orchestrator.run_with_progress()
           │
           ▼
       vates.classify_intent(prompt, history)   ← LLM call (OpenRouter)
           │
   ┌───────┴───────┐
   chat_only       pipeline_start
   │               │
   emit            run vates.plan() → diafor → ahbab → obsidere → patientia → magnumpus
   chat_message    emit agent_event per step
   event           emit pipeline_complete with score+grade
   (only)
```

**Key rules baked into the gate prompt (vates.py `_GATE_SYSTEM`):**

- Bare confirmations ("evet", "hayır", "tamam", "iptal", "ok") never trigger the pipeline.
- Greetings, questions, clarifications, opinions → `chat_only`.
- Concrete production verbs ("yap", "oluştur", "build", "create", "ekle", "kodla", …) → `pipeline_start`.
- Vates returns JSON: `{decision, reason, chat_reply}`.

The orchestrator keeps a sliding 12-turn `_chat_history` (user+assistant) so the
gate sees context — that's what lets it correctly classify a follow-up "evet" as
chat, not as a new build request.

**LLM client (`unity_mcp_server/llm_client.py`):**

OpenAI-compatible chat completions via `httpx` (no SDK dependency).

Settings resolution order:
1. Env vars: `ANTHROPIC_BASE_URL`, `ANTHROPIC_AUTH_TOKEN`, `ANTHROPIC_API_KEY`, `ANTHROPIC_MODEL`
2. `~/.bitbing/settings.json`
3. `com.bitbing.unity-bridge/setting_referance.json` (dev convenience — git-ignored!)

`setting_referance.json` schema:
```json
{ "env": { "ANTHROPIC_BASE_URL": "https://openrouter.ai/api",
           "ANTHROPIC_AUTH_TOKEN": "sk-or-v1-…",
           "ANTHROPIC_MODEL": "minimax/minimax-m2.7" } }
```

⚠️ **NEVER commit `setting_referance.json`.** It is in `.gitignore`. Rotate the
key immediately if it leaks. The OpenRouter base URL is normalized to `/v1` if
not already suffixed.

**New event type: `chat_message`**

When Vates says `chat_only`, the orchestrator emits `{type:"chat_message", text:…}`
instead of `pipeline_complete`. The Agent Panel's `HandleStreamLine` switch has
a case for it that just adds a system bubble — no agent cards advance.

When Vates says `pipeline_start`, agent cards run as before; the cards for
the chat-only branch are emitted as `idle` so the UI doesn't show stale "running"
state from a prior run.

## COPLAY-inspired infrastructure (port registry, PID file, service locator)

Adapted from `CoplayDev/unity-mcp` so the chat server and Unity bridge survive port collisions and double-launches. Both sides share a JSON registry:

- **Registry path:** `%TEMP%/bitbing/ports.json` — `{ unity_port, chat_port, project_path, updated_at }`
- **PID path:** `%TEMP%/bitbing/chat-server.pid` — written by chat_server `start()`, read by Unity `PythonServerLauncher`

Unity side (`com.bitbing.unity-bridge/Editor/`):
- `Helpers/PortManager.cs` — picks free port, writes registry. `DefaultUnityPort=8080`, `DefaultChatPort=8001`.
- `Helpers/PidFileManager.cs` — read/write/IsAlive against the chat-server PID.
- `Helpers/BitBingLog.cs` — central logger with `OnLog` event for Agent Panel routing.
- `Services/BridgeControlService.cs` — single owner of `McpListener`, exposes `IsRunning`/`CurrentPort`/`StateChanged`.
- `Services/PythonServerLauncher.cs` — `EnsureRunningAsync()`: PID-check → free-port-search → `python -m uvicorn` with `PYTHONPATH=src/`.
- `Services/BitBingServiceLocator.cs` — singleton holder for `BridgeControlService` (Agent Panel pulls from here instead of constructing its own listener).
- `Commands/BitBingCommandAttribute.cs` — `[BitBingCommand("name")]` attribute for future auto-discovered commands.
- `Bridge/McpToolRegistry.cs` — `AutoDiscoverTools()` reflects every `IMcpTool` with a parameterless ctor; no manual register block.

Python side (`unity-mcp-server/src/unity_mcp_server/`):
- `port_registry.py` — Python mirror of the registry. `get_unity_port()`, `save_chat_port()`, `is_port_available()`, `find_available_chat_port()`.
- `pid_manager.py` — `claim_or_exit()` aborts cleanly if a live predecessor exists; clears file on shutdown.
- `agents/unity_transport.py` — singleton async `httpx.AsyncClient` for JSON-RPC `tools/call`, used by ahbab and (future) other agents.
- `chat_server.py` — `start()` claims PID, picks free port, writes it to registry; `/health` returns both ports.
- `agents/diafor.py` — reads Unity port from registry instead of hardcoded 8080.

If you ever see `[Errno 10048]` again, it means PID-file or registry got corrupted: delete `%TEMP%/bitbing/` and retry.

## Non-obvious gotchas

- **`.meta` files must have 32-char lowercase hex GUIDs in Unity YAML format.** Use deterministic GUIDs (md5 of package-relative path). See commit "Fix Unity .meta files".
- **`UQueryBuilder<T>` vs `Q<T>`**: `_root.Query<Button>(id)` returns a builder. Use `_root.Q<Button>(id)` for single-element lookup.
- **`ScreenCapture.CaptureScreenshot`** returns `void`; 2nd arg is `int` supersize multiplier, not `bool includeUI`.
- **Close-time NREs in Agent Panel**: unsubscribe `OnLog`/`OnMessageProcessed` *before* calling `McpListener.Stop()` in `OnDisable`.
- **`System.Diagnostics` ambiguity**: `AgentPanelWindow.cs` imports both `System.Diagnostics` (for `Process`) and `UnityEngine`. Always include `using Debug = UnityEngine.Debug;` to resolve the `Debug` conflict.
- **McpListener only handles POST**: diafor's health check must use `POST` with a JSON-RPC body. A `GET /mcp` will not return 200.
- **Agent card dictionary keys must be lowercase**: Python emits `"diafor"` (lowercase); the C# `_agentCards` dictionary stores with lowercase keys. Case-insensitive fallback exists but lowercase is canonical.
- **Line endings**: committed LF; Windows git converts to CRLF on checkout (warnings are benign).
- **Port 8001 (chat server) is not in the frozen list** (57432/8080/57433/57434 are frozen by EKLENTİR.md). Port 8001 can be changed if needed.

## Authority hierarchy

When specs disagree with code, specs win — in this order:

1. `EKLENTİR.md` — Unity bridge protocol (ports 57432/8080, message schema, command payloads, event list). Binding for both Unity-side C# and Python-side client.
2. `KONU.md` — Platform-wide architecture (agent list, Electron shell rules, tech stack). Binding for anything cross-cutting.
3. `BUARDA.md` — COPLAY architecture research. Explains *why* the `?path=` install pattern exists.

Port numbers (57432 TCP, 8080 HTTP, 57433/57434 Unreal) are frozen by EKLENTİR.md §6.1 and KONU.md §3.4 — do not change them without updating both specs.

## Security constraints (from specs, enforced in code)

- Unity bridge accepts `127.0.0.1` only — reject any external IP.
- Command payloads are validated against the §6.3 schema; unknown command names are rejected, no dynamic code execution.
- Script writes are confined to `Assets/`; `../` traversal and writes into `Editor/`, `Packages/`, `ProjectSettings/` are rejected with `INVALID_PATH`. Path safety check: `Path.GetFullPath(target).StartsWith(Application.dataPath, OrdinalIgnoreCase)`.
- Chat server also binds to `127.0.0.1` only (never `0.0.0.0`).
