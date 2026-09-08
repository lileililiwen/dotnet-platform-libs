## Why

The platform contracts need a real adoption test before more applications are migrated. `singleatee` has subscription and feature-limit behavior that is small enough to expose boundary problems without requiring a complete rewrite.

## What Changes

- Add the platform packages to `singleatee` incrementally.
- Adapt its entitlement and usage-limit interfaces through local adapters.
- Preserve its existing EF Core entities and migrations.
- Record migration findings and follow-up changes.

## Capabilities

### New Capabilities

- `pilot-adoption-singleatee`: A documented pilot proving package adoption without replacing product persistence.

### Modified Capabilities

## Impact

Changes the pilot application and this repository's adoption documentation. It must not become a reason to migrate unrelated projects in the same change.

## Context

`singleatee` currently contains `Subscription`, `ISubscriptionLimits`, entitlement, and usage-tracking code. Its product-specific plan and meal-planning rules must remain local.

## Goals / Non-Goals

**Goals:**

- Validate package references and adapter seams.
- Replace only duplicated technical contracts where semantics match.
- Capture compile/test results and migration friction.

**Non-Goals:**

- Rewrite `singleatee` architecture.
- Move its database model into the platform.
- Migrate any other application.

## Decisions

- Start with project references or a local package feed, then test the packaged form.
- Keep a compatibility adapter at the application boundary.
- Revert adoption of any contract that increases coupling or obscures product behavior.

## Risks / Trade-offs

- The pilot may reveal that a contract is too generic or too opinionated; follow-up changes are expected.
- A successful pilot does not prove all applications should adopt the same package.
