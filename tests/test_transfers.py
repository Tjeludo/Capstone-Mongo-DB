import unittest
from unittest.mock import patch, MagicMock

# Import the functions and class from your module
from backend.Transfers import makeTransID, Transfer, transfer

class TestTransfers(unittest.TestCase):

    # --- Tests for Standalone makeTransID() function ---

    @patch('backend.Transfers.collection')
    def test_makeTransID(self, mock_collection_transfers):
        """Tests that the transfer ID is generated correctly."""
        # Arrange: Simulate that there are 7 documents in the collection
        mock_collection_transfers.count_documents.return_value = 7
        
        # Act
        new_id = makeTransID()
        
        # Assert
        self.assertEqual(new_id, "T008")

    # --- Tests for the Transfer Class ---

    @patch('backend.Transfers.get_product_name')
    @patch('backend.Transfers.makeTransID')
    def test_transfer_class_init(self, mock_make_id, mock_get_name):
        """Tests the constructor of the Transfer class."""
        # Arrange
        mock_make_id.return_value = "T123"
        mock_get_name.return_value = "Feeding Bottle"
        
        # Act
        t = Transfer("0040010017", "B001", "B002", 10)
        
        # Assert
        self.assertEqual(t.transferID, "T123")
        self.assertEqual(t.product_name, "Feeding Bottle")
        self.assertIsNotNone(t.date)
        mock_get_name.assert_called_once_with("0040010017")

    @patch('backend.Transfers.collectionInventory')
    def test_execute_transfer(self, mock_collection_inv):
        """Tests that execute_transfer updates inventory correctly."""
        # Arrange
        t = Transfer("0040010017", "B001", "B002", 10, transferID="T123")
        
        # Act
        t.execute_transfer()
        
        # Assert
        # Check that quantity was decreased from the source branch
        mock_collection_inv.update_one.assert_any_call(
            {"product_code": "0040010017", "branchID": "B001"},
            {"$inc": {"quantity": -10}}
        )
        # Check that quantity was increased in the destination branch
        mock_collection_inv.update_one.assert_any_call(
            {"product_code": "0040010017", "branchID": "B002"},
            {"$inc": {"quantity": 10}},
            upsert=True
        )
        self.assertEqual(mock_collection_inv.update_one.call_count, 2)

    @patch('backend.Transfers.collection')
    def test_record_transfer(self, mock_collection_transfers):
        """Tests that record_transfer inserts the correct document."""
        # Arrange
        t = Transfer("0040010017", "B001", "B002", 10, transferID="T123", product_name="Feeding Bottle")

        # Act
        result = t.record_transfer()

        # Assert
        self.assertTrue(result['success'])
        mock_collection_transfers.insert_one.assert_called_once()
        inserted_doc = mock_collection_transfers.insert_one.call_args[0][0]
        self.assertEqual(inserted_doc['transferID'], 'T123')


    # --- Tests for the main transfer() function (Business Logic) ---

    def test_transfer_fails_if_branches_are_same(self):
        """Tests validation: fromBranch cannot equal toBranch."""
        result = transfer("0040010017", "B001", "B001", 10)
        self.assertFalse(result['success'])
        self.assertEqual(result['message'], "Source and destination branches cannot be the same.")

    def test_transfer_fails_if_quantity_is_zero_or_less(self):
        """Tests validation: quantity must be positive."""
        result = transfer("0040010017", "B001", "B002", 0)
        self.assertFalse(result['success'])
        self.assertEqual(result['message'], "Transfer quantity must be greater than zero.")

    @patch('backend.Transfers.collectionBranch')
    def test_transfer_fails_if_to_branch_does_not_exist(self, mock_collection_branch):
        """Tests validation: destination branch must exist."""
        # Arrange: fromBranch exists, toBranch does not.
        mock_collection_branch.find_one.side_effect = [None, {"branchID": "B001"}]
        
        # Act
        result = transfer("0040010017", "B001", "B999", 10)
        
        # Assert
        self.assertFalse(result['success'])
        self.assertIn("Destination branch 'B999' is not a valid branch.", result['message'])

    @patch('backend.Transfers.collectionBranch')
    @patch('backend.Transfers.collectionInventory')
    def test_transfer_fails_if_stock_is_insufficient(self, mock_collection_inv, mock_collection_branch):
        """Tests validation: source branch must have enough stock."""
        # Arrange
        mock_collection_branch.find_one.return_value = {"branchID": "any_branch"}
        mock_collection_inv.find_one.return_value = {"quantity": 5} # Not enough stock
        
        # Act
        result = transfer("0040010017", "B001", "B002", 10)
        
        # Assert
        self.assertFalse(result['success'])
        self.assertIn("Insufficient stock in B001. Available: 5, Requested: 10.", result['message'])

    @patch('backend.Transfers.collection')
    @patch('backend.Transfers.collectionInventory')
    @patch('backend.Transfers.collectionBranch')
    @patch('backend.Transfers.get_product_name')
    def test_transfer_success_path(self, mock_get_name, mock_branch, mock_inventory, mock_transfers):
        """Tests a complete, successful transfer from start to finish."""
        # Arrange: Set up mocks for a successful run
        mock_get_name.return_value = "Feeding Bottle"
        mock_branch.find_one.return_value = {"branchID": "any_branch"}
        mock_inventory.find_one.return_value = {"quantity": 100}

        # Act
        result = transfer("0040010017", "B001", "B002", 10)
        
        # Assert
        self.assertTrue(result['success'])
        self.assertEqual(mock_inventory.update_one.call_count, 2)
        mock_transfers.insert_one.assert_called_once()
        
        inserted_doc = mock_transfers.insert_one.call_args[0][0]
        self.assertEqual(inserted_doc['product_name'], 'Feeding Bottle')

if __name__ == '__main__':
    unittest.main()