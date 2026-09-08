# Dotnet Platform Libs

Small, privately maintained .NET libraries for the shared technical concerns repeated across the local application portfolio.

## Purpose

This repository is a platform library, not a replacement for every application's domain model and not a fork of the FullStackHero starter kit. It provides stable contracts and opt-in infrastructure that individual applications can adopt at their own pace.

## Initial scope

- dependency-light core contracts;
- ASP.NET Core integration helpers;
- subscription and entitlement contracts;
- reusable testing helpers;
- a project template and package/versioning conventions after the first pilot.

Product-specific EF Core entities, migrations, Stripe price IDs, invoice rules, plan names, and business workflows remain in consuming applications.

## Delivery workflow

1. Run `openspec list` and select the first active change.
2. Implement exactly one change and its tests.
3. Update that change's `tasks.md`.
4. Run the relevant tests and `openspec validate --changes --strict --no-interactive`.
5. Archive the completed change.
6. Make one related implementation/archive commit.
7. Update `HANDOFF.md` with evidence and the next change.
8. Make a second handoff-only commit.
9. Stop.

Incomplete or blocked work must not be reported as complete. The handoff must record the exact failed command and next action.

## Current status

The repository is specification-first. The first implementation target is `platform-repository-foundation`, followed by core contracts and a two-project pilot.
