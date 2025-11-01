"""League extractor for SimRacerHub league pages.

This module extracts league metadata and discovers series URLs from league pages.
"""

import re
from typing import Any

from bs4 import BeautifulSoup

from ..schema_validator import SchemaValidator
from ..utils.js_parser import extract_series_data
from .base import BaseExtractor


class LeagueExtractor(BaseExtractor):
    """Extractor for league series pages.

    Extracts league metadata and discovers child series URLs from league pages.
    URL format: https://www.simracerhub.com/league_series.php?league_id={id}

    Attributes:
        validator: SchemaValidator instance for validating page structure
    """

    def __init__(
        self,
        rate_limit_seconds: float = 2.0,
        max_retries: int = 3,
        timeout: int = 30,
        backoff_factor: int = 2,
    ):
        """Initialize the league extractor.

        Args:
            rate_limit_seconds: Minimum seconds between requests (default: 2.0)
            max_retries: Maximum retry attempts (default: 3)
            timeout: Request timeout in seconds (default: 30)
            backoff_factor: Exponential backoff multiplier (default: 2)
        """
        super().__init__(rate_limit_seconds, max_retries, timeout, backoff_factor)
        self.validator = SchemaValidator()

    def extract(self, url: str) -> dict[str, Any]:
        """Extract league data from a league series page.

        Args:
            url: URL of the league series page

        Returns:
            Dictionary with structure:
            {
                "metadata": {
                    "league_id": int,
                    "name": str,
                    "url": str,
                    ...
                },
                "child_urls": {
                    "series": [list of series URLs],
                    "teams": str (optional)
                }
            }

        Raises:
            ValueError: If URL is invalid or missing league_id
            SchemaChangeDetected: If page structure doesn't match expected schema
            requests.exceptions.RequestException: If fetch fails
        """
        # Validate URL format
        self._validate_url(url)

        # Extract league_id from URL
        league_id = self._extract_league_id(url)

        # Fetch and parse the page
        soup = self.fetch_page(url)

        # Validate JavaScript schema
        self.validator.validate_javascript_data("league_series", str(soup))

        # Extract league metadata
        metadata = self._extract_metadata(soup, league_id, url)

        # Validate extracted metadata
        self.validator.validate_extracted_data("league_series", metadata)

        # Extract child URLs (series, teams)
        child_urls = self._extract_child_urls(soup, league_id)

        return {"metadata": metadata, "child_urls": child_urls}

    def _validate_url(self, url: str) -> None:
        """Validate that URL is a valid league series URL.

        Args:
            url: URL to validate

        Raises:
            ValueError: If URL format is invalid
        """
        if not re.search(r"league_series\.php\?league_id=\d+", url):
            raise ValueError(
                f"Invalid league URL format. Expected league_series.php?league_id=<id>, got: {url}"
            )

    def _extract_league_id(self, url: str) -> int:
        """Extract league_id from URL.

        Args:
            url: League series URL

        Returns:
            League ID as integer

        Raises:
            ValueError: If league_id not found in URL
        """
        match = re.search(r"league_id=(\d+)", url)
        if not match:
            raise ValueError(f"Could not extract league_id from URL: {url}")

        return int(match.group(1))

    def _extract_metadata(self, soup: BeautifulSoup, league_id: int, url: str) -> dict[str, Any]:
        """Extract league metadata from page.

        Args:
            soup: BeautifulSoup object of parsed page
            league_id: League ID
            url: Original URL

        Returns:
            Dictionary with league metadata
        """
        metadata = {
            "league_id": league_id,
            "url": url,
        }

        # Extract league name from H1 or page title
        name = self._extract_league_name(soup)
        metadata["name"] = name

        # Extract description if available
        description = self._extract_description(soup)
        if description:
            metadata["description"] = description

        return metadata

    def _extract_league_name(self, soup: BeautifulSoup) -> str:
        """Extract league name from page.

        Tries multiple strategies:
        1. H1 tag
        2. Page title
        3. Default fallback

        Args:
            soup: BeautifulSoup object

        Returns:
            League name string
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
            # Remove "Sim Racer Hub: " prefix if present
            if ":" in title_text:
                name = title_text.split(":", 1)[1].strip()
                if name:
                    return name
            if title_text:
                return title_text

        # Fallback
        return "Unknown League"

    def _extract_description(self, soup: BeautifulSoup) -> str | None:
        """Extract league description if available.

        Args:
            soup: BeautifulSoup object

        Returns:
            Description string or None if not found
        """
        # Try finding description in common locations
        desc_div = soup.find("div", class_="league-description")
        if desc_div:
            return desc_div.get_text(strip=True)

        # Try finding first paragraph after H1
        h1 = soup.find("h1")
        if h1:
            next_p = h1.find_next("p")
            if next_p:
                text = next_p.get_text(strip=True)
                if text:
                    return text

        return None

    def _extract_child_urls(self, soup: BeautifulSoup, league_id: int) -> dict[str, Any]:
        """Extract child entity URLs (series, teams).

        Args:
            soup: BeautifulSoup object
            league_id: League ID

        Returns:
            Dictionary with child URLs:
            {
                "series": [list of series URLs],
                "teams": str (optional)
            }
        """
        child_urls = {}

        # Extract series URLs from JavaScript
        series_urls = self._extract_series_urls(soup)
        child_urls["series"] = series_urls

        # Extract teams URL if present
        teams_url = self._extract_teams_url(soup, league_id)
        if teams_url:
            child_urls["teams"] = teams_url

        return child_urls

    def _extract_series_urls(self, soup: BeautifulSoup) -> list[str]:
        """Extract series URLs from JavaScript data.

        Args:
            soup: BeautifulSoup object

        Returns:
            List of series URLs
        """
        # Extract series data from JavaScript
        html_str = str(soup)
        series_data = extract_series_data(html_str)

        # Build URLs from series IDs
        base_url = "https://www.simracerhub.com"
        series_urls = []

        for series in series_data:
            if "id" in series:
                series_id = series["id"]
                url = f"{base_url}/series_seasons.php?series_id={series_id}"
                series_urls.append(url)

        return series_urls

    def _extract_teams_url(self, soup: BeautifulSoup, league_id: int) -> str | None:
        """Extract teams page URL if present.

        Args:
            soup: BeautifulSoup object
            league_id: League ID

        Returns:
            Teams URL or None if not found
        """
        # Look for teams.php link
        teams_link = soup.find("a", href=re.compile(r"teams\.php\?league_id="))
        if teams_link:
            href = teams_link.get("href", "")
            if href:
                # Make absolute URL if relative
                if href.startswith("http"):
                    return href
                else:
                    return f"https://www.simracerhub.com/{href}"

        # Fallback: construct teams URL
        # Most leagues have a teams page, so include it
        return f"https://www.simracerhub.com/teams.php?league_id={league_id}"
