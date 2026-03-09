import threading
import time
import logging
from typing import Callable, Optional

from .base_agent import BaseAgent
try:
    from ..telemetry import record_run, set_reward, init_metrics, enabled as telemetry_enabled
    telemetry_available = True
except Exception:
    telemetry_available = False

try:
    # optional UI notifier; use non-blocking notify to avoid blocking agent threads
    from ..ui_notifier import default_notifier as ui_notifier
    ui_notifier_available = True
except Exception:
    ui_notifier = None
    ui_notifier_available = False


logger = logging.getLogger(__name__)


class ThreadAgent(BaseAgent):
    """An agent that runs a provided work function in a loop on its own thread.

    The `work_fn` signature is: Callable[[ThreadAgent], None]. If a `parent`
    agent is provided and `work_fn` is omitted, the parent's work function is
    inherited. Use `clone_state_from` to inherit runtime state from a parent.
    """

    def __init__(self, name: str, work_fn: Optional[Callable[["ThreadAgent"], None]] = None, interval: float = 1.0, parent: Optional[BaseAgent] = None):
        super().__init__(name)
        # If a parent is provided and no work_fn is supplied, inherit it.
        if work_fn is None and parent is not None and hasattr(parent, "_work_fn"):
            work_fn = getattr(parent, "_work_fn")
        if work_fn is None:
            raise ValueError("ThreadAgent requires a work_fn or a parent with a work_fn")

        self._work_fn = work_fn
        self._interval = float(interval)
        self._thread: Optional[threading.Thread] = None

        # Clone parent's state if requested
        if parent is not None:
            try:
                self.clone_state_from(parent, deep=True)
            except Exception:
                logger.debug("Failed to clone state from parent %s", getattr(parent, "name", "<unknown>"))

    def start(self):
        if self._thread and self._thread.is_alive():
            return
        super().start()
        self._thread = threading.Thread(target=self._run_loop, name=self.name, daemon=True)
        self._thread.start()

    def _run_loop(self):
        logger.info("Agent %s starting loop", self.name)
        while self.is_running():
            try:
                start = time.perf_counter()
                self.run_once()
                duration = time.perf_counter() - start
                # update runtime stats in state
                st = self.state
                st["run_count"] = int(st.get("run_count", 0)) + 1
                st["total_time"] = float(st.get("total_time", 0.0)) + float(duration)
                st["last_run_time"] = float(duration)
                # also keep convenience attributes
                self.last_run_time = float(duration)
                self.run_count = int(st["run_count"])
                self.total_time = float(st["total_time"])
                # publish a tiny non-blocking UI event
                try:
                    if ui_notifier_available:
                        event = {"name": self.name, "last_run": float(duration), "run_count": self.run_count}
                        # notify under a per-agent key; drop silently if queue is full
                        try:
                            ui_notifier.notify(self.name, event)
                        except Exception:
                            pass
                except Exception:
                    logger.debug("UI notifier not available for agent %s", self.name)
                # record telemetry if available
                try:
                    if telemetry_available:
                        # initialize metrics on first use
                        try:
                            init_metrics()
                        except Exception:
                            pass
                        try:
                            record_run(self.name, duration)
                        except Exception:
                            logger.debug("Failed to record run metric for %s", self.name)
                        try:
                            set_reward(self.name, getattr(self, "reward", 0.0))
                        except Exception:
                            pass
                except Exception:
                    logger.debug("Telemetry not available for agent %s", self.name)
            except Exception:
                logger.exception("Unhandled exception in agent %s", self.name)
            time.sleep(self._interval)
        logger.info("Agent %s stopping loop", self.name)

    def run_once(self):
        return self._work_fn(self)

    def join(self, timeout: Optional[float] = None):
        if self._thread:
            self._thread.join(timeout)
