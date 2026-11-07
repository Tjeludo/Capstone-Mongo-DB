import pytest
from unittest.mock import patch, MagicMock
from backend.colour import Colour


# --- Fixtures ---

@pytest.fixture
def mock_colours_table():
    """Mocks the 'colours_table' database collection for all tests."""
    with patch("backend.colour.colours_table") as mock:
        yield mock


# This auto-use fixture applies the mock to every test function automatically.
@pytest.fixture(autouse=True)
def auto_mock_db(mock_colours_table):
    """Automatically applies the colours_table mock to every test."""
    pass


# --- Test Cases ---

def test_add_colour_success(mock_colours_table):
    """Tests that a new colour is added when it doesn't already exist."""
    # Simulate that the colour is not found in the database
    mock_colours_table.find_one.return_value = None

    colour = Colour(colour_name="Orange", colour_code=5)
    colour.add_colour()

    # Verify that the database was called to insert the new colour
    mock_colours_table.insert_one.assert_called_once()
    inserted_doc = mock_colours_table.insert_one.call_args[0][0]
    assert inserted_doc["colour_name"] == "Orange"
    assert inserted_doc["colour_code"] == 5


def test_add_colour_already_exists(mock_colours_table):
    """Tests that a duplicate colour is not added."""
    # Simulate that the colour is found in the database
    mock_colours_table.find_one.return_value = {"colour_name": "Red", "colour_code": 1}

    colour = Colour(colour_name="Red", colour_code=1)
    colour.add_colour()

    # Verify that insert_one was NOT called
    mock_colours_table.insert_one.assert_not_called()


def test_view_colours_with_data(mock_colours_table):
    """Tests that the view function processes and prints data when colours exist."""
    mock_colours_table.find.return_value = [{"colour_name": "Red", "colour_code": 1}]

    # Patch the 'tabulate' function to prevent printing to console during tests
    with patch("backend.colour.tabulate") as mock_tabulate:
        Colour.view_colours()
        # Verify that tabulate was called, meaning data was processed
        mock_tabulate.assert_called_once()


def test_view_colours_no_data(mock_colours_table):
    """Tests that the view function handles cases where no colours are in the database."""
    mock_colours_table.find.return_value = []  # Simulate an empty database

    with patch("backend.colour.tabulate") as mock_tabulate:
        Colour.view_colours()
        # Verify that tabulate was NOT called
        mock_tabulate.assert_not_called()


def test_get_colour_id_found(mock_colours_table):
    """Tests retrieving the ID for an existing colour name."""
    mock_colours_table.find_one.return_value = {"colour_name": "Blue", "colour_code": 2}
    colour_id = Colour.get_colour_id("Blue")
    assert colour_id == 2


def test_get_colour_id_not_found(mock_colours_table):
    """Tests retrieving the ID for a non-existent colour name."""
    mock_colours_table.find_one.return_value = None
    colour_id = Colour.get_colour_id("Ochre")
    assert colour_id is None