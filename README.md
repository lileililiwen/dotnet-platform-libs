# .NET Platform Libraries

Small, privately maintained .NET libraries for the shared technical concerns repeated across the local application portfolio.

## Purpose

This repository is a platform library, not a replacement for every application's domain model and not a fork of the FullStackHero starter kit. It provides stable contracts and opt-in infrastructure that individual applications can adopt at their own pace.

## Initial scope

- `Platform.Core` — framework-independent shared contracts.
- `Platform.AspNetCore` — ASP.NET Core integration helpers built on `Platform.Core`.
- `Platform.Billing.Contracts` — normalized subscription and entitlement contracts.
- `Platform.Testing` — reusable testing helpers (test-only; production projects must not reference it).

Product-specific EF Core entities, migrations, Stripe price IDs, invoice rules, plan names, and business workflows remain in consuming applications.

## Repository layout

```
src/                     Production libraries (packable, independently versioned)
  Platform.Core/         Framework-independent contracts
  Platform.AspNetCore/   ASP.NET Core integration
  Platform.Billing.Contracts/
  Platform.Testing/      Test-only helpers
tests/                   Focused test projects + dependency-direction tests
docs/                    Build, test, and pack documentation
openspec/                OpenSpec change proposals and capability specs
Directory.Build.props    Shared MSBuild defaults and packaging metadata
Directory.Packages.props Central package version management
```

## Quickstart

```bash
dotnet restore Platform.sln
dotnet build  Platform.sln -c Release
dotnet test   Platform.sln -c Release --nologo
dotnet pack   Platform.sln -c Release --no-build --nologo
```

The solution builds, tests, and packs under .NET 8. NuGet packages are written to each project's `bin/Release/` directory.

## Conventions

- Target frameworks: production libraries target `net8.0` first; `.NET 10` targeting is added only when a concrete package needs it.
- Package versions are managed centrally in `Directory.Packages.props`. Project files declare `<PackageReference Include="..." />` without a `Version` attribute.
- Nullable reference types, implicit usings, deterministic builds, and warnings-as-errors are enabled in `Directory.Build.props` for production code.
- Test projects opt out of packaging via `tests/Directory.Build.props`.
- `Platform.Core` and `Platform.Billing.Contracts` must not reference ASP.NET Core, EF Core, Stripe SDKs, or application projects. `Platform.Architecture.Tests` enforces this and other dependency-direction rules.

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

The foundation change (`platform-repository-foundation`) is implemented. The next target is `platform-core-contracts`, followed by `platform-aspnetcore-foundation`.
