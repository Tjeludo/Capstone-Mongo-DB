import pytest
from unittest.mock import patch, call
import datetime
import random


from backend.StockTake import StockTakeItem, StockTake

# --- Test Data Fixtures for StockTake ---

@pytest.fixture
def sample_stock_take_item():
    """Provides a sample StockTakeItem object."""
    return StockTakeItem(itemID="0090010115", itemName="Stroller", previousQuantity=50, quantityOnHand=48)

@pytest.fixture
def sample_stock_take(sample_stock_take_item):
    """Provides a sample StockTake object."""
    items = [sample_stock_take_item]
    return StockTake(
        stockTakeNum="ST001",
        branchID="B001",
        date=datetime.date(2025, 10, 27).strftime("%Y-%m-%d"),
        items=items
    )

# --- Class Testing: StockTake.py ---

class TestStockTakeItem:
    """Tests for the simple data class StockTakeItem."""
    def test_init(self, sample_stock_take_item):
        assert sample_stock_take_item.itemID == "0090010115"
        assert sample_stock_take_item.quantityOnHand == 48

    def test_to_dict(self, sample_stock_take_item):
        expected = {"itemID": "0090010115", "itemName": "Stroller", "previousQuantity": 50, "quantityOnHand": 48}
        assert sample_stock_take_item.to_dict() == expected

class TestStockTake:
    """Tests for the StockTake class methods."""

    @patch('backend.StockTake.stock_take_table')
    def test_add_stock_take_success(self, mock_stock_take_table, sample_stock_take):
        sample_stock_take.add_stock_take()
        mock_stock_take_table.insert_one.assert_called_once()

    @patch('backend.StockTake.stock_take_table')
    def test_get_next_stock_take_number_first(self, mock_stock_take_table):
        mock_stock_take_table.find_one.return_value = None
        assert StockTake.get_next_stock_take_number() == "ST001"

    
    @patch('backend.StockTake.stock_take_table')
    def test_random_stock_take_number_generation(self, mock_stock_take_table):
        """Random test: Checks number generation with random previous numbers."""
        for _ in range(5): 
            random_num = random.randint(1, 998)
            mock_stock_take_table.find_one.return_value = {"stockTakeNum": f"ST{random_num:03d}"}
            expected_next = f"ST{random_num + 1:03d}"
            assert StockTake.get_next_stock_take_number() == expected_next

    @patch('backend.StockTake.stock_take_table')
    def test_get_next_stock_take_number_subsequent(self, mock_stock_take_table):
        mock_stock_take_table.find_one.return_value = {"stockTakeNum": "ST009"}
        assert StockTake.get_next_stock_take_number() == "ST010"

    @patch('backend.StockTake.inventory_table')
    def test_update_stock_levels(self, mock_inventory_table, sample_stock_take):
        sample_stock_take.update_stock_levels()
        mock_inventory_table.update_one.assert_called_once_with(
            {"product_code": "0090010115", "branchID": "B001"},
            {"$set": {"quantity": 48}},
            upsert=True
        )

    
    
    @patch('builtins.print')
    @patch('backend.StockTake.stock_take_table')
    def test_view_stock_takes(self, mock_stock_take_table, mock_print, sample_stock_take):
        """White-box test: Verifies view_stock_takes queries and prints correctly."""
        # Setup: Mock the database find to return a list of stock take documents.
        mock_stock_takes = [
            {
                "stockTakeNum": "ST001", "date": "2023-10-27",
                "items": [{"itemID": "0090010115", "quantityOnHand": 48}]
            },
            {
                "stockTakeNum": "ST002", "date": "2023-11-15",
                "items": [{"itemID": "0040010017", "quantityOnHand": 100}]
            }
        ]
        mock_stock_take_table.find.return_value = mock_stock_takes

        # Execute the method
        sample_stock_take.view_stock_takes("B001")

        # Assert: Check that the database was queried with the correct branch ID.
        mock_stock_take_table.find.assert_called_once_with({"branchID": "B001"})

        # Assert: Check that print was called with the correctly formatted strings.
        expected_calls = [
            call("Stock Take Number: ST001, Date: 2023-10-27"),
            call("Item ID: 0090010115, Quantity On Hand: 48"),
            call("\n"),
            call("Stock Take Number: ST002, Date: 2023-11-15"),
            call("Item ID: 0040010017, Quantity On Hand: 100"),
            call("\n")
        ]
        mock_print.assert_has_calls(expected_calls)


