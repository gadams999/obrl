"""Base scraper class with rate limiting and error handling."""

import time
import logging
from typing import Optional, Dict, Any
from pathlib import Path

import requests
from bs4 import BeautifulSoup


class BaseScraper:
    """Base scraper with rate limiting and common functionality."""

    def __init__(
        self,
        base_url: str,
        request_delay: float = 2.0,
        max_retries: int = 3,
        timeout: int = 10,
        user_agent: Optional[str] = None,
    ):
        """
        Initialize the base scraper.

        Args:
            base_url: Base URL for the website
            request_delay: Delay between requests in seconds
            max_retries: Maximum number of retries for failed requests
            timeout: Request timeout in seconds
            user_agent: Custom user agent string
        """
        self.base_url = base_url.rstrip("/")
        self.request_delay = request_delay
        self.max_retries = max_retries
        self.timeout = timeout
        self.last_request_time = 0

        self.session = requests.Session()
        self.session.headers.update({
            "User-Agent": user_agent or "SimRacerScraper/1.0 (Educational/Personal Use)",
            "Accept": "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8",
            "Accept-Language": "en-US,en;q=0.5",
            "Accept-Encoding": "gzip, deflate",
            "DNT": "1",
            "Connection": "keep-alive",
            "Upgrade-Insecure-Requests": "1",
        })

        self.logger = logging.getLogger(self.__class__.__name__)

    def _rate_limit(self):
        """Enforce rate limiting between requests."""
        elapsed = time.time() - self.last_request_time
        if elapsed < self.request_delay:
            sleep_time = self.request_delay - elapsed
            self.logger.debug(f"Rate limiting: sleeping for {sleep_time:.2f}s")
            time.sleep(sleep_time)
        self.last_request_time = time.time()

    def fetch_page(self, url: str, retries: int = 0) -> Optional[BeautifulSoup]:
        """
        Fetch and parse a page with rate limiting and retry logic.

        Args:
            url: URL to fetch (can be relative or absolute)
            retries: Current retry count

        Returns:
            BeautifulSoup object or None if fetch failed
        """
        # Make URL absolute if it's relative
        if not url.startswith("http"):
            url = f"{self.base_url}/{url.lstrip('/')}"

        self._rate_limit()

        try:
            self.logger.info(f"Fetching: {url}")
            response = self.session.get(url, timeout=self.timeout)
            response.raise_for_status()

            return BeautifulSoup(response.content, "lxml")

        except requests.exceptions.RequestException as e:
            self.logger.warning(f"Request failed for {url}: {e}")

            if retries < self.max_retries:
                self.logger.info(f"Retrying ({retries + 1}/{self.max_retries})...")
                time.sleep(self.request_delay * (retries + 1))
                return self.fetch_page(url, retries + 1)

            self.logger.error(f"Max retries reached for {url}")
            return None

    def extract_text(self, soup: BeautifulSoup, selector: str) -> Optional[str]:
        """
        Extract text from a CSS selector.

        Args:
            soup: BeautifulSoup object
            selector: CSS selector

        Returns:
            Extracted text or None
        """
        element = soup.select_one(selector)
        return element.get_text(strip=True) if element else None

    def extract_all_text(self, soup: BeautifulSoup, selector: str) -> list[str]:
        """
        Extract text from all elements matching a CSS selector.

        Args:
            soup: BeautifulSoup object
            selector: CSS selector

        Returns:
            List of extracted text
        """
        elements = soup.select(selector)
        return [el.get_text(strip=True) for el in elements]

    def close(self):
        """Close the session."""
        self.session.close()
        self.logger.info("Session closed")

    def __enter__(self):
        """Context manager entry."""
        return self

    def __exit__(self, exc_type, exc_val, exc_tb):
        """Context manager exit."""
        self.close()
