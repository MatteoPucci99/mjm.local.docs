using Mjm.LocalDocs.Infrastructure.Documents;
using NPOI.XWPF.UserModel;

namespace Mjm.LocalDocs.Tests.Documents;

/// <summary>
/// Unit tests for <see cref="WordDocumentReader"/>.
/// </summary>
public sealed class WordDocumentReaderTests
{
    private readonly WordDocumentReader _sut = new();

    #region SupportedExtensions Tests

    [Fact]
    public void SupportedExtensions_ReturnsOnlyDocx()
    {
        // Assert
        Assert.Single(_sut.SupportedExtensions);
        Assert.Contains(".docx", _sut.SupportedExtensions);
    }

    #endregion

    #region CanRead Tests

    [Theory]
    [InlineData(".docx", true)]
    [InlineData(".DOCX", true)]
    [InlineData(".Docx", true)]
    [InlineData(".doc", false)]
    [InlineData(".DOC", false)]
    [InlineData(".txt", false)]
    [InlineData(".pdf", false)]
    [InlineData(".xlsx", false)]
    [InlineData("", false)]
    public void CanRead_ReturnsExpectedResult(string extension, bool expected)
    {
        // Act
        var result = _sut.CanRead(extension);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region ExtractTextAsync Tests - Empty/Invalid

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
    public async Task ExtractTextAsync_WithInvalidDocument_ReturnsFailure()
    {
        // Arrange - random bytes that are not a valid Word document
        var fileContent = new byte[] { 0x00, 0x01, 0x02, 0x03, 0xFF, 0xFE, 0xFD };

        // Act
        var result = await _sut.ExtractTextAsync(fileContent);

        // Assert
        Assert.False(result.Success);
        Assert.Empty(result.Text);
        Assert.Contains("Invalid .docx file format", result.ErrorMessage);
    }

    [Fact]
    public async Task ExtractTextAsync_WithLegacyDocFormat_ReturnsFailureWithConversionHint()
    {
        // Arrange - OLE2 compound document signature (legacy .doc)
        var fileContent = new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 };

        // Act
        var result = await _sut.ExtractTextAsync(fileContent);

        // Assert
        Assert.False(result.Success);
        Assert.Empty(result.Text);
        Assert.Contains("legacy .doc format", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("convert to .docx", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region ExtractTextAsync Tests - .docx Format

    [Fact]
    public async Task ExtractTextAsync_WithValidDocxContainingText_ReturnsSuccess()
    {
        // Arrange
        var docxContent = CreateDocxWithText("Hello World from DOCX");

        // Act
        var result = await _sut.ExtractTextAsync(docxContent);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("Hello", result.Text);
        Assert.Contains("World", result.Text);
        Assert.Contains("DOCX", result.Text);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task ExtractTextAsync_WithDocxMultipleParagraphs_ExtractsAllText()
    {
        // Arrange
        var docxContent = CreateDocxWithMultipleParagraphs(
            "First paragraph content.",
            "Second paragraph content.",
            "Third paragraph content.");

        // Act
        var result = await _sut.ExtractTextAsync(docxContent);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("First paragraph", result.Text);
        Assert.Contains("Second paragraph", result.Text);
        Assert.Contains("Third paragraph", result.Text);
    }

    [Fact]
    public async Task ExtractTextAsync_WithEmptyDocx_ReturnsFailure()
    {
        // Arrange - valid docx structure but no text content
        var docxContent = CreateEmptyDocx();

        // Act
        var result = await _sut.ExtractTextAsync(docxContent);

        // Assert
        Assert.False(result.Success);
        Assert.Empty(result.Text);
        Assert.Contains("No extractable text", result.ErrorMessage);
    }

    #endregion

    #region Cancellation Tests

    [Fact]
    public async Task ExtractTextAsync_SupportsCancellation()
    {
        // Arrange
        var docxContent = CreateDocxWithText("Test content");
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _sut.ExtractTextAsync(docxContent, cts.Token));
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a valid .docx file containing the specified text.
    /// </summary>
    private static byte[] CreateDocxWithText(string text)
    {
        using var stream = new MemoryStream();
        using (var document = new XWPFDocument())
        {
            var paragraph = document.CreateParagraph();
            var run = paragraph.CreateRun();
            run.SetText(text);
            document.Write(stream);
        }
        return stream.ToArray();
    }

    /// <summary>
    /// Creates a valid .docx file with multiple paragraphs.
    /// </summary>
    private static byte[] CreateDocxWithMultipleParagraphs(params string[] paragraphs)
    {
        using var stream = new MemoryStream();
        using (var document = new XWPFDocument())
        {
            foreach (var text in paragraphs)
            {
                var paragraph = document.CreateParagraph();
                var run = paragraph.CreateRun();
                run.SetText(text);
            }
            document.Write(stream);
        }
        return stream.ToArray();
    }

    /// <summary>
    /// Creates an empty .docx file (valid structure but no text).
    /// </summary>
    private static byte[] CreateEmptyDocx()
    {
        using var stream = new MemoryStream();
        using (var document = new XWPFDocument())
        {
            // Just create an empty paragraph with no text
            document.CreateParagraph();
            document.Write(stream);
        }
        return stream.ToArray();
    }

    #endregion
}
