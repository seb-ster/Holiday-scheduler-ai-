"""Non-blocking UI notifier for application UIs.

Agents and background workers should use this notifier to publish small,
lightweight events for the UI. The notifier uses a bounded queue and a
background dispatcher that keeps a latest-snapshot dictionary. `notify()` is
non-blocking and will drop events if the queue is full, ensuring the UI
publisher never blocks the producing (agent) thread.
"""
from queue import Queue, Full, Empty
from threading import Thread, Event
import time
from typing import Any, Dict, Optional


class UINotifier:
    def __init__(self, max_queue: int = 1024):
        self._q: Queue = Queue(maxsize=max_queue)
        self._stop = Event()
        self._snapshot: Dict[str, Any] = {}
        self._thread = Thread(target=self._run, name="ui-notifier", daemon=True)
        self._thread.start()

    def notify(self, key: str, payload: Any) -> bool:
        """Attempt to publish `payload` under `key`.

        Returns True if enqueued, False if dropped (queue full).
        This call is non-blocking.
        """
        try:
            self._q.put_nowait((key, payload))
            return True
        except Full:
            # Drop the event to keep producers non-blocking
            return False

    def _run(self):
        while not self._stop.is_set():
            try:
                key, payload = self._q.get(timeout=0.5)
            except Empty:
                continue
            # Update latest snapshot — keep last value per key
            self._snapshot[key] = payload
            # mark processed
            self._q.task_done()

    def get_snapshot(self) -> Dict[str, Any]:
        return dict(self._snapshot)

    def stop(self, timeout: Optional[float] = None):
        self._stop.set()
        self._thread.join(timeout)


# Provide a module-level default notifier for convenience
default_notifier = UINotifier()
