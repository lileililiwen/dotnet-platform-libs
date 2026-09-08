# Handoff

## Current state

Three of the five Phase 3 pilot-adoption OpenSpec changes are implemented and archived (`platform-extraction-jobs`, `platform-extraction-mailing`, `platform-extraction-eventing`). The repository ships seven production packages (`Platform.Core`, `Platform.AspNetCore`, `Platform.Billing.Contracts`, `Platform.Jobs`, `Platform.Mailing`, `Platform.Eventing`) and one test-only package (`Platform.Testing`). The architecture guardrails ensure production projects do not reference the test package, the test package does not embed xUnit, NUnit, or a mocking framework, and `Platform.Jobs` / `Platform.Mailing` / `Platform.Eventing` do not reference ASP.NET Core, EF Core, scheduling engines, mail providers, templating engines, RabbitMQ, or VisualFlow projects.

## Next change

Select the next active change with `openspec list`. The two remaining candidates are:

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

## Completed: platform-extraction-eventing

- Added `Platform.Eventing` (`net8.0`, version `0.1.0`):
  `IIntegrationEvent`, `IntegrationEvent` (abstract record carrying
  `EventId`, `OccurredAt`, `CorrelationId`), `IntegrationEventEnvelope`
  (transport-agnostic wire format with `MessageId`, `PayloadType`,
  `PayloadJson`, `OccurredAt`, `CorrelationId`),
  `IIntegrationEventEnvelopeSerializer` + default
  `IntegrationEventEnvelopeSerializer`,
  `IIntegrationEventEnvelopeDeserializer` + default
  `IntegrationEventEnvelopeDeserializer`,
  `IIntegrationEventHandler<TEvent>` (with `ConsumerName` and
  `HandleAsync`), `IEventBus`, `InProcessEventBus`, `EventingOptions`,
  and
  `Platform.Eventing.DependencyInjection.ServiceCollectionExtensions`
  with `AddPlatformEventing` and `AddPlatformEventingInProcess`.
- `IntegrationEvent` is an `abstract record` with two convenience
  protected constructors that generate a fresh `EventId`; derived
  records pass the clock-anchored `OccurredAt` (and optional
  `CorrelationId`) through to the base.
- `IntegrationEventEnvelopeSerializer` reads the current time from
  the injected `IClock` when the event does not already carry a
  non-default `OccurredAt`; `MessageId` defaults to the
  `EventId.ToString("D")` so the same identifier travels through
  the envelope. JSON options default to camelCase property names.
- `IntegrationEventEnvelopeDeserializer` resolves the payload type
  from `PayloadType` (via `Type.GetType` then a fallback scan of
  loaded assemblies) and reconstructs the typed event; an unknown
  type throws `InvalidOperationException`.
- `InProcessEventBus` is an `IEventBus` + `IAsyncDisposable` that
  uses a bounded `Channel<IntegrationEventEnvelope>` (default
  capacity 1024 from `EventingOptions.InProcessBoundedCapacity`) with
  back-pressure through `ChannelWriter.WaitToWriteAsync`, consumes
  the `IClock` and the registered `IIntegrationEventEnvelopeDeserializer`,
  resolves every `IIntegrationEventHandler<TEvent>` whose
  `TEvent` matches the deserialised payload's runtime type from
  the host's `IServiceProvider`, and logs+swallows consumer
  failures so the bus never crashes. Disposal is idempotent.
- `AddPlatformEventing(IServiceCollection)` and the
  `Action<EventingOptions>` overload register `EventingOptions`,
  `IIntegrationEventEnvelopeSerializer`, and
  `IIntegrationEventEnvelopeDeserializer`. They also call
  `TryAddSingleton<IClock>(_ => new SystemClock())` so the package
  keeps working when no host clock is registered.
- `AddPlatformEventingInProcess(IServiceCollection)` additionally
  registers `InProcessEventBus` and binds it to `IEventBus`.
- Package depends on `Platform.Core`,
  `Microsoft.Extensions.DependencyInjection.Abstractions`,
  `Microsoft.Extensions.Logging.Abstractions`, and
  `Microsoft.Extensions.Options`. `System.Threading.Channels` is
  in-box with `net8.0` and is not a NuGet dependency. No ASP.NET
  Core, EF Core, RabbitMQ, or VisualFlow references.
- Extended `Platform.Architecture.Tests`:
  - `Platform_Eventing_does_not_reference_forbidden_packages` —
    fails on any `Microsoft.AspNetCore`,
    `Microsoft.EntityFrameworkCore`, or `RabbitMQ` reference.
  - `Platform_Eventing_only_references_Platform_Core` — fails on
    any project reference other than `Platform.Core`.
  - `Platform_Eventing_does_not_reference_visual_flow_projects` —
    fails on any project reference whose path contains
    `VisualFlow`.
- `Platform.Eventing.Tests` (30 tests) covers the assembly marker,
  `EventingOptions` defaults, the envelope shape (with and without
  `CorrelationId`), the serializer (`PayloadType`,
  `OccurredAt` from the event, `CorrelationId`, null payload,
  null clock), the deserializer (round-trip preserves payload and
  `CorrelationId` and `OccurredAt`, unknown type throws, null
  envelope, null clock), the `InProcessEventBus` (typed handler
  dispatch, consumer-failure isolation, idempotent disposal, null
  envelope, non-positive bounded capacity, configuration
  registration), and a `TestServer` integration test that proves
  both the documented defaults and a consumer-published envelope
  flow through a registered `IIntegrationEventHandler<OrderPlaced>`
  to a `RecordingOrderPlacedHandler` in a full `WebApplication`
  host.

## Verification evidence

- `dotnet restore Platform.sln` — clean.
- `dotnet build Platform.sln -c Release --no-restore --nologo` — 0
  warnings, 0 errors (the pre-existing xUnit2013 warning in
  `Platform.Testing.Tests` is not in this change).
- `dotnet test Platform.sln -c Release --no-build --nologo` — 275
  tests passed (28 Core, 49 Billing.Contracts, 36 Testing, 22
  AspNetCore, 33 Jobs, 40 Mailing, 30 Eventing, 37 Architecture), 0
  failed, 0 skipped.
- `dotnet pack src/Platform.Eventing/Platform.Eventing.csproj -c
  Release --no-build --nologo` — produced
  `Platform.Eventing.0.1.0.nupkg`; inspected `.nuspec` and
  confirmed `<dependencies>` contains only `Platform.Core`,
  `Microsoft.Extensions.DependencyInjection.Abstractions`,
  `Microsoft.Extensions.Logging.Abstractions`, and
  `Microsoft.Extensions.Options` (no ASP.NET Core, EF Core,
  RabbitMQ, or VisualFlow references).
- Production isolation: existing architecture tests confirm no
  production project gains a forbidden reference, and the new
  `Platform.Eventing` tests confirm its `Platform.Core`-only
  project reference and the absence of ASP.NET Core, EF Core,
  RabbitMQ, or VisualFlow references.
- `git diff --check` — clean.
- `openspec validate --changes --strict --no-interactive` — 2
  passed, 0 failed.
- `openspec validate --specs --strict --no-interactive` — 8
  passed, 0 failed.
- `openspec list` — 2 active changes (the jobs, mailing, and
  eventing changes are archived).

## Completed earlier: platform-extraction-mailing

- `Platform.Mailing` (`net8.0`, version `0.1.0`): `MailAddress`,
  `MailAttachment`, `MailMessage` (with the documented
  TextBody-or-HtmlBody invariant), `MailSendOutcome`,
  `MailSendResult`, `IMailService`, `MailTemplateId` (with implicit
  string conversions), `IMailTemplateRenderer<TModel>`,
  `RenderedMailTemplate`, `MailingOptions`, and
  `AddPlatformMailing`. 40 unit + TestServer tests; three new
  architecture guardrails (forbidden packages, single
  `Platform.Core` reference, no VisualFlow references).

## Completed earlier: platform-extraction-jobs

- `Platform.Jobs` (`net8.0`, version `0.1.0`): `IJobDispatcher`,
  `IRecurringJobHandler`, `IRecurringJobRegistry`, `IJobTelemetry`,
  `JobPayload`, `RecurringJobAttribute`, `RecurringJobDescriptor`,
  `BackgroundJobsOptions`, and `AddPlatformJobs`. 33 unit +
  TestServer tests; three new architecture guardrails.
