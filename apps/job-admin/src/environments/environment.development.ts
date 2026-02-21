export const environment = {
  production: false,
  gatewayUrl: 'http://localhost:5238/',
  // Direct URLs for SignalR WebSocket connections (can't proxy through Dapr invoke)
  monolithUrl: 'http://localhost:5280/',
  microserviceUrl: 'http://localhost:5262/',
  aiServiceUrl: 'http://localhost:5200/',
  otel: '/v1/traces',
  otelZipkin: 'api/v2/spans'
};
