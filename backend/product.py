from pymongo import errors
from .connect import connect_to_mongodb
from .size import Size
from .colour import Colour
from .category import Category
from .products_helper import Helper
import pandas as pd
import io
from fastapi import UploadFile

products_table = connect_to_mongodb("products")


class Product:
    # Represents a single product, handling its data and database interactions.
    def __init__(self, prod_name, prod_category, prod_size, prod_colour, prod_price):
        self.product_name = prod_name.strip().title()  # Standardize to title case
        self.category_code = prod_category
        self.size = prod_size
        self.colour = prod_colour
        self.price = prod_price
        self.product_code = self.generate_product_code()

    def add_product(self):
        """
        Adds a new product to the database.
        Also updates related collections like inventory and supplier products.
        """
        if self.product_code is None:
            return {"success": False, "message": "Failed to add product.", "data": ""}

        doc = {
            "product_code": self.product_code,
            "product_name": self.product_name,
            "category_code": self.category_code,
            "price(ZAR)": self.price,
            "size": self.size,
            "colour": self.colour,
        }

        if Product.product_exists(self.product_code):
            return {"success": False, "message": "Product already exists.", "data": ""}
        else:
            try:
                # Add product to the main, supplier, and inventory tables
                products_table.insert_one(doc)
                Helper.add_product_to_supplier_prod(self.product_code, self.product_name, self.colour, self.size)
                Helper.add_product_to_inventories(self.product_code, self.product_name, self.colour, self.size)

                # Update the product count for the associated category
                Category.update_product_count(self.category_code)
                return {"success": True, "message": "Product created successfully.", "data": ""}
            except errors.OperationFailure as err:
                # TODO: log err here
                return {"success": False, "message": "Product creation failed.", "data": ""}

    def generate_product_code(self):
        """
        Generates a unique product code based on category, size, and color.
        Format: 0<Category:02d><ProductNum:03d><Size:02d><Colour:02d>
        """
        try:
            # Extract numerical parts of codes needed for the final product code
            category_code = int(self.category_code[1:])
            size_code = Size.get_size_id(self.size)
            colour_code = Colour.get_colour_id(self.colour)

            if size_code is None or colour_code is None:
                return None

            size_code_int = int(size_code)
            colour_code_int = int(colour_code)

            # Before creating a new code, check if an identical product already exists to prevent duplicates.
            existing_product = products_table.find_one({
                "category_code": self.category_code,
                "size": self.size,
                "colour": self.colour})

            if existing_product:
                return existing_product['product_code']

            # Find the last product code in the current category to determine the next sequential number.
            current_category = f"0{category_code:02}"
            next_category = f"0{category_code + 1:02}"

            query_filter = {"product_code": {"$gte": current_category, "$lt": next_category}}
            doc = products_table.find_one(query_filter, sort=[("product_code", -1)])

            if doc and "product_code" in doc:
                last_id = doc["product_code"]
                last_code = int(last_id[3:6])  # Extract the 3-digit product number
                if last_code >= 999:
                    raise ValueError("Maximum product code reached for this category")
                new_code = last_code + 1
            else:
                new_code = 1  # First product in this category

            # Assemble the final code
            return f"0{category_code:02d}{new_code:03d}{size_code_int:02d}{colour_code_int:02d}"

        except (ValueError, errors.OperationFailure) as err:
            # TODO: log err
            return None

    @staticmethod
    def product_exists(product_code):
        """Checks if a product with the given code already exists."""
        try:
            return products_table.find_one({"product_code": product_code}) is not None
        except errors.OperationFailure as err:
            # TODO: log err
            return False

    @staticmethod
    def update_product(product_code, name, price):
        """Updates a product's name and price."""
        if Product.product_exists(product_code):
            try:
                products_table.update_one({"product_code": product_code},
                                          {"$set": {"product_name": name.strip().title(), "price(ZAR)": price}})
                return {"success": True, "message": "Product updated successfully.", "data": ""}
            except errors.OperationFailure as err:
                # TODO: log err
                return {"success": False, "message": "Product update failed.", "data": ""}
        else:
            return {"success": False, "message": "Product does not exist.", "data": ""}

    @staticmethod
    def view_products():
        """Retrieves all products from the database."""
        try:
            cursor = products_table.find({}, {"_id": 0})
            data = list(cursor)
            if data:
                return {"success": True, "message": "", "data": data}
            else:
                return {"success": False, "message": "No products found.", "data": ""}
        except errors.OperationFailure as err:
            # TODO: log err
            return {"success": False, "message": "Failed to retrieve products.", "data": ""}

    @staticmethod
    def search_product(product_code):
        """Finds a single product by its unique code."""
        if Product.product_exists(product_code):
            try:
                doc = products_table.find_one({"product_code": product_code}, {"_id": 0})
                if doc:
                    return {"success": True, "message": "", "data": doc}
                else:
                    return {"success": False, "message": "Product not found.", "data": ""}
            except errors.OperationFailure as err:
                # TODO: log error
                return {"success": False, "message": "Failed to retrieve product.", "data": ""}
        else:
            return {"success": False, "message": "Product does not exist.", "data": ""}

    @staticmethod
    def delete_product(product_code):
        """Deletes a product from all relevant collections."""
        if Product.product_exists(product_code):
            try:
                # Find the product first to get its category for the count update.
                product = products_table.find_one({"product_code": product_code})
                category_code = product.get("category_code") if product else None

                # Perform deletions
                products_table.delete_one({"product_code": product_code})
                Helper.remove_product_from_supplier_prod(product_code)

                # Update category product count after deletion
                if category_code:
                    Category.update_product_count(category_code)

                return {"success": True, "message": "Product deleted successfully.", "data": ""}
            except errors.OperationFailure as err:
                # TODO: log err
                return {"success": False, "message": "Product deletion failed.", "data": ""}
        else:
            return {"success": False, "message": "Product does not exist.", "data": ""}

    @staticmethod
    def download_products_csv():
        """Exports all products to a CSV string."""
        try:
            data = list(products_table.find({}, {"_id": 0}))
            if not data:
                return ""

            df = pd.DataFrame(data)
            csv_buffer = io.StringIO()
            df.to_csv(csv_buffer, index=False)
            return csv_buffer.getvalue()
        except errors.OperationFailure as err:
            # TODO: log err
            return ""

    @staticmethod
    async def upload_products_csv(file: UploadFile):
        """Processes an uploaded CSV file to bulk-add products."""
        try:
            # For safety, only allow CSV upload to an empty product table.
            if products_table.count_documents({}) > 0:
                return {
                    "success": False,
                    "message": "Upload failed. Table already contains data.",
                    "data": ""}

            contents = await file.read()
            df = pd.read_csv(io.StringIO(contents.decode('utf-8')))

            # Define the exact columns we expect in the CSV.
            required_columns = ['product_name', 'category_code', 'size', 'colour', 'price']
            if not all(col in df.columns for col in required_columns):
                return {
                    "success": False,
                    "message": f"CSV must contain exactly these columns:\n{', '.join(required_columns)}",
                    "data": ""}

            # Reject files that have extra, unexpected columns.
            extra_columns = [col for col in df.columns if col not in required_columns]
            if extra_columns:
                return {
                    "success": False,
                    "message": f"CSV contains unexpected columns.\nOnly {', '.join(required_columns)} are allowed.",
                    "data": ""}

            # Loop through each row and attempt to add it as a new product.
            for index, row in df.iterrows():
                try:
                    name = str(row['product_name']).strip()
                    category_code = str(row['category_code']).strip()
                    size = str(row['size']).strip()
                    colour = str(row['colour']).strip()
                    price = row['price']

                    # A simple check to skip rows that appear to be empty.
                    if (pd.isna(row['product_name']) or pd.isna(row['category_code']) or
                            pd.isna(row['size']) or pd.isna(row['colour']) or pd.isna(row['price'])):
                        continue

                    product = Product(name, category_code, size, colour, price)
                    result = product.add_product()

                    # If any product fails to add, stop the entire process.
                    if not result["success"]:
                        return {"success": False, "message": "Upload failed due to an error in the data.", "data": ""}

                except Exception:
                    return {"success": False, "message": "Upload failed while processing a row.", "data": ""}

            return {
                "success": True,
                "message": "Upload successful.\nProducts imported successfully.",
                "data": ""}

        except Exception:
            return {
                "success": False,
                "message": "Upload failed. The file might be invalid.",
                "data": ""}

    @staticmethod
    def get_products_by_category(category_code):
        """Retrieves all products belonging to a specific category."""
        try:
            cursor = products_table.find({"category_code": category_code}, {"_id": 0})
            data = list(cursor)
            if data:
                return {"success": True, "message": "", "data": data}
            else:
                return {"success": False, "message": "No products found in this category.", "data": ""}
        except errors.OperationFailure as err:
            # TODO: log err
            return {"success": False, "message": "Failed to retrieve products for this category.", "data": ""}

    @staticmethod
    def search_products_by_name(name):
        """
        Searches for products using a case-insensitive partial name match.
        """
        try:
            # Use regex for a "contains" search
            cursor = products_table.find(
                {"product_name": {"$regex": name, "$options": "i"}},
                {"_id": 0})
            data = list(cursor)
            if data:
                return {"success": True, "message": "", "data": data}
            else:
                return {"success": False, "message": "No products found matching the search.", "data": ""}
        except errors.OperationFailure as err:
            # TODO: log err
            return {"success": False, "message": "Search failed.", "data": ""}


if __name__ == "__main__":
    # This block runs only when the script is executed directly.
    # It's a good place to set up things like database indexes.
    try:
        products_table.create_index("product_code", unique=True)
    except errors.OperationFailure as e:
        # TODO: log err
        pass