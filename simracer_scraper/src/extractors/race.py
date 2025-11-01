"""Race extractor for SimRacerHub race result pages.

This module extracts race metadata and results from race detail pages.
"""

import re
from typing import Any

from bs4 import BeautifulSoup

from .base import BaseExtractor


class RaceExtractor(BaseExtractor):
    """Extractor for race result pages.

    Extracts race metadata and results table from race pages.
    URL format: https://www.simracerhub.com/season_race.php?schedule_id={id}
    """

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
            Dictionary with race metadata
        """
        metadata = {
            "schedule_id": schedule_id,
            "url": url,
        }

        # Extract race name from H1
        name = self._extract_race_name(soup)
        metadata["name"] = name

        return metadata

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
        """
        try:
            result = {}

            # Position (column 0)
            result["position"] = int(cells[0].get_text(strip=True))

            # Driver name and ID (column 1)
            driver_cell = cells[1]
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

            # Car number (column 2)
            if len(cells) > 2:
                result["car_number"] = cells[2].get_text(strip=True)

            # Additional columns if present
            if len(cells) > 3:
                result["laps"] = cells[3].get_text(strip=True)
            if len(cells) > 4:
                result["interval"] = cells[4].get_text(strip=True)
            if len(cells) > 5:
                result["laps_led"] = cells[5].get_text(strip=True)
            if len(cells) > 6:
                result["points"] = cells[6].get_text(strip=True)

            return result

        except (ValueError, IndexError):
            return None
