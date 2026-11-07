from fastapi import APIRouter, UploadFile, File, Response
from fastapi.responses import JSONResponse
from pydantic import BaseModel
from .product import Product

class ViewCategoryProducts(BaseModel):
    category_code: str

class AddProduct(BaseModel):
    name: str
    category_code: str
    size: str
    colour: str
    price: float

class UpdateProduct(BaseModel):
    product_code: str
    name: str
    price: float

class SearchProduct(BaseModel):
    product_code: str

class SearchProductsByName(BaseModel):
    name: str

class DeleteProduct(BaseModel):
    product_code: str

router = APIRouter()

# View all products
@router.get("/products")
async def get_products():
    result = Product.view_products()
    return JSONResponse(content=result)

# View products by category
@router.post("/products/category")
async def get_products_by_category(data: ViewCategoryProducts):
    result = Product.get_products_by_category(data.category_code)
    return JSONResponse(content=result)

# Create/add product
@router.post("/products/add")
async def add_product(data: AddProduct):
    product = Product(data.name, data.category_code, data.size, data.colour, data.price)
    result = product.add_product()
    return JSONResponse(content=result)

# Update product
@router.post("/products/update")
async def update_product(data: UpdateProduct):
    result = Product.update_product(data.product_code, data.name, data.price)
    return JSONResponse(content=result)

# Search product by code
@router.post("/products/search")
async def search_product(data: SearchProduct):
    result = Product.search_product(data.product_code)
    return JSONResponse(content=result)

# Search products by name
@router.post("/products/search/name")
async def search_products_by_name(data: SearchProductsByName):
    result = Product.search_products_by_name(data.name)
    return JSONResponse(content=result)

# Delete product
@router.delete("/products/delete/{product_code}")
async def delete_product(product_code: str):
    result = Product.delete_product(product_code)
    return JSONResponse(content=result)

# Download products CSV
@router.get("/products/download")
def download_products_csv():
    try:
        csv_content = Product.download_products_csv()
        # Products found
        if csv_content:
            return Response(
                content=csv_content,
                media_type="text/csv",
                headers={"Content-Disposition": 'attachment; filename="products_export.csv"'}
            )
        # No products found
        return Response(
            content="No products found to export.",
            media_type="text/plain",
            status_code=404
        )
    except Exception as err:
        # log err
        return Response(
            content="Download failed.",
            media_type="text/plain",
            status_code=500)

# Upload products CSV
@router.post("/products/upload")
async def upload_products_csv(file: UploadFile = File(...)):
    try:
        # Validate file type
        if not file.filename.endswith('.csv'):
            return JSONResponse(
                content={"success": False, "message": "Please upload a CSV file.", "data": ""})

        # Call the upload method from Product class
        result = await Product.upload_products_csv(file)
        return JSONResponse(content=result)

    except Exception as e:
        # log err
        return JSONResponse(
            content={"success": False, "message": "Upload failed.", "data": ""})