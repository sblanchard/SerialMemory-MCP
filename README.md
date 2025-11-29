# SerialMemory MCP Client

Official **Model Context Protocol (MCP)** client for the SerialMemory Cloud Platform.  
Compatible with **Claude Desktop**, **Cursor**, **Kilo Code**, and any MCP-enabled IDE.

![Release](https://img.shields.io/github/v/release/sblanchard/SerialMemory-MCP)
![License](https://img.shields.io/badge/License-MIT-green)
![Framework](https://img.shields.io/badge/.NET-10-blueviolet)
![MCP](https://img.shields.io/badge/MCP-2024--11--05-black)
![Platform](https://img.shields.io/badge/Windows/Linux/macOS-supported-008080)

---

## 🚀 Overview

The SerialMemory MCP Client is a **thin, stateless proxy** that forwards MCP tool calls to:

https://api.serialmemory.dev/api/mcp/tools/<toolName>

It contains **zero backend logic** — all intelligence happens on SerialMemory Cloud.

---

## 📦 Installation

### 1. Download Binary

➡️ https://github.com/sblanchard/SerialMemory-MCP/releases

- Windows: serialmemory-mcp-win-x64.zip  
- macOS ARM: serialmemory-mcp-macos-arm64.tar.gz  
- Linux: serialmemory-mcp-linux-x64.tar.gz  

Extract anywhere.

---

## ⚙️ Configuration

### Linux / macOS

```
export SERIALMEMORY_ENDPOINT="https://api.serialmemory.dev"
export SERIALMEMORY_API_KEY="your-api-key"
```

### Windows PowerShell

```
setx SERIALMEMORY_ENDPOINT "https://api.serialmemory.dev"
setx SERIALMEMORY_API_KEY "your-api-key"
```

Get your API key from:  
https://app.serialmemory.dev/dashboard

---

## ▶️ Running

### Direct execution

```
./serialmemory-mcp
```

### Docker

```
docker run -it \
  -e SERIALMEMORY_ENDPOINT="https://api.serialmemory.dev" \
  -e SERIALMEMORY_API_KEY="your-api-key" \
  serialcoder/serialmemory-mcp:latest
```

---

## 🔌 MCP Integration

### Claude Desktop

Modify:

`~/Library/Application Support/Claude/claude_desktop_config.json`

Add:

```
{
  "mcpServers": {
    "serialmemory": {
      "command": "/path/to/serialmemory-mcp"
    }
  }
}
```

Restart Claude.

---

### Cursor

`~/.cursor/mcp.json`

```
{
  "serialmemory": {
    "command": "/path/to/serialmemory-mcp"
  }
}
```

---

### Kilo Code

`~/.kilocode/mcp.json`

```
{
  "servers": {
    "serialmemory": {
      "command": "/path/to/serialmemory-mcp"
    }
  }
}
```

---

## 🧩 Tools

| Tool | Description |
|------|-------------|
| memory.search | Semantic & keyword search |
| memory.add | Add a memory item |
| memory.update | Update an existing memory |
| memory.delete | Remove a memory |
| memory.list_recent | Recently added memories |
| session.list | Conversation sessions |
| graph.query | Knowledge-graph query |

All calls proxy to:  
`POST /api/mcp/tools/<toolName>`

---

## 🏗 Development

```
git clone https://github.com/sblanchard/SerialMemory-MCP
cd SerialMemory-MCP
dotnet build
dotnet run --project SerialMemory.Mcp
```

---

## 🐳 Dockerfile (included)

```
FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY . .
ENTRYPOINT ["./serialmemory-mcp"]
```

---

## 🔐 Security

- No local storage  
- No embeddings  
- No database access  
- Pure HTTPS proxy  
- Safe in enterprise environments  

---

## 🗒 Changelog

### v1.0.0

- First public release  
- Claude, Cursor, Kilo Code support  
- Docker build  
- Multi-OS support  
- Stateless MCP proxy  

---

## 📜 License

MIT License © Serialcoder

