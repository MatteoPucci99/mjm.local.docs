# mjm.local.docs

A self-hosted MCP server that lets AI coding agents (Claude Code, OpenCode, etc.) search your local documents using semantic search. Add your specs, docs, or notes — your AI tools find them automatically.

Built with .NET 10, SQLite, and Blazor Server.

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (10.0.102+)

## Quick Start

```bash
git clone https://github.com/markjackmilian/mjm.local.docs
cd mjm.local.docs/mjm.local.docs
dotnet run --project src/Mjm.LocalDocs.Server/Mjm.LocalDocs.Server.csproj
```

Open `http://localhost:5024` for the web UI. The MCP endpoint is at `http://localhost:5024/mcp`.

> No API key needed by default — uses fake embeddings in development.

## Connect to Your AI Agent

Add to `.claude/settings.json` (project) or `~/.claude/settings.json` (global):

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

Same config works for OpenCode (`opencode.json`). Restart your agent after adding.

## Available MCP Tools

| Tool | Description |
|------|-------------|
| `search_docs` | Semantic search across all documents |
| `add_document` | Add a `.txt`, `.md`, `.pdf`, or `.docx` file |
| `update_document` | Replace a document with a new version |
| `list_collections` | List all projects |
| `delete_document` | Remove a document and its embeddings |

## Configuration

All settings live under the `"LocalDocs"` key in `appsettings.json`.

**Embedding providers:** `Fake` (default, no key), `OpenAI`, `AzureOpenAI`, `Ollama`

**Storage providers:** `InMemory`, `Sqlite` (default), `SqliteHnsw` (recommended for >10k docs), `SqlServer`

**File storage:** `Database` (default), `FileSystem`, `AzureBlob`

Production example — OpenAI embeddings + HNSW index:

```json
{
  "ConnectionStrings": { "LocalDocs": "Data Source=/data/localdocs.db" },
  "LocalDocs": {
    "Embeddings": { "Provider": "OpenAI", "Dimension": 1536, "OpenAI": { "Model": "text-embedding-3-small" } },
    "Storage": { "Provider": "SqliteHnsw", "Hnsw": { "IndexPath": "/data/hnsw_index.bin" } }
  }
}
```

Set `OPENAI_API_KEY` as an environment variable — never commit API keys.

## License

MIT
