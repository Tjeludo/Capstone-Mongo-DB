from fastapi import APIRouter, UploadFile, File
from fastapi import Response
from fastapi.responses import JSONResponse
from pydantic import BaseModel
from typing import List, Dict
from .order import Order

# Pydantic models define the expected structure of incoming request data.

class ViewOrder(BaseModel):
    id: str

class ConfirmNewOrder(BaseModel):
    supplier_id: str
    products: List[Dict]
    total_cost: float
    ordered_by: str

class CancelOrder(BaseModel):
    id: str

class UpdateOrderStatus(BaseModel):
    id: str
    status: str

class SearchOrder(BaseModel):
    query: str

router = APIRouter()

@router.get("/orders")
async def get_orders():
    """Endpoint to fetch all orders."""
    result = Order.view_orders()
    return JSONResponse(content=result)

@router.get("/orders/deliveries/upcoming")
async def get_upcoming_deliveries():
    """Endpoint to get orders with future delivery dates."""
    result = Order.get_upcoming_deliveries()
    return JSONResponse(content=result)

@router.get("/orders/view/{order_id}")
async def get_order_products(order_id: str):
    """Endpoint to view the products within a specific order."""
    result = Order.view_order_products(order_id)
    return JSONResponse(content=result)

@router.post("/orders/confirm")
async def confirm_order(data: ConfirmNewOrder):
    """Endpoint to create and confirm a new order."""
    order = Order(supplier_id=data.supplier_id, staff_id=data.ordered_by)
    # Populate the order object with data from the request.
    order.products = data.products
    order.total_cost = data.total_cost

    result = order.confirm_order()
    return JSONResponse(content=result)

@router.post("/orders/cancel")
async def cancel_order(data: CancelOrder):
    """Endpoint to cancel an existing order."""
    result = Order.cancel_order(data.id)
    return JSONResponse(content=result)

@router.post("/orders/status")
async def update_order_status(data: UpdateOrderStatus):
    """Endpoint to update the status of an order."""
    result = Order.update_order_status(data.id, data.status)
    return JSONResponse(content=result)

@router.post("/orders/search")
def search_order(data: SearchOrder):
    """Endpoint to search for a single order by its ID."""
    result = Order.search_order(data.query)
    return JSONResponse(content=result)

@router.get("/orders/download")
def download_orders_csv():
    """Endpoint to download all orders as a CSV file."""
    try:
        csv_content = Order.download_orders_csv()
        if csv_content:
            # If there's content, return it as a downloadable CSV file.
            return Response(
                content=csv_content,
                media_type="text/csv",
                headers={"Content-Disposition": 'attachment; filename="orders_export.csv"'})
        # If no orders were found, return a 404 Not Found status.
        return Response(
            content="No orders found to export.",
            media_type="text/plain",
            status_code=404)
    except Exception as err:
        # log err
        return Response(content=f"Download failed.", media_type="text/plain", status_code=500)

@router.post("/orders/upload")
async def upload_orders_csv(file: UploadFile = File(...)):
    """Endpoint to upload a CSV file of orders."""
    try:
        # Basic file validation.
        if not file.filename.endswith('.csv'):
            return JSONResponse(
                content={"success": False, "message": "Please upload a CSV file.", "data": ""})

        # Delegate the actual upload logic to the Order class.
        result = await Order.upload_orders_csv(file)
        return JSONResponse(content=result)
    except Exception as e:
        # log err
        return JSONResponse(content={"success": False, "message": f"Upload failed.", "data": ""})