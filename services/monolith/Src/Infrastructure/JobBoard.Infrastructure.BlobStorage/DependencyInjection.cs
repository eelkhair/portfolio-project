using Azure.Storage.Blobs;
using JobBoard.Application.Interfaces.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JobBoard.Infrastructure.BlobStorage;

public static class DependencyInjection
{
    public static IServiceCollection AddBlobStorageServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("BlobStorage") ?? "UseDevelopmentStorage=true";

        services.AddSingleton(new BlobServiceClient(connectionString));
        services.AddScoped<IBlobStorageService, AzureBlobStorageService>();

        return services;
    }
}
