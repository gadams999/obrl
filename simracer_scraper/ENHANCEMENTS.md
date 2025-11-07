# SimRacer Scraper - Enhancement Roadmap

**Created**: 2025-11-06
**Status**: Planned

## Overview

This document tracks planned enhancements to improve data completeness and accuracy.

## Enhancements

### 1. Complete Field Population in League, Series, and Season Tables

**Status**: Planned

**Current State**:
- League, series, and season tables exist with comprehensive schemas
- Not all fields are being populated during extraction
- Some metadata fields left NULL

**Goal**:
- Populate all available fields from league pages
- Extract all metadata from series pages (vehicle_type, day_of_week, etc.)
- Fill all season fields (year, start_date, end_date, status, etc.)

**Tasks**:
- [ ] Audit current extraction in `src/extractors/league.py`
- [ ] Identify missing field extractions
- [ ] Update JavaScript parser for series metadata
- [ ] Update season extractor for complete field extraction
- [ ] Add tests for all field extractions
- [ ] Verify data completeness in database

**Affected Files**:
- `src/extractors/league.py`
- `src/extractors/series.py`
- `src/extractors/season.py`
- `src/utils/js_parser.py`
- `tests/unit/test_league_extractor.py`
- `tests/unit/test_series_extractor.py`
- `tests/unit/test_season_extractor.py`

**Acceptance Criteria**:
- All league fields populated (name, description, organizer)
- All series fields populated (vehicle_type, day_of_week, active, season_count, created_date)
- All season fields populated (year, start_date, end_date, scheduled_races, completed_races, status)
- 100% test coverage maintained
- Database queries show no NULL values for available fields

---

### 2. Enhanced Driver Management with Auto-Discovery

**Status**: Planned

**Current State**:
- Drivers table exists in schema
- Driver IDs extracted from race results
- Basic driver info stored
- No automatic discovery/upsert when driver appears in races

**Goal**:
- Automatically upsert driver records when driver_id seen in race results
- Extract driver metadata from race result rows (name, car_number, team, irating, etc.)
- Build comprehensive driver database from race participation

**Tasks**:
- [ ] Update `src/extractors/race.py` to extract driver metadata from results
- [ ] Add driver upsert logic to race result processing
- [ ] Extract available driver fields from race table (name, car_number, team, irating, license_class)
- [ ] Update `src/database.py` upsert_driver() to handle partial data
- [ ] Implement driver discovery during race scraping
- [ ] Add tests for driver auto-discovery
- [ ] Add tests for driver upsert with partial data

**Affected Files**:
- `src/extractors/race.py`
- `src/database.py`
- `src/orchestrator.py`
- `tests/unit/test_race_extractor.py`
- `tests/unit/test_database.py`
- `tests/unit/test_orchestrator.py`

**Database Changes**:
None required - schema already supports this. Ensure upsert_driver() handles:
- Partial data (not all fields available from race results)
- Updates when more complete data found later
- Preserves existing data when new data is NULL

**Acceptance Criteria**:
- Driver records created automatically when seen in race results
- Driver metadata populated from race table (name, irating, team)
- Drivers table grows as races are scraped
- No duplicate driver records
- 100% test coverage maintained

---

### 3. Complete Driver Data Refresh Function

**Status**: Planned

**Current State**:
- Drivers discovered from race results (minimal data)
- No mechanism to fetch complete driver profiles
- Driver profile pages exist but not scraped

**Goal**:
- Function to refresh/complete driver data from driver profile pages
- Fetch comprehensive driver stats (irating, safety_rating, license_class, club, career stats)
- Update existing driver records with complete data

**Tasks**:
- [ ] Create or update `src/extractors/driver.py` (currently doesn't exist)
- [ ] Implement driver profile page extraction
- [ ] Parse driver stats page (driver_stats.php?driver_id={id})
- [ ] Add `refresh_driver_data(driver_id)` to orchestrator
- [ ] Add `refresh_all_drivers()` to orchestrator
- [ ] Add `scrape driver <driver_id>` CLI command
- [ ] Add `scrape drivers --refresh-all` CLI command
- [ ] Handle cases where driver profile page doesn't exist
- [ ] Add comprehensive tests
- [ ] Document driver refresh workflow

**New Files**:
- `src/extractors/driver.py` (new)
- `tests/unit/test_driver_extractor.py` (new)

**Affected Files**:
- `src/orchestrator.py`
- `src/cli.py`
- `src/database.py`
- `tests/unit/test_orchestrator.py`

**API Design**:
```python
# In Orchestrator
def refresh_driver_data(self, driver_id: int, cache_max_age_days: int = 7):
    """Fetch complete driver profile and update database."""
    pass

def refresh_all_drivers(self, cache_max_age_days: int = 7, league_id: int = None):
    """Refresh all drivers in database (optionally filtered by league)."""
    pass
```

```bash
# CLI commands
python scraper.py scrape driver 33132
python scraper.py scrape drivers --refresh-all
python scraper.py scrape drivers --refresh-all --league 1558
python scraper.py scrape drivers --refresh-all --force
```

**Acceptance Criteria**:
- `DriverExtractor` class created
- Driver profile page fully parsed
- All driver fields populated (irating, safety_rating, license_class, club, etc.)
- CLI commands working
- Batch refresh function for all drivers
- 100% test coverage
- Documentation updated

---

### 4. Refactor Races Table - Separate Race and Driver Details

**Status**: Planned

**Current State**:
- `races` table contains race metadata (track, date, conditions, etc.)
- `race_results` table contains driver-specific data (positions, times, points, etc.)
- **Problem**: Race-level stats (leaders, lead_changes) stored in races table but these are aggregate stats, not per-driver

**Issue**:
The current schema mixes race-level and driver-level data:
- `races.leaders` - Number of different leaders (race-level stat)
- `races.lead_changes` - Number of lead changes (race-level stat)
- `race_results.laps_led` - Laps led by this driver (driver-level stat)

These should remain in the `races` table as they describe the race itself, not individual drivers.

**Goal**:
- Clarify separation between race-level and driver-level data
- Ensure race-level aggregate stats (leaders, lead_changes, cautions if available) stay in races table
- Ensure driver-specific data stays in race_results table
- Add any missing race-level fields (cautions, race duration, etc.)

**Tasks**:
- [ ] Audit current races table schema
- [ ] Identify any race-level fields missing (cautions, safety car laps, etc.)
- [ ] Add missing race-level fields to races table schema
- [ ] Update `src/extractors/race.py` to extract race-level stats
- [ ] Ensure clear separation in extraction logic
- [ ] Update database.py upsert methods
- [ ] Update tests
- [ ] Add migration notes if schema changes required

**Potential Schema Additions**:
```sql
-- Add to races table if data available
ALTER TABLE races ADD COLUMN cautions INTEGER;
ALTER TABLE races ADD COLUMN caution_laps INTEGER;
ALTER TABLE races ADD COLUMN safety_car_laps INTEGER;
ALTER TABLE races ADD COLUMN green_flag_laps INTEGER;
```

**Affected Files**:
- `src/database.py` (schema)
- `src/extractors/race.py`
- `tests/unit/test_race_extractor.py`
- `tests/unit/test_database.py`
- `ARCHITECTURE.md` (schema documentation)

**Acceptance Criteria**:
- Clear documentation of race-level vs driver-level data
- All available race-level stats extracted and stored
- No driver-specific data in races table
- No race-level data in race_results table
- Schema changes documented
- Migration path provided if needed
- 100% test coverage maintained
- ARCHITECTURE.md updated with clarified schema

---

## Implementation Priority

**Recommended Order**:

1. **Enhancement 4** (Refactor races table)
   - Foundation for other enhancements
   - Clarifies data model
   - Schema changes should be done first

2. **Enhancement 1** (Complete field population)
   - Improves existing extractors
   - No schema changes
   - Quick wins

3. **Enhancement 2** (Driver auto-discovery)
   - Builds on race extraction
   - No new extractors needed
   - Leverages existing data flow

4. **Enhancement 3** (Driver refresh function)
   - Requires new extractor
   - More complex
   - Builds on Enhancement 2

## Testing Strategy

For each enhancement:
- Write tests first (TDD)
- Maintain 100% coverage
- Test with real fixtures
- Test edge cases (missing data, NULL fields)
- Test upsert behavior (new vs update)

## Documentation Updates Required

After each enhancement:
- Update ARCHITECTURE.md (schema, data flow)
- Update DEVELOPMENT.md (new workflows, CLI commands)
- Update README.md if user-facing changes
- Add code comments for complex logic

## Success Metrics

**Enhancement 1**:
- Zero NULL values for available fields in league/series/season tables
- All metadata fields populated

**Enhancement 2**:
- Driver records auto-created for all participants
- Drivers table grows as races scraped
- No orphaned driver IDs

**Enhancement 3**:
- Complete driver profiles for all drivers
- All driver fields populated (irating, safety_rating, club, etc.)
- CLI commands functional

**Enhancement 4**:
- Clear schema separation
- All race-level stats captured
- No data duplication or misplacement

## Notes

- All enhancements maintain backward compatibility where possible
- Schema changes require database migration or re-scrape
- 100% test coverage is non-negotiable
- Rate limiting must be maintained (2-4 seconds)
- All changes must respect ethical scraping principles

## Future Enhancements (Not Prioritized)

- Team extractor and complete team data
- Season standings extraction
- Historical trend analysis
- Data export (JSON/CSV)
- Incremental update mode (only scrape new races)
- Multi-league support
- Web dashboard for data visualization
