#!/usr/bin/env python3
"""Manual testing script for OBRL league data."""

from src.database import Database
from datetime import datetime

def test_obrl_league():
    """Test with real OBRL league data."""
    # Create in-memory database
    db = Database(":memory:")
    db.connect()
    db.initialize_schema()

    # The OBRL league data
    obrl_id = 1558
    obrl_data = {
        "name": "The OBRL presented by VctryLnSprts",
        "url": "https://www.simracerhub.com/league.php?league_id=1558",
        "description": "Online Oval Racing League",
        "organizer": "VctryLnSprts",
        "scraped_at": datetime.now().isoformat(),
    }

    # Insert the league
    league_id = db.upsert_league(obrl_id, obrl_data)
    print(f"✓ Inserted league {league_id}")

    # Retrieve by ID
    league = db.get_league(obrl_id)
    print(f"\n📋 League by ID ({obrl_id}):")
    for key, value in league.items():
        print(f"  {key}: {value}")

    # Retrieve by URL
    league_by_url = db.get_league_by_url(obrl_data["url"])
    print(f"\n📋 League by URL:")
    print(f"  Found: {league_by_url['name']}")

    # Update the league
    print("\n🔄 Updating league...")
    updated_data = {
        "name": "The OBRL presented by VctryLnSprts (Updated)",
        "url": obrl_data["url"],
        "description": "Premier Online Oval Racing League",
        "organizer": "VctryLnSprts Racing",
        "scraped_at": datetime.now().isoformat(),
    }
    db.upsert_league(obrl_id, updated_data)

    league_updated = db.get_league(obrl_id)
    print(f"  New name: {league_updated['name']}")
    print(f"  New description: {league_updated['description']}")
    print(f"  Created at: {league_updated['created_at']}")
    print(f"  Updated at: {league_updated['updated_at']}")

    db.close()
    print("\n✅ All tests passed!")

if __name__ == "__main__":
    test_obrl_league()
