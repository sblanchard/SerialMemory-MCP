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
    Console.Error.WriteLine("[MCP Error] Missing SERIALMEMORY_ENDPOINT");
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
    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
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

            "shutdown" => null,
            "exit" => Environment.Exit(0),

            "tools/list" => new { tools = ToolDefinitions.GetAllTools() },

            "resources/list" => new
            {
                resources = new object[]
                {
                    new { uri="memory://recent", name="Recent Memories", mimeType="application/json" },
                    new { uri="memory://sessions", name="Conversation Sessions", mimeType="application/json" }
                }
            },

            "tools/call" => await ForwardToApi("mcp/tools", @params),

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

// Forwarding Logic ----------------------------------------------------------
async Task<object> ForwardToApi(string basePath, JsonNode? payload)
{
    var toolName = payload?["name"]?.GetValue<string>();
    var args = payload?["arguments"];

    if (toolName is null)
        return Error("Tool name missing");

    logger.LogInformation("→ Forwarding tool call: {Tool}", toolName);

    var content = new StringContent(args?.ToJsonString() ?? "{}", Encoding.UTF8, "application/json");
    var response = await httpClient.PostAsync($"/api/{basePath}/{toolName}", content);
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

object Error(string message) =>
    new
    {
        isError = true,
        content = new[]
        {
            new { type = "text", text = $"Error: {message}" }
        }
    };
