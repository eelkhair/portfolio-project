export const environment = {
  production: true,
  envName: 'PROD',
  apiUrl: 'https://job-gateway.eelkhair.net/api/',
  aiUrl: 'https://job-ai-v2.eelkhair.net/',
  monolithUrl: 'https://job-monolith.eelkhair.net/',
  otel: 'https://otel.eelkhair.net/v1/traces',
  otelZipkin: 'https://otel.eelkhair.net/api/v2/spans',
  jaegerUrl: 'https://jaeger.eelkhair.net/trace/',
  seqUrl: 'https://seq.eelkhair.net/#/events?filter=TraceId%3D%22{traceId}%22',
  grafanaUrl: 'https://grafana.eelkhair.net/d/bf5m5dwukfncwd/find-by-trace-id?orgId=1&var-TraceId=',
  oidc: {
    authority: 'https://auth.elkhair.tech/realms/job-board',
    redirectUrl: 'https://jobs.eelkhair.net',
    clientId: 'angular-public',
  },
};
