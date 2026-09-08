# .NET Platform Libraries

Small, privately maintained .NET libraries for the shared technical concerns repeated across the local application portfolio.

## Purpose

This repository is a platform library, not a replacement for every application's domain model and not a fork of the FullStackHero starter kit. It provides stable contracts and opt-in infrastructure that individual applications can adopt at their own pace.

## Packages

| Package | Kind | Depends on | Purpose |
| --- | --- | --- | --- |
| `Platform.Core` | Production | (none) | Framework-independent contracts: `IClock`, `Error`, `Result`, `Result<T>`, `CallerContext`, `IAuditable`. |
| `Platform.AspNetCore` | Production | `Platform.Core` | ASP.NET Core integration: explicit registration extensions, sanitized `ProblemDetails` mapping, correlation middleware/accessor, and health-check helpers. |
| `Platform.Billing.Contracts` | Production | (none) | Provider-neutral subscription and entitlement contracts: opaque identifiers, normalized subscription and entitlement snapshots, structured feature-check decisions, replaceable usage-meter interface, and idempotent processed-event store. |
| `Platform.Jobs` | Production | `Platform.Core` | Engine-neutral scheduling contract: `IJobDispatcher`, `IRecurringJobHandler`, `IRecurringJobRegistry`, `IJobTelemetry`, `RecurringJobAttribute`, `RecurringJobDescriptor`, `BackgroundJobsOptions`, and `AddPlatformJobs` opt-in registration. |
| `Platform.Mailing` | Production | `Platform.Core` | Provider-neutral mailing contract: `MailAddress`, `MailAttachment`, `MailMessage`, `MailSendOutcome`, `MailSendResult`, `IMailService`, `MailTemplateId`, `IMailTemplateRenderer<TModel>`, `RenderedMailTemplate`, `MailingOptions`, and `AddPlatformMailing` opt-in registration. |
| `Platform.Eventing` | Production | `Platform.Core` | Transport-agnostic event-bus contract: `IIntegrationEvent`, `IntegrationEvent`, `IntegrationEventEnvelope`, default `System.Text.Json` serializer/deserializer, `IIntegrationEventHandler<TEvent>`, `IEventBus`, `InProcessEventBus` (bounded `Channel<T>`), `EventingOptions`, and `AddPlatformEventing` / `AddPlatformEventingInProcess` opt-in registration. |
| `Platform.Eventing.Contracts` | Production | `Platform.Core` | Framework-neutral durable eventing contracts and in-memory outbox/inbox stores. |
| `Platform.Eventing.EfCore` | Optional production adapter | `Platform.Eventing.Contracts`, `Platform.Core` | Application-owned EF Core outbox/inbox mappings, stores, and hosted outbox dispatcher. |
| `Platform.Caching` | Production | `Platform.Core` | Provider-neutral async cache contracts, validated tenant/application keys, redaction-safe telemetry, and thread-safe in-memory store. |
| `Platform.Caching.Hybrid` | Optional production adapter | `Platform.Caching` | Microsoft HybridCache adapter for local or single-host deployments. |
| `Platform.Caching.Redis` | Optional production adapter | `Platform.Caching` | StackExchange.Redis adapter with application-owned serialization, bounded operations, tags, and provider health. |
| `Platform.Storage` | Production | `Platform.Core` | Provider-neutral object storage contracts, safe keys, limits, presigned operations, and provider status. |
| `Platform.Storage.Local` | Optional production adapter | `Platform.Storage` | Atomic local filesystem object storage for development and single-host deployments. |
| `Platform.Storage.S3` | Optional production adapter | `Platform.Storage` | AWS/S3-compatible storage adapter with application-provided client, presigning, timeout, and failure classification. |
| `Platform.Quota` | Production | `Platform.Core` | Provider-neutral quota checks, atomic reservation lifecycle, explanations, and in-memory store. |
| `Platform.Quota.Testing` | Test/support | `Platform.Quota` | Deterministic quota scenarios and reservation inspection helpers. |
| `Platform.Idempotency` | Production | `Platform.Core` | Framework-neutral idempotency contract: `IdempotencyRecord`, `IIdempotencyStore`, `InMemoryIdempotencyStore`, `RequestFingerprint` (stable SHA-256 over method/route/body-hash), `IdempotencyOptions` (with documented metric-name constants), `IdempotencyMetrics`, and `AddPlatformIdempotency` opt-in registration (no-op when `Enabled` is `false`). |
| `Platform.RateLimiting` | Production | `Platform.Core` | Framework-neutral rate-limit contract: `IRateLimiter`, `InMemoryRateLimiter` (per-key windowed counter), `RateLimitDecision`, `RateLimitKey`, `RateLimitPolicies` (documented default catalog), `IRateLimitBypassResolver`, `IRateLimiterBackendStatusProvider`, `RateLimitingOptions`, and `AddPlatformRateLimiting` opt-in registration. |
| `Platform.Testing` | Test-only | `Platform.Core`, `Platform.AspNetCore`, `Platform.Billing.Contracts` | Deterministic test doubles: `ControllableClock`, `SubscriptionBuilder`, `EntitlementBuilder`, `FakeEntitlementStore`, `RecordingUsageMeter`. Production projects must not reference this package. |
| `Platform.Billing.Stripe` | Optional production adapter | `Platform.Billing` | Raw-HTTP Stripe checkout, portal, subscription lookup, webhook verification/normalization, and status. |
| `Platform.Billing.LemonSqueezy` | Optional production adapter | `Platform.Billing` | Raw-HTTP Lemon Squeezy checkout, subscription lookup, webhook verification/normalization, and status. |

Product-specific EF Core entities, migrations, Stripe price IDs, invoice rules, plan names, and business workflows remain in consuming applications.

## Repository layout

```
src/
  Platform.Core/                Framework-independent contracts
  Platform.AspNetCore/          ASP.NET Core integration
  Platform.Billing.Contracts/   Subscription and entitlement contracts
  Platform.Testing/             Test-only helpers
tests/
  Platform.Core.Tests/          Unit tests for Platform.Core
  Platform.AspNetCore.Tests/    Unit + TestServer integration tests
  Platform.Billing.Contracts.Tests/
  Platform.Testing.Tests/
  Platform.Architecture.Tests/  Dependency-direction and isolation guardrails
  docs/
  build-test-pack.md            Restore, build, test, pack, and validate commands
  packages.md                   Per-package contract reference
  platform-eventing-durable.md  Durable eventing adoption and ownership guidance
  platform-caching.md           Cache authority, key/version, failure, and adoption guidance
  platform-storage.md           Object storage ownership, safety, limits, and migration guidance
  platform-quota.md             Quota lifecycle, concurrency, and application ownership guidance
openspec/
  changes/archive/              Archived proposals
  specs/                        Generated capability specs
Directory.Build.props           Shared MSBuild defaults and packaging metadata
Directory.Packages.props        Central package version management
HANDOFF.md                      Most recent change completion and next action
ROADMAP.md                      Phased delivery plan and current status
```

## Quickstart

```bash
dotnet restore Platform.sln
dotnet build  Platform.sln -c Release
dotnet test   Platform.sln -c Release --nologo
dotnet pack   Platform.sln -c Release --no-build --nologo
```

The solution builds, tests, and packs under .NET 8. NuGet packages are written to each project's `bin/Release/` directory. See [`docs/build-test-pack.md`](docs/build-test-pack.md) for the full set of commands and [`docs/packages.md`](docs/packages.md) for the per-package contract reference.

## Conventions

- Target frameworks: production libraries target `net8.0` first; `.NET 10` targeting is added only when a concrete package needs it.
- Package versions are managed centrally in `Directory.Packages.props`. Project files declare `<PackageReference Include="..." />` without a `Version` attribute.
- Nullable reference types, implicit usings, deterministic builds, and warnings-as-errors are enabled in `Directory.Build.props` for production code.
- Test projects opt out of packaging via `tests/Directory.Build.props`.
- `Platform.Core` and `Platform.Billing.Contracts` must not reference ASP.NET Core, EF Core, Stripe SDKs, or application projects. `Platform.Architecture.Tests` enforces this and the related dependency-direction rules.
- `Platform.Testing` is a test-only package; production projects must not reference it (enforced by `Platform.Architecture.Tests`).

## Delivery workflow

1. Select one active change with `openspec list`.
2. Implement only that change and its tests.
3. Update its `tasks.md` as tasks complete.
4. Verify with relevant tests and strict OpenSpec validation.
5. Archive the completed change.
6. Commit 1: implementation, tests, archive, and related generated specs only.
7. Update `HANDOFF.md` with completion evidence and the next change.
8. Commit 2: only the `HANDOFF.md` update.
9. Stop; do not start another change or push.

Incomplete or blocked work must not be reported as complete. The handoff must record the exact failed command and next action.

## Current status

All 20 OpenSpec changes in `ROADMAP.md` are implemented and archived. The repository contains 30 production/adaptor projects and six testing-support projects, including provider-neutral AI contracts and optional raw-HTTP adapters for OpenAI-compatible/DeepSeek, Anthropic, and Ollama providers. `Platform.Testing` and the other testing-support packages are not referenced by production projects; `Platform.Architecture.Tests` enforces the dependency direction. The repository has 484 passing tests across 22 test projects, 36 source projects, and 19 generated capability specs. `openspec list` is empty; the next work starts with a fresh OpenSpec proposal. See [`docs/platform-ai.md`](docs/platform-ai.md), `HANDOFF.md`, and `ROADMAP.md` for package, completion, and planning details.
