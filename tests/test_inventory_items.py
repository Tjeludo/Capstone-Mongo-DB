import unittest
from unittest.mock import patch

# Import the functions and class from your module
from backend.inventory_items import (
    checkItem,
    checkBranch,
    notAdded,
    allExist,
    Inventory
)

class TestInventoryItems(unittest.TestCase):

    # --- Tests for Standalone Functions ---

    @patch('backend.inventory_items.collectionItem')
    def test_checkItem_when_item_exists(self, mock_collection_item):
        """Tests that checkItem returns True when the item is found."""
        # Arrange: Simulate that find_one finds a document
        mock_collection_item.find_one.return_value = {"product_code": "0050010921"}
        
        # Act
        result = checkItem("0050010921")
        
        # Assert
        self.assertTrue(result)
        mock_collection_item.find_one.assert_called_once_with({"product_code": "0050010921"})

    @patch('backend.inventory_items.collectionItem')
    def test_checkItem_when_item_does_not_exist(self, mock_collection_item):
        """Tests that checkItem returns False when the item is not found."""
        # Arrange: Simulate that find_one returns None
        mock_collection_item.find_one.return_value = None
        
        # Act
        result = checkItem("9999999999")
        
        # Assert
        self.assertFalse(result)

    @patch('backend.inventory_items.collectionBranch')
    def test_checkBranch_when_branch_exists(self, mock_collection_branch):
        """Tests that checkBranch returns True when the branch is found."""
        # Arrange
        mock_collection_branch.find_one.return_value = {"branchID": "B001"}
        
        # Act
        result = checkBranch("B001")
        
        # Assert
        self.assertTrue(result)
        mock_collection_branch.find_one.assert_called_once_with({"branchID": "B001"})

    @patch('backend.inventory_items.collection')
    @patch('backend.inventory_items.collectionBranch')
    def test_allExist_when_all_are_found(self, mock_collection_branch, mock_collection):
        """Tests that allExist returns True when all items are found."""
        # Arrange
        mock_collection.find_one.return_value = {"product_code": "0050010921"}
        mock_collection_branch.find_one.return_value = {"branchID": "B001"}
        
        # Act
        result = allExist("0050010921", "B001", "B002")
        
        # Assert
        self.assertTrue(result)

    @patch('backend.inventory_items.collection')
    @patch('backend.inventory_items.collectionBranch')
    def test_allExist_when_item_is_missing(self, mock_collection_branch, mock_collection):
        """Tests that allExist returns False when one item is not found."""
        # Arrange
        mock_collection.find_one.return_value = None # Simulate item not found
        mock_collection_branch.find_one.return_value = {"branchID": "B001"}
        
        # Act
        result = allExist("9999999999", "B001", "B002")
        
        # Assert
        self.assertFalse(result)


    # --- Tests for the Inventory Class ---

    def test_inventory_to_dict(self):
        """Tests the constructor and to_dict method without any database interaction."""
        # Arrange
        inventory_item = Inventory("0050010921", "Infant Formula", "B001", 50)
        expected_dict = {
            "product_code": "0050010921",
            "product_name": "Infant Formula",
            "branchID": "B001",
            "quantity": 50
        }
        
        # Act
        result = inventory_item.to_dict()
        
        # Assert
        self.assertEqual(result, expected_dict)
        
    @patch('backend.inventory_items.collection')
    def test_getInventory(self, mock_collection):
        """Tests retrieving a list of all inventory items."""
        # Arrange: Create fake data that the mock database will return
        mock_db_data = [
            {"product_code": "0090010115", "product_name": "Stroller", "branchID": "B001", "quantity": 10},
            {"product_code": "0050010921", "product_name": "Infant Formula", "branchID": "B002", "quantity": 100},
        ]
        mock_collection.find.return_value = mock_db_data
        
        # Act
        result = Inventory.getInventory()
        
        # Assert
        self.assertTrue(result["success"])
        self.assertEqual(len(result["data"]), 2)
        self.assertEqual(result["data"][0]["product_name"], "Stroller")

    @patch('backend.inventory_items.collectionItem')
    def test_getInventoryItems(self, mock_collection_item):
        """Tests retrieving a list of all product codes."""
        # Arrange
        mock_db_data = [
            {"product_code": "0090010115", "product_name": "Stroller"},
            {"product_code": "0050010921", "product_name": "Infant Formula"},
        ]
        mock_collection_item.find.return_value = mock_db_data
        
        # Act
        result = Inventory.getInventoryItems()
        
        # Assert
        self.assertTrue(result["success"])
        self.assertEqual(len(result["data"]), 2)
        self.assertIn("0090010115", result["data"])
        self.assertIn("0050010921", result["data"])

if __name__ == '__main__':
    unittest.main()