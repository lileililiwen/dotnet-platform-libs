#!/usr/bin/env bash
set -euo pipefail

workspace_root="/home/paul/code"
platform_root="${workspace_root}/dotnet-platform-libs"
consumer_dir="${workspace_root}/.platform-consumer-bootstrap-test"
consumer_project="${consumer_dir}/Consumer.csproj"
optout_project="${consumer_dir}/OptOut.csproj"

cleanup() {
  rm -rf "${consumer_dir}"
}
trap cleanup EXIT

test -f "${workspace_root}/global.json"
test -f "${workspace_root}/Directory.Build.props"
test -f "${workspace_root}/Directory.Build.targets"
test -f "${platform_root}/build/Platform.Consumer.props"

mkdir -p "${consumer_dir}"
printf '%s\n' '<Project Sdk="Microsoft.NET.Sdk">' '  <PropertyGroup>' '    <TargetFramework>net10.0</TargetFramework>' '  </PropertyGroup>' '</Project>' > "${consumer_project}"

evaluation="$(dotnet msbuild "${consumer_project}" -getProperty:TargetFramework -getItem:ProjectReference -nologo)"
grep -Fq '"TargetFramework": "net10.0"' <<<"${evaluation}"
grep -Fq 'Platform.Core.csproj' <<<"${evaluation}"
grep -Fq "${platform_root}" <<<"${evaluation}"
dotnet restore "${consumer_project}" --nologo -p:RestoreIgnoreFailedSources=true
dotnet build "${consumer_project}" --nologo --no-restore -p:RestoreIgnoreFailedSources=true

printf '%s\n' '<Project Sdk="Microsoft.NET.Sdk">' '  <PropertyGroup>' '    <TargetFramework>net10.0</TargetFramework>' '    <PlatformConsumerOptOut>true</PlatformConsumerOptOut>' '  </PropertyGroup>' '</Project>' > "${optout_project}"
optout_evaluation="$(dotnet msbuild "${optout_project}" -getItem:ProjectReference -nologo)"
if grep -Fq 'Platform.Core.csproj' <<<"${optout_evaluation}"; then
  echo "workspace platform consumer bootstrap: opt-out failed" >&2
  exit 1
fi

echo "workspace platform consumer bootstrap: passed"
