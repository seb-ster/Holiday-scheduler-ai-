# Avalonia GUI

This folder contains an Avalonia (F#) GUI demonstrator for the Holiday Scheduler.

Prerequisites:
- .NET 8 SDK (or later)
- `dotnet` on PATH

Build and run:

```bash
cd gui/avalonia
dotnet restore
dotnet build HolidayScheduler.Gui.fsproj -c Release
dotnet run --project HolidayScheduler.Gui.fsproj
```

Publish single-file (macOS):

```bash
./release_all.sh mac
```

Publish all distribution targets:

```bash
./release_all.sh
```

The GUI is intentionally minimal: a main window with a calendar control. If you want I can wire it to the Python backend or expand the UI components.
