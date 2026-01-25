using System.Text;
using Mjm.LocalDocs.Core.Abstractions;
using UglyToad.PdfPig;

namespace Mjm.LocalDocs.Infrastructure.Documents;

/// <summary>
/// Document reader for PDF files (.pdf) using PdfPig library.
/// </summary>
/// <remarks>
/// This reader extracts native text content from PDFs. It does NOT perform OCR,
/// so scanned/image-based PDFs will return no extractable text.
/// </remarks>
public sealed class PdfDocumentReader : IDocumentReader
{
    /// <inheritdoc />
    public IReadOnlyList<string> SupportedExtensions => [".pdf"];

    /// <inheritdoc />
    public bool CanRead(string fileExtension)
        => fileExtension.Equals(".pdf", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public Task<TextExtractionResult> ExtractTextAsync(
        byte[] fileContent,
        CancellationToken cancellationToken = default)
    {
        if (fileContent.Length == 0)
        {
            return Task.FromResult(TextExtractionResult.Fail(
                "The PDF file is empty."));
        }

        try
        {
            using var document = PdfDocument.Open(fileContent);

            if (document.NumberOfPages == 0)
            {
                return Task.FromResult(TextExtractionResult.Fail(
                    "The PDF file contains no pages."));
            }

            var textBuilder = new StringBuilder();

            foreach (var page in document.GetPages())
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                var pageText = page.Text;
                if (!string.IsNullOrEmpty(pageText))
                {
                    textBuilder.AppendLine(pageText);
                }
            }

            var extractedText = textBuilder.ToString().Trim();

            if (string.IsNullOrWhiteSpace(extractedText))
            {
                return Task.FromResult(TextExtractionResult.Fail(
                    "No extractable text found in the PDF. " +
                    "The document may be scanned/image-based and requires OCR, " +
                    "or it may contain only non-text elements (images, graphics)."));
            }

            return Task.FromResult(TextExtractionResult.Ok(extractedText));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Task.FromResult(TextExtractionResult.Fail(
                $"Failed to read PDF: {ex.Message}"));
        }
    }
}
