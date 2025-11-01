# SimRacer Hub - Discovered Data Model

**Date**: 2025-10-31
**Source**: https://www.simracerhub.com/
**Test League**: The OBRL presented by VctryLnSprts (league_id=1558)

## URL Patterns Discovered

### Entity URL Structure

| Entity | URL Pattern | Example | Notes |
|--------|-------------|---------|-------|
| League Series List | `league_series.php?league_id={id}` | `league_id=1558` | Entry point for league |
| Series Seasons | `series_seasons.php?series_id={id}` | `series_id=3714` | Lists all seasons for a series |
| Season Detail | `series_seasons.php?series_id={sid}&season_id={id}` | `series_id=3714&season_id=26741` | Specific season |
| Race Results | `season_race.php?series_id={id}` | `series_id=3714` | Lists all races in series |
| Individual Race | `season_race.php?schedule_id={id}` | `schedule_id=324462` | Detailed race results |
| Season Standings | `season_standings.php?series_id={id}` | `series_id=3714` | Current season standings |
| Driver Profile | `driver_stats.php?driver_id={id}` | `driver_id=33132` | Driver statistics |
| Teams/Roster | `teams.php?league_id={id}` | `league_id=1558` | All teams in league |
| League Stats | `league_stats.php?league_id={id}` | `league_id=1558` | League-wide statistics |
| Calendar | `calendar.php?league_id={id}` | `league_id=1558` | League calendar |

### Key Observations

1. **Query Parameter Based**: All URLs use query parameters, not REST-style paths
2. **Numeric IDs**: All identifiers are numeric integers
3. **Multiple ID Systems**:
   - `league_id` - League identifier
   - `series_id` - Series identifier
   - `season_id` - Season identifier
   - `schedule_id` - Race schedule identifier
   - `race_id` - Internal race identifier
   - `driver_id` - Driver identifier
   - `team_id` - Team identifier
4. **JavaScript Rendering**: Much data is embedded in JavaScript arrays/objects, not HTML tables

## Entity Hierarchy

```
League (league_id: 1558)
  ├── Teams (15 teams)
  │   └── Drivers (1-9 per team, ~60 total with affiliations)
  │
  └── Series (4 series)
       └── Seasons (28 seasons for Wed series alone)
            └── Races (10 races per season typically)
                 └── Results (40 participants per race)
```

## Entity: League

### Source Page
`league_series.php?league_id=1558`

### Available Fields

```python
{
    "league_id": int,              # 1558
    "name": str,                   # "The OBRL presented by VctryLnSprts"
    "description": str,            # Full league description
    "organizer": str,              # "VctryLnSprts"

    # Relationships
    "series_ids": List[int],       # [3714, 3713, 3712, 3711]
    "team_count": int,             # 15
}
```

### Child URLs to Extract
- Series: `series_seasons.php?series_id={id}` for each series
- Teams/Roster: `teams.php?league_id=1558`
- Stats: `league_stats.php?league_id=1558`
- Calendar: `calendar.php?league_id=1558`

### Extraction Notes
- Series data embedded in JavaScript array
- Each series has: `series_id`, `name`, `description`, `created_date`, `season_count`
- Navigation shows "Teams" link for roster access

## Entity: Series

### Source Page
`series_seasons.php?series_id=3714`

### Available Fields

```python
{
    "series_id": int,              # 3714
    "league_id": int,              # 1558 (parent)
    "name": str,                   # "The OBRL Wednesday Night Series"
    "description": str,            # Series description
    "active": bool,                # True/False
    "created_date": str,           # "Nov 12, 2024"
    "season_count": int,           # 28

    # Metadata (may vary)
    "vehicle_type": str,           # "NASCAR Trucks", "Gen6", "'87 Legends"
    "day_of_week": str,            # "Wednesday"
}
```

### Child URLs to Extract
- Seasons: JavaScript array contains season data (see Season entity)
- Standings: `season_standings.php?series_id={id}`
- Calendar: `calendar.php?series_id={id}` (inferred)
- Stats: `stats.php?series_id={id}` (inferred)

### Extraction Notes
- Season data in JavaScript: `seasons = [{id:26741, n:"2025 OBRL...", ...}, ...]`
- Each season object contains: `id`, `n` (name), `scrt` (start date timestamp), `scrt_str` (readable date), `ns` (scheduled races), `nr` (completed races), `hc`, `mc`, `psn`
- Must parse JavaScript to extract season list

## Entity: Season

### Source Page
`series_seasons.php?series_id=3714&season_id=26741` (or embedded in series page JS)

### Available Fields

```python
{
    "season_id": int,              # 26741
    "series_id": int,              # 3714 (parent)
    "name": str,                   # "2025 OBRL '87 Legends - Season 1"
    "year": int,                   # 2025
    "start_date": str,             # "Aug 5, 2025"
    "start_timestamp": int,        # Unix timestamp

    # Status
    "scheduled_races": int,        # ns: 10
    "completed_races": int,        # nr: 9
    "status": str,                 # "active" (derived from dates)

    # Flags (purpose TBD)
    "hc": bool,                    # false
    "mc": bool,                    # false
    "psn": int,                    # Paint scheme number?
}
```

### Child URLs to Extract
- Race list: `season_race.php?series_id={id}` (shows all races for current season)
- Standings: `season_standings.php?series_id={id}`

### Extraction Notes
- Season data primarily in JavaScript, minimal HTML rendering
- Status derived from comparing dates and nr/ns values
- Race list requires separate page fetch

## Entity: Race (Schedule)

### Source Page
`season_race.php?series_id=3714` (list) or `season_race.php?schedule_id=324462` (detail)

### Available Fields

```python
{
    # Identifiers
    "schedule_id": int,            # 324462 (URL parameter)
    "race_id": int,                # 317615 (internal ID)
    "season_id": int,              # 26741 (parent)
    "series_id": int,              # 3714 (parent)

    # Race Info
    "race_number": int,            # 10 (Race10)
    "name": str,                   # "Daytona International Speedway"
    "track": str,                  # Track name
    "track_config": str,           # "Dual Pit Roads"
    "track_type": str,             # "Oval", "Asphalt"
    "date": str,                   # "Oct 29, 2025"

    # Race Metadata
    "duration": str,               # "0h 58m"
    "laps": int,                   # 70
    "leaders": int,                # 6
    "lead_changes": int,           # 9

    # Conditions
    "weather": str,                # "Partly Cloudy"
    "temperature": str,            # "25° C"
    "humidity": str,               # "0%"

    # Status
    "status": str,                 # "completed", "upcoming", etc.
}
```

### Child URLs to Extract
- Results: Same page contains results table
- Each driver links to: `driver_stats.php?driver_id={id}&season_id={season_id}`

### Extraction Notes
- Race list shows: race number, track, date
- Individual race page has comprehensive results table
- Results table has 20+ columns of data per driver

## Entity: Race Result (per driver)

### Source Page
`season_race.php?schedule_id=324462`

### Available Fields

```python
{
    # Identifiers
    "result_id": int,              # Auto-generated (our DB)
    "race_id": int,                # 317615 (from parent)
    "schedule_id": int,            # 324462 (from URL)
    "driver_id": int,              # 33132 (extracted from profile link)
    "driver_name": str,            # "Joshua A Robinson"

    # Position
    "finish_position": int,        # 1-40
    "starting_position": int,      # Qualifying result
    "car_number": str,             # "23"

    # Timing
    "qualifying_time": str,        # "00:48.835"
    "fastest_lap": str,            # "00:49.023"
    "fastest_lap_number": int,     # Lap number of fastest lap
    "average_lap": str,            # "00:49.458"

    # Performance
    "laps_completed": int,         # 70
    "laps_led": int,               # 27
    "incidents": int,              # 0
    "interval": str,               # "+0.267" (gap to leader/ahead)

    # Points
    "race_points": int,            # 40
    "bonus_points": int,           # 3
    "total_points": int,           # 43

    # Advanced Stats
    "fast_laps": int,              # Number of laps in top N
    "quality_passes": int,         # Clean overtakes
    "closing_passes": int,         # Late race passes
    "total_passes": int,           # All passes
    "average_running_position": float,  # 2.357

    # iRacing Data
    "driver_rating": float,        # 2721
    "irating": int,                # 2721
    "safety_rating": float,        # 4.99
    "license_class": str,          # "A"

    # Status
    "status": str,                 # "Running", "Disqualified", "Disconnected"
    "car_type": str,               # Vehicle used

    # Team
    "team": str,                   # Team name (if affiliated)
}
```

### Extraction Notes
- Results table is HTML (not JavaScript)
- Each row contains 20+ columns
- Driver name is hyperlink to driver profile
- Some fields may be empty for DNF/DNS

## Entity: Driver

### Source Page
`driver_stats.php?driver_id=33132`

### Available Fields

```python
{
    # Identifiers
    "driver_id": int,              # 33132
    "iracing_id": int,             # Same as driver_id? (TBD)

    # Personal Info
    "name": str,                   # "Joshua A Robinson"
    "first_name": str,             # "Joshua A"
    "last_name": str,              # "Robinson"
    "car_numbers": List[str],      # ["23", "1", "30"]
    "primary_number": str,         # "23"

    # iRacing Info
    "club": str,                   # "Ohio Club (club_31)"
    "club_id": int,                # 31
    "irating": int,                # 2721
    "safety_rating": float,        # 4.99
    "license_class": str,          # "A"

    # Relationships
    "league_ids": List[int],       # [1558, 4818, 2517]
    "series_ids": List[int],       # [3714, 12162, ...]
    "season_ids": List[int],       # [26741, 24630, ...]
    "team_id": int,                # If affiliated (from teams page)
    "team_name": str,              # Team name

    # Career Stats (if enabled)
    "total_races": int,            # Career race count
    "wins": int,                   # Career wins
    "poles": int,                  # Career poles
    "top5": int,                   # Top 5 finishes
    "top10": int,                  # Top 10 finishes
    # ... additional career stats
}
```

### Extraction Notes
- React component contains race history in JSON
- Career stats require "Show Career Stats" toggle
- Race participant records: `{race_id, schedule_id, position, points, ...}`
- Can filter by league_id, series_id, season_id (URL params)

## Entity: Team

### Source Page
`teams.php?league_id=1558`

### Available Fields

```python
{
    # Identifiers
    "team_id": int,                # 29430
    "league_id": int,              # 1558 (parent)

    # Info
    "name": str,                   # "ToyMaker Racing"

    # Members
    "driver_names": List[str],     # ["Last, First", ...]
    "driver_count": int,           # 7
    "driver_ids": List[int],       # Must match names to IDs from other sources
}
```

### Extraction Notes
- Simple list structure
- Driver names formatted as "Last, First"
- No direct driver_id links (must match by name)
- Team standings available on season_standings.php

## Entity: Season Standings

### Source Page
`season_standings.php?series_id=3714`

### Available Fields

```python
{
    # Per Driver
    "position": int,               # 1-40
    "driver_id": int,              # From profile link
    "driver_name": str,            # "Joshua A Robinson"
    "team": str,                   # Team affiliation

    # Points
    "total_points": int,           # 395
    "bonus_points": int,           # 16
    "behind_next": int,            # Points behind next position
    "behind_lead": int,            # Points behind leader
    "change": int,                 # Position change since last race

    # Participation
    "starts": int,                 # 10
    "races_counted": int,          # Races counting toward points

    # Performance
    "wins": int,                   # 5
    "poles": int,                  # 5
    "top5": int,                   # 7
    "top10": int,                  # 10
    "laps": int,                   # Total laps completed

    # Incidents
    "incidents": int,              # Total incidents
    "incident_rate": float,        # Incidents per race
}
```

### Extraction Notes
- Two tables: Driver Standings and Team Standings
- Driver names link to profiles
- Toggle for "Race-by-Race Standings" view
- Team standings similar structure

## JavaScript Data Structures

### Series Array (in league_series.php)

```javascript
series.push({
    id: 3714,
    n: "The OBRL Wednesday Night Series",
    // ... additional fields
});
```

### Season Array (in series_seasons.php)

```javascript
seasons = [
    {
        id: 26741,
        n: "2025 OBRL '87 Legends - Season 1",
        scrt: 1754380800,           // Unix timestamp
        scrt_str: "Aug 5, 2025",
        ns: 10,                      // Scheduled races
        nr: 9,                       // Completed races
        hc: false,
        mc: false,
        psn: 1234
    },
    // ...
];
```

### Driver Race History (in driver_stats.php React component)

```javascript
{
    race_id: 317615,
    schedule_id: 324462,
    position: 1,
    points: 43,
    // ... many more fields
}
```

## Data Extraction Strategy

### Phase 1: League & Roster
1. Fetch `league_series.php?league_id={id}`
2. Parse JavaScript `series.push()` calls to get series list
3. Fetch `teams.php?league_id={id}` to get team roster
4. Extract all driver names (need to resolve to driver_ids later)

### Phase 2: Series & Seasons
1. For each series_id from Phase 1:
2. Fetch `series_seasons.php?series_id={id}`
3. Parse JavaScript `seasons = [...]` array
4. Extract all season metadata and IDs

### Phase 3: Races
1. For each series_id:
2. Fetch `season_race.php?series_id={id}` (shows most recent season)
3. Parse race list (numbered links)
4. Extract schedule_ids from race links

### Phase 4: Race Results
1. For each schedule_id:
2. Fetch `season_race.php?schedule_id={id}`
3. Parse HTML results table
4. Extract all driver results
5. Extract driver_ids from profile links

### Phase 5: Standings & Driver Details
1. Fetch `season_standings.php?series_id={id}` for each series
2. Extract current standings with driver_ids
3. For each unique driver_id:
4. Fetch `driver_stats.php?driver_id={id}`
5. Extract comprehensive driver profile

### Phase 6: Team Resolution
1. Match driver names from teams page to driver_ids
2. Update driver records with team affiliations

## Challenges & Solutions

### Challenge 1: JavaScript Embedded Data
**Problem**: Series and season data in JavaScript, not HTML
**Solution**: Use regex or JS parsing to extract arrays from `<script>` tags

### Challenge 2: Driver ID Resolution
**Problem**: Teams page has names but not driver_ids
**Solution**:
- Build driver name → driver_id map from race results
- Match team roster names to resolved IDs
- Handle name variations carefully

### Challenge 3: Multiple ID Systems
**Problem**: race_id vs schedule_id, both used for races
**Solution**:
- Use schedule_id as primary (in URLs)
- Store race_id as alternate/internal ID
- Document relationship

### Challenge 4: Season Context for Races
**Problem**: `season_race.php?series_id={id}` shows current season only
**Solution**:
- Need to iterate through seasons somehow
- May require season_id parameter discovery
- Or fetch from season_standings.php for each season

### Challenge 5: Historical Seasons
**Problem**: How to access races from older seasons?
**Solution**: Investigate if URL supports `season_id` parameter:
- `season_race.php?series_id={id}&season_id={id}`
- If not, may need alternative approach

## Updated Entity Relationships

```
League (1558)
  ├─[has many]─> Series (3714, 3713, 3712, 3711)
  ├─[has many]─> Teams (29430, 29424, ...)
  └─[has many]─> Drivers (via teams + race results)

Series (3714)
  ├─[belongs to]─> League (1558)
  ├─[has many]─> Seasons (26741, ...)
  └─[has many]─> Drivers (participants across all seasons)

Season (26741)
  ├─[belongs to]─> Series (3714)
  └─[has many]─> Races (via schedule_ids)

Race/Schedule (324462)
  ├─[belongs to]─> Season (26741)
  ├─[belongs to]─> Series (3714)
  └─[has many]─> Results (one per driver)

Result
  ├─[belongs to]─> Race (324462)
  └─[belongs to]─> Driver (33132)

Driver (33132)
  ├─[belongs to]─> Team (optional)
  ├─[participates in]─> Leagues (1558, ...)
  ├─[participates in]─> Series (3714, ...)
  └─[has many]─> Results

Team (29430)
  ├─[belongs to]─> League (1558)
  └─[has many]─> Drivers (1-9)
```

## Database Schema Updates Needed

### Add schedule_id to races table

```sql
ALTER TABLE races ADD COLUMN schedule_id INTEGER UNIQUE;
CREATE INDEX idx_races_schedule_id ON races(schedule_id);
```

### Add team_id to drivers table

```sql
ALTER TABLE drivers ADD COLUMN team_id INTEGER;
CREATE INDEX idx_drivers_team_id ON drivers(team_id);
```

### Add teams table

```sql
CREATE TABLE teams (
    team_id INTEGER PRIMARY KEY,
    league_id INTEGER NOT NULL,
    name TEXT NOT NULL,
    driver_count INTEGER,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (league_id) REFERENCES leagues(league_id)
);
```

### Expand race_results columns

Add discovered columns:
- `car_number`
- `qualifying_time`
- `fastest_lap_number`
- `fast_laps`
- `quality_passes`
- `closing_passes`
- `total_passes`
- `average_running_position`
- `irating`
- `car_type`

## Next Steps

1. Update SPECIFICATION.md with actual URL patterns
2. Implement JavaScript parsing for series/season arrays
3. Create extractor for each entity type
4. Build driver name resolution system
5. Implement orchestrator with corrected hierarchy
6. Add team entity support
7. Test with OBRL league (1558)
