## 1. Preparation

- [ ] 1.1 Confirm the prerequisite platform changes are archived and packages are buildable.
- [ ] 1.2 Capture the pre-adoption `singleatee` test and behavior baseline.
- [ ] 1.3 Choose local project references versus a local package feed for the first pass.

## 2. Adapter implementation

- [ ] 2.1 Add platform contract references without moving product entities.
- [ ] 2.2 Implement local entitlement and usage adapters.
- [ ] 2.3 Add adapter tests for free/pro limits and invalidation.
- [ ] 2.4 Keep existing migrations and persistence ownership in `singleatee`.

## 3. Packaged adoption

- [ ] 3.1 Pack the platform libraries and consume them through a local/private feed.
- [ ] 3.2 Pin package versions in the pilot.
- [ ] 3.3 Compare behavior, dependencies, and changed lines with the baseline.

## 4. Verification and handoff

- [ ] 4.1 Run the targeted `singleatee` tests and relevant integration tests.
- [ ] 4.2 Document findings and rejected abstractions.
- [ ] 4.3 Create follow-up OpenSpec changes for any required API corrections.
- [ ] 4.4 Run strict OpenSpec validation in both repositories.
