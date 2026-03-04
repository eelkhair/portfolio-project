using Azure.Storage.Blobs;
using JobBoard.AI.Application.Interfaces.Clients;

namespace JobBoard.AI.Infrastructure.Configuration.Services;

public class BlobStorageService(BlobServiceClient blobServiceClient) : IBlobStorageService
{
    public async Task<byte[]> DownloadAsync(string container, string blobName, CancellationToken ct)
    {
        var containerClient = blobServiceClient.GetBlobContainerClient(container);
        var blobClient = containerClient.GetBlobClient(blobName);
        var response = await blobClient.DownloadStreamingAsync(cancellationToken: ct);

        using var ms = new MemoryStream();
        await response.Value.Content.CopyToAsync(ms, ct);
        return ms.ToArray();
    }
}
