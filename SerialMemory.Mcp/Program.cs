using SerialMemory.Mcp;

// ============================================================================
// SerialMemory MCP Client - PUBLIC DISTRIBUTION BUILD
// Supports both stdio (Claude Code) and HTTP (ChatGPT) transports
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
    await Console.Error.WriteLineAsync("[MCP Error] Missing SERIALMEMORY_API_KEY");
    Environment.Exit(1);
    return;
}

// Logging
using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});
var logger = loggerFactory.CreateLogger("SerialMemory.Mcp");

// Shared MCP handler
var mcpHandler = new McpHandler(apiEndpoint, apiKey, logger);

// Check if running interactively (stdio) or as HTTP server only
var httpOnly = args.Contains("--http-only");

if (!httpOnly)
{
    // Start HTTP server in background
    _ = Task.Run(() => RunHttpServer(mcpHandler, logger));

    // Run stdio transport in foreground
    logger.LogInformation("SerialMemory MCP Client starting → {Endpoint} (stdio + HTTP :4545 + HTTPS :4546)", apiEndpoint);
    await RunStdioTransport(mcpHandler, logger);
}
else
{
    // HTTP-only mode
    logger.LogInformation("SerialMemory MCP Client starting → {Endpoint} (HTTP :4545 + HTTPS :4546)", apiEndpoint);
    await RunHttpServer(mcpHandler, logger);
}

// STDIO Transport (for Claude Code)
async Task RunStdioTransport(McpHandler handler, ILogger log)
{
    using var reader = new StreamReader(Console.OpenStandardInput());
    await using var writer = new StreamWriter(Console.OpenStandardOutput());
    writer.AutoFlush = true;

    while (await reader.ReadLineAsync() is { } line)
    {
        try
        {
            var response = await handler.HandleRequest(line);
            if (response != null)
            {
                await writer.WriteLineAsync(response);
            }
        }
        catch (Exception ex)
        {
            log.LogError(ex, "MCP stdio request error");
            await Console.Error.WriteLineAsync($"[MCP Error] {ex.Message}");
        }
    }
}

// HTTP Transport (for ChatGPT)
async Task RunHttpServer(McpHandler handler, ILogger log)
{
    var builder = WebApplication.CreateBuilder();
    builder.Logging.ClearProviders();

    // Suppress Kestrel startup logs to avoid interfering with stdio
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ListenLocalhost(4545); // HTTP
        options.ListenLocalhost(4546, listenOptions => listenOptions.UseHttps()); // HTTPS
    });

    var app = builder.Build();

    // Health check
    app.MapGet("/", () => Results.Ok(new { status = "ok", service = "serialmemory-mcp", version = "1.0.0" }));

    // MCP over HTTP endpoint
    app.MapPost("/mcp", async ctx =>
    {
        try
        {
            using var reader = new StreamReader(ctx.Request.Body);
            var body = await reader.ReadToEndAsync();

            var response = await handler.HandleRequest(body);

            ctx.Response.ContentType = "application/json";
            if (response != null)
            {
                await ctx.Response.WriteAsync(response);
            }
            else
            {
                await ctx.Response.WriteAsync("{}");
            }
        }
        catch (Exception ex)
        {
            log.LogError(ex, "MCP HTTP request error");
            ctx.Response.StatusCode = 500;
            await ctx.Response.WriteAsync($"{{\"error\": \"{ex.Message}\"}}");
        }
    });

    log.LogInformation("HTTP transport listening on http://localhost:4545/mcp and https://localhost:4546/mcp");
    await app.RunAsync();
}
