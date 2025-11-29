namespace SerialMemory.Mcp.Tools;

/// <summary>
/// MCP tool definitions (schemas only) for the SerialMemory API.
/// This file contains NO implementation logic - just tool schemas for MCP discovery.
/// All tool calls are forwarded to SerialMemory.Api.
/// </summary>
public static class ToolDefinitions
{
    /// <summary>
    /// Returns all tool definitions for MCP tools/list response.
    /// </summary>
    public static object[] GetAllTools() =>
    [
        .. GetCoreTools(),
        .. GetLifecycleTools(),
        .. GetObservabilityTools(),
        .. GetSafetyTools(),
        .. GetExportTools(),
        .. GetReasoningTools()
    ];

    public static object[] GetCoreTools() =>
    [
        // memory_search
        new
        {
            name = "memory_search",
            description = "Search for relevant memories using semantic search, full-text search, or both. Returns memories with entities and temporal context.",
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
                    extract_entities = new { type = "boolean", @default = true, description = "Whether to extract entities and relationships" }
                },
                required = new[] { "content" }
            }
        },
        // memory_about_user
        new
        {
            name = "memory_about_user",
            description = "Retrieve structured information about the user's persona, preferences, skills, goals, and background.",
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
        // memory_multi_hop_search
        new
        {
            name = "memory_multi_hop_search",
            description = "Perform multi-hop reasoning by traversing the knowledge graph. Finds initial memories, then follows entity relationships.",
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

    public static object[] GetLifecycleTools() =>
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
        }
    ];

    public static object[] GetObservabilityTools() =>
    [
        new
        {
            name = "memory_trace",
            description = "Get complete event history for a memory. Shows all mutations in chronological order.",
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

    public static object[] GetSafetyTools() =>
    [
        new
        {
            name = "detect_contradictions",
            description = "Find memories that contradict each other using semantic similarity and content analysis.",
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

    public static object[] GetExportTools() =>
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
        }
    ];

    public static object[] GetReasoningTools() =>
    [
        new
        {
            name = "engineering_analyze",
            description = "Analyze the knowledge graph for engineering insights. Detects power integrity issues (voltage mismatch, overcurrent), signal integrity issues (clock/protocol mismatch), dependency corruption (cascading failures), and thermal risks.",
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
}
