# Publish instructions — macOS self-contained

This project can be published as a self-contained macOS application with no external .NET dependency.

Prerequisites
- .NET 8 SDK installed on the build machine.

Publish (ARM64 macOS):

```bash
cd gui/avalonia
./publish_macos.sh
```

The published artifact will be placed in `dist/macos-arm64/`. You can wrap the produced binary into a `.app` bundle and create a `.dmg` for distribution. Code signing and notarization are required for App Store / Gatekeeper trust.
