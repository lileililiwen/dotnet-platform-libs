# Handoff

## Current state

Two of the five Phase 3 pilot-adoption OpenSpec changes are implemented and archived (`platform-extraction-jobs`, `platform-extraction-mailing`). The repository ships six production packages (`Platform.Core`, `Platform.AspNetCore`, `Platform.Billing.Contracts`, `Platform.Jobs`, `Platform.Mailing`) and one test-only package (`Platform.Testing`). The architecture guardrails ensure production projects do not reference the test package, the test package does not embed xUnit, NUnit, or a mocking framework, and `Platform.Jobs` / `Platform.Mailing` do not reference ASP.NET Core, EF Core, scheduling engines, mail providers, templating engines, or VisualFlow projects.

## Next change

Select the next active change with `openspec list`. The three remaining candidates are:

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

## Completed: platform-extraction-mailing

- Added `Platform.Mailing` (`net8.0`, version `0.1.0`):
  `MailAddress`, `MailAttachment`, `MailMessage`, `MailSendOutcome`,
  `MailSendResult`, `IMailService`, `MailTemplateId`,
  `IMailTemplateRenderer<TModel>`, `RenderedMailTemplate`,
  `MailingOptions`, and
  `Platform.Mailing.DependencyInjection.ServiceCollectionExtensions.AddPlatformMailing`.
- `MailMessage` is a sealed record with a validating primary
  constructor: at least one of `TextBody` or `HtmlBody` must be
  supplied, `Subject` must be non-empty, at least one recipient is
  required, and `From` must be non-null. Attachments and
  `CorrelationId` are optional.
- `MailTemplateId` is a `readonly record struct` with a validating
  constructor and implicit conversions to and from `string` (the
  implicit conversion from `string` re-validates and throws
  `ArgumentException` on null or whitespace).
- `MailingOptions` carries the documented defaults
  (`DefaultFromAddress = "noreply@example.invalid"`,
  `DefaultFromDisplayName = "Platform"`, `MaxAttempts = 3`,
  `InitialBackoffSeconds = 5`, `MaxBackoffSeconds = 60`,
  `TemplatesPath = "templates"`) and the `SectionName` constant
  `"Mailing"`. The package does NOT register default
  implementations of `IMailService` or
  `IMailTemplateRenderer<TModel>` (per spec).
- `AddPlatformMailing(IServiceCollection)` and
  `AddPlatformMailing(IServiceCollection, Action<MailingOptions>)`
  bind `MailingOptions` through the `IOptions<>` pipeline and
  register a `SystemClock` only when no `IClock` is already present.
- Package depends on `Platform.Core` and the two
  `Microsoft.Extensions.*` abstractions; no ASP.NET Core, EF Core,
  SendGrid, Mailgun, SMTP, Razor, Liquid, or VisualFlow references.
- Extended `Platform.Architecture.Tests`:
  - `Platform_Mailing_does_not_reference_forbidden_packages` — fails
    on any `Microsoft.AspNetCore`, `Microsoft.EntityFrameworkCore`,
    `SendGrid`, `Mailgun`, `Smtp`, `Razor`, or `Liquid` reference.
  - `Platform_Mailing_only_references_Platform_Core` — fails on any
    project reference other than `Platform.Core`.
  - `Platform_Mailing_does_not_reference_visual_flow_projects` —
    fails on any project reference whose path contains `VisualFlow`.
- `Platform.Mailing.Tests` (40 tests) covers the assembly marker,
  `MailAddress` and `MailAttachment` validation, `MailMessage`
  construction (text-only, html-only, both, neither, empty
  recipients, null sender, empty subject, attachment defaults,
  correlation id), `MailSendResult` semantics
  (`Sent`/`Bounced` accepted, `TransientFailure`/`PermanentFailure`
  not accepted), `MailTemplateId` implicit conversions and
  validation, `RenderedMailTemplate.Create` validation, the
  `MailingOptions` defaults, the `AddPlatformMailing` registration
  (defaults, configuration overrides, null guards, clock
  preservation, no default `IMailService` or renderer), and a
  `TestServer` integration test that proves both the documented
  defaults and a consumer-registered `IMailService` flow through a
  full `WebApplication` host.

## Verification evidence

- `dotnet restore Platform.sln` — clean.
- `dotnet build Platform.sln -c Release --no-restore --nologo` — 0
  warnings, 0 errors (the pre-existing xUnit2013 warning in
  `Platform.Testing.Tests` is not in this change).
- `dotnet test Platform.sln -c Release --no-build --nologo` — 239
  tests passed (28 Core, 49 Billing.Contracts, 36 Testing, 22
  AspNetCore, 33 Jobs, 40 Mailing, 31 Architecture), 0 failed, 0
  skipped.
- `dotnet pack src/Platform.Mailing/Platform.Mailing.csproj -c
  Release --no-build --nologo` — produced
  `Platform.Mailing.0.1.0.nupkg`; inspected `.nuspec` and confirmed
  `<dependencies>` contains only `Platform.Core`,
  `Microsoft.Extensions.DependencyInjection.Abstractions`, and
  `Microsoft.Extensions.Options`.
- Production isolation: existing architecture tests confirm no
  production project gains a forbidden reference, and the new
  `Platform.Mailing` tests confirm its `Platform.Core`-only
  project reference and the absence of mail-provider, templating,
  ASP.NET Core, EF Core, or VisualFlow references.
- `git diff --check` — clean.
- `openspec validate --changes --strict --no-interactive` — 3
  passed, 0 failed.
- `openspec validate --specs --strict --no-interactive` — 7
  passed, 0 failed.
- `openspec list` — 3 active changes (the jobs and mailing changes
  are archived).

## Completed earlier: platform-extraction-jobs

- `Platform.Jobs` (`net8.0`, version `0.1.0`): `IJobDispatcher`,
  `IRecurringJobHandler`, `IRecurringJobRegistry`, `IJobTelemetry`,
  `JobPayload`, `RecurringJobAttribute`, `RecurringJobDescriptor`,
  `BackgroundJobsOptions`, and
  `Platform.Jobs.DependencyInjection.ServiceCollectionExtensions.AddPlatformJobs`.
  Package depends only on `Platform.Core` and the two
  `Microsoft.Extensions.*` abstractions. `Platform.Jobs.Tests` adds
  33 unit + TestServer tests; `Platform.Architecture.Tests` adds
  three new guardrails (forbidden packages, single `Platform.Core`
  reference, no VisualFlow references).
