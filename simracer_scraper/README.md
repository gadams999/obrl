# SimRacer Scraper

A respectful web scraper for extracting racing league data from SimRacerHub.

## ⚠️ Important Legal Notice

**This scraper is currently awaiting permission from SimRacerHub administrators.**

SimRacerHub's `robots.txt` contains:
```
User-agent: *
Disallow: /
```

This indicates the site operators prefer no automated scraping. Use of this tool should only occur:
- After receiving explicit permission from SimRacerHub administrators
- For personal/educational purposes with your own league data
- With full understanding of the site's policies

## Features

- **Rate-limited requests** - Configurable delays between requests (default: 2 seconds)
- **Retry logic** - Automatic retry with exponential backoff
- **Targeted scraping** - Only scrape specific configured pages
- **Multiple output formats** - Save as JSON, CSV, or both
- **Comprehensive logging** - Track all scraping activity
- **Respectful user-agent** - Identifies the scraper with contact info

## Installation

This project uses [uv](https://github.com/astral-sh/uv) for package management and [mise](https://mise.jdx.dev/) for Python version management.

### With uv (recommended)

```bash
# Install dependencies and sync environment
uv sync

# Copy and configure environment file (optional)
cp .env.example .env
# Edit .env with your settings
```

### With pip (alternative)

```bash
# Install dependencies
pip install -r requirements.txt

# Copy and configure environment file
cp .env.example .env
# Edit .env with your settings
```

### Python Version

This project requires Python 3.10+. If using mise:

```bash
mise install  # Installs Python version from .python-version
```

## Configuration

Edit `config.yaml` to customize scraper behavior:

```yaml
# Add specific pages to scrape
target_pages:
  - /league/your-league-name
  - /standings/your-league-id

# Adjust rate limiting
scraping:
  request_delay: 2.0  # seconds between requests
  max_retries: 3
  timeout: 10
```

## Usage

### Basic Usage

```bash
# With uv (recommended)
uv run python -m src.cli scrape

# Or use the installed script
uv run simracer-scraper scrape

# Scrape a specific page
uv run simracer-scraper scrape --url /league/the-obrl-presented-by-vctrylnsprts

# Use custom config file
uv run simracer-scraper scrape --config custom_config.yaml

# Specify output filename
uv run simracer-scraper scrape --url /league/my-league --output my_league_data
```

### Advanced Options

```bash
# Enable debug logging
uv run simracer-scraper scrape --log-level DEBUG

# Use different config file
uv run simracer-scraper scrape --config production.yaml
```

### Without uv

```bash
# Run directly with Python
python -m src.cli scrape --url /league/your-league
```

## Output

Scraped data is saved to the `./data` directory (configurable) in the specified format:

- **JSON format**: Preserves nested structure, good for programmatic use
- **CSV format**: Flattened data, good for spreadsheet analysis

Default output includes:
- Page URL
- Timestamp
- Page title
- Extracted tables (standings, results, etc.)
- League information

## Project Structure

```
simracer_scraper/
├── src/
│   ├── __init__.py
│   ├── __main__.py
│   ├── base_scraper.py    # Base scraper with rate limiting
│   ├── league_scraper.py  # League-specific scraper
│   └── cli.py             # Command-line interface
├── data/                  # Output directory (created automatically)
├── config.yaml            # Main configuration file
├── pyproject.toml         # Project metadata and dependencies
├── uv.lock                # Locked dependencies (uv)
├── requirements.txt       # Python dependencies (fallback)
├── .python-version        # Python version for mise
├── .env.example          # Environment variables template
└── README.md             # This file
```

## Development

### Installing Development Dependencies

```bash
# Install with dev dependencies
uv sync --all-extras
```

### Adding Custom Extractors

The `LeagueScraper` class can be extended with custom extraction methods:

```python
from src.league_scraper import LeagueScraper

class CustomLeagueScraper(LeagueScraper):
    def _extract_league_info(self, soup):
        # Add custom extraction logic
        info = super()._extract_league_info(soup)
        # ... your custom code
        return info
```

### Code Quality

```bash
# Format code with black
uv run black src/

# Lint with ruff
uv run ruff check src/

# Type checking with mypy
uv run mypy src/
```

### Testing

Before running on production URLs, test with a single page:

```bash
uv run simracer-scraper scrape --url /single-test-page --log-level DEBUG

# Or run tests (when implemented)
uv run pytest
```

## Ethical Scraping Guidelines

This scraper follows ethical scraping practices:

1. **Respect robots.txt** - Awaiting permission despite restrictive policy
2. **Rate limiting** - Default 2-second delay between requests
3. **Identification** - Clear user-agent with contact information
4. **Targeted scraping** - Only scrape specific needed pages
5. **Error handling** - Graceful handling of failures
6. **Logging** - Full audit trail of scraping activity

## License

This tool is provided for educational and personal use only.

## Contact

Before using this scraper, contact SimRacerHub administrators for permission.

Default league: **The OBRL presented by VctryLnSprts**
