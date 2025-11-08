"""Tests for database module."""

import sqlite3
import time

import pytest


def test_database_initialization():
    """Test that Database can be initialized."""
    try:
        from database import Database
    except ImportError:
        from src.database import Database

    db = Database(":memory:")
    assert db.db_path == ":memory:"
    assert db.conn is None


def test_database_connect():
    """Test database connection."""
    try:
        from database import Database
    except ImportError:
        from src.database import Database

    db = Database(":memory:")
    db.connect()

    assert db.conn is not None
    assert isinstance(db.conn, sqlite3.Connection)

    db.close()


def test_database_close():
    """Test database connection closing."""
    try:
        from database import Database
    except ImportError:
        from src.database import Database

    db = Database(":memory:")
    db.connect()
    db.close()

    assert db.conn is None


def test_context_manager():
    """Test database as context manager."""
    try:
        from database import Database
    except ImportError:
        from src.database import Database

    with Database(":memory:") as db:
        assert db.conn is not None

    # Connection should be closed after context
    assert db.conn is None


def test_initialize_schema_creates_all_tables(test_db):
    """Test that all 9 tables are created."""
    cursor = test_db.conn.cursor()
    cursor.execute(
        """
        SELECT name FROM sqlite_master
        WHERE type='table' AND name NOT LIKE 'sqlite_%'
        ORDER BY name
    """
    )
    tables = [row[0] for row in cursor.fetchall()]

    expected_tables = [
        "drivers",
        "leagues",
        "race_results",
        "races",
        "schema_alerts",
        "scrape_log",
        "seasons",
        "series",
        "teams",
    ]

    assert tables == expected_tables, f"Expected {expected_tables}, got {tables}"


def test_leagues_table_columns(test_db):
    """Test that leagues table has all required columns."""
    cursor = test_db.conn.cursor()
    cursor.execute("PRAGMA table_info(leagues)")
    columns = {row[1] for row in cursor.fetchall()}

    required = {
        "league_id",
        "name",
        "description",
        "organizer",
        "url",
        "scraped_at",
        "created_at",
        "updated_at",
    }

    assert required.issubset(columns), f"Missing columns: {required - columns}"


def test_teams_table_columns(test_db):
    """Test that teams table has all required columns."""
    cursor = test_db.conn.cursor()
    cursor.execute("PRAGMA table_info(teams)")
    columns = {row[1] for row in cursor.fetchall()}

    required = {
        "team_id",
        "league_id",
        "name",
        "driver_count",
        "url",
        "scraped_at",
        "created_at",
        "updated_at",
    }

    assert required.issubset(columns), f"Missing columns: {required - columns}"


def test_drivers_table_columns(test_db):
    """Test that drivers table has all required columns."""
    cursor = test_db.conn.cursor()
    cursor.execute("PRAGMA table_info(drivers)")
    columns = {row[1] for row in cursor.fetchall()}

    required = {
        "driver_id",
        "league_id",
        "team_id",
        "name",
        "first_name",
        "last_name",
        "car_numbers",
        "primary_number",
        "club",
        "club_id",
        "irating",
        "safety_rating",
        "license_class",
        "url",
        "scraped_at",
        "created_at",
        "updated_at",
    }

    assert required.issubset(columns), f"Missing columns: {required - columns}"


def test_series_table_columns(test_db):
    """Test that series table has all required columns."""
    cursor = test_db.conn.cursor()
    cursor.execute("PRAGMA table_info(series)")
    columns = {row[1] for row in cursor.fetchall()}

    required = {
        "series_id",
        "league_id",
        "name",
        "description",
        "vehicle_type",
        "day_of_week",
        "active",
        "season_count",
        "created_date",
        "url",
        "scraped_at",
        "created_at",
        "updated_at",
    }

    assert required.issubset(columns), f"Missing columns: {required - columns}"


def test_seasons_table_columns(test_db):
    """Test that seasons table has all required columns."""
    cursor = test_db.conn.cursor()
    cursor.execute("PRAGMA table_info(seasons)")
    columns = {row[1] for row in cursor.fetchall()}

    required = {
        "season_id",
        "series_id",
        "name",
        "year",
        "start_date",
        "start_timestamp",
        "end_date",
        "scheduled_races",
        "completed_races",
        "status",
        "hc",
        "mc",
        "psn",
        "url",
        "scraped_at",
        "created_at",
        "updated_at",
    }

    assert required.issubset(columns), f"Missing columns: {required - columns}"


def test_races_table_columns(test_db):
    """Test that races table has all required columns."""
    cursor = test_db.conn.cursor()
    cursor.execute("PRAGMA table_info(races)")
    columns = {row[1] for row in cursor.fetchall()}

    required = {
        "race_id",
        "schedule_id",
        "season_id",
        "race_number",
        "track_name",
        "track_type",
        "date",
        "race_duration_minutes",
        "total_laps",
        "leaders",
        "lead_changes",
        "weather_type",
        "temperature_f",
        "humidity_pct",
        "fog_pct",
        "num_drivers",
        "url",
        "scraped_at",
        "created_at",
        "updated_at",
    }

    assert required.issubset(columns), f"Missing columns: {required - columns}"


def test_race_results_table_columns(test_db):
    """Test that race_results table has all required columns."""
    cursor = test_db.conn.cursor()
    cursor.execute("PRAGMA table_info(race_results)")
    columns = {row[1] for row in cursor.fetchall()}

    required = {
        "result_id",
        "race_id",
        "driver_id",
        "finish_position",
        "starting_position",
        "car_number",
        "qualifying_time",
        "fastest_lap",
        "fastest_lap_number",
        "average_lap",
        "interval",
        "laps_completed",
        "laps_led",
        "incidents",
        "race_points",
        "bonus_points",
        "total_points",
        "fast_laps",
        "quality_passes",
        "closing_passes",
        "total_passes",
        "average_running_position",
        "irating",
        "status",
        "car_type",
        "team",
        "created_at",
        "updated_at",
    }

    assert required.issubset(columns), f"Missing columns: {required - columns}"


def test_scrape_log_table_columns(test_db):
    """Test that scrape_log table has all required columns."""
    cursor = test_db.conn.cursor()
    cursor.execute("PRAGMA table_info(scrape_log)")
    columns = {row[1] for row in cursor.fetchall()}

    required = {
        "log_id",
        "entity_type",
        "entity_id",
        "entity_url",
        "status",
        "error_message",
        "duration_ms",
        "timestamp",
    }

    assert required.issubset(columns), f"Missing columns: {required - columns}"


def test_schema_alerts_table_columns(test_db):
    """Test that schema_alerts table has all required columns."""
    cursor = test_db.conn.cursor()
    cursor.execute("PRAGMA table_info(schema_alerts)")
    columns = {row[1] for row in cursor.fetchall()}

    required = {
        "alert_id",
        "entity_type",
        "alert_type",
        "details",
        "url",
        "resolved",
        "timestamp",
    }

    assert required.issubset(columns), f"Missing columns: {required - columns}"


def test_indexes_exist(test_db):
    """Test that all indexes are created."""
    cursor = test_db.conn.cursor()
    cursor.execute(
        """
        SELECT name FROM sqlite_master
        WHERE type='index' AND name NOT LIKE 'sqlite_%'
        ORDER BY name
    """
    )
    indexes = [row[0] for row in cursor.fetchall()]

    # Should have indexes for all major query patterns
    assert len(indexes) > 0, "No indexes created"

    # Check some critical indexes exist
    expected_indexes = [
        "idx_leagues_url",
        "idx_series_league_id",
        "idx_seasons_series_id",
        "idx_races_season_id",
        "idx_race_results_race_id",
        "idx_race_results_driver_id",
        "idx_drivers_url",
    ]

    for idx in expected_indexes:
        assert idx in indexes, f"Index {idx} not found"


def test_foreign_keys_enforced(test_db):
    """Test that foreign key constraints are enforced."""
    cursor = test_db.conn.cursor()

    # Try to insert a series with invalid league_id
    with pytest.raises(sqlite3.IntegrityError):
        cursor.execute(
            """
            INSERT INTO series (series_id, league_id, name, url, scraped_at)
            VALUES (1, 9999, 'Test', 'http://test.com', datetime('now'))
        """
        )


def test_unique_constraints_enforced(test_db):
    """Test that unique constraints are enforced."""
    cursor = test_db.conn.cursor()

    # Insert a league
    cursor.execute(
        """
        INSERT INTO leagues (league_id, name, url, scraped_at)
        VALUES (1, 'Test League', 'http://test.com', datetime('now'))
    """
    )
    test_db.conn.commit()

    # Try to insert another league with same URL
    with pytest.raises(sqlite3.IntegrityError):
        cursor.execute(
            """
            INSERT INTO leagues (league_id, name, url, scraped_at)
            VALUES (2, 'Another League', 'http://test.com', datetime('now'))
        """
        )


def test_initialize_schema_without_connection_raises_error():
    """Test that initializing schema without connection raises error."""
    try:
        from database import Database
    except ImportError:
        from src.database import Database

    db = Database(":memory:")
    # Don't connect

    with pytest.raises(RuntimeError, match="Database not connected"):
        db.initialize_schema()


def test_race_results_unique_constraint(test_db):
    """Test that race_results has unique constraint on race_id + driver_id."""
    cursor = test_db.conn.cursor()

    # Insert test data
    cursor.execute(
        """
        INSERT INTO leagues (league_id, name, url, scraped_at)
        VALUES (1, 'League', 'http://league.com', datetime('now'))
    """
    )
    cursor.execute(
        """
        INSERT INTO drivers (driver_id, league_id, name, url, scraped_at)
        VALUES (1, 1, 'Driver', 'http://driver.com', datetime('now'))
    """
    )
    cursor.execute(
        """
        INSERT INTO series (series_id, league_id, name, url, scraped_at)
        VALUES (1, 1, 'Series', 'http://series.com', datetime('now'))
    """
    )
    cursor.execute(
        """
        INSERT INTO seasons (season_id, series_id, name, url, scraped_at)
        VALUES (1, 1, 'Season', 'http://season.com', datetime('now'))
    """
    )
    cursor.execute(
        """
        INSERT INTO races (schedule_id, season_id, race_number, url, scraped_at)
        VALUES (1, 1, 1, 'http://race.com', datetime('now'))
    """
    )

    # Get the auto-generated race_id
    race_id = cursor.lastrowid

    # Insert first result
    cursor.execute(
        """
        INSERT INTO race_results (race_id, driver_id, finish_position)
        VALUES (?, 1, 1)
    """,
        (race_id,),
    )
    test_db.conn.commit()

    # Try to insert duplicate race_id + driver_id
    with pytest.raises(sqlite3.IntegrityError):
        cursor.execute(
            """
            INSERT INTO race_results (race_id, driver_id, finish_position)
            VALUES (?, 1, 2)
        """,
            (race_id,),
        )


def test_check_constraints_on_scrape_log(test_db):
    """Test that CHECK constraints work on scrape_log."""
    cursor = test_db.conn.cursor()

    # Valid entity_type and status should work
    cursor.execute(
        """
        INSERT INTO scrape_log (entity_type, entity_url, status)
        VALUES ('league', 'http://test.com', 'success')
    """
    )
    test_db.conn.commit()

    # Invalid entity_type should fail
    with pytest.raises(sqlite3.IntegrityError):
        cursor.execute(
            """
            INSERT INTO scrape_log (entity_type, entity_url, status)
            VALUES ('invalid', 'http://test.com', 'success')
        """
        )


def test_check_constraints_on_season_status(test_db):
    """Test that CHECK constraints work on season status."""
    cursor = test_db.conn.cursor()

    # Setup
    cursor.execute(
        """
        INSERT INTO leagues (league_id, name, url, scraped_at)
        VALUES (1, 'League', 'http://league.com', datetime('now'))
    """
    )
    cursor.execute(
        """
        INSERT INTO series (series_id, league_id, name, url, scraped_at)
        VALUES (1, 1, 'Series', 'http://series.com', datetime('now'))
    """
    )

    # Valid status should work
    cursor.execute(
        """
        INSERT INTO seasons (season_id, series_id, name, url, status, scraped_at)
        VALUES (1, 1, 'Season', 'http://season.com', 'active', datetime('now'))
    """
    )
    test_db.conn.commit()

    # Invalid status should fail
    with pytest.raises(sqlite3.IntegrityError):
        cursor.execute(
            """
            INSERT INTO seasons (season_id, series_id, name, url, status, scraped_at)
            VALUES (2, 1, 'Season2', 'http://season2.com', 'invalid', datetime('now'))
        """
        )


# Task 2.2: League CRUD Operations Tests


def test_upsert_league_inserts_new(test_db):
    """Test that upsert_league inserts a new league."""
    league_id = test_db.upsert_league(
        1558,
        {
            "name": "The OBRL",
            "url": "http://simracerhub.com/league.php?league_id=1558",
            "description": "Online Racing League",
            "organizer": "OBRL Staff",
            "scraped_at": "2025-01-15 10:00:00",
        },
    )

    assert league_id == 1558

    # Verify it was inserted
    league = test_db.get_league(1558)
    assert league is not None
    assert league["league_id"] == 1558
    assert league["name"] == "The OBRL"
    assert league["url"] == "http://simracerhub.com/league.php?league_id=1558"
    assert league["description"] == "Online Racing League"
    assert league["organizer"] == "OBRL Staff"


def test_upsert_league_updates_existing(test_db):
    """Test that upsert_league updates an existing league."""
    # Insert first version
    test_db.upsert_league(
        1558,
        {
            "name": "The OBRL",
            "url": "http://simracerhub.com/league.php?league_id=1558",
            "description": "Old description",
            "scraped_at": "2025-01-15 10:00:00",
        },
    )

    # Get initial updated_at
    league1 = test_db.get_league(1558)
    updated_at_1 = league1["updated_at"]

    # Wait to ensure timestamp difference
    time.sleep(0.01)

    # Update with new data
    test_db.upsert_league(
        1558,
        {
            "name": "The OBRL Updated",
            "url": "http://simracerhub.com/league.php?league_id=1558",
            "description": "New description",
            "organizer": "New Staff",
            "scraped_at": "2025-01-15 11:00:00",
        },
    )

    # Verify it was updated
    league2 = test_db.get_league(1558)
    assert league2["name"] == "The OBRL Updated"
    assert league2["description"] == "New description"
    assert league2["organizer"] == "New Staff"
    assert league2["scraped_at"] == "2025-01-15 11:00:00"

    # Verify updated_at changed
    assert league2["updated_at"] >= updated_at_1


def test_get_league_returns_correct_data(test_db):
    """Test that get_league returns correct league data."""
    test_db.upsert_league(
        1558,
        {
            "name": "The OBRL",
            "url": "http://simracerhub.com/league.php?league_id=1558",
            "scraped_at": "2025-01-15 10:00:00",
        },
    )

    league = test_db.get_league(1558)
    assert league is not None
    assert isinstance(league, dict)
    assert "league_id" in league
    assert "name" in league
    assert "url" in league
    assert "created_at" in league
    assert "updated_at" in league


def test_get_league_not_found_returns_none(test_db):
    """Test that get_league returns None for non-existent league."""
    league = test_db.get_league(99999)
    assert league is None


def test_get_league_by_url(test_db):
    """Test that get_league_by_url finds league by URL."""
    test_url = "http://simracerhub.com/league.php?league_id=1558"
    test_db.upsert_league(
        1558,
        {
            "name": "The OBRL",
            "url": test_url,
            "scraped_at": "2025-01-15 10:00:00",
        },
    )

    league = test_db.get_league_by_url(test_url)
    assert league is not None
    assert league["league_id"] == 1558
    assert league["url"] == test_url

    # Test not found
    league = test_db.get_league_by_url("http://notfound.com")
    assert league is None


def test_league_timestamps_auto_update(test_db):
    """Test that league timestamps are handled correctly."""
    # Insert league
    test_db.upsert_league(
        1558,
        {
            "name": "The OBRL",
            "url": "http://simracerhub.com/league.php?league_id=1558",
            "scraped_at": "2025-01-15 10:00:00",
        },
    )

    league1 = test_db.get_league(1558)
    assert league1["created_at"] is not None
    assert league1["updated_at"] is not None

    created_at_1 = league1["created_at"]
    updated_at_1 = league1["updated_at"]

    # Wait to ensure timestamp difference
    time.sleep(0.01)

    # Update league
    test_db.upsert_league(
        1558,
        {
            "name": "The OBRL Updated",
            "url": "http://simracerhub.com/league.php?league_id=1558",
            "scraped_at": "2025-01-15 11:00:00",
        },
    )

    league2 = test_db.get_league(1558)

    # created_at should not change
    assert league2["created_at"] == created_at_1

    # updated_at should change or stay the same (>= for fast execution)
    assert league2["updated_at"] >= updated_at_1


def test_upsert_league_missing_required_fields(test_db):
    """Test that upsert_league raises error for missing required fields."""
    # Missing name
    with pytest.raises(ValueError, match="name, url, and scraped_at are required"):
        test_db.upsert_league(1558, {"url": "http://test.com", "scraped_at": "2025-01-15"})

    # Missing url
    with pytest.raises(ValueError, match="name, url, and scraped_at are required"):
        test_db.upsert_league(1558, {"name": "Test", "scraped_at": "2025-01-15"})

    # Missing scraped_at
    with pytest.raises(ValueError, match="name, url, and scraped_at are required"):
        test_db.upsert_league(1558, {"name": "Test", "url": "http://test.com"})


def test_upsert_league_without_connection():
    """Test that upsert_league raises error when database not connected."""
    try:
        from database import Database
    except ImportError:
        from src.database import Database

    db = Database(":memory:")
    # Don't connect

    with pytest.raises(RuntimeError, match="Database not connected"):
        db.upsert_league(
            1558, {"name": "Test", "url": "http://test.com", "scraped_at": "2025-01-15"}
        )


def test_get_league_without_connection():
    """Test that get_league raises error when database not connected."""
    try:
        from database import Database
    except ImportError:
        from src.database import Database

    db = Database(":memory:")
    # Don't connect

    with pytest.raises(RuntimeError, match="Database not connected"):
        db.get_league(1558)


def test_get_league_by_url_without_connection():
    """Test that get_league_by_url raises error when database not connected."""
    try:
        from database import Database
    except ImportError:
        from src.database import Database

    db = Database(":memory:")
    # Don't connect

    with pytest.raises(RuntimeError, match="Database not connected"):
        db.get_league_by_url("http://test.com")


# Task 2.3: Series CRUD Operations Tests


def test_upsert_series(test_db):
    """Test that upsert_series inserts and updates series."""
    # First create a league (required foreign key)
    test_db.upsert_league(
        1558, {"name": "The OBRL", "url": "http://test.com/league", "scraped_at": "2025-01-15"}
    )

    # Insert series
    series_id = test_db.upsert_series(
        3714,
        1558,
        {
            "name": "Wednesday Night Series",
            "url": "http://test.com/series/3714",
            "description": "Weekly oval racing",
            "vehicle_type": "Stock Car",
            "day_of_week": "Wednesday",
            "active": True,
            "season_count": 5,
            "scraped_at": "2025-01-15 10:00:00",
        },
    )

    assert series_id == 3714

    # Verify it was inserted
    series = test_db.get_series(3714)
    assert series is not None
    assert series["series_id"] == 3714
    assert series["league_id"] == 1558
    assert series["name"] == "Wednesday Night Series"
    assert series["vehicle_type"] == "Stock Car"
    assert series["active"] == 1  # SQLite stores boolean as int

    # Update series
    time.sleep(0.01)
    test_db.upsert_series(
        3714,
        1558,
        {
            "name": "Wednesday Night Series Updated",
            "url": "http://test.com/series/3714",
            "active": False,
            "scraped_at": "2025-01-15 11:00:00",
        },
    )

    series_updated = test_db.get_series(3714)
    assert series_updated["name"] == "Wednesday Night Series Updated"
    assert series_updated["active"] == 0
    assert series_updated["updated_at"] >= series["updated_at"]


def test_get_series(test_db):
    """Test that get_series returns correct data."""
    # Setup
    test_db.upsert_league(
        1558, {"name": "The OBRL", "url": "http://test.com/league", "scraped_at": "2025-01-15"}
    )
    test_db.upsert_series(
        3714,
        1558,
        {
            "name": "Wednesday Night Series",
            "url": "http://test.com/series/3714",
            "scraped_at": "2025-01-15",
        },
    )

    # Get series
    series = test_db.get_series(3714)
    assert series is not None
    assert isinstance(series, dict)
    assert "series_id" in series
    assert "league_id" in series
    assert "name" in series
    assert "url" in series
    assert "created_at" in series
    assert "updated_at" in series

    # Get non-existent series
    series = test_db.get_series(99999)
    assert series is None


def test_get_series_by_league(test_db):
    """Test that get_series_by_league returns all series for a league."""
    # Setup league
    test_db.upsert_league(
        1558, {"name": "The OBRL", "url": "http://test.com/league", "scraped_at": "2025-01-15"}
    )

    # Insert multiple series
    test_db.upsert_series(
        3714,
        1558,
        {
            "name": "Wednesday Night Series",
            "url": "http://test.com/series/3714",
            "scraped_at": "2025-01-15",
        },
    )
    test_db.upsert_series(
        3715,
        1558,
        {
            "name": "Friday Night Series",
            "url": "http://test.com/series/3715",
            "scraped_at": "2025-01-15",
        },
    )
    test_db.upsert_series(
        3716,
        1558,
        {
            "name": "Sunday Series",
            "url": "http://test.com/series/3716",
            "scraped_at": "2025-01-15",
        },
    )

    # Get all series for league
    series_list = test_db.get_series_by_league(1558)
    assert len(series_list) == 3
    assert all(isinstance(s, dict) for s in series_list)
    assert all(s["league_id"] == 1558 for s in series_list)

    # Verify they're sorted by series_id
    assert series_list[0]["series_id"] == 3714
    assert series_list[1]["series_id"] == 3715
    assert series_list[2]["series_id"] == 3716

    # Get series for league with no series
    series_list = test_db.get_series_by_league(99999)
    assert series_list == []


def test_series_requires_valid_league_id(test_db):
    """Test that series requires a valid league_id (foreign key)."""
    # Try to insert series with invalid league_id
    with pytest.raises(sqlite3.IntegrityError):
        test_db.upsert_series(
            3714,
            99999,  # Invalid league_id
            {
                "name": "Test Series",
                "url": "http://test.com/series",
                "scraped_at": "2025-01-15",
            },
        )


def test_upsert_series_missing_required_fields(test_db):
    """Test that upsert_series validates required fields."""
    # Setup league
    test_db.upsert_league(
        1558, {"name": "The OBRL", "url": "http://test.com/league", "scraped_at": "2025-01-15"}
    )

    # Missing name
    with pytest.raises(ValueError, match="name, url, and scraped_at are required"):
        test_db.upsert_series(3714, 1558, {"url": "http://test.com", "scraped_at": "2025-01-15"})

    # Missing url
    with pytest.raises(ValueError, match="name, url, and scraped_at are required"):
        test_db.upsert_series(3714, 1558, {"name": "Test", "scraped_at": "2025-01-15"})

    # Missing scraped_at
    with pytest.raises(ValueError, match="name, url, and scraped_at are required"):
        test_db.upsert_series(3714, 1558, {"name": "Test", "url": "http://test.com"})


def test_upsert_series_without_connection():
    """Test that upsert_series raises error when database not connected."""
    try:
        from database import Database
    except ImportError:
        from src.database import Database

    db = Database(":memory:")

    with pytest.raises(RuntimeError, match="Database not connected"):
        db.upsert_series(
            3714, 1558, {"name": "Test", "url": "http://test.com", "scraped_at": "2025-01-15"}
        )


def test_get_series_without_connection():
    """Test that get_series raises error when database not connected."""
    try:
        from database import Database
    except ImportError:
        from src.database import Database

    db = Database(":memory:")

    with pytest.raises(RuntimeError, match="Database not connected"):
        db.get_series(3714)


def test_get_series_by_league_without_connection():
    """Test that get_series_by_league raises error when database not connected."""
    try:
        from database import Database
    except ImportError:
        from src.database import Database

    db = Database(":memory:")

    with pytest.raises(RuntimeError, match="Database not connected"):
        db.get_series_by_league(1558)


# ============================================================================
# URL Caching Tests (Task 2.5)
# ============================================================================


def test_is_url_cached_fresh_returns_true(test_db):
    """Test that is_url_cached returns True for fresh URLs."""
    from datetime import datetime

    # Insert a fresh league
    test_db.upsert_league(
        1558,
        {
            "name": "Test League",
            "url": "http://test.com/league/1558",
            "scraped_at": datetime.now().isoformat(),
        },
    )

    # Check if URL is cached (within 1 day)
    result = test_db.is_url_cached("http://test.com/league/1558", "league", max_age_days=1)
    assert result is True


def test_is_url_cached_expired_returns_false(test_db):
    """Test that is_url_cached returns False for expired URLs."""
    from datetime import datetime, timedelta

    # Insert a league with old timestamp (2 days ago)
    old_timestamp = (datetime.now() - timedelta(days=2)).isoformat()
    test_db.upsert_league(
        1558,
        {
            "name": "Test League",
            "url": "http://test.com/league/1558",
            "scraped_at": old_timestamp,
        },
    )

    # Check if URL is cached (max age 1 day) - should be False (expired)
    result = test_db.is_url_cached("http://test.com/league/1558", "league", max_age_days=1)
    assert result is False


def test_is_url_cached_missing_returns_false(test_db):
    """Test that is_url_cached returns False for non-existent URLs."""
    # Check a URL that doesn't exist
    result = test_db.is_url_cached("http://test.com/league/99999", "league", max_age_days=1)
    assert result is False


def test_is_url_cached_all_entity_types(test_db):
    """Test that is_url_cached works for all entity types."""
    from datetime import datetime

    now = datetime.now().isoformat()

    # League
    test_db.upsert_league(
        1558, {"name": "Test League", "url": "http://test.com/league/1558", "scraped_at": now}
    )
    assert test_db.is_url_cached("http://test.com/league/1558", "league", max_age_days=1)

    # Series
    test_db.upsert_series(
        3714,
        1558,
        {
            "name": "Test Series",
            "url": "http://test.com/series/3714",
            "scraped_at": now,
        },
    )
    assert test_db.is_url_cached("http://test.com/series/3714", "series", max_age_days=1)

    # Season
    test_db.upsert_season(
        12345,
        3714,
        {
            "name": "2025 Season",
            "url": "http://test.com/season/12345",
            "year": 2025,
            "scraped_at": now,
        },
    )
    assert test_db.is_url_cached("http://test.com/season/12345", "season", max_age_days=1)

    # Race
    test_db.upsert_race(
        67890,
        12345,
        {
            "name": "Race 1",
            "url": "http://test.com/race/67890",
            "race_number": 1,
            "scraped_at": now,
        },
    )
    assert test_db.is_url_cached("http://test.com/race/67890", "race", max_age_days=1)

    # Driver
    test_db.upsert_driver(
        111,
        1558,
        {
            "name": "John Smith",
            "url": "http://test.com/driver/111",
            "scraped_at": now,
        },
    )
    assert test_db.is_url_cached("http://test.com/driver/111", "driver", max_age_days=1)

    # Team
    test_db.upsert_team(
        222,
        1558,
        {
            "name": "Team Alpha",
            "url": "http://test.com/team/222",
            "scraped_at": now,
        },
    )
    assert test_db.is_url_cached("http://test.com/team/222", "team", max_age_days=1)


def test_is_url_cached_boundary_exactly_max_age(test_db):
    """Test is_url_cached behavior at exactly max_age boundary."""
    from datetime import datetime, timedelta

    # Insert a league exactly 1 day old
    exactly_one_day_ago = (datetime.now() - timedelta(days=1)).isoformat()
    test_db.upsert_league(
        1558,
        {
            "name": "Test League",
            "url": "http://test.com/league/1558",
            "scraped_at": exactly_one_day_ago,
        },
    )

    # Should be False (expired) since age >= max_age
    result = test_db.is_url_cached("http://test.com/league/1558", "league", max_age_days=1)
    assert result is False


def test_is_url_cached_max_age_none_always_fresh(test_db):
    """Test that is_url_cached with max_age_days=None always returns True if URL exists."""
    from datetime import datetime, timedelta

    # Insert a very old league (100 days ago)
    very_old = (datetime.now() - timedelta(days=100)).isoformat()
    test_db.upsert_league(
        1558,
        {
            "name": "Test League",
            "url": "http://test.com/league/1558",
            "scraped_at": very_old,
        },
    )

    # With max_age_days=None, should still return True (indefinite cache)
    result = test_db.is_url_cached("http://test.com/league/1558", "league", max_age_days=None)
    assert result is True


def test_is_url_cached_invalid_entity_type(test_db):
    """Test that is_url_cached raises ValueError for invalid entity type."""
    with pytest.raises(ValueError, match="Invalid entity_type"):
        test_db.is_url_cached("http://test.com/invalid", "invalid_type", max_age_days=1)


def test_is_url_cached_invalid_timestamp_returns_false(test_db):
    """Test that is_url_cached returns False when scraped_at is invalid."""
    # Insert a league with invalid timestamp
    test_db.conn.execute(
        """
        INSERT INTO leagues (league_id, name, url, scraped_at)
        VALUES (?, ?, ?, ?)
        """,
        (1558, "Test League", "http://test.com/league/1558", "invalid-timestamp"),
    )
    test_db.conn.commit()

    # Should return False since timestamp is invalid
    result = test_db.is_url_cached("http://test.com/league/1558", "league", max_age_days=1)
    assert result is False


def test_is_url_cached_without_connection():
    """Test that is_url_cached raises error when database not connected."""
    try:
        from database import Database
    except ImportError:
        from src.database import Database

    db = Database(":memory:")

    with pytest.raises(RuntimeError, match="Database not connected"):
        db.is_url_cached("http://test.com/league/1558", "league", max_age_days=1)


# ============================================================================
# Scrape Logging Tests (Task 2.6)
# ============================================================================


def test_log_scrape_success(test_db):
    """Test logging a successful scrape."""
    # Log a successful scrape
    log_id = test_db.log_scrape(
        entity_type="league",
        entity_url="http://test.com/league/1558",
        status="success",
        entity_id=1558,
        duration_ms=1500,
    )

    # Verify log was created
    assert log_id is not None
    assert isinstance(log_id, int)

    # Query the log to verify
    cursor = test_db.conn.execute("SELECT * FROM scrape_log WHERE log_id = ?", (log_id,))
    log = cursor.fetchone()

    assert log is not None
    # Columns: log_id, entity_type, entity_id, entity_url, status, error_message, duration_ms, timestamp
    assert log[1] == "league"  # entity_type
    assert log[2] == 1558  # entity_id
    assert log[3] == "http://test.com/league/1558"  # entity_url
    assert log[4] == "success"  # status
    assert log[5] is None  # error_message (no error)
    assert log[6] == 1500  # duration_ms
    assert log[7] is not None  # timestamp


def test_log_scrape_failure_with_error(test_db):
    """Test logging a failed scrape with error message."""
    # Log a failed scrape
    log_id = test_db.log_scrape(
        entity_type="series",
        entity_url="http://test.com/series/3714",
        status="failed",
        entity_id=3714,
        error_msg="Connection timeout",
        duration_ms=30000,
    )

    # Verify log was created
    assert log_id is not None

    # Query the log to verify
    cursor = test_db.conn.execute(
        "SELECT entity_type, status, error_message, duration_ms FROM scrape_log WHERE log_id = ?",
        (log_id,),
    )
    log = cursor.fetchone()

    assert log[0] == "series"
    assert log[1] == "failed"
    assert log[2] == "Connection timeout"
    assert log[3] == 30000


def test_log_scrape_skipped(test_db):
    """Test logging a skipped scrape."""
    # Log a skipped scrape (e.g., due to cache)
    log_id = test_db.log_scrape(
        entity_type="race",
        entity_url="http://test.com/race/67890",
        status="skipped",
        entity_id=67890,
    )

    # Verify log was created
    assert log_id is not None

    # Query the log to verify
    cursor = test_db.conn.execute(
        "SELECT entity_type, status, duration_ms FROM scrape_log WHERE log_id = ?",
        (log_id,),
    )
    log = cursor.fetchone()

    assert log[0] == "race"
    assert log[1] == "skipped"
    assert log[2] is None  # No duration for skipped


def test_log_scrape_without_entity_id(test_db):
    """Test logging a scrape without entity_id (URL-based logging)."""
    # Log a scrape with only URL (entity_id is optional)
    log_id = test_db.log_scrape(
        entity_type="driver",
        entity_url="http://test.com/driver/unknown",
        status="failed",
        error_msg="Driver not found",
    )

    # Verify log was created
    assert log_id is not None

    # Query the log to verify
    cursor = test_db.conn.execute(
        "SELECT entity_id, entity_url, error_message FROM scrape_log WHERE log_id = ?",
        (log_id,),
    )
    log = cursor.fetchone()

    assert log[0] is None  # entity_id is NULL
    assert log[1] == "http://test.com/driver/unknown"
    assert log[2] == "Driver not found"


def test_log_scrape_invalid_status(test_db):
    """Test that log_scrape validates status field."""
    # Try to log with invalid status
    with pytest.raises(ValueError, match="Invalid status"):
        test_db.log_scrape(
            entity_type="league",
            entity_url="http://test.com/league/1558",
            status="invalid_status",  # Not one of: success, failed, skipped
        )


def test_log_scrape_invalid_entity_type(test_db):
    """Test that log_scrape validates entity_type field."""
    # Try to log with invalid entity_type
    with pytest.raises(ValueError, match="Invalid entity_type"):
        test_db.log_scrape(
            entity_type="invalid_type",
            entity_url="http://test.com/something",
            status="success",
        )


def test_log_scrape_required_fields(test_db):
    """Test that log_scrape requires entity_url and status."""
    # Missing entity_url
    with pytest.raises((ValueError, TypeError)):
        test_db.log_scrape(
            entity_type="league",
            entity_url=None,
            status="success",
        )

    # Missing status
    with pytest.raises((ValueError, TypeError)):
        test_db.log_scrape(
            entity_type="league",
            entity_url="http://test.com/league/1558",
            status=None,
        )


def test_log_scrape_without_connection():
    """Test that log_scrape raises error when database not connected."""
    try:
        from database import Database
    except ImportError:
        from src.database import Database

    db = Database(":memory:")

    with pytest.raises(RuntimeError, match="Database not connected"):
        db.log_scrape(
            entity_type="league",
            entity_url="http://test.com/league/1558",
            status="success",
        )
