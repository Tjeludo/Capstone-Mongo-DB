import pytest
from fastapi.testclient import TestClient
from unittest.mock import patch
from fastapi import status

# Adjust the import to get the 'app' object directly from your API file
from backend.InventoryAPI import app

# The TestClient allows us to make requests to the FastAPI app without a live server.
client = TestClient(app)

# --- Test for GET /transfer endpoint ---

@patch('backend.Transfers.Transfer.getTransfers')
def test_get_all_transfers_success(mock_get_transfers):
    """
    Tests successfully retrieving all transfers.
    """
    # Arrange: Mock the return value of the function that hits the database.
    mock_db_data = [
        {"transferID": "T001", "product_code": "0040010017", "from_branch": "B001", "to_branch": "B002", "quantity": 10},
        {"transferID": "T002", "product_code": "0090010115", "from_branch": "B001", "to_branch": "B003", "quantity": 5},
    ]
    mock_get_transfers.return_value = {"success": True, "data": mock_db_data}

    # Act: Make a GET request to the endpoint.
    response = client.get("/transfer")

    # Assert: Check the HTTP response.
    assert response.status_code == status.HTTP_200_OK
    response_data = response.json()
    assert response_data["success"] is True
    assert len(response_data["data"]) == 2
    assert response_data["data"][0]["transferID"] == "T001"

# --- Tests for POST /addTransfer endpoint ---

@patch('backend.InventoryAPI.transfer')
def test_create_transfer_success(mock_transfer_logic):
    """
    Tests successfully creating a new transfer.
    """
    # Arrange: Mock the logic function to return a success message.
    mock_transfer_logic.return_value = {"success": True, "message": "Transfer T003 recorded successfully."}
    transfer_payload = {
        "itemCode": "0040010017",
        "fromBranch": "B001",
        "toBranch": "B002",
        "quantity": 10
    }

    # Act: Make a POST request to the endpoint with some data.
    response = client.post("/addTransfer", json=transfer_payload)

    # Assert
    assert response.status_code == status.HTTP_200_OK
    assert response.json() == {"success": True, "message": "Transfer T003 recorded successfully."}
    # Verify the underlying logic function was called with the correct data
    mock_transfer_logic.assert_called_once_with(
        "0040010017",
        "B001",
        "B002",
        10
    )

@patch('backend.InventoryAPI.transfer')
def test_create_transfer_insufficient_stock(mock_transfer_logic):
    """
    Tests the API's response when the business logic reports insufficient stock.
    """
    # Arrange: Mock the logic function to return a failure message.
    failure_message = "Insufficient stock in B001. Available: 5, Requested: 10."
    mock_transfer_logic.return_value = {"success": False, "message": failure_message}
    transfer_payload = {
        "itemCode": "0040010017",
        "fromBranch": "B001",
        "toBranch": "B002",
        "quantity": 10
    }

    # Act
    response = client.post("/addTransfer", json=transfer_payload)

    # Assert: Check the business logic's direct response
    assert response.status_code == status.HTTP_200_OK
    assert response.json() == {"success": False, "message": failure_message}