from fastapi import APIRouter
from fastapi.responses import JSONResponse
from pydantic import BaseModel, EmailStr, SecretStr
from .user import User

router = APIRouter()

class CreateUser(BaseModel):
    staff_email: EmailStr
    staff_id: str
    password: SecretStr
    staff_name: str
    role: str

class SearchUser(BaseModel):
    staff_id: str

class DeleteUser(BaseModel):
    staff_id: str

class Login(BaseModel):
    staff_id: str
    password: SecretStr

class Verify2FA(BaseModel):
    staff_id: str
    otp_code: str

class ChangePassword(BaseModel):
    staff_id: str
    old_password: SecretStr
    new_password: SecretStr

class ResetPassword(BaseModel):
    staff_email: EmailStr

class UpdateUserStatus(BaseModel):
    staff_id: str
    status: str

# login
@router.post("/login")
async def login(credentials: Login):
    result = User.log_in(credentials.staff_id, credentials.password.get_secret_value())
    return result

# mfa
@router.post("/verify-2fa")
async def verify_2fa(verification: Verify2FA):
    result = User.verify_2fa(verification.staff_id, verification.otp_code)
    if result["success"]:
        return JSONResponse(content=result)
    return JSONResponse(content=result)

# change pass
@router.post("/users/change-password")
def change_password(credentials: ChangePassword):
    result = User.change_password(
        credentials.staff_id,
        credentials.old_password.get_secret_value(),
        credentials.new_password.get_secret_value())
    return JSONResponse(content=result)

# reset pass
@router.post("/reset-password")
def reset_password(req: ResetPassword):
    result = User.reset_password(req.staff_email)
    return JSONResponse(content=result)

# view users
@router.get("/users")
async def view_users():
    result = User.view_users()
    return JSONResponse(content=result)

# update user status
@router.post("/users/status")
async def update_user_status(data: UpdateUserStatus):
    result = User.update_user_status(data.staff_id, data.status)
    return JSONResponse(content=result)

# search user
@router.get("/users/search")
def search_user(staff_id: str):
    result = User.search_user(staff_id)
    if result["success"]:
        return JSONResponse(content=result)
    return JSONResponse(content=result)

# delete user
@router.delete("/users/delete")
def delete_user(staff_id: str):
    result = User.delete_user(staff_id)
    return JSONResponse(content=result)