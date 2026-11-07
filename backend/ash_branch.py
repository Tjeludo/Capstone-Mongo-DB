from pymongo import errors
from .connect import connect_to_mongodb
import pandas as pd
import io
from fastapi import UploadFile

branch_table = connect_to_mongodb("testing")
branch_stock_table = connect_to_mongodb("branch_stock")


class Branch:
    def __init__(self, manager, city, email, phone, address):
        self.branch_manager = manager
        self.branch_city = city
        self.email = email
        self.phone_number = phone
        self.address = address
        self.branch_code = self.generate_branch_code()

    # Add branch
    def add_branch(self):
        if self.branch_code is None:
            return {"success": False, "message": "Failed to add branch.", "data": ""}

        doc = {
            "branch_code": self.branch_code,
            "branch_manager": self.branch_manager,
            "branch_city": self.branch_city,
            "email": self.email,
            "phone_number": self.phone_number,
            "address": self.address
        }

        stock_doc = {
            "branch_code": self.branch_code,
            "stock": []
        }

        if Branch.branch_exists(self.branch_code):
            return {"success": False, "message": "Branch already exists.", "data": ""}
        else:
            try:
                branch_table.insert_one(doc)
                # branch_stock_table.insert_one(stock_doc)
                return {"success": True, "message": "Branch created successfully.", "data": ""}
            except errors.OperationFailure as err:
                # log err here
                return {"success": False, "message": "Branch creation failed.", "data": ""}

    # Generate branch code
    def generate_branch_code(self):
        # Check if branch with same details already exists
        existing_branch = branch_table.find_one({
            "branch_manager": self.branch_manager,
            "branch_city": self.branch_city,
            "email": self.email
        })
        if existing_branch:
            return existing_branch['branch_code']

        # Generate new code if branch doesn't exist
        try:
            doc = branch_table.find_one(sort=[("branch_code", -1)])
            if doc and "branch_code" in doc:
                last_id = doc["branch_code"]
                # Extract numeric part
                num = int(last_id[1:])
                # Increment and check bounds
                if num >= 99:
                    raise ValueError("Maximum branch_code reached (B99)")
                new_id = num + 1
            else:
                new_id = 1
            # format: B01
            branch_code = f"B{new_id:02d}"
            return branch_code
        except errors.OperationFailure as err:
            # log err
            return None
        except ValueError as err:
            # log err
            return None

    # Lookup
    @staticmethod
    def branch_exists(branch_code):
        try:
            doc = branch_table.find_one({"branch_code": branch_code})
            if doc:
                return True
            else:
                return False
        except errors.OperationFailure as err:
            # log err
            return False

    # Update branch
    @staticmethod
    def update_branch(branch_code, manager, city, email, phone, address):
        # check if branch exists
        if Branch.branch_exists(branch_code):
            try:
                branch_table.update_one(
                    {"branch_code": branch_code},
                    {"$set": {
                        "branch_manager": manager,
                        "branch_city": city,
                        "email": email,
                        "phone_number": phone,
                        "address": address
                    }})
                return {"success": True, "message": "Branch updated.", "data": ""}
            except errors.OperationFailure as err:
                # log err
                return {"success": False, "message": "Branch update failed.", "data": ""}
        else:
            return {"success": False, "message": "Branch does not exist.", "data": ""}

    # View branches
    @staticmethod
    def view_branches():
        try:
            cursor = branch_table.find({}, {"_id": 0})
            data = list(cursor)
            if data:
                return {"success": True, "message": "", "data": data}
            else:
                return {"success": False, "message": "No branches found.", "data": ""}
        except errors.OperationFailure as err:
            # log err
            return {"success": False, "message": "Failed to retrieve branches.", "data": ""}

    # Delete branch
    @staticmethod
    def delete_branch(branch_code):
        if Branch.branch_exists(branch_code):
            try:
                # Delete from both branch and branch_stock collections
                branch_table.delete_one({"branch_code": branch_code})
                branch_stock_table.delete_one({"branch_code": branch_code})
                return {"success": True, "message": "Branch deleted successfully.", "data": ""}
            except errors.OperationFailure as err:
                # log err
                return {"success": False, "message": "Branch deletion failed.", "data": ""}
        else:
            return {"success": False, "message": "Branch does not exist.", "data": ""}

    # Search branch
    @staticmethod
    def search_branch(branch_code):
        if Branch.branch_exists(branch_code):
            try:
                doc = branch_table.find_one({"branch_code": branch_code}, {"_id": 0})
                if doc:
                    return {"success": True, "message": "", "data": doc}
                else:
                    return {"success": False, "message": "Branch not found.", "data": ""}
            except errors.OperationFailure as err:
                # log error
                return {"success": False, "message": "Failed to retrieve branch.", "data": ""}
        else:
            return {"success": False, "message": "Branch does not exist.", "data": ""}

    # Download csv
    @staticmethod
    def download_branch_csv():
        try:
            data = list(branch_table.find({}, {"_id": 0}))
            if data:
                df = pd.DataFrame(data)
                csv_buffer = io.StringIO()
                df.to_csv(csv_buffer, index=False)
                csv_content = csv_buffer.getvalue()
                return csv_content
            else:
                return ""
        except errors.OperationFailure as err:
            # log err
            pass

    # Upload csv
    @staticmethod
    async def upload_branches_csv(file: UploadFile):
        try:
            # Check if table is empty - only allow upload on empty table
            existing_count = branch_table.count_documents({})
            if existing_count > 0:
                return {
                    "success": False,
                    "message": "Upload failed. Table already contains data.",
                    "data": ""
                }

            # Read file content
            contents = await file.read()

            # Create DataFrame from CSV
            df = pd.read_csv(io.StringIO(contents.decode('utf-8')))

            # Validate required columns
            required_columns = ['branch_manager', 'branch_city', 'email', 'phone_number', 'address']
            if not all(col in df.columns for col in required_columns):
                return {
                    "success": False,
                    "message": f"CSV must contain exactly these columns:\n{', '.join(required_columns)}",
                    "data": ""
                }

            # Check for unexpected columns (optional)
            allowed_columns = ['branch_manager', 'branch_city', 'email', 'phone_number', 'address']
            extra_columns = [col for col in df.columns if col not in allowed_columns]
            if extra_columns:
                return {
                    "success": False,
                    "message": f"CSV contains unexpected columns.\nOnly {', '.join(allowed_columns)} are allowed.",
                    "data": ""
                }

            # Process each row
            has_errors = False

            for index, row in df.iterrows():
                try:
                    # Extract and validate data
                    manager = str(row['branch_manager']).strip()
                    city = str(row['branch_city']).strip()
                    email = str(row['email']).strip()
                    phone = str(row['phone_number']).strip()
                    address = str(row['address']).strip()

                    # Skip empty rows
                    required_fields = [manager, city, email, phone, address]
                    if any(not field or field.lower() == 'nan' or pd.isna(field) for field in
                           [row['branch_manager'], row['branch_city'], row['email'], row['phone_number'],
                            row['address']]):
                        continue

                    # Create new branch
                    branch = Branch(manager, city, email, phone, address)
                    result = branch.add_branch()

                    if not result["success"]:
                        has_errors = True
                        break

                except Exception as row_error:
                    has_errors = True
                    break

            # Return simple success or failure
            if has_errors:
                return {
                    "success": False,
                    "message": "Upload failed.",
                    "data": ""
                }
            else:
                return {
                    "success": True,
                    "message": "Upload successful.\nSetup complete.",
                    "data": ""
                }

        except Exception as err:
            return {
                "success": False,
                "message": f"Upload failed.",
                "data": ""
            }


if __name__ == "__main__":
    try:
        branch_table.create_index("branch_code", unique=True)
        print("[SUCCESS] branch_code index created.")
    except errors.OperationFailure as e:
        print(f"[ERROR] Index creation failed for branch_code: {e}")

