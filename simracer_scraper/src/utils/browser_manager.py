"""Shared browser and rate limiting manager.

CRITICAL: This module ensures rate limiting is enforced across ALL extractors,
not just within each individual extractor. Without this shared rate limiting,
multiple extractors could fire requests simultaneously, violating respectful
crawling behavior.
"""

import logging
import random
import threading
import time
import warnings

from playwright.sync_api import Browser, Playwright, sync_playwright

logger = logging.getLogger(__name__)

# Suppress Playwright cleanup warnings globally
# This prevents "Task was destroyed but it is pending!" messages when interrupted
warnings.filterwarnings("ignore", message="coroutine.*was never awaited", category=RuntimeWarning)
warnings.filterwarnings(
    "ignore",
    message="Enable tracemalloc to get the object allocation traceback",
    category=RuntimeWarning,
)


class BrowserManager:
    """Manages shared browser instance and coordinated rate limiting.

    This class solves two critical problems:

    1. **Async Loop Conflict**: Using one browser across multiple extractors
       prevents Playwright from detecting event loops and throwing async errors.

    2. **Shared Rate Limiting**: Ensures ALL extractors respect delays between
       requests, not just within individual extractors. Without this, a season
       extractor followed by 13 race extractors could fire requests rapidly,
       violating respectful crawling behavior.

    Example:
        >>> manager = BrowserManager(rate_limit_range=(2.0, 4.0))
        >>> browser = manager.get_browser()
        >>> manager.rate_limit()  # Enforces delay before request
        >>> # ... make request ...
        >>> manager.close()

    Thread Safety:
        All methods use threading.Lock to ensure thread-safe operation.
    """

    def __init__(self, rate_limit_range: tuple[float, float] = (2.0, 4.0)):
        """Initialize browser manager.

        Args:
            rate_limit_range: Tuple of (min_delay, max_delay) in seconds.
                             Random delay is chosen from this range for each request.
                             Default: (2.0, 4.0) for human-like browsing behavior.
        """
        self._playwright: Playwright | None = None
        self._browser: Browser | None = None
        self._last_request_time: float = 0
        self._rate_limit_range: tuple[float, float] = rate_limit_range
        self._lock: threading.Lock = threading.Lock()
        self._interrupted: bool = False  # Track if cleanup is due to interrupt

        logger.info(
            f"BrowserManager initialized with rate limit range: "
            f"{rate_limit_range[0]:.1f}-{rate_limit_range[1]:.1f}s"
        )

    def rate_limit(self) -> None:
        """Enforce rate limiting before making a request.

        This method MUST be called by ALL extractors before EVERY request.
        It ensures proper delays are enforced across different extractor instances.

        Thread-safe: Uses lock to coordinate across concurrent calls.

        Example:
            >>> manager.rate_limit()  # Blocks until enough time has elapsed
            >>> # Now safe to make request
        """
        with self._lock:
            min_delay, max_delay = self._rate_limit_range
            delay = random.uniform(min_delay, max_delay)

            elapsed = time.time() - self._last_request_time

            if elapsed < delay:
                sleep_time = delay - elapsed
                logger.info(f"⏱️  Rate limiting: sleeping {sleep_time:.2f}s before request")
                time.sleep(sleep_time)
            else:
                logger.info(
                    f"⏱️  Rate limiting: {elapsed:.2f}s elapsed (>= {delay:.2f}s target), no sleep needed"
                )

            self._last_request_time = time.time()

    def get_browser(self) -> Browser:
        """Get shared browser instance.

        Creates browser on first call, reuses it for subsequent calls.
        This ensures only ONE browser is used across all extractors,
        preventing async loop conflicts.

        Returns:
            Browser: Playwright browser instance (Chromium).

        Thread-safe: Uses lock to ensure only one browser is created.

        Example:
            >>> browser = manager.get_browser()
            >>> page = browser.new_page()
            >>> # ... use page ...
            >>> page.close()  # Close page, but NOT browser
        """
        with self._lock:
            if not self._browser:
                logger.info("Initializing shared Playwright browser (Chromium)")
                self._playwright = sync_playwright().start()
                self._browser = self._playwright.chromium.launch(headless=True)
                logger.info("Playwright browser initialized successfully")

            return self._browser

    def close(self, interrupted: bool = False) -> None:
        """Close browser and cleanup Playwright resources.

        Should be called when orchestrator exits to free resources.
        Handles cleanup gracefully even when operations are interrupted.

        Args:
            interrupted: If True, skip slow cleanup operations (set during Ctrl+C)

        Thread-safe: Uses lock to ensure clean shutdown.

        Example:
            >>> manager.close()
        """
        with self._lock:
            # Suppress asyncio error logging permanently
            # This prevents error messages during cleanup after Ctrl+C
            asyncio_logger = logging.getLogger("asyncio")
            asyncio_logger.setLevel(logging.CRITICAL)

            if self._browser:
                logger.info("Closing shared Playwright browser")

                if interrupted:
                    # During interrupt: Don't call close(), it hangs waiting for pending operations
                    # Just clear references and let process exit
                    logger.debug("Skipping browser.close() due to interrupt")
                    self._browser = None
                    self._playwright = None
                    return

                try:
                    self._browser.close()
                except (Exception, KeyboardInterrupt):
                    pass  # Ignore all errors during cleanup
                finally:
                    self._browser = None

            if self._playwright:
                logger.debug("Stopping Playwright")
                try:
                    self._playwright.stop()
                except (Exception, KeyboardInterrupt):
                    pass  # Ignore all errors during cleanup
                finally:
                    self._playwright = None

    def __enter__(self):
        """Context manager entry."""
        return self

    def __exit__(self, exc_type, exc_val, exc_tb):
        """Context manager exit - ensures cleanup."""
        self.close()
        return False
