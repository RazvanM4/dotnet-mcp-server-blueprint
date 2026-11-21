<!-- mcp-name: io.github.RazvanM4/dotnet-mcp-server-blueprint -->

# .NET 10 MCP Server Blueprint

A production-ready reference implementation of a **Model Context Protocol (MCP)** server built on **.NET 10** and the official C# SDK (`ModelContextProtocol` & `ModelContextProtocol.AspNetCore`).

Designed as an end-to-end companion to:
- [Build a Production-Ready MCP Server in C#: Tools, Trust, and Transport](https://razvanmares.com/blog/build-a-production-ready-mcp-server-in-c-tools-trust-and-transport)
- [Inside the Official MCP Registry: A Supply Chain for AI Tools](https://razvanmares.com/blog/inside-the-official-mcp-registry-a-supply-chain-for-ai-tools)

---

## Architecture Highlights

- **Dual Transport Support**:
  - **Standard I/O (`stdio`)** by default for local desktop/IDE hosts (`Claude Code`, `Antigravity`, `Cursor`, `VS Code`), with strict `stderr`-only diagnostics so `stdout` stays pure JSON-RPC.
  - **Streamable HTTP (`--http` / `MCP_TRANSPORT=http`)** exposing `/mcp` alongside `/health` for containerized or remote deployments.
- **Typed Domain Logic & Tool Annotations**:
  - Explicit `[McpServerTool(ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]` capabilities.
  - Three deterministic governance tools: `evaluate_mcp_trust_boundary`, `validate_server_manifest`, `generate_tool_policy_summary`.
  - Built-in resource (`architecture://mcp/checklist`) and prompt (`mcp_security_review`).
- **Registry-Ready Supply Chain**:
  - Packaged as a cross-platform .NET tool (`RazvanApps.McpServer.Blueprint`).
  - Ships valid `server.json` for the **Official MCP Registry** and `glama.json` + multi-stage `Dockerfile` for **Glama.ai**.

---

## Quick Start

### Run via .NET Tool (`stdio`)
```bash
dotnet tool install --global RazvanApps.McpServer.Blueprint
razvanapps-mcp-blueprint
```

### Run locally over Streamable HTTP
```bash
dotnet run --project src/RazvanApps.McpServer.Blueprint -- --http
```

MCP endpoint: `http://localhost:5000/mcp`  
Health endpoint: `http://localhost:5000/health`

### Client Configuration (`mcp.json`)
```json
{
  "mcpServers": {
    "razvanapps-mcp-blueprint": {
      "command": "razvanapps-mcp-blueprint"
    }
  }
}
```

---

## Verification

```bash
dotnet test -c Release
dotnet pack src/RazvanApps.McpServer.Blueprint -c Release -o ./artifacts
```

## License

MIT — [Răzvan Mareș (RazvanApps)](https://razvanmares.com)
