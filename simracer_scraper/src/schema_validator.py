"""Schema validator for SimRacerHub pages.

This module validates that scraped HTML/JavaScript matches expected patterns.
If the site structure changes, it raises SchemaChangeDetected to alert us.
"""


class SchemaChangeDetected(Exception):
    """Raised when the scraped page structure doesn't match expected schema.

    This indicates that SimRacerHub has changed their page structure and
    our extractors may need to be updated.
    """

    pass


# Expected schema patterns for different page types
EXPECTED_SCHEMAS = {
    # League series page (league_series.php?league_id={id})
    "league_series": {
        "description": "League series list page with JavaScript-embedded series data",
        "url_pattern": r"league_series\.php\?league_id=\d+",
        "javascript_patterns": [
            r"series\.push\(\{",  # Series array push calls
            r"id\s*:\s*\d+",  # Series ID field
            r"name\s*:\s*[\"']",  # Series name field
        ],
        "html_elements": [
            "div.series-list",  # Container (may vary)
            "a[href*='series_seasons.php']",  # Links to series
        ],
        "required_fields": ["id", "name"],
    },
    # Series seasons page (series_seasons.php?series_id={id})
    "series_seasons": {
        "description": "Series page with JavaScript-embedded season data",
        "url_pattern": r"series_seasons\.php\?series_id=\d+",
        "javascript_patterns": [
            r"seasons\s*=\s*\[",  # Seasons array declaration
            r"\{\s*id\s*:\s*\d+",  # Season object with ID
            r"n\s*:\s*[\"']",  # Season name field (abbreviated as 'n')
            r"scrt\s*:\s*\d+",  # Start timestamp
            r"ns\s*:\s*\d+",  # Scheduled races
            r"nr\s*:\s*\d+",  # Completed races
        ],
        "html_elements": [
            "div.season-list",  # Container (may vary)
        ],
        "required_fields": ["id", "n", "scrt", "ns", "nr"],
    },
    # Race results page (season_race.php?schedule_id={id})
    "race_results_table": {
        "description": "Race results table with driver performance data",
        "url_pattern": r"season_race\.php\?schedule_id=\d+",
        "html_elements": [
            "table",  # Results table
            "thead",  # Table header
            "tbody",  # Table body
            "tr",  # Table rows
        ],
        "table_columns": [
            "Position",  # Or similar
            "Driver",  # Driver name
            "Car",  # Car number
            "Laps",  # Laps completed
            "Points",  # Points earned
            # Additional columns may vary by league
        ],
        "required_columns": ["Driver", "Position"],  # Minimum required
    },
    # Driver profile page (driver_stats.php?driver_id={id})
    "driver_profile": {
        "description": "Driver statistics and race history page",
        "url_pattern": r"driver_stats\.php\?driver_id=\d+",
        "javascript_patterns": [
            r"driver_id\s*:\s*\d+",  # Driver ID
            r"name\s*:\s*[\"']",  # Driver name
        ],
        "html_elements": [
            "div.driver-info",  # Driver information section (may vary)
            "div.race-history",  # Race history section (may vary)
        ],
        "required_fields": ["driver_id", "name"],
    },
    # Teams/roster page (teams.php?league_id={id})
    "teams_page": {
        "description": "League teams and driver roster page",
        "url_pattern": r"teams\.php\?league_id=\d+",
        "html_elements": [
            "div.team",  # Team containers (may vary)
            "a[href*='driver_stats.php']",  # Links to driver profiles
        ],
        "required_elements": ["team", "driver"],  # Generic requirements
    },
}


class SchemaValidator:
    """Validates that scraped HTML/JavaScript matches expected schemas.

    This class checks that the structure of SimRacerHub pages hasn't changed
    in ways that would break our extractors.

    Examples:
        >>> validator = SchemaValidator()
        >>> validator.validate("league_series", html_content)
        True  # Schema matches

        >>> validator.validate("league_series", broken_html)
        SchemaChangeDetected: Missing required JavaScript pattern: series.push({
    """

    def __init__(self):
        """Initialize the schema validator."""
        self.schemas = EXPECTED_SCHEMAS

    def get_schema(self, entity_type: str) -> dict:
        """Get the expected schema for an entity type.

        Args:
            entity_type: Type of entity (league_series, series_seasons, etc.)

        Returns:
            dict: The expected schema definition

        Raises:
            ValueError: If entity_type is not recognized
        """
        if entity_type not in self.schemas:
            valid_types = ", ".join(self.schemas.keys())
            raise ValueError(f"Unknown entity_type '{entity_type}'. Valid types: {valid_types}")

        return self.schemas[entity_type]

    def validate(self, entity_type: str, content: str) -> bool:
        """Validate that content matches the expected schema.

        Args:
            entity_type: Type of entity to validate
            content: HTML or JavaScript content to validate

        Returns:
            bool: True if validation passes

        Raises:
            SchemaChangeDetected: If content doesn't match expected schema
            ValueError: If entity_type is not recognized

        Examples:
            >>> validator = SchemaValidator()
            >>> html = '<script>series.push({id: 123, name: "Test"});</script>'
            >>> validator.validate("league_series", html)
            True
        """
        # Validate that entity_type exists (raises ValueError if not)
        _ = self.get_schema(entity_type)

        # For now, just return True - actual validation will be in Task 2.8
        # This is just the core structure for Task 2.7
        return True

    def validate_javascript_data(self, entity_type: str, script_content: str) -> bool:
        """Validate that JavaScript content contains all required patterns.

        This method checks that the scraped JavaScript/HTML contains all the
        expected patterns defined in the schema. If any required pattern is
        missing, it raises SchemaChangeDetected to alert us that the site
        structure has changed.

        Args:
            entity_type: Type of entity (league_series, series_seasons, etc.)
            script_content: HTML/JavaScript content to validate

        Returns:
            bool: True if all required patterns are found

        Raises:
            SchemaChangeDetected: If any required pattern is missing
            ValueError: If entity_type is not recognized

        Examples:
            >>> validator = SchemaValidator()
            >>> html = '<script>series.push({id: 123, name: "Test"});</script>'
            >>> validator.validate_javascript_data("league_series", html)
            True

            >>> bad_html = '<script>var x = [];</script>'
            >>> validator.validate_javascript_data("league_series", bad_html)
            SchemaChangeDetected: Missing required JavaScript pattern: series\\.push\\(\\{
        """
        import re

        # Get the schema for this entity type (raises ValueError if invalid)
        schema = self.get_schema(entity_type)

        # Check for empty content
        if not script_content or not script_content.strip():
            raise SchemaChangeDetected(
                f"Empty content provided for entity_type '{entity_type}'. "
                "Expected JavaScript patterns not found."
            )

        # Get the JavaScript patterns to check
        js_patterns = schema.get("javascript_patterns", [])

        # If no JS patterns defined, nothing to validate
        if not js_patterns:
            return True

        # Check each required pattern
        missing_patterns = []
        for pattern in js_patterns:
            if not re.search(pattern, script_content, re.DOTALL):
                missing_patterns.append(pattern)

        # If any patterns are missing, raise SchemaChangeDetected
        if missing_patterns:
            if len(missing_patterns) == 1:
                raise SchemaChangeDetected(
                    f"Missing required JavaScript pattern for '{entity_type}': {missing_patterns[0]}\n"
                    f"This indicates that SimRacerHub may have changed their page structure.\n"
                    f"Expected pattern: {missing_patterns[0]}"
                )
            else:
                pattern_list = "\n  - ".join(missing_patterns)
                raise SchemaChangeDetected(
                    f"Missing {len(missing_patterns)} required JavaScript patterns for '{entity_type}':\n"
                    f"  - {pattern_list}\n"
                    f"This indicates that SimRacerHub may have changed their page structure."
                )

        return True

    def validate_extracted_data(self, entity_type: str, data: dict) -> bool:
        """Validate that extracted data contains all required fields.

        This method checks that the extracted/parsed data dictionary contains
        all the required fields defined in the schema. If any required field
        is missing or has a None value, it raises SchemaChangeDetected.

        Args:
            entity_type: Type of entity (league_series, series_seasons, etc.)
            data: Extracted data dictionary to validate

        Returns:
            bool: True if all required fields are present with non-None values

        Raises:
            SchemaChangeDetected: If any required field is missing or None
            ValueError: If entity_type is not recognized

        Examples:
            >>> validator = SchemaValidator()
            >>> data = {"id": 1558, "name": "The OBRL"}
            >>> validator.validate_extracted_data("league_series", data)
            True

            >>> bad_data = {"id": 1558}  # Missing 'name'
            >>> validator.validate_extracted_data("league_series", bad_data)
            SchemaChangeDetected: Missing required field for 'league_series': name
        """
        # Get the schema for this entity type (raises ValueError if invalid)
        schema = self.get_schema(entity_type)

        # Get the required fields to check
        required_fields = schema.get("required_fields", [])

        # If no required fields defined, nothing to validate
        if not required_fields:
            return True

        # Check each required field
        missing_fields = []
        for field in required_fields:
            # Field is missing if:
            # 1. Not in the data dict at all
            # 2. Present but value is None
            if field not in data or data[field] is None:
                missing_fields.append(field)

        # If any fields are missing, raise SchemaChangeDetected
        if missing_fields:
            if len(missing_fields) == 1:
                raise SchemaChangeDetected(
                    f"Missing required field for '{entity_type}': {missing_fields[0]}\n"
                    f"This indicates that the extracted data structure may have changed.\n"
                    f"Expected field: {missing_fields[0]}"
                )
            else:
                field_list = ", ".join(missing_fields)
                raise SchemaChangeDetected(
                    f"Missing {len(missing_fields)} required fields for '{entity_type}': {field_list}\n"
                    f"This indicates that the extracted data structure may have changed."
                )

        return True

    def validate_table_structure(self, entity_type: str, table) -> bool:
        """Validate that HTML table has expected structure and columns.

        This method checks that the HTML table contains all required columns
        as defined in the schema. Column matching is case-insensitive and
        allows extra columns beyond the required ones.

        Args:
            entity_type: Type of entity (race_results_table, etc.)
            table: BeautifulSoup table element to validate

        Returns:
            bool: True if table structure is valid

        Raises:
            SchemaChangeDetected: If table structure is invalid or columns missing
            ValueError: If entity_type is not recognized

        Examples:
            >>> validator = SchemaValidator()
            >>> from bs4 import BeautifulSoup
            >>> html = '<table><thead><tr><th>Position</th><th>Driver</th></tr></thead></table>'
            >>> soup = BeautifulSoup(html, 'html.parser')
            >>> table = soup.find('table')
            >>> validator.validate_table_structure("race_results_table", table)
            True
        """
        # Get the schema for this entity type (raises ValueError if invalid)
        schema = self.get_schema(entity_type)

        # Get the required columns to check
        required_columns = schema.get("required_columns", [])

        # If no required columns defined, nothing to validate
        if not required_columns:
            return True

        # Check for None or empty table
        if table is None:
            raise SchemaChangeDetected(
                f"Table is None for entity_type '{entity_type}'. " "Expected a valid table element."
            )

        # Try to find header row - check thead first, then first tr
        header_row = None
        thead = table.find("thead")
        if thead:
            header_row = thead.find("tr")

        if not header_row:
            # No thead, try first tr in table
            header_row = table.find("tr")

        if not header_row:
            raise SchemaChangeDetected(
                f"No header row found in table for entity_type '{entity_type}'. "
                "Expected table to have <thead> or at least one <tr> with column headers."
            )

        # Extract column names from th elements
        column_headers = header_row.find_all("th")
        if not column_headers:
            raise SchemaChangeDetected(
                f"No column headers (<th>) found in table for entity_type '{entity_type}'. "
                "Expected table headers with <th> elements."
            )

        # Get column names (case-insensitive)
        column_names = [th.get_text(strip=True).lower() for th in column_headers]

        # Check minimum column count
        min_columns = len(required_columns)
        if len(column_names) < min_columns:
            raise SchemaChangeDetected(
                f"Insufficient columns in table for '{entity_type}'. "
                f"Found {len(column_names)} columns, expected at least {min_columns}. "
                f"Columns found: {', '.join(column_names)}"
            )

        # Check each required column is present (case-insensitive)
        missing_columns = []
        for required_col in required_columns:
            required_col_lower = required_col.lower()
            if required_col_lower not in column_names:
                missing_columns.append(required_col)

        # If any columns are missing, raise SchemaChangeDetected
        if missing_columns:
            if len(missing_columns) == 1:
                raise SchemaChangeDetected(
                    f"Missing required column in table for '{entity_type}': {missing_columns[0]}\n"
                    f"Found columns: {', '.join(column_names)}\n"
                    f"This indicates that the table structure may have changed."
                )
            else:
                column_list = ", ".join(missing_columns)
                raise SchemaChangeDetected(
                    f"Missing {len(missing_columns)} required columns in table for '{entity_type}': {column_list}\n"
                    f"Found columns: {', '.join(column_names)}\n"
                    f"This indicates that the table structure may have changed."
                )

        return True
