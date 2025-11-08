#!/usr/bin/env python3
"""Test script to verify race result extraction and database population."""

import sys
from pathlib import Path
from bs4 import BeautifulSoup

# Add src to path
sys.path.insert(0, str(Path(__file__).parent / "src"))

from src.database import Database
from src.extractors.race import RaceExtractor


def main():
    # Read the test fixture
    fixture_path = Path(__file__).parent / "tests" / "fixtures" / "season_race_324462.html"
    with open(fixture_path) as f:
        html_content = f.read()

    # Extract data using RaceExtractor
    print("=" * 80)
    print("EXTRACTING RACE DATA FROM FIXTURE")
    print("=" * 80)

    extractor = RaceExtractor()
    soup = BeautifulSoup(html_content, "html.parser")

    # Use internal method to extract results
    results = extractor._extract_results(soup)

    print(f"\nExtracted {len(results)} race results\n")

    # Print first result to see what we got
    if results:
        print("First result extracted:")
        print("-" * 80)
        for key, value in results[0].items():
            print(f"  {key:30s} = {value}")
        print()

    # Create test database
    db_path = Path(__file__).parent / "test_race_results.db"
    if db_path.exists():
        db_path.unlink()

    print("=" * 80)
    print("CREATING DATABASE AND POPULATING")
    print("=" * 80)

    with Database(str(db_path)) as db:
        # Initialize schema
        db.initialize_schema()

        # Insert a dummy league, series, season, and race
        from datetime import datetime

        league_id = 1558
        db.upsert_league(
            league_id,
            {
                "name": "Test League",
                "url": "http://test.com",
                "scraped_at": datetime.now().isoformat(),
            },
        )

        series_id = 3714
        db.upsert_series(
            series_id,
            league_id,
            {
                "name": "Test Series",
                "url": "http://test.com/series",
                "scraped_at": datetime.now().isoformat(),
            },
        )

        season_id = 15532
        db.upsert_season(
            season_id,
            series_id,
            {
                "name": "Test Season",
                "url": "http://test.com/season",
                "scraped_at": datetime.now().isoformat(),
            },
        )

        schedule_id = 324462
        race_id = db.upsert_race(
            schedule_id,
            season_id,
            {
                "name": "Test Race",
                "url": "http://test.com/race",
                "scraped_at": datetime.now().isoformat(),
                "race_number": 1,
                "date": "2025-01-08",
                "track": "Test Track",
            },
        )

        # Insert drivers
        for result in results:
            driver_id = result.get("driver_id")
            if driver_id:
                db.upsert_driver(
                    driver_id,
                    league_id,
                    {
                        "name": result.get("driver_name", f"Driver {driver_id}"),
                        "url": f"http://test.com/driver/{driver_id}",
                        "scraped_at": datetime.now().isoformat(),
                    },
                )

        # Insert race results
        print(f"\nInserting {len(results)} race results...")
        for result in results:
            driver_id = result.get("driver_id")
            if driver_id:
                # Remove driver_name as it's not a race_results field
                result_data = {k: v for k, v in result.items() if k != "driver_name"}
                db.upsert_race_result(race_id, driver_id, result_data)

        print("Database populated successfully!\n")

        # Query and display results
        print("=" * 80)
        print("QUERYING DATABASE RESULTS")
        print("=" * 80)

        cursor = db.conn.cursor()
        cursor.execute("""
            SELECT
                driver_id,
                team,
                incident_points,
                penalty_points,
                fast_laps,
                quality_passes,
                closing_passes,
                total_passes,
                average_running_position,
                race_points,
                bonus_points,
                total_points
            FROM race_results
            ORDER BY driver_id
        """)

        rows = cursor.fetchall()

        print("\nResults from database:")
        print("-" * 80)
        print(f"{'driver_id':<12} {'team':<15} {'incidents':<10} {'penalties':<10} {'fast_laps':<10}")
        print(f"{'qual_pass':<12} {'close_pass':<12} {'total_pass':<12} {'avg_run_pos':<12}")
        print("-" * 80)

        for row in rows:
            print(f"{row[0]:<12} {str(row[1]):<15} {str(row[2]):<10} {str(row[3]):<10} {str(row[4]):<10}")
            print(f"{str(row[5]):<12} {str(row[6]):<12} {str(row[7]):<12} {str(row[8]):<12}")
            print()

        # Check for NULL values
        print("=" * 80)
        print("CHECKING FOR NULL VALUES")
        print("=" * 80)

        null_checks = [
            ("team", "team"),
            ("incident_points", "incident_points"),
            ("penalty_points", "penalty_points"),
            ("fast_laps", "fast_laps"),
            ("quality_passes", "quality_passes"),
            ("closing_passes", "closing_passes"),
            ("total_passes", "total_passes"),
            ("average_running_position", "average_running_position"),
        ]

        for field_name, column_name in null_checks:
            cursor.execute(f"SELECT COUNT(*) FROM race_results WHERE {column_name} IS NULL")
            null_count = cursor.fetchone()[0]
            cursor.execute("SELECT COUNT(*) FROM race_results")
            total_count = cursor.fetchone()[0]

            status = "✗ PROBLEM" if null_count > 0 else "✓ OK"
            print(f"{field_name:30s}: {null_count}/{total_count} NULL values {status}")


if __name__ == "__main__":
    main()
