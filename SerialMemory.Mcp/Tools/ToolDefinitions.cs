namespace SerialMemory.Mcp.Tools;

/// <summary>
/// MCP tool definitions (schemas only) for the SerialMemory API.
/// This file contains NO implementation logic - just tool schemas for MCP discovery.
/// All tool calls are forwarded to SerialMemory.Api.
/// </summary>
public static class ToolDefinitions
{
    /// <summary>
    /// Returns all tool definitions for MCP tools/list response (full mode).
    /// </summary>
    public static object[] GetAllTools() =>
    [
        .. GetCoreTools(),
        .. GetLifecycleTools(),
        .. GetObservabilityTools(),
        .. GetSafetyTools(),
        .. GetExportTools(),
        .. GetReasoningTools(),
        .. GetWorkspaceTools(),
        .. GetGatewayTools()
    ];

    /// <summary>
    /// Returns only core tools + meta-tools for lazy-MCP mode.
    /// Saves ~84% of tool listing overhead.
    /// </summary>
    public static object[] GetLazyTools()
    {
        var core = GetCoreTools();
        var lazyToolNames = new HashSet<string> { "memory_search", "memory_ingest", "memory_multi_hop_search", "memory_about_user" };
        var lazyCore = core.Where(t => lazyToolNames.Contains(((dynamic)t).name)).ToArray();
        return [.. lazyCore, .. GetMetaTools()];
    }

    public static object[] GetMetaTools() =>
    [
        new
        {
            name = "get_tools_in_category",
            description = "Browse available SerialMemory tools by category. Call with no path for root categories. Categories: lifecycle, observability, safety, export, reasoning, session, admin, workspace.",
            annotations = ReadOnly,
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    path = new { type = "string", description = "Category path (empty for root, e.g. 'lifecycle', 'safety')" }
                }
            }
        },
        new
        {
            name = "execute_tool",
            description = "Execute a SerialMemory tool by its category path. Use get_tools_in_category first to discover tools and their parameters.",
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    tool_path = new { type = "string", description = "Tool path (e.g. 'lifecycle.memory_update', 'safety.detect_contradictions')" },
                    arguments = new { type = "object", description = "Tool arguments as JSON object" }
                },
                required = new[] { "tool_path" }
            }
        }
    ];

    /// <summary>
    /// Category metadata for lazy-MCP browsing.
    /// </summary>
    public static readonly Dictionary<string, (string Title, string Description)> Categories = new()
    {
        ["lifecycle"] = ("Memory Lifecycle", "Update, delete, merge, split, decay, reinforce, expire, supersede memories"),
        ["observability"] = ("Observability", "Trace event history, lineage, explain state, find conflicts"),
        ["safety"] = ("Safety & Integrity", "Detect contradictions, hallucinations, verify hashes, scan loops"),
        ["export"] = ("Export", "Export workspace, memories, graph, user profile, markdown vault"),
        ["reasoning"] = ("Engineering Reasoning", "Analyze graphs, visualize, multi-model reasoning"),
        ["session"] = ("Session Management", "Create/end sessions, instantiate context"),
        ["admin"] = ("Administration", "Persona, integrations, import, crawl, statistics, model info, reembed"),
        ["workspace"] = ("Workspace & Snapshots", "Create/switch workspaces, create/load state snapshots")
    };

    /// <summary>
    /// Map category.tool_name → actual tool name for dispatch.
    /// </summary>
    public static readonly Dictionary<string, string> ToolMap = new()
    {
        ["lifecycle.memory_update"] = "memory_update",
        ["lifecycle.memory_delete"] = "memory_delete",
        ["lifecycle.memory_merge"] = "memory_merge",
        ["lifecycle.memory_split"] = "memory_split",
        ["lifecycle.memory_decay"] = "memory_decay",
        ["lifecycle.memory_reinforce"] = "memory_reinforce",
        ["lifecycle.memory_expire"] = "memory_expire",
        ["lifecycle.memory_supersede"] = "memory_supersede",
        ["observability.memory_trace"] = "memory_trace",
        ["observability.memory_lineage"] = "memory_lineage",
        ["observability.memory_explain"] = "memory_explain",
        ["observability.memory_conflicts"] = "memory_conflicts",
        ["safety.detect_contradictions"] = "detect_contradictions",
        ["safety.detect_hallucinations"] = "detect_hallucinations",
        ["safety.verify_memory_integrity"] = "verify_memory_integrity",
        ["safety.scan_loops"] = "scan_loops",
        ["export.export_workspace"] = "export_workspace",
        ["export.export_memories"] = "export_memories",
        ["export.export_graph"] = "export_graph",
        ["export.export_user_profile"] = "export_user_profile",
        ["export.export_markdown"] = "export_markdown",
        ["reasoning.engineering_analyze"] = "engineering_analyze",
        ["reasoning.engineering_visualize"] = "engineering_visualize",
        ["reasoning.engineering_reason"] = "engineering_reason",
        ["session.initialise_conversation_session"] = "initialise_conversation_session",
        ["session.end_conversation_session"] = "end_conversation_session",
        ["session.instantiate_context"] = "instantiate_context",
        ["admin.set_user_persona"] = "set_user_persona",
        ["admin.get_integrations"] = "get_integrations",
        ["admin.import_from_core"] = "import_from_core",
        ["admin.crawl_relationships"] = "crawl_relationships",
        ["admin.get_graph_statistics"] = "get_graph_statistics",
        ["admin.get_model_info"] = "get_model_info",
        ["admin.reembed_memories"] = "reembed_memories",
        ["workspace.workspace_create"] = "workspace_create",
        ["workspace.workspace_list"] = "workspace_list",
        ["workspace.workspace_switch"] = "workspace_switch",
        ["workspace.snapshot_create"] = "snapshot_create",
        ["workspace.snapshot_list"] = "snapshot_list",
        ["workspace.snapshot_load"] = "snapshot_load"
    };

    /// <summary>
    /// Returns tool definitions for a given category.
    /// </summary>
    public static object[] GetToolsForCategory(string category) => category.ToLowerInvariant() switch
    {
        "lifecycle" => GetLifecycleTools(),
        "observability" => GetObservabilityTools(),
        "safety" => GetSafetyTools(),
        "export" => GetExportTools(),
        "reasoning" => GetReasoningTools(),
        "session" => FilterByName(GetCoreTools(),
            "initialise_conversation_session", "end_conversation_session", "instantiate_context"),
        "admin" => FilterByName(GetCoreTools(),
            "set_user_persona", "get_integrations", "import_from_core",
            "crawl_relationships", "get_graph_statistics", "get_model_info", "reembed_memories"),
        "workspace" => GetWorkspaceTools(),
        _ => []
    };

    private static object[] FilterByName(object[] tools, params string[] names)
    {
        var nameSet = new HashSet<string>(names);
        return tools.Where(t => nameSet.Contains(((dynamic)t).name)).ToArray();
    }

    // Annotation helpers
    private static object ReadOnly => new { readOnlyHint = true };
    private static object Destructive => new { destructiveHint = true };

    private static object[] GetCoreTools() =>
    [
        // memory_search
        new
        {
            name = "memory_search",
            description = "Search for relevant memories using semantic search, full-text search, or both. Returns memories with entities and temporal context.",
            annotations = ReadOnly,
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    query = new { type = "string", description = "Search query (natural language)" },
                    mode = new { type = "string", @enum = new[] { "semantic", "text", "hybrid" }, @default = "hybrid", description = "Search mode" },
                    limit = new { type = "integer", @default = 10, description = "Maximum results to return" },
                    threshold = new { type = "number", @default = 0.7, description = "Minimum similarity threshold (0.0-1.0)" },
                    include_entities = new { type = "boolean", @default = true, description = "Include linked entities" }
                },
                required = new[] { "query" }
            }
        },
        // memory_ingest
        new
        {
            name = "memory_ingest",
            description = "Add a new memory (episode) to the knowledge graph. Automatically extracts entities, relationships, and generates embeddings.",
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    content = new { type = "string", description = "Memory content to store" },
                    source = new { type = "string", description = "Source of the memory (e.g., 'claude-desktop', 'cursor')" },
                    metadata = new { type = "object", description = "Additional metadata (tags, importance, etc.)" },
                    extract_entities = new { type = "boolean", @default = true, description = "Whether to extract entities and relationships" },
                    dedup_mode = new { type = "string", @enum = new[] { "warn", "skip", "append", "off" }, @default = "warn", description = "Dedup mode: warn (create+report), skip (reject if dup), append (merge into existing), off (no check)" },
                    dedup_threshold = new { type = "number", @default = 0.85, description = "Similarity threshold for duplicate detection (0.0-1.0)" }
                },
                required = new[] { "content" }
            }
        },
        // memory_about_user
        new
        {
            name = "memory_about_user",
            description = "Retrieve structured information about the user's persona, preferences, skills, goals, and background.",
            annotations = ReadOnly,
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    user_id = new { type = "string", @default = "default_user", description = "User identifier" }
                }
            }
        },
        // initialise_conversation_session
        new
        {
            name = "initialise_conversation_session",
            description = "Create a new conversation session to track context across interactions.",
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    session_name = new { type = "string", description = "Optional session name/title" },
                    client_type = new { type = "string", description = "Client type (e.g., 'claude-desktop', 'cursor')" },
                    metadata = new { type = "object", description = "Additional session metadata" }
                }
            }
        },
        // end_conversation_session
        new
        {
            name = "end_conversation_session",
            description = "End the current conversation session.",
            inputSchema = new
            {
                type = "object",
                properties = new { }
            }
        },
        // instantiate_context
        new
        {
            name = "instantiate_context",
            description = "Retrieve and summarize memories from the previous day(s) to continue where you left off. Use at the start of a new session to get context from prior work. Optionally filter by project or subject for relevant context only.",
            annotations = ReadOnly,
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    project_or_subject = new { type = "string", description = "Optional project name or subject to filter memories (e.g., 'FlexPilot', 'waterfall rendering'). Uses semantic search to find relevant memories." },
                    days_back = new { type = "integer", @default = 3, description = "Number of days to look back (default: 3)" },
                    limit = new { type = "integer", @default = 50, description = "Maximum memories to retrieve" },
                    include_entities = new { type = "boolean", @default = true, description = "Include linked entities and relationships" }
                }
            }
        },
        // memory_multi_hop_search
        new
        {
            name = "memory_multi_hop_search",
            description = "Perform multi-hop reasoning by traversing the knowledge graph. Finds initial memories, then follows entity relationships.",
            annotations = ReadOnly,
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    query = new { type = "string", description = "Initial search query" },
                    hops = new { type = "integer", @default = 2, description = "Number of relationship hops to traverse" },
                    max_results_per_hop = new { type = "integer", @default = 5, description = "Maximum results per hop" }
                },
                required = new[] { "query" }
            }
        },
        // get_integrations
        new
        {
            name = "get_integrations",
            description = "List available integrations (external tools/APIs).",
            annotations = ReadOnly,
            inputSchema = new
            {
                type = "object",
                properties = new { }
            }
        },
        // import_from_core
        new
        {
            name = "import_from_core",
            description = "Import entities, relations, and observations from CORE MCP export format. Provide JSON with 'entities' array (each with name, entityType, observations[]) and 'relations' array (each with from, to, relationType).",
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    data = new
                    {
                        type = "object",
                        description = "CORE export data with 'entities' and 'relations' arrays",
                        properties = new
                        {
                            entities = new
                            {
                                type = "array",
                                items = new
                                {
                                    type = "object",
                                    properties = new
                                    {
                                        name = new { type = "string" },
                                        entityType = new { type = "string" },
                                        observations = new { type = "array", items = new { type = "string" } }
                                    },
                                    required = new[] { "name" }
                                }
                            },
                            relations = new
                            {
                                type = "array",
                                items = new
                                {
                                    type = "object",
                                    properties = new
                                    {
                                        from = new { type = "string" },
                                        to = new { type = "string" },
                                        relationType = new { type = "string" }
                                    },
                                    required = new[] { "from", "to", "relationType" }
                                }
                            }
                        }
                    },
                    source = new { type = "string", @default = "core-import", description = "Source identifier for imported data" }
                },
                required = new[] { "data" }
            }
        },
        // set_user_persona
        new
        {
            name = "set_user_persona",
            description = "Set or update a user persona attribute (preference, skill, goal, background).",
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    attribute_type = new { type = "string", description = "Type: preference, skill, goal, background" },
                    attribute_key = new { type = "string", description = "Attribute name (e.g., 'programming_language')" },
                    attribute_value = new { type = "string", description = "Attribute value" },
                    confidence = new { type = "number", @default = 1.0, description = "Confidence score (0.0-1.0)" },
                    user_id = new { type = "string", @default = "default_user", description = "User identifier" }
                },
                required = new[] { "attribute_type", "attribute_key", "attribute_value" }
            }
        },
        // crawl_relationships
        new
        {
            name = "crawl_relationships",
            description = "Crawl existing memories to extract entities and relationships. Useful for populating the knowledge graph from memories that were ingested without entity extraction.",
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    batch_size = new { type = "integer", @default = 100, description = "Number of memories to process" },
                    force_reprocess = new { type = "boolean", @default = false, description = "Reprocess memories that already have entities" }
                }
            }
        },
        // get_graph_statistics
        new
        {
            name = "get_graph_statistics",
            description = "Get statistics about the knowledge graph including entity and relationship counts by type.",
            annotations = ReadOnly,
            inputSchema = new
            {
                type = "object",
                properties = new { }
            }
        },
        // get_model_info
        new
        {
            name = "get_model_info",
            description = "Get information about the current embedding model (name, dimensions, supported models, export instructions).",
            annotations = ReadOnly,
            inputSchema = new
            {
                type = "object",
                properties = new { }
            }
        },
        // reembed_memories
        new
        {
            name = "reembed_memories",
            description = "Re-generate embeddings for memories. Use after switching to a different embedding model. By default only re-embeds memories with null embeddings.",
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    force_all = new { type = "boolean", @default = false, description = "Re-embed ALL memories, not just those with null embeddings" },
                    batch_size = new { type = "integer", @default = 100, description = "Number of memories to process" }
                }
            }
        }
    ];

    private static object[] GetLifecycleTools() =>
    [
        new
        {
            name = "memory_update",
            description = "Update memory content with new embedding. Creates MemoryUpdated event. Does not mutate original - creates new version.",
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    memory_id = new { type = "string", description = "UUID of memory to update" },
                    new_content = new { type = "string", description = "New content to replace existing" },
                    reason = new { type = "string", description = "Reason for update (audit trail)" },
                    actor_id = new { type = "string", description = "ID of actor making the update" }
                },
                required = new[] { "memory_id", "new_content" }
            }
        },
        new
        {
            name = "memory_delete",
            description = "Soft delete (invalidate) a memory. No hard deletes - memory remains for audit. Creates MemoryInvalidated event.",
            annotations = Destructive,
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    memory_id = new { type = "string", description = "UUID of memory to soft delete" },
                    reason = new { type = "string", description = "Reason for deletion (required for audit)" },
                    superseded_by_id = new { type = "string", description = "UUID of memory that supersedes this one" },
                    actor_id = new { type = "string", description = "ID of actor making the deletion" }
                },
                required = new[] { "memory_id", "reason" }
            }
        },
        new
        {
            name = "memory_merge",
            description = "Merge multiple memories into a single new memory. Source memories are soft deleted. Creates new memory with causal parents.",
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    source_memory_ids = new { type = "array", items = new { type = "string" }, description = "UUIDs of memories to merge (min 2)" },
                    merged_content = new { type = "string", description = "Combined content for new memory" },
                    strategy = new { type = "string", description = "Merge strategy (e.g., 'concatenate', 'summarize', 'manual')" },
                    actor_id = new { type = "string", description = "ID of actor performing merge" }
                },
                required = new[] { "source_memory_ids", "merged_content" }
            }
        },
        new
        {
            name = "memory_split",
            description = "Split a memory into multiple child memories. Parent is marked as split (inactive). Children reference parent as causal parent.",
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    memory_id = new { type = "string", description = "UUID of memory to split" },
                    child_contents = new { type = "array", items = new { type = "string" }, description = "Content for each child memory (min 2)" },
                    strategy = new { type = "string", description = "Split strategy (e.g., 'semantic', 'temporal', 'manual')" },
                    reason = new { type = "string", description = "Reason for split" },
                    actor_id = new { type = "string", description = "ID of actor performing split" }
                },
                required = new[] { "memory_id", "child_contents" }
            }
        },
        new
        {
            name = "memory_decay",
            description = "Apply time-based confidence decay to a memory using exponential decay formula: confidence * 0.5^(days/half_life).",
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    memory_id = new { type = "string", description = "UUID of memory to decay" },
                    actor_id = new { type = "string", description = "ID of actor/system applying decay" }
                },
                required = new[] { "memory_id" }
            }
        },
        new
        {
            name = "memory_reinforce",
            description = "Reinforce a memory - reset decay timer and optionally boost confidence. Use when memory is validated or frequently accessed.",
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    memory_id = new { type = "string", description = "UUID of memory to reinforce" },
                    confidence = new { type = "number", @default = 1.0, description = "New confidence score (0.0-1.0)" },
                    source = new { type = "string", @default = "manual", description = "Source of reinforcement (e.g., 'user_validation', 'frequent_access')" },
                    validated_by_ids = new { type = "array", items = new { type = "string" }, description = "UUIDs of validating memories" },
                    actor_id = new { type = "string", description = "ID of actor performing reinforcement" }
                },
                required = new[] { "memory_id" }
            }
        },
        new
        {
            name = "memory_expire",
            description = "Expire a memory based on TTL policy. Different from decay - this is a hard cutoff. Memory becomes inactive.",
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    memory_id = new { type = "string", description = "UUID of memory to expire" },
                    policy = new { type = "string", @default = "manual", description = "Expiration policy name" },
                    ttl_days = new { type = "integer", description = "Original TTL in days (for audit)" },
                    actor_id = new { type = "string", description = "ID of actor/system expiring memory" }
                },
                required = new[] { "memory_id" }
            }
        },
        // memory_supersede
        new
        {
            name = "memory_supersede",
            description = "Replace a memory with new content. Creates new memory, invalidates old, links via causal_parents and superseded_by.",
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    old_memory_id = new { type = "string", description = "UUID of memory to supersede" },
                    new_content = new { type = "string", description = "New replacement content" },
                    reason = new { type = "string", description = "Why superseding" },
                    extract_entities = new { type = "boolean", @default = true, description = "Extract entities from new content" },
                    actor_id = new { type = "string", description = "Actor ID" }
                },
                required = new[] { "old_memory_id", "new_content" }
            }
        }
    ];

    private static object[] GetObservabilityTools() =>
    [
        new
        {
            name = "memory_trace",
            description = "Get complete event history for a memory. Shows all mutations in chronological order.",
            annotations = ReadOnly,
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    memory_id = new { type = "string", description = "UUID of memory to trace" },
                    include_payloads = new { type = "boolean", @default = false, description = "Include full event payloads" }
                },
                required = new[] { "memory_id" }
            }
        },
        new
        {
            name = "memory_lineage",
            description = "Trace causal ancestry and descendants of a memory through causal_parents relationships.",
            annotations = ReadOnly,
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    memory_id = new { type = "string", description = "UUID of memory to trace lineage" },
                    max_depth = new { type = "integer", @default = 5, description = "Maximum depth to traverse (1-10)" },
                    direction = new { type = "string", @enum = new[] { "ancestors", "descendants", "both" }, @default = "ancestors", description = "Direction to trace" }
                },
                required = new[] { "memory_id" }
            }
        },
        new
        {
            name = "memory_explain",
            description = "Explain current state of a memory - why it's active/inactive, confidence calculations, relationships, and recommendations.",
            annotations = ReadOnly,
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    memory_id = new { type = "string", description = "UUID of memory to explain" }
                },
                required = new[] { "memory_id" }
            }
        },
        new
        {
            name = "memory_conflicts",
            description = "Find all conflicts/contradictions involving a memory or list all unresolved conflicts.",
            annotations = ReadOnly,
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    memory_id = new { type = "string", description = "UUID of memory to check (optional - if omitted, returns all unresolved)" },
                    limit = new { type = "integer", @default = 50, description = "Maximum conflicts to return" }
                }
            }
        }
    ];

    private static object[] GetSafetyTools() =>
    [
        new
        {
            name = "detect_contradictions",
            description = "Find memories that contradict each other using semantic similarity and content analysis.",
            annotations = ReadOnly,
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    memory_id = new { type = "string", description = "UUID to check for contradictions (optional - if omitted, batch scan)" },
                    similarity_threshold = new { type = "number", @default = 0.85, description = "Minimum similarity to consider (0.5-0.99)" },
                    limit = new { type = "integer", @default = 20, description = "Maximum contradictions to return" },
                    auto_flag = new { type = "boolean", @default = false, description = "Automatically flag detected contradictions in database" }
                }
            }
        },
        new
        {
            name = "detect_hallucinations",
            description = "Flag potential hallucinations based on confidence, validation status, access patterns, and isolation.",
            annotations = ReadOnly,
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    memory_id = new { type = "string", description = "UUID to check (optional - if omitted, batch scan)" },
                    confidence_threshold = new { type = "number", @default = 0.3, description = "Flag memories below this confidence" },
                    limit = new { type = "integer", @default = 20, description = "Maximum results to return" },
                    auto_flag = new { type = "boolean", @default = false, description = "Automatically flag in database" }
                }
            }
        },
        new
        {
            name = "verify_memory_integrity",
            description = "Verify content hash integrity for memories. Detects content corruption.",
            annotations = ReadOnly,
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    memory_id = new { type = "string", description = "UUID to verify (optional - if omitted, batch verify)" },
                    limit = new { type = "integer", @default = 100, description = "Maximum memories to check" },
                    fix_corrupted = new { type = "boolean", @default = false, description = "Automatically recompute hashes for corrupted entries" }
                }
            }
        },
        new
        {
            name = "scan_loops",
            description = "Detect cycles in causal parent relationships (loop detection). Cycles can cause infinite recursion.",
            annotations = ReadOnly,
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    max_depth = new { type = "integer", @default = 10, description = "Maximum depth to search (1-20)" },
                    limit = new { type = "integer", @default = 50, description = "Maximum loops to return" }
                }
            }
        }
    ];

    private static object[] GetExportTools() =>
    [
        new
        {
            name = "export_workspace",
            description = "Export entire workspace - memories, entities, relationships, and optionally events. Supports encryption and compression.",
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    output_path = new { type = "string", description = "Output file path (default: workspace_export_YYYYMMDD.json)" },
                    include_events = new { type = "boolean", @default = false, description = "Include raw event store data" },
                    active_only = new { type = "boolean", @default = true, description = "Only export active memories" },
                    encrypt = new { type = "boolean", @default = false, description = "AES-256 encrypt the export" },
                    encryption_key = new { type = "string", description = "Encryption key (required if encrypt=true)" },
                    compress = new { type = "boolean", @default = false, description = "GZip compress the export" }
                }
            }
        },
        new
        {
            name = "export_memories",
            description = "Export memories with filters. Supports JSON and CSV formats.",
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    output_path = new { type = "string", description = "Output file path" },
                    layer = new { type = "string", @enum = new[] { "L0_RAW", "L1_CONTEXT", "L2_SUMMARY", "L3_KNOWLEDGE", "L4_HEURISTIC" }, description = "Filter by layer" },
                    min_confidence = new { type = "number", description = "Minimum confidence filter (0.0-1.0)" },
                    from_date = new { type = "string", description = "Start date filter (ISO 8601)" },
                    to_date = new { type = "string", description = "End date filter (ISO 8601)" },
                    limit = new { type = "integer", @default = 10000, description = "Maximum memories to export" },
                    format = new { type = "string", @enum = new[] { "json", "csv" }, @default = "json", description = "Output format" }
                }
            }
        },
        new
        {
            name = "export_graph",
            description = "Export knowledge graph (entities and relationships). Supports JSON, GraphML, and Cytoscape formats.",
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    output_path = new { type = "string", description = "Output file path" },
                    format = new { type = "string", @enum = new[] { "json", "graphml", "cytoscape" }, @default = "json", description = "Output format" },
                    include_isolated = new { type = "boolean", @default = false, description = "Include entities with no relationships" }
                }
            }
        },
        new
        {
            name = "export_user_profile",
            description = "Export user persona attributes and memory statistics.",
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    user_id = new { type = "string", @default = "default_user", description = "User ID to export" },
                    output_path = new { type = "string", description = "Output file path" },
                    include_interactions = new { type = "boolean", @default = false, description = "Include interaction history" }
                }
            }
        },
        // export_markdown
        new
        {
            name = "export_markdown",
            description = "Export as Obsidian-compatible Markdown vault with wikilinks, YAML frontmatter, and folder organization.",
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    output_path = new { type = "string", description = "Output directory (default: serial_memory_vault)" },
                    active_only = new { type = "boolean", @default = true, description = "Only export active memories" },
                    include_entities = new { type = "boolean", @default = true, description = "Include entity pages" },
                    include_sessions = new { type = "boolean", @default = true, description = "Include session summaries" },
                    min_confidence = new { type = "number", @default = 0.0, description = "Minimum confidence filter (0.0-1.0)" },
                    group_by = new { type = "string", @enum = new[] { "month", "layer", "source" }, @default = "month", description = "How to group memory files" }
                }
            }
        }
    ];

    private static object[] GetReasoningTools() =>
    [
        new
        {
            name = "engineering_analyze",
            description = "Analyze the knowledge graph for engineering insights. Detects power integrity issues (voltage mismatch, overcurrent), signal integrity issues (clock/protocol mismatch), dependency corruption (cascading failures), and thermal risks.",
            annotations = ReadOnly,
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    memory_id = new { type = "string", description = "Optional: analyze entities related to this memory" },
                    project = new { type = "string", description = "Optional: filter analysis to entities connected to this project name" }
                }
            }
        },
        new
        {
            name = "engineering_visualize",
            description = "Generate graph visualization data with nodes, links, and reasoning overlays. Returns JSON suitable for react-force-graph-3d rendering.",
            annotations = ReadOnly,
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    memory_id = new { type = "string", description = "Optional: visualize entities related to this memory" },
                    project = new { type = "string", description = "Optional: filter to entities connected to this project name" },
                    mode = new { type = "string", @enum = new[] { "software", "hardware", "mixed" }, @default = "mixed", description = "Visualization mode filter" },
                    include_overlays = new { type = "boolean", @default = true, description = "Include reasoning-based risk/warning overlays" }
                }
            }
        },
        new
        {
            name = "engineering_reason",
            description = "Run multi-model reasoning on the knowledge graph. Executes multiple reasoning models in parallel (Structural, Risk, Optimization, Contradiction) and merges results by confidence and agreement. Returns traced insights with source model attribution.",
            annotations = ReadOnly,
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    memory_id = new { type = "string", description = "Optional: reason over entities related to this memory" },
                    project = new { type = "string", description = "Optional: filter reasoning to entities connected to this project name" },
                    max_duration_ms = new { type = "integer", @default = 30000, description = "Maximum reasoning time in milliseconds (default: 30000)" }
                }
            }
        }
    ];

    private static object[] GetWorkspaceTools() =>
    [
        new
        {
            name = "workspace_create",
            description = "Create a new workspace for scoping memories and sessions.",
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    name = new { type = "string", description = "Workspace slug identifier (e.g., 'my-project')" },
                    display_name = new { type = "string", description = "Human-readable display name" },
                    description = new { type = "string", description = "Workspace description" }
                },
                required = new[] { "name" }
            }
        },
        new
        {
            name = "workspace_list",
            description = "List all workspaces for the current tenant.",
            annotations = ReadOnly,
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    limit = new { type = "integer", @default = 50, description = "Maximum workspaces to return" }
                }
            }
        },
        new
        {
            name = "workspace_switch",
            description = "Switch the active workspace for this MCP session. All subsequent operations will be scoped to the new workspace.",
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    workspace_id = new { type = "string", description = "Workspace slug to switch to" }
                },
                required = new[] { "workspace_id" }
            }
        },
        new
        {
            name = "snapshot_create",
            description = "Create a named state snapshot of the current workspace. Captures recent memories, active entities, session state, and custom metadata.",
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    snapshot_name = new { type = "string", description = "Unique name for this snapshot (e.g., 'checkpoint-1')" },
                    goal = new { type = "string", description = "Current goal to capture" },
                    constraints = new { type = "string", description = "Current constraints to capture" },
                    memory = new { type = "string", description = "Conversation essence to capture" },
                    metadata = new { type = "object", description = "Custom metadata to include" }
                },
                required = new[] { "snapshot_name" }
            }
        },
        new
        {
            name = "snapshot_list",
            description = "List snapshots for a workspace.",
            annotations = ReadOnly,
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    workspace_id = new { type = "string", description = "Workspace to list snapshots for (defaults to current)" },
                    limit = new { type = "integer", @default = 20, description = "Maximum snapshots to return" }
                }
            }
        },
        new
        {
            name = "snapshot_load",
            description = "Load a named snapshot and return its captured state data for context restoration.",
            annotations = ReadOnly,
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    snapshot_name = new { type = "string", description = "Name of the snapshot to load" },
                    workspace_id = new { type = "string", description = "Workspace to load from (defaults to current)" }
                },
                required = new[] { "snapshot_name" }
            }
        }
    ];

    private static object[] GetGatewayTools() =>
    [
        new
        {
            name = "get_tools",
            description = "Discover available SerialMemory tools by category. Returns tool schemas and descriptions. Categories: lifecycle, observability, safety, export, reasoning, admin, session, workspace.",
            annotations = ReadOnly,
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    category = new { type = "string", description = "Filter by category (omit for category listing)" }
                }
            }
        },
        new
        {
            name = "use_tool",
            description = "Execute a SerialMemory tool by name. Use get_tools first to discover available tools and their parameters.",
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    tool_name = new { type = "string", description = "Name of the tool to execute" },
                    arguments = new { type = "object", description = "Tool arguments" },
                    context = new
                    {
                        type = "object",
                        description = "Optional per-call context envelope",
                        properties = new
                        {
                            workspace_id = new { type = "string", description = "Override workspace for this call" },
                            session_id = new { type = "string", description = "Override session for this call" },
                            memory = new { type = "string", description = "1-3 sentence conversation essence" },
                            goal = new { type = "string", description = "Current objective" },
                            constraints = new { type = "string", description = "Rules or limits" }
                        }
                    }
                },
                required = new[] { "tool_name" }
            }
        }
    ];
}
