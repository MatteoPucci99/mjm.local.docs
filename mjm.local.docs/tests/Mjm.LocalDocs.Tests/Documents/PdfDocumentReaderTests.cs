using Mjm.LocalDocs.Infrastructure.Documents;

namespace Mjm.LocalDocs.Tests.Documents;

/// <summary>
/// Unit tests for <see cref="PdfDocumentReader"/>.
/// </summary>
public sealed class PdfDocumentReaderTests
{
    private readonly PdfDocumentReader _sut = new();

    #region SupportedExtensions Tests

    [Fact]
    public void SupportedExtensions_ReturnsOnlyPdf()
    {
        // Assert
        Assert.Single(_sut.SupportedExtensions);
        Assert.Contains(".pdf", _sut.SupportedExtensions);
    }

    #endregion

    #region CanRead Tests

    [Theory]
    [InlineData(".pdf", true)]
    [InlineData(".PDF", true)]
    [InlineData(".Pdf", true)]
    [InlineData(".txt", false)]
    [InlineData(".docx", false)]
    [InlineData("", false)]
    public void CanRead_ReturnsExpectedResult(string extension, bool expected)
    {
        // Act
        var result = _sut.CanRead(extension);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region ExtractTextAsync Tests

    [Fact]
    public async Task ExtractTextAsync_WithEmptyFile_ReturnsFailure()
    {
        // Arrange
        var fileContent = Array.Empty<byte>();

        // Act
        var result = await _sut.ExtractTextAsync(fileContent);

        // Assert
        Assert.False(result.Success);
        Assert.Empty(result.Text);
        Assert.Contains("empty", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExtractTextAsync_WithInvalidPdf_ReturnsFailure()
    {
        // Arrange - random bytes that are not a valid PDF
        var fileContent = new byte[] { 0x00, 0x01, 0x02, 0x03, 0xFF, 0xFE, 0xFD };

        // Act
        var result = await _sut.ExtractTextAsync(fileContent);

        // Assert
        Assert.False(result.Success);
        Assert.Empty(result.Text);
        Assert.Contains("Failed to read PDF", result.ErrorMessage);
    }

    [Fact]
    public async Task ExtractTextAsync_WithValidPdfContainingText_ReturnsSuccess()
    {
        // Arrange - minimal valid PDF with text "Hello World"
        var pdfContent = CreateMinimalPdfWithText("Hello World");

        // Act
        var result = await _sut.ExtractTextAsync(pdfContent);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("Hello", result.Text);
        Assert.Contains("World", result.Text);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task ExtractTextAsync_WithPdfNoExtractableText_ReturnsFailureWithOcrHint()
    {
        // Arrange - minimal valid PDF with no text content (empty page)
        var pdfContent = CreateMinimalPdfWithoutText();

        // Act
        var result = await _sut.ExtractTextAsync(pdfContent);

        // Assert
        Assert.False(result.Success);
        Assert.Empty(result.Text);
        Assert.Contains("No extractable text", result.ErrorMessage);
        Assert.Contains("OCR", result.ErrorMessage);
    }

    [Fact]
    public async Task ExtractTextAsync_SupportsCancellation()
    {
        // Arrange
        var pdfContent = CreateMinimalPdfWithText("Test content");
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _sut.ExtractTextAsync(pdfContent, cts.Token));
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a minimal valid PDF containing the specified text.
    /// </summary>
    private static byte[] CreateMinimalPdfWithText(string text)
    {
        // This is a minimal valid PDF 1.4 structure with text
        var pdf = $@"%PDF-1.4
1 0 obj
<< /Type /Catalog /Pages 2 0 R >>
endobj
2 0 obj
<< /Type /Pages /Kids [3 0 R] /Count 1 >>
endobj
3 0 obj
<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >>
endobj
4 0 obj
<< /Length 44 >>
stream
BT /F1 12 Tf 100 700 Td ({text}) Tj ET
endstream
endobj
5 0 obj
<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>
endobj
xref
0 6
0000000000 65535 f 
0000000009 00000 n 
0000000058 00000 n 
0000000115 00000 n 
0000000266 00000 n 
0000000359 00000 n 
trailer
<< /Size 6 /Root 1 0 R >>
startxref
434
%%EOF";

        return System.Text.Encoding.ASCII.GetBytes(pdf);
    }

    /// <summary>
    /// Creates a minimal valid PDF with no text content (empty page).
    /// </summary>
    private static byte[] CreateMinimalPdfWithoutText()
    {
        // This is a minimal valid PDF with an empty page (no text streams)
        var pdf = @"%PDF-1.4
1 0 obj
<< /Type /Catalog /Pages 2 0 R >>
endobj
2 0 obj
<< /Type /Pages /Kids [3 0 R] /Count 1 >>
endobj
3 0 obj
<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] >>
endobj
xref
0 4
0000000000 65535 f 
0000000009 00000 n 
0000000058 00000 n 
0000000115 00000 n 
trailer
<< /Size 4 /Root 1 0 R >>
startxref
191
%%EOF";

        return System.Text.Encoding.ASCII.GetBytes(pdf);
    }

    #endregion
}
