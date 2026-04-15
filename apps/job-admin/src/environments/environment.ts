export const environment = {
  production: true,
  envName: 'PROD',
  gatewayUrl: 'https://job-gateway.eelkhair.net/',
  // Direct URLs for SignalR WebSocket connections (can't proxy through Dapr invoke)
  monolithUrl: 'https://job-monolith.eelkhair.net/',
  microserviceUrl: 'https://job-admin-api.eelkhair.net/',
  aiServiceUrl: 'https://job-ai-v2.eelkhair.net/',
  otel:'https://otel.eelkhair.net/v1/traces',
  otelAspire: undefined as string | undefined,
  otelZipkin: 'https://otel.eelkhair.net/api/v2/spans',
  grafanaUrl: 'https://grafana.eelkhair.net/d/bf5m5dwukfncwd/find-by-trace-id?orgId=1&var-TraceId=',
  jaegerUrl: 'https://jaeger.eelkhair.net/trace/',
  seqUrl: 'https://seq.eelkhair.net/#/events?filter=TraceId%3D%22{traceId}%22',
  oidc: {
    authority: 'https://auth.elkhair.tech/realms/job-board',
    redirectUrl: 'https://job-admin.eelkhair.net',
    clientId: 'angular-admin',
  },
};
