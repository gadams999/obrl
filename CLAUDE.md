# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository Overview

This is a monorepo containing multiple subprojects organized in subdirectories. The primary active project is **simracer_scraper**, a web scraper for extracting racing league data from SimRacerHub.

## Current Status - READ FIRST

**JavaScript Rendering**: ✅ 100% Complete (see `simracer_scraper/JS_RENDERING_STATUS.md`)

Key implementation details:
- JavaScript rendering fully functional for seasons and races
- Shared BrowserManager prevents async conflicts
- Rate limiting enforced across ALL extractors (2-4 seconds between requests)
- Races processed sequentially (one at a time, never concurrent)
- Race results cached forever with `cache_max_age_days=None` (immutable data)
- All data extraction working end-to-end with 0 errors

## SimRacer Scraper Project

Located in `simracer_scraper/`, this is an ethical web scraper for SimRacerHub racing league data.

### Critical Legal Context

**⚠️ This project is awaiting explicit permission from SimRacerHub administrators**. The site's `robots.txt` currently disallows all automated scraping. Do not suggest running scrapers against SimRacerHub without verifying permission status.

### Development Commands

**IMPORTANT**: The Python virtual environment (`.venv/`) is located in `simracer_scraper/`, NOT the repository root. All commands must be run from the `simracer_scraper/` directory.

All commands assume you're in the `simracer_scraper/` directory:

```bash
# Environment setup
uv sync                           # Install dependencies
uv sync --all-extras             # Install with dev dependencies

# Running the scraper
uv run simracer-scraper scrape league 1558                    # Scrape a league
uv run simracer-scraper scrape all --league 1558 --depth race # Full hierarchy scrape

# Code quality
uv run black src/                # Format code (line-length: 100)
uv run ruff check src/           # Lint code
uv run mypy src/                 # Type checking

# Testing
uv run pytest                    # Run all tests
uv run pytest tests/unit/        # Run unit tests only
uv run pytest -v --cov=src --cov-report=term-missing  # With coverage
uv run pytest tests/unit/test_database.py  # Single test file

# Note: Project has 100% coverage requirement (--cov-fail-under=100)
```

### Architecture Overview

The project uses a **hierarchical extractor-orchestrator pattern**:

#### Core Components

1. **Database Layer** (`src/database.py`)
   - SQLite database with comprehensive schema for racing data
   - Hierarchy: leagues → series → seasons → races → race_results
   - Also tracks: teams, drivers (with relationship to races)
   - Built-in caching via `is_url_cached()` and `should_scrape()`
   - Scrape logging and schema change alerts
   - All database operations use upsert patterns (INSERT ... ON CONFLICT)

2. **Extractors** (`src/extractors/`)
   - `base.py`: BaseExtractor with rate limiting, retries, HTML parsing
   - `league.py`: LeagueExtractor for league pages
   - `series.py`: SeriesExtractor for series pages
   - `season.py`: SeasonExtractor for season pages
   - `race.py`: RaceExtractor for race result pages
   - Each extractor returns structured data with `metadata` and `child_urls`

3. **Orchestrator** (`src/orchestrator.py`)
   - Coordinates hierarchical scraping (League → Series → Season → Race)
   - Implements depth control (stop at any level)
   - Smart caching to avoid redundant scrapes
   - Filtering support (e.g., only scrape specific series IDs)
   - Progress tracking and error handling
   - Rate limiting with randomized delays (2-4 seconds default for human-like behavior)

4. **Schema Validator** (`src/schema_validator.py`)
   - Detects when SimRacerHub site structure changes
   - Stores alerts in database for developer attention
   - Critical for preventing silent failures

5. **CLI** (`src/cli.py`)
   - Command-line interface for scraping operations
   - Progressive scraping: scrape incrementally deeper into hierarchy
   - Export discovered URLs to config files
   - Force refresh to bypass cache

#### Data Flow

```
CLI → Orchestrator → Extractor → BaseExtractor (HTTP) → BeautifulSoup
                ↓
            Database (SQLite with caching)
```

#### Rate Limiting

- **Two modes**: Fixed delay or randomized range
- Default: Randomized 2-4 seconds (human-like browsing)
- Configurable per-extractor: `rate_limit_seconds` or `rate_limit_range`
- Exponential backoff on retries (default: 3 retries)

#### Caching Strategy

The orchestrator implements smart caching via `Database.is_url_cached()`:
- `cache_max_age_days`: Configurable staleness threshold (default: 7 days)
- Set to `None` for indefinite caching
- Tracks via `scraped_at` timestamp in each entity table
- Cache can be bypassed with `--force` flag in CLI

### Database Schema Patterns

All entity tables follow similar patterns:
- Primary key from SimRacerHub (league_id, series_id, season_id, schedule_id, driver_id, team_id)
- `url` column (UNIQUE) for cache lookups
- `scraped_at` timestamp for cache freshness
- `created_at` and `updated_at` timestamps
- Foreign key relationships enforce hierarchy

### JavaScript Parsing

SimRacerHub embeds data in JavaScript variables. The `src/utils/js_parser.py` module extracts structured data from `<script>` tags using regex patterns. This is critical for race results and season data.

### Testing Philosophy

- **100% code coverage required** (enforced by pytest)
- Tests use `pytest-mock` for HTTP mocking
- Database tests use in-memory SQLite (`:memory:`)
- Fixtures in `tests/conftest.py` provide reusable test data
- Test markers: `@pytest.mark.unit`, `@pytest.mark.integration`, `@pytest.mark.slow`

### Configuration

- `pyproject.toml`: Python project metadata, dependencies, tool configs
- `config.yaml`: Scraper behavior (rate limits, output formats, target pages)
- `.env`: Environment variables (not in repo, use `.env.example` as template)

### Key Design Patterns

1. **Context Managers**: Database and Orchestrator use `with` statements
2. **Upsert Operations**: All database writes use `INSERT ... ON CONFLICT`
3. **Progressive Discovery**: Each level discovers URLs for the next level
4. **Fail-Soft**: Errors at one entity don't stop scraping of siblings

### Common Gotchas

- The `Database` class requires calling methods that take a `data` dict parameter with a specific structure (see method signatures)
- The simplified convenience methods in Orchestrator (e.g., `upsert_league()`) take kwargs directly instead of a data dict
- Race IDs: SimRacerHub uses `schedule_id` as the unique identifier, but the database uses auto-increment `race_id` internally
- Always use `uv run` prefix for commands to ensure correct environment

### Documentation Files

The `simracer_scraper/` directory contains extensive markdown documentation:
- `README.md`: User-facing documentation
- `WORKFLOW.md`: CLI usage examples and progressive scraping patterns
- `DATA_MODEL.md`: Detailed database schema documentation
- `SPECIFICATION.md`: Technical specification
- `TESTING_RULES.md`: Testing guidelines
- `STATUS.md`: Current implementation status
- `FRAMEWORK.md`: Architecture decisions

## Temporary Files

The `claude_temp/` directory is gitignored and used for ephemeral experiments. Do not place production code there.

## Python Environment

- **Python Version**: 3.10+ (specified in `pyproject.toml` as `>=3.10,<3.15`)
- **Package Manager**: uv (recommended) or pip (fallback)
- **Version Manager**: mise (optional, uses `.python-version`)

## Adding New Features

When adding features to simracer_scraper:

1. Start in the appropriate layer (extractor, orchestrator, database, or CLI)
2. Write tests first (TDD approach, 100% coverage required)
3. Update relevant markdown documentation
4. Use type hints (Python 3.10+ style with `|` for unions)
5. Format with black (line-length: 100)
6. Ensure ruff linting passes
7. Run full test suite before committing
