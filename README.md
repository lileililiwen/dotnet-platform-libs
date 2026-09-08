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
| `Platform.Testing` | Test-only | `Platform.Core`, `Platform.AspNetCore`, `Platform.Billing.Contracts` | Deterministic test doubles: `ControllableClock`, `SubscriptionBuilder`, `EntitlementBuilder`, `FakeEntitlementStore`, `RecordingUsageMeter`. Production projects must not reference this package. |

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

All five OpenSpec changes from ROADMAP Phases 1 and 2 are implemented and archived. `Platform.Core`, `Platform.AspNetCore`, `Platform.Billing.Contracts`, and `Platform.Testing` ship with their public contracts, tests, and architecture guardrails. The repository has 154 passing tests and four packable NuGet packages. Phase 3 (the pilot adoption proposal) is not currently in the change folder and is not represented in `openspec list`; the work, if pursued, starts with a fresh OpenSpec proposal. See `HANDOFF.md` for the latest completion evidence and `ROADMAP.md` for the phased plan.
