from fastapi.testclient import TestClient
from unittest.mock import patch
from backend.analytics_api import router
from fastapi import FastAPI

# --- Test Setup ---
app = FastAPI()
app.include_router(router)
client = TestClient(app)

# --- Test Cases ---
def test_get_dashboard_analytics_default_days():
    """Tests the endpoint with the default 'days' parameter."""
    with patch("backend.analytics_api.Analytics") as MockAnalytics:
        MockAnalytics.get_dashboard_metrics.return_value = {"success": True, "data": {}}
        response = client.get("/analytics/dashboard")
        assert response.status_code == 200
        assert response.json()["success"] is True
        # Verify the underlying logic was called with the default value
        MockAnalytics.get_dashboard_metrics.assert_called_with(days=30)

def test_get_dashboard_analytics_custom_days():
    """Tests the endpoint with a custom 'days' query parameter."""
    with patch("backend.analytics_api.Analytics") as MockAnalytics:
        MockAnalytics.get_dashboard_metrics.return_value = {"success": True, "data": {}}
        response = client.get("/analytics/dashboard?days=90")
        assert response.status_code == 200
        assert response.json()["success"] is True
        # Verify the underlying logic was called with the custom value
        MockAnalytics.get_dashboard_metrics.assert_called_with(days=90)