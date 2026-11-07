# backend/message_request_api.py

from fastapi import APIRouter, Depends, HTTPException
from fastapi.responses import JSONResponse
from pydantic import BaseModel
from typing import List
from .message_request import MessageRequest

router = APIRouter()


class MessageRequestCreate(BaseModel):
    title: str
    body: str
    requester_id: str


class ViewMyRequests(BaseModel):
    staff_id: str


class AdminAction(BaseModel):
    staff_id: str


# --- Endpoints for Managers ---

@router.post("/requests")
async def create_request(request: MessageRequestCreate):
    """Endpoint for managers to create a new message request."""
    new_request = MessageRequest(
        title=request.title,
        body=request.body,
        requester_id=request.requester_id
    )
    result = new_request.save()
    return JSONResponse(content=result)


@router.post("/requests/my-requests")
async def get_my_requests(request: ViewMyRequests):
    """Endpoint for managers to view their own requests."""
    result = MessageRequest.find_by_requester(request.staff_id)
    return JSONResponse(content=result)


# --- Endpoints for Admins ---

@router.get("/requests")
async def get_all_requests():
    """Endpoint for admins to view all requests."""
    result = MessageRequest.find_all()
    return JSONResponse(content=result)

@router.put("/requests/{request_id}/approve")
async def approve_request(request_id: str, action: AdminAction):
    """Endpoint for admins to approve a request."""
    result = MessageRequest.update_status(request_id, "approved", action.staff_id)
    return JSONResponse(content=result)


@router.put("/requests/{request_id}/decline")
async def decline_request(request_id: str, action: AdminAction):
    """Endpoint for admins to decline a request."""
    result = MessageRequest.update_status(request_id, "declined", action.staff_id)
    return JSONResponse(content=result)