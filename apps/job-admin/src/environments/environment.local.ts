export const environment = {
  production: false,
  envName: 'LOCAL',
  gatewayUrl: 'http://localhost:5238/',
  // Direct URLs for SignalR WebSocket connections (can't proxy through Dapr invoke)
  monolithUrl: 'http://localhost:5280/',
  microserviceUrl: 'http://localhost:5262/',
  aiServiceUrl: 'http://localhost:5200/',
  otel: '/v1/traces',
  otelZipkin: '',
  grafanaUrl: 'http://localhost:3200/d/bf5m5dwukfncwd/find-by-trace-id?orgId=1&var-TraceId=',
  jaegerUrl: 'http://localhost:16686/trace/',
  oidc: {
    authority: 'http://localhost:9999/realms/job-board-local',
    redirectUrl: 'http://localhost:4200',
    clientId: 'angular-admin',
  },
};
