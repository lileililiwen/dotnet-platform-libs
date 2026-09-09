# Handoff

## Completed: platform-consumer-conformance

- Added `scripts/conformance.sh` that packs every platform project to a local
  feed at `tests/Platform.ConsumerConformance/.local-feed/`, restores the
  fixture against that feed plus `nuget.org`, and runs the conformance
  tests. Per-step logs land in `tests/Platform.ConsumerConformance/.logs/`
  and any failure is reported as `ENV BLOCKER` with the exact command and
  next action so CI can distinguish a source regression from a missing
  Docker image, database, credential, external provider, or local feed.
- Added `tests/Platform.ConsumerConformance/` (intentionally **not** part
  of `Platform.sln`) as the test-only consumer fixture. The project
  references every adopted platform package via `<PackageReference>`
  against the local feed, disables central package version management, and
  ships its own `nuget.config` plus `.gitignore` so the generated feed,
  package cache, and logs stay out of source control.
- Added 75 conformance tests across 19 classes covering package-feed
  verification (no `<ProjectReference>` to platform projects, loaded
  assembly references only platform assemblies, `nuget.config` declares
  the local feed, `.gitignore` excludes `.local-feed/`), registration
  (Platform.AspNetCore, RateLimiting, Idempotency, Jobs, Mailing,
  Eventing, Caching, Quota, Storage, Persistence, Webhooks, durable
  eventing, billing orchestration), replacement
  (`IRateLimiter`/`IMailService`/`IJobDispatcher`/`IIdempotencyStore`/
  `IObjectStorage` overrides win, consumer clock is preserved, options
  overrides take precedence), opt-in boundaries (each `AddPlatformXxx`
  registers only its own services, optional packages combine cleanly),
  health/readiness (liveness tag, `TestServer` `Healthy`, unhealthy
  aggregation, rate-limit backend status), failure classification
  (`ProviderFailureClassifier` returns the documented kind for every
  exception type and the safe message never carries the original text),
  cancellation (rate limiter and a consumer-registered idempotency
  store observe the token), redaction (HMAC verifier rejects mismatches,
  SSRF validator rejects loopback and insecure schemes), tenant
  isolation, in-process event bus, and an end-to-end host built entirely
  from packages that returns sanitized `ProblemDetails` and echoes the
  correlation identifier.
- Extended `Platform.Architecture.Tests` (213 tests, +4 new):
  `Consumer_conformance_project_does_not_reference_platform_projects`,
  `Consumer_conformance_project_declares_a_local_nuget_feed`,
  `Consumer_conformance_build_script_records_environment_blockers`, and
  `Production_project_does_not_reference_consumer_conformance_project`.
- Added `docs/platform-consumer-conformance.md` with the fixture layout,
  the run loop, the per-conformance-assertion summary, opt-in Docker /
  provider extension guidance, and the rollback path. Updated
  `docs/packages.md` and `docs/build-test-pack.md` to point at the new
  fixture and the `scripts/conformance.sh` entry point.
- Archived the change at
  `openspec/changes/archive/2026-09-09-platform-consumer-conformance/`
  with synchronized `openspec/specs/platform-consumer-conformance/spec.md`
  covering package-feed consumer fixture, registration and replacement
  verification, runtime safety verification, and environment blocker
  reporting. No platform package, application code, or production
  project was modified.

## Verification evidence

- `dotnet build Platform.sln -c Release --nologo -m:1` — 0 warnings, 0
  errors; the pre-existing `Platform.Testing.Tests` xUnit2013 warning was
  unchanged.
- `dotnet test Platform.sln -c Release --no-build --nologo -m:1` —
  763 tests passed, 0 failed, 0 skipped, including the extended
  `Platform.Architecture.Tests` (213 tests, +4 new consumer-conformance
  assertions).
- `./scripts/conformance.sh` — packed every platform project into
  `tests/Platform.ConsumerConformance/.local-feed/`, restored, built, and
  ran the 75 conformance tests against the published artifacts (0 failed).
- `openspec validate --changes --strict --no-interactive` — 8 passed,
  0 failed after archive.
- `openspec validate --specs --strict --no-interactive` — 24 passed,
  0 failed after archive.
- `git diff --check` — clean for the staged change.

## Next change

`platform-featureflags-resilience` is the next active change returned by
`openspec list`. Implement only that change in the next cycle.

## Completed: platform-persistence-multitenancy

- Added framework-neutral tenant contracts in `Platform.Core/Tenancy`:
  `ITenantInfo`, `ITenantResolver` + `TenantResolutionResult` + `TenantResolutionStatus`,
  `IAmbientTenantScope`, `ITenantScopeFactory`, `ITenantConnectionResolver` +
  `TenantConnectionDescriptor`, `ITenantConnectionReadinessProbe` +
  `TenantConnectionReadinessResult`, and `TenantScopeNotResolvedException`. `Platform.Core`
  has no new package dependencies.
- Added `Platform.Persistence.Multitenancy` (`net8.0`, version `0.1.0`):
  `AmbientTenantScope` with a generation counter, `AmbientTenantScopeStore` over
  `AsyncLocal<>`, `ITenantScopeAccessor` + `TenantScopeAccessor`, `TenantScopeFactory` with
  snapshotted prior-scope restoration, `TenantScopeMiddleware` + `UsePlatformMultitenancy`,
  `IGlobalTenantEntity` marker, `TenantModelBuilderExtensions.ApplyDefaultTenantFilters`
  with documented `GlobalFilterBehavior` override (`Apply`, `IgnoreGlobalScope`, `Skip`),
  `ScopedTenantConnectionProvider` (cached within a single generation, fail-closed by
  default, `IDisposable`), `TenantConnectionReadinessCheck` +
  `AddPlatformTenantConnectionReadinessCheck` registering under the `ready` tag, and
  `AddPlatformPersistenceMultitenancy` with bounded `MultitenancyOptions`. Every default
  is registered with `TryAdd` so applications can replace the resolver, connection
  resolver, scope factory, and readiness probe before registration.
- Package references `Platform.Core`, `Platform.AspNetCore`, `Platform.Persistence.EfCore`,
  EF Core, and `Microsoft.Extensions.Diagnostics.HealthChecks.Abstractions`. No Finbuckle,
  Npgsql, Stripe, StackExchange.Redis, or VisualFlow references; the
  `Microsoft.AspNetCore.App` framework reference is declared on the adapter only.
- Added `tests/Platform.Persistence.Multitenancy.Tests` (31 tests) covering options
  defaults and validation, replaceable factory registration, accessor/store wiring,
  scope factory tenant installation and restoration, global-operation scope
  installation, nested scope restoration, identifier length and empty/whitespace
  validation, concurrent `AsyncLocal` isolation across `Task.Run` boundaries, EF model
  filter application per `ITenantScoped` entity, `IGlobalTenantEntity` opt-out,
  `IgnoreGlobalScope` and `Skip` behaviors, scoped connection routing (resolved tenant,
  global operation, fail-closed on unresolved, shared fallback when fail-closed is
  disabled, generation-based caching), TestServer middleware coverage (resolved header,
  bounded length rejection, disabled installation, per-request scope replacement), and
  readiness check aggregation (healthy, unhealthy) and tag registration.
- Extended `Platform.Architecture.Tests` (209 tests, +3 new):
  `Platform_Persistence_Multitenancy_does_not_reference_forbidden_packages` (fails on
  any ASP.NET Core, Npgsql, Finbuckle, StackExchange.Redis, or Stripe package
  reference), `Platform_Persistence_Multitenancy_references_only_platform_core_aspnetcore_and_persistence`
  (fails on any project reference other than `Platform.Core`, `Platform.AspNetCore`,
  and `Platform.Persistence.EfCore`), and `Platform_Core_tenant_contracts_have_no_package_dependencies`
  (confirms `Platform.Core` remains dependency-light). The
  `Only_Platform_AspNetCore_declares_a_FrameworkReference` and
  `Production_project_does_not_reference_forbidden_frameworks` rules were extended to
  cover the new package.
- Added `docs/platform-persistence-multitenancy.md` with adoption, background-handler,
  connection-routing, readiness, and rollback guidance. Updated `docs/packages.md`
  and `docs/platform-persistence.md` to reference the new package.
- Archived the change at
  `openspec/changes/archive/2026-09-08-platform-persistence-multitenancy/` with
  synchronized `openspec/specs/platform-persistence-multitenancy/spec.md` covering
  explicit tenant scope, default tenant isolation, fail-closed tenant access, and
  the connection routing seam. No tenant entities, migrations, connection strings, or
  tenant catalog persistence were added; the pre-existing
  `Platform.Persistence.EfCore` `ITenantScope`/`ITenantScoped` contracts remain
  compatible.

## Verification evidence

- `dotnet restore Platform.sln --ignore-failed-sources -p:NuGetAudit=false --nologo -m:1` — succeeded.
- `dotnet build Platform.sln -c Release --no-restore --nologo -m:1` — 0 warnings, 0 errors; the
  pre-existing `Platform.Testing.Tests` xUnit2013 warning was unchanged.
- `dotnet test Platform.sln -c Release --no-build --nologo -m:1` — 759 tests passed, 0 failed,
  0 skipped, including the new `Platform.Persistence.Multitenancy.Tests` (31 tests) and the
  extended `Platform.Architecture.Tests` (209 tests, +3 new multitenancy assertions).
- `dotnet pack src/Platform.Persistence.Multitenancy/Platform.Persistence.Multitenancy.csproj
  -c Release --no-build --no-restore --nologo -m:1` — produced
  `Platform.Persistence.Multitenancy.0.1.0.nupkg` with `<dependencies>` containing only
  `Platform.Core`, `Platform.AspNetCore`, `Platform.Persistence.EfCore`, EF Core, and
  `Microsoft.Extensions.Diagnostics.HealthChecks.Abstractions`, plus the
  `Microsoft.AspNetCore.App` framework reference.
- `openspec validate --changes --strict --no-interactive` — 9 passed, 0 failed after archive.
- `openspec validate --specs --strict --no-interactive` — 23 passed, 0 failed after archive.
- `git diff --check` — clean for the staged change.
- Implementation commit: `f082250` (`Implement shared persistence multitenancy`).

## Next change

`platform-consumer-conformance` is the next active change returned by
`openspec list`. Implement only that change in the next cycle.

- Added `Platform.Observability` (`net8.0`, ASP.NET Core) with bounded
  `PlatformObservabilityOptions` (service identity, correlation, label length
  caps, request, provider, and host enrichment switches), stable
  `PlatformObservabilityNames` activity source, meter, operation, and tag
  names, a replaceable `IPlatformObservabilityRedactor` plus
  `PlatformObservabilitySafeValuePolicy` (`BoundTag` for already-safe values,
  `RedactTag`/`RedactOperation` for sensitive values, and `RequireOperation`
  plus `RequireCorrelationId` validators), `PlatformDiagnostics` framework
  sources and meters, `IPlatformActivityRecorder` with `DefaultPlatformActivityRecorder`
  emitting bounded tags through the safe value policy,
  `IPlatformObservabilityProviderStatusSource` plus the default safe reporter,
  `PlatformObservabilityProviderCall` plus `IPlatformObservabilityProviderRecorder`
  for provider call enrichment, `IPlatformCorrelationAccessor` plus the
  `HttpPlatformCorrelationAccessor` bridge, `ICorrelationIdGenerator` plus the
  default `Guid.NewGuid("N")` generator, the
  `PlatformObservabilityCorrelationMiddleware` that echoes the bounded
  correlation identifier on the response, the `PlatformObservabilityHostLifetime`
  `IHostedLifecycleService` for host startup and shutdown activities, and the
  `AddPlatformObservability(IServiceCollection)` plus `Action<...>` overload
  with the `UsePlatformObservability(IApplicationBuilder)` middleware helper.
  Every default registration uses `TryAdd` so consumers can replace the
  redactor, accessor, recorder, status source, and host lifetime before
  calling `AddPlatformObservability`. The platform owns no exporter, Serilog
  sink, OpenTelemetry SDK reference, or product log schema.
- Added `tests/Platform.Observability.Tests` (39 tests) covering option
  validation, safe value policy (bound, redact, drop, correlation, operation
  validation), activity recorder emit/disabled/redaction paths, the
  provider recorder counter and duration, the default provider status
  source safe name contract, registration including replaceable redactor and
  host lifetime, host lifecycle activity emission and disabled lifecycle,
  and the correlation middleware across `TestServer` (generated identifier,
  rejected incoming identifier, accepted incoming identifier, disabled
  enrichment, and enabled enrichment with bounded route and method tags).
- Extended `Platform.Architecture.Tests` (203 tests, +2 new) with
  `Platform_Observability_does_not_reference_forbidden_packages` (fails on
  any EF Core, Npgsql, Redis, AWS SDK, OpenTelemetry, Serilog, or Stripe
  reference) and `Platform_Observability_references_only_platform_core_and_web_telemetry`
  (fails on any project reference other than `Platform.Core` and
  `Platform.Web.Telemetry`). The `Only_Platform_AspNetCore_declares_a_FrameworkReference`
  rule was extended to cover the new `Platform.Observability` package.
- Added `docs/platform-observability.md` with adoption, exporter wiring, and
  rollback guidance and updated `docs/packages.md` with the per-package
  reference.
- Archived the change at
  `openspec/changes/archive/2026-09-08-platform-observability/` with
  synchronized `openspec/specs/platform-observability/spec.md` covering
  host observability registration, safe diagnostic enrichment, stable
  platform telemetry, and replaceable exporter and redactor seams.

Verification evidence:

- `dotnet restore Platform.sln --ignore-failed-sources -p:NuGetAudit=false --nologo -m:1` — succeeded.
- `dotnet build Platform.sln -c Release --no-restore --nologo -m:1` — 0 warnings, 0 errors; the
  pre-existing `Platform.Testing.Tests` xUnit2013 warning was unchanged.
- `dotnet test Platform.sln -c Release --no-build --no-restore --nologo -m:1` — full solution
  passed including the new `Platform.Observability.Tests` (39 tests) and the extended
  `Platform.Architecture.Tests` (203 tests, +2 new observability assertions).
- `dotnet pack src/Platform.Observability/Platform.Observability.csproj -c Release
  --no-build --no-restore --nologo -m:1` — produced `Platform.Observability.0.1.0.nupkg`
  with `Platform.Core`, `Platform.Web.Telemetry`, four `Microsoft.Extensions.*`
  abstractions, and the `Microsoft.AspNetCore.App` framework reference.
- `openspec validate --changes --strict --no-interactive` — 10 passed, 0 failed after archive.
- `openspec validate --specs --strict --no-interactive` — 22 passed, 0 failed after archive.
- `git diff --check` — clean before commit.
- Implementation commit: `ef91668` (`Implement platform observability`).

## Next change

`platform-persistence-multitenancy` is the next active change returned by
`openspec list` and continues the shared-platform dependency order. Implement
only that change in the next cycle.

- Added `Platform.Web.Telemetry` (`net8.0`, framework-neutral) with stable
  request and provider instrumentation names, a redactor contract, a configurable
  redaction-safe value policy, the `IPlatformWebTelemetry` log sink, and a
  reusable `AddValidatedOptions<T>()` extension helper consumed by every other
  edge package.
- Added `Platform.Web.Cors` (`net8.0`, ASP.NET Core) with named policy options,
  strict production-time validation (wildcard origins with credentials, wildcard
  origins in production, missing origin in production, non-absolute origins),
  `AddPlatformWebCors(IServiceCollection)` plus the `IHostEnvironment` overload,
  and `UsePlatformWebCors(policyName)`.
- Added `Platform.Web.Resilience` (`net8.0`, ASP.NET Core) with
  `PlatformHttpResilienceOptions` (bounded defaults: 3 attempts, 5s per-attempt
  timeout, 0.5 failure ratio, 30s circuit-breaker sampling window) and
  `PlatformHttpResilienceHandler` that retries only idempotent methods
  (`GET`/`HEAD`/`OPTIONS` plus `PUT`/`DELETE` with `If-Match`), emits
  `X-Retry-Attempt`, and trips a circuit breaker. The default telemetry bridge
  forwards decisions to the platform web telemetry sink.
- Added `Platform.Web.OpenApi` (`net8.0`, ASP.NET Core) with
  `IPlatformOpenApiDocumentProvider`, an aggregating registry, and
  `MapPlatformOpenApiDocument(name)` plus `MapPlatformOpenApiDocuments()` helpers.
  The platform owns no Swashbuckle or NSwag dependency; applications supply the
  document JSON through a provider. Authorization metadata applied with
  `RequireAuthorization()` is preserved. Unknown document names resolve to `404`.
- Added `tests/Platform.Web.Edge.Tests` (44 tests) covering telemetry contract,
  CORS option and TestServer coverage, HTTP resilience option, handler, and
  circuit-breaker coverage, and OpenAPI registry plus TestServer coverage.
- Added architecture guardrails verifying that `Platform.Web.Telemetry` only
  references `Platform.Core`, that the three ASP.NET Core edge packages only
  reference `Platform.Core` and `Platform.Web.Telemetry`, and that none of them
  reference EF Core, Polly, Swashbuckle, or NSwag. The
  `Only_Platform_AspNetCore_declares_a_FrameworkReference` rule was extended
  to cover the new ASP.NET Core edge packages.
- Updated `docs/packages.md` with per-package reference and added
  `docs/platform-web-edge.md` with adoption examples and rollback guidance.
- Archived the change at
  `openspec/changes/archive/2026-09-08-platform-web-edge/` with synchronized
  `openspec/specs/platform-web-edge/spec.md`. The platform owns no
  Swashbuckle, NSwag, Polly, OpenTelemetry exporter, or third-party CORS
  library; the OpenAPI implementation responsibility stays with the
  application. SignalR/SSE work is deferred to a future change.

Verification evidence:

- `dotnet restore Platform.sln --ignore-failed-sources -p:NuGetAudit=false --nologo -m:1` — succeeded.
- `dotnet build Platform.sln -c Release --no-restore --nologo -m:1` — 0 warnings, 0 errors; the
  pre-existing `Platform.Testing.Tests` xUnit2013 warning was unchanged.
- `dotnet test Platform.sln -c Release --no-build --no-restore --nologo -m:1` — full solution
  passed including the new `Platform.Web.Edge.Tests` (44 tests) and the extended
  `Platform.Architecture.Tests` (198 tests, +12 new web-edge assertions).
- `dotnet pack src/Platform.Web.Telemetry/Platform.Web.Telemetry.csproj -c Release
  --no-build --no-restore --nologo -m:1` — produced `Platform.Web.Telemetry.0.1.0.nupkg`
  with only `Platform.Core` and three `Microsoft.Extensions.*` abstractions.
- `dotnet pack src/Platform.Web.Cors/Platform.Web.Cors.csproj -c Release
  --no-build --no-restore --nologo -m:1` — produced `Platform.Web.Cors.0.1.0.nupkg`
  with the `Microsoft.AspNetCore.App` framework reference and the
  `Platform.Web.Telemetry` + `Platform.Core` project references.
- `dotnet pack src/Platform.Web.Resilience/Platform.Web.Resilience.csproj -c Release
  --no-build --no-restore --nologo -m:1` — produced `Platform.Web.Resilience.0.1.0.nupkg`
  with `Microsoft.Extensions.Http`, the `Microsoft.AspNetCore.App` framework
  reference, and the `Platform.Web.Telemetry` + `Platform.Core` project references.
- `dotnet pack src/Platform.Web.OpenApi/Platform.Web.OpenApi.csproj -c Release
  --no-build --no-restore --nologo -m:1` — produced `Platform.Web.OpenApi.0.1.0.nupkg`
  with the `Microsoft.AspNetCore.App` framework reference and the
  `Platform.Web.Telemetry` + `Platform.Core` project references.
- `dotnet pack Platform.sln -c Release --no-build --no-restore --nologo -m:1` — produced
  the four new packages alongside the existing platform packages; the existing
  non-packable sample warning remains.
- `openspec validate --changes --strict --no-interactive` — 11 passed, 0 failed after archive.
- `openspec validate --specs --strict --no-interactive` — 21 passed, 0 failed after archive.
- `git diff --check` — clean before commit.
- Implementation commit: `f4007d1` (`Implement shared web edge integrations`).

## Next change

`platform-observability` is the next active change in the shared-platform
dependency order; it pairs naturally with the new `Platform.Web.Telemetry`
sink. Implement only that change in the next cycle.

- Added `Platform.Webhooks.Contracts` with provider-neutral inbound verification contracts, raw
  byte HMAC-SHA256 verifier, secret resolver, replay-protected inbox store, and an inbound
  processor that returns accepted/duplicate/busy/rejected outcomes with safe redacted failures.
- Added outbound subscription, retry policy, and delivery contracts, an SSRF-safe target validator
  that rejects non-absolute, non-HTTPS, loopback, private, and link-local destinations, and a
  default dispatcher that records the delivery lifecycle without owning secrets or response
  bodies.
- Added optional `Platform.Webhooks.AspNetCore` with `HttpRequest` capture, a default
  `HttpClient`-backed outbound sender with bounded timeouts and safe status projection, and a
  status endpoint helper. No provider-specific routes are registered.
- Added optional `Platform.Webhooks.EfCore` with `IEntityTypeConfiguration<>` adapters for the
  inbox and delivery entities. The application owns the `DbContext` and migrations; the platform
  ships no defaults and no application types.
- Added architecture guards verifying the contracts package only references `Platform.Core`, that
  the AspNetCore package does not reference VisualFlow, and that the EfCore package is excluded
  from the production package-prefix guard.
- Added package, starter, and adoption documentation, and archived the change at
  `openspec/changes/archive/2026-09-08-platform-webhooks/` with synchronized
  `openspec/specs/platform-webhooks/spec.md`. No main spec existed; no migrations, provider
  secrets, or product event payloads were added.

Verification evidence:

- `dotnet restore Platform.sln --ignore-failed-sources -p:NuGetAudit=false --nologo -m:1` — succeeded.
- `dotnet build Platform.sln -c Release --no-restore --nologo -m:1` — 0 warnings, 0 errors; the
  pre-existing `Platform.Testing.Tests` xUnit2013 warning was unchanged.
- `dotnet test Platform.sln -c Release --no-build --no-restore --nologo -m:1` — full solution
  passed including the new `Platform.Webhooks.Tests` (43 tests) and `Platform.Architecture.Tests`
  (178 tests). External DNS-dependent paths use literal public IP addresses so the suite is
  deterministic; live external provider integration tests are deferred to a separate suite.
- `dotnet pack src/Platform.Webhooks.Contracts/Platform.Webhooks.Contracts.csproj -c Release
  --no-build --no-restore --nologo -m:1` — produced `Platform.Webhooks.Contracts.0.1.0.nupkg`.
- `dotnet pack src/Platform.Webhooks.AspNetCore/Platform.Webhooks.AspNetCore.csproj -c Release
  --no-build --no-restore --nologo -m:1` — produced `Platform.Webhooks.AspNetCore.0.1.0.nupkg`.
- `dotnet pack src/Platform.Webhooks.EfCore/Platform.Webhooks.EfCore.csproj -c Release
  --no-build --no-restore --nologo -m:1` — produced `Platform.Webhooks.EfCore.0.1.0.nupkg`.
- `openspec validate --changes --strict --no-interactive` — 12 passed, 0 failed after archive.
- `git diff --check` — clean before commit.
- Implementation commit: `3bbbc4f` (`Implement shared webhook contracts`).

## Next change

`platform-web-edge` is the next active change in the shared-platform dependency order. Implement
only that change in the next cycle.

## Completed: platform-quota

- Added `Platform.Quota` with opaque subject/resource identifiers, explicit UTC windows,
  explanatory check decisions, bounded options, reservation/settlement/release lifecycle
  outcomes, and idempotent operation keys.
- Added a thread-safe `IClock`-driven in-memory store with atomic reservation capacity checks,
  expiration, safe invalid-transition outcomes, snapshots, and deterministic inspection helpers.
- Added the optional entitlement-to-limit resolver seam without importing plan, invoice, wallet,
  persistence, or provider entities. Added `Platform.Quota.Testing` scenario builders and kept
  package folders organized as `Contracts`, `Stores`, `Evaluation`, and `DependencyInjection`.
- Added quota tests, architecture guards, package/starter/sample documentation, and archived the
  completed change at `openspec/changes/archive/2026-09-09-platform-quota/`. No main spec existed
  to synchronize. No billing plans, migrations, ledgers, or product persistence were included.

Verification evidence:

- `/home/paul/.dotnet/dotnet restore Platform.sln --ignore-failed-sources -p:NuGetAudit=false --nologo -m:1` — succeeded.
- `/home/paul/.dotnet/dotnet build Platform.sln -c Release --no-restore --nologo -m:1` — succeeded; the existing `Platform.Testing.Tests` xUnit2013 warning remains.
- `/home/paul/.dotnet/dotnet test Platform.sln -c Release --no-build --no-restore --nologo -m:1` — 559 tests passed, 0 failed, 0 skipped.
- `/home/paul/.dotnet/dotnet test tests/Platform.Quota.Tests/Platform.Quota.Tests.csproj --no-restore --nologo -m:1` — 5 passed, 0 failed, 0 skipped.
- `Platform.Architecture.Tests` — 166 passed, 0 failed, 0 skipped.
- `/home/paul/.dotnet/dotnet pack Platform.sln -c Release --no-build --no-restore --nologo -m:1` — succeeded; `Platform.Quota` and `Platform.Quota.Testing` packages were produced and the existing non-packable sample warning remains.
- `openspec validate --changes --strict --no-interactive` — 2 passed, 0 failed after archive.
- `git diff --check` — clean before commit.
- Implementation commit: `35d407b` (`Implement shared quota lifecycle`).

## Next change

`platform-webhooks` is the next active change in the shared-platform dependency order. Implement
only that change in the next cycle.

## Completed: platform-storage

- Added `Platform.Storage` with provider-neutral object lifecycle contracts, validated object keys,
  upload limits, metadata, presigned operations, safe outcomes, and provider status.
- Added `Platform.Storage.Local` with bounded-root validation, atomic temporary-file writes,
  deterministic metadata/download/delete behavior, and safe disposal.
- Added optional `Platform.Storage.S3` using an application-provided `IAmazonS3` client, bounded
  waits, presigning, not-found handling, and redacted transient provider failures.
- Added storage architecture guards, local/S3 presign and contract coverage, package-folder
  guidance, and documentation for authorization, tenants, retention, metadata, and migration.
- Archived the completed change at
  `openspec/changes/archive/2026-09-08-platform-storage/`. No main spec existed to synchronize.

Verification evidence:

- `/home/paul/.dotnet/dotnet restore Platform.sln --ignore-failed-sources -p:NuGetAudit=false --nologo -m:1` — succeeded.
- `/home/paul/.dotnet/dotnet build Platform.sln -c Release --no-restore --nologo -m:1` — succeeded; the existing `Platform.Testing.Tests` xUnit2013 warning remains.
- `/home/paul/.dotnet/dotnet test Platform.sln -c Release --no-build --no-restore --nologo -m:1` — 546 tests passed, 0 failed, 0 skipped.
- `/home/paul/.dotnet/dotnet pack Platform.sln -c Release --no-build --no-restore --nologo -m:1` — succeeded; storage packages were produced and the existing non-packable sample warning remains.
- Targeted storage suite — 5 passed, 0 failed, 0 skipped; `Platform.Architecture.Tests` — 158 passed, 0 failed.
- `openspec validate --changes --strict --no-interactive` — 3 passed, 0 failed after archive.
- `git diff --check` — clean before commit.
- Implementation commit: `e566c38` (`Implement shared object storage`).

## Next change

`platform-quota` is the next active change in the shared-platform dependency order. Implement only
that change in the next cycle.

## Completed: platform-caching

- Added `Platform.Caching` with provider-neutral async cache contracts, explicit hit/miss/
  unavailable results, absolute expiration, tag invalidation, tenant/application key builders,
  safe provider status, stable telemetry names, and a thread-safe `IClock`-driven in-memory store.
- Added optional `Platform.Caching.Hybrid` and `Platform.Caching.Redis` packages. HybridCache stays
  isolated to its adapter; Redis uses application-owned serialization, bounded operation waits,
  tag sets, and redacted transient failure results.
- Added `Contracts`, `Keys`, `Telemetry`, and `DependencyInjection` organization inside the base
  package, adapter architecture guards, cache/starter/sample documentation, and hit/miss,
  expiry, tag invalidation, DI, and unavailable-backend tests.
- Archived the completed change at
  `openspec/changes/archive/2026-09-08-platform-caching/`. No main spec existed to synchronize.

Verification evidence:

- `/home/paul/.dotnet/dotnet restore Platform.sln --ignore-failed-sources -p:NuGetAudit=false --nologo -m:1` — succeeded.
- `/home/paul/.dotnet/dotnet build Platform.sln -c Release --no-restore --nologo -m:1` — succeeded; the existing `Platform.Testing.Tests` xUnit2013 warning remains.
- `/home/paul/.dotnet/dotnet test Platform.sln -c Release --no-build --no-restore --nologo -m:1` — 529 tests passed, 0 failed, 0 skipped.
- `/home/paul/.dotnet/dotnet pack Platform.sln -c Release --no-build --no-restore --nologo -m:1` — succeeded; caching packages were produced and the existing non-packable sample warning remains.
- Targeted caching suites — 8 passed, 0 failed, 0 skipped; `Platform.Architecture.Tests` — 146 passed, 0 failed.
- `openspec validate --changes --strict --no-interactive` — 4 passed, 0 failed after archive.
- `git diff --check` — clean before commit.
- Implementation commit: `6f35f56` (`Implement shared caching adapters`).

## Next change

`platform-storage` is the next active change in the shared-platform dependency order. Implement
only that change in the next cycle.

## Completed: platform-durable-eventing

- Added framework-neutral `Platform.Eventing.Contracts` outbox/inbox envelopes, state
  transitions, retry/dead-letter policies, lease claims, duplicate decisions, and in-memory
  stores.
- Added optional `Platform.Eventing.EfCore` mappings and stores with application-owned table
  names, transactional claims, lease recovery, bounded dispatch, safe failure logging, and
  hosted-service registration. It does not own an application DbContext, migrations, transport,
  or product event catalog.
- Added SQLite, independent-context, concurrent-claim, dispatcher, contract, and architecture
  coverage. Added durable-eventing package, starter, sample, and adoption documentation.
- Archived the completed change at
  `openspec/changes/archive/2026-09-08-platform-durable-eventing/`. No main spec existed to
  synchronize.

Verification evidence:

- `/home/paul/.dotnet/dotnet restore Platform.sln --ignore-failed-sources -p:NuGetAudit=false --nologo -m:1` — succeeded.
- `/home/paul/.dotnet/dotnet build Platform.sln -c Release --no-restore --nologo -m:1` — succeeded; the existing `Platform.Testing.Tests` xUnit2013 warning remains.
- `/home/paul/.dotnet/dotnet test Platform.sln -c Release --no-build --no-restore --nologo -m:1` — 509 tests passed, 0 failed, 0 skipped.
- `/home/paul/.dotnet/dotnet pack Platform.sln -c Release --no-build --no-restore --nologo -m:1` — succeeded; both eventing packages were produced and the existing non-packable sample warning remains.
- Targeted `Platform.Eventing.EfCore.Tests` — 6 passed, 0 failed, 0 skipped.
- `openspec validate --changes --strict --no-interactive` — 5 passed, 0 failed after archive.
- `Platform.Architecture.Tests` — 134 passed, 0 failed.
- `git diff --check` — clean before commit.
- Implementation commit: `9d38717` (`Implement durable eventing foundation`).

## Next change

`platform-caching` is the next active change in the shared-platform dependency order. Implement
only that change in the next cycle.

## Completed: platform-ai-provider-abstractions

- Added `Platform.Ai.Contracts` with provider-neutral text generation, streaming,
  structured-output, embeddings, usage, cost, capabilities, policy, telemetry, and safe
  failure contracts.
- Added `Platform.Ai` policy-gated generation with single/feature routing, cancellation and
  timeout handling, usage sinks, safe logging, and explicit unsupported-capability results.
- Added raw-HTTP `Platform.Ai.OpenAiCompatible`, `Platform.Ai.Anthropic`, and
  `Platform.Ai.Ollama` adapters, including DeepSeek-compatible configuration, plus deterministic
  `Platform.Ai.Testing` fakes and usage recorders.
- Added adapter, policy, capability, redaction, usage, and architecture coverage and documented
  application-owned prompts, schemas, model choices, data handling, and local mode.
- Archived the change at
  `openspec/changes/archive/2026-09-08-platform-ai-provider-abstractions/` and synchronized
  `openspec/specs/platform-ai-provider-abstractions/spec.md`.

Verification evidence:

- `/home/paul/.dotnet/dotnet restore Platform.sln --ignore-failed-sources -p:NuGetAudit=false --nologo -m:1` — succeeded.
- `/home/paul/.dotnet/dotnet build Platform.sln -c Release --no-restore --nologo -m:1` — 0 warnings, 0 errors.
- `/home/paul/.dotnet/dotnet test Platform.sln -c Release --no-build --no-restore --nologo -m:1` — 484 tests passed, 0 failed, 0 skipped.
- `/home/paul/.dotnet/dotnet pack Platform.sln -c Release --no-build --no-restore --nologo -m:1` — AI packages produced successfully; the existing non-packable sample warning remains.
- Scoped AI `dotnet format --verify-no-changes --no-restore` checks — clean.
- `openspec validate --changes --strict --no-interactive` — 1 passed, 0 failed before archive; `openspec validate --specs --strict --no-interactive` — 19 passed, 0 failed after archive.
- `git diff --check` — clean for the completed change.
- Repository-wide format still reports pre-existing findings in `Platform.AspNetCore/DependencyInjection/ServiceCollectionExtensions.cs`, `tests/Platform.Identity.Tests/IdentityAspNetCoreTests.cs`, and the known `Platform.Testing.Tests` xUnit2013 warning; none were changed.
- Implementation commit: `ffb0232` (`Implement AI provider abstractions`).

## Next change

`openspec list` is empty. The next cycle starts with a fresh OpenSpec proposal; the earlier
`platform-application-starter` handoff entry was stale because that change was not active in the
repository at selection time.

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
