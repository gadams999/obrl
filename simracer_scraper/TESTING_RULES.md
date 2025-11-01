# Testing Rules & Standards

**Version**: 1.0.0
**Date**: 2025-10-31
**Status**: Mandatory

## Core Principles

### 1. ❌ Never Hardcode Test Results

**WRONG**:
```python
def test_extract_series():
    result = extract_series(html)
    assert result == [
        {"id": 3714, "name": "Wednesday Night Series"},
        {"id": 3713, "name": "Thursday Night Series"},
        # ... exact data hardcoded
    ]
```

**WHY IT'S WRONG**:
- Test will pass even if extractor is broken
- Won't detect if site structure changes
- Can't verify extractor logic is correct
- Essentially testing the test data, not the code

**RIGHT**:
```python
def test_extract_series():
    """Test that series extractor finds all series in fixture."""
    html = load_fixture("league_series_1558.html")
    result = extract_series(html)

    # Verify structure and behavior
    assert isinstance(result, list), "Should return a list"
    assert len(result) > 0, "Should find at least one series"
    assert all('id' in series for series in result), "All series should have IDs"
    assert all('name' in series for series in result), "All series should have names"
    assert all(isinstance(series['id'], int) for series in result), "IDs should be integers"

    # Can verify specific count if we know fixture has 4 series
    assert len(result) == 4, "OBRL fixture should have 4 series"

    # Can verify IDs are what we expect from the fixture
    series_ids = {s['id'] for s in result}
    assert 3714 in series_ids, "Should find Wednesday series (3714)"
```

**WHY IT'S RIGHT**:
- Tests extraction logic works on real HTML
- Will fail if extraction pattern breaks
- Will fail if site structure changes
- Verifies data types and structure
- Can still check specific values if meaningful

---

### 2. ✅ Test Real Behavior with Real Fixtures

**Principles**:
- Use actual HTML captured from SimRacerHub
- Tests verify extraction logic works on real data
- If site changes, tests SHOULD fail (that's the point!)
- Schema validator should catch changes first

**Example**:
```python
def test_extract_race_results():
    """Test extracting race results from actual race page."""
    html = load_fixture("season_race_324462.html")
    result = extract_race_results(html)

    # Verify structure
    assert isinstance(result, list)
    assert len(result) > 0, "Should extract at least one result"

    # Verify each result has required fields
    required_fields = ['driver_name', 'finish_position', 'laps_completed', 'points']
    for r in result:
        for field in required_fields:
            assert field in r, f"Result missing field: {field}"

    # Verify data types
    first_result = result[0]
    assert isinstance(first_result['finish_position'], int)
    assert isinstance(first_result['laps_completed'], int)
    assert isinstance(first_result['points'], (int, float))

    # Can check known fixture characteristics
    assert len(result) <= 50, "Should not have more than 50 results in a race"
    assert all(r['finish_position'] > 0 for r in result), "Positions should be positive"
```

---

### 3. 🎯 Test Structure and Logic, Not Exact Data

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

**Example**:
```python
def test_parse_lap_time():
    """Test parsing lap time strings into seconds."""
    # Test the parsing logic, not specific values
    assert parse_lap_time("00:49.123") == 49.123
    assert parse_lap_time("01:23.456") == 83.456
    assert parse_lap_time("invalid") is None
    assert parse_lap_time("") is None

    # Test with real data from fixture
    html = load_fixture("season_race_324462.html")
    results = extract_race_results(html)

    # Verify all lap times are parsed correctly (not specific values)
    for result in results:
        if 'fastest_lap' in result and result['fastest_lap']:
            assert isinstance(result['fastest_lap'], (float, type(None)))
            if result['fastest_lap']:
                assert result['fastest_lap'] > 0, "Lap time should be positive"
```

---

### 4. 🔄 Tests Must Fail When Code is Broken

**Principle**: Tests should catch bugs, not pass regardless of correctness

**Bad Test** (will always pass):
```python
def test_extract_driver_name():
    result = extract_driver_name("<html>...</html>")
    assert result == "John Doe"  # Hardcoded expectation
```

**Good Test** (will fail if extractor breaks):
```python
def test_extract_driver_name():
    """Test that driver name is extracted from profile page."""
    html = load_fixture("driver_stats_33132.html")
    result = extract_driver_name(html)

    # Verify behavior, not exact value
    assert isinstance(result, str), "Should return a string"
    assert len(result) > 0, "Should not be empty"
    assert ' ' in result or result.isalpha(), "Should be a name format"

    # Can verify it's from the right section
    soup = BeautifulSoup(html, 'lxml')
    expected_name = soup.select_one('.driver-profile h1').get_text(strip=True)
    assert result == expected_name, "Should extract from correct HTML element"
```

---

### 5. 📊 Mock External Dependencies

**Always Mock**:
- HTTP requests (use `pytest-mock` or `responses`)
- Database connections (use in-memory SQLite)
- File I/O (use `tmp_path` fixture)
- Time/dates (use `freezegun` if needed)
- Random values

**Example**:
```python
def test_fetch_page_with_retry(mocker):
    """Test that fetch_page retries on failure."""
    # Mock the HTTP request
    mock_get = mocker.patch('requests.Session.get')
    mock_get.side_effect = [
        requests.exceptions.Timeout(),  # First call fails
        requests.exceptions.Timeout(),  # Second call fails
        mocker.Mock(status_code=200, content=b"<html>Success</html>")  # Third succeeds
    ]

    extractor = BaseExtractor()
    result = extractor.fetch_page("http://example.com")

    # Verify retry behavior
    assert mock_get.call_count == 3, "Should have retried twice"
    assert result is not None, "Should eventually succeed"
```

---

### 6. 🏗️ Test-Driven Development Workflow

**Process for each feature**:

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

5. **Add edge case tests**
   ```python
   def test_extract_league_name_missing_h1():
       html = "<html><body>No h1 tag</body></html>"
       with pytest.raises(AttributeError):
           extract_league_name(html)
   ```

6. **Refactor to handle edge case**
   ```python
   def extract_league_name(html: str) -> str:
       soup = BeautifulSoup(html, 'lxml')
       h1 = soup.select_one('h1')
       if not h1:
           raise ValueError("No league name found")
       return h1.get_text(strip=True)
   ```

---

### 7. 📁 Fixture Management

**Fixture Requirements**:
- Capture real HTML from SimRacerHub
- Store in `tests/fixtures/`
- Document capture date in `tests/fixtures/README.md`
- Use meaningful filenames: `{page_type}_{id}.html`
- Version control fixtures
- Update when site changes

**Fixture README Example**:
```markdown
# Test Fixtures

## Captured: 2025-10-31

### League Pages
- `league_series_1558.html` - OBRL league page with 4 series

### Series Pages
- `series_seasons_3714.html` - Wednesday series with 28 seasons

### Race Pages
- `season_race_324462.html` - Daytona race with 40 results

## Update Policy
When SimRacerHub changes:
1. Capture new fixtures
2. Update this README with new date
3. Update tests if needed
4. Update schema validators
```

**Loading Fixtures**:
```python
# tests/conftest.py

from pathlib import Path

FIXTURES_DIR = Path(__file__).parent / "fixtures"

def load_fixture(filename: str) -> str:
    """Load HTML fixture file."""
    filepath = FIXTURES_DIR / filename
    if not filepath.exists():
        raise FileNotFoundError(f"Fixture not found: {filename}")

    with open(filepath, 'r', encoding='utf-8') as f:
        return f.read()

# Make available as fixture
@pytest.fixture
def fixture_loader():
    return load_fixture
```

---

### 8. 🎭 Test Organization

**File Structure**:
```
tests/
├── conftest.py                 # Shared fixtures
├── fixtures/
│   ├── README.md
│   ├── league_series_1558.html
│   ├── series_seasons_3714.html
│   └── ...
├── unit/                       # Unit tests (mocked dependencies)
│   ├── test_database.py
│   ├── test_extractors.py
│   └── ...
└── integration/                # Integration tests (real workflows)
    ├── test_orchestrator.py
    └── test_e2e.py
```

**Naming Conventions**:
- Test files: `test_<module>.py`
- Test classes: `Test<ClassName>`
- Test functions: `test_<function>_<scenario>`

**Example**:
```python
# tests/unit/test_league_extractor.py

class TestLeagueExtractor:
    """Tests for LeagueExtractor class."""

    def test_extract_valid_league_page(self, fixture_loader):
        """Test extracting data from valid league page."""
        pass

    def test_extract_missing_series_raises_error(self):
        """Test that missing series data raises SchemaChangeDetected."""
        pass

    def test_extract_validates_schema(self, mocker):
        """Test that schema validation is called."""
        pass
```

---

### 9. ✅ Assertions Best Practices

**Good Assertions**:
```python
# Be specific
assert result is not None, "Result should not be None"

# Check types
assert isinstance(result, list), "Should return a list"

# Check structure
assert all('id' in item for item in result), "All items should have 'id' field"

# Check ranges
assert 0 < result < 100, "Value should be between 0 and 100"

# Multiple assertions for clarity
assert 'name' in driver
assert isinstance(driver['name'], str)
assert len(driver['name']) > 0
```

**Bad Assertions**:
```python
# Too vague
assert result  # What does this really test?

# Brittle
assert result == expected_result  # Breaks on any change

# Silent failures
try:
    result = extract_data(html)
except:
    pass  # Never do this in tests!
```

---

### 10. 🔍 Coverage Requirements

**Target**: 100% coverage

**Check Coverage**:
```bash
# For specific file
uv run pytest tests/unit/test_module.py --cov=src/module.py --cov-report=term-missing

# For all code
uv run pytest --cov=src --cov-report=html
open htmlcov/index.html
```

**What Counts**:
- ✅ All functions tested
- ✅ All branches tested (if/else)
- ✅ All exception paths tested
- ✅ All class methods tested

**What Doesn't Count**:
- ❌ Test code itself
- ❌ Code in `claude_temp/`
- ❌ `__main__` guards

**Acceptable Exceptions** (must justify):
- Platform-specific code
- Unreachable code (must prove it's unreachable)
- Deprecated code (should be removed)

---

## Common Test Patterns

### Testing Extractors
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

### Testing Database Operations
```python
def test_database_operation(test_db):
    """Test database CRUD operation."""
    # Insert
    entity_id = test_db.upsert_entity(123, {'name': 'Test'})
    assert entity_id == 123

    # Read
    result = test_db.get_entity(123)
    assert result is not None
    assert result['name'] == 'Test'

    # Update
    test_db.upsert_entity(123, {'name': 'Updated'})
    result = test_db.get_entity(123)
    assert result['name'] == 'Updated'
```

### Testing Schema Validation
```python
def test_schema_validator_detects_change():
    """Test that validator detects schema changes."""
    validator = SchemaValidator()

    # Valid HTML should pass
    valid_html = load_fixture("valid_page.html")
    assert validator.validate_javascript_data("entity_type", valid_html)

    # Invalid HTML should raise
    invalid_html = "<html><body>Missing data</body></html>"
    with pytest.raises(SchemaChangeDetected):
        validator.validate_javascript_data("entity_type", invalid_html)
```

### Testing Error Handling
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

---

## Checklist for Each Test

Before considering a test complete:

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

---

## Anti-Patterns to Avoid

### ❌ Don't Do This

```python
# Hardcoded results
def test_bad():
    assert extract() == ["exact", "expected", "data"]

# Testing implementation details
def test_bad():
    assert extractor._internal_method() == "something"

# No assertions
def test_bad():
    extract()  # Just calling the function

# Overly broad try/except
def test_bad():
    try:
        result = extract()
    except Exception:
        result = None
    assert result is not None

# Testing multiple things
def test_bad():
    # Tests 10 different things in one test
    pass
```

### ✅ Do This Instead

```python
# Test behavior with fixtures
def test_good():
    html = load_fixture("page.html")
    result = extract(html)
    assert isinstance(result, list)
    assert len(result) > 0

# Test public interface
def test_good():
    assert extractor.extract(html) is not None

# Clear assertions
def test_good():
    result = extract()
    assert result is not None, "Extract should return data"

# Specific exception testing
def test_good():
    with pytest.raises(ValueError, match="Invalid input"):
        extract(invalid_html)

# Focused tests
def test_extracts_series_ids():
    # Test one thing clearly
    pass

def test_extracts_series_names():
    # Test another thing clearly
    pass
```

---

## Summary

1. **Never hardcode test results** - Test behavior, not data
2. **Use real fixtures** - Test with actual HTML from SimRacerHub
3. **Tests must fail when code breaks** - That's their purpose
4. **Mock external dependencies** - Keep tests fast and reliable
5. **Follow TDD** - Write tests first
6. **100% coverage** - No exceptions
7. **Clear assertions** - Always include messages
8. **One test, one thing** - Keep tests focused

**Remember**: If your test would pass even when the code is broken, it's a bad test.
