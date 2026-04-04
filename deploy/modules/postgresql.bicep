@description('Azure region')
param location string

@description('Name of the PostgreSQL server')
param serverName string

@description('PostgreSQL admin username')
param adminLogin string = 'pgadmin'

@secure()
@description('PostgreSQL admin password')
param adminPassword string

resource pgServer 'Microsoft.DBforPostgreSQL/flexibleServers@2024-08-01' = {
  name: serverName
  location: location
  sku: {
    name: 'Standard_B1ms'
    tier: 'Burstable'
  }
  properties: {
    version: '16'
    administratorLogin: adminLogin
    administratorLoginPassword: adminPassword
    storage: {
      storageSizeGB: 32
    }
    backup: {
      backupRetentionDays: 7
      geoRedundantBackup: 'Disabled'
    }
    highAvailability: {
      mode: 'Disabled'
    }
  }
}

// Allow Azure services
resource firewallRule 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2024-08-01' = {
  parent: pgServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// Enable pgvector extension
resource pgvectorExtension 'Microsoft.DBforPostgreSQL/flexibleServers/configurations@2024-08-01' = {
  parent: pgServer
  name: 'azure.extensions'
  properties: {
    value: 'VECTOR'
    source: 'user-override'
  }
}

// AI Embeddings database
resource aiDb 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2024-08-01' = {
  parent: pgServer
  name: 'AiEmbeddings'
  properties: {
    charset: 'UTF8'
    collation: 'en_US.utf8'
  }
}

// Keycloak database
resource keycloakDb 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2024-08-01' = {
  parent: pgServer
  name: 'keycloak'
  properties: {
    charset: 'UTF8'
    collation: 'en_US.utf8'
  }
}

output pgServerFqdn string = pgServer.properties.fullyQualifiedDomainName
output pgServerName string = pgServer.name
output aiDbConnectionString string = 'Host=${pgServer.properties.fullyQualifiedDomainName};Port=5432;Database=AiEmbeddings;Username=${adminLogin};Password=${adminPassword};SSL Mode=Require;'
output keycloakDbConnectionString string = 'Host=${pgServer.properties.fullyQualifiedDomainName};Port=5432;Database=keycloak;Username=${adminLogin};Password=${adminPassword};SSL Mode=Require;'
