export const environment = {
  production: false,
  envName: 'LOCAL',
  apiUrl: 'http://localhost:5280/api/',
  aiUrl: 'http://localhost:5200/',
  monolithUrl: 'http://localhost:5280/',
  otel: '/v1/traces',
  otelZipkin: 'api/v2/spans',
  oidc: {
    authority: 'http://localhost:9999/realms/job-board-local',
    redirectUrl: 'http://localhost:3000',
    clientId: 'angular-public',
  },
};
