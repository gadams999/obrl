# SimRacer Scraper - Quick Start Demo

## What Works Now

✅ **End-to-end league scraping**: Fetch league data from SimRacerHub and store in SQLite database
✅ **Series discovery**: Automatically extracts all series from JavaScript embedded in the page
✅ **Database caching**: Won't re-scrape data unless you use `--force`
✅ **Rate limiting**: 2-second delay between requests (respectful scraping)
✅ **Full CRUD layer**: Complete database operations for all entities

## Quick Demo

### 1. Scrape The OBRL League

```bash
uv run python demo_scraper.py scrape 1558
```

**Output:**
```
Fetching https://www.simracerhub.com/league_series.php?league_id=1558...
✓ Stored league: Sim Racer Hub: League Series
  ✓ Stored series 3714: The OBRL Wednesday Night Series
  ✓ Stored series 3713: The OBRL Thursday Night Series
  ✓ Stored series 3712: The OBRL Monday Night Series
  ✓ Stored series 3711: The OBRL Sunday Night Series
```

### 2. List What You Scraped

```bash
# List leagues
uv run python demo_scraper.py list

# List series
uv run python demo_scraper.py series 1558

# Show statistics
uv run python demo_scraper.py stats
```

### 3. Test Caching

```bash
# This uses cached data (no network request)
uv run python demo_scraper.py scrape 1558

# Force refresh
uv run python demo_scraper.py scrape 1558 --force
```

## Available Commands

```bash
# Scrape a league
python demo_scraper.py scrape <league_id>

# Force re-scrape (ignore cache)
python demo_scraper.py scrape <league_id> --force

# List all leagues in database
python demo_scraper.py list

# List series (optionally filter by league)
python demo_scraper.py series [league_id]

# Show database statistics
python demo_scraper.py stats
```

## How It Works

1. **Fetch**: Downloads the league page from SimRacerHub
2. **Extract**: Parses HTML with BeautifulSoup and extracts JavaScript arrays
3. **Store**: Saves league and series data to SQLite database
4. **Cache**: Next scrape checks database first, skips if data exists

## Database Location

Data is stored in `simracer_demo.db` (SQLite file in project root).

You can inspect it with:
```bash
sqlite3 simracer_demo.db "SELECT * FROM leagues;"
sqlite3 simracer_demo.db "SELECT * FROM series;"
```

## What's Next?

The demo scraper is intentionally simple (leagues + series only). The full implementation will add:

- ✅ **Seasons**: Scrape all seasons for each series
- ✅ **Races**: Scrape all races for each season
- ✅ **Results**: Scrape race results with driver data
- ✅ **Teams & Drivers**: Scrape rosters
- ✅ **Schema validation**: Detect when SimRacerHub changes their structure
- ✅ **Config export**: Export discovered URLs to YAML for scheduled scraping
- ✅ **Full CLI**: Professional command-line interface

See `WORKFLOW.md` for the complete feature roadmap.

## Code Overview

### Files Created

- `src/simple_scraper.py` - Simple end-to-end scraper (200 lines)
- `src/database.py` - Complete database layer with CRUD (1,161 lines)
- `demo_scraper.py` - Demo CLI script
- `tests/unit/test_database.py` - Database tests (53 tests, 99% coverage)

### Architecture

```
demo_scraper.py (CLI)
    ↓
SimpleLeagueScraper
    ├── fetch_page() → requests + rate limiting
    ├── extract_series_from_js() → regex parsing
    └── Database → SQLite CRUD operations
```

### Key Features

1. **Rate Limiting**: 2-second minimum between requests
2. **Caching**: Database-first approach
3. **Error Handling**: Proper exception handling
4. **User Agent**: Identifies as educational scraper
5. **Foreign Keys**: Enforced relationships between entities

## Testing Other Leagues

You can scrape other leagues by their ID:

```bash
# Find league IDs by browsing SimRacerHub URLs
# Example: league_series.php?league_id=XXXX

uv run python demo_scraper.py scrape 1234  # Replace with any league ID
```

## Respecting robots.txt

⚠️ **Important**: SimRacerHub's `robots.txt` currently has `Disallow: /`. We've built this scraper with:
- Rate limiting (2-second delays)
- Proper user agent identification
- Caching to minimize requests

**Only use this with permission from SimRacerHub or for educational purposes.**

## Production Readiness

This demo is **not** production-ready. It's missing:
- Error recovery and retry logic
- Comprehensive logging
- Schema validation (detect site changes)
- Progress tracking for large scrapes
- CLI progress bars
- Concurrent scraping (with proper rate limits)

See `TASKS.md` for the full implementation plan (35 tasks across 6 phases).
