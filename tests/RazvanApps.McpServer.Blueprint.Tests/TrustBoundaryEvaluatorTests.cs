using System.Text.Json;
using RazvanApps.McpServer.Blueprint.Domain;
using RazvanApps.McpServer.Blueprint.Prompts;
using RazvanApps.McpServer.Blueprint.Resources;
using RazvanApps.McpServer.Blueprint.Tools;

namespace RazvanApps.McpServer.Blueprint.Tests;

public sealed class TrustBoundaryEvaluatorTests
{
    private readonly TrustBoundaryEvaluator _evaluator = new();

    [Fact]
    public void Evaluate_HardenedStdioReadOnlyServer_ReturnsLowRiskAndFullScore()
    {
        var result = _evaluator.Evaluate(
            transport: "stdio",
            isReadOnly: true,
            validatesInputs: true,
            executesShellCommands: false,
            requiresAuthentication: false);

        Assert.Equal("Low", result.RiskLevel);
        Assert.Equal(100, result.Score);
        Assert.Empty(result.Recommendations);
    }

    [Fact]
    public void Evaluate_UnauthenticatedHttpWithShellExecution_ReturnsHighRiskAndClearFindings()
    {
        var result = _evaluator.Evaluate(
            transport: "streamable-http",
            isReadOnly: false,
            validatesInputs: false,
            executesShellCommands: true,
            requiresAuthentication: false,
            bindAddress: "0.0.0.0");

        Assert.Equal("High", result.RiskLevel);
        Assert.True(result.Score < 60);
        Assert.Contains(result.Findings, f => f.Contains("shell command", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Findings, f => f.Contains("authentication", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Findings, f => f.Contains("0.0.0.0", StringComparison.Ordinal));
    }

    [Fact]
    public void Evaluate_InvalidTransport_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            _evaluator.Evaluate("websocket", true, true, false, true));
    }

    [Fact]
    public void ValidateManifest_RepositoryServerJson_IsValidAndMatchesRegistryRules()
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var serverJsonPath = Path.Combine(repoRoot, "server.json");
        var serverJson = File.ReadAllText(serverJsonPath);

        var validation = _evaluator.ValidateManifest(serverJson);

        Assert.True(validation.IsValid, string.Join("; ", validation.Errors));
        Assert.Empty(validation.Warnings);

        var glamaJson = JsonDocument.Parse(File.ReadAllText(Path.Combine(repoRoot, "glama.json")));
        var maintainers = glamaJson.RootElement.GetProperty("maintainers").EnumerateArray().Select(e => e.GetString()).ToList();
        Assert.Contains("RazvanM4", maintainers);
    }

    [Fact]
    public void ValidateManifest_DescriptionOver100Chars_FailsValidation()
    {
        var longDesc = new string('x', 101);
        var json = $$"""
            {
              "$schema": "https://static.modelcontextprotocol.io/schemas/2025-12-11/server.schema.json",
              "name": "io.github.RazvanApps/example",
              "description": "{{longDesc}}",
              "version": "1.0.0",
              "packages": [{ "registryType": "nuget", "identifier": "Example", "version": "1.0.0", "transport": { "type": "stdio" } }]
            }
            """;

        var validation = _evaluator.ValidateManifest(json);

        Assert.False(validation.IsValid);
        Assert.Contains(validation.Errors, e => e.Contains("100", StringComparison.Ordinal));
    }

    [Fact]
    public void SummarizePolicy_DestructiveTool_RequiresHumanApproval()
    {
        var tools = new BlueprintTools(_evaluator);
        var summary = tools.GenerateToolPolicySummary("delete_cluster", isReadOnly: false, isDestructive: true, touchesExternalNetwork: false);

        Assert.Equal("privileged-write", summary.Scope);
        Assert.True(summary.RequiresHumanApproval);
    }

    [Fact]
    public void ResourcesAndPrompts_ReturnNonEmptyGuidance()
    {
        var checklist = BlueprintResources.GetProductionChecklist();
        var prompt = BlueprintPrompts.SecurityReviewPrompt("razvanapps-mcp-blueprint", "stdio");

        Assert.Contains("stderr", checklist, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("razvanapps-mcp-blueprint", prompt, StringComparison.Ordinal);
    }
}
