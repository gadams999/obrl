"""Simple end-to-end league scraper for demonstration."""

import re
import time
from datetime import datetime
from typing import Optional

import requests
from bs4 import BeautifulSoup

from .database import Database


class SimpleLeagueScraper:
    """Simple scraper that fetches and stores league data."""

    def __init__(self, db_path: str = "simracer.db", rate_limit: float = 2.0):
        """
        Initialize the scraper.

        Args:
            db_path: Path to SQLite database file
            rate_limit: Minimum seconds between requests
        """
        self.db_path = db_path
        self.rate_limit = rate_limit
        self.base_url = "https://www.simracerhub.com"
        self.last_request_time = 0

        # Initialize database schema if needed
        self._ensure_database_initialized()

    def _ensure_database_initialized(self):
        """Ensure database schema is initialized."""
        with Database(self.db_path) as db:
            db.initialize_schema()

    def _rate_limit_wait(self):
        """Wait if needed to respect rate limit."""
        elapsed = time.time() - self.last_request_time
        if elapsed < self.rate_limit:
            time.sleep(self.rate_limit - elapsed)
        self.last_request_time = time.time()

    def _fetch_page(self, url: str) -> str:
        """
        Fetch a page with rate limiting.

        Args:
            url: URL to fetch

        Returns:
            HTML content
        """
        self._rate_limit_wait()

        headers = {
            "User-Agent": "SimRacerScraper/0.1.0 (Educational; Contact: see README)"
        }

        response = requests.get(url, headers=headers, timeout=30)
        response.raise_for_status()

        return response.text

    def scrape_league(
        self, league_id: int, force: bool = False, validity_hours: int = 168
    ) -> dict:
        """
        Scrape a league and store in database.

        Args:
            league_id: League ID to scrape
            force: If True, re-scrape even if cached
            validity_hours: Hours before cache is stale (default: 168 = 1 week)

        Returns:
            Dictionary with scraped league data
        """
        url = f"{self.base_url}/league_series.php?league_id={league_id}"

        # Open database connection
        with Database(self.db_path) as db:
            # Check cache unless force refresh
            if not force:
                should_scrape, reason = db.should_scrape("league", league_id, validity_hours)
                if not should_scrape:
                    cached = db.get_league(league_id)
                    print(f"✓ League {league_id} found in cache ({reason})")
                    return cached
                elif reason != "not_in_cache":
                    print(f"⟳ Refreshing league {league_id}: {reason}")

            # Fetch the page
            print(f"Fetching {url}...")
            html = self._fetch_page(url)

            # Parse the page
            soup = BeautifulSoup(html, "lxml")

            # Extract league name from page title or header
            league_name = self._extract_league_name(soup)

            # Extract series data from JavaScript
            series_list = self._extract_series_from_js(html)

            # Prepare league data
            league_data = {
                "name": league_name,
                "url": url,
                "description": f"League with {len(series_list)} series",
                "scraped_at": datetime.now().isoformat(),
            }

            # Store league
            db.upsert_league(league_id, league_data)
            print(f"✓ Stored league: {league_name}")

            # Store series
            for series_data in series_list:
                series_id = series_data["series_id"]
                series_info = {
                    "name": series_data["name"],
                    "url": f"{self.base_url}/series_seasons.php?series_id={series_id}",
                    "description": series_data.get("description", ""),
                    "season_count": series_data.get("season_count"),
                    "created_date": series_data.get("created_date"),
                    "scraped_at": datetime.now().isoformat(),
                }
                db.upsert_series(series_id, league_id, series_info)
                print(f"  ✓ Stored series {series_id}: {series_data['name']}")

            # Return the stored league
            return db.get_league(league_id)

    def _extract_league_name(self, soup: BeautifulSoup) -> str:
        """
        Extract league name from page.

        Args:
            soup: BeautifulSoup object

        Returns:
            League name
        """
        # Try to find page title or main heading
        title_tag = soup.find("title")
        if title_tag:
            # Extract from title (usually "League Name - SimRacerHub")
            title_text = title_tag.get_text()
            if " - " in title_text:
                return title_text.split(" - ")[0].strip()
            return title_text.strip()

        # Fallback: try h1 or h2
        heading = soup.find(["h1", "h2"])
        if heading:
            return heading.get_text().strip()

        return "Unknown League"

    def _extract_series_from_js(self, html: str) -> list[dict]:
        """
        Extract series data from JavaScript arrays in the page.

        The page contains JavaScript like:
        series.push({id: 3714, name: "Wed Night", ...});

        Args:
            html: HTML content

        Returns:
            List of series dictionaries
        """
        series_list = []

        # Find all series.push() calls
        # Pattern: series.push({id: 3714, name: "...", ...});
        pattern = r'series\.push\(\{([^}]+)\}\)'

        matches = re.finditer(pattern, html, re.DOTALL)

        for match in matches:
            series_js = match.group(1)

            # Extract fields with simple regex
            series_data = {}

            # Extract id
            id_match = re.search(r'id\s*:\s*(\d+)', series_js)
            if id_match:
                series_data["series_id"] = int(id_match.group(1))

            # Extract name
            name_match = re.search(r'name\s*:\s*["\']([^"\']+)["\']', series_js)
            if name_match:
                series_data["name"] = name_match.group(1)

            # Extract description
            desc_match = re.search(r'description\s*:\s*["\']([^"\']*)["\']', series_js)
            if desc_match:
                series_data["description"] = desc_match.group(1)

            # Extract season_count
            count_match = re.search(r'season_count\s*:\s*(\d+)', series_js)
            if count_match:
                series_data["season_count"] = int(count_match.group(1))

            # Extract created_date
            date_match = re.search(r'created_date\s*:\s*["\']([^"\']+)["\']', series_js)
            if date_match:
                series_data["created_date"] = date_match.group(1)

            if "series_id" in series_data and "name" in series_data:
                series_list.append(series_data)

        return series_list

    def list_leagues(self) -> list[dict]:
        """
        List all leagues in database.

        Returns:
            List of league dictionaries
        """
        with Database(self.db_path) as db:
            cursor = db.conn.cursor()
            cursor.execute("SELECT * FROM leagues ORDER BY league_id")
            rows = cursor.fetchall()
            return [dict(row) for row in rows]

    def list_series(self, league_id: Optional[int] = None) -> list[dict]:
        """
        List series in database.

        Args:
            league_id: Optional league ID to filter by

        Returns:
            List of series dictionaries
        """
        with Database(self.db_path) as db:
            if league_id:
                return db.get_series_by_league(league_id)
            else:
                cursor = db.conn.cursor()
                cursor.execute("SELECT * FROM series ORDER BY series_id")
                rows = cursor.fetchall()
                return [dict(row) for row in rows]

    def stats(self) -> dict:
        """
        Get database statistics.

        Returns:
            Dictionary with counts
        """
        with Database(self.db_path) as db:
            cursor = db.conn.cursor()

            stats = {}

            # Count each entity type
            for table in ["leagues", "series", "seasons", "races", "drivers", "teams"]:
                cursor.execute(f"SELECT COUNT(*) FROM {table}")
                stats[table] = cursor.fetchone()[0]

            return stats
