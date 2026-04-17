export const environment = {
  production: false,
  envName: 'LOCAL',
  gatewayUrl: 'http://localhost:5238/',
  // Direct URLs for SignalR WebSocket connections (can't proxy through Dapr invoke)
  monolithUrl: 'http://localhost:5280/',
  microserviceUrl: 'http://localhost:5262/',
  aiServiceUrl: 'http://localhost:5200/',
  otel: '/v1/traces',
  otelAspire: '/aspire/v1/traces',
  otelZipkin: '',
  grafanaUrl: 'http://localhost:3200/d/bf5m5dwukfncwd/find-by-trace-id?orgId=1&var-TraceId=',
  jaegerUrl: 'http://localhost:16686/trace/',
  seqUrl: 'http://localhost:5341/#/events?filter=TraceId%3D%22{traceId}%22',
  oidc: {
    authority: 'http://localhost:9999/realms/job-board-local',
    redirectUrl: 'http://localhost:4200',
    clientId: 'angular-admin',
  },
  // Cloudflare Turnstile test site key — always passes verification locally.
  turnstileSiteKey: '1x00000000000000000000AA',
};
