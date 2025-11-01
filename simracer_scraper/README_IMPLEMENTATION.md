# Implementation Guide - Start Here

**Status**: Ready to Begin Implementation
**Current Phase**: Phase 2 - Core Infrastructure
**Next Task**: Task 2.1 - Database Schema & Initialization

## 📋 Quick Reference

### Key Documents
1. **TASKS.md** - 35 atomic tasks to complete the project
2. **TESTING_RULES.md** - Mandatory testing standards
3. **SPECIFICATION.md** - Complete technical specification
4. **DATA_MODEL.md** - Discovered data model from SimRacerHub
5. **FRAMEWORK.md** - Architecture and component design

### Development Rules

✅ **DO**:
- Work in `claude_temp/` for experiments
- Use real HTML fixtures for tests
- Test behavior and structure, not exact data
- Maintain 100% test coverage
- Move to `src/` only when complete and tested

❌ **DON'T**:
- Hardcode test results
- Put test code directly in `src/`
- Skip tests
- Allow coverage to drop below 100%
- Make assumptions about data

## 🚀 Getting Started

### 1. Setup (Already Complete)
```bash
# Dependencies installed
uv sync --all-extras

# Verify setup
uv run pytest --version
uv run python -c "import src; print('OK')"
```

### 2. Start First Task (Task 2.1)

**Read Task Requirements**:
- Open `TASKS.md`
- Read Task 2.1: Database Schema & Initialization
- Understand acceptance criteria

**Create Working Directory**:
```bash
mkdir -p claude_temp/task_2_1
cd claude_temp/task_2_1
```

**Develop Solution**:
```python
# claude_temp/task_2_1/database.py

import sqlite3
from pathlib import Path
from typing import Optional

class Database:
    """SQLite database manager for SimRacer scraper."""

    def __init__(self, db_path: str = ":memory:"):
        self.db_path = db_path
        self.conn: Optional[sqlite3.Connection] = None

    def __enter__(self):
        self.connect()
        return self

    def __exit__(self, exc_type, exc_val, exc_tb):
        self.close()

    def connect(self):
        """Open database connection."""
        self.conn = sqlite3.connect(self.db_path)
        self.conn.row_factory = sqlite3.Row
        # Enable foreign keys
        self.conn.execute("PRAGMA foreign_keys = ON")

    def close(self):
        """Close database connection."""
        if self.conn:
            self.conn.close()
            self.conn = None

    def initialize_schema(self):
        """Create all tables and indexes."""
        # Copy SQL from SPECIFICATION.md Section 4.2
        # Create each table...
        pass
```

**Write Tests**:
```python
# tests/unit/test_database.py

import pytest
from src.database import Database

@pytest.fixture
def test_db():
    """Provide clean test database."""
    db = Database(":memory:")
    db.connect()
    db.initialize_schema()
    yield db
    db.close()

def test_initialize_schema_creates_all_tables(test_db):
    """Test that all 9 tables are created."""
    cursor = test_db.conn.cursor()
    cursor.execute("""
        SELECT name FROM sqlite_master
        WHERE type='table' AND name NOT LIKE 'sqlite_%'
    """)
    tables = [row[0] for row in cursor.fetchall()]

    expected_tables = [
        'leagues', 'teams', 'drivers', 'series',
        'seasons', 'races', 'race_results',
        'scrape_log', 'schema_alerts'
    ]

    for table in expected_tables:
        assert table in tables, f"Table '{table}' not created"

def test_tables_have_correct_columns(test_db):
    """Test that tables have all required columns."""
    cursor = test_db.conn.cursor()

    # Check leagues table
    cursor.execute("PRAGMA table_info(leagues)")
    columns = {row[1] for row in cursor.fetchall()}
    required = {'league_id', 'name', 'url', 'scraped_at'}
    assert required.issubset(columns), "leagues table missing columns"

    # Test other tables...

# More tests...
```

**Run Tests**:
```bash
# From project root
uv run pytest tests/unit/test_database.py -v

# With coverage
uv run pytest tests/unit/test_database.py --cov=src/database.py --cov-report=term-missing
```

**Iterate Until All Tests Pass**

**Check Quality**:
```bash
# Format
uv run black src/database.py

# Lint
uv run ruff check src/database.py

# Type check
uv run mypy src/database.py
```

**Move to src/**:
```bash
# When everything passes
cp claude_temp/task_2_1/database.py src/database.py

# Verify
uv run pytest tests/unit/test_database.py -v
```

**Clean Up**:
```bash
rm -rf claude_temp/task_2_1
```

**Update Task Tracking**:
- Mark Task 2.1 complete in TASKS.md
- Move to Task 2.2

## 📊 Progress Tracking

### Phase 2: Core Infrastructure (12 tasks)
- [ ] 2.1 - Database Schema
- [ ] 2.2 - Database League CRUD
- [ ] 2.3 - Database Series CRUD
- [ ] 2.4 - Database Remaining CRUD
- [ ] 2.5 - Database URL Caching
- [ ] 2.6 - Database Logging
- [ ] 2.7 - Schema Validator Core
- [ ] 2.8 - Schema Validator JS
- [ ] 2.9 - Schema Validator Data
- [ ] 2.10 - Schema Validator Table
- [ ] 2.11 - JavaScript Parser
- [ ] 2.12 - Base Extractor

### Phase 3: Entity Extraction (6 tasks)
- [ ] 3.1 - League Extractor
- [ ] 3.2 - Series Extractor
- [ ] 3.3 - Season Extractor
- [ ] 3.4 - Race Extractor
- [ ] 3.5 - Driver Extractor
- [ ] 3.6 - Team Extractor

### Phase 4: Orchestration (5 tasks)
- [ ] 4.1 - Orchestrator Core
- [ ] 4.2 - League Scraping
- [ ] 4.3 - Series Scraping
- [ ] 4.4 - Season & Race Scraping
- [ ] 4.5 - Driver & Team Resolution

### Phase 5: CLI & Export (2 tasks)
- [ ] 5.1 - CLI Commands
- [ ] 5.2 - Export Functionality

### Phase 6: Production (5 tasks)
- [ ] 6.1 - Fixture Capture
- [ ] 6.2 - E2E Test
- [ ] 6.3 - CI/CD Setup
- [ ] 6.4 - Documentation
- [ ] 6.5 - Production Test

**Total**: 30 tasks (5 tasks are subtasks combined into main tasks)

## 🎯 Task Workflow Checklist

For each task:
- [ ] Read task requirements in TASKS.md
- [ ] Create working directory in `claude_temp/`
- [ ] Develop solution
- [ ] Write tests in `tests/`
- [ ] Capture fixtures if needed
- [ ] Run tests until all pass
- [ ] Check coverage (100%)
- [ ] Format code (black)
- [ ] Lint code (ruff)
- [ ] Type check (mypy)
- [ ] Move code to `src/`
- [ ] Run all tests again
- [ ] Clean up working directory
- [ ] Mark task complete

## 🧪 Testing Standards

### Critical Rules (See TESTING_RULES.md)

1. **Never hardcode results**
   ```python
   # ❌ BAD
   assert extract() == [{"id": 3714, "name": "..."}]

   # ✅ GOOD
   result = extract(load_fixture("page.html"))
   assert len(result) > 0
   assert all('id' in item for item in result)
   ```

2. **Test with real fixtures**
   ```python
   # ✅ GOOD
   html = load_fixture("league_series_1558.html")
   result = extract_series(html)
   assert isinstance(result, list)
   ```

3. **Tests must fail when code breaks**
   - If extractor is wrong, test should fail
   - Don't write tests that always pass

4. **100% coverage required**
   ```bash
   uv run pytest --cov=src --cov-fail-under=100
   ```

## 🔧 Useful Commands

### Testing
```bash
# Run specific test
uv run pytest tests/unit/test_database.py::test_function -v

# Run with coverage
uv run pytest tests/unit/test_database.py --cov=src/database.py

# Run in watch mode (during development)
uv run ptw tests/unit/ -- --cov=src/

# Run all tests
uv run pytest
```

### Code Quality
```bash
# Format all code
uv run black src/ tests/

# Check formatting
uv run black --check src/

# Lint
uv run ruff check src/

# Fix lint issues
uv run ruff check --fix src/

# Type check
uv run mypy src/

# All checks
uv run black src/ && uv run ruff check src/ && uv run mypy src/ && uv run pytest
```

### Development
```bash
# Install new dependency
uv add package-name

# Install dev dependency
uv add --dev package-name

# Update dependencies
uv lock

# Sync environment
uv sync
```

## 📚 Key Concepts

### Entity Hierarchy
```
League (1558)
  ├── Teams → Drivers
  └── Series (4)
       └── Seasons (28+)
            └── Races (10)
                 └── Results (40)
```

### URL Patterns
- League: `league_series.php?league_id=1558`
- Series: `series_seasons.php?series_id=3714`
- Race: `season_race.php?schedule_id=324462`
- Driver: `driver_stats.php?driver_id=33132`

### JavaScript Data
- Series: `series.push({id: 3714, ...})`
- Seasons: `seasons = [{id: 26741, ...}]`
- Must parse from `<script>` tags

### Schema Validation
- Check JavaScript patterns exist
- Check required fields present
- Check table structure valid
- Raise `SchemaChangeDetected` on mismatch

## 🎓 Learning Path

1. **Start with Database** (Tasks 2.1-2.6)
   - Fundamental to everything
   - Good practice with SQLite
   - Tests are straightforward

2. **Schema Validator** (Tasks 2.7-2.10)
   - Important safety mechanism
   - Practice with regex patterns
   - Learn fixture usage

3. **JavaScript Parser** (Task 2.11)
   - Critical for series/season extraction
   - Practice with regex + JSON

4. **Base Extractor** (Task 2.12)
   - Foundation for all extractors
   - Learn HTTP mocking
   - Rate limiting patterns

5. **Entity Extractors** (Tasks 3.1-3.6)
   - Apply all previous learning
   - Work with real HTML
   - Build complete extraction pipeline

6. **Orchestrator** (Tasks 4.1-4.5)
   - Tie everything together
   - Complex workflows
   - Integration testing

## 🆘 If You Get Stuck

1. **Review the specification** - Answer is usually there
2. **Check TESTING_RULES.md** - Verify test approach is correct
3. **Look at task requirements** - Break down further if needed
4. **Check fixtures** - Make sure HTML is correct
5. **Simplify** - Start with minimal implementation
6. **Ask questions** - Better to clarify than assume

## ✅ Success Metrics

### Task Complete When:
- All tests pass
- 100% coverage for new code
- Code formatted (black)
- No lint warnings (ruff)
- No type errors (mypy)
- Code in `src/` directory
- Working directory cleaned up

### Phase Complete When:
- All phase tasks complete
- Phase success criteria met (see SPECIFICATION.md Section 7)
- Integration tests pass
- Documentation updated

### Project Complete When:
- All 35 tasks complete
- Full OBRL scrape works
- Export functionality works
- CI/CD passing
- 100% overall coverage
- Production tested

## 🚦 Ready to Start?

1. Read **Task 2.1** in TASKS.md
2. Create `claude_temp/task_2_1/`
3. Start coding!

Good luck! Remember: **Work in claude_temp/, test thoroughly, move to src/ when complete.**
