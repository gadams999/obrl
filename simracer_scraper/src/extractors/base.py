"""Base extractor with HTTP fetching and parsing utilities.

This module provides the BaseExtractor class that handles:
- HTTP requests with rate limiting
- Retry logic with exponential backoff
- HTML parsing with BeautifulSoup
- Common extraction utilities
"""

import random
import time

import requests
from bs4 import BeautifulSoup


class BaseExtractor:
    """Base class for extractors with HTTP fetching and parsing utilities.

    Provides rate-limited HTTP fetching, retry logic, and common extraction
    methods for parsing HTML content.

    Attributes:
        rate_limit_seconds: Minimum seconds between requests (for fixed delay)
        rate_limit_range: Tuple of (min, max) seconds for randomized delay
        max_retries: Maximum number of retry attempts
        timeout: Request timeout in seconds
        backoff_factor: Exponential backoff multiplier for retries
    """

    def __init__(
        self,
        rate_limit_seconds: float = 2.0,
        rate_limit_range: tuple[float, float] | None = None,
        max_retries: int = 3,
        timeout: int = 30,
        backoff_factor: int = 2,
    ):
        """Initialize the base extractor.

        Args:
            rate_limit_seconds: Fixed seconds between requests (default: 2.0)
                Ignored if rate_limit_range is provided
            rate_limit_range: Tuple of (min, max) seconds for randomized delay
                If provided, overrides rate_limit_seconds
                Example: (2.0, 4.0) for random delay between 2-4 seconds
            max_retries: Maximum retry attempts (default: 3)
            timeout: Request timeout in seconds (default: 30)
            backoff_factor: Exponential backoff multiplier (default: 2)
        """
        self.rate_limit_seconds = rate_limit_seconds
        self.rate_limit_range = rate_limit_range
        self.max_retries = max_retries
        self.timeout = timeout
        self.backoff_factor = backoff_factor
        self._last_request_time = 0

    def fetch_page(self, url: str) -> BeautifulSoup:
        """Fetch and parse a page with rate limiting and retries.

        Args:
            url: URL to fetch

        Returns:
            BeautifulSoup object of parsed HTML

        Raises:
            requests.exceptions.RequestException: If request fails after retries
            requests.exceptions.Timeout: If request times out after retries
        """
        self._rate_limit()

        headers = {
            "User-Agent": "SimRacerScraper/1.0 (Educational purposes; +https://github.com/yourusername/simracer_scraper)"
        }

        attempt = 0
        last_exception = None

        while attempt <= self.max_retries:
            try:
                response = requests.get(url, headers=headers, timeout=self.timeout)
                response.raise_for_status()

                # Update last request time
                self._last_request_time = time.time()

                # Parse with BeautifulSoup
                return BeautifulSoup(response.text, "html.parser")

            except (
                requests.exceptions.RequestException,
                requests.exceptions.Timeout,
            ) as e:
                last_exception = e
                attempt += 1

                if attempt <= self.max_retries:
                    # Exponential backoff: 2^attempt * backoff_factor
                    delay = (self.backoff_factor**attempt) * self.backoff_factor
                    time.sleep(delay)
                else:
                    # Max retries exceeded, raise the last exception
                    raise

        # This should never be reached, but just in case
        if last_exception:
            raise last_exception
        raise requests.exceptions.RequestException("Unknown error during fetch")

    def _rate_limit(self):
        """Enforce rate limiting between requests.

        Sleeps if necessary to ensure minimum time between requests.
        Uses randomized delay if rate_limit_range is set, otherwise fixed delay.
        """
        # Determine delay: randomized or fixed
        if self.rate_limit_range:
            min_delay, max_delay = self.rate_limit_range
            delay = random.uniform(min_delay, max_delay)
        else:
            delay = self.rate_limit_seconds

        if delay <= 0:
            return

        elapsed = time.time() - self._last_request_time
        if elapsed < delay:
            time.sleep(delay - elapsed)

    def extract_text(self, soup: BeautifulSoup, selector: str) -> str | None:
        """Extract text from first matching element.

        Args:
            soup: BeautifulSoup object
            selector: CSS selector or tag name

        Returns:
            Stripped text content or None if not found

        Examples:
            >>> soup = BeautifulSoup('<h1>Title</h1>', 'html.parser')
            >>> extractor.extract_text(soup, 'h1')
            'Title'
        """
        element = soup.find(selector)
        if element:
            return element.get_text(strip=True)
        return None

    def extract_all_text(self, soup: BeautifulSoup, selector: str) -> list[str]:
        """Extract text from all matching elements.

        Args:
            soup: BeautifulSoup object
            selector: CSS selector or tag name

        Returns:
            List of stripped text content from all matching elements

        Examples:
            >>> soup = BeautifulSoup('<p>A</p><p>B</p>', 'html.parser')
            >>> extractor.extract_all_text(soup, 'p')
            ['A', 'B']
        """
        elements = soup.find_all(selector)
        return [elem.get_text(strip=True) for elem in elements]

    def __enter__(self):
        """Context manager entry."""
        return self

    def __exit__(self, exc_type, exc_val, exc_tb):
        """Context manager exit."""
        # No cleanup needed for now
        return False
