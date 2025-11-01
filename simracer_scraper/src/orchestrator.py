"""Orchestrator for hierarchical scraping of SimRacerHub data.

This module coordinates the scraping of league hierarchies using the extractor
classes and database storage with smart caching.
"""

from typing import Any

from .database import Database
from .extractors import LeagueExtractor, RaceExtractor, SeasonExtractor, SeriesExtractor
from .schema_validator import SchemaValidator


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

        # Initialize extractors with rate limiting configuration
        extractor_kwargs = {
            "max_retries": max_retries,
            "timeout": timeout,
        }

        # Use randomized or fixed rate limiting
        if rate_limit_range:
            extractor_kwargs["rate_limit_range"] = rate_limit_range
        else:
            extractor_kwargs["rate_limit_seconds"] = rate_limit_seconds

        self.league_extractor = LeagueExtractor(**extractor_kwargs)
        self.series_extractor = SeriesExtractor(**extractor_kwargs)
        self.season_extractor = SeasonExtractor(**extractor_kwargs)
        self.race_extractor = RaceExtractor(**extractor_kwargs)

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
        """Exit context manager."""
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
                is_cached = self.db.is_url_cached(
                    league_url, "league", cache_max_age_days
                )
                if is_cached:
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
            league_data = self.league_extractor.extract(league_url)
            metadata = league_data["metadata"]

            # Store league in database
            self.db.upsert_league(
                league_id=metadata["league_id"],
                name=metadata["name"],
                url=metadata["url"],
                description=metadata.get("description"),
                organizer=metadata.get("organizer"),
            )

            self.progress["leagues_scraped"] += 1

            # Log successful scrape
            duration_ms = int((time_module.time() - start_time) * 1000)
            self.db.log_scrape(
                "league", league_url, "success", duration_ms=duration_ms
            )

            # If depth allows, scrape child series
            if depth in ["series", "season", "race"]:
                series_urls = league_data["child_urls"]["series"]

                # Apply series filter if specified
                if "series_ids" in filters:
                    allowed_ids = set(filters["series_ids"])
                    series_urls = [
                        s for s in series_urls if s["series_id"] in allowed_ids
                    ]

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
            self.progress["errors"].append(
                {"entity": "league", "url": league_url, "error": str(e)}
            )
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
                is_cached = self.db.is_url_cached(
                    series_url, "series", cache_max_age_days
                )
                if is_cached:
                    self.progress["skipped_cached"] += 1
                    return

            # Extract series data
            series_data = self.series_extractor.extract(series_url)
            metadata = series_data["metadata"]

            # Store series in database
            self.db.upsert_series(
                series_id=metadata["series_id"],
                league_id=league_id,
                name=metadata["name"],
                url=metadata["url"],
                description=metadata.get("description"),
                vehicle_type=metadata.get("vehicle_type"),
                day_of_week=metadata.get("day_of_week"),
                active=metadata.get("active"),
                season_count=metadata.get("season_count"),
                created_date=metadata.get("created_date"),
            )

            self.progress["series_scraped"] += 1

            # Log successful scrape
            duration_ms = int((time_module.time() - start_time) * 1000)
            self.db.log_scrape(
                "series", series_url, "success", duration_ms=duration_ms
            )

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

                # Scrape each season
                for season_info in seasons:
                    self.scrape_season(
                        season_url=season_info["url"],
                        series_id=metadata["series_id"],
                        depth=depth,
                        filters=filters,
                        cache_max_age_days=cache_max_age_days,
                    )

        except Exception as e:
            self.progress["errors"].append(
                {"entity": "series", "url": series_url, "error": str(e)}
            )
            duration_ms = int((time_module.time() - start_time) * 1000)
            self.db.log_scrape(
                "series", series_url, "failed", error_msg=str(e), duration_ms=duration_ms
            )
            # Don't re-raise, continue with other series

    def scrape_season(
        self,
        season_url: str,
        series_id: int,
        depth: str,
        filters: dict[str, Any] | None = None,
        cache_max_age_days: int | None = 7,
    ) -> None:
        """Scrape a season with optional depth control.

        Args:
            season_url: URL like "season_race.php?series_id=3714"
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
                is_cached = self.db.is_url_cached(
                    season_url, "season", cache_max_age_days
                )
                if is_cached:
                    self.progress["skipped_cached"] += 1
                    return

            # Extract season data
            season_data = self.season_extractor.extract(season_url)
            metadata = season_data["metadata"]

            # Store season in database
            self.db.upsert_season(
                series_id=metadata["series_id"],
                name=metadata["name"],
                url=metadata["url"],
            )

            self.progress["seasons_scraped"] += 1

            # Log successful scrape
            duration_ms = int((time_module.time() - start_time) * 1000)
            self.db.log_scrape(
                "season", season_url, "success", duration_ms=duration_ms
            )

            # If depth allows, scrape races
            if depth == "race":
                races = season_data["child_urls"]["races"]

                # Apply race filters if needed (future enhancement)
                # if "race_limit" in filters:
                #     races = races[:filters["race_limit"]]

                # Scrape each race
                for race_info in races:
                    self.scrape_race(
                        race_url=race_info["url"],
                        cache_max_age_days=cache_max_age_days,
                    )

        except Exception as e:
            self.progress["errors"].append(
                {"entity": "season", "url": season_url, "error": str(e)}
            )
            duration_ms = int((time_module.time() - start_time) * 1000)
            self.db.log_scrape(
                "season", season_url, "failed", error_msg=str(e), duration_ms=duration_ms
            )
            # Don't re-raise, continue with other seasons

    def scrape_race(
        self,
        race_url: str,
        cache_max_age_days: int | None = 7,
    ) -> None:
        """Scrape a race and its results.

        Args:
            race_url: URL like "season_race.php?schedule_id=324462"
            cache_max_age_days: Days before cache expires
        """
        import time as time_module

        start_time = time_module.time()

        try:
            # Check cache
            if cache_max_age_days is not None:
                is_cached = self.db.is_url_cached(
                    race_url, "race", cache_max_age_days
                )
                if is_cached:
                    self.progress["skipped_cached"] += 1
                    return

            # Extract race data
            race_data = self.race_extractor.extract(race_url)
            metadata = race_data["metadata"]

            # Store race in database
            # Note: We don't have all race fields from metadata yet
            # This is a simplified version - may need to enhance RaceExtractor
            self.db.upsert_race(
                schedule_id=metadata["schedule_id"],
                name=metadata["name"],
                url=metadata["url"],
            )

            self.progress["races_scraped"] += 1

            # Log successful scrape
            duration_ms = int((time_module.time() - start_time) * 1000)
            self.db.log_scrape(
                "race", race_url, "success", duration_ms=duration_ms
            )

            # TODO: Store race results in race_results table
            # for result in race_data["results"]:
            #     self.db.upsert_race_result(...)

        except Exception as e:
            self.progress["errors"].append(
                {"entity": "race", "url": race_url, "error": str(e)}
            )
            duration_ms = int((time_module.time() - start_time) * 1000)
            self.db.log_scrape(
                "race", race_url, "failed", error_msg=str(e), duration_ms=duration_ms
            )
            # Don't re-raise, continue with other races
