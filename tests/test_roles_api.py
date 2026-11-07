import pytest
from fastapi.testclient import TestClient
from unittest.mock import patch
from backend.roles_api import router
from fastapi import FastAPI

# --- Test Setup ---
app = FastAPI()
app.include_router(router)
client = TestClient(app)


# --- Test Cases ---

def test_get_roles():
    """Tests the GET /roles endpoint."""
    with patch("backend.roles_api.Role") as MockRole:
        MockRole.view_roles.return_value = {"success": True, "data": []}
        response = client.get("/roles")
        assert response.status_code == 200
        assert response.json()["success"] is True
        MockRole.view_roles.assert_called_once()


def test_add_role():
    """Tests the POST /roles/add endpoint."""
    with patch("backend.roles_api.Role") as MockRole:
        mock_instance = MockRole.return_value
        mock_instance.add_role.return_value = {"success": True}

        # UPDATED: Payload no longer includes 'description'
        payload = {"name": "Test Role", "abbrev": "TST", "status": "active"}
        response = client.post("/roles/add", json=payload)

        assert response.status_code == 200
        assert response.json()["success"] is True
        # Verify Role was instantiated with the correct, updated arguments
        MockRole.assert_called_with("Test Role", "TST", "active")
        mock_instance.add_role.assert_called_once()


def test_update_role():
    """Tests the POST /roles/update endpoint."""
    with patch("backend.roles_api.Role") as MockRole:
        MockRole.update_role.return_value = {"success": True}

        # UPDATED: Payload no longer includes 'description'
        payload = {"role_code": 10, "name": "Updated Role", "abbrev": "UPD", "status": "inactive"}
        response = client.post("/roles/update", json=payload)

        assert response.status_code == 200
        assert response.json()["success"] is True
        # Verify update_role was called with the correct, updated arguments
        MockRole.update_role.assert_called_with(10, "Updated Role", "UPD", "inactive")


def test_delete_role():
    """Tests the DELETE /roles/{role_code} endpoint."""
    with patch("backend.roles_api.Role") as MockRole:
        MockRole.delete_role.return_value = {"success": True}
        response = client.delete("/roles/15")
        assert response.status_code == 200
        assert response.json()["success"] is True
        MockRole.delete_role.assert_called_with(15)


def test_search_role():
    """Tests the POST /roles/search endpoint."""
    with patch("backend.roles_api.Role") as MockRole:
        MockRole.search_role.return_value = {"success": True}
        response = client.post("/roles/search", json={"query": 20})
        assert response.status_code == 200
        assert response.json()["success"] is True
        MockRole.search_role.assert_called_with(20)


def test_download_csv_success():
    """Tests the GET /roles/download endpoint when data exists."""
    with patch("backend.roles_api.Role") as MockRole:
        MockRole.download_roles_csv.return_value = "role_name,role_abbrev\nAdmin,ADM"
        response = client.get("/roles/download")
        assert response.status_code == 200
        assert "Admin,ADM" in response.text


def test_download_csv_no_data():
    """Tests the GET /roles/download endpoint when no data exists."""
    with patch("backend.roles_api.Role") as MockRole:
        MockRole.download_roles_csv.return_value = ""  # Simulate no data
        response = client.get("/roles/download")
        assert response.status_code == 404