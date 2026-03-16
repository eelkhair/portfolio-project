# Plan: Add reverse-connector-api to Docker files

## Files to create

### 1. `services/reverse-connector-api/Nuget.Config`
- Copy from connector-api (references private feed `nuget.eelkhair.net` for `Elkhair.*` and `JobBoard.*` packages)

### 2. `services/reverse-connector-api/Dockerfile`
- Same 4-stage pattern as connector-api (aspnet:9.0 / sdk:9.0)
- Replace `connector-api` → `reverse-connector-api` in csproj/dll names
- Entrypoint: `dotnet reverse-connector-api.dll`

### 3. `services/reverse-connector-api/.dockerignore`
- Copy from connector-api (identical)

## Files to modify

### 4. `scripts/docker-compose.dev.yml`
- Add `reverse-connector-api` service (port `5285:8080`, image `registry.eelkhair.net/reverse-connector-api:latest`, `ASPNETCORE_ENVIRONMENT=Production`)
- Add `reverse-connector-api-sidecar` Dapr sidecar (app-id `reverse-connector-api`, volumes from `/home/eelkhair/Dapr/reverse-connector-api/`)
- Add health check entry (index 11): `Reverse Connector API` → `https://job-reverse-connector-dev.eelkhair.net/healthzEndpoint`

### 5. `scripts/docker-compose.prod.yml`
- Same service + sidecar entries as dev
- Health check URL: `https://job-reverse-connector.eelkhair.net/healthzEndpoint`
