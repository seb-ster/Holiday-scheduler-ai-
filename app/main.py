"""
Holiday Scheduler AI – main entry point.

Runs a demonstration of the multi-agent scheduling system described in
issue #1: agents process a queue of holiday requests, competing to
maximise their reward. The better agent of each generation passes its
strategy genes to the next generation.
"""

import random

from app.core.scheduler import HolidayRequest, MultiAgentScheduler


def build_demo_requests(
    employee_ids: list[int],
    total_days: int,
    requests_per_employee: int = 1,
    seed: int = 42,
) -> list[HolidayRequest]:
    """
    Generate a simple set of holiday requests for demonstration purposes.

    Each employee submits *requests_per_employee* request(s) for a random
    duration and with random preferred start days.
    """
    rng = random.Random(seed)
    requests = []
    req_id = 1
    for emp_id in employee_ids:
        for _ in range(requests_per_employee):
            duration = rng.randint(3, 7)
            max_start = total_days - duration
            preferred = [rng.randint(0, max_start) for _ in range(rng.randint(1, 3))]
            requests.append(
                HolidayRequest(
                    request_id=req_id,
                    employee_id=emp_id,
                    duration_days=duration,
                    preferred_start_days=preferred,
                )
            )
            req_id += 1
    rng.shuffle(requests)
    return requests


def main() -> None:
    # ---------------------------------------------------------------
    # Configuration
    # ---------------------------------------------------------------
    employee_ids = list(range(1, 9))   # 8 employees
    total_days = 90                    # 90-day scheduling period (~1 quarter)
    max_off_per_day = 3                # at most 3 employees off on any given day
    generations = 6

    # ---------------------------------------------------------------
    # Build the request queue
    # ---------------------------------------------------------------
    requests = build_demo_requests(
        employee_ids,
        total_days,
        requests_per_employee=2,
        seed=42,
    )

    print(f"Generated {len(requests)} holiday requests:\n")
    for req in requests:
        print(f"  {req}  preferred_starts={req.preferred_start_days}")
    print()

    # ---------------------------------------------------------------
    # Run the multi-agent scheduler
    # ---------------------------------------------------------------
    scheduler = MultiAgentScheduler(
        employee_ids=employee_ids,
        total_days=total_days,
        max_off_per_day=max_off_per_day,
        generations=generations,
        seed=7,
    )

    result = scheduler.run(requests)

    # ---------------------------------------------------------------
    # Final report
    # ---------------------------------------------------------------
    winner = result["winner"]
    final_roster = result["final_roster"]

    print("── Placement details (final winner) ──")
    for entry in winner.placement_history:
        req = entry["request"]
        day = entry["start_day"]
        rwd = entry["reward"]
        if day is not None:
            print(
                f"  Request {req.request_id:>2} (emp {req.employee_id}) "
                f"→ day {day:>3}–{day + req.duration_days - 1:<3}  "
                f"reward={rwd:+.2f}"
            )
        else:
            print(
                f"  Request {req.request_id:>2} (emp {req.employee_id}) "
                f"→ FAILED  reward={rwd:+.2f}"
            )


if __name__ == "__main__":
    main()
