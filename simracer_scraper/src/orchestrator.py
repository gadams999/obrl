"""Orchestrator for hierarchical scraping of SimRacerHub data.

This module coordinates the scraping of league hierarchies using the extractor
classes and database storage with smart caching.
"""

import logging
from typing import Any

from .database import Database
from .extractors import (
    DriverExtractor,
    LeagueExtractor,
    RaceExtractor,
    SeasonExtractor,
    SeriesExtractor,
)
from .schema_validator import SchemaValidator
from .utils.browser_manager import BrowserManager

# Setup logger
logger = logging.getLogger(__name__)


class Orchestrator:
    """Orchestrates hierarchical scraping with depth control and caching.

    The orchestrator manages the workflow of scraping league data by:
    - Coordinating extractors (League → Series → Season → Race)
    - Using database caching to avoid redundant scrapes
    - Tracking progress and errors
    - Respecting depth limits and rate limits
    """

    def __init__(
        self,
        database: Database,
        validator: SchemaValidator,
        rate_limit_seconds: float = 2.0,
        rate_limit_range: tuple[float, float] | None = (2.0, 4.0),
        max_retries: int = 3,
        timeout: int = 30,
        user_agent: str | None = None,
    ):
        """Initialize orchestrator with dependencies.

        Args:
            database: Database instance for storage and caching
            validator: SchemaValidator for detecting site changes
            rate_limit_seconds: Fixed delay between requests (default: 2.0)
                Ignored if rate_limit_range is provided
            rate_limit_range: Tuple of (min, max) seconds for randomized delay
                Default: (2.0, 4.0) for human-like browsing
                Set to None to use fixed rate_limit_seconds
            max_retries: Maximum retry attempts for failed requests (default: 3)
            timeout: Request timeout in seconds (default: 30)
            user_agent: Custom User-Agent string for HTTP requests (default: None)
        """
        self.db = database
        self.validator = validator
        self.rate_limit_seconds = rate_limit_seconds
        self.rate_limit_range = rate_limit_range
        self.max_retries = max_retries
        self.timeout = timeout
        self.user_agent = user_agent

        # CRITICAL: Create ONE shared browser manager for ALL extractors
        # This ensures:
        # 1. No async loop conflicts (one browser across all extractors)
        # 2. Shared rate limiting (delays enforced across ALL requests, not per-extractor)
        # 3. Sequential processing (one request at a time, never concurrent)
        if rate_limit_range:
            self._browser_manager = BrowserManager(rate_limit_range=rate_limit_range)
        else:
            self._browser_manager = BrowserManager(
                rate_limit_range=(rate_limit_seconds, rate_limit_seconds)
            )

        # Initialize extractors with rate limiting configuration
        extractor_kwargs = {
            "max_retries": max_retries,
            "timeout": timeout,
            "browser_manager": self._browser_manager,  # CRITICAL: Share browser manager
            "user_agent": user_agent,
        }

        # Use randomized or fixed rate limiting (ignored when browser_manager is provided)
        if rate_limit_range:
            extractor_kwargs["rate_limit_range"] = rate_limit_range
        else:
            extractor_kwargs["rate_limit_seconds"] = rate_limit_seconds

        # League and Series use static HTML (fast) - no JavaScript needed
        self.league_extractor = LeagueExtractor(**extractor_kwargs | {"render_js": False})
        self.series_extractor = SeriesExtractor(**extractor_kwargs | {"render_js": False})

        # Season and Race require JavaScript rendering (slow but necessary)
        self.season_extractor = SeasonExtractor(**extractor_kwargs | {"render_js": True})
        self.race_extractor = RaceExtractor(**extractor_kwargs | {"render_js": True})

        # Driver uses static HTML (fast) - no JavaScript needed
        self.driver_extractor = DriverExtractor(**extractor_kwargs | {"render_js": False})

        # Progress tracking
        self.progress = {
            "leagues_scraped": 0,
            "series_scraped": 0,
            "seasons_scraped": 0,
            "races_scraped": 0,
            "errors": [],
            "skipped_cached": 0,
        }

    def __enter__(self):
        """Enter context manager."""
        return self

    def __exit__(self, exc_type, exc_val, exc_tb):
        """Exit context manager - cleanup browser resources."""
        # Close shared browser
        if self._browser_manager:
            # Pass interrupted=True if exiting due to KeyboardInterrupt
            interrupted = exc_type is KeyboardInterrupt
            self._browser_manager.close(interrupted=interrupted)
        # Don't suppress exceptions
        return False

    def reset_progress(self) -> None:
        """Reset progress tracking."""
        self.progress = {
            "leagues_scraped": 0,
            "series_scraped": 0,
            "seasons_scraped": 0,
            "races_scraped": 0,
            "errors": [],
            "skipped_cached": 0,
        }

    def get_progress(self) -> dict[str, Any]:
        """Get current progress statistics.

        Returns:
            Dictionary with progress counts and errors
        """
        return self.progress.copy()

    def scrape_league(
        self,
        league_url: str,
        depth: str = "league",
        filters: dict[str, Any] | None = None,
        cache_max_age_days: int | None = 7,
        force: bool = False,
    ) -> dict[str, Any]:
        """Scrape a league with optional depth control and filters.

        Args:
            league_url: URL like "league_series.php?league_id=1558"
            depth: How deep to scrape:
                - "league": Just league metadata
                - "series": League + series metadata
                - "season": League + series + seasons
                - "race": Full hierarchy including race results
            filters: Optional filtering dict:
                {
                    "series_ids": [3714, 3713],  # Only scrape these series
                    "season_year": 2025,  # Only scrape seasons from this year
                    "season_limit": 1,  # Max seasons per series
                }
            cache_max_age_days: Days before cache expires (None = never expire)
            force: Force re-scrape all levels, ignore completion flags

        Returns:
            Statistics dictionary:
            {
                "leagues_scraped": 1,
                "series_scraped": 4,
                "seasons_scraped": 4,
                "races_scraped": 40,
                "errors": [],
                "skipped_cached": 2,
            }

        Examples:
            >>> # Test with one series only
            >>> result = orch.scrape_league(
            ...     url,
            ...     depth="season",
            ...     filters={"series_ids": [3714]}
            ... )

            >>> # Full scrape with filtering
            >>> result = orch.scrape_league(
            ...     url,
            ...     depth="race",
            ...     filters={"season_limit": 1}  # Only 1 season per series
            ... )
        """
        import time as time_module

        start_time = time_module.time()
        filters = filters or {}

        try:
            # Always fetch league page to discover current series
            # League pages are small, fast, and contain critical series discovery
            # We never cache-skip at this level to ensure we always have the latest series list

            # Extract league data
            logger.info(f"🌐 FETCHING: {league_url}")
            league_data = self.league_extractor.extract(league_url)
            metadata = league_data["metadata"]

            # Store league in database
            import datetime

            self.db.upsert_league(
                league_id=metadata["league_id"],
                data={
                    "name": metadata["name"],
                    "url": metadata["url"],
                    "description": metadata.get("description"),
                    "organizer": metadata.get("organizer"),
                    "scraped_at": datetime.datetime.now().isoformat(),
                },
            )

            self.progress["leagues_scraped"] += 1

            # Log successful scrape
            duration_ms = int((time_module.time() - start_time) * 1000)
            self.db.log_scrape("league", league_url, "success", duration_ms=duration_ms)

            # If depth allows, scrape child series
            if depth in ["series", "season", "race"]:
                series_urls = league_data["child_urls"]["series"]

                # Apply series filter if specified
                if "series_ids" in filters:
                    allowed_ids = set(filters["series_ids"])
                    series_urls = [s for s in series_urls if s["series_id"] in allowed_ids]

                # Store series names immediately from league JavaScript data
                # This ensures we capture the correct names before fetching series pages
                # NOTE: We set scraped_at to a very old date so cache checks know
                # we haven't actually scraped the series page yet
                for series_info in series_urls:
                    series_data = {
                        "name": series_info.get("name", "Unknown Series"),
                        "url": series_info["url"],
                        "scraped_at": "1970-01-01T00:00:00",  # Epoch - forces re-scrape
                    }

                    # Add optional metadata from league page
                    if "description" in series_info:
                        series_data["description"] = series_info["description"]
                    if "created_date" in series_info:
                        series_data["created_date"] = series_info["created_date"]
                    if "num_seasons" in series_info:
                        series_data["num_seasons"] = series_info["num_seasons"]

                    self.db.upsert_series(
                        series_id=series_info["series_id"],
                        league_id=metadata["league_id"],
                        data=series_data,
                    )

                # Scrape each series
                for series_info in series_urls:
                    self.scrape_series(
                        series_url=series_info["url"],
                        league_id=metadata["league_id"],
                        depth=depth,
                        filters=filters,
                        cache_max_age_days=cache_max_age_days,
                        force=force,
                    )

            return self.get_progress()

        except Exception as e:
            # Log error
            self.progress["errors"].append({"entity": "league", "url": league_url, "error": str(e)})
            duration_ms = int((time_module.time() - start_time) * 1000)
            self.db.log_scrape(
                "league", league_url, "failed", error_msg=str(e), duration_ms=duration_ms
            )
            raise

    def scrape_series(
        self,
        series_url: str,
        league_id: int,
        depth: str,
        filters: dict[str, Any] | None = None,
        cache_max_age_days: int | None = 7,
        force: bool = False,
    ) -> None:
        """Scrape a series with optional depth control.

        Args:
            series_url: URL like "series_seasons.php?series_id=3714"
            league_id: Parent league ID
            depth: Current depth setting from scrape_league
            filters: Filter dictionary from scrape_league
            cache_max_age_days: Days before cache expires
            force: Force re-scrape even if cached
        """
        import time as time_module

        start_time = time_module.time()
        filters = filters or {}

        try:
            # Always fetch series page to discover current seasons
            # Series pages are lightweight and contain critical season discovery
            # We don't cache-skip at this level to ensure we always have latest season list

            # Extract series data
            logger.info(f"🌐 FETCHING: {series_url}")
            series_data = self.series_extractor.extract(series_url)
            metadata = series_data["metadata"]

            # Store series in database
            import datetime

            # Check if series already exists to preserve metadata from league JavaScript
            existing_series = self.db.get_series(metadata["series_id"])
            series_name = metadata["name"]
            if existing_series and existing_series.get("name"):
                # Preserve the name from league JavaScript (more accurate)
                series_name = existing_series["name"]

            # Build database data dict, preserving existing values for fields not in series page
            db_data = {
                "name": series_name,
                "url": metadata["url"],
                "scraped_at": datetime.datetime.now().isoformat(),
            }

            # Only update description if we have a value (preserve existing if not)
            if metadata.get("description"):
                db_data["description"] = metadata["description"]
            elif existing_series and existing_series.get("description"):
                db_data["description"] = existing_series["description"]

            # Only update created_date if we have a value (preserve existing if not)
            if metadata.get("created_date"):
                db_data["created_date"] = metadata["created_date"]
            elif existing_series and existing_series.get("created_date"):
                db_data["created_date"] = existing_series["created_date"]

            # Only update num_seasons if we have a value (preserve existing if not)
            # Note: metadata might have 'season_count' which maps to 'num_seasons'
            if metadata.get("season_count"):
                db_data["num_seasons"] = metadata["season_count"]
            elif metadata.get("num_seasons"):
                db_data["num_seasons"] = metadata["num_seasons"]
            elif existing_series and existing_series.get("num_seasons"):
                db_data["num_seasons"] = existing_series["num_seasons"]

            self.db.upsert_series(
                series_id=metadata["series_id"],
                league_id=league_id,
                data=db_data,
            )

            self.progress["series_scraped"] += 1

            # Log successful scrape
            duration_ms = int((time_module.time() - start_time) * 1000)
            self.db.log_scrape("series", series_url, "success", duration_ms=duration_ms)

            # If depth allows, scrape child seasons
            if depth in ["season", "race"]:
                seasons = series_data["child_urls"]["seasons"]

                # Apply season filters
                if "season_year" in filters:
                    # Filter by year in season name
                    year_str = str(filters["season_year"])
                    seasons = [s for s in seasons if year_str in s.get("name", "")]

                if "season_limit" in filters:
                    # Limit number of seasons
                    seasons = seasons[: filters["season_limit"]]

                # Store season names immediately from series JavaScript data
                # This ensures we capture the correct names before fetching season pages
                # NOTE: We set scraped_at to a very old date so cache checks know
                # we haven't actually scraped the season page yet
                for season_info in seasons:
                    self.db.upsert_season(
                        season_id=season_info.get("season_id", 0),
                        series_id=metadata["series_id"],
                        data={
                            "name": season_info.get("name", "Unknown Season"),
                            "url": season_info["url"],
                            "scraped_at": "1970-01-01T00:00:00",  # Epoch - forces re-scrape
                        },
                    )

                # Scrape each season
                for season_info in seasons:
                    self.scrape_season(
                        season_url=season_info["url"],
                        season_id=season_info.get("season_id", 0),
                        series_id=metadata["series_id"],
                        depth=depth,
                        filters=filters,
                        cache_max_age_days=cache_max_age_days,
                        force=force,
                    )

        except Exception as e:
            self.progress["errors"].append({"entity": "series", "url": series_url, "error": str(e)})
            duration_ms = int((time_module.time() - start_time) * 1000)
            self.db.log_scrape(
                "series", series_url, "failed", error_msg=str(e), duration_ms=duration_ms
            )
            # Don't re-raise, continue with other series

    def scrape_season(
        self,
        season_url: str,
        season_id: int,
        series_id: int,
        depth: str,
        filters: dict[str, Any] | None = None,
        cache_max_age_days: int | None = 7,
        force: bool = False,
    ) -> None:
        """Scrape a season with optional depth control.

        Args:
            season_url: URL like "season_race.php?series_id=3714&season_id=17424"
            season_id: Season ID (from parent series data)
            series_id: Parent series ID
            depth: Current depth setting
            filters: Filter dictionary
            cache_max_age_days: Days before cache expires
            force: Force re-scrape even if cached
        """
        import time as time_module

        start_time = time_module.time()
        filters = filters or {}

        try:
            # Check cache
            if cache_max_age_days is not None:
                is_cached = self.db.is_url_cached(season_url, "season", cache_max_age_days)
                if is_cached:
                    logger.info(f"⚡ CACHED (skipped): {season_url}")
                    self.progress["skipped_cached"] += 1
                    return

            # Extract season data
            logger.info(f"🌐 FETCHING: {season_url}")
            season_data = self.season_extractor.extract(season_url)
            metadata = season_data["metadata"]

            # Store season in database
            import datetime

            # Check if season already has a name (from series JavaScript)
            existing_season = self.db.get_season(season_id)
            season_name = metadata["name"]
            if existing_season and existing_season.get("name"):
                # Preserve the name from series JavaScript (more accurate)
                season_name = existing_season["name"]

            season_data_dict = {
                "name": season_name,
                "url": metadata["url"],
                "scraped_at": datetime.datetime.now().isoformat(),
            }

            # Add description if available
            if "description" in metadata:
                season_data_dict["description"] = metadata["description"]

            self.db.upsert_season(
                season_id=season_id,
                series_id=series_id,
                data=season_data_dict,
            )

            self.progress["seasons_scraped"] += 1

            # Log successful scrape
            duration_ms = int((time_module.time() - start_time) * 1000)
            self.db.log_scrape("season", season_url, "success", duration_ms=duration_ms)

            # If depth allows, scrape races
            if depth == "race":
                races = season_data["child_urls"]["races"]

                # Apply race filters if needed (future enhancement)
                # if "race_limit" in filters:
                #     races = races[:filters["race_limit"]]

                # CRITICAL: Scrape each race SEQUENTIALLY (one at a time, never concurrent)
                # This ensures respectful rate limiting - each race waits for the previous
                # one to complete before starting. Combined with shared BrowserManager,
                # this guarantees proper delays between ALL requests.
                for race_info in races:
                    self.scrape_race(
                        race_url=race_info["url"],
                        season_id=season_id,
                        schedule_id=race_info["schedule_id"],
                        race_number=race_info.get("race_number", 0),
                        has_results=race_info.get("has_results", True),
                        cache_max_age_days=cache_max_age_days,
                        force=force,
                    )

        except Exception as e:
            self.progress["errors"].append({"entity": "season", "url": season_url, "error": str(e)})
            duration_ms = int((time_module.time() - start_time) * 1000)
            self.db.log_scrape(
                "season", season_url, "failed", error_msg=str(e), duration_ms=duration_ms
            )
            # Don't re-raise, continue with other seasons

    def scrape_race(
        self,
        race_url: str,
        season_id: int,
        schedule_id: int,
        race_number: int = 0,
        has_results: bool = True,
        cache_max_age_days: int | None = 7,
        force: bool = False,
    ) -> None:
        """Scrape a race and its results.

        Args:
            race_url: URL like "season_race.php?schedule_id=324462"
            season_id: Season ID (foreign key for races table)
            schedule_id: Schedule ID (unique identifier from SimRacerHub)
            race_number: Race number from season schedule (e.g., 1, 2, 3...)
            has_results: Whether this race has results available (from season extractor)
            cache_max_age_days: Days before cache expires
            force: Force re-scrape even if race is marked complete
        """
        import datetime
        import time as time_module

        start_time = time_module.time()

        try:
            # If race has no results URL, store metadata only and skip scraping
            if not has_results:
                logger.info(f"⏭️  SKIPPING (no results): {race_url}")
                self.db.upsert_race(
                    schedule_id=schedule_id,
                    season_id=season_id,
                    data={
                        "url": race_url,
                        "is_complete": False,
                        "scraped_at": datetime.datetime.now().isoformat(),
                        "race_number": race_number,
                    },
                )
                return

            # Check if race is already complete (unless --force)
            if not force and self.db.is_race_complete(schedule_id):
                logger.info(f"✅ COMPLETE (skipped): {race_url}")
                self.progress["skipped_cached"] += 1
                return

            # Standard cache check (for recent scrapes)
            if not force and cache_max_age_days is not None:
                is_cached = self.db.is_url_cached(race_url, "race", cache_max_age_days)
                if is_cached:
                    logger.info(f"⚡ CACHED (skipped): {race_url}")
                    self.progress["skipped_cached"] += 1
                    return

            # Extract race data
            logger.info(f"🌐 FETCHING: {race_url}")
            race_data = self.race_extractor.extract(race_url)
            metadata = race_data["metadata"]
            results = race_data.get("results", [])
            schedule = race_data.get("schedule")

            # Build race data from schedule object + HTML metadata
            race_dict = self._build_race_data(metadata, schedule, race_number, len(results))

            # Store race in database with completion flag
            race_id = self.db.upsert_race(
                schedule_id=metadata["schedule_id"],
                season_id=season_id,
                data=race_dict,
            )

            self.progress["races_scraped"] += 1

            # Store race results
            for result in results:
                self._store_race_result(race_id, result, season_id)

            # Log successful scrape
            duration_ms = int((time_module.time() - start_time) * 1000)
            self.db.log_scrape("race", race_url, "success", duration_ms=duration_ms)

        except Exception as e:
            self.progress["errors"].append({"entity": "race", "url": race_url, "error": str(e)})
            duration_ms = int((time_module.time() - start_time) * 1000)
            self.db.log_scrape(
                "race", race_url, "failed", error_msg=str(e), duration_ms=duration_ms
            )
            # Don't re-raise, continue with other races

    def _store_race_result(self, race_id: int, result: dict, season_id: int) -> None:
        """Store a single race result in the database.

        Args:
            race_id: Internal race ID (from races table)
            result: Result dictionary from RaceExtractor
            season_id: Season ID for resolving league context

        Note:
            If driver_id is not in the result (no link in HTML), the result
            will be skipped. To handle this, we'd need to implement fuzzy
            name matching against existing drivers.
        """
        import datetime

        # Get driver_id from result
        driver_id = result.get("driver_id")
        if not driver_id:
            # Driver link not found - skip for now
            # TODO: Implement fuzzy name matching with find_driver_by_name()
            return

        driver_name = result.get("driver_name", "Unknown Driver")

        # Get league_id from season
        # Query database to get series_id from season, then league_id from series
        season = self.db.get_season(season_id)
        if not season:
            # Can't find season - skip result
            return

        series_id = season["series_id"]
        series = self.db.get_series(series_id)
        if not series:
            # Can't find series - skip result
            return

        league_id = series["league_id"]

        # Parse driver name into first and last name
        first_name, last_name = self._parse_driver_name(driver_name)

        # Ensure driver exists in database
        try:
            self.db.upsert_driver(
                driver_id=driver_id,
                league_id=league_id,
                data={
                    "name": driver_name,
                    "first_name": first_name,
                    "last_name": last_name,
                    "url": f"https://www.simracerhub.com/driver_stats.php?driver_id={driver_id}",
                    "scraped_at": datetime.datetime.now().isoformat(),
                },
            )
        except Exception as e:
            # Driver upsert failed - skip this result
            # Log the error for debugging
            print(f"Warning: Failed to upsert driver {driver_id}: {e}")
            return

        # Map result fields to database schema
        # RaceExtractor provides fields with proper names that match database schema
        result_data = {
            "team": result.get("team"),
            "finish_position": result.get("finish_position"),
            "starting_position": result.get("starting_position"),
            "car_number": result.get("car_number"),
            "qualifying_time": result.get("qualifying_time"),
            "fastest_lap": result.get("fastest_lap"),
            "fastest_lap_number": result.get("fastest_lap_number"),
            "average_lap": result.get("average_lap"),
            "interval": result.get("interval"),
            "laps_completed": result.get("laps_completed"),
            "laps_led": result.get("laps_led"),
            "incident_points": result.get("incident_points"),
            "race_points": result.get("race_points"),
            "bonus_points": result.get("bonus_points"),
            "penalty_points": result.get("penalty_points"),
            "total_points": result.get("total_points"),
            "fast_laps": result.get("fast_laps"),
            "quality_passes": result.get("quality_passes"),
            "closing_passes": result.get("closing_passes"),
            "total_passes": result.get("total_passes"),
            "average_running_position": result.get("average_running_position"),
            "irating": result.get("irating"),
            "status": result.get("status"),
            "car_id": result.get("car_id"),
        }

        # Store race result
        try:
            self.db.upsert_race_result(
                race_id=race_id,
                driver_id=driver_id,
                data=result_data,
            )
        except Exception:
            # Failed to store result - continue with others
            pass

    def _parse_int(self, value: str | int | None) -> int | None:
        """Safely parse a value to int.

        Args:
            value: Value to parse

        Returns:
            Parsed int or None if parsing fails
        """
        if value is None:
            return None
        if isinstance(value, int):
            return value
        try:
            return int(value)
        except (ValueError, TypeError):
            return None

    def _parse_float(self, value: str | float | None) -> float | None:
        """Safely parse a value to float.

        Args:
            value: Value to parse

        Returns:
            Parsed float or None if parsing fails
        """
        if value is None:
            return None
        if isinstance(value, float):
            return value
        try:
            return float(value)
        except (ValueError, TypeError):
            return None

    def _yn_to_bool(self, value: str | None) -> bool | None:
        """Convert Y/N string to boolean.

        Args:
            value: "Y", "N", or None

        Returns:
            True for "Y", False for "N", None otherwise
        """
        if value == "Y":
            return True
        elif value == "N":
            return False
        return None

    def _parse_driver_name(self, full_name: str | None) -> tuple[str | None, str | None]:
        """Parse full driver name into first and last name.

        Handles common name formats:
        - "LastName, FirstName" -> ("FirstName", "LastName")
        - "LastName, FirstName MiddleName" -> ("FirstName MiddleName", "LastName")
        - "FirstName LastName" -> ("FirstName", "LastName")
        - "FirstName" -> ("FirstName", None)
        - None or empty -> (None, None)

        Args:
            full_name: Full driver name string

        Returns:
            Tuple of (first_name, last_name)
        """
        if not full_name or not full_name.strip():
            return (None, None)

        full_name = full_name.strip()

        # Check if name is in "LastName, FirstName" format
        if "," in full_name:
            parts = full_name.split(",", 1)  # Split on first comma only
            if len(parts) == 2:
                last_name = parts[0].strip()
                first_name = parts[1].strip()
                # Handle case where there's nothing after the comma
                if not first_name:
                    return (None, last_name) if last_name else (None, None)
                if not last_name:
                    return (first_name, None) if first_name else (None, None)
                return (first_name, last_name)

        # No comma, assume "FirstName LastName" format
        parts = full_name.split()

        if len(parts) == 0:
            return (None, None)
        elif len(parts) == 1:
            # Only one name provided
            return (parts[0], None)
        else:
            # First word is first name, rest is last name
            first_name = parts[0]
            last_name = " ".join(parts[1:])
            return (first_name, last_name)

    def _build_race_data(
        self,
        metadata: dict,
        schedule: dict | None,
        race_number: int,
        num_drivers: int,
    ) -> dict:
        """Build race data dict from schedule object and HTML metadata.

        Uses schedule object fields when available, falls back to HTML metadata.

        Args:
            metadata: HTML metadata from RaceExtractor
            schedule: Schedule object from ReactDOM (or None)
            race_number: Race number from season
            num_drivers: Count of drivers in results

        Returns:
            Dictionary ready for upsert_race()
        """
        import datetime

        race_data = {
            "url": metadata["url"],
            "race_number": race_number,
            "num_drivers": num_drivers,
            "is_complete": True,
            "scraped_at": datetime.datetime.now().isoformat(),
        }

        if schedule:
            # Use schedule object for primary data
            race_data.update(
                {
                    # Basic info
                    "event_name": schedule.get("event_name"),
                    "date": schedule.get("race_date"),
                    "race_time": schedule.get("race_time"),
                    "practice_time": schedule.get("pract_time"),
                    # Track info
                    "track_id": self._parse_int(schedule.get("track_id")),
                    "track_config_id": self._parse_int(schedule.get("track_config_id")),
                    "track_name": schedule.get("track_name"),
                    "track_type": schedule.get("track_config_name"),
                    "track_length": self._parse_float(schedule.get("track_length")),
                    "track_config_iracing_id": schedule.get("track_config_iracing_id"),
                    # Race config
                    "planned_laps": self._parse_int(schedule.get("planned_laps")),
                    "points_race": self._yn_to_bool(schedule.get("points_count")),
                    "off_week": self._yn_to_bool(schedule.get("off_week")),
                    "night_race": self._yn_to_bool(schedule.get("night")),
                    "playoff_race": self._yn_to_bool(schedule.get("chase")),
                    # Weather
                    "weather_type": schedule.get("weather_type"),
                    "cloud_conditions": schedule.get("weather_skies"),
                    "temperature_f": self._parse_int(schedule.get("weather_temp")),
                    "humidity_pct": self._parse_int(schedule.get("weather_rh")),
                    "fog_pct": self._parse_int(schedule.get("weather_fog")),
                    "weather_wind_speed": schedule.get("weather_wind"),
                    "weather_wind_dir": schedule.get("weather_winddir"),
                    "weather_wind_unit": schedule.get("weather_windunit"),
                }
            )
        else:
            # Fallback to HTML metadata
            race_data.update(
                {
                    "track_name": metadata.get("track_name"),
                    "track_type": metadata.get("track_type"),
                    "date": metadata.get("date"),
                    "weather_type": metadata.get("weather_type"),
                    "cloud_conditions": metadata.get("cloud_conditions"),
                    "temperature_f": metadata.get("temperature_f"),
                    "humidity_pct": metadata.get("humidity_pct"),
                    "fog_pct": metadata.get("fog_pct"),
                }
            )

        # Always use HTML metadata for race statistics (computed from results)
        race_data.update(
            {
                "race_duration_minutes": metadata.get("race_duration_minutes"),
                "total_laps": metadata.get("total_laps"),
                "leaders": metadata.get("leaders"),
                "lead_changes": metadata.get("lead_changes"),
                "cautions": metadata.get("cautions"),
                "caution_laps": metadata.get("caution_laps"),
            }
        )

        return race_data

    def refresh_driver_data(
        self, driver_id: int, cache_max_age_days: int = 7, force: bool = False
    ) -> None:
        """Refresh driver profile data from driver stats page.

        Fetches complete driver profile (irating, safety_rating, license_class)
        and updates the database. Respects caching unless force=True.

        Args:
            driver_id: Driver ID to refresh
            cache_max_age_days: Days before cache expires (default: 7)
            force: Force refresh even if recently scraped (default: False)

        Note:
            This only updates driver stats. The driver record must already exist
            in the database (created via race result scraping).
        """
        import datetime
        import time as time_module

        start_time = time_module.time()

        # Build driver URL
        driver_url = f"https://www.simracerhub.com/driver_stats.php?driver_id={driver_id}"

        try:
            # Check cache unless force refresh
            if not force:
                is_cached = self.db.is_url_cached(driver_url, "driver", cache_max_age_days)
                if is_cached:
                    logger.info(f"⚡ CACHED (skipped): {driver_url}")
                    self.progress["skipped_cached"] += 1
                    return

            # Extract driver data
            logger.info(f"🌐 FETCHING: {driver_url}")
            driver_data = self.driver_extractor.extract(driver_url)
            metadata = driver_data["metadata"]

            # Get existing driver to preserve league_id and other fields
            existing_driver = self.db.get_driver(driver_id)
            if not existing_driver:
                logger.warning(f"⚠️  Driver {driver_id} not found in database, skipping")
                return

            # Update driver stats in database (must include name as required field)
            self.db.upsert_driver(
                driver_id=driver_id,
                league_id=existing_driver["league_id"],
                data={
                    "name": existing_driver["name"],  # Required field from existing record
                    "url": driver_url,
                    "irating": metadata.get("irating"),
                    "safety_rating": metadata.get("safety_rating"),
                    "license_class": metadata.get("license_class"),
                    "scraped_at": datetime.datetime.now().isoformat(),
                },
            )

            logger.info(
                f"✅ Updated driver {driver_id}: "
                f"iRating={metadata.get('irating')}, "
                f"SR={metadata.get('safety_rating')}, "
                f"License={metadata.get('license_class')}"
            )

            # Log successful scrape
            duration_ms = int((time_module.time() - start_time) * 1000)
            self.db.log_scrape("driver", driver_url, "success", duration_ms=duration_ms)

        except Exception as e:
            self.progress["errors"].append(
                {"entity": "driver", "url": driver_url, "error": str(e)}
            )
            duration_ms = int((time_module.time() - start_time) * 1000)
            self.db.log_scrape(
                "driver", driver_url, "failed", error_msg=str(e), duration_ms=duration_ms
            )
            logger.error(f"❌ Failed to refresh driver {driver_id}: {e}")

    def refresh_all_drivers(
        self, cache_max_age_days: int = 7, force: bool = False, league_id: int | None = None
    ) -> None:
        """Refresh all drivers in the database.

        Iterates through all drivers and updates their profile data.
        Optionally filters by league_id.

        Args:
            cache_max_age_days: Days before cache expires (default: 7)
            force: Force refresh even if recently scraped (default: False)
            league_id: Only refresh drivers from this league (optional)

        Progress is tracked in self.progress["drivers_refreshed"].
        """
        # Add drivers_refreshed to progress if not present
        if "drivers_refreshed" not in self.progress:
            self.progress["drivers_refreshed"] = 0

        # Get all drivers (optionally filtered by league)
        if league_id:
            drivers = self.db.get_drivers_by_league(league_id)
            logger.info(f"🔄 Refreshing {len(drivers)} drivers from league {league_id}...")
        else:
            drivers = self.db.get_all_drivers()
            logger.info(f"🔄 Refreshing all {len(drivers)} drivers...")

        for i, driver in enumerate(drivers, 1):
            driver_id = driver["driver_id"]
            logger.info(f"[{i}/{len(drivers)}] Refreshing driver {driver_id}...")

            self.refresh_driver_data(
                driver_id=driver_id, cache_max_age_days=cache_max_age_days, force=force
            )

            self.progress["drivers_refreshed"] += 1

        logger.info(f"✅ Driver refresh complete: {self.progress['drivers_refreshed']} updated")
