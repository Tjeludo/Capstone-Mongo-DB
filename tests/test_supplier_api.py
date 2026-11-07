import pytest
from fastapi.testclient import TestClient
from unittest.mock import patch, AsyncMock
from backend.supplier_api import router
from fastapi import FastAPI

# --- Test Setup ---
app = FastAPI()
app.include_router(router)
client = TestClient(app)


# --- Test Cases ---

def test_get_suppliers_success():
    """Tests the GET /suppliers endpoint."""
    with patch("backend.supplier_api.Supplier") as MockSupplier:
        MockSupplier.view_suppliers.return_value = {"success": True, "data": [{"name": "Test Supplier"}]}
        response = client.get("/suppliers")
        assert response.status_code == 200
        assert response.json()["success"] is True
        MockSupplier.view_suppliers.assert_called_once()


def test_get_supplier_products_success():
    """Tests the GET /suppliers/{supplier_id}/items endpoint."""
    with patch("backend.supplier_api.Supplier") as MockSupplier:
        MockSupplier.view_supplier_products.return_value = {"success": True, "data": ["Product A"]}
        response = client.get("/suppliers/S01/items")
        assert response.status_code == 200
        assert response.json()["success"] is True
        MockSupplier.view_supplier_products.assert_called_with("S01")


def test_add_supplier_success():
    """Tests the POST /suppliers/add endpoint for successful creation."""
    with patch("backend.supplier_api.Supplier") as MockSupplier:
        mock_instance = MockSupplier.return_value
        mock_instance.add_supplier.return_value = {"success": True}

        payload = {"name": "New Co", "email": "new@test.com", "phone_number": "123", "address": "addr",
                   "sales_rep": "rep"}
        response = client.post("/suppliers/add", json=payload)

        assert response.status_code == 200
        assert response.json()["success"] is True
        # Verify the Supplier class was instantiated with the payload data
        MockSupplier.assert_called_with("New Co", "new@test.com", "123", "addr", "rep")
        mock_instance.add_supplier.assert_called_once()


def test_add_supplier_invalid_payload():
    """Tests the POST /suppliers/add endpoint with missing data."""
    response = client.post("/suppliers/add", json={"name": "Incomplete"})
    assert response.status_code == 422  # FastAPI's Unprocessable Entity for Pydantic errors


def test_update_supplier_success():
    """Tests the POST /suppliers/update endpoint."""
    with patch("backend.supplier_api.Supplier") as MockSupplier:
        MockSupplier.update_supplier.return_value = {"success": True}
        payload = {"id": "S01", "name": "Updated Co", "email": "up@test.com", "phone_number": "456",
                   "address": "new addr", "sales_rep": "new rep"}
        response = client.post("/suppliers/update", json=payload)
        assert response.status_code == 200
        assert response.json()["success"] is True
        MockSupplier.update_supplier.assert_called_with("S01", "Updated Co", "up@test.com", "456", "new addr",
                                                        "new rep")


def test_delete_supplier_success():
    """Tests the DELETE /suppliers/{supplier_id} endpoint."""
    with patch("backend.supplier_api.Supplier") as MockSupplier:
        MockSupplier.delete_supplier.return_value = {"success": True}
        response = client.delete("/suppliers/S01")
        assert response.status_code == 200
        assert response.json()["success"] is True
        MockSupplier.delete_supplier.assert_called_with("S01")


def test_download_csv_success():
    """Tests the GET /suppliers/download endpoint when data exists."""
    with patch("backend.supplier_api.Supplier") as MockSupplier:
        MockSupplier.download_supplier_csv.return_value = "supplier_id,supplier_name\nS01,Test Co"
        response = client.get("/suppliers/download")
        assert response.status_code == 200
        assert "Test Co" in response.text
        assert "text/csv" in response.headers["content-type"]


def test_download_csv_no_data():
    """Tests the GET /suppliers/download endpoint when no data exists."""
    with patch("backend.supplier_api.Supplier") as MockSupplier:
        MockSupplier.download_supplier_csv.return_value = ""  # Simulate no data
        response = client.get("/suppliers/download")
        assert response.status_code == 404


@pytest.mark.asyncio
async def test_upload_csv_success():
    """Tests the POST /suppliers/upload endpoint for a successful file upload."""
    with patch("backend.supplier_api.Supplier.upload_suppliers_csv", new_callable=AsyncMock) as mock_upload:
        mock_upload.return_value = {"success": True}
        file_data = {"file": ("suppliers.csv", b"supplier_name,email\nTest,test@test.com", "text/csv")}
        response = client.post("/suppliers/upload", files=file_data)
        assert response.status_code == 200
        assert response.json()["success"] is True