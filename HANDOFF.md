# Handoff

## Current state

The repository and OpenSpec structure are initialized. The implementation has not started. Six active changes are defined in `openspec/changes/`.

## Next change

Run `openspec list`, select `platform-repository-foundation`, and implement only that change.

## Required sequence

1. Select one active change with `openspec list`.
2. Implement only that change and its tests.
3. Update its `tasks.md` checkboxes.
4. Verify with relevant tests and strict OpenSpec validation.
5. Archive the completed change.
6. Commit implementation, tests, archive, and related generated specs together.
7. Update this file with completion evidence and the next change.
8. Commit only this handoff update.
9. Stop.

## Verification evidence

- Repository initialization: passed.
- OpenSpec initialization: passed.
- Codex helper installation: blocked by read-only global path `/home/paul/.codex/prompts/opsx-explore.md`; this does not block repository OpenSpec use.
- Change validation: pending after artifact creation.
