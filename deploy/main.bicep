// ══════════════════════════════════════════════════════════════════════════════
// JobBoard Portfolio — Azure Deployment
// Deploy: az deployment group create -g rg-portfolio-jobboard -f main.bicep -p main.bicepparam
// Teardown: az group delete -n rg-portfolio-jobboard --yes --no-wait
// ══════════════════════════════════════════════════════════════════════════════

targetScope = 'resourceGroup'

// ── Parameters ──

@description('Azure region for all resources')
param location string = resourceGroup().location

@description('Unique prefix for globally-unique resource names')
param prefix string = 'jb'

@description('ACR name (globally unique, alphanumeric only)')
param acrName string

@description('Key Vault name (globally unique)')
param keyVaultName string

@description('Storage account name (globally unique, lowercase, no hyphens)')
param storageAccountName string

@secure()
@description('SQL Server admin password')
param sqlAdminPassword string

@secure()
@description('PostgreSQL admin password')
param postgresAdminPassword string

@secure()
@description('Keycloak admin password')
param keycloakAdminPassword string

@description('Container image tag (defaults to latest)')
param imageTag string = 'latest'

// ── Variables ──

var appConfigName = '${prefix}-appconfig'
var sqlServerName = '${prefix}-sql'
var postgresServerName = '${prefix}-pg'
var redisName = '${prefix}-redis-portfolio'
var logAnalyticsName = '${prefix}-logs'
var environmentName = '${prefix}-env'
var identityName = '${prefix}-identity'

// ══════════════════════════════════════════════════════════════════════════════
// Phase 1: Foundation (parallel — no dependencies)
// ══════════════════════════════════════════════════════════════════════════════

module logAnalytics 'modules/log-analytics.bicep' = {
  name: 'log-analytics'
  params: {
    location: location
    workspaceName: logAnalyticsName
  }
}

module keyVault 'modules/key-vault.bicep' = {
  name: 'key-vault'
  params: {
    location: location
    vaultName: keyVaultName
    tenantId: subscription().tenantId
  }
}

module acr 'modules/container-registry.bicep' = {
  name: 'container-registry'
  params: {
    location: location
    acrName: acrName
  }
}

module storage 'modules/storage-account.bicep' = {
  name: 'storage-account'
  params: {
    location: location
    storageAccountName: storageAccountName
  }
}

// ══════════════════════════════════════════════════════════════════════════════
// Phase 2: Data (depends on Key Vault for storing connection strings)
// ══════════════════════════════════════════════════════════════════════════════

module sqlServer 'modules/sql-server.bicep' = {
  name: 'sql-server'
  params: {
    location: location
    serverName: sqlServerName
    adminPassword: sqlAdminPassword
  }
}

module postgresql 'modules/postgresql.bicep' = {
  name: 'postgresql'
  params: {
    location: location
    serverName: postgresServerName
    adminPassword: postgresAdminPassword
  }
}

module redis 'modules/redis.bicep' = {
  name: 'redis'
  params: {
    location: location
    redisName: redisName
  }
}

module appConfig 'modules/app-configuration.bicep' = {
  name: 'app-configuration'
  params: {
    location: location
    appConfigName: appConfigName
    managedIdentityPrincipalId: identity.outputs.identityPrincipalId
  }
}

// ══════════════════════════════════════════════════════════════════════════════
// Phase 2.5: Managed Identity + RBAC (depends on Phase 1 resources)
// ══════════════════════════════════════════════════════════════════════════════

module identity 'modules/managed-identity.bicep' = {
  name: 'managed-identity'
  params: {
    location: location
    identityName: identityName
    acrId: acr.outputs.acrId
    keyVaultId: keyVault.outputs.vaultId
    storageAccountId: storage.outputs.storageAccountId
  }
}

// ══════════════════════════════════════════════════════════════════════════════
// Phase 3: Container Apps Environment + Dapr Components
// ══════════════════════════════════════════════════════════════════════════════

module containerEnv 'modules/container-apps-env.bicep' = {
  name: 'container-apps-env'
  params: {
    location: location
    environmentName: environmentName
    logAnalyticsCustomerId: logAnalytics.outputs.workspaceCustomerId
    logAnalyticsSharedKey: logAnalytics.outputs.workspaceSharedKey
    redisHost: '${redis.outputs.redisHostName}:${redis.outputs.redisPort}'
    redisPassword: redis.outputs.redisPrimaryKey
    rabbitmqHost: 'rabbitmq' // Internal ACA FQDN — resolved after RabbitMQ app deploys
    keyVaultName: keyVault.outputs.vaultName
    managedIdentityClientId: identity.outputs.identityClientId
    appConfigConnectionString: appConfig.outputs.appConfigConnectionString
  }
}

// ══════════════════════════════════════════════════════════════════════════════
// Phase 4: RabbitMQ + Keycloak (infrastructure containers, deploy first)
// ══════════════════════════════════════════════════════════════════════════════

module rabbitmq 'modules/container-app.bicep' = {
  name: 'rabbitmq'
  params: {
    name: 'rabbitmq'
    location: location
    environmentId: containerEnv.outputs.environmentId
    containerImage: 'rabbitmq:4.2-management'
    targetPort: 5672
    cpu: '0.5'
    memory: '1Gi'
    minReplicas: 1
    maxReplicas: 1
    registryServer: acr.outputs.acrLoginServer
    managedIdentityId: identity.outputs.identityId
    externalIngress: false
    envVars: [
      { name: 'RABBITMQ_DEFAULT_USER', value: 'guest' }
      { name: 'RABBITMQ_DEFAULT_PASS', value: 'guest' }
    ]
  }
}

module keycloak 'modules/keycloak.bicep' = {
  name: 'keycloak'
  params: {
    location: location
    environmentId: containerEnv.outputs.environmentId
    registryServer: acr.outputs.acrLoginServer
    containerImage: '${acr.outputs.acrLoginServer}/keycloak:${imageTag}'
    managedIdentityId: identity.outputs.identityId
    postgresUrl: 'jdbc:postgresql://${postgresql.outputs.pgServerFqdn}:5432/keycloak'
    postgresUser: 'pgadmin'
    postgresPassword: postgresAdminPassword
    keycloakAdminPassword: keycloakAdminPassword
  }
}

// ══════════════════════════════════════════════════════════════════════════════
// Phase 5: Backend Services
// ══════════════════════════════════════════════════════════════════════════════

// Common env vars for all backend services
var commonBackendEnvVars = [
  { name: 'ASPNETCORE_ENVIRONMENT', value: 'Production' }
  { name: 'ASPIRE_MODE', value: 'true' }
]

var keycloakEnvVars = [
  { name: 'Keycloak__Authority', value: 'https://${keycloak.outputs.keycloakFqdn}/realms/job-board' }
  { name: 'Keycloak__Audience', value: 'jobboard-api' }
]

// Gateway (external-facing, routes to all services)
module gateway 'modules/container-app.bicep' = {
  name: 'gateway'
  params: {
    name: 'gateway'
    location: location
    environmentId: containerEnv.outputs.environmentId
    containerImage: '${acr.outputs.acrLoginServer}/gateway:${imageTag}'
    targetPort: 8080
    cpu: '0.5'
    memory: '1Gi'
    minReplicas: 0
    maxReplicas: 2
    externalIngress: true
    registryServer: acr.outputs.acrLoginServer
    managedIdentityId: identity.outputs.identityId
    envVars: union(commonBackendEnvVars, keycloakEnvVars, [
      { name: 'AiServiceUrl', value: 'http://ai-service-v2' }
      { name: 'AdminApiUrl', value: 'http://admin-api' }
      { name: 'MonolithUrl', value: 'http://monolith-api' }
      { name: 'ConnectionStrings__Redis', value: redis.outputs.redisConnectionString }
    ])
  }
}

// Monolith API
module monolithApi 'modules/container-app.bicep' = {
  name: 'monolith-api'
  params: {
    name: 'monolith-api'
    location: location
    environmentId: containerEnv.outputs.environmentId
    containerImage: '${acr.outputs.acrLoginServer}/monolith-api:${imageTag}'
    targetPort: 8080
    cpu: '0.5'
    memory: '1Gi'
    minReplicas: 1  // Dapr outbox cron needs running instance
    maxReplicas: 2
    daprEnabled: true
    daprAppId: 'monolith-api'
    daprAppPort: 8080
    externalIngress: false
    registryServer: acr.outputs.acrLoginServer
    managedIdentityId: identity.outputs.identityId
    envVars: union(commonBackendEnvVars, keycloakEnvVars, [
      { name: 'ConnectionStrings__Monolith', value: sqlServer.outputs.monolithConnectionString }
      { name: 'ConnectionStrings__BlobStorage', value: storage.outputs.storageConnectionString }
      { name: 'ConnectionStrings__Redis', value: redis.outputs.redisConnectionString }
      { name: 'RabbitMQ__Host', value: 'amqp://guest:guest@rabbitmq:5672' }
    ])
  }
}

// Monolith MCP
module monolithMcp 'modules/container-app.bicep' = {
  name: 'monolith-mcp'
  params: {
    name: 'monolith-mcp'
    location: location
    environmentId: containerEnv.outputs.environmentId
    containerImage: '${acr.outputs.acrLoginServer}/monolith-mcp:${imageTag}'
    targetPort: 3333
    cpu: '0.25'
    memory: '0.5Gi'
    externalIngress: false
    registryServer: acr.outputs.acrLoginServer
    managedIdentityId: identity.outputs.identityId
    envVars: union(commonBackendEnvVars, keycloakEnvVars, [
      { name: 'ConnectionStrings__Monolith', value: sqlServer.outputs.monolithConnectionString }
    ])
  }
}

// AI Service v2
module aiService 'modules/container-app.bicep' = {
  name: 'ai-service-v2'
  params: {
    name: 'ai-service-v2'
    location: location
    environmentId: containerEnv.outputs.environmentId
    containerImage: '${acr.outputs.acrLoginServer}/ai-service-v2:${imageTag}'
    targetPort: 8080
    cpu: '0.5'
    memory: '1Gi'
    minReplicas: 0
    maxReplicas: 2
    daprEnabled: true
    daprAppId: 'ai-service-v2'
    daprAppPort: 8080
    externalIngress: false
    registryServer: acr.outputs.acrLoginServer
    managedIdentityId: identity.outputs.identityId
    envVars: union(commonBackendEnvVars, keycloakEnvVars, [
      { name: 'ConnectionStrings__PostgreSQL', value: postgresql.outputs.aiDbConnectionString }
      { name: 'ConnectionStrings__SqlServer', value: sqlServer.outputs.monolithConnectionString }
      { name: 'ConnectionStrings__Redis', value: redis.outputs.redisConnectionString }
      { name: 'ConnectionStrings__BlobStorage', value: storage.outputs.storageConnectionString }
      { name: 'McpServer__IntegrationUrl', value: 'http://monolith-mcp:3333' }
      { name: 'McpServer__MicroUrl', value: 'http://admin-api-mcp:3334' }
    ])
  }
}

// Admin API
module adminApi 'modules/container-app.bicep' = {
  name: 'admin-api'
  params: {
    name: 'admin-api'
    location: location
    environmentId: containerEnv.outputs.environmentId
    containerImage: '${acr.outputs.acrLoginServer}/admin-api:${imageTag}'
    targetPort: 8080
    cpu: '0.25'
    memory: '0.5Gi'
    daprEnabled: true
    daprAppId: 'admin-api'
    daprAppPort: 8080
    externalIngress: false
    registryServer: acr.outputs.acrLoginServer
    managedIdentityId: identity.outputs.identityId
    envVars: union(commonBackendEnvVars, keycloakEnvVars, [
      { name: 'ConnectionStrings__AdminDbContext', value: sqlServer.outputs.microConnectionString }
    ])
  }
}

// Admin API MCP
module adminApiMcp 'modules/container-app.bicep' = {
  name: 'admin-api-mcp'
  params: {
    name: 'admin-api-mcp'
    location: location
    environmentId: containerEnv.outputs.environmentId
    containerImage: '${acr.outputs.acrLoginServer}/admin-api-mcp:${imageTag}'
    targetPort: 3334
    cpu: '0.25'
    memory: '0.5Gi'
    daprEnabled: true
    daprAppId: 'admin-api-mcp'
    daprAppPort: 3334
    externalIngress: false
    registryServer: acr.outputs.acrLoginServer
    managedIdentityId: identity.outputs.identityId
    envVars: union(commonBackendEnvVars, keycloakEnvVars, [
      { name: 'ConnectionStrings__AdminDbContext', value: sqlServer.outputs.microConnectionString }
    ])
  }
}

// Company API
module companyApi 'modules/container-app.bicep' = {
  name: 'company-api'
  params: {
    name: 'company-api'
    location: location
    environmentId: containerEnv.outputs.environmentId
    containerImage: '${acr.outputs.acrLoginServer}/company-api:${imageTag}'
    targetPort: 8080
    cpu: '0.25'
    memory: '0.5Gi'
    daprEnabled: true
    daprAppId: 'company-api'
    daprAppPort: 8080
    externalIngress: false
    registryServer: acr.outputs.acrLoginServer
    managedIdentityId: identity.outputs.identityId
    envVars: union(commonBackendEnvVars, keycloakEnvVars, [
      { name: 'ConnectionStrings__CompanyDbContext', value: sqlServer.outputs.microConnectionString }
    ])
  }
}

// Job API
module jobApi 'modules/container-app.bicep' = {
  name: 'job-api'
  params: {
    name: 'job-api'
    location: location
    environmentId: containerEnv.outputs.environmentId
    containerImage: '${acr.outputs.acrLoginServer}/job-api:${imageTag}'
    targetPort: 8080
    cpu: '0.25'
    memory: '0.5Gi'
    daprEnabled: true
    daprAppId: 'job-api'
    daprAppPort: 8080
    externalIngress: false
    registryServer: acr.outputs.acrLoginServer
    managedIdentityId: identity.outputs.identityId
    envVars: union(commonBackendEnvVars, keycloakEnvVars, [
      { name: 'ConnectionStrings__JobDbContext', value: sqlServer.outputs.microConnectionString }
    ])
  }
}

// User API
module userApi 'modules/container-app.bicep' = {
  name: 'user-api'
  params: {
    name: 'user-api'
    location: location
    environmentId: containerEnv.outputs.environmentId
    containerImage: '${acr.outputs.acrLoginServer}/user-api:${imageTag}'
    targetPort: 8080
    cpu: '0.25'
    memory: '0.5Gi'
    minReplicas: 1  // Dapr auth token cron needs running instance
    maxReplicas: 1
    daprEnabled: true
    daprAppId: 'user-api'
    daprAppPort: 8080
    externalIngress: false
    registryServer: acr.outputs.acrLoginServer
    managedIdentityId: identity.outputs.identityId
    envVars: union(commonBackendEnvVars, keycloakEnvVars, [
      { name: 'ConnectionStrings__UserDbContext', value: sqlServer.outputs.microConnectionString }
      { name: 'Keycloak__ServiceClientId', value: 'dapr-service-client' }
      { name: 'Keycloak__TokenUrl', value: 'https://${keycloak.outputs.keycloakFqdn}/realms/job-board/protocol/openid-connect/token' }
    ])
  }
}

// Connector API
module connectorApi 'modules/container-app.bicep' = {
  name: 'connector-api'
  params: {
    name: 'connector-api'
    location: location
    environmentId: containerEnv.outputs.environmentId
    containerImage: '${acr.outputs.acrLoginServer}/connector-api:${imageTag}'
    targetPort: 8080
    cpu: '0.25'
    memory: '0.5Gi'
    daprEnabled: true
    daprAppId: 'connector-api'
    daprAppPort: 8080
    externalIngress: false
    registryServer: acr.outputs.acrLoginServer
    managedIdentityId: identity.outputs.identityId
    envVars: union(commonBackendEnvVars, keycloakEnvVars, [])
  }
}

// Reverse Connector API
module reverseConnectorApi 'modules/container-app.bicep' = {
  name: 'reverse-connector-api'
  params: {
    name: 'reverse-connector-api'
    location: location
    environmentId: containerEnv.outputs.environmentId
    containerImage: '${acr.outputs.acrLoginServer}/reverse-connector-api:${imageTag}'
    targetPort: 8080
    cpu: '0.25'
    memory: '0.5Gi'
    daprEnabled: true
    daprAppId: 'reverse-connector-api'
    daprAppPort: 8080
    externalIngress: false
    registryServer: acr.outputs.acrLoginServer
    managedIdentityId: identity.outputs.identityId
    envVars: union(commonBackendEnvVars, keycloakEnvVars, [])
  }
}

// ══════════════════════════════════════════════════════════════════════════════
// Phase 6: Frontend Apps
// ══════════════════════════════════════════════════════════════════════════════

module jobAdmin 'modules/container-app.bicep' = {
  name: 'job-admin'
  params: {
    name: 'job-admin'
    location: location
    environmentId: containerEnv.outputs.environmentId
    containerImage: '${acr.outputs.acrLoginServer}/job-admin:${imageTag}'
    targetPort: 80
    cpu: '0.25'
    memory: '0.5Gi'
    externalIngress: true
    registryServer: acr.outputs.acrLoginServer
    managedIdentityId: identity.outputs.identityId
  }
}

module jobPublic 'modules/container-app.bicep' = {
  name: 'job-public'
  params: {
    name: 'job-public'
    location: location
    environmentId: containerEnv.outputs.environmentId
    containerImage: '${acr.outputs.acrLoginServer}/job-public:${imageTag}'
    targetPort: 3000
    cpu: '0.25'
    memory: '0.5Gi'
    externalIngress: true
    registryServer: acr.outputs.acrLoginServer
    managedIdentityId: identity.outputs.identityId
  }
}

// Health Check Dashboard
module healthCheck 'modules/container-app.bicep' = {
  name: 'health-check'
  params: {
    name: 'health-check'
    location: location
    environmentId: containerEnv.outputs.environmentId
    containerImage: '${acr.outputs.acrLoginServer}/health-check:${imageTag}'
    targetPort: 8080
    cpu: '0.25'
    memory: '0.5Gi'
    externalIngress: true
    registryServer: acr.outputs.acrLoginServer
    managedIdentityId: identity.outputs.identityId
    envVars: commonBackendEnvVars
  }
}

// ══════════════════════════════════════════════════════════════════════════════
// Outputs
// ══════════════════════════════════════════════════════════════════════════════

output gatewayUrl string = 'https://${gateway.outputs.appFqdn}'
output keycloakUrl string = keycloak.outputs.keycloakUrl
output jobAdminUrl string = 'https://${jobAdmin.outputs.appFqdn}'
output jobPublicUrl string = 'https://${jobPublic.outputs.appFqdn}'
output healthCheckUrl string = 'https://${healthCheck.outputs.appFqdn}'
output acrLoginServer string = acr.outputs.acrLoginServer
output sqlServerFqdn string = sqlServer.outputs.sqlServerFqdn
output postgresServerFqdn string = postgresql.outputs.pgServerFqdn
output appConfigEndpoint string = appConfig.outputs.appConfigEndpoint
