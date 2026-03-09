# Holiday Scheduler AI — Technology Demonstrator

> **Yes — this is the repo!** Built as a technology demonstrator showcasing a
> multi-agent, evolutionary AI approach to holiday (leave) scheduling using
> **F# + Avalonia**.

## What is this?

Holiday Scheduler AI is a desktop application that demonstrates how a population
of self-evolving AI agents can automatically schedule employee holiday requests to
maximise an overall reward score.  Each agent carries a genetic "strategy" that
blends several heuristics.  After every scheduling round the population evolves:
elite agents are selected, their genes are crossed over and mutated, and the
next generation is spawned — exactly like a genetic algorithm.

## Key Features

| Feature | Details |
|---|---|
| **Multi-agent population** | 6 agents run in parallel per generation, each with independently evolving strategy weights |
| **Genetic evolution** | Crossover + mutation of strategy genes across unlimited generations |
| **Four built-in strategies** | Greedy · Load-balancing · Priority-aware · Temporal clustering |
| **Learning layer** | Per-employee acceptance-rate history feeds back into each agent's scoring |
| **Rotating file logger** | Structured log with automatic size-based rotation (up to 5 rotated files, 10 MB each) |
| **GitHub crash reporter** | On unhandled exceptions the logger opens a GitHub issue automatically (requires `GITHUB_TOKEN` + `GITHUB_REPO` env vars) |
| **Fluent UI desktop app** | Cross-platform Avalonia window with sidebar stats, DataGrid roster view, and menu bar |

## Architecture

```
Program.fs          ← entry point, logger init, Avalonia bootstrap
Strategies.fs       ← domain types, strategy functions, agents, population evolution
MainWindow.axaml.fs ← UI controller — wires buttons/menus, drives evolution loop
Logger.fs           ← thread-safe file logger with rotation + GitHub issue sink
App.axaml / MainWindow.axaml  ← Avalonia XAML layouts
```

## Tech Stack

- **Language:** F# 8 (functional-first, immutable domain model)
- **UI framework:** [Avalonia](https://avaloniaui.net/) 11 with Fluent theme
- **Target framework:** .NET 8
- **Build:** `dotnet publish` via `build.sh` (self-contained single-file binary)

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)

### Run in development

```bash
dotnet run --project HolidaySchedulerApp.fsproj
```

### Build a self-contained release binary

```bash
# macOS ARM64 (default)
./build.sh

# Linux x64
TARGET_RID=linux-x64 ./build.sh

# Windows x64
TARGET_RID=win-x64 ./build.sh
```

The binary is written to `releases/<TARGET_RID>/`.

### Optional environment variables

| Variable | Purpose |
|---|---|
| `GITHUB_TOKEN` | Personal access token — enables automatic crash-report issues |
| `GITHUB_REPO` | `owner/repo` string used by the crash reporter |

## How the AI Works

1. **Queue requests** — click *Add Sample Request* to enqueue randomly generated holiday requests.
2. **Run Evolution** — each agent in the current population scores every request using its weighted strategy blend and schedules it.  The agent with the highest total reward "wins" that generation.
3. **Evolve** — the top-3 agents become elite parents; their genes are crossed over and mutated to produce the next generation of 6 agents.
4. **Repeat** — over many generations the population converges on the weighting that best balances load, priority, temporal clustering, and historical acceptance rates.

## License

See [LICENSE](LICENSE).