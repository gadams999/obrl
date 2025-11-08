"""Database manager for SimRacer scraper."""

import sqlite3


class Database:
    """SQLite database manager for SimRacer scraper."""

    def __init__(self, db_path: str = ":memory:"):
        """
        Initialize database manager.

        Args:
            db_path: Path to database file, or ":memory:" for in-memory database
        """
        self.db_path = db_path
        self.conn: sqlite3.Connection | None = None

    def __enter__(self):
        """Context manager entry."""
        self.connect()
        return self

    def __exit__(self, exc_type, exc_val, exc_tb):
        """Context manager exit."""
        self.close()

    def connect(self):
        """Open database connection."""
        self.conn = sqlite3.connect(self.db_path)
        self.conn.row_factory = sqlite3.Row
        # Enable foreign keys
        self.conn.execute("PRAGMA foreign_keys = ON")

    def close(self):
        """Close database connection."""
        if self.conn:
            self.conn.close()
            self.conn = None

    def initialize_schema(self):
        """Create all tables and indexes."""
        if not self.conn:
            raise RuntimeError("Database not connected")

        cursor = self.conn.cursor()

        # Table: leagues
        cursor.execute(
            """
            CREATE TABLE IF NOT EXISTS leagues (
                league_id INTEGER PRIMARY KEY,
                name TEXT NOT NULL,
                description TEXT,
                url TEXT NOT NULL UNIQUE,
                scraped_at TIMESTAMP NOT NULL,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            )
        """
        )
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_leagues_url ON leagues(url)")
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_leagues_scraped_at ON leagues(scraped_at)")

        # Table: teams
        cursor.execute(
            """
            CREATE TABLE IF NOT EXISTS teams (
                team_id INTEGER PRIMARY KEY,
                league_id INTEGER NOT NULL,
                name TEXT NOT NULL,
                driver_count INTEGER,
                url TEXT,
                scraped_at TIMESTAMP NOT NULL,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (league_id) REFERENCES leagues(league_id)
            )
        """
        )
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_teams_league_id ON teams(league_id)")
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_teams_scraped_at ON teams(scraped_at)")

        # Table: drivers
        cursor.execute(
            """
            CREATE TABLE IF NOT EXISTS drivers (
                driver_id INTEGER PRIMARY KEY,
                league_id INTEGER NOT NULL,
                team_id INTEGER,
                name TEXT NOT NULL,
                first_name TEXT,
                last_name TEXT,
                car_numbers TEXT,
                primary_number TEXT,
                club TEXT,
                club_id INTEGER,
                irating INTEGER,
                safety_rating REAL,
                license_class TEXT,
                url TEXT NOT NULL UNIQUE,
                scraped_at TIMESTAMP NOT NULL,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (league_id) REFERENCES leagues(league_id),
                FOREIGN KEY (team_id) REFERENCES teams(team_id)
            )
        """
        )
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_drivers_league_id ON drivers(league_id)")
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_drivers_team_id ON drivers(team_id)")
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_drivers_url ON drivers(url)")
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_drivers_name ON drivers(name)")
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_drivers_scraped_at ON drivers(scraped_at)")

        # Table: series
        cursor.execute(
            """
            CREATE TABLE IF NOT EXISTS series (
                series_id INTEGER PRIMARY KEY,
                league_id INTEGER NOT NULL,
                name TEXT NOT NULL,
                description TEXT,
                created_date DATE,
                num_seasons INTEGER,
                url TEXT NOT NULL UNIQUE,
                scraped_at TIMESTAMP NOT NULL,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (league_id) REFERENCES leagues(league_id)
            )
        """
        )
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_series_league_id ON series(league_id)")
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_series_url ON series(url)")
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_series_scraped_at ON series(scraped_at)")

        # Table: seasons
        cursor.execute(
            """
            CREATE TABLE IF NOT EXISTS seasons (
                season_id INTEGER PRIMARY KEY,
                series_id INTEGER NOT NULL,
                name TEXT NOT NULL,
                description TEXT,
                url TEXT NOT NULL UNIQUE,
                scraped_at TIMESTAMP NOT NULL,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (series_id) REFERENCES series(series_id)
            )
        """
        )
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_seasons_series_id ON seasons(series_id)")
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_seasons_url ON seasons(url)")
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_seasons_scraped_at ON seasons(scraped_at)")

        # Table: races
        cursor.execute(
            """
            CREATE TABLE IF NOT EXISTS races (
                race_id INTEGER PRIMARY KEY AUTOINCREMENT,
                schedule_id INTEGER NOT NULL UNIQUE,
                season_id INTEGER NOT NULL,
                race_number INTEGER NOT NULL,
                event_name TEXT,
                date TIMESTAMP,
                race_time TEXT,
                practice_time TEXT,
                track_id INTEGER,
                track_config_id INTEGER,
                track_name TEXT,
                track_type TEXT,
                track_length REAL,
                track_config_iracing_id TEXT,
                planned_laps INTEGER,
                points_race BOOLEAN,
                off_week BOOLEAN,
                night_race BOOLEAN,
                playoff_race BOOLEAN,
                race_duration_minutes INTEGER,
                total_laps INTEGER,
                leaders INTEGER,
                lead_changes INTEGER,
                cautions INTEGER,
                caution_laps INTEGER,
                num_drivers INTEGER,
                weather_type TEXT,
                cloud_conditions TEXT,
                temperature_f INTEGER,
                humidity_pct INTEGER,
                fog_pct INTEGER,
                weather_wind_speed TEXT,
                weather_wind_dir TEXT,
                weather_wind_unit TEXT,
                url TEXT NOT NULL UNIQUE,
                is_complete BOOLEAN DEFAULT 0,
                scraped_at TIMESTAMP NOT NULL,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (season_id) REFERENCES seasons(season_id)
            )
        """
        )
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_races_schedule_id ON races(schedule_id)")
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_races_season_id ON races(season_id)")
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_races_url ON races(url)")
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_races_date ON races(date)")
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_races_is_complete ON races(is_complete)")
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_races_scraped_at ON races(scraped_at)")

        # Table: race_results
        cursor.execute(
            """
            CREATE TABLE IF NOT EXISTS race_results (
                result_id INTEGER PRIMARY KEY AUTOINCREMENT,
                race_id INTEGER NOT NULL,
                driver_id INTEGER NOT NULL,
                team TEXT,
                finish_position INTEGER,
                starting_position INTEGER,
                car_number TEXT,
                qualifying_time TEXT,
                fastest_lap TEXT,
                fastest_lap_number INTEGER,
                average_lap TEXT,
                interval TEXT,
                laps_completed INTEGER,
                laps_led INTEGER,
                incident_points INTEGER,
                race_points INTEGER,
                bonus_points INTEGER,
                penalty_points INTEGER,
                total_points INTEGER,
                fast_laps INTEGER,
                quality_passes INTEGER,
                closing_passes INTEGER,
                total_passes INTEGER,
                average_running_position REAL,
                irating INTEGER,
                status TEXT,
                car_id INTEGER,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (race_id) REFERENCES races(race_id),
                FOREIGN KEY (driver_id) REFERENCES drivers(driver_id),
                UNIQUE(race_id, driver_id)
            )
        """
        )
        cursor.execute(
            "CREATE INDEX IF NOT EXISTS idx_race_results_race_id ON race_results(race_id)"
        )
        cursor.execute(
            "CREATE INDEX IF NOT EXISTS idx_race_results_driver_id ON race_results(driver_id)"
        )
        cursor.execute(
            "CREATE INDEX IF NOT EXISTS idx_race_results_position ON race_results(finish_position)"
        )

        # Table: scrape_log
        cursor.execute(
            """
            CREATE TABLE IF NOT EXISTS scrape_log (
                log_id INTEGER PRIMARY KEY AUTOINCREMENT,
                entity_type TEXT NOT NULL CHECK(entity_type IN ('league', 'team', 'driver', 'series', 'season', 'race')),
                entity_id INTEGER,
                entity_url TEXT NOT NULL,
                status TEXT NOT NULL CHECK(status IN ('success', 'failed', 'skipped')),
                error_message TEXT,
                duration_ms INTEGER,
                timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            )
        """
        )
        cursor.execute(
            "CREATE INDEX IF NOT EXISTS idx_scrape_log_entity_type ON scrape_log(entity_type)"
        )
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_scrape_log_status ON scrape_log(status)")
        cursor.execute(
            "CREATE INDEX IF NOT EXISTS idx_scrape_log_timestamp ON scrape_log(timestamp)"
        )

        # Table: schema_alerts
        cursor.execute(
            """
            CREATE TABLE IF NOT EXISTS schema_alerts (
                alert_id INTEGER PRIMARY KEY AUTOINCREMENT,
                entity_type TEXT NOT NULL,
                alert_type TEXT NOT NULL,
                details TEXT NOT NULL,
                url TEXT,
                resolved BOOLEAN DEFAULT 0,
                timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            )
        """
        )
        cursor.execute(
            "CREATE INDEX IF NOT EXISTS idx_schema_alerts_resolved ON schema_alerts(resolved)"
        )
        cursor.execute(
            "CREATE INDEX IF NOT EXISTS idx_schema_alerts_timestamp ON schema_alerts(timestamp)"
        )

        self.conn.commit()

    def upsert_league(self, league_id: int, data: dict) -> int:
        """
        Insert or update a league record.

        Args:
            league_id: League ID
            data: Dictionary with league fields (name, url, description, scraped_at)

        Returns:
            The league_id of the inserted/updated record
        """
        if not self.conn:
            raise RuntimeError("Database not connected")

        cursor = self.conn.cursor()

        # Required fields
        name = data.get("name")
        url = data.get("url")
        scraped_at = data.get("scraped_at")

        if not name or not url or not scraped_at:
            raise ValueError("name, url, and scraped_at are required fields")

        # Optional fields
        description = data.get("description")

        cursor.execute(
            """
            INSERT INTO leagues (league_id, name, url, description, scraped_at, updated_at)
            VALUES (?, ?, ?, ?, ?, CURRENT_TIMESTAMP)
            ON CONFLICT(league_id) DO UPDATE SET
                name = excluded.name,
                url = excluded.url,
                description = excluded.description,
                scraped_at = excluded.scraped_at,
                updated_at = CURRENT_TIMESTAMP
        """,
            (league_id, name, url, description, scraped_at),
        )

        self.conn.commit()
        return league_id

    def get_league(self, league_id: int) -> dict | None:
        """
        Get a league by ID.

        Args:
            league_id: League ID

        Returns:
            Dictionary with league data or None if not found
        """
        if not self.conn:
            raise RuntimeError("Database not connected")

        cursor = self.conn.cursor()
        cursor.execute("SELECT * FROM leagues WHERE league_id = ?", (league_id,))
        row = cursor.fetchone()

        if row:
            return dict(row)
        return None

    def get_league_by_url(self, url: str) -> dict | None:
        """
        Get a league by URL.

        Args:
            url: League URL

        Returns:
            Dictionary with league data or None if not found
        """
        if not self.conn:
            raise RuntimeError("Database not connected")

        cursor = self.conn.cursor()
        cursor.execute("SELECT * FROM leagues WHERE url = ?", (url,))
        row = cursor.fetchone()

        if row:
            return dict(row)
        return None

    def upsert_series(self, series_id: int, league_id: int, data: dict) -> int:
        """
        Insert or update a series record.

        Args:
            series_id: Series ID
            league_id: League ID (foreign key)
            data: Dictionary with series fields

        Returns:
            The series_id of the inserted/updated record
        """
        if not self.conn:
            raise RuntimeError("Database not connected")

        cursor = self.conn.cursor()

        # Required fields
        name = data.get("name")
        url = data.get("url")
        scraped_at = data.get("scraped_at")

        if not name or not url or not scraped_at:
            raise ValueError("name, url, and scraped_at are required fields")

        # Optional fields
        description = data.get("description")
        created_date = data.get("created_date")
        num_seasons = data.get("num_seasons")

        cursor.execute(
            """
            INSERT INTO series (
                series_id, league_id, name, url, description,
                created_date, num_seasons, scraped_at, updated_at
            )
            VALUES (?, ?, ?, ?, ?, ?, ?, ?, CURRENT_TIMESTAMP)
            ON CONFLICT(series_id) DO UPDATE SET
                league_id = excluded.league_id,
                name = excluded.name,
                url = excluded.url,
                description = excluded.description,
                created_date = excluded.created_date,
                num_seasons = excluded.num_seasons,
                scraped_at = excluded.scraped_at,
                updated_at = CURRENT_TIMESTAMP
        """,
            (
                series_id,
                league_id,
                name,
                url,
                description,
                created_date,
                num_seasons,
                scraped_at,
            ),
        )

        self.conn.commit()
        return series_id

    def get_series(self, series_id: int) -> dict | None:
        """
        Get a series by ID.

        Args:
            series_id: Series ID

        Returns:
            Dictionary with series data or None if not found
        """
        if not self.conn:
            raise RuntimeError("Database not connected")

        cursor = self.conn.cursor()
        cursor.execute("SELECT * FROM series WHERE series_id = ?", (series_id,))
        row = cursor.fetchone()

        if row:
            return dict(row)
        return None

    def get_series_by_league(self, league_id: int) -> list[dict]:
        """
        Get all series for a league.

        Args:
            league_id: League ID

        Returns:
            List of dictionaries with series data
        """
        if not self.conn:
            raise RuntimeError("Database not connected")

        cursor = self.conn.cursor()
        cursor.execute("SELECT * FROM series WHERE league_id = ? ORDER BY series_id", (league_id,))
        rows = cursor.fetchall()

        return [dict(row) for row in rows]

    def upsert_season(self, season_id: int, series_id: int, data: dict) -> int:
        """
        Insert or update a season record.

        Args:
            season_id: Season ID
            series_id: Series ID (foreign key)
            data: Dictionary with season fields

        Returns:
            The season_id of the inserted/updated record
        """
        if not self.conn:
            raise RuntimeError("Database not connected")

        cursor = self.conn.cursor()

        # Required fields
        name = data.get("name")
        url = data.get("url")
        scraped_at = data.get("scraped_at")

        if not name or not url or not scraped_at:
            raise ValueError("name, url, and scraped_at are required fields")

        # Optional fields
        description = data.get("description")

        cursor.execute(
            """
            INSERT INTO seasons (
                season_id, series_id, name, description, url,
                scraped_at, updated_at
            )
            VALUES (?, ?, ?, ?, ?, ?, CURRENT_TIMESTAMP)
            ON CONFLICT(season_id) DO UPDATE SET
                series_id = excluded.series_id,
                name = excluded.name,
                description = excluded.description,
                url = excluded.url,
                scraped_at = excluded.scraped_at,
                updated_at = CURRENT_TIMESTAMP
        """,
            (
                season_id,
                series_id,
                name,
                description,
                url,
                scraped_at,
            ),
        )

        self.conn.commit()
        return season_id

    def get_season(self, season_id: int) -> dict | None:
        """
        Get a season by ID.

        Args:
            season_id: Season ID

        Returns:
            Dictionary with season data or None if not found
        """
        if not self.conn:
            raise RuntimeError("Database not connected")

        cursor = self.conn.cursor()
        cursor.execute("SELECT * FROM seasons WHERE season_id = ?", (season_id,))
        row = cursor.fetchone()

        if row:
            return dict(row)
        return None

    def get_seasons_by_series(self, series_id: int) -> list[dict]:
        """
        Get all seasons for a series.

        Args:
            series_id: Series ID

        Returns:
            List of dictionaries with season data
        """
        if not self.conn:
            raise RuntimeError("Database not connected")

        cursor = self.conn.cursor()
        cursor.execute("SELECT * FROM seasons WHERE series_id = ? ORDER BY season_id", (series_id,))
        rows = cursor.fetchall()

        return [dict(row) for row in rows]

    def upsert_race(self, schedule_id: int, season_id: int, data: dict) -> int:
        """
        Insert or update a race record.

        Args:
            schedule_id: Schedule ID (unique identifier from SimRacerHub)
            season_id: Season ID (foreign key)
            data: Dictionary with race fields

        Returns:
            The race_id (auto-increment) of the inserted/updated record
        """
        if not self.conn:
            raise RuntimeError("Database not connected")

        cursor = self.conn.cursor()

        # Required fields
        url = data.get("url")
        scraped_at = data.get("scraped_at")
        race_number = data.get("race_number")

        if not url or not scraped_at or race_number is None:
            raise ValueError("url, scraped_at, and race_number are required fields")

        # Optional fields
        event_name = data.get("event_name")
        date = data.get("date")
        race_time = data.get("race_time")
        practice_time = data.get("practice_time")
        track_id = data.get("track_id")
        track_config_id = data.get("track_config_id")
        track_name = data.get("track_name")
        track_type = data.get("track_type")
        track_length = data.get("track_length")
        track_config_iracing_id = data.get("track_config_iracing_id")
        planned_laps = data.get("planned_laps")
        points_race = data.get("points_race")
        off_week = data.get("off_week")
        night_race = data.get("night_race")
        playoff_race = data.get("playoff_race")
        race_duration_minutes = data.get("race_duration_minutes")
        total_laps = data.get("total_laps")
        leaders = data.get("leaders")
        lead_changes = data.get("lead_changes")
        cautions = data.get("cautions")
        caution_laps = data.get("caution_laps")
        num_drivers = data.get("num_drivers")
        weather_type = data.get("weather_type")
        cloud_conditions = data.get("cloud_conditions")
        temperature_f = data.get("temperature_f")
        humidity_pct = data.get("humidity_pct")
        fog_pct = data.get("fog_pct")
        weather_wind_speed = data.get("weather_wind_speed")
        weather_wind_dir = data.get("weather_wind_dir")
        weather_wind_unit = data.get("weather_wind_unit")
        is_complete = data.get("is_complete", False)

        cursor.execute(
            """
            INSERT INTO races (
                schedule_id, season_id, race_number, event_name, date, race_time, practice_time,
                track_id, track_config_id, track_name, track_type, track_length, track_config_iracing_id,
                planned_laps, points_race, off_week, night_race, playoff_race,
                race_duration_minutes, total_laps, leaders, lead_changes, cautions, caution_laps, num_drivers,
                weather_type, cloud_conditions, temperature_f, humidity_pct, fog_pct,
                weather_wind_speed, weather_wind_dir, weather_wind_unit,
                url, is_complete, scraped_at, updated_at
            )
            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, CURRENT_TIMESTAMP)
            ON CONFLICT(schedule_id) DO UPDATE SET
                season_id = excluded.season_id,
                race_number = excluded.race_number,
                event_name = excluded.event_name,
                date = excluded.date,
                race_time = excluded.race_time,
                practice_time = excluded.practice_time,
                track_id = excluded.track_id,
                track_config_id = excluded.track_config_id,
                track_name = excluded.track_name,
                track_type = excluded.track_type,
                track_length = excluded.track_length,
                track_config_iracing_id = excluded.track_config_iracing_id,
                planned_laps = excluded.planned_laps,
                points_race = excluded.points_race,
                off_week = excluded.off_week,
                night_race = excluded.night_race,
                playoff_race = excluded.playoff_race,
                race_duration_minutes = excluded.race_duration_minutes,
                total_laps = excluded.total_laps,
                leaders = excluded.leaders,
                lead_changes = excluded.lead_changes,
                cautions = excluded.cautions,
                caution_laps = excluded.caution_laps,
                num_drivers = excluded.num_drivers,
                weather_type = excluded.weather_type,
                cloud_conditions = excluded.cloud_conditions,
                temperature_f = excluded.temperature_f,
                humidity_pct = excluded.humidity_pct,
                fog_pct = excluded.fog_pct,
                weather_wind_speed = excluded.weather_wind_speed,
                weather_wind_dir = excluded.weather_wind_dir,
                weather_wind_unit = excluded.weather_wind_unit,
                url = excluded.url,
                is_complete = excluded.is_complete,
                scraped_at = excluded.scraped_at,
                updated_at = CURRENT_TIMESTAMP
        """,
            (
                schedule_id,
                season_id,
                race_number,
                event_name,
                date,
                race_time,
                practice_time,
                track_id,
                track_config_id,
                track_name,
                track_type,
                track_length,
                track_config_iracing_id,
                planned_laps,
                points_race,
                off_week,
                night_race,
                playoff_race,
                race_duration_minutes,
                total_laps,
                leaders,
                lead_changes,
                cautions,
                caution_laps,
                num_drivers,
                weather_type,
                cloud_conditions,
                temperature_f,
                humidity_pct,
                fog_pct,
                weather_wind_speed,
                weather_wind_dir,
                weather_wind_unit,
                url,
                is_complete,
                scraped_at,
            ),
        )

        self.conn.commit()
        return cursor.lastrowid

    def get_race(self, schedule_id: int) -> dict | None:
        """
        Get a race by schedule_id.

        Args:
            schedule_id: Schedule ID

        Returns:
            Dictionary with race data or None if not found
        """
        if not self.conn:
            raise RuntimeError("Database not connected")

        cursor = self.conn.cursor()
        cursor.execute("SELECT * FROM races WHERE schedule_id = ?", (schedule_id,))
        row = cursor.fetchone()

        if row:
            return dict(row)
        return None

    def get_races_by_season(self, season_id: int) -> list[dict]:
        """
        Get all races for a season.

        Args:
            season_id: Season ID

        Returns:
            List of dictionaries with race data
        """
        if not self.conn:
            raise RuntimeError("Database not connected")

        cursor = self.conn.cursor()
        cursor.execute("SELECT * FROM races WHERE season_id = ? ORDER BY race_number", (season_id,))
        rows = cursor.fetchall()

        return [dict(row) for row in rows]

    def is_race_complete(self, schedule_id: int) -> bool:
        """
        Check if a race is marked as complete.

        Args:
            schedule_id: Schedule ID

        Returns:
            True if race exists and is_complete=1, False otherwise
        """
        if not self.conn:
            raise RuntimeError("Database not connected")

        cursor = self.conn.cursor()
        cursor.execute("SELECT is_complete FROM races WHERE schedule_id = ?", (schedule_id,))
        row = cursor.fetchone()

        return bool(row and row[0]) if row else False

    def get_incomplete_races(self, season_id: int) -> list[dict]:
        """
        Get all incomplete races for a season.

        Args:
            season_id: Season ID

        Returns:
            List of dictionaries with race data for incomplete races
        """
        if not self.conn:
            raise RuntimeError("Database not connected")

        cursor = self.conn.cursor()
        cursor.execute(
            "SELECT * FROM races WHERE season_id = ? AND is_complete = 0 ORDER BY race_number",
            (season_id,),
        )
        rows = cursor.fetchall()

        return [dict(row) for row in rows]

    def upsert_team(self, team_id: int, league_id: int, data: dict) -> int:
        """
        Insert or update a team record.

        Args:
            team_id: Team ID
            league_id: League ID (foreign key)
            data: Dictionary with team fields

        Returns:
            The team_id of the inserted/updated record
        """
        if not self.conn:
            raise RuntimeError("Database not connected")

        cursor = self.conn.cursor()

        # Required fields
        name = data.get("name")
        scraped_at = data.get("scraped_at")

        if not name or not scraped_at:
            raise ValueError("name and scraped_at are required fields")

        # Optional fields
        url = data.get("url")
        driver_count = data.get("driver_count")

        cursor.execute(
            """
            INSERT INTO teams (
                team_id, league_id, name, driver_count, url, scraped_at, updated_at
            )
            VALUES (?, ?, ?, ?, ?, ?, CURRENT_TIMESTAMP)
            ON CONFLICT(team_id) DO UPDATE SET
                league_id = excluded.league_id,
                name = excluded.name,
                driver_count = excluded.driver_count,
                url = excluded.url,
                scraped_at = excluded.scraped_at,
                updated_at = CURRENT_TIMESTAMP
        """,
            (team_id, league_id, name, driver_count, url, scraped_at),
        )

        self.conn.commit()
        return team_id

    def get_team(self, team_id: int) -> dict | None:
        """
        Get a team by ID.

        Args:
            team_id: Team ID

        Returns:
            Dictionary with team data or None if not found
        """
        if not self.conn:
            raise RuntimeError("Database not connected")

        cursor = self.conn.cursor()
        cursor.execute("SELECT * FROM teams WHERE team_id = ?", (team_id,))
        row = cursor.fetchone()

        if row:
            return dict(row)
        return None

    def get_teams_by_league(self, league_id: int) -> list[dict]:
        """
        Get all teams for a league.

        Args:
            league_id: League ID

        Returns:
            List of dictionaries with team data
        """
        if not self.conn:
            raise RuntimeError("Database not connected")

        cursor = self.conn.cursor()
        cursor.execute("SELECT * FROM teams WHERE league_id = ? ORDER BY team_id", (league_id,))
        rows = cursor.fetchall()

        return [dict(row) for row in rows]

    def upsert_driver(self, driver_id: int, league_id: int, data: dict) -> int:
        """
        Insert or update a driver record.

        Args:
            driver_id: Driver ID
            league_id: League ID (foreign key)
            data: Dictionary with driver fields

        Returns:
            The driver_id of the inserted/updated record
        """
        if not self.conn:
            raise RuntimeError("Database not connected")

        cursor = self.conn.cursor()

        # Required fields
        name = data.get("name")
        url = data.get("url")
        scraped_at = data.get("scraped_at")

        if not name or not url or not scraped_at:
            raise ValueError("name, url, and scraped_at are required fields")

        # Optional fields
        team_id = data.get("team_id")
        first_name = data.get("first_name")
        last_name = data.get("last_name")
        car_numbers = data.get("car_numbers")
        primary_number = data.get("primary_number")
        club = data.get("club")
        club_id = data.get("club_id")
        irating = data.get("irating")
        safety_rating = data.get("safety_rating")
        license_class = data.get("license_class")

        cursor.execute(
            """
            INSERT INTO drivers (
                driver_id, league_id, team_id, name, first_name, last_name,
                car_numbers, primary_number, club, club_id, irating, safety_rating,
                license_class, url, scraped_at, updated_at
            )
            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, CURRENT_TIMESTAMP)
            ON CONFLICT(driver_id) DO UPDATE SET
                league_id = excluded.league_id,
                team_id = excluded.team_id,
                name = excluded.name,
                first_name = excluded.first_name,
                last_name = excluded.last_name,
                car_numbers = excluded.car_numbers,
                primary_number = excluded.primary_number,
                club = excluded.club,
                club_id = excluded.club_id,
                irating = excluded.irating,
                safety_rating = excluded.safety_rating,
                license_class = excluded.license_class,
                url = excluded.url,
                scraped_at = excluded.scraped_at,
                updated_at = CURRENT_TIMESTAMP
        """,
            (
                driver_id,
                league_id,
                team_id,
                name,
                first_name,
                last_name,
                car_numbers,
                primary_number,
                club,
                club_id,
                irating,
                safety_rating,
                license_class,
                url,
                scraped_at,
            ),
        )

        self.conn.commit()
        return driver_id

    def get_driver(self, driver_id: int) -> dict | None:
        """
        Get a driver by ID.

        Args:
            driver_id: Driver ID

        Returns:
            Dictionary with driver data or None if not found
        """
        if not self.conn:
            raise RuntimeError("Database not connected")

        cursor = self.conn.cursor()
        cursor.execute("SELECT * FROM drivers WHERE driver_id = ?", (driver_id,))
        row = cursor.fetchone()

        if row:
            return dict(row)
        return None

    def get_drivers_by_league(self, league_id: int) -> list[dict]:
        """
        Get all drivers for a league.

        Args:
            league_id: League ID

        Returns:
            List of dictionaries with driver data
        """
        if not self.conn:
            raise RuntimeError("Database not connected")

        cursor = self.conn.cursor()
        cursor.execute("SELECT * FROM drivers WHERE league_id = ? ORDER BY driver_id", (league_id,))
        rows = cursor.fetchall()

        return [dict(row) for row in rows]

    def find_driver_by_name(self, name: str, league_id: int | None = None) -> list[dict]:
        """
        Find drivers by name (fuzzy matching).

        Args:
            name: Driver name to search for
            league_id: Optional league ID to filter by

        Returns:
            List of dictionaries with driver data matching the name
        """
        if not self.conn:
            raise RuntimeError("Database not connected")

        cursor = self.conn.cursor()

        if league_id is not None:
            cursor.execute(
                "SELECT * FROM drivers WHERE name LIKE ? AND league_id = ? ORDER BY driver_id",
                (f"%{name}%", league_id),
            )
        else:
            cursor.execute(
                "SELECT * FROM drivers WHERE name LIKE ? ORDER BY driver_id", (f"%{name}%",)
            )

        rows = cursor.fetchall()
        return [dict(row) for row in rows]

    def upsert_race_result(self, race_id: int, driver_id: int, data: dict) -> int:
        """
        Insert or update a race result record.

        Args:
            race_id: Race ID (foreign key)
            driver_id: Driver ID (foreign key)
            data: Dictionary with race result fields

        Returns:
            The result_id of the inserted/updated record
        """
        if not self.conn:
            raise RuntimeError("Database not connected")

        cursor = self.conn.cursor()

        # All fields are optional for race results
        team = data.get("team")
        finish_position = data.get("finish_position")
        starting_position = data.get("starting_position")
        car_number = data.get("car_number")
        qualifying_time = data.get("qualifying_time")
        fastest_lap = data.get("fastest_lap")
        fastest_lap_number = data.get("fastest_lap_number")
        average_lap = data.get("average_lap")
        interval = data.get("interval")
        laps_completed = data.get("laps_completed")
        laps_led = data.get("laps_led")
        incident_points = data.get("incident_points")
        race_points = data.get("race_points")
        bonus_points = data.get("bonus_points")
        penalty_points = data.get("penalty_points")
        total_points = data.get("total_points")
        fast_laps = data.get("fast_laps")
        quality_passes = data.get("quality_passes")
        closing_passes = data.get("closing_passes")
        total_passes = data.get("total_passes")
        average_running_position = data.get("average_running_position")
        irating = data.get("irating")
        status = data.get("status")
        car_id = data.get("car_id")

        cursor.execute(
            """
            INSERT INTO race_results (
                race_id, driver_id, team, finish_position, starting_position, car_number,
                qualifying_time, fastest_lap, fastest_lap_number, average_lap, interval,
                laps_completed, laps_led, incident_points, race_points, bonus_points,
                penalty_points, total_points,
                fast_laps, quality_passes, closing_passes, total_passes, average_running_position,
                irating, status, car_id, updated_at
            )
            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, CURRENT_TIMESTAMP)
            ON CONFLICT(race_id, driver_id) DO UPDATE SET
                team = excluded.team,
                finish_position = excluded.finish_position,
                starting_position = excluded.starting_position,
                car_number = excluded.car_number,
                qualifying_time = excluded.qualifying_time,
                fastest_lap = excluded.fastest_lap,
                fastest_lap_number = excluded.fastest_lap_number,
                average_lap = excluded.average_lap,
                interval = excluded.interval,
                laps_completed = excluded.laps_completed,
                laps_led = excluded.laps_led,
                incident_points = excluded.incident_points,
                race_points = excluded.race_points,
                bonus_points = excluded.bonus_points,
                penalty_points = excluded.penalty_points,
                total_points = excluded.total_points,
                fast_laps = excluded.fast_laps,
                quality_passes = excluded.quality_passes,
                closing_passes = excluded.closing_passes,
                total_passes = excluded.total_passes,
                average_running_position = excluded.average_running_position,
                irating = excluded.irating,
                status = excluded.status,
                car_id = excluded.car_id,
                updated_at = CURRENT_TIMESTAMP
        """,
            (
                race_id,
                driver_id,
                team,
                finish_position,
                starting_position,
                car_number,
                qualifying_time,
                fastest_lap,
                fastest_lap_number,
                average_lap,
                interval,
                laps_completed,
                laps_led,
                incident_points,
                race_points,
                bonus_points,
                penalty_points,
                total_points,
                fast_laps,
                quality_passes,
                closing_passes,
                total_passes,
                average_running_position,
                irating,
                status,
                car_id,
            ),
        )

        self.conn.commit()
        return cursor.lastrowid

    def get_race_results(self, race_id: int) -> list[dict]:
        """
        Get all race results for a race.

        Args:
            race_id: Race ID

        Returns:
            List of dictionaries with race result data, ordered by finish position
        """
        if not self.conn:
            raise RuntimeError("Database not connected")

        cursor = self.conn.cursor()
        cursor.execute(
            "SELECT * FROM race_results WHERE race_id = ? ORDER BY finish_position", (race_id,)
        )
        rows = cursor.fetchall()

        return [dict(row) for row in rows]

    def get_driver_results(self, driver_id: int) -> list[dict]:
        """
        Get all race results for a driver.

        Args:
            driver_id: Driver ID

        Returns:
            List of dictionaries with race result data
        """
        if not self.conn:
            raise RuntimeError("Database not connected")

        cursor = self.conn.cursor()
        cursor.execute(
            "SELECT * FROM race_results WHERE driver_id = ? ORDER BY result_id", (driver_id,)
        )
        rows = cursor.fetchall()

        return [dict(row) for row in rows]

    def should_scrape(
        self, entity_type: str, entity_id: int, validity_hours: int | None = None
    ) -> tuple[bool, str]:
        """
        Determine if an entity should be scraped based on cache validity.

        Args:
            entity_type: Type of entity (league, series, season, race)
            entity_id: Entity ID
            validity_hours: Hours until cache is stale (None = always fresh)

        Returns:
            Tuple of (should_scrape: bool, reason: str)
        """
        if not self.conn:
            raise RuntimeError("Database not connected")

        # Map entity types to tables and ID columns
        table_map = {
            "league": ("leagues", "league_id"),
            "series": ("series", "series_id"),
            "season": ("seasons", "season_id"),
            "race": ("races", "schedule_id"),
            "team": ("teams", "team_id"),
            "driver": ("drivers", "driver_id"),
        }

        if entity_type not in table_map:
            raise ValueError(f"Unknown entity type: {entity_type}")

        table, id_col = table_map[entity_type]
        cursor = self.conn.cursor()

        # Tables that have status column
        has_status = entity_type in ["race", "season"]

        # Check if entity exists
        if has_status:
            cursor.execute(
                f"SELECT scraped_at, status FROM {table} WHERE {id_col} = ?", (entity_id,)
            )
        else:
            cursor.execute(f"SELECT scraped_at FROM {table} WHERE {id_col} = ?", (entity_id,))

        row = cursor.fetchone()

        if not row:
            return True, "not_in_cache"

        scraped_at = row[0]
        status = row[1] if has_status and len(row) > 1 else None

        # If no validity period, trust cache
        if validity_hours is None:
            return False, "cache_valid_indefinitely"

        # Check if cache is stale based on time
        from datetime import datetime

        try:
            scraped_time = datetime.fromisoformat(scraped_at)
            age_hours = (datetime.now() - scraped_time).total_seconds() / 3600

            if age_hours > validity_hours:
                return True, f"cache_stale ({age_hours:.1f}h > {validity_hours}h)"
        except (ValueError, TypeError):
            return True, "invalid_timestamp"

        # For races/seasons, check status to determine if data might change
        if entity_type in ["race", "season"] and status:
            if status in ["upcoming", "ongoing", "active"]:
                # Active/upcoming races/seasons should be refreshed more often
                return True, f"status_{status}_needs_refresh"
            elif status == "completed":
                # Completed races/seasons are stable
                return False, "status_completed_stable"

        return False, f"cache_fresh ({age_hours:.1f}h < {validity_hours}h)"

    def is_url_cached(self, url: str, entity_type: str, max_age_days: int | None = None) -> bool:
        """Check if a URL has been cached and is still fresh.

        This method determines if a given URL has been scraped and stored in the
        database, and whether it's still within the acceptable age limit.

        Args:
            url: The URL to check
            entity_type: Type of entity (league, series, season, race, driver, team)
            max_age_days: Maximum age in days for cache to be valid.
                         If None, cache is always valid (indefinite).

        Returns:
            bool: True if URL is cached and fresh, False otherwise

        Raises:
            ValueError: If entity_type is not valid
            RuntimeError: If database is not connected

        Examples:
            >>> db.is_url_cached("http://test.com/league/1558", "league", max_age_days=7)
            True  # URL exists and is less than 7 days old

            >>> db.is_url_cached("http://test.com/league/9999", "league", max_age_days=1)
            False  # URL doesn't exist in database

            >>> db.is_url_cached("http://test.com/league/1558", "league", max_age_days=None)
            True  # URL exists, age doesn't matter (indefinite cache)
        """
        if not self.conn:
            raise RuntimeError("Database not connected. Use 'with Database()' or call connect()")

        # Validate entity_type
        valid_types = ["league", "series", "season", "race", "driver", "team"]
        if entity_type not in valid_types:
            raise ValueError(
                f"Invalid entity_type '{entity_type}'. Must be one of: {', '.join(valid_types)}"
            )

        # Map entity type to table and URL column
        table_map = {
            "league": "leagues",
            "series": "series",
            "season": "seasons",
            "race": "races",
            "driver": "drivers",
            "team": "teams",
        }

        table = table_map[entity_type]

        # Query for URL and scraped_at timestamp
        cursor = self.conn.execute(f"SELECT scraped_at FROM {table} WHERE url = ?", (url,))
        row = cursor.fetchone()

        # URL not found in cache
        if not row:
            return False

        # If max_age_days is None, cache is indefinite (always fresh)
        if max_age_days is None:
            return True

        # Check if cache is still fresh
        scraped_at = row[0]
        if not scraped_at:
            # No timestamp, consider it stale
            return False

        try:
            from datetime import datetime

            scraped_time = datetime.fromisoformat(scraped_at)
            age_days = (datetime.now() - scraped_time).total_seconds() / 86400  # seconds in a day

            # Cache is fresh if age < max_age_days
            return age_days < max_age_days
        except (ValueError, TypeError):
            # Invalid timestamp, consider it stale
            return False

    def log_scrape(
        self,
        entity_type: str,
        entity_url: str,
        status: str,
        entity_id: int | None = None,
        error_msg: str | None = None,
        duration_ms: int | None = None,
    ) -> int:
        """Log a scrape attempt to the scrape_log table.

        This method records all scraping activity for monitoring, debugging, and
        analytics purposes. It tracks successful scrapes, failures, and skipped URLs.

        Args:
            entity_type: Type of entity being scraped (league, series, season, race, driver, team)
            entity_url: The URL that was scraped (or attempted)
            status: Result of scrape attempt ('success', 'failed', or 'skipped')
            entity_id: Optional entity ID if known
            error_msg: Optional error message if status is 'failed'
            duration_ms: Optional duration in milliseconds

        Returns:
            int: The log_id of the inserted log record

        Raises:
            ValueError: If required fields are missing or invalid
            RuntimeError: If database is not connected

        Examples:
            >>> # Log successful scrape
            >>> db.log_scrape(
            ...     entity_type="league",
            ...     entity_url="http://simracerhub.com/league/1558",
            ...     status="success",
            ...     entity_id=1558,
            ...     duration_ms=1500
            ... )
            1

            >>> # Log failed scrape with error
            >>> db.log_scrape(
            ...     entity_type="series",
            ...     entity_url="http://simracerhub.com/series/3714",
            ...     status="failed",
            ...     entity_id=3714,
            ...     error_msg="Connection timeout",
            ...     duration_ms=30000
            ... )
            2

            >>> # Log skipped scrape (cache hit)
            >>> db.log_scrape(
            ...     entity_type="race",
            ...     entity_url="http://simracerhub.com/race/67890",
            ...     status="skipped",
            ...     entity_id=67890
            ... )
            3
        """
        if not self.conn:
            raise RuntimeError("Database not connected. Use 'with Database()' or call connect()")

        # Validate required fields
        if not entity_url or not status:
            raise ValueError("entity_url and status are required fields")

        # Validate entity_type
        valid_entity_types = ["league", "series", "season", "race", "driver", "team"]
        if entity_type not in valid_entity_types:
            raise ValueError(
                f"Invalid entity_type '{entity_type}'. Must be one of: {', '.join(valid_entity_types)}"
            )

        # Validate status
        valid_statuses = ["success", "failed", "skipped"]
        if status not in valid_statuses:
            raise ValueError(
                f"Invalid status '{status}'. Must be one of: {', '.join(valid_statuses)}"
            )

        # Insert log record
        cursor = self.conn.cursor()
        cursor.execute(
            """
            INSERT INTO scrape_log (entity_type, entity_id, entity_url, status, error_message, duration_ms)
            VALUES (?, ?, ?, ?, ?, ?)
            """,
            (entity_type, entity_id, entity_url, status, error_msg, duration_ms),
        )
        self.conn.commit()

        return cursor.lastrowid
