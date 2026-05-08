using System.Buffers.Text;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using JobBoard.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JobBoard.Infrastructure.Configuration.Services;

public class DemoClaimTokenService(IConfiguration configuration, ILogger<DemoClaimTokenService> logger)
    : IDemoClaimTokenService
{
    private const string SecretKey = "DemoClaim:Secret";

    public string Issue(Guid companyUId, DateTime expiresAt)
    {
        var payload = $"{companyUId:N}.{expiresAt.ToUniversalTime().Ticks.ToString(CultureInfo.InvariantCulture)}";
        var signature = Sign(payload);
        return $"{payload}.{signature}";
    }

    public bool Verify(string token, Guid companyUId)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        var parts = token.Split('.');
        if (parts.Length != 3)
            return false;

        var (companyPart, ticksPart, providedSig) = (parts[0], parts[1], parts[2]);

        if (!Guid.TryParseExact(companyPart, "N", out var parsedCompany) || parsedCompany != companyUId)
            return false;

        if (!long.TryParse(ticksPart, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ticks))
            return false;

        var expiresAt = new DateTime(ticks, DateTimeKind.Utc);
        if (expiresAt < DateTime.UtcNow)
        {
            logger.LogInformation("Demo claim token expired for company {CompanyUId}", companyUId);
            return false;
        }

        var expectedSig = Sign($"{companyPart}.{ticksPart}");
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expectedSig),
            Encoding.UTF8.GetBytes(providedSig));
    }

    private string Sign(string payload)
    {
        var secret = configuration[SecretKey]
            ?? throw new InvalidOperationException($"Configuration value '{SecretKey}' is required for demo claim tokens.");
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Base64Url.EncodeToString(hash);
    }
}
