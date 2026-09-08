# Handoff

## Current state

Phase 4 change `platform-web-runtime-foundation` is implemented and archived. The repository now ships `Platform.Web` as an opt-in runtime layer over `Platform.AspNetCore`; it provides validated options, safe redaction/configuration/provider-status seams, correlation hardening, security headers, request limits, timeout cancellation, and independent `/live` and `/ready` endpoints. It does not register identity, persistence, or external providers.

## Next change

`platform-persistence-efcore` is the next dependency-ordered active change. Nine Phase 4 changes remain active; implement only one change per cycle.

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
