from .conn import connect

client = connect()
database = client["sample_mflix"]
collection = database["Inventory"]
collectionItem = database["products"]
collectionBranch = database["Branch"]


def checkItem(itemCode):
    """Checks if itemCode exists"""
    itemExists = collectionItem.find_one({"product_code": itemCode}) is not None
    return itemExists


def checkBranch(branchID):
    """Checks if branch exists"""
    branchExists = collectionBranch.find_one({"branchID": branchID}) is not None
    return branchExists


def notAdded(itemCode, branchID):
    """Makes sure that a inventory item was not already to the databse"""
    add = collection.find_one({"product_code": itemCode, "branchID": branchID}) is not None
    return add


def allExist(itemCode, fromBranchID, toBranchID):
    """Make sure that itemcode, and both branches exists in the databse"""
    itemExists = collection.find_one({"product_code": itemCode}) is not None
    FrombranchExists = collectionBranch.find_one({"branchID": fromBranchID}) is not None
    TobranchExists = collectionBranch.find_one({"branchID": toBranchID}) is not None
    return itemExists and FrombranchExists and TobranchExists


class Inventory:
    def __init__(self, productCode, productName, branchID, quantity):
        """Constructor"""
        self.product = productCode
        self.productName = productName
        self.branch = branchID
        self.quantity = quantity

    def to_dict(self):
        """Formats the branch objects for them to re passed to the user interface"""
        return {
            "product_code": self.product,
            "product_name": self.productName,
            "branchID": self.branch,
            "quantity": self.quantity
        }

    @staticmethod
    def getInventory():
        """Gets all the inventory documents in the databse"""
        inventory = []
        for doc in collection.find():
            inventory_object = Inventory(
                doc["product_code"],
                doc["product_name"],
                doc["branchID"],
                doc["quantity"]
            )
            inventory.append(inventory_object.to_dict())
        return {"success": True, "message": "", "data": inventory}


    @staticmethod
    def getInventoryItems():
        """Gets the itemcodes only from the database"""
        inventory = []
        for doc in collectionItem.find():
            inventory.append(doc["product_code"])
        return {"success": True, "message": "", "data": inventory}

