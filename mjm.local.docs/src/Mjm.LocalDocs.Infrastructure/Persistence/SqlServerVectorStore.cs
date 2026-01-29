using System.Text;
using Microsoft.Data.SqlClient;
using Mjm.LocalDocs.Core.Abstractions;

namespace Mjm.LocalDocs.Infrastructure.Persistence;

/// <summary>
/// SQL Server implementation of vector store using native VECTOR type and VECTOR_SEARCH.
/// Requires SQL Server 2025+, Azure SQL Database, or Azure SQL Managed Instance.
/// Uses DiskANN-based vector index for approximate nearest neighbor (ANN) search.
/// </summary>
public sealed class SqlServerVectorStore : IVectorStore, IAsyncDisposable
{
    private readonly string _connectionString;
    private readonly int _embeddingDimension;
    private readonly string _schema;
    private readonly string _tableName;
    private readonly bool _useVectorIndex;
    private readonly string _distanceMetric;
    private SqlConnection? _connection;
    private bool _initialized;

    public SqlServerVectorStore(
        string connectionString,
        int embeddingDimension = 1536,
        string schema = "dbo",
        string tableName = "chunk_embeddings",
        bool useVectorIndex = true,
        string distanceMetric = "cosine")
    {
        _connectionString = connectionString;
        _embeddingDimension = embeddingDimension;
        _schema = schema;
        _tableName = tableName;
        _useVectorIndex = useVectorIndex;
        _distanceMetric = distanceMetric;
    }

    private async Task<SqlConnection> GetConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (_connection != null && _connection.State == System.Data.ConnectionState.Open)
        {
            return _connection;
        }

        _connection = new SqlConnection(_connectionString);
        await _connection.OpenAsync(cancellationToken);
        return _connection;
    }

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized)
        {
            return;
        }

        var conn = await GetConnectionAsync(cancellationToken);
        
        var escapedSchema = _schema.Replace("]", "]]");
        var escapedTableName = _tableName.Replace("]", "]]");
        
        var createTableSql = $"""
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = '{_tableName}' AND schema_id = SCHEMA_ID('{_schema}'))
                BEGIN
                    CREATE TABLE [{escapedSchema}].[{escapedTableName}] (
                        chunk_id NVARCHAR(255) PRIMARY KEY,
                        embedding VECTOR({_embeddingDimension}) NOT NULL
                    );
                END
            """;

        await using (var createTableCmd = new SqlCommand(createTableSql, conn))
        {
            await createTableCmd.ExecuteNonQueryAsync(cancellationToken);
        }

        if (_useVectorIndex)
        {
            var indexName = $"vec_idx_{_tableName}";
            var createIndexSql = $"""
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = '{indexName}' AND object_id = OBJECT_ID('[{escapedSchema}].[{escapedTableName}]'))
                BEGIN
                    CREATE VECTOR INDEX [{indexName}] 
                    ON [{escapedSchema}].[{escapedTableName}](embedding)
                    WITH (metric = '{_distanceMetric}');
                END
                """;

            await using (var createIndexCmd = new SqlCommand(createIndexSql, conn))
            {
                await createIndexCmd.ExecuteNonQueryAsync(cancellationToken);
            }
        }

        _initialized = true;
    }

    /// <inheritdoc />
    public async Task UpsertAsync(
        string chunkId,
        ReadOnlyMemory<float> embedding,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        var conn = await GetConnectionAsync(cancellationToken);

        var escapedSchema = _schema.Replace("]", "]]");
        var escapedTableName = _tableName.Replace("]", "]]");

        var sql = $"""
            MERGE [{escapedSchema}].[{escapedTableName}] AS target
            USING (SELECT @chunkId AS chunk_id, CAST(@embedding AS VECTOR({_embeddingDimension})) AS embedding) AS source
            ON target.chunk_id = source.chunk_id
            WHEN MATCHED THEN UPDATE SET embedding = source.embedding
            WHEN NOT MATCHED THEN INSERT (chunk_id, embedding) VALUES (source.chunk_id, source.embedding);
            """;

        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@chunkId", chunkId);
        cmd.Parameters.AddWithValue("@embedding", EmbeddingToJson(embedding));
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpsertBatchAsync(
        IEnumerable<KeyValuePair<string, ReadOnlyMemory<float>>> embeddings,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        var conn = await GetConnectionAsync(cancellationToken);

        var escapedSchema = _schema.Replace("]", "]]");
        var escapedTableName = _tableName.Replace("]", "]]");

        var sql = $"""
            MERGE [{escapedSchema}].[{escapedTableName}] AS target
            USING (SELECT @chunkId AS chunk_id, CAST(@embedding AS VECTOR({_embeddingDimension})) AS embedding) AS source
            ON target.chunk_id = source.chunk_id
            WHEN MATCHED THEN UPDATE SET embedding = source.embedding
            WHEN NOT MATCHED THEN INSERT (chunk_id, embedding) VALUES (source.chunk_id, source.embedding);
            """;

        await using (var transaction = await conn.BeginTransactionAsync(cancellationToken))
        {
            try
            {
                foreach (var (chunkId, embedding) in embeddings)
                {
                    await using var cmd = new SqlCommand(sql, conn)
                    {
                        Transaction = (SqlTransaction)transaction
                    };
                    cmd.Parameters.AddWithValue("@chunkId", chunkId);
                    cmd.Parameters.AddWithValue("@embedding", EmbeddingToJson(embedding));
                    await cmd.ExecuteNonQueryAsync(cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }

    /// <inheritdoc />
    public async Task DeleteAsync(
        string chunkId,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        var conn = await GetConnectionAsync(cancellationToken);

        var escapedSchema = _schema.Replace("]", "]]");
        var escapedTableName = _tableName.Replace("]", "]]");

        var sql = $"DELETE FROM [{escapedSchema}].[{escapedTableName}] WHERE chunk_id = @chunkId";

        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@chunkId", chunkId);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteByDocumentIdAsync(
        string documentId,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        var conn = await GetConnectionAsync(cancellationToken);

        var escapedSchema = _schema.Replace("]", "]]");
        var escapedTableName = _tableName.Replace("]", "]]");

        var sql = $"DELETE FROM [{escapedSchema}].[{escapedTableName}] WHERE chunk_id LIKE @pattern";

        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@pattern", $"{documentId}_chunk_%");
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        ReadOnlyMemory<float> queryEmbedding,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        var conn = await GetConnectionAsync(cancellationToken);

        var escapedSchema = _schema.Replace("]", "]]");
        var escapedTableName = _tableName.Replace("]", "]]");

        List<VectorSearchResult> results;

        if (_useVectorIndex)
        {
            var sql = $"""
                SELECT t.chunk_id, s.distance
                FROM VECTOR_SEARCH(
                    TABLE = [{escapedSchema}].[{escapedTableName}] AS t,
                    COLUMN = embedding,
                    SIMILAR_TO = CAST(@queryEmbedding AS VECTOR({_embeddingDimension})),
                    METRIC = '{_distanceMetric}',
                    TOP_N = @limit
                ) AS s
                ORDER BY s.distance
                """;

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@queryEmbedding", EmbeddingToJson(queryEmbedding));
            cmd.Parameters.AddWithValue("@limit", limit);

            results = await ReadSearchResultsAsync(cmd, cancellationToken);
        }
        else
        {
            var sql = $"""
                SELECT TOP(@limit) chunk_id, 
                       VECTOR_DISTANCE('{_distanceMetric}', embedding, CAST(@queryEmbedding AS VECTOR({_embeddingDimension}))) AS distance
                FROM [{escapedSchema}].[{escapedTableName}]
                ORDER BY distance
                """;

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@queryEmbedding", EmbeddingToJson(queryEmbedding));
            cmd.Parameters.AddWithValue("@limit", limit);

            results = await ReadSearchResultsAsync(cmd, cancellationToken);
        }

        return results;
    }

    private static async Task<List<VectorSearchResult>> ReadSearchResultsAsync(
        SqlCommand cmd,
        CancellationToken cancellationToken)
    {
        var results = new List<VectorSearchResult>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            var distance = reader.GetDouble(1);
            results.Add(new VectorSearchResult
            {
                ChunkId = reader.GetString(0),
                Score = DistanceToSimilarity(distance)
            });
        }

        return results;
    }

    private static double DistanceToSimilarity(double distance)
    {
        return 1.0 / (1.0 + distance);
    }

    private static string EmbeddingToJson(ReadOnlyMemory<float> embedding)
    {
        var sb = new StringBuilder();
        sb.Append('[');

        var span = embedding.Span;
        for (int i = 0; i < span.Length; i++)
        {
            if (i > 0)
            {
                sb.Append(',');
            }
            sb.Append(span[i].ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        sb.Append(']');
        return sb.ToString();
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection != null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }
}
