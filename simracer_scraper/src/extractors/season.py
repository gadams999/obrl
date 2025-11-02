"""Season extractor for SimRacerHub season pages.

This module extracts season metadata and discovers race URLs from season pages.
"""

import re
from typing import Any, TYPE_CHECKING

from bs4 import BeautifulSoup

from .base import BaseExtractor

if TYPE_CHECKING:
    from ..utils.browser_manager import BrowserManager


class SeasonExtractor(BaseExtractor):
    """Extractor for season race pages.

    Extracts season metadata and discovers child race URLs from season pages.
    URL format: https://www.simracerhub.com/season_race.php?series_id={id}

    Note: The URL uses series_id parameter but shows the current season's races.
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
    ):
        """Initialize the season extractor.

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
        """
        super().__init__(
            rate_limit_seconds,
            rate_limit_range,
            max_retries,
            timeout,
            backoff_factor,
            render_js,
            browser_manager,
        )

    def extract(self, url: str) -> dict[str, Any]:
        """Extract season data from a season race page.

        Args:
            url: URL of the season race page

        Returns:
            Dictionary with structure:
            {
                "metadata": {
                    "series_id": int,
                    "name": str,
                    "url": str,
                },
                "child_urls": {
                    "races": [list of race dicts with URL and metadata]
                }
            }

        Raises:
            ValueError: If URL is invalid or missing series_id
            requests.exceptions.RequestException: If fetch fails
        """
        # Validate URL format
        self._validate_url(url)

        # Extract series_id from URL
        series_id = self._extract_series_id(url)

        # Fetch and parse the page
        soup = self.fetch_page(url)

        # Extract season metadata
        metadata = self._extract_metadata(soup, series_id, url)

        # Extract child URLs (races)
        child_urls = self._extract_child_urls(soup)

        return {"metadata": metadata, "child_urls": child_urls}

    def _validate_url(self, url: str) -> None:
        """Validate that URL is a valid season race URL.

        Args:
            url: URL to validate

        Raises:
            ValueError: If URL format is invalid
        """
        if not re.search(r"season_race\.php\?series_id=\d+", url):
            raise ValueError(
                f"Invalid season URL format. Expected season_race.php?series_id=<id>, got: {url}"
            )

    def _extract_series_id(self, url: str) -> int:
        """Extract series_id from URL.

        Args:
            url: Season race URL

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
        """Extract season metadata from page.

        Args:
            soup: BeautifulSoup object of parsed page
            series_id: Series ID
            url: Original URL

        Returns:
            Dictionary with season metadata
        """
        metadata = {
            "series_id": series_id,
            "url": url,
        }

        # Extract season name from H1 or page title
        name = self._extract_season_name(soup)
        metadata["name"] = name

        return metadata

    def _extract_season_name(self, soup: BeautifulSoup) -> str:
        """Extract season name from page.

        Tries multiple strategies:
        1. H1 tag
        2. Page title
        3. Default fallback

        Args:
            soup: BeautifulSoup object

        Returns:
            Season name string
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
            # Remove " - Race Schedule" suffix if present
            if " - " in title_text:
                name = title_text.split(" - ")[0].strip()
                if name:
                    return name
            if title_text:
                return title_text

        # Fallback
        return "Unknown Season"

    def _extract_child_urls(self, soup: BeautifulSoup) -> dict[str, Any]:
        """Extract child entity URLs (races).

        Args:
            soup: BeautifulSoup object

        Returns:
            Dictionary with child URLs and metadata:
            {
                "races": [
                    {
                        "url": str,
                        "schedule_id": int,
                        "track": str,
                    },
                    ...
                ]
            }
        """
        child_urls = {}

        # Extract races from HTML table
        races = self._extract_races(soup)
        child_urls["races"] = races

        return child_urls

    def _extract_races(self, soup: BeautifulSoup) -> list[dict[str, Any]]:
        """Extract race data from dropdown menu or HTML table.

        SimRacerHub shows races in a dropdown menu when JavaScript is enabled,
        or in a table when JavaScript is disabled.

        Args:
            soup: BeautifulSoup object

        Returns:
            List of race dictionaries with URLs and metadata
        """
        races = []
        base_url = "https://www.simracerhub.com"
        seen_schedule_ids = set()  # Track unique races

        # Method 1: Try dropdown menu (JavaScript-rendered content)
        # Note: There may be multiple dropdowns, find the one with schedule links
        dropdowns = soup.find_all("ul", class_="dropdown-menu")
        for dropdown in dropdowns:
            links = dropdown.find_all("a", href=re.compile(r"schedule_id=\d+"))

            # Skip dropdowns without schedule links
            if not links:
                continue

            for link in links:
                href = link.get("href", "")
                track_name = link.get_text(strip=True)

                # Extract schedule_id from URL
                match = re.search(r"schedule_id=(\d+)", href)
                if match:
                    schedule_id = int(match.group(1))

                    # Skip duplicates
                    if schedule_id in seen_schedule_ids:
                        continue
                    seen_schedule_ids.add(schedule_id)

                    # Build full URL
                    if href.startswith("http"):
                        full_url = href
                    else:
                        full_url = f"{base_url}{href}" if href.startswith("/") else f"{base_url}/{href}"

                    race_dict = {
                        "url": full_url,
                        "schedule_id": schedule_id,
                        "track": track_name,
                    }

                    races.append(race_dict)

        # Method 2: Fallback to table (static HTML content)
        if not races:
            table = soup.find("table", class_="schedule-table")
            if not table:
                table = soup.find("table")

            if table:
                rows = table.find_all("tr")

                for row in rows:
                    links = row.find_all("a", href=re.compile(r"schedule_id=\d+"))

                    for link in links:
                        href = link.get("href", "")
                        track_name = link.get_text(strip=True)

                        match = re.search(r"schedule_id=(\d+)", href)
                        if match:
                            schedule_id = int(match.group(1))

                            if schedule_id in seen_schedule_ids:
                                continue
                            seen_schedule_ids.add(schedule_id)

                            if href.startswith("http"):
                                full_url = href
                            else:
                                full_url = f"{base_url}/{href}"

                            race_dict = {
                                "url": full_url,
                                "schedule_id": schedule_id,
                                "track": track_name,
                            }

                            races.append(race_dict)

        return races
