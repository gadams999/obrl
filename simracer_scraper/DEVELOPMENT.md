# SimRacer Scraper - Development Guide

**Last Updated**: 2025-11-06

## Quick Start

### Installation

```bash
cd simracer_scraper

# Install dependencies with uv (recommended)
uv sync --all-extras

# Install Playwright browsers
uv run playwright install chromium

# Or install with pip (fallback)
python3 -m venv .venv
source .venv/bin/activate
pip install -r requirements.txt
playwright install chromium
```

### Running Tests

```bash
# Run all tests
uv run pytest

# Run with coverage
uv run pytest --cov=src --cov-report=term-missing

# Run specific test file
uv run pytest tests/unit/test_database.py

# Run with verbose output
uv run pytest -v
```

### Code Quality

```bash
# Format code
uv run black src/

# Lint
uv run ruff check src/

# Type check
uv run mypy src/
```

## CLI Reference

### Basic Usage

```bash
# Simple usage with config.yaml
python scraper.py

# Or use the module directly
uv run simracer-scraper scrape league 1558

# With uv
uv run python -m src.cli scrape league 1558
```

### Scraping Commands

```bash
# Scrape a league (league metadata only)
python scraper.py scrape league 1558

# Scrape with depth control
python scraper.py scrape all --league 1558 --depth league   # League only
python scraper.py scrape all --league 1558 --depth series   # League + series
python scraper.py scrape all --league 1558 --depth season   # League + series + seasons
python scraper.py scrape all --league 1558 --depth race     # Full scrape (default)

# Force refresh (ignore cache)
python scraper.py scrape league 1558 --force

# Custom database
python scraper.py scrape league 1558 --db my_league.db

# Custom config
python scraper.py scrape league 1558 --config my_config.yaml

# Enable debug logging
python scraper.py scrape league 1558 --log-level DEBUG
```

### Using config.yaml

Edit `config.yaml`:
```yaml
league:
  id: 1558              # Your league ID
  depth: race           # How deep to scrape
  database: obrl.db     # Database file

scraping:
  request_delay: 2.0    # Seconds between requests
  max_retries: 3
  timeout: 10

logging:
  level: INFO
```

Then simply run:
```bash
python scraper.py
```

## Testing Guide

### Core Testing Principles

#### 1. Never Hardcode Test Results ❌

**WRONG**:
```python
def test_extract_series():
    result = extract_series(html)
    assert result == [
        {"id": 3714, "name": "Wednesday Night Series"},
        {"id": 3713, "name": "Thursday Night Series"},
    ]  # Hardcoded expectations
```

**RIGHT**:
```python
def test_extract_series():
    """Test that series extractor finds all series in fixture."""
    html = load_fixture("league_series_1558.html")
    result = extract_series(html)

    # Verify structure and behavior
    assert isinstance(result, list), "Should return a list"
    assert len(result) > 0, "Should find at least one series"
    assert all('id' in series for series in result)
    assert all('name' in series for series in result)
    assert all(isinstance(series['id'], int) for series in result)
```

**Why**: Hardcoded tests pass even when code is broken. Tests should verify extraction logic, not data.

#### 2. Test Behavior, Not Data ✅

**What to Test**:
- ✅ Data types are correct
- ✅ Required fields are present
- ✅ Structure matches expectations
- ✅ Parsing logic works
- ✅ Edge cases handled
- ✅ Errors raised when appropriate

**What NOT to Test**:
- ❌ Exact string values from website
- ❌ Exact number of items (unless meaningful)
- ❌ Specific data that might change

#### 3. Tests Must Fail When Code is Broken ✅

Tests are worthless if they pass regardless of correctness.

**Good Test** (will fail if extractor breaks):
```python
def test_extract_race_results():
    """Test extracting race results from actual race page."""
    html = load_fixture("season_race_324462.html")
    result = extract_race_results(html)

    # Verify structure
    assert isinstance(result, list)
    assert len(result) > 0

    # Verify required fields
    required_fields = ['driver_name', 'finish_position', 'laps_completed', 'points']
    for r in result:
        for field in required_fields:
            assert field in r, f"Missing field: {field}"

    # Verify data types
    first = result[0]
    assert isinstance(first['finish_position'], int)
    assert isinstance(first['laps_completed'], int)
```

#### 4. Mock External Dependencies ✅

Always mock:
- HTTP requests (use `pytest-mock` or `responses`)
- Database connections (use in-memory SQLite)
- File I/O (use `tmp_path` fixture)
- Time/dates
- Random values

**Example**:
```python
def test_fetch_page_with_retry(mocker):
    """Test that fetch_page retries on failure."""
    mock_get = mocker.patch('requests.Session.get')
    mock_get.side_effect = [
        requests.exceptions.Timeout(),  # First call fails
        requests.exceptions.Timeout(),  # Second call fails
        mocker.Mock(status_code=200, content=b"<html>Success</html>")  # Third succeeds
    ]

    extractor = BaseExtractor()
    result = extractor.fetch_page("http://example.com")

    # Verify retry behavior
    assert mock_get.call_count == 3
    assert result is not None
```

### Test-Driven Development Workflow

1. **Write failing test first**
   ```python
   def test_extract_league_name():
       html = load_fixture("league_series_1558.html")
       result = extract_league_name(html)
       assert isinstance(result, str)
       assert len(result) > 0
   ```

2. **Run test - it should fail**
   ```bash
   uv run pytest tests/unit/test_league_extractor.py::test_extract_league_name
   # FAILED - function doesn't exist yet
   ```

3. **Implement minimum code to pass**
   ```python
   def extract_league_name(html: str) -> str:
       soup = BeautifulSoup(html, 'lxml')
       return soup.select_one('h1').get_text(strip=True)
   ```

4. **Run test - should pass**
   ```bash
   uv run pytest tests/unit/test_league_extractor.py::test_extract_league_name
   # PASSED
   ```

5. **Add edge case tests and refactor**

### Coverage Requirements

**Target**: 100% coverage (enforced by `--cov-fail-under=100`)

```bash
# Check coverage for specific file
uv run pytest tests/unit/test_module.py --cov=src/module.py --cov-report=term-missing

# Check all coverage with HTML report
uv run pytest --cov=src --cov-report=html
```

What counts:
- ✅ All functions tested
- ✅ All branches tested (if/else)
- ✅ All exception paths tested
- ✅ All class methods tested

### Test Organization

```
tests/
├── conftest.py                 # Shared fixtures
├── fixtures/                   # HTML fixtures (not version controlled)
│   └── README.md
└── unit/                       # Unit tests
    ├── test_base_extractor.py
    ├── test_database.py
    ├── test_orchestrator.py
    └── ...
```

**Naming Conventions**:
- Test files: `test_<module>.py`
- Test classes: `Test<ClassName>`
- Test functions: `test_<function>_<scenario>`

### Common Test Patterns

#### Testing Extractors
```python
def test_extractor_with_real_fixture():
    """Test extractor with actual HTML from site."""
    html = load_fixture("page.html")
    extractor = MyExtractor()

    result = extractor.extract(html)

    # Verify structure
    assert 'metadata' in result
    assert 'child_urls' in result
    assert isinstance(result['metadata'], dict)
    assert isinstance(result['child_urls'], list)

    # Verify required fields
    required_fields = ['id', 'name', 'url']
    for field in required_fields:
        assert field in result['metadata']
```

#### Testing Database Operations
```python
def test_database_operation(test_db):
    """Test database CRUD operation."""
    # Insert
    entity_id = test_db.upsert_entity(123, {'name': 'Test', 'url': 'http://test', 'scraped_at': '2025-01-01'})
    assert entity_id == 123

    # Read
    result = test_db.get_entity(123)
    assert result is not None
    assert result['name'] == 'Test'

    # Update
    test_db.upsert_entity(123, {'name': 'Updated', 'url': 'http://test', 'scraped_at': '2025-01-02'})
    result = test_db.get_entity(123)
    assert result['name'] == 'Updated'
```

#### Testing Error Handling
```python
def test_error_handling(mocker):
    """Test that errors are handled correctly."""
    mock_request = mocker.patch('requests.Session.get')
    mock_request.side_effect = requests.exceptions.Timeout()

    extractor = BaseExtractor(max_retries=2)

    with pytest.raises(requests.exceptions.Timeout):
        extractor.fetch_page("http://example.com")

    # Verify retries occurred
    assert mock_request.call_count == 3  # Initial + 2 retries
```

### Checklist for Each Test

- [ ] Test has clear docstring
- [ ] Test uses real fixtures (not hardcoded HTML)
- [ ] Test verifies behavior, not exact data
- [ ] Test will fail if implementation is broken
- [ ] Test uses appropriate mocks for external dependencies
- [ ] Test checks data types and structure
- [ ] Test checks error conditions
- [ ] Test has meaningful assertions with messages
- [ ] Test is independent (can run alone)
- [ ] Test is deterministic (no random failures)

## Development Workflow

### Setup Development Environment

```bash
# 1. Clone repository
cd simracer_scraper

# 2. Install dependencies
uv sync --all-extras

# 3. Install Playwright browsers
uv run playwright install chromium

# 4. Copy configuration template
cp config.yaml.example config.yaml

# 5. Verify installation
uv run pytest --version
uv run python -c "import src; print('OK')"
```

### Development Cycle

```bash
# 1. Write code
# 2. Write tests (unit tests first)

# 3. Run specific test
uv run pytest tests/unit/test_new_feature.py

# 4. Check coverage
uv run pytest --cov=src/new_module.py --cov-report=term-missing

# 5. Format & lint
uv run black src/
uv run ruff check src/

# 6. Type check
uv run mypy src/

# 7. Run all tests
uv run pytest

# 8. Verify 100% coverage (enforced)
# Should pass due to --cov-fail-under=100 in pyproject.toml
```

### Git Workflow

```bash
# Run tests before committing
uv run pytest

# Format code
uv run black src/ tests/

# Commit
git add .
git commit -m "feat: add new feature"
```

## Architecture Overview

### Core Components

1. **Orchestrator** (`src/orchestrator.py`)
   - Coordinates hierarchical scraping
   - Implements depth control
   - Smart caching
   - Progress tracking

2. **Extractors** (`src/extractors/`)
   - `base.py`: Rate limiting, HTTP, parsing
   - `league.py`: League + teams + drivers
   - `series.py`: Series (JavaScript parsing)
   - `season.py`: Seasons (JavaScript parsing)
   - `race.py`: Race results (HTML table parsing)

3. **Database** (`src/database.py`)
   - SQLite connection management
   - Schema initialization
   - CRUD operations
   - Caching logic

4. **Browser Manager** (`src/utils/browser_manager.py`)
   - Shared Playwright instance
   - Sequential execution
   - Rate limiting

5. **JavaScript Parser** (`src/utils/js_parser.py`)
   - Extract data from JavaScript
   - Parse `series.push()` patterns
   - Parse `seasons = []` arrays

### Data Flow

```
CLI → Orchestrator → Extractor → BaseExtractor → BrowserManager → SimRacerHub
                ↓
            Database (caching)
                ↓
        Schema Validator
```

## Configuration

### config.yaml

```yaml
league:
  id: 1558              # Default league ID
  depth: race           # How deep to scrape (league, series, season, race)
  database: obrl.db     # Database file path

scraping:
  request_delay: 2.0    # Seconds between requests (minimum)
  max_retries: 3        # Retry attempts
  timeout: 10           # Request timeout

logging:
  level: INFO           # DEBUG, INFO, WARNING, ERROR
```

### Configuration via YAML

All configuration is done through `config.yaml`. See the Configuration section in README.md for details.

Example `config.yaml`:
```yaml
league:
  id: 1558
  depth: race
  database: obrl.db

scraping:
  user_agent: "SimRacerScraper/1.0 (Educational/Personal Use)"

logging:
  level: INFO
```

## Common Tasks

### Add a New Extractor

1. Create test file:
   ```bash
   touch tests/unit/test_my_extractor.py
   ```

2. Write failing tests

3. Create extractor:
   ```bash
   touch src/extractors/my_extractor.py
   ```

4. Implement extractor class inheriting from `BaseExtractor`

5. Run tests and iterate

6. Ensure 100% coverage

### Capture HTML Fixtures

```bash
# Use browser dev tools to save complete HTML
# Save to tests/fixtures/ with descriptive name
# Document in tests/fixtures/README.md
```

### Debug a Scraping Issue

```bash
# Enable debug logging
python scraper.py scrape league 1558 --log-level DEBUG

# Check database for errors
sqlite3 obrl.db "SELECT * FROM scrape_log WHERE status='failed' ORDER BY timestamp DESC LIMIT 10"

# Check schema alerts
sqlite3 obrl.db "SELECT * FROM schema_alerts WHERE resolved=0 ORDER BY timestamp DESC"
```

### Add Database Fields

1. Update schema in `src/database.py` (SQL CREATE TABLE)
2. Update upsert methods
3. Update tests
4. Run `initialize_schema()` on new database or migrate existing

## Troubleshooting

### Tests Failing

```bash
# Run with verbose output
uv run pytest -v

# Run single test
uv run pytest tests/unit/test_module.py::test_function

# Show print statements
uv run pytest -s

# Stop on first failure
uv run pytest -x
```

### Coverage Not 100%

```bash
# See which lines are missing
uv run pytest --cov=src --cov-report=term-missing

# Generate HTML report
uv run pytest --cov=src --cov-report=html
# Open htmlcov/index.html
```

### Playwright Issues

```bash
# Reinstall browsers
uv run playwright install chromium

# Run with headed browser (for debugging)
# Modify browser_manager.py temporarily to set headless=False
```

### Database Locked

```bash
# Close all connections to database
# Check for other processes:
lsof obrl.db

# Or delete and reinitialize
rm obrl.db
python scraper.py scrape league 1558
```

## Best Practices

1. **Always write tests first** (TDD)
2. **Use real HTML fixtures** (not mocked)
3. **Never hardcode test data**
4. **Mock external dependencies** (HTTP, file I/O)
5. **Keep functions small and focused**
6. **Use type hints** (Python 3.10+ style)
7. **Format with black** (line-length: 100)
8. **Document with docstrings**
9. **100% test coverage** (enforced)
10. **Meaningful commit messages**

## Resources

- **pytest**: https://docs.pytest.org/
- **BeautifulSoup**: https://www.crummy.com/software/BeautifulSoup/bs4/doc/
- **Playwright**: https://playwright.dev/python/
- **SQLite**: https://www.sqlite.org/docs.html
- **Black**: https://black.readthedocs.io/
- **Ruff**: https://beta.ruff.rs/docs/
- **mypy**: https://mypy.readthedocs.io/

## Summary

**Key Points**:
- TDD workflow (tests first)
- 100% coverage requirement
- Never hardcode test data
- Test behavior, not exact values
- Mock external dependencies
- Use real HTML fixtures
- Run black/ruff/mypy before committing
