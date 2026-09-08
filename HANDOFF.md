# Handoff

## Current state

The five Phase 3 pilot-adoption OpenSpec changes are in `openspec/changes/`. The first change (`2026-09-08-platform-extraction-jobs`) is implemented and archived. The repository ships five production packages (`Platform.Core`, `Platform.AspNetCore`, `Platform.Billing.Contracts`, `Platform.Jobs`) and one test-only package (`Platform.Testing`). The architecture guardrails ensure production projects do not reference the test package, the test package does not embed xUnit, NUnit, or a mocking framework, and `Platform.Jobs` does not reference ASP.NET Core, EF Core, Hangfire, Quartz, or a VisualFlow project.

## Next change

Select the next active change with `openspec list`. The four remaining candidates are:

- `2026-09-08-platform-extraction-mailing` (mailing sender contracts)
- `2026-09-08-platform-extraction-eventing` (eventing contracts)
- `2026-09-08-platform-extraction-idempotency` (idempotency store contracts)
- `2026-09-08-platform-extraction-ratelimiting` (rate-limiting contracts)

Run the same one-change-at-a-time sequence as below.

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

## Completed: platform-extraction-jobs

- Added `Platform.Jobs` (`net8.0`, version `0.1.0`):
  `IJobDispatcher`, `IRecurringJobHandler`, `IRecurringJobRegistry`,
  `IJobTelemetry`, `JobPayload`, `RecurringJobAttribute`,
  `RecurringJobDescriptor`, `BackgroundJobsOptions`, and
  `Platform.Jobs.DependencyInjection.ServiceCollectionExtensions.AddPlatformJobs`.
- `RecurringJobAttribute` is a runtime attribute (target: `Class`,
  `AllowMultiple = false`, `Inherited = false`) with a required
  `Cron` constructor argument and optional `Name`, `TimeZone`, and
  `Options`. The static helper
  `RecurringJobAttribute.GetDescriptor(Type)` returns a
  `RecurringJobDescriptor` for a decorated handler and throws
  `InvalidOperationException` for an undecorated one.
- `BackgroundJobsOptions` exposes `DefaultTimeZone` (default `UTC`)
  and the `SectionName` constant `"BackgroundJobs"`. The package
  does NOT register default implementations of `IJobDispatcher`,
  `IRecurringJobRegistry`, or `IJobTelemetry` (per spec). Consumers
  provide their own scheduler, registry, and telemetry.
- `AddPlatformJobs(IServiceCollection)` and
  `AddPlatformJobs(IServiceCollection, Action<BackgroundJobsOptions>)`
  bind `BackgroundJobsOptions` through the `IOptions<>` pipeline and
  register a `SystemClock` only when no `IClock` is already present.
- Package depends on `Platform.Core` and the two
  `Microsoft.Extensions.*` abstractions; no ASP.NET Core, EF Core,
  Hangfire, Quartz, or VisualFlow references.
- Extended `Platform.Architecture.Tests`:
  - `Platform.Jobs_does_not_reference_forbidden_packages` — fails
    on any `Microsoft.AspNetCore`, `Microsoft.EntityFrameworkCore`,
    `Hangfire`, or `Quartz` reference.
  - `Platform_Jobs_only_references_Platform_Core` — fails on any
    project reference other than `Platform.Core`.
  - `Platform_Jobs_does_not_reference_visual_flow_projects` — fails
    on any project reference whose path contains `VisualFlow`.
- `Platform.Jobs.Tests` (33 tests) covers the assembly marker, the
  `RecurringJobAttribute` reflection (decorated, undecorated, null
  handler, empty cron), `RecurringJobDescriptor` construction and
  `With*` helpers, `JobPayload.Create` validation, the
  `IJobTelemetry` surface (registry and dispatcher both call
  telemetry with the `IClock`-sourced timestamp), the
  `BackgroundJobsOptions` defaults, the `AddPlatformJobs`
  registration (defaults, configuration overrides, null guards, clock
  preservation, no default dispatcher/registry/telemetry), and a
  `TestServer` integration test that proves the documented defaults
  and a configuration override are observable through a full
  `WebApplication` host.

## Verification evidence

- `dotnet restore Platform.sln` — clean.
- `dotnet build Platform.sln -c Release --no-restore --nologo` — 0
  warnings, 0 errors (the pre-existing xUnit2013 warning in
  `Platform.Testing.Tests` is not in this change).
- `dotnet test Platform.sln -c Release --no-build --nologo` — 193
  tests passed (28 Core, 49 Billing.Contracts, 36 Testing, 22
  AspNetCore, 25 Architecture, 33 Jobs), 0 failed, 0 skipped.
- `dotnet pack src/Platform.Jobs/Platform.Jobs.csproj -c Release
  --no-build --nologo` — produced
  `Platform.Jobs.0.1.0.nupkg`; inspected `.nuspec` and confirmed
  `<dependencies>` contains only `Platform.Core`,
  `Microsoft.Extensions.DependencyInjection.Abstractions`, and
  `Microsoft.Extensions.Options`.
- Production isolation: `Platform.Core`,
  `Platform.Billing.Contracts`, and `Platform.AspNetCore` declare no
  `<PackageReference>` or `<ProjectReference>` to `Platform.Jobs`
  (existing architecture test), and `Platform.Jobs` declares no
  forbidden reference (new architecture tests).
- `git diff --check` — clean.
- `openspec validate --changes --strict --no-interactive` — 4
  passed, 0 failed.
- `openspec validate --specs --strict --no-interactive` — 6
  passed, 0 failed.
- `openspec list` — 4 active changes (the jobs change is archived).
