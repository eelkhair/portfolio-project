using System.ClientModel;
using System.Diagnostics;
using Anthropic.SDK;
using Azure.AI.OpenAI;
using GeminiDotnet;
using GeminiDotnet.Extensions.AI;
using JobBoard.AI.Application.Interfaces.AI;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;
using JobBoard.AI.Infrastructure.AI.AITools;
using JobBoard.AI.Infrastructure.AI.Services;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenAI.Chat;

namespace JobBoard.AI.Infrastructure.AI;

public static class DependencyInjection
{
    public static IServiceCollection AddAiServices(this IServiceCollection services, IConfiguration configuration)
    {

        services.AddKeyedSingleton("openai", 
            new ChatClient(
                 configuration["AI:OPENAI_MODEL"]!, 
                configuration["AI:OPENAI_API_KEY"]
                                                   ?? throw new InvalidOperationException("Missing api key for OpenAI")).AsIChatClient());

        services.AddKeyedSingleton("azure", 
            new AzureOpenAIClient(
                    new Uri(configuration["AI:AZURE_API_ENDPOINT"]!),
                    new ApiKeyCredential(configuration["AI:AZURE_API_KEY"]!)
                )
                .GetChatClient(configuration["AI:AZURE_OPENAI_MODEL"]!)
                .AsIChatClient());

        services.AddKeyedSingleton<IChatClient>("gemini", 
            new GeminiChatClient(new GeminiClientOptions
            {
                ApiKey = configuration["AI:GEMINI_API_KEY"]!,
                ModelId = configuration["AI:GEMINI_MODEL"]!
            }));
        
        services.AddKeyedSingleton<IChatClient>("claude",
            new AnthropicClient(
                new APIAuthentication(configuration["AI:CLAUDE_API_KEY"]!)
            ).Messages);

        
        services.AddKeyedScoped<IAiTools, AiToolRegistry>("ai");
   
        services.AddTransient<ChatOptions>(sp =>
        {
            var isMonolith = configuration.GetValue<bool>("FeatureFlags:Monolith");
            
            var toolService = sp.GetRequiredKeyedService<IAiTools>(isMonolith ? "monolith" : "micro");
            var aiToolService = sp.GetRequiredKeyedService<IAiTools>("ai");
            
            var topologyTools = toolService.GetTools().ToList();
            var aiTools = aiToolService.GetTools().ToList();

            var duplicates = topologyTools
                .Concat(aiTools)
                .GroupBy(t => t.Name)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicates.Any())
                throw new InvalidOperationException(
                    $"Duplicate AI tools detected. Tool names must be unique across " +
                    $"topology and AI tool sets. Duplicates: {string.Join(", ", duplicates)}");


            var tools = topologyTools
                .Concat(aiTools)
                .ToList();
            
            var factory = sp.GetRequiredService<IActivityFactory>();
            using var activity = factory.StartActivity("tool.Selection", ActivityKind.Internal);
            activity?.AddTag("ai.model", configuration["AIModel"]);
            activity?.AddTag("ai.provider", configuration["AIProvider"]);

            activity?.AddTag("ai.tools", isMonolith ? "Monolith,AI" : "Micro,AI");
            activity?.AddTag("ai.tools.count", tools.Count);
            activity?.AddTag("ai.tools.topology.count", topologyTools.Count);
            activity?.AddTag("ai.tools.ai.count", aiTools.Count);
            
            return new ChatOptions
            {
                Tools =tools,
                MaxOutputTokens = 5000,
                Temperature = 1,
                ModelId = configuration["AIModel"]!
            };
        });
        services.AddScoped<IConversationStore, ConversationStore>();
        services.AddTransient<ICompletionService, CompletionService>();
        return services;
    }
}