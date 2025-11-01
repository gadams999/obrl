"""Command-line interface for the SimRacer scraper."""

import argparse
import logging
import sys
from pathlib import Path

import yaml

from .league_scraper import LeagueScraper


def setup_logging(level: str = "INFO", log_file: str = None):
    """Configure logging."""
    log_level = getattr(logging, level.upper(), logging.INFO)

    handlers = [logging.StreamHandler(sys.stdout)]
    if log_file:
        handlers.append(logging.FileHandler(log_file))

    logging.basicConfig(
        level=log_level,
        format="%(asctime)s - %(name)s - %(levelname)s - %(message)s",
        handlers=handlers,
    )


def load_config(config_path: str) -> dict:
    """Load configuration from YAML file."""
    with open(config_path, "r") as f:
        return yaml.safe_load(f)


def main():
    """Main CLI entry point."""
    parser = argparse.ArgumentParser(
        description="SimRacer Hub Web Scraper",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
  # Scrape specific pages from config
  python -m src.cli scrape

  # Scrape a single page
  python -m src.cli scrape --url /league/the-obrl

  # Use custom config
  python -m src.cli scrape --config custom_config.yaml
        """,
    )

    parser.add_argument(
        "command",
        choices=["scrape"],
        help="Command to execute",
    )

    parser.add_argument(
        "--config",
        default="config.yaml",
        help="Path to configuration file (default: config.yaml)",
    )

    parser.add_argument(
        "--url",
        help="Single URL to scrape (overrides config target_pages)",
    )

    parser.add_argument(
        "--output",
        help="Output filename (without extension)",
    )

    parser.add_argument(
        "--log-level",
        choices=["DEBUG", "INFO", "WARNING", "ERROR"],
        help="Override log level from config",
    )

    args = parser.parse_args()

    # Load configuration
    try:
        config = load_config(args.config)
    except FileNotFoundError:
        print(f"Error: Config file '{args.config}' not found")
        return 1
    except yaml.YAMLError as e:
        print(f"Error parsing config file: {e}")
        return 1

    # Setup logging
    log_config = config.get("logging", {})
    log_level = args.log_level or log_config.get("level", "INFO")
    log_file = log_config.get("file")
    setup_logging(log_level, log_file)

    logger = logging.getLogger(__name__)

    # Execute command
    if args.command == "scrape":
        try:
            with LeagueScraper(config) as scraper:
                # Determine URLs to scrape
                if args.url:
                    urls = [args.url]
                else:
                    urls = config.get("target_pages", [])
                    if not urls:
                        logger.error("No URLs specified. Use --url or add target_pages to config.")
                        return 1

                logger.info(f"Scraping {len(urls)} page(s)...")
                results = scraper.scrape_multiple_pages(urls)

                # Save results
                output_name = args.output or f"scraped_data_{Path(urls[0]).stem}"
                scraper.save_results(results, output_name)

                logger.info(f"Scraping complete. Scraped {len(results)} page(s).")

        except KeyboardInterrupt:
            logger.warning("Scraping interrupted by user")
            return 130
        except Exception as e:
            logger.exception(f"Error during scraping: {e}")
            return 1

    return 0


if __name__ == "__main__":
    sys.exit(main())
