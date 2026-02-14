using Mjm.LocalDocs.Core.Configuration;
using Mjm.LocalDocs.Infrastructure.FileStorage;

namespace Mjm.LocalDocs.Tests.FileStorage;

/// <summary>
/// Unit tests for <see cref="FileSystemDocumentFileStorage"/>.
/// </summary>
public sealed class FileSystemDocumentFileStorageTests : IDisposable
{
    private readonly string _testBasePath;
    private readonly FileSystemDocumentFileStorage _sut;

    public FileSystemDocumentFileStorageTests()
    {
        _testBasePath = Path.Combine(Path.GetTempPath(), $"FileStorageTests_{Guid.NewGuid():N}");
        var options = new FileSystemStorageOptions
        {
            BasePath = _testBasePath,
            CreateDirectoryIfNotExists = true
        };
        _sut = new FileSystemDocumentFileStorage(options);
    }

    public void Dispose()
    {
        // Cleanup test directory
        if (Directory.Exists(_testBasePath))
        {
            Directory.Delete(_testBasePath, recursive: true);
        }
    }

    #region SaveFileAsync Tests

    [Fact]
    public async Task SaveFileAsync_CreatesFileWithCorrectPath()
    {
        // Arrange
        var documentId = "doc-123";
        var projectId = "proj-456";
        var fileName = "test-document.pdf";
        var content = "Test file content"u8.ToArray();

        // Act
        var storageLocation = await _sut.SaveFileAsync(documentId, projectId, fileName, content);

        // Assert
        var expectedPath = Path.Combine(projectId, $"{documentId}.pdf");
        Assert.Equal(expectedPath, storageLocation);
        var fullPath = Path.Combine(_testBasePath, storageLocation);
        Assert.True(File.Exists(fullPath));
        Assert.Equal(content, await File.ReadAllBytesAsync(fullPath));
    }

    [Fact]
    public async Task SaveFileAsync_CreatesProjectDirectory()
    {
        // Arrange
        var documentId = "doc-123";
        var projectId = "new-project";
        var fileName = "test.txt";
        var content = "Content"u8.ToArray();

        // Act
        await _sut.SaveFileAsync(documentId, projectId, fileName, content);

        // Assert
        var projectDir = Path.Combine(_testBasePath, projectId);
        Assert.True(Directory.Exists(projectDir));
    }

    #endregion

    #region GetFileAsync Tests

    [Fact]
    public async Task GetFileAsync_WhenFileExists_ReturnsContent()
    {
        // Arrange
        var documentId = "doc-123";
        var projectId = "proj-456";
        var fileName = "test.txt";
        var content = "Test file content"u8.ToArray();
        var storageLocation = await _sut.SaveFileAsync(documentId, projectId, fileName, content);

        // Act
        var result = await _sut.GetFileAsync(documentId, storageLocation);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(content, result);
    }

    [Fact]
    public async Task GetFileAsync_WhenFileDoesNotExist_ReturnsNull()
    {
        // Arrange
        var storageLocation = "non-existent/file.txt";

        // Act
        var result = await _sut.GetFileAsync("doc-123", storageLocation);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetFileAsync_WhenStorageLocationIsNull_ReturnsNull()
    {
        // Act
        var result = await _sut.GetFileAsync("doc-123", null);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetFileStreamAsync Tests

    [Fact]
    public async Task GetFileStreamAsync_WhenFileExists_ReturnsStream()
    {
        // Arrange
        var documentId = "doc-123";
        var projectId = "proj-456";
        var fileName = "test.txt";
        var content = "Test file content"u8.ToArray();
        var storageLocation = await _sut.SaveFileAsync(documentId, projectId, fileName, content);

        // Act
        await using var stream = await _sut.GetFileStreamAsync(documentId, storageLocation);

        // Assert
        Assert.NotNull(stream);
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        Assert.Equal(content, memoryStream.ToArray());
    }

    [Fact]
    public async Task GetFileStreamAsync_WhenFileDoesNotExist_ReturnsNull()
    {
        // Arrange
        var storageLocation = "non-existent/file.txt";

        // Act
        var result = await _sut.GetFileStreamAsync("doc-123", storageLocation);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region DeleteFileAsync Tests

    [Fact]
    public async Task DeleteFileAsync_WhenFileExists_DeletesAndReturnsTrue()
    {
        // Arrange
        var documentId = "doc-123";
        var projectId = "proj-456";
        var fileName = "test.txt";
        var content = "Test file content"u8.ToArray();
        var storageLocation = await _sut.SaveFileAsync(documentId, projectId, fileName, content);
        var fullPath = Path.Combine(_testBasePath, storageLocation);

        // Act
        var result = await _sut.DeleteFileAsync(documentId, storageLocation);

        // Assert
        Assert.True(result);
        Assert.False(File.Exists(fullPath));
    }

    [Fact]
    public async Task DeleteFileAsync_WhenFileDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var storageLocation = "non-existent/file.txt";

        // Act
        var result = await _sut.DeleteFileAsync("doc-123", storageLocation);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteFileAsync_RemovesEmptyParentDirectory()
    {
        // Arrange
        var documentId = "doc-123";
        var projectId = "proj-to-delete";
        var fileName = "test.txt";
        var content = "Content"u8.ToArray();
        var storageLocation = await _sut.SaveFileAsync(documentId, projectId, fileName, content);
        var projectDir = Path.Combine(_testBasePath, projectId);

        // Act
        await _sut.DeleteFileAsync(documentId, storageLocation);

        // Assert
        Assert.False(Directory.Exists(projectDir));
    }

    #endregion

    #region FileExistsAsync Tests

    [Fact]
    public async Task FileExistsAsync_WhenFileExists_ReturnsTrue()
    {
        // Arrange
        var documentId = "doc-123";
        var projectId = "proj-456";
        var fileName = "test.txt";
        var content = "Content"u8.ToArray();
        var storageLocation = await _sut.SaveFileAsync(documentId, projectId, fileName, content);

        // Act
        var result = await _sut.FileExistsAsync(documentId, storageLocation);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task FileExistsAsync_WhenFileDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var storageLocation = "non-existent/file.txt";

        // Act
        var result = await _sut.FileExistsAsync("doc-123", storageLocation);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task FileExistsAsync_WhenStorageLocationIsNull_ReturnsFalse()
    {
        // Act
        var result = await _sut.FileExistsAsync("doc-123", null);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Path Traversal Protection Tests

    [Fact]
    public async Task GetFileAsync_WithPathTraversal_ReturnsNull()
    {
        // Arrange - try to access file outside base path
        var maliciousPath = "../../../etc/passwd";

        // Act
        var result = await _sut.GetFileAsync("doc-123", maliciousPath);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteFileAsync_WithPathTraversal_ReturnsFalse()
    {
        // Arrange - try to delete file outside base path
        var maliciousPath = "..\\..\\..\\important-file.txt";

        // Act
        var result = await _sut.DeleteFileAsync("doc-123", maliciousPath);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task FileExistsAsync_WithPathTraversal_ReturnsFalse()
    {
        // Arrange - try to check file outside base path
        var maliciousPath = "../../../etc/passwd";

        // Act
        var result = await _sut.FileExistsAsync("doc-123", maliciousPath);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetFileStreamAsync_WithPathTraversal_ReturnsNull()
    {
        // Arrange - try to access file outside base path
        var maliciousPath = "..\\..\\Windows\\System32\\config\\SAM";

        // Act
        var result = await _sut.GetFileStreamAsync("doc-123", maliciousPath);

        // Assert
        Assert.Null(result);
    }

    #endregion
}
