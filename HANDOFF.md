# Handoff

## Current state

Foundation, core contracts, and ASP.NET Core integration are in place. `Platform.AspNetCore` exposes opt-in `AddPlatformAspNetCore`, `UsePlatformAspNetCore`, and `MapPlatformEndpoints` helpers; maps known platform failures to sanitized `ProblemDetails`; provides correlation IDs and liveness health checks. The package depends on `Platform.Core` and `Microsoft.AspNetCore.App` only. Three active changes remain in `openspec/changes/`.

## Next change

Run `openspec list`, select `platform-entitlement-contracts`, and implement only that change.

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

## Completed: platform-aspnetcore-foundation

- Added `ServiceCollectionExtensions`: explicit `AddPlatformAspNetCore`
  registers `IClock`, `IProblemDetailsMapper`, `IHttpContextAccessor`,
  `ICorrelationAccessor`, and `PlatformAspNetCoreOptions`.
  `UsePlatformAspNetCore` (and the split
  `UsePlatformCorrelation` / `UsePlatformProblemDetails`) and
  `MapPlatformEndpoints` are explicit pipeline helpers with no hidden
  global registration.
- Added `PlatformProblemDetailsMapper` mapping known codes
  (`platform.validation` → 400, `platform.not_found` → 404, unknown →
  500) to a sanitized `ProblemDetails` with a stable type URI and the
  error code carried in `Extensions["code"]`.
- Added `PlatformProblemException` so application code can surface a
  known platform failure to the boundary.
- Added `ProblemDetailsExceptionMiddleware`: maps
  `PlatformProblemException` to `ProblemDetails`; logs and
  sanitizes unknown exceptions to a generic 500 with no internal
  details exposed to the client.
- Added `CorrelationMiddleware` and `ICorrelationAccessor`
  (`HttpCorrelationAccessor`): generates a correlation id (or accepts
  the configured header only when the host opts in via
  `AcceptIncomingCorrelationHeader`).
- Added `HealthCheckEndpointExtensions`: `AddPlatformHealthChecks`
  registers a `platform.liveness` check tagged `live`;
  `MapPlatformHealthEndpoint` exposes a plain-text health endpoint
  at `PlatformAspNetCoreOptions.HealthCheckPath` (default `/health`).
- Added `Platform.AspNetCore` to the central package version catalog
  via `Microsoft.AspNetCore.Mvc.Testing 8.0.10` for the integration
  tests; production project has zero `<PackageReference>` entries
  (only the `Microsoft.AspNetCore.App` `FrameworkReference`).
- Extended `Platform.Architecture.Tests` with a `FrameworkReference`
  rule: only `Platform.AspNetCore` may declare `FrameworkReference`,
  and only `Microsoft.AspNetCore.App`.

## Verification evidence

- `dotnet build Platform.sln -c Release` — 0 warnings, 0 errors.
- `dotnet test Platform.sln -c Release --nologo` — 71 tests passed
  (28 Core, 22 AspNetCore, 19 Architecture, 1 Billing.Contracts, 1
  Testing), 0 failed, 0 skipped.
- `dotnet pack src/Platform.AspNetCore/Platform.AspNetCore.csproj -c
  Release --no-build --nologo` — produced
  `Platform.AspNetCore.0.1.0.nupkg`; inspected `.nuspec` and confirmed
  `<dependencies>` contains only `Platform.Core 0.1.0` and
  `<frameworkReferences>` contains only `Microsoft.AspNetCore.App`.
- `git diff --check` — clean.
- `openspec validate --changes --strict --no-interactive` — 3 passed,
  0 failed.
- `openspec validate --specs   --strict --no-interactive` — 3 passed,
  0 failed.
- `openspec list` — `platform-aspnetcore-foundation` no longer
  present; 3 active changes remain.
