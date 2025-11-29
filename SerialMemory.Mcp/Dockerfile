# SerialMemory MCP Client - Thin HTTP Proxy
# This is a standalone client with NO backend dependencies

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
    --no-restore \
    --output /app/publish

# Runtime stage - using slim runtime image
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app

# Copy published app
COPY --from=build /app/publish .

# Set environment variables
ENV DOTNET_EnableDiagnostics=0

# Required environment variables (set at runtime):
# - SERIALMEMORY_ENDPOINT: API endpoint URL
# - SERIALMEMORY_API_KEY: API key for authentication

# The MCP client uses STDIO, no ports needed
ENTRYPOINT ["dotnet", "SerialMemory.Mcp.dll"]
