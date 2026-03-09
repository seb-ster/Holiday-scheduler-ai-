#!/usr/bin/env bash
set -euo pipefail

# Publish a self-contained single-file macOS (arm64) app with no external dependencies.
# Requires: .NET 8 SDK installed on the build machine.

PROJECT_DIR="$(cd "$(dirname "$0")" && pwd)"
OUTPUT_DIR="$PROJECT_DIR/../../dist/macos-arm64"

mkdir -p "$OUTPUT_DIR"

dotnet publish "$PROJECT_DIR/HolidayScheduler.Gui.fsproj" \
  -c Release \
  -r osx-arm64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -o "$OUTPUT_DIR"

echo "Published to $OUTPUT_DIR"
