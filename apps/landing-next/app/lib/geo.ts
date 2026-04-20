// Shared geo lookup used by both the SSR layout (initial render) and the
// /api/geo edge route (client-side refresh). Resolution order:
//   1. `request.cf` (CF Pages / Workers) — full geo at zero latency
//   2. Gateway `/api/public/geo` — MaxMind mmdb lookup, served by the
//      gateway service on Proxmox. Requires GATEWAY_GEO_URL env var.
//   3. ipapi.co — last-ditch fallback for country+city+region+lat/lon
//   4. `cf-ipcountry` header — country-only final fallback
// 24h in-memory cache per IP on top of all paths.

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

/**
 * Calls the gateway's `/api/public/geo` endpoint, which looks up the caller's
 * IP in a local MaxMind GeoLite2 database. Landing SSR runs server-to-server,
 * so we forward the real client IP via X-Forwarded-For; the gateway's
 * GeoEndpoint already prefers CF-Connecting-IP → X-Forwarded-For → X-Real-IP.
 *
 * Returns empty when `GATEWAY_GEO_URL` is unset (CF Pages / local dev without
 * the gateway running) — caller falls through to ipapi.
 */
async function lookupViaGateway(ip: string): Promise<IpapiData> {
  if (isPrivateIp(ip)) return EMPTY_IPAPI;
  const url = typeof process !== "undefined" ? process.env?.GATEWAY_GEO_URL : undefined;
  if (!url) return EMPTY_IPAPI;
  try {
    const res = await fetch(url, {
      headers: {
        "x-forwarded-for": ip,
        "User-Agent": "elkhair-portfolio-landing/1.0",
      },
      signal: AbortSignal.timeout(1500),
    });
    if (!res.ok) return EMPTY_IPAPI;
    const j = (await res.json()) as {
      country?: string;
      city?: string;
      region?: string;
      lat?: number | null;
      lon?: number | null;
    };
    return {
      country: (j.country ?? "").toUpperCase(),
      city: j.city ?? "",
      region: j.region ?? "",
      lat: typeof j.lat === "number" ? j.lat : null,
      lon: typeof j.lon === "number" ? j.lon : null,
    };
  } catch {
    return EMPTY_IPAPI;
  }
}

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
 *   2. Gateway `/api/public/geo` (MaxMind mmdb) — when GATEWAY_GEO_URL is set
 *   3. ipapi.co — IP-based lookup, last resort (rate-limited free tier)
 *   4. `cf-ipcountry` header — country-only final fallback
 *
 * Passing `cf` is optional; omit it when the runtime doesn't expose it
 * (Proxmox) and the lookup transparently degrades through the remaining paths.
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

  // Path 2: Gateway (mmdb-backed). Node runtime only — edge runtimes won't
  // have GATEWAY_GEO_URL inlined and this returns empty.
  const gw = await lookupViaGateway(ip);
  if (gw.city || gw.lat !== null) {
    const value: GeoData = {
      country: gw.country || cfCountryHeader || "XX",
      city: gw.city,
      region: gw.region,
      lat: gw.lat,
      lon: gw.lon,
    };
    cache.set(ip, { value, expiresAt: now + CACHE_TTL_MS });
    return value;
  }

  // Path 3 + 4: ipapi.co with cf-ipcountry as last-ditch fallback.
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
