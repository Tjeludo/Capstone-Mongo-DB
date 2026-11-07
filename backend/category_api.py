from fastapi import APIRouter, UploadFile, File, Response
from fastapi.responses import JSONResponse
from pydantic import BaseModel
from .category import Category

# Pydantic models for request body validation.
class AddCategory(BaseModel):
    name: str
    description: str

class UpdateCategory(BaseModel):
    id: str
    name: str
    description: str

router = APIRouter()

@router.get("/categories")
async def get_categories():
    """API endpoint to fetch all categories."""
    result = Category.view_categories()
    return JSONResponse(content=result)

@router.get("/categories/view")
async def get_category_products(id: str):
    """API endpoint to view products in a specific category."""
    result = Category.view_category_products(id)
    return JSONResponse(content=result)

@router.post("/categories/add")
async def add_category(data: AddCategory):
    """API endpoint to create a new category."""
    result = Category(data.name, data.description).add_category()
    return JSONResponse(content=result)

@router.post("/categories/update")
async def update_category(data: UpdateCategory):
    """API endpoint to update an existing category."""
    result = Category.update_category(data.id, data.name, data.description)
    return JSONResponse(content=result)

@router.get("/categories/download")
def download_categories_csv():
    """API endpoint to download all categories as a CSV file."""
    try:
        csv_content = Category.download_category_csv()
        if csv_content:
            # If data exists, return it as a downloadable CSV file.
            return Response(
                content=csv_content,
                media_type="text/csv",
                headers={"Content-Disposition": 'attachment; filename="categories_export.csv"'}
            )
        # If no data, return a user-friendly message.
        return JSONResponse(
            content={"success": False, "message": "No categories found to export.", "data": ""},
            status_code=404
        )
    except Exception:
        # TODO: Log the actual error.
        return JSONResponse(
            content={"success": False, "message": "Download failed due to a server error.", "data": ""},
            status_code=500
        )

@router.post("/categories/upload")
async def upload_categories_csv(file: UploadFile = File(...)):
    """API endpoint to upload a CSV file for bulk category import."""
    try:
        # Basic validation for file type.
        if not file.filename.endswith('.csv'):
            return JSONResponse(
                content={"success": False, "message": "Upload failed. Please select a CSV file.", "data": ""},
                status_code=400)

        result = await Category.upload_categories_csv(file)
        return JSONResponse(content=result)
    except Exception:
        # TODO: Log the actual error.
        return JSONResponse(
            content={"success": False, "message": "Upload failed due to a server error.", "data": ""},
            status_code=500)