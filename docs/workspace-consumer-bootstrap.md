# Workspace consumer bootstrap

The workspace uses .NET 10 as its default SDK and target framework for new
MSBuild projects.

The automatic hook is installed at the mixed-language workspace root:

- `/home/paul/code/global.json` pins the .NET SDK to `10.0.400`.
- `/home/paul/code/Directory.Build.props` supplies `net10.0` when a project has
  not selected a target framework.
- `/home/paul/code/Directory.Build.targets` imports the platform consumer
  configuration after each project file has been evaluated.
- `build/Platform.Consumer.props` adds a source `ProjectReference` to
  `Platform.Core` for eligible `net10.0` consumers.

Python, Rust, and other non-MSBuild projects ignore these files. The platform
repository is also excluded, so it does not reference itself.

## Opt out

A .NET project that must remain independent can set this property in its
project file:

```xml
<PropertyGroup>
  <PlatformConsumerOptOut>true</PlatformConsumerOptOut>
</PropertyGroup>
```

This mechanism intentionally uses a source project reference for local
development. Changes to `Platform.Core` are therefore compiled directly into
consumer builds. Release or portable projects should opt out and declare
explicit NuGet package references instead.

Run the bootstrap check from this repository:

```bash
bash scripts/test-workspace-bootstrap.sh
```
