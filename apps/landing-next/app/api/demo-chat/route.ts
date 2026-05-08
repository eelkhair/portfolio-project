import { NextRequest, NextResponse } from "next/server";

export const runtime = "edge";

const rateLimit = new Map<string, number>();
const RATE_LIMIT_MS = 5_000;

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

function pruneRateLimit(now: number): void {
  if (rateLimit.size < 100) return;
  const cutoff = now - RATE_LIMIT_MS * 2;
  for (const [k, t] of rateLimit) {
    if (t < cutoff) rateLimit.delete(k);
  }
}

const ALLOWED_ORIGINS = new Set([
  "https://elkhair.tech",
  "https://eelkhair.net",
  "https://dev.elkhair.tech",
  "https://dev.eelkhair.net",
  "http://localhost:3000",
  "http://localhost:3001",
]);

function corsHeaders(origin: string | null): Record<string, string> {
  const allow = origin && ALLOWED_ORIGINS.has(origin) ? origin : "";
  if (!allow) return {};
  return {
    "Access-Control-Allow-Origin": allow,
    "Access-Control-Allow-Methods": "POST, OPTIONS",
    "Access-Control-Allow-Headers":
      "Content-Type, traceparent, tracestate, baggage, x-b3-traceid, x-b3-spanid, x-b3-sampled, x-b3-flags, x-b3-parentspanid, b3",
    "Access-Control-Max-Age": "3600",
    Vary: "Origin",
  };
}

export async function OPTIONS(req: NextRequest) {
  return new NextResponse(null, {
    status: 204,
    headers: corsHeaders(req.headers.get("origin")),
  });
}

type DemoAction = "chat" | "create" | "claim";

interface DemoRequest {
  action: DemoAction;
  message?: string;
  conversationId?: string;
  turnstileToken?: string;
  companyName?: string;
  industryHint?: string;
  companyUId?: string;
  claimToken?: string;
  keycloakUserId?: string;
  email?: string;
  firstName?: string;
  lastName?: string;
}

export async function POST(req: NextRequest) {
  const cors = corsHeaders(req.headers.get("origin"));
  const ip = clientIp(req);
  const now = Date.now();
  pruneRateLimit(now);
  const lastSent = rateLimit.get(ip);
  if (lastSent && now - lastSent < RATE_LIMIT_MS) {
    return NextResponse.json(
      { error: "Please slow down between messages." },
      { status: 429, headers: cors },
    );
  }
  rateLimit.set(ip, now);

  let body: DemoRequest;
  try {
    body = await req.json();
  } catch {
    return NextResponse.json({ error: "Invalid request body." }, { status: 400, headers: cors });
  }

  const aiServiceUrl = process.env.AI_SERVICE_URL;
  const monolithUrl = process.env.MONOLITH_URL;
  if (!aiServiceUrl || !monolithUrl) {
    console.error("[DemoChat] Missing AI_SERVICE_URL or MONOLITH_URL env");
    return NextResponse.json(
      { error: "Demo backend is not configured." },
      { status: 500, headers: cors },
    );
  }

  // Forward the trace context so the trip lands as one trace in Jaeger.
  const traceHeaders: Record<string, string> = {};
  for (const h of ["traceparent", "tracestate", "baggage"]) {
    const v = req.headers.get(h);
    if (v) traceHeaders[h] = v;
  }

  try {
    if (body.action === "chat") {
      if (body.turnstileToken) {
        const t = await fetch("https://challenges.cloudflare.com/turnstile/v0/siteverify", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            secret: process.env.TURNSTILE_SECRET_KEY,
            response: body.turnstileToken,
            remoteip: ip,
          }),
        });
        const td = await t.json();
        if (!td.success) {
          return NextResponse.json(
            { error: "Verification failed." },
            { status: 403, headers: cors },
          );
        }
      }

      const aiRes = await fetch(`${aiServiceUrl.replace(/\/$/, "")}/chat/demo`, {
        method: "POST",
        headers: { "Content-Type": "application/json", ...traceHeaders },
        body: JSON.stringify({
          message: body.message,
          conversationId: body.conversationId,
        }),
      });
      const data = await aiRes.json();
      return NextResponse.json(data, { status: aiRes.status, headers: cors });
    }

    if (body.action === "create") {
      const monoRes = await fetch(
        `${monolithUrl.replace(/\/$/, "")}/api/demo/companies`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json", ...traceHeaders },
          body: JSON.stringify({
            name: body.companyName,
            industryUId: undefined, // resolved server-side; landing keeps the API thin
            adminFirstName: body.firstName,
            adminLastName: body.lastName,
          }),
        },
      );
      const data = await monoRes.json();
      return NextResponse.json(data, { status: monoRes.status, headers: cors });
    }

    if (body.action === "claim") {
      const monoRes = await fetch(
        `${monolithUrl.replace(/\/$/, "")}/api/demo/companies/${body.companyUId}/claim`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json", ...traceHeaders },
          body: JSON.stringify({
            claimToken: body.claimToken,
            keycloakUserId: body.keycloakUserId,
            email: body.email,
            firstName: body.firstName,
            lastName: body.lastName,
          }),
        },
      );
      const data = await monoRes.json();
      return NextResponse.json(data, { status: monoRes.status, headers: cors });
    }

    return NextResponse.json({ error: "Unknown action." }, { status: 400, headers: cors });
  } catch (err) {
    console.error("[DemoChat] Backend call failed:", err);
    return NextResponse.json(
      { error: "Demo backend unavailable." },
      { status: 502, headers: cors },
    );
  }
}
