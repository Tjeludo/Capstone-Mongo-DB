import pytest
from fastapi.testclient import TestClient
from unittest.mock import patch, AsyncMock
from backend.staff_api import router
from fastapi import FastAPI

# --- Test Setup ---
app = FastAPI()
app.include_router(router)
client = TestClient(app)


# --- Test Cases ---

def test_get_staff_success():
    """Tests the GET /staff endpoint."""
    with patch("backend.staff_api.Staff") as MockStaff:
        MockStaff.view_staff.return_value = {"success": True, "data": [{"firstname": "John"}]}
        response = client.get("/staff")
        assert response.status_code == 200
        assert response.json()["success"] is True
        MockStaff.view_staff.assert_called_once()


def test_add_staff_success():
    """Tests the POST /staff/add endpoint for successful creation."""
    with patch("backend.staff_api.Staff") as MockStaff:
        mock_instance = MockStaff.return_value
        mock_instance.add_staff.return_value = {"success": True}

        payload = {
            "branch_code": "B01", "role": "MAN", "firstname": "Jane", "lastname": "Doe",
            "email": "jane@test.com", "date_of_birth": "1990-01-01", "gender": "Female",
            "phone_number": "12345", "address": "123 Main St", "status": "Active", "start_date": "2025-01-01"
        }
        response = client.post("/staff/add", json=payload)

        assert response.status_code == 200
        assert response.json()["success"] is True
        # Verify Staff was instantiated with the correct data from the payload
        MockStaff.assert_called_with(
            "B01", "MAN", "Jane", "Doe", "jane@test.com", "1990-01-01", "Female",
            "12345", "123 Main St", "Active", "2025-01-01"
        )
        mock_instance.add_staff.assert_called_once()


def test_add_staff_invalid_payload():
    """Tests the POST /staff/add endpoint with missing data."""
    response = client.post("/staff/add", json={"firstname": "Incomplete"})
    assert response.status_code == 422  # FastAPI's Unprocessable Entity for Pydantic errors


def test_delete_staff_success():
    """Tests the DELETE /staff/{staff_id} endpoint."""
    with patch("backend.staff_api.Staff") as MockStaff:
        MockStaff.delete_staff.return_value = {"success": True}
        response = client.delete("/staff/STF-010125")
        assert response.status_code == 200
        assert response.json()["success"] is True
        MockStaff.delete_staff.assert_called_with("STF-010125")


def test_transfer_staff_success():
    """Tests the POST /staff/transfer endpoint."""
    with patch("backend.staff_api.Staff") as MockStaff:
        MockStaff.transfer_staff.return_value = {"success": True}
        response = client.post("/staff/transfer", json={"id": "STF-010125", "branch_code": "B02"})
        assert response.status_code == 200
        assert response.json()["success"] is True
        MockStaff.transfer_staff.assert_called_with("STF-010125", "B02")


def test_promote_staff_success():
    """Tests the POST /staff/promote endpoint."""
    with patch("backend.staff_api.Staff") as MockStaff:
        MockStaff.promote_staff.return_value = {"success": True}
        response = client.post("/staff/promote", json={"id": "STF-010125", "role": "SNR"})
        assert response.status_code == 200
        assert response.json()["success"] is True
        MockStaff.promote_staff.assert_called_with("STF-010125", "SNR")


def test_download_csv_success():
    """Tests the GET /staff/download endpoint when data exists."""
    with patch("backend.staff_api.Staff") as MockStaff:
        MockStaff.download_staff_csv.return_value = "staff_id,firstname\nSTF-010125,John"
        response = client.get("/staff/download")
        assert response.status_code == 200
        assert "John" in response.text
        assert "text/csv" in response.headers["content-type"]


def test_download_csv_no_data():
    """Tests the GET /staff/download endpoint when no data exists."""
    with patch("backend.staff_api.Staff") as MockStaff:
        MockStaff.download_staff_csv.return_value = ""  # Simulate no data
        response = client.get("/staff/download")
        assert response.status_code == 404


@pytest.mark.asyncio
async def test_upload_csv_success():
    """Tests the POST /staff/upload endpoint for a successful file upload."""
    with patch("backend.staff_api.Staff.upload_staff_csv", new_callable=AsyncMock) as mock_upload:
        mock_upload.return_value = {"success": True}
        file_data = {"file": ("staff.csv", b"firstname,lastname\nJohn,Doe", "text/csv")}
        response = client.post("/staff/upload", files=file_data)
        assert response.status_code == 200
        assert response.json()["success"] is True