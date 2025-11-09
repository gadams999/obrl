"""Driver extractor for SimRacerHub driver profile pages.

This module extracts driver statistics from driver profile pages.
"""

import re
from typing import TYPE_CHECKING, Any

from .base import BaseExtractor

if TYPE_CHECKING:
    from ..utils.browser_manager import BrowserManager


class DriverExtractor(BaseExtractor):
    """Extractor for driver profile pages.

    Extracts driver statistics (irating, safety_rating, license_class) from
    driver profile pages.
    URL format: https://www.simracerhub.com/driver_stats.php?driver_id={id}
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
        """Initialize the driver extractor.

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
                Note: Driver stats are in static HTML, so JS rendering not needed
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
        """Extract driver statistics from a driver profile page.

        Args:
            url: URL of the driver profile page

        Returns:
            Dictionary with structure:
            {
                "metadata": {
                    "driver_id": int,
                    "url": str,
                    "irating": int | None,
                    "safety_rating": float | None,
                    "license_class": str | None,
                }
            }

        Raises:
            ValueError: If URL is invalid or missing driver_id
            requests.exceptions.RequestException: If fetch fails
        """
        # Validate URL format
        self._validate_url(url)

        # Extract driver_id from URL
        driver_id = self._extract_driver_id(url)

        # Fetch and parse the page
        soup = self.fetch_page(url)

        # Extract driver stats from JavaScript
        stats = self._extract_stats(soup)

        # Build metadata
        metadata = {
            "driver_id": driver_id,
            "url": url,
            "irating": stats.get("irating"),
            "safety_rating": stats.get("safety_rating"),
            "license_class": stats.get("license_class"),
        }

        return {"metadata": metadata}

    def _validate_url(self, url: str) -> None:
        """Validate the driver profile URL format.

        Args:
            url: URL to validate

        Raises:
            ValueError: If URL format is invalid
        """
        if not url or "driver_stats.php" not in url:
            raise ValueError(f"Invalid driver profile URL: {url}")

    def _extract_driver_id(self, url: str) -> int:
        """Extract driver_id from URL.

        Args:
            url: Driver profile URL

        Returns:
            Driver ID as integer

        Raises:
            ValueError: If driver_id cannot be extracted
        """
        match = re.search(r"driver_id=(\d+)", url)
        if not match:
            raise ValueError(f"Could not extract driver_id from URL: {url}")
        return int(match.group(1))

    def _extract_stats(self, soup) -> dict[str, Any]:
        """Extract driver statistics from the page.

        Driver stats are embedded in JavaScript as React component props.
        The data is in race participation records, all containing the same
        driver stats (irating, sr, license).

        Args:
            soup: BeautifulSoup object of the page

        Returns:
            Dictionary with driver stats:
            {
                "irating": int | None,
                "safety_rating": float | None,
                "license_class": str | None,
            }
        """
        # Get page HTML as string for regex matching
        html = str(soup)

        # Pattern to extract driver stats from race participation data
        # Format: "irating":"3126","sr":"4.79","license":"Class A"
        pattern = r'"irating":"(\d+)","sr":"([\d.]+)","license":"([^"]+)"'
        match = re.search(pattern, html)

        if not match:
            # No race data found - driver might have no races
            return {
                "irating": None,
                "safety_rating": None,
                "license_class": None,
            }

        # Extract and convert values
        irating = int(match.group(1))
        safety_rating = float(match.group(2))
        license_class = match.group(3)

        return {
            "irating": irating,
            "safety_rating": safety_rating,
            "license_class": license_class,
        }
