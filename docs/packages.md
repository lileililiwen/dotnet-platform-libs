# Package reference

This document summarizes the public surface of each platform package. It is the consumer-facing companion to `docs/build-test-pack.md`.

## Platform.Core

Framework-independent contracts. Zero third-party dependencies; targets `net8.0`.

### Time

- `IClock` — abstraction for the current UTC time. Implementations must return a `DateTimeOffset` with `Offset == TimeSpan.Zero`.
- `SystemClock` — production implementation backed by `DateTimeOffset.UtcNow`.
- `FixedClock` — deterministic implementation that takes a `DateTimeOffset` or `Func<DateTimeOffset>` at construction.

### Results

- `Error` — immutable record with stable `Code`, safe `Message`, and optional `Metadata`. Helpers: `Error.Validation`, `Error.NotFound`; constants `Error.ValidationCode`, `Error.NotFoundCode`.
- `Result` — non-generic outcome. `Result.Success()`, `Result.Failure(Error)`.
- `Result<T>` — generic outcome. `Result<T>.Success(T)`, `Result<T>.Failure(Error)`, `ToResult()`.

### Context

- `CallerContext` — immutable record with optional `SubjectId` and `TenantId`. Helpers: `IsAnonymous`, `HasTenant`. Static singleton: `CallerContext.Anonymous`.

### Audit

- `IAuditable` — exposes `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`. Product types implement it without inheriting from a platform base class.

## Platform.AspNetCore

ASP.NET Core integration. Depends on `Platform.Core` and `Microsoft.AspNetCore.App` (via `FrameworkReference`); no third-party packages.

### Registration

- `AddPlatformAspNetCore(IServiceCollection)` and `AddPlatformAspNetCore(IServiceCollection, Action<PlatformAspNetCoreOptions>)` — register `IClock` (`SystemClock`), `IProblemDetailsMapper`, `IHttpContextAccessor`, `ICorrelationAccessor`, and `PlatformAspNetCoreOptions`. No hidden authentication, persistence, or provider services are added.
- `AddPlatformHealthChecks(IServiceCollection)` — adds a `platform.liveness` check tagged `live`. Returns the `IHealthChecksBuilder` for further configuration.

### Pipeline

The documented middleware order is:

1. `UsePlatformCorrelation` — establish or accept a correlation identifier and echo it on the response.
2. `UsePlatformProblemDetails` — translate `PlatformProblemException` to sanitized `ProblemDetails`; sanitize unknown exceptions to a generic `500` with no internal details.
3. `MapPlatformEndpoints` — map the health endpoint at `PlatformAspNetCoreOptions.HealthCheckPath` (default `/health`).

`UsePlatformAspNetCore` runs steps 1 and 2 in the documented order.

### Errors

- `IProblemDetailsMapper` — maps an `Error` to a `ProblemDetails` with status code, type URI, title, detail, and the `code` extension.
- `PlatformProblemDetailsMapper` — default implementation. `platform.validation` → 400, `platform.not_found` → 404, unknown → 500.
- `PlatformProblemException` — exception type that carries an `Error` to the boundary.
- `ProblemDetailsExceptionMiddleware` — converts `PlatformProblemException` to `ProblemDetails`; sanitizes unknown exceptions to a generic 500.

### Correlation

- `ICorrelationAccessor` — exposes the current request's correlation identifier.
- `HttpCorrelationAccessor` — `HttpContext`-backed implementation.
- `CorrelationMiddleware` — reads or generates the correlation identifier and stores it on `HttpContext.Items`; echoes it on the response via `OnStarting`.

### Options

- `PlatformAspNetCoreOptions` — `CorrelationHeader` (default `X-Correlation-Id`), `AcceptIncomingCorrelationHeader` (default `false`), `HealthCheckPath` (default `/health`).

## Platform.Billing.Contracts

Provider-neutral subscription and entitlement contracts. Zero third-party dependencies; targets `net8.0`.

### Identifiers

- `PlanId`, `FeatureKey`, `SubjectKey` (with `Anonymous` and `IsAnonymous`), `ProviderName`, `ProviderEventId` — opaque strongly-typed identifiers. All have a `Create(string)` factory that rejects null/empty/whitespace values.

### Subscriptions

- `SubscriptionStatus` enum — `Free`, `Active`, `Suspended`, `PastDue`, `Canceled`, `Unknown`. Only `Free` and `Active` are treated as active.
- `Subscription` — record carrying `Subject`, `Plan`, `Status`, `Provider`, `ProviderSubscriptionId`, `PeriodStart`, `PeriodEnd`. Helpers: `IsActive`, `IsWithinPeriod(now)`.

### Entitlements

- `Entitlement` — immutable snapshot: `Subject`, optional `Tenant`, optional `Subscription`, `IReadOnlySet<FeatureKey> ActiveFeatures`, optional `IReadOnlyDictionary<FeatureKey, long> Limits`, `CapturedAt`. Helpers: `Grants(FeatureKey)`, `LimitFor(FeatureKey)`.
- `EntitlementDefaults` — static factory: `Anonymous`, `Inactive(subject)`, `Unknown(subject, subscription)`. Unknown subscriptions are preserved for diagnostics but grant no features.

### Features

- `FeatureCheckReason` enum — `Allowed`, `NotAuthenticated`, `NotSubscribed`, `PlanMismatch`, `LimitExceeded`, `Unknown`.
- `FeatureCheckResult` — structured decision: `Feature`, `Reason`, optional `RequiredPlan`/`CurrentUsage`/`Limit`. Helper: `IsAllowed`.
- `FeatureCheck.Evaluate(Entitlement, FeatureKey)` — maps a snapshot into a decision.

### Usage

- `IUsageMeter` — `CheckAsync(subject, feature, cancellationToken)`, `RecordAsync(subject, feature, units, cancellationToken)`. The platform does not prescribe storage, cache, or counting implementation.
- `UsageCheckResult` — `Feature`, `Used`, optional `Limit`, optional `WindowStart`/`WindowEnd`. Helper: `IsWithinLimit`.

### Events

- `ProviderEvent` — opaque provider event: `Id`, `Provider`, `Type`, `OccurredAt`, `Payload`.
- `ProcessedEvent` — stored record: `EventId`, `Provider`, `ProcessedAt`, `Result`.
- `ProcessedEventDecision` enum — `FirstDelivery`, `Duplicate`.
- `IProcessedEventStore` — `MarkProcessedAsync(processedEvent, cancellationToken)` returns the idempotency decision.

## Platform.Testing

Test-only helpers. Depends on `Platform.Core`, `Platform.AspNetCore`, and `Platform.Billing.Contracts`. No xUnit, NUnit, or mocking-framework dependencies. Targets `net8.0`. Production projects must not reference this package.

### Time

- `ControllableClock` — deterministic `IClock` that never reads system time. `Set(DateTimeOffset)`, `Advance(TimeSpan)`. Rejects non-UTC values. Initial value defaults to `DateTimeOffset.UnixEpoch`.

### Entitlements

- `SubscriptionBuilder` — fluent builder for `Subscription`. Defaults to active status, `plan.test`, `test` provider, `sub_test` provider id, and a 30-day period anchored at the supplied `IClock`. `WithPlan`, `WithStatus`, `WithProvider`, `WithProviderSubscriptionId`, `WithPeriodStart`, `WithPeriodEnd`.
- `EntitlementBuilder` — fluent builder for `Entitlement`. Safe inactive defaults (no features, no limits). `WithTenant`, `WithSubscription`, `Granting(feature)`, `Granting(IEnumerable<FeatureKey>)`, `WithLimit(feature, limit)`, `CapturedAt`.
- `FakeEntitlementStore` — in-memory store with `Configure`, `Get`, `Invalidate`, `InvalidatedSubjects`, `Reset`. `Get` returns `EntitlementDefaults.Inactive` for unknown or invalidated subjects.

### Usage

- `UsageCall` — record describing one recorded call: `Subject`, `Feature`, `Units`, `OperationKind` (`Check` or `Record`).
- `RecordingUsageMeter` — `IUsageMeter` that records every call and accumulates totals per `(subject, feature)`. `SetLimit(feature, limit?)` configures per-feature limits; `TotalFor`; `Calls`; `Reset` clears totals and calls but preserves limits.

## Test projects

| Project | Coverage |
| --- | --- |
| `tests/Platform.Architecture.Tests` | Dependency-direction guardrails (no production project references test projects, ASP.NET Core, EF Core, or Stripe; `FrameworkReference` is allowed only for `Platform.AspNetCore`; `Platform.Core` and `Platform.Billing.Contracts` declare no `<PackageReference>` entries). |
| `tests/Platform.Core.Tests` | Unit tests for time, results, context, and audit contracts. |
| `tests/Platform.AspNetCore.Tests` | Unit tests for the ProblemDetails mapper, exception middleware, and correlation middleware, plus a `TestServer` integration test for the minimal host (known failure → ProblemDetails, unknown failure → sanitized 500, correlation generation, health endpoint). |
| `tests/Platform.Billing.Contracts.Tests` | Unit tests for identifiers, subscription status and period boundaries, entitlement defaults, feature check decisions, usage-meter contract (in-memory implementation), and processed-event idempotency. |
| `tests/Platform.Testing.Tests` | Unit tests for `ControllableClock`, `SubscriptionBuilder`, `EntitlementBuilder`, `FakeEntitlementStore`, and `RecordingUsageMeter`. |
