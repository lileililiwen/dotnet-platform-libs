# Handoff

## Current state

All five OpenSpec changes are implemented and archived. The repository ships four production packages (`Platform.Core`, `Platform.AspNetCore`, `Platform.Billing.Contracts`) and one test-only package (`Platform.Testing`). The architecture guardrails ensure production projects do not reference the test package, and the test package does not embed xUnit, NUnit, or a mocking framework. The pilot adoption change (`pilot-adoption-singleatee`) was removed from the change folder before this handoff and remains in `git status` as a pre-existing deletion that is not part of this work.

## Next change

No active changes remain. `openspec list` is empty; the next work, if any, starts with a fresh OpenSpec proposal.

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

## Completed: platform-testing-toolkit

- Added `ControllableClock` (`Platform.Testing.Time`): deterministic
  `IClock` that never reads system time, with `Set` and `Advance`
  (including negative durations) under a lock; rejects non-UTC
  values.
- Added `SubscriptionBuilder` and `EntitlementBuilder`
  (`Platform.Testing.Entitlements`): fluent builders for
  `Subscription` and `Entitlement` snapshots with safe inactive
  defaults; the entitlement builder exposes
  `Granting`/`WithLimit`/`CapturedAt` helpers and anchors the
  captured-at time at the supplied `IClock`.
- Added `FakeEntitlementStore`: inspectable in-memory store with
  `Configure`, `Get`, `Invalidate`, `InvalidatedSubjects`, and
  `Reset`; the store is framework-neutral and exposes its state for
  tests to assert.
- Added `RecordingUsageMeter` (`Platform.Testing.Usage`):
  `IUsageMeter` implementation that records every call (subject,
  feature, units, operation kind) and accumulates totals per
  (subject, feature); `SetLimit` configures per-feature limits that
  flow through `UsageCheckResult.Limit`; `Reset` clears totals and
  calls but preserves limits.
- Added `UsageCall` record for inspecting recorded calls in
  invocation order.
- The test package has zero xUnit, NUnit, or mocking-framework
  dependencies; it depends on the three platform contracts only.

## Verification evidence

- `dotnet build Platform.sln -c Release` — 0 warnings, 0 errors.
- `dotnet test Platform.sln -c Release --nologo` — 154 tests passed
  (49 Billing.Contracts, 36 Testing, 28 Core, 22 AspNetCore, 19
  Architecture), 0 failed, 0 skipped.
- `dotnet pack src/Platform.Testing/Platform.Testing.csproj -c
  Release --no-build --nologo` — produced
  `Platform.Testing.0.1.0.nupkg`; inspected `.nuspec` and confirmed
  `<dependencies>` contains only the three platform contract
  packages (`Platform.Core`, `Platform.AspNetCore`,
  `Platform.Billing.Contracts`).
- Production isolation: `Platform.Core`,
  `Platform.Billing.Contracts`, and `Platform.AspNetCore` declare no
  `<PackageReference>` or `<ProjectReference>` to
  `Platform.Testing` (enforced by
  `Platform.Architecture.Tests`).
- `git diff --check` — clean.
- `openspec validate --changes --strict --no-interactive` — 0 items
  (no active changes remain); 0 failed.
- `openspec validate --specs --strict --no-interactive` — 5 passed,
  0 failed.
- `openspec list` — empty.
