# SimRacerHub ReactDOM Structure Documentation

**Source URL**: https://www.simracerhub.com/season_race.php?schedule_id=314716
**Date Documented**: 2025-01-08

This document provides a complete reference for the ReactDOM JSON structure used in SimRacerHub race result pages.

## Overview

Race results are embedded in the page via ReactDOM:

```javascript
ReactDOM.createRoot(document.getElementById('driver_table_XXXXX')).render(
    React.createElement(ResultsTable, {
        rps: [...],      // Race participants
        drivers: {...},  // Driver metadata
        teams: {...},    // Team metadata
        schedule: {...}, // Race/event metadata
        // ... other props
    })
);
```

## 1. Race Participants Array (`rps`)

Each element in the `rps` array represents one driver's race result with the following fields:

### Identification
- `race_participant_id` - Unique participant record ID
- `race_id` - Race identifier
- `driver_id` - Driver identifier
- `car_id` - Car/vehicle identifier
- `driver_number` - Car number displayed

### Qualifying
- `qualify_pos` - Overall qualifying position
- `qualify_pos_class` - Class-specific qualifying position
- `qualify_time` - Qualifying lap time (string format: "MM:SS.mmm")

### Race Results
- `finish_pos` - Overall finishing position
- `finish_pos_class` - Class-specific finishing position
- `status` - Race status (e.g., "Running", "DNF", "DQ")
- `num_laps` - Laps completed
- `laps_led` - Number of laps led by this driver

### Timing
- `fastest_lap_time` - Driver's fastest lap (string format: "MM:SS.mmm")
- `fastest_lap_number` - Lap number of fastest lap
- `avg_lap` - Average lap time (string format: "MM:SS.mmm")
- `avg_fast_lap` - Average of fast laps
- `num_fast_lap` - Count of fast laps

### Intervals
- `intv` - Interval to leader/next car (numeric)
- `intv_units` - Units for interval (e.g., "seconds", "laps")
- `intv_str` - Formatted interval string (e.g., "+1.523", "Leader")
- `intv_cls` - Class interval (numeric)
- `intv_cls_str` - Formatted class interval string

### Passing & Position
- `passes` - Total passes made
- `quality_passes` - Quality passes
- `closing_passes` - Passes made while closing
- `avg_pos` - Average position
- `arp` - Average running position
- `fastest_restart` - Fastest restart time/metric
- `pos` - Position (may be duplicate of finish_pos)

### Points System
- `spts` - Season points (or stage points)
- `rpts` - Race points (base race finishing points)
- `bpts` - Bonus points (aggregate of all bonuses)
- `ppts` - Penalty points (negative points for penalties)
- `tpts` - Total points (sum of all points)

### Driver Stats
- `incidents` - Number of incidents/contacts
- `irating` - iRacing rating at time of race
- `sr` - Safety rating
- `license` - License class (e.g., "A", "B", "C")
- `rating` - Overall rating metric

### Other
- `provisional` - Provisional starting position flag
- `name` - Driver name (may be included in participant record)

## 2. Drivers Object (`drivers`)

Keyed by `driver_id` (as string), each entry contains:

```json
{
  "driver_id": "12345",
  "club_id": "5",
  "name": "Driver Full Name",
  "last_first": "LastName, FirstName",
  "is_ai": false
}
```

### Fields
- `driver_id` - Driver identifier
- `club_id` - Club/region identifier
- `name` - Full name (First Last format)
- `last_first` - Name in Last, First format
- `is_ai` - Boolean indicating if AI driver

## 3. Teams Object (`teams`)

Keyed by `team_id` (as string), each entry contains:

```json
{
  "name": "Team Name",
  "pos1": -1,
  "pos2": -1,
  "num_drivers": 2,
  "drivers": ["12345", "67890"],
  "laps": 150,
  "laps_led": 20,
  "tpts": 95,
  "rpts": 85,
  "bpts": 5,
  "ppts": -5,
  "incidents": 3,
  "cars": ["51"],
  "finish_pos": 2,
  "qualify_pos": 1
}
```

### Fields
- `name` - Team name
- `pos1`, `pos2` - Position fields (purpose unclear, often -1)
- `num_drivers` - Number of drivers on team
- `drivers` - Array of driver_id strings
- `laps` - Total laps completed by team
- `laps_led` - Total laps led by team
- `tpts` - Team total points
- `rpts` - Team race points
- `bpts` - Team bonus points
- `ppts` - Team penalty points
- `incidents` - Team total incidents
- `cars` - Array of car IDs used by team
- `finish_pos` - Team finishing position
- `qualify_pos` - Team qualifying position

## 4. Schedule Object (`schedule`)

Contains comprehensive race/event metadata:

### Basic Info
- `schedule_id` - Schedule entry identifier
- `season_id` - Season identifier
- `race_date` - Date of race
- `race_time` - Time of race

### Track Info
- `track_id` - Track identifier
- `track_config_id` - Track configuration identifier
- `track_name` - Track name
- `track_type` - Track type (e.g., "Oval", "Road Course")
- `track_length` - Track length

### Race Details
- `planned_laps` - Scheduled number of laps
- Various weather fields
- League and series metadata

## 5. Team-Driver Mapping (`team_drivers`)

**CRITICAL**: Participant records do NOT contain `team_id`. Instead, there's a separate `team_drivers` object that maps driver IDs to team IDs:

```json
{
  "25099": "29434",  // driver_id: team_id
  "85315": "29434",
  "64635": "29427",
  ...
}
```

To get a driver's team name:
1. Get `driver_id` from participant
2. Look up `team_id` in `team_drivers[driver_id]`
3. Look up team name in `teams[team_id].name`

## 6. Additional Props

The ResultsTable component may receive additional props:

- `race_id` - Current race ID
- `options` - Display options and column preferences
- `team_drivers` - **Team-driver mapping object** (see above)
- `clubs` - Club/region data
- `classes` - Car class definitions
- `car_class` - Car class information

## Points System Breakdown

Based on the structure, the points calculation appears to be:

```
tpts (Total Points) = rpts (Race Points) + bpts (Bonus Points) + ppts (Penalty Points)
```

### Race Points (`rpts`)
Base points awarded for finishing position

### Bonus Points (`bpts`)
Aggregate of various bonus point categories. The individual bonus categories are NOT broken down in the participant record. Possible sources include:
- Leading a lap
- Leading most laps
- Fastest lap
- Finishing the race
- Pole position
- Stage wins (if applicable)

**Note**: The `bpts` field is a sum total. Individual bonus point categories (led_lap_points, finished_race_points, etc.) are NOT provided in the ReactDOM JSON. These would need to be calculated separately or obtained from different data sources.

### Penalty Points (`ppts`)
Points deducted for penalties or infractions (typically negative values)

### Season/Stage Points (`spts`)
Additional points for season or stage-based scoring

## Important Notes

1. **Team Name in Participant Records**: The `team` field does NOT appear directly in participant records (`rps` array). Team information must be looked up using `team_id` from the `teams` object.

2. **Bonus Points Are Aggregated**: Individual bonus point categories (like led_lap_points, fastest_lap_points, etc.) are NOT broken down in the JSON. Only the total `bpts` value is provided.

3. **String Keys**: Object keys in `drivers` and `teams` are strings, not integers.

4. **Multiple Interval Formats**: Both numeric intervals (`intv`, `intv_cls`) and formatted strings (`intv_str`, `intv_cls_str`) are provided.

5. **Class-Specific Fields**: Many fields have both overall and class-specific versions (e.g., `finish_pos` vs `finish_pos_class`).

## Field Mapping for Database

When extracting to database, use these mappings:

| Database Field | ReactDOM Field | Notes |
|----------------|----------------|-------|
| `driver_id` | `driver_id` | Convert to int |
| `team` | Lookup via `team_id` in `teams` object | Use team name |
| `finish_position` | `finish_pos` | |
| `starting_position` | `qualify_pos` | |
| `car_number` | `driver_number` | |
| `qualifying_time` | `qualify_time` | |
| `fastest_lap` | `fastest_lap_time` | |
| `fastest_lap_number` | `fastest_lap_number` | |
| `average_lap` | `avg_lap` | |
| `interval` | `intv_str` | Use formatted string |
| `laps_completed` | `num_laps` | |
| `laps_led` | `laps_led` | |
| `incident_points` | `incidents` | Keep as positive |
| `race_points` | `rpts` | |
| `bonus_points` | `bpts` | Total bonus only |
| `penalty_points` | `ppts` | New field needed |
| `total_points` | `tpts` | |
| `fast_laps` | `num_fast_lap` | |
| `quality_passes` | `quality_passes` | |
| `closing_passes` | `closing_passes` | |
| `total_passes` | `passes` | |
| `average_running_position` | `arp` | |
| `irating` | `irating` | |
| `status` | `status` | |
| `car_id` | `car_id` | |

## Missing Individual Bonus Fields

The following fields we added to the database do NOT exist in the ReactDOM:
- `led_lap_points` - Not in JSON (only aggregate `bpts`)
- `finished_race_points` - Not in JSON (only aggregate `bpts`)

**Recommendation**: Remove these individual bonus point fields from the database schema, or populate them via separate calculation logic based on league rules. The ReactDOM only provides `bpts` (aggregate bonus points).
