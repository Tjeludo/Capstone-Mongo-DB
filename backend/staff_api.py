from fastapi import APIRouter, UploadFile, File, Response
from fastapi.responses import JSONResponse
from pydantic import BaseModel
from .staff import Staff

# These Pydantic models define the expected JSON structure for API requests.
# This helps with data validation and auto-generating the API documentation.
class AddStaff(BaseModel):
    branch_code: str
    role: str
    firstname: str
    lastname: str
    email: str
    date_of_birth: str
    gender: str
    phone_number: str
    address: str
    status: str
    start_date: str # Added to match frontend

class UpdateStaff(BaseModel):
    staff_id: str
    firstname: str
    lastname: str
    gender: str
    phone_number: str
    address: str
    status: str

class TransferStaff(BaseModel):
    id: str
    branch_code: str

class PromoteStaff(BaseModel):
    id: str
    role: str

class UpdateStaffStatus(BaseModel):
    id: str
    status: str

# This creates a new router, which I'll include in my main FastAPI app.
router = APIRouter()

@router.get("/staff")
async def get_staff():
    """API endpoint to get a list of all staff members."""
    result = Staff.view_staff()
    return JSONResponse(content=result)

@router.post("/staff/add")
async def add_staff(data: AddStaff):
    """API endpoint to create a new staff member."""
    # The data received is unpacked and used to create a new Staff instance.
    staff_instance = Staff(
        data.branch_code, data.role, data.firstname, data.lastname,
        data.email, data.date_of_birth, data.gender, data.phone_number,
        data.address, data.status, data.start_date
    )
    result = staff_instance.add_staff()
    return JSONResponse(content=result)

@router.post("/staff/update")
async def update_staff(data: UpdateStaff):
    """API endpoint to update an existing staff member's details."""
    result = Staff.update_staff(
        data.staff_id, data.firstname, data.lastname, data.gender,
        data.phone_number, data.address, data.status
    )
    return JSONResponse(content=result)

@router.delete("/staff/{staff_id}")
async def delete_staff(staff_id: str):
    """API endpoint to delete a staff member by their ID."""
    result = Staff.delete_staff(staff_id)
    return JSONResponse(content=result)

@router.post("/staff/transfer")
async def transfer_staff(data: TransferStaff):
    """API endpoint to transfer a staff member to a new branch."""
    result = Staff.transfer_staff(data.id, data.branch_code)
    return JSONResponse(content=result)

@router.post("/staff/promote")
async def promote_staff(data: PromoteStaff):
    """API endpoint to promote a staff member to a new role."""
    result = Staff.promote_staff(data.id, data.role)
    return JSONResponse(content=result)

# Update staff status
@router.post("/staff/status")
async def update_staff_status(data: UpdateStaffStatus):
    result = Staff.update_staff_status(data.id, data.status)
    return JSONResponse(content=result)

# Download staff CSV
@router.get("/staff/download")
def download_staff_csv():
    try:
        csv_content = Staff.download_staff_csv()
        # Staff found
        if csv_content:
            return Response(
                content=csv_content,
                media_type="text/csv",
                headers={"Content-Disposition": 'attachment; filename="staff_export.csv"'})
        # No staff found
        return Response(
            content="No staff found to export.",
            media_type="text/plain",
            status_code=404)
    except Exception as err:
        # log err
        return Response(content=f"Download failed.", media_type="text/plain", status_code=500)

# Upload staff CSV
@router.post("/staff/upload")
async def upload_staff_csv(file: UploadFile = File(...)):
    try:
        # Validate file type
        if not file.filename.endswith('.csv'):
            return JSONResponse(
                content={"success": False, "message": "Please upload a CSV file.", "data": ""})

        # Call the upload method from Staff class
        result = await Staff.upload_staff_csv(file)
        return JSONResponse(content=result)

    except Exception as e:
        # log err
        return JSONResponse(content={"success": False, "message": f"Upload failed.", "data": ""})