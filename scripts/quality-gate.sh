#!/usr/bin/env bash
set -euo pipefail

root_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
dotnet_cmd="${DOTNET_CMD:-dotnet}"
configuration="${CONFIGURATION:-Release}"
nuget_source="${NUGET_SOURCE:-https://repo.huaweicloud.com/repository/nuget/v3/index.json}"

cd "$root_dir"
"$dotnet_cmd" restore Platform.sln --source "$nuget_source" --ignore-failed-sources -p:NuGetAudit=false --nologo -m:1
"$dotnet_cmd" build Platform.sln -c "$configuration" --no-restore --nologo -m:1
"$dotnet_cmd" test Platform.sln -c "$configuration" --no-build --nologo -m:1
openspec validate --changes --strict --no-interactive
openspec validate --specs --strict --no-interactive
git diff --check
