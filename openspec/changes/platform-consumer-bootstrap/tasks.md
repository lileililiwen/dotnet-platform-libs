# Tasks: One consumer bootstrap for the platform

## 1. BFS — Baseline and impact coverage

- [ ] Inventory `build/Platform.Consumer.props`, the workspace
  `Directory.Build.props` (`PlatformAsSource`, `PlatformPackageVersion`), the
  repository `Directory.Build.props`/`Directory.Packages.props`, the conformance
  fixture, and the release workflow.
- [ ] Confirm the existing specs (`dotnet10-platform-baseline`,
  `platform-consumer-adoption`) so this change extends rather than replaces them,
  and confirm no public package API changes.
- [ ] Add fixture consumers (source mode, package mode, `net8.0`, opt-out)
  before implementation.
- [ ] Record the dependency direction and the smallest adoptable boundary.

## 2. DFS — Requirement-by-requirement implementation

- [ ] Make the source/package switch real in the bootstrap, including the
  missing-checkout error.
- [ ] Propagate consumer defaults (nullability, language version, analyzers,
  warnings policy, central package management) with the opt-out honoured.
- [ ] Add the unsupported-target diagnostic.
- [ ] Establish one version source and extend `release.yml` to push to the
  declared feed.
- [ ] Extend `scripts/conformance.sh` and the fixture to cover both modes.

## 3. BFS — Cross-surface regression and completeness

- [ ] Re-run restore/build/test/pack for the platform solution.
- [ ] Verify an opted-out consumer is untouched and a source-mode consumer uses
  source, not the feed.
- [ ] Verify the architecture tests (production packages never reference
  `Platform.Testing`) and the public-API baseline are unchanged.
- [ ] Update `docs/workspace-consumer-bootstrap.md` and
  `docs/platform-product-adoption.md`.

## 4. Verification

- [ ] `dotnet restore && dotnet build Platform.sln -c Release && dotnet test
  Platform.sln -c Release && dotnet pack Platform.sln -c Release -o artifacts/packages`.
- [ ] `./scripts/conformance.sh` (both modes, plus the `net8.0` and opt-out
  fixtures).
- [ ] `./scripts/quality-gate.sh`, `./scripts/check-public-api.sh`,
  `git diff --check`, and `openspec validate --changes --strict --no-interactive`.
- [ ] Record actual outputs; report any environment-blocked check with its exact
  next action rather than as passing.
