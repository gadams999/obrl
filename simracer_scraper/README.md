# SimRacer Scraper

A hierarchical web scraper for extracting racing league data from SimRacerHub.

## ⚠️ Important Legal Notice

**This scraper is currently awaiting permission from SimRacerHub administrators.**

SimRacerHub's `robots.txt` contains:
```
User-agent: *
Disallow: /
```

This indicates the site operators prefer no automated scraping. Use of this tool should only occur:
- After receiving explicit permission from SimRacerHub administrators
- For personal/educational purposes with your own league data
- With full understanding of the site's policies

## Features

- **Hierarchical scraping** - League → Series → Seasons → Races
- **JavaScript rendering** - Handles dynamic content with Playwright
- **Smart caching** - Database-driven to avoid redundant requests
- **Rate limiting** - Respectful delays (2-4 seconds randomized)
- **Depth control** - Stop scraping at any level
- **100% test coverage** - Comprehensive unit tests
- **Schema validation** - Detects site changes
- **SQLite storage** - Single-file database

## Installation

### Quick Start (Casual Users)

```bash
# 1. Clone repository
git clone <repository-url>
cd obrl/simracer_scraper

# 2. Create virtual environment (Python 3.10+ required)
python3 -m venv .venv

# 3. Activate virtual environment
# On Linux/Mac:
source .venv/bin/activate
# On Windows:
.venv\Scripts\activate

# 4. Install dependencies
pip install -r requirements.txt

# 5. Install Playwright browsers
playwright install chromium

# 6. Copy and edit configuration file
cp config.yaml.example config.yaml
# Edit config.yaml with your league ID and preferences

# 7. Run the scraper
python scraper.py
```

### For Developers

This project uses [uv](https://github.com/astral-sh/uv) for package management and [mise](https://mise.jdx.dev/) for Python version management.

```bash
# Install dependencies
uv sync --all-extras

# Install Playwright browsers
uv run playwright install chromium

# Copy and configure settings
cp config.yaml.example config.yaml
# Edit config.yaml with your preferences
```

**Python Version**: 3.10+ required. If using mise:
```bash
mise install  # Installs Python from .python-version
```

## Configuration

The scraper uses `config.yaml` for all configuration. Copy the example file and customize:

```bash
cp config.yaml.example config.yaml
```

### Configuration File Structure

```yaml
league:
  id: 1558              # Your league ID from SimRacerHub
  depth: race           # How deep to scrape: league, series, season, or race
  database: obrl.db     # Database file path

scraping:
  request_delay: 2.0    # Seconds between requests (minimum)
  max_retries: 3        # Retry attempts for failed requests
  timeout: 10           # Request timeout in seconds
  user_agent: "SimRacerScraper/1.0 (Educational/Personal Use)"

logging:
  level: INFO           # DEBUG, INFO, WARNING, ERROR
  file: ./scraper.log   # Log file path
```

### Configuration Options

**League Settings:**
- `id`: League ID from SimRacerHub URL (e.g., `league_series.php?league_id=1558`)
- `depth`: Scraping depth - `league`, `series`, `season`, or `race` (default: `race`)
- `database`: SQLite database file path (default: `simracer.db`)

**Scraping Behavior:**
- `request_delay`: Minimum delay between requests in seconds (default: 2.0)
- `max_retries`: Maximum retry attempts for failed requests (default: 3)
- `timeout`: Request timeout in seconds (default: 10)
- `user_agent`: Custom User-Agent string for identification

**Logging:**
- `level`: Log verbosity - `DEBUG`, `INFO`, `WARNING`, or `ERROR`
- `file`: Path to log file

**Configuration Priority**: Command-line arguments override config.yaml settings.

## Usage

### Basic Usage

```bash
# Simple usage with config.yaml
python scraper.py

# Or with uv
uv run simracer-scraper scrape league 1558
```

### Depth Control

```bash
# Scrape only league metadata
python scraper.py scrape league 1558 --depth league

# Scrape league + series
python scraper.py scrape all --league 1558 --depth series

# Scrape league + series + seasons
python scraper.py scrape all --league 1558 --depth season

# Full scrape (league + series + seasons + races + results)
python scraper.py scrape all --league 1558 --depth race  # Default
```

### Force Refresh

Bypass cache and re-scrape:

```bash
python scraper.py scrape league 1558 --force
```

### Advanced Options

```bash
# Enable debug logging
python scraper.py scrape league 1558 --log-level DEBUG

# Use custom config file
python scraper.py scrape league 1558 --config custom_config.yaml

# Use custom database
python scraper.py scrape league 1558 --db my_league.db
```

## Output

All scraped data is stored in SQLite database (default: `obrl.db`).

### Database Schema

- **leagues**: League metadata
- **teams**: Team rosters
- **drivers**: Driver profiles and iRacing stats
- **series**: Series within leagues
- **seasons**: Seasons within series
- **races**: Race metadata and conditions
- **race_results**: Detailed results (20+ fields per driver)
- **scrape_log**: Audit trail of scraping activity
- **schema_alerts**: Site structure change notifications

### Querying Data

```bash
# Using sqlite3
sqlite3 obrl.db

# Example queries
SELECT * FROM leagues;
SELECT * FROM series WHERE league_id = 1558;
SELECT * FROM races WHERE season_id = 26741 ORDER BY date;
SELECT * FROM race_results WHERE race_id = 1 AND finish_position <= 5;
```

## Project Structure

```
simracer_scraper/
├── src/
│   ├── cli.py                      # Command-line interface
│   ├── database.py                 # SQLite database manager
│   ├── orchestrator.py             # Hierarchical scrape coordinator
│   ├── schema_validator.py         # Schema validation & alerts
│   ├── extractors/                 # Page-specific extractors
│   │   ├── base.py                 # Base extractor (rate limiting, HTTP)
│   │   ├── league.py               # League extractor
│   │   ├── series.py               # Series extractor (JS parsing)
│   │   ├── season.py               # Season extractor (JS parsing)
│   │   └── race.py                 # Race extractor (results table)
│   └── utils/
│       ├── browser_manager.py      # Shared Playwright browser
│       └── js_parser.py            # JavaScript data extraction
│
├── tests/                          # 100% test coverage
│   ├── conftest.py                 # Shared fixtures
│   └── unit/                       # Unit tests
│
├── config.yaml                     # Main configuration
├── pyproject.toml                  # Project metadata & dependencies
├── scraper.py                      # Convenience wrapper script
├── requirements.txt                # Pip dependencies (fallback)
├── ARCHITECTURE.md                 # Technical architecture docs
├── DEVELOPMENT.md                  # Development guide
└── README.md                       # This file
```

## Development

### Running Tests

```bash
# Run all tests
uv run pytest

# Run with coverage
uv run pytest --cov=src --cov-report=term-missing

# Run specific test file
uv run pytest tests/unit/test_database.py
```

### Code Quality

```bash
# Format code with black
uv run black src/

# Lint with ruff
uv run ruff check src/

# Type checking with mypy
uv run mypy src/
```

### Development Workflow

1. Write failing tests first (TDD)
2. Implement minimum code to pass tests
3. Ensure 100% test coverage
4. Format with black (line-length: 100)
5. Lint with ruff
6. Type check with mypy

See [DEVELOPMENT.md](DEVELOPMENT.md) for detailed development guide.

## Architecture

### Hierarchical Extraction

```
League (league_id: 1558)
  ├── Teams (15 teams)
  │   └── Drivers (~60 total)
  │
  └── Series (4 series)
       └── Seasons (28+ seasons per series)
            └── Races (10 races per season)
                 └── Results (40 participants per race)
```

### Key Components

- **Orchestrator**: Coordinates hierarchical scraping with depth control
- **Extractors**: Parse HTML/JavaScript for each entity type
- **Database**: SQLite with caching and upsert operations
- **Browser Manager**: Shared Playwright instance for JavaScript rendering
- **Schema Validator**: Detects site structure changes

See [ARCHITECTURE.md](ARCHITECTURE.md) for detailed architecture documentation.

## Caching Strategy

- **Database-driven**: Tracks `scraped_at` timestamp for each entity
- **Configurable**: Set cache expiry with `cache_max_age_days` parameter
- **Immutable data**: Race results cached forever (`cache_max_age_days=None`)
- **Force refresh**: Use `--force` flag to bypass cache

## Rate Limiting

- **Randomized delays**: 2-4 seconds between requests (mimics human browsing)
- **Sequential execution**: One request at a time
- **Configurable**: Adjust in config.yaml or orchestrator initialization
- **Respectful**: Prioritizes being respectful over speed

## Ethical Scraping

This scraper follows ethical scraping practices:

1. **Respect robots.txt** - Awaiting permission
2. **Rate limiting** - Randomized 2-4 second delays
3. **Identification** - Clear user-agent with contact information
4. **Targeted scraping** - Only scrape specific needed pages
5. **Error handling** - Graceful handling of failures
6. **Logging** - Full audit trail of scraping activity

## Troubleshooting

### Database Locked

```bash
# Close all connections
# Check for other processes:
lsof obrl.db

# Or delete and reinitialize
rm obrl.db
python scraper.py scrape league 1558
```

### Playwright Issues

```bash
# Reinstall browsers
uv run playwright install chromium

# Or with pip
playwright install chromium
```

### Schema Changes

If SimRacerHub changes their site structure:

```bash
# Check schema alerts
sqlite3 obrl.db "SELECT * FROM schema_alerts WHERE resolved=0 ORDER BY timestamp DESC"

# Site changes require code updates - see DEVELOPMENT.md
```

## Documentation

- [README.md](README.md) - This file (project overview)
- [ARCHITECTURE.md](ARCHITECTURE.md) - Technical architecture and data model
- [DEVELOPMENT.md](DEVELOPMENT.md) - Development guide and testing

## License

This tool is provided for educational and personal use only.

## Contact

Before using this scraper, contact SimRacerHub administrators for permission.

**Default test league**: The OBRL presented by VctryLnSprts (league_id=1558)
