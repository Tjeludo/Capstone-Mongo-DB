from fastapi import APIRouter, UploadFile, File, Response
from fastapi.responses import JSONResponse
from pydantic import BaseModel
from .permissions import Permission


class ViewRolePermissions(BaseModel):
    role_name: str


# UPDATED: Removed perm_description
class AddPermission(BaseModel):
    section_name: str  # Changed from perm_name to reflect UI
    perm_status: str = "active"  # Default status


# UPDATED: Removed perm_description
class UpdatePermission(BaseModel):
    perm_id: str
    perm_name: str
    perm_status: str


class SearchPermission(BaseModel):
    perm_id: str


class AssignPermission(BaseModel):
    perm_id: str
    role_name: str


class RevokePermission(BaseModel):
    perm_id: str
    role_name: str


router = APIRouter()


# --- (get_permissions, get_updated_permissions, get_role_permissions are unchanged) ---
@router.get("/permissions")
async def get_permissions():
    result = Permission.view_permissions()
    return JSONResponse(content=result)


@router.get("/permissions/{staff_id}")
async def get_updated_permissions(staff_id: str):
    result = Permission.get_user_permissions(staff_id)
    return JSONResponse(content=result)


@router.post("/permissions/role")
async def get_role_permissions(data: ViewRolePermissions):
    result = Permission.view_role_permissions(data.role_name)
    return JSONResponse(content=result)


# create/add permission
@router.post("/permissions/add")
async def add_permission(data: AddPermission):
    # Logic to create multiple permissions based on a section name
    actions = ["view", "add", "edit", "delete"]
    section = data.section_name.lower().strip()

    # Basic validation for section name
    if not section or not section.replace('_', '').isalnum():
        return JSONResponse(
            content={"success": False, "message": "Invalid section name. Use letters, numbers, and underscores only.",
                     "data": ""})

    for action in actions:
        perm_name = f"{action}_{section}"
        # UPDATED: Call to constructor is simplified
        Permission(perm_name, data.perm_status).add_permission()

    return JSONResponse(
        content={"success": True, "message": f"Permissions for section '{section}' created successfully.", "data": ""})


# edit/update permission
@router.post("/permissions/update")
async def update_permission(data: UpdatePermission):
    # UPDATED: Function call is simplified
    result = Permission.update_permission(data.perm_id, data.perm_name, data.perm_status)
    return result


# --- (search_permission, assign, revoke, and file operations are unchanged) ---
@router.post("/permissions/search")
def search_permission(data: SearchPermission):
    result = Permission.search_permission(data.perm_id)
    return result


@router.post("/permissions/grant")
async def assign_permission(data: AssignPermission):
    result = Permission.grant_permission(data.perm_id, data.role_name)
    return JSONResponse(content=result)


@router.post("/permissions/revoke")
async def revoke_permission(data: RevokePermission):
    result = Permission.revoke_permission(data.perm_id, data.role_name)
    return JSONResponse(content=result)


@router.get("/permissions/download")
def download_permissions_csv():
    try:
        csv_content = Permission.download_permissions_csv()
        if csv_content:
            return Response(content=csv_content, media_type="text/csv",
                            headers={"Content-Disposition": 'attachment; filename="permissions_export.csv"'})
        return Response(content="No permissions found to export.", media_type="text/plain", status_code=404)
    except Exception as err:
        return Response(content=f"Download failed.", media_type="text/plain", status_code=500)


@router.post("/permissions/upload")
async def upload_permissions_csv(file: UploadFile = File(...)):
    try:
        if not file.filename.endswith('.csv'):
            return JSONResponse(content={"success": False, "message": "Please upload a CSV file.", "data": ""})
        result = await Permission.upload_permissions_csv(file)
        return JSONResponse(content=result)
    except Exception as e:
        return JSONResponse(content={"success": False, "message": f"Upload failed.", "data": ""})