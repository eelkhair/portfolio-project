export const environment = {
  production: true,
  envName: 'PROD',
  apiUrl: 'https://job-monolith.eelkhair.net/api/',
  aiUrl: 'https://job-ai-v2.eelkhair.net/',
  monolithUrl: 'https://job-monolith.eelkhair.net/',
  otel: 'https://otel.eelkhair.net/v1/traces',
  otelZipkin: 'https://otel.eelkhair.net/api/v2/spans',
  oidc: {
    authority: 'https://auth.eelkhair.net/realms/job-board',
    redirectUrl: 'https://jobs.eelkhair.net',
    clientId: 'angular-public',
  },
};
