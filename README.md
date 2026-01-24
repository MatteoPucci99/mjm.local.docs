# mjm.local.docs

Local MCP Server for semantic document search via embeddings. Built with .NET 10, Microsoft Semantic Kernel, and Blazor Server.

## Quick Start

```bash
cd mjm.local.docs
dotnet restore
dotnet run --project src/Mjm.LocalDocs.Server/Mjm.LocalDocs.Server.csproj
```

The server will start at `http://localhost:5024` with the MCP endpoint at `/mcp`.

## Adding to Claude Code

Add the MCP server to your Claude Code configuration:

**Option 1: Project-level configuration** (`.claude/settings.json` in your project root):

```json
{
  "mcpServers": {
    "local-docs": {
      "type": "http",
      "url": "http://localhost:5024/mcp"
    }
  }
}
```

**Option 2: User-level configuration** (`~/.claude/settings.json`):

```json
{
  "mcpServers": {
    "local-docs": {
      "type": "http",
      "url": "http://localhost:5024/mcp"
    }
  }
}
```

After adding the configuration, restart Claude Code. The `search_docs`, `add_document`, and `list_collections` tools will be available.

## Adding to OpenCode

Add the MCP server to your OpenCode configuration file (`opencode.json` or `.opencode.json`):

```json
{
  "mcpServers": {
    "local-docs": {
      "type": "http",
      "url": "http://localhost:5024/mcp"
    }
  }
}
```

Restart OpenCode to load the new MCP server.

## Available MCP Tools

| Tool | Description |
|------|-------------|
| `search_docs` | Semantic search across documents |
| `add_document` | Add a new document to the store |
| `list_collections` | List all document collections |

## Development

Uses fake embeddings in development mode (no API key required). For production, configure a real embedding provider (OpenAI, Azure, Ollama).

See [AGENTS.md](AGENTS.md) for detailed build commands, code style, and architecture guidelines.

## License

MIT
