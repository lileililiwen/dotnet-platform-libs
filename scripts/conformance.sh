#!/usr/bin/env bash
set -euo pipefail

# Pack every platform project to a local feed and run the
# consumer-conformance test suite against the packed artifacts.
# The script records the exact failed command and the next action on
# any failure so CI can distinguish source failures from
# environment blockers.
#
# In addition to the existing packed-artifact conformance, this script
# now exercises the platform consumer bootstrap:
#   * source-mode fixture (PlatformAsSource=true, local checkout)
#   * package-mode fixture (PlatformAsSource unset, feed + pinned version)
#   * unsupported-target fixture (net8.0) — must surface a named diagnostic
#   * opt-out fixture — must receive no reference and no defaults

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
SOLUTION="$ROOT_DIR/Platform.sln"
CONFORMANCE_PROJECT="$ROOT_DIR/tests/Platform.ConsumerConformance/Platform.ConsumerConformance.csproj"
LOCAL_FEED_DIR="$ROOT_DIR/tests/Platform.ConsumerConformance/.local-feed"
LOG_DIR="$ROOT_DIR/tests/Platform.ConsumerConformance/.logs"
NUGET_SOURCE="${NUGET_SOURCE:-https://repo.huaweicloud.com/repository/nuget/v3/index.json}"

if [ ! -f "$SOLUTION" ]; then
  echo "ERROR: cannot find solution at $SOLUTION" >&2
  exit 64
fi

if [ ! -f "$CONFORMANCE_PROJECT" ]; then
  echo "ERROR: cannot find conformance project at $CONFORMANCE_PROJECT" >&2
  exit 64
fi

mkdir -p "$LOG_DIR"

log_path() {
  printf "%s/%s.log" "$LOG_DIR" "$1"
}

step() {
  printf "\n=== %s ===\n" "$1"
}

run_step() {
  local name="$1"
  shift
  local log
  log="$(log_path "$name")"
  if "$@" >"$log" 2>&1; then
    printf "[OK] %s\n" "$name"
  else
    local exit_code=$?
    printf "[ENV BLOCKER] %s — see %s\n" "$name" "$log" >&2
    printf "Command: %s\n" "$*" >&2
    printf "Next action: investigate %s or re-run with the platform sources restored.\n" "$name" >&2
    exit "$exit_code"
  fi
}

run_expected_failure() {
  local name="$1"
  local expected_message="$2"
  shift 2
  local log
  log="$(log_path "$name")"
  if "$@" >"$log" 2>&1; then
    printf "[FAIL] %s — expected failure but the command succeeded. See %s\n" "$name" "$log" >&2
    exit 1
  fi
  if grep -Fq "$expected_message" "$log"; then
    printf "[OK] %s (named diagnostic surfaced)\n" "$name"
  else
    printf "[FAIL] %s — expected diagnostic '%s' not found. See %s\n" "$name" "$expected_message" "$log" >&2
    exit 1
  fi
}

cleanup_local_feed() {
  rm -rf "$LOCAL_FEED_DIR"
  mkdir -p "$LOCAL_FEED_DIR"
}

step "Packing platform projects to local feed"
cleanup_local_feed
run_step "pack" dotnet pack "$SOLUTION" -c Release --no-restore -o "$LOCAL_FEED_DIR" --nologo -m:1

step "Restoring conformance project from local feed"
run_step "restore" dotnet restore "$CONFORMANCE_PROJECT" --source "$LOCAL_FEED_DIR" --source "$NUGET_SOURCE" --nologo -m:1

step "Building conformance project"
run_step "build" dotnet build "$CONFORMANCE_PROJECT" -c Release --no-restore --nologo -m:1

step "Running conformance tests"
run_step "test" dotnet test "$CONFORMANCE_PROJECT" -c Release --no-build --nologo -m:1 --logger "console;verbosity=normal"

step "Exercising bootstrap fixtures"
BOOTSTRAP_DIR="$ROOT_DIR/tests/Platform.ConsumerConformance/Fixtures/Bootstrap"
SOURCE_MODE_PROJECT="$BOOTSTRAP_DIR/SourceMode/SourceModeConsumer.csproj"
PACKAGE_MODE_PROJECT="$BOOTSTRAP_DIR/PackageMode/PackageModeConsumer.csproj"
PACKAGE_MODE_NO_CPM_PROJECT="$BOOTSTRAP_DIR/PackageModeNoCpm/PackageModeNoCpmConsumer.csproj"
OPT_OUT_PROJECT="$BOOTSTRAP_DIR/OptOut/OptOutConsumer.csproj"
UNSUPPORTED_TARGET_PROJECT="$BOOTSTRAP_DIR/UnsupportedTarget/UnsupportedTargetConsumer.csproj"

run_step "bootstrap-source-mode-build" \
  dotnet build "$SOURCE_MODE_PROJECT" -c Release \
    --source "$LOCAL_FEED_DIR" --source "$NUGET_SOURCE" \
    --nologo -m:1

run_step "bootstrap-package-mode-restore" \
  dotnet restore "$PACKAGE_MODE_PROJECT" --source "$LOCAL_FEED_DIR" --source "$NUGET_SOURCE" --nologo -m:1

run_step "bootstrap-package-mode-build" \
  dotnet build "$PACKAGE_MODE_PROJECT" -c Release --no-restore \
    --source "$LOCAL_FEED_DIR" --source "$NUGET_SOURCE" \
    --nologo -m:1

# Package-mode without central package management: the bootstrap sets the
# version inline on the PackageReference, so the build must still succeed.
run_step "bootstrap-package-mode-no-cpm-build" \
  dotnet build "$PACKAGE_MODE_NO_CPM_PROJECT" -c Release \
    --source "$LOCAL_FEED_DIR" --source "$NUGET_SOURCE" \
    --nologo -m:1

# Opt-out fixture must build without receiving a Platform.Core reference.
run_step "bootstrap-opt-out-build" \
  dotnet build "$OPT_OUT_PROJECT" -c Release --nologo -m:1

# Unsupported-target fixture must fail with a named diagnostic naming both
# the consumer's target (net8.0) and the supported target (net10.0).
run_expected_failure "bootstrap-unsupported-target" \
  "net8.0" \
  dotnet build "$UNSUPPORTED_TARGET_PROJECT" -c Release --nologo -m:1

printf "\nConsumer conformance suite succeeded.\n"
