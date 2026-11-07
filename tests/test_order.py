import pytest
from unittest.mock import patch, MagicMock
from datetime import date, timedelta
from backend.order import Order


# --- Fixtures ---

@pytest.fixture
def mock_orders_table():
    """Mocks the 'orders_table' database collection."""
    with patch("backend.order.orders_table") as mock:
        yield mock


# This fixture will be used by all tests in this file automatically.
@pytest.fixture(autouse=True)
def auto_mock_orders_table(mock_orders_table):
    """Automatically applies the orders_table mock to every test."""
    pass


# --- Test Cases ---

def test_order_initialization():
    """Tests that an Order instance is initialized with correct default values."""
    # We patch the methods called by __init__ to isolate the test.
    with patch.object(Order, 'generate_order_id', return_value="ODR123") as mock_gen_id, \
            patch.object(Order, 'generate_delivery_date', return_value=date(2025, 1, 10)) as mock_gen_date:
        order = Order(supplier_id="S01", staff_id="EMP01")

        assert order.supplier_id == "S01"
        assert order.ordered_by == "EMP01"
        assert order.order_id == "ODR123"
        assert order.status == "Pending"
        assert order.products == []
        assert order.total_cost == 0
        mock_gen_id.assert_called_once()
        mock_gen_date.assert_called_once()


def test_generate_order_id_first_order(mock_orders_table):
    """Tests generating the first order ID when the database is empty."""
    mock_orders_table.find_one.return_value = None
    order = Order("S01", "EMP01")
    assert order.order_id == "ODR001"


def test_generate_order_id_next_order(mock_orders_table):
    """Tests generating a sequential order ID."""
    mock_orders_table.find_one.return_value = {"order_id": "ODR009"}
    order = Order("S01", "EMP01")
    assert order.order_id == "ODR010"


def test_generate_delivery_date_not_sunday():
    """Tests delivery date calculation when the estimated day is not a Sunday."""
    with patch('backend.order.date') as mock_date, \
            patch('backend.order.random.randint', return_value=3) as mock_randint:
        mock_date.today.return_value = date(2025, 9, 26)  # A Friday
        # Friday + 3 days = Monday. Not a Sunday, so date should be returned as is.
        expected_date = date(2025, 9, 29)

        order = Order("S01", "EMP01")
        assert order.generate_delivery_date() == expected_date


def test_generate_delivery_date_is_sunday():
    """Tests delivery date calculation, ensuring it skips Sundays."""
    with patch('backend.order.date') as mock_date, \
            patch('backend.order.random.randint', return_value=4) as mock_randint:
        mock_date.today.return_value = date(2025, 9, 26)  # A Friday
        # Friday + 4 days = Tuesday. Wait, let's fix the date to make it land on Sunday.
        mock_date.today.return_value = date(2025, 9, 25)  # A Thursday
        # Thursday + 3 days = Sunday. Should be pushed to Monday.
        mock_randint.return_value = 3
        expected_date = date(2025, 9, 29)  # Monday

        order = Order("S01", "EMP01")
        assert order.generate_delivery_date() == expected_date


def test_confirm_order_success(mock_orders_table):
    """Tests successfully confirming an order with products."""
    mock_orders_table.find_one.return_value = None  # Order does not exist yet
    order = Order("S01", "EMP01")
    order.products = [{"name": "Test Product"}]
    order.total_cost = 100.0
    result = order.confirm_order()

    assert result["success"] is True
    mock_orders_table.insert_one.assert_called_once()


def test_confirm_order_empty_cart(mock_orders_table):
    """Tests that an order with no products cannot be confirmed."""
    order = Order("S01", "EMP01")
    result = order.confirm_order()
    assert result["success"] is False
    assert "Cart is empty" in result["message"]


def test_cancel_order_success(mock_orders_table):
    """Tests successfully cancelling a 'Pending' order."""
    mock_orders_table.find_one.return_value = {"order_id": "ODR001", "status": "Pending"}
    result = Order.cancel_order("ODR001")
    assert result["success"] is True
    mock_orders_table.update_one.assert_called_once()


def test_cancel_order_too_late(mock_orders_table):
    """Tests that an order with status 'In-Transit' cannot be cancelled."""
    mock_orders_table.find_one.return_value = {"order_id": "ODR001", "status": "In-Transit"}
    result = Order.cancel_order("ODR001")
    assert result["success"] is False
    assert "cannot be cancelled" in result["message"]


def test_update_order_status_success(mock_orders_table):
    """Tests a valid, forward status update."""
    mock_orders_table.find_one.return_value = {"order_id": "ODR001", "status": "Confirmed"}
    result = Order.update_order_status("ODR001", "In-Progress")
    assert result["success"] is True
    mock_orders_table.update_one.assert_called_once()


def test_update_order_status_backwards(mock_orders_table):
    """Tests that moving an order's status backward is not allowed."""
    mock_orders_table.find_one.return_value = {"order_id": "ODR001", "status": "In-Transit"}
    result = Order.update_order_status("ODR001", "Confirmed")
    assert result["success"] is False
    assert "back to 'Confirmed'" in result["message"]


def test_get_upcoming_deliveries(mock_orders_table):
    """Tests retrieving orders with a future delivery date."""
    today_plus_5 = (date.today() + timedelta(days=5)).strftime("%d-%m-%Y")

    # This is the data we expect the database to return *after* filtering.
    filtered_mock_return = [
        {"status": "Confirmed", "expect_delivery": today_plus_5},
    ]
    mock_orders_table.find.return_value = filtered_mock_return

    # This is the filter we expect the code to use in its query.
    expected_filter = {
        "status": {"$nin": ["Fully Fulfilled", "Cancelled"]}
    }

    result = Order.get_upcoming_deliveries()

    # Assert that the function called the database with the correct filter.
    mock_orders_table.find.assert_called_with(expected_filter, {"_id": 0})

    # Assert that the function correctly processed the filtered data.
    assert result["success"] is True
    assert len(result["data"]) == 1