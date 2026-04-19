export const environment = {
  production: false,
  envName: 'LOCAL',
  gatewayUrl: 'http://localhost:5238/',
  // Direct URLs for SignalR WebSocket connections (can't proxy through Dapr invoke)
  monolithUrl: 'http://localhost:5280/',
  microserviceUrl: 'http://localhost:5262/',
  aiServiceUrl: 'http://localhost:5200/',
  landingUrl: 'http://localhost:3001/',
  otel: '/v1/traces',
  otelAspire: '/aspire/v1/traces',
  otelZipkin: '',
  // Grafana Faro RUM — Aspire runs Alloy's Faro receiver on :12347.
  // Set to '' to disable (initFaro no-ops).
  faroUrl: 'http://localhost:12347/collect',
  // Landing's /api/geo endpoint — Aspire runs landing on :3001. Falls back
  // to "XX" geo if unreachable.
  geoApiUrl: 'http://localhost:3001/api/geo',
  grafanaUrl: 'http://localhost:3200/d/bf5m5dwukfncwd/find-by-trace-id?orgId=1&var-TraceId=',
  jaegerUrl: 'http://localhost:16686/trace/',
  oidc: {
    authority: 'http://localhost:9999/realms/job-board-local',
    redirectUrl: 'http://localhost:4200',
    clientId: 'angular-admin',
  },
  // Cloudflare Turnstile test site key — always passes verification locally.
  turnstileSiteKey: '1x00000000000000000000AA',
};
