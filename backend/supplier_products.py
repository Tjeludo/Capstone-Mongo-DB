from datetime import datetime
from .connect import connect_to_mongodb
from pymongo import errors
from .supplier import Supplier
import pandas as pd
import io
from fastapi import UploadFile

supplier_products_table = connect_to_mongodb("supplier_products")


class SupplierItem:
    def __init__(self, supplier_id, product_code, price=0):
        self.supplier_id = supplier_id
        self.product_code = product_code
        self.price = price
        self.last_updated = datetime.now().strftime("%d-%m-%Y")

    def add_product(self):
        if Supplier.supplier_exists(self.supplier_id):
            new_product = {
                "product_code": self.product_code,
                "price(ZAR)": self.price,
                # "description": self.description, # This needs a source, like a products table
                "last_updated": self.last_updated
            }

            try:
                existing = supplier_products_table.find_one(
                    {"supplier_id": self.supplier_id, "products.product_code": self.product_code},
                    {"_id": 1})

                if existing:
                    return {
                        "success": False,
                        "message": "Product already exists for this supplier.",
                        "data": ""
                    }

                supplier_products_table.update_one(
                    {"supplier_id": self.supplier_id},
                    {"$push": {"products": new_product}},
                    upsert=False
                )

                return {
                    "success": True,
                    "message": "Product added successfully.",
                    "data": new_product
                }
            except Exception as e:
                return {
                    "success": False,
                    "message": f"Failed to add product: {str(e)}",
                    "data": ""
                }
        else:
            return {"success": False, "message": "Supplier not found. Cannot add product.", "data": ""}

    @staticmethod
    def update_supplier_price(supplier_id, product_code, price):
        if SupplierItem.supplier_product_exists(product_code, supplier_id):
            try:
                new_last_updated = datetime.now().strftime("%d-%m-%Y")
                supplier_products_table.update_one(
                    {"supplier_id": supplier_id, "products.product_code": product_code},
                    {
                        "$set": {
                            "products.$.price(ZAR)": price,
                            "products.$.last_updated": new_last_updated
                        }
                    }
                )
                return {"success": True, "message": "Supplier product & price updated", "data": ""}
            except Exception as err:
                return {"success": False, "message": "Supplier product update failed", "data": ""}
        else:
            return {"success": False, "message": "Supplier product not found for the specified supplier.", "data": ""}

    @staticmethod
    def delete_supplier_product(supplier_id, product_code):
        if SupplierItem.supplier_product_exists(product_code, supplier_id):
            try:
                supplier_products_table.update_one(
                    {"supplier_id": supplier_id},
                    {"$pull": {"products": {"product_code": product_code}}}
                )
                return {"success": True, "message": "Supplier product & price deleted", "data": ""}
            except Exception as err:
                return {"success": False, "message": "Supplier product deletion failed", "data": ""}
        else:
            return {"success": False, "message": "Supplier product not found for the specified supplier.", "data": ""}

    @staticmethod
    def search_supplier_products(supplier_id, product_code=None):
        if Supplier.supplier_exists(supplier_id):
            try:
                doc = supplier_products_table.find_one(
                    {"supplier_id": supplier_id},
                    {"_id": 0, "products": 1})
                if doc and "products" in doc:
                    products = doc['products']
                    if product_code:
                        filtered_products = [p for p in products if p.get("product_code") == product_code]
                        if filtered_products:
                            return {"success": True, "message": "", "data": filtered_products}
                        else:
                            return {"success": False, "message": "Product not found for this supplier.", "data": ""}
                    else:
                        return {"success": True, "message": "", "data": products}
                else:
                    return {"success": False, "message": "No products found for this supplier.", "data": ""}
            except Exception as err:
                return {"success": False, "message": "Failed to retrieve supplier products.", "data": ""}
        else:
            return {"success": False, "message": "Supplier not found.", "data": ""}

    @staticmethod
    def supplier_product_exists(product_code, supplier_id=None):
        try:
            filter_query = {}
            if supplier_id:
                filter_query["supplier_id"] = supplier_id
            for doc in supplier_products_table.find(filter_query, {"products": 1}):
                for product in doc.get("products", []):
                    if product.get("product_code") == product_code:
                        return True
            return False
        except Exception as err:
            return False

    @staticmethod
    def view_products_for_supplier(supplier_id):
        try:
            doc = supplier_products_table.find_one(
                {"supplier_id": supplier_id},
                {"_id": 0, "supplier_id": 1, "products": 1}
            )
            if doc:
                flattened_data = []
                if "products" in doc:
                    sid = doc.get("supplier_id")
                    for p in doc["products"]:
                        flattened_data.append({
                            "supplier_id": sid,
                            "product_code": p.get("product_code"),
                            "price": p.get("price(ZAR)"),
                            "description": p.get("description"),
                            "last_updated": p.get("last_updated")
                        })
                return {"success": True, "message": "Products retrieved successfully.", "data": flattened_data}
            else:
                return {"success": False, "message": "Supplier not found.", "data": ""}
        except Exception as err:
            return {"success": False, "message": f"Failed to retrieve supplier products: {err}", "data": ""}

    @staticmethod
    def view_supplier_products():
        try:
            cursor = supplier_products_table.find({}, {"_id": 0, "supplier_id": 1, "products": 1})
            flattened_data = []
            for doc in cursor:
                sid = doc.get("supplier_id")
                if "products" in doc and sid:
                    for p in doc["products"]:
                        flattened_data.append({
                            "supplier_id": sid,
                            "product_code": p.get("product_code"),
                            "price": p.get("price(ZAR)"),
                            "description": p.get("description"),
                            "last_updated": p.get("last_updated")
                        })
            return {"success": True, "message": "All products retrieved.", "data": flattened_data}
        except Exception as err:
            return {"success": False, "message": f"Failed to retrieve products: {err}", "data": ""}

    @staticmethod
    def download_products_for_supplier_csv(supplier_id):
        if not Supplier.supplier_exists(supplier_id):
            return ""

        try:
            doc = supplier_products_table.find_one(
                {"supplier_id": supplier_id},
                {"_id": 0, "products": 1}
            )

            if doc and "products" in doc:
                products = doc['products']
                product_list = [
                    {
                        "supplier_id": supplier_id,
                        "product_code": p.get("product_code", ""),
                        "description": p.get("description", ""),
                        "price(ZAR)": p.get("price(ZAR)", 0),
                        "last_updated": p.get("last_updated", "")
                    }
                    for p in products
                ]

                if not product_list:
                    return ""

                df = pd.DataFrame(product_list)
                # Reorder columns to have supplier_id first
                df = df[["supplier_id", "product_code", "description", "price(ZAR)", "last_updated"]]
                
                csv_buffer = io.StringIO()
                df.to_csv(csv_buffer, index=False)
                return csv_buffer.getvalue()
            else:
                return ""
        except Exception as err:
            # log err
            return ""

    @staticmethod
    async def upload_prices_for_supplier_csv(supplier_id, file: UploadFile):
        if not Supplier.supplier_exists(supplier_id):
            return {"success": False, "message": "Supplier not found."}

        try:
            contents = await file.read()
            df = pd.read_csv(io.StringIO(contents.decode('utf-8')))

            required_columns = ['product_code', 'price(ZAR)']
            if not all(col in df.columns for col in required_columns):
                return {"success": False, "message": f"CSV must contain these columns: {', '.join(required_columns)}"}

            updated_count = 0
            failed_count = 0

            for index, row in df.iterrows():
                try:
                    product_code = str(row['product_code']).strip()
                    price = float(row['price(ZAR)'])

                    if not product_code or pd.isna(row['price(ZAR)']):
                        failed_count += 1
                        continue

                    result = SupplierItem.update_supplier_price(supplier_id, product_code, price)
                    if result["success"]:
                        updated_count += 1
                    else:
                        failed_count += 1
                except (ValueError, TypeError):
                    failed_count += 1
                    continue

            return {
                "success": True,
                "message": f"Upload complete. Products updated: {updated_count}. Rows failed: {failed_count}."
            }

        except Exception as err:
            return {"success": False, "message": f"Upload failed: {err}"}


if __name__ == "__main__":
    try:
        supplier_products_table.create_index("supplier_id")
    except errors.OperationFailure as e:
        pass