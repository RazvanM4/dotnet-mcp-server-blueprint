using System.ComponentModel;
using ModelContextProtocol.Server;

namespace RazvanApps.McpServer.Blueprint.Prompts;

[McpServerPromptType]
public sealed class BlueprintPrompts
{
    [McpServerPrompt(
        Name = "mcp_security_review",
        Title = "MCP Security Review Prompt")]
    [Description("Generates a structured security audit prompt for an MCP server implementation.")]
    public static string SecurityReviewPrompt(
        [Description("Target MCP server name or repository.")] string serverName,
        [Description("Primary transport ('stdio' or 'streamable-http').")] string transport = "stdio")
        => $"""
            Review the MCP server '{serverName}' running over '{transport}'.
            Inspect:
            1. Stdout contamination risks and structured stderr logging;
            2. Tool input validation, path/SQL injection guards, and shell-execution elimination;
            3. Authentication, Origin validation, and rate limits for HTTP endpoints;
            4. Accuracy of ReadOnly/Destructive/OpenWorld MCP tool annotations.
            """;
}
