/**
 * Next.js 15 instrumentation hook — registers OpenTelemetry on server startup.
 *
 * Registers ONLY on the Node.js runtime (Proxmox landing container).
 * Skipped on the Edge runtime because `@vercel/otel` relies on Node-only
 * APIs (`async_hooks.AsyncLocalStorage`) that Cloudflare Workers don't
 * fully support even with `nodejs_compat`, and because our per-page
 * `export const runtime = 'edge'` declarations route everything through
 * the edge runtime on CF Pages anyway — there's nothing for `@vercel/otel`
 * to instrument there. Dropping it on edge is the explicit tradeoff:
 * Proxmox keeps the rich Next.js render spans (`RSC GET`, `resolve page
 * components`, `start response`), CF Pages only gets the client-side
 * Faro spans.
 *
 * The `register()` export name is a Next.js contract; do not rename.
 */
export async function register() {
  // `NEXT_RUNTIME` is injected by Next.js: 'nodejs' | 'edge'.
  if (process.env.NEXT_RUNTIME !== 'nodejs') return;

  const { registerOTel } = await import('@vercel/otel');
  registerOTel({
    serviceName: process.env.OTEL_SERVICE_NAME ?? 'landing',
  });
}
