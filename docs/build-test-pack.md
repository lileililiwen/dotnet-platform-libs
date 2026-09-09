# Build, test, pack, and validate

The repository uses standard .NET CLI commands. All commands run from the repository root unless noted.

## Restore

```bash
dotnet restore Platform.sln
```

Package versions are defined centrally in `Directory.Packages.props`; project files declare `<PackageReference Include="..." />` without `Version` attributes.

## Build

```bash
dotnet build Platform.sln -c Release
```

Build defaults are centralized in `Directory.Build.props`:

- `Nullable=enable`, `ImplicitUsings=enable`, `LangVersion=latest`
- `Deterministic=true`, `TreatWarningsAsErrors=true`
- `GenerateDocumentationFile=true`
- `AnalysisLevel=latest-recommended`

Test projects override these via `tests/Directory.Build.props` to disable packaging and documentation generation. The solution currently contains 36 source projects, including the provider-neutral AI packages and their optional adapters.

## Test

```bash
dotnet test Platform.sln -c Release --nologo
```

Focused test projects live in `tests/`. `Platform.Architecture.Tests` enforces dependency direction (no production project may reference test projects, ASP.NET Core, EF Core, or Stripe; `Platform.Core` and `Platform.Billing.Contracts` must not reference any other project; `Platform.AspNetCore` may reference only `Platform.Core`).

## Pack

```bash
dotnet pack Platform.sln -c Release --no-build --nologo
```

Packages are written to each project's `bin/Release/` directory. Each package embeds the repository `README.md` via `PackageReadmeFile` and shares the repository `VersionPrefix` (`Directory.Build.props`) but may be released independently later by overriding `Version` per project.

Test projects set `IsPackable=false` and are not packed. The solution pack includes the six testing-support projects under `src/` only when their project metadata allows packaging; AI package details are in [`platform-ai.md`](platform-ai.md).

## Validate OpenSpec

```bash
openspec list
openspec validate --changes --strict --no-interactive
openspec validate --specs   --strict --no-interactive
```

Strict validation must pass before archiving a change. With no active changes, `openspec validate --specs --strict --no-interactive` validates the 19 synchronized generated specs.

## Consumer conformance

The repository ships a separate consumer-conformance fixture that consumes platform
packages from a local feed and is intentionally **not** part of `Platform.sln`. Run
`scripts/conformance.sh` to pack every platform project to a local feed, restore the
fixture, build it, and run the tests against the published artifacts:

```bash
./scripts/conformance.sh
```

The script records the exact failed command and the next action in
`tests/Platform.ConsumerConformance/.logs/` when a step fails so CI can distinguish a
source regression from an environment blocker (missing Docker, no local feed, no
network access). See [`docs/platform-consumer-conformance.md`](platform-consumer-conformance.md)
for the full layout and what the suite covers.

## Lint working tree

```bash
git diff --check
```

`git diff --check` must report no whitespace errors before completion.
