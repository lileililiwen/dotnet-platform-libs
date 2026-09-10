# Handoff

## Completed: platform-identity-lifecycle-contracts

- Extended `Platform.Identity.Contracts` (framework-neutral, no
  third-party packages) with lifecycle contracts and the
  `IIdentityLifecycleCoordinator` composition:
  - `IdentityLifecycleOutcome`, `IdentityLifecycleResult<T>`, and
    `IdentityLifecycleResults` (stable codes: `Succeeded`,
    `InvalidHandle`, `Expired`, `Revoked`, `Replayed`, `PolicyDenied`,
    `PreconditionNotMet`, `ProviderUnavailable`, `InvalidRequest`,
    `Unknown`).
  - `IRefreshTokenStore` (atomic consume-and-replace), `RefreshToken`,
    `RefreshTokenRotation`, `IRefreshTokenService`,
    `DefaultRefreshTokenService` (delegates to the application store
    and the `IIdentityAuditHook`).
  - `IPasswordRecoveryService`, `PasswordRecoveryChallenge`
    (no-enumeration, replay-rejected, expired-window).
  - `ITwoFactorService`, `TwoFactorChallenge` with channel selection
    and code verification.
  - `IImpersonationPolicy`, `IImpersonationService`,
    `ImpersonationAuthorizationRequest`, `ImpersonationGrant`,
    `ImpersonationContext` (fail-closed without a policy; audited
    start/end).
- Extended `Platform.Identity.Testing` with deterministic, non-production
  fakes: `InMemoryRefreshTokenStore` (linearizable, family-revoked),
  `FakePasswordRecoveryService`, `FakeTwoFactorService`,
  `FakeImpersonationService` (delegates to a swappable
  `IImpersonationPolicy`; defaults to `DenyAllImpersonationPolicy`),
  `AllowImpersonationPolicy`, `DenyAllImpersonationPolicy`, and
  `RecordingIdentityAuditHook`.
- Extended `Platform.Identity.AspNetCore` (depends on
  `Platform.Identity.Contracts` + `Platform.Authorization` + the
  `Microsoft.AspNetCore.App` framework reference) with:
  - `AddPlatformIdentityLifecycle(IServiceCollection)` — composes the
    coordinator and the `DefaultRefreshTokenService`; reports
    `ProviderUnavailable` for any lifecycle contract the application
    has not yet wired.
  - `MapPlatformRefreshTokenRotation` /
    `MapPlatformRefreshTokenRevocation` /
    `MapPlatformPasswordRecoveryInitiation` /
    `MapPlatformPasswordRecoveryCompletion` /
    `MapPlatformTwoFactorChallenge` /
    `MapPlatformTwoFactorVerification` /
    `MapPlatformImpersonationStart` /
    `MapPlatformImpersonationEnd` — minimal-API mappers that
    translate the `IdentityLifecycleOutcome` to ProblemDetails and
    reuse the consumer's authentication scheme and claim projection.
- Tests in `tests/Platform.Identity.Tests` (+27 new, +45 total in the
  project): refresh-token rotation + replay + expiry + revocation,
  32-thread concurrent rotation with a single success, audit-event
  emission; password recovery with no-enumeration, replay rejection,
  invalid-challenge handling; two-factor challenge + verify with wrong
  code, unknown challenge, empty subject; impersonation fail-closed
  default, allow policy grants, active context lookup, end-after-start,
  unknown-grant end, invalid request shape; endpoint integration via
  `TestServer` for refresh rotation, refresh replay, password-recovery
  initiation returning `202` for known and unknown subjects, two-factor
  challenge + verify, and impersonation start failing closed without a
  policy.
- Architecture tests already enforce the production/test boundary and
  the forbidden-package list; existing assertions for
  `Platform.Identity.Contracts`, `Platform.Identity.AspNetCore`, and
  `Platform.Identity.Testing` continue to pass (286 architecture
  tests, 0 failed).
- Docs: `docs/packages.md` adds a "Lifecycle" subsection to
  `Platform.Identity.Contracts`, a "Lifecycle integration" subsection
  to `Platform.Identity.AspNetCore`, and a lifecycle-fakes subsection
  to `Platform.Identity.Testing`. The per-package reference at
  `docs/platform-identity-lifecycle.md` covers adoption, refresh
  rotation, password recovery, two-factor, impersonation, outcome
  codes, security requirements, and starter-kit migration/rollback.
- Archived the change at
  `openspec/changes/archive/2026-09-10-platform-identity-lifecycle-contracts/`
  with synchronized
  `openspec/specs/platform-identity-lifecycle/spec.md` covering
  provider-neutral contracts, atomic refresh rotation, safe failure
  outcomes (no user enumeration), and impersonation that fails closed.

## Verification evidence

- `dotnet build Platform.sln -c Release` — 0 warnings, 0 errors.
- `dotnet test Platform.sln -c Release --no-build --nologo -m:1` —
  all projects green, including the new identity lifecycle tests
  (45 passed, 0 failed) and the unchanged architecture tests (286
  passed, 0 failed).
- `openspec validate --changes --strict --no-interactive` — 3
  passed (after archive; `platform-identity-lifecycle-contracts` is
  removed from the active queue).
- `openspec validate --specs --strict --no-interactive` — 36
  passed (after archive; the new
  `platform-identity-lifecycle` spec is included).
- `git diff --check` — clean for the staged change.
- Implementation commit: recorded in the repository log for the
  `platform-identity-lifecycle-contracts` change.

## Next change

`platform-tenant-lifecycle-contracts` is the next active change
returned by `openspec list` (3 active changes remain). Implement
only that change in the next cycle.

## Completed: platform-web-api-versioning

- Added `Platform.Web.Versioning` (net8.0) with centrally managed
  `Asp.Versioning.Http` and `Asp.Versioning.Mvc.ApiExplorer` references
  (both pinned at 8.1.0 in `Directory.Packages.props`).
- `PlatformWebVersioningOptions` (XML-documented, validated at
  registration via `IValidateOptions<>`) — `DefaultMajor`,
  `DefaultMinor`, `AssumeDefaultVersionWhenUnspecified`,
  `ReportApiVersions`, `RouteConstraintName`, `GroupNameFormat`,
  `Reader`, `HeaderName`, `QueryParameterName`. `Validate()` rejects
  negative versions, missing reader-specific names, and empty
  format/constraint strings.
- `PlatformVersionReaderKind` — `UrlSegment`, `Header`, `QueryString`,
  `MediaType`, `Composite`. The default is `UrlSegment`; the
  configurator selects the matching `IApiVersionReader` from the
  platform options and falls back to `UrlSegmentApiVersionReader`
  when the consumer does not opt in.
- `DependencyInjection/ServiceCollectionExtensions.cs`:
  - `AddPlatformWebVersioning(IServiceCollection)` and the
    `Action<...>` overload — register the platform options, the
    Asp.Versioning services, and the API Explorer services. The
    registration is idempotent through `TryAddSingleton<>` and the
    options `Configure` chain (the last `configure` call wins, which
    is the documented `IOptions<>` behavior).
  - `EnablePlatformApiVersionBinding(IApiVersioningBuilder)` — opt-in
    helper for minimal-API `ApiVersion` parameter binding.
  - `MapPlatformApiExplorerDescriptions(configure)` — groups
    `ApiVersionDescription` instances by `GroupNameFormat` and
    invokes the consumer callback for each group, so applications
    can attach their own endpoint, authorization, and OpenAPI
    conventions. Throws `InvalidOperationException` when called
    before the registration extension.
- `IPlatformVersioningDefaultsProvider` /
  `PlatformVersioningDefaultsProvider` — exposes the configured
  `DefaultApiVersion` and `AssumeDefaultVersionWhenUnspecified` flag
  for tests and application code.
- `PlatformWebVersioningAssemblyMarker` — kept the package's
  assembly marker for downstream reflection.
- Architecture guards (`Platform.Architecture.Tests`):
  - `Platform.Web.Versioning` was added to the production-project
    inventory and the test-only assembly inventory; the framework
    reference allow-list was extended; three new architecture tests
    assert no forbidden packages, only `Platform.Core` references,
    and the single `Microsoft.AspNetCore.App` framework reference.
- `tests/Platform.Web.Versioning.Tests` (net8.0, xUnit +
  `Microsoft.AspNetCore.Mvc.Testing`): 26 tests across four files —
  option validation, reader selection (URL/header/query/media type
  composite and the explorer-options / report-versions / constraint
  propagation), `TestServer` registration (opt-in behavior, default
  assumption, URL/header/query readers, repeat registration,
  invalid options at `IOptions<>` resolution), and API Explorer
  grouping (distinct groups, two-version convention, configuration
  error when the provider is not registered).
- Docs: `docs/packages.md` has a new `Platform.Web.Versioning`
  section; the per-package reference at
  `docs/platform-web-versioning.md` covers adoption, options,
  reader selection, API Explorer integration, and starter-kit
  migration/rollback.
- Archived the change at
  `openspec/changes/archive/2026-09-10-platform-web-api-versioning/`
  with synchronized
  `openspec/specs/platform-web-api-versioning/spec.md` covering
  opt-in registration, configurable version policy, and API
  description integration.

## Verification evidence

- `dotnet build Platform.sln -c Release` — 0 warnings, 0 errors.
- `dotnet test Platform.sln -c Release --no-build --nologo -m:1` —
  all projects green, including the new
  `Platform.Web.Versioning.Tests` (26 passed, 0 failed) and the
  extended `Platform.Architecture.Tests` (286 passed, 0 failed,
  +8 new assertions for the versioning package).
- `openspec validate --changes --strict --no-interactive` — 4
  passed (after archive; `platform-web-api-versioning` is removed
  from the active queue).
- `openspec validate --specs --strict --no-interactive` — 35
  passed (after archive; the new
  `platform-web-api-versioning` spec is included).
- `git diff --check` — clean for the staged change.
- Implementation commit: recorded in the repository log for the
  `platform-web-api-versioning` change.

## Next change

`platform-identity-lifecycle-contracts` is the next active change
returned by `openspec list` (4 active changes remain). Implement
only that change in the next cycle.

## Completed: platform-hangfire-reliability

- Added a `JobStorage` registration seam to `AddPlatformHangfireJobs`:
  `AddPlatformHangfireJobs(IServiceCollection, Action<HangfireJobsOptions>?, JobStorage)`
  registers an application-owned `JobStorage` as a DI singleton, so the
  service provider owns the storage lifetime, the Hangfire
  `BackgroundJobServer`, the `IBackgroundJobClient`, the
  `IRecurringJobManager`, and the platform `HangfireJobDispatcher` all
  resolve the same instance, and the storage is disposed with the host.
  The existing `AddPlatformHangfireJobs(IServiceCollection, Action<HangfireJobsOptions>?)`
  overload still owns the default storage creation; production behavior is
  unchanged. Documented the storage ownership contract on `CreateStorage`
  and on the new overload.
- Added a `HangfireEndToEndHost` test helper under
  `tests/Platform.Jobs.Hangfire.Tests` that builds an isolated host with
  a fresh `InMemoryStorage`, a private service provider, and the new
  `JobStorage` registration seam. The host exposes:
  - `StartAsync()` / `StartAsync(TimeSpan readyTimeout,
    CancellationToken)` which start the host and poll the storage until
    the background server has registered itself, failing fast with
    `TimeoutException` if the worker never reports in;
  - `DisposeAsync()` which stops the host with a bounded
    `CancellationTokenSource` and disposes the service provider, so the
    in-memory dispatcher's worker thread joins before the storage is
    torn down;
  - a fluent `Builder.ConfigureServices` /
    `Builder.ConfigureOptions` / `Builder.UseStorage` API; the builder
    forces the static Hangfire log provider to a no-op before each
    build so the disposed `ILoggerFactory` from a previous host is
    never observed by the next host's in-memory dispatcher.
- Refactored the existing `EndToEndTests` (4 tests) to use
  `HangfireEndToEndHost`, removing the inline `BuildHost` helper.
- Added 8 new regression tests:
  - `EndToEndReliabilityTests` (6 tests, sequential collection):
    sequential repeated-dispatch independence, failed-job lifecycle
    with redacted telemetry, cancellation observed through the
    handler, worker-readiness signal before the first enqueue,
    deterministic post-disposal enqueue failure, multiple isolated
    hosts in sequence;
  - `ParallelEndToEndIsolationTests` (2 tests, parallel collection):
    two hosts built concurrently keep their job state isolated, and a
    builder-supplied storage is observable through the host.
- Added a new recording surface `RecordingPayloadHandler.FailedTask`
  so the failed-job lifecycle test can await the failure without
  depending on the success `HandledTask`. Existing tests that use the
  handler are unchanged.
- Updated `docs/platform-jobs-hangfire.md` with a "Test-host storage
  ownership" section documenting the new overload, and updated
  `docs/packages.md` with the second registration overload.
- Archived the change at
  `openspec/changes/archive/2026-09-10-platform-hangfire-reliability/`
  with synchronized `openspec/specs/platform-hangfire-reliability/spec.md`
  covering end-to-end lifecycle ownership, lifecycle-failure surfacing,
  and bounded synchronization.

## Verification evidence

- `dotnet test Platform.sln -c Release --no-build --nologo -m:1` — 1142
  tests passed, 0 failed, 0 skipped, including the new
  `Platform.Jobs.Hangfire.Tests` (69 tests, +8 new reliability
  assertions) and the extended `Platform.Architecture.Tests` (278
  tests, unchanged because the new registration is a production-code
  seam and the existing package guards already cover the adapter).
- `Platform.Jobs.Hangfire.Tests` ran 5 consecutive serial runs, all
  green (69 passed, 0 failed, 0 skipped each time) — the deterministic
  per-host storage ownership and bounded worker-readiness wait hold
  across repeated process executions.
- `openspec validate --changes --strict --no-interactive` — 5 passed
  (after archive; `platform-hangfire-reliability` is removed from the
  active queue).
- `openspec validate --specs --strict --no-interactive` — 34 passed
  (after archive; the new `platform-hangfire-reliability` spec is
  included).
- `git diff --check` — clean for the staged change.
- Implementation commit: recorded in the repository log for the
  `platform-hangfire-reliability` change.

## Next change

`platform-web-api-versioning` is the next active change returned by
`openspec list` (5 active changes remain). Implement only that change
in the next cycle.

## Completed: platform-release-governance

Implemented and verified the package governance foundation: explicit `src/`
package inventory with sample exclusion, GitHub repository metadata, portable
symbols and embedded sources, changelog/release policy, serial quality and
audit scripts, path-scoped PR and manual release workflows, selected public API
baseline, architecture metadata tests, and packed local-feed conformance.

Verified evidence:

- `Platform.Architecture.Tests`: 280 passed.
- `dotnet pack Platform.sln -c Release --no-restore --nologo -m:1`: all
  solution package and symbol artifacts produced successfully.
- `./scripts/conformance.sh`: pack, restore, build, and consumer tests passed.
- `openspec validate --changes --strict --no-interactive`: 7 passed.
- `openspec validate --specs --strict --no-interactive`: 32 passed.
- `./scripts/check-public-api.sh`: passed against
  `eng/public-api-baseline.txt`.
- `git diff --check`: passed.

Completion evidence:

- `./scripts/quality-gate.sh`: Release restore/build/test, strict OpenSpec
  validation, and `git diff --check` passed. Hangfire passed 61 tests after
  test-host global-state isolation was added; RabbitMQ passed 37 tests.
- `./scripts/audit-packages.sh`: `AUDIT_STATUS=VERIFIED` through the Huawei
  Cloud NuGet mirror.

The change was archived at
`openspec/changes/archive/2026-09-09-platform-release-governance/`. The next
change is `platform-hangfire-reliability`; its independent runtime reliability
work remains queued. The test-host isolation fix above is included in this
governance verification because it was required for the full gate.

## Planned queue from starter-kit gap audit

The active planning queue is intentionally dependency-ordered. Implement one change at a time, archive it, update this handoff with evidence, and stop before selecting the next change.

1. `platform-tenant-lifecycle-contracts` — add resumable provisioning/migration/seed orchestration seams; keep tenant catalog, migrations, and connection policy in consumers.
2. `platform-consumer-adoption-conformance` — extend the consumer conformance harness with platform-versioning adoption assertions.
3. `platform-testing-toolkit` — add a shared xUnit collection, fixtures, and assertion helpers for platform tests.
5. `platform-consumer-adoption-conformance` — verify pinned packed-package adoption, upgrade, rollback, and dependency boundaries.
6. `platform-testing-toolkit` — expand deterministic test-only fixtures after the public contracts and adoption path stabilize.

The queue was derived from the comparison with `/home/paul/code/dotnet-starter-kit`. Starter application modules, invoices/wallets/plans, React shells, Aspire, Docker/Terraform, and CLI/template ownership remain explicitly outside these platform changes.

## Completed: platform-eventing-rabbitmq

- Added `Platform.Eventing.RabbitMq` (`net8.0`) — an opt-in RabbitMQ adapter over the durable eventing contracts. `RabbitMqDurableEventPublisher` implements `IDurableEventPublisher`: it resolves the envelope's `PayloadType` through an application-owned `IRabbitMqEventTopology` (deterministic routing keys from registered `RabbitMqEventBinding`s, optional exchange override; unregistered types and unconfigured/invalid topology fail with a permanent configuration error before any message is sent), publishes persistent `application/json` messages carrying `MessageId`, correlation id, occurrence timestamp, and `payload-type`/`tenant-id` headers, and completes only after a broker confirmation (channels are created in publisher-confirm mode with confirmation tracking). Connection/channel lifecycle is adapter-owned: open channels are reused, closed or failed channels are disposed and re-created on the next publish, and every connect and confirmation wait is bounded. Connection, confirmation, and broker failures surface as safe transient `RabbitMqPublishException`s (stable `eventing.rabbitmq.*` codes, fixed safe messages, no broker response text, credentials, or inner exceptions; the original exception type name is logged before rethrowing) so the durable outbox stays the retry owner; caller cancellation is rethrown, never converted into a provider failure. Exposes `RabbitMqEventingProviderStatus`.
- Transport is abstracted behind `IRabbitMqChannelFactory`/`IRabbitMqChannel` seams (default `RabbitMqChannelFactory`/`RabbitMqChannel` over `RabbitMQ.Client` 7.2.2) so applications can share connections or supply an in-process test transport. `RabbitMqEventingOptions` (`SectionName` `"Eventing:RabbitMq"`) bounds host/port/virtual host, the required application-owned `Exchange`, exchange declaration (opt-in `DeclareExchange`, default false — the adapter never creates queues), and `ConnectTimeout`/`ConfirmTimeout`; `Validate()` is invoked at registration and construction with secret-free messages.
- `AddPlatformRabbitMqEventing` validates options at registration and `TryAdd`s `IRabbitMqEventTopology` (fail-closed `UnconfiguredRabbitMqEventTopology`), `IRabbitMqChannelFactory`, and `IDurableEventPublisher` → `RabbitMqDurableEventPublisher`, so application-owned registrations — including a consumer-owned publisher — always win. `Platform.Eventing` and `Platform.Eventing.Contracts` are unchanged and still reference no transport.
- Added `tests/Platform.Eventing.RabbitMq.Tests` (37 tests) covering options defaults/validation and secret-free messages, topology mapping and fail-closed defaults, confirmed publishing with deterministic routing and exchange override, unregistered/unconfigured/invalid-binding configuration failures, connect and confirm timeouts, cancellation preservation, broker-failure transient classification with reconnection, exchange-declaration opt-in, DI registration/idempotency/consumer overrides, an unreachable-broker transient classification (no Docker needed), and a Docker-gated integration test against a real RabbitMQ container (declared exchange, two confirmed publishes; silently skipped without Docker).
- Extended `Platform.Architecture.Tests` (273 → 278 tests, +5): added the package to the production-project list, the test project to the test-only assembly set, and added `Platform_Eventing_RabbitMq_references_only_platform_eventing_contracts` and `Platform_Eventing_RabbitMq_does_not_reference_forbidden_packages` (no ASP.NET Core, EF Core, Redis, Stripe, Npgsql, MailKit, SendGrid, Hangfire, Quartz, or MassTransit).
- Added `docs/platform-eventing-rabbitmq.md` (adoption, topology ownership, failure-classification table, publishing semantics, starter `RabbitMqEventBus` migration, rollback) and updated `docs/packages.md` with the package section and the test-project row; added both projects to `Platform.sln`; centrally versioned `RabbitMQ.Client` `7.2.2`.
- Archived the change at `openspec/changes/archive/2026-09-09-platform-eventing-rabbitmq/` with synchronized `openspec/specs/platform-eventing-rabbitmq/spec.md` covering the RabbitMQ durable publisher, safe transient failure, explicit topology ownership, and cancellation/bounded waits. No event schemas, queues, migrations, credentials, or retry policies were added.

## Verification evidence

- `dotnet build Platform.sln -c Release --no-restore --nologo -m:1` — 0 errors; the only warnings are pre-existing in `tests/Platform.Jobs.Hangfire.Tests` (CS0618, CS8625, xUnit1031) and `tests/Platform.Testing.Tests` (xUnit2013), unchanged and outside this change.
- `dotnet test Platform.sln -c Release --no-build --nologo -m:1` — 1105 tests passed, 0 failed, 0 skipped, including the new `Platform.Eventing.RabbitMq.Tests` (37, including the Docker-gated RabbitMQ broker integration test which ran against a real container in this environment) and the extended `Platform.Architecture.Tests` (278, +5 new assertions).
- `dotnet pack src/Platform.Eventing.RabbitMq/Platform.Eventing.RabbitMq.csproj -c Release --no-build --no-restore --nologo -m:1` — produced `Platform.Eventing.RabbitMq.0.1.0.nupkg` with dependencies `Platform.Eventing.Contracts`, `RabbitMQ.Client` 7.2.2, and the `Microsoft.Extensions.*` abstractions.
- `openspec validate --changes --strict --no-interactive` — 1 passed, 0 failed after archive (`openspec list` is now empty).
- `openspec validate --specs --strict --no-interactive` — 32 passed, 0 failed after archive.
- `git diff --check` — clean for the staged change.
- Implementation commit: `af6cb1e` (`Implement RabbitMQ durable eventing adapter`).

## Next change

`openspec list` is empty; the ten OpenSpec changes in `ROADMAP.md` Phases 1, 2, and 3 are implemented and archived. Any next work starts with a fresh OpenSpec proposal.

## Completed: platform-jobs-hangfire

- Added `Platform.Jobs.Hangfire` (`net8.0`) — an opt-in Hangfire adapter for the engine-neutral `Platform.Jobs` contracts. `HangfireJobDispatcher` serializes the platform payload (System.Text.Json, web defaults) and enqueues it through the Hangfire client into the configured queue; `HangfireRecurringJobRegistry` attaches the platform recurring executor to the descriptor's cron expression and time zone through `IRecurringJobManager` (first registration wins, later same-name registrations are no-ops, failed registrations roll back); `HangfireJobExecutor` is the Hangfire-invoked entry point that routes dispatched payloads to an application-owned `IJobPayloadHandler` (argument values round-trip as JSON values; complex values arrive as `JsonElement`) and recurring executions to the registered `IRecurringJobHandler`, recording `IJobTelemetry` with the stable `jobs.execution_failed` code, a fixed safe message, and only the exception type name before rethrowing so Hangfire's automatic retry model stays the retry owner; cancellation is rethrown and never recorded as a failure.
- Context propagation uses an application-owned `IJobExecutionContext` bridge (`Capture()`/`Restore(snapshot)` over opaque `JobContextSnapshot` tenant/subject identifiers): `JobContextCaptureFilter` captures at job creation into a Hangfire job parameter, `ScopedJobActivator` creates a DI scope per execution, restores the context before any handler is resolved, and disposes restoration + scope when the job ends; context-free jobs run context-free and a context-carrying job without a registered bridge fails closed.
- Storage is application-selected through bounded `HangfireJobsOptions` (`SectionName` `"BackgroundJobs:Hangfire"`): `HangfireStorageKind.InMemory` (default) or `PostgreSql` with an application-supplied connection string (never echoed); bounded `Queue`/`Queues`/`WorkerCount`/`SchedulePollingInterval`/`HeartbeatInterval`; `Validate()` is invoked at registration with secret-free messages. The storage instance is created inside Hangfire's configuration callback (after Hangfire binds the host log provider) and resolved through `JobStorage` from DI.
- The dashboard is opt-in (`DashboardEnabled`, default false) and requires an application-provided `DashboardAuthorization` callback — registration and mapping both fail fast without it; `HangfireDashboardOptionsFactory` never displays the storage connection string. `HangfireStorageHealthCheck` probes storage reachability and reports a redacted diagnostic (exception type only) plus `HangfireJobsProviderStatus`.
- `AddPlatformHangfireJobs` is idempotent (marker-guarded) and `TryAdd`s `HangfireJobExecutor`, `IJobDispatcher`, `IRecurringJobRegistry`, `IHealthCheck`, and `IClock` so application-owned registrations win; `UsePlatformHangfireDashboard` maps the dashboard only when enabled. `Platform.Jobs` itself is unchanged and still references no scheduler.
- Added `tests/Platform.Jobs.Hangfire.Tests` (61 tests) covering options defaults/validation, dispatcher serialization/telemetry/cancellation, recurring registration (first-wins, time zone, rollback), executor payload normalization and redacted failures, context capture/restoration/fail-closed, dashboard factory + TestServer authorization (401 denied / 200 allowed without storage details), storage health redaction, idempotent + consumer-override DI, end-to-end host execution (dispatched with context restoration and disposal, context-free, recurring trigger), and a Docker-gated PostgreSQL integration test (real container: dispatch, execution, healthy storage probe; silently skipped without Docker).
- Extended `Platform.Architecture.Tests` (268 → 273 tests, +5): added the package to the production-project list, the test project to the test-only assembly set, the package to the `FrameworkReference` allow list, and added `Platform_Jobs_Hangfire_references_only_platform_jobs` and `Platform_Jobs_Hangfire_does_not_reference_forbidden_packages` (no ASP.NET Core package refs, EF Core, Quartz, Redis, Stripe, or direct Npgsql).
- Added `docs/platform-jobs-hangfire.md` (adoption, context propagation, dashboard, health, retry ownership, starter-kit migration, rollback) and updated `docs/packages.md` with the package section and the test-project row; added both projects to `Platform.sln`; centrally versioned Hangfire `Core`/`AspNetCore` `1.8.23`, `InMemory` `1.0.0`, and `PostgreSql` `1.21.1`.
- Archived the change at `openspec/changes/archive/2026-09-09-platform-jobs-hangfire/` with synchronized `openspec/specs/platform-jobs-hangfire/spec.md` covering Hangfire job dispatch, scoped execution context, protected dashboard, and job provider health. No dashboard credentials, retry policies, job definitions, migrations, or connection strings were added.

## Verification evidence

- `dotnet build Platform.sln -c Release --no-restore --nologo -m:1` — 0 errors; the pre-existing `Platform.Testing.Tests` xUnit2013 warning is unchanged and outside this change.
- `dotnet test Platform.sln -c Release --no-build --nologo -m:1` — 1063 tests passed, 0 failed, 0 skipped, including the new `Platform.Jobs.Hangfire.Tests` (61, including the Docker-gated PostgreSQL integration test which ran against a real container in this environment) and the extended `Platform.Architecture.Tests` (273, +5 new assertions).
- `dotnet pack src/Platform.Jobs.Hangfire/Platform.Jobs.Hangfire.csproj -c Release --no-build --no-restore --nologo -m:1` — produced `Platform.Jobs.Hangfire.0.1.0.nupkg` with dependencies `Platform.Jobs`, Hangfire `Core`/`AspNetCore`/`InMemory`/`PostgreSql`, the platform abstractions, and the `Microsoft.AspNetCore.App` framework reference.
- `openspec validate --changes --strict --no-interactive` — 1 passed, 0 failed after archive (`platform-eventing-rabbitmq` remains).
- `openspec validate --specs --strict --no-interactive` — 31 passed, 0 failed after archive.
- `git diff --check` — clean for the staged change.
- Implementation commit: `4131c53` (`Implement Hangfire jobs adapter`).

## Next change

`platform-eventing-rabbitmq` is the next active change returned by `openspec list` (1 active change remains in the repository). Implement only that change in the next cycle.

## Completed: platform-mailing-providers

- Added `Platform.Mailing.Smtp` (`net8.0`) — an opt-in MailKit-based `IMailService` adapter with explicit `SmtpSecureMode { StartTls, SslOnConnect, None }` (no auto-negotiation), MIME construction from the immutable `MailMessage` (sender/recipients with display names, subject, text/HTML parts, attachments with parsed content types, `X-Correlation-Id` header), optional SMTP authentication, a `Func<SmtpClient>` client-factory seam, and a bounded `OperationTimeout` (default 30s, 1s–5min). Outcomes are normalized: accepted → `Sent`; SMTP 4xx/connection/TLS/timeout → `TransientFailure`; SMTP 5xx and authentication failures → `PermanentFailure`. Invalid sender/recipient/attachment input returns a `mail.configuration.*` failure before the server is contacted; caller cancellation is rethrown, never converted into a provider failure; provider responses are never surfaced (stable error codes and fixed safe messages only). `SmtpMailOptions.Validate()` is invoked at registration and construction with secret-free messages; `AddPlatformSmtpMail` `TryAdd`s `IMailService` so application-owned registrations win. Exposes `SmtpMailProviderStatus`.
- Added `Platform.Mailing.SendGrid` (`net8.0`) — an opt-in SendGrid adapter over an application-owned `ISendGridClient` with safe response classification: 2xx → `Sent` (with `X-Message-Id` as `ProviderMessageId`), 429 → `TransientFailure` (`mail.sendgrid.rate_limited`), 5xx → `TransientFailure` (`mail.sendgrid.server_error`), other 4xx → `PermanentFailure` (`mail.sendgrid.rejected`), transport/timeout → `TransientFailure`. Response bodies are never surfaced. Mapping covers sender, recipients, subject, plain-text/HTML bodies, base64 attachments, and the `X-Correlation-Id` global header. `SendGridMailOptions` (`ApiKey` required, secret-free validation; bounded `OperationTimeout`) is validated at registration and construction; `AddPlatformSendGridMail` `TryAdd`s both `ISendGridClient` (default `SendGridClient` with `HttpErrorAsException = false`) and `IMailService`. Exposes `SendGridMailProviderStatus`.
- Neither adapter owns templates, retry policies, delivery records, or provider credentials beyond its options; `TransientFailure` is the documented retry signal and retry ownership stays with the application. `Platform.Mailing` itself is unchanged and still references no provider SDK.
- Added `tests/Platform.Mailing.ProviderAdapters.Tests` (44 tests) with a deterministic in-process fake SMTP server (real loopback socket: MIME payload assertions, attachments, AUTH PLAIN capture, RCPT 550 → permanent, MAIL 451 → transient, connection refusal, operation timeout, cancellation preservation, configuration failures without server contact) and a fake `ISendGridClient` (classification, mapping, transport failure, cancellation, configuration failures), plus options-validation and opt-in DI/consumer-override tests for both adapters.
- Extended `Platform.Architecture.Tests` (258 → 268 tests, +10): added both packages to the production-project list, `Platform.Mailing.ProviderAdapters.Tests` to the test-only assembly set, and added `Platform_Mailing_Smtp_references_only_platform_mailing`, `Platform_Mailing_Smtp_does_not_reference_forbidden_packages`, `Platform_Mailing_SendGrid_references_only_platform_mailing`, and `Platform_Mailing_SendGrid_does_not_reference_forbidden_packages`.
- Added `docs/platform-mailing-providers.md` (adoption, normalized-outcome table, safety contract, delivery semantics, starter `MailRequest` migration, rollback) and updated `docs/packages.md` with both package sections and the new test-project row; added both packages and the test project to `Platform.sln`; centrally versioned MailKit `4.17.0` and SendGrid `9.29.3`.
- Archived the change at `openspec/changes/archive/2026-09-09-platform-mailing-providers/` with synchronized `openspec/specs/platform-mailing-providers/spec.md` covering the SMTP adapter, the SendGrid adapter, input/configuration validation, and cancellation preservation. No templates, retry scheduling, delivery databases, provider credentials, or product notifications were added.

## Verification evidence

- `dotnet build Platform.sln -c Release --no-restore --nologo -m:1` — 0 errors; the pre-existing `Platform.Testing.Tests` xUnit2013 warning is unchanged and outside this change.
- `dotnet test Platform.sln -c Release --no-build --nologo -m:1` — 997 tests passed, 0 failed, 0 skipped, including the new `Platform.Mailing.ProviderAdapters.Tests` (44) and the extended `Platform.Architecture.Tests` (268, +10 new assertions).
- `dotnet pack src/Platform.Mailing.Smtp/Platform.Mailing.Smtp.csproj -c Release --no-build --no-restore --nologo -m:1` — produced `Platform.Mailing.Smtp.0.1.0.nupkg`.
- `dotnet pack src/Platform.Mailing.SendGrid/Platform.Mailing.SendGrid.csproj -c Release --no-build --no-restore --nologo -m:1` — produced `Platform.Mailing.SendGrid.0.1.0.nupkg`.
- `openspec validate --changes --strict --no-interactive` — 2 passed, 0 failed after archive (`platform-jobs-hangfire`, `platform-eventing-rabbitmq` remain).
- `openspec validate --specs --strict --no-interactive` — 30 passed, 0 failed after archive.
- `git diff --check` — clean for the staged change.
- Implementation commit: `4326267` (`Implement SMTP and SendGrid mailing provider adapters`).

## Next change

`platform-jobs-hangfire` is the next active change returned by `openspec list` (2 active changes remain in the repository). Implement only that change in the next cycle.

## Completed: platform-auditing (handoff record repair)

- This cycle was implemented, tested, and archived in commit `13deb2b` (`Implement platform auditing contracts and capture adapters`) but its completion was never recorded here; this section restores the record. No code changes were made for it in this cycle.
- Added `Platform.Auditing.Contracts` with normalized audit events, sink, masking, enrichment, retention, dead-letter, and failure-policy contracts; `Platform.Auditing.AspNetCore` with opt-in request, exception, and security capture middleware (fail-open, sensitive values masked before dispatch); and `Platform.Auditing.EfCore` with an `IAuditedEntity` save-changes interceptor. No durable audit store, schema, or retention schedule is prescribed.
- Added `tests/Platform.Auditing.Tests` (67 tests) covering event validation, default masking rules, enricher/recorder semantics, the in-memory sink and options, HTTP middleware capture, exception classification, and EF Core change capture with masking and diffs.
- Extended `Platform.Architecture.Tests` (244 → 258 tests, +14): added the three auditing packages to the production-project list, `Platform.Auditing.Tests` to the test-only assembly set, the AspNetCore/EfCore framework allowances, and five new auditing dependency-direction facts.
- Archived the change at `openspec/changes/archive/2026-09-09-platform-auditing/` with synchronized `openspec/specs/platform-auditing/spec.md`.

## Verification evidence (recorded retroactively for commit `13deb2b`)

- `dotnet test Platform.sln -c Release --no-build --nologo -m:1` at this cycle's completion — 997 tests passed, 0 failed, including `Platform.Auditing.Tests` (67) and `Platform.Architecture.Tests` (268 total; 258 after `platform-auditing`, +10 from `platform-mailing-providers`).
- `openspec validate --specs --strict --no-interactive` — 30 passed, 0 failed (the `platform-auditing` spec is included).
- Implementation commit: `13deb2b`.

## Completed: platform-quota-aspnetcore

- Added `Platform.Quota.AspNetCore` (`net8.0`, ASP.NET Core `FrameworkReference` only) with an optional HTTP quota-enforcement adapter over the framework-neutral `Platform.Quota` contracts. The package references `Platform.Core`, `Platform.Quota`, and the `Microsoft.AspNetCore.App` framework reference only; it owns no plan/Finbuckle types, tenant records, billing plans, or provider entities.
- Added `QuotaEnforcementOptions` with the fail-closed `MissingContextPolicy { FailClosed, Allow }` and `QuotaUnavailablePolicy { FailClosed, Allow }` enums, an `Enabled` switch, a safe `AnonymousSubject` default, default exempt path prefixes (`/health`, `/healthz`, `/ready`, `/live`, `/alive`, `/metrics`), `ExemptMethods`, configurable `QuotaExceededStatusCode` (429), `MissingContextStatusCode` (403), `ProviderUnavailableStatusCode` (503), RFC 9457 `QuotaExceededType`/`QuotaExceededTitle`, `SubjectHeaderName` (`X-Quota-Subject`), and `Validate()` (valid status codes and non-empty subject header) invoked at registration.
- Added the application-owned seams `IQuotaSubjectResolver` (returns a `QuotaSubjectResolution` record, with a static `Missing` so the policy decides), `IQuotaResourceResolver` (returns `IReadOnlyList<QuotaRequest>` of `Resource`/`Limit`/`Amount`/`Window`/`Reserve`), and a `[QuotaExempt]` attribute for endpoint- and class-level exemptions. `HeaderQuotaSubjectResolver` reads the configured header and returns `Missing` on absent/blank value; `UnconfiguredQuotaResourceResolver` throws `InvalidOperationException` so the host fails closed until the application registers a resolver.
- Added `QuotaProblemDetailsWriter` (`IClock`-driven) that emits RFC 9457 `application/problem+json` bodies (via `JsonSerializer` with `JsonSerializerDefaults.Web`) for quota-exceeded and policy responses, surfacing safe diagnostic extensions (resource/limit/usage = consumed + reserved/requested/`retryAfterSeconds`/`resetAtUtc`/`traceId`/`correlationId`) without leaking store internals, and sets `Retry-After` from the decision window.
- Added `PlatformQuotaMiddleware` (`IMiddleware`) that short-circuits when disabled or exempt, resolves the subject (applying `MissingContextPolicy`), resolves resources (capturing a thrown `InvalidOperationException` from the unconfigured resolver into a 503 quota-unavailable policy response), then checks or reserves-and-settles each request via `IQuotaStore` using an idempotent operation key per trace+resource, releasing any reserved amount on downstream failure before rethrowing. Logging uses analyzer-clean `LoggerMessage.Define` (`QuotaLogMessages`) for subject-resolution, store-unavailable, settle, and release events.
- Added `ServiceCollectionExtensions.AddPlatformQuotaAspNetCore(Action<QuotaEnforcementOptions>?)` that registers validated options via `AddOptions<QuotaEnforcementOptions>().Configure(...)`, `TryAdd`s `IClock`→`SystemClock`, `IQuotaSubjectResolver`→`HeaderQuotaSubjectResolver`, `IQuotaResourceResolver`→`UnconfiguredQuotaResourceResolver`, `QuotaProblemDetailsWriter`, and `PlatformQuotaMiddleware`; plus `UsePlatformQuota(IApplicationBuilder)` wrapping `UseMiddleware<PlatformQuotaMiddleware>()`.
- Added `tests/Platform.Quota.AspNetCore.Tests` (13 xunit tests over `TestServer`) covering allowed requests, denied 429 with `Retry-After` and safe metadata, missing-subject fail-closed and allow, health/method/endpoint exemptions, provider-unavailable fail-closed and allow, the unconfigured-resolver 503 path, reserve-and-settle, reserve-and-release-on-downstream-failure, and cancellation-token threading through the store.
- Extended `Platform.Architecture.Tests` (236 → 244 tests, +8): added the new production and test projects to the production-project list and the AspNetCore-projection allowance, and added `Platform_Quota_AspNetCore_references_only_platform_core_and_quota` and `Platform_Quota_AspNetCore_does_not_reference_forbidden_packages`.
- Updated `docs/platform-quota.md` with the ASP.NET Core enforcement section (packages, seams, options, RFC 9457 contract, migration from the starter middleware) and `docs/packages.md` with the `Platform.Quota.AspNetCore` bullet, and added both projects to `Platform.sln`.
- Archived the change at `openspec/changes/archive/2026-09-09-platform-quota-aspnetcore/` with synchronized `openspec/specs/platform-quota-aspnetcore/spec.md` covering HTTP quota enforcement, standard quota rejection, missing-context policy, and configurable exemptions. No plan names, provider IDs, invoice rules, tenant records, migrations, or product resource quotas were added.

## Verification evidence

- `dotnet build Platform.sln -c Release --no-restore --nologo -m:1` — succeeded; 1 pre-existing unrelated warning (xUnit2013 in `Platform.Testing.Tests`) remains outside this change.
- `dotnet test Platform.sln -c Release --no-build --no-restore --nologo -m:1` — 862 tests passed, 0 failed, 0 skipped, including the new `Platform.Quota.AspNetCore.Tests` (13) and the extended `Platform.Architecture.Tests` (244, +8 new assertions).
- `openspec validate --changes --strict --no-interactive` — 4 passed, 0 failed (4 active changes remain after archive).
- `openspec validate --specs --strict --no-interactive` — 28 passed, 0 failed after archive.
- `git diff --check` — clean for the staged change.
- Implementation commit completed for `platform-quota-aspnetcore` (source, tests, architecture guards, docs, archive, and generated spec).

## Next change

`platform-auditing` is the next active change returned by `openspec list` (4 active changes remain in the repository). Implement only that change in the next cycle.

## Completed: platform-identity-host-integration

- Extended `Platform.Identity.AspNetCore` (`net8.0`, ASP.NET Core `FrameworkReference` only) so the host adapter projects configured subject, email, tenant, role, and permission claims into `CurrentUser` and returns `CurrentUser.Anonymous` for unauthenticated requests (`HttpCurrentUserAccessor` with configurable `SubjectClaimType`/`EmailClaimType` falling back to `sub`/`email` and case-insensitive duplicate deduplication). `PlatformIdentityOptions` gained `SubjectClaimType` and `EmailClaimType` while keeping `AuthenticationScheme`, `PermissionClaimType`, and `TenantClaimType`.
- Wired the `PlatformPermissionHandler` to record a denied `IdentityAuditEvent` (`authorization.denied`) to a registered `IIdentityAuditHook` without token contents, in addition to the existing `IAuthorizationDecisionAuditor` decision recording.
- Added replaceable registration seams `AddPlatformIdentityCredentialVerifier<T>`, `AddPlatformIdentityExternalProvider<T>`, `AddPlatformIdentityVerificationProvider<T>`, `AddPlatformIdentitySessionStore<T>`, and `AddPlatformIdentityAuditHook<T>` (all `TryAdd`), plus `IIdentitySessionService`/`IdentitySessionService` that wraps the registered `ISessionStore`, preserves `IdentityProviderResult<T>` outcomes, and reports `ProviderUnavailable` when no store is registered. The identity-store seam `AddPlatformIdentityStore<T>` remains on `Platform.Identity.EntityFrameworkCore` (which gained `IdentityStoreServiceCollectionExtensions` and a `Microsoft.Extensions.DependencyInjection.Abstractions` reference) so the ASP.NET Core package stays free of EF Core.
- Added `PlatformIdentityJwtOptions` (`Enabled`, `Issuer`, `Audience`, `SigningKey`) with a redacted `GetDiagnosticName()` and `AddPlatformIdentityJwt(...)` that validates the signing configuration on startup via `ValidateOnStart`; when JWT is enabled without a signing key, issuer, or audience, the host fails fast with a secret-free `OptionsValidationException` message. The platform validates presence only and issues no tokens.
- Added `tests/Platform.Identity.Tests` (9 new tests) covering configurable subject/email claim types, anonymous projection, duplicate claim deduplication, denied permission recorded to the audit hook without token contents, the session-store seam preserving normalized outcomes, the session service reporting unavailable when no store is registered, JWT startup validation on missing signing configuration, secret-free validation message on missing issuer/audience, and redacted JWT diagnostic names.
- Extended `Platform.Architecture.Tests` (233 → 236 tests, +3): added `Platform_Identity_AspNetCore_references_only_identity_contracts_and_authorization`, `Platform_Identity_AspNetCore_does_not_reference_forbidden_packages`, and `Platform_Identity_Testing_references_only_identity_contracts_and_authorization`. Existing identity contract/EF architecture guards remain.
- Updated `docs/platform-identity.md` with the host-integration section (claim mapping, authorization/audit, replaceable stores, JWT validation), migration-from-starter-Identity guidance, and rollback/claim-compatibility rules; updated `docs/packages.md` with per-package references for `Platform.Identity` (contracts), `Platform.Authorization`, `Platform.Identity.AspNetCore`, `Platform.Identity.EntityFrameworkCore`, and `Platform.Identity.Testing`.
- Archived the change at `openspec/changes/archive/2026-09-09-platform-identity-host-integration/` with synchronized `openspec/specs/platform-identity-host-integration/spec.md` covering current-user claim projection, permission authorization with audit, replaceable identity stores, and security configuration validation. No user entities, JWT issuance, refresh-token storage, password rules, tenant provisioning, or product permissions were added.

## Verification evidence

- `dotnet build Platform.sln -c Release --no-restore --nologo -m:1` — 0 warnings, 0 errors (the pre-existing `Platform.Testing.Tests` xUnit2013 warning was unchanged and outside this change).
- `dotnet test Platform.sln -c Release --no-build --no-restore --nologo -m:1` — 844 tests passed, 0 failed, 0 skipped, including the new `Platform.Identity.Tests` (now 23 total) and the extended `Platform.Architecture.Tests` (236, +3 new assertions).
- `openspec validate --changes --strict --no-interactive` — 5 passed, 0 failed (5 active changes remain after archive).
- `openspec validate --specs --strict --no-interactive` — 26 passed, 0 failed after archive.
- `git diff --check` — clean for the staged change.

## Next change

`platform-quota-aspnetcore` is the next active change returned by `openspec list` (5 active changes remain in the repository). Implement only that change in the next cycle.

## Completed: platform-realtime

- Added `Platform.Realtime` (`net8.0`, framework-neutral) with opt-in, provider-neutral realtime contracts: bounded `RealtimeConnectionOptions` (section `"Realtime"`, `MaxConcurrentConnections=1000`, `MaxPayloadBytes=64*1024`, `ConnectionIdleTimeout=5min`, `HeartbeatInterval=30s`, `AllowCrossTenantBroadcast=false`, `Validate()`); connection authorization (`RealtimeConnectionRequest`, `RealtimeAuthorizationResult.Allow()/Deny(reason)`, `IRealtimeConnectionAuthorizer`, fail-closed `DenyAllRealtimeAuthorizer`); tenant routing (`RealtimeTenantRoute`, `IRealtimeTenantRouter`, fail-closed `DenyCrossTenantRouter` permitting only equal tenants or both-null); bounded `RealtimeMessage.Create(...)` with `GetPayloadByteCount()`; `RealtimeConnectionContext`; topology-free `IRealtimeProviderStatus` returning `ImmutableArray<RealtimeProviderStatus>`; non-durable delivery disclosure (`RealtimeDelivery.IsDurable == false`, `Guarantee == "non-durable"`) and `RealtimeResyncRequest`; and `AddPlatformRealtime(...)` registering an `IClock` default. The package references `Platform.Core`, `Microsoft.Extensions.DependencyInjection.Abstractions`, and `Microsoft.Extensions.Options` only; it declares no framework reference and no ASP.NET Core, SignalR, EF Core, Redis, or scheduling-engine dependency.
- Added `Platform.Realtime.AspNetCore` (`net8.0`, ASP.NET Core `FrameworkReference` only) with the SSE transport: application-owned `IRealtimeCallerResolver` (default `AnonymousRealtimeCallerResolver`), `IRealtimeSseSource`/`IRealtimeMessageSink`, a thread-safe `RealtimeConnectionLimiter` (`TryAcquire`/`Release` over `Interlocked`), `SseMessageSink` enforcing the tenant router and payload bounds and writing `event:`/`data:` frames plus `:` heartbeat comments, and `MapPlatformRealtimeSse(pattern)` that resolves the caller, authorizes (401 if denied), enforces the connection limit (503 if full), sets `text/event-stream` + `X-Accel-Buffering: no` and flush, runs the source with heartbeat, and releases the slot in `finally`. SignalR is opt-in via `AddPlatformRealtimeSignalR(...)`: it wires `AddSignalR()`, a registered `IHubFilter` (`RealtimeHubAuthorizationFilter`) and `RealtimeHubBase` (authorize on connect, `SendToTenantAsync`), an `Action<ISignalRServerBuilder>? ConfigureBackplane` seam, and a fail-closed default `DenyAllRealtimeAuthorizer`. `MapPlatformRealtimeStatus("/realtime/status")` and `MapPlatformRealtimeResync("/realtime/resync")` expose topology-free provider status (404 when no `IRealtimeResyncHandler`, 202 on resync) backed by `CompositeRealtimeProviderStatus` (SSE always, SignalR only when enabled). The package references `Platform.Core`, `Platform.Realtime`, and the `Microsoft.AspNetCore.App` framework reference only.
- Added `tests/Platform.Realtime.Tests` (30 tests) covering option defaults and validation, message payload bounds, authorization allow/deny, the connection limiter, SSE integration (401 on denied authorization, 503 at the connection limit, tenant-route rejection, payload-bound rejection, heartbeat frames) over `TestServer`, SignalR registration (SSE-only status, SignalR status without backplane, backplane-enabled seam, fail-closed default authorizer), and provider-status/resync (404 without handler, 202 with handler).
- Extended `Platform.Architecture.Tests` (230 → 236 tests, +6): added the two new packages to the production-project list and the `FrameworkReference` allow list, `tests/Platform.Realtime.Tests` to the test-only assembly set, and added `Platform_Realtime_does_not_reference_forbidden_packages`, `Platform_Realtime_references_only_platform_core`, `Platform_Realtime_AspNetCore_references_only_platform_core_and_realtime`, `Platform_Realtime_AspNetCore_declares_only_the_aspnetcore_framework_reference`, `Platform_Realtime_AspNetCore_does_not_reference_forbidden_packages`, and `Realtime_test_project_does_not_reference_production_projects`.
- Added `docs/platform-realtime.md` with adoption, tenant-routing, SSE/SignalR opt-in, non-durable delivery disclosure, and rollback guidance; updated `docs/packages.md` with the per-package reference for both new packages.
- Archived the change at `openspec/changes/archive/2026-09-09-platform-realtime/` with synchronized `openspec/specs/platform-realtime/spec.md` covering opt-in transports (SSE-only without SignalR/Redis), application-owned connection authorization and tenant routing, bounded connection lifecycle, and non-durable delivery disclosure. The platform owns no connection strings, transport backplane, tenant records, auth schemes, or product message schemas.

## Verification evidence

- `/home/paul/.dotnet/dotnet build Platform.sln -c Release --no-restore --nologo -m:1` — 0 warnings, 0 errors (the pre-existing `Platform.Testing.Tests` xUnit2013 warning was unchanged and outside this change).
- `/home/paul/.dotnet/dotnet test Platform.sln -c Release --no-build --no-restore --nologo -m:1` — 832 tests passed, 0 failed, 0 skipped, including the new `Platform.Realtime.Tests` (30) and the extended `Platform.Architecture.Tests` (236, +6 new assertions).
- `/home/paul/.dotnet/dotnet pack src/Platform.Realtime/Platform.Realtime.csproj -c Release --no-build --no-restore --nologo -m:1` — produced `Platform.Realtime.0.1.0.nupkg`.
- `/home/paul/.dotnet/dotnet pack src/Platform.Realtime.AspNetCore/Platform.Realtime.AspNetCore.csproj -c Release --no-build --no-restore --nologo -m:1` — produced `Platform.Realtime.AspNetCore.0.1.0.nupkg`.
- `openspec validate --specs --strict --no-interactive` — 26 passed, 0 failed after archive.
- `git diff --check` — clean for the staged change.

## Next change

`platform-identity-host-integration` is the next active change returned by `openspec list` (6 active changes remain in the repository). Implement only that change in the next cycle.

## Completed: platform-featureflags-resilience

- Added `Platform.FeatureManagement` (`net8.0`, ASP.NET Core) with an application-owned `IFeatureContextResolver` + `FeatureContext`, the `PlatformTenantFeatureFilter` (`[FilterAlias("PlatformTenant")]`) that gates a feature by tenant via the resolver, and `RequireFeature` endpoint gating that returns a safe `ProblemDetails` when disabled. `FeatureManagementOptions` is validated at registration (`Enabled`, `SectionName`, bounded `DisabledStatusCode`, `DisabledTitle`). The package references `Platform.Core`, `Platform.Web.Telemetry`, the `Microsoft.AspNetCore.App` framework reference, and `Microsoft.FeatureManagement` only; it owns no feature names, rollout state, billing plans, tenant records, EF Core, or Polly.
- Added `Platform.Http.Resilience` (`net8.0`, framework-neutral) with bounded `PlatformHttpResilienceOptions` (attempt/total timeouts, retry attempts/back-off, circuit-breaker ratio/throughput/break, concurrency limit/queue, idempotent-method set) validated at registration. The pipeline is built from the `Microsoft.Extensions.Http.Resilience` primitives (`ResiliencePipelineBuilder<HttpResponseMessage>` with base retry, timeout, circuit-breaker, and rate-limiter strategy options); the HTTP method is carried through a `ResilienceContext` property so idempotency classification is reliable. `PlatformHttpResilienceHandler` retries only idempotent methods by default, preserves caller cancellation (recorded, never retried), and short-circuits on an open circuit or a full concurrency queue with bounded `PlatformHttpResilienceEvent` telemetry (`IPlatformHttpResilienceTelemetry`, default no-op). The package references `Platform.Core`, `Platform.Web.Telemetry`, `Microsoft.Extensions.Http`, and `Microsoft.Extensions.Http.Resilience`; it declares no framework reference and no feature-management/Polly/provider dependency.
- Added `tests/Platform.FeatureManagement.Tests` (7 tests) covering disabled-feature status/title, enabled-feature execution for an allowed tenant, resolver invocation, application-supplied mutable context, and options validation.
- Added `tests/Platform.Http.Resilience.Tests` (9 tests) covering POST-not-retried, GET-retried-to-limit, successful GET not retried, opt-in idempotent override, circuit-breaker open + telemetry, cancellation preserved and recorded, per-attempt timeout short-circuit, and options validation (retry count, circuit ratio).
- Extended `Platform.Architecture.Tests` (213 → 225 tests, +12): added the two new packages to the production-project list and the `FrameworkReference` allow list; added `Platform_FeatureManagement_references_only_platform_core_and_web_telemetry`, `Platform_FeatureManagement_declares_only_the_aspnetcore_framework_reference`, `Platform_FeatureManagement_does_not_reference_forbidden_packages`, `Platform_Http_Resilience_references_only_platform_core_and_web_telemetry`, `Platform_Http_Resilience_does_not_declare_a_framework_reference`, and `Platform_Http_Resilience_does_not_reference_forbidden_packages`.
- Added `docs/platform-feature-flags-resilience.md` with adoption, retry-ownership, and rollback guidance; updated `docs/packages.md` with the per-package reference for both new packages.
- Archived the change at `openspec/changes/archive/2026-09-09-platform-featureflags-resilience/` with synchronized `openspec/specs/platform-featureflags-resilience/spec.md` covering application-owned feature evaluation, explicit endpoint gating, safe HTTP resilience defaults, and resilience observability. No feature names, rollout persistence, billing plans, tenant records, or product flags were added.

## Verification evidence

- `dotnet build Platform.sln -c Release --no-restore --nologo -m:1` — 0 errors; the pre-existing `Platform.Testing.Tests` xUnit2013 warning is unchanged and outside this change.
- `dotnet test Platform.sln -c Release --no-build --no-restore --nologo -m:1` — 791 tests passed, 0 failed, 0 skipped, including the new `Platform.FeatureManagement.Tests` (7) and `Platform.Http.Resilience.Tests` (9) and the extended `Platform.Architecture.Tests` (225, +12 new assertions).
- `openspec validate --changes --strict --no-interactive` — 8 passed, 0 failed before archive (the 7 other active changes in the repository are included).
- `openspec validate --specs --strict --no-interactive` — 25 passed, 0 failed after archive (the new spec is included).
- `git diff --check` — clean for the staged change.
- Implementation commit: `fe430fa` (`Implement feature flags and HTTP resilience adapters`).

## Next change

`platform-realtime` is the next active change returned by `openspec list` (7 active changes remain in the repository). Implement only that change in the next cycle.

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
