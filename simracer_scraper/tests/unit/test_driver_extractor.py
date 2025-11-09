"""Tests for DriverExtractor."""

from pathlib import Path
from unittest.mock import patch

import pytest
from bs4 import BeautifulSoup

from src.extractors.driver import DriverExtractor


@pytest.fixture
def driver_extractor():
    """Create a DriverExtractor instance."""
    return DriverExtractor(rate_limit_seconds=0)


@pytest.fixture
def driver_html_with_stats():
    """Sample HTML with driver stats embedded in JavaScript."""
    # Note: Actual format has the stats in a specific order: irating, sr, license
    return """<!DOCTYPE html><html><head><title>Driver Stats</title></head><body><div id="driver_stats"></div><script>ReactDOM.createRoot(document.getElementById('driver_stats')).render(React.createElement(DriverStats,{user: {"driver_id":0,"site_admin":null},league_id: 0,series_id: 0,season_id: 0,rps: {"6376792":{"race_participant_id":"6376792","driver_id":"1071","irating":"3126","sr":"4.79","license":"Class A","race_date":"2025-11-06"}}}));</script></body></html>"""


@pytest.fixture
def driver_html_no_races():
    """Sample HTML for a driver with no race history."""
    return """
    <!DOCTYPE html>
    <html>
    <head><title>Driver Stats</title></head>
    <body>
        <div id="driver_stats"></div>
        <script>
        ReactDOM.createRoot(document.getElementById('driver_stats')).render(
            React.createElement(DriverStats, {
                user: {"driver_id":0,"site_admin":null},
                league_id: 0,
                series_id: 0,
                season_id: 0,
                rps: {}
            })
        );
        </script>
    </body>
    </html>
    """


class TestDriverExtractorBasic:
    """Test basic DriverExtractor functionality."""

    def test_init_default_params(self):
        """Test DriverExtractor initializes with default parameters."""
        extractor = DriverExtractor()
        assert extractor.rate_limit_seconds == 2.0
        assert extractor.render_js is False  # Drivers don't need JS rendering

    def test_inherits_from_base_extractor(self, driver_extractor):
        """Test DriverExtractor inherits BaseExtractor methods."""
        assert hasattr(driver_extractor, "fetch_page")


class TestDriverExtractorURLValidation:
    """Test URL validation."""

    def test_validate_url_valid(self, driver_extractor):
        """Test valid driver URL passes validation."""
        url = "https://www.simracerhub.com/driver_stats.php?driver_id=1071"
        # Should not raise
        driver_extractor._validate_url(url)

    def test_validate_url_invalid(self, driver_extractor):
        """Test invalid URL raises ValueError."""
        with pytest.raises(ValueError, match="Invalid driver profile URL"):
            driver_extractor._validate_url("https://example.com")

    def test_extract_driver_id_from_url(self, driver_extractor):
        """Test driver_id extraction from URL."""
        url = "https://www.simracerhub.com/driver_stats.php?driver_id=1071"
        driver_id = driver_extractor._extract_driver_id(url)
        assert driver_id == 1071

    def test_extract_driver_id_missing(self, driver_extractor):
        """Test extraction fails when driver_id missing from URL."""
        url = "https://www.simracerhub.com/driver_stats.php"
        with pytest.raises(ValueError, match="Could not extract driver_id"):
            driver_extractor._extract_driver_id(url)


class TestDriverExtractorExtraction:
    """Test data extraction from driver profile pages."""

    def test_extract_from_html_with_stats(self, driver_extractor, driver_html_with_stats):
        """Test extracting driver stats from HTML."""
        with patch.object(driver_extractor, "fetch_page") as mock_fetch:
            mock_fetch.return_value = BeautifulSoup(driver_html_with_stats, "html.parser")

            result = driver_extractor.extract(
                "https://www.simracerhub.com/driver_stats.php?driver_id=1071"
            )

            assert isinstance(result, dict)
            assert "metadata" in result

    def test_extract_metadata_structure(self, driver_extractor, driver_html_with_stats):
        """Test extracted metadata has correct structure."""
        with patch.object(driver_extractor, "fetch_page") as mock_fetch:
            mock_fetch.return_value = BeautifulSoup(driver_html_with_stats, "html.parser")

            result = driver_extractor.extract(
                "https://www.simracerhub.com/driver_stats.php?driver_id=1071"
            )

            metadata = result["metadata"]
            assert "driver_id" in metadata
            assert "url" in metadata
            assert "irating" in metadata
            assert "safety_rating" in metadata
            assert "license_class" in metadata

    def test_extract_driver_stats_values(self, driver_extractor, driver_html_with_stats):
        """Test extracted driver stats have correct values."""
        with patch.object(driver_extractor, "fetch_page") as mock_fetch:
            mock_fetch.return_value = BeautifulSoup(driver_html_with_stats, "html.parser")

            result = driver_extractor.extract(
                "https://www.simracerhub.com/driver_stats.php?driver_id=1071"
            )

            metadata = result["metadata"]
            assert metadata["driver_id"] == 1071
            assert metadata["irating"] == 3126
            assert metadata["safety_rating"] == 4.79
            assert metadata["license_class"] == "Class A"

    def test_extract_driver_no_races(self, driver_extractor, driver_html_no_races):
        """Test extraction when driver has no race history."""
        with patch.object(driver_extractor, "fetch_page") as mock_fetch:
            mock_fetch.return_value = BeautifulSoup(driver_html_no_races, "html.parser")

            result = driver_extractor.extract(
                "https://www.simracerhub.com/driver_stats.php?driver_id=9999"
            )

            metadata = result["metadata"]
            assert metadata["driver_id"] == 9999
            assert metadata["irating"] is None
            assert metadata["safety_rating"] is None
            assert metadata["license_class"] is None

    def test_extract_stats_method(self, driver_extractor, driver_html_with_stats):
        """Test _extract_stats method directly."""
        soup = BeautifulSoup(driver_html_with_stats, "html.parser")
        stats = driver_extractor._extract_stats(soup)

        assert stats["irating"] == 3126
        assert stats["safety_rating"] == 4.79
        assert stats["license_class"] == "Class A"

    def test_extract_stats_no_data(self, driver_extractor, driver_html_no_races):
        """Test _extract_stats returns None values when no data."""
        soup = BeautifulSoup(driver_html_no_races, "html.parser")
        stats = driver_extractor._extract_stats(soup)

        assert stats["irating"] is None
        assert stats["safety_rating"] is None
        assert stats["license_class"] is None
