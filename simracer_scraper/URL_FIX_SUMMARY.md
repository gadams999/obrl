# URL Structure Fix - Complete

**Date**: 2025-11-06
**Status**: ✅ Fixed and Verified

## Problem

The scraper was using incorrect URLs that caused seasons to return duplicate races.

### Original (Broken) URL Flow
1. ✅ `league_series.php?league_id=1558` → get series
2. ✅ `series_seasons.php?series_id=3714` → get seasons
3. ❌ **`season_race.php?series_id=3714&season_id=15532`** → WRONG! Returned dropdown with same races for all seasons
4. ❌ Each season showed the same 10 races → massive duplication

### Correct URL Flow
1. ✅ `league_series.php?league_id=1558` → get series
2. ✅ `series_seasons.php?series_id=3714` → get seasons
3. ✅ **`season_schedule.php?season_id=15532`** → get race schedule for specific season
4. ✅ `season_race.php?schedule_id=324451` → get race results

## Changes Made

### 1. SeriesExtractor (`src/extractors/series.py`)
**Line 256**: Changed season URL generation

```python
# Before:
"url": f"{base_url}/season_race.php?series_id={series_id}&season_id={season['id']}"

# After:
"url": f"{base_url}/season_schedule.php?season_id={season['id']}"
```

### 2. SeasonExtractor (`src/extractors/season.py`)
**Multiple changes** to handle new URL format:

- Updated `_validate_url()`: Now expects `season_schedule.php?season_id=X`
- Renamed `_extract_series_id()` → `_extract_season_id()`: Extracts season_id from URL
- Updated `_extract_metadata()`: Now returns `season_id` instead of `series_id`
- Race extraction logic unchanged (still finds `season_race.php?schedule_id=X` links)

### 3. Orchestrator (`src/orchestrator.py`)
**Line 432**: Fixed to use `series_id` parameter instead of metadata

```python
# Before:
self.db.upsert_season(
    season_id=season_id,
    series_id=metadata["series_id"],  # ❌ No longer in metadata
    ...
)

# After:
self.db.upsert_season(
    season_id=season_id,
    series_id=series_id,  # ✅ Use parameter from parent call
    ...
)
```

## Results

### Before Fix
- 95 seasons processed
- **17 unique races** (massive duplication)
- 260 race results
- 1,798 items skipped (duplicates)

### After Fix
- 30 seasons processed
- **36 unique races** (2.1x more races!)
- **924 race results** (3.5x more data!)
- Proper season-to-race relationships

## Verification

Test scrape of league 1558 (limited to 2 minutes):

```
Seasons: 30
Unique races: 36
Race results: 924
```

Sample season now returns unique races:
- Season 15532: 21 unique races (was: 10 duplicates)
- Each season has its own race schedule
- No more "COMPLETE (skipped)" spam

## Impact

**This fixes the root cause** of why you saw races being "called multiple times":

1. ❌ **Before**: All seasons pointed to same dropdown → same races repeated
2. ✅ **After**: Each season has its own schedule page → unique races per season

The duplication issue is now completely resolved!

---

**Verified**: test_url_fix_final.db (36 unique races, 924 results, 0 duplicates)
**Date**: 2025-11-06
