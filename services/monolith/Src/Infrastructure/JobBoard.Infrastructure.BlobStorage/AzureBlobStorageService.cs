using System.Collections.Concurrent;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using JobBoard.Application.Interfaces.Storage;

namespace JobBoard.Infrastructure.BlobStorage;

public class AzureBlobStorageService(BlobServiceClient blobServiceClient) : IBlobStorageService
{
    private static readonly ConcurrentDictionary<string, bool> EnsuredContainers = new(StringComparer.Ordinal);

    public async Task<string> UploadAsync(string container, string blobName, Stream stream, string contentType,
        CancellationToken cancellationToken)
    {
        var containerClient = blobServiceClient.GetBlobContainerClient(container);
        await EnsureContainerExistsAsync(containerClient, container, cancellationToken);

        var blobClient = containerClient.GetBlobClient(blobName);

        await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = contentType },
            cancellationToken: cancellationToken);

        return blobClient.Uri.ToString();
    }

    private static async Task EnsureContainerExistsAsync(BlobContainerClient containerClient, string containerName,
        CancellationToken cancellationToken)
    {
        if (EnsuredContainers.ContainsKey(containerName))
            return;

        await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        EnsuredContainers.TryAdd(containerName, true);
    }

    public async Task DeleteAsync(string container, string blobName, CancellationToken cancellationToken)
    {
        var containerClient = blobServiceClient.GetBlobContainerClient(container);
        var blobClient = containerClient.GetBlobClient(blobName);
        await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }

    public async Task<BlobDownloadResponse> DownloadAsync(string container, string blobName,
        CancellationToken cancellationToken)
    {
        var containerClient = blobServiceClient.GetBlobContainerClient(container);
        var blobClient = containerClient.GetBlobClient(blobName);

        var response = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken);
        var contentType = response.Value.Details.ContentType ?? "application/octet-stream";

        return new BlobDownloadResponse(response.Value.Content, contentType);
    }
}
