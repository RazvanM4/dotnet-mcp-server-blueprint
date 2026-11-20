using System.Text.Json;
using System.Text.RegularExpressions;

namespace RazvanApps.McpServer.Blueprint.Domain;

public sealed record TrustBoundaryAssessment(
    string RiskLevel,
    int Score,
    IReadOnlyList<string> Findings,
    IReadOnlyList<string> Recommendations);

public sealed record ManifestValidationResult(
    bool IsValid,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings);

public sealed record ToolPolicySummary(
    string ToolName,
    string Scope,
    bool RequiresHumanApproval,
    string Rationale);

/// <summary>
/// Deterministic policy and manifest evaluator demonstrating safe, bounded MCP tool logic.
/// </summary>
public sealed partial class TrustBoundaryEvaluator
{
    private const int MaxDescriptionLength = 100;

    [GeneratedRegex(@"^io\.github\.[a-zA-Z0-9._-]+/[a-zA-Z0-9._-]+$", RegexOptions.Compiled)]
    private static partial Regex GitHubNamespacePattern();

    public TrustBoundaryAssessment Evaluate(
        string transport,
        bool isReadOnly,
        bool validatesInputs,
        bool executesShellCommands,
        bool requiresAuthentication,
        string? bindAddress = null)
    {
        var normalizedTransport = string.IsNullOrWhiteSpace(transport)
            ? throw new ArgumentException("Transport must not be empty.", nameof(transport))
            : transport.Trim().ToLowerInvariant();

        if (normalizedTransport is not ("stdio" or "http" or "streamable-http"))
        {
            throw new ArgumentException(
                "Unsupported transport. Allowed values: 'stdio', 'http', or 'streamable-http'.",
                nameof(transport));
        }

        var score = 100;
        var findings = new List<string>();
        var recommendations = new List<string>();

        if (executesShellCommands)
        {
            score -= 40;
            findings.Add("Tool permits arbitrary shell command execution, widening prompt-injection blast radius.");
            recommendations.Add("Replace shell invocation with typed, allow-listed domain methods.");
        }

        if (!validatesInputs)
        {
            score -= 25;
            findings.Add("Inputs are not validated before reaching downstream handlers.");
            recommendations.Add("Enforce strict schema constraints, path canonicalization, and length limits.");
        }

        if (!isReadOnly)
        {
            score -= 15;
            findings.Add("Tool can mutate external state without read-only annotation.");
            recommendations.Add("Separate read queries from write mutations and enforce explicit confirmation.");
        }

        var isNetworkTransport = normalizedTransport is "http" or "streamable-http";
        if (isNetworkTransport && !requiresAuthentication)
        {
            score -= 25;
            findings.Add("Network MCP transport is exposed without caller authentication.");
            recommendations.Add("Require OAuth 2.1 / bearer authentication and validate Origin headers.");
        }

        if (isNetworkTransport && string.Equals(bindAddress?.Trim(), "0.0.0.0", StringComparison.Ordinal))
        {
            findings.Add("HTTP transport binds to all network interfaces (0.0.0.0).");
            recommendations.Add("Bind to 127.0.0.1 locally or terminate TLS behind a hardened reverse proxy.");
        }

        score = Math.Clamp(score, 0, 100);
        var riskLevel = score switch
        {
            >= 85 => "Low",
            >= 60 => "Medium",
            _ => "High",
        };

        if (findings.Count == 0)
        {
            findings.Add("Configuration satisfies baseline MCP trust-boundary controls.");
        }

        return new TrustBoundaryAssessment(riskLevel, score, findings, recommendations);
    }

    public ManifestValidationResult ValidateManifest(string serverJson)
    {
        if (string.IsNullOrWhiteSpace(serverJson))
        {
            return new ManifestValidationResult(false, ["server.json payload must not be empty."], []);
        }

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(serverJson);
        }
        catch (JsonException ex)
        {
            return new ManifestValidationResult(false, [$"Invalid JSON: {ex.Message}"], []);
        }

        using (doc)
        {
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return new ManifestValidationResult(false, ["Root JSON value must be an object."], []);
            }

            var errors = new List<string>();
            var warnings = new List<string>();

            if (!root.TryGetProperty("$schema", out var schemaProp) || string.IsNullOrWhiteSpace(schemaProp.GetString()))
            {
                errors.Add("Missing required '$schema' field pointing to the MCP server schema.");
            }

            if (!root.TryGetProperty("name", out var nameProp) || string.IsNullOrWhiteSpace(nameProp.GetString()))
            {
                errors.Add("Missing required 'name' field.");
            }
            else
            {
                var name = nameProp.GetString()!;
                if (!GitHubNamespacePattern().IsMatch(name))
                {
                    warnings.Add("Name does not use reverse-DNS 'io.github.<owner>/<repo>' namespace required for GitHub OAuth publishing.");
                }
            }

            if (!root.TryGetProperty("description", out var descProp) || string.IsNullOrWhiteSpace(descProp.GetString()))
            {
                errors.Add("Missing required 'description' field.");
            }
            else if (descProp.GetString()!.Length > MaxDescriptionLength)
            {
                errors.Add($"Description exceeds {MaxDescriptionLength} characters (actual: {descProp.GetString()!.Length}).");
            }

            if (!root.TryGetProperty("version", out var versionProp) || string.IsNullOrWhiteSpace(versionProp.GetString()))
            {
                errors.Add("Missing required 'version' field.");
            }

            if (root.TryGetProperty("packages", out var packagesProp) && packagesProp.ValueKind == JsonValueKind.Array)
            {
                if (packagesProp.GetArrayLength() == 0)
                {
                    warnings.Add("'packages' array is empty; consider declaring at least one NuGet/npm/PyPI/OCI package.");
                }
            }
            else if (!root.TryGetProperty("remotes", out _))
            {
                errors.Add("Manifest must declare either 'packages' or 'remotes'.");
            }

            return new ManifestValidationResult(errors.Count == 0, errors, warnings);
        }
    }

    public ToolPolicySummary SummarizePolicy(
        string toolName,
        bool isReadOnly,
        bool isDestructive,
        bool touchesExternalNetwork)
    {
        if (string.IsNullOrWhiteSpace(toolName))
        {
            throw new ArgumentException("Tool name must not be empty.", nameof(toolName));
        }

        var normalized = toolName.Trim();
        if (isDestructive)
        {
            return new ToolPolicySummary(
                normalized,
                "privileged-write",
                RequiresHumanApproval: true,
                "Destructive operations require explicit human-in-the-loop confirmation before execution.");
        }

        if (!isReadOnly)
        {
            return new ToolPolicySummary(
                normalized,
                touchesExternalNetwork ? "external-mutation" : "local-mutation",
                RequiresHumanApproval: true,
                "State-changing operations should require confirmation or narrow idempotency tokens.");
        }

        return new ToolPolicySummary(
            normalized,
            touchesExternalNetwork ? "read-open-world" : "read-bounded",
            RequiresHumanApproval: false,
            touchesExternalNetwork
                ? "Read-only open-world tool; apply allow-listed domains and response size caps."
                : "Read-only bounded tool safe for unattended agent invocation.");
    }
}
