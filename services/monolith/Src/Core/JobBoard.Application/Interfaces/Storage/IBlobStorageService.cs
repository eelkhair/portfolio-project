namespace JobBoard.Application.Interfaces.Storage;

public record BlobDownloadResponse(Stream Content, string ContentType);

public interface IBlobStorageService
{
    Task<string> UploadAsync(string container, string blobName, Stream stream, string contentType,
        CancellationToken cancellationToken);

    Task DeleteAsync(string container, string blobName, CancellationToken cancellationToken);

    Task<BlobDownloadResponse> DownloadAsync(string container, string blobName, CancellationToken cancellationToken);
}
