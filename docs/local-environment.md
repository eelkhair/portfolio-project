## Local Development Environment

This project includes a deliberately designed **local development environment**
that prioritizes developer experience, portability, and architectural clarity.

The local setup allows the entire system to run without cloud dependencies while
preserving the same architectural patterns used in production.

### Local Environment Goals

- Fast local iteration
- Full-system observability
- Infrastructure replaceability
- Clear mapping to Azure production services

### Local vs Azure Mapping

| Capability | Local Environment | Azure Target |
|----------|------------------|-------------|
| Configuration | Dapr + Redis | Azure App Configuration |
| Secrets | Dapr + Vault | Azure Key Vault |
| Messaging | RabbitMQ | Azure Service Bus |
| Eventing | Dapr Pub/Sub | Azure Event Grid |
| Real-time Updates | Dapr Pub/Sub | Azure SignalR |
| Hosting | Docker / Docker Compose | App Service / Container Apps |

### Important Notes

- Redis and Vault are used **only for local development**.
- Dapr is used to simplify local integration and experimentation.
- All infrastructure dependencies are abstracted behind application interfaces
  to ensure clean replacement in Azure deployments.
