"""Parquet export utility for SimRacer Scraper database.

This module exports SQLite database tables to Parquet format with optimizations
for Apache Spark and other analytics frameworks.
"""

import sqlite3
from datetime import datetime
from pathlib import Path
from typing import Any

import pyarrow as pa
import pyarrow.parquet as pq


class ParquetExporter:
    """Export SimRacer Scraper database to Parquet format."""

    def __init__(self, db_path: str):
        """
        Initialize Parquet exporter.

        Args:
            db_path: Path to SQLite database file
        """
        self.db_path = Path(db_path)
        if not self.db_path.exists():
            raise FileNotFoundError(f"Database file not found: {db_path}")

        self.conn: sqlite3.Connection | None = None

    def __enter__(self):
        """Context manager entry."""
        self.connect()
        return self

    def __exit__(self, exc_type, exc_val, exc_tb):
        """Context manager exit."""
        self.close()

    def connect(self):
        """Open database connection."""
        self.conn = sqlite3.connect(str(self.db_path))
        self.conn.row_factory = sqlite3.Row

    def close(self):
        """Close database connection."""
        if self.conn:
            self.conn.close()
            self.conn = None

    def export_all(
        self,
        output_dir: str,
        compression: str = "snappy",
        partitioned: bool = True,
    ) -> dict[str, Any]:
        """
        Export all tables to Parquet format.

        Args:
            output_dir: Directory to write Parquet files
            compression: Compression codec (snappy, gzip, zstd, none)
            partitioned: Whether to partition large tables

        Returns:
            Dictionary with export metadata (row counts, file sizes, etc.)
        """
        if not self.conn:
            raise RuntimeError("Database not connected")

        output_path = Path(output_dir)
        output_path.mkdir(parents=True, exist_ok=True)

        metadata = {
            "export_timestamp": datetime.now().isoformat(),
            "source_database": str(self.db_path),
            "compression": compression,
            "partitioned": partitioned,
            "tables": {},
        }

        # Export dimension tables
        dimension_tables = ["leagues", "teams", "drivers", "series", "seasons"]
        for table in dimension_tables:
            result = self._export_table(
                table,
                output_path / table,
                compression=compression,
            )
            metadata["tables"][table] = result

        # Export fact tables with optional partitioning
        if partitioned:
            # Export races partitioned by season_id
            result = self._export_races_partitioned(
                output_path / "races",
                compression=compression,
            )
            metadata["tables"]["races"] = result

            # Export race_results partitioned by race_id
            result = self._export_race_results_partitioned(
                output_path / "race_results",
                compression=compression,
            )
            metadata["tables"]["race_results"] = result
        else:
            result = self._export_table("races", output_path / "races", compression)
            metadata["tables"]["races"] = result

            result = self._export_table(
                "race_results", output_path / "race_results", compression
            )
            metadata["tables"]["race_results"] = result

        return metadata

    def _export_table(
        self,
        table_name: str,
        output_path: Path,
        compression: str = "snappy",
    ) -> dict[str, Any]:
        """
        Export a single table to Parquet.

        Args:
            table_name: Name of the table to export
            output_path: Output file path (without .parquet extension)
            compression: Compression codec

        Returns:
            Dictionary with export stats
        """
        if not self.conn:
            raise RuntimeError("Database not connected")

        # Query all rows
        cursor = self.conn.cursor()
        cursor.execute(f"SELECT * FROM {table_name}")
        rows = cursor.fetchall()

        if not rows:
            return {
                "row_count": 0,
                "file_size": 0,
                "file_path": None,
            }

        # Convert to PyArrow table
        schema = self._get_schema_for_table(table_name)
        data = [dict(row) for row in rows]

        # Convert boolean fields from SQLite integers (0, 1) to Python booleans
        data = self._convert_booleans(data, table_name)

        table = pa.Table.from_pylist(data, schema=schema)

        # Write to Parquet
        output_file = output_path.with_suffix(".parquet")
        pq.write_table(table, output_file, compression=compression)

        return {
            "row_count": len(rows),
            "file_size": output_file.stat().st_size,
            "file_path": str(output_file),
        }

    def _export_races_partitioned(
        self,
        output_path: Path,
        compression: str = "snappy",
    ) -> dict[str, Any]:
        """
        Export races table partitioned by season_id.

        Args:
            output_path: Output directory for partitioned files
            compression: Compression codec

        Returns:
            Dictionary with export stats
        """
        # Placeholder for partitioned export implementation
        # For now, export as single file
        return self._export_table("races", output_path, compression)

    def _export_race_results_partitioned(
        self,
        output_path: Path,
        compression: str = "snappy",
    ) -> dict[str, Any]:
        """
        Export race_results table partitioned by race_id.

        Args:
            output_path: Output directory for partitioned files
            compression: Compression codec

        Returns:
            Dictionary with export stats
        """
        # Placeholder for partitioned export implementation
        # For now, export as single file
        return self._export_table("race_results", output_path, compression)

    def _convert_booleans(self, data: list[dict], table_name: str) -> list[dict]:
        """
        Convert SQLite integer boolean values (0, 1) to Python booleans.

        Args:
            data: List of dictionaries representing rows
            table_name: Name of the table

        Returns:
            Modified data with boolean fields converted
        """
        # Define boolean fields for each table
        boolean_fields = {
            "races": [
                "points_race",
                "off_week",
                "night_race",
                "playoff_race",
                "is_complete",
            ],
            "schema_alerts": ["resolved"],
        }

        if table_name not in boolean_fields:
            return data

        # Convert integers to booleans
        fields_to_convert = boolean_fields[table_name]
        for row in data:
            for field in fields_to_convert:
                if field in row and row[field] is not None:
                    row[field] = bool(row[field])

        return data

    def _get_schema_for_table(self, table_name: str) -> pa.Schema:
        """
        Get PyArrow schema for a table.

        Args:
            table_name: Name of the table

        Returns:
            PyArrow schema definition
        """
        # Schema definitions for each table
        schemas = {
            "leagues": pa.schema(
                [
                    pa.field("league_id", pa.int64(), nullable=False),
                    pa.field("name", pa.string(), nullable=False),
                    pa.field("description", pa.string(), nullable=True),
                    pa.field("url", pa.string(), nullable=False),
                    pa.field("scraped_at", pa.string(), nullable=False),
                    pa.field("created_at", pa.string(), nullable=False),
                    pa.field("updated_at", pa.string(), nullable=False),
                ]
            ),
            "teams": pa.schema(
                [
                    pa.field("team_id", pa.int64(), nullable=False),
                    pa.field("league_id", pa.int64(), nullable=False),
                    pa.field("name", pa.string(), nullable=False),
                    pa.field("driver_count", pa.int32(), nullable=True),
                    pa.field("url", pa.string(), nullable=True),
                    pa.field("scraped_at", pa.string(), nullable=False),
                    pa.field("created_at", pa.string(), nullable=False),
                    pa.field("updated_at", pa.string(), nullable=False),
                ]
            ),
            "drivers": pa.schema(
                [
                    pa.field("driver_id", pa.int64(), nullable=False),
                    pa.field("league_id", pa.int64(), nullable=False),
                    pa.field("team_id", pa.int64(), nullable=True),
                    pa.field("name", pa.string(), nullable=False),
                    pa.field("first_name", pa.string(), nullable=True),
                    pa.field("last_name", pa.string(), nullable=True),
                    pa.field("car_numbers", pa.string(), nullable=True),
                    pa.field("primary_number", pa.string(), nullable=True),
                    pa.field("club", pa.string(), nullable=True),
                    pa.field("club_id", pa.int64(), nullable=True),
                    pa.field("irating", pa.int32(), nullable=True),
                    pa.field("safety_rating", pa.float64(), nullable=True),
                    pa.field("license_class", pa.string(), nullable=True),
                    pa.field("url", pa.string(), nullable=False),
                    pa.field("scraped_at", pa.string(), nullable=False),
                    pa.field("created_at", pa.string(), nullable=False),
                    pa.field("updated_at", pa.string(), nullable=False),
                ]
            ),
            "series": pa.schema(
                [
                    pa.field("series_id", pa.int64(), nullable=False),
                    pa.field("league_id", pa.int64(), nullable=False),
                    pa.field("name", pa.string(), nullable=False),
                    pa.field("description", pa.string(), nullable=True),
                    pa.field("created_date", pa.string(), nullable=True),
                    pa.field("num_seasons", pa.int32(), nullable=True),
                    pa.field("url", pa.string(), nullable=False),
                    pa.field("scraped_at", pa.string(), nullable=False),
                    pa.field("created_at", pa.string(), nullable=False),
                    pa.field("updated_at", pa.string(), nullable=False),
                ]
            ),
            "seasons": pa.schema(
                [
                    pa.field("season_id", pa.int64(), nullable=False),
                    pa.field("series_id", pa.int64(), nullable=False),
                    pa.field("name", pa.string(), nullable=False),
                    pa.field("description", pa.string(), nullable=True),
                    pa.field("url", pa.string(), nullable=False),
                    pa.field("scraped_at", pa.string(), nullable=False),
                    pa.field("created_at", pa.string(), nullable=False),
                    pa.field("updated_at", pa.string(), nullable=False),
                ]
            ),
            "races": pa.schema(
                [
                    pa.field("race_id", pa.int64(), nullable=False),
                    pa.field("schedule_id", pa.int64(), nullable=False),
                    pa.field("season_id", pa.int64(), nullable=False),
                    pa.field("race_number", pa.int32(), nullable=False),
                    pa.field("event_name", pa.string(), nullable=True),
                    pa.field("date", pa.string(), nullable=True),
                    pa.field("race_time", pa.string(), nullable=True),
                    pa.field("practice_time", pa.string(), nullable=True),
                    pa.field("track_id", pa.int64(), nullable=True),
                    pa.field("track_config_id", pa.int64(), nullable=True),
                    pa.field("track_name", pa.string(), nullable=True),
                    pa.field("track_type", pa.string(), nullable=True),
                    pa.field("track_length", pa.float64(), nullable=True),
                    pa.field("track_config_iracing_id", pa.string(), nullable=True),
                    pa.field("planned_laps", pa.int32(), nullable=True),
                    pa.field("points_race", pa.bool_(), nullable=True),
                    pa.field("off_week", pa.bool_(), nullable=True),
                    pa.field("night_race", pa.bool_(), nullable=True),
                    pa.field("playoff_race", pa.bool_(), nullable=True),
                    pa.field("race_duration_minutes", pa.int32(), nullable=True),
                    pa.field("total_laps", pa.int32(), nullable=True),
                    pa.field("leaders", pa.int32(), nullable=True),
                    pa.field("lead_changes", pa.int32(), nullable=True),
                    pa.field("cautions", pa.int32(), nullable=True),
                    pa.field("caution_laps", pa.int32(), nullable=True),
                    pa.field("num_drivers", pa.int32(), nullable=True),
                    pa.field("weather_type", pa.string(), nullable=True),
                    pa.field("cloud_conditions", pa.string(), nullable=True),
                    pa.field("temperature_f", pa.int32(), nullable=True),
                    pa.field("humidity_pct", pa.int32(), nullable=True),
                    pa.field("fog_pct", pa.int32(), nullable=True),
                    pa.field("weather_wind_speed", pa.string(), nullable=True),
                    pa.field("weather_wind_dir", pa.string(), nullable=True),
                    pa.field("weather_wind_unit", pa.string(), nullable=True),
                    pa.field("url", pa.string(), nullable=False),
                    pa.field("is_complete", pa.bool_(), nullable=False),
                    pa.field("scraped_at", pa.string(), nullable=False),
                    pa.field("created_at", pa.string(), nullable=False),
                    pa.field("updated_at", pa.string(), nullable=False),
                ]
            ),
            "race_results": pa.schema(
                [
                    pa.field("result_id", pa.int64(), nullable=False),
                    pa.field("race_id", pa.int64(), nullable=False),
                    pa.field("driver_id", pa.int64(), nullable=False),
                    pa.field("team", pa.string(), nullable=True),
                    pa.field("finish_position", pa.int32(), nullable=True),
                    pa.field("starting_position", pa.int32(), nullable=True),
                    pa.field("car_number", pa.string(), nullable=True),
                    pa.field("qualifying_time", pa.string(), nullable=True),
                    pa.field("fastest_lap", pa.string(), nullable=True),
                    pa.field("fastest_lap_number", pa.int32(), nullable=True),
                    pa.field("average_lap", pa.string(), nullable=True),
                    pa.field("interval", pa.string(), nullable=True),
                    pa.field("laps_completed", pa.int32(), nullable=True),
                    pa.field("laps_led", pa.int32(), nullable=True),
                    pa.field("incident_points", pa.int32(), nullable=True),
                    pa.field("race_points", pa.int32(), nullable=True),
                    pa.field("bonus_points", pa.int32(), nullable=True),
                    pa.field("penalty_points", pa.int32(), nullable=True),
                    pa.field("total_points", pa.int32(), nullable=True),
                    pa.field("fast_laps", pa.int32(), nullable=True),
                    pa.field("quality_passes", pa.int32(), nullable=True),
                    pa.field("closing_passes", pa.int32(), nullable=True),
                    pa.field("total_passes", pa.int32(), nullable=True),
                    pa.field("average_running_position", pa.float64(), nullable=True),
                    pa.field("irating", pa.int32(), nullable=True),
                    pa.field("status", pa.string(), nullable=True),
                    pa.field("car_id", pa.int64(), nullable=True),
                    pa.field("created_at", pa.string(), nullable=False),
                    pa.field("updated_at", pa.string(), nullable=False),
                ]
            ),
            "scrape_log": pa.schema(
                [
                    pa.field("log_id", pa.int64(), nullable=False),
                    pa.field("entity_type", pa.string(), nullable=False),
                    pa.field("entity_id", pa.int64(), nullable=True),
                    pa.field("entity_url", pa.string(), nullable=False),
                    pa.field("status", pa.string(), nullable=False),
                    pa.field("error_message", pa.string(), nullable=True),
                    pa.field("duration_ms", pa.int32(), nullable=True),
                    pa.field("timestamp", pa.string(), nullable=False),
                ]
            ),
            "schema_alerts": pa.schema(
                [
                    pa.field("alert_id", pa.int64(), nullable=False),
                    pa.field("entity_type", pa.string(), nullable=False),
                    pa.field("alert_type", pa.string(), nullable=False),
                    pa.field("details", pa.string(), nullable=False),
                    pa.field("url", pa.string(), nullable=True),
                    pa.field("resolved", pa.bool_(), nullable=False),
                    pa.field("timestamp", pa.string(), nullable=False),
                ]
            ),
        }

        if table_name not in schemas:
            raise ValueError(f"Unknown table: {table_name}")

        return schemas[table_name]
