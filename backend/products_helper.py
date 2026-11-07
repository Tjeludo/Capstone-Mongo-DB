from .connect import connect_to_mongodb
from datetime import datetime
from pymongo import errors

# Establish connections to the relevant MongoDB collections.
branches_table = connect_to_mongodb("Branch")
inventory_table = connect_to_mongodb("Inventory")
supplier_products_table = connect_to_mongodb("supplier_products")

class Helper:

    @staticmethod
    def add_product_to_supplier_prod(product_code, product_name, colour, size):
        """
        Adds a new product to every supplier's product list.
        This is to make sure all suppliers know about the new product.
        """
        new_product = {
            "product_code": product_code,
            "price(ZAR)": 0,
            "description": f"{product_name},{colour},{size}",
            "last_updated": datetime.now().strftime("%d-%m-%Y")
        }
        try:
            # A quick check to see if there are any suppliers to update.
            # estimated_document_count is faster than a full count for this.
            if supplier_products_table.estimated_document_count() == 0:
                return

            # Use $addToSet to add the product only if it doesn't already exist in the list.
            supplier_products_table.update_many({}, {"$addToSet": {"products": new_product}})
        except errors.OperationFailure as err:
            # log err.
            pass

    @staticmethod
    def remove_product_from_supplier_prod(product_code):
        """
        Removes a product from every supplier's product list.
        Useful when a product is discontinued.
        """
        try:
            # {} as the filter means "match all documents".
            query = {}
            # The $pull operator removes all instances of a value from an array.
            update_operation = { "$pull": {"product_list": product_code}}

            supplier_products_table.update_many(query, update_operation)
        except errors.OperationFailure as err:
            # log err
            pass

    @staticmethod
    def add_product_to_inventories(product_code, product_name, colour, size):
        """
        Ensures a new product exists in the inventory of every branch with a quantity of 0.
        This is a setup step so that stock can be added later.
        """
        name = f"{product_name},{colour},{size}"
        try:
            # 1. Get a list of all branch IDs.
            all_branches = list(branches_table.find({}, {"branchID": 1, "_id": 0}))

            if not all_branches:
                return

            # 2. Find which branches already have this product to avoid duplicates.
            # Using a set for fast lookups (e.g., `branch_id in existing_branches`).
            existing_branches = {
                doc["branchID"] for doc in inventory_table.find(
                    {"product_code": product_code},
                    {"branchID": 1, "_id": 0})
            }

            # 3. Prepare a list of new inventory documents for only the missing branches.
            inventory_docs = []
            for branch in all_branches:
                branch_id = branch.get("branchID")
                # Add if the branch has an ID and doesn't already have this product.
                if branch_id and branch_id not in existing_branches:
                    inv_doc = {
                        "product_code": product_code,
                        "product_name": name,
                        "branchID": branch_id,
                        "quantity": 0
                    }
                    inventory_docs.append(inv_doc)

            # 4. If there are any new documents to add, insert them all at once.
            # insert_many is much more efficient than inserting one by one in a loop.
            if inventory_docs:
                inventory_table.insert_many(inventory_docs)

        except errors.OperationFailure as err:
            # log err
            pass

if __name__ == "__main__":
    try:
        # A unique index on product_code would be beneficial here, but the original
        # code has it on the inventory table, which seems incorrect since a product
        # can exist in multiple branch inventories. I'll leave this as-is.
        inventory_table.create_index("product_code", unique=True)
    except errors.OperationFailure as e:
        pass