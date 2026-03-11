# Plan: Replace Auth0 with Keycloak in Angular Admin App

## Keycloak Details
- **Server**: `https://auth.eelkhair.net`
- **Realm**: `job-board-dev`
- **Client ID**: `angular-admin`
- **Flow**: Authorization Code (Standard flow, public client)
- **Library**: `angular-auth-oidc-client`

## Role Name Mapping (Auth0 → Keycloak)
| Auth0 role | Keycloak realm role | Used in |
|---|---|---|
| `admin` | `platform-admin` | menuItems, roleGuard, route data |
| `company-admin` | `company-admin` | (same, no change needed) |
| `applicant` | `candidate` | (not used in admin app currently) |

Keycloak roles live in `realm_access.roles` array in the access token.

## Scope
Frontend only (`apps/job-admin`). Backend `.NET` services remain on Auth0 for now — they'll be migrated when other Keycloak clients are ready.

---

## Steps

### 1. Install `angular-auth-oidc-client`, remove `@auth0/auth0-angular`
- `npm uninstall @auth0/auth0-angular`
- `npm install angular-auth-oidc-client`

### 2. Add Keycloak OIDC config to environment files
Add `oidc` config block to both `environment.ts` and `environment.development.ts`:
```typescript
oidc: {
  authority: 'https://auth.eelkhair.net/realms/job-board-dev',
  redirectUrl: 'https://job-admin-dev.eelkhair.net',  // or http://localhost:6080 for dev
  clientId: 'angular-admin',
}
```

### 3. Replace `provideAuth0()` in `app.config.ts`
Replace with `provideAuth()` from `angular-auth-oidc-client`:
- Authority: from environment
- Client ID: `angular-admin`
- Scope: `openid profile email offline_access roles`
- Response type: `code`
- Silent renew via refresh tokens
- `secureRoutes`: all API base URLs from environment (gateway, monolith, microservice, aiService)
- Replace custom `authInterceptor` with the library's built-in `authInterceptor` from `angular-auth-oidc-client`

### 4. Rewrite `AccountService` (`core/services/account.service.ts`)
- Replace `AuthService` from Auth0 with `OidcSecurityService`
- `isAuthenticated` → from `oidcSecurityService.isAuthenticated$`
- `user` → from `oidcSecurityService.userData$`
- `roles` → extract from `userData.realm_access?.roles` (Keycloak puts roles here when `roles` scope is included)
- `logout()` → call `oidcSecurityService.logoff()`
- Add `getAccessToken(): Observable<string>` → delegates to `oidcSecurityService.getAccessToken()`
- Add `checkAuth()` method to call on app init

### 5. Update `auth.interceptor.ts`
Replace with the built-in interceptor from `angular-auth-oidc-client` (configured via `secureRoutes` in the auth config). Remove the custom file entirely.

### 6. Update `app.routes.ts`
Replace Auth0's `AuthGuard` with `AutoLoginPartialRoutesGuard` from `angular-auth-oidc-client`. This guard redirects unauthenticated users to Keycloak login.

### 7. Update `role-guard.ts`
Change role extraction from `user["https://eelkhair.net/roles"]` to using `AccountService.roles()` signal (which now reads from `realm_access.roles`).

### 8. Update `app.ts` (root component)
Replace `auth.getAccessTokenSilently().subscribe(...)` with `accountService.checkAuth()` to trigger OIDC callback handling + then start SignalR hubs.

### 9. Update `header.ts`
Replace `accountService.auth.user$` with the `accountService.user` signal / `OidcSecurityService.userData$`. Keycloak user profile uses standard OIDC claims (`given_name`, `family_name`, `preferred_username`, `email`) — same field names the header already checks.

### 10. Update SignalR services (token factory)
Both `RealtimeNotificationsService` and `AiRealtimeService` call `account.auth.getAccessTokenSilently()` for WebSocket auth. Replace with `accountService.getAccessToken()`.

### 11. Update role checks in `menuItems.ts`
Change `roles.includes('admin')` → `roles.includes('platform-admin')`.

### 12. Update role checks in route data
Search all route files for `data: { roles: ['admin'] }` and update to `['platform-admin']`.

---

## Files Changed (summary)
| File | Change |
|---|---|
| `package.json` | Swap auth0 → angular-auth-oidc-client |
| `environment.ts` | Add oidc config |
| `environment.development.ts` | Add oidc config |
| `app.config.ts` | Replace provideAuth0 with provideAuth + secureRoutes |
| `core/interceptores/auth.interceptor.ts` | Delete (replaced by library interceptor) |
| `core/services/account.service.ts` | Full rewrite using OidcSecurityService |
| `core/guards/role-guard.ts` | Update role claim path |
| `app.routes.ts` | Replace AuthGuard → AutoLoginPartialRoutesGuard |
| `app.ts` | Replace getAccessTokenSilently → checkAuth |
| `layout/header/header.ts` | Use AccountService signals instead of auth.user$ |
| `core/services/realtime-notifications.service.ts` | Replace token factory |
| `core/services/ai-realtime.service.ts` | Replace token factory |
| `layout/nav/menuItems.ts` | `'admin'` → `'platform-admin'` |
| Feature route files (settings, companies) | Update role data arrays |

## Notes
- Auth0 is being fully replaced (not dual-provider). Backend migration will follow after the frontend is done.
- **User `sub` claim format changes**: Auth0 uses `auth0|xxx`, Keycloak uses a UUID. Backend `HttpUserAccessor` reads `ClaimConstants.NameIdentifierId` — existing user records in the DB will need re-mapping when backend is migrated.
- Backend migration (monolith + AI service) will be a separate task: update `Authority`, `Audience`, role claim paths in `DependencyInjection.cs`, and `HttpUserAccessor` claim extraction.
