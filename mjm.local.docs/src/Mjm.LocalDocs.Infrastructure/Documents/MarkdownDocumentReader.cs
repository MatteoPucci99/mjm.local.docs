using System.Text;
using Mjm.LocalDocs.Core.Abstractions;

namespace Mjm.LocalDocs.Infrastructure.Documents;

/// <summary>
/// Document reader for Markdown files (.md).
/// </summary>
public sealed class MarkdownDocumentReader : IDocumentReader
{
    /// <inheritdoc />
    public IReadOnlyList<string> SupportedExtensions => [".md"];

    /// <inheritdoc />
    public bool CanRead(string fileExtension)
        => fileExtension.Equals(".md", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public Task<TextExtractionResult> ExtractTextAsync(
        byte[] fileContent,
        CancellationToken cancellationToken = default)
    {
        if (fileContent.Length == 0)
        {
            return Task.FromResult(TextExtractionResult.Fail(
                "The Markdown file is empty."));
        }

        try
        {
            var text = Encoding.UTF8.GetString(fileContent);

            if (string.IsNullOrWhiteSpace(text))
            {
                return Task.FromResult(TextExtractionResult.Fail(
                    "The Markdown file contains only whitespace."));
            }

            return Task.FromResult(TextExtractionResult.Ok(text));
        }
        catch (Exception ex)
        {
            return Task.FromResult(TextExtractionResult.Fail(
                $"Failed to read Markdown file: {ex.Message}"));
        }
    }
}
