# Handoff

## Current state

Foundation, core contracts, ASP.NET Core integration, and entitlement contracts are in place. `Platform.Billing.Contracts` exposes provider-neutral identifiers, subscription and entitlement snapshots, structured feature-check decisions, a replaceable usage-meter interface, and an idempotent processed-event store. The package ships with zero third-party dependencies. One active change remains in `openspec/changes/`.

## Next change

Run `openspec list`, select `platform-testing-toolkit`, and implement only that change.

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

## Completed: platform-entitlement-contracts

- Added opaque identifiers in
  `Platform.Billing.Contracts.Identifiers`: `PlanId`, `FeatureKey`,
  `SubjectKey` (with `Anonymous` marker and `IsAnonymous`),
  `ProviderName`, `ProviderEventId` — all with non-empty
  validation.
- Added `Platform.Billing.Contracts.Subscriptions`:
  `SubscriptionStatus` enum (`Free`, `Active`, `Suspended`,
  `PastDue`, `Canceled`, `Unknown`) and immutable `Subscription`
  record with `IsActive` (only `Free` and `Active` count as active)
  and `IsWithinPeriod(now)`.
- Added `Platform.Billing.Contracts.Entitlements`: immutable
  `Entitlement` record (Subject, optional Tenant, optional
  Subscription, `IReadOnlySet<FeatureKey>`, optional Limits,
  CapturedAt) with `Grants`/`LimitFor` helpers; static
  `EntitlementDefaults` factory for `Anonymous`, `Inactive`, and
  `Unknown` (an unknown subscription is preserved on the snapshot
  for diagnostics but grants no features).
- Added `Platform.Billing.Contracts.Features`:
  `FeatureCheckReason` enum (`Allowed`, `NotAuthenticated`,
  `NotSubscribed`, `PlanMismatch`, `LimitExceeded`, `Unknown`),
  `FeatureCheckResult` record (Feature, Reason, optional
  RequiredPlan/CurrentUsage/Limit), and `FeatureCheck.Evaluate`
  mapping an `Entitlement` snapshot into a structured decision.
- Added `Platform.Billing.Contracts.Usage`: `IUsageMeter` interface
  (`CheckAsync`/`RecordAsync` with `CancellationToken`) and
  `UsageCheckResult` with `IsWithinLimit`; the platform does not
  prescribe storage, cache, or counting implementation.
- Added `Platform.Billing.Contracts.Events`: `ProviderEvent`,
  `ProcessedEvent`, `ProcessedEventDecision` enum (`FirstDelivery`
  /`Duplicate`), and `IProcessedEventStore.MarkProcessedAsync` for
  idempotent redelivery handling.

## Verification evidence

- `dotnet build Platform.sln -c Release` — 0 warnings, 0 errors.
- `dotnet test Platform.sln -c Release --nologo` — 119 tests passed
  (49 Billing.Contracts, 28 Core, 22 AspNetCore, 19 Architecture,
  1 Testing), 0 failed, 0 skipped.
- `dotnet pack src/Platform.Billing.Contracts/Platform.Billing.Contracts.csproj
  -c Release --no-build --nologo` — produced
  `Platform.Billing.Contracts.0.1.0.nupkg`; inspected `.nuspec`
  and confirmed `<dependencies>` is empty for `net8.0` (zero
  third-party package dependencies).
- Public API inspection (grep across `src/Platform.Billing.Contracts`):
  no `Microsoft.AspNetCore`, `Microsoft.EntityFrameworkCore`,
  `Stripe`, or `System.Web` references.
- `git diff --check` — clean.
- `openspec validate --changes --strict --no-interactive` — 1 passed,
  0 failed.
- `openspec validate --specs   --strict --no-interactive` — 4 passed,
  0 failed.
- `openspec list` — `platform-entitlement-contracts` no longer
  present; 1 active change remains.
