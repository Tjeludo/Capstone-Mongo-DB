import pytest
from fastapi.testclient import TestClient
from unittest.mock import patch
from backend.user_api import router
from fastapi import FastAPI

# --- Test Setup ---
app = FastAPI()
app.include_router(router)
client = TestClient(app)


# --- Test Cases ---

def test_login_success():
    """Tests the POST /login endpoint."""
    with patch("backend.user_api.User") as MockUser:
        MockUser.log_in.return_value = {"success": True, "message": "2FA required"}
        response = client.post("/login", json={"staff_id": "STF-01", "password": "password123"})
        assert response.status_code == 200
        assert response.json()["message"] == "2FA required"
        MockUser.log_in.assert_called_with("STF-01", "password123")

def test_verify_2fa_success():
    """Tests the POST /verify-2fa endpoint."""
    with patch("backend.user_api.User") as MockUser:
        MockUser.verify_2fa.return_value = {"success": True, "message": "Login successful", "data": {}}
        response = client.post("/verify-2fa", json={"staff_id": "STF-01", "otp_code": "123456"})
        assert response.status_code == 200
        assert response.json()["success"] is True
        MockUser.verify_2fa.assert_called_with("STF-01", "123456")

def test_change_password_success():
    """Tests the POST /users/change-password endpoint."""
    with patch("backend.user_api.User") as MockUser:
        MockUser.change_password.return_value = {"success": True}
        payload = {"staff_id": "STF-01", "old_password": "old", "new_password": "new"}
        response = client.post("/users/change-password", json=payload)
        assert response.status_code == 200
        assert response.json()["success"] is True
        MockUser.change_password.assert_called_with("STF-01", "old", "new")

def test_view_users_success():
    """Tests the GET /users endpoint."""
    with patch("backend.user_api.User") as MockUser:
        MockUser.view_users.return_value = {"success": True, "data": [{"staff_id": "STF-01"}]}
        response = client.get("/users")
        assert response.status_code == 200
        assert response.json()["success"] is True
        assert len(response.json()["data"]) == 1

def test_update_user_status_success():
    """Tests the POST /users/status endpoint."""
    with patch("backend.user_api.User") as MockUser:
        MockUser.update_user_status.return_value = {"success": True}
        response = client.post("/users/status", json={"staff_id": "STF-01", "status": "inactive"})
        assert response.status_code == 200
        assert response.json()["success"] is True
        MockUser.update_user_status.assert_called_with("STF-01", "inactive")

def test_search_user_success():
    """Tests the GET /users/search endpoint."""
    with patch("backend.user_api.User") as MockUser:
        MockUser.search_user.return_value = {"success": True, "data": {}}
        response = client.get("/users/search?staff_id=STF-01")
        assert response.status_code == 200
        assert response.json()["success"] is True
        MockUser.search_user.assert_called_with("STF-01")

def test_delete_user_success():
    """Tests the DELETE /users/delete endpoint."""
    with patch("backend.user_api.User") as MockUser:
        MockUser.delete_user.return_value = {"success": True}
        response = client.delete("/users/delete?staff_id=STF-01")
        assert response.status_code == 200
        assert response.json()["success"] is True
        MockUser.delete_user.assert_called_with("STF-01")