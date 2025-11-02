"""Tests for BrowserManager class.

This module tests the shared browser and rate limiting functionality
that ensures respectful crawling behavior across all extractors.
"""

import threading
import time
from unittest.mock import MagicMock, patch


def test_browser_manager_initialization():
    """Test BrowserManager can be initialized with default rate limit range."""
    try:
        from utils.browser_manager import BrowserManager
    except ImportError:
        from src.utils.browser_manager import BrowserManager

    manager = BrowserManager()

    assert manager is not None
    assert manager._rate_limit_range == (2.0, 4.0)
    assert manager._last_request_time == 0
    assert manager._browser is None
    assert manager._playwright is None


def test_browser_manager_custom_rate_limit():
    """Test BrowserManager accepts custom rate limit range."""
    try:
        from utils.browser_manager import BrowserManager
    except ImportError:
        from src.utils.browser_manager import BrowserManager

    manager = BrowserManager(rate_limit_range=(1.0, 3.0))

    assert manager._rate_limit_range == (1.0, 3.0)


def test_rate_limit_first_request():
    """Test rate limiting on first request (no previous request time)."""
    try:
        from utils.browser_manager import BrowserManager
    except ImportError:
        from src.utils.browser_manager import BrowserManager

    manager = BrowserManager(rate_limit_range=(0.1, 0.2))

    # First request should NOT sleep (no previous request time)
    start_time = time.time()
    manager.rate_limit()
    elapsed = time.time() - start_time

    # Should complete almost immediately (no sleep on first request)
    assert elapsed < 0.1  # Should be very fast
    # But should update last_request_time
    assert manager._last_request_time > 0


def test_rate_limit_subsequent_requests():
    """Test rate limiting enforces delays between requests."""
    try:
        from utils.browser_manager import BrowserManager
    except ImportError:
        from src.utils.browser_manager import BrowserManager

    manager = BrowserManager(rate_limit_range=(0.1, 0.2))

    # First request
    manager.rate_limit()
    first_request_time = time.time()

    # Immediate second request should be delayed
    manager.rate_limit()
    second_request_time = time.time()

    elapsed = second_request_time - first_request_time

    # Should be at least min_delay
    assert elapsed >= 0.1


def test_rate_limit_no_delay_if_enough_time_passed():
    """Test rate limiting doesn't sleep if enough time has already elapsed."""
    try:
        from utils.browser_manager import BrowserManager
    except ImportError:
        from src.utils.browser_manager import BrowserManager

    manager = BrowserManager(rate_limit_range=(0.1, 0.2))

    # First request
    manager.rate_limit()

    # Wait longer than max delay
    time.sleep(0.3)

    # Second request should not need additional delay
    start_time = time.time()
    manager.rate_limit()
    elapsed = time.time() - start_time

    # Should complete almost immediately (< 0.05s overhead)
    assert elapsed < 0.05


def test_rate_limit_thread_safety():
    """Test rate limiting is thread-safe with concurrent calls."""
    try:
        from utils.browser_manager import BrowserManager
    except ImportError:
        from src.utils.browser_manager import BrowserManager

    manager = BrowserManager(rate_limit_range=(0.05, 0.1))
    request_times = []

    def make_request():
        manager.rate_limit()
        request_times.append(time.time())

    # Launch 5 threads simultaneously
    threads = []
    for _ in range(5):
        t = threading.Thread(target=make_request)
        threads.append(t)
        t.start()

    # Wait for all threads to complete
    for t in threads:
        t.join()

    # Verify we got 5 request times
    assert len(request_times) == 5

    # Verify requests were properly spaced (not concurrent)
    request_times.sort()
    for i in range(1, len(request_times)):
        time_between = request_times[i] - request_times[i - 1]
        # Should be at least min_delay apart (with small tolerance)
        assert time_between >= 0.04  # Allow 0.01s tolerance


def test_get_browser_creates_browser():
    """Test get_browser creates a Playwright browser on first call."""
    try:
        from utils.browser_manager import BrowserManager
    except ImportError:
        from src.utils.browser_manager import BrowserManager

    manager = BrowserManager()

    # Mock Playwright
    mock_playwright = MagicMock()
    mock_browser = MagicMock()
    mock_chromium = MagicMock()
    mock_chromium.launch.return_value = mock_browser
    mock_playwright.chromium = mock_chromium

    with patch("src.utils.browser_manager.sync_playwright") as mock_sync_playwright:
        mock_sync_playwright.return_value.start.return_value = mock_playwright

        browser = manager.get_browser()

        # Verify Playwright was initialized
        mock_sync_playwright.return_value.start.assert_called_once()
        mock_chromium.launch.assert_called_once_with(headless=True)

        # Verify browser is returned
        assert browser is mock_browser
        assert manager._browser is mock_browser
        assert manager._playwright is mock_playwright


def test_get_browser_reuses_browser():
    """Test get_browser reuses existing browser on subsequent calls."""
    try:
        from utils.browser_manager import BrowserManager
    except ImportError:
        from src.utils.browser_manager import BrowserManager

    manager = BrowserManager()

    # Mock Playwright
    mock_playwright = MagicMock()
    mock_browser = MagicMock()
    mock_chromium = MagicMock()
    mock_chromium.launch.return_value = mock_browser
    mock_playwright.chromium = mock_chromium

    with patch("src.utils.browser_manager.sync_playwright") as mock_sync_playwright:
        mock_sync_playwright.return_value.start.return_value = mock_playwright

        # First call creates browser
        browser1 = manager.get_browser()

        # Second call should reuse same browser
        browser2 = manager.get_browser()

        # Verify Playwright was only initialized once
        assert mock_sync_playwright.return_value.start.call_count == 1
        assert mock_chromium.launch.call_count == 1

        # Verify same browser returned
        assert browser1 is browser2


def test_get_browser_thread_safety():
    """Test get_browser is thread-safe when called concurrently."""
    try:
        from utils.browser_manager import BrowserManager
    except ImportError:
        from src.utils.browser_manager import BrowserManager

    manager = BrowserManager()
    browsers = []

    # Mock Playwright
    mock_playwright = MagicMock()
    mock_browser = MagicMock()
    mock_chromium = MagicMock()
    mock_chromium.launch.return_value = mock_browser
    mock_playwright.chromium = mock_chromium

    with patch("src.utils.browser_manager.sync_playwright") as mock_sync_playwright:
        mock_sync_playwright.return_value.start.return_value = mock_playwright

        def get_browser():
            browsers.append(manager.get_browser())

        # Launch 5 threads simultaneously
        threads = []
        for _ in range(5):
            t = threading.Thread(target=get_browser)
            threads.append(t)
            t.start()

        # Wait for all threads to complete
        for t in threads:
            t.join()

        # Verify all threads got the same browser instance
        assert len(browsers) == 5
        assert all(b is mock_browser for b in browsers)

        # Verify Playwright was only initialized once (thread-safe)
        assert mock_sync_playwright.return_value.start.call_count == 1


def test_close_cleans_up_browser():
    """Test close() properly cleans up browser and Playwright resources."""
    try:
        from utils.browser_manager import BrowserManager
    except ImportError:
        from src.utils.browser_manager import BrowserManager

    manager = BrowserManager()

    # Mock Playwright
    mock_playwright = MagicMock()
    mock_browser = MagicMock()
    mock_chromium = MagicMock()
    mock_chromium.launch.return_value = mock_browser
    mock_playwright.chromium = mock_chromium

    with patch("src.utils.browser_manager.sync_playwright") as mock_sync_playwright:
        mock_sync_playwright.return_value.start.return_value = mock_playwright

        # Create browser
        manager.get_browser()

        # Close
        manager.close()

        # Verify cleanup
        mock_browser.close.assert_called_once()
        mock_playwright.stop.assert_called_once()

        # Verify internal state cleared
        assert manager._browser is None
        assert manager._playwright is None


def test_close_when_browser_not_initialized():
    """Test close() handles case where browser was never created."""
    try:
        from utils.browser_manager import BrowserManager
    except ImportError:
        from src.utils.browser_manager import BrowserManager

    manager = BrowserManager()

    # Should not raise exception
    manager.close()

    # Internal state should remain None
    assert manager._browser is None
    assert manager._playwright is None


def test_context_manager_enter():
    """Test BrowserManager can be used as context manager (enter)."""
    try:
        from utils.browser_manager import BrowserManager
    except ImportError:
        from src.utils.browser_manager import BrowserManager

    manager = BrowserManager()

    # __enter__ should return self
    result = manager.__enter__()
    assert result is manager


def test_context_manager_exit():
    """Test BrowserManager context manager properly cleans up on exit."""
    try:
        from utils.browser_manager import BrowserManager
    except ImportError:
        from src.utils.browser_manager import BrowserManager

    manager = BrowserManager()

    # Mock Playwright
    mock_playwright = MagicMock()
    mock_browser = MagicMock()
    mock_chromium = MagicMock()
    mock_chromium.launch.return_value = mock_browser
    mock_playwright.chromium = mock_chromium

    with patch("src.utils.browser_manager.sync_playwright") as mock_sync_playwright:
        mock_sync_playwright.return_value.start.return_value = mock_playwright

        # Create browser
        manager.get_browser()

        # Call __exit__
        result = manager.__exit__(None, None, None)

        # Verify cleanup
        mock_browser.close.assert_called_once()
        mock_playwright.stop.assert_called_once()

        # Verify __exit__ returns False (don't suppress exceptions)
        assert result is False


def test_context_manager_integration():
    """Test BrowserManager works correctly with 'with' statement."""
    try:
        from utils.browser_manager import BrowserManager
    except ImportError:
        from src.utils.browser_manager import BrowserManager

    # Mock Playwright
    mock_playwright = MagicMock()
    mock_browser = MagicMock()
    mock_chromium = MagicMock()
    mock_chromium.launch.return_value = mock_browser
    mock_playwright.chromium = mock_chromium

    with patch("src.utils.browser_manager.sync_playwright") as mock_sync_playwright:
        mock_sync_playwright.return_value.start.return_value = mock_playwright

        with BrowserManager() as manager:
            # Get browser inside context
            browser = manager.get_browser()
            assert browser is mock_browser

        # After exiting context, verify cleanup was called
        mock_browser.close.assert_called_once()
        mock_playwright.stop.assert_called_once()


def test_rate_limit_randomization():
    """Test rate limiting uses random delays within range."""
    try:
        from utils.browser_manager import BrowserManager
    except ImportError:
        from src.utils.browser_manager import BrowserManager

    manager = BrowserManager(rate_limit_range=(0.1, 0.3))

    # Collect multiple delay times (need to make 2 calls each time to measure delay)
    delays = []
    for _ in range(10):
        # Reset to measure fresh delay
        manager._last_request_time = 0

        # First call sets the baseline (no sleep)
        manager.rate_limit()

        # Second call will sleep - measure this
        start_time = time.time()
        manager.rate_limit()
        elapsed = time.time() - start_time
        delays.append(elapsed)

    # Verify all delays are within range (with tolerance)
    for delay in delays:
        assert 0.09 <= delay <= 0.35  # Allow 0.05s overhead

    # Verify delays are different (not all the same)
    # At least 5 out of 10 should be different
    unique_delays = len({round(d, 2) for d in delays})
    assert unique_delays >= 5


def test_rate_limit_updates_last_request_time():
    """Test rate_limit() properly updates _last_request_time."""
    try:
        from utils.browser_manager import BrowserManager
    except ImportError:
        from src.utils.browser_manager import BrowserManager

    manager = BrowserManager(rate_limit_range=(0.05, 0.1))

    # Initial state
    assert manager._last_request_time == 0

    # First call should update time
    manager.rate_limit()
    first_time = manager._last_request_time
    assert first_time > 0

    # Second call should update to new time
    time.sleep(0.15)
    manager.rate_limit()
    second_time = manager._last_request_time
    assert second_time > first_time


def test_multiple_managers_independent():
    """Test multiple BrowserManager instances are independent."""
    try:
        from utils.browser_manager import BrowserManager
    except ImportError:
        from src.utils.browser_manager import BrowserManager

    manager1 = BrowserManager(rate_limit_range=(0.1, 0.2))
    manager2 = BrowserManager(rate_limit_range=(0.2, 0.3))

    # Different rate limit ranges
    assert manager1._rate_limit_range != manager2._rate_limit_range

    # Different locks
    assert manager1._lock is not manager2._lock

    # Independent last_request_time
    manager1.rate_limit()
    assert manager1._last_request_time > 0
    assert manager2._last_request_time == 0


def test_rate_limit_with_zero_range():
    """Test rate limiting with zero-delay range (for testing)."""
    try:
        from utils.browser_manager import BrowserManager
    except ImportError:
        from src.utils.browser_manager import BrowserManager

    manager = BrowserManager(rate_limit_range=(0.0, 0.0))

    # Should complete almost immediately
    start_time = time.time()
    manager.rate_limit()
    elapsed = time.time() - start_time

    # Should be very fast (< 0.01s)
    assert elapsed < 0.01
