"""Series extractor for SimRacerHub series pages.

This module extracts series metadata and discovers season URLs from series pages.
"""

import re
from typing import TYPE_CHECKING, Any

from bs4 import BeautifulSoup

from ..schema_validator import SchemaValidator
from ..utils.js_parser import extract_season_data
from .base import BaseExtractor

if TYPE_CHECKING:
    from ..utils.browser_manager import BrowserManager


class SeriesExtractor(BaseExtractor):
    """Extractor for series seasons pages.

    Extracts series metadata and discovers child season URLs from series pages.
    URL format: https://www.simracerhub.com/series_seasons.php?series_id={id}

    Attributes:
        validator: SchemaValidator instance for validating page structure
    """

    def __init__(
        self,
        rate_limit_seconds: float = 2.0,
        rate_limit_range: tuple[float, float] | None = None,
        max_retries: int = 3,
        timeout: int = 30,
        backoff_factor: int = 2,
        render_js: bool = False,
        browser_manager: "BrowserManager | None" = None,
        user_agent: str | None = None,
    ):
        """Initialize the series extractor.

        Args:
            rate_limit_seconds: Fixed seconds between requests (default: 2.0)
                Ignored if rate_limit_range is provided or browser_manager is used
            rate_limit_range: Tuple of (min, max) seconds for randomized delay
                If provided, overrides rate_limit_seconds
                Example: (2.0, 4.0) for random delay between 2-4 seconds
                Ignored if browser_manager is used
            max_retries: Maximum retry attempts (default: 3)
            timeout: Request timeout in seconds (default: 30)
            backoff_factor: Exponential backoff multiplier (default: 2)
            render_js: Use JavaScript rendering (default: False)
            browser_manager: Shared browser manager for coordinated rate limiting
            user_agent: Custom User-Agent string for HTTP requests
        """
        super().__init__(
            rate_limit_seconds,
            rate_limit_range,
            max_retries,
            timeout,
            backoff_factor,
            render_js,
            browser_manager,
            user_agent,
        )
        self.validator = SchemaValidator()

    def extract(self, url: str) -> dict[str, Any]:
        """Extract series data from a series seasons page.

        Args:
            url: URL of the series seasons page

        Returns:
            Dictionary with structure:
            {
                "metadata": {
                    "series_id": int,
                    "name": str,
                    "url": str,
                    ...
                },
                "child_urls": {
                    "seasons": [list of season dicts with URL and metadata]
                }
            }

        Raises:
            ValueError: If URL is invalid or missing series_id
            SchemaChangeDetected: If page structure doesn't match expected schema
            requests.exceptions.RequestException: If fetch fails
        """
        # Validate URL format
        self._validate_url(url)

        # Extract series_id from URL
        series_id = self._extract_series_id(url)

        # Fetch and parse the page
        soup = self.fetch_page(url)

        # Validate JavaScript schema
        self.validator.validate_javascript_data("series_seasons", str(soup))

        # Extract series metadata
        metadata = self._extract_metadata(soup, series_id, url)

        # Extract child URLs (seasons)
        child_urls = self._extract_child_urls(soup, series_id)

        return {"metadata": metadata, "child_urls": child_urls}

    def _validate_url(self, url: str) -> None:
        """Validate that URL is a valid series seasons URL.

        Args:
            url: URL to validate

        Raises:
            ValueError: If URL format is invalid
        """
        if not re.search(r"series_seasons\.php\?series_id=\d+", url):
            raise ValueError(
                f"Invalid series URL format. Expected series_seasons.php?series_id=<id>, got: {url}"
            )

    def _extract_series_id(self, url: str) -> int:
        """Extract series_id from URL.

        Args:
            url: Series seasons URL

        Returns:
            Series ID as integer

        Raises:
            ValueError: If series_id not found in URL
        """
        match = re.search(r"series_id=(\d+)", url)
        if not match:
            raise ValueError(f"Could not extract series_id from URL: {url}")

        return int(match.group(1))

    def _extract_metadata(self, soup: BeautifulSoup, series_id: int, url: str) -> dict[str, Any]:
        """Extract series metadata from page.

        Args:
            soup: BeautifulSoup object of parsed page
            series_id: Series ID
            url: Original URL

        Returns:
            Dictionary with series metadata
        """
        metadata = {
            "series_id": series_id,
            "url": url,
        }

        # Extract series name from H1 or page title
        name = self._extract_series_name(soup)
        metadata["name"] = name

        return metadata

    def _extract_series_name(self, soup: BeautifulSoup) -> str:
        """Extract series name from page.

        Tries multiple strategies:
        1. H1 tag
        2. Page title
        3. Default fallback

        Args:
            soup: BeautifulSoup object

        Returns:
            Series name string
        """
        # Try H1 tag first
        h1 = soup.find("h1")
        if h1:
            name = h1.get_text(strip=True)
            if name:
                return name

        # Try page title
        title = soup.find("title")
        if title:
            title_text = title.get_text(strip=True)
            # Remove "Sim Racer Hub: " prefix or " - Seasons" suffix if present
            if " - " in title_text:
                name = title_text.split(" - ")[0].strip()
                if name:
                    return name
            if title_text:
                return title_text

        # Fallback
        return "Unknown Series"

    def _extract_child_urls(self, soup: BeautifulSoup, series_id: int) -> dict[str, Any]:
        """Extract child entity URLs (seasons).

        Args:
            soup: BeautifulSoup object
            series_id: Series ID

        Returns:
            Dictionary with child URLs and metadata:
            {
                "seasons": [
                    {
                        "url": str,
                        "season_id": int,
                        "name": str,
                        "start_time": int,
                        "scheduled_races": int,
                        "completed_races": int
                    },
                    ...
                ]
            }
        """
        child_urls = {}

        # Extract seasons from JavaScript
        seasons = self._extract_seasons(soup, series_id)
        child_urls["seasons"] = seasons

        return child_urls

    def _extract_seasons(self, soup: BeautifulSoup, series_id: int) -> list[dict[str, Any]]:
        """Extract season data from JavaScript.

        Args:
            soup: BeautifulSoup object
            series_id: Series ID (used to build season URLs)

        Returns:
            List of season dictionaries with URLs and metadata
        """
        # Extract season data from JavaScript
        html_str = str(soup)
        season_data = extract_season_data(html_str)

        # Build season dictionaries with URLs and metadata
        # Note: Including season_id in URL to ensure uniqueness (required by DB schema)
        base_url = "https://www.simracerhub.com"
        seasons = []

        for season in season_data:
            # Handle both fixture format (n) and live site format (sname)
            season_name = season.get("n") or season.get("sname")

            if "id" in season and season_name:
                season_dict = {
                    "url": f"{base_url}/season_schedule.php?season_id={season['id']}",
                    "season_id": season["id"],
                    "name": season_name,
                }

                # Add optional metadata if present
                if "scrt" in season:
                    season_dict["start_time"] = season["scrt"]
                if "ns" in season:
                    season_dict["scheduled_races"] = season["ns"]
                if "nr" in season:
                    season_dict["completed_races"] = season["nr"]

                seasons.append(season_dict)

        return seasons
