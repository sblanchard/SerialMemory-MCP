using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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

// Lazy-MCP mode (default: true, set LAZY_MCP_ENABLED=false to disable)
var lazyMcpEnabled = configuration["LAZY_MCP_ENABLED"]?.ToLowerInvariant() is not ("false" or "0" or "no");

// Optional HTTP bearer token (if set, HTTP transport requires Authorization header)
var mcpHttpToken = configuration["MCP_HTTP_TOKEN"];

// Shared MCP handler
var mcpHandler = new McpHandler(apiEndpoint, apiKey, logger, lazyMcpEnabled);

// Check if running interactively (stdio) or as HTTP server only
var httpOnly = args.Contains("--http-only");

if (!httpOnly)
{
    // Start HTTP server in background (localhost only for security)
    _ = Task.Run(() => RunHttpServer(mcpHandler, logger, bindToAny: false, mcpHttpToken));

    // Run stdio transport in foreground
    logger.LogInformation("SerialMemory MCP Client starting → {Endpoint} (stdio + HTTP :4545 + HTTPS :4546)", apiEndpoint);
    await RunStdioTransport(mcpHandler, logger);
}
else
{
    // HTTP-only mode - bind to all interfaces for Docker
    logger.LogInformation("SerialMemory MCP Client starting → {Endpoint} (HTTP :4545 on 0.0.0.0)", apiEndpoint);
    await RunHttpServer(mcpHandler, logger, bindToAny: true, mcpHttpToken);
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
            await Console.Error.WriteLineAsync("[MCP Error] Request processing failed");
        }
    }
}

// HTTP Transport (for ChatGPT)
async Task RunHttpServer(McpHandler handler, ILogger log, bool bindToAny, string? httpToken)
{
    var builder = WebApplication.CreateBuilder();
    builder.Logging.ClearProviders();

    // Configure Kestrel binding and request limits
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.Limits.MaxRequestBodySize = 1_048_576; // 1 MB

        if (bindToAny)
        {
            // Docker/server mode - HTTP only (use reverse proxy for HTTPS)
            options.ListenAnyIP(4545);
        }
        else
        {
            // Local mode - localhost only for security
            options.ListenLocalhost(4545); // HTTP
            options.ListenLocalhost(4546, listenOptions => listenOptions.UseHttps()); // HTTPS
        }
    });

    // CORS: restrict to localhost origins in local mode; Docker mode uses reverse proxy for CORS
    if (!bindToAny)
    {
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.SetIsOriginAllowed(origin =>
                {
                    if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                        return false;
                    return uri.Host == "localhost" || uri.Host == "127.0.0.1";
                });
                policy.AllowAnyHeader();
                policy.AllowAnyMethod();
            });
        });
    }

    var app = builder.Build();

    if (!bindToAny)
        app.UseCors();

    // Bearer token authentication middleware (skips OAuth discovery endpoint)
    if (!string.IsNullOrWhiteSpace(httpToken))
    {
        log.LogInformation("HTTP bearer token authentication enabled");
        app.Use(async (ctx, next) =>
        {
            // OAuth discovery must remain unauthenticated per spec
            if (ctx.Request.Path.StartsWithSegments("/.well-known"))
            {
                await next();
                return;
            }

            var authHeader = ctx.Request.Headers.Authorization.ToString();
            var providedToken = authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? authHeader["Bearer ".Length..]
                : "";

            var expected = Encoding.UTF8.GetBytes(httpToken);
            var actual = Encoding.UTF8.GetBytes(providedToken);

            if (!CryptographicOperations.FixedTimeEquals(expected, actual))
            {
                log.LogWarning("Unauthorized HTTP request from {RemoteIP}", ctx.Connection.RemoteIpAddress);
                ctx.Response.StatusCode = 401;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.WriteAsync(
                    JsonSerializer.Serialize(new { error = "Unauthorized" }));
                return;
            }

            await next();
        });
    }

    // Health check
    app.MapGet("/", () => Results.Ok(new { status = "ok", service = "serialmemory-mcp", version = "1.0.0" }));

    // OAuth discovery endpoint - returns proper JSON for clients that probe for OAuth support
    // Fixes Mac SSE transport issue where MCP SDK expects JSON response from OAuth discovery
    app.MapGet("/.well-known/oauth-authorization-server", () =>
        Results.NotFound(new { error = "oauth_not_supported", message = "This server uses Bearer token authentication" }));

    // MCP over HTTP endpoint
    app.MapPost("/mcp", async ctx =>
    {
        try
        {
            // Enable buffering so the full body is read before processing.
            // Without this, ReadToEndAsync on a chunked-transfer body can return
            // partial data, causing JsonReaderException on large payloads.
            ctx.Request.EnableBuffering();

            using var ms = new MemoryStream();
            await ctx.Request.Body.CopyToAsync(ms);
            var bodyBytes = ms.ToArray();
            var body = Encoding.UTF8.GetString(bodyBytes);

            if (string.IsNullOrWhiteSpace(body))
            {
                log.LogWarning("Empty MCP request body (Content-Length: {CL})",
                    ctx.Request.ContentLength);
                ctx.Response.StatusCode = 400;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.WriteAsync("{\"error\":\"Empty request body\"}");
                return;
            }

            // Validate JSON is complete before forwarding
            try
            {
                using var doc = JsonDocument.Parse(body);
            }
            catch (JsonException ex)
            {
                log.LogWarning("Truncated MCP request body ({Bytes} bytes received, Content-Length: {CL}): {Error}",
                    bodyBytes.Length, ctx.Request.ContentLength, ex.Message);
                ctx.Response.StatusCode = 400;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.WriteAsync(
                    JsonSerializer.Serialize(new { error = "Malformed JSON in request body" }));
                return;
            }

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
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsync(
                JsonSerializer.Serialize(new { error = "Internal server error" }));
        }
    });

    log.LogInformation("HTTP transport listening on http://localhost:4545/mcp and https://localhost:4546/mcp");
    await app.RunAsync();
}
