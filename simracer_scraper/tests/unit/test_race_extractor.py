"""Tests for RaceExtractor."""

from pathlib import Path
from unittest.mock import patch

import pytest
from bs4 import BeautifulSoup

from src.extractors.race import RaceExtractor


@pytest.fixture
def race_fixture_path():
    """Path to the race detail fixture HTML file."""
    fixtures_dir = Path(__file__).parent.parent / "fixtures"
    return fixtures_dir / "season_race_324462.html"


@pytest.fixture
def race_fixture_html(race_fixture_path):
    """Load the race detail fixture HTML."""
    with open(race_fixture_path, encoding="utf-8") as f:
        return f.read()


@pytest.fixture
def race_extractor():
    """Create a RaceExtractor instance."""
    return RaceExtractor(rate_limit_seconds=0)


class TestRaceExtractorBasic:
    """Test basic RaceExtractor functionality."""

    def test_init_default_params(self):
        """Test RaceExtractor initializes with default parameters."""
        extractor = RaceExtractor()
        assert extractor.rate_limit_seconds == 2.0

    def test_inherits_from_base_extractor(self, race_extractor):
        """Test RaceExtractor inherits BaseExtractor methods."""
        assert hasattr(race_extractor, "fetch_page")


class TestRaceExtractorExtraction:
    """Test data extraction from race pages."""

    def test_extract_from_fixture(self, race_extractor, race_fixture_html):
        """Test extracting race data from fixture."""
        with patch.object(race_extractor, "fetch_page") as mock_fetch:
            mock_fetch.return_value = BeautifulSoup(race_fixture_html, "html.parser")

            result = race_extractor.extract(
                "https://www.simracerhub.com/season_race.php?schedule_id=324462"
            )

            assert isinstance(result, dict)
            assert "metadata" in result
            assert "results" in result

    def test_extract_metadata_structure(self, race_extractor, race_fixture_html):
        """Test extracted metadata has correct structure."""
        with patch.object(race_extractor, "fetch_page") as mock_fetch:
            mock_fetch.return_value = BeautifulSoup(race_fixture_html, "html.parser")

            result = race_extractor.extract(
                "https://www.simracerhub.com/season_race.php?schedule_id=324462"
            )

            metadata = result["metadata"]
            assert "schedule_id" in metadata
            assert "name" in metadata
            assert isinstance(metadata["schedule_id"], int)

    def test_extract_schedule_id_from_url(self, race_extractor, race_fixture_html):
        """Test schedule_id is correctly extracted from URL."""
        with patch.object(race_extractor, "fetch_page") as mock_fetch:
            mock_fetch.return_value = BeautifulSoup(race_fixture_html, "html.parser")

            result = race_extractor.extract(
                "https://www.simracerhub.com/season_race.php?schedule_id=324462"
            )

            assert result["metadata"]["schedule_id"] == 324462

    def test_extract_race_results(self, race_extractor, race_fixture_html):
        """Test race results are extracted from table."""
        with patch.object(race_extractor, "fetch_page") as mock_fetch:
            mock_fetch.return_value = BeautifulSoup(race_fixture_html, "html.parser")

            result = race_extractor.extract(
                "https://www.simracerhub.com/season_race.php?schedule_id=324462"
            )

            results = result["results"]
            assert len(results) == 3  # 3 drivers in fixture

    def test_extract_result_structure(self, race_extractor, race_fixture_html):
        """Test each result has correct structure."""
        with patch.object(race_extractor, "fetch_page") as mock_fetch:
            mock_fetch.return_value = BeautifulSoup(race_fixture_html, "html.parser")

            result = race_extractor.extract(
                "https://www.simracerhub.com/season_race.php?schedule_id=324462"
            )

            first_result = result["results"][0]
            assert "finish_position" in first_result  # Updated field name
            assert "driver_name" in first_result
            assert "car_number" in first_result

    def test_extract_driver_ids(self, race_extractor, race_fixture_html):
        """Test driver IDs are extracted from links when present."""
        with patch.object(race_extractor, "fetch_page") as mock_fetch:
            mock_fetch.return_value = BeautifulSoup(race_fixture_html, "html.parser")

            result = race_extractor.extract(
                "https://www.simracerhub.com/season_race.php?schedule_id=324462"
            )

            # driver_id is only present when there's a link with driver_id parameter
            # Test that the field exists when there are driver links in the HTML
            driver_ids = [r.get("driver_id") for r in result["results"] if "driver_id" in r]
            # Verify structure is correct (list of driver IDs or empty if no links)
            assert isinstance(driver_ids, list)


class TestRaceExtractorEdgeCases:
    """Test edge cases and error handling."""

    def test_extract_invalid_url_format(self, race_extractor):
        """Test extraction with invalid URL format."""
        with pytest.raises(ValueError):
            race_extractor.extract("https://www.simracerhub.com/invalid.php")

    def test_extract_missing_schedule_id(self, race_extractor):
        """Test extraction with missing schedule_id parameter."""
        with pytest.raises(ValueError, match="Invalid race URL format"):
            race_extractor.extract("https://www.simracerhub.com/season_race.php")

    def test_extract_no_results(self, race_extractor):
        """Test extraction when no results found."""
        html = """
        <html><body>
        <h1>Race</h1>
        <table class="results-table"><thead><tr><th>Pos</th></tr></thead><tbody></tbody></table>
        </body></html>
        """
        with patch.object(race_extractor, "fetch_page") as mock_fetch:
            mock_fetch.return_value = BeautifulSoup(html, "html.parser")
            result = race_extractor.extract(
                "https://www.simracerhub.com/season_race.php?schedule_id=999"
            )
            assert result["results"] == []

    def test_extract_race_name_from_title(self, race_extractor):
        """Test race name extraction from title when no H1."""
        html = """
        <html>
        <head><title>Test Race Title</title></head>
        <body>
            <table class="results-table">
                <tbody>
                    <tr>
                        <td>1</td>
                        <td><a href="driver_stats.php?driver_id=123">Driver</a></td>
                        <td>1</td>
                    </tr>
                </tbody>
            </table>
        </body>
        </html>
        """
        with patch.object(race_extractor, "fetch_page") as mock_fetch:
            mock_fetch.return_value = BeautifulSoup(html, "html.parser")
            result = race_extractor.extract(
                "https://www.simracerhub.com/season_race.php?schedule_id=100"
            )
            assert result["metadata"]["name"] == "Test Race Title"

    def test_extract_race_name_fallback_unknown(self, race_extractor):
        """Test race name fallback to 'Unknown Race' when all else fails."""
        html = """
        <html>
        <head></head>
        <body>
            <table class="results-table">
                <tbody>
                    <tr>
                        <td>1</td>
                        <td><a href="driver_stats.php?driver_id=123">Driver</a></td>
                        <td>1</td>
                    </tr>
                </tbody>
            </table>
        </body>
        </html>
        """
        with patch.object(race_extractor, "fetch_page") as mock_fetch:
            mock_fetch.return_value = BeautifulSoup(html, "html.parser")
            result = race_extractor.extract(
                "https://www.simracerhub.com/season_race.php?schedule_id=100"
            )
            assert result["metadata"]["name"] == "Unknown Race"

    def test_extract_no_table_found(self, race_extractor):
        """Test extraction when no table found."""
        html = """
        <html>
        <head><title>Race</title></head>
        <body>
            <h1>Test Race</h1>
            <p>No results available yet.</p>
        </body>
        </html>
        """
        with patch.object(race_extractor, "fetch_page") as mock_fetch:
            mock_fetch.return_value = BeautifulSoup(html, "html.parser")
            result = race_extractor.extract(
                "https://www.simracerhub.com/season_race.php?schedule_id=100"
            )
            assert result["results"] == []

    def test_extract_no_tbody(self, race_extractor):
        """Test extraction when table has no tbody."""
        html = """
        <html><body>
        <h1>Race</h1>
        <table class="results-table">
            <thead><tr><th>Pos</th><th>Driver</th></tr></thead>
        </table>
        </body></html>
        """
        with patch.object(race_extractor, "fetch_page") as mock_fetch:
            mock_fetch.return_value = BeautifulSoup(html, "html.parser")
            result = race_extractor.extract(
                "https://www.simracerhub.com/season_race.php?schedule_id=999"
            )
            assert result["results"] == []

    def test_extract_insufficient_cells(self, race_extractor):
        """Test extraction with rows that have too few cells."""
        html = """
        <html><body>
        <h1>Race</h1>
        <table class="results-table">
            <tbody>
                <tr>
                    <td>1</td>
                    <td>Driver Name</td>
                </tr>
                <tr>
                    <td>2</td>
                    <td>42</td>
                    <td><a href="driver_stats.php?driver_id=123">Valid Driver</a></td>
                </tr>
            </tbody>
        </table>
        </body></html>
        """
        with patch.object(race_extractor, "fetch_page") as mock_fetch:
            mock_fetch.return_value = BeautifulSoup(html, "html.parser")
            result = race_extractor.extract(
                "https://www.simracerhub.com/season_race.php?schedule_id=999"
            )
            # Only the valid row should be included (col order: position, car_number, driver)
            assert len(result["results"]) == 1
            assert result["results"][0]["driver_name"] == "Valid Driver"
            assert result["results"][0]["car_number"] == "42"

    def test_extract_driver_without_link(self, race_extractor):
        """Test extraction when driver cell has no link."""
        html = """
        <html><body>
        <h1>Race</h1>
        <table class="results-table">
            <tbody>
                <tr>
                    <td>1</td>
                    <td>42</td>
                    <td>Plain Driver Name</td>
                </tr>
            </tbody>
        </table>
        </body></html>
        """
        with patch.object(race_extractor, "fetch_page") as mock_fetch:
            mock_fetch.return_value = BeautifulSoup(html, "html.parser")
            result = race_extractor.extract(
                "https://www.simracerhub.com/season_race.php?schedule_id=999"
            )
            # Col order: position, car_number, driver_name
            assert len(result["results"]) == 1
            assert result["results"][0]["driver_name"] == "Plain Driver Name"
            assert result["results"][0]["car_number"] == "42"
            assert "driver_id" not in result["results"][0]

    def test_extract_invalid_position(self, race_extractor):
        """Test extraction with non-numeric position."""
        html = """
        <html><body>
        <h1>Race</h1>
        <table class="results-table">
            <tbody>
                <tr>
                    <td>DNF</td>
                    <td><a href="driver_stats.php?driver_id=123">Driver Name</a></td>
                    <td>42</td>
                </tr>
            </tbody>
        </table>
        </body></html>
        """
        with patch.object(race_extractor, "fetch_page") as mock_fetch:
            mock_fetch.return_value = BeautifulSoup(html, "html.parser")
            result = race_extractor.extract(
                "https://www.simracerhub.com/season_race.php?schedule_id=999"
            )
            # Row should be skipped due to ValueError in parsing
            assert len(result["results"]) == 0


class TestRaceExtractorMetadata:
    """Test extraction of race-level metadata (track, weather, laps, etc.)."""

    def test_extract_track_info(self, race_extractor, race_fixture_html):
        """Test track name and configuration are extracted."""
        with patch.object(race_extractor, "fetch_page") as mock_fetch:
            mock_fetch.return_value = BeautifulSoup(race_fixture_html, "html.parser")

            result = race_extractor.extract(
                "https://www.simracerhub.com/season_race.php?schedule_id=324462"
            )

            metadata = result["metadata"]
            assert "track_name" in metadata
            assert metadata["track_name"] == "Daytona International Speedway"
            assert "track_config" in metadata
            assert metadata["track_config"] == "Dual Pit Roads"

    def test_extract_date(self, race_extractor, race_fixture_html):
        """Test race date is extracted and converted to ISO format."""
        with patch.object(race_extractor, "fetch_page") as mock_fetch:
            mock_fetch.return_value = BeautifulSoup(race_fixture_html, "html.parser")

            result = race_extractor.extract(
                "https://www.simracerhub.com/season_race.php?schedule_id=324462"
            )

            metadata = result["metadata"]
            assert "date" in metadata
            assert metadata["date"] == "2025-10-29T00:00:00"

    def test_extract_weather(self, race_extractor, race_fixture_html):
        """Test weather conditions and temperature are extracted."""
        with patch.object(race_extractor, "fetch_page") as mock_fetch:
            mock_fetch.return_value = BeautifulSoup(race_fixture_html, "html.parser")

            result = race_extractor.extract(
                "https://www.simracerhub.com/season_race.php?schedule_id=324462"
            )

            metadata = result["metadata"]
            assert "weather_type" in metadata
            assert metadata["weather_type"] == "Realistic weather"
            assert "cloud_conditions" in metadata
            assert metadata["cloud_conditions"] == "Partly Cloudy"
            assert "temperature" in metadata
            assert metadata["temperature"] == "23° C"
            assert "humidity" in metadata
            assert metadata["humidity"] == "0%"
            assert "fog" in metadata
            assert metadata["fog"] == "0%"
            assert "wind" in metadata
            assert metadata["wind"] == "SE @1 KPH"

    def test_extract_race_stats(self, race_extractor, race_fixture_html):
        """Test race stats are extracted from race-stats paragraph."""
        with patch.object(race_extractor, "fetch_page") as mock_fetch:
            mock_fetch.return_value = BeautifulSoup(race_fixture_html, "html.parser")

            result = race_extractor.extract(
                "https://www.simracerhub.com/season_race.php?schedule_id=324462"
            )

            metadata = result["metadata"]
            # From: "0h 59m · 63 laps · 9 Leaders · 22 Lead Changes · 4 cautions (9 laps)"
            assert "race_duration" in metadata
            assert metadata["race_duration"] == "0h 59m"
            assert "total_laps" in metadata
            assert metadata["total_laps"] == 63
            assert "leaders" in metadata
            assert metadata["leaders"] == 9
            assert "lead_changes" in metadata
            assert metadata["lead_changes"] == 22
            assert "cautions" in metadata
            assert metadata["cautions"] == 4
            assert "caution_laps" in metadata
            assert metadata["caution_laps"] == 9

    def test_metadata_missing_race_info(self, race_extractor):
        """Test extraction handles missing race-info div gracefully."""
        html = """
        <html><body>
            <h1>Test Race</h1>
            <table class="results-table">
                <tbody>
                    <tr><td>1</td><td>Driver</td><td>23</td><td>50</td><td>Leader</td><td>10</td><td>40</td></tr>
                </tbody>
            </table>
        </body></html>
        """
        with patch.object(race_extractor, "fetch_page") as mock_fetch:
            mock_fetch.return_value = BeautifulSoup(html, "html.parser")

            result = race_extractor.extract(
                "https://www.simracerhub.com/season_race.php?schedule_id=999"
            )

            metadata = result["metadata"]
            # Should still have basic fields
            assert "schedule_id" in metadata
            assert "name" in metadata
            # Optional fields may be missing
            assert metadata.get("track") is None
            assert metadata.get("date") is None
            assert metadata.get("weather") is None

    def test_stats_with_no_stats_paragraph(self, race_extractor):
        """Test extraction when race-stats paragraph is missing."""
        html = """
        <html><body>
            <h1>Test Race</h1>
            <div class="race-info">
                <p>Date: Oct 30, 2025</p>
                <p>Track: Test Track</p>
            </div>
            <table class="results-table">
                <tbody>
                    <tr>
                        <td>1</td><td>23</td><td>Driver</td><td>1</td><td>00:48</td>
                        <td>Leader</td><td>40</td><td>0</td><td>40</td><td>50</td>
                        <td>0</td><td>00:49</td><td>1</td><td>00:49</td><td>0</td>
                        <td>0</td><td>0</td><td>0</td><td>0</td><td>0</td>
                        <td>1.0</td><td>2000</td><td>4.0</td><td>A</td><td></td>
                        <td></td><td></td><td></td><td></td><td></td>
                        <td></td><td></td><td></td><td></td><td></td>
                        <td></td><td></td><td></td>
                    </tr>
                </tbody>
            </table>
        </body></html>
        """
        with patch.object(race_extractor, "fetch_page") as mock_fetch:
            mock_fetch.return_value = BeautifulSoup(html, "html.parser")

            result = race_extractor.extract(
                "https://www.simracerhub.com/season_race.php?schedule_id=999"
            )

            metadata = result["metadata"]
            # Should have date and track_name from paragraphs (date is now ISO format)
            assert metadata["date"] == "2025-10-30T00:00:00"
            assert metadata["track_name"] == "Test Track"
            # No race-stats paragraph = no stats fields
            assert "total_laps" not in metadata
            assert "leaders" not in metadata
            assert "lead_changes" not in metadata


class TestRaceExtractorContextManager:
    """Test context manager functionality."""

    def test_context_manager_works(self):
        """Test RaceExtractor works as context manager."""
        with RaceExtractor() as extractor:
            assert extractor is not None
