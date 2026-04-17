// Dev environment — single public domain: elkhair.tech.
const landingBaseDomain = 'elkhair.tech';

export const environment = {
  production: false,
  envName: 'DEV',
  gatewayUrl: 'https://job-gateway-dev.elkhair.tech/',
  // Direct URLs for SignalR WebSocket connections (can't proxy through Dapr invoke)
  monolithUrl: 'https://job-monolith-dev.elkhair.tech/',
  microserviceUrl: 'https://job-admin-api-dev.elkhair.tech/',
  aiServiceUrl: 'https://job-ai-v2-dev.elkhair.tech/',
  landingUrl: `https://${landingBaseDomain}/`,
  otel: 'https://otel.elkhair.tech/v1/traces',
  otelAspire: undefined as string | undefined,
  otelZipkin: 'https://otel.elkhair.tech/api/v2/spans',
  grafanaUrl: 'https://grafana.elkhair.tech/d/bf5m5dwukfncwd/find-by-trace-id?orgId=1&var-TraceId=',
  jaegerUrl: 'https://jaeger.elkhair.tech/trace/',
  seqUrl: 'https://seq.elkhair.tech/#/events?filter=TraceId%3D%22{traceId}%22',
  oidc: {
    authority: 'https://auth.elkhair.tech/realms/job-board-dev',
    redirectUrl: 'https://job-admin-dev.elkhair.tech',
    clientId: 'angular-admin',
  },
  // Real Cloudflare Turnstile site key — shared with the landing page widget.
  turnstileSiteKey: '0x4AAAAAAC-PTR9B6RUXVbVA',
};
