"""Tests for schema_validator module."""

import pytest


def test_schema_change_detected_exception():
    """Test that SchemaChangeDetected exception can be raised and caught."""
    try:
        from schema_validator import SchemaChangeDetected
    except ImportError:
        from src.schema_validator import SchemaChangeDetected

    # Test that we can raise and catch the exception
    with pytest.raises(SchemaChangeDetected):
        raise SchemaChangeDetected("Test schema change")

    # Test with custom message
    with pytest.raises(SchemaChangeDetected, match="Missing required field"):
        raise SchemaChangeDetected("Missing required field: series_id")


def test_expected_schemas_structure():
    """Test that EXPECTED_SCHEMAS has correct structure."""
    try:
        from schema_validator import EXPECTED_SCHEMAS
    except ImportError:
        from src.schema_validator import EXPECTED_SCHEMAS

    # Verify it's a dictionary
    assert isinstance(EXPECTED_SCHEMAS, dict)

    # Verify all required entity types are present
    required_types = [
        "league_series",
        "series_seasons",
        "race_results_table",
        "driver_profile",
        "teams_page",
    ]

    for entity_type in required_types:
        assert entity_type in EXPECTED_SCHEMAS, f"Missing schema for {entity_type}"

    # Verify each schema has required keys
    for entity_type, schema in EXPECTED_SCHEMAS.items():
        assert "description" in schema, f"{entity_type} missing description"
        assert isinstance(schema["description"], str)

        # Most schemas should have url_pattern
        if entity_type != "teams_page":  # teams_page might not have it
            if "url_pattern" in schema:
                assert isinstance(schema["url_pattern"], str)


def test_schema_validator_class_exists():
    """Test that SchemaValidator class can be instantiated."""
    try:
        from schema_validator import SchemaValidator
    except ImportError:
        from src.schema_validator import SchemaValidator

    validator = SchemaValidator()
    assert validator is not None
    assert hasattr(validator, "schemas")
    assert validator.schemas is not None


def test_get_schema_valid_type():
    """Test getting schema for valid entity types."""
    try:
        from schema_validator import SchemaValidator
    except ImportError:
        from src.schema_validator import SchemaValidator

    validator = SchemaValidator()

    # Test each valid entity type
    schema = validator.get_schema("league_series")
    assert schema is not None
    assert "description" in schema

    schema = validator.get_schema("series_seasons")
    assert schema is not None

    schema = validator.get_schema("race_results_table")
    assert schema is not None

    schema = validator.get_schema("driver_profile")
    assert schema is not None

    schema = validator.get_schema("teams_page")
    assert schema is not None


def test_get_schema_invalid_type():
    """Test that get_schema raises ValueError for invalid types."""
    try:
        from schema_validator import SchemaValidator
    except ImportError:
        from src.schema_validator import SchemaValidator

    validator = SchemaValidator()

    with pytest.raises(ValueError, match="Unknown entity_type"):
        validator.get_schema("invalid_type")

    with pytest.raises(ValueError, match="Unknown entity_type"):
        validator.get_schema("nonexistent")


def test_validate_method_exists():
    """Test that validate method exists and has correct signature."""
    try:
        from schema_validator import SchemaValidator
    except ImportError:
        from src.schema_validator import SchemaValidator

    validator = SchemaValidator()

    # Test that method exists
    assert hasattr(validator, "validate")
    assert callable(validator.validate)

    # Test basic call (should return True for now as validation is not implemented)
    result = validator.validate("league_series", "<html>test</html>")
    assert result is True


def test_league_series_schema_structure():
    """Test league_series schema has required patterns."""
    try:
        from schema_validator import EXPECTED_SCHEMAS
    except ImportError:
        from src.schema_validator import EXPECTED_SCHEMAS

    schema = EXPECTED_SCHEMAS["league_series"]

    # Check for JavaScript patterns
    assert "javascript_patterns" in schema
    assert isinstance(schema["javascript_patterns"], list)
    assert len(schema["javascript_patterns"]) > 0

    # Check for required fields
    assert "required_fields" in schema
    assert "id" in schema["required_fields"]
    assert "name" in schema["required_fields"]


def test_series_seasons_schema_structure():
    """Test series_seasons schema has required patterns."""
    try:
        from schema_validator import EXPECTED_SCHEMAS
    except ImportError:
        from src.schema_validator import EXPECTED_SCHEMAS

    schema = EXPECTED_SCHEMAS["series_seasons"]

    # Check for JavaScript patterns (seasons array)
    assert "javascript_patterns" in schema
    patterns = schema["javascript_patterns"]
    assert any("seasons" in p for p in patterns), "Missing 'seasons' pattern"

    # Check for required fields
    assert "required_fields" in schema
    assert "id" in schema["required_fields"]
    assert "n" in schema["required_fields"]  # Season name is abbreviated


def test_race_results_table_schema_structure():
    """Test race_results_table schema has table columns."""
    try:
        from schema_validator import EXPECTED_SCHEMAS
    except ImportError:
        from src.schema_validator import EXPECTED_SCHEMAS

    schema = EXPECTED_SCHEMAS["race_results_table"]

    # Check for table columns
    assert "table_columns" in schema
    assert isinstance(schema["table_columns"], list)

    # Check for required columns
    assert "required_columns" in schema
    assert "Driver" in schema["required_columns"]
    assert "Position" in schema["required_columns"]


def test_driver_profile_schema_structure():
    """Test driver_profile schema has required fields."""
    try:
        from schema_validator import EXPECTED_SCHEMAS
    except ImportError:
        from src.schema_validator import EXPECTED_SCHEMAS

    schema = EXPECTED_SCHEMAS["driver_profile"]

    # Check for JavaScript patterns
    assert "javascript_patterns" in schema
    patterns = schema["javascript_patterns"]
    assert any("driver_id" in p for p in patterns), "Missing 'driver_id' pattern"

    # Check for required fields
    assert "required_fields" in schema
    assert "driver_id" in schema["required_fields"]
    assert "name" in schema["required_fields"]


def test_teams_page_schema_structure():
    """Test teams_page schema has required elements."""
    try:
        from schema_validator import EXPECTED_SCHEMAS
    except ImportError:
        from src.schema_validator import EXPECTED_SCHEMAS

    schema = EXPECTED_SCHEMAS["teams_page"]

    # Check for HTML elements
    assert "html_elements" in schema
    assert isinstance(schema["html_elements"], list)

    # Check for required elements
    assert "required_elements" in schema
    assert "team" in schema["required_elements"]
    assert "driver" in schema["required_elements"]


# ============================================================================
# JavaScript Validation Tests (Task 2.8)
# ============================================================================


def test_validate_javascript_valid_series_data():
    """Test that valid series JavaScript passes validation."""
    try:
        from schema_validator import SchemaValidator
    except ImportError:
        from src.schema_validator import SchemaValidator

    validator = SchemaValidator()

    # Valid league_series page with series.push() calls
    valid_script = """
    <script>
    var series = [];
    series.push({id: 3714, name: "Wednesday Night Series", active: true});
    series.push({id: 3713, name: "Thursday Night Series", active: true});
    </script>
    """

    # Should pass validation
    result = validator.validate_javascript_data("league_series", valid_script)
    assert result is True


def test_validate_javascript_valid_season_data():
    """Test that valid season JavaScript passes validation."""
    try:
        from schema_validator import SchemaValidator
    except ImportError:
        from src.schema_validator import SchemaValidator

    validator = SchemaValidator()

    # Valid series_seasons page with seasons array
    valid_script = """
    <script>
    seasons = [
        {id: 26741, n: "2025 Season 1", scrt: 1754380800, ns: 10, nr: 9},
        {id: 26740, n: "2024 Season 2", scrt: 1754380700, ns: 10, nr: 10}
    ];
    </script>
    """

    # Should pass validation
    result = validator.validate_javascript_data("series_seasons", valid_script)
    assert result is True


def test_validate_javascript_missing_pattern_raises_error():
    """Test that missing required patterns raise SchemaChangeDetected."""
    try:
        from schema_validator import SchemaChangeDetected, SchemaValidator
    except ImportError:
        from src.schema_validator import SchemaChangeDetected, SchemaValidator

    validator = SchemaValidator()

    # Invalid league_series page - missing series.push() pattern
    invalid_script = """
    <script>
    var series = [];
    // No series.push() calls!
    </script>
    """

    # Should raise SchemaChangeDetected
    with pytest.raises(SchemaChangeDetected):
        validator.validate_javascript_data("league_series", invalid_script)


def test_validate_javascript_error_message_descriptive():
    """Test that error messages clearly describe what's missing."""
    try:
        from schema_validator import SchemaChangeDetected, SchemaValidator
    except ImportError:
        from src.schema_validator import SchemaChangeDetected, SchemaValidator

    validator = SchemaValidator()

    # Missing required pattern
    invalid_script = """
    <script>
    var data = [];
    </script>
    """

    # Should raise with descriptive message (singular or plural)
    with pytest.raises(SchemaChangeDetected, match="Missing.*JavaScript pattern"):
        validator.validate_javascript_data("league_series", invalid_script)


def test_validate_javascript_multiple_missing_patterns():
    """Test validation when multiple patterns are missing."""
    try:
        from schema_validator import SchemaChangeDetected, SchemaValidator
    except ImportError:
        from src.schema_validator import SchemaChangeDetected, SchemaValidator

    validator = SchemaValidator()

    # Empty script - missing all patterns
    invalid_script = "<script></script>"

    # Should raise SchemaChangeDetected
    with pytest.raises(SchemaChangeDetected):
        validator.validate_javascript_data("series_seasons", invalid_script)


def test_validate_javascript_partial_patterns():
    """Test that partial matches don't pass - all patterns must exist."""
    try:
        from schema_validator import SchemaChangeDetected, SchemaValidator
    except ImportError:
        from src.schema_validator import SchemaChangeDetected, SchemaValidator

    validator = SchemaValidator()

    # Has 'id' pattern but missing other required patterns
    partial_script = """
    <script>
    seasons = [{id: 123}];  // Missing n, scrt, ns, nr fields
    </script>
    """

    # Should raise because not all patterns are present
    with pytest.raises(SchemaChangeDetected):
        validator.validate_javascript_data("series_seasons", partial_script)


def test_validate_javascript_invalid_entity_type():
    """Test that invalid entity_type raises ValueError."""
    try:
        from schema_validator import SchemaValidator
    except ImportError:
        from src.schema_validator import SchemaValidator

    validator = SchemaValidator()

    # Should raise ValueError for unknown entity type
    with pytest.raises(ValueError, match="Unknown entity_type"):
        validator.validate_javascript_data("invalid_type", "<script></script>")


def test_validate_javascript_driver_profile():
    """Test validation of driver profile JavaScript."""
    try:
        from schema_validator import SchemaValidator
    except ImportError:
        from src.schema_validator import SchemaValidator

    validator = SchemaValidator()

    # Valid driver profile data
    valid_script = """
    <script>
    var driverData = {driver_id: 33132, name: "John Smith"};
    </script>
    """

    result = validator.validate_javascript_data("driver_profile", valid_script)
    assert result is True


def test_validate_javascript_empty_content():
    """Test that empty content raises SchemaChangeDetected."""
    try:
        from schema_validator import SchemaChangeDetected, SchemaValidator
    except ImportError:
        from src.schema_validator import SchemaChangeDetected, SchemaValidator

    validator = SchemaValidator()

    # Empty content should fail
    with pytest.raises(SchemaChangeDetected):
        validator.validate_javascript_data("league_series", "")

    # Whitespace only should also fail
    with pytest.raises(SchemaChangeDetected):
        validator.validate_javascript_data("league_series", "   \n\t  ")


def test_validate_javascript_entity_with_no_patterns():
    """Test validation for entity types with no javascript_patterns."""
    try:
        from schema_validator import SchemaValidator
    except ImportError:
        from src.schema_validator import SchemaValidator

    validator = SchemaValidator()

    # race_results_table has no javascript_patterns (only table_columns)
    # So any content should pass
    result = validator.validate_javascript_data("race_results_table", "<table></table>")
    assert result is True


def test_validate_javascript_single_missing_pattern():
    """Test error message when exactly one pattern is missing."""
    try:
        from schema_validator import SchemaChangeDetected, SchemaValidator
    except ImportError:
        from src.schema_validator import SchemaChangeDetected, SchemaValidator

    validator = SchemaValidator()

    # Has series.push and id patterns, but missing the name pattern
    partial_script = """
    <script>
    series.push({id: 123});  // Missing name field
    </script>
    """

    # Should raise with single-pattern error message
    with pytest.raises(SchemaChangeDetected, match="Missing required JavaScript pattern for"):
        validator.validate_javascript_data("league_series", partial_script)


# ============================================================================
# Data Field Validation Tests (Task 2.9)
# ============================================================================


def test_validate_data_valid_league():
    """Test that valid league data passes validation."""
    try:
        from schema_validator import SchemaValidator
    except ImportError:
        from src.schema_validator import SchemaValidator

    validator = SchemaValidator()

    # Valid league data with all required fields
    valid_data = {
        "id": 1558,
        "name": "The OBRL",
        "description": "Online racing league",
        "url": "https://www.simracerhub.com/league_series.php?league_id=1558",
    }

    # Should pass validation
    result = validator.validate_extracted_data("league_series", valid_data)
    assert result is True


def test_validate_data_valid_series():
    """Test that valid series data passes validation."""
    try:
        from schema_validator import SchemaValidator
    except ImportError:
        from src.schema_validator import SchemaValidator

    validator = SchemaValidator()

    # Valid series season data with all required fields
    valid_data = {
        "id": 26741,
        "n": "2025 Season 1",
        "scrt": 1754380800,
        "ns": 10,
        "nr": 9,
    }

    # Should pass validation
    result = validator.validate_extracted_data("series_seasons", valid_data)
    assert result is True


def test_validate_data_missing_field_raises_error():
    """Test that missing required fields raise SchemaChangeDetected."""
    try:
        from schema_validator import SchemaChangeDetected, SchemaValidator
    except ImportError:
        from src.schema_validator import SchemaChangeDetected, SchemaValidator

    validator = SchemaValidator()

    # Missing 'name' field (required)
    invalid_data = {
        "id": 1558,
        "description": "Online racing league",
    }

    # Should raise SchemaChangeDetected
    with pytest.raises(SchemaChangeDetected):
        validator.validate_extracted_data("league_series", invalid_data)


def test_validate_data_all_entity_types():
    """Test validation works for all entity types with required fields."""
    try:
        from schema_validator import SchemaValidator
    except ImportError:
        from src.schema_validator import SchemaValidator

    validator = SchemaValidator()

    # Test league_series
    league_data = {"id": 1558, "name": "The OBRL"}
    assert validator.validate_extracted_data("league_series", league_data) is True

    # Test series_seasons
    season_data = {"id": 26741, "n": "2025 S1", "scrt": 1754380800, "ns": 10, "nr": 9}
    assert validator.validate_extracted_data("series_seasons", season_data) is True

    # Test driver_profile
    driver_data = {"driver_id": 33132, "name": "John Smith"}
    assert validator.validate_extracted_data("driver_profile", driver_data) is True


def test_validate_data_extra_fields_allowed():
    """Test that extra fields beyond required ones are allowed."""
    try:
        from schema_validator import SchemaValidator
    except ImportError:
        from src.schema_validator import SchemaValidator

    validator = SchemaValidator()

    # Valid data with extra fields
    data_with_extras = {
        "id": 1558,
        "name": "The OBRL",
        "extra_field_1": "some value",
        "extra_field_2": 12345,
        "nested": {"data": "allowed"},
    }

    # Should pass - extra fields are OK
    result = validator.validate_extracted_data("league_series", data_with_extras)
    assert result is True


def test_validate_data_error_message_descriptive():
    """Test that error messages clearly describe which field is missing."""
    try:
        from schema_validator import SchemaChangeDetected, SchemaValidator
    except ImportError:
        from src.schema_validator import SchemaChangeDetected, SchemaValidator

    validator = SchemaValidator()

    # Missing multiple required fields
    invalid_data = {"id": 26741}  # Missing n, scrt, ns, nr

    # Should raise with descriptive message
    with pytest.raises(SchemaChangeDetected, match="Missing.*required field"):
        validator.validate_extracted_data("series_seasons", invalid_data)


def test_validate_data_single_missing_field():
    """Test error message when exactly one field is missing."""
    try:
        from schema_validator import SchemaChangeDetected, SchemaValidator
    except ImportError:
        from src.schema_validator import SchemaChangeDetected, SchemaValidator

    validator = SchemaValidator()

    # Missing only 'name' field
    invalid_data = {"id": 1558}

    # Should raise with single-field error message
    with pytest.raises(SchemaChangeDetected, match="Missing required field"):
        validator.validate_extracted_data("league_series", invalid_data)


def test_validate_data_multiple_missing_fields():
    """Test validation when multiple fields are missing."""
    try:
        from schema_validator import SchemaChangeDetected, SchemaValidator
    except ImportError:
        from src.schema_validator import SchemaChangeDetected, SchemaValidator

    validator = SchemaValidator()

    # Empty data - missing all required fields
    invalid_data = {}

    # Should raise SchemaChangeDetected
    with pytest.raises(SchemaChangeDetected):
        validator.validate_extracted_data("league_series", invalid_data)


def test_validate_data_invalid_entity_type():
    """Test that invalid entity_type raises ValueError."""
    try:
        from schema_validator import SchemaValidator
    except ImportError:
        from src.schema_validator import SchemaValidator

    validator = SchemaValidator()

    # Should raise ValueError for unknown entity type
    with pytest.raises(ValueError, match="Unknown entity_type"):
        validator.validate_extracted_data("invalid_type", {"id": 123})


def test_validate_data_empty_data_dict():
    """Test that empty data dict raises SchemaChangeDetected."""
    try:
        from schema_validator import SchemaChangeDetected, SchemaValidator
    except ImportError:
        from src.schema_validator import SchemaChangeDetected, SchemaValidator

    validator = SchemaValidator()

    # Empty dict should fail
    with pytest.raises(SchemaChangeDetected):
        validator.validate_extracted_data("league_series", {})


def test_validate_data_entity_with_no_required_fields():
    """Test validation for entity types with no required_fields."""
    try:
        from schema_validator import SchemaValidator
    except ImportError:
        from src.schema_validator import SchemaValidator

    validator = SchemaValidator()

    # teams_page has no required_fields (only required_elements)
    # So any data should pass
    result = validator.validate_extracted_data("teams_page", {"any": "data"})
    assert result is True

    # Even empty dict should pass if no required_fields
    result = validator.validate_extracted_data("teams_page", {})
    assert result is True


def test_validate_data_none_value_in_field():
    """Test that None values are treated as missing fields."""
    try:
        from schema_validator import SchemaChangeDetected, SchemaValidator
    except ImportError:
        from src.schema_validator import SchemaChangeDetected, SchemaValidator

    validator = SchemaValidator()

    # Field exists but is None
    invalid_data = {"id": 1558, "name": None}

    # Should raise because None is not a valid value
    with pytest.raises(SchemaChangeDetected):
        validator.validate_extracted_data("league_series", invalid_data)


# ============================================================================
# Table Structure Validation Tests (Task 2.10)
# ============================================================================


def test_validate_table_valid_race_results():
    """Test that valid race results table passes validation."""
    try:
        from schema_validator import SchemaValidator
    except ImportError:
        from src.schema_validator import SchemaValidator

    from bs4 import BeautifulSoup

    validator = SchemaValidator()

    # Valid race results table with all required columns
    valid_table_html = """
    <table>
        <thead>
            <tr>
                <th>Position</th>
                <th>Driver</th>
                <th>Car</th>
                <th>Laps</th>
                <th>Points</th>
            </tr>
        </thead>
        <tbody>
            <tr>
                <td>1</td>
                <td>John Smith</td>
                <td>12</td>
                <td>50</td>
                <td>25</td>
            </tr>
        </tbody>
    </table>
    """

    soup = BeautifulSoup(valid_table_html, "html.parser")
    table = soup.find("table")

    # Should pass validation
    result = validator.validate_table_structure("race_results_table", table)
    assert result is True


def test_validate_table_missing_column_raises_error():
    """Test that missing required columns raise SchemaChangeDetected."""
    try:
        from schema_validator import SchemaChangeDetected, SchemaValidator
    except ImportError:
        from src.schema_validator import SchemaChangeDetected, SchemaValidator

    from bs4 import BeautifulSoup

    validator = SchemaValidator()

    # Missing 'Driver' column (required)
    invalid_table_html = """
    <table>
        <thead>
            <tr>
                <th>Position</th>
                <th>Car</th>
                <th>Laps</th>
            </tr>
        </thead>
    </table>
    """

    soup = BeautifulSoup(invalid_table_html, "html.parser")
    table = soup.find("table")

    # Should raise SchemaChangeDetected
    with pytest.raises(SchemaChangeDetected):
        validator.validate_table_structure("race_results_table", table)


def test_validate_table_insufficient_columns_raises_error():
    """Test that insufficient total columns raise SchemaChangeDetected."""
    try:
        from schema_validator import SchemaChangeDetected, SchemaValidator
    except ImportError:
        from src.schema_validator import SchemaChangeDetected, SchemaValidator

    from bs4 import BeautifulSoup

    validator = SchemaValidator()

    # Only 1 column (minimum 2 required for race_results_table)
    insufficient_table_html = """
    <table>
        <thead>
            <tr>
                <th>Position</th>
            </tr>
        </thead>
    </table>
    """

    soup = BeautifulSoup(insufficient_table_html, "html.parser")
    table = soup.find("table")

    # Should raise SchemaChangeDetected
    with pytest.raises(SchemaChangeDetected):
        validator.validate_table_structure("race_results_table", table)


def test_validate_table_extra_columns_allowed():
    """Test that extra columns beyond required ones are allowed."""
    try:
        from schema_validator import SchemaValidator
    except ImportError:
        from src.schema_validator import SchemaValidator

    from bs4 import BeautifulSoup

    validator = SchemaValidator()

    # Valid table with extra columns
    table_with_extras = """
    <table>
        <thead>
            <tr>
                <th>Position</th>
                <th>Driver</th>
                <th>Car</th>
                <th>Laps</th>
                <th>Points</th>
                <th>Extra Column 1</th>
                <th>Extra Column 2</th>
            </tr>
        </thead>
    </table>
    """

    soup = BeautifulSoup(table_with_extras, "html.parser")
    table = soup.find("table")

    # Should pass - extra columns are OK
    result = validator.validate_table_structure("race_results_table", table)
    assert result is True


def test_validate_table_case_insensitive_column_matching():
    """Test that column matching is case-insensitive."""
    try:
        from schema_validator import SchemaValidator
    except ImportError:
        from src.schema_validator import SchemaValidator

    from bs4 import BeautifulSoup

    validator = SchemaValidator()

    # Columns in different case
    mixed_case_table = """
    <table>
        <thead>
            <tr>
                <th>POSITION</th>
                <th>driver</th>
                <th>Car</th>
            </tr>
        </thead>
    </table>
    """

    soup = BeautifulSoup(mixed_case_table, "html.parser")
    table = soup.find("table")

    # Should pass - case insensitive matching
    result = validator.validate_table_structure("race_results_table", table)
    assert result is True


def test_validate_table_error_message_descriptive():
    """Test that error messages clearly describe which column is missing."""
    try:
        from schema_validator import SchemaChangeDetected, SchemaValidator
    except ImportError:
        from src.schema_validator import SchemaChangeDetected, SchemaValidator

    from bs4 import BeautifulSoup

    validator = SchemaValidator()

    # Missing both required columns
    invalid_table = """
    <table>
        <thead>
            <tr>
                <th>Car</th>
                <th>Laps</th>
            </tr>
        </thead>
    </table>
    """

    soup = BeautifulSoup(invalid_table, "html.parser")
    table = soup.find("table")

    # Should raise with descriptive message
    with pytest.raises(SchemaChangeDetected, match="Missing.*required column"):
        validator.validate_table_structure("race_results_table", table)


def test_validate_table_no_thead():
    """Test validation when table has no thead (columns in first tr)."""
    try:
        from schema_validator import SchemaValidator
    except ImportError:
        from src.schema_validator import SchemaValidator

    from bs4 import BeautifulSoup

    validator = SchemaValidator()

    # Table without thead, columns in first row
    table_no_thead = """
    <table>
        <tr>
            <th>Position</th>
            <th>Driver</th>
            <th>Car</th>
        </tr>
        <tr>
            <td>1</td>
            <td>John</td>
            <td>12</td>
        </tr>
    </table>
    """

    soup = BeautifulSoup(table_no_thead, "html.parser")
    table = soup.find("table")

    # Should pass - finds columns in first tr
    result = validator.validate_table_structure("race_results_table", table)
    assert result is True


def test_validate_table_empty_table():
    """Test that empty table raises SchemaChangeDetected."""
    try:
        from schema_validator import SchemaChangeDetected, SchemaValidator
    except ImportError:
        from src.schema_validator import SchemaChangeDetected, SchemaValidator

    from bs4 import BeautifulSoup

    validator = SchemaValidator()

    # Empty table
    empty_table = "<table></table>"

    soup = BeautifulSoup(empty_table, "html.parser")
    table = soup.find("table")

    # Should raise
    with pytest.raises(SchemaChangeDetected):
        validator.validate_table_structure("race_results_table", table)


def test_validate_table_none_table():
    """Test that None table raises SchemaChangeDetected."""
    try:
        from schema_validator import SchemaChangeDetected, SchemaValidator
    except ImportError:
        from src.schema_validator import SchemaChangeDetected, SchemaValidator

    validator = SchemaValidator()

    # Should raise for None table
    with pytest.raises(SchemaChangeDetected):
        validator.validate_table_structure("race_results_table", None)


def test_validate_table_invalid_entity_type():
    """Test that invalid entity_type raises ValueError."""
    try:
        from schema_validator import SchemaValidator
    except ImportError:
        from src.schema_validator import SchemaValidator

    from bs4 import BeautifulSoup

    validator = SchemaValidator()

    # Should raise ValueError for unknown entity type
    soup = BeautifulSoup("<table></table>", "html.parser")
    table = soup.find("table")

    with pytest.raises(ValueError, match="Unknown entity_type"):
        validator.validate_table_structure("invalid_type", table)


def test_validate_table_entity_with_no_table_columns():
    """Test validation for entity types with no table_columns defined."""
    try:
        from schema_validator import SchemaValidator
    except ImportError:
        from src.schema_validator import SchemaValidator

    from bs4 import BeautifulSoup

    validator = SchemaValidator()

    # league_series has no table_columns (only javascript_patterns)
    # So any table should pass
    soup = BeautifulSoup("<table></table>", "html.parser")
    table = soup.find("table")

    result = validator.validate_table_structure("league_series", table)
    assert result is True


def test_validate_table_no_th_elements():
    """Test that table with tr but no th elements raises SchemaChangeDetected."""
    try:
        from schema_validator import SchemaChangeDetected, SchemaValidator
    except ImportError:
        from src.schema_validator import SchemaChangeDetected, SchemaValidator

    from bs4 import BeautifulSoup

    validator = SchemaValidator()

    # Table with tr but only td elements, no th
    table_no_th = """
    <table>
        <tr>
            <td>Position</td>
            <td>Driver</td>
        </tr>
    </table>
    """

    soup = BeautifulSoup(table_no_th, "html.parser")
    table = soup.find("table")

    # Should raise because no <th> elements found
    with pytest.raises(SchemaChangeDetected, match="No column headers"):
        validator.validate_table_structure("race_results_table", table)
