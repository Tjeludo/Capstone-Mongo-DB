from fastapi import APIRouter
from fastapi.responses import JSONResponse
from .analytics import Analytics

router = APIRouter()

@router.get("/analytics/dashboard")
def get_dashboard_analytics(days: int = 30):
    """
    Endpoint to get dashboard analytics data.
    Accepts an optional 'days' query parameter to specify the time period.
    """
    result = Analytics.get_dashboard_metrics(days=days)
    return JSONResponse(content=result)

