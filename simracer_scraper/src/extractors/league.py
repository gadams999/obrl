"""League extractor for SimRacerHub league pages.

This module extracts league metadata and discovers series URLs from league pages.
"""

import re
from typing import TYPE_CHECKING, Any

from bs4 import BeautifulSoup

from ..schema_validator import SchemaValidator
from ..utils.js_parser import extract_series_data
from .base import BaseExtractor

if TYPE_CHECKING:
    from ..utils.browser_manager import BrowserManager


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
        rate_limit_range: tuple[float, float] | None = None,
        max_retries: int = 3,
        timeout: int = 30,
        backoff_factor: int = 2,
        render_js: bool = False,
        browser_manager: "BrowserManager | None" = None,
    ):
        """Initialize the league extractor.

        Args:
            rate_limit_seconds: Fixed seconds between requests (default: 2.0)
                Ignored if rate_limit_range is provided or browser_manager is used
            rate_limit_range: Tuple of (min, max) seconds for randomized delay
                If provided, overrides rate_limit_seconds
                Example: (2.0, 4.0) for random delay between 2-4 seconds
                Ignored if browser_manager is used
            render_js: Use JavaScript rendering (default: False)
            max_retries: Maximum retry attempts (default: 3)
            timeout: Request timeout in seconds (default: 30)
            backoff_factor: Exponential backoff multiplier (default: 2)
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
        1. Look for dropdown-toggle button (SimRacerHub-specific pattern)
        2. Look for league name in prominent text content (headings)
        3. Try page title (extracting name from title)
        4. H1 tag (often generic "League Series")
        5. Default fallback

        Args:
            soup: BeautifulSoup object

        Returns:
            League name string
        """
        # Strategy 1: Look for dropdown button (SimRacerHub uses this for league names)
        # The league name is in: <button class="dropdown-toggle bold">League Name</button>
        # Note: Must have BOTH dropdown-toggle AND bold classes (not just dropdown-toggle)
        dropdown_buttons = soup.find_all("button", class_="dropdown-toggle")
        for button in dropdown_buttons:
            button_classes = button.get("class", [])
            # Check if button has BOTH dropdown-toggle AND bold classes
            if "dropdown-toggle" in button_classes and "bold" in button_classes:
                name = button.get_text(strip=True)
                if name and len(name) > 3:
                    return name

        # Strategy 2: Try h2 or h3 tags (often contain the actual league name)
        for tag in ["h2", "h3", "h4"]:
            heading = soup.find(tag)
            if heading:
                name = heading.get_text(strip=True)
                # Ignore generic headings
                if name and name not in ["League Series", "Series", "Seasons", "Race Results"]:
                    return name

        # Try finding a div with class containing "league" or "name"
        for class_pattern in ["league-name", "league-title", "league-header"]:
            div = soup.find("div", class_=re.compile(class_pattern, re.I))
            if div:
                name = div.get_text(strip=True)
                if name and len(name) > 3:
                    return name

        # Strategy 3: Try page title and extract meaningful part
        title = soup.find("title")
        if title:
            title_text = title.get_text(strip=True)
            # Remove "Sim Racer Hub: " prefix if present
            if ":" in title_text:
                name = title_text.split(":", 1)[1].strip()
                # Only use if it's not generic
                if name and name not in ["League Series", "Series Seasons", "Race Results"]:
                    return name

        # Strategy 4: Try H1 tag (often generic but worth trying)
        h1 = soup.find("h1")
        if h1:
            name = h1.get_text(strip=True)
            # Only use if it's not generic
            if name and name not in ["League Series", "Series Seasons", "Race Results"]:
                return name

        # Fallback
        return "Unknown League"

    def _extract_description(self, soup: BeautifulSoup) -> str | None:
        """Extract league description if available.

        Args:
            soup: BeautifulSoup object

        Returns:
            Description string or None if not found
        """
        # Try finding description in pageTitleDescr div (SimRacerHub standard)
        desc_div = soup.find("div", class_="pageTitleDescr")
        if desc_div:
            description = desc_div.get_text(strip=True)
            if description:
                return description

        # Fallback: Try finding description in league-description class
        desc_div = soup.find("div", class_="league-description")
        if desc_div:
            return desc_div.get_text(strip=True)

        # Fallback: Try finding first paragraph after H1
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

    def _extract_series_urls(self, soup: BeautifulSoup) -> list[dict[str, Any]]:
        """Extract series URLs and metadata from JavaScript data and HTML table.

        Args:
            soup: BeautifulSoup object

        Returns:
            List of dicts with "url", "series_id", "name", and optional metadata
        """
        from datetime import datetime

        # Extract series data from JavaScript
        html_str = str(soup)
        series_data = extract_series_data(html_str)

        # Extract descriptions from HTML table
        # The table has rows with series info including description in 4th <td>
        series_descriptions = {}

        # Find all table rows with series data
        for row in soup.find_all("tr", class_="jsTableRow"):
            # Look for series link with series_id
            series_link = row.find("a", href=re.compile(r"series_seasons\.php\?series_id=\d+"))
            if series_link:
                href = series_link.get("href", "")
                match = re.search(r"series_id=(\d+)", href)
                if match:
                    row_series_id = int(match.group(1))

                    # Find all td elements in this row
                    cells = row.find_all("td")

                    # Description is typically in the 4th cell (index 3)
                    # Structure: Active | Name | URL | Stats | Description | Created | Seasons
                    if len(cells) >= 5:
                        description_cell = cells[4]  # 5th cell (0-indexed = 4)
                        description = description_cell.get_text(strip=True)
                        if description:
                            series_descriptions[row_series_id] = description

        # Build URLs from series IDs
        base_url = "https://www.simracerhub.com"
        series_urls = []

        for series in series_data:
            if "id" in series:
                series_id = series["id"]
                url = f"{base_url}/series_seasons.php?series_id={series_id}"

                series_dict = {
                    "url": url,
                    "series_id": series_id,
                    "name": series.get("name", "Unknown Series"),
                }

                # Add description from HTML table if available
                if series_id in series_descriptions:
                    series_dict["description"] = series_descriptions[series_id]

                # Add created_date if scrt (Unix timestamp) is present
                if "scrt" in series:
                    try:
                        created_timestamp = series["scrt"]
                        created_dt = datetime.fromtimestamp(created_timestamp)
                        series_dict["created_date"] = created_dt.strftime("%Y-%m-%d")
                    except (ValueError, OSError):
                        pass

                # Add num_seasons if nsea is present
                if "nsea" in series:
                    series_dict["num_seasons"] = series["nsea"]

                series_urls.append(series_dict)

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
