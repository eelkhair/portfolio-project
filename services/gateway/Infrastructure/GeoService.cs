using System.Net;
using MaxMind.GeoIP2;
using MaxMind.GeoIP2.Exceptions;

namespace Gateway.Api.Infrastructure;

public interface IGeoService
{
    GeoResult Lookup(IPAddress ip);
}

public sealed record GeoResult(
    string Country,
    string City,
    string Region,
    double? Lat,
    double? Lon);

/// <summary>
/// Resolves geo data from a local MaxMind GeoLite2-City.mmdb baked into the
/// container at build time. The mmdb is a few tens of MB and is kept in
/// memory for the lifetime of the process — lookups are &lt;10 microseconds.
/// When the file is missing (dev, misconfigured build) every call returns an
/// empty GeoResult; callers should treat that as "unknown" and not fail.
/// </summary>
public sealed class GeoService : IGeoService, IDisposable
{
    public static readonly GeoResult Empty = new("", "", "", null, null);

    private readonly DatabaseReader? _reader;
    private readonly ILogger<GeoService> _logger;

    public GeoService(IConfiguration configuration, ILogger<GeoService> logger)
    {
        _logger = logger;
        var path = configuration["Geo:MmdbPath"] ?? "/app/geo/GeoLite2-City.mmdb";
        if (!File.Exists(path))
        {
            _logger.LogWarning("MaxMind mmdb not found at {Path}; geo lookups will return empty", path);
            _reader = null;
            return;
        }

        try
        {
            _reader = new DatabaseReader(path, MaxMind.Db.FileAccessMode.Memory);
            _logger.LogInformation("MaxMind mmdb loaded from {Path}", path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load MaxMind mmdb at {Path}; geo lookups will return empty", path);
            _reader = null;
        }
    }

    public GeoResult Lookup(IPAddress ip)
    {
        if (_reader is null) return Empty;
        // Private / loopback addresses aren't in the MaxMind database.
        if (IPAddress.IsLoopback(ip)) return Empty;
        var bytes = ip.GetAddressBytes();
        if (bytes.Length == 4 && (bytes[0] == 10
            || (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
            || (bytes[0] == 192 && bytes[1] == 168))) return Empty;

        try
        {
            var r = _reader.City(ip);
            return new GeoResult(
                r.Country?.IsoCode ?? "",
                r.City?.Name ?? "",
                r.MostSpecificSubdivision?.Name ?? "",
                r.Location?.Latitude,
                r.Location?.Longitude);
        }
        catch (AddressNotFoundException)
        {
            return Empty;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "mmdb lookup failed for {Ip}", ip);
            return Empty;
        }
    }

    public void Dispose() => _reader?.Dispose();
}
