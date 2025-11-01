# Progressive Scraping - Smart Updates

## The Problem

You want to update your database **without re-scraping everything**. For example:
- A future race just finished → scrape only that race's results
- A season is ongoing → check for new races, skip completed ones
- League info rarely changes → only refresh weekly

## The Solution: Cache Validity + Status Tracking

### 1. Time-Based Cache Validity

Different entities have different update frequencies:

```python
VALIDITY_HOURS = {
    "league": 168,    # 1 week - leagues rarely change
    "series": 168,    # 1 week - series info is stable
    "season": 24,     # 1 day - seasons update during active periods
    "race": 1,        # 1 hour - races update during events
    "results": 1,     # 1 hour - results change during/after races
}
```

### 2. Status-Based Scraping

Entities have status fields that indicate if they need updates:

```python
# Seasons
status IN ('upcoming', 'active', 'completed')

# Races
status IN ('upcoming', 'ongoing', 'completed')

# Logic:
if status == 'completed':
    # Data is stable, trust cache longer
    return False, "completed_stable"

elif status in ['active', 'ongoing']:
    # Data is changing, always refresh
    return True, "active_needs_refresh"
```

## Example: Scraping Only New Races

### Scenario

You have a season with 12 races:
- Races 1-8: **completed** (data stable)
- Race 9: **ongoing** (happening now!)
- Races 10-12: **upcoming** (not started)

### Smart Scraping Logic

```python
def scrape_season_progressively(season_id: int):
    with Database() as db:
        # Get all races for the season
        races = db.get_races_by_season(season_id)

        for race in races:
            should_scrape, reason = db.should_scrape(
                "race",
                race['schedule_id'],
                validity_hours=1  # 1 hour for races
            )

            if should_scrape:
                if reason == "not_in_cache":
                    print(f"→ New race found: {race['name']}")
                elif "status_ongoing" in reason:
                    print(f"⟳ Live race: {race['name']}")
                elif "status_upcoming" in reason:
                    print(f"⏰ Upcoming race: {race['name']}")
                elif "stale" in reason:
                    print(f"⟳ Refreshing: {race['name']} ({reason})")

                # Actually scrape it
                scrape_race(race['schedule_id'])
            else:
                if "completed_stable" in reason:
                    print(f"✓ Skipped completed: {race['name']}")
                else:
                    print(f"✓ Cached: {race['name']} ({reason})")
```

**Output:**
```
✓ Cached: Race 1 - Daytona (completed_stable)
✓ Cached: Race 2 - Talladega (completed_stable)
...
✓ Cached: Race 8 - Phoenix (completed_stable)
⟳ Live race: Race 9 - Homestead
→ New race found: Race 10 - Atlanta
⏰ Upcoming race: Race 11 - Bristol
⏰ Upcoming race: Race 12 - Martinsville
```

Only races 9-12 are scraped!

## Database Method: `should_scrape()`

```python
should_scrape, reason = db.should_scrape(
    entity_type="race",
    entity_id=67890,
    validity_hours=1
)

# Returns:
# (True, "not_in_cache")                    - Entity not in DB
# (True, "cache_stale (25.3h > 24h)")       - Too old
# (True, "status_ongoing_needs_refresh")    - Race is live
# (False, "status_completed_stable")        - Completed race
# (False, "cache_fresh (2.1h < 24h)")       - Recent enough
```

## Configuration-Driven Validity

In `config.yaml`:

```yaml
cache:
  validity_hours:
    league: 168   # 1 week
    series: 168   # 1 week
    season: 24    # 1 day
    race: 1       # 1 hour
    results: 1    # 1 hour

  # Override for specific statuses
  status_overrides:
    completed: null     # Never refresh completed races
    ongoing: 0          # Always refresh ongoing races
    active: 4           # Refresh active seasons every 4 hours
```

## Real-World Usage

### Scenario 1: Daily Update Job

```bash
# Run this daily at 2am
# Only scrapes what's changed/new
uv run python scraper.py update-all --league 1558
```

**What happens:**
1. League: Skipped (last scraped 2 days ago, < 1 week)
2. Series: Skipped (stable data)
3. Active seasons: Refreshed (status = active)
4. Completed seasons: Skipped (stable)
5. Ongoing races: Always refreshed
6. Completed races: Skipped
7. Upcoming races: Checked if new

### Scenario 2: Race Night Update

```bash
# Run during a race event
uv run python scraper.py scrape race 67890 --validity-hours 0
```

Forces immediate refresh of the specific race.

### Scenario 3: Full Refresh After Site Change

```bash
# Site changed structure, need everything fresh
uv run python scraper.py scrape-all --league 1558 --force
```

Ignores all cache, re-scrapes everything.

## Implementation Status

### ✅ What's Implemented

- `Database.should_scrape()` method
- Time-based cache validity
- Status-based logic for races/seasons
- Simple league scraper uses smart caching

### 🔨 What's Needed

To fully enable progressive scraping, you need:

1. **Season scraper** that checks each race
2. **Race scraper** that respects status
3. **Update command** that walks the hierarchy
4. **Config loading** for validity hours
5. **Logging** to track what was scraped vs skipped

### Quick Implementation

Here's the pattern for any entity:

```python
def scrape_entity(entity_id, force=False, validity_hours=24):
    with Database() as db:
        if not force:
            should_scrape, reason = db.should_scrape(
                "entity_type",
                entity_id,
                validity_hours
            )

            if not should_scrape:
                print(f"Skipping: {reason}")
                return db.get_entity(entity_id)

        # Fetch and parse
        data = fetch_and_parse(entity_id)

        # Store with status
        db.upsert_entity(entity_id, {
            **data,
            "status": determine_status(data),
            "scraped_at": datetime.now().isoformat()
        })

        return db.get_entity(entity_id)
```

## Benefits

### Network Efficiency
- **Before**: 1000 races × 2 seconds = 33 minutes
- **After**: 10 new races × 2 seconds = 20 seconds

### Respectful Scraping
- Minimizes server load
- Respects rate limits better
- Only scrapes what's needed

### Data Freshness
- Live races always fresh
- Completed races never change
- Configurable staleness

## Next Steps

To make this fully functional:

1. Add `status` tracking to all `upsert_*` methods
2. Implement scrapers for seasons, races, results
3. Create `update-all` command that uses `should_scrape()`
4. Add config file loading for validity hours
5. Log scrape decisions (scraped vs skipped)

See Task 2.6 (Logging) and Task 4.x (Orchestration) in TASKS.md.
