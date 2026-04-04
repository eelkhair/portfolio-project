@description('Name of the Container App')
param name string

@description('Azure region')
param location string

@description('Container Apps Environment ID')
param environmentId string

@description('Container image (e.g., myacr.azurecr.io/myapp:latest)')
param containerImage string

@description('Target port the container listens on')
param targetPort int = 8080

@description('CPU cores (e.g., 0.25, 0.5)')
param cpu string = '0.25'

@description('Memory (e.g., 0.5Gi, 1Gi)')
param memory string = '0.5Gi'

@description('Minimum number of replicas')
param minReplicas int = 0

@description('Maximum number of replicas')
param maxReplicas int = 1

@description('Enable Dapr sidecar')
param daprEnabled bool = false

@description('Dapr app ID')
param daprAppId string = ''

@description('Dapr app port')
param daprAppPort int = 0

@description('External ingress (public) or internal only')
param externalIngress bool = false

@description('Environment variables')
param envVars array = []

@description('ACR login server')
param registryServer string

@description('User-assigned managed identity resource ID')
param managedIdentityId string

resource app 'Microsoft.App/containerApps@2024-03-01' = {
  name: name
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
        external: externalIngress
        targetPort: targetPort
        transport: 'auto'
        allowInsecure: false
      }
      dapr: daprEnabled ? {
        enabled: true
        appId: daprAppId
        appPort: daprAppPort
        appProtocol: 'http'
      } : {
        enabled: false
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
          name: name
          image: containerImage
          resources: {
            cpu: json(cpu)
            memory: memory
          }
          env: envVars
        }
      ]
      scale: {
        minReplicas: minReplicas
        maxReplicas: maxReplicas
      }
    }
  }
}

output appFqdn string = app.properties.configuration.ingress.fqdn
output appName string = app.name
output appId string = app.id
