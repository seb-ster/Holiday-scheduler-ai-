import time
from agents.threadagents.manager import AgentManager


def test_multiple_agents_increment_counter():
    mgr = AgentManager()
    counters = []

    def make_work(i):
        counter = {"count": 0}
        counters.append(counter)

        def work(agent):
            counter["count"] += 1

        return work

    # Spawn 3 agents with a short interval
    mgr.spawn_auto(prefix="t", n=3, work_fn_factory=make_work, interval=0.1)
    time.sleep(0.5)
    mgr.stop_all()
    mgr.join_all()

    # Each counter should have been incremented at least once
    assert all(c["count"] > 0 for c in counters)


def test_spawn_from_inherits_behavior_and_state():
    mgr = AgentManager()

    def parent_work(agent):
        agent.state.setdefault("runs", 0)
        agent.state["runs"] += 1

    parent = mgr.spawn("parent", parent_work, interval=0.05)
    # let parent run a bit and build state
    import time

    time.sleep(0.2)

    # spawn child inheriting parent's behaviour and state
    child = mgr.spawn_from(parent_name="parent", new_name="child", copy_state=True)
    time.sleep(0.2)
    mgr.stop_all()
    mgr.join_all()

    # child should have inherited a 'runs' counter and incremented it further
    assert "runs" in child.state
    assert child.state["runs"] > 0


def test_reward_fastest_agent():
    mgr = AgentManager()

    def fast_work(agent):
        # very short work
        agent.state.setdefault("x", 0)
        agent.state["x"] += 1

    def slow_work(agent):
        # slower work
        import time

        time.sleep(0.05)
        agent.state.setdefault("y", 0)
        agent.state["y"] += 1

    fast = mgr.spawn("fast", fast_work, interval=0.01)
    slow = mgr.spawn("slow", slow_work, interval=0.01)

    import time

    time.sleep(0.4)
    # Award reward to fastest agent
    winner = mgr.reward_fastest(42)
    mgr.stop_all()
    mgr.join_all()

    assert winner.name == "fast"
    assert winner.state.get("reward", 0) >= 42
