# Holiday Scheduler AI

An AI-powered holiday / shift-scheduling system that uses an **evolutionary
multi-agent strategy** to optimally place employee holiday requests.

---

## How it works

The scheduler implements the multi-agent design proposed in
[issue #1](../../issues/1):

1. **Request queue** – employee holiday requests are queued up.
2. **Agent pair** – two agents with slightly different strategy genes start
   each generation.
3. **Processing loop** – each agent independently processes the queue one
   request at a time, choosing the best available slot according to its
   weighted scoring function.
4. **Reward** – every placement earns a reward based on three factors:
   - *Fairness*: giving holidays to employees who have had fewer days off.
   - *Preference*: matching the employee's preferred start dates.
   - *Coverage*: avoiding days where too many employees are already off.
5. **Selection** – the agent with the higher total reward wins the generation.
6. **Inheritance & mutation** – the winner's genes (strategy weights) are
   slightly mutated and passed to the two agents of the next generation.
7. **Evolution** – over multiple generations the agents converge on better
   scheduling strategies.

---

## Project structure

```
app/
  core/
    models.py      – Employee, Roster, Shift, Holiday data classes
    agent.py       – Genes and Agent classes
    scheduler.py   – HolidayRequest, RosterState, MultiAgentScheduler
    tests/
      test_agent.py      – unit tests for agent logic
      test_scheduler.py  – unit tests for scheduler logic
  installer.py     – installation splash screen
  main.py          – runnable demonstration
build.sh           – macOS PyInstaller build script
```

---

## Quick start

```bash
# Run the demo (no external dependencies needed)
python -m app.main

# Run the tests
python -m pytest app/core/tests/ -v
```

---

## Agent genes

| Gene | Description | Default |
|---|---|---|
| `fairness_weight` | Reward weight for fair holiday distribution | 1.0 |
| `preference_weight` | Reward weight for honouring preferred dates | 1.0 |
| `coverage_weight` | Penalty weight for coverage gaps | 1.0 |
| `exploration_rate` | Probability of random slot selection | 0.3 |

---

## License

GNU GPLv3 – see [LICENSE](LICENSE).