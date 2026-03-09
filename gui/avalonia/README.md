# Avalonia GUI

This folder contains a minimal Avalonia (C#) GUI for the Holiday Scheduler.

Prerequisites:
- .NET 8 SDK (or later)
- `dotnet` on PATH

Build and run:

```bash
cd gui/avalonia
dotnet restore
dotnet build -c Release
dotnet run --project HolidayScheduler.Gui.csproj
```

Publish single-file (macOS):

```bash
dotnet publish -c Release -r osx-arm64 -p:PublishSingleFile=true --self-contained=false
```

The GUI is intentionally minimal: a main window with a calendar control. If you want I can wire it to the Python backend or expand the UI components.
