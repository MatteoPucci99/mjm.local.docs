using System.Globalization;
using System.Text;
using Microsoft.Data.SqlClient;
using Mjm.LocalDocs.Core.Abstractions;

namespace Mjm.LocalDocs.Infrastructure.Persistence;

/// <summary>
/// SQL Server/Azure SQL vector store implementation using raw SQL.
/// Stores embeddings in a separate chunk_embeddings table with native VECTOR type.
/// Uses string-formatted vectors for inserts (e.g., '[0.1, 0.2, 0.3]').
/// </summary>
public sealed class SqlServerVectorStore : IVectorStore
{
    private readonly string _connectionString;
    private readonly int _dimension;
    private readonly string _distanceMetric;

    public SqlServerVectorStore(
        string connectionString,
        int dimension = 1536,
        string distanceMetric = "cosine")
    {
        _connectionString = connectionString;
        _dimension = dimension;
        _distanceMetric = distanceMetric;
    }

    /// <inheritdoc />
    public async Task UpsertAsync(
        string chunkId,
        ReadOnlyMemory<float> embedding,
        CancellationToken cancellationToken = default)
    {
        var vectorString = EmbeddingToString(embedding.Span);

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        // Use MERGE for upsert
        var sql = $"""
            MERGE INTO chunk_embeddings AS target
            USING (SELECT @chunkId AS chunk_id) AS source
            ON target.chunk_id = source.chunk_id
            WHEN MATCHED THEN
                UPDATE SET embedding = CAST(@embedding AS VECTOR({_dimension}))
            WHEN NOT MATCHED THEN
                INSERT (chunk_id, embedding)
                VALUES (@chunkId, CAST(@embedding AS VECTOR({_dimension})));
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@chunkId", chunkId);
        command.Parameters.AddWithValue("@embedding", vectorString);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpsertBatchAsync(
        IEnumerable<KeyValuePair<string, ReadOnlyMemory<float>>> embeddings,
        CancellationToken cancellationToken = default)
    {
        var embeddingsList = embeddings.ToList();
        if (embeddingsList.Count == 0) return;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        // Process in batches to avoid command size limits
        const int batchSize = 100;
        foreach (var batch in embeddingsList.Chunk(batchSize))
        {
            await UpsertBatchInternalAsync(connection, batch, cancellationToken);
        }
    }

    private async Task UpsertBatchInternalAsync(
        SqlConnection connection,
        IEnumerable<KeyValuePair<string, ReadOnlyMemory<float>>> batch,
        CancellationToken cancellationToken)
    {
        var batchList = batch.ToList();
        if (batchList.Count == 0) return;

        // Build a multi-row MERGE statement
        var sb = new StringBuilder();
        sb.AppendLine("MERGE INTO chunk_embeddings AS target");
        sb.AppendLine("USING (VALUES");

        var parameters = new List<SqlParameter>();
        for (var i = 0; i < batchList.Count; i++)
        {
            var (chunkId, embedding) = batchList[i];
            var vectorString = EmbeddingToString(embedding.Span);

            if (i > 0) sb.AppendLine(",");
            sb.Append($"    (@chunkId{i}, CAST(@embedding{i} AS VECTOR({_dimension})))");

            parameters.Add(new SqlParameter($"@chunkId{i}", chunkId));
            parameters.Add(new SqlParameter($"@embedding{i}", vectorString));
        }

        sb.AppendLine();
        sb.AppendLine(") AS source (chunk_id, embedding)");
        sb.AppendLine("ON target.chunk_id = source.chunk_id");
        sb.AppendLine("WHEN MATCHED THEN");
        sb.AppendLine("    UPDATE SET embedding = source.embedding");
        sb.AppendLine("WHEN NOT MATCHED THEN");
        sb.AppendLine("    INSERT (chunk_id, embedding)");
        sb.AppendLine("    VALUES (source.chunk_id, source.embedding);");

        await using var command = new SqlCommand(sb.ToString(), connection);
        command.Parameters.AddRange(parameters.ToArray());

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(
        string chunkId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = "DELETE FROM chunk_embeddings WHERE chunk_id = @chunkId";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@chunkId", chunkId);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteByDocumentIdAsync(
        string documentId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        // Chunk IDs follow pattern: {documentId}_chunk_{index}
        const string sql = "DELETE FROM chunk_embeddings WHERE chunk_id LIKE @pattern";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@pattern", $"{documentId}_chunk_%");

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        ReadOnlyMemory<float> queryEmbedding,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var vectorString = EmbeddingToString(queryEmbedding.Span);

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        // Use VECTOR_DISTANCE for similarity search
        var sql = $"""
            SELECT TOP (@limit) 
                chunk_id,
                VECTOR_DISTANCE('{_distanceMetric}', embedding, CAST(@queryEmbedding AS VECTOR({_dimension}))) AS distance
            FROM chunk_embeddings
            ORDER BY distance ASC
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@limit", limit);
        command.Parameters.AddWithValue("@queryEmbedding", vectorString);

        var results = new List<VectorSearchResult>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var chunkId = reader.GetString(0);
            var distance = reader.GetDouble(1);

            results.Add(new VectorSearchResult
            {
                ChunkId = chunkId,
                Score = DistanceToSimilarity(distance)
            });
        }

        return results;
    }

    /// <summary>
    /// Converts an embedding to a SQL Server vector string format: '[0.1, 0.2, 0.3, ...]'
    /// </summary>
    private static string EmbeddingToString(ReadOnlySpan<float> embedding)
    {
        var sb = new StringBuilder(embedding.Length * 12); // Estimate size
        sb.Append('[');

        for (var i = 0; i < embedding.Length; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append(embedding[i].ToString("G9", CultureInfo.InvariantCulture));
        }

        sb.Append(']');
        return sb.ToString();
    }

    /// <summary>
    /// Converts distance to similarity score (higher = more similar).
    /// </summary>
    private static double DistanceToSimilarity(double distance)
    {
        // For cosine distance: similarity = 1 - distance
        // For euclidean: similarity = 1 / (1 + distance)
        return 1.0 / (1.0 + distance);
    }
}
