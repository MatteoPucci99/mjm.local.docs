namespace Mjm.LocalDocs.Core.Abstractions;

/// <summary>
/// Result of a text extraction operation from a document.
/// </summary>
public sealed class TextExtractionResult
{
    /// <summary>
    /// The extracted text content. Empty if extraction failed or no text found.
    /// </summary>
    public required string Text { get; init; }

    /// <summary>
    /// Indicates whether text was successfully extracted.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Error or warning message if extraction failed or produced no text.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Creates a successful extraction result.
    /// </summary>
    /// <param name="text">The extracted text content.</param>
    /// <returns>A successful result containing the extracted text.</returns>
    public static TextExtractionResult Ok(string text) => new()
    {
        Text = text,
        Success = true
    };

    /// <summary>
    /// Creates a failed extraction result with an error message.
    /// </summary>
    /// <param name="errorMessage">Description of why extraction failed.</param>
    /// <returns>A failed result with the error message.</returns>
    public static TextExtractionResult Fail(string errorMessage) => new()
    {
        Text = string.Empty,
        Success = false,
        ErrorMessage = errorMessage
    };
}

/// <summary>
/// Extracts text content from document files of various formats.
/// </summary>
public interface IDocumentReader
{
    /// <summary>
    /// Gets the file extensions supported by this reader (e.g., ".pdf", ".txt").
    /// Extensions should include the leading dot and be lowercase.
    /// </summary>
    IReadOnlyList<string> SupportedExtensions { get; }

    /// <summary>
    /// Determines if this reader can handle the given file extension.
    /// </summary>
    /// <param name="fileExtension">The file extension to check (e.g., ".pdf").</param>
    /// <returns>True if this reader supports the extension; otherwise, false.</returns>
    bool CanRead(string fileExtension);

    /// <summary>
    /// Extracts text content from the file bytes.
    /// </summary>
    /// <param name="fileContent">The raw bytes of the document file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The extraction result containing the text or an error message.</returns>
    Task<TextExtractionResult> ExtractTextAsync(
        byte[] fileContent,
        CancellationToken cancellationToken = default);
}
