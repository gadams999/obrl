# Project Backlog

**Last Updated**: 2025-11-01

This document tracks features and tasks that have been deferred or deprioritized for later implementation.

## Deferred Tasks

### Task 3.5: Driver Extractor
**Status**: Moved to backlog on 2025-11-01
**Priority**: Low
**Estimated Effort**: 2-3 hours (similar to other extractors)

**Description**: Extract driver profile data from `driver_stats.php?driver_id={id}` pages.

**What it would provide**:
- Full driver name (first/last split)
- Club affiliation (e.g., "Ohio Club")
- Complete race history across ALL leagues
- Career statistics aggregated
- iRacing club_id

**Why deferred**:
- Race results already contain 95% of driver data needed
- For single-league tracking, race results are sufficient
- Driver names, iRating, performance metrics all available in race results
- Club affiliation is cosmetic, not essential

**When to implement**:
- If cross-league career tracking becomes a requirement
- If club affiliations needed for features/reports
- If validation of race-derived driver data is desired

**Reference**: See `RESEARCH_DRIVER_TEAM_EXTRACTORS.md` for detailed analysis

---

### Task 3.6: Team Extractor
**Status**: Moved to backlog on 2025-11-01
**Priority**: Low
**Estimated Effort**: 2-3 hours (similar to other extractors)

**Description**: Extract team roster data from `teams.php?league_id={id}` pages.

**What it would provide**:
- Official team rosters
- Team names and team_id mappings
- Team metadata (logos, descriptions)
- League-specific team structure

**Why deferred**:
- Team affiliations can be inferred from race results (team_id present in results)
- For single-league tracking, team inference from race data is adequate
- Team rosters can be derived by grouping drivers by team_id

**When to implement**:
- If official team rosters needed (not inferred)
- If team metadata (logos, descriptions) required for UI
- If validation of race-derived team data is desired

**Reference**: See `RESEARCH_DRIVER_TEAM_EXTRACTORS.md` for detailed analysis

---

## Future Enhancements

### Rate Limiting & Throttling Improvements
**Priority**: High (User Requested 2025-11-01)
**Estimated Effort**: 2-3 hours

**Description**: Enhance rate limiting to better simulate human browsing patterns and avoid overloading SimRacerHub.

**Features to Add**:
1. **Adjustable Inter-Page Delay**
   - Configurable delay between page requests (e.g., 2-5 seconds)
   - Randomized delays to simulate human clicking (e.g., random between 2-4 seconds)
   - Different delays for different entity types (faster for metadata, slower for race results)

2. **Depth Control Filters**
   - Filter at league → series level: "Only scrape series X, Y, Z"
   - Filter at season level: "Only scrape current season" or "Only scrape seasons from year 2025"
   - Allows testing/validation before full recursive scrape

3. **Progressive Validation Mode**
   - Scrape just metadata first (league + series, no races)
   - Review data quality
   - Then optionally scrape deeper levels

**Implementation Approach**:
```python
class Orchestrator:
    def scrape_league(
        self,
        league_url: str,
        depth: str = "league",
        filters: dict = None,  # NEW
        delay_range: tuple = (2.0, 4.0),  # NEW: (min, max) seconds
    ):
        """
        filters = {
            "series_ids": [3714, 3713],  # Only scrape these series
            "season_year": 2025,  # Only scrape 2025 seasons
            "season_limit": 1,  # Only scrape 1 season per series
        }
        """
```

**Benefits**:
- Safer for testing against live site
- Avoid accidentally scraping entire league (100+ races)
- Better rate limiting = good citizenship
- Validate extraction logic on small subset first

**Trigger**: Before implementing Task 4.2 (league scraping)

**Related**: See `REVIEW_ORCHESTRATOR.md` Question 1 (Rate Limiting Strategy)

---

### Performance Optimizations
**Priority**: Medium
**Estimated Effort**: 4-6 hours

- Concurrent scraping (asyncio/aiohttp) - ONLY after rate limiting is solid
- Connection pooling
- Batch database operations
- Caching improvements

**Trigger**: When scraping performance becomes a bottleneck
**Note**: Defer until rate limiting is working well to avoid overloading site

---

### Schema Change Detection
**Priority**: Medium
**Estimated Effort**: Already implemented (SchemaValidator), needs integration

- Integrate SchemaValidator into orchestration layer
- Alert on schema changes
- Log schema alerts to database

**Trigger**: When moving to production use

---

### Advanced Reporting
**Priority**: Low
**Estimated Effort**: 8-12 hours

- Driver performance reports
- Track statistics
- Series comparisons
- Championship standings
- Export to PDF/Excel

**Trigger**: When basic scraping is stable and working

---

### Web UI
**Priority**: Low
**Estimated Effort**: 20+ hours

- Flask/FastAPI backend
- React/Vue frontend
- Browse league data
- View driver profiles
- Track standings

**Trigger**: When CLI is feature-complete and stable

---

## Won't Do (For Now)

### Real-time Race Monitoring
**Reason**: SimRacerHub doesn't provide live timing API
**Alternative**: Scrape completed races only

### Historical Data Import
**Reason**: Focus is on ongoing tracking, not historical backfill
**Alternative**: Start tracking from "now" forward

### Multi-user Support
**Reason**: Single-user local database is sufficient
**Alternative**: Each user runs their own instance

---

## Re-evaluation Schedule

This backlog should be reviewed:
- After Task 4.x (Orchestration) is complete
- After Task 5.x (Professional CLI) is complete
- When new requirements emerge from actual usage
- Quarterly (or after major milestones)

---

## Backlog Promotion Criteria

A backlog item should be promoted to active development if:
1. **User request**: Explicit need identified
2. **Blocking**: Current features can't work without it
3. **High value**: Significant improvement to user experience
4. **Low effort**: Quick win that's easy to implement
5. **Foundation**: Needed for other planned features

---

## Related Documents

- `RESEARCH_DRIVER_TEAM_EXTRACTORS.md` - Analysis of Tasks 3.5-3.6
- `TASKS.md` - Original task definitions
- `STATUS.md` - Current project status
- `SPECIFICATION.md` - Technical requirements
