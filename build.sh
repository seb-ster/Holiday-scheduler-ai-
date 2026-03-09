#!/bin/bash
set -euo pipefail

# Navigate to the project directory
cd "$(dirname "$0")"

# Detect target runtime (default: macOS ARM64)
TARGET_RID="${TARGET_RID:-osx-arm64}"
CONFIGURATION="${CONFIGURATION:-Release}"
OUTPUT_DIR="releases"

echo "Building Holiday Scheduler AI"
echo "  Target RID    : $TARGET_RID"
echo "  Configuration : $CONFIGURATION"
echo "  Output dir    : $OUTPUT_DIR"

# Create releases directory if it doesn't exist
mkdir -p "$OUTPUT_DIR"

# Restore NuGet packages
echo ""
echo "── Restoring packages ──────────────────────────────────────────────────"
dotnet restore HolidaySchedulerApp.fsproj

# Build and publish as a self-contained single-file executable
echo ""
echo "── Publishing ──────────────────────────────────────────────────────────"
# Trimming disabled: Avalonia + F# reflection require untrimmed assemblies
dotnet publish HolidaySchedulerApp.fsproj \
    --configuration "$CONFIGURATION" \
    --runtime "$TARGET_RID" \
    --self-contained true \
    --output "$OUTPUT_DIR/$TARGET_RID" \
    -p:PublishSingleFile=true \
    -p:PublishTrimmed=false \
    -p:IncludeNativeLibrariesForSelfExtract=true

echo ""
echo "Build complete."
echo "Executable is in: $OUTPUT_DIR/$TARGET_RID/"

# ── Legacy Python build (kept for reference) ─────────────────────────────────
# To build the original Python installer instead, run:
#   pyinstaller --onefile --distpath releases/ app/installer.py