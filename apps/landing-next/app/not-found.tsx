import Link from "next/link";

// Required by @cloudflare/next-on-pages: every SSR route must opt into edge runtime.
export const runtime = "edge";

export default function NotFound() {
  return (
    <main id="main" style={{ minHeight: "100vh", display: "flex", alignItems: "center", justifyContent: "center", textAlign: "center", padding: "2rem" }}>
      <div>
        <h1 style={{ fontSize: "3rem", fontWeight: 700, marginBottom: "1rem" }}>404</h1>
        <p style={{ color: "var(--text-secondary)", marginBottom: "2rem" }}>Page not found</p>
        <Link href="/" className="btn btn-primary">Go Home</Link>
      </div>
    </main>
  );
}
