# ADR-013: Keycloak Migration — Auth Provider Strategy

- **Status:** Accepted
- **Date:** 2026-03-16

## Context

The platform originally used Auth0 for authentication and authorization. Auth0's organization feature provided multi-tenancy, and custom claims encoded roles and company associations. While Auth0 worked, several friction points emerged:

- **Cost and quotas**: Auth0's free tier limits were insufficient for development iteration (user count, organization count, machine-to-machine tokens).
- **Custom claim complexity**: Auth0 required Actions (serverless functions) to inject custom claims like `organization`, `roles`, and `company_uid` into tokens. These were fragile, hard to test, and added latency to token issuance.
- **Limited group hierarchy**: Auth0's organization model is flat — a user belongs to an organization with roles, but there's no nested group structure to represent company sub-roles (admin vs. recruiter).
- **Self-hosting for portfolio**: A portfolio project benefits from demonstrating infrastructure ownership. Running Keycloak shows operational capability that a managed SaaS does not.

Two alternatives were considered:
1. **Stay on Auth0**: Accept the cost and workaround complexity. Simpler operationally but limits portfolio demonstration.
2. **Migrate to Keycloak**: Self-hosted, hierarchical groups, full control over claim mapping and email templates. More operational burden but richer architecture story.

## Decision

### Keycloak as Identity Provider

All services were migrated from Auth0 to self-hosted Keycloak with three realm environments:

| Realm | Purpose |
|-------|---------|
| `job-board` | Production |
| `job-board-dev` | Dev/staging |
| `job-board-local` | Local development |

### Hierarchical Group-Based Authorization

Keycloak's group hierarchy replaced Auth0's flat organization model:

```
/Admins                                    → Platform administrators
/Companies/{companyUId}                    → Company root group
  /Companies/{companyUId}/CompanyAdmins    → Company admin users
  /Companies/{companyUId}/Recruiters       → Recruiter users
/Applicants                                → Job seekers
```

The **"Full group path"** mapper is enabled on Keycloak's groups claim mapper, emitting paths like `/Admins` and `/Companies/{uid}/CompanyAdmins` rather than flat group names. This enables:
- Extracting `companyUId` from the group path (e.g., `/Companies/abc-123/CompanyAdmins` → `abc-123`)
- Distinguishing same-named sub-groups across companies
- Hierarchical authorization checks

### Token Claim Normalization

Keycloak's full group paths include a leading `/` that `RequireClaim` policies don't expect. All services that validate group claims strip the leading `/` in `OnTokenValidated`:

```
/Admins → Admins
/Companies/{uid}/CompanyAdmins → Companies/{uid}/CompanyAdmins
```

The Angular frontend applies the same normalization via `g.replace(/^\//, '')`.

### Service-to-Service Authentication

Two patterns coexist:

1. **Dapr OAuth2 middleware**: All Dapr sidecars use a `oauth2clientcredentials` component that auto-injects `Authorization: Bearer {token}` on outbound service invocations. Client credentials are sourced from HashiCorp Vault via Dapr secret store.
2. **Internal API Key**: The monolith accepts `X-Api-Key` headers for internal callers (connector APIs, reverse connector) via a custom `InternalApiKeyAuthenticationHandler`. This avoids the overhead of token acquisition for trusted internal traffic.

### User Provisioning via Keycloak Admin REST API

The user-api service provisions Keycloak resources when a new company is created:

1. Create company group `/Companies/{uid}` under the `Companies` parent
2. Create `CompanyAdmins` sub-group
3. Create `Recruiters` sub-group
4. Create user with email, name, and `companyName` custom attribute
5. Add user to `CompanyAdmins` group
6. Send verification email (non-blocking, only for newly created users)

Token acquisition uses client credentials flow with a service account, cached in Redis (via Dapr state store) with TTL = `ExpiresIn - 120s` for safety margin.

### Authorization Policies

Each service defines policies based on the normalized `groups` claim:

| Service | Policy | Required Groups |
|---------|--------|-----------------|
| Monolith | `Admin` | `Admins` |
| Monolith | `Recruiter` | `Recruiters` |
| Monolith | `AllUsers` | `Admins` or `Recruiters` |
| Monolith | `InternalOrJwt` | JWT or API Key |
| AI Service v2 | `AdminChat` | `Admins` |
| AI Service v2 | `CompanyAdminChat` | `Admins` or `CompanyAdmins` |
| AI Service v2 | `PublicChat` | `Admins`, `CompanyAdmins`, or `Applicants` |
| AI Service v2 | `DaprInternal` | Loopback IP (Dapr sidecar) |
| Microservices | *(none)* | `AllowAnonymous()` — protected at Dapr sidecar level |

### Custom Email Theme

A `jobboard` Keycloak theme provides branded verification emails. The theme is mounted via Docker volume to `/opt/keycloak/themes/jobboard` and activated in Realm Settings. User attributes (e.g., `companyName`) are accessed via `${(user.attributes.companyName)!"fallback"}` — not `?first`, since Multivalued is off.

### Swagger Integration

All services configure Swagger with PKCE-based OAuth2 flow pointing to Keycloak's authorization and token endpoints. No client secret is required (public client). Scopes: `openid`, `profile`, `email`.

## Consequences

### Positive

- **Hierarchical multi-tenancy**: The group tree naturally models the company → role hierarchy without Auth0's flat organization workaround.
- **Self-hosted control**: Full ownership of user management, email templates, claim mappers, and realm configuration. No external vendor dependency for a portfolio project.
- **Simplified claims**: Group paths are the single source of authorization truth — no custom Auth0 Actions or opaque custom claims.
- **Cost elimination**: No per-user or per-token charges. Keycloak runs alongside the existing infrastructure.
- **Consistent cross-service auth**: The same JWT validation + group stripping pattern is applied identically across monolith, AI service, and microservices.

### Tradeoffs

- **Operational overhead**: Keycloak requires hosting, backup, and version management. For a portfolio project, this is acceptable and demonstrates operational capability.
- **Migration scope**: Every service, every Dapr component, and both Angular apps required auth configuration changes. The migration touched ~20 files across 8 services.
- **Group path stripping is a workaround**: The leading `/` in full group paths is a Keycloak quirk that every consumer must handle. A custom claim mapper could normalize this server-side, but the client-side strip is simpler and more visible.
- **Microservices rely on Dapr sidecar for auth**: The FastEndpoints microservices use `AllowAnonymous()` and trust that the Dapr sidecar's OAuth2 middleware validates tokens. This is a deliberate simplification but means the services themselves don't enforce auth if called directly.

## Implementation Notes

- **Keycloak server**: `https://auth.eelkhair.net` with realms per environment.
- **Client ID**: `angular-admin` (admin app), with separate environment files for local/dev/prod.
- **JWT claims used**: `given_name`, `family_name`, `email`, `groups` (not Auth0's custom namespace claims).
- **Vault secrets**: `Keycloak:Authority`, `Keycloak:Audience`, `Keycloak:SwaggerClientId`, `Keycloak:ServiceClientId`, `Keycloak:ServiceClientSecret`, `Keycloak:TokenUrl` — organized into `shared` (cross-env) and `shared-{env}` (env-specific) Vault paths.
- **EF Core migration**: user-api column renames (`Auth0OrganizationId` → `KeycloakGroupId`, `Auth0UserId` → `KeycloakUserId`) applied via migration.
- **Prerequisite**: Keycloak requires an `audience` mapper and a `groups` mapper (Full group path ON) configured in the client scope before tokens contain the expected claims.
