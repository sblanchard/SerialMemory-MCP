# SerialMemory MCP Client - Thin HTTP Proxy
# Supports stdio (Claude Code) and HTTP/HTTPS (ChatGPT)

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project file
COPY SerialMemory.Mcp.csproj .

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY . .

# Build and publish
RUN dotnet publish \
    --configuration Release \
    --output /app/publish

# Runtime stage - using ASP.NET runtime for HTTP hosting
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Copy published app
COPY --from=build /app/publish .

# Set environment variables
ENV DOTNET_EnableDiagnostics=0
ENV ASPNETCORE_URLS=http://+:4545;https://+:4546

# Required environment variables (set at runtime):
# - SERIALMEMORY_ENDPOINT: API endpoint URL
# - SERIALMEMORY_API_KEY: API key for authentication

# Expose HTTP and HTTPS ports
EXPOSE 4545 4546

# Run in HTTP-only mode for Docker (no stdio)
ENTRYPOINT ["dotnet", "SerialMemory.Mcp.dll", "--http-only"]
