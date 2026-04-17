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
  otel: `https://otel.${baseDomain}/v1/traces`,
  otelAspire: undefined as string | undefined,
  otelZipkin: `https://otel.${baseDomain}/api/v2/spans`,
  grafanaUrl: `https://grafana.${baseDomain}/d/bf5m5dwukfncwd/find-by-trace-id?orgId=1&var-TraceId=`,
  jaegerUrl: `https://jaeger.${baseDomain}/trace/`,
  seqUrl: `https://seq.${baseDomain}/#/events?filter=TraceId%3D%22{traceId}%22`,
  oidc: {
    authority: `https://auth.${baseDomain}/realms/job-board`,
    redirectUrl: `https://job-admin.${baseDomain}`,
    clientId: 'angular-admin',
  },
  // Replaced at build time via GitHub Actions; see deploy workflow. Falls back to Turnstile test key.
  turnstileSiteKey: '${TURNSTILE_SITE_KEY}',
};
