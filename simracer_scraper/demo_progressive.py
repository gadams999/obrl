#!/usr/bin/env python3
"""Demo script showing progressive scraping with smart cache."""

from src.database import Database


def demo_cache_logic():
    """Demonstrate the should_scrape() logic."""
    print("=" * 70)
    print("Progressive Scraping Demo - Cache Intelligence")
    print("=" * 70)
    print()

    with Database("simracer_demo.db") as db:
        # Check league (should be fresh)
        should_scrape, reason = db.should_scrape("league", 1558, validity_hours=168)
        print(f"League 1558:")
        print(f"  Should scrape? {should_scrape}")
        print(f"  Reason: {reason}")
        print()

        # Check with different validity periods
        print("Testing different validity periods for league 1558:")
        for hours in [1, 24, 168, None]:
            should_scrape, reason = db.should_scrape("league", 1558, validity_hours=hours)
            validity_str = f"{hours}h" if hours else "indefinite"
            status = "SCRAPE" if should_scrape else "SKIP"
            print(f"  {validity_str:>12}: [{status:>6}] {reason}")
        print()

        # Check non-existent entity
        should_scrape, reason = db.should_scrape("league", 99999, validity_hours=24)
        print(f"Non-existent league 99999:")
        print(f"  Should scrape? {should_scrape}")
        print(f"  Reason: {reason}")
        print()

        # Check series
        print("Series for league 1558:")
        series_list = db.get_series_by_league(1558)
        for series in series_list[:2]:  # Just show first 2
            should_scrape, reason = db.should_scrape(
                "series", series["series_id"], validity_hours=168
            )
            status = "SCRAPE" if should_scrape else "SKIP"
            print(f"  [{series['series_id']}] {series['name']:40} [{status:>6}] {reason}")

    print()
    print("=" * 70)
    print("Key Insights:")
    print("=" * 70)
    print("1. Fresh data (< validity period) is SKIPPED - uses cache")
    print("2. Stale data (> validity period) is SCRAPED - refreshes")
    print("3. Missing data (not in DB) is SCRAPED - new entity")
    print("4. Validity period is configurable per entity type")
    print("5. Status-aware: ongoing races always refresh")
    print()


if __name__ == "__main__":
    demo_cache_logic()
