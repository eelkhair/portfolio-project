export const environment = {
  production: false,
  apiUrl: 'https://job-monolith-dev.eelkhair.net/api/',
  aiUrl: 'https://job-ai-v2-dev.eelkhair.net/',
  monolithUrl: 'https://job-monolith-dev.eelkhair.net/',
  otel: 'https://otel-dev.eelkhair.net/v1/traces',
  otelZipkin: 'https://otel-dev.eelkhair.net/api/v2/spans',
  oidc: {
    authority: 'https://auth.eelkhair.net/realms/job-board-dev',
    redirectUrl: 'https://job-board-dev.eelkhair.net',
    clientId: 'angular-public',
  },
};
