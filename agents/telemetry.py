"""Lightweight Prometheus telemetry helpers for agents.

This module exposes simple helper functions to record agent metrics. It uses
`prometheus_client` if available; otherwise functions are no-ops.
"""
from typing import Optional

_enabled = False
try:
    from prometheus_client import Counter, Gauge, Summary
    _enabled = True
except Exception:  # pragma: no cover - optional dependency
    Counter = Gauge = Summary = None


_runs = None
_run_duration = None
_last_run = None
_reward = None


def init_metrics():
    global _runs, _run_duration, _last_run, _reward
    if not _enabled:
        return
    _runs = Counter("agent_runs_total", "Total number of agent runs", ["agent"])
    _run_duration = Summary("agent_run_duration_seconds", "Agent run duration seconds", ["agent"])
    _last_run = Gauge("agent_last_run_seconds", "Agent last run duration seconds", ["agent"])
    _reward = Gauge("agent_reward", "Agent reward balance", ["agent"])


def record_run(agent: str, duration: float):
    if not _enabled or _runs is None:
        return
    _runs.labels(agent=agent).inc()
    _run_duration.labels(agent=agent).observe(float(duration))
    _last_run.labels(agent=agent).set(float(duration))


def set_reward(agent: str, amount: float):
    if not _enabled or _reward is None:
        return
    _reward.labels(agent=agent).set(float(amount))


def enabled() -> bool:
    return _enabled
