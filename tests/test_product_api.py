import pytest
from fastapi.testclient import TestClient
from unittest.mock import patch, AsyncMock
from backend.product_api import router
from fastapi import FastAPI

# --- Test Setup ---
app = FastAPI()
app.include_router(router)
client = TestClient(app)


# --- Test Cases ---

def test_get_products_success():
    """Tests the GET /products endpoint for a successful response."""
    with patch("backend.product_api.Product") as MockProduct:
        MockProduct.view_products.return_value = {"success": True, "data": [{"name": "Test Shoe"}]}
        response = client.get("/products")
        assert response.status_code == 200
        assert response.json()["success"] is True
        assert response.json()["data"][0]["name"] == "Test Shoe"
        MockProduct.view_products.assert_called_once()


def test_get_products_by_category_success():
    """Tests the POST /products/category endpoint."""
    with patch("backend.product_api.Product") as MockProduct:
        MockProduct.get_products_by_category.return_value = {"success": True, "data": []}
        response = client.post("/products/category", json={"category_code": "C01"})
        assert response.status_code == 200
        assert response.json()["success"] is True
        MockProduct.get_products_by_category.assert_called_with("C01")


def test_add_product_success():
    """Tests the POST /products/add endpoint for successful product creation."""
    with patch("backend.product_api.Product") as MockProduct:
        mock_instance = MockProduct.return_value
        mock_instance.add_product.return_value = {"success": True}

        payload = {"name": "Test Shoe", "category_code": "C01", "size": "M", "colour": "Red", "price": 150.0}
        response = client.post("/products/add", json=payload)

        assert response.status_code == 200
        assert response.json()["success"] is True
        MockProduct.assert_called_with("Test Shoe", "C01", "M", "Red", 150.0)
        mock_instance.add_product.assert_called_once()


def test_add_product_invalid_payload():
    """Tests the POST /products/add endpoint with missing data, expecting a 422 error."""
    response = client.post("/products/add", json={"name": "Incomplete Product"})
    assert response.status_code == 422  # FastAPI's Unprocessable Entity for Pydantic errors


def test_update_product_success():
    """Tests the POST /products/update endpoint."""
    with patch("backend.product_api.Product") as MockProduct:
        MockProduct.update_product.return_value = {"success": True}
        payload = {"product_code": "P01", "name": "Updated Shoe", "price": 200.0}
        response = client.post("/products/update", json=payload)
        assert response.status_code == 200
        assert response.json()["success"] is True
        MockProduct.update_product.assert_called_with("P01", "Updated Shoe", 200.0)


def test_search_product_by_code_success():
    """Tests the POST /products/search endpoint."""
    with patch("backend.product_api.Product") as MockProduct:
        MockProduct.search_product.return_value = {"success": True, "data": {}}
        response = client.post("/products/search", json={"product_code": "P01"})
        assert response.status_code == 200
        assert response.json()["success"] is True
        MockProduct.search_product.assert_called_with("P01")


def test_search_products_by_name_success():
    """Tests the POST /products/search/name endpoint."""
    with patch("backend.product_api.Product") as MockProduct:
        MockProduct.search_products_by_name.return_value = {"success": True, "data": []}
        response = client.post("/products/search/name", json={"name": "shoe"})
        assert response.status_code == 200
        assert response.json()["success"] is True
        MockProduct.search_products_by_name.assert_called_with("shoe")


def test_delete_product_success():
    """Tests the DELETE /products/delete/{product_code} endpoint."""
    with patch("backend.product_api.Product") as MockProduct:
        MockProduct.delete_product.return_value = {"success": True}
        response = client.delete("/products/delete/P01")
        assert response.status_code == 200
        assert response.json()["success"] is True
        MockProduct.delete_product.assert_called_with("P01")


def test_download_csv_success():
    """Tests the GET /products/download endpoint when data exists."""
    with patch("backend.product_api.Product") as MockProduct:
        MockProduct.download_products_csv.return_value = "product_code,product_name\nP01,Test Shoe"
        response = client.get("/products/download")
        assert response.status_code == 200
        assert "Test Shoe" in response.text
        assert "text/csv" in response.headers["content-type"]


def test_download_csv_no_data():
    """Tests the GET /products/download endpoint when no data exists."""
    with patch("backend.product_api.Product") as MockProduct:
        MockProduct.download_products_csv.return_value = ""  # Simulate no data
        response = client.get("/products/download")
        assert response.status_code == 404  # As per the logic in your API
        assert "No products found" in response.text


@pytest.mark.asyncio
async def test_upload_csv_success():
    """Tests the POST /products/upload endpoint for a successful file upload."""
    with patch("backend.product_api.Product.upload_products_csv", new_callable=AsyncMock) as mock_upload:
        mock_upload.return_value = {"success": True}
        file_data = {
            "file": ("products.csv", b"product_name,category_code,size,colour,price\nShoe,C01,M,Red,100", "text/csv")}
        response = client.post("/products/upload", files=file_data)
        assert response.status_code == 200
        assert response.json()["success"] is True


@pytest.mark.asyncio
async def test_upload_csv_wrong_file_type():
    """Tests the POST /products/upload endpoint with a non-CSV file."""
    file_data = {"file": ("products.txt", b"some text data", "text/plain")}
    response = client.post("/products/upload", files=file_data)
    # NOTE: While a 400 Bad Request would be more conventional,
    # your API handles this by returning a JSON response with a default 200 status code.
    # This test verifies that current behavior.
    assert response.status_code == 200
    assert response.json()["success"] is False
    assert "Please upload a CSV file" in response.json()["message"]