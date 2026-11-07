from fastapi.testclient import TestClient
from unittest.mock import patch
from backend.permissions_api import router
from fastapi import FastAPI

app = FastAPI()
app.include_router(router)
client = TestClient(app)


def test_get_permissions():
    """Tests the GET /permissions endpoint."""
    with patch("backend.permissions_api.Permission") as MockPermission:
        MockPermission.view_permissions.return_value = {"success": True, "data": []}
        response = client.get("/permissions")
        assert response.status_code == 200
        assert response.json()["success"] is True


def test_add_permission_section():
    """Tests the POST /permissions/add endpoint for creating a section of permissions."""
    # This endpoint creates multiple permissions, so we mock the class constructor
    with patch("backend.permissions_api.Permission") as MockPermission:
        mock_instance = MockPermission.return_value
        mock_instance.add_permission.return_value = {"success": True}

        response = client.post("/permissions/add", json={"section_name": "orders"})

        assert response.status_code == 200
        assert response.json()["success"] is True
        # Check that Permission was instantiated 4 times (for view, add, edit, delete)
        assert MockPermission.call_count == 4
        # Check one of the calls to ensure correct perm_name construction
        MockPermission.assert_any_call("add_orders", "active")


def test_update_permission():
    """Tests the POST /permissions/update endpoint."""
    with patch("backend.permissions_api.Permission") as MockPermission:
        MockPermission.update_permission.return_value = {"success": True}
        payload = {"perm_id": "PERM-001", "perm_name": "new_name", "perm_status": "inactive"}
        response = client.post("/permissions/update", json=payload)
        assert response.status_code == 200
        assert response.json()["success"] is True
        MockPermission.update_permission.assert_called_with("PERM-001", "new_name", "inactive")


def test_grant_permission():
    """Tests the POST /permissions/grant endpoint."""
    with patch("backend.permissions_api.Permission") as MockPermission:
        MockPermission.grant_permission.return_value = {"success": True}
        response = client.post("/permissions/grant", json={"perm_id": "PERM-001", "role_name": "Manager"})
        assert response.status_code == 200
        assert response.json()["success"] is True
        MockPermission.grant_permission.assert_called_with("PERM-001", "Manager")