# Implement `Request` data structure

## Summary

Define a `Request` model representing a single scheduling request: an employee or manager request for a shift, day off, swap, or other action.

## Proposed fields

- `id` (str)
- `type` (str): e.g., `shift_request`, `day_off`, `swap`, `cover`
- `employee_id` (str) — who requested it
- `target_date` / `date_range` — requested date or range
- `shift_type` (str, optional)
- `priority` (int) — higher priority requests are favored
- `status` (str) — `pending`, `approved`, `rejected`, `fulfilled`
- `metadata` (dict)

## Acceptance criteria

- A `Request` dataclass or model at `holiday_roster.models.request` or `agents/models/request.py`.
- Support for serializing request status changes.
- Unit tests demonstrating creation and basic lifecycle transitions.
