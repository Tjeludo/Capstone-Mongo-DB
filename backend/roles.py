from .connect import connect_to_mongodb
from pymongo import errors
import pandas as pd
import io
from fastapi import UploadFile

roles_table = connect_to_mongodb("roles")


class Role:
    # UPDATED: Removed description from the constructor
    def __init__(self, name, abbrev, status="active"):
        self.role_name = name
        self.role_abbrev = abbrev
        self.permissions = []
        self.status = status
        self.role_code = self.generate_role_code()

    # Add role
    def add_role(self):
        if self.role_code is None:
            return {"success": False, "message": "Failed to add role.", "data": ""}

        # UPDATED: Document no longer includes description
        doc = {
            "role_name": self.role_name,
            "role_abbrev": self.role_abbrev,
            "role_code": self.role_code,
            "permissions": self.permissions,
            "status": self.status
        }

        if Role.role_exists(self.role_abbrev):
            return {"success": False, "message": "Role already exists.", "data": ""}
        else:
            try:
                roles_table.insert_one(doc)
                return {"success": True, "message": "Role created successfully.", "data": ""}
            except errors.OperationFailure as err:
                # log err here
                return {"success": False, "message": "Role creation failed.", "data": ""}

    # --- (generate_role_code and other static methods are mostly unchanged) ---

    # Generate role code
    def generate_role_code(self):
        existing_role = roles_table.find_one(
            {"role_name": self.role_name, "role_abbrev": self.role_abbrev})
        if existing_role:
            return existing_role['role_code']
        try:
            doc = roles_table.find_one(sort=[("role_code", -1)])
            if doc and "role_code" in doc:
                last_id = doc["role_code"]
                new_id = last_id + 5
            else:
                new_id = 0
            return new_id
        except errors.OperationFailure as err:
            return None
        except ValueError as err:
            return None

    # --- (view_role_permissions is now by role_code) ---
    @staticmethod
    def view_role_permissions(role_code):
        try:
            role_doc = roles_table.find_one({"role_code": role_code}, {"_id": 0})
            if role_doc and "permissions" in role_doc:
                permissions = role_doc.get("permissions", [])
                return {"success": True, "message": "Permissions retrieved.", "data": permissions}
            else:
                return {"success": False, "message": "Role not found or has no permissions.", "data": ""}
        except errors.OperationFailure:
            return {"success": False, "message": "Failed to retrieve role permissions.", "data": ""}

    # Lookup by abbreviation
    @staticmethod
    def role_exists(role_abbrev):
        try:
            doc = roles_table.find_one({"role_abbrev": role_abbrev})
            return bool(doc)
        except errors.OperationFailure as err:
            return False

    # Lookup by role code
    @staticmethod
    def role_exists_by_code(role_code):
        try:
            doc = roles_table.find_one({"role_code": role_code})
            return bool(doc)
        except errors.OperationFailure as err:
            return False

    # Update role
    @staticmethod
    def update_role(role_code, name, abbrev, status):
        if Role.role_exists_by_code(role_code):
            try:
                # UPDATED: $set operation no longer includes description
                roles_table.update_one(
                    {"role_code": role_code},
                    {"$set": {
                        "role_name": name,
                        "role_abbrev": abbrev,
                        "status": status
                    }})
                return {"success": True, "message": "Role updated successfully.", "data": ""}
            except errors.OperationFailure as err:
                return {"success": False, "message": "Role update failed.", "data": ""}
        else:
            return {"success": False, "message": "Role does not exist.", "data": ""}

    # --- (view_roles, delete_role, search_role, and download_roles_csv are unchanged) ---
    @staticmethod
    def delete_role(role_code):
        if Role.role_exists_by_code(role_code):
            try:
                roles_table.delete_one({"role_code": role_code})
                return {"success": True, "message": "Role deleted successfully.", "data": ""}
            except errors.OperationFailure as err:
                return {"success": False, "message": "Role deletion failed.", "data": ""}
        else:
            return {"success": False, "message": "Role does not exist.", "data": ""}

    @staticmethod
    def view_roles():
        try:
            cursor = roles_table.find({}, {"_id": 0})
            data = list(cursor)
            if data:
                return {"success": True, "message": "", "data": data}
            else:
                return {"success": False, "message": "No roles found.", "data": ""}
        except errors.OperationFailure as err:
            return {"success": False, "message": "Failed to retrieve roles.", "data": ""}

    @staticmethod
    def search_role(role_code):
        if Role.role_exists_by_code(role_code):
            try:
                doc = roles_table.find_one({"role_code": role_code}, {"_id": 0})
                if doc:
                    return {"success": True, "message": "", "data": doc}
                else:
                    return {"success": False, "message": "Role not found.", "data": ""}
            except errors.OperationFailure as err:
                return {"success": False, "message": "Failed to retrieve role.", "data": ""}
        else:
            return {"success": False, "message": "Role does not exist.", "data": ""}

    @staticmethod
    def download_roles_csv():
        try:
            data = list(roles_table.find({}, {"_id": 0}))
            if data:
                df = pd.DataFrame(data)
                csv_buffer = io.StringIO()
                df.to_csv(csv_buffer, index=False)
                csv_content = csv_buffer.getvalue()
                return csv_content
            else:
                return ""
        except errors.OperationFailure as err:
            pass

    # Upload csv
    @staticmethod
    async def upload_roles_csv(file: UploadFile):
        try:
            existing_count = roles_table.count_documents({})
            if existing_count > 0:
                return {"success": False, "message": "Upload failed. Table already contains data.", "data": ""}

            contents = await file.read()
            df = pd.read_csv(io.StringIO(contents.decode('utf-8')))

            # UPDATED: Required and allowed columns no longer include 'description'
            required_columns = ['role_name', 'role_abbrev', 'status']
            if not all(col in df.columns for col in required_columns):
                return {"success": False,
                        "message": f"CSV must contain exactly these columns:\n{', '.join(required_columns)}",
                        "data": ""}

            allowed_columns = ['role_name', 'role_abbrev', 'status']
            extra_columns = [col for col in df.columns if col not in allowed_columns]
            if extra_columns:
                return {"success": False,
                        "message": f"CSV contains unexpected columns.\nOnly {', '.join(allowed_columns)} are allowed.",
                        "data": ""}

            has_errors = False
            for index, row in df.iterrows():
                try:
                    name = str(row['role_name']).strip()
                    abbrev = str(row['role_abbrev']).strip()
                    status = str(row['status']).strip().lower()

                    if not name or name.lower() == 'nan' or pd.isna(row['role_name']):
                        continue
                    if not abbrev or abbrev.lower() == 'nan' or pd.isna(row['role_abbrev']):
                        continue
                    if status not in ['active', 'inactive']:
                        status = 'active'

                    # UPDATED: Role instantiation no longer includes description
                    role = Role(name, abbrev, status)
                    result = role.add_role()

                    if not result["success"]:
                        has_errors = True
                        break
                except Exception as row_error:
                    has_errors = True
                    break

            if has_errors:
                return {"success": False, "message": "Upload failed.", "data": ""}
            else:
                return {"success": True, "message": "Upload successful.\nSetup complete.", "data": ""}

        except Exception as err:
            return {"success": False, "message": f"Upload failed.", "data": ""}


if __name__ == "__main__":
    try:
        roles_table.create_index("role_abbrev", unique=True)
        print("[SUCCESS] role_abbrev index created.")
    except errors.OperationFailure as e:
        print(f"[ERROR] Index creation failed for role_abbrev: {e}")

    roles = {
        "ADM": "Administrator",
        "MGR": "Branch Manager",
        "SUP": "Supervisor",
        "INV": "Inventory Clerk",
        "BRS": "Backroom Staff",
        "IT": "IT Specialist"
    }

    for abbrev, name in roles.items():
        Role(name, abbrev).add_role()  # UPDATED: No description passed