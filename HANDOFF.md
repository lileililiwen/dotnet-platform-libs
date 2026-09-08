# Handoff

## Completed: platform-billing-provider-adapters

- Added optional `Platform.Billing.Stripe` and `Platform.Billing.LemonSqueezy` HTTP adapters
  implementing the provider-neutral billing boundary, application-owned plan mappings,
  checkout/session operations, subscription lookup, provider status, and provider-specific
  webhook verification/normalization.
- Added safe provider failure categories and classification for transient, permanent,
  configuration, authentication, and malformed-response failures. Secrets and provider response
  bodies are excluded from failure metadata.
- Added synthetic signature, normalization, HTTP mapping, duplicate/replay, health, and provider
  architecture coverage in `Platform.Billing.ProviderAdapters.Tests` and extended dependency
  direction tests.
- Added package/reference documentation and archived the change at
  `openspec/changes/archive/2026-09-08-platform-billing-provider-adapters/` with synchronized
  `openspec/specs/platform-billing-provider-adapters/spec.md`.

Verification evidence:

- `/home/paul/.dotnet/dotnet restore Platform.sln --ignore-failed-sources -p:NuGetAudit=false --nologo -m:1` — succeeded.
- `/home/paul/.dotnet/dotnet build Platform.sln -c Release --no-restore --nologo -m:1` — 0 warnings, 0 errors.
- `/home/paul/.dotnet/dotnet test Platform.sln -c Release --no-build --no-restore --nologo -m:1` — 457 tests passed, 0 failed, 0 skipped.
- Adapter package pack commands for `Platform.Billing.Stripe` and `Platform.Billing.LemonSqueezy` — both succeeded.
- `openspec validate --changes --strict --no-interactive` — 5 passed, 0 failed before archive; `openspec validate --specs --strict --no-interactive` — 15 passed, 0 failed after archive.
- Scoped `dotnet format --verify-no-changes --no-restore` for both adapter projects and the adapter test project — clean.
- Repository-wide `dotnet format Platform.sln --verify-no-changes --no-restore --verbosity minimal` remains blocked by pre-existing whitespace findings in `src/Platform.AspNetCore/DependencyInjection/ServiceCollectionExtensions.cs` and `tests/Platform.Identity.Tests/IdentityAspNetCoreTests.cs`, plus the known xUnit2013 warning in `tests/Platform.Testing.Tests/Usage/RecordingUsageMeterTests.cs`; these files were not changed.
- `git diff --check` and cached diff check — clean for the scoped change.
- Implementation commit: `355b96b` (`Implement billing provider adapters`).

## Next change

`platform-application-starter` is the next active change returned by `openspec list`. Implement
only that change in the next cycle.

## Completed: platform-billing-provider-abstractions

- Extended billing contracts with provider-neutral checkout, portal, subscription lookup,
  cancellation, webhook normalization, provider status, entitlement storage, plan catalogs,
  and application-owned provider-reference mappings.
- Added `Platform.Billing` orchestration for `(provider, event id)` deduplication, stale-event
  rejection, expiry-aware feature decisions, and usage-limit explanations.
- Added `Platform.Billing.Testing` deterministic in-memory entitlement, usage, and provider
  fakes; lifecycle, duplicate, out-of-order, cancellation, expiry, usage, architecture, and
  provider-boundary tests; and `docs/platform-billing.md`.
- Archived the change at
  `openspec/changes/archive/2026-09-08-platform-billing-provider-abstractions/` and synchronized
  `openspec/specs/platform-billing-provider-abstractions/spec.md`.

Verification evidence:

- `dotnet restore Platform.sln --ignore-failed-sources -p:NuGetAudit=false --nologo -m:1` — succeeded.
- `dotnet build Platform.sln -c Release --no-restore --nologo -m:1` — 0 warnings, 0 errors.
- `dotnet test Platform.sln -c Release --no-build --no-restore --nologo -m:1` — 435 tests passed,
  0 failed, 0 skipped.
- `dotnet pack` for `Platform.Billing.Contracts`, `Platform.Billing`, and
  `Platform.Billing.Testing` — all succeeded.
- `openspec validate --changes --strict --no-interactive` — 6 passed, 0 failed before archive.
- `git diff --check` and cached diff check — clean.

## Next change

`platform-billing-provider-adapters` is the next active change in the Phase 4 dependency order.
Implement only that change in the next cycle.

## Completed: platform-admin-capability

- Added `Platform.Admin.Contracts` with bounded admin query/page contracts, safe user/role/
  permission/session/audit/provider/subscription projections, explicit permission catalog,
  host-owned store/tenant/audit/impersonation extension points, and endpoint metadata.
- Added opt-in `Platform.Admin.AspNetCore` registration and endpoint mapping for user, role,
  permission, session, audit, provider, subscription, mutation, and optional impersonation
  surfaces. Routes require explicit permissions; query bounds, tenant checks, safe results, and
  structured mutation/impersonation audit events are enforced.
- Added `Platform.Admin.Testing` in-memory store and recording audit sink, TestServer/unit
  coverage, architecture guards, and `docs/platform-admin.md`.
- Archived the completed change at `openspec/changes/archive/2026-09-08-platform-admin-capability/`
  and synchronized `openspec/specs/platform-admin-capability/spec.md`.

Verification evidence:

- `dotnet restore Platform.sln --ignore-failed-sources -p:NuGetAudit=false --nologo -m:1` — succeeded.
- `dotnet build Platform.sln -c Release --no-restore --nologo -m:1` — 0 warnings, 0 errors.
- `dotnet test Platform.sln -c Release --no-build --no-restore --nologo -m:1` — 428 tests passed,
  0 failed, 0 skipped.
- `dotnet pack src/Platform.Admin.Contracts/Platform.Admin.Contracts.csproj -c Release --no-build --no-restore --nologo -m:1` — succeeded.
- `dotnet pack src/Platform.Admin.AspNetCore/Platform.Admin.AspNetCore.csproj -c Release --no-build --no-restore --nologo -m:1` — succeeded.
- `openspec validate --changes --strict --no-interactive` — 7 passed, 0 failed before archive.
- `git diff --check` and cached diff check — clean.

## Next change

`platform-billing-provider-abstractions` is the next active change in the Phase 4 dependency order.
Implement only that change in the next cycle.

## Completed: platform-identity-authorization

- Added provider-neutral identity contracts for current users, credentials, external identities, verification, sessions, provider status, and security-sensitive audit hooks.
- Added `Platform.Authorization` permission definitions/catalogs and authorization decision contracts; consuming modules own product permissions and roles.
- Added `Platform.Identity.AspNetCore` authentication scheme ownership, claims projection, current-user accessor, permission policies, and replaceable registration helpers.
- Added optional `Platform.Identity.EntityFrameworkCore` store contracts/adapters and deterministic `Platform.Identity.Testing` fake providers.
- Added identity contract, provider-failure, authorization, audit-hook, and architecture tests plus `docs/platform-identity.md`.
- Archived the completed change at `openspec/changes/archive/2026-09-08-platform-identity-authorization/` and synchronized `openspec/specs/platform-identity-authorization/spec.md`.

Verification evidence:

- `dotnet build Platform.sln -c Release --nologo -m:1` — 0 warnings, 0 errors.
- `dotnet test Platform.sln -c Release --nologo --no-build` — 411 tests passed, 0 failed, 0 skipped.
- Added `global.json` pinning SDK `8.0.424` with latest-patch roll-forward. This matches the repository's `net8.0` target and prevents SDK 10.0.400's silent `--no-build` pack failure inside `_GetFrameworkAssemblyReferences`.
- `dotnet pack src/Platform.Identity.AspNetCore/Platform.Identity.AspNetCore.csproj -c Release --no-build --no-restore --nologo -m:1` — produced `Platform.Identity.AspNetCore.0.1.0.nupkg` under SDK 8.0.424.
- Removed an unrelated async-without-await warning in `Platform.Idempotency.Tests`; SDK 8 solution build is now warning-free.
- A default SDK 8 restore attempted NuGet vulnerability metadata and hit unavailable `api.nuget.org` (`NU1900`); cached verification used `--ignore-failed-sources -p:NuGetAudit=false` and completed.
- `openspec validate --changes --strict --no-interactive` — 8 passed, 0 failed before archive.
- `git diff --check` and `git diff --cached --check` — clean.

## Current state

Phase 4 changes `platform-web-runtime-foundation`, `platform-persistence-efcore`, and `platform-identity-authorization` are implemented and archived. The repository now ships `Platform.Web` as an opt-in runtime layer over `Platform.AspNetCore`, optional provider-neutral EF Core persistence conventions, a separate PostgreSQL adapter, and replaceable identity/authorization packages. Persistence and identity do not own application entities, contexts, migrations, tenant types, product roles, or business permissions.

## Next change

`platform-application-starter` is the next active change returned by `openspec list`. Seven Phase 4 changes remain active; implement only one change per cycle.

## Completed: platform-persistence-efcore

- Added `Platform.Persistence.EfCore` with `IAuditableEntity`, `ISoftDeletable`, `ITenantScoped`, `ITenantScope`, bounded paging, specification composition, explicit model filters, configurable save interception, read-only migration status, readiness health checks, and DI registration.
- Added `Platform.Persistence.Postgres` with only Npgsql options configuration; it does not create contexts or apply migrations.
- Added in-memory and SQLite tests, concurrent independent-context coverage, PostgreSQL adapter coverage, architecture guards, and `docs/platform-persistence.md`.

Verification evidence:

- `dotnet build Platform.sln -c Release --nologo -m:1` — 26 projects, 0 warnings, 0 errors.
- `dotnet test Platform.sln -c Release --nologo --no-build` — 385 tests passed, 0 failed, 0 skipped, 0 warnings.
- `dotnet pack src/Platform.Persistence.EfCore/Platform.Persistence.EfCore.csproj -c Release --no-build --no-restore --nologo -m:1` — produced `Platform.Persistence.EfCore.0.1.0.nupkg`.
- `dotnet pack src/Platform.Persistence.Postgres/Platform.Persistence.Postgres.csproj -c Release --no-build --no-restore --nologo -m:1` — produced `Platform.Persistence.Postgres.0.1.0.nupkg`.
- `openspec validate --changes --strict --no-interactive` — 9 passed, 0 failed before archive.
- `git diff --check` — clean before commit.

## Completed: platform-web-runtime-foundation

- Added `src/Platform.Web` (`net8.0`) with only a `Platform.AspNetCore` project reference and the ASP.NET Core framework reference.
- Added `AddPlatformWeb`, `UsePlatformWeb`, and `MapPlatformRuntimeEndpoints` with explicit, replaceable DI registrations.
- Added safe option validation, configuration validator/redactor/provider-status contracts, security headers, request-size enforcement, request timeout cancellation, hardened incoming correlation values, and stable liveness/readiness JSON responses.
- Added 8 `Platform.Web.Tests` unit and TestServer tests plus architecture coverage for package/project direction and framework references.
- Added `docs/platform-web.md` covering bootstrap, ordering, configuration, replacement, and migration boundaries.

Verification evidence:

- `dotnet restore Platform.sln` — succeeded.
- `dotnet build Platform.sln -c Release --nologo --no-restore -m:1` — 0 warnings, 0 errors. Parallel build attempts had an SDK project-reference resolution failure with no reported diagnostics; serial build succeeded.
- `dotnet test Platform.sln -c Release --nologo` — 366 tests passed, 0 failed; one pre-existing xUnit2013 warning in `Platform.Testing.Tests`.
- `dotnet pack src/Platform.Web/Platform.Web.csproj -c Release --no-build --no-restore --nologo -m:1` — produced `Platform.Web.0.1.0.nupkg`.
- `openspec validate --changes --strict --no-interactive` — 10 passed, 0 failed.
- `git diff --check` — clean.

## Required sequence

1. Select one active change with `openspec list`.
2. Implement only that change and its tests.
3. Update its `tasks.md` as tasks complete.
4. Verify with relevant tests and strict OpenSpec validation.
5. Archive the completed change.
6. Commit 1: implementation, tests, archive, and related generated specs only.
7. Update this file with completion evidence and the next change.
8. Commit 2: only the `HANDOFF.md` update.
9. Stop; do not start another change or push.

## Completed: platform-extraction-ratelimiting

- Added `Platform.RateLimiting` (`net8.0`, version `0.1.0`):
  `IRateLimiter`, `InMemoryRateLimiter`, `RateLimitDecision`,
  `RateLimitKey`, `RateLimitPolicies`,
  `IRateLimitBypassResolver`,
  `ConfigurationRateLimitBypassResolver`,
  `IRateLimiterBackendStatusProvider`,
  `InMemoryRateLimiterBackendStatusProvider`,
  `IRateLimiterBackendStatus`, `HttpContextAbstraction`,
  `RateLimitingOptions`, `RateLimitPolicyOptions`, and
  `Platform.RateLimiting.DependencyInjection.ServiceCollectionExtensions.AddPlatformRateLimiting`.
- `InMemoryRateLimiter` is a thread-safe default that tracks a
  per-key windowed counter, returns a `RateLimitDecision` with
  the documented fields (`Allowed`, `Limit`, `Remaining`,
  `RetryAfterSeconds`), reads the current time from an injected
  `IClock` (no `DateTimeOffset.UtcNow` call), and rolls the bucket
  over when the configured `WindowSeconds` elapses. Different
  subjects under the same policy have independent buckets.
- `RateLimitPolicies.Default()` exposes the documented catalog
  (`feed`, `search`, `uploads`, `downloads`, `account-recovery`)
  with their documented limits and windows. Consumers register a
  custom `RateLimitPolicies` before
  `AddPlatformRateLimiting` to override the catalog.
- `ConfigurationRateLimitBypassResolver` is the default
  `IRateLimitBypassResolver` that matches documented bypass tokens
  (case-insensitive) against
  `HttpContextAbstraction.BypassToken`.
- `InMemoryRateLimiterBackendStatusProvider` reports
  `Provider = "memory"`, `Available = true` so readiness checks
  can observe the backend without changing call sites. Consumer
  adapters can replace the registration with a provider-specific
  implementation.
- `AddPlatformRateLimiting(IServiceCollection)` and the
  `Action<RateLimitingOptions>` overload register the
  `InMemoryRateLimiter`, the
  `ConfigurationRateLimitBypassResolver`, the
  `InMemoryRateLimiterBackendStatusProvider`, and an
  `IClock` when no implementation is already present.
- Package depends on `Platform.Core` and the two
  `Microsoft.Extensions.*` abstractions; no ASP.NET Core, EF Core,
  StackExchange.Redis, or VisualFlow references.
- Extended `Platform.Architecture.Tests`:
  - `Platform_RateLimiting_does_not_reference_forbidden_packages`
    — fails on any `Microsoft.AspNetCore`,
    `Microsoft.EntityFrameworkCore`, or `StackExchange.Redis`
    reference.
  - `Platform_RateLimiting_only_references_Platform_Core` — fails
    on any project reference other than `Platform.Core`.
  - `Platform_RateLimiting_does_not_reference_visual_flow_projects`
    — fails on any project reference whose path contains
    `VisualFlow`.
- `Platform.RateLimiting.Tests` (34 tests) covers the assembly
  marker, `RateLimitPolicies` (default catalog, `Find`, invalid
  entry dropping, null guard), `RateLimitPolicyOptions` defaults,
  `RateLimitingOptions` defaults, `InMemoryRateLimiter` (first
  request, burst over the limit, window roll-over with a
  `MutableClock`, per-subject isolation, unknown/empty policy /
  subject rejection, null dependency guards),
  `ConfigurationRateLimitBypassResolver` (empty/whitespace token,
  matching token, case-insensitive match, non-matching token, null
  context, null options),
  `InMemoryRateLimiterBackendStatusProvider`, and a `TestServer`
  integration test that exercises the documented default policy
  catalog, a configuration override, the limiter decision through
  DI, and the readiness surface through a full `WebApplication`
  host.

## Verification evidence

- `dotnet restore Platform.sln` — clean.
- `dotnet build Platform.sln -c Release --no-restore --nologo` — 0
  warnings, 0 errors (the pre-existing xUnit2013 warning in
  `Platform.Testing.Tests` is not in this change).
- `dotnet test Platform.sln -c Release --no-build --nologo` — 354
  tests passed (28 Core, 49 Billing.Contracts, 36 Testing, 22
  AspNetCore, 33 Jobs, 40 Mailing, 30 Eventing, 33 Idempotency,
  34 RateLimiting, 49 Architecture), 0 failed, 0 skipped.
- `dotnet pack src/Platform.RateLimiting/Platform.RateLimiting.csproj
  -c Release --no-build --nologo` — produced
  `Platform.RateLimiting.0.1.0.nupkg`; inspected `.nuspec` and
  confirmed `<dependencies>` contains only `Platform.Core`,
  `Microsoft.Extensions.DependencyInjection.Abstractions`, and
  `Microsoft.Extensions.Options`.
- Production isolation: existing architecture tests confirm no
  production project gains a forbidden reference, and the new
  `Platform.RateLimiting` tests confirm its `Platform.Core`-only
  project reference and the absence of ASP.NET Core, EF Core,
  StackExchange.Redis, or VisualFlow references.
- `git diff --check` — clean.
- `openspec validate --changes --strict --no-interactive` — 0
  passed, 0 failed (no active changes remain).
- `openspec validate --specs --strict --no-interactive` — 10
  passed, 0 failed.
- `openspec list` — empty.

## Completed earlier: platform-extraction-idempotency

- `Platform.Idempotency` (`net8.0`, version `0.1.0`):
  `IdempotencyRecord`, `IIdempotencyStore`,
  `InMemoryIdempotencyStore` (consumes `IClock`, honours
  `RetentionSeconds` and `MaxKeyLength`),
  `RequestFingerprint` (stable, hex-encoded SHA-256 over
  normalised method + route + body hash),
  `IdempotencyOptions` (with the documented metric-name
  constants), `IdempotencyMetrics`, and `AddPlatformIdempotency`.
  33 unit + TestServer tests; three new architecture guardrails.

## Completed earlier: platform-extraction-eventing

- `Platform.Eventing` (`net8.0`, version `0.1.0`):
  `IIntegrationEvent`, `IntegrationEvent`,
  `IntegrationEventEnvelope`, the default serializer /
  deserializer, `IIntegrationEventHandler<TEvent>`, `IEventBus`,
  `InProcessEventBus` (bounded `Channel<T>`, consumes `IClock`,
  idempotent disposal), `EventingOptions`, `AddPlatformEventing`,
  and `AddPlatformEventingInProcess`. 30 unit + TestServer tests;
  three new architecture guardrails.

## Completed earlier: platform-extraction-mailing

- `Platform.Mailing` (`net8.0`, version `0.1.0`): `MailAddress`,
  `MailAttachment`, `MailMessage`, `MailSendOutcome`,
  `MailSendResult`, `IMailService`, `MailTemplateId`,
  `IMailTemplateRenderer<TModel>`, `RenderedMailTemplate`,
  `MailingOptions`, and `AddPlatformMailing`. 40 unit + TestServer
  tests; three new architecture guardrails.

## Completed earlier: platform-extraction-jobs

- `Platform.Jobs` (`net8.0`, version `0.1.0`): `IJobDispatcher`,
  `IRecurringJobHandler`, `IRecurringJobRegistry`, `IJobTelemetry`,
  `JobPayload`, `RecurringJobAttribute`, `RecurringJobDescriptor`,
  `BackgroundJobsOptions`, and `AddPlatformJobs`. 33 unit +
  TestServer tests; three new architecture guardrails.
# Completed: platform-application-starter

- Added `Platform.Starter` with explicit web, identity, admin, billing, AI, notifications, and
  SMS capability switches; web is enabled by default, optional capabilities remain disabled,
  provider names are explicit in production, and consumer registrations remain replaceable.
- Added `UsePlatformApplication` and `MapPlatformApplicationEndpoints` with documented runtime
  and admin mapping order, plus safe starter status/configuration validation.
- Added the `Platform.Starter.Sample` conformance host and a `dotnet new`-compatible scaffold
  with API, domain, infrastructure, test, configuration, and Razor/React client seam files.
- Added starter registration/production-validation tests, package documentation, adoption and
  rollback guidance, and archived the change at
  `openspec/changes/archive/2026-09-08-platform-application-starter/` with synchronized
  `openspec/specs/platform-application-starter/spec.md`.

Verification evidence:

- `/home/paul/.dotnet/dotnet restore Platform.sln --ignore-failed-sources -p:NuGetAudit=false --nologo -m:1` — succeeded.
- `/home/paul/.dotnet/dotnet build Platform.sln -c Release --no-restore --nologo -m:1` — 0 warnings, 0 errors.
- `/home/paul/.dotnet/dotnet test Platform.sln -c Release --no-build --no-restore --nologo -m:1` — 457 tests passed, 0 failed, 0 skipped.
- `Platform.Starter` pack succeeded; the temporary local package-chain template verification generated `GeneratedStarter`, restored it, and built it with 0 warnings and 0 errors.
- `openspec validate --changes --strict --no-interactive` — 4 passed, 0 failed; `git diff --check` and cached diff check — clean.
- Implementation commit: `60211ae` (`Implement platform application starter`).

## Next change

`platform-ui-design-system` is the next active change returned by `openspec list`. Implement
only that change in the next cycle.
# Completed: platform-ui-design-system

- Added DTCG-compatible token source and generated semantic CSS/TypeScript outputs with light,
  dark, reduced-motion, focus, spacing, typography, and state tokens.
- Added ignored-path-safe `@platform/react-ui` primitives/state components and
  `@platform/react-shell` navigation/auth/permission contracts without copying application
  pages.
- Added `Platform.UI.Razor` static web assets, equivalent state conventions, package tests, and
  solution integration. Added token/component verification scripts and adoption documentation.
- Archived the change at
  `openspec/changes/archive/2026-09-08-platform-ui-design-system/` with synchronized
  `openspec/specs/platform-ui-design-system/spec.md`.

Verification evidence:

- `/home/paul/.dotnet/dotnet restore Platform.sln --ignore-failed-sources -p:NuGetAudit=false --nologo -m:1` — succeeded.
- `/home/paul/.dotnet/dotnet build Platform.sln -c Release --no-restore --nologo -m:1` — 0 errors; one pre-existing xUnit2013 warning in `Platform.Testing.Tests`.
- `/home/paul/.dotnet/dotnet test Platform.sln -c Release --no-build --no-restore --nologo -m:1` — 464 tests passed, 0 failed, 0 skipped.
- `node ui/scripts/generate-tokens.mjs`, `node ui/scripts/verify-ui.mjs`, and React UI component contract tests — passed.
- `Platform.UI.Razor` pack succeeded; `openspec validate --changes --strict --no-interactive` — 3 passed, 0 failed; `git diff --check` — clean.
- Implementation commit: `7320bd0` (`Implement platform UI design system`).

## Next change

`platform-notifications-sms` is the next active change returned by `openspec list`. Implement
only that change in the next cycle.
# Completed: platform-notifications-sms

- Added `Platform.Notifications` channel-neutral email/SMS intents, normalized outcomes,
  provider-status contracts, safe failure categories, bounded transient retries, and stable
  idempotency-key suppression.
- Added explicit integration with existing mailing, jobs, and idempotency contracts; production
  registration adds no provider, while `Platform.Notifications.Testing` supplies a deterministic
  in-memory email/SMS provider for development and tests.
- Added scheduling helpers, retry/duplicate/unconfigured-provider/redaction tests, package docs,
  and archived the change at
  `openspec/changes/archive/2026-09-08-platform-notifications-sms/` with synchronized
  `openspec/specs/platform-notifications-sms/spec.md`.

Verification evidence:

- `/home/paul/.dotnet/dotnet restore Platform.sln --ignore-failed-sources -p:NuGetAudit=false --nologo -m:1` — succeeded.
- `/home/paul/.dotnet/dotnet build Platform.sln -c Release --no-restore --nologo -m:1` — 0 errors; one pre-existing xUnit2013 warning in `Platform.Testing.Tests`.
- `/home/paul/.dotnet/dotnet test Platform.sln -c Release --no-build --no-restore --nologo -m:1` — 469 tests passed, 0 failed, 0 skipped.
- Both notification packages packed successfully; `openspec validate --changes --strict --no-interactive` — 2 passed, 0 failed; `git diff --check` and cached diff check — clean.
- Implementation commit: `a8a4510` (`Implement platform notifications and SMS`).

## Next change

`platform-ai-provider-abstractions` is the next active change returned by `openspec list`.
Implement only that change in the next cycle.
