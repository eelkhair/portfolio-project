export const environment = {
  production: false,
  envName: 'DEV',
  gatewayUrl: 'https://job-gateway-dev.eelkhair.net/',
  // Direct URLs for SignalR WebSocket connections (can't proxy through Dapr invoke)
  monolithUrl: 'https://job-monolith-dev.eelkhair.net/',
  microserviceUrl: 'https://job-admin-api-dev.eelkhair.net/',
  aiServiceUrl: 'https://job-ai-v2-dev.eelkhair.net/',
  otel: 'https://otel.eelkhair.net/v1/traces',
  otelAspire: undefined as string | undefined,
  otelZipkin: 'https://otel.eelkhair.net/api/v2/spans',
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
