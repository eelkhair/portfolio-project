import { NextResponse } from "next/server";
import { services } from "../../data/portfolio-data";

// Edge runtime so this works identically on Proxmox Node and Cloudflare Pages.
export const runtime = "edge";

type Status = "up" | "degraded";

interface ServiceStatus {
  name: string;
  url: string;
  status: Status;
  latencyMs: number;
  httpStatus: number | null;
  reason?: string;
}

// Anything slower than this counts as `degraded` even if 2xx.
const SLOW_THRESHOLD_MS = 2000;
// Hard timeout to bound the route's total wall time.
const PROBE_TIMEOUT_MS = 3000;

async function probe(name: string, url: string, displayUrl: string): Promise<ServiceStatus> {
  const started = Date.now();
  try {
    const res = await fetch(url, {
      method: "GET",
      redirect: "manual",
      signal: AbortSignal.timeout(PROBE_TIMEOUT_MS),
      // Avoid sending cookies / credentials cross-origin.
      credentials: "omit",
      // Some hosts 404 on HEAD; GET with no body parse is fine and cheap enough.
      headers: { "user-agent": "landing-status-probe/1.0" },
    });
    const latencyMs = Date.now() - started;
    const ok = res.status >= 200 && res.status < 400;
    const fast = latencyMs <= SLOW_THRESHOLD_MS;
    return {
      name,
      url: displayUrl,
      status: ok && fast ? "up" : "degraded",
      latencyMs,
      httpStatus: res.status,
      reason: !ok ? `http ${res.status}` : !fast ? `slow (${latencyMs}ms)` : undefined,
    };
  } catch (err) {
    const latencyMs = Date.now() - started;
    const reason = err instanceof Error
      ? (err.name === "TimeoutError" || err.name === "AbortError" ? "timeout" : err.message)
      : "network error";
    return { name, url: displayUrl, status: "degraded", latencyMs, httpStatus: null, reason };
  }
}

export async function GET(): Promise<NextResponse> {
  const results = await Promise.all(
    services.map((s) => probe(s.name, s.healthUrl, s.url)),
  );
  return NextResponse.json(results, {
    headers: {
      // Edge-cache for 30s; serve stale up to 60s while revalidating so a cold
      // visitor still gets an instant response and a fresh probe runs in the
      // background.
      "cache-control": "public, max-age=30, s-maxage=30, stale-while-revalidate=60",
    },
  });
}
