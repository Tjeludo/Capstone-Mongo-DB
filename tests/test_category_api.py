import pytest
from fastapi.testclient import TestClient
from unittest.mock import patch, MagicMock, AsyncMock
from backend.category_api import router
from fastapi import FastAPI

# --- Test Setup ---
app = FastAPI()
app.include_router(router)
client = TestClient(app)

# --- Test Cases ---

def test_get_categories():
    """Tests the GET /categories endpoint for a successful response."""
    with patch("backend.category_api.Category") as MockCategory:
        MockCategory.view_categories.return_value = {"success": True, "data": []}
        response = client.get("/categories")
        assert response.status_code == 200
        assert response.json()["success"] is True
        MockCategory.view_categories.assert_called_once()

def test_get_category_products():
    """Tests the GET /categories/view endpoint for a successful response."""
    with patch("backend.category_api.Category") as MockCategory:
        MockCategory.view_category_products.return_value = {"success": True, "data": ["product1"]}
        response = client.get("/categories/view?id=C01")
        assert response.status_code == 200
        assert response.json()["success"] is True
        MockCategory.view_category_products.assert_called_with("C01")

def test_add_category():
    """Tests the POST /categories/add endpoint for successful category creation."""
    with patch("backend.category_api.Category") as MockCategory:
        mock_instance = MockCategory.return_value
        mock_instance.add_category.return_value = {"success": True}
        response = client.post("/categories/add", json={"name": "Books", "description": "Desc"})
        assert response.status_code == 200
        assert response.json()["success"] is True
        MockCategory.assert_called_with("Books", "Desc")
        mock_instance.add_category.assert_called_once()

# --- NEW EDGE CASE TEST ---
def test_add_category_invalid_payload():
    """Tests that the add endpoint fails if required fields are missing."""
    # Pydantic validation should catch the missing 'description' field.
    response = client.post("/categories/add", json={"name": "Missing Description"})
    # FastAPI returns a 422 Unprocessable Entity for validation errors.
    assert response.status_code == 422


def test_update_category():
    """Tests the POST /categories/update endpoint for a successful update."""
    with patch("backend.category_api.Category") as MockCategory:
        MockCategory.update_category.return_value = {"success": True}
        response = client.post("/categories/update", json={"id": "C01", "name": "Books", "description": "Desc"})
        assert response.status_code == 200
        assert response.json()["success"] is True
        MockCategory.update_category.assert_called_with("C01", "Books", "Desc")

def test_download_categories_csv_success():
    """Tests the GET /categories/download endpoint when data exists."""
    with patch("backend.category_api.Category") as MockCategory:
        MockCategory.download_category_csv.return_value = "category_code,category_name\nC01,Books"
        response = client.get("/categories/download")
        assert response.status_code == 200
        assert "Books" in response.text
        assert "text/csv" in response.headers["content-type"]

def test_download_categories_csv_not_found():
    """Tests the GET /categories/download endpoint when no data exists."""
    with patch("backend.category_api.Category") as MockCategory:
        MockCategory.download_category_csv.return_value = None
        response = client.get("/categories/download")
        assert response.status_code == 404
        assert response.json()["success"] is False

@pytest.mark.asyncio
async def test_upload_categories_csv_success():
    """Tests the POST /categories/upload endpoint for a successful file upload."""
    with patch("backend.category_api.Category") as MockCategory:
        MockCategory.upload_categories_csv = AsyncMock(return_value={"success": True})
        file_data = {"file": ("test.csv", b"category_name,description\nBooks,Desc\n", "text/csv")}
        response = client.post("/categories/upload", files=file_data)
        assert response.status_code == 200
        assert response.json()["success"] is True
        MockCategory.upload_categories_csv.assert_awaited_once()

@pytest.mark.asyncio
async def test_upload_categories_csv_api_invalid_columns():
    """Tests the upload endpoint's response when CSV columns are wrong."""
    with patch("backend.category_api.Category") as MockCategory:
        # Configure the mock to return the specific failure message from the logic
        MockCategory.upload_categories_csv = AsyncMock(return_value={
            "success": False,
            "message": "CSV must contain exactly these columns: category_name, description"
        })
        file_data = {"file": ("bad.csv", b"name,desc\n", "text/csv")}
        response = client.post("/categories/upload", files=file_data)
        assert response.status_code == 200 # The API itself handles the error gracefully
        assert response.json()["success"] is False
        assert "must contain exactly these columns" in response.json()["message"]


def test_upload_categories_csv_wrong_filetype():
    """Tests the POST /categories/upload endpoint with a non-CSV file."""
    file_data = {"file": ("test.txt", b"invalid", "text/plain")}
    response = client.post("/categories/upload", files=file_data)
    assert response.status_code == 400
    assert response.json()["success"] is False