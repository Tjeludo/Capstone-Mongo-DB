import pytest
from unittest.mock import patch, MagicMock, AsyncMock
from backend.supplier_products import SupplierItem


# --- Fixtures ---

@pytest.fixture
def mock_supplier_products_table():
    """Mocks the 'supplier_products_table' database collection."""
    with patch("backend.supplier_products.supplier_products_table") as mock:
        yield mock


@pytest.fixture
def mock_supplier_class():
    """Mocks the 'Supplier' class to control its behavior."""
    with patch("backend.supplier_products.Supplier") as mock:
        # Assume the supplier exists by default for most tests
        mock.supplier_exists.return_value = True
        yield mock


# This special fixture applies the mocks to every test function automatically.
@pytest.fixture(autouse=True)
def auto_mock_dependencies(mock_supplier_products_table, mock_supplier_class):
    """Automatically applies all mocks to every test."""
    pass


# --- Test Cases ---

def test_add_product_success(mock_supplier_products_table):
    """Tests the successful addition of a new product to a supplier."""
    # Simulate that the product does not already exist for this supplier
    mock_supplier_products_table.find_one.return_value = None

    # The __init__ in the source code is slightly different from how add_product is used.
    # We will simulate the instance creation that the API does.
    item = SupplierItem(supplier_id="S01", product_code="P123")
    item.price = 100  # Manually set price as the API would

    result = item.add_product()

    assert result["success"] is True
    assert "added successfully" in result["message"]
    mock_supplier_products_table.update_one.assert_called_once()


def test_add_product_already_exists(mock_supplier_products_table):
    """Tests that adding a duplicate product to a supplier is prevented."""
    # Simulate that the product already exists
    mock_supplier_products_table.find_one.return_value = {"_id": "some_id"}

    item = SupplierItem(supplier_id="S01", product_code="P123")
    result = item.add_product()

    assert result["success"] is False
    assert "already exists" in result["message"]


def test_add_product_supplier_not_found(mock_supplier_class):
    """Tests that a product cannot be added if the supplier does not exist."""
    mock_supplier_class.supplier_exists.return_value = False

    item = SupplierItem(supplier_id="S99", product_code="P123")
    result = item.add_product()

    assert result["success"] is False
    assert "Supplier not found" in result["message"]


def test_update_supplier_price_success(mock_supplier_products_table):
    """Tests a successful price update for an existing supplier product."""
    # Mock the supplier_product_exists static method to return True
    with patch("backend.supplier_products.SupplierItem.supplier_product_exists", return_value=True):
        result = SupplierItem.update_supplier_price("S01", "P123", 199.99)

        assert result["success"] is True
        mock_supplier_products_table.update_one.assert_called_with(
            {"supplier_id": "S01", "products.product_code": "P123"},
            {"$set": {"products.$.price(ZAR)": 199.99}}
        )


def test_delete_supplier_product_success(mock_supplier_products_table):
    """Tests the successful deletion of a product from a supplier's list."""
    with patch("backend.supplier_products.SupplierItem.supplier_product_exists", return_value=True):
        result = SupplierItem.delete_supplier_product("S01", "P123")

        assert result["success"] is True
        mock_supplier_products_table.update_one.assert_called_with(
            {"supplier_id": "S01"},
            {"$pull": {"products": {"product_code": "P123"}}}
        )


def test_view_products_for_supplier_success(mock_supplier_products_table):
    """Tests retrieving and flattening product data for a single supplier."""
    mock_return = {
        "supplier_id": "S01",
        "products": [
            {"product_code": "P123", "price(ZAR)": 100},
            {"product_code": "P456", "price(ZAR)": 200}
        ]
    }
    mock_supplier_products_table.find_one.return_value = mock_return
    result = SupplierItem.view_products_for_supplier("S01")

    assert result["success"] is True
    assert len(result["data"]) == 2
    assert result["data"][0]["supplier_id"] == "S01"
    assert result["data"][1]["product_code"] == "P456"


def test_view_all_supplier_products_success(mock_supplier_products_table):
    """Tests retrieving and flattening product data from all suppliers."""
    mock_return = [
        {"supplier_id": "S01", "products": [{"product_code": "P123"}]},
        {"supplier_id": "S02", "products": [{"product_code": "P789"}]}
    ]
    mock_supplier_products_table.find.return_value = mock_return
    result = SupplierItem.view_supplier_products()

    assert result["success"] is True
    assert len(result["data"]) == 2
    assert result["data"][0]["supplier_id"] == "S01"
    assert result["data"][1]["supplier_id"] == "S02"


@pytest.mark.asyncio
async def test_upload_prices_csv_success(mock_supplier_products_table):
    """Tests a successful price update via CSV upload."""
    # Since the upload method calls the static update method, we patch that.
    with patch("backend.supplier_products.SupplierItem.update_supplier_price") as mock_update:
        mock_update.return_value = {"success": True}

        csv_content = "product_code,price\nP123,150.50\nP456,250.75"
        fake_file = AsyncMock()
        fake_file.read.return_value = csv_content.encode('utf-8')

        result = await SupplierItem.upload_prices_for_supplier_csv("S01", fake_file)

        assert result["success"] is True
        assert "Products updated: 2" in result["message"]
        assert mock_update.call_count == 2