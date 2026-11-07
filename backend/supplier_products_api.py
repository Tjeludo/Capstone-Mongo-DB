from fastapi import APIRouter, UploadFile, File, Response
from fastapi.responses import JSONResponse
from pydantic import BaseModel
from .supplier_products import SupplierItem


class AddSupplierProduct(BaseModel):
    supplier_id: str
    product_code: str
    product_name: str
    price: float


class UpdateSupplierProduct(BaseModel):
    supplier_id: str
    product_code: str
    price: float


class DeleteSupplierProduct(BaseModel):
    supplier_id: str
    product_code: str


class ViewSupplierProducts(BaseModel):
    supplier_id: str


class SearchSupplierProducts(BaseModel):
    supplier_id: str
    product_code: str


class DownloadSupplierProducts(BaseModel):
    supplier_id: str


router = APIRouter()

# Endpoint for ALL products from ALL suppliers
@router.get("/supplier/products")
async def get_all_supplier_products():
    result = SupplierItem.view_supplier_products()
    return JSONResponse(content=result)

# Endpoint for a SINGLE supplier's products
@router.get("/supplier/products/{supplier_id}")
async def get_products_for_supplier(supplier_id: str):
    result = SupplierItem.view_products_for_supplier(supplier_id=supplier_id)
    return JSONResponse(content=result)

# Update supplier product price
@router.post("/supplier/products/update")
async def update_supplier_product(data: UpdateSupplierProduct):
    result = SupplierItem.update_supplier_price(data.supplier_id, data.product_code, data.price)
    return JSONResponse(content=result)

# Add supplier product
@router.post("/supplier/products/add")
async def add_supplier_product(data: AddSupplierProduct):
    supplier_item = SupplierItem(data.supplier_id,data.product_name, data.product_code, data.price)
    result = supplier_item.add_product()
    return JSONResponse(content=result)

# Delete supplier product
@router.post("/supplier/products/delete")
async def delete_supplier_product(data: DeleteSupplierProduct):
    result = SupplierItem.delete_supplier_product(data.supplier_id, data.product_code)
    return JSONResponse(content=result)


# Search supplier products
@router.post("/supplier/products/search")
async def search_supplier_products(data: SearchSupplierProducts):
    result = SupplierItem.search_supplier_products(data.supplier_id, data.product_code)
    return JSONResponse(content=result)


# NEW: Download CSV template for a specific supplier
@router.get("/supplier/products/download/{supplier_id}")
def download_supplier_products_csv(supplier_id: str):
    try:
        csv_content = SupplierItem.download_products_for_supplier_csv(supplier_id)
        if csv_content:
            return Response(
                content=csv_content,
                media_type="text/csv",
                headers={"Content-Disposition": f'attachment; filename="price_update_{supplier_id}.csv"'}
            )
        return Response(content="No products found for this supplier.", status_code=404)
    except Exception as err:
        return Response(content="Download failed.", status_code=500)


# NEW: Upload CSV to update prices for a specific supplier
@router.post("/supplier/products/upload-prices/{supplier_id}")
async def upload_supplier_prices_csv(supplier_id: str, file: UploadFile = File(...)):
    if not file.filename.endswith('.csv'):
        return JSONResponse(content={"success": False, "message": "Please upload a CSV file."})

    result = await SupplierItem.upload_prices_for_supplier_csv(supplier_id, file)
    return JSONResponse(content=result)