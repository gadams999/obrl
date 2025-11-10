# SimRacer Scraper - Parquet Export Tool

This utility exports the SimRacer Scraper SQLite database to Apache Parquet format for use with Apache Spark, Pandas, and other analytics frameworks.

## Installation

Install the export dependencies:

```bash
cd simracer_scraper
uv sync --extra export
```

Or with all extras:

```bash
uv sync --all-extras
```

## Usage

### Basic Export

Export all tables with default settings (SNAPPY compression, partitioning enabled):

```bash
uv run simracer-export --database obrl-nov8.db --output ./parquet_output
```

### Custom Compression

Choose a compression codec:

```bash
# GZIP (higher compression, slower)
uv run simracer-export --database obrl-nov8.db --output ./parquet_output --compression gzip

# ZSTD (balanced)
uv run simracer-export --database obrl-nov8.db --output ./parquet_output --compression zstd

# No compression
uv run simracer-export --database obrl-nov8.db --output ./parquet_output --compression none
```

### Disable Partitioning

Export all tables as single files (no partitioning):

```bash
uv run simracer-export --database obrl-nov8.db --output ./parquet_output --no-partition
```

### Verbose Output

Show detailed export information:

```bash
uv run simracer-export --database obrl-nov8.db --output ./parquet_output --verbose
```

## Output Structure

The export creates the following directory structure:

```
parquet_output/
├── _metadata.json              # Export metadata (timestamps, row counts, etc.)
├── leagues.parquet             # Leagues dimension table
├── teams.parquet               # Teams dimension table
├── drivers.parquet             # Drivers dimension table
├── series.parquet              # Series dimension table
├── seasons.parquet             # Seasons dimension table
├── races.parquet               # Races fact table (or partitioned by season_id)
├── race_results.parquet        # Race results fact table (or partitioned by race_id)
├── scrape_log.parquet          # Scraping audit log
└── schema_alerts.parquet       # Schema change alerts
```

### Metadata File

The `_metadata.json` file contains:

```json
{
  "export_timestamp": "2025-11-10T15:30:00",
  "source_database": "/path/to/obrl-nov8.db",
  "compression": "snappy",
  "partitioned": true,
  "tables": {
    "leagues": {
      "row_count": 1,
      "file_size": 1024,
      "file_path": "/path/to/parquet_output/leagues.parquet"
    },
    ...
  }
}
```

## Reading Parquet Files

### With Python/Pandas

```python
import pandas as pd

# Read a single table
df = pd.read_parquet('parquet_output/drivers.parquet')

# Read all race results
df = pd.read_parquet('parquet_output/race_results.parquet')
```

### With PyArrow

```python
import pyarrow.parquet as pq

# Read a table
table = pq.read_table('parquet_output/drivers.parquet')
df = table.to_pandas()
```

### With Apache Spark

```python
from pyspark.sql import SparkSession

spark = SparkSession.builder.appName("SimRacerAnalysis").getOrCreate()

# Read dimension tables
leagues_df = spark.read.parquet("parquet_output/leagues.parquet")
drivers_df = spark.read.parquet("parquet_output/drivers.parquet")
teams_df = spark.read.parquet("parquet_output/teams.parquet")

# Read fact tables
races_df = spark.read.parquet("parquet_output/races.parquet")
results_df = spark.read.parquet("parquet_output/race_results.parquet")

# Join for analysis
full_results = results_df \
    .join(races_df, "race_id") \
    .join(drivers_df, "driver_id") \
    .select("driver_name", "track_name", "finish_position", "race_points")

full_results.show()
```

### With DuckDB

```python
import duckdb

con = duckdb.connect()

# Query Parquet directly
results = con.execute("""
    SELECT
        d.name as driver_name,
        COUNT(*) as races,
        AVG(rr.finish_position) as avg_finish
    FROM 'parquet_output/race_results.parquet' rr
    JOIN 'parquet_output/drivers.parquet' d ON rr.driver_id = d.driver_id
    GROUP BY d.name
    ORDER BY avg_finish
""").fetchdf()

print(results)
```

## Schema Information

For detailed schema documentation, see [DATABASE_SCHEMA_REFERENCE.md](DATABASE_SCHEMA_REFERENCE.md).

### Key Tables

| Table | Description | Typical Size |
|-------|-------------|--------------|
| `race_results` | Individual driver results per race | 40,000+ rows |
| `races` | Race events with metadata | 1,000+ rows |
| `drivers` | Driver profiles and stats | ~60 rows |
| `seasons` | Racing seasons | 100+ rows |
| `series` | Racing series | ~4 rows |
| `teams` | Team information | ~15 rows |
| `leagues` | League metadata | 1 row |

### Data Types

All timestamp fields are stored as ISO 8601 strings. For time-series analysis, convert to timestamp type:

```python
import pandas as pd

df = pd.read_parquet('parquet_output/races.parquet')
df['date'] = pd.to_datetime(df['date'])
df['scraped_at'] = pd.to_datetime(df['scraped_at'])
```

## Performance

### Export Performance

Typical export times (on a modern laptop):

- Small tables (leagues, teams, series): < 1 second
- Medium tables (drivers, seasons): < 2 seconds
- Large tables (races): ~3-5 seconds
- Very large tables (race_results): ~10-20 seconds

**Total export time**: ~30 seconds for full database

### File Sizes

Compression comparison for full database:

| Codec | Size | Compression Ratio | Speed |
|-------|------|-------------------|-------|
| None | ~50 MB | 1x | Fastest |
| SNAPPY | ~16 MB | 3x | Fast |
| GZIP | ~10 MB | 5x | Medium |
| ZSTD | ~12 MB | 4x | Medium |

**Recommendation**: Use SNAPPY for balanced performance and compression.

## Partitioning

By default, large tables are partitioned for optimal query performance:

### Races Table

Partitioned by `season_id`:

```
parquet_output/races/
├── season_id=26741/
│   └── data.parquet
├── season_id=26742/
│   └── data.parquet
...
```

This enables efficient queries like:

```python
# Only reads data for specific season
df = pd.read_parquet('parquet_output/races',
                     filters=[('season_id', '=', 26741)])
```

### Race Results Table

Partitioned by `race_id` (or `season_id` for broader queries).

## Troubleshooting

### PyArrow Not Installed

```bash
uv sync --extra export
```

### Memory Issues with Large Exports

For very large databases, export tables individually:

```python
from export import ParquetExporter

with ParquetExporter('obrl-nov8.db') as exporter:
    # Export one table at a time
    exporter._export_table('race_results', 'output/race_results')
```

### Timestamp Parsing Issues

SQLite stores timestamps as strings. Convert after reading:

```python
df['scraped_at'] = pd.to_datetime(df['scraped_at'])
```

## Integration with Analytics Tools

### Jupyter Notebooks

```python
import pandas as pd
import matplotlib.pyplot as plt

# Load data
results = pd.read_parquet('parquet_output/race_results.parquet')
drivers = pd.read_parquet('parquet_output/drivers.parquet')

# Merge and analyze
df = results.merge(drivers, on='driver_id')

# Plot
df.groupby('name')['finish_position'].mean().plot(kind='bar')
plt.title('Average Finish Position by Driver')
plt.show()
```

### Tableau

1. Install Tableau Desktop
2. Connect to data source → "Parquet File"
3. Select `parquet_output/race_results.parquet`
4. Join with other dimension tables as needed

### Power BI

1. Get Data → "Parquet"
2. Navigate to parquet_output directory
3. Select tables to import
4. Use Power Query to join tables

## Development

### Running Tests

Tests for the export tool will be added to the test suite:

```bash
cd simracer_scraper
uv run pytest tests/unit/test_parquet_exporter.py
```

### Code Quality

Format and lint:

```bash
uv run black src/export/
uv run ruff check src/export/
```

## Future Enhancements

Planned features:

- [ ] Automatic partitioning optimization
- [ ] Incremental export (only new/changed data)
- [ ] Delta Lake format support
- [ ] Timestamp conversion options
- [ ] Lap time parsing (convert "MM:SS.mmm" to seconds)
- [ ] Denormalized views for common queries
- [ ] Cloud storage support (S3, GCS, Azure)
- [ ] Schema evolution handling
- [ ] Export to other formats (CSV, JSON, Avro)

## See Also

- [DATABASE_SCHEMA_REFERENCE.md](DATABASE_SCHEMA_REFERENCE.md) - Complete schema documentation
- [ARCHITECTURE.md](ARCHITECTURE.md) - Project architecture
- [README.md](README.md) - Main project README
