using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VaultSharp;
using VaultSharp.V1.AuthMethods.Token;

namespace JobBoard.Infrastructure.Vault;

public class VaultConfigurationProvider(
    string vaultAddress,
    string vaultToken,
    string enginePath,
    string[] secretPaths,
    ILogger logger) : ConfigurationProvider
{
    public override void Load()
    {
        try
        {
            LoadAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to load secrets from Vault at {Address}. Falling back to user-secrets", vaultAddress);
        }
    }

    private async Task LoadAsync()
    {
        var authMethod = new TokenAuthMethodInfo(vaultToken);
        var settings = new VaultClientSettings(vaultAddress, authMethod);
        var client = new VaultClient(settings);

        foreach (var path in secretPaths)
        {
            try
            {
                var secret = await client.V1.Secrets.KeyValue.V2.ReadSecretAsync(
                    path: path,
                    mountPoint: enginePath);

                if (secret?.Data?.Data is null) continue;

                foreach (var kvp in secret.Data.Data)
                {
                    var key = kvp.Key;
                    var value = kvp.Value?.ToString();

                    if (value is not null)
                    {
                        Data[key] = value;
                    }
                }

                logger.LogInformation("Loaded {Count} secrets from Vault path '{Path}'",
                    secret.Data.Data.Count, path);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to read Vault secret path '{Path}'", path);
            }
        }
    }
}
