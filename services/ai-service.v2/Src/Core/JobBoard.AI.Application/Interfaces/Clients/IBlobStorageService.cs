namespace JobBoard.AI.Application.Interfaces.Clients;

/// <summary>
/// Thin abstraction over blob storage so Application-layer handlers
/// don't depend on Azure.Storage.Blobs directly.
/// </summary>
public interface IBlobStorageService
{
    /// <summary>
    /// Downloads a blob and returns its content as a byte array.
    /// </summary>
    Task<byte[]> DownloadAsync(string container, string blobName, CancellationToken ct);
}
