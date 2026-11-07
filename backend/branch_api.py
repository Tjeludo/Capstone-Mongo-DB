from fastapi import APIRouter, UploadFile, File
from fastapi import Response
from fastapi.responses import JSONResponse
from pydantic import BaseModel
from .ash_branch import Branch

class ViewBranch(BaseModel):
    id: str

class AddBranch(BaseModel):
    manager: str
    city: str
    email: str
    phone: str
    address: str

class UpdateBranch(BaseModel):
    branch_code: str  # Changed from id to branch_code
    manager: str
    city: str
    email: str
    phone: str
    address: str

class SearchBranch(BaseModel):
    query: str

router = APIRouter()

# view branches
@router.get("/branches")
async def get_branches():
    result = Branch.view_branches()
    return JSONResponse(content=result)

# view single branch
@router.get("/branches/view")
async def get_branch(data: ViewBranch):
    result = Branch.search_branch(data.id)
    return JSONResponse(content=result)

# create/add
@router.post("/branches/add")
async def add_branch(data: AddBranch):
    result = Branch(data.manager, data.city, data.email, data.phone, data.address).add_branch()
    return result

# edit/update
@router.post("/branches/update")
async def update_branch(data: UpdateBranch):
    result = Branch.update_branch(data.branch_code, data.manager, data.city, data.email, data.phone, data.address)
    return result

# delete
@router.delete("/branches/{branch_id}")
async def delete_branch(branch_id: str):
    result = Branch.delete_branch(branch_id)
    return JSONResponse(content=result)

# search
@router.post("/branches/search")
def search_branch(data: SearchBranch):
    result = Branch.search_branch(data.query)
    return result

# download
@router.get("/branches/download")
def download_branches_csv():
    try:
        csv_content = Branch.download_branch_csv()
        # Branches found
        if csv_content:
            return Response(
                content=csv_content,
                media_type="text/csv",
                headers={"Content-Disposition": 'attachment; filename="branches_export.csv"'})
        # No branches found
        return Response(
            content="No branches found to export.",
            media_type="text/plain",
            status_code=404)
    except Exception as err:
        # log err
        return Response(content=f"Download failed.", media_type="text/plain", status_code=500)

# upload
@router.post("/branches/upload")
async def upload_branches_csv(file: UploadFile = File(...)):
    try:
        # Validate file type
        if not file.filename.endswith('.csv'):
            return JSONResponse(
                content={"success": False, "message": "Please upload a CSV file.", "data": ""})

        # Call the upload method from Branch class
        result = await Branch.upload_branches_csv(file)
        return JSONResponse(content=result)

    except Exception as e:
        # log err
        return JSONResponse(content={"success": False, "message": f"Upload failed.", "data": ""})