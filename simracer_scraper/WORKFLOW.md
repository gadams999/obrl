# SimRacer Scraper - User Workflow

**Version**: 1.0.0
**Date**: 2025-10-31

## Overview

Progressive scraping workflow with database caching, config export, and force refresh capabilities.

## Core Principles

1. **Database-first**: All scraped data stored in SQLite database
2. **Smart caching**: Don't re-scrape data unless needed
3. **Progressive discovery**: Scrape hierarchy level by level
4. **Config export**: Save discovered URLs for future scraping
5. **Force refresh**: Override cache when site schema changes

---

## Usage Examples

### 1. Initial League Discovery

Scrape a league by ID or URL:

```bash
# By league ID
simracer-scraper scrape league 1558

# By league URL
simracer-scraper scrape league https://www.simracerhub.com/league.php?league_id=1558

# By league name (searches database first)
simracer-scraper scrape league "The OBRL"
```

**Output**:
```
✓ Scraped league: The OBRL presented by VctryLnSprts
  - League ID: 1558
  - Series found: 4
  - Teams found: 12
  - Drivers found: 156
  - Stored in database
```

### 2. Progressive Scraping

Scrape progressively deeper into hierarchy:

```bash
# Scrape all series for a league
simracer-scraper scrape series --league 1558

# Scrape all seasons for a series
simracer-scraper scrape seasons --series 3714

# Scrape all races for a season
simracer-scraper scrape races --season 12345

# Scrape specific race results
simracer-scraper scrape race 67890
```

### 3. Full Hierarchy Scraping

Scrape entire hierarchy at once:

```bash
# Scrape everything: league → series → seasons → races → results
simracer-scraper scrape all --league 1558

# With depth control
simracer-scraper scrape all --league 1558 --depth series  # Stop at series
simracer-scraper scrape all --league 1558 --depth season  # Stop at seasons
simracer-scraper scrape all --league 1558 --depth race    # Full scrape
```

### 4. Force Refresh (Override Cache)

Re-scrape existing data (useful when site schema changes):

```bash
# Force re-scrape a league
simracer-scraper scrape league 1558 --force

# Force re-scrape all series
simracer-scraper scrape series --league 1558 --force

# Force full hierarchy refresh
simracer-scraper scrape all --league 1558 --force
```

### 5. Export Configuration

Export discovered URLs to config file for scheduled scraping:

```bash
# Export single league hierarchy
simracer-scraper export-config --league 1558

# Export multiple leagues
simracer-scraper export-config --league 1558,1234,5678

# Export to specific file
simracer-scraper export-config --league 1558 --output obrl_config.yaml

# Export all leagues in database
simracer-scraper export-config --all
```

**Generated config** (`scrape_config.yaml`):

```yaml
version: "1.0"
generated_at: "2025-10-31T12:00:00Z"

leagues:
  - league_id: 1558
    name: "The OBRL presented by VctryLnSprts"
    url: "https://www.simracerhub.com/league.php?league_id=1558"
    scrape_depth: "race"  # league, series, season, race

    series:
      - series_id: 3714
        name: "Wednesday Night Series"
        url: "https://www.simracerhub.com/league_series.php?series_id=3714"

        seasons:
          - season_id: 12345
            name: "2025 Season 1"
            url: "https://www.simracerhub.com/league_season.php?season_id=12345"

            races:
              - schedule_id: 67890
                name: "Week 1 - Daytona"
                url: "https://www.simracerhub.com/league_results.php?schedule_id=67890"
              - schedule_id: 67891
                name: "Week 2 - Talladega"
                url: "https://www.simracerhub.com/league_results.php?schedule_id=67891"
```

### 6. Scrape from Config

Use exported config for scheduled/batch scraping:

```bash
# Scrape all entities in config
simracer-scraper scrape from-config scrape_config.yaml

# With force refresh
simracer-scraper scrape from-config scrape_config.yaml --force

# Dry run (show what would be scraped)
simracer-scraper scrape from-config scrape_config.yaml --dry-run
```

### 7. Status and Info

Check what's in the database:

```bash
# Show all leagues
simracer-scraper list leagues

# Show series for a league
simracer-scraper list series --league 1558

# Show database stats
simracer-scraper stats

# Show scrape log (recent activity)
simracer-scraper log --limit 50

# Show cache status for a league
simracer-scraper cache-status --league 1558
```

---

## Caching Behavior

### Default Behavior (Cache-first)

When scraping without `--force`:

1. **Check database**: Does entity URL exist?
2. **Check freshness**: Is `scraped_at` recent enough? (configurable, default: 24 hours)
3. **Skip if fresh**: Log as "skipped (cached)" and return cached data
4. **Scrape if stale**: Re-scrape and update database

### Force Refresh

When using `--force`:

1. **Ignore cache**: Always scrape, even if data exists
2. **Update database**: Upsert with new `scraped_at` timestamp
3. **Log action**: Mark in `scrape_log` as "refreshed"

### Configuration

In `config.yaml`:

```yaml
cache:
  # How long cached data is valid before re-scraping
  validity_hours:
    league: 168    # 1 week
    series: 168    # 1 week
    season: 24     # 1 day (seasons change frequently)
    race: 24       # 1 day
    results: 1     # 1 hour (results update after races)
```

---

## Database Schema Validation

When scraping with cache enabled:

1. **Scrape entity**
2. **Validate schema**: Check against known patterns (schema validator)
3. **If schema changed**:
   - Log schema alert to `schema_alerts` table
   - Exit with error code
   - Notify user to update extractors
4. **If schema matches**:
   - Store data
   - Continue scraping

---

## CLI Design

### Command Structure

```
simracer-scraper <command> [subcommand] [options]

Commands:
  scrape          Scrape data from SimRacerHub
  export-config   Export database to config file
  list            List entities in database
  stats           Show database statistics
  log             Show scrape log
  cache-status    Show cache status
  validate        Validate database integrity
```

### Scrape Command

```
simracer-scraper scrape <entity-type> [identifier] [options]

Entity types:
  league          Scrape a league
  series          Scrape series for a league
  seasons         Scrape seasons for a series
  races           Scrape races for a season
  race            Scrape a single race
  all             Scrape full hierarchy
  from-config     Scrape from config file

Options:
  --league <id>        League ID or name
  --series <id>        Series ID
  --season <id>        Season ID
  --depth <level>      Scraping depth (league|series|season|race)
  --force              Force refresh (ignore cache)
  --dry-run            Show what would be scraped
  --rate-limit <ms>    Override rate limit (default: 2000ms)
```

### Export Command

```
simracer-scraper export-config [options]

Options:
  --league <id>        Export specific league(s)
  --all                Export all leagues in database
  --output <file>      Output file (default: scrape_config.yaml)
  --format <fmt>       Output format (yaml|json)
  --include-stale      Include stale cached data
```

---

## Implementation Notes

### Task Priority

Based on your requirements, here's the suggested task order:

1. ✅ **Task 2.1**: Database Schema (DONE)
2. ✅ **Task 2.2**: League CRUD (DONE)
3. **Task 2.3-2.5**: Series, Season, Race, Driver CRUD operations
4. **Task 2.6**: URL Caching logic
5. **Task 2.7-2.10**: Schema Validator
6. **Task 3.x**: Entity Extractors (parse HTML/JS)
7. **Task 4.x**: Orchestrator (progressive scraping logic)
8. **Task 5.1**: CLI commands
9. **Task 5.2**: Config export/import

### Key Features to Implement

1. **Cache checking**: `should_scrape(url, entity_type) -> bool`
2. **Force flag**: Bypass cache when `--force` is set
3. **Config export**: `export_league_config(league_id) -> dict`
4. **Config import**: `scrape_from_config(config_file, force=False)`
5. **Progressive scraping**: Each entity type scrapes its children when depth allows

---

## Example Full Workflow

```bash
# 1. Initial discovery
simracer-scraper scrape league 1558
# → Stores league, teams, drivers, discovers 4 series

# 2. Explore series
simracer-scraper list series --league 1558
# → Shows 4 series discovered

# 3. Scrape seasons for one series
simracer-scraper scrape seasons --series 3714
# → Discovers all seasons for Wednesday Night Series

# 4. Scrape full hierarchy for league
simracer-scraper scrape all --league 1558
# → Scrapes everything, skips already-cached data

# 5. Export to config
simracer-scraper export-config --league 1558
# → Creates scrape_config.yaml with full hierarchy

# 6. Later: refresh stale data
simracer-scraper scrape from-config scrape_config.yaml
# → Only re-scrapes stale data (>24 hours old)

# 7. Site changed: force full refresh
simracer-scraper scrape all --league 1558 --force
# → Re-scrapes everything regardless of cache
```

---

## Next Steps

1. Continue with **Task 2.3** (Series CRUD operations)
2. Implement **cache validity checking** in Database class
3. Build **CLI framework** with Click or Typer
4. Implement **progressive scraping orchestrator**
5. Create **config export/import** functionality
