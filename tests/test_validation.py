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
import backend.StockTake as stocktake_module

# --- Test Application Setup ---
app = FastAPI()
app.include_router(router)

# --- Database Fixture ---
@pytest.fixture(scope="module")
def db_connection():
    """
    Creates a connection to a dedicated validation test MongoDB database.
    """
    mongo_uri = os.getenv("MONGO_TEST_URI", "mongodb://localhost:27017/")
    client = MongoClient(mongo_uri)
    db = client["test_inventory_db_validation"] # Use a dedicated validation database

    yield db

    print("\nDropping validation test database...")
    client.drop_database("test_inventory_db_validation")
    client.close()


@pytest.fixture(scope="function")
def test_client(db_connection, monkeypatch):
    """
    Provides a TestClient, cleans the DB, and patches all modules
    to use the isolated test database.
    """
    # Patch all modules that have a database connection
    db_tables = {
        'orders_table': db_connection.orders,
        'delivery_table': db_connection.deliveries,
        'inventory_table': db_connection.Inventory,
        'stock_take_table': db_connection.stock_take,
        'branches_table': db_connection.Branch,
        'suppliers_table': db_connection.suppliers
    }
    for module in [api_module, delivery_module, stocktake_module]:
        for table_name, table_obj in db_tables.items():
            if hasattr(module, table_name):
                monkeypatch.setattr(module, table_name, table_obj)

    # Clean database before each test
    for collection_name in db_connection.list_collection_names():
        db_connection[collection_name].delete_many({})
    
    with TestClient(app) as client:
        yield client

# --- Validation Tests based on User Stories ---

class TestUserStoryDeliveries:
    """User Story: As an inventory manager, I want to record a delivery against an order."""

    def test_ac1_record_full_delivery(self, test_client, db_connection):
        """
        Acceptance Criterion: When a delivery for the full quantity of an order is
        recorded, the order status must become 'Fully Fulfilled' and inventory must increase.
        """
        # Arrange: A dispatched order exists for 10 strollers, and stock is at 50.
        db_connection.suppliers.insert_one({"supplier_id": "S01", "supplier_name": "Test Supplier"})
        db_connection.orders.insert_one({
            "order_id": "ORDER-101", "supplier_id": "S01", "status": "Dispatched",
            "products": [{"product_code": "STROLLER-01", "description": "Stroller", "quantity": 10}]
        })
        db_connection.Inventory.insert_one({"product_code": "STROLLER-01", "branchID": "B001", "quantity": 50})

        # Act: The manager records a delivery of 10 strollers.
        delivery_payload = {
            "supplierId": "S01", "supplierName": "Test Supplier", "orderId": "ORDER-101",
            "checkedBy": "Manager A",
            "deliveryItems": [{"itemId": "STROLLER-01", "itemName": "Stroller", "quantityReceived": 10}]
        }
        response = test_client.post("/save_delivery", json=delivery_payload)
        assert response.status_code == 200

        # Assert: The system validates that the order is now fulfilled and stock is correct.
        order = db_connection.orders.find_one({"order_id": "ORDER-101"})
        inventory = db_connection.Inventory.find_one({"product_code": "STROLLER-01"})
        
        assert order["status"] == "Fully Fulfilled"
        assert inventory["quantity"] == 60 # 50 initial + 10 delivered

    def test_ac2_record_partial_delivery(self, test_client, db_connection):
        """
        Acceptance Criterion: When a partial delivery is recorded, the order status must
        become 'Partially Fulfilled' and inventory must still increase.
        """
        # Arrange: A dispatched order exists for 10 strollers.
        db_connection.suppliers.insert_one({"supplier_id": "S01", "supplier_name": "Test Supplier"})
        db_connection.orders.insert_one({
            "order_id": "ORDER-102", "supplier_id": "S01", "status": "Dispatched",
            "products": [{"product_code": "STROLLER-02", "description": "Stroller", "quantity": 10}]
        })
        db_connection.Inventory.insert_one({"product_code": "STROLLER-02", "branchID": "B001", "quantity": 20})

        # Act: The manager records a delivery of only 4 strollers.
        delivery_payload = {
            "supplierId": "S01", "supplierName": "Test Supplier", "orderId": "ORDER-102",
            "checkedBy": "Manager B",
            "deliveryItems": [{"itemId": "STROLLER-02", "itemName": "Stroller", "quantityReceived": 4}]
        }
        response = test_client.post("/save_delivery", json=delivery_payload)
        assert response.status_code == 200

        # Assert: The system validates the order is partially fulfilled.
        order = db_connection.orders.find_one({"order_id": "ORDER-102"})
        inventory = db_connection.Inventory.find_one({"product_code": "STROLLER-02"})

        assert order["status"] == "Partially Fulfilled"
        assert inventory["quantity"] == 24 # 20 initial + 4 delivered

    
    def test_ac4_reject_missing_fields(self, test_client, db_connection):
        """
        Acceptance Criterion: The system must reject a delivery submission if required
        fields (e.g., orderId) are missing.
        """
        # Arrange: A valid payload is missing the 'orderId'.
        delivery_payload = {
            "supplierId": "S01", "supplierName": "Test Supplier",
            "checkedBy": "Manager D",
            "deliveryItems": [{"itemId": "STROLLER-04", "itemName": "Stroller", "quantityReceived": 1}]
        }
        
        # Act: The manager submits the incomplete form.
        response = test_client.post("/save_delivery", json=delivery_payload)

        # Assert: The system rejects the request due to a validation error.
        # FastAPI's Pydantic models automatically return a 422 for missing required fields.
        assert response.status_code == 422
        assert "field required" in response.text.lower()


class TestUserStoryStocktake:
    """User Story: As a branch manager, I want to perform a stocktake to correct inventory levels."""

    def test_ac1_perform_stocktake_corrects_inventory(self, test_client, db_connection):
        """
        Acceptance Criterion: When a stocktake is submitted with a new quantity, the system must
        update that item's inventory level to the new amount.
        """
        # Arrange: An item exists in inventory with a recorded quantity of 100.
        db_connection.Inventory.insert_one({"product_code": "BOTTLE-01", "branchID": "B002", "quantity": 100})
        
        # Act: The manager performs a count and finds only 95 items, then submits the stocktake.
        stocktake_payload = {
            "stockTakeNum": "ST-VAL-001", "branchId": "B002",
            "date": datetime.date.today().strftime("%Y-%m-%d"),
            "items": [{"itemID": "BOTTLE-01", "itemName": "Feeding Bottle", "previousQuantity": 100, "quantityOnHand": 95}]
        }
        response = test_client.post("/save_stocktake", json=stocktake_payload)
        assert response.status_code == 200

        # Assert: The system validates that the inventory level is now corrected to 95.
        inventory = db_connection.Inventory.find_one({"product_code": "BOTTLE-01", "branchID": "B002"})
        assert inventory["quantity"] == 95

    def test_ac2_reject_negative_quantity(self, test_client, db_connection):
        """
        Acceptance Criterion: The system must reject a stocktake submission if the
        quantity on hand for any item is a negative number.
        """
        # Arrange: A valid payload is created with an invalid quantity.
        stocktake_payload = {
            "stockTakeNum": "ST-VAL-002", "branchId": "B002",
            "date": datetime.date.today().strftime("%Y-%m-%d"),
            "items": [{"itemID": "BOTTLE-02", "itemName": "Feeding Bottle", "previousQuantity": 100, "quantityOnHand": -5}]
        }

        # Act: The manager submits the form with a negative quantity.
        response = test_client.post("/save_stocktake", json=stocktake_payload)

        # Assert: The system rejects the request due to a validation error.
        # Pydantic models should be configured to check for non-negative integers.
        assert response.status_code == 422
        assert "greater than or equal to 0" in response.text.lower()

class TestUserStoryInventoryCheck:
    """User Story: As a staff member, I need to look up inventory for my branch."""

    def test_ac1_view_inventory_for_specific_branch(self, test_client, db_connection):
        """
        Acceptance Criterion: The system must show a list of all products and their quantities
        for my specific branch, and not for other branches.
        """
        # Arrange: Multiple items exist across two different branches.
        db_connection.Inventory.insert_many([
            {"product_code": "ITEM-A", "branchID": "B-NORTH", "quantity": 10},
            {"product_code": "ITEM-B", "branchID": "B-NORTH", "quantity": 20},
            {"product_code": "ITEM-C", "branchID": "B-SOUTH", "quantity": 30} # Should not be returned
        ])

        # Act: The staff member requests the inventory for branch B-NORTH.
        response = test_client.get("/get_inventory_by_branch?branchId=B-NORTH")
        assert response.status_code == 200

        # Assert: The system validates that only items from branch B-NORTH are returned.
        data = response.json()
        assert len(data) == 2
        item_ids = {item['itemId'] for item in data}
        assert "ITEM-A" in item_ids
        assert "ITEM-B" in item_ids
        assert "ITEM-C" not in item_ids

