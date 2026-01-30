# Getting Started with mjm.local.docs

A local MCP Server for semantic document search via embeddings. Built with .NET 10, Microsoft Semantic Kernel, and Blazor Server.

---

## Table of Contents

- [Prerequisites](#prerequisites)
- [Quick Start](#quick-start)
- [Configuration Wizard](#configuration-wizard)
- [Configuration Guide](#configuration-guide)
  - [Storage Providers](#1-storage-providers-metadata--vector-store)
  - [Embedding Providers](#2-embedding-providers)
  - [File Storage Providers](#3-file-storage-providers)
  - [Authentication & Server](#4-authentication--server)
  - [Document Chunking](#5-document-chunking)
- [Complete Configuration Examples](#complete-configuration-examples)
- [Environment Variables Reference](#environment-variables-reference)
- [MCP Client Configuration](#mcp-client-configuration)
- [Troubleshooting](#troubleshooting)

---

## Prerequisites

| Requirement | Version | Notes |
|-------------|---------|-------|
| .NET SDK | 10.0.102+ | Required |
| OpenAI API Key | - | Optional, for production embeddings |
| SQL Server | 2025+ / Azure SQL | Optional, for enterprise deployments |
| Azure Storage Account | - | Optional, for cloud file storage |

---

## Quick Start

Get up and running in 5 minutes with the default development configuration.

### 1. Clone and Build

```bash
cd mjm.local.docs
dotnet restore
dotnet build
```

### 2. Run the Server

```bash
dotnet run --project src/Mjm.LocalDocs.Server/Mjm.LocalDocs.Server.csproj
```

### 3. Access the Application

- **Web UI:** http://localhost:5024
- **MCP Endpoint:** http://localhost:5024/mcp
- **Default credentials:** `admin` / `admin`

The default configuration uses:
- **SQLite** for metadata and vector storage
- **Fake embeddings** (deterministic, no API calls)
- **Database** file storage (inline in SQLite)

---

## Configuration Wizard

Use this decision tree to choose the right configuration for your needs:

### Step 1: Choose Storage Provider

```
How many documents will you store?
│
├─► Less than 100 (testing/dev) ────────► InMemory
│
├─► Less than 10,000 ───────────────────► Sqlite
│
├─► 10,000 - 100,000 ───────────────────► SqliteHnsw
│
└─► More than 100,000 or Enterprise ────► SqlServer
```

### Step 2: Choose Embedding Provider

```
What's your use case?
│
├─► Development/Testing ────────────────► Fake (free, no API)
│
└─► Production (real semantic search) ──► OpenAI
```

### Step 3: Choose File Storage

```
Where should document files be stored?
│
├─► Simple setup (small files) ─────────► Database
│
├─► Large files or shared access ───────► FileSystem
│
└─► Cloud deployment ───────────────────► AzureBlob
```

### Quick Reference Table

| Scenario | Storage | Embeddings | FileStorage |
|----------|---------|------------|-------------|
| Local development | `Sqlite` | `Fake` | `Database` |
| Small team (< 10K docs) | `Sqlite` | `OpenAI` | `FileSystem` |
| Medium deployment | `SqliteHnsw` | `OpenAI` | `FileSystem` |
| Enterprise / Azure | `SqlServer` | `OpenAI` | `AzureBlob` |

---

## Configuration Guide

All configuration is done in `appsettings.json` (or environment-specific files like `appsettings.Production.json`).

### Configuration Structure Overview

```json
{
  "ConnectionStrings": {
    "LocalDocs": "<connection-string>"
  },
  "LocalDocs": {
    "Server": { },
    "Authentication": { },
    "Mcp": { },
    "Embeddings": { },
    "Storage": { },
    "FileStorage": { },
    "Chunking": { }
  }
}
```

---

### 1. Storage Providers (Metadata & Vector Store)

Choose a storage provider based on your dataset size and performance requirements.

| Provider | Vector Search | Persistence | Best For |
|----------|---------------|-------------|----------|
| `InMemory` | Brute-force O(n) | None | Development, testing |
| `Sqlite` | Brute-force O(n) | Yes | Small datasets (<10K docs) |
| `SqliteHnsw` | Approximate O(log n) | Yes | Medium datasets (10K-100K docs) |
| `SqlServer` | ANN (DiskANN) | Yes | Large datasets, enterprise |

#### InMemory (Development Only)

No persistence - data is lost when the server stops.

```json
{
  "LocalDocs": {
    "Storage": {
      "Provider": "InMemory"
    }
  }
}
```

> **Note:** No connection string required.

---

#### SQLite (Small Deployments)

Simple, file-based database with brute-force vector search.

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

**Connection String Options:**

| Scenario | Connection String |
|----------|-------------------|
| Relative path | `Data Source=localdocs.db` |
| Absolute path | `Data Source=C:/data/localdocs.db` |
| In-memory (testing) | `Data Source=:memory:` |

---

#### SQLite + HNSW (Medium Deployments)

SQLite for metadata with a separate HNSW index file for fast approximate nearest neighbor search.

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

**HNSW Options:**

| Setting | Default | Range | Description |
|---------|---------|-------|-------------|
| `IndexPath` | `hnsw_index.bin` | - | Path to the HNSW index file |
| `MaxConnections` | `16` | 12-48 | Max connections per node (M). Higher = better recall, more memory |
| `EfConstruction` | `200` | 100-500 | Build quality. Higher = better index, slower build |
| `EfSearch` | `50` | 50-500 | Search quality. Higher = better recall, slower search |
| `AutoSaveDelayMs` | `5000` | 0+ | Auto-save delay in ms. Set `0` to disable |

**Tuning Tips:**
- For **better recall**: Increase `EfSearch` (e.g., 100-200)
- For **faster search**: Decrease `EfSearch` (e.g., 30-50)
- For **large datasets**: Increase `MaxConnections` to 24-32

---

#### SQL Server (Enterprise Deployments)

Uses native `VECTOR(n)` type with DiskANN-based approximate nearest neighbor search.

**Requirements:**
- SQL Server 2025+, Azure SQL Database, or Azure SQL Managed Instance

```json
{
  "ConnectionStrings": {
    "LocalDocs": "Server=myserver.database.windows.net;Database=localdocs;Authentication=Active Directory Default;Encrypt=True;"
  },
  "LocalDocs": {
    "Storage": {
      "Provider": "SqlServer",
      "SqlServer": {
        "Schema": "dbo",
        "TableName": "chunk_embeddings",
        "UseVectorIndex": true,
        "DistanceMetric": "cosine"
      }
    }
  }
}
```

**SQL Server Options:**

| Setting | Default | Description |
|---------|---------|-------------|
| `Schema` | `dbo` | Schema name for embeddings table. Useful for multi-tenant or isolated deployments |
| `TableName` | `chunk_embeddings` | Table name for embeddings. Allows multiple instances on the same database |
| `UseVectorIndex` | `true` | Use vector index for ANN search. Set `false` for exact k-NN (slower but more precise) |
| `DistanceMetric` | `cosine` | Distance metric: `cosine`, `euclidean`, `dotproduct` |

**Connection String Examples:**

| Scenario | Connection String |
|----------|-------------------|
| Azure SQL (Managed Identity) | `Server=myserver.database.windows.net;Database=localdocs;Authentication=Active Directory Default;Encrypt=True;` |
| Azure SQL (SQL Auth) | `Server=myserver.database.windows.net;Database=localdocs;User Id=myuser;Password=mypassword;Encrypt=True;` |
| SQL Server on-premises | `Server=localhost;Database=localdocs;Integrated Security=True;TrustServerCertificate=True;` |

**Auto-created Schema:**

The application automatically creates the required schema (if not `dbo`), table, and vector index based on your configuration:

```sql
-- Schema created if Schema != 'dbo'
CREATE SCHEMA [your_schema];

-- Table with configured name
CREATE TABLE [your_schema].[your_table] (
    chunk_id NVARCHAR(255) PRIMARY KEY,
    embedding VECTOR(1536) NOT NULL
);

-- Vector index created only if UseVectorIndex = true
CREATE VECTOR INDEX vec_idx_your_table 
ON [your_schema].[your_table](embedding)
WITH (metric = 'cosine');
```

**Multi-tenant Example:**

Use custom schema for tenant isolation:

```json
{
  "LocalDocs": {
    "Storage": {
      "Provider": "SqlServer",
      "SqlServer": {
        "Schema": "tenant_acme",
        "TableName": "embeddings",
        "UseVectorIndex": true,
        "DistanceMetric": "cosine"
      }
    }
  }
}
```

This creates `[tenant_acme].[embeddings]` table, completely isolated from other tenants.

---

### 2. Embedding Providers

Choose an embedding provider based on your use case.

| Provider | API Calls | Cost | Best For |
|----------|-----------|------|----------|
| `Fake` | None | Free | Development, testing |
| `OpenAI` | Yes | Pay-per-use | Production |

#### Fake Embeddings (Development)

Generates deterministic fake embeddings without any API calls.

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

> **Warning:** Fake embeddings produce meaningless vectors. Semantic search will not work correctly. Use only for development/testing.

---

#### OpenAI Embeddings (Production)

Uses OpenAI's embedding models for high-quality semantic search.

```json
{
  "LocalDocs": {
    "Embeddings": {
      "Provider": "OpenAI",
      "Dimension": 1536,
      "OpenAI": {
        "ApiKey": "sk-...",
        "Model": "text-embedding-3-small"
      }
    }
  }
}
```

**OpenAI Options:**

| Setting | Default | Required | Description |
|---------|---------|----------|-------------|
| `ApiKey` | - | Yes* | OpenAI API key (or use `OPENAI_API_KEY` env var) |
| `Model` | `text-embedding-3-small` | No | OpenAI embedding model |

*Required when `Provider` is `OpenAI`.

**Available Models:**

| Model | Dimensions | Cost | Notes |
|-------|------------|------|-------|
| `text-embedding-3-small` | 1536 | $0.02/1M tokens | Recommended for most use cases |
| `text-embedding-3-large` | 3072 | $0.13/1M tokens | Higher quality, higher cost |
| `text-embedding-ada-002` | 1536 | $0.10/1M tokens | Legacy model |

**Using Environment Variable:**

Instead of putting the API key in `appsettings.json`, use an environment variable:

```bash
# Linux/macOS
export OPENAI_API_KEY=sk-...

# Windows (PowerShell)
$env:OPENAI_API_KEY = "sk-..."

# Windows (CMD)
set OPENAI_API_KEY=sk-...
```

Then omit the `ApiKey` from config:

```json
{
  "LocalDocs": {
    "Embeddings": {
      "Provider": "OpenAI",
      "Dimension": 1536,
      "OpenAI": {
        "Model": "text-embedding-3-small"
      }
    }
  }
}
```

---

### 3. File Storage Providers

Choose where document files (PDF, Word, etc.) are stored.

| Provider | Storage Location | Best For |
|----------|------------------|----------|
| `Database` | Inline in database | Simple setups, small files |
| `FileSystem` | Local disk / network share | Large files, shared storage |
| `AzureBlob` | Azure Blob Storage | Cloud deployments |

#### Database Storage (Default)

Stores file content directly in the database.

```json
{
  "LocalDocs": {
    "FileStorage": {
      "Provider": "Database"
    }
  }
}
```

**Pros:** Simple, no external dependencies  
**Cons:** Database grows with file size, backup includes files

---

#### FileSystem Storage

Stores files on local disk or network share.

```json
{
  "LocalDocs": {
    "FileStorage": {
      "Provider": "FileSystem",
      "FileSystem": {
        "BasePath": "./DocumentFiles",
        "CreateDirectoryIfNotExists": true
      }
    }
  }
}
```

**FileSystem Options:**

| Setting | Default | Description |
|---------|---------|-------------|
| `BasePath` | `DocumentFiles` | Base path where files are stored |
| `CreateDirectoryIfNotExists` | `true` | Auto-create directory structure |

**File Path Structure:** `{BasePath}/{ProjectId}/{DocumentId}.{ext}`

**Examples:**

| Scenario | BasePath |
|----------|----------|
| Relative to app | `./DocumentFiles` |
| Absolute path | `C:/data/documents` |
| Network share | `//server/share/documents` |

---

#### Azure Blob Storage

Stores files in Azure Blob Storage.

```json
{
  "LocalDocs": {
    "FileStorage": {
      "Provider": "AzureBlob",
      "AzureBlob": {
        "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...;EndpointSuffix=core.windows.net",
        "ContainerName": "documents",
        "CreateContainerIfNotExists": true
      }
    }
  }
}
```

**Azure Blob Options:**

| Setting | Default | Required | Description |
|---------|---------|----------|-------------|
| `ConnectionString` | - | Yes* | Azure Storage connection string |
| `ContainerName` | `documents` | No | Blob container name |
| `CreateContainerIfNotExists` | `true` | No | Auto-create container |

*Required when `Provider` is `AzureBlob`. Can also use `AZURE_STORAGE_CONNECTION_STRING` env var.

**Using Environment Variable:**

```bash
export AZURE_STORAGE_CONNECTION_STRING="DefaultEndpointsProtocol=https;..."
```

---

### 4. Authentication & Server

#### Server Options

```json
{
  "LocalDocs": {
    "Server": {
      "UseHttps": false
    }
  }
}
```

| Setting | Default | Description |
|---------|---------|-------------|
| `UseHttps` | `false` | Enable HTTPS redirect. Set `false` when behind a reverse proxy (IIS, nginx, Azure App Service) |

#### Authentication

```json
{
  "LocalDocs": {
    "Authentication": {
      "Username": "admin",
      "Password": "your-secure-password"
    }
  }
}
```

| Setting | Required | Description |
|---------|----------|-------------|
| `Username` | Yes | Username for web UI and MCP authentication |
| `Password` | Yes | Password for authentication |

> **Security:** Change the default password in production!

#### MCP Options

```json
{
  "LocalDocs": {
    "Mcp": {
      "RequireAuthentication": true
    }
  }
}
```

| Setting | Default | Description |
|---------|---------|-------------|
| `RequireAuthentication` | `true` | Require authentication for MCP requests |

---

### 5. Document Chunking

Configure how documents are split into chunks for embedding.

```json
{
  "LocalDocs": {
    "Chunking": {
      "MaxChunkSize": 3000,
      "OverlapSize": 300
    }
  }
}
```

| Setting | Default | Description |
|---------|---------|-------------|
| `MaxChunkSize` | `3000` | Maximum characters per chunk |
| `OverlapSize` | `300` | Overlap between chunks for context continuity |

**Tuning Tips:**
- **Larger chunks** (4000-5000): Better context, fewer chunks, may miss specific details
- **Smaller chunks** (1000-2000): More precise matches, more chunks, may lose context
- **Overlap** (10-20% of chunk size): Prevents cutting important content at boundaries

---

## Complete Configuration Examples

### Development (Fake + SQLite)

Minimal setup for local development and testing.

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "LocalDocs": "Data Source=localdocs.db"
  },
  "LocalDocs": {
    "Server": {
      "UseHttps": false
    },
    "Authentication": {
      "Username": "admin",
      "Password": "admin"
    },
    "Mcp": {
      "RequireAuthentication": true
    },
    "Embeddings": {
      "Provider": "Fake",
      "Dimension": 1536
    },
    "Storage": {
      "Provider": "Sqlite"
    },
    "FileStorage": {
      "Provider": "Database"
    }
  }
}
```

---

### Production (OpenAI + SQLite)

Simple production setup with real embeddings.

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "LocalDocs": "Data Source=/data/localdocs.db"
  },
  "LocalDocs": {
    "Server": {
      "UseHttps": false
    },
    "Authentication": {
      "Username": "admin",
      "Password": "CHANGE_THIS_PASSWORD"
    },
    "Mcp": {
      "RequireAuthentication": true
    },
    "Embeddings": {
      "Provider": "OpenAI",
      "Dimension": 1536,
      "OpenAI": {
        "Model": "text-embedding-3-small"
      }
    },
    "Storage": {
      "Provider": "Sqlite"
    },
    "FileStorage": {
      "Provider": "FileSystem",
      "FileSystem": {
        "BasePath": "/data/documents",
        "CreateDirectoryIfNotExists": true
      }
    },
    "Chunking": {
      "MaxChunkSize": 3000,
      "OverlapSize": 300
    }
  }
}
```

> **Note:** Set `OPENAI_API_KEY` environment variable.

---

### Production (OpenAI + SQLite HNSW)

Medium-scale production with fast vector search.

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "LocalDocs": "Data Source=/data/localdocs.db"
  },
  "LocalDocs": {
    "Server": {
      "UseHttps": false
    },
    "Authentication": {
      "Username": "admin",
      "Password": "CHANGE_THIS_PASSWORD"
    },
    "Mcp": {
      "RequireAuthentication": true
    },
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
        "MaxConnections": 24,
        "EfConstruction": 200,
        "EfSearch": 100,
        "AutoSaveDelayMs": 5000
      }
    },
    "FileStorage": {
      "Provider": "FileSystem",
      "FileSystem": {
        "BasePath": "/data/documents",
        "CreateDirectoryIfNotExists": true
      }
    },
    "Chunking": {
      "MaxChunkSize": 3000,
      "OverlapSize": 300
    }
  }
}
```

---

### Enterprise (SQL Server + Azure Blob)

Full enterprise setup with Azure services.

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "LocalDocs": "Server=myserver.database.windows.net;Database=localdocs;Authentication=Active Directory Default;Encrypt=True;"
  },
  "LocalDocs": {
    "Server": {
      "UseHttps": false
    },
    "Authentication": {
      "Username": "admin",
      "Password": "CHANGE_THIS_PASSWORD"
    },
    "Mcp": {
      "RequireAuthentication": true
    },
    "Embeddings": {
      "Provider": "OpenAI",
      "Dimension": 1536,
      "OpenAI": {
        "Model": "text-embedding-3-small"
      }
    },
    "Storage": {
      "Provider": "SqlServer",
      "SqlServer": {
        "Schema": "dbo",
        "TableName": "chunk_embeddings",
        "UseVectorIndex": true,
        "DistanceMetric": "cosine"
      }
    },
    "FileStorage": {
      "Provider": "AzureBlob",
      "AzureBlob": {
        "ContainerName": "documents",
        "CreateContainerIfNotExists": true
      }
    },
    "Chunking": {
      "MaxChunkSize": 3000,
      "OverlapSize": 300
    }
  }
}
```

> **Note:** Set environment variables:
> - `OPENAI_API_KEY`
> - `AZURE_STORAGE_CONNECTION_STRING`

---

### Multi-tenant / Multi-instance (SQL Server)

Run multiple isolated instances on the same SQL Server database.

**Instance 1 (Team A):**
```json
{
  "ConnectionStrings": {
    "LocalDocs": "Server=myserver.database.windows.net;Database=shared_localdocs;..."
  },
  "LocalDocs": {
    "Storage": {
      "Provider": "SqlServer",
      "SqlServer": {
        "Schema": "team_a",
        "TableName": "chunk_embeddings"
      }
    }
  }
}
```

**Instance 2 (Team B):**
```json
{
  "ConnectionStrings": {
    "LocalDocs": "Server=myserver.database.windows.net;Database=shared_localdocs;..."
  },
  "LocalDocs": {
    "Storage": {
      "Provider": "SqlServer",
      "SqlServer": {
        "Schema": "team_b",
        "TableName": "chunk_embeddings"
      }
    }
  }
}
```

This creates separate tables `[team_a].[chunk_embeddings]` and `[team_b].[chunk_embeddings]`, fully isolated.

---

## Environment Variables Reference

| Variable | Config Path | Description |
|----------|-------------|-------------|
| `OPENAI_API_KEY` | `LocalDocs:Embeddings:OpenAI:ApiKey` | OpenAI API key for embeddings |
| `AZURE_STORAGE_CONNECTION_STRING` | `LocalDocs:FileStorage:AzureBlob:ConnectionString` | Azure Blob Storage connection string |

**ASP.NET Core Configuration Override:**

You can override any setting via environment variables using `__` as separator:

```bash
# Override storage provider
export LocalDocs__Storage__Provider=SqlServer

# Override connection string
export ConnectionStrings__LocalDocs="Server=localhost;Database=localdocs;..."
```

---

## MCP Client Configuration

### Claude Desktop

Add to your Claude Desktop configuration file:

**Windows:** `%APPDATA%\Claude\claude_desktop_config.json`  
**macOS:** `~/Library/Application Support/Claude/claude_desktop_config.json`

```json
{
  "mcpServers": {
    "localdocs": {
      "command": "npx",
      "args": [
        "mcp-remote",
        "http://localhost:5024/mcp",
        "--header",
        "Authorization: Basic YWRtaW46YWRtaW4="
      ]
    }
  }
}
```

> **Note:** `YWRtaW46YWRtaW4=` is Base64 for `admin:admin`. Generate your own:
> ```bash
> echo -n "username:password" | base64
> ```

### Generic MCP Client

- **Endpoint:** `http://localhost:5024/mcp`
- **Transport:** HTTP with SSE (Server-Sent Events)
- **Authentication:** Basic Auth (when `RequireAuthentication` is `true`)

---

## Troubleshooting

### Common Issues

#### "Connection string 'LocalDocs' not found"

Ensure `ConnectionStrings:LocalDocs` is set in your configuration:

```json
{
  "ConnectionStrings": {
    "LocalDocs": "Data Source=localdocs.db"
  }
}
```

#### "OpenAI API key not configured"

Either set the API key in config or via environment variable:

```bash
export OPENAI_API_KEY=sk-...
```

#### "Unable to create SQLite database"

Ensure the directory exists and has write permissions:

```bash
mkdir -p /data
chmod 755 /data
```

#### "SQL Server connection failed"

1. Verify the connection string format
2. Check firewall rules (port 1433)
3. Ensure the database exists
4. Verify authentication method

#### "Cannot create schema" (SQL Server)

If using a custom schema, ensure the database user has `CREATE SCHEMA` permission:

```sql
GRANT CREATE SCHEMA TO [your_user];
```

Or pre-create the schema manually:

```sql
CREATE SCHEMA [your_schema];
```

#### "HNSW index file not found"

The index file is created automatically on first use. Ensure the directory has write permissions:

```bash
chmod 755 /data
```

#### "Azure Blob Storage connection failed"

1. Verify the connection string
2. Check the container exists (or set `CreateContainerIfNotExists: true`)
3. Verify network access to Azure Storage

### Logging

Enable detailed logging for troubleshooting:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Information",
      "ModelContextProtocol": "Debug"
    }
  }
}
```

### Health Check

The application exposes a health endpoint (when running):

```bash
curl http://localhost:5024/health
```

---

## Next Steps

1. **Upload documents** via the Web UI at http://localhost:5024
2. **Create projects** to organize your documents
3. **Test search** using the MCP tools or Web UI
4. **Connect MCP clients** like Claude Desktop

For more information, see the [AGENTS.md](./AGENTS.md) file for development guidelines.
