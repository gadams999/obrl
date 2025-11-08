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
        orch = Orchestrator(database=test_db, validator=schema_validator, rate_limit_seconds=5.0)
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


class TestOrchestratorHelpers:
    """Test helper methods."""

    def test_parse_driver_name_comma_format(self, orchestrator):
        """Test parsing 'LastName, FirstName' format (primary format)."""
        first, last = orchestrator._parse_driver_name("Doe, John")
        assert first == "John"
        assert last == "Doe"

    def test_parse_driver_name_comma_with_middle_name(self, orchestrator):
        """Test parsing 'LastName, FirstName MiddleName' format."""
        first, last = orchestrator._parse_driver_name("Smith, John Michael")
        assert first == "John Michael"
        assert last == "Smith"

    def test_parse_driver_name_comma_with_suffix(self, orchestrator):
        """Test parsing 'LastName, FirstName' with suffix in first name."""
        first, last = orchestrator._parse_driver_name("Doe, John Jr.")
        assert first == "John Jr."
        assert last == "Doe"

    def test_parse_driver_name_comma_only_last_name(self, orchestrator):
        """Test parsing comma with only last name."""
        first, last = orchestrator._parse_driver_name("Doe, ")
        assert first is None
        assert last == "Doe"

    def test_parse_driver_name_comma_only_first_name(self, orchestrator):
        """Test parsing comma with only first name."""
        first, last = orchestrator._parse_driver_name(", John")
        assert first == "John"
        assert last is None

    def test_parse_driver_name_comma_with_whitespace(self, orchestrator):
        """Test parsing comma format with extra whitespace."""
        first, last = orchestrator._parse_driver_name("  Doe  ,  John  ")
        assert first == "John"
        assert last == "Doe"

    def test_parse_driver_name_hyphenated_last_name_comma(self, orchestrator):
        """Test parsing hyphenated last name in comma format."""
        first, last = orchestrator._parse_driver_name("Smith-Jones, Mary")
        assert first == "Mary"
        assert last == "Smith-Jones"

    def test_parse_driver_name_no_comma_full_name(self, orchestrator):
        """Test parsing 'FirstName LastName' format (fallback)."""
        first, last = orchestrator._parse_driver_name("John Doe")
        assert first == "John"
        assert last == "Doe"

    def test_parse_driver_name_no_comma_with_middle(self, orchestrator):
        """Test parsing 'FirstName MiddleName LastName' format (fallback)."""
        first, last = orchestrator._parse_driver_name("John Michael Smith")
        assert first == "John"
        assert last == "Michael Smith"

    def test_parse_driver_name_single_name(self, orchestrator):
        """Test parsing single name returns first name only."""
        first, last = orchestrator._parse_driver_name("John")
        assert first == "John"
        assert last is None

    def test_parse_driver_name_empty_string(self, orchestrator):
        """Test parsing empty string returns None for both."""
        first, last = orchestrator._parse_driver_name("")
        assert first is None
        assert last is None

    def test_parse_driver_name_whitespace_only(self, orchestrator):
        """Test parsing whitespace-only string returns None for both."""
        first, last = orchestrator._parse_driver_name("   ")
        assert first is None
        assert last is None

    def test_parse_driver_name_none(self, orchestrator):
        """Test parsing None returns None for both."""
        first, last = orchestrator._parse_driver_name(None)
        assert first is None
        assert last is None
