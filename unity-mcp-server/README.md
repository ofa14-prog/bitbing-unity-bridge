# Unity MCP Server

AI GameDev Platform için MCP (Model Context Protocol) sunucusu. Unity Editor'e AI asistanları (Claude, Cursor, Copilot) bağlar.

## Mimari

```
AI Assistant (Claude/Cursor/Copilot)
    ↓ MCP (JSON-RPC 2.0)
Python MCP Server
    ↓ HTTP (:8080)
Unity C# Bridge (UPM Paketi)
    ↓ Unity Editor API
Unity Editor
```

## Özellikler

- **MCP Protokolü**: AI assistant'lar ile standart iletişim
- **6-Agent Orkestrasyon**: vates → Diafor → ahbab → obsidere → patientia → magnumpus
- **11 Unity Komutu**: create_gameobject, write_script, enter_play_mode, vb.
- **UI Panel**: Unity Editor içinde izleme ve kontrol

## Kurulum

### Python Server

```bash
# uvx ile çalıştırma (önerilen)
uvx unity-mcp-server

# veya pip ile
pip install unity-mcp-server
python -m unity_mcp_server.main
```

### Unity Paketi

Unity Editor'de:
```
Window → Package Manager → + → Add package from git URL
```

```
https://github.com/{org}/aigamedev-unity-bridge.git#main
```

## Kullanım

### Claude Code ile

```bash
# Claude Code'da MCP sunucusunu başlat
claude mcp add unity-bridge -- uvx unity-mcp-server

# Komutları kullan
claude "Create a 2D platformer game in Unity"
```

### MCP Tools

| Tool | Açıklama |
|------|----------|
| `create_gameobject` | GameObject oluştur |
| `write_script` | C# script yaz |
| `enter_play_mode` | Play Mode başlat |
| `run_tests` | Test çalıştır |
| `orchestrate_pipeline` | 6-agent pipeline çalıştır |

## Agent Sistemi

| Agent | Renk | Rol |
|-------|------|-----|
| **vates** | Kırmızı | Input analizi, DAG oluşturma |
| **Diafor** | Sarı | Bağımlılık kontrolü |
| **ahbab** | Yeşil | Unity komutları gönderme |
| **obsidere** | Mavi | Çıktı denetimi |
| **patientia** | Mor | Test ve puanlama |
| **magnumpus** | Turuncu | Paketleme |

## Lisans

MIT
