import logging
from typing import Callable, Dict, List, Optional

from .thread_agent import ThreadAgent


logger = logging.getLogger(__name__)


class AgentManager:
    """Manage multiple ThreadAgent instances.

    Allows spawning N agents automatically and stopping/joining them.
    """

    def __init__(self):
        self._agents: Dict[str, ThreadAgent] = {}

    def spawn(self, name: str, work_fn: Callable[[ThreadAgent], None], interval: float = 1.0) -> ThreadAgent:
        if name in self._agents:
            raise ValueError(f"Agent with name '{name}' already exists")
        agent = ThreadAgent(name=name, work_fn=work_fn, interval=interval)
        self._agents[name] = agent
        agent.start()
        logger.info("Spawned agent %s", name)
        return agent

    def spawn_auto(self, prefix: str, n: int, work_fn_factory: Callable[[int], Callable[[ThreadAgent], None]], interval: float = 1.0) -> List[ThreadAgent]:
        agents = []
        for i in range(n):
            name = f"{prefix}-{i+1}"
            agent = self.spawn(name, work_fn_factory(i), interval=interval)
            agents.append(agent)
        return agents

    def spawn_from(self, parent_name: str, new_name: str, override_work_fn: Optional[Callable[[ThreadAgent], None]] = None, copy_state: bool = True, interval: Optional[float] = None) -> ThreadAgent:
        """Spawn a new agent that inherits behaviour (and optionally state)
        from an existing agent named `parent_name`.

        - `override_work_fn`: if provided, use this work function instead of the parent's.
        - `copy_state`: if True, clone parent's `state` into the new agent.
        - `interval`: override the parent's interval if provided.
        """
        parent = self._agents.get(parent_name)
        if parent is None:
            raise KeyError(f"Parent agent '{parent_name}' not found")
        if new_name in self._agents:
            raise ValueError(f"Agent with name '{new_name}' already exists")

        work_fn = override_work_fn or getattr(parent, "_work_fn", None)
        agent_interval = interval if interval is not None else getattr(parent, "_interval", 1.0)
        agent = ThreadAgent(name=new_name, work_fn=work_fn, interval=agent_interval, parent=parent if copy_state else None)
        self._agents[new_name] = agent
        agent.start()
        logger.info("Spawned agent %s inheriting from %s", new_name, parent_name)
        return agent

    def reward_fastest(self, amount: float):
        """Award `amount` to the agent with the lowest average run time.

        Average run time is computed as `total_time / run_count`. Agents with
        no runs are ignored. If multiple agents tie, the first encountered is
        awarded.
        """
        best = None
        best_avg = None
        for a in self._agents.values():
            st = getattr(a, "state", {})
            rc = st.get("run_count", 0)
            tt = st.get("total_time", 0.0)
            if rc <= 0:
                continue
            avg = float(tt) / float(rc)
            if best is None or avg < best_avg:
                best = a
                best_avg = avg

        if best is None:
            raise RuntimeError("No agent has run yet to determine fastest")

        best.add_reward(amount)
        logger.info("Awarded reward %s to agent %s (avg run time %.6f)", amount, best.name, best_avg)
        return best

    def stop(self, name: str):
        agent = self._agents.get(name)
        if not agent:
            return
        agent.stop()

    def stop_all(self):
        for a in list(self._agents.values()):
            a.stop()

    def join_all(self, timeout: Optional[float] = None):
        for a in list(self._agents.values()):
            a.join(timeout)
