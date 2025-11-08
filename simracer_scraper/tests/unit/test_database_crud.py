"""Tests for database CRUD operations (Task 2.4)."""

import sqlite3

import pytest

# Task 2.4: Season, Race, Driver, Team, Race Result CRUD Operations Tests


def test_upsert_season(test_db):
    """Test that upsert_season inserts and updates seasons."""
    # Setup prerequisites
    test_db.upsert_league(
        1558, {"name": "The OBRL", "url": "http://test.com/league", "scraped_at": "2025-01-15"}
    )
    test_db.upsert_series(
        3714,
        1558,
        {
            "name": "Wednesday Night Series",
            "url": "http://test.com/series",
            "scraped_at": "2025-01-15",
        },
    )

    # Insert season
    season_id = test_db.upsert_season(
        12345,
        3714,
        {
            "name": "2025 Season 1",
            "url": "http://test.com/season/12345",
            "year": 2025,
            "status": "active",
            "scheduled_races": 12,
            "completed_races": 3,
            "scraped_at": "2025-01-15",
        },
    )

    assert season_id == 12345

    season = test_db.get_season(12345)
    assert season is not None
    assert season["season_id"] == 12345
    assert season["series_id"] == 3714
    assert season["name"] == "2025 Season 1"
    assert season["year"] == 2025
    assert season["status"] == "active"


def test_get_seasons_by_series(test_db):
    """Test getting all seasons for a series."""
    # Setup
    test_db.upsert_league(
        1558, {"name": "The OBRL", "url": "http://test.com/league", "scraped_at": "2025-01-15"}
    )
    test_db.upsert_series(
        3714,
        1558,
        {"name": "Series", "url": "http://test.com/series", "scraped_at": "2025-01-15"},
    )

    # Insert multiple seasons
    test_db.upsert_season(
        12345,
        3714,
        {"name": "Season 1", "url": "http://test.com/season/1", "scraped_at": "2025-01-15"},
    )
    test_db.upsert_season(
        12346,
        3714,
        {"name": "Season 2", "url": "http://test.com/season/2", "scraped_at": "2025-01-15"},
    )

    seasons = test_db.get_seasons_by_series(3714)
    assert len(seasons) == 2
    assert seasons[0]["season_id"] == 12345
    assert seasons[1]["season_id"] == 12346


def test_upsert_race(test_db):
    """Test that upsert_race inserts and updates races."""
    # Setup
    test_db.upsert_league(
        1558, {"name": "The OBRL", "url": "http://test.com/league", "scraped_at": "2025-01-15"}
    )
    test_db.upsert_series(
        3714,
        1558,
        {"name": "Series", "url": "http://test.com/series", "scraped_at": "2025-01-15"},
    )
    test_db.upsert_season(
        12345,
        3714,
        {"name": "Season", "url": "http://test.com/season", "scraped_at": "2025-01-15"},
    )

    # Insert race
    race_id = test_db.upsert_race(
        67890,
        12345,
        {
            "race_number": 1,
            "url": "http://test.com/race/67890",
            "track_name": "Daytona International Speedway",
            "laps": 200,
            "scraped_at": "2025-01-15",
        },
    )

    assert race_id is not None

    race = test_db.get_race(67890)
    assert race is not None
    assert race["schedule_id"] == 67890
    assert race["season_id"] == 12345
    assert race["race_number"] == 1
    assert race["track_name"] == "Daytona International Speedway"


def test_get_races_by_season(test_db):
    """Test getting all races for a season."""
    # Setup
    test_db.upsert_league(
        1558, {"name": "The OBRL", "url": "http://test.com/league", "scraped_at": "2025-01-15"}
    )
    test_db.upsert_series(
        3714,
        1558,
        {"name": "Series", "url": "http://test.com/series", "scraped_at": "2025-01-15"},
    )
    test_db.upsert_season(
        12345,
        3714,
        {"name": "Season", "url": "http://test.com/season", "scraped_at": "2025-01-15"},
    )

    # Insert multiple races
    test_db.upsert_race(
        67890,
        12345,
        {
            "race_number": 1,
            "url": "http://test.com/race/1",
            "scraped_at": "2025-01-15",
        },
    )
    test_db.upsert_race(
        67891,
        12345,
        {
            "race_number": 2,
            "url": "http://test.com/race/2",
            "scraped_at": "2025-01-15",
        },
    )

    races = test_db.get_races_by_season(12345)
    assert len(races) == 2
    assert races[0]["race_number"] == 1
    assert races[1]["race_number"] == 2


def test_upsert_team(test_db):
    """Test that upsert_team inserts and updates teams."""
    # Setup
    test_db.upsert_league(
        1558, {"name": "The OBRL", "url": "http://test.com/league", "scraped_at": "2025-01-15"}
    )

    # Insert team
    team_id = test_db.upsert_team(
        5001,
        1558,
        {
            "name": "Team Rocket",
            "driver_count": 4,
            "url": "http://test.com/team/5001",
            "scraped_at": "2025-01-15",
        },
    )

    assert team_id == 5001

    team = test_db.get_team(5001)
    assert team is not None
    assert team["team_id"] == 5001
    assert team["league_id"] == 1558
    assert team["name"] == "Team Rocket"
    assert team["driver_count"] == 4


def test_get_teams_by_league(test_db):
    """Test getting all teams for a league."""
    # Setup
    test_db.upsert_league(
        1558, {"name": "The OBRL", "url": "http://test.com/league", "scraped_at": "2025-01-15"}
    )

    # Insert multiple teams
    test_db.upsert_team(5001, 1558, {"name": "Team 1", "scraped_at": "2025-01-15"})
    test_db.upsert_team(5002, 1558, {"name": "Team 2", "scraped_at": "2025-01-15"})

    teams = test_db.get_teams_by_league(1558)
    assert len(teams) == 2
    assert teams[0]["team_id"] == 5001
    assert teams[1]["team_id"] == 5002


def test_upsert_driver(test_db):
    """Test that upsert_driver inserts and updates drivers."""
    # Setup
    test_db.upsert_league(
        1558, {"name": "The OBRL", "url": "http://test.com/league", "scraped_at": "2025-01-15"}
    )
    test_db.upsert_team(5001, 1558, {"name": "Team Rocket", "scraped_at": "2025-01-15"})

    # Insert driver
    driver_id = test_db.upsert_driver(
        9001,
        1558,
        {
            "name": "John Doe",
            "url": "http://test.com/driver/9001",
            "team_id": 5001,
            "first_name": "John",
            "last_name": "Doe",
            "primary_number": "42",
            "irating": 3500,
            "safety_rating": 3.75,
            "license_class": "A",
            "scraped_at": "2025-01-15",
        },
    )

    assert driver_id == 9001

    driver = test_db.get_driver(9001)
    assert driver is not None
    assert driver["driver_id"] == 9001
    assert driver["league_id"] == 1558
    assert driver["team_id"] == 5001
    assert driver["name"] == "John Doe"
    assert driver["irating"] == 3500


def test_get_drivers_by_league(test_db):
    """Test getting all drivers for a league."""
    # Setup
    test_db.upsert_league(
        1558, {"name": "The OBRL", "url": "http://test.com/league", "scraped_at": "2025-01-15"}
    )

    # Insert multiple drivers
    test_db.upsert_driver(
        9001,
        1558,
        {"name": "Driver 1", "url": "http://test.com/driver/1", "scraped_at": "2025-01-15"},
    )
    test_db.upsert_driver(
        9002,
        1558,
        {"name": "Driver 2", "url": "http://test.com/driver/2", "scraped_at": "2025-01-15"},
    )

    drivers = test_db.get_drivers_by_league(1558)
    assert len(drivers) == 2
    assert drivers[0]["driver_id"] == 9001
    assert drivers[1]["driver_id"] == 9002


def test_find_driver_by_name(test_db):
    """Test finding drivers by name with fuzzy matching."""
    # Setup
    test_db.upsert_league(
        1558, {"name": "The OBRL", "url": "http://test.com/league", "scraped_at": "2025-01-15"}
    )

    # Insert drivers
    test_db.upsert_driver(
        9001,
        1558,
        {"name": "John Smith", "url": "http://test.com/driver/1", "scraped_at": "2025-01-15"},
    )
    test_db.upsert_driver(
        9002,
        1558,
        {"name": "Jane Doe", "url": "http://test.com/driver/2", "scraped_at": "2025-01-15"},
    )
    test_db.upsert_driver(
        9003,
        1558,
        {"name": "John Doe", "url": "http://test.com/driver/3", "scraped_at": "2025-01-15"},
    )

    # Search for "John"
    results = test_db.find_driver_by_name("John")
    assert len(results) == 2
    assert all("John" in r["name"] for r in results)

    # Search with league filter
    results = test_db.find_driver_by_name("Doe", league_id=1558)
    assert len(results) == 2
    assert all("Doe" in r["name"] for r in results)

    # Search with no results
    results = test_db.find_driver_by_name("Nonexistent")
    assert len(results) == 0


def test_upsert_race_result(test_db):
    """Test that upsert_race_result inserts and updates race results."""
    # Setup full hierarchy
    test_db.upsert_league(
        1558, {"name": "The OBRL", "url": "http://test.com/league", "scraped_at": "2025-01-15"}
    )
    test_db.upsert_driver(
        9001,
        1558,
        {"name": "Driver 1", "url": "http://test.com/driver/1", "scraped_at": "2025-01-15"},
    )
    test_db.upsert_series(
        3714,
        1558,
        {"name": "Series", "url": "http://test.com/series", "scraped_at": "2025-01-15"},
    )
    test_db.upsert_season(
        12345,
        3714,
        {"name": "Season", "url": "http://test.com/season", "scraped_at": "2025-01-15"},
    )
    test_db.upsert_race(
        67890,
        12345,
        {"race_number": 1, "url": "http://test.com/race", "scraped_at": "2025-01-15"},
    )

    # Get the auto-generated race_id
    race = test_db.get_race(67890)
    race_id = race["race_id"]

    # Insert race result
    result_id = test_db.upsert_race_result(
        race_id,
        9001,
        {
            "finish_position": 1,
            "starting_position": 3,
            "car_number": "42",
            "laps_completed": 200,
            "incident_points": 0,
            "total_points": 50,
        },
    )

    assert result_id is not None

    # Get results
    results = test_db.get_race_results(race_id)
    assert len(results) == 1
    assert results[0]["finish_position"] == 1
    assert results[0]["driver_id"] == 9001


def test_get_driver_results(test_db):
    """Test getting all race results for a driver."""
    # Setup
    test_db.upsert_league(
        1558, {"name": "The OBRL", "url": "http://test.com/league", "scraped_at": "2025-01-15"}
    )
    test_db.upsert_driver(
        9001,
        1558,
        {"name": "Driver 1", "url": "http://test.com/driver/1", "scraped_at": "2025-01-15"},
    )
    test_db.upsert_series(
        3714,
        1558,
        {"name": "Series", "url": "http://test.com/series", "scraped_at": "2025-01-15"},
    )
    test_db.upsert_season(
        12345,
        3714,
        {"name": "Season", "url": "http://test.com/season", "scraped_at": "2025-01-15"},
    )

    # Insert two races
    test_db.upsert_race(
        67890,
        12345,
        {"race_number": 1, "url": "http://test.com/race/1", "scraped_at": "2025-01-15"},
    )
    test_db.upsert_race(
        67891,
        12345,
        {"race_number": 2, "url": "http://test.com/race/2", "scraped_at": "2025-01-15"},
    )

    race1 = test_db.get_race(67890)
    race2 = test_db.get_race(67891)

    # Insert results for same driver in different races
    test_db.upsert_race_result(race1["race_id"], 9001, {"finish_position": 1})
    test_db.upsert_race_result(race2["race_id"], 9001, {"finish_position": 2})

    # Get all results for driver
    results = test_db.get_driver_results(9001)
    assert len(results) == 2


def test_foreign_key_constraints(test_db):
    """Test that foreign key constraints are enforced for all entities."""
    # Season requires valid series_id
    with pytest.raises(sqlite3.IntegrityError):
        test_db.upsert_season(
            12345,
            99999,
            {"name": "Season", "url": "http://test.com", "scraped_at": "2025-01-15"},
        )

    # Race requires valid season_id
    with pytest.raises(sqlite3.IntegrityError):
        test_db.upsert_race(
            67890,
            99999,
            {"race_number": 1, "url": "http://test.com", "scraped_at": "2025-01-15"},
        )

    # Team requires valid league_id
    with pytest.raises(sqlite3.IntegrityError):
        test_db.upsert_team(5001, 99999, {"name": "Team", "scraped_at": "2025-01-15"})

    # Driver requires valid league_id
    with pytest.raises(sqlite3.IntegrityError):
        test_db.upsert_driver(
            9001,
            99999,
            {"name": "Driver", "url": "http://test.com", "scraped_at": "2025-01-15"},
        )


def test_required_fields_validation(test_db):
    """Test that required fields are validated."""
    # Setup
    test_db.upsert_league(
        1558, {"name": "The OBRL", "url": "http://test.com/league", "scraped_at": "2025-01-15"}
    )
    test_db.upsert_series(
        3714,
        1558,
        {"name": "Series", "url": "http://test.com/series", "scraped_at": "2025-01-15"},
    )

    # Season missing required fields
    with pytest.raises(ValueError):
        test_db.upsert_season(12345, 3714, {"name": "Season"})

    # Race missing required fields
    with pytest.raises(ValueError):
        test_db.upsert_race(67890, 12345, {"url": "http://test.com"})

    # Team missing required fields
    with pytest.raises(ValueError):
        test_db.upsert_team(5001, 1558, {"name": "Team"})

    # Driver missing required fields
    with pytest.raises(ValueError):
        test_db.upsert_driver(9001, 1558, {"name": "Driver"})


def test_methods_without_connection():
    """Test that all methods raise RuntimeError when database not connected."""
    try:
        from database import Database
    except ImportError:
        from src.database import Database

    db = Database(":memory:")
    # Don't connect

    # Season methods
    with pytest.raises(RuntimeError, match="Database not connected"):
        db.upsert_season(
            12345, 3714, {"name": "Season", "url": "http://test.com", "scraped_at": "2025-01-15"}
        )
    with pytest.raises(RuntimeError, match="Database not connected"):
        db.get_season(12345)
    with pytest.raises(RuntimeError, match="Database not connected"):
        db.get_seasons_by_series(3714)

    # Race methods
    with pytest.raises(RuntimeError, match="Database not connected"):
        db.upsert_race(
            67890,
            12345,
            {"race_number": 1, "url": "http://test.com", "scraped_at": "2025-01-15"},
        )
    with pytest.raises(RuntimeError, match="Database not connected"):
        db.get_race(67890)
    with pytest.raises(RuntimeError, match="Database not connected"):
        db.get_races_by_season(12345)

    # Team methods
    with pytest.raises(RuntimeError, match="Database not connected"):
        db.upsert_team(5001, 1558, {"name": "Team", "scraped_at": "2025-01-15"})
    with pytest.raises(RuntimeError, match="Database not connected"):
        db.get_team(5001)
    with pytest.raises(RuntimeError, match="Database not connected"):
        db.get_teams_by_league(1558)

    # Driver methods
    with pytest.raises(RuntimeError, match="Database not connected"):
        db.upsert_driver(
            9001, 1558, {"name": "Driver", "url": "http://test.com", "scraped_at": "2025-01-15"}
        )
    with pytest.raises(RuntimeError, match="Database not connected"):
        db.get_driver(9001)
    with pytest.raises(RuntimeError, match="Database not connected"):
        db.get_drivers_by_league(1558)
    with pytest.raises(RuntimeError, match="Database not connected"):
        db.find_driver_by_name("John")

    # Race result methods
    with pytest.raises(RuntimeError, match="Database not connected"):
        db.upsert_race_result(1, 9001, {})
    with pytest.raises(RuntimeError, match="Database not connected"):
        db.get_race_results(1)
    with pytest.raises(RuntimeError, match="Database not connected"):
        db.get_driver_results(9001)
