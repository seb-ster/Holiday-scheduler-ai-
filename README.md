# Holiday Scheduler AI – Demonstrator App

A desktop demonstrator built with **F# + Avalonia** that shows a **multi-agent evolutionary scheduler** placing employee holiday requests for maximum reward.

## What it demonstrates

- **Multi-agent evolution**: a population of agents each score holiday requests using a weighted combination of strategies (greedy, load-balancing, priority-aware, temporal-clustering, and a learning heuristic).
- **Genetic inheritance**: after each generation the top-performing agents are selected as parents; child agents are produced via crossover and mutation of their strategy weights (genes).
- **Real-time UI**: a live roster grid, generation counter, best-reward tracker, and queue status update after every action.

## Quick start

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download)

### Run from source
```bash
dotnet run --project HolidaySchedulerApp.fsproj
```

### One-click demo
Click **Run Demo** in the sidebar to:
1. Reset the schedule to a clean state.
2. Load 15 pre-seeded holiday requests across five employees with varying priorities.
3. Automatically run five evolution cycles so you can see agents compete and improve.

### Manual exploration
- **Add Sample Request** – append a random request to the queue.
- **Run Evolution** – advance the population by one generation.
- **File → New Schedule** – reset everything and start fresh.

## Project structure

| File | Purpose |
|------|---------|
| `Strategies.fs` | Domain types, strategy functions, agent genes, evolution engine |
| `Logger.fs` | Structured file logger with rotation and optional GitHub crash-issue creation |
| `MainWindow.axaml` | Avalonia XAML layout (sidebar, roster grid, status bar) |
| `MainWindow.axaml.fs` | UI logic – wires controls to the scheduler engine |
| `App.axaml(.fs)` | Avalonia application entry point |
| `Program.fs` | .NET entry point; initialises logger and starts the Avalonia app |

## Agent genes

Each agent carries four weight genes that control how it scores a request:

| Gene | Strategy |
|------|----------|
| `GreedyWeight` | Prefer longer holidays (more days = higher reward) |
| `LoadBalanceWeight` | Prefer dates with fewer existing bookings |
| `PriorityWeight` | Prefer high-priority requests (lower priority number) |
| `ClusterWeight` | Prefer dates near already-scheduled holidays |

Genes mutate slightly each generation and are combined via crossover, so the population converges toward high-reward strategies over time.

## Building a release binary

```bash
TARGET_RID=linux-x64 bash build.sh
```

The self-contained executable is written to `releases/<TARGET_RID>/`.