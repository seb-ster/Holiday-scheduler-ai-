# Publish instructions — cross-platform

This project can be published as self-contained artifacts per OS, with no external .NET dependency on the target machine.

Prerequisites
- .NET 8 SDK installed on the build machine.

Build all targets (macOS app bundle + zip, Windows, Linux):

```bash
cd gui/avalonia
./release_all.sh
```

Build only selected targets:

```bash
./release_all.sh mac
./release_all.sh win
./release_all.sh linux
./release_all.sh mac,win
```

Outputs are written under `dist/`:
- `dist/holiday demonstrator.app` and `dist/holiday-demonstrator-macos-arm64.zip`
- `dist/win-x64/HolidayScheduler.Gui.exe`
- `dist/linux-x64/HolidayScheduler.Gui`

You can override app metadata with environment variables:

```bash
APP_NAME="holiday demonstrator" APP_ID="com.example.holidaydemonstrator" ./release_all.sh
```

Note: code signing and notarization are still required for trusted macOS distribution.
