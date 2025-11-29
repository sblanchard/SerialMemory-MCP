# 🚀 SerialMemory Developer Agents  
*Deep contextual coding assistants powered by the SerialMemory MCP backend.*

SerialMemory Agents extend Claude Code / Cursor / Kilo developer environments with **real memory**, enabling:

- Long-term context persistence  
- Multi-session project recall  
- File + architecture lineage  
- Self-coherent reasoning  
- Full semantic search  
- Multi-hop graph queries  
- Memory reinforcement + drift handling  
- Seamless integration into your coding IDE  

This page explains how to install, configure, and use the two official development agents:

1. **serialmemory-context-agent** (automatic memory search)
2. **serialmemory-writer-agent** (automatic memory ingestion / write-back)

---

# 📦 Prerequisites

Before using the agents:

1. **Create a SerialMemory account**  
2. **Get your API key**  
3. **Install the SerialMemory MCP client**  
   (Docker, Windows binary, or `cargo run` options available)

Learn more: https://serialmemory.dev

---

# 🧠 1. SerialMemory Context Agent  
> “Never lose track of your project again.”

This agent *always* uses SerialMemory to load context before answering.

Place this file in your agent directory  
(e.g. `~/claude/agents/serialmemory-context-agent.md`):

```markdown
---
name: serialmemory-context-agent
description: >
  A memory-first coding & reasoning agent that automatically retrieves
  context from SerialMemory before answering any request.
color: cyan
icon: database
model: sonnet
tools:
  - mcp__serialmemory-memory__memory_search
  - mcp__serialmemory-memory__memory_multi_hop_search
  - mcp__serialmemory-memory__memory_about_user
  - mcp__serialmemory-memory__memory_lineage
  - mcp__serialmemory-memory__memory_trace
---

# SERIALMEMORY CONTEXT AGENT — RULES

1. Always run a memory search before responding.
2. Always integrate memory results into the solution.
3. Use multi-hop search for project lineage or multi-file reasoning.
4. If no memory exists, state that explicitly.
5. Never invent past context.
6. Maintain continuity across coding sessions.
```

### What this agent does
When you prompt the agent:

> “Continue working on the FlexPilot UI”

It automatically executes:

- `memory_search` → finds your last FlexPilot work  
- `memory_lineage` → finds related decisions  
- `memory_trace` → retrieves engineering history  
- `memory_multi_hop_search` → expands dependencies  

Then it produces a coherent continuation.

---

# ✍️ 2. SerialMemory Writer Agent  
> “Your IDE’s automatic historian.”

This agent writes memories back to SerialMemory:

- Code files  
- Architecture decisions  
- Explanations  
- Branch notes  
- Bug reports  
- Upgrade paths  
- Reasoning steps  
- Session conclusions  

Place this file in `serialmemory-writer-agent.md`:

```markdown
---
name: serialmemory-writer-agent
description: >
  A specialized agent that writes coding decisions, explanations,
  and project artifacts into SerialMemory using automatic ingestion.
color: purple
icon: upload
model: sonnet
tools:
  - mcp__serialmemory-memory__memory_ingest
  - mcp__serialmemory-memory__memory_update
  - mcp__serialmemory-memory__memory_reinforce
---

# SERIALMEMORY WRITER RULES

1. Summarize the user's work into structured chunks.
2. Write stable facts as memories using memory_ingest.
3. Use memory_reinforce for repeated patterns.
4. Update existing memories when user says “revise”, “improve”, etc.
5. Never store secrets, passwords, or tokens.
6. Always include: title, content, categories, related entities.
```

### Example usage

> “Store this architectural decision”

The writer agent will ingest:

- Summary  
- Reasoning  
- Dependencies  
- Risks  
- Alternatives  
- Timestamp  

---

# 🔧 Installation Instructions

Place your agent `.md` files here depending on your IDE:

### Cursor
```
~/.cursor/agents/
```

### Claude Code
```
~/Library/Application Support/Claude/agents/
```

### Kilo
```
~/.kilo/agents/
```

### VSCode Claude MCP plugin
```
.vscode/claude/agents/
```

Restart your IDE.

---

# 🔌 Configuring MCP Tools

You must add SerialMemory MCP to your Claude Code or Cursor config.

### Cursor example (`cursor.json`):

```json
{
  "mcpServers": {
    "serialmemory": {
      "command": "docker",
      "args": ["run", "-i", "--rm",
        "-e", "SERIALMEMORY_API_KEY=your-key",
        "-e", "SERIALMEMORY_ENDPOINT=https://api.serialmemory.dev",
        "serialcoder/serialmemory-mcp"
      ]
    }
  }
}
```

### Claude Desktop example (`claude/config.yaml`):

```yaml
mcpServers:
  serialmemory:
    command: docker
    args:
      - run
      - -i
      - --rm
      - -e
      - SERIALMEMORY_API_KEY=your-key
      - -e
      - SERIALMEMORY_ENDPOINT=https://api.serialmemory.dev
      - serialcoder/serialmemory-mcp
```

---

# 🔁 Example Workflow

### 1. You start a session
> “Work on the SessionHighLows NinjaTrader refactor”

Agent auto-searches:
- memory_search("SessionHighLows")  
- multi-hop search (related indicators)  
- project lineage  
- last code version  

### 2. You code something new  
> “Improve the shadow branch logic”

Writer agent ingests structured memory:
- title: "Shadow Branch Logic v3"
- summary
- new algorithm
- performance metrics

### 3. Next day  
> “Continue yesterday’s work”

Context agent reconstructs full state.

---

# 👁️ Advanced Capabilities

### Multi-Hop Graph Reasoning
Traverse memory relationships.

### Trace Recovery
Retrieve causal event chain of previous work.

### Memory Drift Tracking
Spot when long sessions drift from earlier intentions.

### Entity + Relationship Extraction
Built into the SerialMemory backend.

---

# 🛠️ Tips for Best Results

- Use short and consistent project names  
- Ask the writer agent to “store this” after important outputs  
- Use memory search often manually for debugging  
- Keep ambiguous tasks labeled clearly (“Module X V2 Plan”)  

---

# 📚 Summary

SerialMemory developer agents turn Cursor/Claude/Kilo into a **persistent, memory-aware development environment**.

You get:

- Seamless project continuation  
- Real memory across sessions and devices  
- Reasoning lineage  
- Complete search recall  
- Powerful ingestion  
- No hallucinated context  

Your IDE becomes your second brain.

---
