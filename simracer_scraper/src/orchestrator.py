"""Orchestrator for hierarchical scraping of SimRacerHub data.

This module coordinates the scraping of league hierarchies using the extractor
classes and database storage with smart caching.
"""

import logging
from typing import Any

from .database import Database
from .extractors import LeagueExtractor, RaceExtractor, SeasonExtractor, SeriesExtractor
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
        """
        self.db = database
        self.validator = validator
        self.rate_limit_seconds = rate_limit_seconds
        self.rate_limit_range = rate_limit_range
        self.max_retries = max_retries
        self.timeout = timeout

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
            self._browser_manager.close()
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
            # Check cache
            if cache_max_age_days is not None:
                is_cached = self.db.is_url_cached(league_url, "league", cache_max_age_days)
                if is_cached:
                    logger.info(f"⚡ CACHED (skipped): {league_url}")
                    self.progress["skipped_cached"] += 1
                    self.db.log_scrape(
                        "league",
                        league_url,
                        "skipped",
                        error_msg="URL cached",
                        duration_ms=int((time_module.time() - start_time) * 1000),
                    )
                    return self.get_progress()

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
                for series_info in series_urls:
                    self.db.upsert_series(
                        series_id=series_info["series_id"],
                        league_id=metadata["league_id"],
                        data={
                            "name": series_info.get("name", "Unknown Series"),
                            "url": series_info["url"],
                            "scraped_at": datetime.datetime.now().isoformat(),
                        },
                    )

                # Scrape each series
                for series_info in series_urls:
                    self.scrape_series(
                        series_url=series_info["url"],
                        league_id=metadata["league_id"],
                        depth=depth,
                        filters=filters,
                        cache_max_age_days=cache_max_age_days,
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
    ) -> None:
        """Scrape a series with optional depth control.

        Args:
            series_url: URL like "series_seasons.php?series_id=3714"
            league_id: Parent league ID
            depth: Current depth setting from scrape_league
            filters: Filter dictionary from scrape_league
            cache_max_age_days: Days before cache expires
        """
        import time as time_module

        start_time = time_module.time()
        filters = filters or {}

        try:
            # Check cache
            if cache_max_age_days is not None:
                is_cached = self.db.is_url_cached(series_url, "series", cache_max_age_days)
                if is_cached:
                    logger.info(f"⚡ CACHED (skipped): {series_url}")
                    self.progress["skipped_cached"] += 1
                    return

            # Extract series data
            logger.info(f"🌐 FETCHING: {series_url}")
            series_data = self.series_extractor.extract(series_url)
            metadata = series_data["metadata"]

            # Store series in database
            import datetime

            # Check if series already has a name (from league JavaScript)
            existing_series = self.db.get_series(metadata["series_id"])
            series_name = metadata["name"]
            if existing_series and existing_series.get("name"):
                # Preserve the name from league JavaScript (more accurate)
                series_name = existing_series["name"]

            self.db.upsert_series(
                series_id=metadata["series_id"],
                league_id=league_id,
                data={
                    "name": series_name,
                    "url": metadata["url"],
                    "description": metadata.get("description"),
                    "vehicle_type": metadata.get("vehicle_type"),
                    "day_of_week": metadata.get("day_of_week"),
                    "active": metadata.get("active"),
                    "season_count": metadata.get("season_count"),
                    "created_date": metadata.get("created_date"),
                    "scraped_at": datetime.datetime.now().isoformat(),
                },
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
                for season_info in seasons:
                    self.db.upsert_season(
                        season_id=season_info.get("season_id", 0),
                        series_id=metadata["series_id"],
                        data={
                            "name": season_info.get("name", "Unknown Season"),
                            "url": season_info["url"],
                            "scraped_at": datetime.datetime.now().isoformat(),
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
    ) -> None:
        """Scrape a season with optional depth control.

        Args:
            season_url: URL like "season_race.php?series_id=3714&season_id=17424"
            season_id: Season ID (from parent series data)
            series_id: Parent series ID
            depth: Current depth setting
            filters: Filter dictionary
            cache_max_age_days: Days before cache expires
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

            self.db.upsert_season(
                season_id=season_id,
                series_id=metadata["series_id"],
                data={
                    "name": season_name,
                    "url": metadata["url"],
                    "scraped_at": datetime.datetime.now().isoformat(),
                },
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
                        cache_max_age_days=cache_max_age_days,
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
        cache_max_age_days: int | None = 7,
    ) -> None:
        """Scrape a race and its results.

        Args:
            race_url: URL like "season_race.php?schedule_id=324462"
            season_id: Season ID (foreign key for races table)
            cache_max_age_days: Days before cache expires
        """
        import time as time_module

        start_time = time_module.time()

        try:
            # Check cache
            if cache_max_age_days is not None:
                is_cached = self.db.is_url_cached(race_url, "race", cache_max_age_days)
                if is_cached:
                    logger.info(f"⚡ CACHED (skipped): {race_url}")
                    self.progress["skipped_cached"] += 1
                    return

            # Extract race data
            logger.info(f"🌐 FETCHING: {race_url}")
            race_data = self.race_extractor.extract(race_url)
            metadata = race_data["metadata"]

            # Store race in database and get internal race_id for storing results
            import datetime

            # Use season_id passed from parent scrape_season() method
            race_id = self.db.upsert_race(
                schedule_id=metadata["schedule_id"],
                season_id=season_id,
                data={
                    "name": metadata["name"],
                    "url": metadata["url"],
                    "race_number": metadata.get("race_number", 0),
                    "scraped_at": datetime.datetime.now().isoformat(),
                },
            )

            self.progress["races_scraped"] += 1

            # Store race results
            results = race_data.get("results", [])
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

        # Ensure driver exists in database
        try:
            self.db.upsert_driver(
                driver_id=driver_id,
                league_id=league_id,
                data={
                    "name": driver_name,
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
        result_data = {
            "finish_position": result.get("finish_position"),
            "car_number": result.get("car_number"),
            "laps_completed": self._parse_int(result.get("laps")),
            "interval": result.get("interval"),
            "laps_led": self._parse_int(result.get("laps_led")),
            "race_points": self._parse_float(result.get("points")),
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
