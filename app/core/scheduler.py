"""
Scheduler module: HolidayRequest, RosterState, and MultiAgentScheduler.

MultiAgentScheduler runs evolutionary generations of Agent pairs. Each
generation both agents independently process the full request queue on
fresh copies of the roster. The agent with the higher total reward wins
and its Genes (plus a small mutation) seed the next generation.
"""

from __future__ import annotations

import copy
import random

from app.core.agent import Agent, Genes


# ---------------------------------------------------------------------------
# Domain objects
# ---------------------------------------------------------------------------


class HolidayRequest:
    """A holiday request waiting in the scheduling queue."""

    def __init__(
        self,
        request_id: int,
        employee_id: int,
        duration_days: int,
        preferred_start_days: list[int] | None = None,
    ):
        self.request_id = request_id
        self.employee_id = employee_id
        self.duration_days = duration_days
        # Optional list of preferred start days (integer day indices)
        self.preferred_start_days: list[int] = preferred_start_days or []

    def __repr__(self) -> str:
        return (
            f"HolidayRequest(id={self.request_id}, "
            f"employee={self.employee_id}, "
            f"duration={self.duration_days}d)"
        )


class RosterState:
    """
    Tracks the current state of the holiday roster.

    Days are represented as non-negative integers (0 = first day of the
    scheduling period). ``total_days`` is the length of the period.
    ``max_off_per_day`` is the maximum number of employees who can be on
    holiday on any single day.
    """

    def __init__(
        self,
        employee_ids: list[int],
        total_days: int,
        max_off_per_day: int = 2,
    ):
        self.employee_ids = list(employee_ids)
        self.total_days = total_days
        self.max_off_per_day = max_off_per_day
        # employee_id -> total days of holiday placed so far
        self._holiday_days: dict[int, int] = {eid: 0 for eid in employee_ids}
        # day -> list of employee_ids on holiday that day
        self._schedule: dict[int, list[int]] = {}

    # ------------------------------------------------------------------
    # Queries
    # ------------------------------------------------------------------

    def employee_holiday_days(self, employee_id: int) -> int:
        return self._holiday_days.get(employee_id, 0)

    def average_holiday_days(self) -> float:
        if not self._holiday_days:
            return 0.0
        return sum(self._holiday_days.values()) / len(self._holiday_days)

    def employees_off(self, day: int) -> int:
        """Return the number of employees on holiday on *day*."""
        return len(self._schedule.get(day, []))

    def coverage_penalty(self, start_day: int, duration_days: int) -> float:
        """
        Return a penalty score for placing a block starting on *start_day*.

        The penalty increases as available coverage drops toward zero.
        """
        penalty = 0.0
        for offset in range(duration_days):
            day = start_day + offset
            off = self.employees_off(day) + 1  # +1 for the request being evaluated
            remaining = len(self.employee_ids) - off
            if remaining <= 0:
                penalty += 10.0
            elif remaining == 1:
                penalty += 5.0
            elif remaining == 2:
                penalty += 2.0
        return penalty

    def available_start_days(self, duration_days: int) -> list[int]:
        """
        Return all start days where a block of *duration_days* fits within the
        scheduling period without exceeding *max_off_per_day* on any day.
        """
        result = []
        for start in range(self.total_days - duration_days + 1):
            if all(
                self.employees_off(start + offset) < self.max_off_per_day
                for offset in range(duration_days)
            ):
                result.append(start)
        return result

    # ------------------------------------------------------------------
    # Mutations
    # ------------------------------------------------------------------

    def place(self, request: HolidayRequest, start_day: int) -> None:
        """Record a holiday placement on the roster."""
        for offset in range(request.duration_days):
            day = start_day + offset
            self._schedule.setdefault(day, []).append(request.employee_id)
        self._holiday_days[request.employee_id] = (
            self._holiday_days.get(request.employee_id, 0) + request.duration_days
        )

    # ------------------------------------------------------------------
    # Reporting
    # ------------------------------------------------------------------

    def summary(self) -> dict:
        """Return a summary dict for reporting purposes."""
        return {
            "total_days": self.total_days,
            "employee_count": len(self.employee_ids),
            "holiday_days_per_employee": dict(self._holiday_days),
            "days_with_any_holiday": len(self._schedule),
        }


# ---------------------------------------------------------------------------
# Multi-agent scheduler
# ---------------------------------------------------------------------------


class MultiAgentScheduler:
    """
    Manages evolutionary generations of Agent pairs.

    Each generation:
      1. Two agents independently process all requests in the queue on
         separate copies of the roster (queue is consumed, agents end).
      2. The agent with the higher *total_reward* is selected.
      3. The winner's Genes are mutated slightly to seed the next generation.

    After *generations* rounds the final winner and its placed schedule are
    returned together with the full generation history.
    """

    def __init__(
        self,
        employee_ids: list[int],
        total_days: int,
        max_off_per_day: int = 2,
        generations: int = 5,
        seed: int | None = None,
    ):
        self.employee_ids = employee_ids
        self.total_days = total_days
        self.max_off_per_day = max_off_per_day
        self.generations = generations
        if seed is not None:
            random.seed(seed)

    # ------------------------------------------------------------------
    # Internal helpers
    # ------------------------------------------------------------------

    def _fresh_roster(self) -> RosterState:
        return RosterState(self.employee_ids, self.total_days, self.max_off_per_day)

    def _initial_agents(self) -> list[Agent]:
        """Create the first pair of agents with diverse initial strategies."""
        genes_a = Genes(
            fairness_weight=1.2,
            preference_weight=1.0,
            coverage_weight=1.0,
            exploration_rate=0.3,
        )
        genes_b = Genes(
            fairness_weight=0.8,
            preference_weight=1.5,
            coverage_weight=1.2,
            exploration_rate=0.2,
        )
        return [
            Agent(agent_id=1, genes=genes_a, generation=1),
            Agent(agent_id=2, genes=genes_b, generation=1),
        ]

    def _spawn_next_generation(self, winner: Agent, generation: int) -> list[Agent]:
        """Return two new agents whose Genes are derived from *winner*."""
        child_genes_a = winner.genes.mutate(mutation_rate=0.25)
        child_genes_b = winner.genes.mutate(mutation_rate=0.25)
        return [
            Agent(agent_id=1, genes=child_genes_a, generation=generation),
            Agent(agent_id=2, genes=child_genes_b, generation=generation),
        ]

    def _run_agent(self, agent: Agent, queue: list[HolidayRequest]) -> RosterState:
        """Run *agent* on an independent copy of the roster and return the result."""
        roster_copy = self._fresh_roster()
        agent.run(list(queue), roster_copy)  # agent consumes the queue copy
        return roster_copy

    def _select_winner(self, agents: list[Agent]) -> Agent:
        """Return the agent with the highest total reward (ties broken by id)."""
        return max(agents, key=lambda a: (a.total_reward, -a.agent_id))

    # ------------------------------------------------------------------
    # Public API
    # ------------------------------------------------------------------

    def run(self, requests: list[HolidayRequest]) -> dict:
        """
        Run the multi-agent evolutionary scheduling loop.

        Parameters
        ----------
        requests:
            The queue of holiday requests to be processed (order preserved).

        Returns
        -------
        dict with keys:
          - ``"generations"``: list of per-generation result dicts
          - ``"winner"``: the best agent from the final generation
          - ``"final_roster"``: RosterState produced by the final winner
        """
        agents = self._initial_agents()
        generation_history = []

        print(f"{'='*60}")
        print(f"Multi-Agent Holiday Scheduler")
        print(f"  Employees : {len(self.employee_ids)}")
        print(f"  Days      : {self.total_days}")
        print(f"  Requests  : {len(requests)}")
        print(f"  Generations: {self.generations}")
        print(f"{'='*60}")

        final_winner: Agent | None = None
        final_roster: RosterState | None = None

        for gen_num in range(1, self.generations + 1):
            print(f"\n── Generation {gen_num} ──")
            rosters: list[RosterState] = []

            for agent in agents:
                roster = self._run_agent(agent, requests)
                rosters.append(roster)
                print(
                    f"  {agent}  |  genes: {agent.genes}"
                )

            winner = self._select_winner(agents)
            winner_roster = rosters[agents.index(winner)]

            print(
                f"  ✓ Winner: Agent {winner.agent_id} "
                f"(reward={winner.total_reward:.2f}, "
                f"success={winner.success_rate:.0%})"
            )

            generation_history.append(
                {
                    "generation": gen_num,
                    "agents": list(agents),
                    "winner": winner,
                    "winner_roster_summary": winner_roster.summary(),
                }
            )

            final_winner = winner
            final_roster = winner_roster

            if gen_num < self.generations:
                agents = self._spawn_next_generation(winner, gen_num + 1)

        print(f"\n{'='*60}")
        print("Scheduling complete.")
        if final_winner and final_roster:
            print(f"Best agent overall: {final_winner}")
            summary = final_roster.summary()
            print(f"Holiday days per employee: {summary['holiday_days_per_employee']}")
        print(f"{'='*60}\n")

        return {
            "generations": generation_history,
            "winner": final_winner,
            "final_roster": final_roster,
        }
