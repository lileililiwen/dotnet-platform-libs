# Handoff

## Current state

Foundation work is complete. The repository builds, tests, and packs under .NET 8; package versions and packaging metadata are centralized; the dependency-direction guardrail suite enforces the platform boundaries. Five active changes remain in `openspec/changes/`.

## Next change

Run `openspec list`, select `platform-core-contracts`, and implement only that change.

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

## Completed: platform-repository-foundation

- Created `Platform.sln` plus four packable libraries under `src/`:
  `Platform.Core`, `Platform.AspNetCore`, `Platform.Billing.Contracts`,
  `Platform.Testing`. Each ships with a matching focused test project under
  `tests/` plus a separate `Platform.Architecture.Tests` project for
  dependency-direction rules.
- Centralized MSBuild defaults and packaging metadata in
  `Directory.Build.props`; centralized package versions in
  `Directory.Packages.props`; added `tests/Directory.Build.props` to opt test
  projects out of packaging and documentation generation.
- Documented restore, build, test, pack, and strict OpenSpec validation
  commands in `docs/build-test-pack.md` and refreshed `README.md` with the
  new repository layout and conventions.
- Archived the change as `2026-09-08-platform-repository-foundation` and
  generated `openspec/specs/platform-repository-foundation/spec.md`.

## Verification evidence

- `dotnet restore Platform.sln` — restored all 9 projects.
- `dotnet build Platform.sln -c Release` — 0 warnings, 0 errors.
- `dotnet test Platform.sln -c Release --nologo` — 20 tests passed
  (1 per library + 16 architecture-direction cases), 0 failed, 0 skipped.
- `dotnet pack Platform.sln -c Release --no-build --nologo` — produced
  `Platform.Core.0.1.0.nupkg`, `Platform.Billing.Contracts.0.1.0.nupkg`,
  `Platform.AspNetCore.0.1.0.nupkg`, `Platform.Testing.0.1.0.nupkg`, each
  embedding `README.md` and shared package metadata.
- `git diff --check` — clean.
- `openspec validate --changes --strict --no-interactive` — 5 passed, 0 failed.
- `openspec validate --specs   --strict --no-interactive` — 1 passed, 0 failed.
- `openspec list` — `platform-repository-foundation` no longer present;
  5 active changes remain.
