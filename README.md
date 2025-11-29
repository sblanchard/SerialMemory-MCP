# SerialMemory MCP Client

[![License: MIT](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)
![.NET](https://img.shields.io/badge/.NET-10-blueviolet)
![Status](https://img.shields.io/badge/MCP-Compatible-brightgreen)
![Docker](https://img.shields.io/badge/Docker-ready-0db7ed.svg)
![Platform](https://img.shields.io/badge/Platform-Windows%20%7C%20Linux%20%7C%20Mac-informational)

A lightweight, open-source **Model Context Protocol (MCP)** client that connects
developer tools (Claude Desktop, Cursor, Windsurf, VS Code MCP extensions) to your SerialMemory backend.

This MCP client contains **no business logic**.
It is a **thin, secure HTTP proxy** that forwards tool requests to your SerialMemory Server.

---

## ✨ Features

- ⚡ **Zero-logic proxy** (forwards all MCP requests to your API)
- 🔐 **API-key secured**
- 📦 **No database access, no local AI models**
- 🐳 **Docker-ready**
- 🛠 **Works with all MCP-compatible tools**
  - Claude Desktop (**official first-class support**)
  - Cursor
  - Windsurf
  - VS Code + Cline

---

## 🚀 Quick Start

### 1. Get your SerialMemory API Key  
Sign up at:

**https://serialmemory.dev**

You will receive:  
- Email verification  
- API key  
- Developer dashboard access  

---

### 2. Run the MCP Client

```sh
docker run -it \
  -e SERIALMEMORY_ENDPOINT="https://api.serialmemory.com" \
  -e SERIALMEMORY_API_KEY="your-api-key" \
  serialcoder/serialmemory-mcp
