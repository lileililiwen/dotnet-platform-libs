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

All ten OpenSpec changes from `ROADMAP.md` Phases 1, 2, and 3 are implemented and archived. The repository ships eight production packages (`Platform.Core`, `Platform.AspNetCore`, `Platform.Billing.Contracts`, `Platform.Jobs`, `Platform.Mailing`, `Platform.Eventing`, `Platform.Idempotency`, `Platform.RateLimiting`) and one test-only package (`Platform.Testing`). The five Phase 3 pilot-adoption changes — `platform-extraction-ratelimiting`, `platform-extraction-mailing`, `platform-extraction-eventing`, `platform-extraction-idempotency`, and `platform-extraction-jobs` — each shipped a framework-neutral package and proved that VisualFlow's `BuildingBlocks` types can move into the platform without re-introducing ASP.NET Core, EF Core, scheduling engines, mail providers, templating engines, RabbitMQ, or StackExchange.Redis. `Platform.Testing` exists; production projects do not reference it (enforced by `Platform.Architecture.Tests`). The repository has 354 passing tests and ten packable NuGet packages. `openspec list` is empty; the next work, if any, starts with a fresh OpenSpec proposal. See `HANDOFF.md` for the latest completion evidence and `ROADMAP.md` for the phased plan.
