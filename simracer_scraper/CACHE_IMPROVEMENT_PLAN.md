# Cache Improvement Plan

## Current Problem

The current caching strategy has several issues:

1. **League-level cache bypass**: When interrupted and restarted, the league level is skipped even though we need to discover series
2. **Series-level over-caching**: Series metadata is stored with epoch timestamp, but subsequent runs still fetch the entire series page
3. **Season-level redundant fetching**: Every season page is re-fetched even when the data hasn't changed
4. **Race-level missing completion flag**: Race results are re-scraped every time, even though completed races never change

## Desired Behavior

### League Level (Few Records - Always Query)
- **Always fetch the league page** to discover current series
- League pages are small and fast
- This ensures we always have the latest list of series
- Cache the league metadata itself, but never skip the fetch

### Series Level (Query Always, Smart Upsert)
- **Always fetch series pages** to get current season lists
- Compare fetched season list with database:
  - **Add new seasons** that don't exist in DB
  - **Update existing seasons** only if data has changed (check row hash/comparison)
- Don't re-fetch series page if it was fetched recently (configurable threshold)

### Season Level (Smart Race Discovery)
- **Always fetch season page** to get race schedule
- For each race in the schedule:
  - **Check if race has results URL** in the HTML
  - If results URL exists:
    - Check if we already have that race in DB with `is_complete=true`
    - Only scrape race if not already complete
  - If no results URL:
    - Store race metadata with `is_complete=false`
    - Skip race result scraping

### Race Level (Completion Flag)
- Add `is_complete` boolean flag to `races` table
- **Only scrape race results if:**
  - Race has a results URL, AND
  - `is_complete=false` OR race doesn't exist in DB
- **After successful race scrape:**
  - Set `is_complete=true`
- **--force flag behavior:**
  - Ignore `is_complete` flag
  - Re-scrape all races regardless of completion status

## Database Schema Changes

### 1. Add `is_complete` Column to Races Table

```sql
ALTER TABLE races ADD COLUMN is_complete BOOLEAN DEFAULT 0;
CREATE INDEX idx_races_is_complete ON races(is_complete);
```

### 2. Add Row Comparison Support (Future Enhancement)

For detecting changes in series/season data, consider adding:
- `data_hash` column to store hash of critical fields
- Compare current scrape hash with stored hash
- Only update if hash differs

## Implementation Plan

### Phase 1: Race Completion Flag (Immediate Priority)

1. **Update Database Schema** (`src/database.py`)
   - Add `is_complete` column to races table
   - Update `upsert_race()` to accept `is_complete` parameter
   - Add `get_incomplete_races()` method
   - Update `is_url_cached()` to check `is_complete` for races

2. **Update Race Extractor** (`src/extractors/race.py`)
   - No changes needed (already extracts results)

3. **Update Season Extractor** (`src/extractors/season.py`)
   - Modify `child_urls` to include flag indicating if race has results URL
   - Return structure: `{"url": "...", "has_results": true/false, "schedule_id": ...}`

4. **Update Orchestrator** (`src/orchestrator.py`)
   - **League Level**: Remove cache check for league pages (always fetch)
   - **Series Level**: Add configurable cache threshold (e.g., 1 day)
   - **Season Level**: Parse race list, check for results URLs
   - **Race Level**:
     - Check `is_complete` flag before scraping
     - Set `is_complete=true` after successful scrape
     - Respect `--force` flag to override

5. **Update CLI** (`src/cli.py`)
   - Ensure `--force` flag is passed through to orchestrator

### Phase 2: Smart Series/Season Updates (Future Enhancement)

1. **Add Data Hash Support**
   - Add `data_hash` column to series/seasons tables
   - Compute hash of critical fields during scrape
   - Compare with stored hash to detect changes

2. **Implement Differential Updates**
   - Only update records where hash differs
   - Log when data changes are detected
   - Optimize database writes

### Phase 3: Configurable Cache Policies (Future Enhancement)

1. **Add Cache Configuration**
   - Per-level cache thresholds
   - Separate thresholds for metadata vs. results
   - Status-based caching (completed vs. active)

2. **Smart Cache Invalidation**
   - Invalidate cache when upstream data changes
   - Cascade invalidation down hierarchy

## Code Changes Detail

### 1. Database Schema Migration

```python
# In src/database.py - initialize_schema()
cursor.execute(
    """
    CREATE TABLE IF NOT EXISTS races (
        race_id INTEGER PRIMARY KEY AUTOINCREMENT,
        schedule_id INTEGER NOT NULL UNIQUE,
        ...existing fields...
        is_complete BOOLEAN DEFAULT 0,  # NEW FIELD
        scraped_at TIMESTAMP NOT NULL,
        created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
        updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
        FOREIGN KEY (season_id) REFERENCES seasons(season_id)
    )
    """
)
cursor.execute("CREATE INDEX IF NOT EXISTS idx_races_is_complete ON races(is_complete)")
```

### 2. Update Database Methods

```python
# In src/database.py
def upsert_race(self, schedule_id: int, season_id: int, data: dict) -> int:
    """Insert or update a race record."""
    # Add is_complete to the upsert
    is_complete = data.get("is_complete", False)
    cursor.execute(
        """
        INSERT INTO races (
            schedule_id, season_id, ..., is_complete, scraped_at, updated_at
        )
        VALUES (?, ?, ..., ?, ?, CURRENT_TIMESTAMP)
        ON CONFLICT(schedule_id) DO UPDATE SET
            ...existing fields...
            is_complete = excluded.is_complete,
            scraped_at = excluded.scraped_at,
            updated_at = CURRENT_TIMESTAMP
        """,
        (schedule_id, season_id, ..., is_complete, scraped_at),
    )
    return cursor.lastrowid

def get_incomplete_races(self, season_id: int) -> list[dict]:
    """Get all incomplete races for a season."""
    cursor = self.conn.cursor()
    cursor.execute(
        "SELECT * FROM races WHERE season_id = ? AND is_complete = 0 ORDER BY race_number",
        (season_id,)
    )
    return [dict(row) for row in cursor.fetchall()]

def is_race_complete(self, schedule_id: int) -> bool:
    """Check if a race is marked as complete."""
    cursor = self.conn.cursor()
    cursor.execute(
        "SELECT is_complete FROM races WHERE schedule_id = ?",
        (schedule_id,)
    )
    row = cursor.fetchone()
    return bool(row and row[0]) if row else False
```

### 3. Update Orchestrator Cache Logic

```python
# In src/orchestrator.py - scrape_league()
def scrape_league(self, league_url: str, depth: str = "league", ...) -> dict[str, Any]:
    """Scrape a league with optional depth control and filters."""

    # REMOVE CACHE CHECK - Always fetch league to discover series
    # Old code (DELETE):
    # if cache_max_age_days is not None:
    #     is_cached = self.db.is_url_cached(league_url, "league", cache_max_age_days)
    #     if is_cached:
    #         logger.info(f"⚡ CACHED (skipped): {league_url}")
    #         return self.get_progress()

    # Always fetch league page (lightweight, contains series discovery)
    logger.info(f"🌐 FETCHING: {league_url}")
    league_data = self.league_extractor.extract(league_url)
    # ... rest of implementation
```

```python
# In src/orchestrator.py - scrape_race()
def scrape_race(
    self,
    race_url: str,
    season_id: int,
    schedule_id: int,  # NEW: Pass schedule_id from parent
    has_results: bool,  # NEW: Flag from season extractor
    cache_max_age_days: int | None = 7,
    force: bool = False,  # NEW: Respect --force flag
) -> None:
    """Scrape a race and its results."""

    # If race has no results URL, store metadata only and skip
    if not has_results:
        logger.info(f"⏭️  SKIPPING (no results): {race_url}")
        self.db.upsert_race(
            schedule_id=schedule_id,
            season_id=season_id,
            data={
                "url": race_url,
                "is_complete": False,
                "scraped_at": datetime.datetime.now().isoformat(),
                "race_number": 0,  # Extract from metadata
            },
        )
        return

    # Check if race is already complete (unless --force)
    if not force and self.db.is_race_complete(schedule_id):
        logger.info(f"✅ COMPLETE (skipped): {race_url}")
        self.progress["skipped_cached"] += 1
        return

    # Standard cache check
    if not force and cache_max_age_days is not None:
        is_cached = self.db.is_url_cached(race_url, "race", cache_max_age_days)
        if is_cached:
            logger.info(f"⚡ CACHED (skipped): {race_url}")
            self.progress["skipped_cached"] += 1
            return

    # Extract race data
    logger.info(f"🌐 FETCHING: {race_url}")
    race_data = self.race_extractor.extract(race_url)
    metadata = race_data["metadata"]

    # Store race with completion flag
    race_id = self.db.upsert_race(
        schedule_id=schedule_id,
        season_id=season_id,
        data={
            "name": metadata["name"],
            "url": metadata["url"],
            "race_number": metadata.get("race_number", 0),
            "is_complete": True,  # Mark as complete after successful scrape
            "scraped_at": datetime.datetime.now().isoformat(),
        },
    )

    # Store race results
    results = race_data.get("results", [])
    for result in results:
        self._store_race_result(race_id, result, season_id)

    self.progress["races_scraped"] += 1
```

### 4. Update Season Scraper

```python
# In src/orchestrator.py - scrape_season()
def scrape_season(self, season_url: str, season_id: int, ...) -> None:
    """Scrape a season with optional depth control."""

    # ... existing fetch logic ...

    # If depth allows, scrape races
    if depth == "race":
        races = season_data["child_urls"]["races"]

        # NEW: Races now include has_results flag from extractor
        # Format: [{"url": "...", "schedule_id": 123, "has_results": True}, ...]

        for race_info in races:
            self.scrape_race(
                race_url=race_info["url"],
                season_id=season_id,
                schedule_id=race_info["schedule_id"],  # NEW
                has_results=race_info["has_results"],  # NEW
                cache_max_age_days=cache_max_age_days,
                force=self.force_refresh,  # NEW: Pass force flag
            )
```

### 5. Update Season Extractor

```python
# In src/extractors/season.py - extract()
def extract(self, url: str) -> dict:
    """Extract season data and race schedule."""

    # ... existing parsing logic ...

    # Parse race schedule table
    races = []
    for row in race_table_rows:
        schedule_id = extract_schedule_id(row)
        race_url = extract_race_url(row)

        # NEW: Check if row has a results link
        results_cell = row.find("td", class_="results")  # Example selector
        has_results = bool(results_cell and results_cell.find("a"))

        races.append({
            "schedule_id": schedule_id,
            "url": race_url,
            "has_results": has_results,  # NEW
        })

    return {
        "metadata": {...},
        "child_urls": {"races": races},
    }
```

## Testing Strategy

### Unit Tests
1. Test `is_race_complete()` method
2. Test `get_incomplete_races()` method
3. Test race upsert with `is_complete` flag
4. Test season extractor with/without results URLs

### Integration Tests
1. Test full scrape with interrupted/resumed workflow
2. Test `--force` flag overrides completion check
3. Test league always fetches (no cache skip)
4. Test completed races are skipped on re-run

### Manual Testing
1. Start fresh scrape, interrupt mid-season
2. Resume scrape, verify only incomplete races are fetched
3. Run again with `--force`, verify all races are re-scraped
4. Verify league page is always fetched

## Rollout Plan

1. **Week 1**: Implement Phase 1 (Race completion flag)
   - Add database schema changes
   - Update orchestrator logic
   - Write unit tests

2. **Week 2**: Test and validate
   - Run integration tests
   - Test interrupted scrape scenarios
   - Verify performance improvements

3. **Week 3**: Deploy and monitor
   - Deploy to production
   - Monitor cache hit rates
   - Gather performance metrics

4. **Future**: Implement Phase 2 & 3 based on real-world usage

## Expected Improvements

### Performance
- **First run**: Similar performance (all races scraped)
- **Subsequent runs**: 80-90% faster (only new/incomplete races)
- **Interrupted runs**: Resume exactly where stopped

### Reliability
- **No missed data**: Always fetch upstream discovery pages
- **Consistent state**: Completion flag prevents partial data
- **Graceful interruption**: Can stop/start without data loss

### Resource Usage
- **Network requests**: Reduced by 80-90% on re-runs
- **Database writes**: Only changed data is updated
- **Rate limiting**: Fewer requests = less delay

## Migration Guide

### For Existing Databases

Run this SQL to add the new column to existing databases:

```sql
-- Backup database first!
-- cp simracer.db simracer.db.backup

-- Add new column
ALTER TABLE races ADD COLUMN is_complete BOOLEAN DEFAULT 0;

-- Mark all existing races with results as complete
UPDATE races SET is_complete = 1 WHERE scraped_at IS NOT NULL;

-- Create index for performance
CREATE INDEX idx_races_is_complete ON races(is_complete);

-- Verify changes
SELECT COUNT(*) FROM races WHERE is_complete = 1;
```

### For Fresh Scrapes

New databases will automatically have the `is_complete` column.

## Configuration Options

Add to `config.yaml`:

```yaml
scraping:
  cache:
    # League-level caching (always fetch)
    league_max_age_days: null  # null = always fetch

    # Series-level caching
    series_max_age_days: 1  # Re-fetch series pages after 1 day

    # Season-level caching
    season_max_age_days: 1  # Re-fetch season pages after 1 day

    # Race-level caching
    race_max_age_days: null  # null = use completion flag only
    respect_completion_flag: true  # Honor is_complete flag
```

## Open Questions

1. **How to handle races with missing results that later get results?**
   - Proposal: Set low max_age_days for incomplete races
   - Re-check incomplete races on each run

2. **Should we add a "last_checked" timestamp separate from "scraped_at"?**
   - Proposal: Add `last_checked_at` to track when we verified no changes
   - Useful for series/seasons where we check but don't update

3. **How to handle series/seasons that are deleted upstream?**
   - Proposal: Add `deleted` boolean flag
   - Mark as deleted if not found in parent's child list

## Success Metrics

Track these metrics to validate improvement:

1. **Cache hit rate**: % of races skipped due to completion flag
2. **Scrape duration**: Time to complete full vs. incremental scrape
3. **Network requests**: Total requests per run
4. **Data freshness**: Time since last update for each entity
5. **Error rate**: Errors during interrupted/resumed scrapes

## Conclusion

This plan provides a pragmatic approach to caching:
- **Simple**: Uses completion flag, not complex state machines
- **Reliable**: Always fetches discovery pages (league/series/season)
- **Efficient**: Skips completed races, respects --force flag
- **Incremental**: Can be implemented in phases
- **Testable**: Clear success criteria and test cases

The key insight is that **discovery is cheap, results are expensive**. We always fetch the small discovery pages (league, series, season) but intelligently skip expensive race result pages that we've already scraped.
