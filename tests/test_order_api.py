import pytest
from fastapi.testclient import TestClient
from unittest.mock import patch
from backend.order_api import router
from fastapi import FastAPI

# --- Test Setup ---
app = FastAPI()
app.include_router(router)
client = TestClient(app)


# Note: The /orders/upload endpoint is defined in your API, but the corresponding
# 'upload_orders_csv' method is missing from your Order class.
# Tests for that endpoint have been omitted.

# --- Test Cases ---

def test_get_orders_success():
    """Tests the GET /orders endpoint."""
    with patch("backend.order_api.Order") as MockOrder:
        MockOrder.view_orders.return_value = {"success": True, "data": [{"order_id": "ODR001"}]}
        response = client.get("/orders")
        assert response.status_code == 200
        assert response.json()["success"] is True
        MockOrder.view_orders.assert_called_once()


def test_get_upcoming_deliveries_success():
    """Tests the GET /orders/deliveries/upcoming endpoint."""
    with patch("backend.order_api.Order") as MockOrder:
        MockOrder.get_upcoming_deliveries.return_value = {"success": True, "data": []}
        response = client.get("/orders/deliveries/upcoming")
        assert response.status_code == 200
        assert response.json()["success"] is True
        MockOrder.get_upcoming_deliveries.assert_called_once()


def test_get_order_products_success():
    """Tests the GET /orders/view/{order_id} endpoint."""
    with patch("backend.order_api.Order") as MockOrder:
        MockOrder.view_order_products.return_value = {"success": True, "data": [{"product": "Test"}]}
        response = client.get("/orders/view/ODR001")
        assert response.status_code == 200
        assert response.json()["success"] is True
        MockOrder.view_order_products.assert_called_with("ODR001")


def test_confirm_order_success():
    """Tests the POST /orders/confirm endpoint."""
    with patch("backend.order_api.Order") as MockOrder:
        mock_instance = MockOrder.return_value
        mock_instance.confirm_order.return_value = {"success": True}

        payload = {"supplier_id": "S01", "products": [], "total_cost": 120.50, "ordered_by": "EMP01"}
        response = client.post("/orders/confirm", json=payload)

        assert response.status_code == 200
        assert response.json()["success"] is True
        # Check that the Order class was instantiated correctly
        MockOrder.assert_called_with(supplier_id="S01", staff_id="EMP01")
        # Check that the instance attributes were set from the payload
        assert mock_instance.products == []
        assert mock_instance.total_cost == 120.50
        mock_instance.confirm_order.assert_called_once()


def test_confirm_order_invalid_payload():
    """Tests the POST /orders/confirm endpoint with missing data."""
    response = client.post("/orders/confirm", json={"supplier_id": "S01"})
    assert response.status_code == 422  # FastAPI's Unprocessable Entity


def test_cancel_order_success():
    """Tests the POST /orders/cancel endpoint."""
    with patch("backend.order_api.Order") as MockOrder:
        MockOrder.cancel_order.return_value = {"success": True}
        response = client.post("/orders/cancel", json={"id": "ODR001"})
        assert response.status_code == 200
        assert response.json()["success"] is True
        MockOrder.cancel_order.assert_called_with("ODR001")


def test_update_order_status_success():
    """Tests the POST /orders/status endpoint."""
    with patch("backend.order_api.Order") as MockOrder:
        MockOrder.update_order_status.return_value = {"success": True}
        response = client.post("/orders/status", json={"id": "ODR001", "status": "In-Progress"})
        assert response.status_code == 200
        assert response.json()["success"] is True
        MockOrder.update_order_status.assert_called_with("ODR001", "In-Progress")


def test_search_order_success():
    """Tests the POST /orders/search endpoint."""
    with patch("backend.order_api.Order") as MockOrder:
        MockOrder.search_order.return_value = {"success": True, "data": {}}
        response = client.post("/orders/search", json={"query": "ODR001"})
        assert response.status_code == 200
        assert response.json()["success"] is True
        MockOrder.search_order.assert_called_with("ODR001")


def test_download_orders_csv_success():
    """Tests the GET /orders/download endpoint when data exists."""
    with patch("backend.order_api.Order") as MockOrder:
        MockOrder.download_orders_csv.return_value = "order_id,status\nODR001,Pending"
        response = client.get("/orders/download")
        assert response.status_code == 200
        assert "ODR001,Pending" in response.text
        assert "text/csv" in response.headers["content-type"]


def test_download_orders_csv_no_data():
    """Tests the GET /orders/download endpoint when no data exists."""
    with patch("backend.order_api.Order") as MockOrder:
        MockOrder.download_orders_csv.return_value = ""  # Simulate no data
        response = client.get("/orders/download")
        assert response.status_code == 404