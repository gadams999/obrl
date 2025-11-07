#!/usr/bin/env python3
"""Test race extraction on real race pages from SimRacerHub."""

from bs4 import BeautifulSoup
from src.extractors.race import RaceExtractor
from src.utils.browser_manager import BrowserManager
import json

# Race URLs to test
RACE_URLS = [
    "https://www.simracerhub.com/season_race.php?schedule_id=162902",
    "https://www.simracerhub.com/season_race.php?schedule_id=82724",
    "https://www.simracerhub.com/season_race.php?schedule_id=91657",
]


def test_race_extraction():
    """Test extraction on real race pages."""

    with BrowserManager() as browser_manager:
        extractor = RaceExtractor(browser_manager=browser_manager, render_js=True)

        results = []

        for i, url in enumerate(RACE_URLS, 1):
            print(f"\n{'='*80}")
            print(f"RACE {i}: {url}")
            print(f"{'='*80}\n")

            try:
                # Fetch the page
                print("Fetching HTML...")
                soup = extractor.fetch_page(url)
                html = str(soup)

                # Save HTML for inspection
                html_file = f"race_{url.split('=')[1]}_raw.html"
                with open(html_file, 'w', encoding='utf-8') as f:
                    f.write(html)
                print(f"Saved HTML to: {html_file}")

                # Extract race-info div specifically
                race_info_div = soup.find("div", class_="race-info")
                if race_info_div:
                    print("\n--- RACE INFO DIV CONTENT ---")
                    print(race_info_div.prettify())
                    print("--- END RACE INFO DIV ---\n")
                else:
                    print("⚠️  No race-info div found!\n")

                # Extract metadata
                print("Extracting metadata...")
                data = extractor.extract(url)
                metadata = data["metadata"]

                # Display extracted metadata
                print("\n--- EXTRACTED METADATA ---")
                for key, value in sorted(metadata.items()):
                    print(f"  {key}: {value}")
                print("--- END METADATA ---\n")

                # Save results
                results.append({
                    "url": url,
                    "schedule_id": url.split("=")[1],
                    "html_file": html_file,
                    "metadata": metadata,
                    "race_info_found": race_info_div is not None,
                    "results_count": len(data.get("results", [])),
                })

                print(f"✅ Successfully extracted {len(data.get('results', []))} driver results")

            except Exception as e:
                print(f"❌ Error: {e}")
                import traceback
                traceback.print_exc()
                results.append({
                    "url": url,
                    "schedule_id": url.split("=")[1],
                    "error": str(e),
                })

        # Save summary
        summary_file = "test_real_races_summary.json"
        with open(summary_file, 'w', encoding='utf-8') as f:
            json.dump(results, f, indent=2, default=str)
        print(f"\n{'='*80}")
        print(f"Summary saved to: {summary_file}")
        print(f"{'='*80}\n")

        return results


if __name__ == "__main__":
    test_race_extraction()
