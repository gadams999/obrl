# SimRacer Scraper - Database Schema Reference

**Purpose**: Complete database structure documentation for Parquet/Spark export implementation
**Last Updated**: 2025-11-10
**Database**: SQLite3 (file: `obrl-nov8.db`)
**Schema Version**: Production

---

## Table of Contents

1. [Overview](#overview)
2. [Entity Hierarchy](#entity-hierarchy)
3. [Complete Table Schemas](#complete-table-schemas)
4. [Relationships & Foreign Keys](#relationships--foreign-keys)
5. [Indexes](#indexes)
6. [Data Types & Constraints](#data-types--constraints)
7. [Parquet Export Considerations](#parquet-export-considerations)

---

## Overview

The SimRacer Scraper uses a hierarchical relational database with 9 tables:

- **Core Entities**: `leagues`, `series`, `seasons`, `races`, `race_results`
- **Driver/Team Entities**: `drivers`, `teams`
- **Metadata Tables**: `scrape_log`, `schema_alerts`

**Total Tables**: 9
**Foreign Key Constraints**: Enabled (`PRAGMA foreign_keys = ON`)

---

## Entity Hierarchy

```
League (1 record typically: league_id=1558)
  ├── Teams (15 teams)
  │   └── Drivers (~60 drivers with team affiliations)
  │
  ├── Drivers (all drivers, ~60 total)
  │
  └── Series (4 series)
       └── Seasons (100+ seasons across all series)
            └── Races (1000+ races)
                 └── Race Results (40,000+ result records)
```

**Key Relationships**:
- 1 League → Many Series
- 1 Series → Many Seasons
- 1 Season → Many Races
- 1 Race → Many Race Results
- 1 Driver → Many Race Results
- 1 Team → Many Drivers (optional relationship)

---

## Complete Table Schemas

### 1. leagues

**Description**: Top-level racing league information

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `league_id` | INTEGER | PRIMARY KEY | SimRacerHub league identifier |
| `name` | TEXT | NOT NULL | League name |
| `description` | TEXT | | League description/details |
| `url` | TEXT | NOT NULL, UNIQUE | Full SimRacerHub URL |
| `scraped_at` | TIMESTAMP | NOT NULL | Last scrape timestamp |
| `created_at` | TIMESTAMP | DEFAULT CURRENT_TIMESTAMP | Record creation time |
| `updated_at` | TIMESTAMP | DEFAULT CURRENT_TIMESTAMP | Last update time |

**Indexes**:
- `idx_leagues_url` on `url`
- `idx_leagues_scraped_at` on `scraped_at`

**Typical Row Count**: 1

**Sample URL**: `https://www.simracerhub.com/league_series.php?league_id=1558`

---

### 2. teams

**Description**: Racing teams within a league

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `team_id` | INTEGER | PRIMARY KEY | SimRacerHub team identifier |
| `league_id` | INTEGER | NOT NULL, FK → leagues | Parent league |
| `name` | TEXT | NOT NULL | Team name |
| `driver_count` | INTEGER | | Number of affiliated drivers |
| `url` | TEXT | | Team profile URL (if available) |
| `scraped_at` | TIMESTAMP | NOT NULL | Last scrape timestamp |
| `created_at` | TIMESTAMP | DEFAULT CURRENT_TIMESTAMP | Record creation time |
| `updated_at` | TIMESTAMP | DEFAULT CURRENT_TIMESTAMP | Last update time |

**Foreign Keys**:
- `league_id` REFERENCES `leagues(league_id)`

**Indexes**:
- `idx_teams_league_id` on `league_id`
- `idx_teams_scraped_at` on `scraped_at`

**Typical Row Count**: 15

---

### 3. drivers

**Description**: Individual driver profiles and statistics

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `driver_id` | INTEGER | PRIMARY KEY | SimRacerHub driver identifier |
| `league_id` | INTEGER | NOT NULL, FK → leagues | Parent league |
| `team_id` | INTEGER | FK → teams | Team affiliation (nullable) |
| `name` | TEXT | NOT NULL | Full driver name |
| `first_name` | TEXT | | First name (parsed) |
| `last_name` | TEXT | | Last name (parsed) |
| `car_numbers` | TEXT | | All car numbers used (comma-separated) |
| `primary_number` | TEXT | | Primary/most common car number |
| `club` | TEXT | | iRacing club name |
| `club_id` | INTEGER | | iRacing club identifier |
| `irating` | INTEGER | | iRacing rating |
| `safety_rating` | REAL | | iRacing safety rating (e.g., 4.56) |
| `license_class` | TEXT | | iRacing license class (e.g., "A", "B") |
| `url` | TEXT | NOT NULL, UNIQUE | Driver profile URL |
| `scraped_at` | TIMESTAMP | NOT NULL | Last scrape timestamp |
| `created_at` | TIMESTAMP | DEFAULT CURRENT_TIMESTAMP | Record creation time |
| `updated_at` | TIMESTAMP | DEFAULT CURRENT_TIMESTAMP | Last update time |

**Foreign Keys**:
- `league_id` REFERENCES `leagues(league_id)`
- `team_id` REFERENCES `teams(team_id)`

**Indexes**:
- `idx_drivers_league_id` on `league_id`
- `idx_drivers_team_id` on `team_id`
- `idx_drivers_url` on `url`
- `idx_drivers_name` on `name`
- `idx_drivers_scraped_at` on `scraped_at`

**Typical Row Count**: 60

**Sample URL**: `https://www.simracerhub.com/driver_profile.php?driver_id=12345`

---

### 4. series

**Description**: Racing series within a league

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `series_id` | INTEGER | PRIMARY KEY | SimRacerHub series identifier |
| `league_id` | INTEGER | NOT NULL, FK → leagues | Parent league |
| `name` | TEXT | NOT NULL | Series name |
| `description` | TEXT | | Series description/details |
| `created_date` | DATE | | Series creation date |
| `num_seasons` | INTEGER | | Total number of seasons |
| `url` | TEXT | NOT NULL, UNIQUE | Series URL |
| `scraped_at` | TIMESTAMP | NOT NULL | Last scrape timestamp |
| `created_at` | TIMESTAMP | DEFAULT CURRENT_TIMESTAMP | Record creation time |
| `updated_at` | TIMESTAMP | DEFAULT CURRENT_TIMESTAMP | Last update time |

**Foreign Keys**:
- `league_id` REFERENCES `leagues(league_id)`

**Indexes**:
- `idx_series_league_id` on `league_id`
- `idx_series_url` on `url`
- `idx_series_scraped_at` on `scraped_at`

**Typical Row Count**: 4

**Sample URL**: `https://www.simracerhub.com/series_seasons.php?series_id=3714`

---

### 5. seasons

**Description**: Racing seasons within a series

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `season_id` | INTEGER | PRIMARY KEY | SimRacerHub season identifier |
| `series_id` | INTEGER | NOT NULL, FK → series | Parent series |
| `name` | TEXT | NOT NULL | Season name |
| `description` | TEXT | | Season description/details |
| `url` | TEXT | NOT NULL, UNIQUE | Season URL |
| `scraped_at` | TIMESTAMP | NOT NULL | Last scrape timestamp |
| `created_at` | TIMESTAMP | DEFAULT CURRENT_TIMESTAMP | Record creation time |
| `updated_at` | TIMESTAMP | DEFAULT CURRENT_TIMESTAMP | Last update time |

**Foreign Keys**:
- `series_id` REFERENCES `series(series_id)`

**Indexes**:
- `idx_seasons_series_id` on `series_id`
- `idx_seasons_url` on `url`
- `idx_seasons_scraped_at` on `scraped_at`

**Typical Row Count**: 100+ (across all series)

**Sample URL**: `https://www.simracerhub.com/season_race.php?series_id=3714&season_id=26741`

---

### 6. races

**Description**: Individual race events with metadata

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `race_id` | INTEGER | PRIMARY KEY AUTOINCREMENT | Internal auto-increment ID |
| `schedule_id` | INTEGER | NOT NULL, UNIQUE | SimRacerHub schedule identifier |
| `season_id` | INTEGER | NOT NULL, FK → seasons | Parent season |
| `race_number` | INTEGER | NOT NULL | Race number in season (1-based) |
| `event_name` | TEXT | | Event/race name |
| `date` | TIMESTAMP | | Race date and time |
| `race_time` | TEXT | | Scheduled race time |
| `practice_time` | TEXT | | Scheduled practice time |
| `track_id` | INTEGER | | Track identifier |
| `track_config_id` | INTEGER | | Track configuration ID |
| `track_name` | TEXT | | Track name (e.g., "Daytona") |
| `track_type` | TEXT | | Track type (e.g., "Oval", "Road") |
| `track_length` | REAL | | Track length in miles |
| `track_config_iracing_id` | TEXT | | iRacing track config identifier |
| `planned_laps` | INTEGER | | Planned number of laps |
| `points_race` | BOOLEAN | | Whether race counts for points |
| `off_week` | BOOLEAN | | Whether this is an off-week |
| `night_race` | BOOLEAN | | Whether this is a night race |
| `playoff_race` | BOOLEAN | | Whether this is a playoff race |
| `race_duration_minutes` | INTEGER | | Actual race duration |
| `total_laps` | INTEGER | | Total laps completed |
| `leaders` | INTEGER | | Number of different leaders |
| `lead_changes` | INTEGER | | Number of lead changes |
| `cautions` | INTEGER | | Number of cautions |
| `caution_laps` | INTEGER | | Total laps under caution |
| `num_drivers` | INTEGER | | Number of participating drivers |
| `weather_type` | TEXT | | Weather condition type |
| `cloud_conditions` | TEXT | | Cloud cover description |
| `temperature_f` | INTEGER | | Temperature in Fahrenheit |
| `humidity_pct` | INTEGER | | Humidity percentage |
| `fog_pct` | INTEGER | | Fog percentage |
| `weather_wind_speed` | TEXT | | Wind speed |
| `weather_wind_dir` | TEXT | | Wind direction |
| `weather_wind_unit` | TEXT | | Wind speed unit |
| `url` | TEXT | NOT NULL, UNIQUE | Race results URL |
| `is_complete` | BOOLEAN | DEFAULT 0 | Whether race results are complete |
| `scraped_at` | TIMESTAMP | NOT NULL | Last scrape timestamp |
| `created_at` | TIMESTAMP | DEFAULT CURRENT_TIMESTAMP | Record creation time |
| `updated_at` | TIMESTAMP | DEFAULT CURRENT_TIMESTAMP | Last update time |

**Foreign Keys**:
- `season_id` REFERENCES `seasons(season_id)`

**Indexes**:
- `idx_races_schedule_id` on `schedule_id`
- `idx_races_season_id` on `season_id`
- `idx_races_url` on `url`
- `idx_races_date` on `date`
- `idx_races_is_complete` on `is_complete`
- `idx_races_scraped_at` on `scraped_at`

**Typical Row Count**: 1000+

**Sample URL**: `https://www.simracerhub.com/season_race.php?schedule_id=324462`

**Important Note**:
- `race_id` is auto-increment for internal use
- `schedule_id` is the SimRacerHub identifier (unique)

---

### 7. race_results

**Description**: Driver results for each race

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `result_id` | INTEGER | PRIMARY KEY AUTOINCREMENT | Internal auto-increment ID |
| `race_id` | INTEGER | NOT NULL, FK → races | Parent race |
| `driver_id` | INTEGER | NOT NULL, FK → drivers | Driver who participated |
| `team` | TEXT | | Team name for this race |
| `finish_position` | INTEGER | | Final finishing position |
| `starting_position` | INTEGER | | Starting grid position |
| `car_number` | TEXT | | Car number used in race |
| `qualifying_time` | TEXT | | Qualifying lap time (formatted) |
| `fastest_lap` | TEXT | | Fastest lap time (formatted) |
| `fastest_lap_number` | INTEGER | | Lap number of fastest lap |
| `average_lap` | TEXT | | Average lap time (formatted) |
| `interval` | TEXT | | Time interval from leader/next car |
| `laps_completed` | INTEGER | | Total laps completed |
| `laps_led` | INTEGER | | Laps led |
| `incident_points` | INTEGER | | iRacing incident points |
| `race_points` | INTEGER | | Points earned for finishing position |
| `bonus_points` | INTEGER | | Bonus points earned |
| `penalty_points` | INTEGER | | Penalty points deducted |
| `total_points` | INTEGER | | Total championship points |
| `fast_laps` | INTEGER | | Number of fast laps |
| `quality_passes` | INTEGER | | Quality passes metric |
| `closing_passes` | INTEGER | | Closing passes metric |
| `total_passes` | INTEGER | | Total passes made |
| `average_running_position` | REAL | | Average position during race |
| `irating` | INTEGER | | iRating at time of race |
| `status` | TEXT | | Race status (e.g., "Running", "DNF") |
| `car_id` | INTEGER | | Car/vehicle identifier |
| `created_at` | TIMESTAMP | DEFAULT CURRENT_TIMESTAMP | Record creation time |
| `updated_at` | TIMESTAMP | DEFAULT CURRENT_TIMESTAMP | Last update time |

**Foreign Keys**:
- `race_id` REFERENCES `races(race_id)`
- `driver_id` REFERENCES `drivers(driver_id)`

**Unique Constraint**:
- `UNIQUE(race_id, driver_id)` - Each driver appears once per race

**Indexes**:
- `idx_race_results_race_id` on `race_id`
- `idx_race_results_driver_id` on `driver_id`
- `idx_race_results_position` on `finish_position`

**Typical Row Count**: 40,000+ (40 drivers × 1000+ races)

---

### 8. scrape_log

**Description**: Audit log of all scraping operations

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `log_id` | INTEGER | PRIMARY KEY AUTOINCREMENT | Internal auto-increment ID |
| `entity_type` | TEXT | NOT NULL, CHECK constraint | Entity type being scraped |
| `entity_id` | INTEGER | | Entity identifier (if known) |
| `entity_url` | TEXT | NOT NULL | URL that was scraped |
| `status` | TEXT | NOT NULL, CHECK constraint | Scrape result status |
| `error_message` | TEXT | | Error message if failed |
| `duration_ms` | INTEGER | | Scrape duration in milliseconds |
| `timestamp` | TIMESTAMP | DEFAULT CURRENT_TIMESTAMP | When scrape occurred |

**Check Constraints**:
- `entity_type IN ('league', 'team', 'driver', 'series', 'season', 'race')`
- `status IN ('success', 'failed', 'skipped')`

**Indexes**:
- `idx_scrape_log_entity_type` on `entity_type`
- `idx_scrape_log_status` on `status`
- `idx_scrape_log_timestamp` on `timestamp`

**Typical Row Count**: 1000+ (grows with each scrape)

**Purpose**: Monitoring, debugging, analytics

---

### 9. schema_alerts

**Description**: Alerts for detected website schema changes

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `alert_id` | INTEGER | PRIMARY KEY AUTOINCREMENT | Internal auto-increment ID |
| `entity_type` | TEXT | NOT NULL | Entity type affected |
| `alert_type` | TEXT | NOT NULL | Type of schema change detected |
| `details` | TEXT | NOT NULL | Detailed alert message |
| `url` | TEXT | | URL where issue was detected |
| `resolved` | BOOLEAN | DEFAULT 0 | Whether alert has been resolved |
| `timestamp` | TIMESTAMP | DEFAULT CURRENT_TIMESTAMP | When alert was created |

**Indexes**:
- `idx_schema_alerts_resolved` on `resolved`
- `idx_schema_alerts_timestamp` on `timestamp`

**Typical Row Count**: 0-10 (grows when schema changes detected)

**Purpose**: Early warning system for website changes

---

## Relationships & Foreign Keys

### Visual Relationship Map

```
leagues (1)
  ├── [FK] teams (N)
  │     └── [FK] drivers (N)
  ├── [FK] drivers (N)
  └── [FK] series (N)
        └── [FK] seasons (N)
              └── [FK] races (N)
                    └── [FK] race_results (N)
                          └── [FK] drivers (1)
```

### Foreign Key Relationships

| Child Table | Foreign Key Column | Parent Table | Parent Column |
|-------------|-------------------|--------------|---------------|
| `teams` | `league_id` | `leagues` | `league_id` |
| `drivers` | `league_id` | `leagues` | `league_id` |
| `drivers` | `team_id` | `teams` | `team_id` |
| `series` | `league_id` | `leagues` | `league_id` |
| `seasons` | `series_id` | `series` | `series_id` |
| `races` | `season_id` | `seasons` | `season_id` |
| `race_results` | `race_id` | `races` | `race_id` |
| `race_results` | `driver_id` | `drivers` | `driver_id` |

**Note**: Foreign key enforcement is **ENABLED** via `PRAGMA foreign_keys = ON`

---

## Indexes

### Purpose of Indexes

All indexes are created to optimize:
1. **Foreign key lookups** (e.g., finding all races for a season)
2. **Cache checking** (URL-based lookups)
3. **Time-based queries** (scraped_at timestamps)
4. **Analysis queries** (driver names, race positions)

### Complete Index List

| Index Name | Table | Column(s) | Purpose |
|------------|-------|-----------|---------|
| `idx_leagues_url` | leagues | url | Cache lookups |
| `idx_leagues_scraped_at` | leagues | scraped_at | Staleness checks |
| `idx_teams_league_id` | teams | league_id | Find teams by league |
| `idx_teams_scraped_at` | teams | scraped_at | Staleness checks |
| `idx_drivers_league_id` | drivers | league_id | Find drivers by league |
| `idx_drivers_team_id` | drivers | team_id | Find drivers by team |
| `idx_drivers_url` | drivers | url | Cache lookups |
| `idx_drivers_name` | drivers | name | Driver search |
| `idx_drivers_scraped_at` | drivers | scraped_at | Staleness checks |
| `idx_series_league_id` | series | league_id | Find series by league |
| `idx_series_url` | series | url | Cache lookups |
| `idx_series_scraped_at` | series | scraped_at | Staleness checks |
| `idx_seasons_series_id` | seasons | series_id | Find seasons by series |
| `idx_seasons_url` | seasons | url | Cache lookups |
| `idx_seasons_scraped_at` | seasons | scraped_at | Staleness checks |
| `idx_races_schedule_id` | races | schedule_id | Lookup by SimRacerHub ID |
| `idx_races_season_id` | races | season_id | Find races by season |
| `idx_races_url` | races | url | Cache lookups |
| `idx_races_date` | races | date | Time-based queries |
| `idx_races_is_complete` | races | is_complete | Find incomplete races |
| `idx_races_scraped_at` | races | scraped_at | Staleness checks |
| `idx_race_results_race_id` | race_results | race_id | Find results by race |
| `idx_race_results_driver_id` | race_results | driver_id | Find results by driver |
| `idx_race_results_position` | race_results | finish_position | Position-based queries |
| `idx_scrape_log_entity_type` | scrape_log | entity_type | Filter by entity type |
| `idx_scrape_log_status` | scrape_log | status | Filter by status |
| `idx_scrape_log_timestamp` | scrape_log | timestamp | Time-based queries |
| `idx_schema_alerts_resolved` | schema_alerts | resolved | Find unresolved alerts |
| `idx_schema_alerts_timestamp` | schema_alerts | timestamp | Time-based queries |

**Total Indexes**: 28

---

## Data Types & Constraints

### SQLite Type Mappings

| SQLite Type | Python Type | Typical Values | Notes |
|-------------|-------------|----------------|-------|
| INTEGER | int | 1558, 3714 | Used for all IDs |
| TEXT | str | "John Doe", "Daytona" | Variable length strings |
| REAL | float | 4.56, 2.5 | Decimals (safety rating, track length) |
| BOOLEAN | bool | 0, 1 | Stored as INTEGER (0=False, 1=True) |
| TIMESTAMP | str | "2025-11-10 15:30:00" | ISO 8601 format |
| DATE | str | "2025-11-10" | ISO 8601 date format |

### Timestamp Fields

All timestamp fields use **ISO 8601 format**: `YYYY-MM-DD HH:MM:SS`

**Timestamp columns across all tables**:
- `scraped_at` - Last successful scrape time (NOT NULL)
- `created_at` - Record creation time (DEFAULT CURRENT_TIMESTAMP)
- `updated_at` - Last update time (DEFAULT CURRENT_TIMESTAMP)
- `timestamp` - Log/alert creation time (DEFAULT CURRENT_TIMESTAMP)
- `date` - Race date and time (nullable)

### NULL Handling

**Fields that are typically NULL**:
- Optional descriptive fields: `description`, `event_name`
- Driver stats: `irating`, `safety_rating`, `license_class` (if not available)
- Race weather data: All `weather_*` fields (if not recorded)
- Team affiliations: `team_id` in drivers table (some drivers unaffiliated)

**Fields that are NEVER NULL**:
- All primary keys
- All foreign keys (except `team_id` in drivers)
- `name` fields
- `url` fields (except teams)
- `scraped_at` timestamps

### Special Value Formats

| Field | Format | Example |
|-------|--------|---------|
| `qualifying_time` | "MM:SS.mmm" | "01:23.456" |
| `fastest_lap` | "MM:SS.mmm" | "01:23.456" |
| `average_lap` | "MM:SS.mmm" | "01:24.789" |
| `interval` | "+S.mmm" or "-N laps" | "+2.345" or "-3 laps" |
| `car_numbers` | Comma-separated | "5,17,23" |
| `weather_wind_speed` | "N mph" or "N m/s" | "5 mph" |
| `weather_wind_dir` | Compass direction | "NE", "SSW" |

---

## Parquet Export Considerations

### Recommended Partitioning Strategies

#### Option 1: By Series and Season (Recommended)
```
/series_id=3714/
  /season_id=26741/
    races.parquet
    race_results.parquet
```

**Pros**: Natural hierarchy, efficient for time-based queries
**Cons**: Many small files if not careful

#### Option 2: By Entity Type
```
/leagues/
  leagues.parquet
/series/
  series.parquet
/seasons/
  seasons.parquet
/races/
  races.parquet (partitioned by season_id)
/race_results/
  race_results.parquet (partitioned by race_id)
```

**Pros**: Clean separation, easier to manage
**Cons**: Joins required for analysis

#### Option 3: Denormalized Flat Structure
```
/race_results_full/
  race_results_with_all_metadata.parquet
```

**Pros**: No joins needed, optimal for analytics
**Cons**: Data duplication, larger file sizes

### Type Conversions for Parquet

| SQLite Type | Parquet Type | Notes |
|-------------|--------------|-------|
| INTEGER | INT64 | All IDs, counts |
| TEXT | STRING (UTF-8) | Names, descriptions |
| REAL | FLOAT64 | Safety ratings, positions |
| BOOLEAN | BOOLEAN | Use native boolean |
| TIMESTAMP | TIMESTAMP (ms) | Convert ISO 8601 to timestamp |
| DATE | DATE32 | Convert ISO 8601 to date |

### Schema Recommendations

#### For Fact Tables (race_results)
```python
race_results_schema = pa.schema([
    pa.field('result_id', pa.int64(), nullable=False),
    pa.field('race_id', pa.int64(), nullable=False),
    pa.field('driver_id', pa.int64(), nullable=False),
    pa.field('team', pa.string(), nullable=True),
    pa.field('finish_position', pa.int32(), nullable=True),
    pa.field('starting_position', pa.int32(), nullable=True),
    pa.field('car_number', pa.string(), nullable=True),
    pa.field('laps_completed', pa.int32(), nullable=True),
    pa.field('laps_led', pa.int32(), nullable=True),
    pa.field('incident_points', pa.int32(), nullable=True),
    pa.field('race_points', pa.int32(), nullable=True),
    pa.field('total_points', pa.int32(), nullable=True),
    pa.field('average_running_position', pa.float64(), nullable=True),
    pa.field('irating', pa.int32(), nullable=True),
    pa.field('status', pa.string(), nullable=True),
    pa.field('created_at', pa.timestamp('ms'), nullable=False),
    pa.field('updated_at', pa.timestamp('ms'), nullable=False),
])
```

#### For Dimension Tables (drivers, teams, etc.)
```python
drivers_schema = pa.schema([
    pa.field('driver_id', pa.int64(), nullable=False),
    pa.field('league_id', pa.int64(), nullable=False),
    pa.field('team_id', pa.int64(), nullable=True),
    pa.field('name', pa.string(), nullable=False),
    pa.field('first_name', pa.string(), nullable=True),
    pa.field('last_name', pa.string(), nullable=True),
    pa.field('irating', pa.int32(), nullable=True),
    pa.field('safety_rating', pa.float64(), nullable=True),
    pa.field('license_class', pa.string(), nullable=True),
    pa.field('scraped_at', pa.timestamp('ms'), nullable=False),
])
```

### Time-String Fields (Special Handling)

**Challenge**: Lap times stored as strings ("01:23.456")

**Options**:
1. **Keep as STRING**: Preserve original format
2. **Convert to FLOAT64**: Total seconds (83.456)
3. **Convert to DURATION**: PyArrow duration type

**Recommendation**: Convert to FLOAT64 (total seconds) for analytics, keep original in separate column if needed

### Compression Recommendations

| Table | Compression | Reason |
|-------|-------------|--------|
| `race_results` | SNAPPY | Balance of speed/size for large fact table |
| `races` | SNAPPY | Moderate size, frequent queries |
| `drivers`, `teams` | GZIP | Small tables, high compression ratio |
| `seasons`, `series` | GZIP | Small tables, infrequent writes |
| `leagues` | GZIP | Single row, rarely changes |

### Missing/NULL Value Strategy

**Parquet NULL handling**:
- Use PyArrow's native NULL support
- Set `nullable=True` for optional fields
- Consider default values for numeric fields in analytics

**Common NULL patterns**:
- Weather data: ~30% NULL
- Driver stats: ~20% NULL
- Team affiliations: ~15% NULL

### Query Patterns to Optimize For

1. **Driver Performance Over Time**
   - Partition: `driver_id`, `race_date`
   - Indexes: Sort by `race_id`

2. **Season Standings**
   - Partition: `season_id`
   - Indexes: `driver_id`, `finish_position`

3. **Track-Specific Analysis**
   - Partition: `track_name`
   - Indexes: `driver_id`, `fastest_lap`

4. **Team Performance**
   - Partition: `team_id`, `season_id`
   - Indexes: `driver_id`

### Denormalization Opportunities

For analytics workloads, consider creating denormalized views:

```sql
-- Example: Fully denormalized race results
CREATE VIEW race_results_full AS
SELECT
    rr.*,
    r.schedule_id, r.race_number, r.track_name, r.track_type, r.date as race_date,
    s.season_id, s.name as season_name,
    ser.series_id, ser.name as series_name,
    d.name as driver_name, d.irating as driver_irating, d.team_id,
    t.name as team_name,
    l.league_id, l.name as league_name
FROM race_results rr
JOIN races r ON rr.race_id = r.race_id
JOIN seasons s ON r.season_id = s.season_id
JOIN series ser ON s.series_id = ser.series_id
JOIN drivers d ON rr.driver_id = d.driver_id
LEFT JOIN teams t ON d.team_id = t.team_id
JOIN leagues l ON d.league_id = l.league_id
```

This denormalized view can be exported as a single Parquet dataset.

### Estimated Dataset Sizes

| Table | Rows | SQLite Size | Parquet Size (SNAPPY) |
|-------|------|-------------|----------------------|
| leagues | 1 | < 1 KB | < 1 KB |
| teams | 15 | < 10 KB | < 5 KB |
| drivers | 60 | ~50 KB | ~20 KB |
| series | 4 | < 5 KB | < 2 KB |
| seasons | 100 | ~100 KB | ~40 KB |
| races | 1,000 | ~2 MB | ~800 KB |
| race_results | 40,000 | ~50 MB | ~15 MB |
| scrape_log | 1,000 | ~500 KB | ~200 KB |
| schema_alerts | 10 | < 5 KB | < 2 KB |
| **TOTAL** | **42,190** | **~53 MB** | **~16 MB** |

**Compression Ratio**: ~70% size reduction with SNAPPY

---

## Notes for Implementation

### Database Connection
```python
import sqlite3

conn = sqlite3.connect('/home/gadams/github/obrl/simracer_scraper/obrl-nov8.db')
conn.row_factory = sqlite3.Row
cursor = conn.cursor()
```

### Batch Export Strategy
1. Export dimension tables first (leagues, series, seasons, drivers, teams)
2. Export fact tables last (races, race_results)
3. Validate row counts match
4. Create manifest file with metadata

### Spark Schema Generation
- Use `pandas.read_sql()` → `to_parquet()` for quick conversion
- Or use PySpark with explicit schema definitions
- Consider using Delta Lake for ACID transactions

### Metadata to Include
- Export timestamp
- Source database file
- Schema version
- Row counts per table
- Parquet version used
- Compression codec used

---

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2025-11-10 | Initial documentation for Parquet export |

---

## References

- Source Code: `/home/gadams/github/obrl/simracer_scraper/src/database.py`
- Architecture Doc: `/home/gadams/github/obrl/simracer_scraper/ARCHITECTURE.md`
- Database File: `/home/gadams/github/obrl/simracer_scraper/obrl-nov8.db`
- Configuration: `/home/gadams/github/obrl/simracer_scraper/config.yaml`
