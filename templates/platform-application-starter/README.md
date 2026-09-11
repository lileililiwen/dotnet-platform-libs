# StarterApp

Generated from the `platform-app` template (`Platform.Application.Template`).
This source is application-owned and detached: it has no project reference
to the template pack and needs no template package at runtime.

## Run

```bash
dotnet restore
dotnet run
```

The host exposes `/` (this application), `/live` (liveness), and `/ready`
(readiness) with safe development defaults and no production provider
credentials. Enable capabilities explicitly in `Program.cs` and register
application-owned stores and providers before deployment.

## Versions

Package references pin exact versions (`Platform.* 0.1.0`, third-party
versions as generated). To adopt newer platform packages, update the
`Version` attributes and rebuild; to abandon the template, keep or delete
this source — uninstalling the template pack never affects it.
