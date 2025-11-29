# SerialMemory MCP Client

A lightweight, open-source **Model Context Protocol (MCP)** client for SerialMemory.  
This MCP exposes your SerialMemory workspace to AI models (Claude, Cursor, VS Code agents, etc.).

The MCP Client **stores no data**, contains **zero business logic**, and simply proxies requests from your AI assistant → to your SerialMemory account → and returns results.

---

## ✨ Features

- **Pure MCP Spec Implementation**  
  Works with any MCP-compatible model (Claude Desktop, ChatGPT MCP, Cursor AI, Cline, Windsurf).

- **Zero Backend Logic**  
  This client performs *no database reads, no Npgsql, no Redis, no Dapper*.  
  All logic executes on your SerialMemory backend via secure API.

- **Secure API Forwarding**  
  - Sends MCP tool calls to:  
    `POST /api/mcp/tools/{toolName}`
  - Includes your API key in every request.
  - Validates structured request/response format.

- **Simple Docker Deployment**  
  Standard:  
  ```sh
  docker run -it \
    -e SERIALMEMORY_API_KEY=your-key \
    ghcr.io/sblanchard/serialmemory-mcp
