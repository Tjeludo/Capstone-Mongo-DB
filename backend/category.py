from pymongo import errors
from .connect import connect_to_mongodb
import pandas as pd
import io
from fastapi import UploadFile

# Establish connection to MongoDB collections.
category_table = connect_to_mongodb("category")
products_table = connect_to_mongodb("products")


class Category:
    def __init__(self, name, description):
        """Initializes a new Category instance."""
        # Standardize inputs: Title Case for name, lowercase for description.
        self.category_name = name.title()
        self.description = description.lower()
        self.category_code = self.generate_category_code()
        self.products = 0

    def add_category(self):
        """Adds the category to the database if it doesn't already exist."""
        if self.category_code is None:
            return {"success": False, "message": "Failed to generate a category code.", "data": ""}

        doc = {
            "category_code": self.category_code,
            "category_name": self.category_name,
            "description": self.description,
            "products": self.products
        }

        # Prevent creating duplicate categories.
        if Category.category_exists(self.category_code):
            return {"success": False, "message": "A category with this name and description already exists.",
                    "data": ""}
        else:
            try:
                category_table.insert_one(doc)
                return {"success": True, "message": "Category created successfully.", "data": ""}
            except errors.OperationFailure as err:
                # TODO: Log the actual error 'err'.
                return {"success": False, "message": "Category creation failed.", "data": ""}

    def generate_category_code(self):
        """
        Generates a new category code (e.g., C01, C02) or returns the existing
        code if a category with the same name and description is found.
        """
        # First, check if a category with the same details already exists to avoid duplicates.
        existing_category = category_table.find_one(
            {"category_name": self.category_name})
        if existing_category:
            return existing_category['category_code']

        # If it's a new category, generate the next sequential code.
        try:
            # Find the document with the highest category_code.
            doc = category_table.find_one(sort=[("category_code", -1)])
            if doc and "category_code" in doc:
                last_id = doc["category_code"]
                num = int(last_id[1:])  # Extract the number part (e.g., from 'C09' to 9).
                new_id = num + 1
            else:
                # This is the first category in the collection.
                new_id = 1

            # Format the new code as C## (e.g., C01, C12).
            category_code = f"C{new_id:02d}"
            return category_code
        except (errors.OperationFailure, ValueError) as err:
            # TODO: Log the actual error 'err'.
            return None

    @staticmethod
    def update_category(category_code, name, description):
        """Updates a category's name and description."""
        if Category.category_exists(category_code):
            try:
                # Standardize inputs: Title Case for name, lowercase for description.
                update_data = {"$set": {"category_name": name.title(), "description": description.lower()}}
                category_table.update_one({"category_code": category_code}, update_data)
                return {"success": True, "message": "Category updated successfully.", "data": ""}
            except errors.OperationFailure:
                # TODO: Log error.
                return {"success": False, "message": "Category update failed.", "data": ""}
        else:
            return {"success": False, "message": "Category does not exist.", "data": ""}

    @staticmethod
    def category_exists(category_code):
        """Checks if a category with the given code exists in the database."""
        try:
            return category_table.find_one({"category_code": category_code}) is not None
        except errors.OperationFailure:
            # TODO: Log error.
            return False

    @staticmethod
    def view_categories():
        """Retrieves a list of all categories from the database."""
        try:
            # Find all documents, excluding the internal '_id' field.
            cursor = category_table.find({}, {"_id": 0})
            data = list(cursor)
            if data:
                return {"success": True, "message": "Categories retrieved.", "data": data}
            else:
                return {"success": False, "message": "No categories found.", "data": ""}
        except errors.OperationFailure:
            # TODO: Log error.
            return {"success": False, "message": "Failed to retrieve categories.", "data": ""}

    @staticmethod
    def view_category_products(category_code):
        """Retrieves all products belonging to a specific category."""
        try:
            if Category.category_exists(category_code):
                # Find all products that match the given category_code.
                cursor = products_table.find({"category_code": category_code}, {"_id": 0})
                products = list(cursor)
                if products:
                    return {"success": True, "message": "Products retrieved.", "data": products}
                else:
                    # The category exists but has no products.
                    return {"success": False, "message": "No products found in this category.", "data": ""}
            else:
                return {"success": False, "message": "Category does not exist.", "data": ""}
        except errors.OperationFailure:
            # TODO: Log error.
            return {"success": False, "message": "Failed to retrieve products for this category.", "data": ""}

    @staticmethod
    def download_category_csv():
        """Exports all categories to a CSV formatted string."""
        try:
            data = list(category_table.find({}, {"_id": 0}))
            if not data:
                return None  # Return None if there's nothing to export.

            df = pd.DataFrame(data)
            csv_buffer = io.StringIO()
            df.to_csv(csv_buffer, index=False)
            return csv_buffer.getvalue()
        except errors.OperationFailure:
            # TODO: Log error.
            return None

    @staticmethod
    async def upload_categories_csv(file: UploadFile):
        """Processes an uploaded CSV file to bulk-add categories."""
        try:
            # For simplicity, only allow uploading to an empty collection.
            if category_table.count_documents({}) > 0:
                return {"success": False, "message": "Upload failed. Categories table is not empty.", "data": ""}

            contents = await file.read()
            df = pd.read_csv(io.StringIO(contents.decode('utf-8')))

            # Define the exact columns required in the CSV.
            required_columns = ['category_name', 'description']
            if not all(col in df.columns for col in required_columns) or len(df.columns) != len(required_columns):
                return {"success": False,
                        "message": f"CSV must contain exactly these columns: {', '.join(required_columns)}", "data": ""}

            # Process each row in the DataFrame.
            for index, row in df.iterrows():
                name = str(row['category_name']).strip()
                description = str(row['description']).strip()

                # Skip rows that are empty or just contain 'nan'.
                if not name or pd.isna(row['category_name']):
                    continue

                # Create and add the new category.
                category = Category(name, description)
                result = category.add_category()

                if not result["success"]:
                    # If any row fails, stop the whole process.
                    return {"success": False, "message": f"Upload failed on row {index + 2}: {result['message']}",
                            "data": ""}

            return {"success": True, "message": "CSV uploaded and categories added successfully.", "data": ""}
        except Exception:
            # TODO: Log specific error.
            return {"success": False, "message": "An unexpected error occurred during CSV upload.", "data": ""}

    @staticmethod
    def search_category(category_code):
        """Finds and returns a single category by its code."""
        if Category.category_exists(category_code):
            try:
                doc = category_table.find_one({"category_code": category_code}, {"_id": 0})
                if doc:
                    return {"success": True, "message": "Category found.", "data": doc}
                else:
                    # This case is unlikely if category_exists passed, but good for safety.
                    return {"success": False, "message": "Category not found.", "data": ""}
            except errors.OperationFailure:
                # TODO: Log error.
                return {"success": False, "message": "Failed to retrieve category.", "data": ""}
        else:
            return {"success": False, "message": "Category does not exist.", "data": ""}

    @staticmethod
    def update_product_count(category_code):
        """
        Recalculates and updates the number of products for a given category.
        This should be called whenever a product is added or removed.
        """
        if Category.category_exists(category_code):
            try:
                # Count how many products belong to this category.
                product_count = products_table.count_documents({"category_code": category_code})
                # Update the 'products' field in the category document.
                category_table.update_one(
                    {"category_code": category_code},
                    {"$set": {"products": product_count}}
                )
            except errors.OperationFailure:
                # TODO: Log error.
                pass


# This block runs only when the script is executed directly.
# It's a good place for setup tasks, like ensuring database indexes exist.
if __name__ == "__main__":
    try:
        # Create a unique index on 'category_code' to prevent duplicates and speed up searches.
        category_table.create_index("category_code", unique=True)
    except errors.OperationFailure as e:
        # TODO: Log error.
        pass