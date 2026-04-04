using './main.bicep'

param location = 'centralus'
param prefix = 'jb'
param acrName = 'acrjobboardportfolio'
param keyVaultName = 'kv-jb-portfolio'
param storageAccountName = 'stjbportfolio'
param sqlAdminPassword = readEnvironmentVariable('SQL_ADMIN_PASSWORD')
param postgresAdminPassword = readEnvironmentVariable('POSTGRES_ADMIN_PASSWORD')
param keycloakAdminPassword = readEnvironmentVariable('KEYCLOAK_ADMIN_PASSWORD')
