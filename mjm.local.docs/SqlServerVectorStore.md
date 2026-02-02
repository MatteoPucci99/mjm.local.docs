# Azure SQL Server Vector Store

## Overview

`SqlServerVectorStore` provides persistent storage and fast approximate nearest neighbor (ANN) vector search using Azure SQL Database or SQL Server 2025+.

## Features

- **Native VECTOR(n) type** - Efficient binary storage for embeddings
- **VECTOR_SEARCH with DiskANN** - O(log n) ANN search with high recall
- **Full transactional support** - ACID-compliant updates
- **Tool-friendly** - Works with SSMS, Azure Data Studio, etc.
- **Scalable** - Azure SQL auto-scales with your data

## Requirements

| Platform | Minimum Version |
|-----------|----------------|
| Azure SQL Database | Current |
| Azure SQL Managed Instance | SQL Server 2025 (Always-up-to-date) |
| SQL Server on-prem | SQL Server 2025 |

## Configuration

```json
{
  "ConnectionStrings": {
    "SqlServer": "Server=myserver.database.windows.net;Database=localdocs;..."
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

### Connection String Examples

**Azure SQL Database with Managed Identity:**
```
Server=myserver.database.windows.net;Database=localdocs;Authentication=Active Directory Default;Encrypt=True;
```

**Azure SQL Database with SQL Authentication:**
```
Server=myserver.database.windows.net;Database=localdocs;User Id=myuser;Password=mypassword;Encrypt=True;
```

**SQL Server on-premises:**
```
Server=localhost;Database=localdocs;Integrated Security=True;TrustServerCertificate=True;
```

## SQL Schema

The store automatically creates the following schema on first use:

```sql
CREATE TABLE [dbo].[chunk_embeddings] (
    chunk_id NVARCHAR(255) PRIMARY KEY,
    embedding VECTOR(1536) NOT NULL
);

CREATE VECTOR INDEX vec_idx_chunk_embeddings 
ON [dbo].[chunk_embeddings](embedding)
WITH (metric = 'cosine');
```

## Distance Metrics

Supported distance metrics:

| Metric | Description |
|---------|-------------|
| `cosine` | Cosine similarity (default, recommended for embeddings) |
| `euclidean` | Euclidean (L2) distance |
| `dotproduct` | Dot product similarity (higher is more similar) |

## Performance

| Operation | Complexity | Typical Latency |
|-----------|-------------|------------------|
| Upsert (single) | O(log n) | 5-10 ms |
| Upsert (batch 100) | O(n × log n) | 100-200 ms |
| Search (ANN) | O(log n) | 10-50 ms |
| Search (exact) | O(n) | 100-500 ms (use for <50K vectors) |

**Recommendations:**
- Use `UseVectorIndex: true` for datasets with 10,000+ vectors
- Use exact search (set `UseVectorIndex: false`) for <50,000 vectors if precision is critical
- Vector index is automatically created on first run

## Migration from SQLite

To migrate from SQLite to Azure SQL Server:

1. **Export from SQLite:**
```bash
dotnet run --project src/Mjm.LocalDocs.Server/Mjm.LocalDocs.Server.csproj
# Export documents first
```

2. **Configure Azure SQL connection string** in `appsettings.json`

3. **Import to Azure SQL:**
The application will create the schema automatically on first run.

## Troubleshooting

### "Vector type not supported"

Ensure you're using:
- SQL Server 2025+ or Azure SQL Database
- Latest version of `Microsoft.EntityFrameworkCore.SqlServer` (10.0.2+)

### "VECTOR_SEARCH function not found"

Ensure:
- `UseVectorIndex: true` is set in configuration
- Vector index has been created (automatic on first run)

### Connection Timeouts

Increase connection timeout in connection string:
```
Connection Timeout=60;Command Timeout=300
```
