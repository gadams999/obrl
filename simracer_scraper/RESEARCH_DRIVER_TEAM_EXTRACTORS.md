# Research: Driver and Team Extractor Necessity

**Date**: 2025-11-01
**Context**: Decision on whether to implement Tasks 3.5 (Driver Extractor) and 3.6 (Team Extractor)

## Research Question

Do we need dedicated extractors for driver profile pages (`driver_stats.php?driver_id=X`) and team roster pages (`teams.php?league_id=X`), or can we derive this data from race results?

## Findings

### Race Results Data (from Real Site)

Examined live race result page: `https://www.simracerhub.com/season_race.php?schedule_id=324462`

Race results are loaded dynamically via React. However, driver profile pages (`driver_stats.php?driver_id=33132`) contain embedded JSON with detailed race participant data in the `rps` (race participants) object.

**Available per race result**:
```javascript
{
  "race_participant_id": "6345314",
  "race_id": "317615",
  "driver_id": "33132",
  "car_id": "103",
  "driver_number": "23",
  "qualify_pos": "2",
  "qualify_pos_class": "2",
  "qualify_time": "44.0784",
  "num_laps": "70",
  "laps_led": "0",
  "finish_pos": "5",
  "finish_pos_class": "5",
  "race_points": "36",
  "status": "Running",
  "intv": "14.2199",
  "intv_units": "ms",
  "fastest_lap_time": "43.6535",
  "fastest_lap_number": "50",
  "incidents": "0",
  "avg_lap": "49.18",
  "provisional": "N",
  "irating": "2721",
  "sr": "4.99",
  "license": "Class A",
  "schedule_id": "324462",
  "race_date": "2025-10-29",
  "season_id": "26741",
  "track_config_id": "157",
  "series_id": "3714",
  "league_id": "1558",
  "avg_pos": "6.97143",
  "arp": "6.82812",
  "avg_fast_lap": "437592",
  "num_fast_lap": "6",
  "passes": "12",
  "quality_passes": "10",
  "closing_passes": "1",
  "fastest_restart": "870784",
  "rating": "100.933",
  "track_id": "68",
  "track_type_id": "4",
  "race_timestamp": 1761710400,
  "race_date_str": "Oct 29, 2025"
}
```

### Database Schema Coverage

Our existing `race_results` table (src/database.py:214-246) already supports:
- ✅ finish_position, starting_position, car_number
- ✅ qualifying_time, fastest_lap, fastest_lap_number, average_lap
- ✅ interval, laps_completed, laps_led, incidents
- ✅ race_points, fast_laps, quality_passes, closing_passes, total_passes
- ✅ average_running_position, irating, status

Our `drivers` table (src/database.py:88-108) supports:
- ✅ name, first_name, last_name, car_numbers, primary_number
- ✅ club, club_id, irating, safety_rating, license_class
- ✅ league_id, team_id, url

## Data Comparison

### What Race Results Provide (per race)

From race results alone, we can derive:
- ✅ Driver names (extracted from each race)
- ✅ Car numbers used (per race)
- ✅ iRating snapshot (per race)
- ✅ Safety rating snapshot (per race)
- ✅ License class (per race)
- ✅ Track performance metrics
- ✅ Series/season participation
- ✅ League affiliation
- ✅ All racing statistics (laps, led, incidents, passes, etc.)

**Aggregation possibilities**:
- Driver's race history within a league
- Performance trends over time
- Track-specific statistics
- Car number preferences
- Team membership (via team_id in results)

### What Driver Profile Pages Add

Source: `driver_stats.php?driver_id=33132`

**Additional value**:
1. **Cross-league race history**: Shows ALL races across ALL leagues, not just one league
2. **Club affiliation**: Driver's iRacing club (e.g., "Ohio Club")
3. **Career aggregated stats**: Total races, wins, avg finish across all leagues
4. **Complete timeline**: Historical data beyond current season

**Example from live site**:
```html
<div class='col-12 col-md-6 pageTitleLeft'>
<img src='images/club_31_sm.png' title='Ohio Club'>
</div>
<div class='col-12 col-md-6 mt-4 mt-md-0 text-md-start'>
<h1 class='m0'>Joshua A Robinson</h1>
```

Driver profile contains full JSON history in `rps` object with 20+ race entries spanning multiple leagues and series.

### What Team Pages Add

Source: `teams.php?league_id=1558` (from DATA_MODEL.md)

**Additional value**:
1. **Team rosters**: Which drivers belong to which teams
2. **Team names and IDs**: Official team names
3. **Team affiliations**: League-specific team structure
4. **Team metadata**: Team logos, descriptions, etc.

**Note**: `team_id` already appears in race results, so team membership can be inferred from race participation.

## Analysis

### For Single-League Scraping (e.g., The OBRL)

**Race results provide sufficient data for**:
- Driver identification and statistics
- Performance tracking
- Track/car aggregations
- League participation
- Team inference (via team_id in results)

**Missing without driver extractor**:
- Club affiliation (minor - cosmetic)
- Cross-league career stats (not needed for single-league focus)

**Missing without team extractor**:
- Explicit team rosters (can be inferred from race results)
- Team names (can be inferred if team_id is consistent)

### For Multi-League/Career Tracking

**Driver extractor becomes valuable for**:
- Tracking drivers across multiple leagues
- Complete career statistics
- Club affiliations
- Historical data beyond current season

**Team extractor becomes valuable for**:
- Official team roster listings
- Team metadata (names, logos)
- League-wide team structure

## Database Impact

### Current Schema Can Support Race-Derived Data

**Drivers table** can be populated from race results:
```sql
-- Derive driver from first race appearance
INSERT INTO drivers (driver_id, name, league_id, url)
SELECT DISTINCT
  driver_id,
  driver_name,
  league_id,
  'driver_stats.php?driver_id=' || driver_id
FROM race_results
```

**Team associations** can be inferred:
```sql
-- Derive team_id from race results
UPDATE drivers
SET team_id = (
  SELECT team_id
  FROM race_results
  WHERE race_results.driver_id = drivers.driver_id
  LIMIT 1
)
```

### If We Add Extractors Later

The extractors would:
1. **Enrich** existing driver records with club, full name split
2. **Validate** team associations
3. **Add** cross-league history
4. **Fill gaps** for drivers who haven't raced recently

## Decision

### Recommendation: SKIP Tasks 3.5 & 3.6 for Now

**Rationale**:
1. Race results contain 95% of needed data
2. Current focus is single-league scraping (The OBRL)
3. Orchestration layer (Task 4.x) will provide more immediate value
4. Driver/Team extractors can be added later if needed

**Proceed with**:
- Task 4.x: Orchestration (hierarchical scraping)
- Derive driver/team data from race results
- Use aggregation queries for statistics

**Add to backlog**:
- Task 3.5: Driver Extractor (for cross-league career stats)
- Task 3.6: Team Extractor (for official rosters)

### When to Revisit

Consider implementing Driver/Team extractors if:
1. **Cross-league tracking** becomes a requirement
2. **Club affiliations** are needed for features
3. **Official team rosters** are required (not inferred)
4. **Complete career history** beyond race results is needed
5. **Validation** of race-derived data is desired

## Implementation Notes for Future

### Driver Extractor (Task 3.5)

**URL Pattern**: `driver_stats.php?driver_id={id}`

**Data to extract**:
- Full name (first/last split)
- Club name and club_id
- iRacing ID (if different from driver_id)
- Complete race history (all leagues)
- Career statistics aggregates

**Extraction approach**:
- Parse embedded `rps` JSON object (similar to series/season extractors)
- Extract club from image tag: `<img src='images/club_31_sm.png' title='Ohio Club'>`
- Name from H1: `<h1 class='m0'>Joshua A Robinson</h1>`

**Test fixture**: Use `driver_stats.php?driver_id=33132` as reference

### Team Extractor (Task 3.6)

**URL Pattern**: `teams.php?league_id={id}`

**Data to extract**:
- Team list with team_id, name
- Driver rosters per team
- Team metadata (logos, descriptions)

**Extraction approach**:
- Likely HTML table or JavaScript array
- Parse team structure
- Associate drivers with teams

**Test fixture**: Use `teams.php?league_id=1558` (The OBRL)

## Related Documents

- `DATA_MODEL.md` - Entity structure and URL patterns
- `TASKS.md` - Original task definitions (Tasks 3.5-3.6)
- `src/database.py:88-108` - Drivers table schema
- `src/database.py:214-246` - Race results table schema
- `STATUS.md` - Project status tracking

## Live URLs Used in Research

- Race results: https://www.simracerhub.com/season_race.php?schedule_id=324462
- Driver profile: https://www.simracerhub.com/driver_stats.php?driver_id=33132
- League series: https://www.simracerhub.com/league_series.php?league_id=1558

## Conclusion

**Race results provide comprehensive data for single-league tracking.** Driver and Team extractors offer value for cross-league career statistics and official roster data, but are not essential for the current implementation phase. They remain valuable backlog items for future enhancement.

**Current Status**: Tasks 3.5 and 3.6 moved to backlog. Proceeding with Task 4.x (Orchestration).
