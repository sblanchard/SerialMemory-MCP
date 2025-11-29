# 🚀 Getting Started with SerialMemory

SerialMemory is your AI's long-term memory system — a temporal knowledge graph with reasoning, versions, timelines, mutations, and full MCP (Model Context Protocol) support.

Follow these steps to start ingesting and retrieving memories in under 30 seconds.

---

## 1️⃣ Create an Account

Sign up at:

👉 **https://app.serialmemory.dev**

Every new account gets:
- A dedicated **tenant**
- A **workspace**
- A **private API key**
- Usage dashboard + memory explorer

---

## 2️⃣ Get Your API Key

After signup, go to:

👉 **Dashboard → API Keys**

Your key will look like:

```
sm_live_xxxxxxxxxxxxxxxxxxxxxxxxx
```

This key is required for:
- HTTP API access  
- MCP tool calls  
- Claude Desktop, Cursor, Kilo Code integration  

Keep it secret.

---

## 3️⃣ Ingest Your First Memory (HTTP API)

Use your API key with the MCP tool endpoint:

```bash
curl -X POST https://api.serialmemory.dev/api/mcp/tools/memory_ingest \
  -H "Authorization: Bearer sm_live_yourapikey" \
  -H "Content-Type: application/json" \
  -d '{
        "content": "Hello, SerialMemory! This is my first memory."
      }'
```

Response:

```json
{
  "content": [
    {
      "type": "text",
      "text": "Memory successfully ingested (ID: mem_abc123...)"
    }
  ]
}
```

---

## 4️⃣ Search Memory

```bash
curl -X POST https://api.serialmemory.dev/api/mcp/tools/memory_search \
  -H "Authorization: Bearer sm_live_yourapikey" \
  -H "Content-Type: application/json" \
  -d '{
        "query": "hello"
      }'
```

---

## 5️⃣ Use SerialMemory with Claude Desktop (MCP)

1. Open Claude Desktop  
2. Click: **Settings → Developer → MCP**  
3. Add this entry to your JSON config:

```json
{
  "mcpServers": {
    "serialmemory": {
      "command": "serialmemory-mcp",
      "args": []
    }
  }
}
```

Your environment must include:

```
SERIALMEMORY_ENDPOINT=https://api.serialmemory.dev
SERIALMEMORY_API_KEY=sm_live_yourapikey
```

Restart Claude → It will automatically load the SerialMemory MCP tools.

---

## 6️⃣ Use SerialMemory in Cursor or Kilo Code

SerialMemory is fully MCP-compatible.

Create `.cursor/mcp.json` or `.kilocode/mcp.json`:

```json
{
  "mcpServers": {
    "serialmemory": {
      "command": "serialmemory-mcp",
      "args": []
    }
  }
}
```

Set environment variables (PowerShell example):

```powershell
setx SERIALMEMORY_ENDPOINT "https://api.serialmemory.dev"
setx SERIALMEMORY_API_KEY "sm_live_yourapikey"
```

Restart Cursor/Kilo → tools appear instantly.

---

## 7️⃣ Install the SerialMemory MCP Client (Binary)

Download from GitHub Releases:

👉 https://github.com/sblanchard/SerialMemory-MCP/releases

Available for:
- Windows  
- Linux  
- macOS (Intel & ARM)  

Then run:

```bash
serialmemory-mcp
```

The client requires only:
- `SERIALMEMORY_ENDPOINT`
- `SERIALMEMORY_API_KEY`

It contains **zero local database**, **no embeddings**, **no indexing**.  
All intelligence lives on SerialMemory Cloud.

---

## 8️⃣ View Your Memory in the Dashboard

Once you ingest a few memories, visit:

👉 https://app.serialmemory.dev/dashboard/memories

You’ll see:
- Memory items  
- Confidence  
- Entities  
- Relationships  
- Timeline + mutations  
- Graph visualization  

---

## 9️⃣ Recommended Setup for Agents

Create a `.md` file in your agent framework:

```yaml
name: memory-search
description: Use SerialMemory to recall past context, project history, and user details.
tools:
  - mcp__serialmemory__memory_search
model: sonnet
```

SerialMemory enables agents that *never forget*.

--- 
