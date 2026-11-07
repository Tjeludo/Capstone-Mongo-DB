import unittest
from unittest.mock import patch, MagicMock

# By having the __init__.py files, you can now import from the backend folder
from backend.branch import Branch

class TestBranch(unittest.TestCase):

    def setUp(self):
        """This method runs before each test."""
        self.branch_data = {
            "location": "Cape Town",
            "contactDetails": "021-123-4567",
            "manager": "Jane Doe"
        }
        self.branch_instance = Branch(**self.branch_data)

    # The @patch decorator intercepts the call to the database object.
    # It replaces 'backend.branch.collection' with a mock object for the duration of this test.
    @patch('backend.branch.collection')
    def test_addBranch_success(self, mock_collection):
        """Tests the successful creation of a new branch."""
        # --- Arrange ---
        # We don't need to configure the mock much, just check that insert_one is called.
        
        # --- Act ---
        result = self.branch_instance.addBranch()
        
        # --- Assert ---
        self.assertTrue(result["success"])
        self.assertEqual(result["message"], "Branch created successfully.")
        
        # Verify that the database insert method was called exactly once.
        mock_collection.insert_one.assert_called_once()
        # You could also check WHAT it was called with for a more robust test.
        # mock_collection.insert_one.assert_called_with(...)

    @patch('backend.branch.collection')
    def test_getBranches(self, mock_collection):
        """Tests retrieving a list of all branches."""
        # --- Arrange ---
        # Create fake data that our mock database will return.
        mock_db_return_value = [
            {
                "branchID": "B001",
                "location": "Johannesburg",
                "contactDetails": "011-987-6543",
                "manager": "John Smith"
            },
            {
                "branchID": "B002",
                "location": "Durban",
                "contactDetails": "031-456-7890",
                "manager": "Mary Jane"
            }
        ]
        # Configure the mock's find() method to return our fake data.
        mock_collection.find.return_value = mock_db_return_value
        
        # --- Act ---
        result = Branch.getBranches()
        
        # --- Assert ---
        self.assertTrue(result["success"])
        self.assertEqual(len(result["data"]), 2) # Check if we got two branches back
        self.assertEqual(result["data"][0]["location"], "Johannesburg") # Check data integrity
        mock_collection.find.assert_called_once() # Verify the find() method was called.

    @patch('backend.branch.collection')
    def test_update_branch_success(self, mock_collection):
        """Tests a successful update of a branch."""
        # --- Arrange ---
        # The code checks `result.modified_count`. We must simulate this.
        # We create a mock object for the return value of update_one.
        mock_update_result = MagicMock()
        mock_update_result.modified_count = 1
        mock_collection.update_one.return_value = mock_update_result
        
        branch_id_to_update = "B001"
        update_payload = {"manager": "Peter Parker"}

        # --- Act ---
        result = Branch.update_branch(branch_id_to_update, update_payload)
        
        # --- Assert ---
        self.assertTrue(result["success"])
        self.assertEqual(result["message"], "Branch updated successfully.")
        
        # Verify update_one was called correctly
        mock_collection.update_one.assert_called_once_with(
            {"branchID": branch_id_to_update},
            {"$set": update_payload}
        )

    @patch('backend.branch.collection')
    def test_update_branch_not_found(self, mock_collection):
        """Tests updating a branch that doesn't exist."""
        # --- Arrange ---
        # Simulate that no documents were modified.
        mock_update_result = MagicMock()
        mock_update_result.modified_count = 0
        mock_collection.update_one.return_value = mock_update_result
        
        # --- Act ---
        result = Branch.update_branch("B999", {"manager": "Tony Stark"})
        
        # --- Assert ---
        self.assertFalse(result["success"])
        self.assertEqual(result["message"], "Branch not found or data is the same.")
        mock_collection.update_one.assert_called_once()

# This allows the script to be run directly
if __name__ == '__main__':
    unittest.main()