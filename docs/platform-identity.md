# Platform identity and authorization

The identity capability is split into provider-neutral contracts, an optional ASP.NET Core adapter, an optional EF Core adapter, and deterministic test providers.

## Local development

Register `AddPlatformIdentity()` in the application service collection. Tests and local tools can register `FakeCurrentUserAccessor`, `FakeCredentialVerifier`, `FakeExternalIdentityProvider`, or `FakeVerificationProvider` explicitly. The default current-user value is anonymous and does not throw when no request identity exists.

Applications register their own permission catalog entries and then call `RequirePlatformPermission("resource.action")`. Platform packages do not define product roles or permissions.

## Production configuration

Applications own authentication handlers, token formats, user entities, tenant resolution, and external provider credentials. Call `AddPlatformIdentityAuthentication()` only when the application intentionally owns the platform scheme name; application-owned schemes may be registered separately. Claims are projected into `CurrentUser` through `HttpCurrentUserAccessor` using subject, email, tenant, role, and permission claims.

Provider adapters must return `IdentityProviderResult<T>` with a normalized `IdentityFailureReason`. Provider response bodies, secrets, and vendor exceptions must not be placed in the result.

## Persistence and replacement

`Platform.Identity.EntityFrameworkCore` supplies `IIdentityStore` and `IdentityDbContextAdapter`, but no user entity, schema, migration, or business relationship. Applications derive the adapter and configure their own model. Replace the store and all external providers through the public interfaces.

Authorization decisions can be observed through `IAuthorizationDecisionAuditor`. Security-sensitive identity mutations use `IIdentityAuditHook`; the later admin/audit capability can provide the durable implementation.

## Migration guidance

Adopt contracts first, map the application's existing user and claims model to `CurrentUser`, then add the ASP.NET Core adapter and named policies. Add the EF adapter only after the application has selected its entity model and migration ownership. Keep provider SDK references in application/provider adapter projects, never in contract packages.
