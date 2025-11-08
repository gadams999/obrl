"""Tests for JavaScript parser utilities."""


def test_extract_series_data_valid():
    """Test extracting series data from valid JavaScript."""
    try:
        from utils.js_parser import extract_series_data
    except ImportError:
        from src.utils.js_parser import extract_series_data

    # Valid HTML with series.push() calls
    html = """
    <script>
    var series = [];
    series.push({id: 3714, name: "Wednesday Night Series", description: "Wed racing", season_count: 28});
    series.push({id: 3713, name: "Thursday Night Series", description: "Thu racing", season_count: 15});
    </script>
    """

    result = extract_series_data(html)

    # Should extract both series
    assert len(result) == 2
    assert result[0]["id"] == 3714
    assert result[0]["name"] == "Wednesday Night Series"
    assert result[0]["description"] == "Wed racing"
    assert result[0]["season_count"] == 28
    assert result[1]["id"] == 3713
    assert result[1]["name"] == "Thursday Night Series"


def test_extract_series_data_empty():
    """Test that empty/missing JavaScript returns empty list."""
    try:
        from utils.js_parser import extract_series_data
    except ImportError:
        from src.utils.js_parser import extract_series_data

    # No JavaScript
    html = "<html><body>No JavaScript here</body></html>"

    result = extract_series_data(html)

    assert result == []


def test_extract_series_data_partial_fields():
    """Test extracting series with only required fields."""
    try:
        from utils.js_parser import extract_series_data
    except ImportError:
        from src.utils.js_parser import extract_series_data

    # Minimal series data (only id and name)
    html = """
    <script>
    series.push({id: 100, name: "Minimal Series"});
    </script>
    """

    result = extract_series_data(html)

    assert len(result) == 1
    assert result[0]["id"] == 100
    assert result[0]["name"] == "Minimal Series"
    assert "description" not in result[0]  # Optional field not present


def test_extract_season_data_valid():
    """Test extracting season data from valid JavaScript."""
    try:
        from utils.js_parser import extract_season_data
    except ImportError:
        from src.utils.js_parser import extract_season_data

    # Valid HTML with seasons array
    html = """
    <script>
    seasons = [
        {id: 26741, n: "2025 Season 1", scrt: 1754380800, ns: 10, nr: 9},
        {id: 26740, n: "2024 Season 2", scrt: 1722844800, ns: 12, nr: 12}
    ];
    </script>
    """

    result = extract_season_data(html)

    # Should extract both seasons
    assert len(result) == 2
    assert result[0]["id"] == 26741
    assert result[0]["n"] == "2025 Season 1"
    assert result[0]["scrt"] == 1754380800
    assert result[0]["ns"] == 10
    assert result[0]["nr"] == 9
    assert result[1]["id"] == 26740


def test_extract_season_data_empty():
    """Test that empty/missing JavaScript returns empty list."""
    try:
        from utils.js_parser import extract_season_data
    except ImportError:
        from src.utils.js_parser import extract_season_data

    # No JavaScript
    html = "<html><body>No seasons here</body></html>"

    result = extract_season_data(html)

    assert result == []


def test_extract_season_data_empty_array():
    """Test extracting from empty seasons array."""
    try:
        from utils.js_parser import extract_season_data
    except ImportError:
        from src.utils.js_parser import extract_season_data

    # Empty seasons array
    html = """
    <script>
    seasons = [];
    </script>
    """

    result = extract_season_data(html)

    assert result == []


def test_extract_js_array_custom_pattern():
    """Test extracting JavaScript array with custom pattern."""
    try:
        from utils.js_parser import extract_js_array
    except ImportError:
        from src.utils.js_parser import extract_js_array

    # Custom JavaScript array
    html = """
    <script>
    myData = [
        {foo: 123, bar: "test"},
        {foo: 456, bar: "data"}
    ];
    </script>
    """

    result = extract_js_array(html, "myData")

    assert len(result) == 2
    assert result[0]["foo"] == 123
    assert result[0]["bar"] == "test"


def test_extract_js_array_malformed_json():
    """Test that malformed JSON raises helpful error."""
    try:
        from utils.js_parser import extract_js_array
    except ImportError:
        from src.utils.js_parser import extract_js_array

    # Malformed JSON (missing quotes, trailing commas, etc.)
    html = """
    <script>
    data = [
        {invalid: broken,}
    ];
    </script>
    """

    # Should handle malformed JSON gracefully
    result = extract_js_array(html, "data")
    # Either returns empty list or raises specific exception
    assert isinstance(result, list)


def test_extract_series_data_special_characters():
    """Test extracting series with special characters in names."""
    try:
        from utils.js_parser import extract_series_data
    except ImportError:
        from src.utils.js_parser import extract_series_data

    # Series name with apostrophes, quotes, etc.
    html = """
    <script>
    series.push({id: 100, name: "The OBRL '87 Legends - Season 1", description: "It's racing!"});
    </script>
    """

    result = extract_series_data(html)

    assert len(result) == 1
    assert "OBRL '87" in result[0]["name"]
    assert result[0]["description"] == "It's racing!"


def test_extract_series_data_no_id():
    """Test that series without id are skipped."""
    try:
        from utils.js_parser import extract_series_data
    except ImportError:
        from src.utils.js_parser import extract_series_data

    # Series missing required 'id' field
    html = """
    <script>
    series.push({name: "No ID Series"});
    series.push({id: 200, name: "Valid Series"});
    </script>
    """

    result = extract_series_data(html)

    # Should only extract the valid one
    assert len(result) == 1
    assert result[0]["id"] == 200


def test_extract_season_data_multiline():
    """Test extracting seasons from multiline JavaScript."""
    try:
        from utils.js_parser import extract_season_data
    except ImportError:
        from src.utils.js_parser import extract_season_data

    # Multiline formatting
    html = """
    <script>
    seasons = [
        {
            id: 100,
            n: "Season Name",
            scrt: 1234567890,
            ns: 10,
            nr: 5
        },
        {
            id: 200,
            n: "Another Season",
            scrt: 9876543210,
            ns: 8,
            nr: 8
        }
    ];
    </script>
    """

    result = extract_season_data(html)

    assert len(result) == 2
    assert result[0]["id"] == 100
    assert result[1]["id"] == 200


def test_extract_js_array_not_found():
    """Test that missing array variable returns empty list."""
    try:
        from utils.js_parser import extract_js_array
    except ImportError:
        from src.utils.js_parser import extract_js_array

    html = "<script>var other = [];</script>"

    result = extract_js_array(html, "notFound")

    assert result == []


def test_extract_series_data_numeric_fields():
    """Test that numeric fields are parsed as integers."""
    try:
        from utils.js_parser import extract_series_data
    except ImportError:
        from src.utils.js_parser import extract_series_data

    html = """
    <script>
    series.push({id: 999, name: "Test", season_count: 42});
    </script>
    """

    result = extract_series_data(html)

    assert isinstance(result[0]["id"], int)
    assert isinstance(result[0]["season_count"], int)
    assert result[0]["season_count"] == 42


def test_parse_js_object_with_single_quotes():
    """Test parsing JavaScript object with single-quoted strings (regex fallback)."""
    try:
        from utils.js_parser import _parse_js_object
    except ImportError:
        from src.utils.js_parser import _parse_js_object

    # Malformed JavaScript that forces regex fallback
    # Trailing comma makes JSON parsing fail
    js = "id: 100, name: 'Single Quote', active: true, data: null,"

    result = _parse_js_object(js)

    assert result["id"] == 100
    assert result["name"] == "Single Quote"
    assert result["active"] is True
    assert result["data"] is None


def test_parse_js_object_with_booleans():
    """Test parsing JavaScript object with boolean values (regex fallback)."""
    try:
        from utils.js_parser import _parse_js_object
    except ImportError:
        from src.utils.js_parser import _parse_js_object

    # JavaScript with boolean values and trailing comma (forces regex fallback)
    js = "active: true, archived: false,"

    result = _parse_js_object(js)

    assert result["active"] is True
    assert result["archived"] is False


def test_parse_js_object_with_null():
    """Test parsing JavaScript object with null value (regex fallback)."""
    try:
        from utils.js_parser import _parse_js_object
    except ImportError:
        from src.utils.js_parser import _parse_js_object

    # JavaScript with null value and trailing comma (forces regex fallback)
    js = "id: 100, description: null,"

    result = _parse_js_object(js)

    assert result["id"] == 100
    assert result["description"] is None


def test_extract_react_props_array():
    """Test extracting array prop from ReactDOM."""
    try:
        from utils.js_parser import extract_react_props
    except ImportError:
        from src.utils.js_parser import extract_react_props

    html = """
    <script>
    ReactDOM.createRoot(document.getElementById('root')).render(
        React.createElement(Table, {
            rps: [{"id": 1, "name": "Driver 1"}, {"id": 2, "name": "Driver 2"}],
            other: "value"
        })
    );
    </script>
    """

    result = extract_react_props(html, "rps")

    assert result is not None
    assert isinstance(result, list)
    assert len(result) == 2
    assert result[0]["id"] == 1
    assert result[0]["name"] == "Driver 1"
    assert result[1]["id"] == 2


def test_extract_react_props_object():
    """Test extracting object prop from ReactDOM."""
    try:
        from utils.js_parser import extract_react_props
    except ImportError:
        from src.utils.js_parser import extract_react_props

    html = """
    <script>
    ReactDOM.render(
        React.createElement(Table, {
            drivers: {"123": {"name": "Test Driver", "irating": 2000}, "456": {"name": "Another"}},
            other: []
        })
    );
    </script>
    """

    result = extract_react_props(html, "drivers")

    assert result is not None
    assert isinstance(result, dict)
    assert "123" in result
    assert result["123"]["name"] == "Test Driver"
    assert result["123"]["irating"] == 2000
    assert "456" in result


def test_extract_react_props_not_found():
    """Test handling of missing prop."""
    try:
        from utils.js_parser import extract_react_props
    except ImportError:
        from src.utils.js_parser import extract_react_props

    html = """
    <script>
    ReactDOM.render(React.createElement(Table, {other: []}));
    </script>
    """

    result = extract_react_props(html, "rps")

    assert result is None


def test_extract_react_props_nested_objects():
    """Test extracting nested object structures."""
    try:
        from utils.js_parser import extract_react_props
    except ImportError:
        from src.utils.js_parser import extract_react_props

    html = """
    <script>
    ReactDOM.createRoot(...).render(
        React.createElement(ResultsTable, {
            teams: {
                "100": {"name": "Team Alpha", "tpts": 103, "members": ["1", "2"]},
                "200": {"name": "Team Beta", "tpts": 95}
            }
        })
    );
    </script>
    """

    result = extract_react_props(html, "teams")

    assert result is not None
    assert isinstance(result, dict)
    assert result["100"]["name"] == "Team Alpha"
    assert result["100"]["tpts"] == 103
    assert isinstance(result["100"]["members"], list)


def test_extract_race_results_json():
    """Test extraction of all race result props."""
    try:
        from utils.js_parser import extract_race_results_json
    except ImportError:
        from src.utils.js_parser import extract_race_results_json

    html = """
    <script>
    ReactDOM.createRoot(document.getElementById('driver_table_12345')).render(
        React.createElement(ResultsTable, {
            rps: [{"driver_id": "1", "finish_pos": "1"}],
            drivers: {"1": {"name": "Driver One"}},
            teams: {"1": {"name": "Team One"}},
            schedule: {"schedule_id": "12345"}
        })
    );
    </script>
    """

    result = extract_race_results_json(html)

    assert result["rps"] is not None
    assert isinstance(result["rps"], list)
    assert len(result["rps"]) == 1
    assert result["drivers"] is not None
    assert isinstance(result["drivers"], dict)
    assert result["teams"] is not None
    assert result["schedule"] is not None


def test_extract_race_results_json_missing_props():
    """Test handling when some props are missing."""
    try:
        from utils.js_parser import extract_race_results_json
    except ImportError:
        from src.utils.js_parser import extract_race_results_json

    html = """
    <script>
    ReactDOM.render(
        React.createElement(ResultsTable, {
            rps: [{"driver_id": "1"}]
        })
    );
    </script>
    """

    result = extract_race_results_json(html)

    assert result["rps"] is not None
    assert result["drivers"] is None
    assert result["teams"] is None
    assert result["schedule"] is None
