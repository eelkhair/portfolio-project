using System.Net;

namespace Gateway.Api.Infrastructure;

/// <summary>
/// GET /api/public/geo — resolves the caller's IP to country/city/region/lat/lon
/// using a locally-loaded MaxMind GeoLite2 database. Called by:
///  * Angular admin + public apps (Faro RUM init, cross-origin)
///  * Landing page (SSR initial render, same-origin)
/// CORS is handled by the gateway-level policy; no auth required because
/// only the caller's own IP is ever resolved.
/// </summary>
public static class GeoEndpoint
{
    public static IResult Handle(HttpContext ctx, IGeoService geo)
    {
        var ip = ResolveCallerIp(ctx);
        var result = ip is null ? GeoService.Empty : geo.Lookup(ip);

        // Use short private caching to amortize repeat calls from a single
        // client page load without holding stale data across long sessions.
        ctx.Response.Headers.CacheControl = "private, max-age=300";
        return Results.Json(new
        {
            country = result.Country,
            city = result.City,
            region = result.Region,
            lat = result.Lat,
            lon = result.Lon,
        });
    }

    private static IPAddress? ResolveCallerIp(HttpContext ctx)
    {
        // CF-Connecting-IP is set by Cloudflare Tunnel for public traffic.
        var cf = ctx.Request.Headers["CF-Connecting-IP"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(cf) && IPAddress.TryParse(cf.Trim(), out var cfIp)) return cfIp;

        var xff = ctx.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(xff))
        {
            var first = xff.Split(',', 2)[0].Trim();
            if (IPAddress.TryParse(first, out var xffIp)) return xffIp;
        }

        var real = ctx.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(real) && IPAddress.TryParse(real.Trim(), out var realIp)) return realIp;

        return ctx.Connection.RemoteIpAddress;
    }
}
