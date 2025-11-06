"""Command-line interface for the SimRacer scraper."""

import argparse
import logging
import sys
from pathlib import Path

import yaml

from .database import Database
from .orchestrator import Orchestrator
from .schema_validator import SchemaValidator


def setup_logging(level: str = "INFO"):
    """Configure logging."""
    log_level = getattr(logging, level.upper(), logging.INFO)

    logging.basicConfig(
        level=log_level,
        format="%(asctime)s - %(name)s - %(levelname)s - %(message)s",
        handlers=[logging.StreamHandler(sys.stdout)],
    )


def load_config(config_path: str) -> dict:
    """Load configuration from YAML file."""
    try:
        with open(config_path, "r") as f:
            return yaml.safe_load(f)
    except FileNotFoundError:
        return {}
    except yaml.YAMLError:
        return {}


def main():
    """Main CLI entry point."""
    parser = argparse.ArgumentParser(
        description="SimRacer Hub Web Scraper",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
  # Use config.yaml (simplest - edit league.id in config.yaml)
  python scraper.py

  # Scrape a specific league
  python scraper.py scrape league 1558

  # Scrape with depth control
  python scraper.py scrape all --league 1558 --depth league

  # Force refresh (ignore cache)
  python scraper.py scrape league 1558 --force
        """,
    )

    # Use subparsers for better command structure
    subparsers = parser.add_subparsers(dest="command", help="Command to execute")

    # Scrape command
    scrape_parser = subparsers.add_parser("scrape", help="Scrape league data")
    scrape_parser.add_argument(
        "entity",
        nargs="?",
        choices=["league", "all"],
        help="What to scrape (league, all) - optional if config has league.id",
    )
    scrape_parser.add_argument(
        "league_id",
        nargs="?",
        help="League ID to scrape - optional if config has league.id",
    )
    scrape_parser.add_argument(
        "--league",
        help="League ID (alternative to positional arg)",
    )
    scrape_parser.add_argument(
        "--depth",
        choices=["league", "series", "season", "race"],
        default="race",
        help="How deep to scrape hierarchy (default: race)",
    )
    scrape_parser.add_argument(
        "--force",
        action="store_true",
        help="Force re-scrape, ignore cache",
    )
    scrape_parser.add_argument(
        "--db",
        default="simracer.db",
        help="Database path (default: simracer.db)",
    )
    scrape_parser.add_argument(
        "--config",
        default="config.yaml",
        help="Path to config file (default: config.yaml)",
    )
    scrape_parser.add_argument(
        "--log-level",
        choices=["DEBUG", "INFO", "WARNING", "ERROR"],
        default="INFO",
        help="Log level (default: INFO)",
    )

    args = parser.parse_args()

    # If no command provided, default to scrape with config
    if not args.command:
        args.command = "scrape"
        args.entity = None
        args.league_id = None
        args.league = None
        args.depth = "race"
        args.force = False
        args.db = "simracer.db"
        args.config = "config.yaml"
        args.log_level = "INFO"

    # Load config
    config = load_config(args.config)

    # Setup logging (from config or CLI arg)
    log_level = args.log_level
    if not log_level and config.get("logging"):
        log_level = config["logging"].get("level", "INFO")
    setup_logging(log_level or "INFO")
    logger = logging.getLogger(__name__)

    # Get league ID (priority: CLI positional > CLI --league > config)
    league_id = args.league_id or args.league
    if not league_id and config.get("league"):
        league_id = config["league"].get("id")

    if not league_id:
        logger.error("League ID required. Provide via command line or config.yaml")
        logger.error("Usage: python scraper.py scrape league <league_id>")
        logger.error("   Or: python scraper.py (with league.id in config.yaml)")
        return 1

    # Get depth (priority: CLI > config > default)
    depth = args.depth
    if depth == "race" and config.get("league"):  # "race" is the default
        depth = config["league"].get("depth", "race")

    # Get database path (priority: CLI > config > default)
    db_path = args.db
    if db_path == "simracer.db" and config.get("league"):  # "simracer.db" is the default
        db_path = config["league"].get("database", "simracer.db")

    # Execute command
    if args.command == "scrape":
        try:
            # Initialize database and orchestrator
            with Database(db_path) as db:
                db.initialize_schema()
                validator = SchemaValidator()
                with Orchestrator(
                    database=db,
                    validator=validator,
                    rate_limit_range=(2.0, 4.0),
                ) as orchestrator:
                    # Build league URL
                    league_url = f"https://www.simracerhub.com/league_series.php?league_id={league_id}"

                    # Set cache behavior
                    cache_max_age = None if args.force else 7

                    logger.info(f"Scraping league {league_id} with depth={depth}")

                    # Scrape with specified depth
                    result = orchestrator.scrape_league(
                        league_url=league_url,
                        depth=depth,
                        cache_max_age_days=cache_max_age,
                    )

                    # Show progress
                    progress = orchestrator.get_progress()
                    logger.info(f"Scraping complete!")
                    logger.info(f"  Leagues: {progress['leagues_scraped']}")
                    logger.info(f"  Series: {progress['series_scraped']}")
                    logger.info(f"  Seasons: {progress['seasons_scraped']}")
                    logger.info(f"  Races: {progress['races_scraped']}")
                    logger.info(f"  Cached (skipped): {progress['skipped_cached']}")

                    if progress['errors']:
                        logger.warning(f"  Errors: {len(progress['errors'])}")

        except KeyboardInterrupt:
            logger.warning("Scraping interrupted by user")
            return 130
        except Exception as e:
            logger.exception(f"Error during scraping: {e}")
            return 1

    return 0


if __name__ == "__main__":
    sys.exit(main())
