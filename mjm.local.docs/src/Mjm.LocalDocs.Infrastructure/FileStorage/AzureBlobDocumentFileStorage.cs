using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Mjm.LocalDocs.Core.Abstractions;
using Mjm.LocalDocs.Core.Configuration;

namespace Mjm.LocalDocs.Infrastructure.FileStorage;

/// <summary>
/// Stores document file content in Azure Blob Storage.
/// Files are organized as: {ContainerName}/{ProjectId}/{DocumentId}{Extension}
/// </summary>
public sealed class AzureBlobDocumentFileStorage : IDocumentFileStorage
{
    private readonly BlobContainerClient _containerClient;

    /// <summary>
    /// Creates a new instance of AzureBlobDocumentFileStorage.
    /// </summary>
    /// <param name="options">The Azure Blob storage options.</param>
    /// <exception cref="InvalidOperationException">Thrown when connection string is not configured.</exception>
    public AzureBlobDocumentFileStorage(AzureBlobStorageOptions options)
    {
        var connectionString = options.ConnectionString
            ?? Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING");

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException(
                "Azure Blob Storage connection string is required. " +
                "Configure 'LocalDocs:FileStorage:AzureBlob:ConnectionString' in appsettings.json " +
                "or set the AZURE_STORAGE_CONNECTION_STRING environment variable.");
        }

        var blobServiceClient = new BlobServiceClient(connectionString);
        _containerClient = blobServiceClient.GetBlobContainerClient(options.ContainerName);

        if (options.CreateContainerIfNotExists)
        {
            _containerClient.CreateIfNotExists(PublicAccessType.None);
        }
    }

    /// <summary>
    /// Creates a new instance of AzureBlobDocumentFileStorage with a pre-configured container client.
    /// Useful for testing.
    /// </summary>
    /// <param name="containerClient">The blob container client.</param>
    public AzureBlobDocumentFileStorage(BlobContainerClient containerClient)
    {
        _containerClient = containerClient;
    }

    /// <inheritdoc />
    public async Task<string> SaveFileAsync(
        string documentId,
        string projectId,
        string fileName,
        byte[] content,
        CancellationToken cancellationToken = default)
    {
        // Build the blob path: {projectId}/{documentId}{extension}
        var extension = Path.GetExtension(fileName);
        var blobPath = $"{projectId}/{documentId}{extension}";

        var blobClient = _containerClient.GetBlobClient(blobPath);

        // Upload with content type based on extension
        var contentType = GetContentType(extension);
        var options = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
        };

        await using var stream = new MemoryStream(content);
        await blobClient.UploadAsync(stream, options, cancellationToken);

        return blobPath;
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

        var blobClient = _containerClient.GetBlobClient(storageLocation);

        if (!await blobClient.ExistsAsync(cancellationToken))
        {
            return null;
        }

        var response = await blobClient.DownloadContentAsync(cancellationToken);
        return response.Value.Content.ToArray();
    }

    /// <inheritdoc />
    public async Task<Stream?> GetFileStreamAsync(
        string documentId,
        string? storageLocation,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(storageLocation))
        {
            return null;
        }

        var blobClient = _containerClient.GetBlobClient(storageLocation);

        if (!await blobClient.ExistsAsync(cancellationToken))
        {
            return null;
        }

        var response = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken);
        return response.Value.Content;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteFileAsync(
        string documentId,
        string? storageLocation,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(storageLocation))
        {
            return false;
        }

        var blobClient = _containerClient.GetBlobClient(storageLocation);
        var response = await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
        return response.Value;
    }

    /// <inheritdoc />
    public async Task<bool> FileExistsAsync(
        string documentId,
        string? storageLocation,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(storageLocation))
        {
            return false;
        }

        var blobClient = _containerClient.GetBlobClient(storageLocation);
        var response = await blobClient.ExistsAsync(cancellationToken);
        return response.Value;
    }

    private static string GetContentType(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".txt" => "text/plain",
            ".md" => "text/markdown",
            ".json" => "application/json",
            ".xml" => "application/xml",
            ".html" or ".htm" => "text/html",
            ".csv" => "text/csv",
            _ => "application/octet-stream"
        };
    }
}
