"""JavaScript parser utilities for extracting data from SimRacerHub pages.

SimRacerHub embeds data in JavaScript arrays and objects. This module provides
utilities to extract and parse that data into Python dictionaries.
"""

import json
import re
from typing import Any


def extract_series_data(html: str) -> list[dict[str, Any]]:
    """Extract series data from JavaScript series.push() calls.

    SimRacerHub league pages contain JavaScript like:
        series.push({id: 3714, name: "Wednesday Night", ...});

    Args:
        html: HTML content containing JavaScript

    Returns:
        List of series dictionaries with extracted fields

    Examples:
        >>> html = 'series.push({id: 100, name: "Test"});'
        >>> extract_series_data(html)
        [{'id': 100, 'name': 'Test'}]
    """
    series_list = []

    # Find all series.push() calls
    # Pattern: series.push({...});
    pattern = r"series\.push\(\{([^}]+)\}\)"

    matches = re.finditer(pattern, html, re.DOTALL)

    for match in matches:
        series_js = match.group(1)

        # Parse the JavaScript object into a dictionary
        series_data = _parse_js_object(series_js)

        # Only include series with required fields (id and name)
        if "id" in series_data and "name" in series_data:
            series_list.append(series_data)

    return series_list


def extract_season_data(html: str) -> list[dict[str, Any]]:
    """Extract season data from JavaScript seasons array.

    SimRacerHub series pages contain JavaScript like:
        seasons = [{id: 26741, n: "2025 S1", scrt: 1754380800, ...}, ...];

    Args:
        html: HTML content containing JavaScript

    Returns:
        List of season dictionaries with extracted fields

    Examples:
        >>> html = 'seasons = [{id: 100, n: "Season 1", scrt: 123, ns: 10, nr: 5}];'
        >>> extract_season_data(html)
        [{'id': 100, 'n': 'Season 1', 'scrt': 123, 'ns': 10, 'nr': 5}]
    """
    return extract_js_array(html, "seasons")


def extract_js_array(html: str, var_name: str) -> list[dict[str, Any]]:
    """Extract JavaScript array assignment into Python list.

    Finds patterns like: varName = [{...}, {...}];

    Args:
        html: HTML content containing JavaScript
        var_name: JavaScript variable name to extract

    Returns:
        List of dictionaries from the JavaScript array

    Examples:
        >>> html = 'myData = [{id: 1}, {id: 2}];'
        >>> extract_js_array(html, 'myData')
        [{'id': 1}, {'id': 2}]
    """
    # Pattern: varName = [...];
    # This handles multiline arrays with objects
    pattern = rf"{re.escape(var_name)}\s*=\s*\[(.*?)\];"

    match = re.search(pattern, html, re.DOTALL)

    if not match:
        return []

    array_content = match.group(1).strip()

    if not array_content:
        return []

    # Split by object boundaries: },{
    # This is a simple approach that works for well-formatted JavaScript
    result = []

    # Find all {...} objects in the array
    object_pattern = r"\{([^{}]*(?:\{[^{}]*\}[^{}]*)*)\}"

    for obj_match in re.finditer(object_pattern, array_content):
        obj_content = obj_match.group(1)
        parsed = _parse_js_object(obj_content)
        if parsed:  # Only add non-empty objects
            result.append(parsed)

    return result


def _parse_js_object(js_content: str) -> dict[str, Any]:
    """Parse JavaScript object content into Python dictionary.

    Handles common JavaScript object notation patterns:
    - key: value (with or without quotes)
    - Numeric values
    - String values in single or double quotes

    Args:
        js_content: Content inside JavaScript object braces

    Returns:
        Dictionary with parsed key-value pairs

    Examples:
        >>> _parse_js_object('id: 100, name: "Test"')
        {'id': 100, 'name': 'Test'}
    """
    data = {}

    # Try JSON parsing first after converting to valid JSON
    # This handles most cases if we can convert JavaScript object notation to JSON
    json_str = _js_to_json(js_content)

    try:
        return json.loads(json_str)
    except json.JSONDecodeError:
        # Fall back to regex parsing for non-standard JavaScript
        pass

    # Regex-based parsing as fallback
    # Pattern for key-value pairs
    # Matches: key: value where value can be number, string, or boolean
    pair_pattern = r'(\w+)\s*:\s*(?:(\d+)|"([^"]*)"|\'([^\']*)\'|([a-zA-Z_]\w*))'

    for match in re.finditer(pair_pattern, js_content):
        key = match.group(1)
        # Try each capture group for the value
        num_val = match.group(2)
        dquote_val = match.group(3)
        squote_val = match.group(4)
        ident_val = match.group(5)

        if num_val:
            data[key] = int(num_val)
        elif dquote_val is not None:
            data[key] = dquote_val
        elif squote_val is not None:
            data[key] = squote_val
        elif ident_val:
            # Boolean or identifier
            if ident_val == "true":
                data[key] = True
            elif ident_val == "false":
                data[key] = False
            elif ident_val == "null":
                data[key] = None
            else:
                data[key] = ident_val

    return data


def _js_to_json(js_content: str) -> str:
    """Convert JavaScript object notation to valid JSON.

    Adds quotes to unquoted keys and handles JavaScript-specific syntax.

    Args:
        js_content: JavaScript object content

    Returns:
        JSON-compatible string

    Examples:
        >>> _js_to_json('id: 100, name: "test"')
        '{"id": 100, "name": "test"}'
    """
    # Add quotes to unquoted keys
    # Pattern: word characters followed by colon
    result = re.sub(r"(\w+)\s*:", r'"\1":', js_content)

    # Replace single quotes with double quotes
    result = result.replace("'", '"')

    # Wrap in braces if not already
    result = result.strip()
    if not result.startswith("{"):
        result = "{" + result + "}"

    return result
