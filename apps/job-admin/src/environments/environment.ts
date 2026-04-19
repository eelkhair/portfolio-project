// Derive base domain from the current hostname so both eelkhair.net and elkhair.tech work
const host = typeof window !== 'undefined' ? window.location.hostname : '';
const baseDomain = host.endsWith('.elkhair.tech') ? 'elkhair.tech'
  : host.endsWith('.eelkhair.net') ? 'eelkhair.net'
  : 'elkhair.tech'; // fallback

export const environment = {
  production: true,
  envName: 'PROD',
  gatewayUrl: `https://job-gateway.${baseDomain}/`,
  // Direct URLs for SignalR WebSocket connections (can't proxy through Dapr invoke)
  monolithUrl: `https://job-monolith.${baseDomain}/`,
  microserviceUrl: `https://job-admin-api.${baseDomain}/`,
  aiServiceUrl: `https://job-ai-v2.${baseDomain}/`,
  landingUrl: `https://${baseDomain}/`,
  otel: `https://otel.${baseDomain}/v1/traces`,
  otelAspire: undefined as string | undefined,
  otelZipkin: `https://otel.${baseDomain}/api/v2/spans`,
  // Grafana Faro RUM — POSTs through Cloudflare to Alloy on .160.
  faroUrl: 'https://faro.elkhair.tech/collect',
  // Landing's /api/geo endpoint (cross-origin), enriches Faro spans with
  // visitor country/city/region/lat/lon. Hardcoded to the .elkhair.tech
  // zone (Cloudflare-served) — the .eelkhair.net zone goes through NPM
  // which strips cf-connecting-ip, so the upstream geo handler can't
  // resolve a real IP and always returns XX.
  geoApiUrl: 'https://elkhair.tech/api/geo',
  grafanaUrl: `https://grafana.${baseDomain}/d/bf5m5dwukfncwd/find-by-trace-id?orgId=1&var-TraceId=`,
  jaegerUrl: `https://jaeger.${baseDomain}/trace/`,
  oidc: {
    authority: `https://auth.${baseDomain}/realms/job-board`,
    redirectUrl: `https://job-admin.${baseDomain}`,
    clientId: 'angular-admin',
  },
  // Real Cloudflare Turnstile site key — shared widget with landing + dev.
  turnstileSiteKey: '0x4AAAAAAC-PTR9B6RUXVbVA',
};
