#!/usr/bin/env python3
"""
Simple wrapper script to run the SimRacer scraper.

This script allows users to run the scraper without using the -m flag:
    python scraper.py scrape league 1558

Instead of:
    python -m src.cli scrape league 1558
"""

import sys
from src.cli import main

if __name__ == "__main__":
    sys.exit(main())
