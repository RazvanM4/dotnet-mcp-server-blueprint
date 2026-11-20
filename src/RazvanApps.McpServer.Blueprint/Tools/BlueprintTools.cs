using System.ComponentModel;
using ModelContextProtocol.Server;
using RazvanApps.McpServer.Blueprint.Domain;

namespace RazvanApps.McpServer.Blueprint.Tools;

[McpServerToolType]
public sealed class BlueprintTools(TrustBoundaryEvaluator evaluator)
{
    [McpServerTool(
        Name = "evaluate_mcp_trust_boundary",
        Title = "Evaluate MCP Trust Boundary",
        ReadOnly = true,
        Idempotent = true,
        Destructive = false,
        OpenWorld = false)]
    [Description("Evaluates an MCP server deployment configuration against trust-boundary best practices.")]
    public TrustBoundaryAssessment EvaluateMcpTrustBoundary(
        [Description("Transport type: 'stdio', 'http', or 'streamable-http'.")] string transport,
        [Description("True when every tool avoids external state mutations.")] bool isReadOnly,
        [Description("True when all tool arguments are schema-validated and canonicalized.")] bool validatesInputs,
        [Description("True when any tool can invoke arbitrary OS shell commands.")] bool executesShellCommands,
        [Description("True when HTTP/network transport requires caller authentication.")] bool requiresAuthentication,
        [Description("Optional listen address when using HTTP transport (e.g. 127.0.0.1).")] string? bindAddress = null)
        => evaluator.Evaluate(transport, isReadOnly, validatesInputs, executesShellCommands, requiresAuthentication, bindAddress);

    [McpServerTool(
        Name = "validate_server_manifest",
        Title = "Validate Official MCP Registry Manifest",
        ReadOnly = true,
        Idempotent = true,
        Destructive = false,
        OpenWorld = false)]
    [Description("Validates a server.json payload against Official MCP Registry constraints.")]
    public ManifestValidationResult ValidateServerManifest(
        [Description("Raw JSON content of server.json.")] string serverJson)
        => evaluator.ValidateManifest(serverJson);

    [McpServerTool(
        Name = "generate_tool_policy_summary",
        Title = "Generate Tool Policy Summary",
        ReadOnly = true,
        Idempotent = true,
        Destructive = false,
        OpenWorld = false)]
    [Description("Classifies an MCP tool into a least-privilege capability scope and approval policy.")]
    public ToolPolicySummary GenerateToolPolicySummary(
        [Description("Canonical snake_case tool name.")] string toolName,
        [Description("True if the tool only reads state.")] bool isReadOnly,
        [Description("True if the tool performs destructive or irreversible actions.")] bool isDestructive,
        [Description("True if the tool accesses external third-party URLs or APIs.")] bool touchesExternalNetwork)
        => evaluator.SummarizePolicy(toolName, isReadOnly, isDestructive, touchesExternalNetwork);
}
