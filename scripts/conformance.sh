#!/usr/bin/env bash
set -euo pipefail

# Pack every platform project to a local feed and run the
# consumer-conformance test suite against the packed artifacts.
# The script records the exact failed command and the next action on
# any failure so CI can distinguish source failures from
# environment blockers.

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

printf "\nConsumer conformance suite succeeded.\n"
