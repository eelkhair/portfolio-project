const host = typeof window !== 'undefined' ? window.location.hostname : '';
const baseDomain = host.endsWith('.elkhair.tech') ? 'elkhair.tech'
  : host.endsWith('.eelkhair.net') ? 'eelkhair.net'
  : 'elkhair.tech';

export const environment = {
  production: false,
  envName: 'DEV',
  gatewayUrl: `https://job-gateway-dev.${baseDomain}/`,
  monolithUrl: `https://job-monolith-dev.${baseDomain}/`,
  microserviceUrl: `https://job-admin-api-dev.${baseDomain}/`,
  aiServiceUrl: `https://job-ai-v2-dev.${baseDomain}/`,
  landingUrl: `https://${baseDomain}/`,
  otel: `https://otel.${baseDomain}/v1/traces`,
  otelAspire: undefined as string | undefined,
  otelZipkin: `https://otel.${baseDomain}/api/v2/spans`,
  faroUrl: 'https://faro.elkhair.tech/collect',
  // Gateway /api/public/geo endpoint, MaxMind mmdb baked into the gateway
  // image. Hardcoded to .elkhair.tech — the .eelkhair.net zone goes through
  // NPM which strips cf-connecting-ip, causing lookups to return XX.
  geoApiUrl: 'https://job-gateway-dev.elkhair.tech/api/public/geo',
  grafanaUrl: `https://grafana.${baseDomain}/d/bf5m5dwukfncwd/find-by-trace-id?orgId=1&var-TraceId=`,
  jaegerUrl: `https://jaeger.${baseDomain}/trace/`,
  oidc: {
    authority: `https://auth.${baseDomain}/realms/job-board-dev`,
    redirectUrl: `https://job-admin-dev.${baseDomain}`,
    clientId: 'angular-admin',
  },
  turnstileSiteKey: '0x4AAAAAAC-PTR9B6RUXVbVA',
};
