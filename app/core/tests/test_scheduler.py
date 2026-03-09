"""Unit tests for HolidayRequest, RosterState, and MultiAgentScheduler."""

import random

import pytest

from app.core.scheduler import HolidayRequest, MultiAgentScheduler, RosterState


# ---------------------------------------------------------------------------
# HolidayRequest
# ---------------------------------------------------------------------------


class TestHolidayRequest:
    def test_defaults(self):
        req = HolidayRequest(request_id=1, employee_id=5, duration_days=3)
        assert req.preferred_start_days == []

    def test_preferred_start_days_stored(self):
        req = HolidayRequest(1, 1, 5, preferred_start_days=[10, 20])
        assert req.preferred_start_days == [10, 20]

    def test_repr(self):
        req = HolidayRequest(7, 3, 4)
        assert "7" in repr(req)
        assert "3" in repr(req)


# ---------------------------------------------------------------------------
# RosterState
# ---------------------------------------------------------------------------


def make_roster(employee_ids=(1, 2, 3, 4), total_days=30, max_off=2):
    return RosterState(list(employee_ids), total_days, max_off)


class TestRosterState:
    def test_initial_holiday_days_zero(self):
        roster = make_roster()
        for eid in (1, 2, 3, 4):
            assert roster.employee_holiday_days(eid) == 0

    def test_average_holiday_days_zero_initially(self):
        roster = make_roster()
        assert roster.average_holiday_days() == 0.0

    def test_place_updates_counts(self):
        roster = make_roster()
        req = HolidayRequest(1, employee_id=1, duration_days=3)
        roster.place(req, start_day=0)
        assert roster.employee_holiday_days(1) == 3
        assert roster.employees_off(0) == 1
        assert roster.employees_off(2) == 1
        assert roster.employees_off(3) == 0

    def test_available_start_days_respects_capacity(self):
        roster = make_roster(employee_ids=(1, 2, 3), total_days=10, max_off=1)
        req = HolidayRequest(1, employee_id=1, duration_days=1)
        roster.place(req, start_day=0)

        # Day 0 now has 1 employee off; max_off=1 so it should not appear
        available = roster.available_start_days(duration_days=1)
        assert 0 not in available

    def test_available_start_days_block_fits(self):
        roster = make_roster(total_days=10, max_off=4)
        # 3-day block can't start at day 9 (would extend to day 11)
        available = roster.available_start_days(duration_days=3)
        assert all(d <= 7 for d in available)

    def test_coverage_penalty_increases_when_crowded(self):
        roster = make_roster(employee_ids=(1, 2, 3), total_days=10, max_off=2)
        # Place one employee on day 5
        roster.place(HolidayRequest(1, 1, 1), start_day=5)
        # Place another on day 5
        roster.place(HolidayRequest(2, 2, 1), start_day=5)

        # Adding a third on day 5 should leave 0 employees remaining → high penalty
        penalty = roster.coverage_penalty(start_day=5, duration_days=1)
        assert penalty > 0

    def test_coverage_penalty_zero_when_plenty_of_staff(self):
        roster = make_roster(employee_ids=list(range(1, 11)), total_days=10, max_off=5)
        penalty = roster.coverage_penalty(start_day=0, duration_days=3)
        assert penalty == 0.0

    def test_summary_keys(self):
        roster = make_roster()
        s = roster.summary()
        assert "total_days" in s
        assert "employee_count" in s
        assert "holiday_days_per_employee" in s


# ---------------------------------------------------------------------------
# MultiAgentScheduler
# ---------------------------------------------------------------------------


def simple_requests():
    return [
        HolidayRequest(i, employee_id=(i % 4) + 1, duration_days=3, preferred_start_days=[i * 5])
        for i in range(1, 9)
    ]


class TestMultiAgentScheduler:
    def test_run_returns_expected_keys(self):
        scheduler = MultiAgentScheduler(
            employee_ids=[1, 2, 3, 4],
            total_days=60,
            max_off_per_day=2,
            generations=2,
            seed=0,
        )
        result = scheduler.run(simple_requests())
        assert "generations" in result
        assert "winner" in result
        assert "final_roster" in result

    def test_generation_count_matches(self):
        scheduler = MultiAgentScheduler(
            employee_ids=[1, 2, 3, 4],
            total_days=60,
            max_off_per_day=2,
            generations=3,
            seed=0,
        )
        result = scheduler.run(simple_requests())
        assert len(result["generations"]) == 3

    def test_winner_is_agent(self):
        from app.core.agent import Agent

        scheduler = MultiAgentScheduler(
            employee_ids=[1, 2, 3, 4],
            total_days=60,
            generations=2,
            seed=1,
        )
        result = scheduler.run(simple_requests())
        assert isinstance(result["winner"], Agent)

    def test_final_roster_is_roster_state(self):
        scheduler = MultiAgentScheduler(
            employee_ids=[1, 2, 3, 4],
            total_days=60,
            generations=2,
            seed=2,
        )
        result = scheduler.run(simple_requests())
        assert isinstance(result["final_roster"], RosterState)

    def test_winner_reward_nonnegative(self):
        scheduler = MultiAgentScheduler(
            employee_ids=list(range(1, 7)),
            total_days=90,
            max_off_per_day=3,
            generations=4,
            seed=42,
        )
        requests = [
            HolidayRequest(i, employee_id=(i % 6) + 1, duration_days=5)
            for i in range(1, 7)
        ]
        result = scheduler.run(requests)
        assert result["winner"].total_reward >= 0

    def test_each_generation_has_two_agents(self):
        scheduler = MultiAgentScheduler(
            employee_ids=[1, 2, 3],
            total_days=30,
            generations=3,
            seed=5,
        )
        result = scheduler.run(simple_requests()[:4])
        for gen_data in result["generations"]:
            assert len(gen_data["agents"]) == 2

    def test_generations_inherit_from_winner(self):
        """Agents in gen 2+ should have genes derived from gen 1 winner."""
        scheduler = MultiAgentScheduler(
            employee_ids=[1, 2, 3, 4],
            total_days=60,
            generations=2,
            seed=10,
        )
        result = scheduler.run(simple_requests())
        gen1_winner = result["generations"][0]["winner"]
        gen2_agents = result["generations"][1]["agents"]

        # Gene values should be close (within mutation range) to the parent
        for child in gen2_agents:
            assert abs(child.genes.fairness_weight - gen1_winner.genes.fairness_weight) < 2.0
            assert child.generation == 2
