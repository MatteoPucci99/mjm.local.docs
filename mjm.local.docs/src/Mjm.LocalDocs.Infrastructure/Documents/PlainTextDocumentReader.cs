using System.Text;
using Mjm.LocalDocs.Core.Abstractions;

namespace Mjm.LocalDocs.Infrastructure.Documents;

/// <summary>
/// Document reader for plain text files (.txt).
/// </summary>
public sealed class PlainTextDocumentReader : IDocumentReader
{
    /// <inheritdoc />
    public IReadOnlyList<string> SupportedExtensions => [".txt"];

    /// <inheritdoc />
    public bool CanRead(string fileExtension)
        => fileExtension.Equals(".txt", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public Task<TextExtractionResult> ExtractTextAsync(
        byte[] fileContent,
        CancellationToken cancellationToken = default)
    {
        if (fileContent.Length == 0)
        {
            return Task.FromResult(TextExtractionResult.Fail(
                "The text file is empty."));
        }

        try
        {
            var text = Encoding.UTF8.GetString(fileContent);

            if (string.IsNullOrWhiteSpace(text))
            {
                return Task.FromResult(TextExtractionResult.Fail(
                    "The text file contains only whitespace."));
            }

            return Task.FromResult(TextExtractionResult.Ok(text));
        }
        catch (Exception ex)
        {
            return Task.FromResult(TextExtractionResult.Fail(
                $"Failed to read text file: {ex.Message}"));
        }
    }
}
