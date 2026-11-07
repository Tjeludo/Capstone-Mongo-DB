import pytest
from unittest.mock import patch, MagicMock, AsyncMock
from backend.category import Category

# ------------------------------
# Fixtures
# ------------------------------
@pytest.fixture
def mock_category_table():
    """Mocks the 'category_table' database collection for isolated testing."""
    with patch("backend.category.category_table") as mock:
        yield mock

@pytest.fixture
def mock_products_table():
    """Mocks the 'products_table' database collection for isolated testing."""
    with patch("backend.category.products_table") as mock:
        yield mock


# ------------------------------
# Tests for Category class
# ------------------------------

def test_generate_category_code_new(mock_category_table):
    """Tests if a new sequential code (e.g., C06) is generated when other categories exist."""
    mock_category_table.find_one.side_effect = [None, {"category_code": "C05"}]
    c = Category("Electronics", "Devices and gadgets")
    assert c.category_code == "C06"


def test_generate_category_code_first(mock_category_table):
    """Tests if the first category code is 'C01' when the database is empty."""
    mock_category_table.find_one.side_effect = [None, None]
    c = Category("Books", "Reading materials")
    assert c.category_code == "C01"


def test_generate_category_code_existing(mock_category_table):
    """Tests if an existing code is reused if a category with the same name is created."""
    mock_category_table.find_one.return_value = {"category_code": "C03"}
    c = Category("Books", "Reading materials")
    code = c.generate_category_code()
    assert code == "C03"  # should reuse existing


def test_add_category_success(mock_category_table):
    """Tests the successful creation of a new, non-duplicate category."""
    mock_category_table.find_one.return_value = None
    mock_category_table.insert_one.return_value = MagicMock()
    c = Category("Toys", "Kids play items")
    result = c.add_category()
    assert result["success"] is True
    assert "created successfully" in result["message"]


def test_add_category_already_exists(mock_category_table):
    """Tests that creating a duplicate category is correctly prevented."""
    mock_category_table.find_one.return_value = {"category_code": "C01"}
    c = Category("Toys", "Kids play items")
    result = c.add_category()
    assert result["success"] is False
    assert "already exists" in result["message"]


def test_category_exists_true(mock_category_table):
    """Tests if category_exists returns True for an existing category."""
    mock_category_table.find_one.return_value = {"category_code": "C01"}
    assert Category.category_exists("C01") is True


def test_category_exists_false(mock_category_table):
    """Tests if category_exists returns False for a non-existent category."""
    mock_category_table.find_one.return_value = None
    assert Category.category_exists("C01") is False


def test_update_category_success(mock_category_table):
    """Tests the successful update of an existing category's details."""
    mock_category_table.find_one.return_value = {"category_code": "C01"}
    mock_category_table.update_one.return_value = MagicMock()
    result = Category.update_category("C01", "Updated", "Desc")
    assert result["success"] is True


def test_update_category_case_standardization(mock_category_table):
    """Tests that update_category correctly standardizes the case of inputs."""
    mock_category_table.find_one.return_value = {"category_code": "C01"}
    mock_category_table.update_one.return_value = MagicMock()
    Category.update_category("C01", "lowercase name", "UPPERCASE DESC")
    # Verify that the data sent to the DB was correctly formatted
    expected_update = {"$set": {"category_name": "Lowercase Name", "description": "uppercase desc"}}
    mock_category_table.update_one.assert_called_with({"category_code": "C01"}, expected_update)


def test_update_category_not_exists(mock_category_table):
    """Tests that updating a non-existent category fails as expected."""
    mock_category_table.find_one.return_value = None
    result = Category.update_category("C99", "X", "Y")
    assert result["success"] is False


def test_view_categories_with_data(mock_category_table):
    """Tests viewing all categories when the database contains data."""
    mock_category_table.find.return_value = [{"category_code": "C01"}]
    result = Category.view_categories()
    assert result["success"] is True
    assert len(result["data"]) == 1


def test_view_categories_empty(mock_category_table):
    """Tests viewing categories when the database is empty."""
    mock_category_table.find.return_value = []
    result = Category.view_categories()
    assert result["success"] is False


def test_view_category_products_found(mock_category_table, mock_products_table):
    """Tests retrieving products for a category that contains products."""
    mock_category_table.find_one.return_value = {"category_code": "C01"}
    mock_products_table.find.return_value = [{"product": "Laptop"}]
    result = Category.view_category_products("C01")
    assert result["success"] is True
    assert result["data"][0]["product"] == "Laptop"


def test_view_category_products_category_not_found(mock_category_table):
    """Tests retrieving products when the category itself does not exist."""
    # Simulate category_exists returning False
    mock_category_table.find_one.return_value = None
    result = Category.view_category_products("C99")
    assert result["success"] is False
    assert "Category does not exist" in result["message"]


def test_view_category_products_none(mock_category_table, mock_products_table):
    """Tests retrieving products for a category that has no products."""
    mock_category_table.find_one.return_value = {"category_code": "C01"}
    mock_products_table.find.return_value = []
    result = Category.view_category_products("C01")
    assert result["success"] is False


def test_search_category_found(mock_category_table):
    """Tests that a single category can be successfully found by its code."""
    mock_category_table.find_one.side_effect = [
        {"category_code": "C01"}, {"category_code": "C01", "category_name": "Books"}
    ]
    result = Category.search_category("C01")
    assert result["success"] is True


def test_download_category_csv(mock_category_table):
    """Tests that category data can be correctly formatted into a CSV string."""
    mock_category_table.find.return_value = [{"category_code": "C01", "category_name": "Books"}]
    csv = Category.download_category_csv()
    assert "category_code" in csv
    assert "Books" in csv


@pytest.mark.asyncio
async def test_upload_categories_csv_success(mock_category_table):
    """Tests the successful upload and processing of a valid CSV file."""
    mock_category_table.count_documents.return_value = 0
    mock_category_table.find_one.return_value = None
    mock_category_table.insert_one.return_value = MagicMock()
    fake_file = AsyncMock()
    csv_content = "category_name,description\nBooks,Reading\n"
    fake_file.read.return_value = csv_content.encode()
    result = await Category.upload_categories_csv(fake_file)
    assert result["success"] is True


@pytest.mark.asyncio
async def test_upload_csv_invalid_columns(mock_category_table):
    """Tests CSV upload failure when the column names are incorrect."""
    mock_category_table.count_documents.return_value = 0
    fake_file = AsyncMock()
    # CSV content with wrong column headers
    csv_content = "name,desc\nBooks,Reading\n"
    fake_file.read.return_value = csv_content.encode()
    result = await Category.upload_categories_csv(fake_file)
    assert result["success"] is False
    assert "must contain exactly these columns" in result["message"]


@pytest.mark.asyncio
async def test_upload_categories_csv_table_not_empty(mock_category_table):
    """Tests that CSV upload is blocked if the categories table is not empty."""
    mock_category_table.count_documents.return_value = 1
    fake_file = AsyncMock()
    result = await Category.upload_categories_csv(fake_file)
    assert result["success"] is False
    assert "table is not empty" in result["message"]


def test_update_product_count(mock_category_table, mock_products_table):
    """Tests that the product count for a category is updated correctly."""
    mock_category_table.find_one.return_value = {"category_code": "C01"}
    mock_products_table.count_documents.return_value = 5
    mock_category_table.update_one.return_value = MagicMock()
    Category.update_product_count("C01")
    mock_category_table.update_one.assert_called_once()