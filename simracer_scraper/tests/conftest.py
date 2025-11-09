"""Shared pytest fixtures."""

import sys
from pathlib import Path

import pytest

# Add claude_temp to path for testing work-in-progress code
CLAUDE_TEMP = Path(__file__).parent.parent / "claude_temp"
if CLAUDE_TEMP.exists():
    for task_dir in CLAUDE_TEMP.iterdir():
        if task_dir.is_dir():
            sys.path.insert(0, str(task_dir))


@pytest.fixture
def test_db():
    """Provide clean test database for each test."""
    # Import from current task directory or src
    try:
        from database import Database
    except ImportError:
        from src.database import Database

    db = Database(":memory:")
    db.connect()
    db.initialize_schema()
    yield db
    db.close()
