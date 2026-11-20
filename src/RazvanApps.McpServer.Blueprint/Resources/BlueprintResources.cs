using System.ComponentModel;
using ModelContextProtocol.Server;

namespace RazvanApps.McpServer.Blueprint.Resources;

[McpServerResourceType]
public sealed class BlueprintResources
{
    [McpServerResource(
        UriTemplate = "architecture://mcp/checklist",
        Name = "mcp_production_checklist",
        Title = "MCP Production Readiness Checklist",
        MimeType = "text/markdown")]
    [Description("Production checklist covering transport isolation, stderr logging, least privilege, and supply-chain verification.")]
    public static string GetProductionChecklist() => """
        # Production MCP Server Checklist (.NET 10)

        1. **Transport Isolation**: Keep `stdout` strictly reserved for JSON-RPC frames in `stdio` mode; route all logs to `stderr`.
        2. **Http Binding & Auth**: Bind local servers to `127.0.0.1` or terminate OAuth 2.1 + TLS cleanly behind a reverse proxy.
        3. **Explicit Tool Annotations**: Set `ReadOnly`, `Idempotent`, `Destructive`, and `OpenWorld` accurately on every tool.
        4. **Validated Inputs**: Canonicalize paths, bound string/collection sizes, and avoid arbitrary shell execution.
        5. **Supply-Chain Provenance**: Ship a valid `server.json` for the Official MCP Registry and embed `<!-- mcp-name: ... -->` in NuGet README metadata.
        """;
}
