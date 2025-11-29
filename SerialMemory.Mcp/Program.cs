using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SerialMemory.Mcp.Tools;

// ============================================================================
// SerialMemory MCP Client - Thin HTTP Proxy
// ============================================================================
// This is a THIN CLIENT that forwards all tool calls to SerialMemory.Api.
// It contains NO backend logic - no database, no embeddings, no reasoning.
// ============================================================================

#region Configuration

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"), optional: true, reloadOnChange: false)
    .AddEnvironmentVariables()
    .Build();

// Required: API endpoint and key
var apiEndpoint = configuration["SERIALMEMORY_ENDPOINT"]?.TrimEnd('/');
var apiKey = configuration["SERIALMEMORY_API_KEY"];

if (string.IsNullOrEmpty(apiEndpoint))
{
    await Console.Error.WriteLineAsync("[MCP Error] SERIALMEMORY_ENDPOINT is required (e.g., https://api.serialmemory.com)");
    Environment.Exit(1);
    return;
}

if (string.IsNullOrEmpty(apiKey))
{
    await Console.Error.WriteLineAsync("[MCP Error] SERIALMEMORY_API_KEY is required");
    Environment.Exit(1);
    return;
}

#endregion

#region HTTP Client Setup

using var httpClient = new HttpClient
{
    BaseAddress = new Uri(apiEndpoint),
    Timeout = TimeSpan.FromSeconds(120)
};

httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

#endregion

#region Logging

using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);
    builder.SetMinimumLevel(LogLevel.Information);
});
var logger = loggerFactory.CreateLogger("SerialMemory.Mcp");

logger.LogInformation("SerialMemory MCP Client starting");
logger.LogInformation("API Endpoint: {Endpoint}", apiEndpoint);

#endregion

#region JSON Serialization

var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    WriteIndented = false,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
};

#endregion

#region MCP STDIO Protocol

await using var stdin = Console.OpenStandardInput();
await using var stdout = Console.OpenStandardOutput();
using var reader = new StreamReader(stdin);
await using var writer = new StreamWriter(stdout) { AutoFlush = true };

while (await reader.ReadLineAsync() is { } line)
{
    if (string.IsNullOrWhiteSpace(line)) continue;

    try
    {
        var request = JsonNode.Parse(line);
        if (request == null) continue;

        var id = request["id"];
        var method = request["method"]?.GetValue<string>();
        var @params = request["params"];

        logger.LogDebug("Received: {Method}", method);

        object? result = method switch
        {
            "initialize" => HandleInitialize(),
            "notifications/initialized" => null,
            "tools/list" => HandleToolsList(),
            "resources/list" => HandleResourcesList(),
            "resources/read" => await ForwardToApi("resources/read", @params),
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
        logger.LogError(ex, "Error processing request");
        await Console.Error.WriteLineAsync($"[MCP Error] {ex.Message}");
    }
}

#endregion

#region Protocol Handlers

object HandleInitialize()
{
    return new
    {
        protocolVersion = "2024-11-05",
        serverInfo = new
        {
            name = "serialmemory-client",
            version = "3.0.0"
        },
        capabilities = new
        {
            tools = new { },
            resources = new { }
        }
    };
}

object HandleToolsList()
{
    return new { tools = ToolDefinitions.GetAllTools() };
}

object HandleResourcesList()
{
    return new
    {
        resources = new object[]
        {
            new
            {
                uri = "memory://recent",
                name = "Recent Memories",
                description = "List of recently added memories",
                mimeType = "application/json"
            },
            new
            {
                uri = "memory://sessions",
                name = "Conversation Sessions",
                description = "List of recent conversation sessions",
                mimeType = "application/json"
            }
        }
    };
}

async Task<object> HandleToolsCall(JsonNode? @params)
{
    var toolName = @params?["name"]?.GetValue<string>();
    var arguments = @params?["arguments"];

    if (string.IsNullOrEmpty(toolName))
    {
        return CreateErrorResponse("Tool name is required");
    }

    logger.LogInformation("Forwarding tool call: {Tool}", toolName);

    try
    {
        return await ForwardToApi($"mcp/tools/{toolName}", arguments);
    }
    catch (HttpRequestException ex)
    {
        logger.LogError(ex, "API request failed for tool {Tool}", toolName);
        return CreateErrorResponse($"API request failed: {ex.Message}");
    }
    catch (TaskCanceledException)
    {
        return CreateErrorResponse("Request timed out");
    }
}

async Task<object> ForwardToApi(string path, JsonNode? payload)
{
    var json = payload?.ToJsonString() ?? "{}";
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    var response = await httpClient.PostAsync($"/api/{path}", content);

    var responseBody = await response.Content.ReadAsStringAsync();

    if (!response.IsSuccessStatusCode)
    {
        logger.LogWarning("API error {Status}: {Body}", response.StatusCode, responseBody);

        // Try to extract error message from response
        try
        {
            var errorObj = JsonNode.Parse(responseBody);
            var errorMessage = errorObj?["error"]?.GetValue<string>()
                            ?? errorObj?["message"]?.GetValue<string>()
                            ?? responseBody;
            return CreateErrorResponse(errorMessage);
        }
        catch
        {
            return CreateErrorResponse($"API error {(int)response.StatusCode}: {responseBody}");
        }
    }

    // Parse and return the API response
    try
    {
        var result = JsonNode.Parse(responseBody);

        // If API returns MCP-formatted response, use it directly
        if (result?["content"] != null)
        {
            return JsonSerializer.Deserialize<object>(responseBody, jsonOptions)!;
        }

        // Otherwise wrap in MCP text response
        return CreateTextResponse(responseBody);
    }
    catch
    {
        return CreateTextResponse(responseBody);
    }
}

object CreateTextResponse(string text)
{
    return new
    {
        content = new[]
        {
            new { type = "text", text }
        }
    };
}

object CreateErrorResponse(string message)
{
    return new
    {
        content = new[]
        {
            new { type = "text", text = $"Error: {message}" }
        },
        isError = true
    };
}

#endregion
