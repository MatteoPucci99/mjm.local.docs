using Mjm.LocalDocs.Core.Abstractions;
using NPOI.XWPF.Extractor;
using NPOI.XWPF.UserModel;

namespace Mjm.LocalDocs.Infrastructure.Documents;

/// <summary>
/// Document reader for Microsoft Word files (.docx) using NPOI library.
/// </summary>
/// <remarks>
/// This reader extracts text content from Word documents including:
/// - Paragraphs
/// - Tables
/// - Headers and footers
/// 
/// Supports modern .docx (Office Open XML) format.
/// Legacy .doc format is not supported - users should convert to .docx first.
/// </remarks>
public sealed class WordDocumentReader : IDocumentReader
{
    /// <inheritdoc />
    public IReadOnlyList<string> SupportedExtensions => [".docx"];

    /// <inheritdoc />
    public bool CanRead(string fileExtension)
        => fileExtension.Equals(".docx", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public Task<TextExtractionResult> ExtractTextAsync(
        byte[] fileContent,
        CancellationToken cancellationToken = default)
    {
        if (fileContent.Length == 0)
        {
            return Task.FromResult(TextExtractionResult.Fail(
                "The Word document is empty."));
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Verify it's a valid .docx file (ZIP-based)
            if (!IsDocxFormat(fileContent))
            {
                return Task.FromResult(TextExtractionResult.Fail(
                    "Invalid .docx file format. The file may be corrupted or in legacy .doc format. " +
                    "Legacy .doc format is not supported - please convert to .docx first."));
            }

            var extractedText = ExtractFromDocx(fileContent, cancellationToken);

            if (string.IsNullOrWhiteSpace(extractedText))
            {
                return Task.FromResult(TextExtractionResult.Fail(
                    "No extractable text found in the Word document. " +
                    "The document may be empty or contain only non-text elements (images, graphics)."));
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
                $"Failed to read Word document: {ex.Message}"));
        }
    }

    /// <summary>
    /// Extracts text from a .docx file (Office Open XML format).
    /// </summary>
    private static string ExtractFromDocx(byte[] fileContent, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream(fileContent);
        using var document = new XWPFDocument(stream);
        
        cancellationToken.ThrowIfCancellationRequested();
        
        var extractor = new XWPFWordExtractor(document);
        var text = extractor.Text;
        
        return text?.Trim() ?? string.Empty;
    }

    /// <summary>
    /// Determines if the file content is in .docx format (ZIP-based Office Open XML).
    /// </summary>
    /// <remarks>
    /// .docx files are ZIP archives and start with the ZIP signature (PK).
    /// .doc files are OLE2 compound documents and start with D0 CF 11 E0.
    /// </remarks>
    private static bool IsDocxFormat(byte[] fileContent)
    {
        if (fileContent.Length < 4)
            return false;

        // ZIP signature: 50 4B 03 04 (PK..)
        return fileContent[0] == 0x50 && 
               fileContent[1] == 0x4B && 
               fileContent[2] == 0x03 && 
               fileContent[3] == 0x04;
    }
}
