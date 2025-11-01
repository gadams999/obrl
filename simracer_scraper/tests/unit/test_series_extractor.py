"""Tests for SeriesExtractor."""

from pathlib import Path
from unittest.mock import patch

import pytest
from bs4 import BeautifulSoup

from src.extractors.series import SeriesExtractor
from src.schema_validator import SchemaChangeDetected


@pytest.fixture
def series_fixture_path():
    """Path to the series seasons fixture HTML file."""
    fixtures_dir = Path(__file__).parent.parent / "fixtures"
    return fixtures_dir / "series_seasons_3714.html"


@pytest.fixture
def series_fixture_html(series_fixture_path):
    """Load the series seasons fixture HTML."""
    with open(series_fixture_path, encoding="utf-8") as f:
        return f.read()


@pytest.fixture
def series_extractor():
    """Create a SeriesExtractor instance."""
    return SeriesExtractor(rate_limit_seconds=0)  # No rate limiting in tests


class TestSeriesExtractorBasic:
    """Test basic SeriesExtractor functionality."""

    def test_init_default_params(self):
        """Test SeriesExtractor initializes with default parameters."""
        extractor = SeriesExtractor()

        assert extractor.rate_limit_seconds == 2.0
        assert extractor.max_retries == 3
        assert extractor.timeout == 30
        assert extractor.validator is not None

    def test_init_custom_params(self):
        """Test SeriesExtractor initializes with custom parameters."""
        extractor = SeriesExtractor(rate_limit_seconds=1.0, max_retries=5, timeout=60)

        assert extractor.rate_limit_seconds == 1.0
        assert extractor.max_retries == 5
        assert extractor.timeout == 60

    def test_inherits_from_base_extractor(self, series_extractor):
        """Test SeriesExtractor inherits BaseExtractor methods."""
        assert hasattr(series_extractor, "fetch_page")
        assert hasattr(series_extractor, "extract_text")
        assert hasattr(series_extractor, "extract_all_text")
        assert hasattr(series_extractor, "_rate_limit")


class TestSeriesExtractorExtraction:
    """Test data extraction from series pages."""

    def test_extract_from_fixture(self, series_extractor, series_fixture_html):
        """Test extracting series data from fixture."""
        with patch.object(series_extractor, "fetch_page") as mock_fetch:
            mock_fetch.return_value = BeautifulSoup(series_fixture_html, "html.parser")

            result = series_extractor.extract(
                "https://www.simracerhub.com/series_seasons.php?series_id=3714"
            )

            # Should return a dictionary
            assert isinstance(result, dict)
            assert "metadata" in result
            assert "child_urls" in result

    def test_extract_metadata_structure(self, series_extractor, series_fixture_html):
        """Test extracted metadata has correct structure."""
        with patch.object(series_extractor, "fetch_page") as mock_fetch:
            mock_fetch.return_value = BeautifulSoup(series_fixture_html, "html.parser")

            result = series_extractor.extract(
                "https://www.simracerhub.com/series_seasons.php?series_id=3714"
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

    def test_extract_series_id_from_url(self, series_extractor, series_fixture_html):
        """Test series_id is correctly extracted from URL."""
        with patch.object(series_extractor, "fetch_page") as mock_fetch:
            mock_fetch.return_value = BeautifulSoup(series_fixture_html, "html.parser")

            result = series_extractor.extract(
                "https://www.simracerhub.com/series_seasons.php?series_id=3714"
            )

            assert result["metadata"]["series_id"] == 3714

    def test_extract_series_name(self, series_extractor, series_fixture_html):
        """Test series name is correctly extracted from H1."""
        with patch.object(series_extractor, "fetch_page") as mock_fetch:
            mock_fetch.return_value = BeautifulSoup(series_fixture_html, "html.parser")

            result = series_extractor.extract(
                "https://www.simracerhub.com/series_seasons.php?series_id=3714"
            )

            name = result["metadata"]["name"]
            assert "OBRL Wednesday Night Series" in name
            assert isinstance(name, str)
            assert len(name) > 0

    def test_extract_seasons_data(self, series_extractor, series_fixture_html):
        """Test seasons data is extracted from JavaScript."""
        with patch.object(series_extractor, "fetch_page") as mock_fetch:
            mock_fetch.return_value = BeautifulSoup(series_fixture_html, "html.parser")

            result = series_extractor.extract(
                "https://www.simracerhub.com/series_seasons.php?series_id=3714"
            )

            seasons = result["child_urls"]["seasons"]

            # Should have 3 seasons from fixture
            assert len(seasons) == 3

            # Each should be a season URL dict with metadata
            for season in seasons:
                assert "url" in season
                assert "season_id" in season
                assert "name" in season

    def test_extract_season_ids_correct(self, series_extractor, series_fixture_html):
        """Test season IDs are correctly extracted."""
        with patch.object(series_extractor, "fetch_page") as mock_fetch:
            mock_fetch.return_value = BeautifulSoup(series_fixture_html, "html.parser")

            result = series_extractor.extract(
                "https://www.simracerhub.com/series_seasons.php?series_id=3714"
            )

            seasons = result["child_urls"]["seasons"]
            season_ids = [s["season_id"] for s in seasons]

            # Should match fixture season IDs
            assert 26741 in season_ids
            assert 26740 in season_ids
            assert 26739 in season_ids

    def test_extract_season_metadata(self, series_extractor, series_fixture_html):
        """Test season metadata is included."""
        with patch.object(series_extractor, "fetch_page") as mock_fetch:
            mock_fetch.return_value = BeautifulSoup(series_fixture_html, "html.parser")

            result = series_extractor.extract(
                "https://www.simracerhub.com/series_seasons.php?series_id=3714"
            )

            seasons = result["child_urls"]["seasons"]
            first_season = seasons[0]

            # Should have metadata from JavaScript
            assert "scheduled_races" in first_season
            assert "completed_races" in first_season
            assert "start_time" in first_season


class TestSeriesExtractorValidation:
    """Test schema validation integration."""

    def test_extract_validates_schema(self, series_extractor, series_fixture_html):
        """Test that extraction validates schema."""
        with patch.object(series_extractor, "fetch_page") as mock_fetch:
            mock_fetch.return_value = BeautifulSoup(series_fixture_html, "html.parser")

            with patch.object(
                series_extractor.validator, "validate_javascript_data"
            ) as mock_validate:
                series_extractor.extract(
                    "https://www.simracerhub.com/series_seasons.php?series_id=3714"
                )

                # Should have called validator
                mock_validate.assert_called_once()
                call_args = mock_validate.call_args
                assert call_args[0][0] == "series_seasons"

    def test_extract_raises_on_invalid_schema(self, series_extractor, series_fixture_html):
        """Test extraction raises SchemaChangeDetected on invalid schema."""
        with patch.object(series_extractor, "fetch_page") as mock_fetch:
            mock_fetch.return_value = BeautifulSoup(series_fixture_html, "html.parser")

            with patch.object(
                series_extractor.validator, "validate_javascript_data"
            ) as mock_validate:
                mock_validate.side_effect = SchemaChangeDetected("Test schema change")

                with pytest.raises(SchemaChangeDetected):
                    series_extractor.extract(
                        "https://www.simracerhub.com/series_seasons.php?series_id=3714"
                    )


class TestSeriesExtractorEdgeCases:
    """Test edge cases and error handling."""

    def test_extract_invalid_url_format(self, series_extractor):
        """Test extraction with invalid URL format."""
        with pytest.raises(ValueError):
            series_extractor.extract("https://www.simracerhub.com/invalid.php")

    def test_extract_missing_series_id(self, series_extractor):
        """Test extraction with missing series_id parameter."""
        with pytest.raises(ValueError):
            series_extractor.extract("https://www.simracerhub.com/series_seasons.php")

    def test_extract_no_seasons_found(self, series_extractor):
        """Test extraction when no seasons are found."""
        html_no_seasons = """
        <html>
        <head><title>Series</title></head>
        <body>
            <h1>Empty Series</h1>
            <script>
                var seasons = [];
                // Mock patterns for validation
                var id = 0; var n = "test"; var scrt = 0; var ns = 0; var nr = 0;
            </script>
        </body>
        </html>
        """

        with patch.object(series_extractor, "fetch_page") as mock_fetch:
            mock_fetch.return_value = BeautifulSoup(html_no_seasons, "html.parser")

            # Mock validator to allow this edge case
            with patch.object(series_extractor.validator, "validate_javascript_data"):
                result = series_extractor.extract(
                    "https://www.simracerhub.com/series_seasons.php?series_id=9999"
                )

                # Should return empty seasons list
                assert result["child_urls"]["seasons"] == []

    def test_extract_missing_series_name(self, series_extractor):
        """Test extraction when series name is missing."""
        html_no_name = """
        <html>
        <head><title>Series Page</title></head>
        <body>
            <script>
                var seasons = [{id: 100, n: "S1", scrt: 123, ns: 10, nr: 5}];
            </script>
        </body>
        </html>
        """

        with patch.object(series_extractor, "fetch_page") as mock_fetch:
            mock_fetch.return_value = BeautifulSoup(html_no_name, "html.parser")

            result = series_extractor.extract(
                "https://www.simracerhub.com/series_seasons.php?series_id=9999"
            )

            # Should have a fallback name
            assert "name" in result["metadata"]
            assert isinstance(result["metadata"]["name"], str)

    def test_extract_series_name_from_title(self, series_extractor):
        """Test series name extraction from title when no H1."""
        html = """
        <html>
        <head><title>Test Series Title</title></head>
        <body>
            <script>
                var seasons = [{id: 100, n: "S1", scrt: 123, ns: 10, nr: 5}];
            </script>
        </body>
        </html>
        """

        with patch.object(series_extractor, "fetch_page") as mock_fetch:
            mock_fetch.return_value = BeautifulSoup(html, "html.parser")

            result = series_extractor.extract(
                "https://www.simracerhub.com/series_seasons.php?series_id=100"
            )

            assert result["metadata"]["name"] == "Test Series Title"

    def test_extract_series_name_from_title_with_dash(self, series_extractor):
        """Test series name extraction from title with dash separator."""
        html = """
        <html>
        <head><title>My Series - Seasons</title></head>
        <body>
            <script>
                var seasons = [{id: 100, n: "S1", scrt: 123, ns: 10, nr: 5}];
            </script>
        </body>
        </html>
        """

        with patch.object(series_extractor, "fetch_page") as mock_fetch:
            mock_fetch.return_value = BeautifulSoup(html, "html.parser")

            result = series_extractor.extract(
                "https://www.simracerhub.com/series_seasons.php?series_id=100"
            )

            assert result["metadata"]["name"] == "My Series"

    def test_extract_series_name_fallback_unknown(self, series_extractor):
        """Test series name fallback to 'Unknown Series' when all else fails."""
        html = """
        <html>
        <head></head>
        <body>
            <script>
                var seasons = [{id: 100, n: "S1", scrt: 123, ns: 10, nr: 5}];
            </script>
        </body>
        </html>
        """

        with patch.object(series_extractor, "fetch_page") as mock_fetch:
            mock_fetch.return_value = BeautifulSoup(html, "html.parser")

            result = series_extractor.extract(
                "https://www.simracerhub.com/series_seasons.php?series_id=100"
            )

            assert result["metadata"]["name"] == "Unknown Series"


class TestSeriesExtractorContextManager:
    """Test context manager functionality."""

    def test_context_manager_works(self):
        """Test SeriesExtractor works as context manager."""
        with SeriesExtractor() as extractor:
            assert extractor is not None
            assert hasattr(extractor, "extract")

    def test_context_manager_cleanup(self):
        """Test context manager cleanup doesn't raise errors."""
        extractor = SeriesExtractor()
        extractor.__enter__()
        result = extractor.__exit__(None, None, None)
        # Should return False (don't suppress exceptions)
        assert result is False
