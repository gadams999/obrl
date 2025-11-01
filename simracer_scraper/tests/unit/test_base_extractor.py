"""Tests for base extractor."""

import time

import pytest
import requests


def test_base_extractor_initialization():
    """Test BaseExtractor can be initialized."""
    try:
        from extractors.base import BaseExtractor
    except ImportError:
        from src.extractors.base import BaseExtractor

    extractor = BaseExtractor(rate_limit_seconds=2.0)

    assert extractor is not None
    assert extractor.rate_limit_seconds == 2.0
    assert extractor.max_retries == 3
    assert extractor.timeout == 30


def test_fetch_page_success(mocker):
    """Test successful page fetch."""
    try:
        from extractors.base import BaseExtractor
    except ImportError:
        from src.extractors.base import BaseExtractor

    # Mock requests.get
    mock_response = mocker.Mock()
    mock_response.text = "<html><body>Test Content</body></html>"
    mock_response.raise_for_status = mocker.Mock()
    mock_get = mocker.patch("requests.get", return_value=mock_response)

    extractor = BaseExtractor(rate_limit_seconds=0)  # No delay for tests
    soup = extractor.fetch_page("https://example.com/test")

    # Verify request was made
    mock_get.assert_called_once()
    assert "https://example.com/test" in str(mock_get.call_args)

    # Verify BeautifulSoup object returned
    assert soup is not None
    assert soup.find("body") is not None
    assert "Test Content" in soup.get_text()


def test_fetch_page_rate_limiting():
    """Test that rate limiting delays are enforced."""
    try:
        from extractors.base import BaseExtractor
    except ImportError:
        from src.extractors.base import BaseExtractor

    extractor = BaseExtractor(rate_limit_seconds=0.5)

    # Record time before first call
    start_time = time.time()

    # Make two calls (second should be delayed)
    extractor._last_request_time = time.time() - 0.1  # Recent request
    extractor._rate_limit()

    elapsed = time.time() - start_time

    # Should have waited ~0.4 seconds (0.5 - 0.1)
    assert elapsed >= 0.3  # Allow some tolerance


def test_fetch_page_retry_on_failure(mocker):
    """Test retry logic on failed requests."""
    try:
        from extractors.base import BaseExtractor
    except ImportError:
        from src.extractors.base import BaseExtractor

    # First two calls fail, third succeeds
    mock_response = mocker.Mock()
    mock_response.text = "<html><body>Success</body></html>"
    mock_response.raise_for_status = mocker.Mock()

    mock_get = mocker.patch("requests.get")
    mock_get.side_effect = [
        requests.exceptions.RequestException("Network error"),
        requests.exceptions.RequestException("Another error"),
        mock_response,
    ]

    # Mock sleep to avoid delays in tests
    mocker.patch("time.sleep")

    extractor = BaseExtractor(rate_limit_seconds=0, max_retries=3)
    soup = extractor.fetch_page("https://example.com/test")

    # Should have retried and eventually succeeded
    assert mock_get.call_count == 3
    assert soup is not None
    assert "Success" in soup.get_text()


def test_fetch_page_max_retries_exceeded(mocker):
    """Test that max retries raises exception."""
    try:
        from extractors.base import BaseExtractor
    except ImportError:
        from src.extractors.base import BaseExtractor

    # All calls fail
    mock_get = mocker.patch("requests.get")
    mock_get.side_effect = requests.exceptions.RequestException("Persistent error")

    # Mock sleep to avoid delays
    mocker.patch("time.sleep")

    extractor = BaseExtractor(rate_limit_seconds=0, max_retries=3)

    # Should raise after max retries
    with pytest.raises(requests.exceptions.RequestException):
        extractor.fetch_page("https://example.com/test")

    # Should have tried 4 times (initial + 3 retries)
    assert mock_get.call_count == 4


def test_fetch_page_timeout_handling(mocker):
    """Test timeout handling."""
    try:
        from extractors.base import BaseExtractor
    except ImportError:
        from src.extractors.base import BaseExtractor

    mock_get = mocker.patch("requests.get")
    mock_get.side_effect = requests.exceptions.Timeout("Timeout")

    mocker.patch("time.sleep")

    extractor = BaseExtractor(rate_limit_seconds=0, max_retries=2)

    with pytest.raises(requests.exceptions.Timeout):
        extractor.fetch_page("https://example.com/test")

    # Should have tried 3 times (initial + 2 retries)
    assert mock_get.call_count == 3


def test_extract_text_single_element():
    """Test extracting text from single element."""
    try:
        from extractors.base import BaseExtractor
    except ImportError:
        from src.extractors.base import BaseExtractor

    from bs4 import BeautifulSoup

    html = "<html><body><h1>Title</h1><p>Paragraph</p></body></html>"
    soup = BeautifulSoup(html, "html.parser")

    extractor = BaseExtractor()
    result = extractor.extract_text(soup, "h1")

    assert result == "Title"


def test_extract_text_not_found():
    """Test extract_text returns None when element not found."""
    try:
        from extractors.base import BaseExtractor
    except ImportError:
        from src.extractors.base import BaseExtractor

    from bs4 import BeautifulSoup

    html = "<html><body><p>No heading</p></body></html>"
    soup = BeautifulSoup(html, "html.parser")

    extractor = BaseExtractor()
    result = extractor.extract_text(soup, "h1")

    assert result is None


def test_extract_text_with_strip():
    """Test that extracted text is stripped of whitespace."""
    try:
        from extractors.base import BaseExtractor
    except ImportError:
        from src.extractors.base import BaseExtractor

    from bs4 import BeautifulSoup

    html = "<html><body><h1>  Whitespace  </h1></body></html>"
    soup = BeautifulSoup(html, "html.parser")

    extractor = BaseExtractor()
    result = extractor.extract_text(soup, "h1")

    assert result == "Whitespace"


def test_extract_all_text_multiple_elements():
    """Test extracting text from multiple elements."""
    try:
        from extractors.base import BaseExtractor
    except ImportError:
        from src.extractors.base import BaseExtractor

    from bs4 import BeautifulSoup

    html = "<html><body><p>First</p><p>Second</p><p>Third</p></body></html>"
    soup = BeautifulSoup(html, "html.parser")

    extractor = BaseExtractor()
    result = extractor.extract_all_text(soup, "p")

    assert len(result) == 3
    assert result == ["First", "Second", "Third"]


def test_extract_all_text_empty():
    """Test extract_all_text returns empty list when no elements found."""
    try:
        from extractors.base import BaseExtractor
    except ImportError:
        from src.extractors.base import BaseExtractor

    from bs4 import BeautifulSoup

    html = "<html><body><div>No paragraphs</div></body></html>"
    soup = BeautifulSoup(html, "html.parser")

    extractor = BaseExtractor()
    result = extractor.extract_all_text(soup, "p")

    assert result == []


def test_context_manager():
    """Test BaseExtractor works as context manager."""
    try:
        from extractors.base import BaseExtractor
    except ImportError:
        from src.extractors.base import BaseExtractor

    with BaseExtractor() as extractor:
        assert extractor is not None
        assert hasattr(extractor, "fetch_page")

    # Context manager should exit cleanly


def test_exponential_backoff_timing(mocker):
    """Test that retry delays use exponential backoff."""
    try:
        from extractors.base import BaseExtractor
    except ImportError:
        from src.extractors.base import BaseExtractor

    mock_get = mocker.patch("requests.get")
    mock_get.side_effect = requests.exceptions.RequestException("Error")

    mock_sleep = mocker.patch("time.sleep")

    extractor = BaseExtractor(rate_limit_seconds=0, max_retries=3, backoff_factor=2)

    with pytest.raises(requests.exceptions.RequestException):
        extractor.fetch_page("https://example.com/test")

    # Check that sleep was called with increasing delays
    # Exponential backoff: (2^attempt) * backoff_factor
    # First retry (attempt=1): 2^1 * 2 = 4
    # Second retry (attempt=2): 2^2 * 2 = 8
    # Third retry (attempt=3): 2^3 * 2 = 16
    assert mock_sleep.call_count == 3
    delays = [call[0][0] for call in mock_sleep.call_args_list]
    assert delays[0] == 4  # First retry
    assert delays[1] == 8  # Second retry
    assert delays[2] == 16  # Third retry


def test_custom_timeout():
    """Test custom timeout parameter."""
    try:
        from extractors.base import BaseExtractor
    except ImportError:
        from src.extractors.base import BaseExtractor

    extractor = BaseExtractor(timeout=60)

    assert extractor.timeout == 60


def test_user_agent_header(mocker):
    """Test that User-Agent header is set."""
    try:
        from extractors.base import BaseExtractor
    except ImportError:
        from src.extractors.base import BaseExtractor

    mock_response = mocker.Mock()
    mock_response.text = "<html></html>"
    mock_response.raise_for_status = mocker.Mock()
    mock_get = mocker.patch("requests.get", return_value=mock_response)

    extractor = BaseExtractor(rate_limit_seconds=0)
    extractor.fetch_page("https://example.com/test")

    # Check that User-Agent was set in headers
    call_kwargs = mock_get.call_args[1]
    assert "headers" in call_kwargs
    assert "User-Agent" in call_kwargs["headers"]
