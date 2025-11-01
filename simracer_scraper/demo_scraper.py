#!/usr/bin/env python3
"""Demo script to test the simple league scraper."""

import sys
from src.simple_scraper import SimpleLeagueScraper


def main():
    """Run the demo scraper."""
    if len(sys.argv) < 2:
        print("Usage:")
        print("  python demo_scraper.py scrape <league_id>  # Scrape a league")
        print("  python demo_scraper.py list                # List leagues")
        print("  python demo_scraper.py series [league_id]  # List series")
        print("  python demo_scraper.py stats               # Show stats")
        print()
        print("Example:")
        print("  python demo_scraper.py scrape 1558  # Scrape The OBRL")
        sys.exit(1)

    command = sys.argv[1]
    scraper = SimpleLeagueScraper(db_path="simracer_demo.db")

    if command == "scrape":
        if len(sys.argv) < 3:
            print("Error: league_id required")
            print("Usage: python demo_scraper.py scrape <league_id>")
            sys.exit(1)

        league_id = int(sys.argv[2])
        force = "--force" in sys.argv

        print(f"\n{'='*60}")
        print(f"Scraping league {league_id}...")
        print(f"{'='*60}\n")

        result = scraper.scrape_league(league_id, force=force)

        print(f"\n{'='*60}")
        print("League Data:")
        print(f"{'='*60}")
        for key, value in result.items():
            if key not in ["created_at", "updated_at"]:
                print(f"  {key}: {value}")

    elif command == "list":
        print(f"\n{'='*60}")
        print("Leagues in Database:")
        print(f"{'='*60}\n")

        leagues = scraper.list_leagues()
        if not leagues:
            print("  (No leagues found)")
        else:
            for league in leagues:
                print(f"  [{league['league_id']}] {league['name']}")
                print(f"       URL: {league['url']}")
                print(f"       Scraped: {league['scraped_at']}")
                print()

    elif command == "series":
        league_id = int(sys.argv[2]) if len(sys.argv) > 2 else None

        if league_id:
            print(f"\n{'='*60}")
            print(f"Series for League {league_id}:")
            print(f"{'='*60}\n")
        else:
            print(f"\n{'='*60}")
            print("All Series in Database:")
            print(f"{'='*60}\n")

        series_list = scraper.list_series(league_id)
        if not series_list:
            print("  (No series found)")
        else:
            for series in series_list:
                print(f"  [{series['series_id']}] {series['name']}")
                print(f"       League: {series['league_id']}")
                if series.get('season_count'):
                    print(f"       Seasons: {series['season_count']}")
                print(f"       URL: {series['url']}")
                print()

    elif command == "stats":
        print(f"\n{'='*60}")
        print("Database Statistics:")
        print(f"{'='*60}\n")

        stats = scraper.stats()
        for table, count in stats.items():
            print(f"  {table.capitalize()}: {count}")

    else:
        print(f"Unknown command: {command}")
        sys.exit(1)


if __name__ == "__main__":
    main()
