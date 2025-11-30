using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SerialMemory.Mcp.Tools;

// ============================================================================
// SerialMemory MCP Client - PUBLIC DISTRIBUTION BUILD
// ============================================================================

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

// Required configuration
var apiEndpoint = configuration["SERIALMEMORY_ENDPOINT"]?.TrimEnd('/');
var apiKey = configuration["SERIALMEMORY_API_KEY"];

if (string.IsNullOrWhiteSpace(apiEndpoint))
{
    await Console.Error.WriteLineAsync("[MCP Error] SERIALMEMORY_ENDPOINT is required (e.g., https://api.serialmemory.dev)");
    Environment.Exit(1);
    return;
}

if (string.IsNullOrWhiteSpace(apiKey))
{
    Console.Error.WriteLine("[MCP Error] Missing SERIALMEMORY_API_KEY");
    return;
}

// HTTP Client ---------------------------------------------------------------
using var httpClient = new HttpClient
{
    BaseAddress = new Uri(apiEndpoint),
    Timeout = TimeSpan.FromSeconds(120)
};

httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("SerialMemory-MCP/1.0");

// Logging -------------------------------------------------------------------
using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});
var logger = loggerFactory.CreateLogger("SerialMemory.Mcp");
logger.LogInformation("SerialMemory MCP Client starting → {Endpoint}", apiEndpoint);

// JSON Options --------------------------------------------------------------
var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = false,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
};

// MCP STDIO Loop ------------------------------------------------------------
using var reader = new StreamReader(Console.OpenStandardInput());
using var writer = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };

while (await reader.ReadLineAsync() is { } line)
{
    if (string.IsNullOrWhiteSpace(line))
        continue;

    try
    {
        var req = JsonNode.Parse(line);
        var id = req?["id"];
        var method = req?["method"]?.GetValue<string>();
        var @params = req?["params"];

        object? result = method switch
        {
            "initialize" => new
            {
                protocolVersion = "2024-11-05",
                serverInfo = new { name = "serialmemory-mcp", version = "1.0.0" },
                capabilities = new { tools = new { }, resources = new { } }
            },
            "notifications/initialized" => null,
            "shutdown" => null,
            "exit" => Environment.Exit(0),
            "tools/list" => new { tools = ToolDefinitions.GetAllTools() },
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

        if (result != null)
        {
            var response = JsonSerializer.Serialize(new
            {
                jsonrpc = "2.0",
                id = id?.GetValue<object>(),
                result
            }, jsonOptions);

            await writer.WriteLineAsync(response);
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "MCP request error");
        await Console.Error.WriteLineAsync($"[MCP Error] {ex.Message}");
    }
}

// Tool Call Handler ---------------------------------------------------------
async Task<object> HandleToolsCall(JsonNode? @params)
{
    var toolName = @params?["name"]?.GetValue<string>();
    var arguments = @params?["arguments"];

    if (string.IsNullOrEmpty(toolName))
        return Error("Tool name is required");

    logger.LogInformation("→ Forwarding tool call: {Tool}", toolName);

    try
    {
        var route = GetToolRoute(toolName);
        if (route == null)
            return Error($"Unknown tool: {toolName}");

        return await ForwardToApi(route.Value.path, route.Value.method, arguments);
    }
    catch (HttpRequestException ex)
    {
        logger.LogError(ex, "API request failed for tool {Tool}", toolName);
        return Error($"API request failed: {ex.Message}");
    }
    catch (TaskCanceledException)
    {
        return Error("Request timed out");
    }
}

// Tool Routing --------------------------------------------------------------
(string path, string method)? GetToolRoute(string toolName) => toolName switch
{
    // Core tools
    "memory_search" => ("memories/search", "GET"),
    "memory_ingest" => ("memories", "POST"),
    "memory_about_user" => ("persona", "GET"),
    "initialise_conversation_session" => ("sessions", "POST"),
    "end_conversation_session" => ("sessions/current/end", "POST"),
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

    // Reasoning tools
    "engineering_analyze" => ("reasoning/run", "POST"),
    "engineering_visualize" => ("visualize/graph", "GET"),
    "engineering_reason" => ("reasoning/start", "POST"),

    _ => null
};

// API Forwarding ------------------------------------------------------------
async Task<object> ForwardToApi(string path, string method, JsonNode? payload)
{
    HttpResponseMessage response;

    if (method == "GET")
    {
        var queryString = BuildQueryString(payload);
        var url = string.IsNullOrEmpty(queryString) ? $"/api/{path}" : $"/api/{path}?{queryString}";
        logger.LogDebug("GET {Url}", url);
        response = await httpClient.GetAsync(url);
    }
    else
    {
        var json = payload?.ToJsonString() ?? "{}";
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        logger.LogDebug("POST /api/{Path} with {Json}", path, json);
        response = await httpClient.PostAsync($"/api/{path}", content);
    }

    var body = await response.Content.ReadAsStringAsync();

    if (!response.IsSuccessStatusCode)
        return Error(body);

    // If already MCP response
    if (JsonNode.Parse(body)?["content"] is not null)
        return JsonSerializer.Deserialize<object>(body, jsonOptions)!;

    // Wrap plain JSON/text
    return new
    {
        content = new[]
        {
            new { type = "text", text = body }
        }
    };
}

string BuildQueryString(JsonNode? payload)
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

object Error(string message) =>
    new
    {
        isError = true,
        content = new[]
        {
            new { type = "text", text = $"Error: {message}" }
        }
    };
