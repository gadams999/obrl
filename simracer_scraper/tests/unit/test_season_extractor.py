"""Tests for SeasonExtractor."""

from pathlib import Path
from unittest.mock import patch

import pytest
from bs4 import BeautifulSoup

from src.extractors.season import SeasonExtractor


@pytest.fixture
def season_fixture_path():
    """Path to the season race fixture HTML file."""
    fixtures_dir = Path(__file__).parent.parent / "fixtures"
    return fixtures_dir / "season_race_3714.html"


@pytest.fixture
def season_fixture_html(season_fixture_path):
    """Load the season race fixture HTML."""
    with open(season_fixture_path, encoding="utf-8") as f:
        return f.read()


@pytest.fixture
def season_extractor():
    """Create a SeasonExtractor instance."""
    return SeasonExtractor(rate_limit_seconds=0)  # No rate limiting in tests


class TestSeasonExtractorBasic:
    """Test basic SeasonExtractor functionality."""

    def test_init_default_params(self):
        """Test SeasonExtractor initializes with default parameters."""
        extractor = SeasonExtractor()

        assert extractor.rate_limit_seconds == 2.0
        assert extractor.max_retries == 3
        assert extractor.timeout == 30

    def test_init_custom_params(self):
        """Test SeasonExtractor initializes with custom parameters."""
        extractor = SeasonExtractor(rate_limit_seconds=1.0, max_retries=5, timeout=60)

        assert extractor.rate_limit_seconds == 1.0
        assert extractor.max_retries == 5
        assert extractor.timeout == 60

    def test_inherits_from_base_extractor(self, season_extractor):
        """Test SeasonExtractor inherits BaseExtractor methods."""
        assert hasattr(season_extractor, "fetch_page")
        assert hasattr(season_extractor, "extract_text")
        assert hasattr(season_extractor, "extract_all_text")
        assert hasattr(season_extractor, "_rate_limit")


class TestSeasonExtractorExtraction:
    """Test data extraction from season pages."""

    def test_extract_from_fixture(self, season_extractor, season_fixture_html):
        """Test extracting season data from fixture."""
        with patch.object(season_extractor, "fetch_page") as mock_fetch:
            mock_fetch.return_value = BeautifulSoup(season_fixture_html, "html.parser")

            result = season_extractor.extract(
                "https://www.simracerhub.com/season_race.php?series_id=3714"
            )

            # Should return a dictionary
            assert isinstance(result, dict)
            assert "metadata" in result
            assert "child_urls" in result

    def test_extract_metadata_structure(self, season_extractor, season_fixture_html):
        """Test extracted metadata has correct structure."""
        with patch.object(season_extractor, "fetch_page") as mock_fetch:
            mock_fetch.return_value = BeautifulSoup(season_fixture_html, "html.parser")

            result = season_extractor.extract(
                "https://www.simracerhub.com/season_race.php?series_id=3714"
            )

            metadata = result["metadata"]

            # Should have required fields
            assert "series_id" in metadata
            assert "name" in metadata
            assert "url" in metadata

            # Check data types
            assert isinstance(metadata["series_id"], int)
            assert isinstance(metadata["name"], str)
            assert isinstance(metadata["url"], str)

    def test_extract_series_id_from_url(self, season_extractor, season_fixture_html):
        """Test series_id is correctly extracted from URL."""
        with patch.object(season_extractor, "fetch_page") as mock_fetch:
            mock_fetch.return_value = BeautifulSoup(season_fixture_html, "html.parser")

            result = season_extractor.extract(
                "https://www.simracerhub.com/season_race.php?series_id=3714"
            )

            assert result["metadata"]["series_id"] == 3714

    def test_extract_season_name(self, season_extractor, season_fixture_html):
        """Test season name is correctly extracted from H1."""
        with patch.object(season_extractor, "fetch_page") as mock_fetch:
            mock_fetch.return_value = BeautifulSoup(season_fixture_html, "html.parser")

            result = season_extractor.extract(
                "https://www.simracerhub.com/season_race.php?series_id=3714"
            )

            name = result["metadata"]["name"]
            assert "2025 OBRL" in name
            assert isinstance(name, str)
            assert len(name) > 0

    def test_extract_race_urls(self, season_extractor, season_fixture_html):
        """Test race URLs are extracted from table."""
        with patch.object(season_extractor, "fetch_page") as mock_fetch:
            mock_fetch.return_value = BeautifulSoup(season_fixture_html, "html.parser")

            result = season_extractor.extract(
                "https://www.simracerhub.com/season_race.php?series_id=3714"
            )

            races = result["child_urls"]["races"]

            # Should have 4 races from fixture
            assert len(races) == 4

            # Each should be a race URL dict with metadata
            for race in races:
                assert "url" in race
                assert "schedule_id" in race
                assert "track" in race

    def test_extract_race_schedule_ids_correct(self, season_extractor, season_fixture_html):
        """Test race schedule IDs are correctly extracted."""
        with patch.object(season_extractor, "fetch_page") as mock_fetch:
            mock_fetch.return_value = BeautifulSoup(season_fixture_html, "html.parser")

            result = season_extractor.extract(
                "https://www.simracerhub.com/season_race.php?series_id=3714"
            )

            races = result["child_urls"]["races"]
            schedule_ids = [r["schedule_id"] for r in races]

            # Should match fixture schedule IDs
            assert 324460 in schedule_ids
            assert 324461 in schedule_ids
            assert 324462 in schedule_ids
            assert 324463 in schedule_ids

    def test_extract_race_metadata(self, season_extractor, season_fixture_html):
        """Test race metadata is included."""
        with patch.object(season_extractor, "fetch_page") as mock_fetch:
            mock_fetch.return_value = BeautifulSoup(season_fixture_html, "html.parser")

            result = season_extractor.extract(
                "https://www.simracerhub.com/season_race.php?series_id=3714"
            )

            races = result["child_urls"]["races"]
            first_race = races[0]

            # Should have metadata from table
            assert "track" in first_race
            assert "Daytona" in first_race["track"]


class TestSeasonExtractorEdgeCases:
    """Test edge cases and error handling."""

    def test_extract_invalid_url_format(self, season_extractor):
        """Test extraction with invalid URL format."""
        with pytest.raises(ValueError):
            season_extractor.extract("https://www.simracerhub.com/invalid.php")

    def test_extract_missing_series_id(self, season_extractor):
        """Test extraction with missing series_id parameter."""
        with pytest.raises(ValueError):
            season_extractor.extract("https://www.simracerhub.com/season_race.php")

    def test_extract_no_races_found(self, season_extractor):
        """Test extraction when no races are found."""
        html_no_races = """
        <html>
        <head><title>Season</title></head>
        <body>
            <h1>Empty Season</h1>
            <table class="schedule-table">
                <thead><tr><th>Race</th><th>Track</th></tr></thead>
                <tbody></tbody>
            </table>
        </body>
        </html>
        """

        with patch.object(season_extractor, "fetch_page") as mock_fetch:
            mock_fetch.return_value = BeautifulSoup(html_no_races, "html.parser")

            result = season_extractor.extract(
                "https://www.simracerhub.com/season_race.php?series_id=9999"
            )

            # Should return empty races list
            assert result["child_urls"]["races"] == []

    def test_extract_missing_season_name(self, season_extractor):
        """Test extraction when season name is missing."""
        html_no_name = """
        <html>
        <head><title>Season Page</title></head>
        <body>
            <table class="schedule-table">
                <thead><tr><th>Race</th><th>Track</th></tr></thead>
                <tbody>
                    <tr>
                        <td>Race 1</td>
                        <td><a href="season_race.php?schedule_id=100">Track</a></td>
                    </tr>
                </tbody>
            </table>
        </body>
        </html>
        """

        with patch.object(season_extractor, "fetch_page") as mock_fetch:
            mock_fetch.return_value = BeautifulSoup(html_no_name, "html.parser")

            result = season_extractor.extract(
                "https://www.simracerhub.com/season_race.php?series_id=9999"
            )

            # Should have a fallback name
            assert "name" in result["metadata"]
            assert isinstance(result["metadata"]["name"], str)

    def test_extract_season_name_from_title(self, season_extractor):
        """Test season name extraction from title when no H1."""
        html = """
        <html>
        <head><title>Test Season Title</title></head>
        <body>
            <table class="schedule-table">
                <thead><tr><th>Race</th><th>Track</th></tr></thead>
                <tbody>
                    <tr>
                        <td>Race 1</td>
                        <td><a href="season_race.php?schedule_id=100">Track</a></td>
                    </tr>
                </tbody>
            </table>
        </body>
        </html>
        """

        with patch.object(season_extractor, "fetch_page") as mock_fetch:
            mock_fetch.return_value = BeautifulSoup(html, "html.parser")

            result = season_extractor.extract(
                "https://www.simracerhub.com/season_race.php?series_id=100"
            )

            assert result["metadata"]["name"] == "Test Season Title"

    def test_extract_season_name_fallback_unknown(self, season_extractor):
        """Test season name fallback to 'Unknown Season' when all else fails."""
        html = """
        <html>
        <head></head>
        <body>
            <table class="schedule-table">
                <thead><tr><th>Race</th><th>Track</th></tr></thead>
                <tbody>
                    <tr>
                        <td>Race 1</td>
                        <td><a href="season_race.php?schedule_id=100">Track</a></td>
                    </tr>
                </tbody>
            </table>
        </body>
        </html>
        """

        with patch.object(season_extractor, "fetch_page") as mock_fetch:
            mock_fetch.return_value = BeautifulSoup(html, "html.parser")

            result = season_extractor.extract(
                "https://www.simracerhub.com/season_race.php?series_id=100"
            )

            assert result["metadata"]["name"] == "Unknown Season"

    def test_extract_season_name_from_title_with_dash(self, season_extractor):
        """Test season name extraction from title with dash separator."""
        html = """
        <html>
        <head><title>My Season - Race Schedule</title></head>
        <body>
            <table class="schedule-table">
                <thead><tr><th>Race</th><th>Track</th></tr></thead>
                <tbody>
                    <tr>
                        <td>Race 1</td>
                        <td><a href="season_race.php?schedule_id=100">Track</a></td>
                    </tr>
                </tbody>
            </table>
        </body>
        </html>
        """

        with patch.object(season_extractor, "fetch_page") as mock_fetch:
            mock_fetch.return_value = BeautifulSoup(html, "html.parser")

            result = season_extractor.extract(
                "https://www.simracerhub.com/season_race.php?series_id=100"
            )

            assert result["metadata"]["name"] == "My Season"

    def test_extract_races_with_absolute_url(self, season_extractor):
        """Test race extraction with absolute URL."""
        html = """
        <html>
        <head><title>Season</title></head>
        <body>
            <h1>Test Season</h1>
            <table class="schedule-table">
                <tbody>
                    <tr>
                        <td><a href="https://www.simracerhub.com/season_race.php?schedule_id=100">Track</a></td>
                    </tr>
                </tbody>
            </table>
        </body>
        </html>
        """

        with patch.object(season_extractor, "fetch_page") as mock_fetch:
            mock_fetch.return_value = BeautifulSoup(html, "html.parser")

            result = season_extractor.extract(
                "https://www.simracerhub.com/season_race.php?series_id=100"
            )

            races = result["child_urls"]["races"]
            assert len(races) == 1
            assert races[0]["url"].startswith("https://")

    def test_extract_races_no_table(self, season_extractor):
        """Test race extraction when no table found."""
        html = """
        <html>
        <head><title>Season</title></head>
        <body>
            <h1>Test Season</h1>
            <p>No races scheduled yet.</p>
        </body>
        </html>
        """

        with patch.object(season_extractor, "fetch_page") as mock_fetch:
            mock_fetch.return_value = BeautifulSoup(html, "html.parser")

            result = season_extractor.extract(
                "https://www.simracerhub.com/season_race.php?series_id=100"
            )

            # Should return empty races list
            assert result["child_urls"]["races"] == []


class TestSeasonExtractorContextManager:
    """Test context manager functionality."""

    def test_context_manager_works(self):
        """Test SeasonExtractor works as context manager."""
        with SeasonExtractor() as extractor:
            assert extractor is not None
            assert hasattr(extractor, "extract")

    def test_context_manager_cleanup(self):
        """Test context manager cleanup doesn't raise errors."""
        extractor = SeasonExtractor()
        extractor.__enter__()
        result = extractor.__exit__(None, None, None)
        # Should return False (don't suppress exceptions)
        assert result is False
