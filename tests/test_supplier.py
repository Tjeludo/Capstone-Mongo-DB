import pytest
from unittest.mock import patch, MagicMock, AsyncMock
from backend.supplier import Supplier


# --- Fixtures ---

@pytest.fixture
def mock_suppliers_table():
    """Mocks the 'suppliers_table' database collection."""
    with patch("backend.supplier.suppliers_table") as mock:
        yield mock


@pytest.fixture
def mock_supplier_products_table():
    """Mocks the 'supplier_products' database collection."""
    with patch("backend.supplier.supplier_products") as mock:
        yield mock


# This special fixture applies the mocks to every test function automatically.
@pytest.fixture(autouse=True)
def auto_mock_db_tables(mock_suppliers_table, mock_supplier_products_table):
    """Automatically applies all database table mocks to every test."""
    pass


# --- Test Cases ---

def test_supplier_initialization(mock_suppliers_table):
    """Tests that a Supplier instance is initialized with correctly formatted data."""
    mock_suppliers_table.find_one.return_value = None  # No existing supplier
    supplier = Supplier("  test co.  ", "  TEST@EMAIL.COM  ", " 123 ", " 123 Main St ", "  john doe  ")

    assert supplier.supplier_name == "Test Co."
    assert supplier.email == "test@email.com"
    assert supplier.phone_number == "123"
    assert supplier.address == "123 Main St"
    assert supplier.sales_rep == "John Doe"
    assert supplier.supplier_id == "S01"  # Expects first ID


def test_add_supplier_success(mock_suppliers_table, mock_supplier_products_table):
    """Tests the successful creation of a new supplier."""
    mock_suppliers_table.find_one.return_value = None  # No existing supplier
    supplier = Supplier("New Supplier", "new@test.com", "123", "addr", "rep")
    result = supplier.add_supplier()

    assert result["success"] is True
    assert "created successfully" in result["message"]
    mock_suppliers_table.insert_one.assert_called_once()
    mock_supplier_products_table.insert_one.assert_called_once()


def test_add_supplier_already_exists(mock_suppliers_table):
    """Tests that adding a duplicate supplier is prevented."""
    mock_suppliers_table.find_one.return_value = {"supplier_id": "S01"}  # Simulate it exists
    supplier = Supplier("Existing Supplier", "exist@test.com", "123", "addr", "rep")
    result = supplier.add_supplier()

    assert result["success"] is False
    assert "already exists" in result["message"]


def test_generate_supplier_id_first(mock_suppliers_table):
    """Tests generating the first supplier ID (S01) in an empty database."""
    mock_suppliers_table.find_one.return_value = None
    supplier = Supplier("First Co", "first@test.com", "123", "addr", "rep")
    assert supplier.supplier_id == "S01"


def test_generate_supplier_id_next(mock_suppliers_table):
    """Tests generating a sequential supplier ID (e.g., S02)."""
    # First call (check by name) returns None, second call (get highest ID) returns S01.
    mock_suppliers_table.find_one.side_effect = [None, {"supplier_id": "S01"}]
    supplier = Supplier("Second Co", "second@test.com", "123", "addr", "rep")
    assert supplier.supplier_id == "S02"


def test_generate_supplier_id_reuses_existing(mock_suppliers_table):
    """Tests that an existing ID is reused if the supplier name/email match."""
    mock_suppliers_table.find_one.return_value = {"supplier_id": "S99"}
    supplier = Supplier("Existing Co", "exist@test.com", "123", "addr", "rep")
    assert supplier.supplier_id == "S99"


def test_update_supplier_success(mock_suppliers_table):
    """Tests that updating a supplier correctly formats data and calls the database."""
    mock_suppliers_table.find_one.return_value = {"supplier_id": "S01"}  # Supplier exists
    Supplier.update_supplier("S01", " new name ", " NEW@EMAIL.COM ", "456", " new addr ", " new rep ")

    expected_update = {"$set": {
        "supplier_name": "New Name", "email": "new@email.com",
        "phone_number": "456", "address": "New Addr", "sales_rep": "New Rep"
    }}
    mock_suppliers_table.update_one.assert_called_with({"supplier_id": "S01"}, expected_update)


def test_delete_supplier_success(mock_suppliers_table, mock_supplier_products_table):
    """Tests that a supplier and their products document are both deleted."""
    mock_suppliers_table.find_one.return_value = {"supplier_id": "S01"}  # Supplier exists
    result = Supplier.delete_supplier("S01")

    assert result["success"] is True
    mock_suppliers_table.delete_one.assert_called_once_with({"supplier_id": "S01"})
    mock_supplier_products_table.delete_one.assert_called_once_with({"supplier_id": "S01"})


def test_view_supplier_products_success(mock_suppliers_table, mock_supplier_products_table):
    """Tests retrieving products for an existing supplier."""
    mock_suppliers_table.find_one.return_value = {"supplier_id": "S01"}  # Supplier exists
    mock_supplier_products_table.find_one.return_value = {"products": ["Product A"]}
    result = Supplier.view_supplier_products("S01")

    assert result["success"] is True
    assert result["data"] == ["Product A"]


def test_view_supplier_products_no_products(mock_suppliers_table, mock_supplier_products_table):
    """Tests the case where a supplier exists but has no products listed."""
    mock_suppliers_table.find_one.return_value = {"supplier_id": "S01"}
    mock_supplier_products_table.find_one.return_value = {"products": []}
    result = Supplier.view_supplier_products("S01")

    assert result["success"] is False
    assert "No products found" in result["message"]


@pytest.mark.asyncio
async def test_upload_suppliers_csv_success():
    """Tests a successful CSV upload to an empty table."""
    # Use a local patch here to mock the Supplier class within the static method
    with patch("backend.supplier.Supplier") as MockSupplier:
        mock_instance = MockSupplier.return_value
        mock_instance.add_supplier.return_value = {"success": True}

        # Need to also patch the table check inside the function
        with patch("backend.supplier.suppliers_table") as mock_table:
            mock_table.count_documents.return_value = 0

            csv_content = "supplier_name,email,phone_number,address,sales_rep\nTest Supplier,test@test.com,123,123 St,Test Rep"
            fake_file = AsyncMock()
            fake_file.read.return_value = csv_content.encode('utf-8')

            result = await Supplier.upload_suppliers_csv(fake_file)

            assert result["success"] is True
            assert "All suppliers added" in result["message"]