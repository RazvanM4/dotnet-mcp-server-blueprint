using ModelContextProtocol.Protocol;
using RazvanApps.McpServer.Blueprint.Domain;
using RazvanApps.McpServer.Blueprint.Prompts;
using RazvanApps.McpServer.Blueprint.Resources;
using RazvanApps.McpServer.Blueprint.Tools;

var useHttp = args.Contains("--http", StringComparer.OrdinalIgnoreCase)
    || string.Equals(Environment.GetEnvironmentVariable("MCP_TRANSPORT"), "http", StringComparison.OrdinalIgnoreCase);

if (useHttp)
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Services.AddSingleton<TrustBoundaryEvaluator>();
    builder.Services
        .AddMcpServer(ConfigureServerOptions)
        .WithHttpTransport()
        .WithTools<BlueprintTools>()
        .WithResources<BlueprintResources>()
        .WithPrompts<BlueprintPrompts>();

    var app = builder.Build();
    app.MapGet("/health", () => Results.Ok(new
    {
        status = "healthy",
        server = "razvanapps-mcp-blueprint",
        version = "1.0.0",
        transport = "streamable-http",
    }));
    app.MapMcp("/mcp");
    await app.RunAsync();
}
else
{
    var builder = Host.CreateApplicationBuilder(args);
    // All diagnostic logs in stdio transport must go exclusively to stderr.
    builder.Logging.ClearProviders();
    builder.Logging.AddConsole(options =>
    {
        options.LogToStandardErrorThreshold = LogLevel.Trace;
    });

    builder.Services.AddSingleton<TrustBoundaryEvaluator>();
    builder.Services
        .AddMcpServer(ConfigureServerOptions)
        .WithStdioServerTransport()
        .WithTools<BlueprintTools>()
        .WithResources<BlueprintResources>()
        .WithPrompts<BlueprintPrompts>();

    var host = builder.Build();
    await host.RunAsync();
}

static void ConfigureServerOptions(ModelContextProtocol.Server.McpServerOptions options)
{
    options.ServerInfo = new Implementation
    {
        Name = "razvanapps-mcp-blueprint",
        Title = "RazvanApps MCP Server Blueprint",
        Version = "1.0.0",
    };
}

public partial class Program;
