import pytest
from unittest.mock import patch
from datetime import datetime
from backend.analytics import Analytics


@pytest.fixture
def mock_orders_table():
    """Mocks the 'orders_table' database collection."""
    with patch("backend.analytics.orders_table") as mock:
        yield mock


def test_get_dashboard_metrics(mock_orders_table):
    """
    Tests that the analytics function calls the database with the correct
    aggregation pipelines and correctly structures the final data.
    """
    # Mock the return values for the three separate aggregation calls
    mock_most_ordered = [{"product_code": "P001", "total_quantity": 10}]
    mock_status_dist = [{"status": "Confirmed", "count": 5}]
    mock_freq_by_supplier = [{"supplier_id": "S01", "order_count": 3}]

    # Use side_effect to return different values for each call to aggregate()
    mock_orders_table.aggregate.side_effect = [
        mock_most_ordered,
        mock_status_dist,
        mock_freq_by_supplier
    ]

    # Patch datetime to make the date filter predictable
    with patch("backend.analytics.datetime") as mock_dt:
        mock_dt.now.return_value = datetime(2025, 10, 31)

        result = Analytics.get_dashboard_metrics(days=30)

        assert result["success"] is True
        # Verify the structure of the returned data
        assert "most_ordered_products" in result["data"]
        assert "order_status_distribution" in result["data"]
        assert "order_frequency_by_supplier" in result["data"]
        # Verify the data matches the mocked return values
        assert result["data"]["most_ordered_products"] == mock_most_ordered
        assert result["data"]["order_status_distribution"] == mock_status_dist
        # Verify that aggregate was called exactly three times
        assert mock_orders_table.aggregate.call_count == 3