@description('Azure region')
param location string

@description('Container Apps Environment ID')
param environmentId string

@description('ACR login server')
param registryServer string

@description('Keycloak container image')
param containerImage string

@description('User-assigned managed identity resource ID')
param managedIdentityId string

@description('PostgreSQL connection URL for Keycloak')
param postgresUrl string

@description('PostgreSQL admin username')
param postgresUser string

@secure()
@description('PostgreSQL admin password')
param postgresPassword string

@secure()
@description('Keycloak admin password')
param keycloakAdminPassword string

resource keycloak 'Microsoft.App/containerApps@2024-03-01' = {
  name: 'keycloak'
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentityId}': {}
    }
  }
  properties: {
    managedEnvironmentId: environmentId
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: 8080
        transport: 'auto'
        allowInsecure: false
      }
      registries: [
        {
          server: registryServer
          identity: managedIdentityId
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'keycloak'
          image: containerImage
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          command: [ 'start' ]
          args: [
            '--hostname-strict=false'
            '--proxy-headers=xforwarded'
            '--http-enabled=true'
          ]
          env: [
            { name: 'KC_DB', value: 'postgres' }
            { name: 'KC_DB_URL', value: postgresUrl }
            { name: 'KC_DB_USERNAME', value: postgresUser }
            { name: 'KC_DB_PASSWORD', value: postgresPassword }
            { name: 'KEYCLOAK_ADMIN', value: 'admin' }
            { name: 'KEYCLOAK_ADMIN_PASSWORD', value: keycloakAdminPassword }
            { name: 'KC_HEALTH_ENABLED', value: 'true' }
            { name: 'KC_METRICS_ENABLED', value: 'true' }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 1
      }
    }
  }
}

output keycloakFqdn string = keycloak.properties.configuration.ingress.fqdn
output keycloakUrl string = 'https://${keycloak.properties.configuration.ingress.fqdn}'
