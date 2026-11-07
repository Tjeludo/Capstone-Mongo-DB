import pytest
from unittest.mock import patch, MagicMock, AsyncMock
from backend.product import Product


# --- Fixtures ---
# These fixtures create mock objects for all external dependencies.

@pytest.fixture
def mock_products_table():
    """Mocks the 'products_table' database collection."""
    with patch("backend.product.products_table") as mock:
        yield mock


@pytest.fixture
def mock_size_class():
    """Mocks the 'Size' class and its methods."""
    with patch("backend.product.Size") as mock:
        mock.get_size_id.return_value = "01"  # Default mock return for size ID
        yield mock


@pytest.fixture
def mock_colour_class():
    """Mocks the 'Colour' class and its methods."""
    with patch("backend.product.Colour") as mock:
        mock.get_colour_id.return_value = "02"  # Default mock return for colour ID
        yield mock


@pytest.fixture
def mock_category_class():
    """Mocks the 'Category' class and its methods."""
    with patch("backend.product.Category") as mock:
        yield mock


@pytest.fixture
def mock_helper_class():
    """Mocks the 'Helper' class and its methods."""
    with patch("backend.product.Helper") as mock:
        yield mock


# --- Auto-use Fixture ---
# This special fixture applies the mocks to every test function automatically.
@pytest.fixture(autouse=True)
def auto_mock_all_dependencies(mock_size_class, mock_colour_class, mock_category_class, mock_helper_class):
    """Automatically applies all class mocks to every test in this file."""
    pass


# --- Test Cases ---

def test_product_initialization(mock_products_table):
    """Tests that a Product instance is initialized correctly and generates a code."""
    mock_products_table.find_one.return_value = None  # No existing products
    prod = Product("  test shoe  ", "C01", "M", "Red", 199.99)
    assert prod.product_name == "Test Shoe"  # Checks stripping and title case
    assert prod.product_code == "0010010102"  # Checks code generation


def test_generate_product_code_first_in_category(mock_products_table):
    """Tests generating a product code for the very first item in a category."""
    # Simulate no identical product and no other products in the category
    mock_products_table.find_one.side_effect = [None, None]
    prod = Product("New Shoe", "C05", "S", "Blue", 150.00)
    assert prod.product_code == "0050010102"  # Should be product #001 for category #05


def test_generate_product_code_next_in_category(mock_products_table):
    """Tests generating a sequential product code."""
    # Simulate no identical product, but another product exists in the category
    mock_products_table.find_one.side_effect = [
        None,  # No identical product
        {"product_code": "0010050102"}  # Last product was #005
    ]
    prod = Product("Another Shoe", "C01", "L", "Green", 250.00)
    assert prod.product_code == "0010060102"  # Expects next code to be #006


def test_generate_product_code_reuses_existing(mock_products_table):
    """Tests that an existing product code is reused to prevent duplicates."""
    # Simulate that an identical product already exists
    mock_products_table.find_one.return_value = {"product_code": "0010010102"}
    prod = Product("Test Shoe", "C01", "M", "Red", 199.99)
    assert prod.product_code == "0010010102"


def test_add_product_success(mock_products_table, mock_helper_class, mock_category_class):
    """Tests the successful addition of a new product."""
    mock_products_table.find_one.return_value = None  # No existing product
    prod = Product("New Kicks", "C01", "S", "Black", 500.00)
    result = prod.add_product()

    assert result["success"] is True
    assert "created successfully" in result["message"]
    mock_products_table.insert_one.assert_called_once()
    mock_helper_class.add_product_to_supplier_prod.assert_called_once()
    mock_helper_class.add_product_to_inventories.assert_called_once()
    mock_category_class.update_product_count.assert_called_once_with("C01")


def test_add_product_already_exists(mock_products_table):
    """Tests that adding a duplicate product is prevented."""
    # Simulate product_exists returning True
    mock_products_table.find_one.return_value = {"product_code": "0010010102"}
    prod = Product("Test Shoe", "C01", "M", "Red", 199.99)
    result = prod.add_product()

    assert result["success"] is False
    assert "already exists" in result["message"]


def test_update_product_success(mock_products_table):
    """Tests successfully updating a product's name and price."""
    mock_products_table.find_one.return_value = {"product_code": "P01"}  # Product exists
    result = Product.update_product("P01", "  updated name  ", 99.99)

    assert result["success"] is True
    # Check that input was stripped and title-cased
    expected_update = {"$set": {"product_name": "Updated Name", "price(ZAR)": 99.99}}
    mock_products_table.update_one.assert_called_with({"product_code": "P01"}, expected_update)


def test_update_product_not_found(mock_products_table):
    """Tests that updating a non-existent product fails."""
    mock_products_table.find_one.return_value = None  # Product does not exist
    result = Product.update_product("P99", "Fake", 1.00)
    assert result["success"] is False
    assert "does not exist" in result["message"]


def test_delete_product_success(mock_products_table, mock_helper_class, mock_category_class):
    """Tests the successful deletion of a product."""
    mock_products_table.find_one.return_value = {"product_code": "P01", "category_code": "C01"}
    result = Product.delete_product("P01")

    assert result["success"] is True
    mock_products_table.delete_one.assert_called_once_with({"product_code": "P01"})
    mock_helper_class.remove_product_from_supplier_prod.assert_called_once_with("P01")
    mock_category_class.update_product_count.assert_called_once_with("C01")


def test_delete_product_not_found(mock_products_table):
    """Tests that deleting a non-existent product fails."""
    mock_products_table.find_one.return_value = None
    result = Product.delete_product("P99")
    assert result["success"] is False
    assert "does not exist" in result["message"]


def test_search_products_by_name_found(mock_products_table):
    """Tests searching for products by name when matches are found."""
    mock_products_table.find.return_value = [{"product_name": "Blue Shoe"}]
    result = Product.search_products_by_name("shoe")
    assert result["success"] is True
    assert len(result["data"]) == 1


def test_search_products_by_name_not_found(mock_products_table):
    """Tests searching for products by name when no matches are found."""
    mock_products_table.find.return_value = []  # Simulate no results
    result = Product.search_products_by_name("nonexistent")
    assert result["success"] is False
    assert "No products found" in result["message"]


@pytest.mark.asyncio
async def test_upload_products_csv_success(mock_products_table):
    """Tests a successful CSV upload to an empty table."""
    mock_products_table.count_documents.return_value = 0  # Table is empty
    mock_products_table.find_one.return_value = None  # No duplicates

    csv_content = "product_name,category_code,size,colour,price\nShoe,C01,M,Red,100\n"
    fake_file = AsyncMock()
    fake_file.read.return_value = csv_content.encode('utf-8')

    result = await Product.upload_products_csv(fake_file)
    assert result["success"] is True
    assert "Upload successful" in result["message"]


@pytest.mark.asyncio
async def test_upload_products_csv_table_not_empty(mock_products_table):
    """Tests that CSV upload is blocked if the table is not empty."""
    mock_products_table.count_documents.return_value = 1  # Table has data
    fake_file = AsyncMock()
    result = await Product.upload_products_csv(fake_file)
    assert result["success"] is False
    assert "already contains data" in result["message"]


@pytest.mark.asyncio
async def test_upload_products_csv_invalid_columns(mock_products_table):
    """Tests CSV upload with missing or extra columns."""
    mock_products_table.count_documents.return_value = 0

    # Test with missing columns
    csv_content_missing = "product_name,category_code,size,colour\nShoe,C01,M,Red\n"
    fake_file_missing = AsyncMock()
    fake_file_missing.read.return_value = csv_content_missing.encode('utf-8')
    result_missing = await Product.upload_products_csv(fake_file_missing)
    assert result_missing["success"] is False
    assert "must contain exactly these columns" in result_missing["message"]

    # Test with extra columns
    csv_content_extra = "product_name,category_code,size,colour,price,extra\nShoe,C01,M,Red,100,abc\n"
    fake_file_extra = AsyncMock()
    fake_file_extra.read.return_value = csv_content_extra.encode('utf-8')
    result_extra = await Product.upload_products_csv(fake_file_extra)
    assert result_extra["success"] is False
    assert "unexpected columns" in result_extra["message"]