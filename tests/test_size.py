import pytest
from unittest.mock import patch
from backend.size import Size


# --- Fixtures ---

@pytest.fixture
def mock_sizes_table():
    """Mocks the 'sizes_table' database collection for all tests."""
    with patch("backend.size.sizes_table") as mock:
        yield mock


# This auto-use fixture applies the mock to every test function automatically.
@pytest.fixture(autouse=True)
def auto_mock_db(mock_sizes_table):
    """Automatically applies the sizes_table mock to every test."""
    pass


# --- Test Cases ---

def test_add_size_success(mock_sizes_table):
    """Tests that a new size is added when it doesn't already exist."""
    # Simulate that the size is not found in the database
    mock_sizes_table.find_one.return_value = None

    size = Size(name="newborn", code=1)
    size.add_size()

    # Verify that the database was called to insert the new size
    mock_sizes_table.insert_one.assert_called_once()
    inserted_doc = mock_sizes_table.insert_one.call_args[0][0]
    assert inserted_doc["size_name"] == "newborn"
    assert inserted_doc["size_code"] == 1


def test_add_size_already_exists(mock_sizes_table):
    """Tests that a duplicate size is not added."""
    # Simulate that the size is found in the database
    mock_sizes_table.find_one.return_value = {"size_name": "newborn", "size_code": 1}

    size = Size(name="newborn", code=1)
    size.add_size()

    # Verify that insert_one was NOT called
    mock_sizes_table.insert_one.assert_not_called()


def test_view_sizes_with_data(mock_sizes_table):
    """Tests that the view function processes and prints data when sizes exist."""
    mock_sizes_table.find.return_value = [{"size_name": "newborn", "size_code": 1}]

    # Patch the 'tabulate' function to prevent printing to console during tests
    with patch("backend.size.tabulate") as mock_tabulate:
        Size.view_sizes()
        # Verify that tabulate was called, meaning data was processed
        mock_tabulate.assert_called_once()


def test_view_sizes_no_data(mock_sizes_table):
    """Tests that the view function handles cases where no sizes are in the database."""
    mock_sizes_table.find.return_value = []  # Simulate an empty database

    with patch("backend.size.tabulate") as mock_tabulate:
        Size.view_sizes()
        # Verify that tabulate was NOT called
        mock_tabulate.assert_not_called()


def test_get_size_id_found(mock_sizes_table):
    """Tests retrieving the ID for an existing size name."""
    mock_sizes_table.find_one.return_value = {"size_name": "3-6 months", "size_code": 3}
    size_id = Size.get_size_id("3-6 months")
    assert size_id == 3


def test_get_size_id_not_found(mock_sizes_table):
    """Tests retrieving the ID for a non-existent size name."""
    mock_sizes_table.find_one.return_value = None
    size_id = Size.get_size_id("Toddler")
    assert size_id is None