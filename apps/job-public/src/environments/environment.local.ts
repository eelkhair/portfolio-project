export const environment = {
  production: false,
  envName: 'LOCAL',
  apiUrl: 'http://localhost:5280/api/',
  aiUrl: 'http://localhost:5200/',
  monolithUrl: 'http://localhost:5280/',
  landingUrl: 'http://localhost:3001/',
  otel: '/v1/traces',
  otelZipkin: 'api/v2/spans',
  // Grafana Faro RUM — Aspire runs Alloy's Faro receiver on :12347.
  // Set to '' to disable (initFaro no-ops).
  faroUrl: 'http://localhost:12347/collect',
  // Gateway /api/public/geo endpoint — Aspire runs gateway on :5238. Falls
  // back to "XX" geo if unreachable (dev builds without mmdb).
  geoApiUrl: 'http://localhost:5238/api/public/geo',
  jaegerUrl: 'http://localhost:16686/trace/',
  grafanaUrl: 'http://localhost:3200/d/bf5m5dwukfncwd/find-by-trace-id?orgId=1&var-TraceId=',
  oidc: {
    authority: 'http://localhost:9999/realms/job-board-local',
    redirectUrl: 'http://localhost:3000',
    clientId: 'angular-public',
  },
  // Cloudflare Turnstile test site key — always passes verification locally.
  turnstileSiteKey: '1x00000000000000000000AA',
};
