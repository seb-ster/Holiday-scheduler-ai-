# Implement calendar / schedule model

## Summary

Create a canonical calendar/schedule model that represents the roster for a period (day/week/month). This model will be the primary interface for agents to place shifts and compute rewards.

## Proposed components

- `Schedule` or `Roster` object containing:
  - date range
  - list of `Shift` entries (with `employee_id`, `shift_type`, `start`, `end`, `metadata`)
  - utility methods to query availability, conflicts, and compute metrics (hours, coverage)

- `Shift` model:
  - `id`, `employee_id`, `shift_type`, `start`, `end`, `tags`

## Acceptance criteria

- Models implemented under `holiday_roster.models.schedule` or `agents/models/schedule.py`.
- Methods to add/remove shifts, check conflicts, and compute total hours per employee.
- Tests for common scheduling operations and conflict detection.

## Notes

Start with day-level granularity; add time-of-day when needed.
