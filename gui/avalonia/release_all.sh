#!/usr/bin/env bash
set -euo pipefail

# Builds release artifacts for macOS, Windows, and Linux from a macOS host.
# Outputs are written to dist/ at the repository root.

PROJECT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_FILE="$PROJECT_DIR/HolidayScheduler.Gui.fsproj"
DIST_ROOT="$PROJECT_DIR/../../dist"

APP_NAME="${APP_NAME:-Holiday Scheduler Demonstrator}"
APP_ID="${APP_ID:-com.sebastiaan.holidayschedulerdemonstrator}"
TARGETS="${1:-all}"

sanitize_name() {
  echo "$1" | tr '[:upper:]' '[:lower:]' | tr ' ' '-'
}

SLUG_NAME="$(sanitize_name "$APP_NAME")"

has_target() {
  local value="$1"
  [[ "$TARGETS" == "all" ]] || [[ ",${TARGETS}," == *",${value},"* ]]
}

publish_target() {
  local rid="$1"
  local out_dir="$2"

  mkdir -p "$out_dir"

  dotnet publish "$PROJECT_FILE" \
    -c Release \
    -r "$rid" \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -p:EnableCompressionInSingleFile=true \
    -p:DebugType=None \
    -p:DebugSymbols=false \
    -o "$out_dir"
}

build_macos() {
  local rid="osx-arm64"
  local out_dir="$DIST_ROOT/$rid"
  local app_dir="$DIST_ROOT/$APP_NAME.app"
  local zip_file="$DIST_ROOT/${SLUG_NAME}-macos-arm64.zip"
  local icon_svg="$PROJECT_DIR/roster-icon.svg"
  local iconset_dir="$DIST_ROOT/AppIcon.iconset"
  local icon_icns="$app_dir/Contents/Resources/AppIcon.icns"
  local binary_src="$out_dir/HolidayScheduler.Gui"
  local binary_dst="$app_dir/Contents/MacOS/$APP_NAME"

  echo "Publishing $rid..."
  publish_target "$rid" "$out_dir"

  rm -rf "$app_dir"
  mkdir -p "$app_dir/Contents/MacOS" "$app_dir/Contents/Resources"
  cp "$binary_src" "$binary_dst"
  chmod +x "$binary_dst"

  cat > "$app_dir/Contents/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleName</key>
  <string>$APP_NAME</string>
  <key>CFBundleDisplayName</key>
  <string>$APP_NAME</string>
  <key>CFBundleExecutable</key>
  <string>$APP_NAME</string>
  <key>CFBundleIdentifier</key>
  <string>$APP_ID</string>
  <key>CFBundleVersion</key>
  <string>1.0</string>
  <key>CFBundleShortVersionString</key>
  <string>1.0.0</string>
  <key>CFBundlePackageType</key>
  <string>APPL</string>
  <key>CFBundleIconFile</key>
  <string>AppIcon</string>
  <key>LSMinimumSystemVersion</key>
  <string>12.0</string>
  <key>NSHighResolutionCapable</key>
  <true/>
</dict>
</plist>
PLIST

  # Build a native .icns file so Finder/Spotlight show a proper macOS app icon.
  if command -v qlmanage >/dev/null 2>&1 && command -v sips >/dev/null 2>&1 && command -v iconutil >/dev/null 2>&1 && [[ -f "$icon_svg" ]]; then
    local tmp_png="$DIST_ROOT/app-icon-1024.png"
    rm -rf "$iconset_dir"
    mkdir -p "$iconset_dir"

    qlmanage -t -s 1024 -o "$DIST_ROOT" "$icon_svg" >/dev/null 2>&1 || true

    if [[ -f "$DIST_ROOT/roster-icon.svg.png" ]]; then
      mv "$DIST_ROOT/roster-icon.svg.png" "$tmp_png"
    elif [[ -f "$DIST_ROOT/roster-icon.png" ]]; then
      mv "$DIST_ROOT/roster-icon.png" "$tmp_png"
    fi

    if [[ -f "$tmp_png" ]]; then
      sips -z 16 16     "$tmp_png" --out "$iconset_dir/icon_16x16.png" >/dev/null 2>&1
      sips -z 32 32     "$tmp_png" --out "$iconset_dir/icon_16x16@2x.png" >/dev/null 2>&1
      sips -z 32 32     "$tmp_png" --out "$iconset_dir/icon_32x32.png" >/dev/null 2>&1
      sips -z 64 64     "$tmp_png" --out "$iconset_dir/icon_32x32@2x.png" >/dev/null 2>&1
      sips -z 128 128   "$tmp_png" --out "$iconset_dir/icon_128x128.png" >/dev/null 2>&1
      sips -z 256 256   "$tmp_png" --out "$iconset_dir/icon_128x128@2x.png" >/dev/null 2>&1
      sips -z 256 256   "$tmp_png" --out "$iconset_dir/icon_256x256.png" >/dev/null 2>&1
      sips -z 512 512   "$tmp_png" --out "$iconset_dir/icon_256x256@2x.png" >/dev/null 2>&1
      sips -z 512 512   "$tmp_png" --out "$iconset_dir/icon_512x512.png" >/dev/null 2>&1
      cp "$tmp_png" "$iconset_dir/icon_512x512@2x.png"

      iconutil -c icns "$iconset_dir" -o "$icon_icns" >/dev/null 2>&1 || true
    fi
  fi

  plutil -lint "$app_dir/Contents/Info.plist" >/dev/null
  rm -f "$zip_file"
  ditto -c -k --sequesterRsrc --keepParent "$app_dir" "$zip_file"

  echo "Created: $app_dir"
  echo "Created: $zip_file"
}

build_windows() {
  local rid="win-x64"
  local out_dir="$DIST_ROOT/$rid"

  echo "Publishing $rid..."
  publish_target "$rid" "$out_dir"
  echo "Created: $out_dir/HolidayScheduler.Gui.exe"
}

build_linux() {
  local rid="linux-x64"
  local out_dir="$DIST_ROOT/$rid"

  echo "Publishing $rid..."
  publish_target "$rid" "$out_dir"
  echo "Created: $out_dir/HolidayScheduler.Gui"
}

mkdir -p "$DIST_ROOT"

if has_target "mac"; then
  build_macos
fi

if has_target "win"; then
  build_windows
fi

if has_target "linux"; then
  build_linux
fi

echo "Release outputs are available in: $DIST_ROOT"
echo "Note: macOS code signing/notarization is not included in this script."