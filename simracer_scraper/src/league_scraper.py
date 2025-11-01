"""League-specific scraper for SimRacerHub."""

import json
import csv
import logging
from typing import Dict, List, Any, Optional
from pathlib import Path
from datetime import datetime

from bs4 import BeautifulSoup
from .base_scraper import BaseScraper


class LeagueScraper(BaseScraper):
    """Scraper for racing league data from SimRacerHub."""

    def __init__(self, config: Dict[str, Any]):
        """
        Initialize league scraper with configuration.

        Args:
            config: Configuration dictionary
        """
        scraping_config = config.get("scraping", {})
        super().__init__(
            base_url=config.get("base_url", "https://www.simracerhub.com"),
            request_delay=scraping_config.get("request_delay", 2.0),
            max_retries=scraping_config.get("max_retries", 3),
            timeout=scraping_config.get("timeout", 10),
            user_agent=scraping_config.get("user_agent"),
        )

        self.config = config
        self.output_config = config.get("output", {})
        self.output_dir = Path(self.output_config.get("output_dir", "./data"))
        self.output_dir.mkdir(parents=True, exist_ok=True)

    def scrape_page(self, url: str) -> Optional[Dict[str, Any]]:
        """
        Scrape a single page and extract relevant data.

        Args:
            url: URL to scrape

        Returns:
            Dictionary containing scraped data or None
        """
        soup = self.fetch_page(url)
        if not soup:
            return None

        # Basic page data
        data = {
            "url": url,
            "scraped_at": datetime.utcnow().isoformat(),
            "title": self.extract_text(soup, "title"),
        }

        # Extract any tables (common for standings/results)
        tables = self._extract_tables(soup)
        if tables:
            data["tables"] = tables

        # Extract league information if present
        league_info = self._extract_league_info(soup)
        if league_info:
            data["league_info"] = league_info

        return data

    def _extract_tables(self, soup: BeautifulSoup) -> List[Dict[str, Any]]:
        """
        Extract all tables from the page.

        Args:
            soup: BeautifulSoup object

        Returns:
            List of table data dictionaries
        """
        tables = []
        for table in soup.find_all("table"):
            table_data = {
                "headers": [],
                "rows": [],
            }

            # Extract headers
            header_row = table.find("thead")
            if header_row:
                headers = header_row.find_all(["th", "td"])
                table_data["headers"] = [h.get_text(strip=True) for h in headers]

            # Extract rows
            body = table.find("tbody") or table
            for row in body.find_all("tr"):
                cells = row.find_all(["td", "th"])
                row_data = [cell.get_text(strip=True) for cell in cells]
                if row_data:  # Skip empty rows
                    table_data["rows"].append(row_data)

            if table_data["rows"]:  # Only add if we found data
                tables.append(table_data)

        return tables

    def _extract_league_info(self, soup: BeautifulSoup) -> Optional[Dict[str, Any]]:
        """
        Extract league-specific information.

        Args:
            soup: BeautifulSoup object

        Returns:
            Dictionary with league info or None
        """
        # This will need to be customized based on actual page structure
        # For now, extract common elements
        info = {}

        # Look for common league info selectors
        league_name = self.extract_text(soup, "h1") or self.extract_text(soup, ".league-name")
        if league_name:
            info["name"] = league_name

        # Extract any metadata
        meta_elements = soup.find_all("meta")
        for meta in meta_elements:
            if meta.get("property") or meta.get("name"):
                key = meta.get("property") or meta.get("name")
                value = meta.get("content")
                if value:
                    info[f"meta_{key}"] = value

        return info if info else None

    def scrape_multiple_pages(self, urls: List[str]) -> List[Dict[str, Any]]:
        """
        Scrape multiple pages.

        Args:
            urls: List of URLs to scrape

        Returns:
            List of scraped data dictionaries
        """
        results = []
        for url in urls:
            self.logger.info(f"Scraping page {len(results) + 1}/{len(urls)}")
            data = self.scrape_page(url)
            if data:
                results.append(data)

        return results

    def save_results(self, data: List[Dict[str, Any]], filename: str = "results"):
        """
        Save scraped data to file(s).

        Args:
            data: List of data dictionaries to save
            filename: Base filename (without extension)
        """
        output_format = self.output_config.get("format", "json")

        if output_format in ["json", "both"]:
            self._save_json(data, filename)

        if output_format in ["csv", "both"]:
            self._save_csv(data, filename)

    def _save_json(self, data: List[Dict[str, Any]], filename: str):
        """Save data as JSON."""
        filepath = self.output_dir / f"{filename}.json"
        indent = self.output_config.get("json_indent", 2)

        with open(filepath, "w", encoding="utf-8") as f:
            json.dump(data, f, indent=indent, ensure_ascii=False)

        self.logger.info(f"Saved JSON to {filepath}")

    def _save_csv(self, data: List[Dict[str, Any]], filename: str):
        """Save data as CSV (flattened)."""
        if not data:
            return

        filepath = self.output_dir / f"{filename}.csv"

        # Flatten nested data for CSV
        flattened = []
        for item in data:
            flat_item = self._flatten_dict(item)
            flattened.append(flat_item)

        # Get all unique keys
        fieldnames = set()
        for item in flattened:
            fieldnames.update(item.keys())
        fieldnames = sorted(fieldnames)

        with open(filepath, "w", newline="", encoding="utf-8") as f:
            writer = csv.DictWriter(f, fieldnames=fieldnames)
            writer.writeheader()
            writer.writerows(flattened)

        self.logger.info(f"Saved CSV to {filepath}")

    def _flatten_dict(
        self, d: Dict[str, Any], parent_key: str = "", sep: str = "_"
    ) -> Dict[str, Any]:
        """Flatten nested dictionary."""
        items = []
        for k, v in d.items():
            new_key = f"{parent_key}{sep}{k}" if parent_key else k
            if isinstance(v, dict):
                items.extend(self._flatten_dict(v, new_key, sep=sep).items())
            elif isinstance(v, list):
                items.append((new_key, json.dumps(v)))
            else:
                items.append((new_key, v))
        return dict(items)
