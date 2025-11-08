"""Base extractor with HTTP fetching and parsing utilities.

This module provides the BaseExtractor class that handles:
- HTTP requests with rate limiting
- Retry logic with exponential backoff
- HTML parsing with BeautifulSoup
- JavaScript rendering with Playwright (optional)
- Common extraction utilities
"""

import random
import time
from typing import TYPE_CHECKING

import requests
from bs4 import BeautifulSoup
from playwright.sync_api import sync_playwright

if TYPE_CHECKING:
    from ..utils.browser_manager import BrowserManager


class BaseExtractor:
    """Base class for extractors with HTTP fetching and parsing utilities.

    Provides rate-limited HTTP fetching, retry logic, and common extraction
    methods for parsing HTML content. Optionally supports JavaScript rendering
    with Playwright for dynamic content.

    Attributes:
        rate_limit_seconds: Minimum seconds between requests (for fixed delay)
        rate_limit_range: Tuple of (min, max) seconds for randomized delay
        max_retries: Maximum number of retry attempts
        timeout: Request timeout in seconds
        backoff_factor: Exponential backoff multiplier for retries
        render_js: Whether to use browser for JavaScript rendering
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
        """Initialize the base extractor.

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
            render_js: Use Playwright for JavaScript rendering (default: False)
                Set to True for pages that load content dynamically
            browser_manager: Shared browser manager for coordinated rate limiting
                and browser reuse. When provided, rate limiting is coordinated
                across ALL extractors to ensure respectful crawling behavior.
        """
        self.rate_limit_seconds = rate_limit_seconds
        self.rate_limit_range = rate_limit_range
        self.max_retries = max_retries
        self.timeout = timeout
        self.backoff_factor = backoff_factor
        self.render_js = render_js
        self._browser_manager = browser_manager
        self._last_request_time = 0  # Fallback for standalone use
        self._playwright = None
        self._browser = None

    def fetch_page(self, url: str) -> BeautifulSoup:
        """Fetch and parse a page with rate limiting and retries.

        Chooses between static HTTP fetching (fast) or browser rendering (slow)
        based on the render_js setting.

        Args:
            url: URL to fetch

        Returns:
            BeautifulSoup object of parsed HTML

        Raises:
            requests.exceptions.RequestException: If request fails after retries
            requests.exceptions.Timeout: If request times out after retries
            Exception: If browser rendering fails
        """
        if self.render_js:
            return self._fetch_with_browser(url)
        else:
            return self._fetch_static(url)

    def _fetch_static(self, url: str) -> BeautifulSoup:
        """Fetch page with static HTTP request (fast, no JavaScript).

        Args:
            url: URL to fetch

        Returns:
            BeautifulSoup object of parsed HTML

        Raises:
            requests.exceptions.RequestException: If request fails after retries
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

    def _fetch_with_browser(self, url: str) -> BeautifulSoup:
        """Fetch page with browser rendering (slow, executes JavaScript).

        Uses Playwright to render JavaScript before parsing. If browser_manager
        is provided, uses the shared browser instance. Otherwise, creates and
        manages its own browser for standalone use.

        Args:
            url: URL to fetch

        Returns:
            BeautifulSoup object of parsed HTML (after JS execution)

        Raises:
            Exception: If browser rendering fails
        """
        self._rate_limit()

        # Get browser: shared or standalone
        if self._browser_manager:
            # Use shared browser from manager (prevents async conflicts)
            browser = self._browser_manager.get_browser()
        else:
            # Fallback: use own browser (standalone extractor)
            if self._browser is None:
                self._init_browser()
            browser = self._browser

        attempt = 0
        last_exception = None

        while attempt <= self.max_retries:
            try:
                # Create a new page (tab) for this request
                page = browser.new_page()
                page.set_default_timeout(self.timeout * 1000)  # Playwright uses milliseconds

                # Navigate to URL and wait for network to be idle
                page.goto(url, wait_until="networkidle")

                # Additional wait for dynamic content - wait for tables to be present
                # This ensures JavaScript has time to populate the page
                try:
                    page.wait_for_selector("table", timeout=5000)
                except Exception:
                    # Table might not exist on all pages, continue anyway
                    pass

                # Get the rendered HTML
                html = page.content()

                # Close the page (but NOT the browser - it's shared or will be reused)
                page.close()

                # Update last request time (only for standalone fallback)
                if not self._browser_manager:
                    self._last_request_time = time.time()

                # Parse with BeautifulSoup
                return BeautifulSoup(html, "html.parser")

            except Exception as e:
                last_exception = e
                attempt += 1

                if attempt <= self.max_retries:
                    # Exponential backoff
                    delay = (self.backoff_factor**attempt) * self.backoff_factor
                    time.sleep(delay)
                else:
                    raise

        # This should never be reached
        if last_exception:
            raise last_exception
        raise Exception("Unknown error during browser fetch")

    def _init_browser(self):
        """Initialize Playwright browser (headless Chromium)."""
        self._playwright = sync_playwright().start()
        self._browser = self._playwright.chromium.launch(headless=True)

    def _close_browser(self):
        """Close browser and cleanup Playwright resources."""
        if self._browser:
            self._browser.close()
            self._browser = None
        if self._playwright:
            self._playwright.stop()
            self._playwright = None

    def _rate_limit(self):
        """Enforce rate limiting between requests.

        If browser_manager is provided, uses SHARED rate limiting coordinated
        across all extractors. Otherwise falls back to instance-specific rate
        limiting for standalone extractor use.

        CRITICAL: Shared rate limiting ensures that when multiple extractors
        run in sequence (e.g., season extractor followed by 13 race extractors),
        proper delays are enforced between ALL requests, not just within each
        individual extractor.
        """
        if self._browser_manager:
            # Use SHARED rate limiter - coordinates across all extractors
            self._browser_manager.rate_limit()
        else:
            # Fallback: instance-specific rate limiting (standalone use)
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
        """Context manager exit - cleanup browser resources."""
        self._close_browser()
        return False
