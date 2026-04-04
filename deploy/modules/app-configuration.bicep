@description('Azure region')
param location string

@description('Name of the App Configuration store')
param appConfigName string

@description('Managed identity principal ID for Data Reader role')
param managedIdentityPrincipalId string

resource appConfig 'Microsoft.AppConfiguration/configurationStores@2023-03-01' = {
  name: appConfigName
  location: location
  sku: {
    name: 'free'
  }
  properties: {
    disableLocalAuth: false
  }
}

// App Configuration Data Reader role: 516239f1-63e1-4d78-a4de-a74fb236a071
resource dataReaderRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(appConfig.id, managedIdentityPrincipalId, '516239f1-63e1-4d78-a4de-a74fb236a071')
  scope: appConfig
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '516239f1-63e1-4d78-a4de-a74fb236a071')
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

output appConfigEndpoint string = appConfig.properties.endpoint
output appConfigName string = appConfig.name
output appConfigId string = appConfig.id
output appConfigConnectionString string = appConfig.listKeys().value[0].connectionString
