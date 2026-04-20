import { NextRequest, NextResponse } from "next/server";
import { resolveGeo, type CfProperties } from "../../lib/geo";

// Edge Runtime keeps cold start tiny. Returns the same shape used SSR-side.
export const runtime = "edge";

// Allowed origins that may GET this endpoint cross-origin. Landing calls it
// same-origin; the Angular admin and public apps call it from another domain
// to populate Faro RUM with country/city/region/lat/lon at app init.
const ALLOWED_ORIGINS = new Set([
  "https://elkhair.tech",
  "https://eelkhair.net",
  "https://job-admin.elkhair.tech",
  "https://job-admin-dev.elkhair.tech",
  "https://job-admin.eelkhair.net",
  "https://job-admin-dev.eelkhair.net",
  "https://jobs.elkhair.tech",
  "https://jobs-dev.elkhair.tech",
  "https://jobs.eelkhair.net",
  "https://jobs-dev.eelkhair.net",
  "http://localhost:4200",
  "http://localhost:3100",
  "http://localhost:3000",
]);

function corsHeaders(origin: string | null): Record<string, string> {
  const allow = origin && ALLOWED_ORIGINS.has(origin) ? origin : "";
  if (!allow) return {};
  return {
    "Access-Control-Allow-Origin": allow,
    "Access-Control-Allow-Methods": "GET, OPTIONS",
    // Echo Angular tracingInterceptor headers + standard tracing headers so
    // browser preflights pass regardless of what the SDK adds.
    "Access-Control-Allow-Headers":
      "Content-Type, x-mode, traceparent, tracestate, baggage, x-b3-traceid, x-b3-spanid, x-b3-sampled, x-b3-flags, x-b3-parentspanid, b3",
    "Access-Control-Max-Age": "3600",
    "Vary": "Origin",
  };
}

export async function OPTIONS(req: NextRequest) {
  return new NextResponse(null, {
    status: 204,
    headers: corsHeaders(req.headers.get("origin")),
  });
}

export async function GET(req: NextRequest) {
  // `request.cf` is populated by Cloudflare Pages / Workers at the edge with
  // country/city/region/lat/lon. Not present on Node/Proxmox — `resolveGeo`
  // falls back to ipapi.co there.
  const cf = (req as unknown as { cf?: CfProperties }).cf;
  const geo = await resolveGeo(req.headers, cf);
  return NextResponse.json(geo, {
    headers: {
      "Cache-Control": "private, max-age=86400",
      ...corsHeaders(req.headers.get("origin")),
    },
  });
}
