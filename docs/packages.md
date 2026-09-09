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

## Platform.Billing provider adapters

- `Platform.Billing.Stripe` — optional raw-HTTP Stripe adapter with application-owned plan
  mapping, checkout, portal, subscription lookup, webhook verification/normalization, and status.
- `Platform.Billing.LemonSqueezy` — optional raw-HTTP Lemon Squeezy adapter with checkout,
  subscription lookup, webhook verification/normalization, and status. Its unsupported portal
  and cancellation operations are explicit.
- `ProviderFailureClassifier` and `ProviderFailure` — safe transient, permanent, configuration,
  authentication, and malformed-response categories without secrets or response bodies.

## Platform.Ai

`Platform.Ai.Contracts` provides provider-neutral generation, streaming, structured-output,
embedding, usage, cost, capability, policy, and failure contracts without provider references.
`Platform.Ai` provides policy-gated generation and single- or feature-based routing with safe
telemetry. `Platform.Ai.Testing` provides deterministic fakes and usage recording.

The optional `Platform.Ai.OpenAiCompatible`, `Platform.Ai.Anthropic`, and `Platform.Ai.Ollama`
packages use raw HTTP only. OpenAI-compatible configuration also supports DeepSeek endpoint
selection. See [`docs/platform-ai.md`](platform-ai.md) for data-handling and adoption rules.

## Platform.Jobs

Engine-neutral scheduling contract. Depends on `Platform.Core`, `Microsoft.Extensions.Options`, and `Microsoft.Extensions.DependencyInjection.Abstractions`; targets `net8.0`. Does not reference ASP.NET Core, EF Core, Hangfire, Quartz, or application projects.

### Contracts

- `IJobDispatcher` — `EnqueueAsync(payload, cancellationToken)`. Implementations read the current time from an injected `IClock`; the platform exposes only the contract.
- `IRecurringJobHandler` — marker interface; `ExecuteAsync(cancellationToken)`.
- `IRecurringJobRegistry` — `Register(descriptor)`, `Registered` enumeration. Implementations invoke `IJobTelemetry` on every register.
- `IJobTelemetry` — `JobRegistered(descriptor, registeredAt)`, `JobEnqueued(payload, enqueuedAt)`, `JobExecuted(name, executedAt)`, `JobFailed(name, error, failedAt)`. The platform does not ship a default implementation.

### Value types

- `JobPayload` — record with `Name` and optional `Arguments` dictionary. `Create(name, arguments)` factory rejects null/empty/whitespace names.
- `RecurringJobDescriptor` — record with `Name`, `Cron`, `HandlerType`, `TimeZone` (default `"UTC"`), optional `Options` dictionary. `WithName`/`WithCron`/`WithOptions` helpers return a new descriptor.
- `RecurringJobAttribute` — runtime attribute (target: `Class`, `AllowMultiple = false`, `Inherited = false`) with a required `Cron` constructor argument and optional `Name`, `TimeZone` (default `"UTC"`), `Options`. The static `GetDescriptor(Type)` helper returns a descriptor for a decorated handler and throws `InvalidOperationException` for an undecorated one.

### Options

- `BackgroundJobsOptions` — `DefaultTimeZone` (default `"UTC"`), `SectionName` constant `"BackgroundJobs"`. Bound by `AddPlatformJobs` through the `IOptions<>` pipeline.

### Registration

- `AddPlatformJobs(IServiceCollection)` and `AddPlatformJobs(IServiceCollection, Action<BackgroundJobsOptions>)` — bind `BackgroundJobsOptions` and register `IClock` only when no implementation is already present. The package does not register default `IJobDispatcher`, `IRecurringJobRegistry`, or `IJobTelemetry`; consumers provide their own.

## Platform.Mailing

Provider-neutral mailing contract. Depends on `Platform.Core`, `Microsoft.Extensions.Options`, and `Microsoft.Extensions.DependencyInjection.Abstractions`; targets `net8.0`. Does not reference ASP.NET Core, EF Core, SendGrid, Mailgun, SMTP, Razor, Liquid, or application projects.

### Value types

- `MailAddress` — record with `Address` and optional `DisplayName`. `Create(address)` factory rejects null/empty/whitespace.
- `MailAttachment` — record with `FileName`, `ContentType`, `Content` bytes. `Create(fileName, contentType, content)` factory validates all three.
- `MailMessage` — sealed record with a validating primary constructor. At least one of `TextBody` or `HtmlBody` must be supplied; `Subject` must be non-empty; at least one recipient is required; `From` must be non-null. Optional `Attachments` (defaults to empty) and `CorrelationId`.
- `MailSendOutcome` — enum: `Sent`, `TransientFailure`, `PermanentFailure`, `Bounced`.
- `MailSendResult` — record with `Outcome`, optional `ProviderMessageId`, `ErrorCode`, `ErrorMessage`. `IsAccepted` is `true` for `Sent` or `Bounced`.
- `MailTemplateId` — `readonly record struct` with a validating constructor and implicit conversions to and from `string` (the conversion from `string` re-validates).
- `RenderedMailTemplate` — record with `Subject`, optional `TextBody`, optional `HtmlBody`. `Create(subject, textBody, htmlBody)` factory enforces the subject non-empty and body invariants.

### Contracts

- `IMailService` — `SendAsync(message, cancellationToken)`. Implementations return a `MailSendResult`.
- `IMailTemplateRenderer<TModel>` — `RenderAsync(templateId, model, cancellationToken)`. The platform does not ship a default renderer.

### Options

- `MailingOptions` — `DefaultFromAddress` (default `"noreply@example.invalid"`), `DefaultFromDisplayName` (default `"Platform"`), `MaxAttempts` (default `3`), `InitialBackoffSeconds` (default `5`), `MaxBackoffSeconds` (default `60`), `TemplatesPath` (default `"templates"`), `SectionName` constant `"Mailing"`.

### Registration

- `AddPlatformMailing(IServiceCollection)` and `AddPlatformMailing(IServiceCollection, Action<MailingOptions>)` — bind `MailingOptions` and register `IClock` only when no implementation is already present. The package does not register default `IMailService` or `IMailTemplateRenderer<TModel>`; consumers provide their own.

## Platform.Eventing

Transport-agnostic event-bus contract. Depends on `Platform.Core`, `Microsoft.Extensions.Options`, `Microsoft.Extensions.DependencyInjection.Abstractions`, and `Microsoft.Extensions.Logging.Abstractions`; targets `net8.0`. `System.Threading.Channels` ships in-box with `net8.0`. Does not reference ASP.NET Core, EF Core, RabbitMQ, or application projects.

### Value types

- `IIntegrationEvent` — marker interface for typed events.
- `IntegrationEvent` — `abstract record` with `EventId`, `OccurredAt`, `CorrelationId`. Two protected convenience constructors generate a fresh `EventId`.
- `IntegrationEventEnvelope` — transport-agnostic wire format with `MessageId`, `PayloadType`, `PayloadJson`, `OccurredAt`, optional `CorrelationId`.

### Contracts

- `IEventBus` — `PublishAsync(envelope, cancellationToken)`.
- `IIntegrationEventEnvelopeSerializer` — `Serialize(event, cancellationToken)`. Default `IntegrationEventEnvelopeSerializer` is `System.Text.Json`-backed, reads the current time from `IClock` when the event does not already carry a non-default `OccurredAt`, and defaults to camelCase JSON options.
- `IIntegrationEventEnvelopeDeserializer` — `Deserialize(envelope, cancellationToken)`. Default `IntegrationEventEnvelopeDeserializer` resolves the payload type from `PayloadType` (via `Type.GetType` plus a fallback scan of loaded assemblies) and reconstructs the typed event with the envelope's `OccurredAt` and `CorrelationId`.
- `IIntegrationEventHandler<TEvent>` — `ConsumerName` plus `HandleAsync(payload, envelope, cancellationToken)`.

### Default adapter

- `InProcessEventBus` — `IEventBus` + `IAsyncDisposable` with a bounded `Channel<IntegrationEventEnvelope>` (default capacity 1024 from `EventingOptions.InProcessBoundedCapacity`). Back-pressures the publisher through `ChannelWriter.WaitToWriteAsync`, consumes `IClock` and the registered deserializer, resolves every `IIntegrationEventHandler<TEvent>` whose `TEvent` matches the deserialised payload's runtime type from the host's `IServiceProvider`, and logs+swallows consumer failures so the bus never crashes. Disposal is idempotent.

### Options

- `EventingOptions` — `InProcessBoundedCapacity` (default `1024`), `SectionName` constant `"Eventing"`.

### Registration

- `AddPlatformEventing(IServiceCollection)` and `AddPlatformEventing(IServiceCollection, Action<EventingOptions>)` — bind `EventingOptions` and register `IIntegrationEventEnvelopeSerializer` and `IIntegrationEventEnvelopeDeserializer`. Also call `TryAddSingleton<IClock>(_ => new SystemClock())` so the package works when no host clock is registered.

## Platform.Eventing.Contracts and Platform.Eventing.EfCore

`Platform.Eventing.Contracts` provides framework-neutral durable outbox/inbox
records, lease and retry states, store interfaces, `IDurableEventPublisher`,
and thread-safe in-memory stores. `Platform.Eventing.EfCore` is optional and
maps those records into an application-owned EF Core context; it does not own
migrations or select a transport. See
[`platform-eventing-durable.md`](platform-eventing-durable.md).
- `AddPlatformEventingInProcess(IServiceCollection)` — additionally registers `InProcessEventBus` and binds it to `IEventBus`.

## Platform.Caching

`Platform.Caching` is the provider-neutral cache boundary. It depends on `Platform.Core`,
`Microsoft.Extensions.Options`, and dependency-injection abstractions; it has no Redis,
HybridCache, ASP.NET Core, EF Core, or JSON dependency.

### Contracts and keys

- `ICacheStore` — asynchronous get, get-or-create, set, remove, and portable tag invalidation.
- `CacheReadResult<T>` / `CacheOperationResult` — explicit hit, miss, unavailable, and operation
  states with safe `CacheFailure` metadata.
- `CacheEntryOptions` — absolute expiration and validated portable tags.
- `CacheKey` / `CacheKeyBuilder` — bounded whitespace-free physical keys with explicit application
  and tenant prefixes.
- `ICacheProviderStatus` — safe provider health state for fail-open/fail-closed application policy.
- `CacheTelemetry` — stable activity, metric, hit, miss, and failure names that do not include raw
  keys or values.

### Implementations

- `InMemoryCacheStore` — thread-safe local store using `IClock`, absolute expiry, and tag removal.
- `Platform.Caching.Hybrid` / `HybridCacheStore` — optional Microsoft HybridCache adapter.
- `Platform.Caching.Redis` / `RedisCacheStore` — optional Redis adapter using an application-owned
  `ICacheValueSerializer`, bounded operation waits, tag sets, and unavailable-provider results.

See [`platform-caching.md`](platform-caching.md) for authority, versioning, tenant isolation,
failure policy, and migration guidance.

## Platform.Storage

`Platform.Storage` provides provider-neutral object upload, download, metadata, delete, and
presigned-operation contracts. It depends on `Platform.Core` and dependency-injection abstractions
only; cloud and filesystem dependencies remain in separate adapters.

- `StorageObjectKey` — bounded key rejecting control characters, traversal, absolute paths, and
  empty segments.
- `StorageUploadRequest`, `StorageDownloadResult`, `PresignRequest`, and `PresignedOperation` —
  explicit content, method, expiry, and size constraints.
- `IObjectStorage` — async object lifecycle boundary; `IStorageProviderStatus` exposes safe health.
- `Platform.Storage.Local` / `LocalFileStorage` — atomic local filesystem adapter.
- `Platform.Storage.S3` / `S3Storage` — optional AWS/S3-compatible adapter using an application-owned
  client and bucket configuration.

Applications own authorization, tenant prefixes, metadata tables, retention, scanning, and object
migration. See [`platform-storage.md`](platform-storage.md).

## Platform.Quota

`Platform.Quota` provides provider-neutral capacity checks and an atomic reservation lifecycle. It
depends on `Platform.Core` and dependency-injection abstractions only; it does not define plans,
prices, invoices, wallets, ledgers, or persistence models.

- `QuotaSubject`, `QuotaResource`, and `QuotaOperationKey` — bounded opaque identifiers.
- `QuotaWindow`, `QuotaDecision`, `QuotaSnapshot`, and `QuotaReservation` — explicit window and
  explanatory usage models.
- `IQuotaStore` — `CheckAsync`, idempotent `ReserveAsync`, `SettleAsync`, `ReleaseAsync`, and
  snapshot inspection.
- `InMemoryQuotaStore` — thread-safe `IClock`-driven implementation with expiration and
  `GetReservation` inspection.
- `IQuotaLimitResolver` — optional seam for application-owned entitlement-to-limit resolution.
- `Platform.Quota.Testing` — `QuotaScenarioBuilder` for deterministic test setup.

See [`platform-quota.md`](platform-quota.md) for unit conversion, reconciliation, migration, and
ownership boundaries.

## Platform.Idempotency

Framework-neutral idempotency contract. Depends on `Platform.Core`, `Microsoft.Extensions.Options`, and `Microsoft.Extensions.DependencyInjection.Abstractions`; targets `net8.0`. Does not reference ASP.NET Core, EF Core, StackExchange.Redis, or application projects.

### Value types

- `IdempotencyRecord` — sealed record with `Key`, `Fingerprint`, `StatusCode`, `ContentType`, `ResponseHeaders` (defaulted to empty ordinal-case-insensitive map), `ResponseBody`, `CreatedAt`.

### Contracts

- `IIdempotencyStore` — `TryGetAsync(key, ct)`, `SaveAsync(record, ct)`, `EvictExpiredAsync(ct)`. `SaveAsync` rejects keys longer than the configured `MaxKeyLength` with `ArgumentException`.
- `IdempotencyMetrics` — `Hit`, `Miss`, `FingerprintMismatch`. The platform does not ship a default implementation.

### Helpers

- `RequestFingerprint.Compute(method, route, bodyHash)` — stable, hex-encoded SHA-256 of the normalised request. The method is upper-cased before hashing.
- `RequestFingerprint.ComputeBodyHash(ReadOnlySpan<byte>)` and `ComputeBodyHash(string)` — produce the matching `bodyHash` input.

### Default backend

- `InMemoryIdempotencyStore` — thread-safe default backed by a `Dictionary<string, IdempotencyRecord>` guarded by a lock. Consumes `IClock` for retention and key-staleness checks (no `DateTimeOffset.UtcNow`), honours the documented `RetentionSeconds` window, and treats expired records as misses on lookup.

### Options

- `IdempotencyOptions` — `Enabled` (default `true`), `Storage` (default `"memory"`), `RetentionSeconds` (default `86400`), `MaxKeyLength` (default `256`), `HeaderName` (default `"Idempotency-Key"`), `SectionName` constant `"Idempotency"`. Metric-name constants: `HitMetric = "idempotency.hit"`, `MissMetric = "idempotency.miss"`, `FingerprintMismatchMetric = "idempotency.fingerprint_mismatch"`.

### Registration

- `AddPlatformIdempotency(IServiceCollection)` and `AddPlatformIdempotency(IServiceCollection, Action<IdempotencyOptions>)` — bind `IdempotencyOptions` and register `IIdempotencyStore`. When `IdempotencyOptions.Enabled` is `false` the registration resolves a no-op store so consumers can opt out without changing call sites.

## Platform.RateLimiting

Framework-neutral rate-limit contract. Depends on `Platform.Core`, `Microsoft.Extensions.Options`, and `Microsoft.Extensions.DependencyInjection.Abstractions`; targets `net8.0`. Does not reference ASP.NET Core, EF Core, StackExchange.Redis, or application projects.

### Value types

- `RateLimitKey` — `readonly record struct` with `Policy` and `Subject`. `Composite` returns the documented `policy|subject` form.
- `RateLimitDecision` — sealed record with `Allowed`, `Limit`, `Remaining`, `RetryAfterSeconds`.
- `RateLimitBypassDecision` — sealed record with `Allowed` and optional `Label`.
- `IRateLimiterBackendStatus` — sealed record with `Provider`, `Available`, optional `Detail`.
- `HttpContextAbstraction` — framework-neutral wrapper with `BypassToken`, optional `Headers`, optional `RemoteIp`.

### Contracts

- `IRateLimiter` — `CheckAsync(key, cancellationToken)`. Implementations read the current time from an injected `IClock`.
- `IRateLimitBypassResolver` — `Evaluate(context)`.
- `IRateLimiterBackendStatusProvider` — `GetStatus()`. Consumer adapters can replace the registration with a provider-specific implementation.

### Default backend

- `InMemoryRateLimiter` — thread-safe default that tracks a per-key windowed counter and returns a `RateLimitDecision` with the documented fields. Reads the current time from `IClock` (no `DateTimeOffset.UtcNow`), honours the configured `WindowSeconds`, and rolls the bucket over when the window elapses. Different subjects under the same policy have independent buckets.
- `ConfigurationRateLimitBypassResolver` — default `IRateLimitBypassResolver` that matches documented bypass tokens (case-insensitive) against `HttpContextAbstraction.BypassToken`.
- `InMemoryRateLimiterBackendStatusProvider` — reports `Provider = "memory"`, `Available = true` so readiness checks can observe the backend without changing call sites.

### Policy catalog

- `RateLimitPolicies` — catalog of `RateLimitPolicyOptions { Name, Limit, WindowSeconds }`. `Find(name)` returns the matching entry or `null`. `Default()` returns the documented catalog: `feed` (60/60s), `search` (30/60s), `uploads` (5/60s), `downloads` (10/60s), `account-recovery` (3/3600s). Consumers register a custom `RateLimitPolicies` before `AddPlatformRateLimiting` to override the catalog.

### Options

- `RateLimitingOptions` — `Policies` (defaults to `RateLimitPolicies.Default()`), `BypassTokens` (defaults to empty), `SectionName` constant `"RateLimiting"`.

### Registration

- `AddPlatformRateLimiting(IServiceCollection)` and `AddPlatformRateLimiting(IServiceCollection, Action<RateLimitingOptions>)` — bind `RateLimitingOptions` and register `IRateLimiter`, `IRateLimitBypassResolver`, `IRateLimiterBackendStatusProvider`, and `IClock` when no implementation is already present.

## Platform.Persistence.EfCore

Optional provider-neutral EF Core conventions. Depends on `Platform.Core`, EF Core, relational
abstractions, and health-check abstractions; targets `net8.0`. It does not own application
entities, contexts, migrations, tenants, or business filters.

- `IAuditableEntity`, `ISoftDeletable`, `ITenantScoped`, and `ITenantScope` — minimal contracts
  for explicitly selected application entities and scopes.
- `PlatformSaveChangesInterceptor` — opt-in audit and soft-delete interception using replaceable
  `IClock` and `IActorAccessor` services.
- `PageRequest`, `PageResult<T>`, `PageExtensions`, `Specification<T>`, and
  `SpecificationEvaluator` — bounded paging and predicate composition over consumer queryables.
- `EfCoreMigrationStatusReader`, `MigrationStatus`, and `EfCoreReadinessCheck` — read-only
  connection/migration status; no implicit migration application.
- `AddPlatformPersistenceEfCore` and `ModelBuilderExtensions` — explicit registration and
  per-entity soft-delete/tenant filter helpers.

## Platform.Persistence.Postgres

Optional Npgsql adapter over `Platform.Persistence.EfCore`. It contains only PostgreSQL options
configuration (`UsePlatformPostgres`) and does not add contexts, migrations, or domain behavior.

## Platform.Persistence.Multitenancy

Optional multitenancy adapter over `Platform.Persistence.EfCore` and `Platform.AspNetCore`.
Targets `net8.0`. Depends on `Platform.Core`, `Platform.AspNetCore`, `Platform.Persistence.EfCore`,
EF Core, and health-check abstractions. The package owns no tenant entities, tenant catalog,
migrations, or database credentials.

### Tenancy contracts (`Platform.Core`)

- `ITenantInfo` — application-supplied tenant metadata.
- `TenantResolutionResult`, `TenantResolutionStatus` — resolution outcome (resolved, global operation, or unresolved).
- `ITenantResolver` — application-owned resolver; the platform never inspects HTTP headers or claims directly.
- `IAmbientTenantScope`, `AmbientTenantScope` — current ambient state; the package adds a generation counter so cached collaborators can detect replacement.
- `ITenantScopeFactory` — installs and restores ambient scopes for background handlers and explicit global operations.
- `ITenantConnectionResolver`, `TenantConnectionDescriptor` — connection routing seam for shared and dedicated tenant databases.
- `ITenantConnectionReadinessProbe`, `TenantConnectionReadinessResult` — readiness probe contract.
- `TenantScopeNotResolvedException` — fail-closed outcome when no scope is in effect.

### Adapter

- `AddPlatformPersistenceMultitenancy` and `MultitenancyOptions` — bounded registration with safe defaults.
- `AmbientTenantScopeStore`, `ITenantScopeAccessor` — scoped accessor over an `AsyncLocal<AmbientTenantScope>`.
- `TenantScopeFactory` — default `ITenantScopeFactory` with snapshotted prior-scope restoration.
- `TenantScopeMiddleware` and `UsePlatformMultitenancy` — ASP.NET Core middleware that resolves and installs the ambient scope.
- `IGlobalTenantEntity` — explicit global marker that opts an entity out of the default filter.
- `TenantModelBuilderExtensions.ApplyDefaultTenantFilters(scope[, behavior, overrideTenantId])` — explicit per-`DbContext` model customizer.
- `ScopedTenantConnectionProvider` — returns the connection descriptor for the current scope; caches within a single generation.
- `TenantConnectionReadinessCheck` and `AddPlatformTenantConnectionReadinessCheck` — readiness aggregation for tenant connections.

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
| `tests/Platform.Architecture.Tests` | Dependency-direction guardrails (no production project references test projects, ASP.NET Core, EF Core, or Stripe; `FrameworkReference` is allowed only for `Platform.AspNetCore`; `Platform.Core` and `Platform.Billing.Contracts` declare no `<PackageReference>` entries; per-package forbidden-reference rules for `Platform.Jobs`, `Platform.Mailing`, `Platform.Eventing`, `Platform.Idempotency`, and `Platform.RateLimiting`). |
| `tests/Platform.Core.Tests` | Unit tests for time, results, context, and audit contracts. |
| `tests/Platform.AspNetCore.Tests` | Unit tests for the ProblemDetails mapper, exception middleware, and correlation middleware, plus a `TestServer` integration test for the minimal host (known failure → ProblemDetails, unknown failure → sanitized 500, correlation generation, health endpoint). |
| `tests/Platform.Billing.Contracts.Tests` | Unit tests for identifiers, subscription status and period boundaries, entitlement defaults, feature check decisions, usage-meter contract (in-memory implementation), and processed-event idempotency. |
| `tests/Platform.Jobs.Tests` | Unit tests for the `RecurringJobAttribute` reflection, the `RecurringJobDescriptor` value type, the `IJobTelemetry` surface, and `BackgroundJobsOptions` defaults, plus a `TestServer` integration test for `AddPlatformJobs` defaults and configuration overrides. |
| `tests/Platform.Mailing.Tests` | Unit tests for `MailAddress`, `MailAttachment`, `MailMessage` validation (text-only / html-only / both / neither / empty recipients / null sender / empty subject), `MailSendResult` semantics, `MailTemplateId` implicit conversions, `RenderedMailTemplate.Create` validation, and `MailingOptions` defaults, plus a `TestServer` integration test for `AddPlatformMailing` defaults, configuration overrides, and a consumer-registered `IMailService`. |
| `tests/Platform.Eventing.Tests` | Unit tests for the envelope shape, the default `IntegrationEventEnvelopeSerializer` and `IntegrationEventEnvelopeDeserializer`, the `InProcessEventBus` (typed dispatch, consumer-failure isolation, idempotent disposal, bounded-capacity null guard), and `EventingOptions` defaults, plus a `TestServer` integration test for `AddPlatformEventing` and `AddPlatformEventingInProcess` defaults, configuration overrides, and a consumer-published envelope flowing to a typed `IIntegrationEventHandler<>`. |
| `tests/Platform.Idempotency.Tests` | Unit tests for `RequestFingerprint` stability + method normalisation + body-hash helper, `IdempotencyOptions` defaults and metric-name constants, `InMemoryIdempotencyStore` round-trip / null-or-empty-key / oversize-key / retention sweep / expired-record-as-miss / null-dependency guards, plus a `TestServer` integration test for `AddPlatformIdempotency` defaults, configuration overrides, save/try-get round-trip, eviction sweep driven by a `MutableClock`, and the no-op path when `Idempotency:Enabled = false`. |
| `tests/Platform.RateLimiting.Tests` | Unit tests for `RateLimitPolicies` default catalog + `Find` + invalid-entry dropping + null guard, `RateLimitingOptions` defaults, `InMemoryRateLimiter` (first request, burst over limit, window roll-over, per-subject isolation, unknown/empty policy / subject rejection, null dependency guards), `ConfigurationRateLimitBypassResolver`, and `InMemoryRateLimiterBackendStatusProvider`, plus a `TestServer` integration test for `AddPlatformRateLimiting` defaults, configuration overrides, limiter decisions through DI, and the readiness surface. |
| `tests/Platform.Persistence.EfCore.Tests` | In-memory and SQLite tests for explicit options, audit/soft-delete interception, tenant filters, paging/specification helpers, concurrent independent contexts, read-only migration status, and readiness behavior. |
| `tests/Platform.Persistence.Multitenancy.Tests` | Multitenancy options validation, scope factory installation/restoration, EF Core model filter application, global-entity opt-out, scoped connection routing (tenant/global/shared), connection caching, HTTP middleware TestServer coverage (resolved/disabled/length-bounded), and tenant readiness check aggregation. |
| `tests/Platform.Persistence.Postgres.Tests` | Provider-boundary test for PostgreSQL options configuration. |
| `tests/Platform.Testing.Tests` | Unit tests for `ControllableClock`, `SubscriptionBuilder`, `EntitlementBuilder`, `FakeEntitlementStore`, and `RecordingUsageMeter`. |
| `tests/Platform.Webhooks.Tests` | Synthetic signature, replay, normalization, and HTTP mapping coverage for the inbound and outbound flows. |
| `tests/Platform.Web.Edge.Tests` | Telemetry contract, CORS option and TestServer coverage, HTTP resilience option/handler/circuit-breaker coverage, OpenAPI registry and TestServer coverage. |
| `tests/Platform.ConsumerConformance` | Test-only consumer fixture that restores platform packages from a local NuGet feed and verifies registration, replacement, health, failure classification, opt-in boundaries, and end-to-end host behavior. Driven by `scripts/conformance.sh`; intentionally not part of `Platform.sln`. |
# Platform.Starter

`Platform.Starter` composes the opt-in web, identity, administration, billing, and mailing
boundaries. It owns no application persistence or provider implementation. See
[`platform-starter.md`](platform-starter.md).

# Platform UI

`Platform.UI.Razor` ships token-backed Razor static assets. The token source and generated
React artifacts live under `ui/`: `@platform/design-tokens`, `@platform/react-ui`, and
`@platform/react-shell`. React and Razor share semantic tokens and state conventions while
retaining independent implementations.

# Platform.Notifications

`Platform.Notifications` provides channel-neutral email/SMS intents, delivery outcomes,
bounded retries, idempotency integration, and job scheduling helpers. `Platform.Notifications.Testing`
contains the deterministic in-memory provider. Providers and templates remain application-owned;
see [`platform-notifications.md`](platform-notifications.md).

# Platform.Webhooks

`Platform.Webhooks.Contracts` provides provider-neutral inbound verification, replay
suppression, outbound subscriptions, delivery attempts, retry options, and safe failure
metadata. `Platform.Webhooks.AspNetCore` adds the `HttpRequest` reader and a default
`HttpClient`-backed sender. `Platform.Webhooks.EfCore` exposes inbox and delivery entity
configurations for the application's `DbContext`. See [`platform-webhooks.md`](platform-webhooks.md).

# Platform.Observability

`Platform.Observability` provides opt-in host observability registration with safe
correlation, logging enrichment, tracing, metrics, and provider-status contracts. It depends
on `Platform.Core`, `Platform.Web.Telemetry`, the `Microsoft.AspNetCore.App` framework
reference, and the four `Microsoft.Extensions.*` abstractions. It does not reference EF Core,
PostgreSQL, Redis, AWS SDK, OpenTelemetry, Serilog, or Stripe. The platform owns no
exporter, collector, dashboard, Serilog sink configuration, or product log schema.

- `PlatformObservabilityOptions` — `ApplicationName`, `ApplicationVersion`,
  `EnvironmentName`, `CorrelationHeader`, `AcceptIncomingCorrelationHeader`,
  `MaxCorrelationIdLength`, `MaxTagLength`, `MaxOperationLength`,
  `TruncateOversizedValues`, `EnableRequestEnrichment`, `EnableProviderEnrichment`,
  `EnableHostLifecycle`, `RecordRequestBodyPreview`. Validated at registration time.
- `PlatformObservabilityNames` — stable activity source, meter, operation, tag, and
  outcome names. Do not rename.
- `IPlatformObservabilityRedactor` / `DefaultPlatformObservabilityRedactor` —
  redaction contract and `[REDACTED]` placeholder.
- `PlatformObservabilitySafeValuePolicy` — `BoundTag` (length-bound only) for
  already-safe values; `RedactTag` and `RedactOperation` (redact + bound) for
  known-sensitive values; `RequireOperation` and `RequireCorrelationId` validators.
- `PlatformDiagnostics` — framework-owned `ActivitySource` and `Meter` accessors plus
  the latest observed duration and counter helpers.
- `IPlatformActivityRecorder` / `DefaultPlatformActivityRecorder` — bounded
  activities for host, request, and provider call paths.
- `IPlatformObservabilityProviderStatusSource` /
  `DefaultPlatformObservabilityProviderStatusSource` — safe availability reporting.
- `PlatformObservabilityProviderCall` / `IPlatformObservabilityProviderRecorder` /
  `DefaultPlatformObservabilityProviderRecorder` — provider call enrichment bridge.
- `IPlatformCorrelationAccessor` / `HttpPlatformCorrelationAccessor` — correlation
  identifier accessor.
- `ICorrelationIdGenerator` / `DefaultCorrelationIdGenerator` — bounded identifier
  generation and normalization.
- `PlatformObservabilityCorrelationMiddleware` — echoes the bounded correlation
  identifier on the response and emits the request enrichment activity.
- `PlatformObservabilityHostLifetime` — `IHostedLifecycleService` that records host
  startup and shutdown activities and metrics.
- `AddPlatformObservability(IServiceCollection)` and the `Action<...>` overload.
- `UsePlatformObservability(IApplicationBuilder)` — adds the correlation middleware.

See [`platform-observability.md`](platform-observability.md) for adoption and migration
guidance.

# Platform.Web edge packages

Four small, independently adoptable packages extend `Platform.Web` with adjacent web
capabilities. Each is optional; consumers adopt one at a time and the core runtime
behaviour is unchanged. See [`platform-web-edge.md`](platform-web-edge.md) for the
adoption guide.

## Platform.Web.Telemetry

Framework-neutral redaction-safe web telemetry names, option-validation helpers, and a
structured log sink. Depends only on `Platform.Core`, `Microsoft.Extensions.DependencyInjection.Abstractions`,
`Microsoft.Extensions.Logging.Abstractions`, `Microsoft.Extensions.Options`, and
`System.Diagnostics.DiagnosticSource`; targets `net8.0`. Does not reference ASP.NET Core,
EF Core, Polly, Swashbuckle, or NSwag.

- `PlatformWebTelemetryNames` — stable activity source, operation, and metric names
  (`RequestActivitySource`, `ProviderActivitySource`, `RequestOperation`,
  `ProviderOperation`, `RequestCountMetric`, `ProviderCountMetric`, plus stable
  tag keys).
- `PlatformWebTelemetryOptions` — `ApplicationName`, `MaxTagLength`,
  `MaxOperationLength`, `TruncateOversizedValues`. Validated at registration time.
- `PlatformWebRequestEvent` / `PlatformWebProviderEvent` — typed event records for
  requests and provider calls. `Default` constructor enforces non-empty operation,
  valid status code, and non-negative duration.
- `IPlatformWebTelemetryRedactor` / `DefaultPlatformWebTelemetryRedactor` — redaction
  contract and `[REDACTED]` placeholder.
- `PlatformWebTelemetrySafeValuePolicy` — bounds and redacts tag and operation values.
- `IPlatformWebTelemetry` / `DefaultPlatformWebTelemetry` — structured log sink.
- `PlatformOptionsValidator` and `AddValidatedOptions<T>()` — shared option
  validation helper for any platform options type.
- `AddPlatformWebTelemetry(IServiceCollection)` and the `Action<...>` overload — register
  the redactor, the default telemetry sink, and validated options.

## Platform.Web.Cors

Optional ASP.NET Core CORS configuration and production-time validation. Depends on
`Platform.Core`, `Platform.Web.Telemetry`, `Microsoft.AspNetCore.App` (via
`FrameworkReference`); targets `net8.0`. Does not reference EF Core, Stripe, or
application projects.

- `PlatformWebCorsPolicyOptions` — per-policy `Name`, `AllowedOrigins`,
  `AllowedHeaders`, `ExposedHeaders`, `AllowedMethods`, `AllowCredentials`,
  `PreflightMaxAge`. `Validate(environmentName)` rejects wildcard origins with
  credentials, wildcard origins in production, missing origins in production, and
  non-absolute or non-HTTPS origins outside development.
- `PlatformWebCorsOptions` — `Environment`, `Policies` collection, `RoutePrefix`.
  `Validate()` enforces non-empty policies, unique names, and per-policy validation.
- `AddPlatformWebCors(IServiceCollection)`, the `Action<...>` overload, and the
  `IHostEnvironment` overload — register validated options, telemetry, and named
  CORS policies.
- `UsePlatformWebCors()` and `UsePlatformWebCors(policyName)` — call the underlying
  `UseCors` with the supplied policy.

## Platform.Web.Resilience

Optional `HttpClient` resilience conventions (retry, timeout, circuit breaker) with
bounded defaults. Depends on `Platform.Core`, `Platform.Web.Telemetry`,
`Microsoft.AspNetCore.App`, and `Microsoft.Extensions.Http`; targets `net8.0`. Does
not reference EF Core, Polly, or application projects.

- `PlatformHttpResilienceOptions` — `AttemptTimeout` (5s), `MaxRetryAttempts` (3),
  `RetryBaseDelay` (200ms), `CircuitBreakerFailureRatio` (0.5),
  `CircuitBreakerSamplingDuration` (30s), `CircuitBreakerMinimumThroughput` (10),
  `CircuitBreakerBreakDuration` (30s), `IdempotentMethods`, `EmitRetryAttemptHeader`.
  Validated at registration time.
- `HttpResilienceDecision` — `None`, `Retried`, `TimedOut`, `CircuitBroken`.
- `HttpResilienceEvent` — typed event with `Decision`, `Operation`, `Attempt`,
  `StatusCode`.
- `IHttpResilienceTelemetry` / `DefaultHttpResilienceTelemetry` — bridge to the
  platform web telemetry sink.
- `PlatformHttpResilienceHandler` — `DelegatingHandler` that retries only
  idempotent methods (`GET`, `HEAD`, `OPTIONS`, `PUT`/`DELETE` with `If-Match`,
  and any explicitly opted-in method), emits `X-Retry-Attempt` on each attempt,
  applies a per-attempt timeout, and trips a circuit breaker after
  `CircuitBreakerMinimumThroughput` failures at or above the failure ratio.
- `PlatformHttpCircuitOpenException` — thrown while the breaker is open.
- `AddPlatformHttpResilience(IServiceCollection)` and the `Action<...>` overload.
- `AddPlatformHttpResilience(IHttpClientBuilder)` — adds the handler to a named
  client.

## Platform.Web.OpenApi

Optional OpenAPI document registry and explicit mapping helper. Depends on
`Platform.Core`, `Platform.Web.Telemetry`, and `Microsoft.AspNetCore.App` (via
`FrameworkReference`); targets `net8.0`. Does not reference Swashbuckle, NSwag,
EF Core, or application projects.

- `PlatformWebOpenApiOptions` — `Documents`, `RoutePrefix` (`/openapi`).
  `Validate()` enforces unique names, absolute paths, and bounded options.
- `PlatformWebOpenApiDocumentOptions` — `Name`, `Path`, `ContentType`, `Title`,
  `OpenApiVersion`.
- `IPlatformOpenApiDocumentProvider` — `SupportedDocuments` plus
  `GetDocument(name)`. The platform does not own a specific OpenAPI
  implementation; applications supply the JSON.
- `IPlatformOpenApiDocumentRegistry` / `PlatformOpenApiDocumentRegistry` — aggregate
  providers and resolve by name. Returns `null` for unknown names.
- `Providers.StaticPlatformOpenApiDocumentProvider` — emits a minimal placeholder
  document for testing and bootstrapping.
- `AddPlatformWebOpenApi(IServiceCollection)` and the `Action<...>` overload.
- `AddPlatformOpenApiDocument(PlatformWebOpenApiDocumentOptions)` — convenience
  registration of a static document provider.
- `MapPlatformOpenApiDocument(name)` — maps `/openapi/{name}.json`. Authorization
  metadata applied with `RequireAuthorization()` is preserved.
- `MapPlatformOpenApiDocuments()` — maps every registered document.

## Platform.FeatureManagement

Optional ASP.NET Core feature-flag integration. Depends on `Platform.Core`,
`Platform.Web.Telemetry`, the `Microsoft.AspNetCore.App` framework reference, and
`Microsoft.FeatureManagement`; targets `net8.0`. Does not own feature names,
rollout state, billing plans, tenant records, EF Core, or Polly. The host owns the
`FeatureManagement` configuration section and the rollout rules.

### Context

- `FeatureContext` — record carrying optional `TenantId`, `Subject`, and bounded
  `Properties` (no secrets or rollout state).
- `IFeatureContextResolver` — `ResolveAsync(cancellationToken)` returns the
  application-provided `FeatureContext`. `DefaultFeatureContextResolver` returns an
  empty context; consumers replace it before registration to supply tenant/subject
  state.

### Filters and gating

- `PlatformTenantFeatureFilter` — `[FilterAlias("PlatformTenant")]` filter that
  reads `TenantId` from the resolver and compares it against the per-feature
  `AllowedTenants` parameter; returns `false` when the tenant is empty. Registered
  automatically when the integration is enabled.
- `FeatureGateEndpointFilter` and
  `RequireFeature(this RouteHandlerBuilder, string featureName)` — gate an endpoint
  behind a flag. On a disabled feature the filter returns a safe `ProblemDetails`
  built from `FeatureManagementOptions`; no flag or rollout state leaks.

### Options

- `FeatureManagementOptions` — `Enabled` (default `true`), `SectionName` (default
  `"FeatureManagement"`), `DisabledStatusCode` (default `404`), `DisabledTitle`
  (default `"Feature disabled"`). `Validate()` bounds the status code to 100–599 and
  rejects empty section or title.

### Registration

- `AddPlatformFeatureManagement(IServiceCollection, IConfiguration)` and the
  `Action<FeatureManagementOptions>` overload — register validated options and the
  platform web telemetry sink, register the tenant filter when `Enabled`, and
  register `IFeatureContextResolver` (default empty). The host owns the feature
  definition section.

## Platform.Http.Resilience

Optional outbound HTTP resilience integration. Framework-neutral; targets `net8.0`.
Depends on `Platform.Core`, `Platform.Web.Telemetry`, `Microsoft.Extensions.Http`,
and `Microsoft.Extensions.Http.Resilience`. Does not reference ASP.NET Core, EF
Core, `Microsoft.FeatureManagement`, or Polly directly, and adds no provider SDKs.
Each named `HttpClient` receives its own bounded pipeline; applications own provider
selection and per-service overrides.

### Options

- `PlatformHttpResilienceOptions` — `Enabled` (default `true`), `MaxRetryAttempts`
  (default `3`, bounded 1–10), `RetryBaseDelay` (default `500ms`), `AttemptTimeout`
  (default `5s`), `TotalTimeout` (default `30s`), `CircuitBreakerFailureRatio`
  (default `0.5`), `CircuitBreakerSamplingDuration` (default `30s`),
  `CircuitBreakerMinimumThroughput` (default `10`), `CircuitBreakerBreakDuration`
  (default `30s`), `MaxConcurrentCalls` (default `100`), `MaxQueueLength` (default
  `1000`), `IdempotentMethods` (default empty; the safe set `GET`/`HEAD`/`OPTIONS`/
  `TRACE` is always included). `Validate()` enforces every bound and rejects empty
  idempotent entries.

### Telemetry

- `PlatformHttpResilienceDecision` — `None`, `Retried`, `CircuitOpen`,
  `ConcurrencyRejected`, `Cancelled`.
- `PlatformHttpResilienceEvent` — bounded record (`Decision`, `Operation`, `Attempt`,
  `StatusCode`); `Operation` never includes URLs, secrets, or headers.
- `IPlatformHttpResilienceTelemetry` / `DefaultPlatformHttpResilienceTelemetry` —
  record sink; the default is a no-op that consumers replace with an
  application-owned sink.

### Registration

- `AddPlatformHttpResilience(this IHttpClientBuilder)` and the
  `Action<PlatformHttpResilienceOptions>` overload — bind validated options,
  register the telemetry sink (default no-op) and `PlatformHttpResilienceHandler`,
  and add the handler as an HTTP message handler. When `Enabled` is `false` the
  handler is a pass-through.

### Behavior

- The pipeline applies, in order: a total timeout, retry (exponential back-off with
  jitter, idempotent methods only by default), a per-attempt timeout, a circuit
  breaker (trips after the configured failure ratio and minimum throughput), and a
  concurrency limiter (bounded queue). The HTTP method is carried through a
  resilience context property so idempotency classification is reliable. Caller
  cancellation is preserved and recorded without being retried. Non-idempotent
  methods are never replayed unless explicitly added to `IdempotentMethods`.

# Platform.Web.Edge.Tests

`tests/Platform.Web.Edge.Tests` exercises all four edge packages:

- Telemetry contract tests: stable names, default option validation, safe-value
  policy, default telemetry registration, and validator helper.
- CORS option tests: wildcard+credentials rejection, production origin
  requirement, localhost exception, duplicate policy names, empty collection, and
  runtime validation.
- CORS integration tests: allowed origin receives the header, disallowed origin
  does not, and wildcard origin is served without credentials.
- Resilience option tests: defaults, bounded ranges, and runtime validation.
- Resilience handler tests: transient GET retries, POST is not retried by
  default, explicit idempotent override, successful response is not retried,
  retry attempt header is emitted, and circuit breaker opens after the threshold.
- OpenAPI integration tests: registered document is served, unregistered document
  is 404, multiple documents resolve independently.
