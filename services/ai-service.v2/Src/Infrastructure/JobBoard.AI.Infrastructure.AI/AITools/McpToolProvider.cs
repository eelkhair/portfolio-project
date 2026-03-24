using JobBoard.AI.Application.Interfaces.Configurations;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;

namespace JobBoard.AI.Infrastructure.AI.AITools;

/// <summary>
/// Fetches tools from a remote MCP server at runtime.
/// Lazily connects on first GetTools() call — the API starts even if MCP servers are down.
/// McpClientTool extends AITool, so they drop directly into ChatOptions.Tools.
/// </summary>
public sealed class McpToolProvider(
    string serverUrl,
    string serverName,
    ILogger<McpToolProvider> logger) : IAiTools, IAsyncDisposable
{
    private McpClient? _client;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public IEnumerable<AITool> GetTools()
    {
        try
        {
            var client = Task.Run(() => GetOrCreateClientAsync()).GetAwaiter().GetResult();
            if (client is null) return [];

            var tools = Task.Run(async () => await client.ListToolsAsync()).GetAwaiter().GetResult();
            logger.LogInformation("Loaded {Count} tools from MCP server {Server}",
                tools.Count, serverName);
            return tools;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch tools from MCP server {Server} at {Url}", serverName, serverUrl);
            _client = null; // Reset so next call retries
            return [];
        }
    }

    private async Task<McpClient?> GetOrCreateClientAsync()
    {
        if (_client is not null) return _client;

        await _lock.WaitAsync();
        try
        {
            if (_client is not null) return _client;

            logger.LogInformation("Connecting to MCP server {Server} at {Url}", serverName, serverUrl);
            var handler = new Infrastructure.UserTokenForwardingHandler
            {
                InnerHandler = new HttpClientHandler()
            };
            var httpClient = new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan };
            _client = await McpClient.CreateAsync(
                new HttpClientTransport(
                    new HttpClientTransportOptions
                    {
                        Endpoint = new Uri(serverUrl),
                        Name = serverName
                    },
                    httpClient,
                    ownsHttpClient: true));
            return _client;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to connect to MCP server {Server} at {Url}", serverName, serverUrl);
            return null;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_client is not null)
            await _client.DisposeAsync();
    }
}
