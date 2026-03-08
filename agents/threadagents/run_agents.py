"""Example runner for the thread agent framework.

Run this script to start several agents that log activity. This can be used as
an example scaffold for spawning agents automatically.
"""

import logging
import time
from agents.threadagents import AgentManager


logging.basicConfig(level=logging.INFO, format="%(asctime)s %(levelname)s %(message)s")


def sample_work_factory(i: int):
    counter = {"count": 0}

    def work(agent):
        counter["count"] += 1
        logging.info("%s ran sample work (%d)", agent.name, counter["count"])

    return work


def main():
    mgr = AgentManager()
    agents = mgr.spawn_auto(prefix="agent", n=3, work_fn_factory=sample_work_factory, interval=1.0)
    try:
        # Let them run for a short demo period
        time.sleep(5)
    finally:
        mgr.stop_all()
        mgr.join_all()


if __name__ == "__main__":
    main()
