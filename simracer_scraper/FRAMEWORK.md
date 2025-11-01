# SimRacer Scraper - Implementation Framework

**Version**: 0.3.0
**Date**: 2025-10-31
**Status**: Ready for Implementation

## Overview

This document summarizes the framework and architecture for implementing the SimRacer Hub web scraper with complete test coverage and schema validation.

## Key Decisions

### Language: Python 3.10+
**Rationale**:
- Excellent HTML/JavaScript parsing (BeautifulSoup, regex, json)
- Built-in SQLite support
- Rich testing ecosystem (pytest, coverage.py)
- Simple extraction of JavaScript-embedded data
- Already configured with uv + mise

### Database: SQLite
**Rationale**:
- Single file, easy backup/share
- Built-in Python support
- Sufficient performance
- ACID compliance

### Testing: 100% Coverage Requirement
**Strategy**:
- Unit tests for all functions
- Integration tests for workflows
- pytest-cov with --cov-fail-under=100
- CI/CD enforcement

### Schema Validation: Halt on Changes
**Strategy**:
- Pattern matching on JavaScript structure
- Field validation on extracted data
- Column validation on HTML tables
- Alert system (exit code + file + optional email/webhook)

## Project Structure

```
simracer_scraper/
├── src/
│   ├── __init__.py
│   ├── __main__.py
│   ├── cli.py                   # CLI interface
│   ├── config.py                # Configuration management
│   ├── database.py              # SQLite database manager
│   ├── schema_validator.py      # Schema validation & alerting
│   ├── orchestrator.py          # Hierarchical scrape coordination
│   ├── extractors/
│   │   ├── __init__.py
│   │   ├── base.py             # Base extractor (HTTP, parsing, rate limiting)
│   │   ├── league.py           # League extractor
│   │   ├── team.py             # Team extractor
│   │   ├── driver.py           # Driver extractor
│   │   ├── series.py           # Series extractor (JavaScript parsing)
│   │   ├── season.py           # Season extractor (JavaScript parsing)
│   │   ├── race.py             # Race extractor (results table)
│   │   └── utils.py            # Shared utilities (JS parsing, etc.)
│   └── models/
│       ├── __init__.py
│       ├── league.py           # League data model
│       ├── team.py             # Team data model
│       ├── driver.py           # Driver data model
│       ├── series.py           # Series data model
│       ├── season.py           # Season data model
│       ├── race.py             # Race data model
│       └── result.py           # Race result data model
│
├── tests/
│   ├── __init__.py
│   ├── conftest.py             # Shared pytest fixtures
│   ├── unit/
│   │   ├── __init__.py
│   │   ├── test_base_extractor.py
│   │   ├── test_league_extractor.py
│   │   ├── test_team_extractor.py
│   │   ├── test_driver_extractor.py
│   │   ├── test_series_extractor.py
│   │   ├── test_season_extractor.py
│   │   ├── test_race_extractor.py
│   │   ├── test_database.py
│   │   ├── test_orchestrator.py
│   │   ├── test_schema_validator.py
│   │   └── test_utils.py
│   ├── integration/
│   │   ├── __init__.py
│   │   ├── test_league_scrape_workflow.py
│   │   ├── test_series_scrape_workflow.py
│   │   ├── test_database_operations.py
│   │   └── test_schema_validation_flow.py
│   └── fixtures/
│       ├── README.md
│       ├── league_series_1558.html
│       ├── series_seasons_3714.html
│       ├── season_race_324462.html
│       ├── season_standings_3714.html
│       ├── driver_stats_33132.html
│       ├── teams_1558.html
│       ├── empty_results.html
│       └── invalid_javascript.html
│
├── data/                       # Created at runtime
│   └── simracer.db            # SQLite database
│
├── alerts/                     # Created at runtime
│   └── schema_change_*.json   # Alert files
│
├── config.yaml                # Main configuration
├── pyproject.toml            # Project metadata & dependencies
├── uv.lock                   # Locked dependencies
├── .python-version           # Python version for mise
├── SPECIFICATION.md          # Complete technical specification
├── DATA_MODEL.md            # Discovered data model
├── FRAMEWORK.md             # This file
└── README.md                # User-facing documentation
```

## Core Components

### 1. Base Extractor (`src/extractors/base.py`)
**Responsibilities**:
- HTTP session management
- Rate limiting (2s default)
- Retry logic with exponential backoff
- HTML parsing with BeautifulSoup
- Common extraction utilities

**Key Methods**:
```python
class BaseExtractor:
    def fetch_page(url: str) -> BeautifulSoup
    def extract_text(soup, selector: str) -> str
    def extract_all_text(soup, selector: str) -> List[str]
    def _rate_limit()
    def _retry_request()
```

### 2. JavaScript Parser (`src/extractors/utils.py`)
**Responsibilities**:
- Extract JavaScript arrays from `<script>` tags
- Parse series.push() calls
- Parse seasons = [...] arrays
- Convert to Python dictionaries

**Key Functions**:
```python
def extract_js_array(html: str, var_name: str) -> List[dict]
def extract_series_data(html: str) -> List[dict]
def extract_season_data(html: str) -> List[dict]
```

### 3. Entity Extractors (`src/extractors/*.py`)
Each extractor implements:
```python
class EntityExtractor(BaseExtractor):
    def extract(url: str) -> dict:
        """
        Returns:
            {
                "metadata": {...},      # Entity fields
                "child_urls": [...]     # URLs to scrape next (if applicable)
            }
        """
```

**Extractors**:
- `LeagueExtractor` → series_urls, team URLs
- `TeamExtractor` → driver names
- `DriverExtractor` → driver profile data
- `SeriesExtractor` → season_urls (from JS)
- `SeasonExtractor` → race_urls (from JS)
- `RaceExtractor` → race results (from HTML table)

### 4. Database Manager (`src/database.py`)
**Responsibilities**:
- SQLite connection management
- Schema initialization
- CRUD operations for all entities
- URL cache checking
- Upsert operations

**Key Methods**:
```python
class Database:
    def initialize_schema()
    def is_url_cached(url: str, entity_type: str, max_age_days: int) -> bool
    def upsert_league(league_id: int, data: dict) -> int
    def upsert_series(series_id: int, league_id: int, data: dict) -> int
    def upsert_season(season_id: int, series_id: int, data: dict) -> int
    def upsert_race(schedule_id: int, season_id: int, data: dict) -> int
    def upsert_driver(driver_id: int, league_id: int, data: dict) -> int
    def upsert_team(team_id: int, league_id: int, data: dict) -> int
    def upsert_race_result(race_id: int, driver_id: int, data: dict) -> int
    def find_driver_by_name(name: str) -> Optional[int]
    def log_scrape(entity_type: str, url: str, status: str, error: str = None)
```

### 5. Schema Validator (`src/schema_validator.py`)
**Responsibilities**:
- Validate JavaScript patterns
- Validate extracted data fields
- Validate HTML table structure
- Raise SchemaChangeDetected exception
- Log alerts to database & file

**Key Methods**:
```python
class SchemaValidator:
    def validate_javascript_data(entity_type: str, script_content: str) -> bool
    def validate_extracted_data(entity_type: str, data: dict) -> bool
    def validate_table_structure(entity_type: str, table: BeautifulSoup) -> bool
    def alert_schema_change(entity_type: str, details: str)

class SchemaChangeDetected(Exception):
    """Custom exception for schema changes."""
```

### 6. Orchestrator (`src/orchestrator.py`)
**Responsibilities**:
- Coordinate hierarchical scraping
- Check URL cache before scraping
- Call appropriate extractors
- Store data in database
- Track progress
- Handle errors and schema changes

**Key Methods**:
```python
class Orchestrator:
    def scrape_league(league_url: str, depth: str = "full")
    def scrape_series(series_url: str, league_id: int, depth: str)
    def scrape_season(season_url: str, series_id: int, depth: str)
    def scrape_race(race_url: str, season_id: int)
    def scrape_drivers(driver_urls: List[str], league_id: int)
    def scrape_teams(teams_url: str, league_id: int)
```

## Implementation Phases

### Phase 1: Foundation ✅ COMPLETED
- [x] Project structure
- [x] Configuration system
- [x] Dependencies installed
- [x] Data model documented

### Phase 2: Core Infrastructure (NEXT)
Priority order:
1. Database manager with schema
2. Schema validator with alert system
3. JavaScript parsing utilities
4. Base extractor with rate limiting
5. Unit tests for above (100% coverage)

### Phase 3: Entity Extraction
Priority order:
1. League extractor
2. Series extractor (JS parsing)
3. Season extractor (JS parsing)
4. Team extractor
5. Driver extractor
6. Race extractor
7. Unit tests for each (100% coverage)

### Phase 4: Orchestration
1. Orchestrator implementation
2. URL cache integration
3. Progress tracking
4. Depth control
5. Integration tests (100% coverage)

### Phase 5: CLI & Export
1. Enhanced CLI commands
2. Export functionality
3. Status reporting
4. Alert configuration
5. End-to-end tests

### Phase 6: Production
1. Performance optimization
2. CI/CD setup
3. Documentation
4. Production testing

## Testing Strategy

### Unit Tests (100% Coverage)
**For each module**:
- Test all public methods
- Test all branches
- Mock external dependencies (HTTP, DB)
- Test error conditions
- Test edge cases

**Example**:
```python
# tests/unit/test_series_extractor.py

class TestSeriesExtractor:
    def test_extract_seasons_from_js_success(self, mocker):
        """Test extracting seasons from valid JavaScript."""
        # Given
        html = load_fixture("series_seasons_3714.html")
        extractor = SeriesExtractor()

        # When
        result = extractor.extract(html)

        # Then
        assert len(result["child_urls"]) == 28
        assert result["metadata"]["series_id"] == 3714

    def test_extract_seasons_missing_js_raises_error(self):
        """Test that missing JavaScript raises SchemaChangeDetected."""
        # Given
        html = "<html><body>No JavaScript</body></html>"
        extractor = SeriesExtractor()

        # When/Then
        with pytest.raises(SchemaChangeDetected):
            extractor.extract(html)
```

### Integration Tests
**Workflows to test**:
- Full league scrape (with mocked HTTP)
- URL caching behavior
- Database transactions
- Schema validation flow
- Error propagation

**Example**:
```python
# tests/integration/test_league_scrape_workflow.py

def test_full_league_scrape(test_db, mock_http):
    """Test complete league scrape workflow."""
    # Given
    orchestrator = Orchestrator(test_db)
    mock_http.return_league_page("league_series_1558.html")

    # When
    orchestrator.scrape_league("https://example.com/league?id=1558", depth="full")

    # Then
    assert test_db.count_leagues() == 1
    assert test_db.count_series() == 4
    assert test_db.count_seasons() > 100
    assert test_db.count_races() > 1000
```

### Fixtures
**Capture from SimRacerHub**:
1. Save complete HTML responses
2. Include JavaScript data
3. Document capture date
4. Version control fixtures
5. Update when schema changes

## Schema Validation

### Expected Schemas
```python
# src/schema_validator.py

EXPECTED_SCHEMAS = {
    "league_series": {
        "javascript_patterns": [
            r"series\.push\(\{",
            r"id:\s*\d+",
            r"n:\s*\".*\"",
        ],
        "required_fields": ["id", "n"],
    },
    "series_seasons": {
        "javascript_patterns": [
            r"seasons\s*=\s*\[",
            r"id:\s*\d+",
            r"ns:\s*\d+",
            r"nr:\s*\d+",
        ],
        "required_fields": ["id", "n", "scrt", "ns", "nr"],
    },
    "race_results_table": {
        "min_columns": 20,
        "required_columns": [
            "Fin", "Car", "Driver", "Start", "Pts",
            "Laps", "Led", "Fastest", "Incidents"
        ],
    },
    "driver_profile": {
        "required_fields": ["driver_id", "name", "irating"],
    },
    "teams_page": {
        "min_teams": 1,
    },
}
```

### Alert System
**On schema change**:
1. Raise `SchemaChangeDetected` exception
2. Log to `schema_alerts` table
3. Write alert file to `./alerts/`
4. Exit with code 2
5. Optional: Email/webhook notification

## Configuration

### config.yaml
```yaml
# Database
database:
  path: "./data/simracer.db"
  backup_enabled: true

# Cache validity (days)
cache:
  league: 7
  team: 7
  driver: 7
  series: 7
  season_active: 1
  season_completed: 30
  race_upcoming: 1
  race_completed: 30

# Scraping
scraping:
  request_delay: 2.0
  max_retries: 3
  timeout: 10
  user_agent: "SimRacerScraper/0.3.0 (Educational/Personal Use)"

# Schema validation
schema_validation:
  enabled: true
  halt_on_change: true

# Alerts
alerts:
  enabled: true
  methods:
    - exit_code
    - file
  email:
    enabled: false
    smtp_host: "smtp.gmail.com"
    smtp_port: 587
    from_addr: "scraper@example.com"
    to_addr: "admin@example.com"
  webhook:
    enabled: false
    url: ""
    type: "slack"

# Logging
logging:
  level: "INFO"
  file: "./scraper.log"
  database_log: true
```

## Development Workflow

### Setup
```bash
# Install dependencies
uv sync --all-extras

# Verify installation
uv run pytest --version
uv run python -c "import src; print('OK')"
```

### Development Cycle
```bash
# 1. Write code
# 2. Write tests (unit tests first)
# 3. Run tests
uv run pytest tests/unit/test_new_feature.py

# 4. Check coverage
uv run pytest --cov=src/new_module.py

# 5. Format & lint
uv run black src/
uv run ruff check src/

# 6. Type check
uv run mypy src/

# 7. Run all tests
uv run pytest

# 8. Verify 100% coverage
# Should pass due to --cov-fail-under=100
```

### Test-Driven Development
1. Write failing test
2. Implement minimum code to pass
3. Refactor
4. Ensure 100% coverage maintained
5. Commit

## Success Metrics

### Phase 2 Complete When:
- ✅ Database creates all tables correctly
- ✅ Schema validator detects all change types
- ✅ JavaScript parser extracts series/seasons
- ✅ Base extractor handles rate limiting & retries
- ✅ 100% unit test coverage for Phase 2 code

### Phase 3 Complete When:
- ✅ All extractors working for OBRL league
- ✅ Race results table fully parsed (20+ columns)
- ✅ Driver profiles extracted
- ✅ Driver name resolution working
- ✅ 100% unit test coverage for Phase 3 code

### Phase 4 Complete When:
- ✅ Full league scrape completes successfully
- ✅ URL caching prevents duplicate requests
- ✅ Progress tracking accurate
- ✅ Schema changes halt execution with alerts
- ✅ 100% integration test coverage

### Production Ready When:
- ✅ Complete OBRL scrape (4 series, 100+ seasons, 1000+ races)
- ✅ Export to JSON/CSV working
- ✅ Re-scrape only updates changed data
- ✅ CI/CD pipeline passing
- ✅ 100% overall test coverage maintained

## Next Steps

1. **Create test infrastructure** (Phase 2)
   - Create `tests/unit/`, `tests/integration/`, `tests/fixtures/` directories
   - Write `conftest.py` with common fixtures
   - Capture HTML fixtures from SimRacerHub

2. **Implement database manager** (Phase 2)
   - Create schema initialization
   - Implement CRUD operations
   - Add URL cache checking
   - Write comprehensive unit tests

3. **Implement schema validator** (Phase 2)
   - Define expected schemas
   - Implement validation methods
   - Create alert system
   - Write comprehensive unit tests

4. **Continue with extractors** (Phase 3)
   - Start with LeagueExtractor
   - Add JavaScript parsing for SeriesExtractor
   - Progress through hierarchy
   - Maintain 100% test coverage throughout

## References

- **SPECIFICATION.md** - Complete technical specification
- **DATA_MODEL.md** - Discovered data model from SimRacerHub
- **README.md** - User-facing documentation
- SimRacerHub: https://www.simracerhub.com/
- Test League: https://www.simracerhub.com/league_series.php?league_id=1558
