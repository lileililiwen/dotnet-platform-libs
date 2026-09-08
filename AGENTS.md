# Agent Instructions

## Scope

This repository contains reusable .NET libraries, not application features. Keep APIs small, explicit, and independently adoptable. Do not import a whole starter-kit architecture into this repository.

## Boundaries

- `Platform.Core` must remain free of ASP.NET Core and EF Core dependencies.
- `Platform.AspNetCore` may depend on `Platform.Core`, but not on product code.
- `Platform.Billing.Contracts` contains normalized subscription and entitlement contracts only.
- `Platform.Testing` may depend on the public platform contracts and test packages, but production packages must not depend on it.
- Product-specific persistence, migrations, provider IDs, invoice rules, and plan names stay in applications.

## Workflow

Implement one active OpenSpec change at a time. Use `openspec list` to select it, update `tasks.md`, run tests and `openspec validate --changes --strict --no-interactive`, archive it, make the related implementation commit, update `HANDOFF.md`, make the handoff-only commit, and stop.

## Quality gates

- nullable reference types enabled;
- warnings treated as errors;
- public APIs have tests and XML documentation where useful;
- package dependencies are centrally versioned;
- no public API change without an OpenSpec change;
- `git diff --check` and strict OpenSpec validation must pass before completion.
