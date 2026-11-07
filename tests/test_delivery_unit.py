import pytest
from unittest.mock import MagicMock, patch
import datetime
import random


from backend.delivery import deliveryItem, Delivery


@pytest.fixture
def sample_delivery_item():
    """Provides a sample deliveryItem object."""
    return deliveryItem(item_id="0090010115", itemName="Stroller", quantity=10, comments="Fragile")

@pytest.fixture
def sample_delivery(sample_delivery_item):
    """Provides a sample Delivery object instance for testing."""
    items = [sample_delivery_item]
    return Delivery(
        deliveryNum="DEL001",
        supplier_id="S01",
        supplierName="Little Baby",
        order_id="ODR001",
        date=datetime.date(2025, 10, 27).strftime("%Y-%m-%d"),
        checkedByWho="Alice",
        deliveryItems=items,
        comments="Urgent delivery"
    )

# --- Class Testing: delivery.py ---

class TestDeliveryItem:
    """Tests for the simple data class deliveryItem."""
    def test_init(self, sample_delivery_item):
        assert sample_delivery_item.item_id == "0090010115"
        assert sample_delivery_item.quantity == 10

    def test_to_dict(self, sample_delivery_item):
        expected_dict = {"item_id": "0090010115", "itemName": "Stroller", "quantity": 10, "comments": "Fragile"}
        assert sample_delivery_item.to_dict() == expected_dict

class TestDelivery:
    """Tests for the Delivery class methods."""

    @patch('backend.delivery.delivery_table')
    def test_add_delivery_item_success(self, mock_delivery_table, sample_delivery):
        result = sample_delivery.add_delivery_item()
        mock_delivery_table.insert_one.assert_called_once()
        assert result is True

    @patch('backend.delivery.delivery_table')
    def test_add_delivery_item_failure(self, mock_delivery_table, sample_delivery):
        mock_delivery_table.insert_one.side_effect = Exception("DB Error")
        result = sample_delivery.add_delivery_item()
        mock_delivery_table.insert_one.assert_called_once()
        assert result is False

    @patch('backend.delivery.delivery_table')
    def test_get_del_number_first_delivery(self, mock_delivery_table):
        mock_delivery_table.find_one.return_value = None
        assert Delivery.get_del_number() == "DEL001"

    @patch('backend.delivery.delivery_table')
    def test_random_delivery_number_generation(self, mock_delivery_table):
        """Random test: Checks number generation with random previous numbers."""
        for _ in range(5): 
            random_num = random.randint(1, 998)
            mock_delivery_table.find_one.return_value = {"delivery_num": f"DEL{random_num:03d}"}
            expected_next = f"DEL{random_num + 1:03d}"
            assert Delivery.get_del_number() == expected_next

    @patch('backend.delivery.delivery_table')
    def test_get_del_number_subsequent_delivery(self, mock_delivery_table):
        mock_delivery_table.find_one.return_value = {"delivery_num": "DEL005"}
        assert Delivery.get_del_number() == "DEL006"

    @patch('backend.delivery.inventory_table')
    def test_update_main_branch_inventory(self, mock_inventory_table, sample_delivery):
        sample_delivery.update_main_branch_inventory()
        mock_inventory_table.update_one.assert_called_once_with(
            {"product_code": "0090010115", "branchID": "B001"},
            {"$inc": {"quantity": 10}},
            upsert=True
        )

    @patch('backend.delivery.delivery_table')
    @patch('backend.delivery.orders_table')
    def test_update_order_status_to_partially_fulfilled(self, mock_orders_table, mock_delivery_table, sample_delivery):
        """Partition Test: A delivery partially fulfills an order."""
        mock_orders_table.find_one.return_value = {
            "order_id": "ODR001", "status": "Dispatched",
            "products": [{"product_code": "0090010115", "description": "Stroller", "quantity": 20}]
        }
        # Simulate the dictionary structure that would be found in the database
        delivery_doc_in_db = {
            "deliveryItems": [item.to_dict() for item in sample_delivery.deliveryItems]
        }
        mock_delivery_table.find.return_value = [delivery_doc_in_db]

        sample_delivery.update_order_status()

        mock_orders_table.update_one.assert_called_once_with(
            {"order_id": "ODR001"}, {"$set": {"status": "Partially Fulfilled"}}
        )
        mock_delivery_table.update_one.assert_called_once_with(
            {"delivery_num": "DEL001"},
            {"$set": {"missing items": [{"itemcode": "0090010115", "itemName": "Stroller", "missing_quantity": 10}]}}
        )

    @patch('backend.delivery.delivery_table')
    @patch('backend.delivery.orders_table')
    def test_update_order_status_to_fully_fulfilled(self, mock_orders_table, mock_delivery_table, sample_delivery):
        """Partition Test: A delivery completely fulfills an order."""
        mock_orders_table.find_one.return_value = {
            "order_id": "ODR001", "status": "Partially Fulfilled",
            "products": [{"product_code": "0090010115", "description": "Stroller", "quantity": 10}]
        }
        delivery_doc_in_db = {
            "deliveryItems": [item.to_dict() for item in sample_delivery.deliveryItems]
        }
        mock_delivery_table.find.return_value = [delivery_doc_in_db]

        sample_delivery.update_order_status()

        mock_orders_table.update_one.assert_called_once_with(
            {"order_id": "ODR001"}, {"$set": {"status": "Fully Fulfilled"}}
        )
        mock_delivery_table.update_one.assert_called_once_with(
            {"delivery_num": "DEL001"}, {"$unset": {"missing items": ""}}
        )

    @patch('backend.delivery.delivery_table')
    @patch('backend.delivery.orders_table')
    def test_update_order_status_for_already_fulfilled_order(self, mock_orders_table, mock_delivery_table, sample_delivery):
        """Partition Test: A delivery is for an already fulfilled order."""
        mock_orders_table.find_one.return_value = {"order_id": "ODR001", "status": "Fully Fulfilled"}

        sample_delivery.update_order_status()

        mock_orders_table.update_one.assert_not_called()
        mock_delivery_table.delete_one.assert_called_once_with({"delivery_num": sample_delivery.deliveryNum})
