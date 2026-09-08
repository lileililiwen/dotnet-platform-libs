# Roadmap

The roadmap is organized into three phases. Items marked **Done** are implemented, archived, and have generated capability specs in `openspec/specs/`.

## Phase 1: Foundation — Done

1. `platform-repository-foundation` — solution, four production projects, central MSBuild and package metadata, dependency-direction guardrails.
2. `platform-core-contracts` — `IClock`, `Error`, `Result`, `Result<T>`, `CallerContext`, `IAuditable` in `Platform.Core`.
3. `platform-aspnetcore-foundation` — `AddPlatformAspNetCore`, `UsePlatformAspNetCore`, `MapPlatformEndpoints`, sanitized `ProblemDetails`, correlation middleware/accessor, and health-check helpers in `Platform.AspNetCore`.

## Phase 2: Reusable product contracts — Done

4. `platform-entitlement-contracts` — opaque identifiers, normalized subscription and entitlement snapshots, structured feature-check decisions, replaceable usage-meter interface, and idempotent processed-event store in `Platform.Billing.Contracts`.
5. `platform-testing-toolkit` — `ControllableClock`, `SubscriptionBuilder`, `EntitlementBuilder`, `FakeEntitlementStore`, and `RecordingUsageMeter` in `Platform.Testing`.

## Phase 3: Pilot adoption — Done

6. `platform-extraction-ratelimiting` — `IRateLimiter`, `InMemoryRateLimiter`, `RateLimitDecision`, `RateLimitKey`, `RateLimitPolicies` (documented default catalog), `IRateLimitBypassResolver`, `IRateLimiterBackendStatusProvider`, `RateLimitingOptions`, and `AddPlatformRateLimiting` in `Platform.RateLimiting`.
7. `platform-extraction-mailing` — `MailAddress`, `MailAttachment`, `MailMessage`, `MailSendOutcome`, `MailSendResult`, `IMailService`, `MailTemplateId`, `IMailTemplateRenderer<TModel>`, `RenderedMailTemplate`, `MailingOptions`, and `AddPlatformMailing` in `Platform.Mailing`.
8. `platform-extraction-eventing` — `IIntegrationEvent`, `IntegrationEvent`, `IntegrationEventEnvelope`, default `System.Text.Json` serializer/deserializer, `IIntegrationEventHandler<TEvent>`, `IEventBus`, `InProcessEventBus` (bounded `Channel<T>`), `EventingOptions`, and `AddPlatformEventing` / `AddPlatformEventingInProcess` in `Platform.Eventing`.
9. `platform-extraction-idempotency` — `IdempotencyRecord`, `IIdempotencyStore`, `InMemoryIdempotencyStore`, `RequestFingerprint`, `IdempotencyOptions` (with documented metric-name constants), `IdempotencyMetrics`, and `AddPlatformIdempotency` in `Platform.Idempotency`.
10. `platform-extraction-jobs` — `IJobDispatcher`, `IRecurringJobHandler`, `IRecurringJobRegistry`, `IJobTelemetry`, `RecurringJobAttribute`, `RecurringJobDescriptor`, `JobPayload`, `BackgroundJobsOptions`, and `AddPlatformJobs` in `Platform.Jobs`.

## Deferred

- a shared EF Core persistence package;
- a shared Stripe implementation;
- shared invoice, wallet, or tenant-billing workflows;
- migration of all existing projects;
- a separately deployed billing service;
- automatic synchronization of every application to the newest package version;
- application-specific backends for the new framework-neutral packages (Hangfire/Quartz, SendGrid/Mailgun/SMTP, RabbitMQ/Service Bus, Redis/Postgres idempotency, Redis rate-limit).

## Status summary

- Active changes: none (`openspec list` is empty).
- Archived changes: ten (see `openspec/changes/archive/`).
- Generated specs: ten (see `openspec/specs/`).
- Production packages: eight (`Platform.Core`, `Platform.AspNetCore`, `Platform.Billing.Contracts`, `Platform.Jobs`, `Platform.Mailing`, `Platform.Eventing`, `Platform.Idempotency`, `Platform.RateLimiting`) plus the test-only `Platform.Testing`.
- Tests: 354 passing across nine test projects (`Platform.Architecture.Tests`, `Platform.Core.Tests`, `Platform.AspNetCore.Tests`, `Platform.Billing.Contracts.Tests`, `Platform.Jobs.Tests`, `Platform.Mailing.Tests`, `Platform.Eventing.Tests`, `Platform.Idempotency.Tests`, `Platform.RateLimiting.Tests`, `Platform.Testing.Tests`).
