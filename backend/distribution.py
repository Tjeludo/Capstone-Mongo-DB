from .conn import connect
from datetime import datetime

# global variable
client = connect()
database = client["sample_mflix"]       # Choosing the database
collection = database["Inventory"]         # Choosing the table inside the chosen database
collectionDistribution = database["Distributions"]
collectionItem = database["products"]
collectionBranch = database["Branch"] 

# Distributions can only be donne from main branch 
def makeDistID():
    currentCount = collectionDistribution.count_documents({})   # Counting the number of collections currently in
    ID = "D00" + str(currentCount + 1)
    return ID


def checkAvailability(itemCode, quantity):
    """
    Checks item availability and provides a specific reason for failure.
    Returns a tuple: (bool, str) -> (is_available, message)
    """
    doc = collection.find_one({"product_code": itemCode, "branchID": "B001"})

    # Case 1: The item code does not exist in the main branch inventory.
    if not doc:
        return (False, f"Item '{itemCode}' not found in main branch inventory.")

    # Case 2: The item exists, but the quantity is insufficient.
    current_quantity = doc.get("quantity", 0)
    if current_quantity < quantity:
        return (False, f"Insufficient inventory for {itemCode}. Available: {current_quantity}, Requested: {quantity}.")

    # Case 3: Success! The item is available.
    return (True, "Item is available.")

@staticmethod
def does_product_exist(itemCode):
    """Checks if a product code exists in the main 'products' collection."""
    # collectionItem is the variable for your 'products' collection
    product = collectionItem.find_one({"product_code": itemCode})
    if product:
        return True
    return False

def get_product_name(item_code):
    """Fetches the product name for a given product code."""
    product = collectionItem.find_one(
        {"product_code": item_code},
        {"_id": 0, "product_name": 1}
    )
    if product and "product_name" in product:
        return product["product_name"]
    return "Unknown Product"  # Return a default value if not found

class DistributionItem:

    def __init__(self, itemCode,toBranch, quantity):
        self.itemCode = itemCode
        self.toBranch = toBranch
        self.quantity = quantity

    def to_dict(self):
        return {
            "product_code": self.itemCode,
            "branch": self.toBranch,
            "quantity": self.quantity
        }

    @staticmethod
    def getDistributionDetails(distributionID):
        """
        Returns all items distributed under a given distributionID.
        """
        doc = collectionDistribution.find_one(
            {"distributionID": distributionID},
            {"_id": 0, "distributions": 1}  # Projection to get only the distributions array
        )

        if doc and "distributions" in doc:
            # The 'distributions' array already contains all the necessary fields.
            return {"success": True, "message": "", "data": doc["distributions"]}
        
        # Return empty list if not found
        return {"success": True, "message": "Distribution not found or has no items.", "data": []}


class Distrubition:

    # distributions = []

    def __init__(self, date=None, distributionID=None):
        # This single constructor handles BOTH cases.

        # Case 1: Creating a NEW distribution (no arguments passed)
        if date is None and distributionID is None:
            self.distributionID = makeDistID()
            self.date = datetime.now().strftime("%d-%m-%Y %H:%M")

        # Case 2: Loading an EXISTING distribution from the DB
        else:
            self.distributionID = distributionID
            self.date = date

        # Common attributes for both cases
        self.distributions = []
        self.mainBranch = "B001"
        
    def addItem(self,distributionItem):   # add what has to be distributed to the array 

        if isinstance(distributionItem, DistributionItem):
            self.distributions.append(distributionItem)
        else:
            print("You can only add Distribution items ")
    
    def distribute(self,itemCode,toBranch,quantity ):  # the things in the array
        # move from the main branch to 
        # make sure that you don't distrube what you dont have 
        # print item name 
        # don't allow any negative distributions
        collection.update_one(
        {"product_code": itemCode, "branchID": toBranch},  # filter (safety check)
        {"$inc": {"quantity": quantity}}  
        )
        collection.update_one(
        {"product_code": itemCode, "branchID": self.mainBranch},  # filter (safety check)
        {"$inc": {"quantity": -quantity}}  
        )

    @staticmethod
    def getProductCodes():
        """
        Retrieves a list of all product codes from the 'products' collection.
        """
        product_codes = []
        try:
            # The 'collectionItem' variable is already defined globally in this file
            for doc in collectionItem.find({}, {"_id": 0, "product_code": 1}):
                if "product_code" in doc:
                    product_codes.append(doc["product_code"])

            return {"success": True, "message": "Product codes retrieved.", "data": product_codes}

        except Exception as e:
            return {"success": False, "message": str(e), "data": []}

    def recordDistributions(self):
        """
        Builds the complete distribution document in memory and saves it
        to the database in a single, atomic operation.
        """
        # 1. Prepare the list of distribution items
        distributions_to_insert = []
        for dist in self.distributions:
            product_name = get_product_name(dist.itemCode)
            distri_item = {
                "product_code": dist.itemCode,
                "product_name": product_name,  
                "branch": dist.toBranch,
                "quantity": dist.quantity
            }
            distributions_to_insert.append(distri_item)
        
        # 2. Create the final document with the populated list
        distri_doc = {
            "date": self.date,
            "distributionID": self.distributionID,
            "from": self.mainBranch,
            "distributions": distributions_to_insert  # Use the fully populated list
        }

        # 3. Insert the complete document in a single database call
        collectionDistribution.insert_one(distri_doc)


    def to_dict(self):
        return {
            "date": self.date,
            "distributionID": self.distributionID
            }
    # For the datagrid
    @staticmethod
    def getDistributions():
        distributions = []
        for doc in collectionDistribution.find():
            distri_item = Distrubition(doc["date"],doc["distributionID"])
            distributions.append(distri_item.to_dict())
        return {"success": True, "message": "", "data": distributions}


        
