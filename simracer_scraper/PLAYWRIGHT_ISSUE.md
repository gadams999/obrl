# Playwright System Dependencies Issue

## Problem

Playwright requires system libraries that are not installed in the current WSL environment. This prevents race pages from being scraped because they require JavaScript rendering.

## Error Messages

1. **Missing system dependencies**:
```
BrowserType.launch:
╔══════════════════════════════════════════════════════╗
║ Host system is missing dependencies to run browsers. ║
║ Please install them with the following command:      ║
║                                                      ║
║     sudo playwright install-deps                     ║
║                                                      ║
║ Alternatively, use apt:                              ║
║     sudo apt-get install libnspr4\                   ║
║         libnss3                                      ║
╚══════════════════════════════════════════════════════╝
```

2. **Async loop error** (appears after first error):
```
It looks like you are using Playwright Sync API inside the asyncio loop.
Please use the Async API instead.
```

The async error is a side effect of the failed browser launch, not the root cause.

## Root Cause

The scraper uses Playwright for JavaScript rendering, which is required for:
- **Season pages** with `depth=race` - to find individual race links in dropdown menus
- **Race pages** - to render JavaScript-generated race result tables

Without system dependencies, Playwright cannot launch the Chromium browser, so:
1. Season pages fail to scrape when `depth=race`
2. No race links are discovered
3. No race results can be scraped

## Solution

### Option 1: Install System Dependencies (Recommended)

```bash
# Install Playwright system dependencies
sudo playwright install-deps

# Or install specific libraries
sudo apt-get update
sudo apt-get install -y \
    libnspr4 \
    libnss3 \
    libatk1.0-0 \
    libatk-bridge2.0-0 \
    libcups2 \
    libxkbcommon0 \
    libxcomposite1 \
    libxdamage1 \
    libxfixes3 \
    libxrandr2 \
    libgbm1 \
    libpango-1.0-0 \
    libcairo2 \
    libasound2

# Then reinstall Playwright browsers
uv run playwright install chromium
```

### Option 2: Use Docker (If sudo not available)

Create a Docker container with Playwright pre-installed:

```dockerfile
FROM mcr.microsoft.com/playwright/python:v1.41.0-jammy

WORKDIR /app
COPY . .

RUN pip install uv
RUN uv sync

CMD ["uv", "run", "python", "scraper.py", "scrape", "league", "1558"]
```

### Option 3: Disable JavaScript Rendering (Limited Functionality)

If you only need league/series/season metadata without race results, you can run with `--depth season` which doesn't require JavaScript:

```bash
python scraper.py scrape league 1558 --depth season
```

**Note**: This will NOT scrape individual races or race results.

## Testing After Fix

Once system dependencies are installed, test with:

```bash
# Test simple season scrape (no JS)
uv run python scraper.py scrape league 1558 --depth season --db test.db

# Test full hierarchy with races (requires JS)
uv run python scraper.py scrape league 1558 --depth race --db test.db

# Check if races were scraped
uv run python -c "
import sqlite3
conn = sqlite3.connect('test.db')
cursor = conn.cursor()
cursor.execute('SELECT COUNT(*) FROM races')
print(f'Races: {cursor.fetchone()[0]}')
cursor.execute('SELECT COUNT(*) FROM race_results')
print(f'Race results: {cursor.fetchone()[0]}')
conn.close()
"
```

## Current Status

- ✅ Cache improvements implemented and working
- ✅ Database schema updated with `is_complete` flag
- ✅ Season extractor updated to detect race results URLs
- ✅ Orchestrator updated with smart caching logic
- ✅ **Playwright system dependencies installed (openSUSE Tumbleweed)**
- ✅ **Race results scraping working perfectly**

## Impact

**✅ ALL SYSTEMS OPERATIONAL**

- League scraping: ✅ Works
- Series scraping: ✅ Works
- Season scraping: ✅ Works (with race discovery via JavaScript rendering)
- Race scraping: ✅ **Works** (JavaScript rendering functional)
- Race results: ✅ **Saved successfully** (260 results from 16 races tested)

## Test Results (2025-11-06)

Successful test scrape of league 1558:
- **1 league** scraped
- **4 series** discovered
- **47 seasons** discovered
- **16 races** discovered (10 with results, 6 future/incomplete)
- **260 race results** extracted successfully
- **56 drivers** identified
- Playwright browser: ✅ Chromium launched successfully (fallback build for Ubuntu 20.04)
- Rate limiting: ✅ Working (2-4 second delays enforced)
- No async loop errors
- All data persisted to SQLite database

The Playwright issue has been **RESOLVED**. Race-level scraping is fully functional.
