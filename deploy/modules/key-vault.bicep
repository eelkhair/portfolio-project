@description('Azure region')
param location string

@description('Name of the Key Vault')
param vaultName string

@description('Tenant ID for RBAC')
param tenantId string

resource vault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: vaultName
  location: location
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: tenantId
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
    enablePurgeProtection: false // allow easy teardown for portfolio
  }
}

output vaultUri string = vault.properties.vaultUri
output vaultName string = vault.name
output vaultId string = vault.id
