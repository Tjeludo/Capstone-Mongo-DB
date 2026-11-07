from .conn import connect



client = connect()
database = client["sample_mflix"]       # Choosing the database
collection = database["Branch"]         # Choosing the table inside the chosen database
inventory_collection = database["Inventory"] # New handle for the Inventory collection
products_collection = database["products"]   # New handle for the Products collection


class Branch:
 
    def __init__(self, location, contactDetails, manager, branchID=None):
        """Branch constructor"""
        self.ID = branchID if branchID else self.makeID()
        self.location = location
        self.contactDetails = contactDetails 
        self.manager = manager

    def __str__(self):
        return f"Branch ID = {self.ID} {self.location} {self.contactDetails} \nManager: {self.manager}" 

    def makeID(self):
        """Makes a unique branch based on the number of branches already there"""
        currentCount = collection.count_documents({})
        return "B00" + str(currentCount + 1)

    # --- MODIFICATION 2: Update the addBranch method ---
    def addBranch(self):
        """
        Adds a new branch to the database and then populates the inventory
        with all existing products for that new branch, setting their quantity to 0.
        """
        branch_document = {
            "branchID": self.ID,
            "location": self.location,
            "contactDetails": self.contactDetails,
            "manager": self.manager
        }
        collection.insert_one(branch_document)
        
        #Populate inventory for the newly created branch
        try:
            
            all_products = list(products_collection.find({}, {"_id": 0, "product_code": 1, "product_name": 1}))

            if not all_products:
                print("Warning: Branch created, but no products found to populate inventory.")
                return {"success": True, "message": "Branch created successfully, but no products were available to add to inventory.", "data": ""}

            new_inventory_items = []
            for product in all_products:
                inventory_doc = {
                    "product_code": product["product_code"],
                    "product_name": f"{product.get("product_name")},{product.get("colour")},{product.get("size")}",
                    "branchID": self.ID,  # Use the ID of the branch we just created
                    "quantity": 0
                }
                new_inventory_items.append(inventory_doc)
            
            if new_inventory_items:
                inventory_collection.insert_many(new_inventory_items)
            
            return {"success": True, "message": f"Branch created and {len(new_inventory_items)} inventory items populated.", "data": ""}

        except Exception as e:
            # If inventory population fails, the branch was still created.
            print(f"ERROR: Could not populate inventory for new branch {self.ID}. Reason: {e}")
            return {"success": False, "message": f"Branch was created, but failed to populate inventory items. Error: {e}", "data": ""}
        
    def to_dict(self):
        """Formats the branch objects for them to re passed to the user interface"""
        return {
            "branchID": self.ID,
            "location": self.location,
            "contactDetails": self.contactDetails,
            "manager": self.manager
        }

    @staticmethod
    def getBranches():
        """Gets all the branches at the database and passes calls to_dict for them to be in the correct formart of the ui"""
        branches = [] 
        for doc in collection.find():
            branch_obj = Branch(
                location=doc["location"],
                contactDetails=doc["contactDetails"],
                manager=doc["manager"],
                branchID=doc["branchID"]
            )
            branches.append(branch_obj.to_dict())
        return {"success": True, "message": "", "data": branches}


    @staticmethod
    def getBranchCode():
        """Gets all the branch codes from the database"""
        branches = []
        for doc in collection.find():
            branches.append(doc["branchID"])
        return {"success": True, "message": "", "data": branches}

    @staticmethod
    def update_branch(branch_id, update_data):
        """Updates existing branch information """
        # Find the branch by its unique branchID and update it
        result = collection.update_one(
            {"branchID": branch_id},
            {"$set": update_data}
        )
        
        # Check if any document was actually modified
        if result.modified_count > 0:
            return {"success": True, "message": "Branch updated successfully."}
        else:
            return {"success": False, "message": "Branch not found or data is the same."}