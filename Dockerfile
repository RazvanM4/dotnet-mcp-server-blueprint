FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/RazvanApps.McpServer.Blueprint/RazvanApps.McpServer.Blueprint.csproj src/RazvanApps.McpServer.Blueprint/
COPY README.md ./
RUN dotnet restore src/RazvanApps.McpServer.Blueprint/RazvanApps.McpServer.Blueprint.csproj

COPY src/ src/
RUN dotnet publish src/RazvanApps.McpServer.Blueprint/RazvanApps.McpServer.Blueprint.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
USER app
ENV ASPNETCORE_URLS=http://+:8080
ENV MCP_TRANSPORT=http
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "RazvanApps.McpServer.Blueprint.dll", "--http"]
