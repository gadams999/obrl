"""Command-line interface for Parquet export utility."""

import argparse
import sys
from pathlib import Path

from src.export import ParquetExporter


def main():
    """Main entry point for export CLI."""
    parser = argparse.ArgumentParser(
        description="Export SimRacer Scraper database to Parquet format",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
  # Export all tables with default settings
  python -m src.export_cli --database obrl-nov8.db --output ./parquet_output

  # Export with GZIP compression
  python -m src.export_cli --database obrl-nov8.db --output ./parquet_output --compression gzip

  # Export without partitioning
  python -m src.export_cli --database obrl-nov8.db --output ./parquet_output --no-partition

  # Using uv
  uv run python -m src.export_cli --database obrl-nov8.db --output ./parquet_output
        """,
    )

    parser.add_argument(
        "-d",
        "--database",
        required=True,
        help="Path to SQLite database file",
    )

    parser.add_argument(
        "-o",
        "--output",
        required=True,
        help="Output directory for Parquet files",
    )

    parser.add_argument(
        "-c",
        "--compression",
        choices=["snappy", "gzip", "zstd", "none"],
        default="snappy",
        help="Compression codec (default: snappy)",
    )

    parser.add_argument(
        "--no-partition",
        action="store_true",
        help="Disable partitioning for large tables",
    )

    parser.add_argument(
        "-v",
        "--verbose",
        action="store_true",
        help="Enable verbose output",
    )

    args = parser.parse_args()

    # Validate database exists
    db_path = Path(args.database)
    if not db_path.exists():
        print(f"Error: Database file not found: {args.database}", file=sys.stderr)
        sys.exit(1)

    # Create output directory
    output_path = Path(args.output)
    output_path.mkdir(parents=True, exist_ok=True)

    print(f"Exporting database: {args.database}")
    print(f"Output directory: {args.output}")
    print(f"Compression: {args.compression}")
    print(f"Partitioning: {'disabled' if args.no_partition else 'enabled'}")
    print()

    try:
        with ParquetExporter(str(db_path)) as exporter:
            metadata = exporter.export_all(
                output_dir=args.output,
                compression=args.compression,
                partitioned=not args.no_partition,
            )

            # Print summary
            print("Export completed successfully!")
            print()
            print("Summary:")
            print(f"  Export timestamp: {metadata['export_timestamp']}")
            print(f"  Source database: {metadata['source_database']}")
            print(f"  Compression: {metadata['compression']}")
            print()

            total_rows = 0
            total_size = 0

            print("Tables exported:")
            for table_name, table_info in metadata["tables"].items():
                row_count = table_info["row_count"]
                file_size = table_info["file_size"]
                total_rows += row_count
                total_size += file_size

                # Format file size
                if file_size < 1024:
                    size_str = f"{file_size} B"
                elif file_size < 1024 * 1024:
                    size_str = f"{file_size / 1024:.2f} KB"
                else:
                    size_str = f"{file_size / (1024 * 1024):.2f} MB"

                print(f"  {table_name:20s} {row_count:8,d} rows    {size_str:>12s}")

                if args.verbose and table_info["file_path"]:
                    print(f"    → {table_info['file_path']}")

            print()
            print(f"Total: {total_rows:,} rows, {total_size / (1024 * 1024):.2f} MB")
            print()
            print(f"Metadata saved to: {output_path / '_metadata.json'}")

    except Exception as e:
        print(f"Error during export: {e}", file=sys.stderr)
        if args.verbose:
            import traceback

            traceback.print_exc()
        sys.exit(1)


if __name__ == "__main__":
    main()
