// Azure Container Apps deployment
// URLs are set at build time via GitHub Actions — replace placeholders before building
export const environment = {
  production: true,
  envName: 'AZURE',
  apiUrl: '${GATEWAY_URL}/api/',
  aiUrl: '${GATEWAY_URL}/ai/v2/',
  monolithUrl: '${GATEWAY_URL}/',
  landingUrl: '${LANDING_URL}/',
  otel: undefined as string | undefined,
  otelZipkin: undefined as string | undefined,
  oidc: {
    authority: '${KEYCLOAK_URL}/realms/job-board',
    redirectUrl: '${JOB_PUBLIC_URL}',
    clientId: 'angular-public',
  },
  // Real Cloudflare Turnstile site key.
  turnstileSiteKey: '0x4AAAAAAC-PTR9B6RUXVbVA',
};
