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
```bash
export SERIALMEMORY_ENDPOINT="https://api.serialmemory.dev"
export SERIALMEMORY_API_KEY="your-api-key"
