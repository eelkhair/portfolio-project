export const environment = {
  production: true,
  gatewayUrl: 'https://job-gateway.eelkhair.net/',
  // Direct URLs for SignalR WebSocket connections (can't proxy through Dapr invoke)
  monolithUrl: 'https://job-monolith.eelkhair.net/',
  microserviceUrl: 'https://job-admin-api.eelkhair.net/',
  aiServiceUrl: 'https://job-ai-v2.eelkhair.net/',
  otel:'https://otel.eelkhair.net/v1/traces',
  otelZipkin: 'https://otel.eelkhair.net/api/v2/spans',
};
