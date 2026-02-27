namespace JobBoard.AI.Application.Interfaces.AI;

public interface IEmbeddingService
{
    Task<float[]> GenerateEmbeddingsAsync(string embeddingText, CancellationToken cancellationToken);
}