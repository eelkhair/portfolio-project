using System.Diagnostics;
using JobBoard.AI.Application.Interfaces.AI;
using JobBoard.AI.Application.Interfaces.Observability;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JobBoard.AI.Infrastructure.AI.Services;

public class EmbeddingService(IEmbeddingProviderResolver resolver, IConfiguration configuration, IActivityFactory activityFactory) : IEmbeddingService
{
    private IEmbeddingGenerator<string, Embedding<float>> GetClient()
    {
        var provider = configuration["AI:EmbeddingProvider"] ?? "openai.embedding";
        Activity.Current?.SetTag("embedding.provider", provider);

        return resolver.Resolve(provider);
    }

    public async Task<float[]> GenerateEmbeddingsAsync(
        string text,
        CancellationToken cancellationToken)
    {
        using var activity = activityFactory.StartActivity("text.embedding", ActivityKind.Internal);
        activity?.SetTag("embedding.text.length", text.Length);

        var client = GetClient();

        var embedding = await client.GenerateAsync(
            text,
            new EmbeddingGenerationOptions
            {
                Dimensions = 1536
            },
            cancellationToken);
        
        activity?.SetTag("embedding.vector.length", embedding.Vector.Length);
        return embedding.Vector.ToArray();
    }


}


public interface IEmbeddingProviderResolver
{
    IEmbeddingGenerator<string, Embedding<float>> Resolve(string provider);
}

public class EmbeddingProviderResolver(IServiceProvider sp)
    : IEmbeddingProviderResolver
{
    public IEmbeddingGenerator<string, Embedding<float>> Resolve(string provider)
        => sp.GetRequiredKeyedService<IEmbeddingGenerator<string, Embedding<float>>>(provider);
}