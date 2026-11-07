from fastapi import FastAPI, HTTPException, Query, APIRouter
from pydantic import BaseModel, Field
from typing import List, Optional
from .delivery import Delivery, deliveryItem
from .StockTake import StockTake, StockTakeItem
from .connection import connect_MongoDB
import datetime

# Initialize the FastAPI application
router = APIRouter()

# Connect to the MongoDB collections
orders_table = connect_MongoDB("orders")
delivery_table = connect_MongoDB("deliveries")
inventory_table = connect_MongoDB("Inventory")
stock_take_table = connect_MongoDB("stock_take")
branches_table = connect_MongoDB("Branch")
suppliers_table = connect_MongoDB("suppliers") 

# --- Pydantic Models for Data Validation ---

class OrderItem(BaseModel):
    orderId: str

class DeliveryItem(BaseModel):
    itemId: str
    itemName: str
    quantityReceived: int = Field(..., gt=0, description="Quantity must be greater than zero.")
    comments: Optional[str] = None

class DeliveryData(BaseModel):
    deliveryId: Optional[str] = None
    supplierId: str
    supplierName: str
    orderId: str
    checkedBy: str
    comments: Optional[str] = None
    deliveryItems: List[DeliveryItem]
    missingItems: Optional[List[dict]] = None  # List of missing items if any

class StockTakeItemModel(BaseModel):
    itemID: str
    itemName: str
    previousQuantity: int
    quantityOnHand:  int = Field(..., ge=0)

class StockTakeModel(BaseModel):
    stockTakeNum: str
    branchId: str
    date: str
    items: List[StockTakeItemModel]

# --- API Endpoints ---

@router.post("/get_order_items")
async def get_order_items(order: OrderItem):
    try:
        order_data = orders_table.find_one({"order_id": order.orderId})
        if not order_data:
            raise HTTPException(status_code=404, detail="Order not found")

        #Check if the order is already fulfilled
        if order_data.get("status") == "Fully Fulfilled":
            raise HTTPException(status_code=409, detail="This order is already fully fulfilled.")
        if order_data.get("status") == "Cancelled":
            raise HTTPException(status_code=409, detail="This order has been cancelled and cannot be processed.")
        if order_data.get("status") == "Pending":
            raise HTTPException(status_code=409, detail="This order is still pending confirmation and cannot be processed.")
        if order_data.get("status") == "Confirmed":
            raise HTTPException(status_code=409, detail="This order is confirmed but not yet dispatched. Please wait until it is dispatched to process the delivery.")
        if order_data.get("status") == "In-Progress":
            raise HTTPException(status_code=409, detail="This order is currently in progress and cannot be processed for delivery.")
        
        # Fetch supplier details
        supplier_id = order_data.get("supplier_id")
        supplier_name = "Unknown Supplier"  # Default value in case supplier isn't found
        if supplier_id:
            # Find the matching supplier in the 'suppliers' collection
            supplier_doc = suppliers_table.find_one({"supplier_id": supplier_id})
            if supplier_doc:
                # Get the name from the supplier document
                supplier_name = supplier_doc.get("supplier_name", "Unknown Supplier")

        #  Calculate what has already been delivered
        all_deliveries = delivery_table.find({"order_id": order.orderId})
        total_delivered_map = {}
        for delivery_doc in all_deliveries:
            for item in delivery_doc.get("deliveryItems", []):
                item_id = item["item_id"]
                quantity = item.get("quantity", 0)
                total_delivered_map[item_id] = total_delivered_map.get(item_id, 0) + quantity
        
        #  Prepare the detailed item list for the frontend
        response_items = []
        order_products = order_data.get("products", [])
        
        for item in order_products:
            item_id = item["product_code"]
            total_required = item["quantity"]
            already_delivered = total_delivered_map.get(item_id, 0)
            missing_quantity = total_required - already_delivered
            
            # Only send items that are still missing
            if missing_quantity > 0:
                response_items.append({
                    "itemId": item_id,
                    "itemName": item["description"],
                    "totalRequired": total_required,
                    "alreadyDelivered": already_delivered,
                    "missingQuantity": missing_quantity,
                    "quantityReceived": 0, # Default for the input box
                    "comments": ""
                })

        # Create a new response object that includes supplier details and items
        response_data = {
            "supplierId": order_data.get("supplier_id"),
            "supplierName": supplier_name, # Ensure this field name matches your DB
            "items": response_items
        }
        return response_data
            
    except HTTPException as http_exc:
        raise http_exc # Re-raise HTTPException to preserve status code
    except Exception as e:
        print(f"Error fetching order items: {e}")
        raise HTTPException(status_code=500, detail="An internal server error occurred")
    
@router.get("/get_supplier_by_id")
async def get_supplier_by_id(supplier_id: str = Query(..., description="The ID of the supplier to fetch.")):
    """
    Finds a supplier by their ID and returns their details.
    """
    try:
        
        supplier = suppliers_table.find_one({"supplier_id": supplier_id})

        if not supplier:
            
            raise HTTPException(status_code=404, detail="Supplier ID not found")


        return {
            "supplierId": supplier.get("supplier_id"),
            "supplierName": supplier.get("supplier_name")
        }

    except Exception as e:
        print(f"Error fetching supplier: {e}")
        raise HTTPException(status_code=500, detail="An internal server error occurred")

@router.post("/save_delivery")
async def save_delivery(delivery_data: DeliveryData):
    try:
        
        deliveryNum = delivery_data.deliveryId or Delivery.get_del_number()

        deliveryItemsList = [
            deliveryItem(item.itemId, item.itemName, item.quantityReceived, item.comments)
            for item in delivery_data.deliveryItems
        ]

        date = datetime.datetime.now().strftime("%Y-%m-%d")

        new_delivery = Delivery(
            deliveryNum=deliveryNum,
            supplier_id=delivery_data.supplierId,
            supplierName=delivery_data.supplierName,
            order_id=delivery_data.orderId,
            date=date,
            checkedByWho=delivery_data.checkedBy,
            deliveryItems=deliveryItemsList,
            comments=delivery_data.comments
        )

        
        new_delivery.add_delivery_item()
        new_delivery.update_order_status()
        new_delivery.update_main_branch_inventory()

        return {"message": "Delivery saved successfully", "deliveryNum": deliveryNum}

    except Exception as e:
        print(f"Error saving delivery: {e}")
        raise HTTPException(status_code=500, detail="An internal error occurred while saving the delivery")


@router.get("/generate_delivery_number")
async def generate_delivery_number():
    try:
        next_number = Delivery.get_del_number()
        return {"deliveryNum": next_number}
    except Exception as e:
        print(f"Error generating delivery number: {e}")
        raise HTTPException(status_code=500, detail="Could not generate delivery number")
    
@router.get("/get_deliveries")
async def get_deliveries():
    try:
        deliveries = delivery_table.find().sort("date", -1)
        result = []
        for d in deliveries:
            result.append({
                "Date": d.get("date", ""),
                "DeliveryId": d.get("delivery_num", ""),
                "SupplierId": d.get("supplier name", ""),
                "SupplierName": d.get("supplier name", ""),
                "Order_id": d.get("order_id", ""),
                "CheckedBy": d.get("checkedByWho", ""),
                "DeliveryItems": d.get("deliveryItems", []),
                "MissingItems": d.get("missing items", []),
                "Comments": d.get("comments", "")
            })
        return result
    except Exception as e:
        print(f"Error fetching deliveries: {e}")
        raise HTTPException(status_code=500, detail="Could not fetch deliveries")

@router.get("/get_stocktakes")
async def get_stocktakes():
    try:
        stocktakes = list(stock_take_table.find())
        result = []
        for st in stocktakes:
            result.append({
                "stockTakeNum": st.get("stockTakeNum"),
                "branchID": st.get("branchID"),
                "date": st.get("date"),
                "items": st.get("items", [])
            })
        return result
    except Exception as e:
        print(f"Error fetching stock takes: {e}")
        raise HTTPException(status_code=500, detail="Could not fetch stock takes")

@router.post("/save_stocktake")
async def save_stocktake(stocktake: StockTakeModel):
    try:
        items = [StockTakeItem(item.itemID, item.itemName,item.previousQuantity, item.quantityOnHand) for item in stocktake.items]
        new_stock_take = StockTake(stocktake.stockTakeNum, stocktake.branchId, stocktake.date, items)

        new_stock_take.add_stock_take()
        new_stock_take.update_stock_levels()

        return {"message": "Stock take saved successfully", "stockTakeNum": stocktake.stockTakeNum}
    except Exception as e:
        print(f"Error saving stock take: {e}")
        raise HTTPException(status_code=500, detail="An internal error occurred while saving the stock take")

@router.get("/generate_stocktake_number")
async def generate_stocktake_number():
    try:
        next_num = StockTake.get_next_stock_take_number()
        return {"stockTakeNum": next_num}
    except Exception as e:
        print(f"Error generating stock take number: {e}")
        raise HTTPException(status_code=500, detail="Could not generate stock take number")

@router.get("/get_inventory_by_branch")
async def get_inventory_by_branch(branchId: str = Query(..., description="Branch ID to filter inventory")):
    try:
        inventory_items = inventory_table.find({"branchID": branchId})
        result = []
        for item in inventory_items:
            result.append({
                "itemId": item.get("product_code", ""),
                "itemName": item.get("product_name", ""),
                "quantityOnHand": item.get("quantity", 0)
            })
        return result
    except Exception as e:
        print(f"Error fetching inventory: {e}")
        raise HTTPException(status_code=500, detail="Could not fetch inventory for branch")

@router.get("/get_branches")
async def get_branches():
    try:
        branches = list(branches_table.find())
        result = []
        for b in branches:
            result.append({
                "branchId": b.get("branchID", ""),
                "branchName": b.get("location", "")
            })
        return result
    except Exception as e:
        print(f"Error fetching branches: {e}")
        raise HTTPException(status_code=500, detail="Could not fetch branches")