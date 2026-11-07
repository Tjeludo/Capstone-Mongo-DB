from fastapi import APIRouter, UploadFile, File
from fastapi import Response
from fastapi.responses import JSONResponse
from pydantic import BaseModel
from .roles import Role

class ViewRole(BaseModel):
    role_code: int

# UPDATED: Removed description
class AddRole(BaseModel):
    name: str
    abbrev: str
    status: str = "active"

# UPDATED: Removed description
class UpdateRole(BaseModel):
    role_code: int
    name: str
    abbrev: str
    status: str

class SearchRole(BaseModel):
    query: int

# --- (NEW models for permission endpoints) ---
class ViewRolePermissions(BaseModel):
    role_code: int
router = APIRouter()

# --- (get_roles, get_role are unchanged) ---
@router.get("/roles")
async def get_roles():
    result = Role.view_roles()
    return JSONResponse(content=result)
@router.get("/roles/view")
async def get_role(data: ViewRole):
    result = Role.search_role(data.role_code)
    return JSONResponse(content=result)

# Create/add
@router.post("/roles/add")
async def add_role(data: AddRole):
    # UPDATED: Call to Role constructor no longer passes description
    result = Role(data.name, data.abbrev, data.status).add_role()
    return result

# Edit/update
@router.post("/roles/update")
async def update_role(data: UpdateRole):
    # UPDATED: Call to update_role no longer passes description
    result = Role.update_role(data.role_code, data.name, data.abbrev, data.status)
    return result

# --- (NEW endpoints for viewing permissions) ---
@router.post("/roles/permissions")
async def get_role_permissions(data: ViewRolePermissions):
    result = Role.view_role_permissions(data.role_code)
    return JSONResponse(content=result)

# --- (delete_role, search_role, download, and upload endpoints are unchanged) ---
@router.delete("/roles/{role_code}")
async def delete_role(role_code: int):
    result = Role.delete_role(role_code)
    return JSONResponse(content=result)
@router.post("/roles/search")
def search_role(data: SearchRole):
    result = Role.search_role(data.query)
    return result
@router.get("/roles/download")
def download_roles_csv():
    try:
        csv_content = Role.download_roles_csv()
        if csv_content:
            return Response(
                content=csv_content,
                media_type="text/csv",
                headers={"Content-Disposition": 'attachment; filename="roles_export.csv"'})
        return Response(
            content="No roles found to export.",
            media_type="text/plain",
            status_code=404)
    except Exception as err:
        return Response(content=f"Download failed.", media_type="text/plain", status_code=500)
@router.post("/roles/upload")
async def upload_roles_csv(file: UploadFile = File(...)):
    try:
        if not file.filename.endswith('.csv'):
            return JSONResponse(
                content={"success": False, "message": "Please upload a CSV file.", "data": ""})
        result = await Role.upload_roles_csv(file)
        return JSONResponse(content=result)
    except Exception as e:
        return JSONResponse(content={"success": False, "message": f"Upload failed.", "data": ""})