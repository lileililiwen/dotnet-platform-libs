# Platform quota

`Platform.Quota` standardizes atomic capacity checks and reservation lifecycle without defining
commercial plans. Subjects, resources, units, limits, and windows are application-owned. The
database, billing system, or domain service remains authoritative for how those values are derived.

## Packages and layout

- `Platform.Quota` — `Contracts`, `Stores`, `Evaluation`, and `DependencyInjection`; includes the
  thread-safe `InMemoryQuotaStore`.
- `Platform.Quota.Testing` — deterministic `QuotaScenarioBuilder` and inspection-oriented helpers.

The package does not own plans, prices, invoices, wallets, ledgers, persistence schemas, Redis, or
background cleanup.

## Lifecycle and concurrency

Use a stable operation key for `Reserve`, `Settle`, and `Release`. Repeated lifecycle calls are
idempotent. Accepted reservations consume reserved capacity until settlement, release, or expiry;
the in-memory implementation performs the capacity decision and reservation mutation under one
lock. Persistent adapters must provide equivalent atomicity for the same subject/resource/window.

Applications should reconcile expired or leaked reservations and decide whether settlement uses the
reserved amount or an application-converted amount. Unit conversion, cleanup, and durable storage
remain application responsibilities.

## Entitlements and migration

`IQuotaLimitResolver` is an optional seam for resolving an opaque limit from an application-owned
entitlement. It does not import billing plans or invoice entities. A migration can shadow-check the
platform store beside an existing quota service, then switch reserve calls after decisions match.
Rollback removes the wrapper and leaves existing application ledger data unchanged.
