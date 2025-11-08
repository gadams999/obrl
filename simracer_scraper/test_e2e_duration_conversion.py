"""Test race_duration conversion to minutes."""
import re

# Test the conversion logic
def convert_duration_to_minutes(duration_str):
    """Convert '1h 11m' to total minutes."""
    try:
        hours_match = re.search(r"(\d+)h", duration_str)
        minutes_match = re.search(r"(\d+)m", duration_str)
        
        hours = int(hours_match.group(1)) if hours_match else 0
        minutes = int(minutes_match.group(1)) if minutes_match else 0
        
        return (hours * 60) + minutes
    except (ValueError, AttributeError):
        return None

# Test cases
test_cases = [
    ("0h 59m", 59),
    ("1h 11m", 71),
    ("2h 30m", 150),
    ("0h 0m", 0),
    ("3h 0m", 180),
]

print("Testing duration conversion:")
for duration, expected in test_cases:
    result = convert_duration_to_minutes(duration)
    status = "✓" if result == expected else "✗"
    print(f"{status} '{duration}' -> {result} minutes (expected {expected})")

print("\nAll tests passed!" if all(convert_duration_to_minutes(d) == e for d, e in test_cases) else "\nSome tests failed!")
