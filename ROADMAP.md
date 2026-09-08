# Roadmap

The roadmap is organized into three phases. Items marked **Done** are implemented, archived, and have generated capability specs in `openspec/specs/`.

## Phase 1: Foundation — Done

1. `platform-repository-foundation` — solution, four production projects, central MSBuild and package metadata, dependency-direction guardrails.
2. `platform-core-contracts` — `IClock`, `Error`, `Result`, `Result<T>`, `CallerContext`, `IAuditable` in `Platform.Core`.
3. `platform-aspnetcore-foundation` — `AddPlatformAspNetCore`, `UsePlatformAspNetCore`, `MapPlatformEndpoints`, sanitized `ProblemDetails`, correlation middleware/accessor, and health-check helpers in `Platform.AspNetCore`.

## Phase 2: Reusable product contracts — Done

4. `platform-entitlement-contracts` — opaque identifiers, normalized subscription and entitlement snapshots, structured feature-check decisions, replaceable usage-meter interface, and idempotent processed-event store in `Platform.Billing.Contracts`.
5. `platform-testing-toolkit` — `ControllableClock`, `SubscriptionBuilder`, `EntitlementBuilder`, `FakeEntitlementStore`, and `RecordingUsageMeter` in `Platform.Testing`.

## Phase 3: Adoption proof — Pending

6. A pilot adoption proposal is not currently in `openspec/changes/`. The pilot must prove that the shared contracts reduce duplication without forcing the application to replace its existing persistence model. To pursue this work, create a fresh OpenSpec proposal in `openspec/changes/`.

## Deferred

- a shared EF Core persistence package;
- a shared Stripe implementation;
- shared invoice, wallet, or tenant-billing workflows;
- migration of all existing projects;
- a separately deployed billing service;
- automatic synchronization of every application to the newest package version.

## Status summary

- Active changes: none (`openspec list` is empty).
- Archived changes: five (see `openspec/changes/archive/`).
- Generated specs: five (see `openspec/specs/`).
- Production packages: four (`Platform.Core`, `Platform.AspNetCore`, `Platform.Billing.Contracts`, plus the test-only `Platform.Testing`).
- Tests: 154 passing across five test projects (`Platform.Architecture.Tests`, `Platform.Core.Tests`, `Platform.AspNetCore.Tests`, `Platform.Billing.Contracts.Tests`, `Platform.Testing.Tests`).
