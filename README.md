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

Uses fake embeddings in development mode (no API key required). For production, configure a real embedding provider.

See [AGENTS.md](AGENTS.md) for detailed build commands, code style, and architecture guidelines.

## Storage Configuration

LocalDocs supports multiple storage strategies for vector embeddings. Choose based on your dataset size and performance requirements.

### Storage Providers Overview

| Provider | Persistence | Search Complexity | Best For |
|----------|-------------|-------------------|----------|
| `InMemory` | None (lost on restart) | O(n) brute-force | Development, testing |
| `Sqlite` | SQLite database | O(n) brute-force | Small datasets (<10k documents) |
| `SqliteHnsw` | SQLite + binary file | O(log n) approximate | Production (10k+ documents) |

### InMemory

Stores all data in memory. Fast and simple, but data is lost when the server restarts.

**When to use:** Unit tests, rapid prototyping, demos.

```json
{
  "LocalDocs": {
    "Storage": {
      "Provider": "InMemory"
    }
  }
}
```

### Sqlite (Brute-Force Search)

Persists embeddings as BLOBs in a SQLite table (`chunk_embeddings`). Computes cosine similarity against **all** stored vectors for each search query.

**How it works:**
1. All embeddings are loaded into memory on startup
2. For each search, calculates cosine similarity against every vector
3. Returns top-k results sorted by similarity

**Pros:**
- Simple setup, single database file
- Exact results (not approximate)
- Good for small to medium datasets

**Cons:**
- O(n) search complexity - slows linearly with dataset size
- All embeddings must fit in memory

**When to use:** Datasets with fewer than 10,000 documents where exact search is preferred.

```json
{
  "ConnectionStrings": {
    "LocalDocs": "Data Source=localdocs.db"
  },
  "LocalDocs": {
    "Storage": {
      "Provider": "Sqlite"
    }
  }
}
```

### SqliteHnsw (Recommended for Production)

Combines SQLite for metadata (projects, documents, chunks) with an HNSW (Hierarchical Navigable Small World) graph for fast approximate nearest neighbor search.

**How it works:**
1. SQLite stores document metadata and text content
2. HNSW graph stores embeddings in a navigable multi-layer structure
3. Search traverses the graph in O(log n) time to find approximate nearest neighbors
4. Index auto-saves to a binary file with configurable debounce

**Pros:**
- O(log n) search - scales to millions of documents
- Persistent index survives restarts
- High recall (typically 95%+ accuracy)
- Configurable trade-offs between speed and accuracy

**Cons:**
- Approximate results (very accurate, but not exact)
- Higher memory usage than brute-force
- More parameters to tune

**When to use:** Production environments with 10,000+ documents.

```json
{
  "ConnectionStrings": {
    "LocalDocs": "Data Source=localdocs.db"
  },
  "LocalDocs": {
    "Storage": {
      "Provider": "SqliteHnsw",
      "Hnsw": {
        "IndexPath": "hnsw_index.bin",
        "MaxConnections": 16,
        "EfConstruction": 200,
        "EfSearch": 50,
        "AutoSaveDelayMs": 5000
      }
    }
  }
}
```

### HNSW Parameters Tuning

Fine-tune the HNSW index based on your requirements:

| Parameter | Default | Range | Description |
|-----------|---------|-------|-------------|
| `IndexPath` | `hnsw_index.bin` | - | Path to the binary index file |
| `MaxConnections` | 16 | 12-48 | Max edges per node (M parameter). Higher = better recall, more memory |
| `EfConstruction` | 200 | 100-500 | Build-time candidate list size. Higher = better quality index, slower build |
| `EfSearch` | 50 | 50-500 | Search-time candidate list size. Higher = better recall, slower search |
| `AutoSaveDelayMs` | 5000 | 0+ | Debounce delay before auto-saving index. 0 = disabled |

**Tuning Guidelines:**

| Dataset Size | MaxConnections | EfConstruction | EfSearch |
|--------------|----------------|----------------|----------|
| < 50k docs | 16 (default) | 200 (default) | 50 (default) |
| 50k - 500k docs | 24-32 | 300 | 100 |
| > 500k docs | 32-48 | 400-500 | 150-200 |

**Trade-offs:**
- **Higher recall needed?** Increase `EfSearch` (costs search speed)
- **Faster indexing?** Decrease `EfConstruction` (costs index quality)
- **Memory constrained?** Decrease `MaxConnections` (costs recall)

## Embedding Configuration

LocalDocs uses embeddings to convert text into vectors for semantic search. Choose an embedding provider based on your environment.

### Embedding Providers Overview

| Provider | API Key Required | Cost | Embedding Quality | Use Case |
|----------|------------------|------|-------------------|----------|
| `Fake` | No | Free | Random (deterministic) | Development, testing |
| `OpenAI` | Yes | Pay-per-use | High quality | Production |

### Fake Embeddings (Development)

Generates deterministic pseudo-random vectors based on text hash. No external API calls required.

**How it works:**
- Hashes the input text to seed a random number generator
- Generates a vector of the configured dimension
- Same text always produces the same vector

**When to use:** Local development, unit tests, CI/CD pipelines.

```json
{
  "LocalDocs": {
    "Embeddings": {
      "Provider": "Fake",
      "Dimension": 1536
    }
  }
}
```

### OpenAI Embeddings (Production)

Uses OpenAI's embedding API for high-quality semantic vectors.

**Available Models:**

| Model | Dimensions | Performance | Cost |
|-------|------------|-------------|------|
| `text-embedding-3-small` | 1536 | Good | Lowest |
| `text-embedding-3-large` | 3072 | Best | Higher |
| `text-embedding-ada-002` | 1536 | Good (legacy) | Medium |

**API Key Configuration:**

Option 1: Environment variable (recommended for production):
```bash
export OPENAI_API_KEY=sk-your-api-key-here
```

Option 2: Configuration file (for development):
```json
{
  "LocalDocs": {
    "Embeddings": {
      "Provider": "OpenAI",
      "Dimension": 1536,
      "OpenAI": {
        "ApiKey": "sk-your-api-key-here",
        "Model": "text-embedding-3-small"
      }
    }
  }
}
```

**Security Note:** Never commit API keys to source control. Use environment variables or user secrets for production.

## Full Configuration Example

Complete `appsettings.json` for a production deployment:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "LocalDocs": "Data Source=/data/localdocs.db"
  },
  "LocalDocs": {
    "Embeddings": {
      "Provider": "OpenAI",
      "Dimension": 1536,
      "OpenAI": {
        "Model": "text-embedding-3-small"
      }
    },
    "Storage": {
      "Provider": "SqliteHnsw",
      "Hnsw": {
        "IndexPath": "/data/hnsw_index.bin",
        "MaxConnections": 16,
        "EfConstruction": 200,
        "EfSearch": 50,
        "AutoSaveDelayMs": 5000
      }
    }
  }
}
```

**Development configuration** (default, no API key needed):

```json
{
  "LocalDocs": {
    "Embeddings": {
      "Provider": "Fake",
      "Dimension": 1536
    },
    "Storage": {
      "Provider": "SqliteHnsw"
    }
  }
}
```

## License

MIT
