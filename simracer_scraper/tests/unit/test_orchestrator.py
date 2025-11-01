"""Tests for Orchestrator."""

from pathlib import Path
from unittest.mock import MagicMock, patch

import pytest

from src.database import Database
from src.orchestrator import Orchestrator
from src.schema_validator import SchemaValidator


@pytest.fixture
def test_db():
    """Create a temporary test database."""
    db = Database(":memory:")
    db.connect()
    yield db
    db.close()


@pytest.fixture
def schema_validator():
    """Create a SchemaValidator instance."""
    return SchemaValidator()


@pytest.fixture
def orchestrator(test_db, schema_validator):
    """Create an Orchestrator instance."""
    return Orchestrator(database=test_db, validator=schema_validator, rate_limit_seconds=0)


class TestOrchestratorInitialization:
    """Test orchestrator initialization."""

    def test_init_with_database(self, test_db, schema_validator):
        """Test orchestrator initializes with database."""
        orch = Orchestrator(database=test_db, validator=schema_validator)
        assert orch.db == test_db
        assert orch.validator == schema_validator
        assert orch.rate_limit_seconds == 2.0  # default

    def test_init_with_custom_rate_limit(self, test_db, schema_validator):
        """Test orchestrator initializes with custom rate limit."""
        orch = Orchestrator(
            database=test_db, validator=schema_validator, rate_limit_seconds=5.0
        )
        assert orch.rate_limit_seconds == 5.0

    def test_init_creates_extractors(self, orchestrator):
        """Test orchestrator creates extractor instances."""
        assert hasattr(orchestrator, "league_extractor")
        assert hasattr(orchestrator, "series_extractor")
        assert hasattr(orchestrator, "season_extractor")
        assert hasattr(orchestrator, "race_extractor")

    def test_init_creates_progress_tracker(self, orchestrator):
        """Test orchestrator initializes progress tracker."""
        assert hasattr(orchestrator, "progress")
        assert isinstance(orchestrator.progress, dict)


class TestOrchestratorProgress:
    """Test progress tracking."""

    def test_reset_progress(self, orchestrator):
        """Test resetting progress tracker."""
        # Modify progress
        orchestrator.progress["leagues_scraped"] = 5
        orchestrator.progress["errors"].append("test error")

        # Reset
        orchestrator.reset_progress()

        # Check reset
        assert orchestrator.progress["leagues_scraped"] == 0
        assert orchestrator.progress["errors"] == []
        assert orchestrator.progress["skipped_cached"] == 0

    def test_get_progress(self, orchestrator):
        """Test getting progress returns a copy."""
        # Modify progress
        orchestrator.progress["leagues_scraped"] = 3

        # Get progress
        progress = orchestrator.get_progress()

        # Check it's a copy
        assert progress["leagues_scraped"] == 3
        progress["leagues_scraped"] = 999  # Modify copy

        # Original unchanged
        assert orchestrator.progress["leagues_scraped"] == 3


class TestOrchestratorContextManager:
    """Test context manager functionality."""

    def test_context_manager_works(self, test_db, schema_validator):
        """Test orchestrator works as context manager."""
        with Orchestrator(database=test_db, validator=schema_validator) as orch:
            assert orch is not None
            assert hasattr(orch, "db")

    def test_context_manager_cleanup(self, test_db, schema_validator):
        """Test context manager cleanup doesn't raise errors."""
        orch = Orchestrator(database=test_db, validator=schema_validator)
        orch.__enter__()
        result = orch.__exit__(None, None, None)
        # Should return False (don't suppress exceptions)
        assert result is False
