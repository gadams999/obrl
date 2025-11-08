"""Verify weather conversion logic."""
import re

# Temperature conversion: C to F
def convert_temp(temp_str):
    """Convert '88° F' or '23° C' to Fahrenheit integer."""
    temp_match = re.search(r"(\d+)°\s*([CF])", temp_str)
    if temp_match:
        temp_value = int(temp_match.group(1))
        temp_unit = temp_match.group(2)
        if temp_unit == "C":
            temp_f = int((temp_value * 9/5) + 32)
        else:
            temp_f = temp_value
        return temp_f
    return None

# Percentage extraction
def extract_pct(pct_str):
    """Extract '55%' to integer 55."""
    pct_match = re.search(r"(\d+)%?", pct_str)
    if pct_match:
        return int(pct_match.group(1))
    return None

print("Testing conversions:")
print(f"23° C → {convert_temp('23° C')} °F (expected 73)")
print(f"88° F → {convert_temp('88° F')} °F (expected 88)")
print(f"0° C → {convert_temp('0° C')} °F (expected 32)")
print(f"55% → {extract_pct('55%')} (expected 55)")
print(f"0% → {extract_pct('0%')} (expected 0)")
print(f"100% → {extract_pct('100%')} (expected 100)")
