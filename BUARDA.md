# BUARDA.md — COPLAY ve Unity MCP Mimari Araştırması

> **Araştırma Tarihi:** 2026-04-22
> **Amaç:** COPLAY'ın mimarisini analiz etmek ve proje için alternatif mimari seçeneklerini belirlemek

---

## 1. COPLAY (coplay-ai/unity-mcp) — Mimari Analizi

### 1.1 Genel Bakış

COPLAY, **Model Context Protocol (MCP)** kullanarak AI assistant'ları (Claude, Cursor, VS Code Copilot) Unity Editor'e bağlayan açık kaynaklı bir Unity eklentisidir.

| Özellik | Değer |
|---------|-------|
| **GitHub** | [coplay-ai/unity-mcp](https://github.com/coplay-ai/unity-mcp) |
| **Lisans** | MIT |
| **Yıldız** | 5,800+ |
| **Fork** | 700+ |
| **Desteklenen Unity** | 2021.3 LTS → Unity 6 |

### 1.2 Teknoloji Yığını

```
┌─────────────────────────────────────────────────────────────┐
│                    AI Assistant                             │
│              (Claude / Cursor / Copilot)                    │
├─────────────────────────────────────────────────────────────┤
│                    MCP Protocol                             │
│              (JSON-RPC 2.0 — stdio / HTTP)                  │
├─────────────────────────────────────────────────────────────┤
│                 Python MCP Server                           │
│              (uvx ile çalışan Python server)                 │
│              localhost:8080 (HTTP varsayılan)                  │
├─────────────────────────────────────────────────────────────┤
│                 WebSocket + HTTP                            │
│              (localhost üzerinden Unity'a bağlantı)          │
├─────────────────────────────────────────────────────────────┤
│              Unity Editor Plugin (C#)                       │
│         (UPM paketi — "Bridge" olarak adlandırılıyor)        │
│         Editor içinde komutları yürütür                     │
├─────────────────────────────────────────────────────────────┤
│                   Unity Editor API                          │
│         (Scene, Assets, Scripts, vb.)                         │
└─────────────────────────────────────────────────────────────┘
```

### 1.3 İki Parçalı Mimari Tasarımı

COPLAY iki temel bileşenden oluşur:

| Bileşen | Dil | Rol | Kurulum |
|---------|-----|-----|---------|
| **Unity Bridge** | C# (UPM paketi) | Editor içinde "eller" — gelen komutları yürütür | Unity projesine UPM ile kurulur |
| **MCP Server** | Python | "Beyin" — AI isteklerini komutlara çevirir | `uvx unity_mcp` ile çalıştırılır |

### 1.4 İletişim Protokolü

| Katman | Protokol | Detay |
|--------|----------|-------|
| AI ↔ MCP Server | MCP (JSON-RPC 2.0) | stdio veya HTTP |
| MCP Server ↔ Unity | HTTP / WebSocket | localhost:8080 |
| Unity ↔ Editor API | C# Unity API | Direkt metod çağrıları |

### 1.5 Mevcut Araçlar

COPLAY'ın **86+ araç** sağladığı belirtiliyor:
- Asset yönetimi
- Sahne kontrolü
- Script düzenleme
- GameObject oluşturma/yönetme
- Bileşen ekleme/değiştirme

---

## 2. Alternatif Mimari Seçenekleri

### 2.1 Seçenek A — Python MCP Server + C# Unity Bridge (COPLAY Modeli)

```
AI Assistant → MCP (stdio/HTTP) → Python Server → HTTP → Unity Bridge (C#)
```

| Avantajlar | Dezavantajlar |
|------------|---------------|
| MCP standardına uygun | Python bağımlılığı |
| Claude, Cursor, Copilot ile uyumlu | Ek kurulum adımı gerekiyor |
| Geniş araç desteği (86+) | Runtime bağımlılık |
| Aktif topluluk (5800+ stars) | |

### 2.2 Seçenek B — C# MCP Server + C# Unity Bridge (IvanMurzak Modeli)

```
AI Assistant → MCP (stdio/HTTP) → C# Server (.NET binary) → stdio → Unity Bridge (C#)
```

| Avantajlar | Dezavantajlar |
|------------|---------------|
| Tek dil (C#) | Daha az araç (19+) |
| .NET runtime yeterli | Daha küçük topluluk |
| Runtime'da çalışabilir | HTTP desteği yok |

### 2.3 Seçenek C — Sadece C# Unity Bridge (EKLENTİR.md Modeli)

```
Harici AI → TCP Socket (:57432) → Unity Bridge (C#)
```

| Avantajlar | Dezavantajlar |
|------------|---------------|
| Python/.NET gerekmiyor | MCP standardına uymaz |
| Basit mimari | Sadece TCP üzerinden |
| EKLENTİR.md ile uyumlu | AI client bağımsız |

---

## 3. Karşılaştırma Matrisi

| Kriter | Seçenek A (COPLAY) | Seçenek B (IvanMurzak) | Seçenek C (EKLENTİR) |
|--------|:------------------:|:---------------------:|:-------------------:|
| **Protokol** | MCP | MCP | TCP (özel) |
| **Server Dil** | Python | C# (.NET) | — |
| **Unity Tarafı** | C# | C# | C# |
| **AI Uyumu** | Claude/Cursor/Copilot | Claude/Cursor | Özel |
| **Kurulum Zorluğu** | Orta | Kolay | Kolay |
| **Araç Sayısı** | 86+ | 19+ | 11 |
| **Topluluk** | Büyük | Orta | Yeni |
| **MCP Standardı** | Evet | Evet | Hayır |

---

## 4. MCP (Model Context Protocol) Nedir?

> *"MCP, AI için 'USB Type-C' gibidir."* — MCP dokümantasyonu

MCP, AI assistant'ların harici araçlara güvenli ve yapılandırılmış erişimini sağlayan açık bir protokoldür.

| Özellik | Açıklama |
|---------|----------|
| **Protokol** | JSON-RPC 2.0 |
| **Transport** | stdio (yerel) / HTTP (uzak) |
| **Standardizasyon** | AI client bağımsız — Claude, Cursor, Copilot, Windsurf |
| **Güvenlik** | Sandboxed araç erişimi |

---

## 5. Öneri

Eğer **MCP standardına uymak** ve **geniş AI client uyumu** isteniyorsa:

**→ Seçenek A (COPLAY modeli)** tercih edilmeli
- Python MCP server (`uvx unity_mcp`)
- C# Unity Bridge (UPM)
- MCP protokolü ile Claude/Cursor/Copilot uyumlu

Eğer **sadece belirli bir AI client** (örn. sadece Claude Code) ile çalışacaksa ve **basitlik** öncelikse:

**→ Seçenek C (EKLENTİR.md modeli)** tercih edilebilir
- Özel TCP protokolü
- Sadece C# — başka bağımlılık yok

---

## 6. Kaynaklar

- [COPLAY unity-mcp](https://github.com/coplay-ai/unity-mcp)
- [IvanMurzak Unity-MCP](https://github.com/IvanMurzak/Unity-MCP)
- [Unity MCP Resmi Dokümantasyon](https://unity.com/ai/docs/editor/mcp)
- [Model Context Protocol](https://modelcontextprotocol.io/)

---

*Bu dosya COPLAY'ın mimarisini analiz eder. Karar verildikten sonra proje mimarisi BUARDA.md'ye sadık kalınarak oluşturulacaktır.*
