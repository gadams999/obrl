# SimRacer Scraper - Technical Specification

**Version**: 0.3.0
**Last Updated**: 2025-10-31
**Status**: Draft

## 1. Executive Summary

### 1.1 Purpose
This specification defines the behavior, interfaces, and implementation details for a hierarchical web scraper that extracts racing league data from SimRacerHub (https://www.simracerhub.com/).

### 1.2 Scope
The scraper will extract publicly accessible league data in a hierarchical structure (League → Series → Season → Race) with complete driver rosters, storing entity URLs locally to prevent re-querying. Includes comprehensive schema validation and 100% test coverage.

### 1.3 Primary Use Case
Extract complete hierarchical data for "The OBRL presented by VctryLnSprts" (league_id=1558) league, including all series, seasons, races, and driver information for archival and analysis purposes.

### 1.4 Implementation Language
**Python 3.10+** - Selected for:
- Excellent HTML/JavaScript parsing (BeautifulSoup, lxml, regex)
- Built-in SQLite support
- Simple handling of embedded JavaScript data
- Rich testing ecosystem (pytest, coverage.py)
- Already configured with uv + mise

## 2. Data Model (Discovered from SimRacerHub)

### 2.1 URL Patterns

**All URLs use query parameter format**: `page.php?param=value`

| Entity | URL Pattern | Example |
|--------|-------------|---------|
| League Series List | `league_series.php?league_id={id}` | `league_id=1558` |
| Series Seasons | `series_seasons.php?series_id={id}` | `series_id=3714` |
| Season Detail | `series_seasons.php?series_id={sid}&season_id={id}` | `series_id=3714&season_id=26741` |
| Race List | `season_race.php?series_id={id}` | `series_id=3714` |
| Race Detail | `season_race.php?schedule_id={id}` | `schedule_id=324462` |
| Season Standings | `season_standings.php?series_id={id}` | `series_id=3714` |
| Driver Profile | `driver_stats.php?driver_id={id}` | `driver_id=33132` |
| Teams/Roster | `teams.php?league_id={id}` | `league_id=1558` |

### 2.2 Entity Hierarchy

```
League (league_id: 1558)
  ├── Teams (15 teams)
  │   └── Drivers (1-9 per team, ~60 total affiliated)
  │
  └── Series (4 series: 3714, 3713, 3712, 3711)
       └── Seasons (28+ seasons per series)
            └── Races (10 races per season typically)
                 ├── Race Metadata (track, date, conditions)
                 └── Results (40 participants per race)
                      └── Driver Stats (20+ metrics per driver)
```

### 2.3 Entity Relationships

```
┌─────────────────────────────────────────────────────────────┐
│                         League                               │
│  - league_id (PK, from URL)                                 │
│  - name                                                      │
│  - description                                              │
│  - url (unique)                                             │
│  - scraped_at                                               │
└──────────┬─────────────────────────────────────────┬────────┘
           │                                         │
           │ 1:N                                     │ 1:N
           ▼                                         ▼
┌──────────────────┐                    ┌──────────────────────┐
│      Team        │                    │       Series         │
│  - team_id (PK)  │                    │  - series_id (PK)    │
│  - league_id(FK) │                    │  - league_id (FK)    │
│  - name          │                    │  - name              │
│  - url           │◄────┐              │  - vehicle_type      │
└──────┬───────────┘     │              │  - day_of_week       │
       │ 1:N             │              │  - url (unique)      │
       ▼                 │              └──────┬───────────────┘
┌──────────────────┐     │                     │ 1:N
│     Driver       │     │                     ▼
│  - driver_id(PK) │     │           ┌──────────────────────┐
│  - league_id(FK) │─────┘           │       Season         │
│  - team_id (FK)  │                 │  - season_id (PK)    │
│  - name          │                 │  - series_id (FK)    │
│  - irating       │                 │  - name              │
│  - safety_rating │                 │  - year              │
│  - license_class │                 │  - start_date        │
│  - club          │                 │  - status            │
│  - car_numbers[] │                 │  - scheduled_races   │
│  - url (unique)  │                 │  - completed_races   │
└──────────────────┘                 │  - url (unique)      │
           │                         └──────┬───────────────┘
           │                                │ 1:N
           │                                ▼
           │                      ┌──────────────────────┐
           │                      │        Race          │
           │                      │  - race_id (PK)      │
           │                      │  - schedule_id       │
           │                      │  - season_id (FK)    │
           │                      │  - race_number       │
           │                      │  - track             │
           │                      │  - track_config      │
           │                      │  - date              │
           │                      │  - laps              │
           │                      │  - duration          │
           │                      │  - weather           │
           │                      │  - temperature       │
           │                      │  - url (unique)      │
           │                      └──────┬───────────────┘
           │                             │ 1:N
           │                             ▼
           │              ┌──────────────────────────────────┐
           └──────────────│      Race Result                 │
                          │  - result_id (PK)                │
                          │  - race_id (FK)                  │
                          │  - driver_id (FK)                │
                          │  - finish_position               │
                          │  - starting_position             │
                          │  - car_number                    │
                          │  - qualifying_time               │
                          │  - laps_completed                │
                          │  - laps_led                      │
                          │  - fastest_lap                   │
                          │  - fastest_lap_number            │
                          │  - average_lap                   │
                          │  - incidents                     │
                          │  - race_points                   │
                          │  - bonus_points                  │
                          │  - total_points                  │
                          │  - fast_laps                     │
                          │  - quality_passes                │
                          │  - closing_passes                │
                          │  - total_passes                  │
                          │  - avg_running_position          │
                          │  - status                        │
                          │  - car_type                      │
                          │  - team                          │
                          └──────────────────────────────────┘
```

### 2.4 JavaScript Data Extraction

**Critical Discovery**: Series and season data embedded in JavaScript, not HTML tables.

#### Series Data (in league_series.php)
```javascript
series.push({
    id: 3714,
    n: "The OBRL Wednesday Night Series",
    // ... additional metadata
});
```

#### Season Data (in series_seasons.php)
```javascript
seasons = [
    {
        id: 26741,
        n: "2025 OBRL '87 Legends - Season 1",
        scrt: 1754380800,           // Unix timestamp
        scrt_str: "Aug 5, 2025",
        ns: 10,                      // Scheduled races
        nr: 9,                       // Completed races
        hc: false,
        mc: false,
        psn: 1234
    }
];
```

**Extraction Strategy**:
```python
import re
import json

# Extract from <script> tags
script_content = soup.find('script', string=re.compile('seasons = '))
match = re.search(r'seasons = (\[.*?\]);', script_content.string, re.DOTALL)
seasons = json.loads(match.group(1))  # JavaScript array is JSON-compatible
```

### 2.5 URL Caching Strategy

**Purpose**: Prevent re-querying already scraped entities

**Rules**:
1. Every entity stores its unique source URL
2. Before scraping an entity, check if URL exists in database
3. If URL exists and within cache validity period, skip scraping
4. If URL exists but expired, re-scrape and update
5. URLs are unique constraints in the database

**Cache Validity**:
- League: 7 days
- Team: 7 days
- Driver: 7 days
- Series: 7 days
- Season: 1 day (active), 30 days (completed)
- Race: 1 day (upcoming/ongoing), 30 days (completed)

## 3. Schema Validation & Monitoring

### 3.1 Schema Change Detection

**Requirement**: Detect when SimRacerHub changes their data structure and halt scraping.

**Implementation Strategy**:

#### 3.1.1 Schema Fingerprints

Store expected schema fingerprints for each entity type:

```python
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
    # ... for each entity type
}
```

#### 3.1.2 Validation on Each Scrape

```python
class SchemaValidator:
    """Validate extracted data against expected schema."""

    def validate_javascript_data(self, entity_type: str, script_content: str) -> bool:
        """Validate JavaScript structure matches expected patterns."""
        schema = EXPECTED_SCHEMAS[entity_type]

        for pattern in schema["javascript_patterns"]:
            if not re.search(pattern, script_content):
                raise SchemaChangeDetected(
                    f"Missing pattern: {pattern} in {entity_type}"
                )

        return True

    def validate_extracted_data(self, entity_type: str, data: dict) -> bool:
        """Validate extracted data has required fields."""
        schema = EXPECTED_SCHEMAS[entity_type]

        for field in schema["required_fields"]:
            if field not in data:
                raise SchemaChangeDetected(
                    f"Missing field: {field} in {entity_type}"
                )

        return True

    def validate_table_structure(self, entity_type: str, table: BeautifulSoup) -> bool:
        """Validate HTML table has expected columns."""
        schema = EXPECTED_SCHEMAS[entity_type]

        headers = [th.get_text(strip=True) for th in table.find_all('th')]

        if len(headers) < schema["min_columns"]:
            raise SchemaChangeDetected(
                f"Table has {len(headers)} columns, expected >= {schema['min_columns']}"
            )

        for col in schema["required_columns"]:
            if col not in headers:
                raise SchemaChangeDetected(
                    f"Missing column: {col} in {entity_type} table"
                )

        return True
```

#### 3.1.3 Schema Change Exception

```python
class SchemaChangeDetected(Exception):
    """Raised when data structure doesn't match expected schema."""
    pass
```

#### 3.1.4 Error Handling in Orchestrator

```python
def scrape_league(league_url: str):
    try:
        # ... scraping logic
        validator.validate_javascript_data("league_series", script_content)
        # ... continue scraping
    except SchemaChangeDetected as e:
        logger.critical(f"SCHEMA CHANGE DETECTED: {e}")
        alert_user(e)  # Method TBD
        raise  # Stop execution
```

### 3.2 Alerting Mechanisms (TBD)

**Options for Implementation**:

1. **Exit with Error Code**
   - Simple: Exit with code 2 (schema error)
   - User checks exit code

2. **Email Alert**
   - Requires SMTP configuration
   - Send email on schema change

3. **Slack/Discord Webhook**
   - Post message to channel
   - Requires webhook URL configuration

4. **File-based Alert**
   - Write alert to `./alerts/schema_change_{timestamp}.json`
   - User monitors directory

5. **Database Alert Log**
   - Insert into `schema_alerts` table
   - User queries database

**Configuration** (in config.yaml):
```yaml
alerts:
  enabled: true
  methods:
    - exit_code  # Always enabled
    - file       # Write to alerts/ directory
    - email      # Optional
    - webhook    # Optional

  email:
    smtp_host: "smtp.gmail.com"
    smtp_port: 587
    from_addr: "scraper@example.com"
    to_addr: "admin@example.com"

  webhook:
    url: "https://hooks.slack.com/services/..."
    type: "slack"  # or "discord"
```

### 3.3 Schema Update Workflow

When schema change detected:

1. **Scraper halts immediately**
2. **Alert generated** (via configured methods)
3. **User investigates** SimRacerHub changes
4. **Update schema fingerprints** in code
5. **Update extractors** if needed
6. **Update database schema** if new fields
7. **Run tests** to validate
8. **Resume scraping**

## 4. Database Schema

### 4.1 Database Technology

**Choice**: SQLite

**Rationale**:
- Single file, easy to backup/share
- No server setup required
- Sufficient performance for expected data volume
- Built-in Python support
- ACID compliance

**Location**: `./data/simracer.db` (configurable)

### 4.2 Tables

#### 4.2.1 Table: `leagues`

```sql
CREATE TABLE leagues (
    league_id INTEGER PRIMARY KEY,
    name TEXT NOT NULL,
    description TEXT,
    organizer TEXT,
    url TEXT NOT NULL UNIQUE,
    scraped_at TIMESTAMP NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_leagues_url ON leagues(url);
CREATE INDEX idx_leagues_scraped_at ON leagues(scraped_at);
```

#### 4.2.2 Table: `teams`

```sql
CREATE TABLE teams (
    team_id INTEGER PRIMARY KEY,
    league_id INTEGER NOT NULL,
    name TEXT NOT NULL,
    driver_count INTEGER,
    url TEXT,
    scraped_at TIMESTAMP NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (league_id) REFERENCES leagues(league_id)
);

CREATE INDEX idx_teams_league_id ON teams(league_id);
CREATE INDEX idx_teams_scraped_at ON teams(scraped_at);
```

#### 4.2.3 Table: `drivers`

```sql
CREATE TABLE drivers (
    driver_id INTEGER PRIMARY KEY,
    league_id INTEGER NOT NULL,
    team_id INTEGER,
    name TEXT NOT NULL,
    first_name TEXT,
    last_name TEXT,
    car_numbers TEXT,  -- JSON array: ["23", "1", "30"]
    primary_number TEXT,

    -- iRacing Info
    club TEXT,
    club_id INTEGER,
    irating INTEGER,
    safety_rating REAL,
    license_class TEXT,

    url TEXT NOT NULL UNIQUE,
    scraped_at TIMESTAMP NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,

    FOREIGN KEY (league_id) REFERENCES leagues(league_id),
    FOREIGN KEY (team_id) REFERENCES teams(team_id)
);

CREATE INDEX idx_drivers_league_id ON drivers(league_id);
CREATE INDEX idx_drivers_team_id ON drivers(team_id);
CREATE INDEX idx_drivers_url ON drivers(url);
CREATE INDEX idx_drivers_name ON drivers(name);
CREATE INDEX idx_drivers_scraped_at ON drivers(scraped_at);
```

#### 4.2.4 Table: `series`

```sql
CREATE TABLE series (
    series_id INTEGER PRIMARY KEY,
    league_id INTEGER NOT NULL,
    name TEXT NOT NULL,
    description TEXT,
    vehicle_type TEXT,
    day_of_week TEXT,
    active BOOLEAN,
    season_count INTEGER,
    created_date TEXT,
    url TEXT NOT NULL UNIQUE,
    scraped_at TIMESTAMP NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (league_id) REFERENCES leagues(league_id)
);

CREATE INDEX idx_series_league_id ON series(league_id);
CREATE INDEX idx_series_url ON series(url);
CREATE INDEX idx_series_active ON series(active);
CREATE INDEX idx_series_scraped_at ON series(scraped_at);
```

#### 4.2.5 Table: `seasons`

```sql
CREATE TABLE seasons (
    season_id INTEGER PRIMARY KEY,
    series_id INTEGER NOT NULL,
    name TEXT NOT NULL,
    year INTEGER,
    start_date DATE,
    start_timestamp INTEGER,
    end_date DATE,
    scheduled_races INTEGER,
    completed_races INTEGER,
    status TEXT CHECK(status IN ('upcoming', 'active', 'completed')),

    -- Unknown fields from JS (preserve for investigation)
    hc BOOLEAN,
    mc BOOLEAN,
    psn INTEGER,

    url TEXT NOT NULL UNIQUE,
    scraped_at TIMESTAMP NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (series_id) REFERENCES series(series_id)
);

CREATE INDEX idx_seasons_series_id ON seasons(series_id);
CREATE INDEX idx_seasons_url ON seasons(url);
CREATE INDEX idx_seasons_year ON seasons(year);
CREATE INDEX idx_seasons_status ON seasons(status);
CREATE INDEX idx_seasons_scraped_at ON seasons(scraped_at);
```

#### 4.2.6 Table: `races`

```sql
CREATE TABLE races (
    race_id INTEGER PRIMARY KEY AUTOINCREMENT,
    schedule_id INTEGER NOT NULL UNIQUE,  -- From SimRacerHub
    internal_race_id INTEGER,              -- SimRacerHub's internal race_id
    season_id INTEGER NOT NULL,
    race_number INTEGER NOT NULL,
    name TEXT,
    track TEXT,
    track_config TEXT,
    track_type TEXT,
    date TIMESTAMP,

    -- Race Metadata
    duration TEXT,
    laps INTEGER,
    leaders INTEGER,
    lead_changes INTEGER,

    -- Conditions
    weather TEXT,
    temperature TEXT,
    humidity TEXT,

    url TEXT NOT NULL UNIQUE,
    status TEXT CHECK(status IN ('upcoming', 'ongoing', 'completed')),
    scraped_at TIMESTAMP NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (season_id) REFERENCES seasons(season_id)
);

CREATE INDEX idx_races_schedule_id ON races(schedule_id);
CREATE INDEX idx_races_season_id ON races(season_id);
CREATE INDEX idx_races_url ON races(url);
CREATE INDEX idx_races_date ON races(date);
CREATE INDEX idx_races_status ON races(status);
CREATE INDEX idx_races_scraped_at ON races(scraped_at);
```

#### 4.2.7 Table: `race_results`

```sql
CREATE TABLE race_results (
    result_id INTEGER PRIMARY KEY AUTOINCREMENT,
    race_id INTEGER NOT NULL,
    driver_id INTEGER NOT NULL,

    -- Position
    finish_position INTEGER,
    starting_position INTEGER,
    car_number TEXT,

    -- Timing
    qualifying_time TEXT,
    fastest_lap TEXT,
    fastest_lap_number INTEGER,
    average_lap TEXT,
    interval TEXT,

    -- Performance
    laps_completed INTEGER,
    laps_led INTEGER,
    incidents INTEGER,

    -- Points
    race_points INTEGER,
    bonus_points INTEGER,
    total_points INTEGER,

    -- Advanced Stats
    fast_laps INTEGER,
    quality_passes INTEGER,
    closing_passes INTEGER,
    total_passes INTEGER,
    average_running_position REAL,

    -- iRacing (at time of race)
    irating INTEGER,

    -- Status
    status TEXT,
    car_type TEXT,
    team TEXT,

    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (race_id) REFERENCES races(race_id),
    FOREIGN KEY (driver_id) REFERENCES drivers(driver_id),
    UNIQUE(race_id, driver_id)
);

CREATE INDEX idx_race_results_race_id ON race_results(race_id);
CREATE INDEX idx_race_results_driver_id ON race_results(driver_id);
CREATE INDEX idx_race_results_position ON race_results(finish_position);
```

#### 4.2.8 Table: `scrape_log`

```sql
CREATE TABLE scrape_log (
    log_id INTEGER PRIMARY KEY AUTOINCREMENT,
    entity_type TEXT NOT NULL CHECK(entity_type IN ('league', 'team', 'driver', 'series', 'season', 'race')),
    entity_id INTEGER,
    entity_url TEXT NOT NULL,
    status TEXT NOT NULL CHECK(status IN ('success', 'failed', 'skipped')),
    error_message TEXT,
    duration_ms INTEGER,
    timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_scrape_log_entity_type ON scrape_log(entity_type);
CREATE INDEX idx_scrape_log_status ON scrape_log(status);
CREATE INDEX idx_scrape_log_timestamp ON scrape_log(timestamp);
```

#### 4.2.9 Table: `schema_alerts`

```sql
CREATE TABLE schema_alerts (
    alert_id INTEGER PRIMARY KEY AUTOINCREMENT,
    entity_type TEXT NOT NULL,
    alert_type TEXT NOT NULL,  -- 'missing_pattern', 'missing_field', 'missing_column'
    details TEXT NOT NULL,      -- JSON with specifics
    url TEXT,
    resolved BOOLEAN DEFAULT 0,
    timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_schema_alerts_resolved ON schema_alerts(resolved);
CREATE INDEX idx_schema_alerts_timestamp ON schema_alerts(timestamp);
```

### 4.3 Database Operations

#### 4.3.1 URL Cache Check

```python
def is_url_cached(url: str, entity_type: str, max_age_days: int) -> bool:
    """
    Check if URL is cached and fresh.

    Args:
        url: The URL to check
        entity_type: Type of entity (league, driver, series, season, race)
        max_age_days: Maximum age in days for cache validity

    Returns:
        True if cached and fresh, False otherwise
    """
    table = f"{entity_type}s"
    query = f"""
        SELECT scraped_at FROM {table}
        WHERE url = ?
        AND scraped_at > datetime('now', '-{max_age_days} days')
    """
    result = db.execute(query, (url,)).fetchone()
    return result is not None
```

#### 4.3.2 Upsert Pattern

```python
def upsert_entity(table: str, id_field: str, entity_id: int, data: dict) -> int:
    """
    Insert or update entity based on primary key.

    Args:
        table: Table name
        id_field: Primary key field name
        entity_id: Primary key value
        data: Dictionary of column:value pairs

    Returns:
        Entity ID (primary key)
    """
    # Build INSERT ... ON CONFLICT query
    columns = list(data.keys())
    placeholders = ', '.join(['?' for _ in columns])

    query = f"""
        INSERT INTO {table} ({id_field}, {', '.join(columns)})
        VALUES (?, {placeholders})
        ON CONFLICT({id_field}) DO UPDATE SET
        {', '.join([f'{col}=excluded.{col}' for col in columns])}
    """

    db.execute(query, [entity_id] + list(data.values()))
    return entity_id
```

## 5. Testing Requirements

### 5.1 Test Coverage

**Requirement**: 100% code coverage for all production code

**Tool**: `pytest-cov`

**Configuration** (in `pyproject.toml`):
```toml
[tool.pytest.ini_options]
testpaths = ["tests"]
python_files = ["test_*.py"]
python_classes = ["Test*"]
python_functions = ["test_*"]
addopts = [
    "-v",
    "--cov=src",
    "--cov-report=term-missing",
    "--cov-report=html",
    "--cov-fail-under=100",  # Fail if coverage < 100%
]
```

### 5.2 Test Types

#### 5.2.1 Unit Tests

**Scope**: Individual functions and classes in isolation

**Location**: `tests/unit/`

**Requirements**:
- Test each extractor method independently
- Mock all HTTP requests
- Mock database operations
- Test edge cases and error conditions
- Test schema validation logic
- Test data transformation functions

**Example Structure**:
```
tests/unit/
├── test_base_extractor.py
├── test_league_extractor.py
├── test_series_extractor.py
├── test_season_extractor.py
├── test_race_extractor.py
├── test_driver_extractor.py
├── test_team_extractor.py
├── test_database.py
├── test_orchestrator.py
├── test_schema_validator.py
└── test_utils.py
```

**Coverage Target**: 100% of all functions, branches, and statements

#### 5.2.2 Integration Tests

**Scope**: Component interactions and end-to-end flows

**Location**: `tests/integration/`

**Requirements**:
- Test complete scrape workflows
- Use test database (SQLite in-memory or temp file)
- Use fixture HTML/JavaScript responses (no live HTTP)
- Test database transactions
- Test error propagation
- Test cache invalidation

**Example Structure**:
```
tests/integration/
├── test_league_scrape_workflow.py
├── test_series_scrape_workflow.py
├── test_database_operations.py
├── test_orchestrator_depth_control.py
└── test_schema_validation_flow.py
```

**Coverage Target**: All integration points between components

#### 5.2.3 Fixture Data

**Location**: `tests/fixtures/`

**Requirements**:
- Capture real HTML/JavaScript from SimRacerHub
- Sanitize if needed (remove personal info)
- Version fixtures (track when captured)
- Include fixtures for:
  - Each entity type page
  - Error responses
  - Edge cases (empty tables, missing data)

**Structure**:
```
tests/fixtures/
├── league_series_1558.html
├── series_seasons_3714.html
├── season_race_324462.html
├── season_standings_3714.html
├── driver_stats_33132.html
├── teams_1558.html
├── empty_results.html
├── invalid_javascript.html
└── README.md  # Describes fixtures and when captured
```

### 5.3 Test Execution

#### Run All Tests
```bash
uv run pytest
```

#### Run with Coverage Report
```bash
uv run pytest --cov=src --cov-report=html
open htmlcov/index.html
```

#### Run Specific Test Suite
```bash
# Unit tests only
uv run pytest tests/unit/

# Integration tests only
uv run pytest tests/integration/

# Specific test file
uv run pytest tests/unit/test_league_extractor.py

# Specific test
uv run pytest tests/unit/test_league_extractor.py::test_extract_series_ids
```

#### Run in Watch Mode (during development)
```bash
uv run pytest-watch
```

### 5.4 Continuous Integration

**GitHub Actions Workflow** (`.github/workflows/test.yml`):

```yaml
name: Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    strategy:
      matrix:
        python-version: ["3.10", "3.11", "3.12"]

    steps:
      - uses: actions/checkout@v4

      - name: Install uv
        run: curl -LsSf https://astral.sh/uv/install.sh | sh

      - name: Set up Python
        uses: actions/setup-python@v5
        with:
          python-version: ${{ matrix.python-version }}

      - name: Install dependencies
        run: uv sync --all-extras

      - name: Run linting
        run: |
          uv run ruff check src/
          uv run black --check src/
          uv run mypy src/

      - name: Run tests with coverage
        run: uv run pytest --cov=src --cov-report=xml --cov-fail-under=100

      - name: Upload coverage to Codecov
        uses: codecov/codecov-action@v3
        with:
          file: ./coverage.xml
```

### 5.5 Test Standards

#### 5.5.1 Naming Conventions

- Test files: `test_<module>.py`
- Test classes: `Test<ClassName>`
- Test functions: `test_<function>_<scenario>`

**Examples**:
```python
# tests/unit/test_league_extractor.py

class TestLeagueExtractor:
    def test_extract_series_ids_success(self):
        """Test extracting series IDs from valid JavaScript."""
        pass

    def test_extract_series_ids_missing_script_raises_error(self):
        """Test that missing script tag raises appropriate error."""
        pass

    def test_extract_series_ids_malformed_json_raises_error(self):
        """Test that malformed JSON in script raises error."""
        pass
```

#### 5.5.2 Test Documentation

Each test must have:
- Docstring explaining what is being tested
- Clear arrangement (Given-When-Then or Arrange-Act-Assert)
- Descriptive assertion messages

**Example**:
```python
def test_extract_season_data_from_javascript(self):
    """Test extracting season data from JavaScript array."""
    # Given: HTML with embedded JavaScript season array
    html = load_fixture("series_seasons_3714.html")
    extractor = SeriesExtractor()

    # When: Extracting season data
    seasons = extractor.extract_seasons_from_js(html)

    # Then: Should return list of season dictionaries
    assert len(seasons) == 28, "Should extract all 28 seasons"
    assert seasons[0]["id"] == 26741, "First season should have correct ID"
    assert seasons[0]["n"] == "2025 OBRL '87 Legends - Season 1"
    assert seasons[0]["ns"] == 10, "Should have 10 scheduled races"
```

#### 5.5.3 Mock Standards

Use `pytest-mock` for mocking:

```python
def test_fetch_page_with_retry(mocker):
    """Test that fetch_page retries on failure."""
    # Mock requests.Session.get to fail twice then succeed
    mock_get = mocker.patch('requests.Session.get')
    mock_get.side_effect = [
        requests.exceptions.Timeout(),
        requests.exceptions.Timeout(),
        mocker.Mock(status_code=200, content="<html>...</html>")
    ]

    extractor = BaseExtractor()
    result = extractor.fetch_page("http://example.com")

    assert mock_get.call_count == 3
    assert result is not None
```

### 5.6 Test Data Management

#### 5.6.1 Test Database

Use in-memory SQLite for tests:

```python
import pytest
from src.database import Database

@pytest.fixture
def test_db():
    """Provide clean test database for each test."""
    db = Database(":memory:")  # In-memory database
    db.initialize_schema()
    yield db
    db.close()

def test_upsert_league(test_db):
    """Test upserting league data."""
    league_data = {
        "name": "Test League",
        "url": "https://example.com/league/1",
        "scraped_at": "2025-10-31T12:00:00Z"
    }

    league_id = test_db.upsert_league(1558, league_data)

    assert league_id == 1558
    # Verify data in database
    result = test_db.get_league(1558)
    assert result["name"] == "Test League"
```

#### 5.6.2 Fixture Loading

```python
from pathlib import Path

FIXTURES_DIR = Path(__file__).parent / "fixtures"

def load_fixture(filename: str) -> str:
    """Load test fixture file."""
    filepath = FIXTURES_DIR / filename
    with open(filepath, 'r', encoding='utf-8') as f:
        return f.read()

def test_parse_league_page():
    """Test parsing league page HTML."""
    html = load_fixture("league_series_1558.html")
    # ... test logic
```

## 6. Implementation Phases

### Phase 1: Foundation ✅ COMPLETED
- [x] Base extractor with HTTP/parsing
- [x] Database schema designed
- [x] Project structure with uv + mise
- [x] Configuration system
- [x] Data model documented

### Phase 2: Core Extraction (CURRENT)
- [ ] Database manager implementation
- [ ] Schema validator implementation
- [ ] JavaScript parsing utilities
- [ ] League extractor
- [ ] Series extractor (with JS parsing)
- [ ] Season extractor (with JS parsing)
- [ ] Unit tests for Phase 2 (100% coverage)

### Phase 3: Race & Driver Data
- [ ] Race extractor
- [ ] Driver extractor
- [ ] Team extractor
- [ ] Results extractor
- [ ] Driver name resolution
- [ ] Unit tests for Phase 3 (100% coverage)

### Phase 4: Orchestration
- [ ] Orchestrator implementation
- [ ] Progress tracking
- [ ] Depth control (league/series/season/full)
- [ ] URL cache integration
- [ ] Integration tests (100% coverage)

### Phase 5: Export & CLI
- [ ] Export commands (JSON/CSV)
- [ ] Status command
- [ ] CLI enhancements
- [ ] Alert system implementation
- [ ] End-to-end tests

### Phase 6: Production Readiness
- [ ] Performance optimization
- [ ] Documentation completion
- [ ] CI/CD setup
- [ ] User guide
- [ ] Production testing with OBRL league

## 7. Success Criteria

### Phase 2 Success:
1. ✅ Successfully extract league metadata
2. ✅ Successfully extract all series IDs from JavaScript
3. ✅ Successfully extract all season data from JavaScript
4. ✅ Store in database with proper structure
5. ✅ Schema validation detects changes
6. ✅ 100% unit test coverage for Phase 2 code

### Phase 3 Success:
1. ✅ Successfully extract race list
2. ✅ Successfully extract race results (all 20+ columns)
3. ✅ Successfully extract driver profiles
4. ✅ Successfully resolve driver names to IDs
5. ✅ Store all data with proper relationships
6. ✅ 100% unit test coverage for Phase 3 code

### Phase 4 Success:
1. ✅ Orchestrator coordinates full league scrape
2. ✅ URL caching prevents re-scraping
3. ✅ Depth control works correctly
4. ✅ Progress reporting accurate
5. ✅ 100% integration test coverage

### Phase 6 Success:
1. ✅ Complete scrape of OBRL league (all 4 series, 100+ seasons, 1000+ races)
2. ✅ Export complete dataset to JSON/CSV
3. ✅ Re-scrape only updates new/changed data
4. ✅ Schema changes detected and halt scraping
5. ✅ Zero data loss or integrity issues
6. ✅ 100% overall test coverage maintained
7. ✅ CI/CD pipeline passes all checks

## 8. Version History

| Version | Date | Changes |
|---------|------|---------|
| 0.1.0 | 2025-10-31 | Initial specification |
| 0.2.0 | 2025-10-31 | Restructured with hierarchical model, added database, extractors |
| 0.3.0 | 2025-10-31 | Added discovered data model, schema validation, 100% test coverage requirements |
