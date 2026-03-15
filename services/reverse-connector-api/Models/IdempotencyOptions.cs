namespace ReverseConnectorAPI.Models;

internal static class IdempotencyOptions
{
    public const int PendingTTLSeconds = 120;
    public const int CompletedTTLSeconds = 7 * 24 * 3600; // 7 days
}
