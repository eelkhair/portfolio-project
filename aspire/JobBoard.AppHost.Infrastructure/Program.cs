var builder = DistributedApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Infrastructure (persistent containers)
// ---------------------------------------------------------------------------

const string stack = "jobboard-aspire";

var sqlServer = builder.AddContainer("sqlserver", "mcr.microsoft.com/mssql/server", "2022-latest")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithEndpoint(11433, 1433, name: "tcp", isProxied: false)
    .WithEnvironment("ACCEPT_EULA", "Y")
    .WithEnvironment("MSSQL_SA_PASSWORD", "YourStrong!Passw0rd")
    .WithVolume("aspire-sqlserver-data", "/var/opt/mssql")
    .WithContainerRuntimeArgs("--label", $"com.docker.compose.project={stack}");

var postgres = builder.AddContainer("postgres", "ankane/pgvector", "latest")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithEndpoint(5432, 5432, name: "tcp", isProxied: false)
    .WithEnvironment("POSTGRES_USER", "postgres")
    .WithEnvironment("POSTGRES_PASSWORD", "postgres")
    .WithEnvironment("POSTGRES_DB", "AiEmbeddings")
    .WithVolume("postgres-data", "/var/lib/postgresql/data")
    .WithContainerRuntimeArgs("--label", $"com.docker.compose.project={stack}");

var redis = builder.AddContainer("redis", "redis", "8.2")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithEndpoint(6379, 6379, name: "tcp", isProxied: false)
    .WithContainerRuntimeArgs("--health-cmd", "redis-cli ping || exit 1")
    .WithContainerRuntimeArgs("--health-interval", "2s")
    .WithContainerRuntimeArgs("--health-retries", "5")
    .WithContainerRuntimeArgs("--label", $"com.docker.compose.project={stack}");

var redisSeed = builder.AddContainer("redis-seed", "redis", "8.2")
    .WithBindMount("../JobBoard.AppHost/RedisInit/seed.sh", "/seed.sh", isReadOnly: true)
    .WithEntrypoint("/bin/sh")
    .WithArgs("/seed.sh")
    .WaitFor(redis)
    .WithContainerRuntimeArgs("--label", $"com.docker.compose.project={stack}");

var redisCommander = builder.AddContainer("redis-commander", "rediscommander/redis-commander")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithHttpEndpoint(8081, 8081, name: "ui", isProxied: false)
    .WithEnvironment("REDIS_HOSTS", "local:host.docker.internal:6379")
    .WithContainerRuntimeArgs("--label", $"com.docker.compose.project={stack}");

var rabbitMq = builder.AddContainer("rabbitmq", "rabbitmq", "4.2-management")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithEndpoint(5672, 5672, name: "amqp", isProxied: false)
    .WithHttpEndpoint(15672, 15672, name: "management", isProxied: false)
    .WithEnvironment("RABBITMQ_DEFAULT_USER", "guest")
    .WithEnvironment("RABBITMQ_DEFAULT_PASS", "guest")
    .WithContainerRuntimeArgs("--label", $"com.docker.compose.project={stack}");

var keycloakPassword = builder.AddParameter("keycloak-password", "admin");
var keycloak = builder.AddKeycloak("keycloak", 9999, adminPassword: keycloakPassword)
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent)
    .WithRealmImport("../JobBoard.AppHost/KeycloakRealm")
    .WithContainerRuntimeArgs("--label", $"com.docker.compose.project={stack}");

var jaeger = builder.AddContainer("jaeger", "jaegertracing/all-in-one", "1.64.0")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithHttpEndpoint(16686, 16686, name: "ui", isProxied: false)
    .WithEnvironment("LOG_LEVEL", "debug")
    .WithContainerRuntimeArgs("--label", $"com.docker.compose.project={stack}");

var otelCollector = builder.AddContainer("otel-collector", "otel/opentelemetry-collector-contrib", "latest")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithEndpoint(4327, 4317, name: "otlp-grpc", isProxied: false)
    .WithEndpoint(4328, 4318, name: "otlp-http", isProxied: false)
    .WithEndpoint(8889, 8889, name: "prometheus", isProxied: false)
    .WithBindMount("../JobBoard.AppHost/OtelCollector/otel-collector-config.yaml", "/etc/otelcol-contrib/config.yaml", isReadOnly: true)
    .WaitFor(jaeger)
    .WithContainerRuntimeArgs("--label", $"com.docker.compose.project={stack}");

var mailpit = builder.AddContainer("mailpit", "axllent/mailpit")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithHttpEndpoint(8025, 8025, name: "ui", isProxied: false)
    .WithEndpoint(1025, 1025, name: "smtp", isProxied: false)
    .WithContainerRuntimeArgs("--label", $"com.docker.compose.project={stack}");

var elasticsearch = builder.AddContainer("elasticsearch", "docker.elastic.co/elasticsearch/elasticsearch", "8.17.0")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithEndpoint(9200, 9200, name: "http", isProxied: false)
    .WithEnvironment("discovery.type", "single-node")
    .WithEnvironment("xpack.security.enabled", "false")
    .WithEnvironment("ES_JAVA_OPTS", "-Xms512m -Xmx512m")
    .WithVolume("aspire-elasticsearch-data", "/usr/share/elasticsearch/data")
    .WithContainerRuntimeArgs("--label", $"com.docker.compose.project={stack}");

var azurite = builder.AddContainer("azurite", "mcr.microsoft.com/azure-storage/azurite", "latest")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithEndpoint(10000, 10000, name: "blob", isProxied: false)
    .WithEndpoint(10001, 10001, name: "queue", isProxied: false)
    .WithEndpoint(10002, 10002, name: "table", isProxied: false)
    .WithVolume("aspire-azurite-data", "/data")
    .WithContainerRuntimeArgs("--label", $"com.docker.compose.project={stack}");

var prometheus = builder.AddContainer("prometheus", "prom/prometheus", "latest")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithHttpEndpoint(9090, 9090, name: "ui", isProxied: false)
    .WithBindMount("../JobBoard.AppHost/PrometheusConfig/prometheus.yml", "/etc/prometheus/prometheus.yml", isReadOnly: true)
    .WithVolume("aspire-prometheus-data", "/prometheus")
    .WaitFor(otelCollector)
    .WithContainerRuntimeArgs("--label", $"com.docker.compose.project={stack}");

var grafana = builder.AddContainer("grafana", "grafana/grafana", "latest")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithHttpEndpoint(3200, 3000, name: "ui", isProxied: false)
    .WithEnvironment("GF_SECURITY_ADMIN_PASSWORD", "admin")
    .WithVolume("aspire-grafana-data", "/var/lib/grafana")
    .WithBindMount("../JobBoard.AppHost/GrafanaProvisioning/datasources", "/etc/grafana/provisioning/datasources", isReadOnly: true)
    .WithBindMount("../JobBoard.AppHost/GrafanaProvisioning/dashboards", "/etc/grafana/provisioning/dashboards", isReadOnly: true)
    .WithBindMount("../JobBoard.AppHost/GrafanaDashboards", "/var/lib/grafana/dashboards", isReadOnly: true)
    .WaitFor(prometheus)
    .WithContainerRuntimeArgs("--label", $"com.docker.compose.project={stack}");

// ---------------------------------------------------------------------------

builder.Build().Run();
