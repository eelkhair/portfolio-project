const host = typeof window !== 'undefined' ? window.location.hostname : '';
const baseDomain =
  host.endsWith('.elkhair.tech') ? 'elkhair.tech'
  : host.endsWith('.eelkhair.net') ? 'eelkhair.net'
  : 'elkhair.tech';
console.log("HOST: '" + host + "'", "BASEDOMAIN: '" + baseDomain + "'");
export const environment = {
  production: false,
  envName: 'DEV',
  apiUrl: `https://job-gateway-dev.${baseDomain}/api/`,
  aiUrl: `https://job-ai-v2-dev.${baseDomain}/`,
  monolithUrl: `https://job-monolith-dev.${baseDomain}/`,
  landingUrl: `https://${baseDomain}/`,
  otel: `https://otel.${baseDomain}/v1/traces`,
  otelZipkin: `https://otel.${baseDomain}/api/v2/spans`,
  faroUrl: 'https://faro.elkhair.tech/collect',
  // Gateway /api/public/geo, MaxMind mmdb in the gateway image. Hardcoded
  // to .elkhair.tech — .eelkhair.net zone strips cf-connecting-ip at NPM,
  // so lookups return XX there.
  geoApiUrl: 'https://job-gateway-dev.elkhair.tech/api/public/geo',
  jaegerUrl: `https://jaeger.${baseDomain}/trace/`,
  grafanaUrl: `https://grafana.${baseDomain}/d/bf5m5dwukfncwd/find-by-trace-id?orgId=1&var-TraceId=`,
  oidc: {
    authority: `https://auth.${baseDomain}/realms/job-board-dev`,
    redirectUrl: `https://jobs-dev.${baseDomain}`,
    clientId: 'angular-public',
  },
  turnstileSiteKey: '0x4AAAAAAC-PTR9B6RUXVbVA',
};
