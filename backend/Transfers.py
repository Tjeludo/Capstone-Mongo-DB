# In Transfers.py

from .conn import connect
from datetime import datetime
from .distribution import get_product_name

# Database connections
client = connect()
database = client["sample_mflix"]
collection = database["Transfers"]
collectionInventory = database["Inventory"]
# ADDED: Connection to the Branch collection for validation
collectionBranch = database["Branch"]

def makeTransID():
    """Makes a unique transfer ID based on the number of transfers recorded """
    currentCount = collection.count_documents({})
    ID = "T00" + str(currentCount + 1)
    return ID

class Transfer:
    def __init__(self, itemCode, fromBranch, toBranch, quantity, transferID=None, date=None, product_name=None):
        self.itemCode = itemCode
        self.fromBranch = fromBranch
        self.toBranch = toBranch
        self.quantity = quantity
        self.transferID = transferID if transferID is not None else makeTransID()
        self.date = date if date is not None else datetime.now().strftime("%d-%m-%Y %H:%M")
        self.product_name = product_name if product_name is not None else get_product_name(self.itemCode)

    def execute_transfer(self):
        """Mkaes a transfer basically removes from one branch and add to another"""
        # Decrease quantity from the source branch
        collectionInventory.update_one(
            {"product_code": self.itemCode, "branchID": self.fromBranch},
            {"$inc": {"quantity": -self.quantity}}
        )
        # Increase quantity in the destination branch (or create it if it doesn't exist)
        collectionInventory.update_one(
            {"product_code": self.itemCode, "branchID": self.toBranch},
            {"$inc": {"quantity": self.quantity}},
            upsert=True
        )

    def record_transfer(self):
        """Records the inventory transaction """
        transfer_doc = {
            "transferID": self.transferID,
            "date": self.date,
            "product_code": self.itemCode,
            "product_name": self.product_name,
            "from_branch": self.fromBranch,
            "to_branch": self.toBranch,
            "quantity": self.quantity
        }
        collection.insert_one(transfer_doc)
        return {"success": True, "message": f"Transfer {self.transferID} recorded successfully."}

    @staticmethod
    def getTransfers():
        """Gets all transactions of transfers"""
        transfers_list = []
        for doc in collection.find({}, {"_id": 0}): # Exclude the _id field directly
            transfers_list.append(doc)
        return {"success": True, "message": "Transfers retrieved.", "data": transfers_list}


def transfer(itemCode, fromBranch, toBranch, quantity):
    """Validates and performs a stock transfer."""
   
    if fromBranch == toBranch:
        return {"success": False, "message": "Source and destination branches cannot be the same."}

    if quantity <= 0:
        return {"success": False, "message": "Transfer quantity must be greater than zero."}

    to_branch_exists = collectionBranch.find_one({"branchID": toBranch})
    if not to_branch_exists:
        return {"success": False, "message": f"Destination branch '{toBranch}' is not a valid branch."}
 
    from_branch_exists = collectionBranch.find_one({"branchID": fromBranch})
    if not from_branch_exists:
        return {"success": False, "message": f"Source branch '{fromBranch}' is not a valid branch."}

    source_inventory = collectionInventory.find_one(
        {"product_code": itemCode, "branchID": fromBranch}
    )

    if not source_inventory:
        return {
            "success": False,
            "message": f"Item '{itemCode}' not found in source branch '{fromBranch}'."
        }

    if source_inventory.get("quantity", 0) < quantity:
        available_qty = source_inventory.get("quantity", 0)
        return {
            "success": False,
            "message": f"Insufficient stock in {fromBranch}. Available: {available_qty}, Requested: {quantity}."
        }

    new_transfer = Transfer(itemCode, fromBranch, toBranch, quantity)
    new_transfer.execute_transfer()
    return new_transfer.record_transfer()

