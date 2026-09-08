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

Test projects override these via `tests/Directory.Build.props` to disable packaging and documentation generation.

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

Test projects set `IsPackable=false` and are not packed.

## Validate OpenSpec

```bash
openspec list
openspec validate --changes --strict --no-interactive
openspec validate --specs   --strict --no-interactive
```

Strict validation must pass before archiving a change.

## Lint working tree

```bash
git diff --check
```

`git diff --check` must report no whitespace errors before completion.
