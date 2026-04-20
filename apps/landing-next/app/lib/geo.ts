// Shared geo lookup used by both the SSR layout (initial render) and the
// /api/geo edge route (client-side refresh). Country always comes from
// Cloudflare's CF-IPCountry header (sync, no network). City/region/lat/lon
// come from ipapi.co with a 24h in-memory cache per IP.
//
// NOTE: The MaxMind mmdb path lives on the gateway service (/api/public/geo)
// because Next.js's edge transformer strips runtime import tricks, making it
// impossible to load mmdb from within the landing app. Angular frontends
// call the gateway directly; this landing route is kept as a compat shim.

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

type IpapiData = {
  country: string;
  city: string;
  region: string;
  lat: number | null;
  lon: number | null;
};

const EMPTY_IPAPI: IpapiData = { country: "", city: "", region: "", lat: null, lon: null };

async function lookupViaIpapi(ip: string): Promise<IpapiData> {
  if (isPrivateIp(ip)) return EMPTY_IPAPI;
  try {
    const res = await fetch(`https://ipapi.co/${encodeURIComponent(ip)}/json/`, {
      headers: { "User-Agent": "elkhair-portfolio-geo/1.0" },
      signal: AbortSignal.timeout(2000),
    });
    if (!res.ok) return EMPTY_IPAPI;
    const j = (await res.json()) as {
      country?: string;
      country_code?: string;
      city?: string;
      region?: string;
      latitude?: number;
      longitude?: number;
    };
    // ipapi.co returns ISO-2 code as both `country` and `country_code`.
    const code = (j.country_code ?? j.country ?? "").toUpperCase();
    return {
      country: code,
      city: j.city ?? "",
      region: j.region ?? "",
      lat: typeof j.latitude === "number" ? j.latitude : null,
      lon: typeof j.longitude === "number" ? j.longitude : null,
    };
  } catch {
    return EMPTY_IPAPI;
  }
}

/**
 * Subset of the `request.cf` object Cloudflare attaches to every incoming
 * request on Workers / Pages Functions runtime. Zero-latency geo (country,
 * city, region, lat/lon) sourced directly from CF's edge. Undefined when
 * running on Node/Proxmox.
 */
export type CfProperties = {
  country?: string | null;
  city?: string | null;
  region?: string | null;
  regionCode?: string | null;
  latitude?: string | number | null;
  longitude?: string | number | null;
};

function parseCoord(v: string | number | null | undefined): number | null {
  if (v === null || v === undefined) return null;
  const n = typeof v === "number" ? v : Number.parseFloat(v);
  return Number.isFinite(n) ? n : null;
}

/**
 * Resolves geo for the given headers. Resolution order:
 *   1. `request.cf` (CF Pages / Workers) — full geo at zero latency
 *   2. ipapi.co — IP-based lookup for Node/Proxmox runtime
 *   3. `cf-ipcountry` header — country-only fallback if ipapi fails
 *
 * Passing `cf` is optional; omit it when the runtime doesn't expose it
 * (Proxmox) and the lookup transparently degrades to the ipapi path.
 */
export async function resolveGeo(headers: Headers, cf?: CfProperties): Promise<GeoData> {
  const ip = clientIp(headers);
  const cfCountryHeader = (headers.get("cf-ipcountry") ?? "").toUpperCase();
  const now = Date.now();
  pruneCache(now);

  // Path 1: Cloudflare Pages / Workers — skip ipapi entirely when CF gave us
  // enough. Country + (city OR lat/lon) is the signal we use.
  if (cf) {
    const country = (cf.country ?? "").toUpperCase();
    const city = cf.city ?? "";
    const region = cf.region ?? cf.regionCode ?? "";
    const lat = parseCoord(cf.latitude);
    const lon = parseCoord(cf.longitude);
    if (country && (city || (lat !== null && lon !== null))) {
      const value: GeoData = { country, city, region, lat, lon };
      cache.set(ip, { value, expiresAt: now + CACHE_TTL_MS });
      return value;
    }
  }

  const cached = cache.get(ip);
  if (cached && cached.expiresAt > now) {
    return cached.value;
  }

  // Path 2 + 3: ipapi.co with cf-ipcountry as last-ditch fallback.
  const ipapi = await lookupViaIpapi(ip);
  const value: GeoData = {
    country: ipapi.country || cfCountryHeader || "XX",
    city: ipapi.city,
    region: ipapi.region,
    lat: ipapi.lat,
    lon: ipapi.lon,
  };
  cache.set(ip, { value, expiresAt: now + CACHE_TTL_MS });
  return value;
}
