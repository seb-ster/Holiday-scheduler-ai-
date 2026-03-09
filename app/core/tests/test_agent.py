"""Unit tests for Genes and Agent classes."""

import copy
import random

import pytest

from app.core.agent import Agent, Genes
from app.core.scheduler import HolidayRequest, RosterState


# ---------------------------------------------------------------------------
# Genes
# ---------------------------------------------------------------------------


class TestGenes:
    def test_default_values(self):
        g = Genes()
        assert g.fairness_weight == 1.0
        assert g.preference_weight == 1.0
        assert g.coverage_weight == 1.0
        assert g.exploration_rate == 0.3

    def test_custom_values(self):
        g = Genes(fairness_weight=2.0, preference_weight=0.5, coverage_weight=1.5, exploration_rate=0.1)
        assert g.fairness_weight == 2.0
        assert g.preference_weight == 0.5

    def test_exploration_rate_clamped(self):
        g_low = Genes(exploration_rate=-1.0)
        assert g_low.exploration_rate >= 0.05

        g_high = Genes(exploration_rate=99.0)
        assert g_high.exploration_rate <= 1.0

    def test_mutate_returns_new_object(self):
        g = Genes()
        m = g.mutate(mutation_rate=1.0)
        assert m is not g

    def test_mutate_stays_positive(self):
        random.seed(0)
        g = Genes(fairness_weight=0.11, preference_weight=0.11, coverage_weight=0.11)
        for _ in range(50):
            g = g.mutate(mutation_rate=1.0)
            assert g.fairness_weight > 0
            assert g.preference_weight > 0
            assert g.coverage_weight > 0
            assert 0.05 <= g.exploration_rate <= 1.0


# ---------------------------------------------------------------------------
# Agent – scoring
# ---------------------------------------------------------------------------


def make_roster(employee_ids=(1, 2, 3, 4), total_days=30, max_off=2):
    return RosterState(list(employee_ids), total_days, max_off)


def make_request(req_id=1, employee_id=1, duration=3, preferred=None):
    return HolidayRequest(req_id, employee_id, duration, preferred or [])


class TestAgentScoring:
    def test_preference_bonus_applied(self):
        agent = Agent(agent_id=1, genes=Genes(preference_weight=2.0, exploration_rate=0.0))
        roster = make_roster()
        req = make_request(preferred=[5])

        score_pref = agent.score_placement(req, start_day=5, roster_state=roster)
        score_no_pref = agent.score_placement(req, start_day=10, roster_state=roster)
        # preferred start should score higher
        assert score_pref > score_no_pref

    def test_coverage_penalty_applied(self):
        roster = make_roster(employee_ids=(1, 2, 3), total_days=30, max_off=2)
        # Fill day 0 so any additional placement there incurs a penalty
        existing_req = make_request(req_id=99, employee_id=2, duration=1)
        roster.place(existing_req, start_day=0)

        agent = Agent(agent_id=1, genes=Genes(coverage_weight=3.0, exploration_rate=0.0))
        req = make_request(employee_id=1, duration=1)

        score_crowded = agent.score_placement(req, start_day=0, roster_state=roster)
        score_clear = agent.score_placement(req, start_day=20, roster_state=roster)
        assert score_crowded < score_clear

    def test_fairness_bonus_applied(self):
        roster = make_roster(employee_ids=(1, 2), total_days=30, max_off=2)
        # Give employee 2 some holidays already
        req2 = HolidayRequest(99, employee_id=2, duration_days=5)
        roster.place(req2, start_day=0)

        agent = Agent(agent_id=1, genes=Genes(fairness_weight=2.0, preference_weight=0.0, coverage_weight=0.0, exploration_rate=0.0))
        req1 = make_request(employee_id=1, duration=3)

        # Employee 1 has 0 days; average is 2.5 → fairness bonus should be positive
        score = agent.score_placement(req1, start_day=10, roster_state=roster)
        assert score > 0


# ---------------------------------------------------------------------------
# Agent – choose_start_day
# ---------------------------------------------------------------------------


class TestAgentChooseStartDay:
    def test_returns_none_when_no_slots(self):
        agent = Agent(agent_id=1, genes=Genes(exploration_rate=0.0))
        roster = make_roster()
        req = make_request()
        result = agent.choose_start_day(req, [], roster)
        assert result is None

    def test_exploitation_picks_best(self):
        random.seed(42)
        agent = Agent(agent_id=1, genes=Genes(preference_weight=10.0, exploration_rate=0.0))
        roster = make_roster(total_days=30)
        req = make_request(preferred=[5])
        # With exploration_rate=0 the agent must pick the preferred day
        chosen = agent.choose_start_day(req, [0, 5, 15], roster)
        assert chosen == 5

    def test_exploration_can_pick_any(self):
        random.seed(1)
        agent = Agent(agent_id=1, genes=Genes(exploration_rate=1.0))
        roster = make_roster(total_days=30)
        req = make_request(preferred=[5])
        choices = {agent.choose_start_day(req, [0, 5, 15], roster) for _ in range(30)}
        # With full exploration all three slots should eventually be chosen
        assert len(choices) > 1


# ---------------------------------------------------------------------------
# Agent – process_request
# ---------------------------------------------------------------------------


class TestAgentProcessRequest:
    def test_successful_placement_increments_counters(self):
        agent = Agent(agent_id=1, genes=Genes(exploration_rate=0.0))
        roster = make_roster(total_days=30, max_off=3)
        req = make_request()

        start_day, reward = agent.process_request(req, roster)

        assert start_day is not None
        assert agent.placements == 1
        assert agent.successful_placements == 1
        assert agent.failed_placements == 0
        assert agent.total_reward == reward

    def test_failed_placement_when_no_slots(self):
        # Only 1 employee; max_off=1. Place one holiday then try another.
        roster = make_roster(employee_ids=(1,), total_days=5, max_off=1)
        req1 = HolidayRequest(1, employee_id=1, duration_days=5)
        roster.place(req1, start_day=0)

        agent = Agent(agent_id=2, genes=Genes(exploration_rate=0.0))
        req2 = HolidayRequest(2, employee_id=1, duration_days=3)
        start_day, reward = agent.process_request(req2, roster)

        assert start_day is None
        assert reward == -1.0
        assert agent.failed_placements == 1

    def test_run_consumes_full_queue(self):
        random.seed(0)
        agent = Agent(agent_id=1)
        roster = make_roster(total_days=60, max_off=4)
        queue = [make_request(req_id=i, employee_id=(i % 4) + 1, duration=3) for i in range(1, 6)]

        agent.run(list(queue), roster)

        assert agent.placements == 5
        assert len(agent.placement_history) == 5


# ---------------------------------------------------------------------------
# Agent – metrics
# ---------------------------------------------------------------------------


class TestAgentMetrics:
    def test_average_reward_zero_when_no_placements(self):
        agent = Agent(agent_id=1)
        assert agent.average_reward == 0.0

    def test_success_rate_zero_when_no_placements(self):
        agent = Agent(agent_id=1)
        assert agent.success_rate == 0.0

    def test_repr_contains_key_info(self):
        agent = Agent(agent_id=3, generation=2)
        r = repr(agent)
        assert "id=3" in r
        assert "gen=2" in r
