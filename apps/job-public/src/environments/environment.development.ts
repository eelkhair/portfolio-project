export const environment = {
  production: false,
  envName: 'DEV',
  apiUrl: 'https://job-gateway-dev.eelkhair.net/api/',
  aiUrl: 'https://job-ai-v2-dev.eelkhair.net/',
  monolithUrl: 'https://job-monolith-dev.eelkhair.net/',
  otel: 'https://otel.eelkhair.net/v1/traces',
  otelZipkin: 'https://otel.eelkhair.net/api/v2/spans',
  jaegerUrl: 'https://jaeger.eelkhair.net/trace/',
  seqUrl: 'https://seq.eelkhair.net/#/events?filter=TraceId%3D%22{traceId}%22',
  grafanaUrl: 'https://grafana.eelkhair.net/d/bf5m5dwukfncwd/find-by-trace-id?orgId=1&var-TraceId=',
  oidc: {
    authority: 'https://auth.eelkhair.net/realms/job-board-dev',
    redirectUrl: 'https://jobs-dev.eelkhair.net',
    clientId: 'angular-public',
  },
  // Real Cloudflare Turnstile site key — fill from the Cloudflare dashboard for dev.
  turnstileSiteKey: '1x00000000000000000000AA',
};
