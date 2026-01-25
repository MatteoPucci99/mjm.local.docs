using Mjm.LocalDocs.Core.Abstractions;

namespace Mjm.LocalDocs.Infrastructure.Documents;

/// <summary>
/// Aggregates multiple document readers and delegates to the appropriate one based on file extension.
/// </summary>
public sealed class CompositeDocumentReader : IDocumentReader
{
    private readonly IReadOnlyList<IDocumentReader> _readers;
    private readonly IReadOnlyList<string> _supportedExtensions;

    /// <summary>
    /// Creates a new composite document reader with the specified readers.
    /// </summary>
    /// <param name="readers">The document readers to aggregate.</param>
    public CompositeDocumentReader(IEnumerable<IDocumentReader> readers)
    {
        _readers = readers.ToList();
        _supportedExtensions = _readers
            .SelectMany(r => r.SupportedExtensions)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <inheritdoc />
    public IReadOnlyList<string> SupportedExtensions => _supportedExtensions;

    /// <inheritdoc />
    public bool CanRead(string fileExtension)
        => _readers.Any(r => r.CanRead(fileExtension));

    /// <inheritdoc />
    public Task<TextExtractionResult> ExtractTextAsync(
        byte[] fileContent,
        CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException(
            "CompositeDocumentReader requires a file extension to determine the appropriate reader. " +
            "Use ExtractTextAsync(byte[], string, CancellationToken) instead.");
    }

    /// <summary>
    /// Extracts text content from the file bytes using the appropriate reader for the file extension.
    /// </summary>
    /// <param name="fileContent">The raw bytes of the document file.</param>
    /// <param name="fileExtension">The file extension (e.g., ".pdf", ".txt").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The extraction result containing the text or an error message.</returns>
    public Task<TextExtractionResult> ExtractTextAsync(
        byte[] fileContent,
        string fileExtension,
        CancellationToken cancellationToken = default)
    {
        var reader = _readers.FirstOrDefault(r => r.CanRead(fileExtension));

        if (reader is null)
        {
            var supportedFormats = string.Join(", ", _supportedExtensions);
            return Task.FromResult(TextExtractionResult.Fail(
                $"Unsupported file format '{fileExtension}'. Supported formats: {supportedFormats}"));
        }

        return reader.ExtractTextAsync(fileContent, cancellationToken);
    }
}
