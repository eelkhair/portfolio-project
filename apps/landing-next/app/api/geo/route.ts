import { NextRequest, NextResponse } from "next/server";

// Edge Runtime keeps cold start tiny and gets us close to the user.
export const runtime = "edge";

/**
 * Returns coarse geo for the requesting IP. Used by FaroProvider to tag
 * RUM spans with country + city so the Grafana dashboard can group/map.
 *
 * Source of truth:
 *   - country: Cloudflare's `cf-ipcountry` header (always present through tunnel).
 *   - city + region + lat/lon: ipapi.co server-side lookup (free 1k req/day).
 *
 * Per-IP responses are cached in module memory for 24h to keep us well under
 * the free-tier limit and to keep latency near zero for repeat callers.
 * Memory is per-container — restarts/cold scales drop the cache, that's fine.
 */
type GeoResponse = {
  country: string;
  city: string;
  region: string;
  lat: number | null;
  lon: number | null;
};

const cache = new Map<string, { value: GeoResponse; expiresAt: number }>();
const CACHE_TTL_MS = 24 * 60 * 60 * 1000;

function clientIp(req: NextRequest): string {
  const cf = req.headers.get("cf-connecting-ip");
  if (cf) return cf.trim();
  const xff = req.headers.get("x-forwarded-for");
  if (xff) {
    const first = xff.split(",")[0]?.trim();
    if (first) return first;
  }
  const real = req.headers.get("x-real-ip");
  if (real) return real.trim();
  return "unknown";
}

function pruneCache(now: number): void {
  if (cache.size < 500) return;
  for (const [k, v] of cache) {
    if (v.expiresAt < now) cache.delete(k);
  }
}

async function lookupCity(ip: string): Promise<{ city: string; region: string; lat: number | null; lon: number | null }> {
  // Don't bother for non-routable / unknown IPs (local dev, private networks).
  if (
    ip === "unknown" ||
    ip.startsWith("127.") ||
    ip.startsWith("10.") ||
    ip.startsWith("192.168.") ||
    ip.startsWith("::1") ||
    ip === "0.0.0.0"
  ) {
    return { city: "", region: "", lat: null, lon: null };
  }
  try {
    // ipapi.co free tier: 1k req/day, no key required.
    const res = await fetch(`https://ipapi.co/${encodeURIComponent(ip)}/json/`, {
      headers: { "User-Agent": "elkhair-portfolio-geo/1.0" },
      // Edge runtime supports an explicit timeout via AbortController; keep it short.
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

export async function GET(req: NextRequest) {
  const ip = clientIp(req);
  const country = (req.headers.get("cf-ipcountry") ?? "XX").toUpperCase();
  const now = Date.now();
  pruneCache(now);

  const cached = cache.get(ip);
  if (cached && cached.expiresAt > now) {
    return NextResponse.json(cached.value, {
      headers: { "Cache-Control": "private, max-age=86400" },
    });
  }

  const cityData = await lookupCity(ip);
  const value: GeoResponse = { country, ...cityData };
  cache.set(ip, { value, expiresAt: now + CACHE_TTL_MS });

  return NextResponse.json(value, {
    headers: { "Cache-Control": "private, max-age=86400" },
  });
}
