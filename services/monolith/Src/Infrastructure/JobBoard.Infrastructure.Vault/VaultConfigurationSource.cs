using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JobBoard.Infrastructure.Vault;

public class VaultConfigurationSource(
    string vaultAddress,
    string vaultToken,
    string enginePath,
    string[] secretPaths,
    ILogger logger) : IConfigurationSource
{
    public IConfigurationProvider Build(IConfigurationBuilder builder)
        => new VaultConfigurationProvider(vaultAddress, vaultToken, enginePath, secretPaths, logger);
}
