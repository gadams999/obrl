# Enhancement 4: Race Table Schema Analysis

**Date**: 2025-11-06
**Status**: In Progress

## Current State Analysis

### Current Schema (races table)

```sql
CREATE TABLE races (
    race_id INTEGER PRIMARY KEY AUTOINCREMENT,
    schedule_id INTEGER NOT NULL UNIQUE,
    internal_race_id INTEGER,
    season_id INTEGER NOT NULL,
    race_number INTEGER NOT NULL,
    name TEXT,
    track TEXT,                    -- ❌ NOT POPULATED
    track_config TEXT,             -- ❌ NOT POPULATED
    track_type TEXT,               -- ❌ NOT POPULATED
    date TIMESTAMP,                -- ❌ NOT POPULATED
    duration TEXT,                 -- ❌ NOT POPULATED
    laps INTEGER,                  -- ❌ NOT POPULATED (race-level: total laps)
    leaders INTEGER,               -- ❌ NOT POPULATED (race-level: # of different leaders)
    lead_changes INTEGER,          -- ❌ NOT POPULATED (race-level: # of lead changes)
    weather TEXT,                  -- ❌ NOT POPULATED
    temperature TEXT,              -- ❌ NOT POPULATED
    humidity TEXT,                 -- ❌ NOT POPULATED
    url TEXT NOT NULL UNIQUE,
    status TEXT CHECK(status IN ('upcoming', 'ongoing', 'completed')),
    is_complete BOOLEAN DEFAULT 0,
    scraped_at TIMESTAMP NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (season_id) REFERENCES seasons(season_id)
)
```

### Current race_results Table

```sql
CREATE TABLE race_results (
    result_id INTEGER PRIMARY KEY AUTOINCREMENT,
    race_id INTEGER NOT NULL,
    driver_id INTEGER NOT NULL,
    finish_position INTEGER,
    starting_position INTEGER,
    car_number TEXT,
    qualifying_time TEXT,
    fastest_lap TEXT,
    fastest_lap_number INTEGER,
    average_lap TEXT,
    interval TEXT,
    laps_completed INTEGER,        -- ✅ DRIVER-LEVEL (this driver's laps)
    laps_led INTEGER,              -- ✅ DRIVER-LEVEL (this driver's laps led)
    incidents INTEGER,
    race_points INTEGER,
    bonus_points INTEGER,
    total_points INTEGER,
    fast_laps INTEGER,
    quality_passes INTEGER,
    closing_passes INTEGER,
    total_passes INTEGER,
    average_running_position REAL,
    irating INTEGER,
    status TEXT,
    car_type TEXT,
    team TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (race_id) REFERENCES races(race_id),
    FOREIGN KEY (driver_id) REFERENCES drivers(driver_id),
    UNIQUE(race_id, driver_id)
)
```

### Current Extraction Issues

**In `src/extractors/race.py`:**
- `_extract_metadata()` only extracts `schedule_id`, `url`, and `name`
- Does NOT extract: track, laps, leaders, lead_changes, weather, temperature, humidity, duration, date, etc.

**In `src/orchestrator.py` (lines 537-547):**
```python
race_id = self.db.upsert_race(
    schedule_id=metadata["schedule_id"],
    season_id=season_id,
    data={
        "name": metadata["name"],
        "url": metadata["url"],
        "race_number": metadata.get("race_number", 0),
        "is_complete": True,
        "scraped_at": datetime.datetime.now().isoformat(),
    },
)
```
Only passes: name, url, race_number, is_complete, scraped_at

## Data Separation: Race-Level vs Driver-Level

### Race-Level Data (belongs in `races` table)
These describe the race itself, independent of any driver:

✅ **Currently in Schema (needs extraction):**
- `track` - Track name (e.g., "Daytona International Speedway")
- `track_config` - Track configuration (e.g., "Dual Pit Roads")
- `track_type` - Track type (e.g., "Oval", "Asphalt")
- `date` - Race date/time
- `duration` - Race duration (e.g., "0h 58m")
- `laps` - Total laps in race (race configuration)
- `leaders` - Number of different drivers who led
- `lead_changes` - Number of times lead changed
- `weather` - Weather conditions
- `temperature` - Temperature
- `humidity` - Humidity

📋 **Missing from Schema (may not be available):**
- `cautions` - Number of caution periods
- `caution_laps` - Total laps under caution
- `safety_car_laps` - Laps under safety car
- `green_flag_laps` - Total green flag laps
- `pole_winner` - Driver who won pole position
- `pole_time` - Pole qualifying time
- `fastest_lap_winner` - Driver with fastest race lap
- `fastest_lap_time` - Fastest lap time in race
- `race_winner` - Driver who won race (redundant with results, but convenient)

### Driver-Level Data (belongs in `race_results` table)
These are specific to each driver's performance:

✅ **Currently Extracted:**
- `finish_position` - Where driver finished
- `starting_position` - Where driver started
- `car_number` - Driver's car number
- `laps_completed` - Laps this driver completed
- `laps_led` - Laps this driver led
- `incidents` - This driver's incidents
- `race_points`, `bonus_points`, `total_points` - Driver's points
- `fastest_lap` - Driver's fastest lap
- And many more...

## Implementation Plan

### Phase 1: Extract Existing Schema Fields

**Goal**: Populate the fields that already exist in the races table

**Tasks**:
1. Update `src/extractors/race.py`:
   - Add extraction logic for track, track_config, track_type
   - Add extraction logic for date, duration
   - Add extraction logic for laps (total race laps)
   - Add extraction logic for leaders, lead_changes
   - Add extraction logic for weather, temperature, humidity

2. Update `src/orchestrator.py`:
   - Pass all extracted metadata to `upsert_race()`

3. Update tests:
   - Add tests for each new extraction
   - Mock HTML with race metadata
   - Verify fields populated in database

**Affected Files**:
- `src/extractors/race.py` (main changes)
- `src/orchestrator.py` (minor changes to data passing)
- `tests/unit/test_race_extractor.py` (add tests)

### Phase 2: Add Missing Race-Level Fields (Optional)

**Goal**: Add fields for additional race-level stats if data is available

**Potential New Fields**:
```sql
ALTER TABLE races ADD COLUMN cautions INTEGER;
ALTER TABLE races ADD COLUMN caution_laps INTEGER;
ALTER TABLE races ADD COLUMN green_flag_laps INTEGER;
ALTER TABLE races ADD COLUMN pole_winner_id INTEGER;
ALTER TABLE races ADD COLUMN pole_time TEXT;
ALTER TABLE races ADD COLUMN fastest_lap_winner_id INTEGER;
ALTER TABLE races ADD COLUMN fastest_lap_time TEXT;
```

**Note**: These fields should only be added if:
1. Data is consistently available on race pages
2. Data is valuable for analysis
3. Data cannot be easily derived from race_results table

### Phase 3: Documentation

Update documentation to clarify data separation:
- ARCHITECTURE.md - Schema section
- Comments in database.py
- Comments in race extractor

## Race Page HTML Structure (Needs Investigation)

To complete Phase 1, we need to understand where each field appears on the race page:

**Known Locations**:
- Race name: `<h1>` tag or page title
- Results table: `<table>` with driver results

**Unknown Locations** (need to find):
- Track name, config, type: ???
- Date, duration: ???
- Laps: ??? (might be max of laps_completed from results)
- Leaders, lead_changes: ??? (might need to calculate from results)
- Weather, temperature, humidity: ???

**Action Items**:
1. Examine actual race page HTML
2. Identify CSS selectors for each field
3. Document extraction strategy for each field

## Migration Strategy

Since we're not adding new fields (just populating existing ones), no migration needed:
- Existing NULL values will be filled on next scrape
- Use `--force` flag to re-scrape all races and populate fields
- No breaking changes

## Success Criteria

**Phase 1 Complete When**:
- [ ] All existing race schema fields populated (no NULLs for available data)
- [ ] RaceExtractor extracts all race-level metadata
- [ ] Orchestrator passes all metadata to database
- [ ] Tests verify all field extractions
- [ ] 100% test coverage maintained
- [ ] Documentation updated

**Validation Query**:
```sql
-- Check that race fields are populated
SELECT
    COUNT(*) as total_races,
    COUNT(track) as has_track,
    COUNT(laps) as has_laps,
    COUNT(leaders) as has_leaders,
    COUNT(weather) as has_weather,
    COUNT(duration) as has_duration
FROM races
WHERE is_complete = 1;
```

Should show same count for all fields (or explain why some are NULL).

## Next Steps

1. ✅ Audit current schema (DONE)
2. 🔄 Examine race page HTML to find metadata locations
3. ⏳ Design extraction methods for each field
4. ⏳ Implement extractions in RaceExtractor
5. ⏳ Update orchestrator to pass data
6. ⏳ Write comprehensive tests
7. ⏳ Update documentation
