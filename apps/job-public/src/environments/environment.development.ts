export const environment = {
  production: false,
  envName: 'DEV',
  apiUrl: 'https://job-monolith-dev.eelkhair.net/api/',
  aiUrl: 'https://job-ai-v2-dev.eelkhair.net/',
  monolithUrl: 'https://job-monolith-dev.eelkhair.net/',
  otel: 'https://otel.eelkhair.net/v1/traces',
  otelZipkin: 'https://otel.eelkhair.net/api/v2/spans',
  oidc: {
    authority: 'https://auth.eelkhair.net/realms/job-board-dev',
    redirectUrl: 'https://jobs-dev.eelkhair.net',
    clientId: 'angular-public',
  },
};
