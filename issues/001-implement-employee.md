# Implement `Employee` data structure

## Summary

We need a canonical `Employee` data structure to represent staff members in the scheduler. This model will be used across agents, the simulation environment, and the GUI.

## Motivation

Right now there is no centralized representation of employees (name, ID, availability, skills, seniority, preferences). Having a single, well-documented model will make it easier to implement placement heuristics, multi-agent strategies, and UI features.

## Proposed fields

- `id` (str): unique identifier
- `name` (str)
- `roles` (List[str]) — skills or eligible shift types
- `seniority` (int) — numeric ranking for tie-breaking
- `availability` (list of date/range objects) — days/times employee is available
- `preferences` (dict) — e.g., preferred shifts, days off, teammates
- `max_hours_per_week` (int)
- `metadata` (dict) — freeform for extensions

## Acceptance criteria

- A Python dataclass or pydantic model exists at `holiday_roster.models.employee` or `agents/models/employee.py`.
- Serialization to/from JSON is supported.
- Unit tests cover basic validation and serialization.

## Notes

Keep the model minimal and extendable. Start with availability expressed as simple day ranges; we can add time ranges later.
