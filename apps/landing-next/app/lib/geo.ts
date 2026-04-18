// Shared geo lookup used by both the SSR layout (initial render) and the
// /api/geo edge route (client-side refresh). Country always comes from
// Cloudflare's CF-IPCountry header (sync, no network). City/region/lat/lon
// come from ipapi.co with a 24h in-memory cache per IP.

export type GeoData = {
  country: string;
  city: string;
  region: string;
  lat: number | null;
  lon: number | null;
};

const cache = new Map<string, { value: GeoData; expiresAt: number }>();
const CACHE_TTL_MS = 24 * 60 * 60 * 1000;

export function clientIp(headers: Headers): string {
  const cf = headers.get("cf-connecting-ip");
  if (cf) return cf.trim();
  const xff = headers.get("x-forwarded-for");
  if (xff) {
    const first = xff.split(",")[0]?.trim();
    if (first) return first;
  }
  const real = headers.get("x-real-ip");
  if (real) return real.trim();
  return "unknown";
}

function isPrivateIp(ip: string): boolean {
  return (
    ip === "unknown" ||
    ip.startsWith("127.") ||
    ip.startsWith("10.") ||
    ip.startsWith("192.168.") ||
    ip.startsWith("::1") ||
    ip === "0.0.0.0"
  );
}

function pruneCache(now: number): void {
  if (cache.size < 500) return;
  for (const [k, v] of cache) {
    if (v.expiresAt < now) cache.delete(k);
  }
}

async function lookupViaIpapi(
  ip: string,
): Promise<{ city: string; region: string; lat: number | null; lon: number | null }> {
  if (isPrivateIp(ip)) return { city: "", region: "", lat: null, lon: null };
  try {
    const res = await fetch(`https://ipapi.co/${encodeURIComponent(ip)}/json/`, {
      headers: { "User-Agent": "elkhair-portfolio-geo/1.0" },
      signal: AbortSignal.timeout(2000),
    });
    if (!res.ok) return { city: "", region: "", lat: null, lon: null };
    const j = (await res.json()) as {
      city?: string;
      region?: string;
      latitude?: number;
      longitude?: number;
    };
    return {
      city: j.city ?? "",
      region: j.region ?? "",
      lat: typeof j.latitude === "number" ? j.latitude : null,
      lon: typeof j.longitude === "number" ? j.longitude : null,
    };
  } catch {
    return { city: "", region: "", lat: null, lon: null };
  }
}

/**
 * Resolves geo for the given headers. Country from CF-IPCountry (always),
 * city/region/lat/lon from ipapi.co (cached per IP for 24h).
 */
export async function resolveGeo(headers: Headers): Promise<GeoData> {
  const ip = clientIp(headers);
  const country = (headers.get("cf-ipcountry") ?? "XX").toUpperCase();
  const now = Date.now();
  pruneCache(now);

  const cached = cache.get(ip);
  if (cached && cached.expiresAt > now) {
    // Country can shift if CF reroutes; trust the freshest header.
    return { ...cached.value, country };
  }

  const cityData = await lookupViaIpapi(ip);
  const value: GeoData = { country, ...cityData };
  cache.set(ip, { value, expiresAt: now + CACHE_TTL_MS });
  return value;
}
