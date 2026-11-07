# app/routers/home_api.py
from fastapi import APIRouter
from fastapi.responses import JSONResponse
from .home import Home

router = APIRouter()

@router.get("/home/dashboard")
def get_dashboard_data():
    """
    Endpoint to get all the required data for the home page dashboard.
    """
    result = Home.get_dashboard_metrics()
    return JSONResponse(content=result)
