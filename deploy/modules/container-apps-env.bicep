@description('Azure region')
param location string

@description('Name of the Container Apps Environment')
param environmentName string

@description('Log Analytics workspace customer ID')
param logAnalyticsCustomerId string

@secure()
@description('Log Analytics workspace shared key')
param logAnalyticsSharedKey string

@description('Redis connection host:port for Dapr state store')
param redisHost string

@secure()
@description('Redis password for Dapr state store')
param redisPassword string

@description('RabbitMQ host for Dapr pub/sub (internal Container App FQDN)')
param rabbitmqHost string

@description('Key Vault name for Dapr secret store')
param keyVaultName string

@description('Managed identity client ID for Key Vault access')
param managedIdentityClientId string

@description('Azure App Configuration connection string for Dapr config stores')
param appConfigConnectionString string

// All Dapr-enabled service app IDs
var daprServiceScopes = [
  'ai-service-v2'
  'admin-api'
  'admin-api-mcp'
  'company-api'
  'job-api'
  'user-api'
  'connector-api'
  'reverse-connector-api'
]

// All services including non-Dapr ones that use config store
var allServiceScopes = union(daprServiceScopes, [
  'gateway'
  'monolith-api'
  'monolith-mcp'
])

// Per-service config store definitions
var perServiceConfigs = [
  { name: 'appconfig-admin-api', scope: 'admin-api' }
  { name: 'appconfig-admin-api-mcp', scope: 'admin-api-mcp' }
  { name: 'appconfig-company-api', scope: 'company-api' }
  { name: 'appconfig-job-api', scope: 'job-api' }
  { name: 'appconfig-user-api', scope: 'user-api' }
  { name: 'appconfig-connector-api', scope: 'connector-api' }
  { name: 'appconfig-reverse-connector-api', scope: 'reverse-connector-api' }
  { name: 'appconfig-ai-service-v2', scope: 'ai-service-v2' }
  { name: 'appconfig-gateway', scope: 'gateway' }
  { name: 'appconfig-monolith-api', scope: 'monolith-api' }
  { name: 'appconfig-monolith-mcp', scope: 'monolith-mcp' }
]

// ── Container Apps Environment ──
resource environment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: environmentName
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalyticsCustomerId
        sharedKey: logAnalyticsSharedKey
      }
    }
    zoneRedundant: false
  }
}

// ══════════════════════════════════════════════════════════════════════════════
// Dapr Components
// ══════════════════════════════════════════════════════════════════════════════

// NOTE: Code hardcodes "rabbitmq.pubsub" and "statestore.redis" (with dots).
// ACA Dapr component names may not support dots — if deployment fails,
// update these constants in the codebase:
//   - services/micro-services/Elkhair.Dev.Common/Domain/Constants/PubSubNames.cs
//   - services/micro-services/Elkhair.Dev.Common/Domain/Constants/StateStores.cs
//   - All [Topic("rabbitmq.pubsub", ...)] attributes across services

// 1. Pub/Sub — RabbitMQ
resource daprPubSub 'Microsoft.App/managedEnvironments/daprComponents@2024-03-01' = {
  parent: environment
  name: 'rabbitmq.pubsub'
  properties: {
    componentType: 'pubsub.rabbitmq'
    version: 'v1'
    metadata: [
      { name: 'connectionString', value: 'amqp://guest:guest@${rabbitmqHost}:5672' }
      { name: 'exchangeKind', value: 'fanout' }
      { name: 'deletedWhenUnused', value: 'false' }
      { name: 'autoAck', value: 'false' }
      { name: 'reconnectWait', value: '5s' }
      { name: 'maxRetries', value: '10' }
      { name: 'enableDeadLetter', value: 'true' }
    ]
    scopes: daprServiceScopes
  }
}

// 2. State Store — Azure Redis
resource daprStateStore 'Microsoft.App/managedEnvironments/daprComponents@2024-03-01' = {
  parent: environment
  name: 'statestore.redis'
  properties: {
    componentType: 'state.redis'
    version: 'v1'
    metadata: [
      { name: 'redisHost', value: redisHost }
      { name: 'redisPassword', value: redisPassword }
      { name: 'enableTLS', value: 'true' }
      { name: 'actorStateStore', value: 'true' }
    ]
    scopes: daprServiceScopes
  }
}

// 3. Secret Store — Azure Key Vault
resource daprSecretStore 'Microsoft.App/managedEnvironments/daprComponents@2024-03-01' = {
  parent: environment
  name: 'keyvault-secrets'
  properties: {
    componentType: 'secretstores.azure.keyvault'
    version: 'v1'
    metadata: [
      { name: 'vaultName', value: keyVaultName }
      { name: 'azureClientId', value: managedIdentityClientId }
    ]
    scopes: daprServiceScopes
  }
}

// 4. Global Configuration Store — Azure App Configuration
// Replaces Redis DB 1 config stores. Dapr Configuration API abstracts the backing store,
// so no code changes needed. Keys in App Config use the same names as Redis keys.
// Proxmox continues using Redis config stores via Aspire Dapr components.
resource daprConfigStoreGlobal 'Microsoft.App/managedEnvironments/daprComponents@2024-03-01' = {
  parent: environment
  name: 'appconfig-global'
  properties: {
    componentType: 'configuration.azure.appconfig'
    version: 'v1'
    metadata: [
      { name: 'connectionString', value: appConfigConnectionString }
      { name: 'label', value: 'global' }
    ]
    scopes: allServiceScopes
  }
}

// 5. Per-Service Configuration Stores — Azure App Configuration
// Each service reads from appconfig-{serviceName} at startup via Dapr.
// Uses labels to separate per-service config within a single App Config instance.
resource daprPerServiceConfigs 'Microsoft.App/managedEnvironments/daprComponents@2024-03-01' = [for cfg in perServiceConfigs: {
  parent: environment
  name: cfg.name
  properties: {
    componentType: 'configuration.azure.appconfig'
    version: 'v1'
    metadata: [
      { name: 'connectionString', value: appConfigConnectionString }
      { name: 'label', value: cfg.scope }
    ]
    scopes: [
      cfg.scope
    ]
  }
}]

// 6. Cron Binding — Outbox Processor (monolith)
resource daprOutboxCron 'Microsoft.App/managedEnvironments/daprComponents@2024-03-01' = {
  parent: environment
  name: 'process-outbox-messages'
  properties: {
    componentType: 'bindings.cron'
    version: 'v1'
    metadata: [
      { name: 'schedule', value: '@every 10s' }
      { name: 'route', value: '/process-outbox-messages' }
    ]
    scopes: [
      'monolith-api'
    ]
  }
}

// 7. Cron Binding — Auth Token Refresh (user-api)
resource daprAuthTokenCron 'Microsoft.App/managedEnvironments/daprComponents@2024-03-01' = {
  parent: environment
  name: 'refresh-auth-token'
  properties: {
    componentType: 'bindings.cron'
    version: 'v1'
    metadata: [
      { name: 'schedule', value: '@every 23h' }
      { name: 'route', value: '/refresh-auth-token' }
    ]
    scopes: [
      'user-api'
    ]
  }
}

output environmentId string = environment.id
output environmentName string = environment.name
output defaultDomain string = environment.properties.defaultDomain
