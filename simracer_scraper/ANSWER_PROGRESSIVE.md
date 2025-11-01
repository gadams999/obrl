# Answer: Progressive Scraping Without --force

## Your Question

> Will scraping work progressively? Without using --force, is there a way to determine if there is new data (e.g., a future race has completed) and we want the stats but not pulling everything?

## Short Answer

**Yes!** The database now has a `should_scrape()` method that uses:
1. **Time-based cache validity** - data older than X hours is refreshed
2. **Status-based intelligence** - completed races are stable, ongoing races always refresh
3. **Configurable per entity type** - leagues (1 week), races (1 hour), etc.

## How It Works

### The `should_scrape()` Method

```python
from src.database import Database

with Database("simracer.db") as db:
    should_scrape, reason = db.should_scrape(
        entity_type="race",
        entity_id=67890,
        validity_hours=1  # 1 hour for races
    )

    if should_scrape:
        print(f"Scraping: {reason}")
        # Actually fetch and parse
    else:
        print(f"Using cache: {reason}")
        # Use cached data
```

### Example Outputs

```
# Fresh data
(False, "cache_fresh (0.5h < 1h)")

# Stale data
(True, "cache_stale (25.3h > 24h)")

# Not in database yet
(True, "not_in_cache")

# Ongoing race (always refresh)
(True, "status_ongoing_needs_refresh")

# Completed race (stable)
(False, "status_completed_stable")
```

## Real-World Example: Race Night

### Scenario

Season has 12 races:
- Races 1-8: **Completed** yesterday
- Race 9: **Ongoing** right now
- Races 10-12: **Upcoming**

### What Gets Scraped?

```python
for race in season.races:
    should_scrape, reason = db.should_scrape("race", race.id, validity_hours=1)

    if should_scrape:
        scrape_race(race.id)  # Only scrape this one
    else:
        use_cache(race.id)    # Skip, use DB
```

**Result:**
- Races 1-8: **SKIPPED** (status=completed, stable)
- Race 9: **SCRAPED** (status=ongoing, needs refresh)
- Race 10: **SCRAPED** (not in cache yet)
- Races 11-12: **SCRAPED** (not in cache yet)

**Network cost:** 4 requests instead of 12 (67% reduction)

## Configuration

Recommended validity periods:

```python
VALIDITY_HOURS = {
    "league": 168,    # 1 week - rarely changes
    "series": 168,    # 1 week - stable
    "season": 24,     # 1 day - during active season
    "race": 1,        # 1 hour - during race weekend
    "results": 1,     # 1 hour - post-race
}
```

## Try It Yourself

### Demo 1: Basic Cache Intelligence

```bash
# Shows cache decisions for league 1558
uv run python demo_progressive.py
```

**Output:**
```
League 1558:
  Should scrape? False
  Reason: cache_fresh (0.1h < 168h)

Testing different validity periods:
       1h: [  SKIP] cache_fresh (0.1h < 1h)
      24h: [  SKIP] cache_fresh (0.1h < 24h)
     168h: [  SKIP] cache_fresh (0.1h < 168h)
```

### Demo 2: Scraping With Smart Cache

```bash
# First scrape - fetches from web
uv run python demo_scraper.py scrape 1558

# Second scrape - uses cache
uv run python demo_scraper.py scrape 1558
# Output: ✓ League 1558 found in cache (cache_fresh (0.1h < 168h))

# Force refresh
uv run python demo_scraper.py scrape 1558 --force
```

## Full Implementation Pattern

For any entity scraper:

```python
def scrape_entity(entity_id, force=False, validity_hours=24):
    with Database() as db:
        # Check cache
        if not force:
            should_scrape, reason = db.should_scrape(
                "entity_type",
                entity_id,
                validity_hours
            )

            if not should_scrape:
                print(f"✓ Using cache: {reason}")
                return db.get_entity(entity_id)

            if reason != "not_in_cache":
                print(f"⟳ Refreshing: {reason}")

        # Fetch from web
        data = fetch_and_parse(entity_id)

        # Store with timestamp and status
        db.upsert_entity(entity_id, {
            **data,
            "status": determine_status(data),  # completed/ongoing/upcoming
            "scraped_at": datetime.now().isoformat()
        })

        return db.get_entity(entity_id)
```

## Benefits

### 1. Network Efficiency

Before:
```
Scraping 1000 completed races = 1000 requests × 2 seconds = 33 minutes
```

After:
```
- 990 completed races: cached (0 requests)
- 10 new/ongoing races: scraped (10 requests)
= 10 requests × 2 seconds = 20 seconds
```

**99% reduction!**

### 2. Respectful Scraping

- Only hits server when necessary
- Completed data never re-fetched
- Configurable freshness requirements

### 3. Up-to-Date Data

- Ongoing races always fresh
- New races detected automatically
- Stale data auto-refreshed

## What's Missing (For Full Progressive Scraping)

To complete the vision from WORKFLOW.md, you still need:

1. ✅ **`should_scrape()` method** - DONE
2. ✅ **Time-based validity** - DONE
3. ✅ **Status-aware logic** - DONE
4. 🔨 **Season/race scrapers** - Need implementation
5. 🔨 **Update command** - `scrape update --league 1558`
6. 🔨 **Config file** - Load validity hours from YAML
7. 🔨 **Hierarchical walker** - Walk league → series → seasons → races

## Next Steps

To enable full progressive scraping:

### Option A: Build Season Scraper (Next)

```python
def scrape_season_progressively(season_id):
    """Scrape only new/changed races in a season."""
    # Implementation in PROGRESSIVE_SCRAPING.md
```

### Option B: Build Update Command

```bash
uv run python scraper.py update --league 1558
# Walks entire hierarchy, only scrapes what's needed
```

### Option C: Continue with TASKS.md

- Task 2.5: URL Caching
- Task 2.6: Logging
- Task 3.x: Entity Extractors
- Task 4.x: Orchestration (progressive scraping logic)

## Summary

**Your question:** Can we scrape progressively without re-fetching everything?

**Answer:** YES!

**How:**
1. Database tracks `scraped_at` timestamp for all entities
2. `should_scrape()` compares age vs. validity period
3. Completed races/seasons are never re-scraped
4. Ongoing/new data is auto-detected and refreshed
5. Configurable freshness per entity type

**Status:**
- ✅ Core logic implemented
- ✅ Working demo available
- 🔨 Need full hierarchy scrapers to use it end-to-end

Try the demos to see it in action!
