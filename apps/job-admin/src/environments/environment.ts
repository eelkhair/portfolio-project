// Single public domain: elkhair.tech. All URLs built from one base for
// easy future moves.
const baseDomain = 'elkhair.tech';

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
  grafanaUrl: `https://grafana.${baseDomain}/d/bf5m5dwukfncwd/find-by-trace-id?orgId=1&var-TraceId=`,
  jaegerUrl: `https://jaeger.${baseDomain}/trace/`,
  seqUrl: `https://seq.${baseDomain}/#/events?filter=TraceId%3D%22{traceId}%22`,
  oidc: {
    authority: `https://auth.${baseDomain}/realms/job-board`,
    redirectUrl: `https://job-admin.${baseDomain}`,
    clientId: 'angular-admin',
  },
  // Real Cloudflare Turnstile site key — shared widget with landing + dev.
  turnstileSiteKey: '0x4AAAAAAC-PTR9B6RUXVbVA',
};
