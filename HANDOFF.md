# Handoff

## Current state

Four of the five Phase 3 pilot-adoption OpenSpec changes are implemented and archived (`platform-extraction-jobs`, `platform-extraction-mailing`, `platform-extraction-eventing`, `platform-extraction-idempotency`). The repository ships eight production packages (`Platform.Core`, `Platform.AspNetCore`, `Platform.Billing.Contracts`, `Platform.Jobs`, `Platform.Mailing`, `Platform.Eventing`, `Platform.Idempotency`) and one test-only package (`Platform.Testing`). The architecture guardrails ensure production projects do not reference the test package, the test package does not embed xUnit, NUnit, or a mocking framework, and the new framework-neutral packages do not reference ASP.NET Core, EF Core, scheduling engines, mail providers, templating engines, RabbitMQ, StackExchange.Redis, or VisualFlow projects.

## Next change

Select the next active change with `openspec list`. The one remaining candidate is:

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

## Completed: platform-extraction-idempotency

- Added `Platform.Idempotency` (`net8.0`, version `0.1.0`):
  `IdempotencyRecord`, `IIdempotencyStore`,
  `InMemoryIdempotencyStore`, `RequestFingerprint`,
  `IdempotencyOptions`, `IdempotencyMetrics`, and
  `Platform.Idempotency.DependencyInjection.ServiceCollectionExtensions.AddPlatformIdempotency`.
- `IdempotencyRecord` is a sealed record carrying `Key`,
  `Fingerprint`, `StatusCode`, `ContentType`,
  `ResponseHeaders`, `ResponseBody`, and `CreatedAt`. A
  convenience constructor without `ResponseHeaders` defaults the
  header dictionary to an empty ordinal-case-insensitive map.
- `IIdempotencyStore` exposes `TryGetAsync(key, ct)`,
  `SaveAsync(record, ct)`, and `EvictExpiredAsync(ct)`. The
  `SaveAsync` contract rejects keys longer than the configured
  `MaxKeyLength` with `ArgumentException`.
- `InMemoryIdempotencyStore` is a thread-safe default backed by a
  `Dictionary<string, IdempotencyRecord>` guarded by a lock. It
  consumes `IClock` for retention and key-staleness checks (no
  `DateTimeOffset.UtcNow` call), honours the documented
  `RetentionSeconds` window, and treats expired records as misses
  on lookup.
- `RequestFingerprint.Compute(method, route, bodyHash)` produces a
  stable, hex-encoded SHA-256 fingerprint of the normalised
  request. The method is upper-cased before hashing. Two
  `ComputeBodyHash` overloads (string and `ReadOnlySpan<byte>`)
  produce the matching `bodyHash` input.
- `IdempotencyOptions` carries the documented defaults
  (`Enabled = true`, `Storage = "memory"`,
  `RetentionSeconds = 86400`, `MaxKeyLength = 256`,
  `HeaderName = "Idempotency-Key"`) and the documented
  metric-name constants (`idempotency.hit`, `.miss`,
  `.fingerprint_mismatch`).
- `IdempotencyMetrics` is the documented counter surface
  (preserved verbatim so existing dashboards keep working). The
  package does NOT ship a default implementation; consumers wire
  their own.
- `AddPlatformIdempotency(IServiceCollection)` and the
  `Action<IdempotencyOptions>` overload register the options
  pipeline and `IIdempotencyStore`. When `IdempotencyOptions.Enabled`
  is `false` the registration resolves a no-op store so consumers
  can opt out without changing call sites.
- Package depends on `Platform.Core` and the two
  `Microsoft.Extensions.*` abstractions; no ASP.NET Core, EF Core,
  StackExchange.Redis, or VisualFlow references.
- Extended `Platform.Architecture.Tests`:
  - `Platform_Idempotency_does_not_reference_forbidden_packages` —
    fails on any `Microsoft.AspNetCore`,
    `Microsoft.EntityFrameworkCore`, or `StackExchange.Redis`
    reference.
  - `Platform_Idempotency_only_references_Platform_Core` — fails on
    any project reference other than `Platform.Core`.
  - `Platform_Idempotency_does_not_reference_visual_flow_projects` —
    fails on any project reference whose path contains
    `VisualFlow`.
- `Platform.Idempotency.Tests` (33 tests) covers the assembly
  marker, `IdempotencyOptions` defaults and metric-name constants,
  `RequestFingerprint` (stability, method normalisation, body-hash
  helper, input validation), `InMemoryIdempotencyStore`
  (round-trip, null/empty key rejection, oversize-key rejection,
  retention sweep returns the documented count, expired record
  treated as miss, null dependency guards), and a `TestServer`
  integration test that exercises the documented defaults, a
  configuration override, the in-memory round-trip, the eviction
  sweep driven by a `MutableClock`, and the no-op path when
  `Idempotency:Enabled = false`.

## Verification evidence

- `dotnet restore Platform.sln` — clean.
- `dotnet build Platform.sln -c Release --no-restore --nologo` — 0
  warnings, 0 errors (the pre-existing xUnit2013 warning in
  `Platform.Testing.Tests` is not in this change).
- `dotnet test Platform.sln -c Release --no-build --nologo` — 314
  tests passed (28 Core, 49 Billing.Contracts, 36 Testing, 22
  AspNetCore, 33 Jobs, 40 Mailing, 30 Eventing, 33 Idempotency,
  43 Architecture), 0 failed, 0 skipped.
- `dotnet pack src/Platform.Idempotency/Platform.Idempotency.csproj
  -c Release --no-build --nologo` — produced
  `Platform.Idempotency.0.1.0.nupkg`; inspected `.nuspec` and
  confirmed `<dependencies>` contains only `Platform.Core`,
  `Microsoft.Extensions.DependencyInjection.Abstractions`, and
  `Microsoft.Extensions.Options`.
- Production isolation: existing architecture tests confirm no
  production project gains a forbidden reference, and the new
  `Platform.Idempotency` tests confirm its `Platform.Core`-only
  project reference and the absence of ASP.NET Core, EF Core,
  StackExchange.Redis, or VisualFlow references.
- `git diff --check` — clean.
- `openspec validate --changes --strict --no-interactive` — 1
  passed, 0 failed.
- `openspec validate --specs --strict --no-interactive` — 9
  passed, 0 failed.
- `openspec list` — 1 active change (the jobs, mailing, eventing,
  and idempotency changes are archived).

## Completed earlier: platform-extraction-eventing

- `Platform.Eventing` (`net8.0`, version `0.1.0`): `IIntegrationEvent`,
  `IntegrationEvent`, `IntegrationEventEnvelope`, the default
  `IntegrationEventEnvelopeSerializer` / Deserializer, `IEventBus`,
  `IIntegrationEventHandler<TEvent>`, `InProcessEventBus` (bounded
  `Channel<T>`, consumes `IClock`, idempotent disposal),
  `EventingOptions`, `AddPlatformEventing`, and
  `AddPlatformEventingInProcess`. 30 unit + TestServer tests; three
  new architecture guardrails.

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
