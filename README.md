# Holiday Scheduler AI

A desktop application that uses a **multi-agent evolutionary algorithm** to optimally schedule employee holiday requests. Built with **F# + Avalonia** for a cross-platform native UI experience.

![Platform](https://img.shields.io/badge/platform-macOS%20%7C%20Linux%20%7C%20Windows-lightgrey)
![Framework](https://img.shields.io/badge/.NET-8.0-blue)
![Language](https://img.shields.io/badge/language-F%23-purple)
![UI](https://img.shields.io/badge/UI-Avalonia%2011-teal)

---

## How It Works

Holiday Scheduler AI applies **evolutionary computing** to the problem of fairly placing employee holiday requests:

1. **Queue** — Holiday requests (employee, start/end date, priority) are added to a queue.
2. **Agents compete** — Each generation, a population of agents independently processes the full queue. Every agent is parameterised by a set of *genes* (strategy weights) that determine how it scores and places requests.
3. **Scoring** — Each agent uses a weighted combination of four placement strategies:
   - **Greedy** — maximise total days scheduled
   - **Load-balancing** — prefer less-congested date ranges
   - **Priority-aware** — favour high-priority requests
   - **Temporal clustering** — group holidays near already-scheduled ones
4. **Selection** — After all agents process the queue, the top-performing agents (by average reward) are selected as *elites*.
5. **Evolution** — Elite agents cross over their genes and mutate slightly to produce the next generation. This iterates toward strategies that maximise cumulative scheduling reward.

### Strategy Genes

Each agent carries a `Genes` record:

```fsharp
type Genes =
    { GreedyWeight      : float   // weight for greedy strategy
      LoadBalanceWeight : float   // weight for load-balancing strategy
      PriorityWeight    : float   // weight for priority-aware strategy
      ClusterWeight     : float   // weight for temporal clustering
      MutationRate      : float } // strength of per-generation mutation
```

---

## Features

| Feature | Description |
|---------|-------------|
| **Add Sample Request** | Add a randomly generated holiday request to the queue |
| **Add 10 Requests** | Bulk-add 10 random requests to quickly populate the queue |
| **Run Evolution** | Run one generation of evolution (non-blocking, background thread) |
| **Clear Queue** | Remove all pending requests without resetting the schedule |
| **New Schedule** | Reset everything — schedule, queue, population, and history |
| **Export Schedule…** | Save the current schedule to a CSV file on the Desktop |
| **Evolution History** | Sidebar shows the last 5 generations' best reward scores |

---

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8)

### Run from source

```bash
git clone https://github.com/seb-ster/Holiday-scheduler-ai-.git
cd Holiday-scheduler-ai-
dotnet run --project HolidaySchedulerApp.fsproj
```

### Build a self-contained binary

```bash
# macOS Apple Silicon
TARGET_RID=osx-arm64 ./build.sh

# macOS Intel
TARGET_RID=osx-x64 ./build.sh

# Linux x64
TARGET_RID=linux-x64 ./build.sh

# Windows x64
TARGET_RID=win-x64 ./build.sh
```

Executables are placed in `releases/<TARGET_RID>/`.

---

## Usage Walkthrough

1. **Launch** the app — the sidebar shows Generation 1 with 6 agents.
2. Click **"++ Add 10 Requests"** to populate the queue with 10 random holiday requests.
3. Click **"▶ Run Evolution"** — the agents compete in the background; the best-performing state is committed to the roster.
4. Repeat steps 2–3 a few times. Watch the *Best Reward* and *Evolution History* values in the sidebar climb as the population adapts.
5. Use **File › Export Schedule…** to save the current roster to a CSV file.

---

## Project Structure

```
Holiday-scheduler-ai-/
├── Strategies.fs          # Domain types, strategies, agent genetics, evolution
├── Logger.fs              # Rotating file logger with optional GitHub issue reporter
├── MainWindow.axaml       # Avalonia XAML UI layout
├── MainWindow.axaml.fs    # UI code-behind: event handlers, state management
├── App.axaml / App.axaml.fs
├── Program.fs             # Entry point
├── HolidaySchedulerApp.fsproj
├── build.sh               # Cross-platform build script
└── app/
    ├── core/models.py     # Python domain models (prototype/reference)
    └── installer.py       # Installation helper
```

### Key Types (`Strategies.fs`)

```fsharp
type HolidayRequest  = { EmployeeId; StartDate; EndDate; Priority; SubmittedAt }
type ScheduledHoliday = { Request; Reward; PlacedAt }
type SchedulerState  = { Queue; Schedule; Blocked; Capacity }
type Agent           = { Id; Genes; Metrics; History }
type Population      = Agent list
```

---

## Logging

The app writes structured logs to a rotating log file (`holiday-scheduler.log`). The log directory is chosen automatically (alongside the executable, then `~/logs`, then `%TEMP%/HolidayScheduler/logs`).

Optionally, critical errors can be automatically filed as GitHub issues by setting environment variables:

```bash
export GITHUB_TOKEN="ghp_..."
export GITHUB_REPO="seb-ster/Holiday-scheduler-ai-"
```

---

## Design Notes

This project was created to demonstrate a **technology-demonstrator for multi-agent scheduling**. The design follows [Issue #1](https://github.com/seb-ster/Holiday-scheduler-ai-/issues/1):

- Agents take requests from a shared queue and process them one at a time
- After draining the queue each agent "ends"; its performance is measured
- The best agent's genes seed the next generation (with crossover + mutation)
- Over generations the population converges on high-reward placement strategies

The approach is intentionally simple — pure heuristics weighted by evolved floats — making it fast, interpretable, and easy to extend with more sophisticated scoring functions or ML-based weight learning.
