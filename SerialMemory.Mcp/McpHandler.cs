using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using SerialMemory.Mcp.Tools;

namespace SerialMemory.Mcp;

public sealed class McpHandler
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly bool _lazyMcpEnabled;

    public McpHandler(string apiEndpoint, string apiKey, ILogger logger, bool lazyMcpEnabled = true)
    {
        _logger = logger;
        _lazyMcpEnabled = lazyMcpEnabled;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(apiEndpoint),
            Timeout = TimeSpan.FromSeconds(120)
        };
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("SerialMemory-MCP/1.0");

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    public async Task<string?> HandleRequest(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return null;

        var req = JsonNode.Parse(line);
        var id = req?["id"];
        var method = req?["method"]?.GetValue<string>();
        var @params = req?["params"];

        _logger.LogInformation("← MCP request: {Method}", method);

        if (method == "exit")
        {
            Environment.Exit(0);
            return null;
        }

        object? result = method switch
        {
            "initialize" => new
            {
                protocolVersion = "2024-11-05",
                serverInfo = new { name = "serialmemory-mcp", version = "1.0.0" },
                capabilities = new
                {
                    tools = new { listChanged = true },
                    resources = new { listChanged = true }
                }
            },
            "notifications/initialized" => null,
            "shutdown" => null,
            "tools/list" => new { tools = _lazyMcpEnabled ? ToolDefinitions.GetLazyTools() : ToolDefinitions.GetAllTools() },
            "resources/list" => new
            {
                resources = new object[]
                {
                    new { uri = "memory://recent", name = "Recent Memories", mimeType = "application/json" },
                    new { uri = "memory://sessions", name = "Conversation Sessions", mimeType = "application/json" }
                }
            },
            "resources/read" => await ForwardToApi("resources/read", "POST", @params),
            "tools/call" => await HandleToolsCall(@params),
            _ => null
        };

        if (result == null)
            return null;

        return JsonSerializer.Serialize(new
        {
            jsonrpc = "2.0",
            id = id?.GetValue<object>(),
            result
        }, _jsonOptions);
    }

    private async Task<object> HandleToolsCall(JsonNode? @params)
    {
        var toolName = @params?["name"]?.GetValue<string>();
        var arguments = @params?["arguments"];

        if (string.IsNullOrEmpty(toolName))
            return Error("Tool name is required");

        _logger.LogInformation("→ Forwarding tool call: {Tool}", toolName);

        // Handle meta-tools locally
        if (toolName == "get_tools_in_category")
            return HandleGetToolsInCategory(arguments);

        if (toolName == "execute_tool")
            return await HandleExecuteTool(arguments);

        try
        {
            var route = GetToolRoute(toolName);
            if (route == null)
                return Error($"Unknown tool: {toolName}");

            return await ForwardToApi(route.Value.path, route.Value.method, arguments);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "API request failed for tool {Tool}", toolName);
            return Error($"API request failed: {ex.Message}");
        }
        catch (TaskCanceledException)
        {
            return Error("Request timed out");
        }
    }

    private object HandleGetToolsInCategory(JsonNode? arguments)
    {
        var path = arguments?["path"]?.GetValue<string>()?.Trim()?.ToLowerInvariant() ?? "";

        if (string.IsNullOrEmpty(path))
        {
            var text = "## SerialMemory Tool Categories\n\n";
            foreach (var (key, info) in ToolDefinitions.Categories)
            {
                var toolCount = ToolDefinitions.ToolMap.Keys.Count(k => k.StartsWith(key + "."));
                text += $"- **{key}** ({toolCount} tools) — {info.Description}\n";
            }
            text += "\nUse `get_tools_in_category` with a category name to see available tools.";
            return new { content = new[] { new { type = "text", text } } };
        }

        if (!ToolDefinitions.Categories.TryGetValue(path, out var categoryInfo))
            return Error($"Unknown category: {path}. Available: {string.Join(", ", ToolDefinitions.Categories.Keys)}");

        var tools = ToolDefinitions.GetToolsForCategory(path);
        var json = JsonSerializer.Serialize(tools, new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        return new
        {
            content = new[]
            {
                new { type = "text", text = $"## {categoryInfo.Title}\n{categoryInfo.Description}\n\n**{tools.Length} tools available.** Use `execute_tool` with path `{path}.<tool_name>` to execute.\n\n{json}" }
            }
        };
    }

    private async Task<object> HandleExecuteTool(JsonNode? arguments)
    {
        var toolPath = arguments?["tool_path"]?.GetValue<string>()?.Trim()?.ToLowerInvariant();
        if (string.IsNullOrEmpty(toolPath))
            return Error("tool_path is required (e.g. 'lifecycle.memory_update')");

        if (!ToolDefinitions.ToolMap.TryGetValue(toolPath, out var actualToolName))
            return Error($"Unknown tool path: {toolPath}. Use get_tools_in_category to discover available tools.");

        var toolArguments = arguments?["arguments"];

        _logger.LogInformation("→ execute_tool: {Path} → {Tool}", toolPath, actualToolName);

        var route = GetToolRoute(actualToolName);
        if (route == null)
            return Error($"No API route for tool: {actualToolName}");

        try
        {
            return await ForwardToApi(route.Value.path, route.Value.method, toolArguments);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "API request failed for tool {Tool} via execute_tool", actualToolName);
            return Error($"API request failed: {ex.Message}");
        }
        catch (TaskCanceledException)
        {
            return Error("Request timed out");
        }
    }

    private static (string path, string method)? GetToolRoute(string toolName) => toolName switch
    {
        // Core tools
        "memory_search" => ("memories/search", "GET"),
        "memory_ingest" => ("memories", "POST"),
        "memory_about_user" => ("persona", "GET"),
        "initialise_conversation_session" => ("sessions", "POST"),
        "end_conversation_session" => ("sessions/current/end", "POST"),
        "instantiate_context" => ("context/instantiate", "GET"),
        "memory_multi_hop_search" => ("memories/multi-hop", "GET"),
        "get_integrations" => ("integrations", "GET"),
        "import_from_core" => ("import/core", "POST"),
        "set_user_persona" => ("persona", "POST"),
        "crawl_relationships" => ("relationships/discover", "POST"),
        "get_graph_statistics" => ("stats", "GET"),
        "get_model_info" => ("llm/config", "GET"),
        "reembed_memories" => ("jobs/reembed", "POST"),

        // Lifecycle tools
        "memory_update" => ("power/memory/update", "POST"),
        "memory_delete" => ("power/memory/delete", "POST"),
        "memory_merge" => ("power/memory/merge", "POST"),
        "memory_split" => ("power/memory/split", "POST"),
        "memory_decay" => ("jobs/decay", "POST"),
        "memory_reinforce" => ("power/memory/reinforce", "POST"),
        "memory_expire" => ("power/memory/expire", "POST"),
        "memory_supersede" => ("power/memory/supersede", "POST"),

        // Observability tools
        "memory_trace" => ("power/trace", "GET"),
        "memory_lineage" => ("power/lineage", "GET"),
        "memory_explain" => ("power/explain", "GET"),
        "memory_conflicts" => ("power/conflicts", "GET"),

        // Safety tools
        "detect_contradictions" => ("mind/contradictions", "GET"),
        "detect_hallucinations" => ("mind/hallucinations", "GET"),
        "verify_memory_integrity" => ("integrity/verify-all", "POST"),
        "scan_loops" => ("integrity/scan-loops", "GET"),

        // Export tools
        "export_workspace" => ("export/workspace", "POST"),
        "export_memories" => ("export/memories", "POST"),
        "export_graph" => ("export/graph", "POST"),
        "export_user_profile" => ("export/user-profile", "POST"),
        "export_markdown" => ("export/markdown", "POST"),

        // Reasoning tools
        "engineering_analyze" => ("reasoning/run", "POST"),
        "engineering_visualize" => ("visualize/graph", "GET"),
        "engineering_reason" => ("reasoning/start", "POST"),

        _ => null
    };

    private async Task<object> ForwardToApi(string path, string method, JsonNode? payload)
    {
        HttpResponseMessage response;

        if (method == "GET")
        {
            var queryString = BuildQueryString(payload);
            var url = string.IsNullOrEmpty(queryString) ? $"/api/{path}" : $"/api/{path}?{queryString}";
            _logger.LogDebug("GET {Url}", url);
            response = await _httpClient.GetAsync(url);
        }
        else
        {
            var json = payload?.ToJsonString() ?? "{}";
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            _logger.LogDebug("POST /api/{Path} with {Json}", path, json);
            response = await _httpClient.PostAsync($"/api/{path}", content);
        }

        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            return Error(body);

        // If already MCP response (check it's an object first)
        var parsed = JsonNode.Parse(body);
        if (parsed is JsonObject obj && obj["content"] is not null)
            return JsonSerializer.Deserialize<object>(body, _jsonOptions)!;

        // Wrap plain JSON/text
        return new
        {
            content = new[]
            {
                new { type = "text", text = body }
            }
        };
    }

    private static string BuildQueryString(JsonNode? payload)
    {
        if (payload is not JsonObject obj)
            return "";

        var parts = new List<string>();
        foreach (var prop in obj)
        {
            if (prop.Value == null) continue;

            var value = prop.Value switch
            {
                JsonValue v => v.ToString(),
                _ => prop.Value.ToJsonString()
            };

            parts.Add($"{Uri.EscapeDataString(prop.Key)}={Uri.EscapeDataString(value)}");
        }

        return string.Join("&", parts);
    }

    private static object Error(string message) =>
        new
        {
            isError = true,
            content = new[]
            {
                new { type = "text", text = $"Error: {message}" }
            }
        };
}
