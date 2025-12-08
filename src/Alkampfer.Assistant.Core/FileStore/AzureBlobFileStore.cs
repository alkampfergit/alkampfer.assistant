using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Alkampfer.Assistant.Interfaces;

namespace Alkampfer.Assistant.Core.FileStore;

public class AzureBlobFileStore : IFileStore
{
    private readonly BlobContainerClient _containerClient;

    public AzureBlobFileStore(string connectionString, string containerName)
    {
        var blobServiceClient = new BlobServiceClient(connectionString);
        _containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        // We don't create the container here to avoid blocking IO in constructor.
        // It is expected to exist or be created by an initialization script/process.
        // However, for developer convenience, we could try to create it if we are sure it's safe.
        // For now, we assume it exists or will be created on first write if we change strategy.
    }

    /// <summary>
    /// Constructor for testing.
    /// </summary>
    public AzureBlobFileStore(BlobContainerClient containerClient)
    {
        _containerClient = containerClient;
    }

    public async Task SaveFileAsync(string path, Stream content, CancellationToken cancellationToken = default)
    {
        await _containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        var blobClient = _containerClient.GetBlobClient(path);
        await blobClient.UploadAsync(content, overwrite: true, cancellationToken: cancellationToken);
    }

    public async Task<Stream?> GetFileAsync(string path, CancellationToken cancellationToken = default)
    {
        var blobClient = _containerClient.GetBlobClient(path);
        if (!await blobClient.ExistsAsync(cancellationToken))
        {
            return null;
        }

        var response = await blobClient.DownloadAsync(cancellationToken);
        return response.Value.Content;
    }

    public async Task DeleteFileAsync(string path, CancellationToken cancellationToken = default)
    {
        var blobClient = _containerClient.GetBlobClient(path);
        await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<string>> ListFilesAsync(string prefix, CancellationToken cancellationToken = default)
    {
        var results = new List<string>();
        await foreach (var blobItem in _containerClient.GetBlobsAsync(prefix: prefix, cancellationToken: cancellationToken))
        {
            results.Add(blobItem.Name);
        }
        return results;
    }

    public Task<string?> GetPublicUrlAsync(string path, CancellationToken cancellationToken = default)
    {
        var blobClient = _containerClient.GetBlobClient(path);
        return Task.FromResult<string?>(blobClient.Uri.ToString());
    }
}
