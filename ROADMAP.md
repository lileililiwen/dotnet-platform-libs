# Roadmap

## Phase 1: Foundation

1. `platform-repository-foundation`
2. `platform-core-contracts`
3. `platform-aspnetcore-foundation`

## Phase 2: Reusable product contracts

4. `platform-entitlement-contracts`
5. `platform-testing-toolkit`

## Phase 3: Adoption proof

6. `pilot-adoption-singleatee`

The pilot must prove that the shared contracts reduce duplication without forcing the application to replace its existing persistence model.

## Deferred

- a shared EF Core persistence package;
- a shared Stripe implementation;
- shared invoice, wallet, or tenant-billing workflows;
- migration of all existing projects;
- a separately deployed billing service;
- automatic synchronization of every application to the newest package version.
