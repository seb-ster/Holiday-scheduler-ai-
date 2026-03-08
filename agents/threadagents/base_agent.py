import threading
import time


class BaseAgent:
    """Lightweight base agent interface."""

    def __init__(self, name: str):
        self.name = name
        self._running = threading.Event()
        # Optional agent-local state container which implementations may use
        # to store persistent runtime state that can be cloned by child agents.
        self.state = {}
        # Simple reward balance for the agent (numeric)
        self.reward = 0.0

    def start(self):
        self._running.set()

    def stop(self):
        self._running.clear()

    def is_running(self) -> bool:
        return bool(self._running.is_set())

    def run_once(self):
        """Override in subclasses to perform one unit of work."""
        raise NotImplementedError()

    def clone_state_from(self, other: "BaseAgent", deep: bool = True):
        """Clone runtime state from another agent into this agent.

        This provides a simple mechanism for a new agent to inherit a
        predecessor's internal state. By default a deep copy is performed.
        """
        import copy

        if not hasattr(other, "state"):
            return
        self.state = copy.deepcopy(other.state) if deep else dict(other.state)

    def add_reward(self, amount: float):
        """Add a numeric reward to this agent's balance and record in state."""
        try:
            amount = float(amount)
        except Exception:
            amount = 0.0
        self.reward += amount
        self.state.setdefault("reward", 0.0)
        self.state["reward"] = float(self.state.get("reward", 0.0)) + amount
