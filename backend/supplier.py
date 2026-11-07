from pymongo import errors
from .connect import connect_to_mongodb
import pandas as pd
import io
from fastapi import UploadFile
from datetime import datetime

# Establish connection to MongoDB collections.
suppliers_table = connect_to_mongodb("suppliers")
supplier_products = connect_to_mongodb("supplier_products")
products_table = connect_to_mongodb("products")


class Supplier:
    def __init__(self, name, email, phone_number, address, sales_rep):
        # Clean and format input data upon object creation.
        self.supplier_name = name.strip().title()
        self.email = email.strip().lower()
        self.phone_number = phone_number.strip()
        self.address = address.strip().title()
        self.sales_rep = sales_rep.strip().title()
        self.supplier_id = self.generate_supplier_id()

    def add_supplier(self):
        """
        Adds a new supplier to the database.
        It also creates a corresponding document in the supplier_products collection.
        """
        if self.supplier_id is None:
            return {"success": False, "message": "Failed to generate supplier ID.", "data": ""}

        doc = {
            "supplier_id": self.supplier_id,
            "supplier_name": self.supplier_name,
            "email": self.email,
            "phone_number": self.phone_number,
            "address": self.address,
            "sales_rep": self.sales_rep,
        }

        if Supplier.supplier_exists(self.supplier_id):
            return {"success": False, "message": "Supplier already exists.", "data": ""}
        else:
            try:
                suppliers_table.insert_one(doc)
                # Create a parallel record for this supplier's products.
                doc2 = {"supplier_id": self.supplier_id, "supplier_name": self.supplier_name, "products": []}
                supplier_products.insert_one(doc2)

                # Fetch all existing products from the products table
                all_products = list(products_table.find({}, {"_id": 0, "product_code": 1, "product_name": 1, "colour": 1, "size": 1}))
                
                if all_products:
                    products_to_add = []
                    for product in all_products:
                        # Create a document for the supplier_products list
                        supplier_product_doc = {
                            "product_code": product.get("product_code"),
                            "price(ZAR)": 0,
                            "description": f"{product.get("product_name")},{product.get("colour")},{product.get("size")}",
                            "last_updated": datetime.now().strftime("%d-%m-%Y")                                                        
                        }
                        products_to_add.append(supplier_product_doc)
                    
                    # Update the newly created supplier_products document with the list of all products
                    if products_to_add:
                        supplier_products.update_one(
                            {"supplier_id": self.supplier_id},
                            {"$set": {"products": products_to_add}})
                        return {"success": True, "message": f"Supplier created and {len(products_to_add)} products added.", "data": ""}

                # This message is returned if no products exist in the products table yet.
                return {"success": True, "message": "Supplier created successfully. No products available to add.", "data": ""}

            except errors.OperationFailure as err:
                # TODO: Log this error.
                return {"success": False, "message": "Supplier creation failed.", "data": ""}

    def generate_supplier_id(self):
        """
        Generates a new, sequential supplier ID (e.g., S01, S02).
        If a supplier with the same name and email already exists, it returns their existing ID.
        """
        # First, check if this exact supplier already exists to avoid duplicates.
        existing_supplier = suppliers_table.find_one({
            "supplier_name": self.supplier_name,
            "email": self.email
        })
        if existing_supplier:
            return existing_supplier['supplier_id']

        # If not, generate a new ID.
        try:
            # Find the document with the highest 'supplier_id'.
            doc = suppliers_table.find_one(sort=[("supplier_id", -1)])
            if doc and "supplier_id" in doc:
                last_id = doc["supplier_id"]
                # Extract the numeric part (e.g., '01' from 'S01').
                num = int(last_id[1:])
                if num >= 99:
                    raise ValueError("Maximum supplier_id reached (S99)")
                new_id = num + 1
            else:
                # This is the first supplier.
                new_id = 1
            # Format to a two-digit padded string, like 'S01'.
            supplier_id = f"S{new_id:02d}"
            return supplier_id
        except (errors.OperationFailure, ValueError) as err:
            # TODO: Log these errors.
            return None

    @staticmethod
    def supplier_exists(supplier_id):
        """Checks if a supplier with the given ID exists."""
        try:
            doc = suppliers_table.find_one({"supplier_id": supplier_id})
            return bool(doc)
        except errors.OperationFailure as err:
            # TODO: Log this error.
            return False

    @staticmethod
    def update_supplier(supplier_id, name, email, phone_number, address, sales_rep):
        """Updates the details of an existing supplier."""
        if Supplier.supplier_exists(supplier_id):
            try:
                suppliers_table.update_one(
                    {"supplier_id": supplier_id},
                    {"$set": {
                        # Clean and format the data just before updating.
                        "supplier_name": name.strip().title(),
                        "email": email.strip().lower(),
                        "phone_number": phone_number.strip(),
                        "address": address.strip().title(),
                        "sales_rep": sales_rep.strip().title()
                    }})
                return {"success": True, "message": "Supplier updated.", "data": ""}
            except errors.OperationFailure as err:
                # TODO: Log this error.
                return {"success": False, "message": "Supplier update failed.", "data": ""}
        else:
            return {"success": False, "message": "Supplier does not exist.", "data": ""}

    @staticmethod
    def delete_supplier(supplier_id):
        """Deletes a supplier and their associated product list."""
        if Supplier.supplier_exists(supplier_id):
            try:
                suppliers_table.delete_one({"supplier_id": supplier_id})
                supplier_products.delete_one({"supplier_id": supplier_id})
                return {"success": True, "message": "Supplier deleted.", "data": ""}
            except errors.OperationFailure as err:
                # TODO: Log this error.
                return {"success": False, "message": "Supplier deletion failed.", "data": ""}
        else:
            return {"success": False, "message": "Supplier does not exist.", "data": ""}

    @staticmethod
    def view_suppliers():
        """Retrieves a list of all suppliers."""
        try:
            # Use a projection to exclude the default '_id' field from the result.
            cursor = suppliers_table.find({}, {"_id": 0})
            data = list(cursor)
            if data:
                return {"success": True, "message": "", "data": data}
            else:
                return {"success": False, "message": "No suppliers found.", "data": ""}
        except errors.OperationFailure as err:
            # TODO: Log this error.
            return {"success": False, "message": "Failed to retrieve suppliers.", "data": ""}

    @staticmethod
    def search_supplier(supplier_id):
        """Finds and returns a single supplier by their ID."""
        if Supplier.supplier_exists(supplier_id):
            try:
                doc = suppliers_table.find_one({"supplier_id": supplier_id}, {"_id": 0})
                if doc:
                    return {"success": True, "message": "", "data": doc}
                else:
                    return {"success": False, "message": "Supplier not found.", "data": ""}
            except errors.OperationFailure as err:
                # TODO: Log this error.
                return {"success": False, "message": "Failed to retrieve supplier.", "data": ""}
        else:
            return {"success": False, "message": "Supplier does not exist.", "data": ""}

    @staticmethod
    def view_supplier_products(supplier_id):
        """Retrieves all products associated with a specific supplier."""
        try:
            if Supplier.supplier_exists(supplier_id):
                doc = supplier_products.find_one({"supplier_id": supplier_id}, {"_id": 0})
                if doc and "products" in doc:
                    products = doc["products"]
                    if products:
                        return {"success": True, "message": "", "data": products}
                # Handles cases where the supplier exists but has no products.
                return {"success": False, "message": "No products found for this supplier.", "data": ""}
            else:
                return {"success": False, "message": "Supplier does not exist.", "data": ""}
        except errors.OperationFailure as err:
            # TODO: Log this error.
            return {"success": False, "message": "Failed to retrieve products for this supplier.", "data": ""}

    @staticmethod
    def download_supplier_csv():
        """Generates a CSV string of all supplier data for downloading."""
        try:
            data = list(suppliers_table.find({}, {"_id": 0}))
            if not data:
                return ""

            df = pd.DataFrame(data)
            csv_buffer = io.StringIO()
            df.to_csv(csv_buffer, index=False)
            return csv_buffer.getvalue()
        except errors.OperationFailure as err:
            # TODO: Log this error.
            return None  # Return None to indicate failure

    @staticmethod
    async def upload_suppliers_csv(file: UploadFile):
        """Processes an uploaded CSV file to bulk-add suppliers."""
        try:
            # Safety check: Prevent upload if data already exists.
            if suppliers_table.count_documents({}) > 0:
                return {"success": False, "message": "Upload failed. Table must be empty.", "data": ""}

            contents = await file.read()
            df = pd.read_csv(io.StringIO(contents.decode('utf-8')))

            # Define the exact columns needed in the CSV.
            required_columns = ['supplier_name', 'email', 'phone_number', 'address', 'sales_rep']
            if not all(col in df.columns for col in required_columns) or any(
                    col not in required_columns for col in df.columns):
                return {"success": False,
                        "message": f"CSV must contain exactly these columns: {', '.join(required_columns)}", "data": ""}

            # Iterate through the DataFrame and add each supplier.
            for index, row in df.iterrows():
                try:
                    # Skip rows that are mostly empty.
                    if pd.isna(row['supplier_name']) or pd.isna(row['email']):
                        continue

                    supplier = Supplier(
                        name=str(row['supplier_name']),
                        email=str(row['email']),
                        phone_number=str(row['phone_number']),
                        address=str(row['address']),
                        sales_rep=str(row['sales_rep']))
                    result = supplier.add_supplier()

                    # If any row fails, stop the whole process.
                    if not result["success"]:
                        # This provides a more specific error if a duplicate is found during upload.
                        if "already exists" in result.get("message", ""):
                            return {"success": False,
                                    "message": f"Upload failed: Supplier '{row['supplier_name']}' already exists.",
                                    "data": ""}
                        return {"success": False, "message": f"Upload failed processing row {index + 2}.", "data": ""}
                except Exception:
                    return {"success": False, "message": f"Upload failed due to an error at row {index + 2}.",
                            "data": ""}

            return {"success": True, "message": "Upload successful. All suppliers added.", "data": ""}

        except Exception:
            return {"success": False, "message": "Upload failed due to an unexpected error.", "data": ""}


if __name__ == "__main__":
    # This block runs only when the script is executed directly.
    # It's a good place for setup tasks, like creating database indexes.
    try:
        # Ensures 'supplier_id' is unique, preventing duplicate entries.
        suppliers_table.create_index("supplier_id", unique=True)
    except errors.OperationFailure as e:
        pass