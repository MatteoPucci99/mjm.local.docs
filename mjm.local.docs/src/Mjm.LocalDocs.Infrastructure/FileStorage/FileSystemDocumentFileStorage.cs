using Mjm.LocalDocs.Core.Abstractions;
using Mjm.LocalDocs.Core.Configuration;

namespace Mjm.LocalDocs.Infrastructure.FileStorage;

/// <summary>
/// Stores document file content on the local file system.
/// Files are organized as: {BasePath}/{ProjectId}/{DocumentId}{Extension}
/// </summary>
public sealed class FileSystemDocumentFileStorage : IDocumentFileStorage
{
    private readonly FileSystemStorageOptions _options;
    private readonly string _absoluteBasePath;

    /// <summary>
    /// Creates a new instance of FileSystemDocumentFileStorage.
    /// </summary>
    /// <param name="options">The file system storage options.</param>
    public FileSystemDocumentFileStorage(FileSystemStorageOptions options)
    {
        _options = options;
        _absoluteBasePath = Path.GetFullPath(_options.BasePath);

        if (_options.CreateDirectoryIfNotExists && !Directory.Exists(_absoluteBasePath))
        {
            Directory.CreateDirectory(_absoluteBasePath);
        }
    }

    /// <summary>
    /// Validates that a path is within the base path to prevent path traversal attacks.
    /// </summary>
    /// <param name="storageLocation">The storage location to validate.</param>
    /// <param name="fullPath">The resolved full path if valid.</param>
    /// <returns>True if the path is valid and within the base path.</returns>
    private bool TryGetSafePath(string storageLocation, out string fullPath)
    {
        fullPath = Path.GetFullPath(Path.Combine(_absoluteBasePath, storageLocation));
        
        // Ensure the resolved path starts with the base path to prevent path traversal
        return fullPath.StartsWith(_absoluteBasePath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            || fullPath.Equals(_absoluteBasePath, StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public async Task<string> SaveFileAsync(
        string documentId,
        string projectId,
        string fileName,
        byte[] content,
        CancellationToken cancellationToken = default)
    {
        // Build the relative path: {projectId}/{documentId}{extension}
        var extension = Path.GetExtension(fileName);
        var relativePath = Path.Combine(projectId, $"{documentId}{extension}");
        
        if (!TryGetSafePath(relativePath, out var fullPath))
        {
            throw new ArgumentException(
                $"Invalid storage path. The path '{relativePath}' resolves outside the allowed base directory.",
                nameof(projectId));
        }

        // Ensure directory exists
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Write file
        await File.WriteAllBytesAsync(fullPath, content, cancellationToken);

        // Return the relative path as storage location
        return relativePath;
    }

    /// <inheritdoc />
    public async Task<byte[]?> GetFileAsync(
        string documentId,
        string? storageLocation,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(storageLocation))
        {
            return null;
        }

        if (!TryGetSafePath(storageLocation, out var fullPath))
        {
            return null;
        }
        
        if (!File.Exists(fullPath))
        {
            return null;
        }

        return await File.ReadAllBytesAsync(fullPath, cancellationToken);
    }

    /// <inheritdoc />
    public Task<Stream?> GetFileStreamAsync(
        string documentId,
        string? storageLocation,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(storageLocation))
        {
            return Task.FromResult<Stream?>(null);
        }

        if (!TryGetSafePath(storageLocation, out var fullPath))
        {
            return Task.FromResult<Stream?>(null);
        }
        
        if (!File.Exists(fullPath))
        {
            return Task.FromResult<Stream?>(null);
        }

        // Return a FileStream - caller is responsible for disposing
        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult<Stream?>(stream);
    }

    /// <inheritdoc />
    public Task<bool> DeleteFileAsync(
        string documentId,
        string? storageLocation,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(storageLocation))
        {
            return Task.FromResult(false);
        }

        if (!TryGetSafePath(storageLocation, out var fullPath))
        {
            return Task.FromResult(false);
        }
        
        if (!File.Exists(fullPath))
        {
            return Task.FromResult(false);
        }

        File.Delete(fullPath);

        // Try to remove empty parent directory
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
        {
            try
            {
                if (!Directory.EnumerateFileSystemEntries(directory).Any())
                {
                    Directory.Delete(directory);
                }
            }
            catch (IOException)
            {
                // Ignore I/O errors when deleting empty directories (e.g., directory in use)
            }
            catch (UnauthorizedAccessException)
            {
                // Ignore permission errors when deleting empty directories
            }
        }

        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task<bool> FileExistsAsync(
        string documentId,
        string? storageLocation,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(storageLocation))
        {
            return Task.FromResult(false);
        }

        if (!TryGetSafePath(storageLocation, out var fullPath))
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(File.Exists(fullPath));
    }
}
