import pytest
from fastapi.testclient import TestClient
from fastapi import FastAPI
from pymongo import MongoClient
import os
import datetime

# Import the router and the modules that connect to the database
from backend.api import router
import backend.api as api_module
import backend.delivery as delivery_module

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
    db = client["test_inventory_db_delivery"] # Use a dedicated test database

    yield db

    print("\nDropping delivery test database...")
    client.drop_database("test_inventory_db_delivery")
    client.close()


@pytest.fixture(scope="function")
def test_client(db_connection, monkeypatch):
    """
    Provides a TestClient instance, ensures the database is clean, and patches
    the modules to use the test database connection.
    """
    # Use monkeypatch to replace the real DB connections with the test DB
    monkeypatch.setattr(api_module, 'orders_table', db_connection.orders)
    monkeypatch.setattr(api_module, 'delivery_table', db_connection.deliveries)
    monkeypatch.setattr(api_module, 'inventory_table', db_connection.Inventory)
    monkeypatch.setattr(api_module, 'suppliers_table', db_connection.suppliers)
    monkeypatch.setattr(delivery_module, 'orders_table', db_connection.orders)
    monkeypatch.setattr(delivery_module, 'delivery_table', db_connection.deliveries)
    monkeypatch.setattr(delivery_module, 'inventory_table', db_connection.Inventory)

    # Cleanup before each test to ensure isolation
    for collection_name in db_connection.list_collection_names():
        db_connection[collection_name].delete_many({})
    
    with TestClient(app) as client:
        yield client


# --- Delivery Integration Tests ---

def test_full_delivery_workflow(test_client, db_connection):
    """
    Tests the entire delivery process: saving a delivery, updating the order,
    and updating the inventory.
    """
    # 1. Arrange
    db_connection.suppliers.insert_one({"supplier_id": "S01", "supplier_name": "ABC Babies"})
    db_connection.orders.insert_one({
        "order_id": "ODR001", "supplier_id": "S01", "status": "Dispatched", # Corrected status
        "products": [{"product_code": "0090010115", "description": "Stroller", "quantity": 10}]
    })
    db_connection.Inventory.insert_one({"product_code": "0090010115", "branchID": "B001", "quantity": 50})

    # 2. Act
    delivery_payload = {
        "supplierId": "S01", "supplierName": "ABC Babies", "orderId": "ODR001",
        "checkedBy": "Alice",
        "deliveryItems": [{"itemId": "0090010115", "itemName": "Stroller", "quantityReceived": 10}]
    }
    response = test_client.post("/save_delivery", json=delivery_payload)

    # 3. Assert
    assert response.status_code == 200
    assert "deliveryNum" in response.json()

    delivery_in_db = db_connection.deliveries.find_one({"order_id": "ODR001"})
    assert delivery_in_db is not None
    assert delivery_in_db["checkedByWho"] == "Alice"

    order_in_db = db_connection.orders.find_one({"order_id": "ODR001"})
    assert order_in_db["status"] == "Fully Fulfilled"

    inventory_item = db_connection.Inventory.find_one({"product_code": "0090010115"})
    assert inventory_item["quantity"] == 60


def test_get_order_items_for_unfulfilled_order(test_client, db_connection):
    """Tests fetching items for a valid, unfulfilled order."""
    # Arrange
    db_connection.orders.insert_one({
        "order_id": "ODR002", "status": "Dispatched", "supplier_id": "S02", # Corrected status
        "products": [{"product_code": "0040010017", "description": "Feeding Bottle", "quantity": 20}]
    })
    db_connection.suppliers.insert_one({"supplier_id": "S02", "supplier_name": "Little Baby"})

    # Act
    response = test_client.post("/get_order_items", json={"orderId": "ODR002"})

    # Assert
    assert response.status_code == 200
    data = response.json()
    assert data["supplierName"] == "Little Baby"
    assert len(data["items"]) == 1
    assert data["items"][0]["missingQuantity"] == 20


@pytest.mark.parametrize("status, expected_message", [
    ("Fully Fulfilled", "already fully fulfilled"),
    ("Cancelled", "has been cancelled"),
    ("Pending", "still pending confirmation"),
    ("Confirmed", "confirmed but not yet dispatched"),
    ("In-Progress", "currently in progress")
])
def test_get_order_items_for_invalid_status_fails(test_client, db_connection, status, expected_message):
    """Tests that the API returns a 409 Conflict error for various invalid order statuses."""

    order_id = f"O-{status.replace(' ', '-')}"

    db_connection.orders.insert_one({
        "order_id": order_id, "status": status, "supplier_id": "S-temp",
        "products": [{"product_code": "P-temp", "description": "Temp", "quantity": 1}]
    })

    # Act
    response = test_client.post("/get_order_items", json={"orderId": order_id})

    # Assert
    assert response.status_code == 409
    assert expected_message in response.json()["detail"]


def test_generate_delivery_number(test_client, db_connection):
    """Tests the delivery number generation endpoint."""
    # Scenario 1: No deliveries in the database
    response = test_client.get("/generate_delivery_number")
    assert response.status_code == 200
    assert response.json() == {"deliveryNum": "DEL001"}

    # Scenario 2: A delivery already exists
    db_connection.deliveries.insert_one({"delivery_num": "DEL005"})
    response = test_client.get("/generate_delivery_number")
    assert response.status_code == 200
    assert response.json() == {"deliveryNum": "DEL006"}


def test_get_deliveries(test_client, db_connection):
    """Tests fetching all deliveries, ensuring they are returned and sorted correctly."""
    # Arrange
    db_connection.deliveries.insert_many([
        {"delivery_num": "DEL001", "date": "2025-10-26", "supplier name": "ABC Babies"},
        {"delivery_num": "DEL002", "date": "2025-10-27", "supplier name": "Little Baby"}
    ])

    # Act
    response = test_client.get("/get_deliveries")

    # Assert
    assert response.status_code == 200
    data = response.json()
    assert len(data) == 2
    assert data[0]["DeliveryId"] == "DEL002" # Newest first
    assert data[1]["DeliveryId"] == "DEL001"

