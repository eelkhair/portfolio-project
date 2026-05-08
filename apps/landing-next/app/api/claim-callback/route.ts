import { NextRequest, NextResponse } from "next/server";

export const runtime = "edge";

interface ClaimRequest {
  code?: string;
  codeVerifier?: string;
  claimToken?: string;
  companyUId?: string;
  redirectUri?: string;
}

interface IdTokenClaims {
  sub?: string;
  email?: string;
  given_name?: string;
  family_name?: string;
  preferred_username?: string;
}

/**
 * Decode a JWT payload without verifying the signature.
 * Safe here because:
 * - we just exchanged the auth code with Keycloak ourselves;
 * - the id_token came back over TLS in the same response we trust;
 * - we use the `sub` claim only as an opaque identifier, not as an
 *   authorization assertion.
 */
function decodeJwtPayload(token: string): IdTokenClaims | null {
  const parts = token.split(".");
  if (parts.length !== 3) return null;
  try {
    // Edge runtime: atob handles base64url after we restore padding.
    const padded = parts[1].replace(/-/g, "+").replace(/_/g, "/");
    const pad = padded.length % 4 === 0 ? "" : "=".repeat(4 - (padded.length % 4));
    const json = atob(padded + pad);
    return JSON.parse(json);
  } catch {
    return null;
  }
}

export async function POST(req: NextRequest) {
  let body: ClaimRequest;
  try {
    body = await req.json();
  } catch {
    return NextResponse.json({ error: "Invalid request body." }, { status: 400 });
  }

  const { code, codeVerifier, claimToken, companyUId, redirectUri } = body;
  if (!code || !codeVerifier || !claimToken || !companyUId || !redirectUri) {
    return NextResponse.json({ error: "Missing claim parameters." }, { status: 400 });
  }

  const keycloakAuthority =
    process.env.KEYCLOAK_AUTHORITY ?? process.env.NEXT_PUBLIC_KEYCLOAK_AUTHORITY;
  const clientId = process.env.KEYCLOAK_CLAIM_CLIENT_ID
    ?? process.env.NEXT_PUBLIC_KEYCLOAK_CLAIM_CLIENT_ID
    ?? "landing-claim";
  const monolithUrl = process.env.MONOLITH_URL;

  if (!keycloakAuthority || !monolithUrl) {
    console.error("[ClaimCallback] Missing KEYCLOAK_AUTHORITY or MONOLITH_URL env");
    return NextResponse.json(
      { error: "Claim flow is not configured on this environment." },
      { status: 500 },
    );
  }

  // Forward the trace context so the OIDC exchange + monolith claim land
  // in one trace in Jaeger alongside the saga that follows.
  const traceHeaders: Record<string, string> = {};
  for (const h of ["traceparent", "tracestate", "baggage"]) {
    const v = req.headers.get(h);
    if (v) traceHeaders[h] = v;
  }

  // 1. Exchange the auth code for tokens (PKCE — no client secret needed).
  const tokenUrl = `${keycloakAuthority.replace(/\/$/, "")}/protocol/openid-connect/token`;
  const tokenForm = new URLSearchParams({
    grant_type: "authorization_code",
    code,
    code_verifier: codeVerifier,
    redirect_uri: redirectUri,
    client_id: clientId,
  });

  let tokenRes: Response;
  try {
    tokenRes = await fetch(tokenUrl, {
      method: "POST",
      headers: {
        "Content-Type": "application/x-www-form-urlencoded",
        ...traceHeaders,
      },
      body: tokenForm.toString(),
    });
  } catch (err) {
    console.error("[ClaimCallback] Keycloak token endpoint unreachable:", err);
    return NextResponse.json({ error: "Authentication server unreachable." }, { status: 502 });
  }

  if (!tokenRes.ok) {
    const errText = await tokenRes.text().catch(() => "");
    console.error("[ClaimCallback] Token exchange failed:", tokenRes.status, errText);
    return NextResponse.json(
      { error: "Could not exchange authorization code." },
      { status: 401 },
    );
  }

  const tokens = await tokenRes.json();
  const idToken: string | undefined = tokens.id_token;
  if (!idToken) {
    return NextResponse.json({ error: "No id_token in token response." }, { status: 502 });
  }

  const claims = decodeJwtPayload(idToken);
  if (!claims?.sub || !claims.email) {
    return NextResponse.json({ error: "id_token missing required claims." }, { status: 502 });
  }

  // 2. Hand off to the monolith claim endpoint with the user's identity.
  const claimRes = await fetch(
    `${monolithUrl.replace(/\/$/, "")}/api/demo/companies/${companyUId}/claim`,
    {
      method: "POST",
      headers: { "Content-Type": "application/json", ...traceHeaders },
      body: JSON.stringify({
        claimToken,
        keycloakUserId: claims.sub,
        email: claims.email,
        firstName: claims.given_name ?? claims.preferred_username ?? "",
        lastName: claims.family_name ?? "",
      }),
    },
  );

  const claimData = await claimRes.json().catch(() => ({}));
  if (!claimRes.ok) {
    return NextResponse.json(claimData, { status: claimRes.status });
  }

  return NextResponse.json({
    ok: true,
    company: claimData,
    user: {
      email: claims.email,
      firstName: claims.given_name ?? null,
      lastName: claims.family_name ?? null,
    },
  });
}
