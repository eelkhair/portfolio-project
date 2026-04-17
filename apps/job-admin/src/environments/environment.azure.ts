// Azure Container Apps deployment
// URLs are set at build time via GitHub Actions — replace placeholders before building
export const environment = {
  production: true,
  envName: 'AZURE',
  gatewayUrl: '${GATEWAY_URL}/',
  monolithUrl: '${GATEWAY_URL}/',
  microserviceUrl: '${GATEWAY_URL}/',
  aiServiceUrl: '${GATEWAY_URL}/ai/v2/',
  otel: undefined as string | undefined,
  otelAspire: undefined as string | undefined,
  otelZipkin: undefined as string | undefined,
  grafanaUrl: undefined as string | undefined,
  jaegerUrl: undefined as string | undefined,
  seqUrl: undefined as string | undefined,
  oidc: {
    authority: '${KEYCLOAK_URL}/realms/job-board',
    redirectUrl: '${JOB_ADMIN_URL}',
    clientId: 'angular-admin',
  },
  // Real Cloudflare Turnstile site key.
  turnstileSiteKey: '0x4AAAAAAC-PTR9B6RUXVbVA',
};
