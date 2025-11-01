# SimRacer Scraper - Implementation Tasks

**Version**: 1.0.0
**Date**: 2025-10-31
**Status**: Ready to Execute

## Task Execution Rules

### ✅ Development Rules

1. **Work in `claude_temp/` for experiments**
   - All exploratory code goes in `claude_temp/` directory
   - Test ideas, prototype, experiment freely
   - This directory is gitignored

2. **Only move to `src/` when complete**
   - Code must be fully tested and working
   - All tests must pass
   - 100% coverage achieved
   - Code formatted and linted

3. **Never hardcode test results**
   - ❌ BAD: `assert extract_series() == [{"id": 3714, ...}]`
   - ✅ GOOD: `assert len(extract_series()) == 4` and `assert all('id' in s for s in extract_series())`
   - Tests must verify structure and behavior, not exact data
   - Tests should fail if implementation is wrong

4. **Test with real fixtures**
   - Use actual HTML from SimRacerHub
   - Tests validate extraction logic works on real data
   - If site changes, tests will fail (as intended)

5. **Each task is atomic**
   - Complete one task fully before moving to next
   - Task includes: implementation + tests + documentation
   - Task must pass all quality checks

### 🔄 Task Workflow

For each task:

```
1. Read task requirements
2. Work in claude_temp/ to develop solution
3. Write tests in tests/ with fixtures
4. Run tests until passing
5. Check coverage (must be 100% for new code)
6. Format and lint code
7. Move working code from claude_temp/ to src/
8. Verify all tests still pass
9. Commit (if applicable)
10. Move to next task
```

## Phase 2: Core Infrastructure

### Task 2.1: Database Schema & Initialization
**Goal**: Create database manager with schema initialization

**Location**: `src/database.py`

**Requirements**:
- Create `Database` class with SQLite connection
- Implement `initialize_schema()` method
- Create all 9 tables from specification
- Create all indexes
- Support in-memory database for testing
- Connection context manager (`with` statement)

**Acceptance Criteria**:
- [ ] Database creates all tables without errors
- [ ] All indexes created
- [ ] Foreign key constraints work
- [ ] Can use `:memory:` for tests
- [ ] Context manager works (auto-close)

**Tests** (`tests/unit/test_database.py`):
```python
def test_initialize_schema_creates_all_tables(test_db)
def test_tables_have_correct_columns(test_db)
def test_indexes_exist(test_db)
def test_foreign_keys_enforced(test_db)
def test_unique_constraints_enforced(test_db)
def test_context_manager_closes_connection()
```

**Success Criteria**:
- All tests pass
- 100% coverage of `src/database.py`
- Code formatted with black
- No ruff warnings

---

### Task 2.2: Database - League CRUD Operations
**Goal**: Implement league create, read, update operations

**Location**: `src/database.py` (add methods)

**Requirements**:
- `upsert_league(league_id: int, data: dict) -> int`
- `get_league(league_id: int) -> Optional[dict]`
- `get_league_by_url(url: str) -> Optional[dict]`
- Proper timestamp handling (scraped_at, updated_at)
- Handle missing fields gracefully

**Acceptance Criteria**:
- [ ] Insert new league works
- [ ] Update existing league works
- [ ] Get by ID works
- [ ] Get by URL works
- [ ] Timestamps auto-update

**Tests** (`tests/unit/test_database.py`):
```python
def test_upsert_league_inserts_new(test_db)
def test_upsert_league_updates_existing(test_db)
def test_get_league_returns_correct_data(test_db)
def test_get_league_not_found_returns_none(test_db)
def test_get_league_by_url(test_db)
def test_league_timestamps_auto_update(test_db)
```

**Success Criteria**:
- All tests pass
- 100% coverage of new methods
- No SQL injection vulnerabilities

---

### Task 2.3: Database - Series CRUD Operations
**Goal**: Implement series create, read, update operations

**Location**: `src/database.py` (add methods)

**Requirements**:
- `upsert_series(series_id: int, league_id: int, data: dict) -> int`
- `get_series(series_id: int) -> Optional[dict]`
- `get_series_by_league(league_id: int) -> List[dict]`
- Foreign key to league

**Acceptance Criteria**:
- [ ] Insert/update series works
- [ ] Get series by ID works
- [ ] Get all series for league works
- [ ] Foreign key constraint enforced

**Tests** (`tests/unit/test_database.py`):
```python
def test_upsert_series(test_db)
def test_get_series(test_db)
def test_get_series_by_league(test_db)
def test_series_requires_valid_league_id(test_db)
```

**Success Criteria**:
- All tests pass
- 100% coverage

---

### Task 2.4: Database - Season, Race, Driver, Team CRUD Operations
**Goal**: Complete CRUD operations for remaining entities

**Location**: `src/database.py` (add methods)

**Requirements**:
- Season: `upsert_season()`, `get_season()`, `get_seasons_by_series()`
- Race: `upsert_race()`, `get_race()`, `get_races_by_season()`
- Driver: `upsert_driver()`, `get_driver()`, `get_drivers_by_league()`, `find_driver_by_name()`
- Team: `upsert_team()`, `get_team()`, `get_teams_by_league()`
- Race Result: `upsert_race_result()`, `get_race_results()`, `get_driver_results()`

**Acceptance Criteria**:
- [ ] All entity CRUD operations work
- [ ] Foreign keys enforced
- [ ] Unique constraints enforced
- [ ] `find_driver_by_name()` handles fuzzy matching

**Tests** (`tests/unit/test_database.py`):
```python
# 15+ tests covering all CRUD operations
def test_upsert_season(test_db)
def test_upsert_race(test_db)
def test_upsert_driver(test_db)
def test_upsert_team(test_db)
def test_upsert_race_result(test_db)
def test_find_driver_by_name(test_db)
# ... etc
```

**Success Criteria**:
- All tests pass
- 100% coverage
- Database operations efficient (proper indexing)

---

### Task 2.5: Database - URL Caching
**Goal**: Implement URL cache checking logic

**Location**: `src/database.py` (add methods)

**Requirements**:
- `is_url_cached(url: str, entity_type: str, max_age_days: int) -> bool`
- Check if URL exists and is fresh
- Support all entity types (league, series, season, race, driver, team)
- Efficient queries using indexes

**Acceptance Criteria**:
- [ ] Returns True for fresh URLs
- [ ] Returns False for expired URLs
- [ ] Returns False for non-existent URLs
- [ ] Works for all entity types

**Tests** (`tests/unit/test_database.py`):
```python
def test_is_url_cached_fresh_returns_true(test_db)
def test_is_url_cached_expired_returns_false(test_db)
def test_is_url_cached_missing_returns_false(test_db)
def test_is_url_cached_all_entity_types(test_db)
```

**Success Criteria**:
- All tests pass
- 100% coverage
- Queries use indexes (check EXPLAIN QUERY PLAN)

---

### Task 2.6: Database - Logging
**Goal**: Implement scrape logging

**Location**: `src/database.py` (add methods)

**Requirements**:
- `log_scrape(entity_type: str, entity_url: str, status: str, error_msg: str = None, duration_ms: int = None)`
- Insert into `scrape_log` table
- Support success, failed, skipped statuses

**Acceptance Criteria**:
- [ ] Logs scrape attempts
- [ ] Captures errors
- [ ] Records duration

**Tests** (`tests/unit/test_database.py`):
```python
def test_log_scrape_success(test_db)
def test_log_scrape_failure_with_error(test_db)
def test_log_scrape_skipped(test_db)
```

**Success Criteria**:
- All tests pass
- 100% coverage

---

### Task 2.7: Schema Validator - Core Structure
**Goal**: Create schema validator with expected schemas

**Location**: `src/schema_validator.py`

**Requirements**:
- Create `SchemaValidator` class
- Define `EXPECTED_SCHEMAS` constant with patterns for:
  - `league_series` (JavaScript patterns)
  - `series_seasons` (JavaScript patterns)
  - `race_results_table` (HTML table columns)
  - `driver_profile` (required fields)
  - `teams_page` (structure)
- Create `SchemaChangeDetected` exception

**Acceptance Criteria**:
- [ ] Class structure defined
- [ ] Expected schemas documented
- [ ] Exception class created

**Tests** (`tests/unit/test_schema_validator.py`):
```python
def test_schema_change_detected_exception()
def test_expected_schemas_structure()
```

**Success Criteria**:
- All tests pass
- 100% coverage

---

### Task 2.8: Schema Validator - JavaScript Validation
**Goal**: Implement JavaScript pattern validation

**Location**: `src/schema_validator.py` (add method)

**Requirements**:
- `validate_javascript_data(entity_type: str, script_content: str) -> bool`
- Check all patterns exist in script
- Raise `SchemaChangeDetected` if pattern missing
- Clear error messages

**Acceptance Criteria**:
- [ ] Valid JavaScript passes
- [ ] Missing patterns raise exception
- [ ] Error messages are descriptive

**Tests** (`tests/unit/test_schema_validator.py`):
```python
def test_validate_javascript_valid_series_data()
def test_validate_javascript_valid_season_data()
def test_validate_javascript_missing_pattern_raises_error()
def test_validate_javascript_error_message_descriptive()
```

**Success Criteria**:
- All tests pass with real fixtures
- 100% coverage
- Tests use actual HTML from fixtures

---

### Task 2.9: Schema Validator - Data Field Validation
**Goal**: Implement extracted data validation

**Location**: `src/schema_validator.py` (add method)

**Requirements**:
- `validate_extracted_data(entity_type: str, data: dict) -> bool`
- Check all required fields present
- Raise `SchemaChangeDetected` if field missing

**Acceptance Criteria**:
- [ ] Valid data passes
- [ ] Missing fields raise exception
- [ ] Works for all entity types

**Tests** (`tests/unit/test_schema_validator.py`):
```python
def test_validate_data_valid_league()
def test_validate_data_valid_series()
def test_validate_data_missing_field_raises_error()
def test_validate_data_all_entity_types()
```

**Success Criteria**:
- All tests pass
- 100% coverage

---

### Task 2.10: Schema Validator - Table Structure Validation
**Goal**: Implement HTML table validation

**Location**: `src/schema_validator.py` (add method)

**Requirements**:
- `validate_table_structure(entity_type: str, table: BeautifulSoup) -> bool`
- Check minimum column count
- Check required columns present
- Raise `SchemaChangeDetected` if structure invalid

**Acceptance Criteria**:
- [ ] Valid tables pass
- [ ] Missing columns raise exception
- [ ] Insufficient columns raise exception

**Tests** (`tests/unit/test_schema_validator.py`):
```python
def test_validate_table_valid_race_results()
def test_validate_table_missing_column_raises_error()
def test_validate_table_insufficient_columns_raises_error()
```

**Success Criteria**:
- All tests pass with real fixtures
- 100% coverage

---

### Task 2.11: JavaScript Parser Utilities
**Goal**: Create utilities to extract JavaScript data

**Location**: `src/utils/js_parser.py`

**Requirements**:
- `extract_js_array(html: str, var_name: str, pattern: str) -> List[dict]`
- `extract_series_data(html: str) -> List[dict]`
- `extract_season_data(html: str) -> List[dict]`
- Handle malformed JavaScript gracefully
- Return empty list if not found

**Acceptance Criteria**:
- [ ] Extracts series.push() calls
- [ ] Extracts seasons = [...] array
- [ ] Handles missing JavaScript
- [ ] Parses JSON correctly

**Tests** (`tests/unit/test_js_parser.py`):
```python
def test_extract_series_data_from_fixture()
def test_extract_season_data_from_fixture()
def test_extract_js_array_missing_returns_empty()
def test_extract_js_array_malformed_raises_error()
```

**Success Criteria**:
- All tests pass with real fixtures
- 100% coverage
- Extracts data from actual SimRacerHub HTML

---

### Task 2.12: Base Extractor - HTTP & Parsing
**Goal**: Create base extractor with HTTP and parsing

**Location**: `src/extractors/base.py`

**Requirements**:
- `BaseExtractor` class
- `fetch_page(url: str) -> BeautifulSoup` with rate limiting
- `_rate_limit()` enforces delay
- `extract_text(soup, selector) -> str`
- `extract_all_text(soup, selector) -> List[str]`
- Retry logic with exponential backoff
- Timeout handling

**Acceptance Criteria**:
- [ ] Fetches and parses HTML
- [ ] Rate limiting enforced
- [ ] Retries on failure
- [ ] Timeouts handled
- [ ] Context manager support

**Tests** (`tests/unit/test_base_extractor.py`):
```python
def test_fetch_page_success(mocker)
def test_fetch_page_rate_limiting(mocker)
def test_fetch_page_retry_on_failure(mocker)
def test_fetch_page_max_retries_exceeded(mocker)
def test_extract_text(mocker)
def test_extract_all_text(mocker)
def test_context_manager()
```

**Success Criteria**:
- All tests pass
- 100% coverage
- Mocked HTTP requests (no real requests in tests)
- Rate limiting verified with timing

---

## Phase 3: Entity Extraction

### Task 3.1: League Extractor
**Goal**: Extract league metadata and child URLs

**Location**: `src/extractors/league.py`

**Requirements**:
- `LeagueExtractor(BaseExtractor)` class
- `extract(url: str) -> dict` method
- Extract league name, description
- Extract series URLs from JavaScript
- Extract teams URL
- Validate with SchemaValidator

**Acceptance Criteria**:
- [ ] Extracts league metadata
- [ ] Extracts all series IDs
- [ ] Returns proper structure: {metadata, child_urls}
- [ ] Schema validation integrated

**Tests** (`tests/unit/test_league_extractor.py`):
```python
def test_extract_league_from_fixture()
def test_extract_returns_correct_structure()
def test_extract_all_series_urls()
def test_extract_validates_schema()
def test_extract_invalid_html_raises_error()
```

**Fixtures Needed**:
- `tests/fixtures/league_series_1558.html`

**Success Criteria**:
- All tests pass with real fixture
- 100% coverage
- Extracts 4 series from OBRL fixture

---

### Task 3.2: Series Extractor
**Goal**: Extract series metadata and season URLs

**Location**: `src/extractors/series.py`

**Requirements**:
- `SeriesExtractor(BaseExtractor)` class
- `extract(url: str) -> dict` method
- Extract series name, metadata
- Extract seasons from JavaScript array
- Use `js_parser.extract_season_data()`
- Validate with SchemaValidator

**Acceptance Criteria**:
- [ ] Extracts series metadata
- [ ] Extracts all season data from JS
- [ ] Returns proper structure
- [ ] Schema validation integrated

**Tests** (`tests/unit/test_series_extractor.py`):
```python
def test_extract_series_from_fixture()
def test_extract_all_seasons()
def test_extract_season_count_matches()
def test_extract_validates_schema()
def test_extract_missing_js_raises_error()
```

**Fixtures Needed**:
- `tests/fixtures/series_seasons_3714.html`

**Success Criteria**:
- All tests pass with real fixture
- 100% coverage
- Extracts 28+ seasons from fixture

---

### Task 3.3: Season Extractor
**Goal**: Extract season metadata and race URLs

**Location**: `src/extractors/season.py`

**Requirements**:
- `SeasonExtractor(BaseExtractor)` class
- `extract(url: str) -> dict` method
- Extract season name, dates, status
- Extract race URLs from race list page
- Determine season status (active/completed)

**Acceptance Criteria**:
- [ ] Extracts season metadata
- [ ] Extracts race URLs
- [ ] Determines status correctly
- [ ] Returns proper structure

**Tests** (`tests/unit/test_season_extractor.py`):
```python
def test_extract_season_from_fixture()
def test_extract_all_race_urls()
def test_determine_season_status_active()
def test_determine_season_status_completed()
```

**Fixtures Needed**:
- `tests/fixtures/season_race_3714.html` (race list)

**Success Criteria**:
- All tests pass with real fixture
- 100% coverage

---

### Task 3.4: Race Extractor
**Goal**: Extract race metadata and results

**Location**: `src/extractors/race.py`

**Requirements**:
- `RaceExtractor(BaseExtractor)` class
- `extract(url: str) -> dict` method
- Extract race metadata (track, date, conditions)
- Extract results table (all 20+ columns)
- Parse timing data correctly
- Validate table structure

**Acceptance Criteria**:
- [ ] Extracts race metadata
- [ ] Extracts all result rows
- [ ] Parses all 20+ columns correctly
- [ ] Handles missing data gracefully
- [ ] Schema validation on table

**Tests** (`tests/unit/test_race_extractor.py`):
```python
def test_extract_race_from_fixture()
def test_extract_all_results()
def test_extract_result_columns_correct()
def test_extract_timing_data_parsed()
def test_extract_validates_table_structure()
def test_extract_handles_missing_data()
```

**Fixtures Needed**:
- `tests/fixtures/season_race_324462.html` (race detail)

**Success Criteria**:
- All tests pass with real fixture
- 100% coverage
- Extracts 40 results from fixture

---

### Task 3.5: Driver Extractor
**Goal**: Extract driver profile data

**Location**: `src/extractors/driver.py`

**Requirements**:
- `DriverExtractor(BaseExtractor)` class
- `extract(url: str) -> dict` method
- Extract driver name, iRacing info
- Extract car numbers
- Parse club info

**Acceptance Criteria**:
- [ ] Extracts driver metadata
- [ ] Parses iRacing data
- [ ] Returns proper structure

**Tests** (`tests/unit/test_driver_extractor.py`):
```python
def test_extract_driver_from_fixture()
def test_extract_iracing_data()
def test_extract_car_numbers()
def test_extract_club_info()
```

**Fixtures Needed**:
- `tests/fixtures/driver_stats_33132.html`

**Success Criteria**:
- All tests pass with real fixture
- 100% coverage

---

### Task 3.6: Team Extractor
**Goal**: Extract team roster data

**Location**: `src/extractors/team.py`

**Requirements**:
- `TeamExtractor(BaseExtractor)` class
- `extract(url: str) -> dict` method
- Extract all teams
- Extract driver names per team
- Parse team IDs

**Acceptance Criteria**:
- [ ] Extracts all teams
- [ ] Extracts driver lists
- [ ] Returns proper structure

**Tests** (`tests/unit/test_team_extractor.py`):
```python
def test_extract_teams_from_fixture()
def test_extract_team_count()
def test_extract_driver_names()
def test_extract_team_ids()
```

**Fixtures Needed**:
- `tests/fixtures/teams_1558.html`

**Success Criteria**:
- All tests pass with real fixture
- 100% coverage
- Extracts 15 teams from fixture

---

## Phase 4: Orchestration

### Task 4.1: Orchestrator - Core Structure
**Goal**: Create orchestrator with basic structure

**Location**: `src/orchestrator.py`

**Requirements**:
- `Orchestrator` class
- Initialize with Database and SchemaValidator
- Progress tracking structure
- Error handling framework

**Acceptance Criteria**:
- [ ] Class structure defined
- [ ] Dependencies injected
- [ ] Progress tracker initialized

**Tests** (`tests/unit/test_orchestrator.py`):
```python
def test_orchestrator_initialization()
def test_orchestrator_with_dependencies()
```

**Success Criteria**:
- All tests pass
- 100% coverage

---

### Task 4.2: Orchestrator - League Scraping
**Goal**: Implement league scraping logic

**Location**: `src/orchestrator.py` (add method)

**Requirements**:
- `scrape_league(league_url: str, depth: str) -> dict` method
- Check URL cache
- Call LeagueExtractor
- Store in database
- Handle errors
- Return summary

**Acceptance Criteria**:
- [ ] Scrapes league metadata
- [ ] Checks cache first
- [ ] Stores in database
- [ ] Returns statistics

**Tests** (`tests/integration/test_orchestrator.py`):
```python
def test_scrape_league_new(test_db, mocker)
def test_scrape_league_cached_skips(test_db, mocker)
def test_scrape_league_stores_data(test_db, mocker)
def test_scrape_league_handles_error(test_db, mocker)
```

**Success Criteria**:
- All tests pass
- 100% coverage
- Uses mocked HTTP

---

### Task 4.3: Orchestrator - Series Scraping
**Goal**: Implement series scraping with depth control

**Location**: `src/orchestrator.py` (add method)

**Requirements**:
- `scrape_series(series_url: str, league_id: int, depth: str)` method
- Check cache
- Call SeriesExtractor
- Store in database
- Recurse to seasons if depth allows

**Acceptance Criteria**:
- [ ] Scrapes series
- [ ] Respects depth control
- [ ] Caches properly

**Tests** (`tests/integration/test_orchestrator.py`):
```python
def test_scrape_series_depth_series(test_db, mocker)
def test_scrape_series_depth_season(test_db, mocker)
def test_scrape_series_cached(test_db, mocker)
```

**Success Criteria**:
- All tests pass
- 100% coverage

---

### Task 4.4: Orchestrator - Season & Race Scraping
**Goal**: Complete scraping hierarchy

**Location**: `src/orchestrator.py` (add methods)

**Requirements**:
- `scrape_season(season_url: str, series_id: int, depth: str)`
- `scrape_race(race_url: str, season_id: int)`
- Full hierarchy working

**Acceptance Criteria**:
- [ ] Scrapes seasons and races
- [ ] Full depth works
- [ ] All data linked correctly

**Tests** (`tests/integration/test_orchestrator.py`):
```python
def test_scrape_season(test_db, mocker)
def test_scrape_race(test_db, mocker)
def test_full_hierarchy_scrape(test_db, mocker)
```

**Success Criteria**:
- All tests pass
- 100% coverage
- Full workflow tested

---

### Task 4.5: Orchestrator - Driver & Team Resolution
**Goal**: Implement driver name resolution

**Location**: `src/orchestrator.py` (add methods)

**Requirements**:
- `scrape_teams(teams_url: str, league_id: int)`
- `scrape_drivers(driver_urls: List[str], league_id: int)`
- `resolve_driver_names(team_roster: List[str])`
- Match team names to driver IDs from race results

**Acceptance Criteria**:
- [ ] Scrapes teams
- [ ] Resolves driver names
- [ ] Links teams to drivers

**Tests** (`tests/integration/test_orchestrator.py`):
```python
def test_scrape_teams(test_db, mocker)
def test_resolve_driver_names(test_db, mocker)
def test_link_teams_to_drivers(test_db, mocker)
```

**Success Criteria**:
- All tests pass
- 100% coverage

---

## Phase 5: CLI & Export

### Task 5.1: CLI - Enhanced Commands
**Goal**: Implement all CLI commands

**Location**: `src/cli.py` (update)

**Requirements**:
- `scrape-league` command with depth
- `scrape-series` command
- `scrape-season` command
- `scrape-race` command
- `status` command
- `export` command
- Progress output

**Acceptance Criteria**:
- [ ] All commands work
- [ ] Help text clear
- [ ] Progress displayed

**Tests** (`tests/integration/test_cli.py`):
```python
def test_cli_scrape_league(tmp_path)
def test_cli_status(tmp_path)
def test_cli_export(tmp_path)
def test_cli_help()
```

**Success Criteria**:
- All tests pass
- 100% coverage

---

### Task 5.2: Export Functionality
**Goal**: Implement JSON/CSV export

**Location**: `src/exporter.py`

**Requirements**:
- `export_league(league_id: int, format: str, output_path: str)`
- Export to JSON
- Export to CSV (flattened)
- Support filtering by series/season

**Acceptance Criteria**:
- [ ] JSON export works
- [ ] CSV export works
- [ ] Filtering works

**Tests** (`tests/unit/test_exporter.py`):
```python
def test_export_json(test_db, tmp_path)
def test_export_csv(test_db, tmp_path)
def test_export_with_filters(test_db, tmp_path)
```

**Success Criteria**:
- All tests pass
- 100% coverage

---

## Phase 6: Production Readiness

### Task 6.1: Fixture Capture
**Goal**: Capture all HTML fixtures from SimRacerHub

**Location**: `tests/fixtures/`

**Requirements**:
- Capture league page
- Capture series page
- Capture season/race list page
- Capture race detail page
- Capture driver profile
- Capture teams page
- Document capture date in README

**Acceptance Criteria**:
- [ ] All fixtures captured
- [ ] README documents fixtures
- [ ] Fixtures sanitized if needed

**Success Criteria**:
- All fixtures available
- README complete

---

### Task 6.2: End-to-End Test
**Goal**: Test complete OBRL scrape

**Location**: `tests/integration/test_e2e.py`

**Requirements**:
- Test full league scrape with real fixtures
- Verify all data linked correctly
- Verify cache works on re-scrape
- Verify export works

**Acceptance Criteria**:
- [ ] E2E test passes
- [ ] All entities created
- [ ] Relationships correct

**Tests** (`tests/integration/test_e2e.py`):
```python
def test_full_obrl_scrape(test_db, mocker)
def test_re_scrape_uses_cache(test_db, mocker)
def test_export_complete_data(test_db, tmp_path, mocker)
```

**Success Criteria**:
- All tests pass
- 100% overall coverage maintained

---

### Task 6.3: CI/CD Setup
**Goal**: Set up GitHub Actions

**Location**: `.github/workflows/test.yml`

**Requirements**:
- Test on Python 3.10, 3.11, 3.12
- Run linting (black, ruff, mypy)
- Run tests with coverage
- Fail if coverage < 100%
- Upload coverage to Codecov

**Acceptance Criteria**:
- [ ] Workflow file created
- [ ] Tests run on push/PR
- [ ] Coverage enforced

**Success Criteria**:
- Workflow passes
- Coverage badge added to README

---

### Task 6.4: Documentation
**Goal**: Complete all documentation

**Location**: Various

**Requirements**:
- Update README with usage examples
- Add docstrings to all public methods
- Create user guide
- Document configuration options

**Acceptance Criteria**:
- [ ] README complete
- [ ] All functions documented
- [ ] User guide exists

**Success Criteria**:
- Documentation comprehensive
- Examples tested

---

### Task 6.5: Production Test with OBRL
**Goal**: Run full scrape against real OBRL league

**Location**: Manual test

**Requirements**:
- Request permission from SimRacerHub (if not already granted)
- Run full scrape with rate limiting
- Monitor for errors
- Verify data integrity
- Verify schema validation works

**Acceptance Criteria**:
- [ ] Permission granted
- [ ] Full scrape completes
- [ ] No data loss
- [ ] Schema validation working

**Success Criteria**:
- Complete OBRL data in database
- Export successful
- Zero integrity errors

---

## Task Tracking

### Current Phase: Phase 2
### Current Task: Task 2.1
### Completed Tasks: 0/35

## Quick Reference

### Task Template

When starting a task:

1. **Read requirements** from this file
2. **Create working directory**: `claude_temp/task_X_Y/`
3. **Develop solution** in working directory
4. **Write tests** in `tests/`
5. **Capture fixtures** if needed
6. **Run tests**: `uv run pytest tests/unit/test_module.py -v`
7. **Check coverage**: `uv run pytest --cov=src/module.py`
8. **Format**: `uv run black src/module.py`
9. **Lint**: `uv run ruff check src/module.py`
10. **Move to src/**: Copy working code to `src/`
11. **Verify**: Run all tests again
12. **Clean up**: Remove working directory

### Running Tests

```bash
# Run specific test file
uv run pytest tests/unit/test_database.py -v

# Run with coverage
uv run pytest tests/unit/test_database.py --cov=src/database.py --cov-report=term-missing

# Run all tests
uv run pytest

# Watch mode (during development)
uv run ptw tests/unit/test_database.py -- --cov=src/database.py
```

### Quality Checks

```bash
# Format code
uv run black src/

# Lint
uv run ruff check src/

# Type check
uv run mypy src/

# All checks
uv run black src/ && uv run ruff check src/ && uv run mypy src/ && uv run pytest
```

## Notes

- Each task builds on previous tasks
- Don't move to next task until current is 100% complete
- If stuck, revise task requirements (don't skip)
- Maintain 100% coverage throughout
- Use real fixtures for realistic tests
- Never hardcode expected results in tests
