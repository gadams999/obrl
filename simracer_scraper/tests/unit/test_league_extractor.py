"""Tests for LeagueExtractor."""

from pathlib import Path
from unittest.mock import patch

import pytest
from bs4 import BeautifulSoup

from src.extractors.league import LeagueExtractor
from src.schema_validator import SchemaChangeDetected


@pytest.fixture
def league_fixture_path():
    """Path to the league series fixture HTML file."""
    fixtures_dir = Path(__file__).parent.parent / "fixtures"
    return fixtures_dir / "league_series_1558.html"


@pytest.fixture
def league_fixture_html(league_fixture_path):
    """Load the league series fixture HTML."""
    with open(league_fixture_path, encoding="utf-8") as f:
        return f.read()


@pytest.fixture
def league_extractor():
    """Create a LeagueExtractor instance."""
    return LeagueExtractor(rate_limit_seconds=0)  # No rate limiting in tests


@pytest.fixture
def mocked_fetch(league_fixture_html):
    """Mock the fetch_page method to return fixture HTML."""

    def _mock_fetch(url):
        return BeautifulSoup(league_fixture_html, "html.parser")

    return _mock_fetch


class TestLeagueExtractorBasic:
    """Test basic LeagueExtractor functionality."""

    def test_init_default_params(self):
        """Test LeagueExtractor initializes with default parameters."""
        extractor = LeagueExtractor()

        assert extractor.rate_limit_seconds == 2.0
        assert extractor.max_retries == 3
        assert extractor.timeout == 30
        assert extractor.validator is not None

    def test_init_custom_params(self):
        """Test LeagueExtractor initializes with custom parameters."""
        extractor = LeagueExtractor(rate_limit_seconds=1.0, max_retries=5, timeout=60)

        assert extractor.rate_limit_seconds == 1.0
        assert extractor.max_retries == 5
        assert extractor.timeout == 60

    def test_inherits_from_base_extractor(self, league_extractor):
        """Test LeagueExtractor inherits BaseExtractor methods."""
        # Should have BaseExtractor methods
        assert hasattr(league_extractor, "fetch_page")
        assert hasattr(league_extractor, "extract_text")
        assert hasattr(league_extractor, "extract_all_text")
        assert hasattr(league_extractor, "_rate_limit")


class TestLeagueExtractorExtraction:
    """Test data extraction from league pages."""

    def test_extract_from_fixture(self, league_extractor, league_fixture_html):
        """Test extracting league data from fixture."""
        # Mock fetch_page to return fixture
        with patch.object(league_extractor, "fetch_page") as mock_fetch:
            mock_fetch.return_value = BeautifulSoup(league_fixture_html, "html.parser")

            result = league_extractor.extract(
                "https://www.simracerhub.com/league_series.php?league_id=1558"
            )

            # Should return a dictionary
            assert isinstance(result, dict)

            # Should have metadata and child_urls
            assert "metadata" in result
            assert "child_urls" in result

    def test_extract_metadata_structure(self, league_extractor, league_fixture_html):
        """Test extracted metadata has correct structure."""
        with patch.object(league_extractor, "fetch_page") as mock_fetch:
            mock_fetch.return_value = BeautifulSoup(league_fixture_html, "html.parser")

            result = league_extractor.extract(
                "https://www.simracerhub.com/league_series.php?league_id=1558"
            )

            metadata = result["metadata"]

            # Should have required fields
            assert "league_id" in metadata
            assert "name" in metadata
            assert "url" in metadata

            # Check data types
            assert isinstance(metadata["league_id"], int)
            assert isinstance(metadata["name"], str)
            assert isinstance(metadata["url"], str)

    def test_extract_league_id_from_url(self, league_extractor, league_fixture_html):
        """Test league_id is correctly extracted from URL."""
        with patch.object(league_extractor, "fetch_page") as mock_fetch:
            mock_fetch.return_value = BeautifulSoup(league_fixture_html, "html.parser")

            result = league_extractor.extract(
                "https://www.simracerhub.com/league_series.php?league_id=1558"
            )

            assert result["metadata"]["league_id"] == 1558

    def test_extract_league_name(self, league_extractor, league_fixture_html):
        """Test league name is correctly extracted from H1."""
        with patch.object(league_extractor, "fetch_page") as mock_fetch:
            mock_fetch.return_value = BeautifulSoup(league_fixture_html, "html.parser")

            result = league_extractor.extract(
                "https://www.simracerhub.com/league_series.php?league_id=1558"
            )

            name = result["metadata"]["name"]
            assert "OBRL" in name
            assert isinstance(name, str)
            assert len(name) > 0

    def test_extract_series_urls(self, league_extractor, league_fixture_html):
        """Test series URLs are extracted from JavaScript."""
        with patch.object(league_extractor, "fetch_page") as mock_fetch:
            mock_fetch.return_value = BeautifulSoup(league_fixture_html, "html.parser")

            result = league_extractor.extract(
                "https://www.simracerhub.com/league_series.php?league_id=1558"
            )

            series_urls = result["child_urls"]["series"]

            # Should have 4 series from fixture
            assert len(series_urls) == 4

            # Each should be a valid series URL
            for url in series_urls:
                assert "series_id=" in url
                assert url.startswith("https://www.simracerhub.com/")

    def test_extract_series_ids_correct(self, league_extractor, league_fixture_html):
        """Test series IDs are correctly extracted."""
        with patch.object(league_extractor, "fetch_page") as mock_fetch:
            mock_fetch.return_value = BeautifulSoup(league_fixture_html, "html.parser")

            result = league_extractor.extract(
                "https://www.simracerhub.com/league_series.php?league_id=1558"
            )

            series_urls = result["child_urls"]["series"]

            # Extract IDs from URLs
            series_ids = [int(url.split("series_id=")[1].split("&")[0]) for url in series_urls]

            # Should match fixture series IDs
            assert 3714 in series_ids
            assert 3713 in series_ids
            assert 3712 in series_ids
            assert 3711 in series_ids

    def test_extract_teams_url_if_present(self, league_extractor, league_fixture_html):
        """Test teams URL is extracted if present."""
        with patch.object(league_extractor, "fetch_page") as mock_fetch:
            mock_fetch.return_value = BeautifulSoup(league_fixture_html, "html.parser")

            result = league_extractor.extract(
                "https://www.simracerhub.com/league_series.php?league_id=1558"
            )

            # Teams URL should be in child_urls
            if "teams" in result["child_urls"]:
                teams_url = result["child_urls"]["teams"]
                assert "teams.php" in teams_url
                assert "league_id=1558" in teams_url


class TestLeagueExtractorValidation:
    """Test schema validation integration."""

    def test_extract_validates_schema(self, league_extractor, league_fixture_html):
        """Test that extraction validates schema."""
        with patch.object(league_extractor, "fetch_page") as mock_fetch:
            mock_fetch.return_value = BeautifulSoup(league_fixture_html, "html.parser")

            # Mock validator to track calls
            with patch.object(
                league_extractor.validator, "validate_javascript_data"
            ) as mock_validate:
                league_extractor.extract(
                    "https://www.simracerhub.com/league_series.php?league_id=1558"
                )

                # Should have called validator
                mock_validate.assert_called_once()
                call_args = mock_validate.call_args
                assert call_args[0][0] == "league_series"

    def test_extract_raises_on_invalid_schema(self, league_extractor, league_fixture_html):
        """Test extraction raises SchemaChangeDetected on invalid schema."""
        with patch.object(league_extractor, "fetch_page") as mock_fetch:
            mock_fetch.return_value = BeautifulSoup(league_fixture_html, "html.parser")

            # Mock validator to raise exception
            with patch.object(
                league_extractor.validator, "validate_javascript_data"
            ) as mock_validate:
                mock_validate.side_effect = SchemaChangeDetected("Test schema change")

                with pytest.raises(SchemaChangeDetected):
                    league_extractor.extract(
                        "https://www.simracerhub.com/league_series.php?league_id=1558"
                    )

    def test_extract_validates_extracted_data(self, league_extractor, league_fixture_html):
        """Test that extracted data is validated."""
        with patch.object(league_extractor, "fetch_page") as mock_fetch:
            mock_fetch.return_value = BeautifulSoup(league_fixture_html, "html.parser")

            # Mock validator
            with patch.object(
                league_extractor.validator, "validate_extracted_data"
            ) as mock_validate_data:
                league_extractor.extract(
                    "https://www.simracerhub.com/league_series.php?league_id=1558"
                )

                # Should have validated extracted metadata
                mock_validate_data.assert_called_once()
                call_args = mock_validate_data.call_args
                assert call_args[0][0] == "league_series"
                assert isinstance(call_args[0][1], dict)


class TestLeagueExtractorEdgeCases:
    """Test edge cases and error handling."""

    def test_extract_invalid_url_format(self, league_extractor):
        """Test extraction with invalid URL format."""
        with pytest.raises(ValueError):
            league_extractor.extract("https://www.simracerhub.com/invalid.php")

    def test_extract_missing_league_id(self, league_extractor):
        """Test extraction with missing league_id parameter."""
        with pytest.raises(ValueError):
            league_extractor.extract("https://www.simracerhub.com/league_series.php")

    def test_extract_no_series_found(self, league_extractor):
        """Test extraction when no series are found.

        Note: This test mocks the validator since a page with no series
        would normally fail schema validation (which is the correct behavior).
        """
        html_no_series = """
        <html>
        <head><title>League</title></head>
        <body>
            <h1>Empty League</h1>
            <script>
                var series = [];
                // Has the pattern but no actual push calls
                var id = 0; var name = "test";
            </script>
        </body>
        </html>
        """

        with patch.object(league_extractor, "fetch_page") as mock_fetch:
            mock_fetch.return_value = BeautifulSoup(html_no_series, "html.parser")

            # Mock the validator to allow this edge case
            with patch.object(league_extractor.validator, "validate_javascript_data"):
                with patch.object(league_extractor.validator, "validate_extracted_data"):
                    result = league_extractor.extract(
                        "https://www.simracerhub.com/league_series.php?league_id=9999"
                    )

                    # Should return empty series list
                    assert result["child_urls"]["series"] == []

    def test_extract_missing_league_name(self, league_extractor):
        """Test extraction when league name is missing."""
        html_no_name = """
        <html>
        <head><title>League</title></head>
        <body>
            <script>
                var series = [];
                series.push({id: 100, name: "Test Series"});
            </script>
        </body>
        </html>
        """

        with patch.object(league_extractor, "fetch_page") as mock_fetch:
            mock_fetch.return_value = BeautifulSoup(html_no_name, "html.parser")

            result = league_extractor.extract(
                "https://www.simracerhub.com/league_series.php?league_id=9999"
            )

            # Should have a fallback name
            assert "name" in result["metadata"]
            # Could be page title or default
            assert isinstance(result["metadata"]["name"], str)


class TestLeagueExtractorEdgeCasesExtended:
    """Additional edge case tests for complete coverage."""

    def test_extract_league_name_from_title_with_empty_suffix(self, league_extractor):
        """Test league name extraction when title has colon but empty suffix."""
        html = """
        <html>
        <head><title>League: </title></head>
        <body>
            <script>
                var series = [];
                series.push({id: 100, name: "Test"});
            </script>
        </body>
        </html>
        """

        with patch.object(league_extractor, "fetch_page") as mock_fetch:
            mock_fetch.return_value = BeautifulSoup(html, "html.parser")

            result = league_extractor.extract(
                "https://www.simracerhub.com/league_series.php?league_id=100"
            )

            # Should fall back to full title text
            assert result["metadata"]["name"] == "League:"

    def test_extract_league_name_no_h1_uses_title(self, league_extractor):
        """Test league name uses title when no H1 present."""
        html = """
        <html>
        <head><title>Test League Title</title></head>
        <body>
            <script>
                var series = [];
                series.push({id: 100, name: "Test"});
            </script>
        </body>
        </html>
        """

        with patch.object(league_extractor, "fetch_page") as mock_fetch:
            mock_fetch.return_value = BeautifulSoup(html, "html.parser")

            result = league_extractor.extract(
                "https://www.simracerhub.com/league_series.php?league_id=100"
            )

            assert result["metadata"]["name"] == "Test League Title"

    def test_extract_description_p_tag_empty(self, league_extractor):
        """Test description extraction when p tag is empty."""
        html = """
        <html>
        <head><title>League</title></head>
        <body>
            <h1>Test League</h1>
            <p>   </p>
            <script>
                var series = [];
                series.push({id: 100, name: "Test"});
            </script>
        </body>
        </html>
        """

        with patch.object(league_extractor, "fetch_page") as mock_fetch:
            mock_fetch.return_value = BeautifulSoup(html, "html.parser")

            result = league_extractor.extract(
                "https://www.simracerhub.com/league_series.php?league_id=100"
            )

            # Description should be None (empty p tag ignored)
            assert "description" not in result["metadata"]

    def test_extract_teams_url_absolute_link(self, league_extractor):
        """Test teams URL extraction with absolute link."""
        html = """
        <html>
        <head><title>League</title></head>
        <body>
            <h1>Test League</h1>
            <a href="https://www.simracerhub.com/teams.php?league_id=100">Teams</a>
            <script>
                var series = [];
                series.push({id: 1, name: "Test"});
            </script>
        </body>
        </html>
        """

        with patch.object(league_extractor, "fetch_page") as mock_fetch:
            mock_fetch.return_value = BeautifulSoup(html, "html.parser")

            result = league_extractor.extract(
                "https://www.simracerhub.com/league_series.php?league_id=100"
            )

            # Should use the absolute URL from the link
            teams_url = result["child_urls"]["teams"]
            assert teams_url == "https://www.simracerhub.com/teams.php?league_id=100"

    def test_extract_teams_url_relative_link(self, league_extractor):
        """Test teams URL extraction with relative link."""
        html = """
        <html>
        <head><title>League</title></head>
        <body>
            <h1>Test League</h1>
            <a href="teams.php?league_id=100">Teams</a>
            <script>
                var series = [];
                series.push({id: 1, name: "Test"});
            </script>
        </body>
        </html>
        """

        with patch.object(league_extractor, "fetch_page") as mock_fetch:
            mock_fetch.return_value = BeautifulSoup(html, "html.parser")

            result = league_extractor.extract(
                "https://www.simracerhub.com/league_series.php?league_id=100"
            )

            # Should convert relative to absolute URL
            teams_url = result["child_urls"]["teams"]
            assert teams_url == "https://www.simracerhub.com/teams.php?league_id=100"

    def test_extract_league_name_fallback_unknown(self, league_extractor):
        """Test league name fallback to 'Unknown League' when all else fails."""
        html = """
        <html>
        <head></head>
        <body>
            <script>
                var series = [];
                series.push({id: 1, name: "Test"});
            </script>
        </body>
        </html>
        """

        with patch.object(league_extractor, "fetch_page") as mock_fetch:
            mock_fetch.return_value = BeautifulSoup(html, "html.parser")

            result = league_extractor.extract(
                "https://www.simracerhub.com/league_series.php?league_id=100"
            )

            # Should return default fallback
            assert result["metadata"]["name"] == "Unknown League"

    def test_extract_league_name_title_colon_with_content(self, league_extractor):
        """Test league name extraction from title with colon and content."""
        html = """
        <html>
        <head><title>Sim Racer Hub: My League</title></head>
        <body>
            <script>
                var series = [];
                series.push({id: 1, name: "Test"});
            </script>
        </body>
        </html>
        """

        with patch.object(league_extractor, "fetch_page") as mock_fetch:
            mock_fetch.return_value = BeautifulSoup(html, "html.parser")

            result = league_extractor.extract(
                "https://www.simracerhub.com/league_series.php?league_id=100"
            )

            # Should extract name after colon
            assert result["metadata"]["name"] == "My League"

    def test_extract_description_p_with_content(self, league_extractor):
        """Test description extraction from p tag with content."""
        html = """
        <html>
        <head><title>League</title></head>
        <body>
            <h1>Test League</h1>
            <p>This is a valid description.</p>
            <script>
                var series = [];
                series.push({id: 1, name: "Test"});
            </script>
        </body>
        </html>
        """

        with patch.object(league_extractor, "fetch_page") as mock_fetch:
            mock_fetch.return_value = BeautifulSoup(html, "html.parser")

            result = league_extractor.extract(
                "https://www.simracerhub.com/league_series.php?league_id=100"
            )

            # Should extract description
            assert result["metadata"]["description"] == "This is a valid description."


class TestLeagueExtractorContextManager:
    """Test context manager functionality."""

    def test_context_manager_works(self):
        """Test LeagueExtractor works as context manager."""
        with LeagueExtractor() as extractor:
            assert extractor is not None
            assert hasattr(extractor, "extract")

    def test_context_manager_cleanup(self):
        """Test context manager cleanup doesn't raise errors."""
        extractor = LeagueExtractor()
        extractor.__enter__()
        result = extractor.__exit__(None, None, None)
        # Should return False (don't suppress exceptions)
        assert result is False
