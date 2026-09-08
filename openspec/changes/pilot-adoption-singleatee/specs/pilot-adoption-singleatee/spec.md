## ADDED Requirements

### Requirement: Pilot adoption SHALL preserve product persistence

The pilot SHALL adapt existing `singleatee` persistence and migrations rather than moving them into the platform repository.

#### Scenario: Existing subscription migration

- **WHEN** the pilot uses the platform entitlement contract
- **THEN** the existing subscription table and migrations remain owned by `singleatee`

### Requirement: Pilot adoption SHALL preserve feature-limit behavior

The pilot SHALL prove that existing free/pro limits and usage behavior remain unchanged after introducing platform contracts.

#### Scenario: Free user checks a limit

- **WHEN** a free user invokes an existing limited feature
- **THEN** the result and reason match the pre-adoption behavior

### Requirement: Pilot SHALL support independent package pinning

The consuming application SHALL be able to reference a specific platform package version without requiring changes to unrelated applications.

#### Scenario: Pin a platform package

- **WHEN** `singleatee` references a released platform package
- **THEN** it restores and tests against that version independently

### Requirement: Pilot findings SHALL be recorded

The pilot SHALL document successful adapters, rejected abstractions, dependency issues, and follow-up OpenSpec changes.

#### Scenario: Contract mismatch

- **WHEN** a platform contract does not represent a product behavior safely
- **THEN** the adapter remains local and the mismatch is recorded rather than forcing a platform API change during the pilot
