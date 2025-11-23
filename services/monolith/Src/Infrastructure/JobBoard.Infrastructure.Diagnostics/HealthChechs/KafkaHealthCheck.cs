using Confluent.Kafka;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace JobBoard.Infrastructure.Diagnostics.HealthChechs;

public class KafkaHealthCheck(ProducerConfig producerConfig, ILogger<KafkaHealthCheck> logger)
    : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await Task.Run(() =>
            {
                using var adminClient = new AdminClientBuilder(producerConfig).Build();
                
                adminClient.GetMetadata(TimeSpan.FromSeconds(5));

            }, cancellationToken);

            return HealthCheckResult.Healthy("Successfully connected to Kafka broker and retrieved metadata.");
        }
        catch (OperationCanceledException ex)
        {
            logger.LogWarning(ex, "Kafka health check timed out");
            return HealthCheckResult.Unhealthy("The health check timed out, indicating the Kafka broker is not reachable.");
        }
        catch (KafkaException ex)
        {
            logger.LogWarning(ex, "Kafka health check failed with a KafkaException");
            return HealthCheckResult.Unhealthy($"A Kafka-specific error occurred while connecting to the broker: {ex.Message}");
        }
        catch (Exception ex)
        {
            // A general catch-all for any other unexpected errors during the check.
            logger.LogError(ex, "An unexpected error occurred during the Kafka health check");
            return HealthCheckResult.Unhealthy($"An unexpected error occurred: {ex.Message}");
        }
    }
}