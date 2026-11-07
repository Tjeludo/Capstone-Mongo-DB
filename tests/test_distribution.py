import unittest
from unittest.mock import patch, MagicMock

# Import all the necessary components from your module
from backend.distribution import (
    makeDistID,
    checkAvailability,
    get_product_name,
    DistributionItem,
    Distrubition
)

class TestDistribution(unittest.TestCase):

    # --- Tests for Standalone Functions ---

    @patch('backend.distribution.collectionDistribution')
    def test_makeDistID(self, mock_collection_dist):
        """Tests the ID generation logic."""
        # Arrange: Simulate that there are 9 documents already
        mock_collection_dist.count_documents.return_value = 9
        
        # Act
        new_id = makeDistID()
        
        # Assert
        self.assertEqual(new_id, "D0010")

    @patch('backend.distribution.collection')
    def test_checkAvailability(self, mock_collection_inv):
        """Tests the three possible outcomes of checking item availability."""
        # Case 1: Item is not found at all
        mock_collection_inv.find_one.return_value = None
        is_available, message = checkAvailability("9999999999", 5) # Non-existent code
        self.assertFalse(is_available)
        self.assertIn("not found", message)

        # Case 2: Item found, but insufficient quantity
        mock_collection_inv.find_one.return_value = {"quantity": 2}
        is_available, message = checkAvailability("0050010921", 5)
        self.assertFalse(is_available)
        self.assertIn("Insufficient inventory", message)

        # Case 3: Success, item is available
        mock_collection_inv.find_one.return_value = {"quantity": 10}
        is_available, message = checkAvailability("0050010921", 5)
        self.assertTrue(is_available)
        self.assertEqual(message, "Item is available.")

    # --- Tests for the DistributionItem Class ---
    
    def test_distribution_item_creation(self):
        """Tests the simple data storage of the DistributionItem class."""
        item = DistributionItem("0050010921", "B002", 50)
        expected_dict = {
            "product_code": "0050010921",
            "branch": "B002",
            "quantity": 50
        }
        self.assertEqual(item.to_dict(), expected_dict)

    # --- Tests for the Distrubition Class ---

    @patch('backend.distribution.makeDistID')
    def test_distrubition_init_new(self, mock_make_id):
        """Tests creating a new distribution instance."""
        # Arrange
        mock_make_id.return_value = "D777"
        
        # Act
        dist = Distrubition()
        
        # Assert
        self.assertEqual(dist.distributionID, "D777")
        self.assertIsNotNone(dist.date)
        self.assertEqual(dist.mainBranch, "B001")

    def test_distrubition_init_existing(self):
        """Tests loading an existing distribution."""
        dist = Distrubition(date="15-09-2025 10:00", distributionID="D123")
        self.assertEqual(dist.distributionID, "D123")
        self.assertEqual(dist.date, "15-09-2025 10:00")
    
    def test_addItem(self):
        """Tests adding a DistributionItem to a distribution."""
        dist = Distrubition(distributionID="D123")
        item = DistributionItem("0050010921", "B002", 50)
        dist.addItem(item)
        self.assertEqual(len(dist.distributions), 1)
        self.assertIs(dist.distributions[0], item)

    @patch('backend.distribution.collection')
    def test_distribute_method(self, mock_collection_inv):
        """Tests that the distribute method updates inventory correctly."""
        # Arrange
        dist = Distrubition(distributionID="D123")
        
        # Act
        dist.distribute("0050010921", "B002", 50)
        
        # Assert
        # Check that quantity was increased in the destination branch
        mock_collection_inv.update_one.assert_any_call(
            {"product_code": "0050010921", "branchID": "B002"},
            {"$inc": {"quantity": 50}}
        )
        # Check that quantity was decreased in the main branch
        mock_collection_inv.update_one.assert_any_call(
            {"product_code": "0050010921", "branchID": "B001"},
            {"$inc": {"quantity": -50}}
        )
        self.assertEqual(mock_collection_inv.update_one.call_count, 2)

    @patch('backend.distribution.collectionDistribution')
    @patch('backend.distribution.get_product_name')
    def test_recordDistributions(self, mock_get_name, mock_collection_dist):
        """Tests the logic for recording the final distribution document."""
        # Arrange
        mock_get_name.return_value = "Infant Formula"
        dist = Distrubition(distributionID="D123")
        dist.addItem(DistributionItem("0050010921", "B002", 50))
        dist.addItem(DistributionItem("0090010115", "B003", 25))
        
        # Act
        dist.recordDistributions()
        
        # Assert
        # Check that the database insertion method was called exactly once
        mock_collection_dist.insert_one.assert_called_once()
        
        # Check the structure and content of the document that was inserted
        inserted_doc = mock_collection_dist.insert_one.call_args[0][0]
        self.assertEqual(inserted_doc["distributionID"], "D123")
        self.assertEqual(len(inserted_doc["distributions"]), 2)
        self.assertEqual(inserted_doc["distributions"][0]["product_name"], "Infant Formula")
        self.assertEqual(inserted_doc["distributions"][1]["quantity"], 25)

if __name__ == '__main__':
    unittest.main()