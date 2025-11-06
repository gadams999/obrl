# SimRacer Scraper - Architecture

**Last Updated**: 2025-11-06
**Status**: Production
**Source**: https://www.simracerhub.com/

## Overview

SimRacer Scraper is a hierarchical web scraper for extracting racing league data from SimRacerHub. It uses Python 3.10+, SQLite for storage, Playwright for JavaScript rendering, and implements 100% test coverage.

## Technology Stack

- **Language**: Python 3.10+
- **Database**: SQLite3
- **HTML Parsing**: BeautifulSoup4
- **JavaScript Rendering**: Playwright (Chromium)
- **Testing**: pytest with 100% coverage requirement
- **Package Manager**: uv
- **Code Quality**: black, ruff, mypy

## Project Structure

```
simracer_scraper/
├── src/
│   ├── __init__.py
│   ├── __main__.py
│   ├── cli.py                      # Command-line interface
│   ├── database.py                 # SQLite database manager
│   ├── orchestrator.py             # Hierarchical scrape coordination
│   ├── schema_validator.py         # Schema validation & alerting
│   ├── extractors/
│   │   ├── __init__.py
│   │   ├── base.py                 # Base extractor (HTTP, rate limiting, parsing)
│   │   ├── league.py               # League extractor
│   │   ├── series.py               # Series extractor (JavaScript parsing)
│   │   ├── season.py               # Season extractor (JavaScript parsing)
│   │   └── race.py                 # Race extractor (results table)
│   └── utils/
│       ├── __init__.py
│       ├── browser_manager.py      # Shared Playwright browser instance
│       └── js_parser.py            # JavaScript data extraction utilities
│
├── tests/
│   ├── __init__.py
│   ├── conftest.py                 # Shared pytest fixtures
│   └── unit/
│       ├── test_base_extractor.py
│       ├── test_league_extractor.py
│       ├── test_series_extractor.py
│       ├── test_season_extractor.py
│       ├── test_race_extractor.py
│       ├── test_database.py
│       ├── test_database_crud.py
│       ├── test_orchestrator.py
│       ├── test_schema_validator.py
│       ├── test_browser_manager.py
│       └── test_js_parser.py
│
├── config.yaml                     # Configuration
├── config.yaml.example             # Configuration template
├── pyproject.toml                  # Project metadata & dependencies
├── scraper.py                      # Convenience wrapper script
├── requirements.txt                # Pip fallback dependencies
└── obrl.db                         # SQLite database (created at runtime)
```

## Data Model

### Entity Hierarchy

```
League (league_id: 1558)
  ├── Teams (15 teams)
  │   └── Drivers (1-9 per team, ~60 total with affiliations)
  │
  └── Series (4 series)
       └── Seasons (28+ seasons per series)
            └── Races (10 races per season typically)
                 └── Results (40 participants per race)
```

### URL Patterns

SimRacerHub uses query parameter-based URLs (not REST):

| Entity | URL Pattern | Example |
|--------|-------------|---------|
| League Series List | `league_series.php?league_id={id}` | `league_id=1558` |
| Series Seasons | `series_seasons.php?series_id={id}` | `series_id=3714` |
| Season Races | `season_race.php?series_id={id}&season_id={id}` | `series_id=3714&season_id=26741` |
| Individual Race | `season_race.php?schedule_id={id}` | `schedule_id=324462` |

**Key Observations**:
- All identifiers are numeric integers
- JavaScript rendering required for series/season data
- Race results in HTML tables (20+ columns)

### Database Schema

#### Tables

**leagues**
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
)
```

**teams**
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
)
```

**drivers**
```sql
CREATE TABLE drivers (
    driver_id INTEGER PRIMARY KEY,
    league_id INTEGER NOT NULL,
    team_id INTEGER,
    name TEXT NOT NULL,
    first_name TEXT,
    last_name TEXT,
    car_numbers TEXT,
    primary_number TEXT,
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
)
```

**series**
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
)
```

**seasons**
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
    hc BOOLEAN,
    mc BOOLEAN,
    psn INTEGER,
    url TEXT NOT NULL UNIQUE,
    scraped_at TIMESTAMP NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (series_id) REFERENCES series(series_id)
)
```

**races**
```sql
CREATE TABLE races (
    race_id INTEGER PRIMARY KEY AUTOINCREMENT,
    schedule_id INTEGER NOT NULL UNIQUE,
    internal_race_id INTEGER,
    season_id INTEGER NOT NULL,
    race_number INTEGER NOT NULL,
    name TEXT,
    track TEXT,
    track_config TEXT,
    track_type TEXT,
    date TIMESTAMP,
    duration TEXT,
    laps INTEGER,
    leaders INTEGER,
    lead_changes INTEGER,
    weather TEXT,
    temperature TEXT,
    humidity TEXT,
    url TEXT NOT NULL UNIQUE,
    status TEXT CHECK(status IN ('upcoming', 'ongoing', 'completed')),
    is_complete BOOLEAN DEFAULT 0,
    scraped_at TIMESTAMP NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (season_id) REFERENCES seasons(season_id)
)
```

**race_results**
```sql
CREATE TABLE race_results (
    result_id INTEGER PRIMARY KEY AUTOINCREMENT,
    race_id INTEGER NOT NULL,
    driver_id INTEGER NOT NULL,
    finish_position INTEGER,
    starting_position INTEGER,
    car_number TEXT,
    qualifying_time TEXT,
    fastest_lap TEXT,
    fastest_lap_number INTEGER,
    average_lap TEXT,
    interval TEXT,
    laps_completed INTEGER,
    laps_led INTEGER,
    incidents INTEGER,
    race_points INTEGER,
    bonus_points INTEGER,
    total_points INTEGER,
    fast_laps INTEGER,
    quality_passes INTEGER,
    closing_passes INTEGER,
    total_passes INTEGER,
    average_running_position REAL,
    irating INTEGER,
    status TEXT,
    car_type TEXT,
    team TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (race_id) REFERENCES races(race_id),
    FOREIGN KEY (driver_id) REFERENCES drivers(driver_id),
    UNIQUE(race_id, driver_id)
)
```

**scrape_log**
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
)
```

**schema_alerts**
```sql
CREATE TABLE schema_alerts (
    alert_id INTEGER PRIMARY KEY AUTOINCREMENT,
    entity_type TEXT NOT NULL,
    alert_type TEXT NOT NULL,
    details TEXT NOT NULL,
    url TEXT,
    resolved BOOLEAN DEFAULT 0,
    timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP
)
```

## Core Components

### 1. Orchestrator (`src/orchestrator.py`)

**Responsibilities**:
- Coordinates hierarchical scraping (League → Series → Season → Race)
- Implements depth control (stop at any level)
- Smart caching to avoid redundant scrapes
- Progress tracking and error handling
- Rate limiting enforcement

**Key Methods**:
```python
class Orchestrator:
    def scrape_league(league_url: str, depth: str, cache_max_age_days: int, force: bool)
    def _scrape_series_list(league_id: int, series_urls: list, depth: str, cache_max_age_days: int)
    def _scrape_seasons_list(series_id: int, season_urls: list, depth: str, cache_max_age_days: int)
    def _scrape_races_list(season_id: int, race_urls: list, cache_max_age_days: int)
    def get_progress() -> dict
```

**Depth Levels**:
- `league`: Scrape only league metadata
- `series`: Scrape league + series
- `season`: Scrape league + series + seasons
- `race`: Full scrape (league + series + seasons + races + results)

### 2. Extractors (`src/extractors/`)

#### Base Extractor (`base.py`)

**Responsibilities**:
- Rate limiting (2-4 seconds randomized delay)
- Retry logic with exponential backoff (3 retries default)
- HTML parsing with BeautifulSoup
- Shared browser manager integration

**Key Methods**:
```python
class BaseExtractor:
    def fetch_html(url: str, use_js: bool = False) -> str
    def parse_html(html: str) -> BeautifulSoup
    def _rate_limit()
```

#### League Extractor (`league.py`)

**Extracts**:
- League metadata (name, description, organizer)
- Series IDs and URLs (from JavaScript)
- Team data and driver affiliations

**JavaScript Parsing**: Extracts `series.push({...})` calls

#### Series Extractor (`series.py`)

**Extracts**:
- Series metadata (name, description, vehicle_type)
- Season IDs and URLs (from JavaScript)

**JavaScript Parsing**: Extracts `seasons = [...]` array

#### Season Extractor (`season.py`)

**Extracts**:
- Season metadata (name, year, start_date, race counts)
- Race IDs and URLs (from HTML links)

**Requires**: JavaScript rendering to get race URLs

#### Race Extractor (`race.py`)

**Extracts**:
- Race metadata (track, date, conditions)
- Race results table (20+ columns per driver)
- Driver IDs (from profile links)

**Requires**: JavaScript rendering for full results table

### 3. Database Manager (`src/database.py`)

**Responsibilities**:
- SQLite connection management (context manager)
- Schema initialization
- Upsert operations for all entities
- Cache checking via `is_url_cached()`
- Scrape logging

**Cache Strategy**:
- Tracks `scraped_at` timestamp for each entity
- `cache_max_age_days` parameter controls staleness
- Set to `None` for indefinite caching (immutable data like race results)
- Default: 7 days for most entities

**Key Methods**:
```python
class Database:
    def initialize_schema()
    def is_url_cached(url: str, cache_max_age_days: int) -> bool
    def should_scrape(url: str, cache_max_age_days: int) -> bool
    def upsert_league(league_id: int, data: dict) -> int
    def upsert_series(series_id: int, data: dict) -> int
    def upsert_season(season_id: int, data: dict) -> int
    def upsert_race(schedule_id: int, data: dict) -> int
    def upsert_driver(driver_id: int, data: dict) -> int
    def upsert_team(team_id: int, data: dict) -> int
    def upsert_race_result(race_id: int, driver_id: int, data: dict) -> int
```

### 4. Browser Manager (`src/utils/browser_manager.py`)

**Responsibilities**:
- Manages shared Playwright browser instance
- Prevents async conflicts
- Enforces rate limiting (2-4 seconds)
- Handles JavaScript rendering

**Key Features**:
- Singleton pattern (one browser for entire session)
- Sequential page operations (prevents race conditions)
- Automatic browser cleanup on exit

**Usage**:
```python
async with get_browser_manager() as manager:
    html = await manager.fetch_html(url)
```

### 5. JavaScript Parser (`src/utils/js_parser.py`)

**Responsibilities**:
- Extract data from JavaScript embedded in HTML
- Parse `series.push({...})` patterns
- Parse `seasons = [...]` arrays
- Convert JavaScript objects to Python dicts

**Key Functions**:
```python
def extract_series_from_js(html: str) -> list[dict]
def extract_seasons_from_js(html: str) -> list[dict]
def extract_race_urls_from_js(html: str) -> list[str]
```

### 6. Schema Validator (`src/schema_validator.py`)

**Responsibilities**:
- Validate JavaScript patterns still exist
- Validate extracted data has required fields
- Log alerts to database
- Raise exceptions on schema changes

**Key Methods**:
```python
class SchemaValidator:
    def validate_league_data(data: dict) -> bool
    def validate_series_data(data: dict) -> bool
    def validate_season_data(data: dict) -> bool
    def validate_race_data(data: dict) -> bool
    def log_alert(entity_type: str, alert_type: str, details: str)
```

## Data Flow

### Typical Scrape Flow

```
CLI → Orchestrator → Extractor → BaseExtractor → Browser Manager → SimRacerHub
                ↓
            Database (with caching)
                ↓
        Schema Validator
```

### Hierarchical Scraping

1. **League Level**:
   - Fetch `league_series.php?league_id={id}`
   - Parse JavaScript for series IDs
   - Extract team/driver data
   - Store in database

2. **Series Level** (if depth >= series):
   - For each series: fetch `series_seasons.php?series_id={id}`
   - Parse JavaScript for season IDs
   - Store in database

3. **Season Level** (if depth >= season):
   - For each season: fetch season detail page
   - Extract race URLs from JavaScript
   - Store in database

4. **Race Level** (if depth >= race):
   - For each race: fetch `season_race.php?schedule_id={id}`
   - Parse results table (HTML)
   - Extract driver IDs from profile links
   - Store race + results in database

## Rate Limiting

**Strategy**: Randomized delays to mimic human browsing

- **Default**: 2-4 seconds between requests
- **Configurable**: Set `rate_limit_range=(min, max)` in orchestrator
- **Applied**: Before every HTTP request
- **Shared**: Enforced across all extractors via BrowserManager

**Implementation**:
```python
# In BaseExtractor
self._rate_limit()  # Sleeps random time in range
html = await self.fetch_html(url)
```

## Caching Strategy

**Database-driven**: All caching via `scraped_at` timestamps

**Cache Levels**:
- **League/Series/Season**: 7 days default
- **Races (completed)**: Forever (`cache_max_age_days=None`)
- **Races (upcoming)**: 1 day

**Force Refresh**: CLI `--force` flag sets `cache_max_age_days=None` to bypass all caching

**Implementation**:
```python
if db.is_url_cached(url, cache_max_age_days=7):
    logger.info(f"Skipping cached URL: {url}")
    return
```

## JavaScript Rendering

**Challenge**: SimRacerHub embeds critical data in JavaScript

**Solution**: Playwright with Chromium

**What Requires JS Rendering**:
- League → Series list
- Series → Seasons list
- Season → Race URLs
- Race → Full results table

**Implementation**:
- Shared BrowserManager prevents conflicts
- Sequential execution (no concurrent requests)
- Rate limiting enforced before each page load

## Error Handling

**Strategy**: Fail-soft with logging

**Error Types**:
1. **HTTP Errors**: Retry with exponential backoff
2. **Parsing Errors**: Log and skip entity
3. **Schema Changes**: Alert and continue or halt
4. **Database Errors**: Rollback transaction

**Logging**: All scrapes logged to `scrape_log` table with status and error messages

## Testing

**Requirement**: 100% code coverage

**Structure**:
- `tests/unit/`: Unit tests with mocked dependencies
- `tests/conftest.py`: Shared fixtures (in-memory DB, mock HTTP)

**Coverage Enforcement**:
```bash
uv run pytest --cov=src --cov-fail-under=100
```

**Fixtures**: Use in-memory SQLite (`:memory:`) and pytest-mock for HTTP

## Configuration

**File**: `config.yaml`

**Key Settings**:
```yaml
league:
  id: 1558              # Default league to scrape
  depth: race           # How deep to scrape
  database: obrl.db     # Database file path

scraping:
  request_delay: 2.0    # Minimum delay between requests
  max_retries: 3
  timeout: 10

logging:
  level: INFO
```

**Priority**: CLI args > config.yaml > defaults

## Design Patterns

1. **Context Managers**: Database and Orchestrator use `with` statements
2. **Upsert Operations**: All database writes use `INSERT ... ON CONFLICT`
3. **Progressive Discovery**: Each level discovers URLs for the next
4. **Fail-Soft**: Errors at one entity don't stop scraping siblings
5. **Singleton**: BrowserManager ensures one browser instance

## Performance Considerations

**Rate Limiting vs Speed**: Prioritizes being respectful over speed

**Typical Performance**:
- League: ~5 seconds
- Series (4): ~20 seconds
- Seasons (100+): ~5-10 minutes
- Races (1000+): ~2-3 hours

**Optimization**:
- Caching prevents re-scraping
- Database indexes on foreign keys and URLs
- Completed races cached forever (immutable)

## Future Enhancements

**Potential Improvements**:
1. Progress bar for long scrapes
2. Webhook notifications on completion/errors
3. Export to JSON/CSV
4. Incremental scraping (only new races)
5. Parallel scraping (with careful rate limiting)
