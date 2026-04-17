// Derive base domain from the current hostname so both eelkhair.net and elkhair.tech work
const host = typeof window !== 'undefined' ? window.location.hostname : '';
const baseDomain = host.endsWith('.elkhair.tech') ? 'elkhair.tech'
  : host.endsWith('.eelkhair.net') ? 'eelkhair.net'
  : 'elkhair.tech'; // fallback

export const environment = {
  production: true,
  envName: 'PROD',
  apiUrl: `https://job-gateway.${baseDomain}/api/`,
  aiUrl: `https://job-ai-v2.${baseDomain}/`,
  monolithUrl: `https://job-monolith.${baseDomain}/`,
  otel: `https://otel.${baseDomain}/v1/traces`,
  otelZipkin: `https://otel.${baseDomain}/api/v2/spans`,
  jaegerUrl: `https://jaeger.${baseDomain}/trace/`,
  seqUrl: `https://seq.${baseDomain}/#/events?filter=TraceId%3D%22{traceId}%22`,
  grafanaUrl: `https://grafana.${baseDomain}/d/bf5m5dwukfncwd/find-by-trace-id?orgId=1&var-TraceId=`,
  oidc: {
    authority: `https://auth.${baseDomain}/realms/job-board`,
    redirectUrl: `https://jobs.${baseDomain}`,
    clientId: 'angular-public',
  },
  // Real Cloudflare Turnstile site key — shared widget with landing + dev.
  turnstileSiteKey: '0x4AAAAAAC-PTR9B6RUXVbVA',
};
