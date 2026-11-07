import pytest
from fastapi.testclient import TestClient
from fastapi import FastAPI
from pymongo import MongoClient
import os
import datetime

# Import the router and the modules that connect to the database
from backend.api import router
import backend.api as api_module
import backend.StockTake as stocktake_module

# --- Test Application Setup ---
app = FastAPI()
app.include_router(router)

# --- Database Fixture ---
@pytest.fixture(scope="module")
def db_connection():
    """
    Creates a connection to a test MongoDB database and handles cleanup.
    """
    mongo_uri = os.getenv("MONGO_TEST_URI", "mongodb://localhost:27017/")
    client = MongoClient(mongo_uri)
    db = client["test_inventory_db_stocktake"] # Use a dedicated test database

    yield db

    print("\nDropping stocktake test database...")
    client.drop_database("test_inventory_db_stocktake")
    client.close()


@pytest.fixture(scope="function")
def test_client(db_connection, monkeypatch):
    """
    Provides a TestClient instance, ensures the database is clean, and patches
    the modules to use the test database connection.
    """
    # Use monkeypatch to replace the real DB connections with the test DB
    monkeypatch.setattr(api_module, 'stock_take_table', db_connection.stock_take)
    monkeypatch.setattr(api_module, 'inventory_table', db_connection.Inventory)
    monkeypatch.setattr(api_module, 'branches_table', db_connection.Branch)
    monkeypatch.setattr(stocktake_module, 'stock_take_table', db_connection.stock_take)
    monkeypatch.setattr(stocktake_module, 'inventory_table', db_connection.Inventory)

    # Cleanup before each test to ensure isolation
    for collection_name in db_connection.list_collection_names():
        db_connection[collection_name].delete_many({})
    
    with TestClient(app) as client:
        yield client


# --- Stocktake and Inventory Integration Tests ---

def test_full_stocktake_workflow(test_client, db_connection):
    """
    Tests the entire stocktake process: saving a stocktake and verifying
    that the inventory quantity was updated.
    """
    # 1. Arrange
    db_connection.Branch.insert_one({"branchID": "B002", "location": "Test Branch"})
    # Using the product code from your test run
    product_id = "0040010017"
    db_connection.Inventory.insert_one({"product_code": product_id, "branchID": "B002", "quantity": 100})
    
    st_num_response = test_client.get("/generate_stocktake_number")
    stocktake_num = st_num_response.json()["stockTakeNum"]

    # 2. Act
    stocktake_payload = {
        "stockTakeNum": stocktake_num, "branchId": "B002",
        "date": datetime.date.today().strftime("%Y-%m-%d"),
        "items": [{"itemID": product_id, "itemName": "Feeding bottle", "previousQuantity": 100, "quantityOnHand": 95}]
    }
    response = test_client.post("/save_stocktake", json=stocktake_payload)

    # 3. Assert
    assert response.status_code == 200
    assert response.json()["message"] == "Stock take saved successfully"

    stocktake_in_db = db_connection.stock_take.find_one({"stockTakeNum": stocktake_num})
    assert stocktake_in_db is not None
    assert stocktake_in_db["items"][0]["quantityOnHand"] == 95

    # CORRECTED: Ensure we query for the same product_id we inserted.
    inventory_item = db_connection.Inventory.find_one({"product_code": product_id})
    assert inventory_item["quantity"] == 95


def test_generate_stocktake_number(test_client, db_connection):
    """Tests the stocktake number generation endpoint."""
    # Scenario 1: No stock takes in the database
    response = test_client.get("/generate_stocktake_number")
    assert response.status_code == 200
    assert response.json() == {"stockTakeNum": "ST001"}

    # Scenario 2: A stock take already exists
    db_connection.stock_take.insert_one({"stockTakeNum": "ST008"})
    response = test_client.get("/generate_stocktake_number")
    assert response.status_code == 200
    assert response.json() == {"stockTakeNum": "ST009"}


def test_get_stocktakes(test_client, db_connection):
    """Tests fetching all stocktake documents."""
    # Arrange
    db_connection.stock_take.insert_many([
        {"stockTakeNum": "ST001", "branchID": "B001", "date": "2023-10-26"},
        {"stockTakeNum": "ST002", "branchID": "B002", "date": "2023-10-27"}
    ])

    # Act
    response = test_client.get("/get_stocktakes")

    # Assert
    assert response.status_code == 200
    data = response.json()
    assert len(data) == 2
    assert data[0]["stockTakeNum"] == "ST001"
    assert data[1]["stockTakeNum"] == "ST002"


def test_get_inventory_by_branch(test_client, db_connection):
    """Tests fetching inventory for a specific branch."""
    # Arrange
    db_connection.Inventory.insert_many([
        {"product_code": "P1", "branchID": "B1", "quantity": 10},
        {"product_code": "P2", "branchID": "B1", "quantity": 20},
        {"product_code": "P3", "branchID": "B2", "quantity": 30}
    ])

    # Act
    response = test_client.get("/get_inventory_by_branch?branchId=B1")

    # Assert
    assert response.status_code == 200
    data = response.json()
    assert len(data) == 2
    item_ids = {item['itemId'] for item in data}
    assert "P1" in item_ids and "P2" in item_ids


def test_get_branches(test_client, db_connection):
    """Tests fetching all branch documents."""
    # Arrange
    db_connection.Branch.insert_many([
        {"branchID": "B001", "location": "Main Branch"},
        {"branchID": "B002", "location": "Second Branch"}
    ])

    # Act
    response = test_client.get("/get_branches")

    # Assert
    assert response.status_code == 200
    data = response.json()
    assert len(data) == 2
    assert data[0]["branchId"] == "B001"
    assert data[1]["branchName"] == "Second Branch"

