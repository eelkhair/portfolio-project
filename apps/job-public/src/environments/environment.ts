// Derive base domain from the current hostname so both eelkhair.net and elkhair.tech work
const host = typeof window !== 'undefined' ? window.location.hostname : '';
const baseDomain = host.endsWith('.elkhair.tech') ? 'elkhair.tech'
  : host.endsWith('.eelkhair.net') ? 'eelkhair.net'
  : 'elkhair.tech'; // fallback

console.log(host, baseDomain);
export const environment = {
  production: true,
  envName: 'PROD',
  apiUrl: `https://job-gateway.${baseDomain}/api/`,
  aiUrl: `https://job-ai-v2.${baseDomain}/`,
  monolithUrl: `https://job-monolith.${baseDomain}/`,
  landingUrl: `https://${baseDomain}/`,
  otel: `https://otel.${baseDomain}/v1/traces`,
  otelZipkin: `https://otel.${baseDomain}/api/v2/spans`,
  // Grafana Faro RUM — POSTs through Cloudflare to Alloy on .160.
  faroUrl: 'https://faro.elkhair.tech/collect',
  // Gateway /api/public/geo endpoint (cross-origin), enriches Faro spans
  // with visitor country/city/region/lat/lon. Backed by a MaxMind GeoLite2
  // mmdb baked into the gateway image. Hardcoded to .elkhair.tech — the
  // .eelkhair.net zone strips cf-connecting-ip at NPM, so lookups return XX.
  geoApiUrl: 'https://job-gateway.elkhair.tech/api/public/geo',
  jaegerUrl: `https://jaeger.${baseDomain}/trace/`,
  grafanaUrl: `https://grafana.${baseDomain}/d/bf5m5dwukfncwd/find-by-trace-id?orgId=1&var-TraceId=`,
  oidc: {
    authority: `https://auth.${baseDomain}/realms/job-board`,
    redirectUrl: `https://jobs.${baseDomain}`,
    clientId: 'angular-public',
  },
  // Real Cloudflare Turnstile site key — shared widget with landing + dev.
  turnstileSiteKey: '0x4AAAAAAC-PTR9B6RUXVbVA',
};
