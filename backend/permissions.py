from pymongo import errors
from .connect import connect_to_mongodb
import pandas as pd
import io
from fastapi import UploadFile
from datetime import datetime

permissions_table = connect_to_mongodb("permissions")
roles_table = connect_to_mongodb("roles")
staff_table = connect_to_mongodb("staff")


class Permission:
    # UPDATED: Removed perm_description from constructor
    def __init__(self, perm_name, status='active'):
        self.perm_name = perm_name  # name format: VIEW_USERS
        self.perm_status = status
        self.perm_id = self.generate_perm_id()
        self.creation_date = datetime.now().strftime("%d-%m-%Y %H:%M")

    # Add permission
    def add_permission(self):
        if self.perm_id is None:
            return {"success": False, "message": "Failed to add permission.", "data": ""}

        # UPDATED: Document no longer includes perm_description
        doc = {"perm_id": self.perm_id,
               "perm_name": self.perm_name,
               "perm_status": self.perm_status,
               "creation_date": self.creation_date}

        if Permission.permission_exists(self.perm_id):
            return {"success": False, "message": "Permission already exists.", "data": ""}
        else:
            try:
                permissions_table.insert_one(doc)
                return {"success": True, "message": "Permission created successfully.", "data": ""}
            except errors.OperationFailure as err:
                # log err here
                return {"success": False, "message": "Permission creation failed.", "data": ""}

    # --- (generate_perm_id and permission_exists methods are unchanged) ---
    def generate_perm_id(self):
        existing_permission = permissions_table.find_one({"perm_name": self.perm_name})
        if existing_permission:
            return existing_permission['perm_id']
        try:
            doc = permissions_table.find_one(sort=[("perm_id", -1)])
            if doc and "perm_id" in doc:
                last_id = doc["perm_id"]
                num = int(last_id.split('-')[1])
                if num >= 999:
                    raise ValueError("Maximum perm_id reached (PERM-999)")
                new_id = num + 1
            else:
                new_id = 1
            perm_id = f"PERM-{new_id:03d}"
            return perm_id
        except errors.OperationFailure as err:
            return None
        except ValueError as err:
            return None
    @staticmethod
    def permission_exists(perm_id):
        try:
            return bool(permissions_table.find_one({"perm_id": perm_id}))
        except errors.OperationFailure as err:
            return False

    # Update permission
    @staticmethod
    def update_permission(perm_id, perm_name, status):
        if Permission.permission_exists(perm_id):
            try:
                # UPDATED: $set operation no longer includes perm_description
                permissions_table.update_one(
                    {"perm_id": perm_id},
                    {"$set": {"perm_name": perm_name,
                              "perm_status": status}})
                return {"success": True, "message": "Permission updated.", "data": ""}
            except errors.OperationFailure as err:
                return {"success": False, "message": "Permission update failed.", "data": ""}
        else:
            return {"success": False, "message": "Permission does not exist.", "data": ""}

    # --- (view_permissions, search_permission, grant, revoke, and get_user_permissions are unchanged) ---
    @staticmethod
    def view_permissions():
        try:
            cursor = permissions_table.find({}, {"_id": 0})
            data = list(cursor)
            if data:
                return {"success": True, "message": "", "data": data}
            else:
                return {"success": False, "message": "No permissions found.", "data": ""}
        except errors.OperationFailure as err:
            return {"success": False, "message": "Failed to retrieve permissions.", "data": ""}
    @staticmethod
    def search_permission(perm_id):
        if Permission.permission_exists(perm_id):
            try:
                doc = permissions_table.find_one({"perm_id": perm_id}, {"_id": 0})
                if doc:
                    return {"success": True, "message": "", "data": doc}
                else:
                    return {"success": False, "message": "Permission not found.", "data": ""}
            except errors.OperationFailure as err:
                return {"success": False, "message": "Failed to retrieve permission.", "data": ""}
        else:
            return {"success": False, "message": "Permission does not exist.", "data": ""}
    @staticmethod
    def grant_permission(perm_id, role_name):
        if not Permission.permission_exists(perm_id):
            return {"success": False, "message": "Permission does not exist.", "data": ""}
        try:
            role_doc = roles_table.find_one({"role_name": role_name})
            if not role_doc:
                return {"success": False, "message": "Role does not exist.", "data": ""}
            perm_doc = permissions_table.find_one({"perm_id": perm_id})
            perm_name = perm_doc["perm_name"]
            if "permissions" in role_doc and perm_name in role_doc["permissions"]:
                return {"success": False, "message": "Permission already assigned to role.", "data": ""}
            roles_table.update_one({"role_name": role_name}, {"$addToSet": {"permissions": perm_name}})
            return {"success": True, "message": "Permission assigned successfully.", "data": ""}
        except errors.OperationFailure as err:
            return {"success": False, "message": "Failed to assign permission.", "data": ""}
    @staticmethod
    def revoke_permission(perm_id, role_name):
        if not Permission.permission_exists(perm_id):
            return {"success": False, "message": "Permission does not exist.", "data": ""}
        try:
            role_doc = roles_table.find_one({"role_name": role_name})
            if not role_doc:
                return {"success": False, "message": "Role does not exist.", "data": ""}
            perm_doc = permissions_table.find_one({"perm_id": perm_id})
            perm_name = perm_doc["perm_name"]
            if "permissions" not in role_doc or perm_name not in role_doc["permissions"]:
                return {"success": False, "message": "Permission not assigned to role.", "data": ""}
            roles_table.update_one({"role_name": role_name}, {"$pull": {"permissions": perm_name}})
            return {"success": True, "message": "Permission revoked successfully.", "data": ""}
        except errors.OperationFailure as err:
            return {"success": False, "message": "Failed to revoke permission.", "data": ""}
    @staticmethod
    def get_user_permissions(staff_id):
        try:
            staff_doc = staff_table.find_one({"staff_id": staff_id})
            if not staff_doc:
                return {"success": False, "message": "User not found.", "data": ""}
            role_name = staff_doc.get("role")
            if not role_name:
                return {"success": False, "message": "User has no assigned role.", "data": ""}
            role_doc = roles_table.find_one({"role_name": role_name})
            if not role_doc:
                return {"success": False, "message": "Role not found.", "data": ""}
            permissions_list = role_doc.get("permissions", [])
            return {"success": True, "message": "Permissions retrieved successfully.", "data": permissions_list}
        except errors.OperationFailure as err:
            return {"success": False, "message": "Failed to retrieve user permissions.", "data": ""}

    # Download csv
    @staticmethod
    def download_permissions_csv():
        try:
            data = list(permissions_table.find({}, {"_id": 0}))
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
    async def upload_permissions_csv(file: UploadFile):
        try:
            # Check if table is empty - only allow upload on empty table
            if permissions_table.count_documents({}) > 0:
                return {"success": False, "message": "Upload failed. Table already contains data.", "data": ""}

            contents = await file.read()
            df = pd.read_csv(io.StringIO(contents.decode('utf-8')))

            # UPDATED: Validate required columns - no longer includes perm_description
            required_columns = ['perm_name', 'perm_status']
            if not all(col in df.columns for col in required_columns):
                return {"success": False, "message": f"CSV must contain exactly these columns:\n{', '.join(required_columns)}", "data": ""}

            # UPDATED: Check for unexpected columns
            allowed_columns = ['perm_name', 'perm_status']
            extra_columns = [col for col in df.columns if col not in allowed_columns]
            if extra_columns:
                return {"success": False, "message": f"CSV contains unexpected columns.\nOnly {', '.join(allowed_columns)} are allowed.", "data": ""}

            has_errors = False
            for index, row in df.iterrows():
                try:
                    perm_name = str(row['perm_name']).strip()
                    perm_status = str(row['perm_status']).strip()

                    # Skip empty rows
                    if not perm_name or pd.isna(row['perm_name']):
                        continue
                    if not perm_status or pd.isna(row['perm_status']):
                        continue

                    # Create new permission (this will auto-generate perm_id)
                    # UPDATED: Instantiation no longer includes description
                    permission = Permission(perm_name, perm_status)
                    result = permission.add_permission()

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
        permissions_table.create_index("perm_id", unique=True)
    except errors.OperationFailure as e:
        pass

    nav_menu = ["home",
                "branches",
                "categories",
                "products",
                "orders",
                "suppliers",
                "staff",
                "iam",
                "users",
                "roles",
                "permissions",
                "guests",
                "logs",
                "deliveries",
                "inventory",
                "transfers",
                "analytics",
                "distributions",
                "stock_take",
                "category_products",
                "supplier_products",
                "message_requests",
                "profile",
                "password",
                "setting"]

    actions = ["view", "add", "edit", "delete", "download", "upload", "transfer", "cancel", "status", "promote", "place", "confirm","approve","decline","print"]

    permissions_to_add = [f"{action}_{view}" for view in nav_menu for action in actions]
    roles_table.update_one({"role_name": "Administrator"},{"$addToSet":  {"permissions": {"$each": permissions_to_add}}})
    # for view in nav_menu:
    #     for action in actions:
    #         Permission(f"{action}_{view}").add_permission()