import { NextRequest, NextResponse } from "next/server";
import { Resend } from "resend";

// In-memory rate limit: one successful submit per IP per window.
// Behind Cloudflare + tunnel, real client IP arrives as `cf-connecting-ip`;
// `x-forwarded-for` is a comma-separated chain whose first entry is the client.
// Process memory is per-container — deployments/restarts reset the map.
const rateLimit = new Map<string, number>();
const RATE_LIMIT_MS = 30_000;

/** Best-effort client IP extraction for rate-limit bucketing. */
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

/** Periodic sweep: drop entries older than 2× the rate-limit window. */
function pruneRateLimit(now: number): void {
  if (rateLimit.size < 100) return; // cheap threshold; avoid churn
  const cutoff = now - RATE_LIMIT_MS * 2;
  for (const [k, t] of rateLimit) {
    if (t < cutoff) rateLimit.delete(k);
  }
}

// Allowed origins that may POST to this endpoint from another domain.
// The landing page itself calls it same-origin; the Angular admin and public apps
// post cross-origin and need CORS allow-list + credential pass-through.
const ALLOWED_ORIGINS = new Set([
  "https://elkhair.tech",
  "https://job-admin.elkhair.tech",
  "https://job-admin-dev.elkhair.tech",
  "https://jobs.elkhair.tech",
  "https://jobs-dev.elkhair.tech",
  "http://localhost:4200",
  "http://localhost:3000",
]);

function corsHeaders(origin: string | null): Record<string, string> {
  const allow = origin && ALLOWED_ORIGINS.has(origin) ? origin : "";
  if (!allow) return {};
  return {
    "Access-Control-Allow-Origin": allow,
    "Access-Control-Allow-Methods": "POST, OPTIONS",
    // Angular apps send Content-Type plus tracing headers via their TracingInterceptor
    // (x-mode, traceparent, tracestate, baggage, x-b3-*). Echo the requested headers
    // back so the preflight passes regardless of which tracing lib is in play.
    "Access-Control-Allow-Headers": "Content-Type, x-mode, traceparent, tracestate, baggage, x-b3-traceid, x-b3-spanid, x-b3-sampled, x-b3-flags, x-b3-parentspanid, b3",
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

export async function POST(req: NextRequest) {
  const cors = corsHeaders(req.headers.get("origin"));
  const ip = clientIp(req);
  const now = Date.now();
  pruneRateLimit(now);
  const lastSent = rateLimit.get(ip);
  if (lastSent && now - lastSent < RATE_LIMIT_MS) {
    const waitMs = RATE_LIMIT_MS - (now - lastSent);
    console.warn(`[Contact] Rate limited: ip=${ip}, wait=${waitMs}ms`);
    return NextResponse.json(
      { error: "Please wait before sending another message." },
      { status: 429, headers: cors },
    );
  }

  let body: { name?: string; email?: string; subject?: string; message?: string; token?: string };
  try {
    body = await req.json();
  } catch (err) {
    console.error("[Contact] Failed to parse request body:", err);
    return NextResponse.json({ error: "Invalid request." }, { status: 400, headers: cors });
  }

  const { name, email, subject, message, token } = body;

  if (!token) {
    console.error("[Contact] Missing Turnstile token: ip=%s", ip);
    return NextResponse.json({ error: "Verification required." }, { status: 400, headers: cors });
  }

  const turnstileRes = await fetch("https://challenges.cloudflare.com/turnstile/v0/siteverify", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      secret: process.env.TURNSTILE_SECRET_KEY,
      response: token,
      remoteip: ip,
    }),
  });
  const turnstileData = await turnstileRes.json();
  if (!turnstileData.success) {
    console.error("[Contact] Turnstile verification failed: ip=%s, errors=%j", ip, turnstileData["error-codes"]);
    return NextResponse.json({ error: "Verification failed. Please try again." }, { status: 403, headers: cors });
  }

  if (!name?.trim() || !email?.trim() || !message?.trim()) {
    return NextResponse.json(
      { error: "Name, email, and message are required." },
      { status: 400, headers: cors },
    );
  }

  const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
  if (!emailRegex.test(email)) {
    return NextResponse.json({ error: "Invalid email address." }, { status: 400, headers: cors });
  }

  // Resend SDK (not SMTP) — matches the official Next.js example env var names:
  // RESEND_API_KEY, EMAIL_FROM, CONTACT_EMAIL.
  // EMAIL_FROM must be on a domain verified in the Resend dashboard.
  const resendApiKey = process.env.RESEND_API_KEY;
  const emailFrom = process.env.EMAIL_FROM;
  const contactEmail = process.env.CONTACT_EMAIL;
  if (!resendApiKey || !emailFrom || !contactEmail) {
    console.error("[Contact] Missing env: RESEND_API_KEY/EMAIL_FROM/CONTACT_EMAIL");
    return NextResponse.json(
      { error: "Mail is not configured." },
      { status: 500, headers: cors },
    );
  }

  const resend = new Resend(resendApiKey);
  const resolvedSubject = subject?.trim() || `Portfolio Contact: ${name}`;

  try {
    const { data, error } = await resend.emails.send({
      from: emailFrom,
      to: contactEmail,
      replyTo: email,
      subject: resolvedSubject,
      text: `Name: ${name}\nEmail: ${email}\n\n${message}`,
      html: `
        <h3>New Contact Form Submission</h3>
        <p><strong>Name:</strong> ${escapeHtml(name)}</p>
        <p><strong>Email:</strong> ${escapeHtml(email)}</p>
        <p><strong>Subject:</strong> ${escapeHtml(subject?.trim() || "N/A")}</p>
        <hr />
        <p>${escapeHtml(message).replace(/\n/g, "<br />")}</p>
      `,
    });

    if (error) {
      console.error("[Contact] Resend error: from=%s, to=%s, error=%j", email, contactEmail, error);
      return NextResponse.json(
        { error: "Failed to send message. Please try again later." },
        { status: 502, headers: cors },
      );
    }

    rateLimit.set(ip, now);
    console.log("[Contact] Email sent: id=%s, from=%s, subject=%s", data?.id, email, resolvedSubject);
    return NextResponse.json({ success: true, id: data?.id }, { headers: cors });
  } catch (err) {
    console.error("[Contact] Resend exception: from=%s, to=%s, error=%s", email, contactEmail, err);
    return NextResponse.json(
      { error: "Failed to send message. Please try again later." },
      { status: 500, headers: cors },
    );
  }
}

function escapeHtml(str: string): string {
  return str
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;");
}
