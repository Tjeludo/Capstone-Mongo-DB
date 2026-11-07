from fastapi import APIRouter, UploadFile, File, Response
from fastapi.responses import JSONResponse
from pydantic import BaseModel
from .supplier import Supplier

# Pydantic models define the expected shape of request bodies.
# This helps with validation and API documentation.
class ViewSupplier(BaseModel):
    id: str

class AddSupplier(BaseModel):
    name: str
    email: str
    phone_number: str
    address: str
    sales_rep: str

class UpdateSupplier(BaseModel):
    id: str
    name: str
    email: str
    phone_number: str
    address: str
    sales_rep: str

class SearchSupplier(BaseModel):
    query: str

router = APIRouter()

@router.get("/suppliers")
async def get_suppliers():
    """API endpoint to retrieve a list of all suppliers."""
    result = Supplier.view_suppliers()
    return JSONResponse(content=result)

@router.get("/suppliers/{supplier_id}/items")
async def get_supplier_products(supplier_id: str):
    """API endpoint to view all products for a specific supplier."""
    result = Supplier.view_supplier_products(supplier_id)
    return JSONResponse(content=result)

@router.post("/suppliers/add")
async def add_supplier(data: AddSupplier):
    """API endpoint to create a new supplier."""
    supplier_instance = Supplier(data.name, data.email, data.phone_number, data.address, data.sales_rep)
    result = supplier_instance.add_supplier()
    return JSONResponse(content=result)

@router.post("/suppliers/update")
async def update_supplier(data: UpdateSupplier):
    """API endpoint to update an existing supplier's details."""
    result = Supplier.update_supplier(data.id, data.name, data.email, data.phone_number, data.address, data.sales_rep)
    return JSONResponse(content=result)

@router.delete("/suppliers/{supplier_id}")
async def delete_supplier(supplier_id: str):
    """API endpoint to delete a supplier by their ID."""
    result = Supplier.delete_supplier(supplier_id)
    return JSONResponse(content=result)

@router.post("/suppliers/search")
def search_supplier(data: SearchSupplier):
    """API endpoint to search for a supplier by their ID."""
    result = Supplier.search_supplier(data.query)
    return JSONResponse(content=result)

@router.get("/suppliers/download")
def download_suppliers_csv():
    """API endpoint to download all supplier data as a CSV file."""
    try:
        csv_content = Supplier.download_supplier_csv()
        if csv_content:
            return Response(
                content=csv_content,
                media_type="text/csv",
                headers={"Content-Disposition": 'attachment; filename="suppliers_export.csv"'}
            )
        return Response(content="No suppliers found to export.", media_type="text/plain", status_code=404)
    except Exception:
        # TODO: Log this error.
        return Response(content="Download failed due to a server error.", media_type="text/plain", status_code=500)

@router.post("/suppliers/upload")
async def upload_suppliers_csv(file: UploadFile = File(...)):
    """API endpoint to upload a CSV file for bulk supplier creation."""
    try:
        if not file.filename.endswith('.csv'):
            return JSONResponse(content={"success": False, "message": "Invalid file type. Please upload a CSV file.", "data": ""})

        result = await Supplier.upload_suppliers_csv(file)
        return JSONResponse(content=result)
    except Exception:
        # TODO: Log this error.
        return JSONResponse(content={"success": False, "message": "Upload failed due to a server error.", "data": ""})