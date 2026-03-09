"""
Agent module: defines Genes and Agent for the multi-agent scheduling system.

Each Agent holds a set of Genes (strategy weights) that determine how it
scores candidate holiday placements. After a generation the better agent's
Genes are inherited (with optional mutation) by the next generation.
"""

from __future__ import annotations

import copy
import random
from typing import TYPE_CHECKING

if TYPE_CHECKING:
    from app.core.scheduler import HolidayRequest, RosterState


class Genes:
    """Strategy parameters carried by an agent and inherited across generations."""

    def __init__(
        self,
        fairness_weight: float = 1.0,
        preference_weight: float = 1.0,
        coverage_weight: float = 1.0,
        exploration_rate: float = 0.3,
    ):
        # How much to reward giving holidays to under-holidayed employees
        self.fairness_weight = fairness_weight
        # How much to reward matching an employee's preferred dates
        self.preference_weight = preference_weight
        # How much to penalise coverage gaps caused by the placement
        self.coverage_weight = coverage_weight
        # Probability of choosing a random slot instead of the best-scored one
        self.exploration_rate = max(0.05, min(1.0, exploration_rate))

    def mutate(self, mutation_rate: float = 0.2) -> "Genes":
        """Return a new Genes object with small Gaussian perturbations."""
        child = copy.copy(self)
        if random.random() < mutation_rate:
            child.fairness_weight = max(0.1, child.fairness_weight + random.gauss(0, 0.15))
        if random.random() < mutation_rate:
            child.preference_weight = max(0.1, child.preference_weight + random.gauss(0, 0.15))
        if random.random() < mutation_rate:
            child.coverage_weight = max(0.1, child.coverage_weight + random.gauss(0, 0.15))
        if random.random() < mutation_rate:
            child.exploration_rate = max(0.05, min(1.0, child.exploration_rate + random.gauss(0, 0.05)))
        return child

    def __repr__(self) -> str:
        return (
            f"Genes(fairness={self.fairness_weight:.2f}, "
            f"pref={self.preference_weight:.2f}, "
            f"coverage={self.coverage_weight:.2f}, "
            f"explore={self.exploration_rate:.2f})"
        )


class Agent:
    """
    A scheduling agent that processes holiday requests one-by-one from a queue.

    On each step the agent:
    1. Takes the next request from the queue (queue shrinks by one).
    2. Scores every available placement slot using its Genes.
    3. Places the request in the highest-scoring slot (or explores randomly).
    4. Accumulates the reward for that placement.

    When the queue is empty the agent ends. Its metrics are then compared with
    those of the sibling agent; the better agent's Genes seed the next generation.
    """

    def __init__(self, agent_id: int, genes: "Genes | None" = None, generation: int = 1):
        self.agent_id = agent_id
        self.generation = generation
        self.genes = genes if genes is not None else Genes()
        self.total_reward: float = 0.0
        self.placements: int = 0
        self.successful_placements: int = 0
        self.failed_placements: int = 0
        self.placement_history: list = []

    # ------------------------------------------------------------------
    # Scoring
    # ------------------------------------------------------------------

    def score_placement(self, request: "HolidayRequest", start_day: int, roster_state: "RosterState") -> float:
        """Return a scalar reward for placing *request* starting on *start_day*."""
        reward = 0.0

        # Fairness: reward when the requester has fewer holidays than average
        avg = roster_state.average_holiday_days()
        employee_days = roster_state.employee_holiday_days(request.employee_id)
        fairness_bonus = max(0.0, avg - employee_days)
        reward += self.genes.fairness_weight * fairness_bonus

        # Preference: bonus when start_day is among the employee's preferred days
        if start_day in request.preferred_start_days:
            reward += self.genes.preference_weight * 2.0

        # Coverage: penalty when the slot would leave critical coverage gaps
        coverage_penalty = roster_state.coverage_penalty(start_day, request.duration_days)
        reward -= self.genes.coverage_weight * coverage_penalty

        return reward

    # ------------------------------------------------------------------
    # Decision making
    # ------------------------------------------------------------------

    def choose_start_day(
        self, request: "HolidayRequest", available_days: list, roster_state: "RosterState"
    ) -> "int | None":
        """
        Choose a start day for the request.

        With probability *exploration_rate* a random available day is picked
        (exploration); otherwise the highest-scoring day is chosen (exploitation).
        Returns ``None`` when no slots are available.
        """
        if not available_days:
            return None
        if random.random() < self.genes.exploration_rate:
            return random.choice(available_days)
        return max(available_days, key=lambda d: self.score_placement(request, d, roster_state))

    # ------------------------------------------------------------------
    # Queue processing
    # ------------------------------------------------------------------

    def process_request(self, request: "HolidayRequest", roster_state: "RosterState") -> tuple:
        """
        Process one request from the queue (one step).

        Returns ``(start_day, reward)`` where *start_day* is ``None`` on failure.
        """
        available = roster_state.available_start_days(request.duration_days)
        start_day = self.choose_start_day(request, available, roster_state)

        if start_day is None:
            reward = -1.0
            self.failed_placements += 1
        else:
            reward = self.score_placement(request, start_day, roster_state)
            roster_state.place(request, start_day)
            self.successful_placements += 1

        self.total_reward += reward
        self.placements += 1
        self.placement_history.append(
            {"request": request, "start_day": start_day, "reward": reward}
        )
        return start_day, reward

    def run(self, queue: list, roster_state: "RosterState") -> "RosterState":
        """
        Process every request in *queue* (which is consumed left-to-right).

        Mutates *roster_state* in-place and returns it. Callers should pass a
        deep-copy of the shared roster so agents do not interfere with each other.
        """
        for request in queue:
            self.process_request(request, roster_state)
        return roster_state

    # ------------------------------------------------------------------
    # Metrics
    # ------------------------------------------------------------------

    @property
    def average_reward(self) -> float:
        if self.placements == 0:
            return 0.0
        return self.total_reward / self.placements

    @property
    def success_rate(self) -> float:
        if self.placements == 0:
            return 0.0
        return self.successful_placements / self.placements

    def __repr__(self) -> str:
        return (
            f"Agent(id={self.agent_id}, gen={self.generation}, "
            f"reward={self.total_reward:.2f}, "
            f"avg={self.average_reward:.3f}, "
            f"success={self.success_rate:.0%})"
        )
