// Derive landing base from current hostname so the contact form posts same-zone
// (jobs-dev.eelkhair.net → eelkhair.net/api/contact; jobs-dev.elkhair.tech →
// elkhair.tech/api/contact). Avoids cross-zone cert mismatches and keeps CORS
// identical to the document origin. Defaults to elkhair.tech for SSR/unknown hosts.
const landingHost = typeof window !== 'undefined' ? window.location.hostname : '';
const landingBaseDomain = landingHost.endsWith('.elkhair.tech') ? 'elkhair.tech'
  : landingHost.endsWith('.eelkhair.net') ? 'eelkhair.net'
  : 'elkhair.tech';

export const environment = {
  production: false,
  envName: 'DEV',
  gatewayUrl: 'https://job-gateway-dev.eelkhair.net/',
  // Direct URLs for SignalR WebSocket connections (can't proxy through Dapr invoke)
  monolithUrl: 'https://job-monolith-dev.eelkhair.net/',
  microserviceUrl: 'https://job-admin-api-dev.eelkhair.net/',
  aiServiceUrl: 'https://job-ai-v2-dev.eelkhair.net/',
  landingUrl: `https://${landingBaseDomain}/`,
  otel: 'https://otel.eelkhair.net/v1/traces',
  otelAspire: undefined as string | undefined,
  otelZipkin: 'https://otel.eelkhair.net/api/v2/spans',
  // Grafana Faro RUM — POSTs through Cloudflare to Alloy on .160.
  faroUrl: 'https://faro-dev.elkhair.tech/collect',
  // Landing's /api/geo endpoint (cross-origin), enriches Faro spans with
  // visitor country/city/region/lat/lon. Points at DEV landing so dev admin
  // doesn't depend on prod landing being up-to-date (prod may lag a deploy).
  // Only dev.elkhair.tech has the dev landing — both .tech and .net app
  // origins are CORS-allowed on that endpoint.
  geoApiUrl: 'https://dev.elkhair.tech/api/geo',
  grafanaUrl: 'https://grafana.eelkhair.net/d/bf5m5dwukfncwd/find-by-trace-id?orgId=1&var-TraceId=',
  jaegerUrl: 'https://jaeger.eelkhair.net/trace/',
  seqUrl: 'https://seq.eelkhair.net/#/events?filter=TraceId%3D%22{traceId}%22',
  oidc: {
    authority: 'https://auth.eelkhair.net/realms/job-board-dev',
    redirectUrl: 'https://job-admin-dev.eelkhair.net',
    clientId: 'angular-admin',
  },
  // Real Cloudflare Turnstile site key — shared with the landing page widget.
  turnstileSiteKey: '0x4AAAAAAC-PTR9B6RUXVbVA',
};
