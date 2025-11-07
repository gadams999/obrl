"""Race extractor for SimRacerHub race result pages.

This module extracts race metadata and results from race detail pages.
"""

import re
from typing import Any, TYPE_CHECKING

from bs4 import BeautifulSoup

from .base import BaseExtractor

if TYPE_CHECKING:
    from ..utils.browser_manager import BrowserManager


class RaceExtractor(BaseExtractor):
    """Extractor for race result pages.

    Extracts race metadata and results table from race pages.
    URL format: https://www.simracerhub.com/season_race.php?schedule_id={id}
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
        """Initialize the race extractor.

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
            render_js: Use JavaScript rendering (default: False)
            browser_manager: Shared browser manager for coordinated rate limiting
        """
        super().__init__(
            rate_limit_seconds,
            rate_limit_range,
            max_retries,
            timeout,
            backoff_factor,
            render_js,
            browser_manager,
        )

    def extract(self, url: str) -> dict[str, Any]:
        """Extract race data from a race detail page.

        Args:
            url: URL of the race detail page

        Returns:
            Dictionary with structure:
            {
                "metadata": {
                    "schedule_id": int,
                    "name": str,
                    "url": str,
                },
                "results": [
                    {
                        "position": int,
                        "driver_name": str,
                        "driver_id": int,
                        "car_number": str,
                        ...
                    },
                    ...
                ]
            }

        Raises:
            ValueError: If URL is invalid or missing schedule_id
            requests.exceptions.RequestException: If fetch fails
        """
        # Validate URL format
        self._validate_url(url)

        # Extract schedule_id from URL
        schedule_id = self._extract_schedule_id(url)

        # Fetch and parse the page
        soup = self.fetch_page(url)

        # Extract race metadata
        metadata = self._extract_metadata(soup, schedule_id, url)

        # Extract race results
        results = self._extract_results(soup)

        return {"metadata": metadata, "results": results}

    def _validate_url(self, url: str) -> None:
        """Validate that URL is a valid race detail URL.

        Args:
            url: URL to validate

        Raises:
            ValueError: If URL format is invalid
        """
        if not re.search(r"season_race\.php\?schedule_id=\d+", url):
            raise ValueError(
                f"Invalid race URL format. Expected season_race.php?schedule_id=<id>, got: {url}"
            )

    def _extract_schedule_id(self, url: str) -> int:
        """Extract schedule_id from URL.

        Args:
            url: Race detail URL

        Returns:
            Schedule ID as integer

        Raises:
            ValueError: If schedule_id not found in URL
        """
        match = re.search(r"schedule_id=(\d+)", url)
        if not match:
            raise ValueError(f"Could not extract schedule_id from URL: {url}")

        return int(match.group(1))

    def _extract_metadata(self, soup: BeautifulSoup, schedule_id: int, url: str) -> dict[str, Any]:
        """Extract race metadata from page.

        Args:
            soup: BeautifulSoup object of parsed page
            schedule_id: Schedule ID
            url: Original URL

        Returns:
            Dictionary with race metadata including:
            - schedule_id, url, name (required)
            - track, track_config, track_type, date, weather, temperature, humidity (optional)
        """
        metadata = {
            "schedule_id": schedule_id,
            "url": url,
        }

        # Extract race name from H1
        name = self._extract_race_name(soup)
        metadata["name"] = name

        # Extract race info from page (track, date, weather, etc.)
        race_info = self._extract_race_info(soup)
        metadata.update(race_info)

        return metadata

    def _extract_race_info(self, soup: BeautifulSoup) -> dict[str, Any]:
        """Extract race information from page.

        Parses race statistics and weather info from the session-details div.
        Real HTML structure:
        <div class="session-details">
            1h 11m · <span>140 laps</span> · <span>5 Leaders</span> · ... · <span>4 cautions (17 laps)</span>
            <br/>Realistic weather · <span>Clear</span> · <span>88° F</span> · ...
        </div>

        Args:
            soup: BeautifulSoup object

        Returns:
            Dictionary with race info fields (may be empty if not found)
        """
        info = {}

        # Extract track_name from span.track-name
        track_name_span = soup.find("span", class_="track-name")
        if track_name_span:
            info["track_name"] = track_name_span.get_text(strip=True)

        # Extract track_type and date from div.track-meta
        # Format: "Mar 16, 2022<span>·</span><i>Oval - 2008</i>"
        # or: "Mar 16, 2022<span>·</span><i>Road Course - 2008</i>"
        track_meta = soup.find("div", class_="track-meta")
        if track_meta:
            # Get full text to extract date
            meta_text = track_meta.get_text(separator=" ", strip=True)

            # Extract date from beginning
            date_match = re.search(r"([A-Za-z]{3}\s+\d{1,2},\s+\d{4})", meta_text)
            if date_match:
                from datetime import datetime

                date_str = date_match.group(1)
                try:
                    # Parse the date string and convert to ISO format
                    parsed_date = datetime.strptime(date_str, "%b %d, %Y")
                    info["date"] = parsed_date.isoformat()
                except ValueError:
                    # If parsing fails, store the raw string
                    info["date"] = date_str

            # Find the <i> tag which contains track type
            track_type_tag = track_meta.find("i")
            if track_type_tag:
                track_type_text = track_type_tag.get_text(strip=True)
                # Extract track type (before the dash and year if present)
                # E.g., "Oval - 2008" -> "Oval", "Road Course - 2008" -> "Road Course"
                if " - " in track_type_text:
                    track_type = track_type_text.split(" - ")[0].strip()
                else:
                    track_type = track_type_text.strip()
                if track_type:
                    info["track_type"] = track_type

        # Look for race-details div to extract date (keeping for fallback)
        race_details = soup.find("div", class_="race-details")
        if race_details:
            # Parse the race-details text
            # Expected format: "Oct 29, 2025 - Road Course" or "Date: Oct 29, 2025 - Road Course"
            details_text = race_details.get_text(separator=" ", strip=True)

            # Extract date (format: "Oct 29, 2025" or similar)
            date_match = re.search(r"([A-Za-z]{3}\s+\d{1,2},\s+\d{4})", details_text)
            if date_match:
                from datetime import datetime
                date_str = date_match.group(1)
                try:
                    # Parse the date string and convert to ISO format
                    parsed_date = datetime.strptime(date_str, "%b %d, %Y")
                    info["date"] = parsed_date.isoformat()
                except ValueError:
                    # If parsing fails, store the raw string
                    info["date"] = date_str

                # Extract track_type (after the date, usually after a dash or separator)
                # Look for patterns like "Road Course", "Oval", "Street Circuit", etc.
                after_date = details_text.split(date_str, 1)
                if len(after_date) > 1:
                    remaining = after_date[1].strip()
                    # Remove common separators
                    for sep in ["-", "·", "|", ":"]:
                        if remaining.startswith(sep):
                            remaining = remaining[1:].strip()
                            break

                    # Track type is typically the first word(s) after the separator
                    # Common patterns: "Road Course", "Oval", "Road", "Street Circuit"
                    if remaining:
                        # Try to extract just the track type (stop at next separator or newline)
                        track_type_match = re.match(r"^([^·\-\n\r]+)", remaining)
                        if track_type_match:
                            track_type = track_type_match.group(1).strip()
                            if track_type:
                                info["track_type"] = track_type
            else:
                # No date found, check if entire text might be track type
                # Look for common track type keywords
                if any(
                    keyword in details_text.lower()
                    for keyword in [
                        "road",
                        "oval",
                        "street",
                        "circuit",
                        "speedway",
                        "road course",
                        "street circuit",
                    ]
                ):
                    info["track_type"] = details_text.strip()

        # Look for session-details div (actual SimRacerHub structure)
        session_details = soup.find("div", class_="session-details")
        if not session_details:
            # Fallback to old race-info structure for test fixtures
            race_info_div = soup.find("div", class_="race-info")
            if not race_info_div:
                return info

            # Old structure: extract from p tags with classes
            race_stats = race_info_div.find("p", class_="race-stats")
            weather_info = race_info_div.find("p", class_="weather-info")

            if race_stats:
                stats_line = race_stats.get_text(strip=True)
            else:
                stats_line = None

            if weather_info:
                weather_line = weather_info.get_text(strip=True)
            else:
                weather_line = None
        else:
            # New structure: extract from session-details div with br separation
            # Split HTML on <br/> tag to separate race stats from weather
            html_str = str(session_details)
            parts = html_str.split("<br/>")

            # Extract text from each part
            if parts:
                stats_soup = BeautifulSoup(parts[0], "html.parser")
                stats_line = stats_soup.get_text(separator=" ", strip=True)
            else:
                stats_line = None

            if len(parts) > 1:
                weather_soup = BeautifulSoup(parts[1], "html.parser")
                weather_line = weather_soup.get_text(separator=" ", strip=True)
            else:
                weather_line = None

        # Process race statistics line
        if stats_line:
            # Parse format: "1h 11m · 140 laps · 5 Leaders · 9 Lead Changes · 4 cautions (17 laps)"
            parts = [p.strip() for p in stats_line.split("·")]

            for part in parts:
                # Duration: "1h 11m"
                if "h " in part and "m" in part:
                    info["race_duration"] = part

                # Total laps: "140 laps"
                elif part.endswith(" laps") and "cautions" not in part:
                    try:
                        info["total_laps"] = int(part.replace(" laps", "").strip())
                    except ValueError:
                        pass

                # Leaders: "5 Leaders"
                elif "Leaders" in part:
                    try:
                        info["leaders"] = int(part.replace("Leaders", "").strip())
                    except ValueError:
                        pass

                # Lead Changes: "9 Lead Changes"
                elif "Lead Changes" in part:
                    try:
                        info["lead_changes"] = int(part.replace("Lead Changes", "").strip())
                    except ValueError:
                        pass

                # Cautions: "4 cautions (17 laps)" or "0 cautions"
                elif "cautions" in part:
                    try:
                        # Extract cautions count and caution laps
                        if "(" in part:
                            caution_part, laps_part = part.split("(")
                            info["cautions"] = int(caution_part.replace("cautions", "").strip())
                            info["caution_laps"] = int(laps_part.replace("laps)", "").strip())
                        else:
                            caution_count = int(part.replace("cautions", "").strip())
                            info["cautions"] = caution_count
                            # If no caution laps specified, default to 0
                            info["caution_laps"] = 0
                    except ValueError:
                        pass

        # Process weather line
        if weather_line:
            # Parse format: "Realistic weather · Clear · 88° F · Humidity 55% · Fog 0% · Wind N @2 MPH"
            parts = [p.strip() for p in weather_line.split("·")]

            for part in parts:
                # Weather type: "Realistic weather"
                if "weather" in part.lower():
                    info["weather_type"] = part

                # Cloud conditions: "Clear", "Partly Cloudy", etc.
                elif any(word in part for word in ["Cloudy", "Clear", "Overcast", "Rain", "Storm"]):
                    info["cloud_conditions"] = part

                # Temperature: "88° F" or "23° C"
                elif "°" in part and ("C" in part or "F" in part):
                    info["temperature"] = part

                # Humidity: "Humidity 55%"
                elif "Humidity" in part:
                    info["humidity"] = part.replace("Humidity", "").strip()

                # Fog: "Fog 0%"
                elif "Fog" in part:
                    info["fog"] = part.replace("Fog", "").strip()

                # Wind: "Wind N @2 MPH"
                elif "Wind" in part:
                    info["wind"] = part.replace("Wind", "").strip()

        # Extract date and track from other elements (fallback for old structure)
        # Look for common patterns in the page
        all_text = soup.get_text()

        # Try to find date pattern (only if not already extracted from race-details)
        if "date" not in info:
            date_match = re.search(r"Date:\s*([A-Za-z]{3}\s+\d{1,2},\s+\d{4})", all_text)
            if date_match:
                from datetime import datetime
                date_str = date_match.group(1)
                try:
                    # Parse the date string and convert to ISO format
                    parsed_date = datetime.strptime(date_str, "%b %d, %Y")
                    info["date"] = parsed_date.isoformat()
                except ValueError:
                    # If parsing fails, store the raw string
                    info["date"] = date_str

        # Try to find track pattern (only if not already extracted from span.track-name)
        if "track_name" not in info:
            track_match = re.search(r"Track:\s*([^\n]+?)(?:\s*-\s*([^\n]+))?(?:\n|$)", all_text)
            if track_match:
                info["track_name"] = track_match.group(1).strip()
                if track_match.group(2):
                    info["track_config"] = track_match.group(2).strip()

        return info

    def _extract_race_name(self, soup: BeautifulSoup) -> str:
        """Extract race name from page.

        Args:
            soup: BeautifulSoup object

        Returns:
            Race name string
        """
        h1 = soup.find("h1")
        if h1:
            return h1.get_text(strip=True)

        title = soup.find("title")
        if title:
            return title.get_text(strip=True)

        return "Unknown Race"

    def _extract_results(self, soup: BeautifulSoup) -> list[dict[str, Any]]:
        """Extract race results from HTML table.

        Args:
            soup: BeautifulSoup object

        Returns:
            List of result dictionaries
        """
        results = []

        # Find the results table
        table = soup.find("table", class_="results-table")
        if not table:
            table = soup.find("table")

        if not table:
            return []

        # Find table body
        tbody = table.find("tbody")
        if not tbody:
            return []

        # Process each row
        rows = tbody.find_all("tr")
        for row in rows:
            cells = row.find_all("td")
            if len(cells) < 3:  # Need at least position, driver, car#
                continue

            result = self._parse_result_row(cells)
            if result:
                results.append(result)

        return results

    def _parse_result_row(self, cells: list) -> dict[str, Any] | None:
        """Parse a single result row.

        Args:
            cells: List of table cells

        Returns:
            Result dictionary or None if invalid

        Table structure (38 columns):
        0: FIN (finish position)
        1: CAR # (car number)
        2: DRIVER (driver name/link)
        3: ST (starting position)
        4: QUAL TIME (qualifying time)
        5: INT (interval)
        6: RACE PTS (race points)
        7: BNS PTS (bonus points)
        8: TOT PTS (total points)
        9: LAPS (laps completed)
        10: LAPS LED (laps led)
        11: FASTEST LAP (fastest lap time)
        12: FAST LAP # (fastest lap number)
        13: INC (incidents)
        14: AVG LAP (average lap time)
        15: STATUS (running/DNF/etc)
        16: CAR (car type)
        17: FAST LAPS (fast laps count)
        18: QUALITY PASSES
        19: CLOSING PASSES
        20: TOTAL PASSES
        21: ARP (average running position)
        22: DRIVER RATING (iRating)
        23: TEAM (team name)
        24-37: Duplicate/bonus columns (ignored)
        """
        try:
            result = {}

            # Helper function to safely get cell text
            def get_cell(index: int, default: str = "") -> str:
                if len(cells) > index:
                    text = cells[index].get_text(strip=True)
                    # Return None for empty strings, "-", or whitespace
                    if text and text != "-" and text.strip():
                        return text
                return default

            # Helper function to safely get integer
            def get_int(index: int) -> int | None:
                text = get_cell(index)
                if text and text != "":
                    try:
                        return int(text)
                    except ValueError:
                        return None
                return None

            # Helper function to safely get float
            def get_float(index: int) -> float | None:
                text = get_cell(index)
                if text and text != "":
                    try:
                        return float(text)
                    except ValueError:
                        return None
                return None

            # Required fields
            result["finish_position"] = int(cells[0].get_text(strip=True))
            result["car_number"] = get_cell(1, "0")

            # Driver name and ID (column 2)
            if len(cells) > 2:
                driver_cell = cells[2]
                driver_link = driver_cell.find("a")
                if driver_link:
                    result["driver_name"] = driver_link.get_text(strip=True)
                    # Extract driver_id from href
                    href = driver_link.get("href", "")
                    match = re.search(r"driver_id=(\d+)", href)
                    if match:
                        result["driver_id"] = int(match.group(1))
                else:
                    result["driver_name"] = driver_cell.get_text(strip=True)

            # Optional fields - only include if they have values
            starting_pos = get_int(3)
            if starting_pos is not None:
                result["starting_position"] = starting_pos

            qual_time = get_cell(4)
            if qual_time:
                result["qualifying_time"] = qual_time

            interval = get_cell(5)
            if interval:
                result["interval"] = interval

            race_points = get_int(6)
            if race_points is not None:
                result["race_points"] = race_points

            bonus_points = get_int(7)
            if bonus_points is not None:
                result["bonus_points"] = bonus_points

            total_points = get_int(8)
            if total_points is not None:
                result["total_points"] = total_points

            laps = get_int(9)
            if laps is not None:
                result["laps_completed"] = laps

            laps_led = get_int(10)
            if laps_led is not None:
                result["laps_led"] = laps_led

            fastest_lap = get_cell(11)
            if fastest_lap:
                result["fastest_lap"] = fastest_lap

            fastest_lap_num = get_int(12)
            if fastest_lap_num is not None:
                result["fastest_lap_number"] = fastest_lap_num

            incidents = get_int(13)
            if incidents is not None:
                result["incidents"] = incidents

            avg_lap = get_cell(14)
            if avg_lap:
                result["average_lap"] = avg_lap

            status = get_cell(15)
            if status:
                result["status"] = status

            car_type = get_cell(16)
            if car_type:
                result["car_type"] = car_type

            fast_laps = get_int(17)
            if fast_laps is not None:
                result["fast_laps"] = fast_laps

            quality_passes = get_int(18)
            if quality_passes is not None:
                result["quality_passes"] = quality_passes

            closing_passes = get_int(19)
            if closing_passes is not None:
                result["closing_passes"] = closing_passes

            total_passes = get_int(20)
            if total_passes is not None:
                result["total_passes"] = total_passes

            arp = get_float(21)
            if arp is not None:
                result["average_running_position"] = arp

            irating = get_float(22)
            if irating is not None:
                result["irating"] = int(irating)

            team = get_cell(23)
            if team:
                result["team"] = team

            return result

        except (ValueError, IndexError) as e:
            # Log the error for debugging but don't crash
            return None
