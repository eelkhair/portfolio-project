// PKCE helpers for the landing-page demo claim flow.
// Edge-runtime safe: relies only on Web Crypto + TextEncoder.

function base64UrlEncode(bytes: ArrayBuffer | Uint8Array): string {
  const bin = String.fromCharCode(...new Uint8Array(bytes as ArrayBuffer));
  return btoa(bin).replace(/\+/g, "-").replace(/\//g, "_").replace(/=+$/, "");
}

export function generateRandomString(length = 64): string {
  const buf = new Uint8Array(length);
  crypto.getRandomValues(buf);
  return base64UrlEncode(buf).slice(0, length);
}

export async function pkceChallenge(verifier: string): Promise<string> {
  const data = new TextEncoder().encode(verifier);
  const digest = await crypto.subtle.digest("SHA-256", data);
  return base64UrlEncode(digest);
}
