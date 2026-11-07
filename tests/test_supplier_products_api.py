import pytest
from fastapi.testclient import TestClient
from unittest.mock import patch, AsyncMock
from backend.supplier_products_api import router
from fastapi import FastAPI

# --- Test Setup ---
app = FastAPI()
app.include_router(router)
client = TestClient(app)


# --- Test Cases ---

def test_get_all_supplier_products():
    """Tests the GET /supplier/products endpoint."""
    with patch("backend.supplier_products_api.SupplierItem") as MockSupplierItem:
        MockSupplierItem.view_supplier_products.return_value = {"success": True, "data": []}
        response = client.get("/supplier/products")
        assert response.status_code == 200
        assert response.json()["success"] is True
        MockSupplierItem.view_supplier_products.assert_called_once()


def test_get_products_for_supplier():
    """Tests the GET /supplier/products/{supplier_id} endpoint."""
    with patch("backend.supplier_products_api.SupplierItem") as MockSupplierItem:
        MockSupplierItem.view_products_for_supplier.return_value = {"success": True, "data": []}
        response = client.get("/supplier/products/S01")
        assert response.status_code == 200
        assert response.json()["success"] is True
        MockSupplierItem.view_products_for_supplier.assert_called_with(supplier_id="S01")


def test_add_supplier_product():
    """Tests the POST /supplier/products/add endpoint."""
    with patch("backend.supplier_products_api.SupplierItem") as MockSupplierItem:
        # Mock the instance that will be created by the endpoint
        mock_instance = MockSupplierItem.return_value
        mock_instance.add_product.return_value = {"success": True}

        payload = {"supplier_id": "S01", "product_code": "P123", "product_name": "Test", "price": 100}
        response = client.post("/supplier/products/add", json=payload)

        assert response.status_code == 200
        assert response.json()["success"] is True
        # Verify that the class was instantiated with the correct data
        MockSupplierItem.assert_called_with("S01", "Test", "P123", 100)
        mock_instance.add_product.assert_called_once()


def test_update_supplier_product():
    """Tests the POST /supplier/products/update endpoint."""
    with patch("backend.supplier_products_api.SupplierItem") as MockSupplierItem:
        MockSupplierItem.update_supplier_price.return_value = {"success": True}
        payload = {"supplier_id": "S01", "product_code": "P123", "price": 199.99}
        response = client.post("/supplier/products/update", json=payload)
        assert response.status_code == 200
        assert response.json()["success"] is True
        MockSupplierItem.update_supplier_price.assert_called_with("S01", "P123", 199.99)


def test_delete_supplier_product():
    """Tests the POST /supplier/products/delete endpoint."""
    with patch("backend.supplier_products_api.SupplierItem") as MockSupplierItem:
        MockSupplierItem.delete_supplier_product.return_value = {"success": True}
        payload = {"supplier_id": "S01", "product_code": "P123"}
        response = client.post("/supplier/products/delete", json=payload)
        assert response.status_code == 200
        assert response.json()["success"] is True
        MockSupplierItem.delete_supplier_product.assert_called_with("S01", "P123")


def test_download_supplier_products_csv():
    """Tests the GET /supplier/products/download/{supplier_id} endpoint."""
    with patch("backend.supplier_products_api.SupplierItem") as MockSupplierItem:
        MockSupplierItem.download_products_for_supplier_csv.return_value = "product_code,price\nP123,100"
        response = client.get("/supplier/products/download/S01")
        assert response.status_code == 200
        assert "P123,100" in response.text
        assert "text/csv" in response.headers["content-type"]


@pytest.mark.asyncio
async def test_upload_supplier_prices_csv():
    """Tests the POST /supplier/products/upload-prices/{supplier_id} endpoint."""
    with patch("backend.supplier_products_api.SupplierItem.upload_prices_for_supplier_csv",
               new_callable=AsyncMock) as mock_upload:
        mock_upload.return_value = {"success": True}
        file_data = {"file": ("prices.csv", b"product_code,price\nP123,150", "text/csv")}
        response = client.post("/supplier/products/upload-prices/S01", files=file_data)
        assert response.status_code == 200
        assert response.json()["success"] is True