export const environment = {
  production: false,
  envName: 'LOCAL',
  apiUrl: 'http://localhost:5280/api/',
  aiUrl: 'http://localhost:5200/',
  monolithUrl: 'http://localhost:5280/',
  otel: '/v1/traces',
  otelZipkin: 'api/v2/spans',
  jaegerUrl: 'http://localhost:16686/trace/',
  seqUrl: 'http://localhost:5341/#/events?filter=TraceId%3D%22{traceId}%22',
  grafanaUrl: 'http://localhost:3200/d/bf5m5dwukfncwd/find-by-trace-id?orgId=1&var-TraceId=',
  oidc: {
    authority: 'http://localhost:9999/realms/job-board-local',
    redirectUrl: 'http://localhost:3000',
    clientId: 'angular-public',
  },
  // Cloudflare Turnstile test site key — always passes verification locally.
  turnstileSiteKey: '1x00000000000000000000AA',
};
