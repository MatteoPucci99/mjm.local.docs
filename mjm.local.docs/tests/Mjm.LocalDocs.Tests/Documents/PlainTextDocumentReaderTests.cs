using System.Text;
using Mjm.LocalDocs.Infrastructure.Documents;

namespace Mjm.LocalDocs.Tests.Documents;

/// <summary>
/// Unit tests for <see cref="PlainTextDocumentReader"/>.
/// </summary>
public sealed class PlainTextDocumentReaderTests
{
    private readonly PlainTextDocumentReader _sut = new();

    #region SupportedExtensions Tests

    [Fact]
    public void SupportedExtensions_ReturnsOnlyTxt()
    {
        // Assert
        Assert.Single(_sut.SupportedExtensions);
        Assert.Contains(".txt", _sut.SupportedExtensions);
    }

    #endregion

    #region CanRead Tests

    [Theory]
    [InlineData(".txt", true)]
    [InlineData(".TXT", true)]
    [InlineData(".Txt", true)]
    [InlineData(".pdf", false)]
    [InlineData(".docx", false)]
    [InlineData(".md", false)]
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
    public async Task ExtractTextAsync_WithValidContent_ReturnsSuccess()
    {
        // Arrange
        var content = "Hello, this is a test document with some content.";
        var fileContent = Encoding.UTF8.GetBytes(content);

        // Act
        var result = await _sut.ExtractTextAsync(fileContent);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(content, result.Text);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task ExtractTextAsync_WithUnicodeContent_ReturnsSuccess()
    {
        // Arrange
        var content = "Ciao mondo! こんにちは世界 🌍";
        var fileContent = Encoding.UTF8.GetBytes(content);

        // Act
        var result = await _sut.ExtractTextAsync(fileContent);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(content, result.Text);
    }

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
    public async Task ExtractTextAsync_WithOnlyWhitespace_ReturnsFailure()
    {
        // Arrange
        var content = "   \n\t\r\n   ";
        var fileContent = Encoding.UTF8.GetBytes(content);

        // Act
        var result = await _sut.ExtractTextAsync(fileContent);

        // Assert
        Assert.False(result.Success);
        Assert.Empty(result.Text);
        Assert.Contains("whitespace", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExtractTextAsync_WithMultilineContent_PreservesNewlines()
    {
        // Arrange
        var content = "Line 1\nLine 2\r\nLine 3";
        var fileContent = Encoding.UTF8.GetBytes(content);

        // Act
        var result = await _sut.ExtractTextAsync(fileContent);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(content, result.Text);
    }

    #endregion
}
