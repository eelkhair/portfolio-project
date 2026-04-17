// Dev environment — single public domain: elkhair.tech.
const landingBaseDomain = 'elkhair.tech';

export const environment = {
  production: false,
  envName: 'DEV',
  apiUrl: 'https://job-gateway-dev.elkhair.tech/api/',
  aiUrl: 'https://job-ai-v2-dev.elkhair.tech/',
  monolithUrl: 'https://job-monolith-dev.elkhair.tech/',
  landingUrl: `https://${landingBaseDomain}/`,
  otel: 'https://otel.elkhair.tech/v1/traces',
  otelZipkin: 'https://otel.elkhair.tech/api/v2/spans',
  jaegerUrl: 'https://jaeger.elkhair.tech/trace/',
  seqUrl: 'https://seq.elkhair.tech/#/events?filter=TraceId%3D%22{traceId}%22',
  grafanaUrl: 'https://grafana.elkhair.tech/d/bf5m5dwukfncwd/find-by-trace-id?orgId=1&var-TraceId=',
  oidc: {
    authority: 'https://auth.elkhair.tech/realms/job-board-dev',
    redirectUrl: 'https://jobs-dev.elkhair.tech',
    clientId: 'angular-public',
  },
  // Real Cloudflare Turnstile site key — shared with the landing page widget.
  turnstileSiteKey: '0x4AAAAAAC-PTR9B6RUXVbVA',
};
