# BitBing Unity Bridge

Connects the BitBing AI game-dev platform agent pipeline to the Unity Editor.
Provides programmatic editor control (create GameObjects, write scripts,
enter/exit Play Mode, run tests, …) and an in-editor UI panel for monitoring
and interaction.

- **Package ID:** `com.bitbing.unity-bridge`
- **Unity:** 2022.3 LTS · 2023 LTS · Unity 6
- **License:** MIT
- **Spec:** see `EKLENTİR.md` and `KONU.md` at the repo root.

---

## Installation

This repository is a monorepo. The UPM package itself lives in the
`com.bitbing.unity-bridge/` subfolder, so installs must point to that
subfolder with the `?path=` query parameter — same pattern COPLAY's
`unity-mcp` uses.

### 1. Package Manager — Add package from git URL

In Unity: **Window → Package Manager → + → Add package from git URL…**

```
https://github.com/ofa14-prog/bitbing-unity-bridge.git?path=/com.bitbing.unity-bridge#main
```

Pin to a specific release:

```
https://github.com/ofa14-prog/bitbing-unity-bridge.git?path=/com.bitbing.unity-bridge#v0.1.0
```

### 2. `Packages/manifest.json`

```json
{
  "dependencies": {
    "com.bitbing.unity-bridge": "https://github.com/ofa14-prog/bitbing-unity-bridge.git?path=/com.bitbing.unity-bridge#main"
  }
}
```

### 3. Local development

```json
{
  "dependencies": {
    "com.bitbing.unity-bridge": "file:../../path/to/bitbing-unity-bridge/com.bitbing.unity-bridge"
  }
}
```

### Auto-resolved dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| `com.unity.nuget.newtonsoft-json` | 3.2.1 | JSON serialization for bridge messages |

---

## Usage

1. Install the package (above).
2. Open **Window → BitBing → Agent Panel** (`Ctrl+Shift+G`).
3. Configure transport/port in **Edit → Project Settings → BitBing Unity Bridge**.
4. Start the platform-side client (Electron main process) on the same port
   — TCP `localhost:57432` by default — and it will connect.

---

## Package layout

```
com.bitbing.unity-bridge/
├── package.json
├── README.md
├── CHANGELOG.md
├── LICENSE.md
├── Editor/
│   ├── com.bitbing.unity-bridge.editor.asmdef
│   ├── Bridge/        # MCP listener, tool registry
│   ├── Commands/      # IAgentCommand implementations
│   ├── Settings/      # BridgeSettings + provider
│   └── UI/            # AgentPanelWindow (UI Toolkit)
├── Runtime/
│   ├── com.bitbing.unity-bridge.runtime.asmdef
│   └── AgentRuntimeBridge.cs
└── Tests/
    ├── Editor/com.bitbing.unity-bridge.tests.editor.asmdef
    └── Runtime/com.bitbing.unity-bridge.tests.runtime.asmdef
```

---

## Protocol

See `EKLENTİR.md` §6 for the bridge message schema, command payloads, and
event list. The Unity side of this package implements that contract exactly.
