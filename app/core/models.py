class Employee:
    def __init__(self, employee_id, name, role):
        self.employee_id = employee_id
        self.name = name
        self.role = role

class Roster:
    def __init__(self, roster_id, shifts):
        self.roster_id = roster_id
        self.shifts = shifts  # List of Shift objects

class Shift:
    def __init__(self, shift_id, employee, start_time, end_time):
        self.shift_id = shift_id
        self.employee = employee  # Employee object
        self.start_time = start_time
        self.end_time = end_time

class Holiday:
    def __init__(self, holiday_id, date, description):
        self.holiday_id = holiday_id
        self.date = date
        self.description = description