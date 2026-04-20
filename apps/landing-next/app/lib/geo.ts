// Shared geo lookup used by both the SSR layout (initial render) and the
// /api/geo route (client-side refresh). Resolution order:
//   1. Cloudflare's `request.cf` (on CF Pages / Workers) — zero network
//   2. MaxMind GeoLite2 mmdb baked into the container at `/app/geo/GeoLite2-City.mmdb` — local, no external calls
//   3. ipapi.co — last-ditch fallback when neither is available (e.g. during first-boot before the mmdb is present)
// 24h in-memory cache per IP on top of all three.

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

// MaxMind mmdb reader is lazy-loaded and cached at module scope. `maxmind`
// uses Node fs APIs — guard by runtime check so edge bundles can still
// evaluate this file. When the reader can't be initialized (edge runtime,
// mmdb file missing, bad license key at build time) we silently fall
// through to ipapi.
type MmdbReader = { get(ip: string): MmdbCityRecord | null };
type MmdbCityRecord = {
  country?: { iso_code?: string };
  city?: { names?: { en?: string } };
  subdivisions?: Array<{ names?: { en?: string } }>;
  location?: { latitude?: number; longitude?: number };
};
let mmdbReaderPromise: Promise<MmdbReader | null> | null = null;

async function getMmdbReader(): Promise<MmdbReader | null> {
  if (mmdbReaderPromise) return mmdbReaderPromise;
  mmdbReaderPromise = (async (): Promise<MmdbReader | null> => {
    // Edge runtime: no `process.versions.node`, no `node:fs`. Skip cleanly.
    if (typeof process === "undefined" || !process.versions?.node) return null;
    try {
      const mmdbPath = process.env.MMDB_PATH ?? "/app/geo/GeoLite2-City.mmdb";
      // String-variable module names evade webpack's static import analysis
      // so the edge bundle doesn't try to pull `maxmind`/`node:fs`.
      const maxmindName = "maxmind";
      const fsName = "node:fs/promises";
      const [{ Reader }, { readFile }] = await Promise.all([
        import(/* webpackIgnore: true */ maxmindName),
        import(/* webpackIgnore: true */ fsName),
      ]);
      const buffer = await readFile(mmdbPath);
      return new Reader(buffer) as MmdbReader;
    } catch {
      return null;
    }
  })();
  return mmdbReaderPromise;
}

async function lookupViaMmdb(ip: string): Promise<IpapiData> {
  if (isPrivateIp(ip)) return EMPTY_IPAPI;
  const reader = await getMmdbReader();
  if (!reader) return EMPTY_IPAPI;
  try {
    const r = reader.get(ip);
    if (!r) return EMPTY_IPAPI;
    return {
      country: (r.country?.iso_code ?? "").toUpperCase(),
      city: r.city?.names?.en ?? "",
      region: r.subdivisions?.[0]?.names?.en ?? "",
      lat: typeof r.location?.latitude === "number" ? r.location.latitude : null,
      lon: typeof r.location?.longitude === "number" ? r.location.longitude : null,
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

  // Path 2: MaxMind mmdb (Node runtime only). Local file, no network, no rate limit.
  const mmdb = await lookupViaMmdb(ip);
  if (mmdb.city || mmdb.lat !== null) {
    const value: GeoData = {
      country: mmdb.country || cfCountryHeader || "XX",
      city: mmdb.city,
      region: mmdb.region,
      lat: mmdb.lat,
      lon: mmdb.lon,
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
