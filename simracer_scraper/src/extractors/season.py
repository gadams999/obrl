"""Season extractor for SimRacerHub season pages.

This module extracts season metadata and discovers race URLs from season pages.
"""

import re
from typing import TYPE_CHECKING, Any

from bs4 import BeautifulSoup

from .base import BaseExtractor

if TYPE_CHECKING:
    from ..utils.browser_manager import BrowserManager


class SeasonExtractor(BaseExtractor):
    """Extractor for season schedule pages.

    Extracts season metadata and discovers child race URLs from season schedule pages.
    URL format: https://www.simracerhub.com/season_schedule.php?season_id={id}

    The season schedule page lists all races with race numbers in a table.
    Individual race results are at: season_race.php?schedule_id={race_id}
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
        """Extract season data from a season schedule page.

        Args:
            url: URL of the season schedule page (season_schedule.php?season_id=X)

        Returns:
            Dictionary with structure:
            {
                "metadata": {
                    "season_id": int,
                    "name": str,
                    "url": str,
                },
                "child_urls": {
                    "races": [list of race dicts with URL and metadata]
                }
            }

        Raises:
            ValueError: If URL is invalid or missing season_id
            requests.exceptions.RequestException: If fetch fails
        """
        # Validate URL format
        self._validate_url(url)

        # Extract season_id from URL
        season_id = self._extract_season_id(url)

        # Fetch and parse the page
        soup = self.fetch_page(url)

        # Extract season metadata
        metadata = self._extract_metadata(soup, season_id, url)

        # Extract child URLs (races)
        child_urls = self._extract_child_urls(soup)

        return {"metadata": metadata, "child_urls": child_urls}

    def _validate_url(self, url: str) -> None:
        """Validate that URL is a valid season schedule URL.

        Args:
            url: URL to validate

        Raises:
            ValueError: If URL format is invalid
        """
        if not re.search(r"season_schedule\.php\?season_id=\d+", url):
            raise ValueError(
                f"Invalid season URL format. Expected season_schedule.php?season_id=<id>, got: {url}"
            )

    def _extract_season_id(self, url: str) -> int:
        """Extract season_id from URL.

        Args:
            url: Season schedule URL (season_schedule.php?season_id=...)

        Returns:
            Season ID as integer

        Raises:
            ValueError: If season_id not found in URL
        """
        match = re.search(r"season_id=(\d+)", url)
        if not match:
            raise ValueError(f"Could not extract season_id from URL: {url}")

        return int(match.group(1))

    def _extract_metadata(self, soup: BeautifulSoup, season_id: int, url: str) -> dict[str, Any]:
        """Extract season metadata from page.

        Args:
            soup: BeautifulSoup object of parsed page
            season_id: Season ID
            url: Original URL

        Returns:
            Dictionary with season metadata
        """
        metadata = {
            "season_id": season_id,
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
                        "has_results": bool,
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

        Extracts race date and time from schedule, converts to UTC.

        Args:
            soup: BeautifulSoup object

        Returns:
            List of race dictionaries with URLs and metadata including has_results flag
        """
        races = []
        base_url = "https://www.simracerhub.com"
        seen_schedule_ids = set()  # Track unique races

        # Extract races from schedule table
        # season_schedule.php has a table with race numbers in first column
        table = soup.find("table", class_="schedule-table")
        if not table:
            table = soup.find("table")

        if table:
            rows = table.find_all("tr")

            for row in rows:
                cells = row.find_all("td")

                # Skip header rows or empty rows
                if not cells:
                    continue

                # Find schedule_id link in this row
                links = row.find_all("a", href=re.compile(r"schedule_id=\d+"))
                if not links:
                    continue

                for link in links:
                    href = link.get("href", "")
                    track_name = link.get_text(strip=True)  # Track name from link text

                    match = re.search(r"schedule_id=(\d+)", href)
                    if not match:
                        continue

                    schedule_id = int(match.group(1))

                    if schedule_id in seen_schedule_ids:
                        continue
                    seen_schedule_ids.add(schedule_id)

                    if href.startswith("http"):
                        full_url = href
                    else:
                        full_url = f"{base_url}/{href}"

                    # Check if this race has results available
                    has_results = bool(href and "season_race.php" in href)

                    # Extract race number from first cell
                    # Can be just a number like "1", "2", "3" or "Race 1", "Race 2", etc.
                    # Skip rows without a valid race number (informational rows)
                    race_number = 0
                    try:
                        first_cell_text = cells[0].get_text(strip=True)
                        # Try direct number first
                        if first_cell_text.isdigit():
                            race_number = int(first_cell_text)
                        else:
                            # Try to extract number from "Race N" pattern
                            race_num_match = re.search(
                                r"Race\s+(\d+)", first_cell_text, re.IGNORECASE
                            )
                            if race_num_match:
                                race_number = int(race_num_match.group(1))
                    except (ValueError, IndexError):
                        pass

                    # Skip this row if no valid race number found (informational row)
                    if race_number == 0:
                        continue

                    race_dict = {
                        "url": full_url,
                        "schedule_id": schedule_id,
                        "track": track_name,
                        "has_results": has_results,
                        "race_number": race_number,
                    }
                    for cell in cells:
                        cell_text = cell.get_text(strip=True)
                        # Look for date+time patterns like "Oct 29, 2025 7:00 PM" or just date
                        datetime_match = re.search(
                            r"([A-Za-z]{3}\s+\d{1,2},\s+\d{4})\s+(\d{1,2}):(\d{2})\s*(AM|PM)?",
                            cell_text,
                        )
                        if datetime_match:
                            from datetime import datetime, timezone
                            from zoneinfo import ZoneInfo

                            date_str = datetime_match.group(1)
                            hour = int(datetime_match.group(2))
                            minute = int(datetime_match.group(3))
                            am_pm = datetime_match.group(4)

                            try:
                                # Parse date
                                parsed_date = datetime.strptime(date_str, "%b %d, %Y")

                                # Add time
                                if am_pm:
                                    # 12-hour format
                                    if am_pm == "PM" and hour != 12:
                                        hour += 12
                                    elif am_pm == "AM" and hour == 12:
                                        hour = 0

                                # Create datetime with time in local timezone
                                # Try to get system timezone, fall back to UTC
                                try:
                                    import time as time_module

                                    # Get system timezone name
                                    if time_module.daylight:
                                        tz_name = time_module.tzname[1]
                                    else:
                                        tz_name = time_module.tzname[0]

                                    # Try common timezone abbreviations mapping
                                    tz_map = {
                                        "EST": "America/New_York",
                                        "EDT": "America/New_York",
                                        "CST": "America/Chicago",
                                        "CDT": "America/Chicago",
                                        "MST": "America/Denver",
                                        "MDT": "America/Denver",
                                        "PST": "America/Los_Angeles",
                                        "PDT": "America/Los_Angeles",
                                    }
                                    tz_full = tz_map.get(tz_name, "UTC")
                                    local_tz = ZoneInfo(tz_full)
                                except Exception:
                                    local_tz = timezone.utc

                                local_dt = parsed_date.replace(
                                    hour=hour, minute=minute, tzinfo=local_tz
                                )

                                # Convert to UTC
                                utc_dt = local_dt.astimezone(timezone.utc)
                                race_dict["date"] = utc_dt.isoformat()
                            except (ValueError, Exception):
                                # If parsing fails, try date-only
                                pass
                            break

                        # Fallback: just date without time
                        date_match = re.search(r"([A-Za-z]{3}\s+\d{1,2},\s+\d{4})", cell_text)
                        if date_match and "date" not in race_dict:
                            from datetime import datetime

                            date_str = date_match.group(1)
                            try:
                                # Parse the date string and convert to ISO format
                                parsed_date = datetime.strptime(date_str, "%b %d, %Y")
                                race_dict["date"] = parsed_date.isoformat()
                            except ValueError:
                                # If parsing fails, store the raw string
                                race_dict["date"] = date_str
                            break

                    races.append(race_dict)

        return races
