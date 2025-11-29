# SerialMemory MCP Client  
Official **Model Context Protocol (MCP) client** for connecting the SerialMemory Cloud Platform to tools like **Claude Desktop**, **Cursor**, **Kilo Code**, and any MCP runtime.

![GitHub Release](https://img.shields.io/github/v/release/sblanchard/SerialMemory-MCP)
![License](https://img.shields.io/badge/license-MIT-green)
![Platform](https://img.shields.io/badge/.NET-10-blueviolet)
![MCP Compatible](https://img.shields.io/badge/MCP-2024--11--05-black)
![Docker](https://img.shields.io/badge/docker-supported-2496ED)
![Kilo Code](https://img.shields.io/badge/Kilo%20Code-MCP%20Compatible-orange)

---

## 🚀 Overview

This repository contains the **official lightweight MCP client** for SerialMemory.

The MCP client:

- Provides all SerialMemory tools to AI IDEs and assistants  
- Contains **zero backend logic**  
- Proxies all requests to:  
  **https://api.serialmemory.dev**
- Works with:
  - Claude Desktop
  - Cursor
  - Kilo Code
  - IntelliJ / VSCode MCP plugins
  - Any STDIO-based MCP runtime

No databases, no embeddings, no ML, no Redis — all intelligence is in the **SerialMemory platform**.

---

## ✨ Features

- ✔ Full MCP compatibility (2024-11-05 spec)
- ✔ Works out of the box with Claude / Cursor / Kilo Code  
- ✔ Cross-platform: Windows, macOS, Linux  
- ✔ Docker-ready  
- ✔ Secure API-key authentication  
- ✔ Extremely lightweight (<300 LOC)  
- ✔ No state stored locally  
- ✔ Safe for public distribution  

---

## 📦 Installation

### 1. Download release

https://github.com/sblanchard/SerialMemory-MCP/releases

Choose:

- Windows: `serialmemory-mcp-win-x64.zip`
- macOS ARM: `serialmemory-mcp-macos-arm64.tar.gz`
- Linux: `serialmemory-mcp-linux-x64.tar.gz`

Extract anywhere you like.

---

## ⚙️ Configuration

Set two required environment variables:

### Linux/macOS

export SERIALMEMORY_ENDPOINT="https://api.serialmemory.dev"
export SERIALMEMORY_API_KEY="your-api-key"

Windows (PowerShell)

setx SERIALMEMORY_ENDPOINT "https://api.serialmemory.dev"
setx SERIALMEMORY_API_KEY "your-api-key"

Get your key from:
👉 https://serialmemory.dev/dashboard

▶️ Running
Direct exe / binary
./serialmemory-mcp

Docker
docker run -it \
  -e SERIALMEMORY_ENDPOINT="https://api.serialmemory.dev" \
  -e SERIALMEMORY_API_KEY="your-api-key" \
  serialcoder/serialmemory-mcp:latest

🔌 MCP Integration
Claude Desktop

Edit:

~/Library/Application Support/Claude/claude_desktop_config.json

Add:

{
  "mcpServers": {
    "serialmemory": {
      "command": "/path/to/serialmemory-mcp"
    }
  }
}


Restart Claude → tools appear automatically.

Cursor

Edit:

~/.cursor/mcp.json

{
  "serialmemory": {
    "command": "/path/to/serialmemory-mcp"
  }
}

Kilo Code (MCP IDE)

Add to:

~/.kilocode/mcp.json

{
  "servers": {
    "serialmemory": {
      "command": "/path/to/serialmemory-mcp"
    }
  }
}


Restart Kilo Code → SerialMemory tools appear in the MCP panel.

🧩 Available Tools

From SerialMemory.Mcp/Tools/ToolDefinitions.cs.

Tool	Description
memory.search	Hybrid search over your memory graph
memory.add	Add a memory
memory.update	Update a memory
memory.delete	Hard or soft delete
memory.list_recent	Recent memory activity
session.list	List conversation sessions
graph.query	Query entity & relationship graph

All calls are forwarded to the SerialMemory backend.

🏗 Development
git clone https://github.com/sblanchard/SerialMemory-MCP
cd SerialMemory-MCP
dotnet build
dotnet run --project SerialMemory.Mcp


Project structure:

SerialMemory-MCP/
 ├── SerialMemory.Mcp/
 │    ├── Program.cs
 │    ├── Dockerfile
 │    └── Tools/
 │         └── ToolDefinitions.cs
 ├── README.md
 └── docs/
      ├── index.md
      └── tools.md

🔐 Security

No local storage

No caching

No credential persistence

Uses secure API tokens

Forwards only to your tenant via SerialMemory Cloud

No access to local files or other system processes

Safe for: workplace use, multi-tenant SAAS, and production teams

📚 Documentation
/docs/index.md
# SerialMemory MCP Documentation

Welcome to the docs for the SerialMemory MCP client.

## Contents
- Tool API
- Configuration
- MCP Integration (Claude, Cursor, Kilo Code)
- Security Notes

/docs/tools.md
# Public MCP Tools

## memory.search
Hybrid vector + keyword search.

## memory.add
Insert a structured memory.

## memory.list_recent
Recent created memories.

## session.list
Conversation sessions.

## graph.query
Entity graph queries.

📦 Dockerfile
FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY . .
ENTRYPOINT ["./serialmemory-mcp"]

🗒 CHANGELOG
v1.0.0 — Initial Release (2025-11-29)

Complete MCP specification support

Lightweight proxy-only architecture

Claude/Cursor/Kilo Code support

API-key authentication

Docker image

Cross-platform binaries

📣 Support

Issues: https://github.com/sblanchard/SerialMemory-MCP/issues

Website: https://serialmemory.dev

Email: support@serialmemory.dev

📜 License

MIT License.








