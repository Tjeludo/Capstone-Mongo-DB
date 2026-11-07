# Backend API
from fastapi import APIRouter, UploadFile, File
from fastapi import  Response, FastAPI
from fastapi.responses import JSONResponse
from pydantic import BaseModel
from .inventory_items import Inventory
from .branch import Branch
from .Transfers import Transfer, transfer
from .distribution import Distrubition, DistributionItem, checkAvailability, does_product_exist
from typing import List


class addBranch(BaseModel):
    location: str
    contactDetails: str
    manager: str

class makeTransfer(BaseModel):
    itemCode: str
    fromBranch: str
    toBranch: str
    quantity: int

class BranchDistribution(BaseModel):
    branchID: str
    quantity: int

class MakeDistribution(BaseModel):
    itemCode: str
    distributions: List[BranchDistribution]

class DistributionDetails(BaseModel):
    distributionID:  str

class UpdateBranch(BaseModel):
    branchID: str
    location: str
    contactDetails: str
    manager: str


router = APIRouter()

@router.get("/inventory")
async def getInventory():
    result = Inventory.getInventory()
    return JSONResponse(content = result)

@router.get("/branch")
async def getBranch():
    result = Branch.getBranches()
    return JSONResponse(content = result)

@router.get("/transfer")
async def getTransfers():
    result = Transfer.getTransfers()
    return JSONResponse(content = result)

@router.get("/distributions")
async def getDistributions():
    result = Distrubition.getDistributions()
    return JSONResponse(content = result)

@router.post("/addBranch")
async def addBranch(data: addBranch):
    result = Branch(data.location, data.contactDetails, data.manager).addBranch()
    return result

@router.get("/getproductcode")
async def getProductname():
    result = Inventory.getInventoryItems()
    return JSONResponse(content = result)

@router.get("/getbranchID")
async def getBranchID():
    result = Branch.getBranchCode()
    return JSONResponse(content = result)

@router.post("/addTransfer")
async def addTransfer(data: makeTransfer):
    result = transfer(data.itemCode, data.fromBranch, data.toBranch, data.quantity)
    return result


@router.post("/makeDistribution")
async def makeDistribution(data: MakeDistribution):
    
    if not does_product_exist(data.itemCode):
        return JSONResponse(
            status_code=404, # 404 Not Found is a good status code here
            content={"success": False, "message": f"Product '{data.itemCode}' does not exist."}
        )
    # Calculate total quantity and validate input
    total_quantity = 0
    for dist in data.distributions:
        if dist.quantity < 0:
            return JSONResponse(
                status_code=400,
                content={"success": False, "message": "Distribution quantities cannot be negative."}
            )
        total_quantity += dist.quantity

    # Call the availability check and get BOTH the status and message
    is_available, message = checkAvailability(data.itemCode, total_quantity)

    # Check the status and use the message directly
    if not is_available:
        return JSONResponse(
            status_code=400,
            content={"success": False, "message": message}
        )

    # If available, perform the distribution
    new_distribution = Distrubition()
    for dist_item in data.distributions:
        new_distribution.distribute(data.itemCode, dist_item.branchID, dist_item.quantity)
        item_to_record = DistributionItem(data.itemCode, dist_item.branchID, dist_item.quantity)
        new_distribution.addItem(item_to_record)

    new_distribution.recordDistributions()

    return {"success": True, "message": "Distribution completed successfully."}

@router.get("/getproductcode")
async def getProductname():
    # This is the only line that needs to change
    result = Distrubition.getProductCodes()
    return JSONResponse(content = result)

@router.get("/distributionDetails")
async def getDistributionDetails(distributionID: str):
    result = DistributionItem.getDistributionDetails(distributionID)
    return JSONResponse(content = result)

@router.put("/updateBranch")
async def update_branch(data: UpdateBranch):
    update_data = {
        "location": data.location,
        "contactDetails": data.contactDetails,
        "manager": data.manager
    }
    result = Branch.update_branch(data.branchID, update_data)
    
    if result["success"]:
        return JSONResponse(content=result)
    else:
        return JSONResponse(status_code=404, content=result)

# ADDED: This part is crucial for making the file testable with TestClient
app = FastAPI()
app.include_router(router)

