using Mjm.LocalDocs.Core.Abstractions;
using Mjm.LocalDocs.Core.Models;

namespace Mjm.LocalDocs.Infrastructure.Documents;

/// <summary>
/// Simple document processor that splits text by paragraphs/sections.
/// </summary>
public sealed class SimpleDocumentProcessor : IDocumentProcessor
{
    private readonly int _maxChunkSize;
    private readonly int _overlapSize;

    /// <summary>
    /// Creates a new document processor.
    /// </summary>
    /// <param name="maxChunkSize">Maximum characters per chunk (default 1000).</param>
    /// <param name="overlapSize">Overlap between chunks for context (default 100).</param>
    public SimpleDocumentProcessor(int maxChunkSize = 1000, int overlapSize = 100)
    {
        _maxChunkSize = maxChunkSize;
        _overlapSize = overlapSize;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<DocumentChunk>> ChunkDocumentAsync(
        Document document, 
        CancellationToken cancellationToken = default)
    {
        var chunks = new List<DocumentChunk>();
        var content = document.Content;

        if (string.IsNullOrWhiteSpace(content))
            return Task.FromResult<IReadOnlyList<DocumentChunk>>(chunks);

        // Split by double newlines (paragraphs) first
        var paragraphs = content.Split(["\n\n", "\r\n\r\n"], StringSplitOptions.RemoveEmptyEntries);
        
        var currentChunk = "";
        var chunkIndex = 0;

        foreach (var paragraph in paragraphs)
        {
            var trimmedParagraph = paragraph.Trim();
            
            if (string.IsNullOrEmpty(trimmedParagraph))
                continue;

            // If adding this paragraph exceeds max size, save current chunk
            if (currentChunk.Length + trimmedParagraph.Length > _maxChunkSize && currentChunk.Length > 0)
            {
                chunks.Add(CreateChunk(document, currentChunk.Trim(), chunkIndex++));
                
                // Keep overlap from previous chunk
                currentChunk = currentChunk.Length > _overlapSize 
                    ? currentChunk[^_overlapSize..] 
                    : "";
            }

            currentChunk += (currentChunk.Length > 0 ? "\n\n" : "") + trimmedParagraph;

            // If single paragraph is too large, split it
            while (currentChunk.Length > _maxChunkSize)
            {
                var splitPoint = FindSplitPoint(currentChunk, _maxChunkSize);
                chunks.Add(CreateChunk(document, currentChunk[..splitPoint].Trim(), chunkIndex++));
                currentChunk = currentChunk[Math.Max(0, splitPoint - _overlapSize)..];
            }
        }

        // Don't forget the last chunk
        if (!string.IsNullOrWhiteSpace(currentChunk))
        {
            chunks.Add(CreateChunk(document, currentChunk.Trim(), chunkIndex));
        }

        return Task.FromResult<IReadOnlyList<DocumentChunk>>(chunks);
    }

    private static DocumentChunk CreateChunk(Document document, string content, int index)
    {
        return new DocumentChunk
        {
            Id = $"{document.Id}_chunk_{index}",
            Content = content,
            DocumentId = document.Id,
            Collection = document.Collection,
            Title = document.Title,
            ChunkIndex = index,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    private static int FindSplitPoint(string text, int maxLength)
    {
        // Try to split at sentence boundary
        var searchStart = Math.Min(maxLength, text.Length) - 1;
        
        for (var i = searchStart; i > maxLength / 2; i--)
        {
            if (text[i] == '.' || text[i] == '!' || text[i] == '?')
                return i + 1;
        }

        // Fall back to word boundary
        for (var i = searchStart; i > maxLength / 2; i--)
        {
            if (char.IsWhiteSpace(text[i]))
                return i;
        }

        // Last resort: hard cut
        return maxLength;
    }
}
