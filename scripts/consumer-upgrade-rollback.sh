#!/usr/bin/env bash
set -euo pipefail

if [[ $# -ne 3 ]]; then
  echo "Usage: $0 <previous-feed> <candidate-feed> <candidate-version>" >&2
  exit 64
fi

root_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
previous_feed="$1"
candidate_feed="$2"
candidate_version="$3"
dotnet_cmd="${DOTNET_CMD:-dotnet}"
nuget_source="${NUGET_SOURCE:-https://repo.huaweicloud.com/repository/nuget/v3/index.json}"
temp_dir="$(mktemp -d)"
trap 'rm -rf "$temp_dir"' EXIT

tar -C "$root_dir/tests/Platform.ConsumerConformance" \
  --exclude='./bin' --exclude='./obj' --exclude='./.local-feed' \
  --exclude='./.nuget-packages' --exclude='./.logs' -cf - . \
  | tar -C "$temp_dir" -xf -

project="$temp_dir/Platform.ConsumerConformance.csproj"
set_version() {
  local version="$1"
  sed -i -E "/PackageReference Include=\"Platform\./ s/Version=\"[^\"]+\"/Version=\"$version\"/" "$project"
}

run_fixture() {
  local label="$1"
  local feed="$2"
  echo "=== $label ($3) ==="
  "$dotnet_cmd" restore "$project" --source "$feed" --source "$previous_feed" --source "$nuget_source" --ignore-failed-sources --nologo -m:1
  "$dotnet_cmd" test "$project" -c Release --no-restore --nologo -m:1
}

set_version "$candidate_version"
run_fixture candidate "$candidate_feed" "$candidate_version"

set_version "0.1.0"
run_fixture rollback "$previous_feed" "0.1.0"

echo "Consumer upgrade and rollback smoke test succeeded."
