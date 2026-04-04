@description('Azure region')
param location string

@description('Name of the Redis cache')
param redisName string

resource redis 'Microsoft.Cache/redis@2024-03-01' = {
  name: redisName
  location: location
  properties: {
    sku: {
      name: 'Basic'
      family: 'C'
      capacity: 0
    }
    enableNonSslPort: false
    minimumTlsVersion: '1.2'
    redisConfiguration: {
      'maxmemory-policy': 'allkeys-lru'
    }
  }
}

output redisHostName string = redis.properties.hostName
output redisPort int = redis.properties.sslPort
output redisId string = redis.id
output redisConnectionString string = '${redis.properties.hostName}:${redis.properties.sslPort},password=${redis.listKeys().primaryKey},ssl=True,abortConnect=False'
output redisPrimaryKey string = redis.listKeys().primaryKey
